using Medley.Application.Interfaces;
using Medley.Domain.Entities;
using Medley.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pgvector;

namespace Medley.Application.Services;

/// <summary>
/// Service for clustering fragments using K-means clustering
/// </summary>
public class FragmentClusteringService(
    ILogger<FragmentClusteringService> logger,
    IFragmentRepository fragmentRepository,
    IClusterRepository clusterRepository,
    IUnitOfWork unitOfWork,
    KMeansBucketingService bucketingService) : IFragmentClusteringService
{
    private readonly ILogger<FragmentClusteringService> _logger = logger;
    private readonly IFragmentRepository _fragmentRepository = fragmentRepository;
    private readonly IClusterRepository _clusterRepository = clusterRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly KMeansBucketingService _bucketingService = bucketingService;

    public async Task<IEnumerable<Cluster>> PerformClusteringAsync(
        ClusteringSession session, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Starting K-means clustering session {SessionId}",
            session.Id);

        try
        {
            // Load fragments with clustering embeddings
            var fragments = await _fragmentRepository.Query()
                .Where(f => !f.IsDeleted && f.ClusteringEmbedding != null)
                .Select(f => new { f.Id, Embedding = f.ClusteringEmbedding })
                .ToListAsync(cancellationToken);

            if (fragments.Count == 0)
            {
                _logger.LogWarning("No fragments with clustering embeddings found for clustering");
                return [];
            }

            _logger.LogInformation("Processing {Count} fragments for K-means clustering", fragments.Count);
            session.FragmentCount = fragments.Count;

            // Perform K-means clustering
            var buckets = await _bucketingService.PerformBucketingAsync(fragments, cancellationToken);
            _logger.LogInformation("K-means clustering created {ClusterCount} clusters", buckets.Count);

            // Convert buckets to Cluster entities
            var clusters = new List<Cluster>();
            int clusterNumber = 0;

            foreach (var bucket in buckets)
            {
                clusterNumber++;
                
                var cluster = new Cluster
                {
                    Id = Guid.NewGuid(),
                    ClusteringSessionId = session.Id,
                    ClusteringSession = session,
                    ClusterNumber = clusterNumber,
                    FragmentCount = bucket.Count,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                // Load full fragment entities for the cluster
                var fragmentIds = bucket.Select(f => f.Id).ToList();
                var fragmentEntities = await _fragmentRepository.Query()
                    .Where(f => fragmentIds.Contains(f.Id))
                    .ToListAsync(cancellationToken);

                // Add fragments to cluster (EF Core will handle many-to-many)
                foreach (var fragment in fragmentEntities)
                {
                    cluster.Fragments.Add(fragment);
                }

                clusters.Add(cluster);
            }

            session.ClusterCount = clusters.Count;
            session.Status = ClusteringStatus.Completed;
            session.CompletedAt = DateTimeOffset.UtcNow;
            session.StatusMessage = $"Successfully created {clusters.Count} clusters from {fragments.Count} fragments";

            _logger.LogInformation(
                "K-means clustering session {SessionId} completed successfully with {ClusterCount} clusters",
                session.Id, clusters.Count);

            return clusters;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "K-means clustering session {SessionId} failed", session.Id);
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
        _logger.LogDebug("Calculating metrics for cluster {ClusterId}", cluster.Id);

        // Load cluster with fragments
        var clusterWithFragments = await _clusterRepository.GetWithFragmentsAsync(cluster.Id, cancellationToken);
        
        if (clusterWithFragments == null || clusterWithFragments.Fragments.Count == 0)
        {
            _logger.LogWarning("Cluster {ClusterId} has no fragments", cluster.Id);
            return;
        }

        var fragments = clusterWithFragments.Fragments
            .Where(f => f.ClusteringEmbedding != null)
            .ToList();

        if (fragments.Count == 0)
        {
            _logger.LogWarning("Cluster {ClusterId} has no fragments with clustering embeddings", cluster.Id);
            return;
        }

        // Calculate centroid
        var centroid = CalculateCentroid(fragments.Select(f => f.ClusteringEmbedding!.ToArray()).ToList());
        cluster.Centroid = new Vector(centroid);

        // Calculate intra-cluster distance (average distance to centroid)
        var distances = fragments
            .Select(f => CalculateCosineDistance(f.ClusteringEmbedding!.ToArray(), centroid))
            .ToList();

        cluster.IntraClusterDistance = distances.Average();

        _logger.LogDebug(
            "Cluster {ClusterId} metrics: {FragmentCount} fragments, intra-cluster distance: {Distance:F4}",
            cluster.Id, fragments.Count, cluster.IntraClusterDistance);
    }

    #region Private Helper Methods

    private static float[] CalculateCentroid(List<float[]> embeddings)
    {
        if (embeddings.Count == 0)
        {
            return [];
        }

        var dimensions = embeddings[0].Length;
        var centroid = new float[dimensions];

        foreach (var embedding in embeddings)
        {
            for (int i = 0; i < dimensions; i++)
            {
                centroid[i] += embedding[i];
            }
        }

        for (int i = 0; i < dimensions; i++)
        {
            centroid[i] /= embeddings.Count;
        }

        return centroid;
    }

    private static double CalculateCosineDistance(float[] x, float[] y)
    {
        if (x.Length != y.Length)
        {
            throw new ArgumentException("Embeddings must have the same length");
        }

        double dotProduct = 0.0;
        double magnitudeX = 0.0;
        double magnitudeY = 0.0;

        for (int i = 0; i < x.Length; i++)
        {
            dotProduct += x[i] * y[i];
            magnitudeX += x[i] * x[i];
            magnitudeY += y[i] * y[i];
        }

        magnitudeX = Math.Sqrt(magnitudeX);
        magnitudeY = Math.Sqrt(magnitudeY);

        if (magnitudeX == 0.0 || magnitudeY == 0.0)
        {
            return 1.0; // Maximum distance if either vector is zero
        }

        var cosineSimilarity = dotProduct / (magnitudeX * magnitudeY);
        return 1.0 - cosineSimilarity; // Convert similarity to distance
    }

    #endregion
}
