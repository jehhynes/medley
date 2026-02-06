using Hangfire;
using Hangfire.Console;
using Hangfire.MissionControl;
using Hangfire.Server;
using Hangfire.Storage;
using Medley.Application.Interfaces;
using Medley.Application.Models.DTOs.Llm;
using Medley.Application.Services;
using Medley.Domain.Entities;
using Medley.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Medley.Application.Jobs;

[MissionLauncher]
public class KnowledgeUnitGeneratorJob : BaseHangfireJob<KnowledgeUnitGeneratorJob>
{
    private readonly IFragmentRepository _fragmentRepository;
    private readonly IKnowledgeUnitRepository _knowledgeUnitRepository;
    private readonly IRepository<KnowledgeCategory> _knowledgeCategoryRepository;
    private readonly IRepository<AiPrompt> _promptRepository;
    private readonly IRepository<FragmentKnowledgeUnit> _fragmentKnowledgeUnitRepository;
    private readonly IClusterRepository _clusterRepository;
    private readonly IAiProcessingService _aiProcessingService;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly AiCallContext _aiCallContext;

    public KnowledgeUnitGeneratorJob(
        IFragmentRepository fragmentRepository,
        IKnowledgeUnitRepository knowledgeUnitRepository,
        IRepository<KnowledgeCategory> knowledgeCategoryRepository,
        IRepository<AiPrompt> promptRepository,
        IRepository<FragmentKnowledgeUnit> fragmentKnowledgeUnitRepository,
        IClusterRepository clusterRepository,
        IAiProcessingService aiProcessingService,
        IBackgroundJobClient backgroundJobClient,
        IUnitOfWork unitOfWork,
        ILogger<KnowledgeUnitGeneratorJob> logger,
        AiCallContext aiCallContext) : base(unitOfWork, logger)
    {
        _fragmentRepository = fragmentRepository;
        _knowledgeUnitRepository = knowledgeUnitRepository;
        _knowledgeCategoryRepository = knowledgeCategoryRepository;
        _promptRepository = promptRepository;
        _fragmentKnowledgeUnitRepository = fragmentKnowledgeUnitRepository;
        _clusterRepository = clusterRepository;
        _aiProcessingService = aiProcessingService;
        _backgroundJobClient = backgroundJobClient;
        _aiCallContext = aiCallContext;
    }

    [Mission]
    [DisableConcurrentExecution(timeoutInSeconds: 1)]
    public async Task ExecuteAsync(Guid clusteringSessionId, PerformContext context, CancellationToken cancellationToken)
    {
        LogInfo(context, $"Starting Knowledge Unit Generation Job for clustering session {clusteringSessionId}");

        var maxDuration = TimeSpan.FromMinutes(10);
        var startTime = DateTimeOffset.UtcNow;
        var processedClusterCount = 0;

        while (DateTimeOffset.UtcNow - startTime < maxDuration)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                LogInfo(context, $"Cancellation requested after processing {processedClusterCount} clusters. Exiting job loop.");
                break;
            }

