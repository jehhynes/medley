using Medley.Application.Hubs;
using Medley.Application.Hubs.Clients;
using Medley.Application.Interfaces;
using Medley.Application.Models.DTOs;
using Medley.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Medley.Web.Controllers.Api;

[Route("api/articles/{articleId}/reviews")]
[ApiController]
[ApiExplorerSettings(GroupName = "api")]
[Authorize]
public class ArticleReviewsApiController : ControllerBase
{
    private readonly IArticleReviewService _reviewService;
    private readonly IRepository<Article> _articleRepository;
    private readonly IHubContext<ArticleHub, IArticleClient> _hubContext;
    private readonly IMedleyContext _medleyContext;
    private readonly ILogger<ArticleReviewsApiController> _logger;

    public ArticleReviewsApiController(
        IArticleReviewService reviewService,
        IRepository<Article> articleRepository,
        IHubContext<ArticleHub, IArticleClient> hubContext,
        IMedleyContext medleyContext,
        ILogger<ArticleReviewsApiController> logger)
    {
        _reviewService = reviewService;
        _articleRepository = articleRepository;
        _hubContext = hubContext;
        _medleyContext = medleyContext;
        _logger = logger;
    }

    /// <summary>
    /// Get all reviews for an article
    /// </summary>
    /// <param name="articleId">The article ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of reviews ordered by date descending</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<ArticleReviewDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<ArticleReviewDto>>> GetReviews(
        [FromRoute] Guid articleId,
        CancellationToken cancellationToken)
    {
        try
        {
            // Verify article exists
            var article = await _articleRepository.GetByIdAsync(articleId, cancellationToken);
            if (article == null)
            {
                return NotFound(new { message = $"Article with ID {articleId} not found" });
            }

            var reviews = await _reviewService.GetReviewsForArticleAsync(articleId, cancellationToken);
            return Ok(reviews);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get reviews for article {ArticleId}", articleId);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { message = "An error occurred while retrieving reviews" });
        }
    }

    /// <summary>
    /// Get a specific review by ID
    /// </summary>
    /// <param name="articleId">The article ID</param>
    /// <param name="reviewId">The review ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The review details</returns>
    [HttpGet("{reviewId}")]
    [ProducesResponseType(typeof(ArticleReviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ArticleReviewDto>> GetReview(
        [FromRoute] Guid articleId,
        [FromRoute] Guid reviewId,
        CancellationToken cancellationToken)
    {
        try
        {
            var review = await _reviewService.GetReviewAsync(articleId, reviewId, cancellationToken);
            if (review == null)
            {
                return NotFound(new { message = $"Review with ID {reviewId} not found for article {articleId}" });
            }

            return Ok(review);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get review {ReviewId} for article {ArticleId}", reviewId, articleId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving the review" });
        }
    }

    /// <summary>
    /// Create a new review for an article
    /// </summary>
    /// <param name="articleId">The article ID</param>
    /// <param name="request">The review creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response with review details and approval/assignment status</returns>
    [HttpPost]
    [ProducesResponseType(typeof(CreateArticleReviewResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CreateArticleReviewResponse>> CreateReview(
        [FromRoute] Guid articleId,
        [FromBody] CreateArticleReviewRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get current user
            var currentUser = await _medleyContext.GetCurrentUserAsync();
            if (currentUser == null)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            // Verify article exists
            var article = await _articleRepository.GetByIdAsync(articleId, cancellationToken);
            if (article == null)
            {
                return NotFound(new { message = $"Article with ID {articleId} not found" });
            }

            // Create the review
            var response = await _reviewService.CreateReviewAsync(
                articleId,
                request,
                currentUser,
                cancellationToken);

            // Send SignalR notifications
            await SendReviewNotificationsAsync(articleId, response);

            return CreatedAtAction(
                nameof(GetReview),
                new { articleId, reviewId = response.Review.Id },
                response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation while creating review for article {ArticleId}", articleId);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create review for article {ArticleId}", articleId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while creating the review" });
        }
    }

    /// <summary>
    /// Send SignalR notifications for review events
    /// </summary>
    private async Task SendReviewNotificationsAsync(Guid articleId, CreateArticleReviewResponse response)
    {
        try
        {
            // Notify that a review was added
            await _hubContext.Clients.All.ArticleReviewAdded(articleId, response.Review);

            // If article was approved, send approval notification
            if (response.ArticleApproved)
            {
                await _hubContext.Clients.All.ArticleApproved(articleId);
                _logger.LogInformation("Sent article approved notification for {ArticleId}", articleId);
            }

            // If article was reassigned, send assignment changed notification
            if (response.ArticleReassigned && response.ReassignedToUserId.HasValue)
            {
                // Get the assigned user details for the payload
                var assignedUser = await _articleRepository.Query()
                    .Where(a => a.Id == articleId)
                    .Select(a => a.AssignedUser)
                    .FirstOrDefaultAsync();

                await _hubContext.Clients.All.ArticleAssignmentChanged(new ArticleAssignmentChangedPayload
                {
                    ArticleId = articleId,
                    UserId = assignedUser?.Id,
                    UserName = assignedUser?.FullName,
                    UserInitials = assignedUser?.Initials,
                    UserColor = assignedUser?.Color,
                    Timestamp = DateTimeOffset.UtcNow
                });

                _logger.LogInformation(
                    "Sent article assignment changed notification for {ArticleId} to user {UserId}",
                    articleId,
                    response.ReassignedToUserId.Value);
            }
        }
        catch (Exception ex)
        {
            // Log but don't fail the request if SignalR notification fails
            _logger.LogWarning(ex, "Failed to send SignalR notifications for review on article {ArticleId}", articleId);
        }
    }
}
