using Hangfire;
using Medley.Application.Interfaces;
using Medley.Application.Jobs;
using Medley.Application.Models.DTOs;
using Medley.Domain.Entities;
using Medley.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Medley.Application.Services;

/// <summary>
/// Service for managing article reviews with auto-approval and successor assignment logic
/// </summary>
public class ArticleReviewService : IArticleReviewService
{
    private readonly IRepository<ArticleReview> _reviewRepository;
    private readonly IRepository<Article> _articleRepository;
    private readonly IRepository<ArticleVersion> _versionRepository;
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<Organization> _organizationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ArticleReviewService> _logger;
    private readonly IBackgroundJobClient _backgroundJobClient;

    public ArticleReviewService(
        IRepository<ArticleReview> reviewRepository,
        IRepository<Article> articleRepository,
        IRepository<ArticleVersion> versionRepository,
        IRepository<User> userRepository,
        IRepository<Organization> organizationRepository,
        IUnitOfWork unitOfWork,
        ILogger<ArticleReviewService> logger,
        IBackgroundJobClient backgroundJobClient)
    {
        _reviewRepository = reviewRepository;
        _articleRepository = articleRepository;
        _versionRepository = versionRepository;
        _userRepository = userRepository;
        _organizationRepository = organizationRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _backgroundJobClient = backgroundJobClient;
    }

    /// <summary>
    /// Get all reviews for an article
    /// </summary>
    public async Task<List<ArticleReviewDto>> GetReviewsForArticleAsync(Guid articleId, CancellationToken cancellationToken = default)
    {
        var reviews = await _reviewRepository.Query()
            .Where(r => r.ArticleId == articleId)
            .Include(r => r.ReviewedBy)
            .OrderByDescending(r => r.ReviewedAt)
            .ToListAsync(cancellationToken);

        return reviews.Select(r => MapToDto(r)).ToList();
    }

    /// <summary>
    /// Get a specific review by ID
    /// </summary>
    public async Task<ArticleReviewDto?> GetReviewAsync(Guid articleId, Guid reviewId, CancellationToken cancellationToken = default)
    {
        var review = await _reviewRepository.Query()
            .Where(r => r.Id == reviewId && r.ArticleId == articleId)
            .Include(r => r.ReviewedBy)
            .FirstOrDefaultAsync(cancellationToken);

        return review != null ? MapToDto(review) : null;
    }

    /// <summary>
    /// Create a new review for an article with auto-approval and successor assignment logic
    /// </summary>
    public async Task<CreateArticleReviewResponse> CreateReviewAsync(
        Guid articleId,
        CreateArticleReviewRequest request,
        User currentUser,
        CancellationToken cancellationToken = default)
    {
        // Validate article exists
        var article = await _articleRepository.Query()
            .Include(a => a.AssignedUser)
            .FirstOrDefaultAsync(a => a.Id == articleId, cancellationToken);

        if (article == null)
        {
            throw new InvalidOperationException($"Article with ID {articleId} not found");
        }

        // Create the review
        var review = new ArticleReview
        {
            Id = Guid.NewGuid(),
            ArticleId = articleId,
            ReviewedById = currentUser.Id,
            ReviewedAt = DateTimeOffset.UtcNow,
            Action = request.Action,
            Comments = request.Comments
        };

        await _reviewRepository.AddAsync(review);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Reload review with user data for DTO mapping
        var savedReview = await _reviewRepository.Query()
            .Include(r => r.ReviewedBy)
            .FirstAsync(r => r.Id == review.Id, cancellationToken);

        var response = new CreateArticleReviewResponse
        {
            Review = MapToDto(savedReview),
            ArticleApproved = false,
            ArticleReassigned = false,
            ReassignedToUserId = null,
            Message = GetReviewActionMessage(request.Action)
        };

        // If manual reassignment is specified, always reassign (skip auto logic)
        if (request.ReassignToUserId.HasValue)
        {
            await AssignArticleToUserAsync(article, request.ReassignToUserId.Value, response, cancellationToken);
        }
        // If action is Approve (and no manual reassignment), check auto-approval logic (forward to successor)
        else if (request.Action == ArticleReviewAction.Approve)
        {
            await ProcessApprovalLogicAsync(article, currentUser, response, cancellationToken);
        }
        // If action is RequestChanges (and no manual reassignment), go back to previous author
        else if (request.Action == ArticleReviewAction.RequestChanges)
        {
            await AssignToPreviousAuthorAsync(article, currentUser.Id, response, cancellationToken);
            
            // Set status to Review if currently Draft or Approved
            if (article.Status == ArticleStatus.Draft || article.Status == ArticleStatus.Approved)
            {
                article.Status = ArticleStatus.Review;
                _logger.LogInformation("Article {ArticleId} status changed to Review after RequestChanges", article.Id);
                
                // Update response to indicate status change
                response.StatusChanged = true;
                response.NewStatus = ArticleStatus.Review;
            }
        }
        // If action is Comment (and no manual reassignment), no reassignment occurs

        return response;
    }

