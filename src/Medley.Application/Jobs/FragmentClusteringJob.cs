using Hangfire;
using Hangfire.Console;
using Hangfire.MissionControl;
using Hangfire.Server;
using Hangfire.Storage;
using Medley.Application.Interfaces;
using Medley.Application.Services;
using Medley.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Medley.Application.Jobs;

[MissionLauncher]
public class FragmentClusteringJob : BaseHangfireJob<FragmentClusteringJob>
{
    private readonly IFragmentRepository _fragmentRepository;
    private readonly IRepository<FragmentCategory> _fragmentCategoryRepository;
    private readonly IAiProcessingService _aiProcessingService;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly AiCallContext _aiCallContext;

    public FragmentClusteringJob(
        IFragmentRepository fragmentRepository,
        IRepository<FragmentCategory> fragmentCategoryRepository,
        IAiProcessingService aiProcessingService,
        IBackgroundJobClient backgroundJobClient,
        IUnitOfWork unitOfWork,
        ILogger<FragmentClusteringJob> logger,
        AiCallContext aiCallContext) : base(unitOfWork, logger)
    {
        _fragmentRepository = fragmentRepository;
        _fragmentCategoryRepository = fragmentCategoryRepository;
        _aiProcessingService = aiProcessingService;
        _backgroundJobClient = backgroundJobClient;
        _aiCallContext = aiCallContext;
    }

    [Mission]
    public async Task ExecuteAsync(PerformContext context, CancellationToken cancellationToken)
    {
        LogInfo(context, "Starting Fragment Clustering Job");

        for (int i = 0; i < 10; i++) //Process 10 records per job run to avoid long executions
        {
            if (cancellationToken.IsCancellationRequested)
            {
                LogInfo(context, "Cancellation requested. Exiting job loop.");
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
                    cancellationToken);

                var similarFragments = similarResults
                    .Select(r => r.Fragment)
                    .Where(f => f.Id != candidate.Id && !f.IsCluster)
                    .ToList();

                if (!similarFragments.Any())
                {
                    LogInfo(context, $"No similar fragments found for {candidate.Id}. Marking as processed.");
                    candidate.ClusteringProcessed = DateTimeOffset.UtcNow;
                    return true;
                }

                var clusterParticipants = new List<Fragment> { candidate };
                clusterParticipants.AddRange(similarFragments);

                LogInfo(context, $"Found {similarFragments.Count} similar fragments to cluster.");

                // 5) Pass contents to LLM
                var clusterResponse = await GenerateClusterAsync(clusterParticipants, cancellationToken);
                if (clusterResponse == null)
                {
                    LogWarning(context, $"Failed to generate cluster content for {candidate.Id}. Marking as processed to avoid loops.");
                    candidate.ClusteringProcessed = DateTimeOffset.UtcNow;
                    return true;
                }

                var fragmentCategory = await _fragmentCategoryRepository.Query().Where(x => x.Name == clusterResponse.Category).FirstOrDefaultAsync()
                    ?? await _fragmentCategoryRepository.Query().Where(x => x.Name == "How-To").FirstOrDefaultAsync()
                    ?? throw new InvalidDataException("Could not find fragment category");

                // 6) Create new Fragment (Cluster)
                var cluster = new Fragment
                {
                    Id = Guid.NewGuid(),
                    Title = clusterResponse.Title.Trim().Substring(0, Math.Min(200, clusterResponse.Title.Trim().Length)),
                    Summary = clusterResponse.Summary.Trim().Substring(0, Math.Min(500, clusterResponse.Summary.Trim().Length)),
                    FragmentCategory = fragmentCategory,
                    Content = clusterResponse.Content.Trim().Substring(0, Math.Min(10000, clusterResponse.Content.Trim().Length)),
                    IsCluster = true,
                    ClusteringProcessed = DateTimeOffset.UtcNow,
                    Source = null,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                await _fragmentRepository.AddAsync(cluster);
                createdClusterId = cluster.Id;

                LogSuccess(context, $"Created cluster {cluster.Id} with {clusterParticipants.Count} fragments");

                // Update participants
                foreach (var participant in clusterParticipants)
                {
                    participant.ClusteredInto = cluster;
                    participant.ClusteringProcessed = DateTimeOffset.UtcNow;
                }

                LogDebug($"Created cluster {cluster.Id} with {clusterParticipants.Count} fragments. Message: {clusterResponse.Message}");
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
    }

    private async Task<FragmentClusteringResponse?> GenerateClusterAsync(List<Fragment> fragments, CancellationToken cancellationToken = default)
    {
        using (_aiCallContext.SetContext(nameof(FragmentClusteringJob), nameof(GenerateClusterAsync), nameof(Fragment), fragments.FirstOrDefault()?.Id ?? Guid.Empty))
        {
            var serializedFragments = JsonSerializer.Serialize(fragments.Select(f => new 
            {
                f.Title,
                f.Summary,
                Category = f.FragmentCategory.Name,
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
    [Required]
    [Description("Clear, descriptive heading for the clustered content")]
    public required string Title { get; set; }
    [Required]
    [Description("Short, human-readable condensation of the full content")]
    public required string Summary { get; set; }
    [Required]
    [Description("The most appropriate category for this cluster (e.g. Decision, Action Item, Feature Request)")]
    public required string Category { get; set; }
    [Required]
    [Description("The full consolidated text content")]
    public required string Content { get; set; }
    
    [Description("Any auxiliary information, reasoning, or comments")]
    public string? Message { get; set; }
}
