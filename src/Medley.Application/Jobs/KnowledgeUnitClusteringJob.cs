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
public class KnowledgeUnitClusteringJob : BaseHangfireJob<KnowledgeUnitClusteringJob>
{
    private readonly IFragmentRepository _fragmentRepository;
    private readonly IKnowledgeUnitRepository _knowledgeUnitRepository;
    private readonly IRepository<KnowledgeCategory> _knowledgeCategoryRepository;
    private readonly IRepository<AiPrompt> _promptRepository;
    private readonly IRepository<FragmentKnowledgeUnit> _fragmentKnowledgeUnitRepository;
    private readonly IAiProcessingService _aiProcessingService;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly AiCallContext _aiCallContext;

    public KnowledgeUnitClusteringJob(
        IFragmentRepository fragmentRepository,
        IKnowledgeUnitRepository knowledgeUnitRepository,
        IRepository<KnowledgeCategory> knowledgeCategoryRepository,
        IRepository<AiPrompt> promptRepository,
        IRepository<FragmentKnowledgeUnit> fragmentKnowledgeUnitRepository,
        IAiProcessingService aiProcessingService,
        IBackgroundJobClient backgroundJobClient,
        IUnitOfWork unitOfWork,
        ILogger<KnowledgeUnitClusteringJob> logger,
        AiCallContext aiCallContext) : base(unitOfWork, logger)
    {
        _fragmentRepository = fragmentRepository;
        _knowledgeUnitRepository = knowledgeUnitRepository;
        _knowledgeCategoryRepository = knowledgeCategoryRepository;
        _promptRepository = promptRepository;
        _fragmentKnowledgeUnitRepository = fragmentKnowledgeUnitRepository;
        _aiProcessingService = aiProcessingService;
        _backgroundJobClient = backgroundJobClient;
        _aiCallContext = aiCallContext;
    }

    [Mission]
    [DisableConcurrentExecution(timeoutInSeconds: 1)]
    public async Task ExecuteAsync(PerformContext context, CancellationToken cancellationToken)
    {
        LogInfo(context, "Starting Knowledge Unit Clustering Job");

        var maxDuration = TimeSpan.FromMinutes(10);
        var startTime = DateTimeOffset.UtcNow;
        var processedCount = 0;

        while (DateTimeOffset.UtcNow - startTime < maxDuration)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                LogInfo(context, $"Cancellation requested after processing {processedCount} fragments. Exiting job loop.");
                break;
            }

            var createdKnowledgeUnitIds = new List<Guid>();
            var shouldContinue = await ExecuteWithTransactionAsync(async () =>
            {
                // 1) Grab the first unprocessed fragment
                var candidate = await _fragmentRepository.Query()
                    .Where(f => !f.ClusteringProcessed.HasValue && f.Embedding != null)
                    .OrderByDescending(f => f.CreatedAt) 
                    .FirstOrDefaultAsync();

                if (candidate == null)
                {
                    LogInfo(context, "No more unprocessed fragments found. Job finished.");
                    return false;
                }

                LogInfo(context, $"Processing fragment {candidate.Id}");

                // 2) Query for similar fragments
                var minSimilarity = 0.85; // 0 to 1 where 1 is identical

                var similarResults = await _fragmentRepository.FindSimilarAsync(
                    candidate.Embedding!.ToArray(),
                    limit: 100,
                    minSimilarity: minSimilarity,
                    excludeClustered: true,
                    cancellationToken: cancellationToken,
                    filter: q => q.Where(f => f.Id != candidate.Id && !f.ClusteringProcessed.HasValue)
                );


                if (!similarResults.Any())
                {
                    LogInfo(context, $"No similar fragments found for {candidate.Id}. Marking as processed.");
                    candidate.ClusteringProcessed = DateTimeOffset.UtcNow;
                    return true;
                }

                // Reload fragments with navigation properties for clustering
                var fragmentIds = similarResults.Select(f => f.Fragment.Id).Append(candidate.Id).ToList();
                var clusterParticipants = await _fragmentRepository.Query()
                    .Where(f => fragmentIds.Contains(f.Id))
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

                LogInfo(context, $"Found {fragmentIds.Count} similar fragments to cluster.");

                // 3) Pass contents to LLM
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

                // Track which fragments were included in any cluster
                var allIncludedFragmentIds = new HashSet<Guid>();

                // 4) Create each knowledge unit
                foreach (var kuCluster in clusterResponse.KnowledgeUnits)
                {
                    // Validate minimum fragments
                    if (kuCluster.FragmentIds.Count < 2)
                    {
                        LogWarning(context, $"Skipping knowledge unit '{kuCluster.Title}' - only {kuCluster.FragmentIds.Count} fragment(s)");
                        continue;
                    }

                    // Get fragments for this cluster
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

                // 5) Mark ALL processed fragments as ClusteringProcessed
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

                processedCount++;
                return true;
            });

            // Trigger embedding generation for the newly created KnowledgeUnits (outside transaction)
            if (createdKnowledgeUnitIds.Any())
            {
                var currentJobId = context.BackgroundJob.Id;
                foreach (var knowledgeUnitId in createdKnowledgeUnitIds)
                {
                    _backgroundJobClient.ContinueJobWith<EmbeddingGenerationJob>(
                        currentJobId,
                        j => j.GenerateKnowledgeUnitEmbeddings(default!, default, knowledgeUnitId));
                }
                LogInfo(context, $"Enqueued embedding generation jobs for {createdKnowledgeUnitIds.Count} knowledge units");
            }

            if (!shouldContinue)
            {
                break;
            }
        }

        LogInfo(context, $"Knowledge Unit Clustering Job completed. Processed {processedCount} fragments in {(DateTimeOffset.UtcNow - startTime).TotalMinutes:F2} minutes.");
    }

    private async Task<FragmentClusteringResponse> GenerateClusterAsync(List<Fragment> fragments, CancellationToken cancellationToken = default)
    {
        using (_aiCallContext.SetContext(nameof(KnowledgeUnitClusteringJob), nameof(GenerateClusterAsync), nameof(Fragment), fragments.FirstOrDefault()?.Id ?? Guid.Empty))
        {
            // Retrieve the fragment clustering prompt template
            var clusteringPrompt = await _promptRepository.Query()
                .FirstOrDefaultAsync(t => t.Type == PromptType.FragmentClustering, cancellationToken);

            if (clusteringPrompt == null)
            {
                throw new InvalidOperationException($"Fragment clustering prompt (PromptType.{nameof(PromptType.FragmentClustering)}) is not configured in the database.");
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

            var request = new FragmentClusteringRequest
            {
                PrimaryGuidance = clusteringPrompt.Content,
                FragmentWeighting = weightingPrompt.Content,
                CategoryDefinitions = categoryDefinitions,
                Fragments = fragments.Select(f => new FragmentWithContentData
                {
                    Id = f.Id,
                    Title = f.Title,
                    Summary = f.Summary,
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

            var userPrompt = JsonSerializer.Serialize(request);
            var systemPrompt = "You are a knowledge clustering assistant. Process the provided JSON request containing instructions and fragments. Create multiple knowledge units when appropriate to maintain focused, granular clusters.";

            return await _aiProcessingService.ProcessStructuredPromptAsync<FragmentClusteringResponse>(
                userPrompt: userPrompt,
                systemPrompt: systemPrompt,
                cancellationToken: cancellationToken);
        }
    }
}
