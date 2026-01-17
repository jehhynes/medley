using Medley.Domain.Entities;

namespace Medley.Application.Interfaces;

/// <summary>
/// Service for managing article version history
/// </summary>
public interface IArticleVersionService
{
    /// <summary>
    /// Capture a new version of article content
    /// </summary>
    /// <param name="articleId">The article ID</param>
    /// <param name="newContent">The new content to save</param>
    /// <param name="previousContent">The previous content (for diff generation)</param>
    /// <param name="userId">The user making the change</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The captured version information</returns>
    Task<ArticleVersionServiceDto> CaptureUserVersionAsync(Guid articleId, string newContent, string? previousContent, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get version history for an article
    /// </summary>
    /// <param name="articleId">The article ID</param>
    /// <param name="versionType">Optional version type filter (User, AI, or null for all)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of versions ordered by version number descending</returns>
    Task<List<ArticleVersionServiceDto>> GetVersionsAsync(Guid articleId, Domain.Enums.VersionType? versionType = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get raw markdown content for version comparison
    /// </summary>
    /// <param name="versionId">The version ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Version comparison data with before and after markdown content</returns>
    Task<ArticleVersionComparisonServiceDto?> GetVersionComparisonAsync(Guid versionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create an AI-generated version of an article
    /// </summary>
    /// <param name="articleId">The article ID</param>
    /// <param name="content">The AI-generated content</param>
    /// <param name="changeMessage">Description of the changes</param>
    /// <param name="userId">The user ID</param>
    /// <param name="conversationId">Optional conversation ID for tracking</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created AI version information</returns>
    Task<ArticleVersionServiceDto> CreateAiVersionAsync(
        Guid articleId, 
        string content, 
        string changeMessage, 
        Guid? conversationId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Accept an AI version by creating a new User version with the AI content
    /// </summary>
    /// <param name="versionId">The AI version ID to accept</param>
    /// <param name="user">The user accepting the version</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created User version information</returns>
    Task<ArticleVersionServiceDto> AcceptAiVersionAsync(
        Guid versionId, 
        User user, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reject an AI version by marking it as rejected with review tracking
    /// </summary>
    /// <param name="versionId">The AI version ID to reject</param>
    /// <param name="user">The user rejecting the version</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task RejectAiVersionAsync(
        Guid versionId,
        User user,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// DTO for article version information (internal to version service)
/// </summary>
public class ArticleVersionServiceDto
{
    public Guid Id { get; set; }
    public string VersionNumber { get; set; } = string.Empty;
    public Guid? CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public string? CreatedByEmail { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public bool IsNewVersion { get; set; }
    public string VersionType { get; set; } = string.Empty;
    public string? ChangeMessage { get; set; }
    
    /// <summary>
    /// Computed status of this version
    /// </summary>
    public string Status { get; set; } = string.Empty;
    
    /// <summary>
    /// When this version was reviewed (for AI versions)
    /// </summary>
    public DateTimeOffset? ReviewedAt { get; set; }
    
    /// <summary>
    /// User ID who reviewed this version (for AI versions)
    /// </summary>
    public Guid? ReviewedById { get; set; }
    
    /// <summary>
    /// Name of user who reviewed this version (for AI versions)
    /// </summary>
    public string? ReviewedByName { get; set; }
}

/// <summary>
/// DTO for article version comparison with raw markdown content (internal to version service)
/// </summary>
public class ArticleVersionComparisonServiceDto
{
    public string BeforeContent { get; set; } = string.Empty;
    public string AfterContent { get; set; } = string.Empty;
    public string VersionNumber { get; set; } = string.Empty;
}


