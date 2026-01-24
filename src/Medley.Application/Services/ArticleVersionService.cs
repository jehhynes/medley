using DiffMatchPatch;
using Medley.Application.Interfaces;
using Medley.Domain.Entities;
using Medley.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Medley.Application.Services;

/// <summary>
/// Service for managing article version history with diff tracking
/// </summary>
public class ArticleVersionService : IArticleVersionService
{
    private readonly IRepository<ArticleVersion> _versionRepository;
    private readonly IRepository<Article> _articleRepository;
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<Plan> _planRepository;
    private readonly ILogger<ArticleVersionService> _logger;
    private readonly diff_match_patch _diffMatchPatch;

    public ArticleVersionService(
        IRepository<ArticleVersion> versionRepository,
        IRepository<Article> articleRepository,
        IRepository<User> userRepository,
        IRepository<Plan> planRepository,
        ILogger<ArticleVersionService> logger)
    {
        _versionRepository = versionRepository;
        _articleRepository = articleRepository;
        _userRepository = userRepository;
        _planRepository = planRepository;
        _logger = logger;
        _diffMatchPatch = new diff_match_patch();
    }

    /// <summary>
    /// Computes the status of a version based on its relationships and review state
    /// </summary>
    private async Task<VersionStatus> ComputeVersionStatusAsync(
        ArticleVersion version,
        Guid? currentVersionId,
        Dictionary<Guid, List<ArticleVersion>> aiVersionsByParent,
        CancellationToken cancellationToken = default)
    {
        // User versions: check if current or old
        if (version.VersionType == VersionType.User)
        {
            return version.Id == currentVersionId 
                ? VersionStatus.CurrentVersion 
                : VersionStatus.OldVersion;
        }

        // AI versions: check review action first
        if (version.ReviewAction == ReviewAction.Accepted)
        {
            return VersionStatus.AcceptedAiVersion;
        }

        if (version.ReviewAction == ReviewAction.Rejected)
        {
            return VersionStatus.RejectedAiVersion;
        }

        // AI versions with ReviewAction.None: determine if pending or old
        if (version.ParentVersionId.HasValue && 
            aiVersionsByParent.TryGetValue(version.ParentVersionId.Value, out var siblingVersions))
        {
            // Find the absolute latest AI version for this parent (highest version number, regardless of review status)
            var absoluteLatestVersion = siblingVersions
                .OrderByDescending(v => v.VersionNumber)
                .FirstOrDefault();

            // Only mark as PendingAiVersion if this is the absolute latest AND unreviewed
            if (absoluteLatestVersion?.Id == version.Id && version.ReviewAction == ReviewAction.None)
            {
                return VersionStatus.PendingAiVersion;
            }
        }

        return VersionStatus.OldAiVersion;
    }

    /// <summary>
    /// Checks if content has meaningful text beyond the markdown H1 header
    /// </summary>
    private bool HasContentBeyondHeader(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return false;
        }

        // Strip HTML comment header (e.g., <!-- articleId: xxx -->)
        var strippedContent = System.Text.RegularExpressions.Regex.Replace(
            content, 
            @"^\s*<!--[\s\S]*?-->\s*", 
            string.Empty, 
            System.Text.RegularExpressions.RegexOptions.None,
            TimeSpan.FromSeconds(1));

        // Find the first H1 markdown heading
        var h1Pattern = @"^#\s+.*?$";
        var match = System.Text.RegularExpressions.Regex.Match(
            strippedContent, 
            h1Pattern, 
            System.Text.RegularExpressions.RegexOptions.Multiline);

        if (!match.Success)
        {
            // No H1 found, check if there's any non-whitespace content
            return !string.IsNullOrWhiteSpace(strippedContent);
        }

        // Get content after the H1
        var contentAfterH1 = strippedContent.Substring(match.Index + match.Length);
        
