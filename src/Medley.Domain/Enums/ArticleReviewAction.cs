namespace Medley.Domain.Enums;

/// <summary>
/// Represents the action taken when reviewing an article for approval
/// </summary>
public enum ArticleReviewAction
{
    /// <summary>
    /// Approve the article
    /// </summary>
    Approve = 1,
    
    /// <summary>
    /// Request changes to the article
    /// </summary>
    RequestChanges = 2,
    
    /// <summary>
    /// Comment only (no approval or rejection)
    /// </summary>
    Comment = 3
}
