using Medley.Application.Models.DTOs;
using Medley.Domain.Entities;

namespace Medley.Application.Interfaces;

/// <summary>
/// Service for managing article reviews with auto-approval and successor assignment logic
/// </summary>
public interface IArticleReviewService
{
    /// <summary>
    /// Get all reviews for an article
    /// </summary>
    /// <param name="articleId">The article ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of reviews ordered by date descending</returns>
    Task<List<ArticleReviewDto>> GetReviewsForArticleAsync(Guid articleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new review for an article with auto-approval and successor assignment logic
    /// </summary>
    /// <param name="articleId">The article ID</param>
    /// <param name="request">The review creation request</param>
    /// <param name="currentUser">The user creating the review</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response with review details and approval/assignment status</returns>
    Task<CreateArticleReviewResponse> CreateReviewAsync(
        Guid articleId,
        CreateArticleReviewRequest request,
        User currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a specific review by ID
    /// </summary>
    /// <param name="articleId">The article ID</param>
    /// <param name="reviewId">The review ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The review details or null if not found</returns>
    Task<ArticleReviewDto?> GetReviewAsync(Guid articleId, Guid reviewId, CancellationToken cancellationToken = default);
}
