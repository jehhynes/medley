using Medley.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Microsoft.ML.Data;
using Pgvector;

namespace Medley.Application.Services;

/// <summary>
/// Service for bucketing fragments using K-means clustering
/// </summary>
public class KMeansBucketingService
{
    private readonly ILogger<KMeansBucketingService> _logger;
    
    private const int TARGET_BUCKET_SIZE = 50;
    private const int MAX_BUCKET_SIZE = 100;
    private const int MAX_ITERATIONS = 1000;

    public KMeansBucketingService(ILogger<KMeansBucketingService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Performs K-means bucketing to divide fragments into manageable buckets
    /// </summary>
    public async Task<List<List<FragmentEmbedding>>> PerformBucketingAsync(
        IEnumerable<dynamic> fragments,
        CancellationToken cancellationToken = default)
    {
        var fragmentList = fragments.Select(f => new FragmentEmbedding
        {
            Id = (Guid)f.Id,
            Embedding = ((Vector)f.Embedding).ToArray()
        }).ToList();

        _logger.LogInformation("Starting K-means bucketing for {Count} fragments", fragmentList.Count);

        var buckets = new List<List<FragmentEmbedding>>();
        var queue = new Queue<List<FragmentEmbedding>>();
        queue.Enqueue(fragmentList);

        while (queue.Count > 0)
        {
            var currentBatch = queue.Dequeue();

            if (currentBatch.Count <= MAX_BUCKET_SIZE)
            {
                // Bucket is small enough
                buckets.Add(currentBatch);
                _logger.LogDebug("Bucket finalized with {Count} fragments", currentBatch.Count);
            }
            else
            {
                // Split into smaller buckets using K-means
                var numberOfClusters = (int)Math.Floor((double)currentBatch.Count / TARGET_BUCKET_SIZE);
                _logger.LogInformation(
                    "Splitting batch of {Count} fragments into {ClusterCount} sub-buckets",
                    currentBatch.Count, numberOfClusters);

                var subBuckets = await RunKMeansAsync(currentBatch, numberOfClusters, cancellationToken);

                foreach (var subBucket in subBuckets)
                {
                    queue.Enqueue(subBucket);
                }
            }
        }

        _logger.LogInformation(
            "K-means bucketing complete: {BucketCount} buckets created (avg size: {AvgSize:F0})",
            buckets.Count, buckets.Average(b => b.Count));

        return buckets;
    }

    private async Task<List<List<FragmentEmbedding>>> RunKMeansAsync(
        List<FragmentEmbedding> fragments,
        int numberOfClusters,
        CancellationToken cancellationToken)
    {
        var mlContext = new MLContext(seed: 42);

        // Convert to ML.NET format
        var embeddingDimension = fragments.First().Embedding.Length;
        var dataPoints = fragments.Select(f => new EmbeddingDataPoint
        {
            FragmentId = f.Id.ToString("N"),
            Features = f.Embedding
        }).ToList();

        // Create schema with fixed-size vector
        var schemaDefinition = SchemaDefinition.Create(typeof(EmbeddingDataPoint));
        schemaDefinition["Features"].ColumnType = new VectorDataViewType(NumberDataViewType.Single, embeddingDimension);
        var dataView = mlContext.Data.LoadFromEnumerable(dataPoints, schemaDefinition);

        // Configure K-means
        var options = new Microsoft.ML.Trainers.KMeansTrainer.Options
        {
            FeatureColumnName = "Features",
            NumberOfClusters = numberOfClusters,
            MaximumNumberOfIterations = MAX_ITERATIONS
        };

        var pipeline = mlContext.Clustering.Trainers.KMeans(options);

        // Train and predict
        _logger.LogDebug("Training K-means model with {Clusters} clusters", numberOfClusters);
        var model = pipeline.Fit(dataView);
        var predictions = model.Transform(dataView);
        var predictedData = mlContext.Data.CreateEnumerable<ClusterPrediction>(predictions, reuseRowObject: false).ToList();

        // Group by cluster
        var fragmentsMap = fragments.ToDictionary(f => f.Id.ToString("N"));
        var clusterGroups = predictedData
            .GroupBy(p => p.PredictedClusterId)
            .Select(g => g.Select(p => fragmentsMap[p.FragmentId]).ToList())
            .ToList();

        _logger.LogDebug(
            "K-means created {ClusterCount} clusters (sizes: {Sizes})",
            clusterGroups.Count,
            string.Join(", ", clusterGroups.Select(g => g.Count)));

        return await Task.FromResult(clusterGroups);
    }

    #region Data Classes

    /// <summary>
    /// ML.NET data point for K-means clustering
    /// </summary>
    private class EmbeddingDataPoint
    {
        public required string FragmentId { get; set; }
        
        [VectorType]
        public float[] Features { get; set; } = [];
    }

    /// <summary>
    /// ML.NET prediction result from K-means
    /// </summary>
    private class ClusterPrediction
    {
        public string FragmentId { get; set; } = string.Empty;
        
        [ColumnName("PredictedLabel")]
        public uint PredictedClusterId { get; set; }
        
        [ColumnName("Score")]
        public float[] Distances { get; set; } = [];
    }

    #endregion
}

/// <summary>
/// Represents a fragment with its embedding for K-means bucketing
/// </summary>
public class FragmentEmbedding
{
    public Guid Id { get; set; }
    public float[] Embedding { get; set; } = [];
}
