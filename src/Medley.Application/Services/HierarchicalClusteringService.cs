using Aglomera;
using Aglomera.Linkage;
using Medley.Application.Interfaces;
using Medley.Domain.Entities;
using Medley.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pgvector;

namespace Medley.Application.Services;

/// <summary>
/// Service for performing Hierarchical Agglomerative Clustering (HAC) on fragment buckets
/// </summary>
public class HierarchicalClusteringService
{
    private readonly ILogger<HierarchicalClusteringService> _logger;
    private readonly IFragmentRepository _fragmentRepository;
    private readonly IClusterRepository _clusterRepository;

    public HierarchicalClusteringService(
        ILogger<HierarchicalClusteringService> logger,
        IFragmentRepository fragmentRepository,
        IClusterRepository clusterRepository)
    {
        _logger = logger;
        _fragmentRepository = fragmentRepository;
        _clusterRepository = clusterRepository;
    }

    /// <summary>
    /// Performs HAC clustering on a single bucket of fragments
    /// </summary>
    public async Task<List<Cluster>> ClusterBucketAsync(
        ClusteringSession session,
        List<FragmentEmbedding> bucket,
        int bucketNumber,
        CancellationToken cancellationToken = default)
    {
        // Convert to Aglomera dataset
        var dataSet = ConvertBucketToDataSet(bucket, session.DistanceMetric);

        if (dataSet.Count == 0)
        {
            _logger.LogWarning("Bucket {BucketNumber} dataset is empty after conversion", bucketNumber);
            return [];
        }

        // Configure and run HAC
        var linkage = CreateLinkageCriterion(session.Linkage ?? LinkageType.Average, session.DistanceMetric);
        var algorithm = new AgglomerativeClusteringAlgorithm<DataPoint>(linkage);
        
        _logger.LogDebug("Running HAC on bucket {BucketNumber} with {Count} data points", bucketNumber, dataSet.Count);
        var clusteringResult = algorithm.GetClustering(dataSet);

        if (clusteringResult == null)
        {
            throw new InvalidOperationException($"HAC algorithm returned null result for bucket {bucketNumber}");
        }

        // Extract clusters at the desired threshold
        var hacClusters = ExtractClusters(clusteringResult, session.DistanceThreshold);
        _logger.LogDebug("Bucket {BucketNumber}: Extracted {Count} raw clusters", bucketNumber, hacClusters.Length);

        // Filter by minimum cluster size
        var validClusters = hacClusters
            .Where(c => c.Items.Count >= session.MinClusterSize)
            .ToList();

        if (session.MaxClusterSize.HasValue)
        {
            validClusters = validClusters
                .Where(c => c.Items.Count <= session.MaxClusterSize.Value)
                .ToList();
        }

        _logger.LogDebug(
            "Bucket {BucketNumber}: Found {ValidCount} valid clusters after filtering",
            bucketNumber, validClusters.Count);

        // Create cluster entities
        var clusterEntities = await CreateClusterEntitiesAsync(
            session, 
            validClusters, 
            cancellationToken);

        return clusterEntities;
    }

    /// <summary>
    /// Calculates metrics for a cluster (centroid and intra-cluster distance)
    /// </summary>
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
            .Where(f => f.Embedding != null)
            .ToList();

        if (fragments.Count == 0)
        {
            _logger.LogWarning("Cluster {ClusterId} has no fragments with embeddings", cluster.Id);
            return;
        }

        // Calculate centroid
        var centroid = CalculateCentroid(fragments.Select(f => f.Embedding!.ToArray()).ToList());
        cluster.Centroid = new Vector(centroid);

        // Calculate intra-cluster distance (average distance to centroid)
        var distances = fragments
            .Select(f => CalculateCosineDistance(f.Embedding!.ToArray(), centroid))
            .ToList();

        cluster.IntraClusterDistance = distances.Average();

