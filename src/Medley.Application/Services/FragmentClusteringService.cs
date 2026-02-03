using Medley.Application.Interfaces;
using Medley.Domain.Entities;
using Medley.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Medley.Application.Services;

/// <summary>
/// Orchestrates 2-stage clustering: K-means bucketing followed by HAC clustering
/// </summary>
public class FragmentClusteringService : IFragmentClusteringService
{
    private readonly ILogger<FragmentClusteringService> _logger;
    private readonly IFragmentRepository _fragmentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly KMeansBucketingService _bucketingService;
    private readonly HierarchicalClusteringService _hacService;

    public FragmentClusteringService(
        ILogger<FragmentClusteringService> logger,
        IFragmentRepository fragmentRepository,
        IUnitOfWork unitOfWork,
        KMeansBucketingService bucketingService,
        HierarchicalClusteringService hacService)
    {
        _logger = logger;
        _fragmentRepository = fragmentRepository;
        _unitOfWork = unitOfWork;
        _bucketingService = bucketingService;
        _hacService = hacService;
    }

    public async Task<IEnumerable<Cluster>> PerformClusteringAsync(
        ClusteringSession session, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Starting 2-stage clustering session {SessionId} with method {Method}, linkage {Linkage}, distance metric {Metric}",
            session.Id, session.Method, session.Linkage, session.DistanceMetric);

        try
        {
            // Load fragments with embeddings
            var fragments = await _fragmentRepository.Query()
                .Where(f => !f.IsDeleted && f.Embedding != null)
                .Select(f => new { f.Id, f.Embedding })
                .ToListAsync(cancellationToken);

            if (fragments.Count == 0)
            {
                _logger.LogWarning("No fragments with embeddings found for clustering");
                return [];
            }

            _logger.LogInformation("Processing {Count} fragments for 2-stage clustering", fragments.Count);
            session.FragmentCount = fragments.Count;

            // Stage 1: K-means bucketing
            var buckets = await _bucketingService.PerformBucketingAsync(fragments, cancellationToken);
            _logger.LogInformation("K-means bucketing created {BucketCount} buckets", buckets.Count);

            // Stage 2: HAC clustering within each bucket
            var allClusters = new List<Cluster>();
            int bucketNumber = 0;

            foreach (var bucket in buckets)
            {
                bucketNumber++;
                _logger.LogInformation(
                    "Processing bucket {BucketNumber}/{TotalBuckets} with {FragmentCount} fragments",
                    bucketNumber, buckets.Count, bucket.Count);

                var bucketClusters = await _hacService.ClusterBucketAsync(
                    session, 
                    bucket, 
                    bucketNumber,
                    cancellationToken);

                allClusters.AddRange(bucketClusters);
            }

            session.ClusterCount = allClusters.Count;
            session.Status = ClusteringStatus.Completed;
            session.CompletedAt = DateTimeOffset.UtcNow;
            session.StatusMessage = $"Successfully created {allClusters.Count} clusters from {fragments.Count} fragments across {buckets.Count} buckets";

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "2-stage clustering session {SessionId} completed successfully with {ClusterCount} clusters",
                session.Id, allClusters.Count);

            return allClusters;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "2-stage clustering session {SessionId} failed", session.Id);
            session.Status = ClusteringStatus.Failed;
            session.CompletedAt = DateTimeOffset.UtcNow;
            session.StatusMessage = $"Clustering failed: {ex.Message}";
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    public async Task CalculateClusterMetricsAsync(
        Cluster cluster, 
        CancellationToken cancellationToken = default)
    {
        await _hacService.CalculateClusterMetricsAsync(cluster, cancellationToken);
    }
}