            var createdKnowledgeUnitIds = new List<Guid>();
            var shouldContinue = await ExecuteWithTransactionAsync(async () =>
            {
                // 1) Find all clusters within the clustering session that have unprocessed fragments
                // Server-side filtering: only get clusters where at least one fragment is unprocessed
                var largestCluster = await _clusterRepository.Query()
                    .Where(c => c.ClusteringSessionId == clusteringSessionId)
                    .Where(c => c.Fragments.Any(f => f.ClusteringProcessed == null))
                    .OrderByDescending(c => c.FragmentCount)
                    .FirstOrDefaultAsync(cancellationToken);

                if (largestCluster == null)
                {
                    LogInfo(context, "No more clusters with unprocessed fragments found. Job finished.");
                    return false;
                }

                LogInfo(context, $"Processing cluster {largestCluster.ClusterNumber} (total fragments: {largestCluster.FragmentCount})");

                // 3) Reload fragments with full navigation properties for clustering
                var clusterParticipants = await _fragmentRepository.Query()
                    .Where(f => f.Clusters.Any(c => c.Id == largestCluster.Id))
                    .Where(f => !f.ClusteringProcessed.HasValue)
                    .Include(f => f.KnowledgeCategory)
                    .Include(f => f.Source)
                        .ThenInclude(s => s!.PrimarySpeaker)
                    .Include(f => f.Source)
                        .ThenInclude(s => s!.Tags)
                            .ThenInclude(t => t.TagType)
                    .Include(f => f.Source)
                        .ThenInclude(s => s!.Tags)
                            .ThenInclude(t => t.TagOption)
                    .ToListAsync(cancellationToken);

                LogInfo(context, $"Loaded {clusterParticipants.Count} fragments from cluster {largestCluster.ClusterNumber}");

                // 4) Pass contents to LLM
                var clusterResponse = await GenerateClusterAsync(clusterParticipants, cancellationToken);
                
                // Check if LLM created any knowledge units
                if (!clusterResponse.KnowledgeUnits.Any())
                {
                    LogWarning(context, $"LLM did not create any knowledge units. Marking all {clusterParticipants.Count} fragments as processed.");
                    foreach (var fragment in clusterParticipants)
                    {
                        fragment.ClusteringProcessed = DateTimeOffset.UtcNow;
                    }
                    return true;
                }

                // Track which fragments were included in any knowledge unit
                var allIncludedFragmentIds = new HashSet<Guid>();

                // 5) Create each knowledge unit
                foreach (var kuCluster in clusterResponse.KnowledgeUnits)
                {
                    // Validate minimum fragments
                    if (kuCluster.FragmentIds.Count < 2)
                    {
                        LogWarning(context, $"Skipping knowledge unit '{kuCluster.Title}' - only {kuCluster.FragmentIds.Count} fragment(s)");
                        continue;
                    }

                    // Get fragments for this knowledge unit
                    var clusterFragments = clusterParticipants
                        .Where(f => kuCluster.FragmentIds.Contains(f.Id))
                        .ToList();

                    if (clusterFragments.Count < 2)
                    {
                        LogWarning(context, $"Skipping knowledge unit '{kuCluster.Title}' - invalid fragment IDs");
                        continue;
                    }

                    // Find category
                    var knowledgeCategory = await _knowledgeCategoryRepository.Query()
                        .FirstOrDefaultAsync(x => x.Name == kuCluster.Category, cancellationToken);
                    
                    if (knowledgeCategory == null)
                    {
                        LogWarning(context, $"Skipping knowledge unit '{kuCluster.Title}' - invalid category '{kuCluster.Category}'");
                        continue;
                    }

                    // Create knowledge unit
                    var knowledgeUnit = new KnowledgeUnit
                    {
                        Id = Guid.NewGuid(),
                        Title = kuCluster.Title.Trim().Substring(0, Math.Min(200, kuCluster.Title.Trim().Length)),
                        Summary = kuCluster.Summary.Trim().Substring(0, Math.Min(500, kuCluster.Summary.Trim().Length)),
                        Content = kuCluster.Content.Trim().Substring(0, Math.Min(10000, kuCluster.Content.Trim().Length)),
                        Confidence = kuCluster.Confidence,
                        ConfidenceComment = kuCluster.ConfidenceComment?.Trim().Substring(0, Math.Min(1000, kuCluster.ConfidenceComment.Trim().Length)),
                        ClusteringComment = kuCluster.ClusteringRationale?.Trim().Substring(0, Math.Min(2000, kuCluster.ClusteringRationale.Trim().Length)),
                        Category = knowledgeCategory,
                        KnowledgeCategoryId = knowledgeCategory.Id,
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    };

                    await _knowledgeUnitRepository.AddAsync(knowledgeUnit);
                    createdKnowledgeUnitIds.Add(knowledgeUnit.Id);

                    // Create many-to-many relationships
                    foreach (var fragment in clusterFragments)
                    {
                        var joinEntity = new FragmentKnowledgeUnit
                        {
                            Id = Guid.NewGuid(),
                            FragmentId = fragment.Id,
                            Fragment = fragment,
                            KnowledgeUnitId = knowledgeUnit.Id,
                            KnowledgeUnit = knowledgeUnit,
                            CreatedAt = DateTimeOffset.UtcNow
                        };
                        
                        await _fragmentKnowledgeUnitRepository.AddAsync(joinEntity);
                        allIncludedFragmentIds.Add(fragment.Id);
                    }

                    LogSuccess(context, $"Created KnowledgeUnit '{knowledgeUnit.Title}' ({knowledgeUnit.Id}) with {clusterFragments.Count} fragments");
                }

                // 6) Mark ALL processed fragments as ClusteringProcessed
                foreach (var fragment in clusterParticipants)
                {
                    fragment.ClusteringProcessed = DateTimeOffset.UtcNow;
                }

                var excludedCount = clusterParticipants.Count - allIncludedFragmentIds.Count;
                LogInfo(context, $"Created {createdKnowledgeUnitIds.Count} knowledge units. Included {allIncludedFragmentIds.Count} fragments, excluded {excludedCount}");
                
                if (!string.IsNullOrWhiteSpace(clusterResponse.Message))
                {
                    LogDebug($"LLM clustering message: {clusterResponse.Message}");
                }

                processedClusterCount++;
                return true;
            });

