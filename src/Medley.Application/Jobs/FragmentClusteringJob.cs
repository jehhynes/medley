using Hangfire;
using Hangfire.MissionControl;
using Hangfire.Server;
using Hangfire.Storage;
using Medley.Application.Interfaces;
using Medley.Application.Services;
using Medley.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Text.Json;

namespace Medley.Application.Jobs;

[MissionLauncher]
public class FragmentClusteringJob : BaseHangfireJob<FragmentClusteringJob>
{
    private readonly IFragmentRepository _fragmentRepository;
    private readonly IAiProcessingService _aiProcessingService;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly AiCallContext _aiCallContext;

    public FragmentClusteringJob(
        IFragmentRepository fragmentRepository,
        IAiProcessingService aiProcessingService,
        IBackgroundJobClient backgroundJobClient,
        IUnitOfWork unitOfWork,
        ILogger<FragmentClusteringJob> logger,
        AiCallContext aiCallContext) : base(unitOfWork, logger)
    {
        _fragmentRepository = fragmentRepository;
        _aiProcessingService = aiProcessingService;
        _backgroundJobClient = backgroundJobClient;
        _aiCallContext = aiCallContext;
    }

    [DisableMultipleQueuedItemsFilter]
    [Mission]
    public async Task ExecuteAsync(PerformContext context, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Fragment Clustering Job");

        for (int i = 0; i < 10; i++) //Process 10 records per job run to avoid long executions
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("Cancellation requested. Exiting job loop.");
                break;
            }

            Guid? createdClusterId = null;
            var shouldContinue = await ExecuteWithTransactionAsync(async () =>
            {
                // 1) Grab the first unprocessed fragment that is not a cluster
                var candidate = await _fragmentRepository.Query()
                    .Where(f => !f.IsCluster && !f.ClusteringProcessed.HasValue && f.Embedding != null)
                    .OrderBy(f => f.CreatedAt) // FIFO
                    .FirstOrDefaultAsync(cancellationToken);

                if (candidate == null)
                {
                    _logger.LogInformation("No more unprocessed fragments found. Job finished.");
                    return false;
                }

                _logger.LogInformation("Processing fragment {FragmentId}", candidate.Id);

                // 2) Query for similar fragments
                var minSimilarity = 0.85; // 0 to 1 where 1 is identical

                var similarResults = await _fragmentRepository.FindSimilarAsync(
                    candidate.Embedding!.ToArray(),
                    limit: 100,
                    minSimilarity: minSimilarity,
                    cancellationToken);

                var similarFragments = similarResults
                    .Select(r => r.Fragment)
                    .Where(f => f.Id != candidate.Id && !f.IsCluster)
                    .ToList();

                if (!similarFragments.Any())
                {
                    _logger.LogInformation("No similar fragments found for {FragmentId}. Marking as processed.", candidate.Id);
                    candidate.ClusteringProcessed = DateTimeOffset.UtcNow;
                    return true;
                }

                var clusterParticipants = new List<Fragment> { candidate };
                clusterParticipants.AddRange(similarFragments);

                _logger.LogInformation("Found {Count} similar fragments to cluster.", similarFragments.Count);

                // 5) Pass contents to LLM
                var clusterResponse = await GenerateClusterAsync(clusterParticipants, cancellationToken);
                if (clusterResponse == null)
                {
                    _logger.LogWarning("Failed to generate cluster content for {FragmentId}. Marking as processed to avoid loops.", candidate.Id);
                    candidate.ClusteringProcessed = DateTimeOffset.UtcNow;
                    return true;
                }

                // 6) Create new Fragment (Cluster)
                var cluster = new Fragment
                {
                    Id = Guid.NewGuid(),
                    Title = clusterResponse.Title?.Trim().Substring(0, Math.Min(200, clusterResponse.Title.Trim().Length)),
                    Summary = clusterResponse.Summary?.Trim().Substring(0, Math.Min(500, clusterResponse.Summary.Trim().Length)),
                    Category = clusterResponse.Category?.Trim().Substring(0, Math.Min(100, clusterResponse.Category.Trim().Length)),
                    Content = clusterResponse.Content?.Trim().Substring(0, Math.Min(10000, clusterResponse.Content.Trim().Length)) ?? string.Empty,
                    IsCluster = true,
                    ClusteringProcessed = DateTimeOffset.UtcNow,
                    Source = null,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                await _fragmentRepository.SaveAsync(cluster);
                createdClusterId = cluster.Id;

                // Update participants
                foreach (var participant in clusterParticipants)
                {
                    participant.ClusteredInto = cluster;
                    participant.ClusteringProcessed = DateTimeOffset.UtcNow;
                }

                _logger.LogInformation("Created cluster {ClusterId} with {ParticipantCount} fragments. Message: {Message}",
                    cluster.Id, clusterParticipants.Count, clusterResponse.Message);
                return true;
            });

            // Trigger embedding generation for the newly created cluster (outside transaction)
            if (createdClusterId.HasValue)
            {
                var currentJobId = context.BackgroundJob.Id;
                _backgroundJobClient.ContinueJobWith<EmbeddingGenerationJob>(
                    currentJobId,
                    j => j.GenerateFragmentEmbeddings(default!, default, null, createdClusterId.Value));
                _logger.LogInformation("Enqueued embedding generation job for cluster fragment {ClusterId}", createdClusterId.Value);
            }

            if (!shouldContinue)
            {
                break;
            }
        }
    }

    private async Task<FragmentClusteringResponse?> GenerateClusterAsync(List<Fragment> fragments, CancellationToken cancellationToken = default)
    {
        using (_aiCallContext.SetContext(nameof(FragmentClusteringJob), nameof(GenerateClusterAsync), nameof(Fragment), fragments.FirstOrDefault()?.Id ?? Guid.Empty))
        {
            var serializedFragments = JsonSerializer.Serialize(fragments.Select(f => new 
            {
                f.Title,
                f.Summary,
                f.Category,
                f.Content,
                Date = f.Source?.Date
            }));

            var systemPrompt = @"You are a knowledge clustering assistant.
Combine the list of knowledge fragments provided by the user into a single, cohesive, consolidated knowledge fragment.
Retain as much detail as possible from the original fragments but remove redundancy.
If conflicts exist then prefer the details from the more recent fragments.";

            return await _aiProcessingService.ProcessStructuredPromptAsync<FragmentClusteringResponse>(
                userPrompt: serializedFragments,
                systemPrompt: systemPrompt,
                cancellationToken: cancellationToken);
        }
    }
}

public class FragmentClusteringResponse
{
    [Description("Clear, descriptive heading for the clustered content")]
    public string? Title { get; set; }
    
    [Description("Short, human-readable condensation of the full content")]
    public string? Summary { get; set; }
    
    [Description("The most appropriate category for this cluster (e.g. Decision, Action Item, Feature Request)")]
    public string? Category { get; set; }
    
    [Description("The full consolidated text content")]
    public string? Content { get; set; }
    
    [Description("Any auxiliary information, reasoning, or comments")]
    public string? Message { get; set; }
}
