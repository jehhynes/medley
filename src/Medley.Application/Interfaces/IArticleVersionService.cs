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
    Task<ArticleVersionDto> CaptureVersionAsync(Guid articleId, string newContent, string? previousContent, Guid? userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get version history for an article
    /// </summary>
    /// <param name="articleId">The article ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of versions ordered by version number descending</returns>
    Task<List<ArticleVersionDto>> GetVersionHistoryAsync(Guid articleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get raw markdown content for version comparison
    /// </summary>
    /// <param name="versionId">The version ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Version comparison data with before and after markdown content</returns>
    Task<ArticleVersionComparisonDto?> GetVersionComparisonAsync(Guid versionId, CancellationToken cancellationToken = default);
}

/// <summary>
/// DTO for article version information
/// </summary>
public class ArticleVersionDto
{
    public Guid Id { get; set; }
    public int VersionNumber { get; set; }
    public Guid? CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public string? CreatedByEmail { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>
/// DTO for article version comparison with raw markdown content
/// </summary>
public class ArticleVersionComparisonDto
{
    public string BeforeContent { get; set; } = string.Empty;
    public string AfterContent { get; set; } = string.Empty;
    public int VersionNumber { get; set; }
    public int? PreviousVersionNumber { get; set; }
}


