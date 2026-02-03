namespace Medley.Domain.Enums;

/// <summary>
/// Clustering algorithm methods
/// </summary>
public enum ClusteringMethod
{
    /// <summary>
    /// Hierarchical Agglomerative Clustering
    /// </summary>
    HierarchicalAgglomerative = 1,
    
    /// <summary>
    /// K-Means clustering
    /// </summary>
    KMeans = 2,
    
    /// <summary>
    /// Density-Based Spatial Clustering of Applications with Noise
    /// </summary>
    DBSCAN = 3
}