        // Check if there's any non-whitespace content after the H1
        return !string.IsNullOrWhiteSpace(contentAfterH1);
    }

    public async Task<ArticleVersionServiceDto> CaptureUserVersionAsync(
        Guid articleId, 
        string newContent, 
        string? previousContent, 
        Guid userId, 
        CancellationToken cancellationToken = default)
    {

        try
        {
            // Get the latest version with parent version loaded
            var latestVersion = await _versionRepository.Query()
                .Where(v => v.ArticleId == articleId)
                .Include(v => v.ParentVersion)
                .OrderByDescending(v => v.VersionNumber)
                .FirstOrDefaultAsync(cancellationToken);

            bool isNewVersion = true;
            ArticleVersion version;

            // Check if we need to create a base version first (if article has content beyond header)
            if (latestVersion == null && !string.IsNullOrEmpty(previousContent) && HasContentBeyondHeader(previousContent))
            {
                // Load article with CreatedBy to create base version
                var articleForBase = await _articleRepository.Query()
                    .Where(a => a.Id == articleId)
                    .Include(a => a.CreatedBy)
                    .SingleAsync(cancellationToken);

                // Create base version (v1) with the existing article content
                latestVersion = await CreateBaseVersionAsync(
                    articleForBase,
                    previousContent,
                    cancellationToken);
            }

            // Check if content is unchanged - don't create/update version if identical
            if (latestVersion != null)
            {
                if (previousContent == newContent)
                {
                    version = latestVersion;
                    isNewVersion = true;
                }
                else
                {
                    // Check if we can update the existing version (draft mode)
                    var lastModified = latestVersion.ModifiedAt ?? latestVersion.CreatedAt;
                    var timeSinceModified = DateTimeOffset.UtcNow - lastModified;

                    // Update existing version if same user and within 30 minutes
                    if (latestVersion.CreatedById == userId && timeSinceModified.TotalMinutes <= 30)
                    {
                        // Update existing version (draft mode)
                        isNewVersion = false;
                        version = await UpdateExistingVersionAsync(latestVersion, newContent, cancellationToken);
                    }
                    else
                    {
                        // Create new version with latestVersion as parent
                        version = await CreateNewVersionAsync(articleId, newContent, latestVersion, userId, cancellationToken);
                    }
                }
            }
            else
            {
                version = await CreateNewVersionAsync(articleId, newContent, latestVersion, userId, cancellationToken);
            }

            // Get current version ID for status computation
            var article = await _articleRepository.GetByIdAsync(articleId);
            
            return new ArticleVersionServiceDto
            {
                Id = version.Id,
                VersionNumber = version.VersionNumber.ToString(),
                CreatedBy = userId,
                CreatedByName = version.CreatedBy?.FullName,
                CreatedByEmail = version.CreatedBy?.Email,
                CreatedAt = version.CreatedAt,
                IsNewVersion = isNewVersion,
                VersionType = version.VersionType.ToString(),
                ChangeMessage = version.ChangeMessage,
                Status = version.Id == article?.CurrentVersionId 
                    ? VersionStatus.CurrentVersion 
                    : VersionStatus.OldVersion
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Failed to capture version for article {ArticleId}", articleId);
            throw;
        }
    }

    /// <summary>
    /// Update an existing draft version with new content
    /// </summary>
    private async Task<ArticleVersion> UpdateExistingVersionAsync(
        ArticleVersion existingVersion,
        string newContent,
        CancellationToken cancellationToken)
    {
        // Generate diff from the parent version's content (the version this was based on)
        string? diffPatch = null;
        if (existingVersion.ParentVersion != null && !string.IsNullOrEmpty(existingVersion.ParentVersion.ContentSnapshot))
        {
            var diffs = _diffMatchPatch.diff_main(existingVersion.ParentVersion.ContentSnapshot, newContent);
            _diffMatchPatch.diff_cleanupSemantic(diffs);
            diffPatch = _diffMatchPatch.patch_toText(_diffMatchPatch.patch_make(diffs));
        }

        existingVersion.ContentSnapshot = newContent;
        existingVersion.ContentDiff = diffPatch;
        existingVersion.ModifiedAt = DateTimeOffset.UtcNow;

        

        _logger.LogInformation(
            "Updated draft version {VersionNumber} for article {ArticleId} by user {UserId}",
            existingVersion.VersionNumber, existingVersion.ArticleId, existingVersion.CreatedById);

        return existingVersion;
    }

    /// <summary>
    /// Create a new version with the parent version reference
    /// </summary>
    private async Task<ArticleVersion> CreateNewVersionAsync(
        Guid articleId,
        string newContent,
        ArticleVersion? parentVersion,
        Guid? userId,
        CancellationToken cancellationToken)
    {
        var nextVersion = (parentVersion?.VersionNumber ?? 0) + 1;

        // Generate diff from parent version if it exists
        string? diffPatch = null;
        if (parentVersion != null && !string.IsNullOrEmpty(parentVersion.ContentSnapshot))
        {
            var diffs = _diffMatchPatch.diff_main(parentVersion.ContentSnapshot, newContent);
            _diffMatchPatch.diff_cleanupSemantic(diffs);
            diffPatch = _diffMatchPatch.patch_toText(_diffMatchPatch.patch_make(diffs));
        }

        // Load article for required navigation property
        var article = await _articleRepository.GetByIdAsync(articleId);
        if (article == null)
        {
            throw new InvalidOperationException($"Article {articleId} not found");
        }

        // Load user if userId is provided
        User? user = null;
        if (userId.HasValue)
        {
            user = await _userRepository.GetByIdAsync(userId.Value);
        }

        var version = new ArticleVersion
        {
            Article = article,
            ContentSnapshot = newContent,
            ContentDiff = diffPatch,
            VersionNumber = nextVersion,
            CreatedBy = user,
            CreatedAt = DateTimeOffset.UtcNow,
            VersionType = VersionType.User,
            ParentVersionId = null // User versions have no parent
        };

        version.ModifiedAt = version.CreatedAt;

        await _versionRepository.AddAsync(version);

        // Update the article's CurrentVersionId to point to this new User version
        article.CurrentVersionId = version.Id;

        _logger.LogInformation(
            "Created new User version {VersionNumber} for article {ArticleId} by user {UserId}",
            nextVersion, articleId, userId);

        return version;
    }

    /// <summary>
    /// Create a base version (v1) using the article's creation metadata and existing content
    /// </summary>
    private async Task<ArticleVersion> CreateBaseVersionAsync(
        Article article,
        string existingContent,
        CancellationToken cancellationToken)
    {
        // Use the article's creator if available
        var baseVersion = new ArticleVersion
        {
            Article = article,
            ContentSnapshot = existingContent,
            ContentDiff = null, // No parent version, so no diff
            VersionNumber = 1,
            CreatedBy = article.CreatedBy,
            CreatedAt = article.CreatedAt,
            VersionType = VersionType.User,
            ParentVersionId = null,
            ChangeMessage = "Initial version"
        };

        baseVersion.ModifiedAt = baseVersion.CreatedAt;

        await _versionRepository.AddAsync(baseVersion);

        // Update the article's CurrentVersionId to point to this base version
        article.CurrentVersionId = baseVersion.Id;

        _logger.LogInformation(
            "Created base version (v1) for article {ArticleId} with creation date {CreatedAt} by user {UserId}",
            article.Id, article.CreatedAt, article.CreatedById);

        return baseVersion;
    }

    public async Task<List<ArticleVersionServiceDto>> GetVersionsAsync(
        Guid articleId,
        VersionType? versionType = null,
        CancellationToken cancellationToken = default)
    {
        var query = _versionRepository.Query()
            .Where(v => v.ArticleId == articleId);

        // Apply version type filter if specified
        if (versionType.HasValue)
        {
            query = query.Where(v => v.VersionType == versionType.Value);
        }

        var allVersions = await query
            .Include(v => v.CreatedBy)
            .Include(v => v.ParentVersion)
            .Include(v => v.ReviewedBy)
            .ToListAsync(cancellationToken);

        // Get the article to determine current version
        var article = await _articleRepository.GetByIdAsync(articleId);
        var currentVersionId = article?.CurrentVersionId;

        // Build a lookup of AI versions by parent for status computation
        var aiVersionsByParent = allVersions
            .Where(v => v.VersionType == VersionType.AI && v.ParentVersionId.HasValue)
            .GroupBy(v => v.ParentVersionId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Sort versions hierarchically and compute status for each
        var sortedVersions = new List<ArticleVersionServiceDto>();
        
        foreach (var v in allVersions
            .OrderByDescending(v => v.ParentVersion?.VersionNumber ?? v.VersionNumber)
            .ThenBy(v => v.ParentVersion == null ? 1 : 0)
            .ThenByDescending(v => v.VersionNumber))
        {
            var status = await ComputeVersionStatusAsync(v, currentVersionId, aiVersionsByParent, cancellationToken);
            
            sortedVersions.Add(new ArticleVersionServiceDto
            {
                Id = v.Id,
                VersionNumber = v.ParentVersion != null 
                    ? v.ParentVersion.VersionNumber.ToString() + "." + v.VersionNumber.ToString()
                    : v.VersionNumber.ToString(),
                CreatedBy = v.CreatedById,
                CreatedByName = v.CreatedBy?.FullName,
                CreatedByEmail = v.CreatedBy?.Email,
                CreatedAt = v.CreatedAt,
                VersionType = v.VersionType.ToString(),
                ChangeMessage = v.ChangeMessage,
                Status = status,
                ReviewedAt = v.ReviewedAt,
                ReviewedById = v.ReviewedById,
                ReviewedByName = v.ReviewedBy?.FullName
            });
        }

        return sortedVersions;
    }

    public async Task<ArticleVersionComparisonServiceDto?> GetVersionComparisonAsync(
        Guid versionId,
        CancellationToken cancellationToken = default)
    {
        var version = await _versionRepository.Query()
            .Where(v => v.Id == versionId)
            .Include(v => v.ParentVersion)
            .FirstOrDefaultAsync(cancellationToken);

        if (version == null)
        {
            return null;
        }

        ArticleVersion? previousVersion = null;

        if (version.VersionType == Domain.Enums.VersionType.User)
        {
            // For User versions, compare with the previous User version
            previousVersion = await _versionRepository.Query()
                .Where(v => v.ArticleId == version.ArticleId 
                    && v.VersionType == Domain.Enums.VersionType.User
                    && v.VersionNumber < version.VersionNumber)
                .OrderByDescending(v => v.VersionNumber)
                .Include(v => v.ParentVersion)
                .FirstOrDefaultAsync(cancellationToken);
        }
        else if (version.VersionType == Domain.Enums.VersionType.AI && version.ParentVersionId.HasValue)
        {
            // For AI versions, compare with the parent User version
            previousVersion = await _versionRepository.Query()
                .Where(v => v.Id == version.ParentVersionId.Value)
                .Include(v => v.ParentVersion)
                .FirstOrDefaultAsync(cancellationToken);
        }

        // Format version number
        var versionNumber = version.ParentVersion != null 
            ? $"{version.ParentVersion.VersionNumber}.{version.VersionNumber}"
            : version.VersionNumber.ToString();

        return new ArticleVersionComparisonServiceDto
        {
            BeforeContent = previousVersion?.ContentSnapshot ?? string.Empty,
            AfterContent = version.ContentSnapshot,
            VersionNumber = versionNumber
        };
    }

    public async Task<ArticleVersionServiceDto> CreateAiVersionAsync(
        Guid articleId,
        string content,
        string changeMessage,
        Guid? conversationId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Article? article = null;

            _logger.LogInformation("Creating AI article version for article: {ArticleId}", articleId);

            // Get the most recent User version - AI versions always use the most recent User version as parent
            var mostRecentUserVersion = await _versionRepository.Query()
                .Where(v => v.ArticleId == articleId && v.VersionType == VersionType.User)
                .OrderByDescending(v => v.VersionNumber)
                .FirstOrDefaultAsync(cancellationToken);

            // If no User version exists, create one from the current article content
            if (mostRecentUserVersion == null)
            {
                _logger.LogInformation("No User version found for article {ArticleId}, creating base version", articleId);
                
                article = await _articleRepository.Query()
                    .Where(a => a.Id == articleId)
                    .Include(a => a.CreatedBy)
                    .SingleOrDefaultAsync(cancellationToken);

                if (article == null)
                {
                    throw new InvalidOperationException($"Article {articleId} not found");
                }

                // Create base version with current article content
                mostRecentUserVersion = await CreateBaseVersionAsync(
                    article,
                    article.Content ?? string.Empty,
                    cancellationToken);
            }

            // VersionNumber is scoped to the parent User version
            // Get the max version number for AI versions with the same parent and increment
            var maxVersionNumber = await _versionRepository.Query()
                .Where(v => v.ArticleId == articleId
                    && v.VersionType == VersionType.AI
                    && v.ParentVersionId == mostRecentUserVersion.Id)
                .MaxAsync(v => (int?)v.VersionNumber, cancellationToken);

            int versionNumber = (maxVersionNumber ?? 0) + 1;

            // Calculate diff from parent User version
            string? contentDiff = null;
            if (!string.IsNullOrEmpty(mostRecentUserVersion.ContentSnapshot))
            {
                var diffs = _diffMatchPatch.diff_main(mostRecentUserVersion.ContentSnapshot, content);
                _diffMatchPatch.diff_cleanupSemantic(diffs);
                contentDiff = _diffMatchPatch.patch_toText(_diffMatchPatch.patch_make(diffs));
            }

            // Load article for required navigation property
            if (article == null)
                article = await _articleRepository.GetByIdAsync(articleId);

            if (article == null)
            {
                throw new InvalidOperationException($"Article {articleId} not found");
            }

            // Create new AI version
            var newVersion = new ArticleVersion
            {
                Article = article,
                ContentSnapshot = content,
                ContentDiff = contentDiff,
                VersionNumber = versionNumber,
                CreatedAt = DateTimeOffset.UtcNow,
                VersionType = VersionType.AI,
                ParentVersionId = mostRecentUserVersion.Id,
                ChangeMessage = changeMessage,
                ConversationId = conversationId,
                ReviewAction = ReviewAction.None
            };

            await _versionRepository.AddAsync(newVersion);

            _logger.LogInformation("Created AI article version {VersionId} (v{VersionNumber}) for article {ArticleId}",
                newVersion.Id, versionNumber, articleId);

            return new ArticleVersionServiceDto
            {
                Id = newVersion.Id,
                VersionNumber = mostRecentUserVersion.VersionNumber + "." + newVersion.VersionNumber,
                CreatedBy = null,
                CreatedByName = null,
                CreatedByEmail = null,
                CreatedAt = newVersion.CreatedAt,
                IsNewVersion = true,
                VersionType = newVersion.VersionType.ToString(),
                ChangeMessage = newVersion.ChangeMessage,
                Status = VersionStatus.PendingAiVersion,
                ReviewedAt = null,
                ReviewedById = null,
                ReviewedByName = null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create AI version for article {ArticleId}", articleId);
            throw;
        }
    }

    public async Task<ArticleVersionServiceDto> AcceptAiVersionAsync(
        Guid versionId, 
        User user, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Load the AI version
            var aiVersion = await _versionRepository.Query()
                .Where(v => v.Id == versionId)
                .Include(v => v.Article)
                .Include(v => v.ParentVersion)
                .FirstOrDefaultAsync(cancellationToken);

            if (aiVersion == null)
            {
                throw new InvalidOperationException($"Version {versionId} not found");
            }

            if (aiVersion.VersionType != VersionType.AI)
            {
                throw new InvalidOperationException($"Version {versionId} is not an AI version");
            }

            // Get the latest User version for this article
            var latestUserVersion = await _versionRepository.Query()
                .Where(v => v.Article.Id == aiVersion.Article.Id && v.VersionType == VersionType.User)
                .OrderByDescending(v => v.VersionNumber)
                .FirstOrDefaultAsync(cancellationToken);

            // Determine the content to save (the AI version's content)
            string newContent = aiVersion.ContentSnapshot;
            string? previousContent = latestUserVersion?.ContentSnapshot;

            // Create a new User version with the AI content
            // This will be chained off the latest User version
            var newUserVersion = await CreateNewVersionAsync(
                aiVersion.Article.Id, 
                newContent, 
                latestUserVersion, 
                user.Id, 
                cancellationToken);
            newUserVersion.ChangeMessage = aiVersion.ChangeMessage;

            // Mark the AI version as accepted with review tracking
            aiVersion.ReviewAction = ReviewAction.Accepted;
            aiVersion.ReviewedAt = DateTimeOffset.UtcNow;
            aiVersion.ReviewedById = user.Id;

            // Update the article's content to match the accepted AI version
            var article = aiVersion.Article;
            article.Content = newContent;

            // Copy fragments from plan to article if this version is from a plan implementation
            if (aiVersion.ConversationId.HasValue)
            {
                await CopyFragmentsFromPlan(aiVersion, article, cancellationToken);
            }

            _logger.LogInformation(
                "Accepted AI version {AiVersionId} for article {ArticleId}, created new User version {UserVersionId}",
                versionId, article.Id, newUserVersion.Id);

            return new ArticleVersionServiceDto
            {
                Id = newUserVersion.Id,
                VersionNumber = newUserVersion.VersionNumber.ToString(),
                CreatedBy = user.Id,
                CreatedByName = user.FullName,
                CreatedByEmail = user.Email,
                CreatedAt = newUserVersion.CreatedAt,
                IsNewVersion = true,
                VersionType = newUserVersion.VersionType.ToString(),
                ChangeMessage = newUserVersion.ChangeMessage,
                Status = VersionStatus.CurrentVersion
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to accept AI version {VersionId}", versionId);
            throw;
        }
    }

    private async Task CopyFragmentsFromPlan(ArticleVersion aiVersion, Article article, CancellationToken cancellationToken)
    {
        // Check if this conversation is implementing a plan
        var plan = await _planRepository.Query()
            .Include(p => p.PlanFragments)
                .ThenInclude(pf => pf.Fragment)
            .FirstOrDefaultAsync(p => p.ConversationId != null && p.ConversationId == aiVersion.ConversationId, cancellationToken);

        if (plan != null)
        {
            // Load article with fragments to check for duplicates
            var articleWithFragments = await _articleRepository.Query()
                .Include(a => a.Fragments)
                .FirstOrDefaultAsync(a => a.Id == article.Id, cancellationToken);

            if (articleWithFragments != null)
            {
                // Get fragments marked for inclusion
                var fragmentsToAdd = plan.PlanFragments
                    .Where(pf => pf.Include)
                    .Select(pf => pf.Fragment)
                    .ToList();

                // Add fragments that aren't already linked
                var addedCount = 0;
                foreach (var fragment in fragmentsToAdd)
                {
                    if (!articleWithFragments.Fragments.Any(f => f.Id == fragment.Id))
                    {
                        articleWithFragments.Fragments.Add(fragment);
                        addedCount++;
                    }
                }

                if (addedCount > 0)
                {
                    _logger.LogInformation(
                        "Copied {Count} fragments from plan {PlanId} to article {ArticleId}",
                        addedCount, plan.Id, article.Id);
                }

                // Mark plan as completed
                plan.Status = PlanStatus.Applied;
                plan.AppliedAt = DateTimeOffset.UtcNow;
            }
        }
    }

    public async Task RejectAiVersionAsync(
        Guid versionId,
        User user,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Load the AI version
            var aiVersion = await _versionRepository.Query()
                .Where(v => v.Id == versionId)
                .FirstOrDefaultAsync(cancellationToken);

            if (aiVersion == null)
            {
                throw new InvalidOperationException($"Version {versionId} not found");
            }

            if (aiVersion.VersionType != VersionType.AI)
            {
                throw new InvalidOperationException($"Version {versionId} is not an AI version");
            }

            // Mark the AI version as rejected with review tracking
            aiVersion.ReviewAction = ReviewAction.Rejected;
            aiVersion.ReviewedAt = DateTimeOffset.UtcNow;
            aiVersion.ReviewedById = user.Id;

            _logger.LogInformation("Rejected AI version {VersionId} by user {UserId}", versionId, user.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reject AI version {VersionId}", versionId);
            throw;
        }
    }
}


