namespace Medley.Domain.Enums;

/// <summary>
/// Distance metrics for clustering
/// </summary>
public enum DistanceMetric
{
    /// <summary>
    /// Cosine distance (1 - cosine similarity) - best for embeddings
    /// </summary>
    Cosine = 1,
    
    /// <summary>
    /// Euclidean distance (L2 norm) - traditional for Ward linkage
    /// </summary>
    Euclidean = 2
}
