namespace Medley.Domain.Enums;

/// <summary>
/// Linkage types for hierarchical clustering
/// </summary>
public enum LinkageType
{
    /// <summary>
    /// Ward's minimum variance method
    /// </summary>
    Ward = 1,
    
    /// <summary>
    /// Single linkage (minimum distance)
    /// </summary>
    Single = 2,
    
    /// <summary>
    /// Complete linkage (maximum distance)
    /// </summary>
    Complete = 3,
    
    /// <summary>
    /// Average linkage (UPGMA)
    /// </summary>
    Average = 4
}
