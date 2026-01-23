namespace Medley.Application.Interfaces;

/// <summary>
/// Service for interacting with Cursor CLI via SSH
/// </summary>
public interface ICursorService
{
    /// <summary>
    /// Reviews and improves article content using Cursor Agent
    /// </summary>
    /// <param name="articleContent">The original article content</param>
    /// <param name="instructions">Instructions for how to improve the article</param>
    /// <param name="articleTypeId">Optional article type ID to lookup and sync the associated prompt</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing improved content and change summary</returns>
    Task<CursorReviewResult> ReviewArticleAsync(
        string articleContent,
        string instructions,
        Guid? articleTypeId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asks Cursor Agent a question without file operations
    /// </summary>
    /// <param name="question">The question to ask the Cursor Agent</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the AI's response</returns>
    Task<CursorQuestionResult> AskQuestionAsync(
        string question,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a Cursor review operation
/// </summary>
public class CursorReviewResult
{
    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The improved article content
    /// </summary>
    public string ImprovedContent { get; set; } = string.Empty;

    /// <summary>
    /// Summary of changes made by Cursor
    /// </summary>
    public string ChangesSummary { get; set; } = string.Empty;

    /// <summary>
    /// Error message if operation failed
    /// </summary>
    public string? Error { get; set; }
}

/// <summary>
/// Result of a question operation
/// </summary>
public class CursorQuestionResult
{
    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The AI's response to the question
    /// </summary>
    public string Response { get; set; } = string.Empty;

    /// <summary>
    /// Error message if operation failed
    /// </summary>
    public string? Error { get; set; }
}