    /// <summary>
    /// Process auto-approval logic after an approval review
    /// </summary>
    private async Task ProcessApprovalLogicAsync(
        Article article,
        User reviewer,
        CreateArticleReviewResponse response,
        CancellationToken cancellationToken)
    {
        // Load organization settings
        var organization = await _organizationRepository.Query().FirstOrDefaultAsync(cancellationToken);
        if (organization == null)
        {
            _logger.LogWarning("No organization found for auto-approval check");
            return;
        }

        // Count existing approvals (distinct reviewers)
        var approvalCount = await _reviewRepository.Query()
            .Where(r => r.ArticleId == article.Id && r.Action == ArticleReviewAction.Approve)
            .Select(r => r.ReviewedById)
            .Distinct()
            .CountAsync(cancellationToken);

        // Check if required reviewer has approved (if configured)
        var hasRequiredReviewer = !organization.RequiredReviewerId.HasValue ||
            await _reviewRepository.Query()
                .AnyAsync(r => r.ArticleId == article.Id
                    && r.Action == ArticleReviewAction.Approve
                    && r.ReviewedById == organization.RequiredReviewerId.Value,
                    cancellationToken);

        _logger.LogInformation(
            "Article {ArticleId}: Approval count={Count}, Required={Required}, HasRequiredReviewer={HasRequired}, AutoApprove={Auto}",
            article.Id, approvalCount, organization.RequiredReviewCount, hasRequiredReviewer, organization.AutoApprove);

        // Check if auto-approval criteria are met
        if (organization.AutoApprove &&
            approvalCount >= organization.RequiredReviewCount &&
            hasRequiredReviewer)
        {
            // Auto-approve the article
            article.Status = ArticleStatus.Approved;
            article.PublishedAt = DateTimeOffset.UtcNow;
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            response.ArticleApproved = true;
            response.Message = $"Review submitted. Article auto-approved with {approvalCount} approval(s).";

            _logger.LogInformation("Article {ArticleId} auto-approved", article.Id);

            // Trigger Zendesk sync if enabled
            if (organization.EnableArticleZendeskSync)
            {
                _backgroundJobClient.Enqueue<ZendeskArticleSyncJob>(
                    job => job.SyncArticleAsync(article.Id, default!, default));
                
                _logger.LogInformation("Enqueued Zendesk sync job for article {ArticleId}", article.Id);
            }
        }
        else
        {
            // Check if reviewer has a successor configured
            var reviewerWithSuccessor = await _userRepository.Query()
                .Include(u => u.ReviewSuccessor)
                .FirstOrDefaultAsync(u => u.Id == reviewer.Id, cancellationToken);

            if (reviewerWithSuccessor?.ReviewSuccessorId.HasValue == true)
            {
                // Assign article to successor
                article.AssignedUserId = reviewerWithSuccessor.ReviewSuccessorId.Value;
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                response.ArticleReassigned = true;
                response.ReassignedToUserId = reviewerWithSuccessor.ReviewSuccessorId.Value;
                response.Message = $"Review submitted. Article assigned to {reviewerWithSuccessor.ReviewSuccessor?.FullName ?? "successor"} for further review.";

                _logger.LogInformation(
                    "Article {ArticleId} reassigned to successor {SuccessorId}",
                    article.Id,
                    reviewerWithSuccessor.ReviewSuccessorId.Value);
            }
            else
            {
                var remaining = Math.Max(0, organization.RequiredReviewCount - approvalCount);
                if (remaining > 0)
                {
                    response.Message = $"Review submitted. {remaining} more approval(s) needed.";
                }
                else if (!hasRequiredReviewer)
                {
                    response.Message = "Review submitted. Waiting for required reviewer approval.";
                }
            }
        }
    }