        _logger.LogDebug(
            "Cluster {ClusterId} metrics: {FragmentCount} fragments, intra-cluster distance: {Distance:F4}",
            cluster.Id, fragments.Count, cluster.IntraClusterDistance);
    }

    #region Private Helper Methods

    private HashSet<DataPoint> ConvertBucketToDataSet(
        List<FragmentEmbedding> bucket,
        DistanceMetric distanceMetric)
    {
        var dataSet = new HashSet<DataPoint>();

        foreach (var fragment in bucket)
        {
            try
            {
                var dataPoint = new DataPoint(fragment.Id, fragment.Embedding, distanceMetric);
                dataSet.Add(dataPoint);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to convert fragment {FragmentId} to data point", fragment.Id);
            }
        }

        _logger.LogDebug("Converted {Count} fragments to data points", dataSet.Count);
        return dataSet;
    }

    private static ILinkageCriterion<DataPoint> CreateLinkageCriterion(
        LinkageType linkageType, 
        DistanceMetric distanceMetric)
    {
        var metric = new DataPoint(Guid.Empty, [], distanceMetric);

        return linkageType switch
        {
            LinkageType.Single => new SingleLinkage<DataPoint>(metric),
            LinkageType.Complete => new CompleteLinkage<DataPoint>(metric),
            LinkageType.Average => new AverageLinkage<DataPoint>(metric),
            LinkageType.Ward => new WardsMinimumVarianceLinkage<DataPoint>(metric, DataPoint.GetCentroid),
            _ => new AverageLinkage<DataPoint>(metric)
        };
    }

    private HacCluster[] ExtractClusters(
        ClusteringResult<DataPoint> clusteringResult, 
        double? distanceThreshold)
    {
        var clusterSets = clusteringResult.ToList();
        _logger.LogDebug("Found {Count} cluster sets in dendrogram", clusterSets.Count);

        if (clusterSets.Count == 0)
        {
            return [];
        }

        // Find cluster set at the specified distance threshold
        ClusterSet<DataPoint>? targetClusterSet;

        if (distanceThreshold.HasValue)
        {
            // Find the cluster set closest to the threshold
            targetClusterSet = clusterSets
                .Where(cs => cs.Dissimilarity <= distanceThreshold.Value && cs.Count > 0)
                .OrderByDescending(cs => cs.Dissimilarity)
                .FirstOrDefault();

            if (targetClusterSet == null)
            {
                // If no set below threshold, take the one with lowest dissimilarity
                targetClusterSet = clusterSets
                    .Where(cs => cs.Count > 0)
                    .OrderBy(cs => cs.Dissimilarity)
                    .FirstOrDefault();
            }

            _logger.LogDebug(
                "Selected cluster set at dissimilarity {Dissimilarity:F4} (threshold: {Threshold:F4}) with {Count} clusters",
                targetClusterSet?.Dissimilarity, distanceThreshold.Value, targetClusterSet?.Count);
        }
        else
        {
            // No threshold specified, use the median cluster set
            var medianIndex = clusterSets.Count / 2;
            targetClusterSet = clusterSets[medianIndex];
            
            _logger.LogDebug(
                "No threshold specified, using median cluster set at dissimilarity {Dissimilarity:F4} with {Count} clusters",
                targetClusterSet.Dissimilarity, targetClusterSet.Count);
        }

        if (targetClusterSet == null)
        {
            return [];
        }

        // Convert to HacCluster array
        var hacClusters = new List<HacCluster>();
        int clusterIndex = 0;

        foreach (var cluster in targetClusterSet)
        {
            var items = ExtractItemsFromCluster(cluster);
            if (items.Count > 0)
            {
                hacClusters.Add(new HacCluster(clusterIndex++, items));
            }
        }

        return [.. hacClusters];
    }

    private List<DataPoint> ExtractItemsFromCluster(Cluster<DataPoint> cluster)
    {
        var items = new List<DataPoint>();

        if (cluster == null || cluster.Count == 0)
        {
            return items;
        }

        try
        {
            foreach (var item in cluster)
            {
                items.Add(item);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract items from cluster");
        }

        return items;
    }

    private async Task<List<Cluster>> CreateClusterEntitiesAsync(
        ClusteringSession session,
        List<HacCluster> hacClusters,
        CancellationToken cancellationToken)
    {
        var clusterEntities = new List<Cluster>();

        // Load all fragments that will be assigned to clusters
        var allFragmentIds = hacClusters
            .SelectMany(c => c.Items.Select(i => i.Id))
            .Distinct()
            .ToList();

        var fragmentsDict = await _fragmentRepository.Query()
            .Where(f => allFragmentIds.Contains(f.Id))
            .ToDictionaryAsync(f => f.Id, cancellationToken);

        foreach (var hacCluster in hacClusters)
        {
            var clusterEntity = new Cluster
            {
                Id = Guid.NewGuid(),
                ClusteringSessionId = session.Id,
                ClusteringSession = session,
                ClusterNumber = hacCluster.Index + 1,
                FragmentCount = hacCluster.Items.Count,
                CreatedAt = DateTimeOffset.UtcNow
            };

            // Assign fragments to cluster
            foreach (var item in hacCluster.Items)
            {
                if (fragmentsDict.TryGetValue(item.Id, out var fragment))
                {
                    clusterEntity.Fragments.Add(fragment);
                }
            }

            await _clusterRepository.AddAsync(clusterEntity);
            clusterEntities.Add(clusterEntity);

            _logger.LogDebug(
                "Created cluster {ClusterNumber} with {FragmentCount} fragments",
                clusterEntity.ClusterNumber, clusterEntity.FragmentCount);
        }

        // Calculate metrics for all clusters
        foreach (var cluster in clusterEntities)
        {
            await CalculateClusterMetricsAsync(cluster, cancellationToken);
        }

        return clusterEntities;
    }

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

    #region Data Classes

    /// <summary>
    /// Represents a cluster from HAC algorithm
    /// </summary>
    private class HacCluster
    {
        public int Index { get; set; }
        public List<DataPoint> Items { get; set; }

        public HacCluster(int index, List<DataPoint> items)
        {
            Index = index;
            Items = items ?? [];
        }
    }

    #endregion
}
