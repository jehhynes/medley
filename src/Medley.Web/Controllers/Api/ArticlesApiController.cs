using System;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Medley.Application.Hubs;
using Medley.Application.Hubs.Clients;
using Medley.Application.Interfaces;
using Medley.Application.Models.DTOs;
using Medley.Domain.Entities;
using Medley.Domain.Enums;
using Medley.Web.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Medley.Web.Controllers.Api;

[Route("api/articles")]
[ApiController]
[ApiExplorerSettings(GroupName = "api")]
[Authorize]
public class ArticlesApiController : ControllerBase
{
    private readonly IRepository<Article> _articleRepository;
    private readonly IRepository<ArticleType> _articleTypeRepository;
    private readonly IRepository<User> _userRepository;
    private readonly IHubContext<ArticleHub, IArticleClient> _hubContext;
    private readonly IArticleVersionService _versionService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMedleyContext _medleyContext;
    private readonly ILogger<ArticlesApiController> _logger;

    public ArticlesApiController(
        IRepository<Article> articleRepository,
        IRepository<ArticleType> articleTypeRepository,
        IRepository<User> userRepository,
        IHubContext<ArticleHub, IArticleClient> hubContext,
        IArticleVersionService versionService,
        IUnitOfWork unitOfWork,
        IMedleyContext medleyContext,
        ILogger<ArticlesApiController> logger)
    {
        _articleRepository = articleRepository;
        _articleTypeRepository = articleTypeRepository;
        _userRepository = userRepository;
        _hubContext = hubContext;
        _versionService = versionService;
        _unitOfWork = unitOfWork;
        _medleyContext = medleyContext;
        _logger = logger;
    }

