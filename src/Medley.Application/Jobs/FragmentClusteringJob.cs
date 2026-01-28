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
public class FragmentClusteringJob : BaseHangfireJob<FragmentClusteringJob>
{
    private readonly IFragmentRepository _fragmentRepository;
    private readonly IRepository<FragmentCategory> _fragmentCategoryRepository;
    private readonly IRepository<AiPrompt> _promptRepository;
    private readonly IAiProcessingService _aiProcessingService;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly AiCallContext _aiCallContext;

    public FragmentClusteringJob(
        IFragmentRepository fragmentRepository,
        IRepository<FragmentCategory> fragmentCategoryRepository,
        IRepository<AiPrompt> promptRepository,
        IAiProcessingService aiProcessingService,
        IBackgroundJobClient backgroundJobClient,
        IUnitOfWork unitOfWork,
        ILogger<FragmentClusteringJob> logger,
        AiCallContext aiCallContext) : base(unitOfWork, logger)
    {
        _fragmentRepository = fragmentRepository;
        _fragmentCategoryRepository = fragmentCategoryRepository;
        _promptRepository = promptRepository;
        _aiProcessingService = aiProcessingService;
        _backgroundJobClient = backgroundJobClient;
        _aiCallContext = aiCallContext;
    }

    [Mission]
    public async Task ExecuteAsync(PerformContext context, CancellationToken cancellationToken)
    {
        LogInfo(context, "Starting Fragment Clustering Job");

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

            Guid? createdClusterId = null;
            var shouldContinue = await ExecuteWithTransactionAsync(async () =>
            {
                // 1) Grab the first unprocessed fragment that is not a cluster
                var candidate = await _fragmentRepository.Query()
                    .Where(f => !f.IsCluster && !f.ClusteringProcessed.HasValue && f.Embedding != null)
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
                    filter: q => q.Where(f => !f.IsCluster && f.Id != candidate.Id && !f.ClusteringProcessed.HasValue),
                    cancellationToken: cancellationToken
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
                    .Include(f => f.FragmentCategory)
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

                // 5) Pass contents to LLM
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

                // Get the representative fragment to determine category
                var representativeFragment = includedFragments.FirstOrDefault(f => f.Id == clusterResponse.RepresentativeFragmentId);
                if (representativeFragment == null)
                {
                    LogWarning(context, $"Representative fragment {clusterResponse.RepresentativeFragmentId} not found. Marking as processed.");
                    candidate.ClusteringProcessed = DateTimeOffset.UtcNow;
                    return true;
                }

                var fragmentCategory = await _fragmentCategoryRepository.Query().Where(x => x.Name == clusterResponse.Category).FirstOrDefaultAsync()
                    ?? throw new InvalidDataException("Could not find fragment category");

                // 6) Create new Fragment (Cluster)
                var cluster = new Fragment
                {
                    Id = Guid.NewGuid(),
                    Title = clusterResponse.Title.Trim().Substring(0, Math.Min(200, clusterResponse.Title.Trim().Length)),
                    Summary = clusterResponse.Summary.Trim().Substring(0, Math.Min(500, clusterResponse.Summary.Trim().Length)),
                    FragmentCategory = fragmentCategory,
                    Content = clusterResponse.Content.Trim().Substring(0, Math.Min(10000, clusterResponse.Content.Trim().Length)),
                    Confidence = clusterResponse.Confidence,
                    ConfidenceComment = clusterResponse.ConfidenceComment?.Trim().Substring(0, Math.Min(500, clusterResponse.ConfidenceComment.Trim().Length)),
                    ClusteringMessage = clusterResponse.Message?.Trim().Substring(0, Math.Min(2000, clusterResponse.Message.Trim().Length)),
                    IsCluster = true,
                    ClusteringProcessed = DateTimeOffset.UtcNow,
                    RepresentativeFragment = representativeFragment,
                    Source = null,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                await _fragmentRepository.AddAsync(cluster);
                createdClusterId = cluster.Id;

                LogSuccess(context, $"Created cluster {cluster.Id} with {includedFragments.Count} fragments (excluded {excludedFragments.Count})");

                // Update only included participants
                foreach (var participant in includedFragments)
                {
                    participant.ClusteredInto = cluster;
                    participant.ClusteringProcessed = DateTimeOffset.UtcNow;
                }


                LogDebug($"Created cluster {cluster.Id} with {includedFragments.Count} fragments. Message: {clusterResponse.Message}");
                processedCount++;
                return true;
            });

            // Trigger embedding generation for the newly created cluster (outside transaction)
            if (createdClusterId.HasValue)
            {
                var currentJobId = context.BackgroundJob.Id;
                _backgroundJobClient.ContinueJobWith<EmbeddingGenerationJob>(
                    currentJobId,
                    j => j.GenerateFragmentEmbeddings(default!, default, null, createdClusterId.Value));
                LogInfo(context, $"Enqueued embedding generation job for cluster fragment {createdClusterId.Value}");
            }

            if (!shouldContinue)
            {
                break;
            }
        }

        LogInfo(context, $"Fragment Clustering Job completed. Processed {processedCount} fragments in {(DateTimeOffset.UtcNow - startTime).TotalMinutes:F2} minutes.");
    }

    private async Task<FragmentClusteringResponse> GenerateClusterAsync(List<Fragment> fragments, Guid primaryFragmentId, CancellationToken cancellationToken = default)
    {
        using (_aiCallContext.SetContext(nameof(FragmentClusteringJob), nameof(GenerateClusterAsync), nameof(Fragment), fragments.FirstOrDefault()?.Id ?? Guid.Empty))
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

            var request = new FragmentClusteringRequest
            {
                PrimaryFragmentId = primaryFragmentId,
                PrimaryGuidance = clusteringPrompt.Content,
                FragmentWeighting = weightingPrompt.Content,
                Fragments = fragments.Select(f => new FragmentWithContentData
                {
                    Id = f.Id,
                    Title = f.Title,
                    Summary = f.Summary,
                    Category = f.FragmentCategory.Name,
                    Content = f.Content,
                    Confidence = f.Confidence,
                    ConfidenceComment = f.ConfidenceComment,
                    Source = f.Source != null ? new SourceData
                    {
                        Date = f.Source.Date,
                        SourceType = f.Source.Type.ToString(),
                        Scope = f.Source.IsInternal == true ? "Internal" : "External",
                        PrimarySpeaker = f.Source.PrimarySpeaker?.Name,
                        PrimarySpeakerTrustLevel = f.Source.PrimarySpeaker?.TrustLevel.ToString(),
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