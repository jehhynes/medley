using Hangfire;
using Hangfire.Console;
using Hangfire.MissionControl;
using Hangfire.Server;
using Medley.Application.Interfaces;
using Medley.Domain.Entities;
using Medley.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Medley.Application.Jobs;

/// <summary>
/// Background job for clustering fragments using K-means clustering
/// </summary>
[MissionLauncher]
public class FragmentClusteringJob : BaseHangfireJob<FragmentClusteringJob>
{
    private readonly IFragmentClusteringService _clusteringService;
    private readonly IRepository<ClusteringSession> _sessionRepository;
    private readonly IClusterRepository _clusterRepository;

    public FragmentClusteringJob(
        IFragmentClusteringService clusteringService,
        IRepository<ClusteringSession> sessionRepository,
        IClusterRepository clusterRepository,
        IUnitOfWork unitOfWork,
        ILogger<FragmentClusteringJob> logger) : base(unitOfWork, logger)
    {
        _clusteringService = clusteringService;
        _sessionRepository = sessionRepository;
        _clusterRepository = clusterRepository;
    }

    /// <summary>
    /// Executes fragment clustering using K-means
    /// </summary>
    /// <param name="context">Hangfire perform context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [Mission]
    [DisableConcurrentExecution(timeoutInSeconds: 10)]
    public async Task ExecuteAsync(
        PerformContext context,
        CancellationToken cancellationToken)
    {
        LogInfo(context, "Starting Fragment Clustering Job");

        // Step 1: Create clustering session in a separate transaction
        var sessionId = await ExecuteWithTransactionAsync(async () =>
        {
            var session = new ClusteringSession
            {
                Id = Guid.NewGuid(),
                Method = ClusteringMethod.KMeans,
                Status = ClusteringStatus.Running,
                CreatedAt = DateTimeOffset.UtcNow
            };

            await _sessionRepository.Add(session);
            
            LogInfo(context, $"Created clustering session {session.Id}");
            
            return session.Id;
        });

        // Step 2: Perform clustering in a separate long-running transaction
        try
        {
            await ExecuteWithTransactionAsync(async () =>
            {
                // Reload the session
                var session = await _sessionRepository.GetByIdAsync(sessionId, cancellationToken);
                if (session == null)
                {
                    throw new InvalidOperationException($"Clustering session {sessionId} not found");
                }

                // Perform K-means clustering
                var clusters = await _clusteringService.PerformClusteringAsync(session, cancellationToken);
                var clusterList = clusters.ToList();

                if (!clusterList.Any())
                {
                    LogWarning(context, "No clusters were created");
                    session.Status = ClusteringStatus.Completed;
                    session.StatusMessage = "No clusters created - insufficient fragments or all filtered out";
                    session.CompletedAt = DateTimeOffset.UtcNow;
                    return;
                }

                // Add clusters to repository (EF Core will handle many-to-many)
                foreach (var cluster in clusterList)
                {
                    await _clusterRepository.Add(cluster);
                }

                await _unitOfWork.SaveChangesAsync();

                LogInfo(context, $"Created {clusterList.Count} clusters");

                // Calculate metrics for each cluster
                for (int i = 0; i < clusterList.Count; i++)
                {
                    var cluster = clusterList[i];
                    await _clusteringService.CalculateClusterMetricsAsync(cluster, cancellationToken);
                    
                    LogDebug($"Cluster {cluster.ClusterNumber}: {cluster.FragmentCount} fragments, " +
                            $"Cohesion={cluster.IntraClusterDistance:F4}");
                }

                await _unitOfWork.SaveChangesAsync();

                // Update session status
                session.Status = ClusteringStatus.Completed;
                session.StatusMessage = $"Successfully created {clusterList.Count} clusters from {session.FragmentCount} fragments";
                session.CompletedAt = DateTimeOffset.UtcNow;

                LogSuccess(context, $"Clustering completed successfully!");
                LogInfo(context, $"Session {session.Id}: {session.ClusterCount} clusters, {session.FragmentCount} fragments");
                
                // Log cluster size distribution
                var sizeDistribution = clusterList
                    .GroupBy(c => c.FragmentCount)
                    .OrderBy(g => g.Key)
                    .Select(g => $"{g.Key} fragments: {g.Count()} clusters");
                
                LogInfo(context, "Cluster size distribution:");
                foreach (var dist in sizeDistribution)
                {
                    LogInfo(context, $"  {dist}");
                }
            });
        }
        catch (Exception ex)
        {
            LogError(context, ex, "Clustering failed");
            
            // Update session status in a separate transaction
            await ExecuteWithTransactionAsync(async () =>
            {
                var session = await _sessionRepository.GetByIdAsync(sessionId, cancellationToken);
                if (session != null)
                {
                    session.Status = ClusteringStatus.Failed;
                    session.StatusMessage = $"Error: {ex.Message}";
                    session.CompletedAt = DateTimeOffset.UtcNow;
                }
            });
            
            throw;
        }

        LogInfo(context, "Fragment Clustering Job completed");
    }
}