            if (!shouldContinue)
            {
                break;
            }
        }

        _backgroundJobClient.ContinueJobWith<EmbeddingGenerationJob>(
            context.BackgroundJob.Id,
            j => j.GenerateKnowledgeUnitEmbeddings(default!, default));

        LogInfo(context, $"Knowledge Unit Generation Job completed. Processed {processedClusterCount} clusters in {(DateTimeOffset.UtcNow - startTime).TotalMinutes:F2} minutes.");
    }

    private async Task<FragmentClusteringResponse> GenerateClusterAsync(List<Fragment> fragments, CancellationToken cancellationToken = default)
    {
        using (_aiCallContext.SetContext(nameof(KnowledgeUnitGeneratorJob), nameof(GenerateClusterAsync), nameof(Fragment), fragments.FirstOrDefault()?.Id ?? Guid.Empty))
        {
            // Retrieve the fragment clustering prompt template
            var clusteringPrompt = await _promptRepository.Query()
                .FirstOrDefaultAsync(t => t.Type == PromptType.KnowledgeUnitClustering, cancellationToken);

            if (clusteringPrompt == null)
            {
                throw new InvalidOperationException($"Fragment clustering prompt (PromptType.{nameof(PromptType.KnowledgeUnitClustering)}) is not configured in the database.");
            }

            // Retrieve the fragment weighting prompt template
            var weightingPrompt = await _promptRepository.Query()
                .FirstOrDefaultAsync(t => t.Type == PromptType.FragmentWeighting, cancellationToken);

            if (weightingPrompt == null)
            {
                throw new InvalidOperationException($"Fragment weighting prompt (PromptType.{nameof(PromptType.FragmentWeighting)}) is not configured in the database.");
            }

            // Get distinct categories from the fragments
            var distinctCategoryIds = fragments
                .Select(f => f.KnowledgeCategory.Id)
                .Distinct()
                .ToList();

            // Load category-specific prompts for the categories present in the fragments
            var categoryPrompts = await _promptRepository.Query()
                .Where(t => t.Type == PromptType.KnowledgeCategoryExtraction && 
                           t.KnowledgeCategoryId != null && 
                           distinctCategoryIds.Contains(t.KnowledgeCategoryId.Value))
                .Include(t => t.KnowledgeCategory)
                .ToListAsync(cancellationToken);

            // Build category guidance list
            var categoryDefinitions = categoryPrompts
                .Where(p => p.KnowledgeCategory != null)
                .Select(p => new CategoryDefinition
                {
                    Name = p.KnowledgeCategory!.Name,
                    Guidance = p.Content
                })
                .ToList();

            // Build system prompt with all guidance
            var guidanceRequest = new FragmentClusteringGuidance
            {
                PrimaryGuidance = clusteringPrompt.Content,
                FragmentWeighting = weightingPrompt.Content,
                CategoryDefinitions = categoryDefinitions
            };

            var systemPrompt = JsonSerializer.Serialize(guidanceRequest);

            // Build user prompt with fragments
            var userRequest = new FragmentClusteringRequest
            {
                Fragments = fragments.Select(f => new FragmentWithContentData
                {
                    Id = f.Id,
                    Title = f.Title,
                    //Summary = f.Summary,
                    Category = f.KnowledgeCategory.Name,
                    Content = f.Content,
                    Confidence = f.Confidence,
                    ConfidenceComment = f.ConfidenceComment,
                    Source = f.Source != null ? new SourceData
                    {
                        Date = f.Source.Date,
                        SourceType = f.Source.Type.ToString(),
                        Scope = f.Source.IsInternal == true ? "Internal" : "External",
                        PrimarySpeaker = f.Source.PrimarySpeaker?.Name,
                        PrimarySpeakerTrustLevel = f.Source.PrimarySpeaker?.TrustLevel,
                        Tags = f.Source.Tags.Select(t => new TagData
                        {
                            Type = t.TagType.Name,
                            Value = t.Value
                        }).ToList()
                    } : null
                }).ToList()
            };

            var userPrompt = JsonSerializer.Serialize(userRequest);

            return await _aiProcessingService.ProcessStructuredPromptAsync<FragmentClusteringResponse>(
                userPrompt: userPrompt,
                systemPrompt: systemPrompt,
                cancellationToken: cancellationToken);
        }
    }
}
