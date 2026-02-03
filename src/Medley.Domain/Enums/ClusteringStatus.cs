namespace Medley.Domain.Enums;

/// <summary>
/// Status of a clustering session
/// </summary>
public enum ClusteringStatus
{
    /// <summary>
    /// Session created but not yet started
    /// </summary>
    Pending = 1,
    
    /// <summary>
    /// Clustering is currently in progress
    /// </summary>
    Running = 2,
    
    /// <summary>
    /// Clustering completed successfully
    /// </summary>
    Completed = 3,
    
    /// <summary>
    /// Clustering failed with an error
    /// </summary>
    Failed = 4
}
