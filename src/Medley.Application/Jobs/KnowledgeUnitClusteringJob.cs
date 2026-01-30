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
    private readonly IAiProcessingService _aiProcessingService;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly AiCallContext _aiCallContext;

    public KnowledgeUnitClusteringJob(
        IFragmentRepository fragmentRepository,
        IKnowledgeUnitRepository knowledgeUnitRepository,
        IRepository<KnowledgeCategory> knowledgeCategoryRepository,
        IRepository<AiPrompt> promptRepository,
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

            Guid? createdKnowledgeUnitId = null;
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
                var clusterResponse = await GenerateClusterAsync(clusterParticipants, candidate.Id, cancellationToken);
                
                // Validate that at least some fragments were included
                if (!clusterResponse.IncludedFragmentIds.Any())
                {
                    LogWarning(context, $"LLM did not include any fragments in cluster for {candidate.Id}. Marking as processed.");
                    candidate.ClusteringProcessed = DateTimeOffset.UtcNow;
                    return true;
                }

                // Filter to only included fragments
                var includedFragments = clusterParticipants
                    .Where(f => clusterResponse.IncludedFragmentIds.Contains(f.Id))
                    .ToList();

                var excludedFragments = clusterParticipants
                    .Where(f => !clusterResponse.IncludedFragmentIds.Contains(f.Id))
                    .ToList();

                if (excludedFragments.Any())
                {
                    LogInfo(context, $"LLM excluded {excludedFragments.Count} fragments as unrelated: {string.Join(", ", excludedFragments.Select(f => f.Id))}");
                }

                // If only one fragment is included, don't create a cluster
                if (includedFragments.Count == 1)
                {
                    LogInfo(context, $"Only one fragment included in cluster for {candidate.Id}. Marking as processed without creating cluster.");
                    candidate.ClusteringProcessed = DateTimeOffset.UtcNow;
                    return true;
                }

                var knowledgeCategory = await _knowledgeCategoryRepository.Query().Where(x => x.Name == clusterResponse.Category).FirstOrDefaultAsync()
                    ?? throw new InvalidDataException("Could not find knowledge category");

                // 4) Create new KnowledgeUnit entity
                var knowledgeUnit = new KnowledgeUnit
                {
                    Id = Guid.NewGuid(),
                    Title = clusterResponse.Title.Trim().Substring(0, Math.Min(200, clusterResponse.Title.Trim().Length)),
                    Summary = clusterResponse.Summary.Trim().Substring(0, Math.Min(500, clusterResponse.Summary.Trim().Length)),
                    Content = clusterResponse.Content.Trim().Substring(0, Math.Min(10000, clusterResponse.Content.Trim().Length)),
                    Confidence = clusterResponse.Confidence,
                    ConfidenceComment = clusterResponse.ConfidenceComment?.Trim().Substring(0, Math.Min(1000, clusterResponse.ConfidenceComment.Trim().Length)),
                    ClusteringComment = clusterResponse.Message?.Trim().Substring(0, Math.Min(2000, clusterResponse.Message.Trim().Length)),
                    Category = knowledgeCategory,
                    KnowledgeCategoryId = knowledgeCategory.Id,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                };

                await _knowledgeUnitRepository.AddAsync(knowledgeUnit);
                createdKnowledgeUnitId = knowledgeUnit.Id;

                LogSuccess(context, $"Created KnowledgeUnit {knowledgeUnit.Id} with {includedFragments.Count} fragments (excluded {excludedFragments.Count})");

                // 5) Link fragments to KnowledgeUnit and set ClusteringProcessed timestamp
                foreach (var participant in includedFragments)
                {
                    participant.KnowledgeUnit = knowledgeUnit;
                    participant.KnowledgeUnitId = knowledgeUnit.Id;
                    participant.ClusteringProcessed = DateTimeOffset.UtcNow;
                }

                LogDebug($"Created KnowledgeUnit {knowledgeUnit.Id} with {includedFragments.Count} fragments. Message: {clusterResponse.Message}");
                processedCount++;
                return true;
            });

            // Trigger embedding generation for the newly created KnowledgeUnit (outside transaction)
            if (createdKnowledgeUnitId.HasValue)
            {
                var currentJobId = context.BackgroundJob.Id;
                _backgroundJobClient.ContinueJobWith<EmbeddingGenerationJob>(
                    currentJobId,
                    j => j.GenerateKnowledgeUnitEmbeddings(default!, default, createdKnowledgeUnitId.Value));
                LogInfo(context, $"Enqueued embedding generation job for KnowledgeUnit {createdKnowledgeUnitId.Value}");
            }

            if (!shouldContinue)
            {
                break;
            }
        }

        LogInfo(context, $"Knowledge Unit Clustering Job completed. Processed {processedCount} fragments in {(DateTimeOffset.UtcNow - startTime).TotalMinutes:F2} minutes.");
    }

    private async Task<FragmentClusteringResponse> GenerateClusterAsync(List<Fragment> fragments, Guid primaryFragmentId, CancellationToken cancellationToken = default)
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
                PrimaryFragmentId = primaryFragmentId,
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
            var systemPrompt = "You are a knowledge clustering assistant. Process the provided JSON request containing instructions and fragments.";

            return await _aiProcessingService.ProcessStructuredPromptAsync<FragmentClusteringResponse>(
                userPrompt: userPrompt,
                systemPrompt: systemPrompt,
                cancellationToken: cancellationToken);
        }
    }
}