    /// <summary>
    /// Get all article types
    /// </summary>
    /// <returns>List of article types</returns>
    [HttpGet("types")]
    [ProducesResponseType(typeof(List<ArticleTypeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ArticleTypeDto>>> GetArticleTypes()
    {
        var articleTypes = await _articleTypeRepository.Query()
            .OrderBy(at => at.Name)
            .Select(at => new ArticleTypeDto
            {
                Id = at.Id,
                Name = at.Name,
                Icon = at.Icon
            })
            .ToListAsync();

        return Ok(articleTypes);
    }

    /// <summary>
    /// Get all articles as a tree structure, optionally filtered
    /// </summary>
    /// <param name="query">Text search query</param>
    /// <param name="statuses">Filter by article statuses</param>
    /// <param name="articleTypeIds">Filter by article type IDs</param>
    /// <returns>Tree structure of articles</returns>
    [HttpGet("tree")]
    [ProducesResponseType(typeof(List<ArticleSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ArticleSummaryDto>>> GetTree(
        [FromQuery] string? query = null,
        [FromQuery] int[]? statuses = null,
        [FromQuery] Guid[]? articleTypeIds = null)
    {
        // Check if any filters are applied
        var hasFilters = !string.IsNullOrWhiteSpace(query) || 
                        (statuses != null && statuses.Length > 0) || 
                        (articleTypeIds != null && articleTypeIds.Length > 0);

        List<Article> articles;

        if (hasFilters)
        {
            // Get all articles first (we'll need them for parent resolution)
            var allArticles = await _articleRepository.Query()
                .Include(a => a.ArticleType)
                .Include(a => a.CurrentConversation)
                .Include(a => a.AssignedUser)
                .Include(a => a.CurrentVersion)
                .ToListAsync();

            // Apply filters to get matching articles
            var matchingArticles = allArticles.AsQueryable();

            if (!string.IsNullOrWhiteSpace(query))
            {
                matchingArticles = matchingArticles.Where(a => a.Title.Contains(query, StringComparison.OrdinalIgnoreCase));
            }

            if (statuses != null && statuses.Length > 0)
            {
                var statusEnums = statuses.Select(s => (Domain.Enums.ArticleStatus)s).ToList();
                matchingArticles = matchingArticles.Where(a => statusEnums.Contains(a.Status));
            }

            if (articleTypeIds != null && articleTypeIds.Length > 0)
            {
                matchingArticles = matchingArticles.Where(a => a.ArticleTypeId.HasValue && articleTypeIds.Contains(a.ArticleTypeId.Value));
            }

            var matchingArticlesList = matchingArticles.ToList();

            // Include all ancestor articles for each matching article
            var articlesToInclude = new HashSet<Guid>();
            foreach (var article in matchingArticlesList)
            {
                articlesToInclude.Add(article.Id);

                // Walk up the parent chain
                var currentParentId = article.ParentArticleId;
                while (currentParentId.HasValue)
                {
                    articlesToInclude.Add(currentParentId.Value);
                    var parent = allArticles.FirstOrDefault(a => a.Id == currentParentId.Value);
                    currentParentId = parent?.ParentArticleId;
                }
            }

            // Filter to only include matching articles and their ancestors
            articles = allArticles.Where(a => articlesToInclude.Contains(a.Id)).ToList();
        }
        else
        {
            // No filters, return all articles
            articles = await _articleRepository.Query()
                .Include(a => a.ArticleType)
                .Include(a => a.CurrentConversation)
                .Include(a => a.AssignedUser)
                .Include(a => a.CurrentVersion)
                .ToListAsync();
        }

        var tree = BuildTree(articles, null);
        return Ok(tree);
    }

    /// <summary>
    /// Get a specific article by ID
    /// </summary>
    /// <param name="id">Article ID</param>
    /// <returns>Article details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ArticleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ArticleDto>> Get(Guid id)
    {
        var article = await _articleRepository.Query()
            .Include(a => a.ParentArticle)
            .Include(a => a.CurrentConversation)
            .Include(a => a.CurrentPlan)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (article == null)
        {
            return NotFound();
        }

        return Ok(new ArticleDto
        {
            Id = article.Id,
            Title = article.Title,
            Content = StripArticleHeader(article.Content),
            Status = article.Status,
            CreatedAt = article.CreatedAt,
            ParentArticleId = article.ParentArticleId,
            CurrentConversation = article.CurrentConversation != null ? new ConversationSummaryDto
            { 
                Id = article.CurrentConversation.Id,
                IsRunning = article.CurrentConversation.IsRunning,
                State = article.CurrentConversation.State
            } : null,
            CurrentPlan = article.CurrentPlan != null ? new PlanRef
            {
                Id = article.CurrentPlan.Id,
                Status = article.CurrentPlan.Status
            } : null
        });
    }

    /// <summary>
    /// Create a new article
    /// </summary>
    /// <param name="request">Article creation request</param>
    /// <returns>Created article</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ArticleSummaryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ArticleSummaryDto>> Create([FromBody] ArticleCreateRequest request)
    {
        var currentUser = await _medleyContext.GetCurrentUserAsync();
        
        ArticleType? articleType = null;
        if (request.ArticleTypeId.HasValue)
        {
            articleType = await _articleTypeRepository.GetByIdAsync(request.ArticleTypeId.Value);
        }

        var article = new Article
        {
            Title = request.Title,
            Status = Domain.Enums.ArticleStatus.Draft,
            ParentArticleId = request.ParentArticleId,
            ArticleType = articleType,
            Content = $"# {request.Title}\n\n",
            CreatedBy = currentUser
        };

        await _articleRepository.AddAsync(article);

        // Register post-commit action to send SignalR notification
        HttpContext.RegisterPostCommitAction(async () =>
        {
            await _hubContext.Clients.All.ArticleCreated(new ArticleCreatedPayload
            {
                ArticleId = article.Id,
                Title = article.Title,
                ParentArticleId = article.ParentArticleId,
                ArticleTypeId = article.ArticleTypeId,
                Timestamp = DateTimeOffset.UtcNow
            });
        });

        return CreatedAtAction(nameof(Get), new { id = article.Id }, new ArticleSummaryDto
        {
            Id = article.Id,
            Title = article.Title,
            Status = article.Status,
            ParentArticleId = article.ParentArticleId,
            ArticleTypeId = article.ArticleTypeId,
            CreatedAt = article.CreatedAt
        });
    }

    /// <summary>
    /// Update article metadata (title, type, status)
    /// </summary>
    /// <param name="id">Article ID</param>
    /// <param name="request">Metadata update request</param>
    /// <returns>Success response</returns>
    [HttpPut("{id}/metadata")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> UpdateMetadata(Guid id, [FromBody] ArticleUpdateMetadataRequest request)
    {
        var article = await _articleRepository.GetByIdAsync(id);
        if (article == null)
        {
            return NotFound();
        }

        // Track if title changed
        var titleChanged = article.Title != request.Title;
        var oldTitle = article.Title;
        
        // Update title
        article.Title = request.Title;
        
        // Conditional sync: Only sync H1 if title and H1 were previously in sync
        if (titleChanged && TitleMatchesH1(oldTitle, article.Content))
        {
            article.Content = UpdateFirstH1InMarkdown(article.Content, request.Title);
        }
        
        if (request.Status.HasValue)
        {
            article.Status = request.Status.Value;
        }
        if (request.ArticleTypeId.HasValue)
        {
            article.ArticleTypeId = request.ArticleTypeId.Value;
        }

        

        // Register post-commit action to send SignalR notification
        HttpContext.RegisterPostCommitAction(async () =>
        {
            await _hubContext.Clients.All.ArticleUpdated(new ArticleUpdatedPayload
            {
                ArticleId = article.Id,
                Title = article.Title,
                ArticleTypeId = article.ArticleTypeId,
                Timestamp = DateTimeOffset.UtcNow
            });
        });

        return Ok();
    }

    /// <summary>
    /// Update article content
    /// </summary>
    /// <param name="id">Article ID</param>
    /// <param name="request">Content update request</param>
    /// <returns>Version capture response</returns>
    [HttpPut("{id}/content")]
    [ProducesResponseType(typeof(VersionCaptureResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<VersionCaptureResponse>> UpdateContent(Guid id, [FromBody] ArticleUpdateContentRequest request)
    {
        var user = await _medleyContext.GetCurrentUserAsync();
        if (user == null)
        {
            return Unauthorized();
        }

        var article = await _articleRepository.GetByIdAsync(id);
        if (article == null)
        {
            return NotFound();
        }

        // Capture old content for versioning
        var oldContent = article.Content;
        var oldTitle = article.Title;
        var newH1 = ExtractFirstH1(request.Content);
        
        // Conditional sync: Only update title if it currently matches the old H1
        if (newH1 != null && TitleMatchesH1(article.Title, article.Content))
        {
            article.Title = newH1;
        }
        
        // Preserve existing HTML comment header if present
        var existingHeader = ExtractArticleHeader(article.Content);
        if (!string.IsNullOrEmpty(existingHeader))
        {
            article.Content = existingHeader + request.Content;
        }
        else
        {
            article.Content = request.Content;
        }

        // Auto-assign to current user
        var assignmentChanged = await AssignArticleToUserAsync(article, user.Id);

        

        // Capture version after saving article
        var capturedVersion = await _versionService.CaptureUserVersionAsync(id, article.Content, oldContent, user.Id);

        if (capturedVersion.IsNewVersion)
        {
            // Register post-commit action to send SignalR notification
            HttpContext.RegisterPostCommitAction(async () =>
            {
                await _hubContext.Clients.All.VersionCreated(new VersionCreatedPayload
                {
                    ArticleId = id,
                    VersionId = capturedVersion.Id,
                    VersionNumber = capturedVersion.VersionNumber,
                    VersionType = VersionType.User,
                    CreatedAt = capturedVersion.CreatedAt
                });
            });
        }

        // Register post-commit action to send SignalR notification (only if title changed)
        if (article.Title != oldTitle)
        {
            HttpContext.RegisterPostCommitAction(async () =>
            {
                await _hubContext.Clients.All.ArticleUpdated(new ArticleUpdatedPayload
                {
                    ArticleId = article.Id,
                    Title = article.Title,
                    ArticleTypeId = article.ArticleTypeId,
                    Timestamp = DateTimeOffset.UtcNow
                });
            });
        }

        // Send assignment notification if changed
        if (assignmentChanged)
        {
            await SendAssignmentNotificationAsync(article);
        }

        return Ok(new VersionCaptureResponse
        {
            VersionId = capturedVersion.Id,
            VersionNumber = capturedVersion.VersionNumber,
            IsNewVersion = capturedVersion.IsNewVersion
        });
    }

    /// <summary>
    /// Delete an article
    /// </summary>
    /// <param name="id">Article ID</param>
    /// <returns>No content</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ArticleDeleteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ArticleDeleteResponse>> Delete(Guid id)
    {
        var article = await _articleRepository.GetByIdAsync(id);
        if (article == null)
        {
            return NotFound();
        }

        // Note: This is a simplified delete. In production, you'd want to handle child articles
        // and potentially implement soft delete
        return Ok(new ArticleDeleteResponse { Message = "Delete functionality to be implemented" });
    }

    /// <summary>
    /// Move an article to a different parent
    /// </summary>
    /// <param name="id">Article ID to move</param>
    /// <param name="request">Move request with new parent ID</param>
    /// <returns>Success response</returns>
    [HttpPut("{id}/move")]
    [ProducesResponseType(typeof(ArticleMoveResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ArticleMoveResponse>> Move(Guid id, [FromBody] ArticleMoveRequest request)
    {
        // Get the article to move
        var article = await _articleRepository.Query()
            .FirstOrDefaultAsync(a => a.Id == id);
        
        if (article == null)
        {
            return NotFound(new { message = "Article not found" });
        }

        // Validate that a parent is being set (cannot move to root)
        if (!request.NewParentArticleId.HasValue)
        {
            return BadRequest(new { message = "Articles must have a parent. Cannot move to root level." });
        }

        // Check if moving to the same parent (no-op)
        if (article.ParentArticleId == request.NewParentArticleId.Value)
        {
            return Ok(new ArticleMoveResponse 
            { 
                Message = "Article is already under this parent",
                ArticleId = article.Id,
                OldParentId = article.ParentArticleId,
                NewParentId = request.NewParentArticleId.Value
            });
        }

        // Get the new parent article
        var newParent = await _articleRepository.Query()
            .Include(a => a.ArticleType)
            .FirstOrDefaultAsync(a => a.Id == request.NewParentArticleId.Value);
        
        if (newParent == null)
        {
            return NotFound(new { message = "Target parent article not found" });
        }

        // Validate that the new parent is of type "Index"
        if (newParent.ArticleType == null || !newParent.ArticleType.Name.Equals("Index", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Articles can only be moved under articles of type 'Index'" });
        }

        // Check for circular reference (moving under itself or a descendant)
        if (await IsCircularReference(id, request.NewParentArticleId.Value))
        {
            return BadRequest(new { message = "Cannot move an article under itself or one of its descendants" });
        }

        // Store old parent ID for SignalR notification
        var oldParentId = article.ParentArticleId;

        // Update the parent
        article.ParentArticleId = request.NewParentArticleId.Value;
        

        // Register post-commit action to send SignalR notification
        HttpContext.RegisterPostCommitAction(async () =>
        {
            await _hubContext.Clients.All.ArticleMoved(new ArticleMovedPayload
            {
                ArticleId = article.Id,
                OldParentId = oldParentId,
                NewParentId = request.NewParentArticleId.Value,
                Timestamp = DateTimeOffset.UtcNow
            });
        });

        return Ok(new ArticleMoveResponse
        { 
            Message = "Article moved successfully",
            ArticleId = article.Id,
            OldParentId = oldParentId,
            NewParentId = request.NewParentArticleId.Value
        });
    }

    /// <summary>
    /// Check if moving an article would create a circular reference
    /// </summary>
    private async Task<bool> IsCircularReference(Guid articleId, Guid targetParentId)
    {
        // Cannot move under itself
        if (articleId == targetParentId)
        {
            return true;
        }

        // Check if targetParent is a descendant of article
        var currentParentId = targetParentId;
        var visitedIds = new HashSet<Guid> { targetParentId };

        while (currentParentId != Guid.Empty)
        {
            var parent = await _articleRepository.Query()
                .Where(a => a.Id == currentParentId)
                .Select(a => new { a.ParentArticleId })
                .FirstOrDefaultAsync();

            if (parent == null || !parent.ParentArticleId.HasValue)
            {
                // Reached root, no circular reference
                return false;
            }

            currentParentId = parent.ParentArticleId.Value;

            // If we encounter the article being moved, it's a circular reference
            if (currentParentId == articleId)
            {
                return true;
            }

            // Prevent infinite loops in case of data corruption
            if (visitedIds.Contains(currentParentId))
            {
                return true; // Circular reference detected in the chain
            }
            visitedIds.Add(currentParentId);
        }

        return false;
    }

    /// <summary>
    /// Helper method to build tree structure
    /// </summary>
    private List<ArticleSummaryDto> BuildTree(List<Article> allArticles, Guid? parentId)
    {
        return allArticles
            .Where(a => a.ParentArticleId == parentId)
            .OrderBy(a => a.ArticleType?.Name?.Equals("Index", StringComparison.OrdinalIgnoreCase) == true ? 0 : 1)
            .ThenBy(a => a.Title)
            .Select(a => new ArticleSummaryDto
            {
                Id = a.Id,
                ParentArticleId = a.ParentArticleId,
                Title = a.Title,
                Status = a.Status,
                CreatedAt = a.CreatedAt,
                ModifiedAt = a.CurrentVersion != null ? a.CurrentVersion.ModifiedAt : null,
                ArticleTypeId = a.ArticleTypeId,
                CurrentConversation = a.CurrentConversation != null ? new ConversationSummaryDto
                { 
                    Id = a.CurrentConversation.Id,
                    IsRunning = a.CurrentConversation.IsRunning,
                    State = a.CurrentConversation.State
                } : null,
                AssignedUser = a.AssignedUser != null ? new UserSummaryDto
                {
                    Id = a.AssignedUser.Id,
                    FullName = a.AssignedUser.FullName ?? a.AssignedUser.Email ?? "Unknown",
                    Initials = a.AssignedUser.Initials,
                    Color = a.AssignedUser.Color
                } : null,
                Children = BuildTree(allArticles, a.Id)
            })
            .ToList();
    }

    /// <summary>
    /// Helper method to extract HTML comment header from article content
    /// </summary>
    private static string? ExtractArticleHeader(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        // Extract HTML comment header at the start of the content
        // Pattern: ^\s*<!--[\s\S]*?-->\s*
        var pattern = @"^\s*(<!--[\s\S]*?-->\s*)";
        var match = Regex.Match(content, pattern, RegexOptions.None, TimeSpan.FromSeconds(1));
        
        return match.Success ? match.Groups[1].Value : null;
    }

    /// <summary>
    /// Helper method to strip HTML comment header from article content
    /// </summary>
    private static string? StripArticleHeader(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return content;
        }

        // Remove HTML comment header at the start of the content
        // Pattern: ^\s*<!--[\s\S]*?-->\s*
        var pattern = @"^\s*<!--[\s\S]*?-->\s*";
        return Regex.Replace(content, pattern, string.Empty, RegexOptions.None, TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// Extract the first H1 heading from markdown content
    /// </summary>
    private static string? ExtractFirstH1(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        // Pattern to match first H1: # Title
        var h1Pattern = @"^#\s+(.+)$";
        var match = Regex.Match(content, h1Pattern, RegexOptions.Multiline);
        
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    /// <summary>
    /// Update the first H1 in markdown content
    /// </summary>
    private static string UpdateFirstH1InMarkdown(string? content, string newTitle)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return $"# {newTitle}\n\n";
        }

        var h1Pattern = @"^#\s+.*?$";
        var match = Regex.Match(content, h1Pattern, RegexOptions.Multiline);
        
        if (match.Success)
        {
            return Regex.Replace(content, h1Pattern, $"# {newTitle}", 
                RegexOptions.Multiline, TimeSpan.FromSeconds(1));
        }
        else
        {
            // No H1 found, prepend one
            return $"# {newTitle}\n\n{content}";
        }
    }

    /// <summary>
    /// Check if title and first H1 in content match
    /// </summary>
    private static bool TitleMatchesH1(string title, string? content)
    {
        var h1 = ExtractFirstH1(content);
        return h1 != null && h1.Equals(title, StringComparison.Ordinal);
    }

    /// <summary>
    /// Get version history for an article (all versions - User and AI)
    /// </summary>
    /// <param name="id">Article ID</param>
    /// <returns>List of article versions</returns>
    [HttpGet("{id}/versions")]
    [ProducesResponseType(typeof(List<ArticleVersionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<ArticleVersionDto>>> GetVersionHistory(Guid id)
    {
        var article = await _articleRepository.GetByIdAsync(id);
        if (article == null)
        {
            return NotFound();
        }

        // Return all versions (User and AI) - let the client filter as needed
        var versions = await _versionService.GetVersionsAsync(id, versionType: null);
        
        var versionDtos = versions.Select(version => new ArticleVersionDto
        {
            Id = version.Id,
            ArticleId = id,
            VersionNumber = version.VersionNumber,
            CreatedBy = version.CreatedBy.HasValue ? new UserRef
            {
                Id = version.CreatedBy.Value,
                FullName = version.CreatedByName ?? version.CreatedByEmail ?? "Unknown"
            } : null,
            CreatedAt = version.CreatedAt,
            VersionType = Enum.Parse<Domain.Enums.VersionType>(version.VersionType),
            ChangeMessage = version.ChangeMessage,
            ReviewAction = version.Status == Domain.Enums.VersionStatus.AcceptedAiVersion ? Domain.Enums.ReviewAction.Accepted :
                          version.Status == Domain.Enums.VersionStatus.RejectedAiVersion ? Domain.Enums.ReviewAction.Rejected :
                          Domain.Enums.ReviewAction.None,
            ReviewedAt = version.ReviewedAt,
            ReviewedBy = version.ReviewedById.HasValue ? new UserRef
            {
                Id = version.ReviewedById.Value,
                FullName = version.ReviewedByName ?? "Unknown"
            } : null,
            Status = version.Status
        }).ToList();
        
        return Ok(versionDtos);
    }

    /// <summary>
    /// Get a specific version by ID (User or AI)
    /// </summary>
    /// <param name="articleId">Article ID</param>
    /// <param name="versionId">Version ID</param>
    /// <returns>Version details</returns>
    [HttpGet("{articleId}/versions/{versionId}")]
    [ProducesResponseType(typeof(ArticleVersionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ArticleVersionDto>> GetVersion(Guid articleId, Guid versionId)
    {
        var article = await _articleRepository.GetByIdAsync(articleId);
        if (article == null)
        {
            return NotFound(new { message = "Article not found" });
        }

        // Load version details - pass null to allow both User and AI versions
        var versions = await _versionService.GetVersionsAsync(articleId, versionType: null);
        var version = versions.FirstOrDefault(v => v.Id == versionId);
        
        if (version == null)
        {
            return NotFound(new { message = "Version not found" });
        }

        // Get the full content for this specific version
        var comparison = await _versionService.GetVersionComparisonAsync(versionId);
        
        var versionDto = new ArticleVersionDto
        {
            Id = version.Id,
            ArticleId = articleId,
            VersionNumber = version.VersionNumber,
            CreatedBy = version.CreatedBy.HasValue ? new UserRef
            {
                Id = version.CreatedBy.Value,
                FullName = version.CreatedByName ?? version.CreatedByEmail ?? "Unknown"
            } : null,
            CreatedAt = version.CreatedAt,
            VersionType = Enum.Parse<Domain.Enums.VersionType>(version.VersionType),
            ChangeMessage = version.ChangeMessage,
            ReviewAction = version.Status == Domain.Enums.VersionStatus.AcceptedAiVersion ? Domain.Enums.ReviewAction.Accepted :
                          version.Status == Domain.Enums.VersionStatus.RejectedAiVersion ? Domain.Enums.ReviewAction.Rejected :
                          Domain.Enums.ReviewAction.None,
            ReviewedAt = version.ReviewedAt,
            ReviewedBy = version.ReviewedById.HasValue ? new UserRef
            {
                Id = version.ReviewedById.Value,
                FullName = version.ReviewedByName ?? "Unknown"
            } : null,
            Status = version.Status
        };

        return Ok(versionDto);
    }

    /// <summary>
    /// Get HTML diff for a specific version
    /// </summary>
    /// <param name="articleId">Article ID</param>
    /// <param name="versionId">Version ID</param>
    /// <returns>Version comparison with before/after content</returns>
    [HttpGet("{articleId}/versions/{versionId}/diff")]
    [ProducesResponseType(typeof(ArticleVersionComparisonDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ArticleVersionComparisonDto>> GetVersionDiff(Guid articleId, Guid versionId)
    {
        var article = await _articleRepository.GetByIdAsync(articleId);
        if (article == null)
        {
            return NotFound(new { message = "Article not found" });
        }

        var comparison = await _versionService.GetVersionComparisonAsync(versionId);
        if (comparison == null)
        {
            return NotFound(new { message = "Version not found" });
        }

        return Ok(new ArticleVersionComparisonDto
        { 
            VersionId = versionId,
            VersionNumber = comparison.VersionNumber,
            BeforeContent = comparison.BeforeContent,
            AfterContent = comparison.AfterContent
        });
    }

    /// <summary>
    /// Accept an AI version by creating a new User version with the AI content
    /// </summary>
    /// <param name="articleId">Article ID</param>
    /// <param name="versionId">AI version ID to accept</param>
    /// <returns>New user version details</returns>
    [HttpPost("{articleId}/versions/{versionId}/accept")]
    [ProducesResponseType(typeof(VersionCaptureResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<VersionCaptureResponse>> AcceptAiVersion(Guid articleId, Guid versionId)
    {
        var article = await _articleRepository.GetByIdAsync(articleId);
        if (article == null)
        {
            return NotFound(new { message = "Article not found" });
        }

        var currentUser = await _medleyContext.GetCurrentUserAsync();
        if (currentUser == null)
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        try
        {
            var newVersion = await _versionService.AcceptAiVersionAsync(versionId, currentUser);

            // Register post-commit action to send SignalR notification
            HttpContext.RegisterPostCommitAction(async () =>
            {
                await _hubContext.Clients.All.VersionCreated(new VersionCreatedPayload
                {
                    ArticleId = articleId,
                    VersionId = newVersion.Id,
                    VersionNumber = newVersion.VersionNumber,
                    VersionType = VersionType.User,
                    CreatedAt = newVersion.CreatedAt
                });
            });

            return Ok(new VersionCaptureResponse
            {
                VersionId = newVersion.Id,
                VersionNumber = newVersion.VersionNumber,
                IsNewVersion = true
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accepting AI version {VersionId} for article {ArticleId}", versionId, articleId);
            return StatusCode(500, new { message = "An error occurred while accepting the version" });
        }
    }

    /// <summary>
    /// Reject an AI version by marking it as inactive
    /// </summary>
    /// <param name="articleId">Article ID</param>
    /// <param name="versionId">AI version ID to reject</param>
    /// <returns>Success response</returns>
    [HttpPost("{articleId}/versions/{versionId}/reject")]
    [ProducesResponseType(typeof(VersionActionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<VersionActionResponse>> RejectAiVersion(Guid articleId, Guid versionId)
    {
        var article = await _articleRepository.GetByIdAsync(articleId);
        if (article == null)
        {
            return NotFound(new { message = "Article not found" });
        }

        var currentUser = await _medleyContext.GetCurrentUserAsync();
        if (currentUser == null)
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        try
        {
            await _versionService.RejectAiVersionAsync(versionId, currentUser);

            return Ok(new VersionActionResponse
            {
                Success = true,
                Message = "AI version rejected successfully"
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new VersionActionResponse { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting AI version {VersionId} for article {ArticleId}", versionId, articleId);
            return StatusCode(500, new VersionActionResponse { Success = false, Message = "An error occurred while rejecting the version" });
        }
    }

    /// <summary>
    /// Assign article to user if not already assigned
    /// </summary>
    /// <returns>True if assignment changed, false otherwise</returns>
    private async Task<bool> AssignArticleToUserAsync(Article article, Guid userId)
    {
        if (userId == Guid.Empty || article.AssignedUserId == userId)
        {
            return false;
        }

        article.AssignedUserId = userId;
        
        // Load user data if not already loaded
        if (article.AssignedUser == null || article.AssignedUser.Id != userId)
        {
            article.AssignedUser = await _userRepository.GetByIdAsync(userId);
        }

        return true;
    }

    /// <summary>
    /// Send SignalR notification for assignment change
    /// </summary>
    private async Task SendAssignmentNotificationAsync(Article article)
    {
        HttpContext.RegisterPostCommitAction(async () =>
        {
            await _hubContext.Clients.All.ArticleAssignmentChanged(new ArticleAssignmentChangedPayload
            {
                ArticleId = article.Id,
                UserId = article.AssignedUser?.Id,
                UserName = article.AssignedUser?.FullName,
                UserInitials = article.AssignedUser?.Initials,
                UserColor = article.AssignedUser?.Color,
                Timestamp = DateTimeOffset.UtcNow
            });
        });
    }
}