    /// <summary>
    /// Map ArticleReview entity to DTO
    /// </summary>
    private static ArticleReviewDto MapToDto(ArticleReview review)
    {
        return new ArticleReviewDto
        {
            Id = review.Id,
            ArticleId = review.ArticleId,
            ReviewedBy = new UserSummaryDto
            {
                Id = review.ReviewedBy.Id,
                FullName = review.ReviewedBy.FullName,
                Initials = review.ReviewedBy.Initials,
                Color = review.ReviewedBy.Color
            },
            ReviewedAt = review.ReviewedAt,
            Action = review.Action,
            Comments = review.Comments
        };
    }

    /// <summary>
    /// Assign article to a specific user
    /// </summary>
    private async Task AssignArticleToUserAsync(
        Article article,
        Guid userId,
        CreateArticleReviewResponse response,
        CancellationToken cancellationToken)
    {
        var targetUser = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (targetUser != null)
        {
            article.AssignedUserId = userId;
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            response.ArticleReassigned = true;
            response.ReassignedToUserId = userId;
            response.Message = $"Review submitted. Article assigned to {targetUser.FullName}.";

            _logger.LogInformation(
                "Article {ArticleId} manually reassigned to user {UserId}",
                article.Id,
                userId);
        }
    }

    /// <summary>
    /// Assign article back to the most recent version author (excluding current user)
    /// </summary>
    private async Task AssignToPreviousAuthorAsync(
        Article article,
        Guid currentUserId,
        CreateArticleReviewResponse response,
        CancellationToken cancellationToken)
    {
        // Find the most recent version by a different user
        var previousVersion = await _versionRepository.Query()
            .Where(v => v.ArticleId == article.Id && v.CreatedById.HasValue && v.CreatedById.Value != currentUserId)
            .OrderByDescending(v => v.CreatedAt)
            .Include(v => v.CreatedBy)
            .FirstOrDefaultAsync(cancellationToken);

        if (previousVersion?.CreatedBy != null)
        {
            article.AssignedUserId = previousVersion.CreatedById!.Value;
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            response.ArticleReassigned = true;
            response.ReassignedToUserId = previousVersion.CreatedById.Value;
            response.Message = $"Review submitted. Article assigned back to {previousVersion.CreatedBy.FullName}.";

            _logger.LogInformation(
                "Article {ArticleId} assigned back to previous author {UserId}",
                article.Id,
                previousVersion.CreatedById.Value);
        }
    }

    /// <summary>
    /// Get appropriate message for review action
    /// </summary>
    private static string GetReviewActionMessage(ArticleReviewAction action)
    {
        return action switch
        {
            ArticleReviewAction.Approve => "Approval submitted successfully.",
            ArticleReviewAction.RequestChanges => "Changes requested successfully.",
            ArticleReviewAction.Comment => "Comment submitted successfully.",
            _ => "Review submitted successfully."
        };
    }
}
