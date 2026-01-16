namespace Medley.Domain.Enums;

/// <summary>
/// Computed status for an article version based on its relationship to other versions
/// and review state. This is not stored in the database but computed at runtime.
/// </summary>
public enum VersionStatus
{
    /// <summary>
    /// Current active User version (matches Article.CurrentVersionId)
    /// </summary>
    CurrentVersion,
    
    /// <summary>
    /// Superseded User version (older than current)
    /// </summary>
    OldVersion,
    
    /// <summary>
    /// Latest AI version for a parent User version, pending review
    /// </summary>
    PendingAiVersion,
    
    /// <summary>
    /// AI version that was explicitly accepted
    /// </summary>
    AcceptedAiVersion,
    
    /// <summary>
    /// AI version that was explicitly rejected
    /// </summary>
    RejectedAiVersion,
    
    /// <summary>
    /// Older AI version that was superseded by a newer AI version (not accepted/rejected)
    /// </summary>
    OldAiVersion
}
