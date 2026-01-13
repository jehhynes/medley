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
    private readonly ILogger<ArticleVersionService> _logger;
    private readonly diff_match_patch _diffMatchPatch;

    public ArticleVersionService(
        IRepository<ArticleVersion> versionRepository,
        IRepository<Article> articleRepository,
        ILogger<ArticleVersionService> logger)
    {
        _versionRepository = versionRepository;
        _articleRepository = articleRepository;
        _logger = logger;
        _diffMatchPatch = new diff_match_patch();
    }

    public async Task<ArticleVersionDto> CaptureUserVersionAsync(
        Guid articleId, 
        string newContent, 
        string? previousContent, 
        Guid? userId, 
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

            // Check if we can update the existing version (draft mode)
            if (latestVersion != null && userId.HasValue)
            {
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
            else
            {
                // Create new version (no latest version or no user)
                // latestVersion will be null for first version
                version = await CreateNewVersionAsync(articleId, newContent, latestVersion, userId, cancellationToken);
            }

            // Get user details for the DTO
            string? createdByName = null;
            string? createdByEmail = null;
            if (userId.HasValue)
            {
                var user = await _versionRepository.Query()
                    .Where(v => v.Id == version.Id)
                    .Select(v => new { v.CreatedBy!.FullName, v.CreatedBy.Email })
                    .FirstOrDefaultAsync(cancellationToken);
                
                if (user != null)
                {
                    createdByName = user.FullName;
                    createdByEmail = user.Email;
                }
            }

            return new ArticleVersionDto
            {
                Id = version.Id,
                VersionNumber = version.VersionNumber.ToString(),
                CreatedBy = userId,
                CreatedByName = createdByName,
                CreatedByEmail = createdByEmail,
                CreatedAt = version.CreatedAt,
                IsNewVersion = isNewVersion
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

        await _versionRepository.AddAsync(existingVersion);

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


        var version = new ArticleVersion
        {
            ArticleId = articleId,
            ContentSnapshot = newContent,
            ContentDiff = diffPatch,
            VersionNumber = nextVersion,
            CreatedById = userId,
            CreatedAt = DateTimeOffset.UtcNow,
            VersionType = VersionType.User,
            ParentVersionId = null // User versions have no parent
        };

        version.ModifiedAt = version.CreatedAt;

        await _versionRepository.AddAsync(version);

        _logger.LogInformation(
            "Created new User version {VersionNumber} for article {ArticleId} by user {UserId}",
            nextVersion, articleId, userId);

        return version;
    }

    public async Task<List<ArticleVersionDto>> GetVersionsAsync(
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

        var versions = await query
            .Include(v => v.CreatedBy)
            .Include(v => v.ParentVersion)
            .OrderByDescending(v => v.VersionNumber)
            .Select(v => new ArticleVersionDto
            {
                Id = v.Id,
                // Format: "parentVersion.childVersion" for AI versions, just "version" for User versions
                VersionNumber = v.ParentVersion != null 
                    ? v.ParentVersion.VersionNumber.ToString() + "." + v.VersionNumber.ToString()
                    : v.VersionNumber.ToString(),
                CreatedBy = v.CreatedById,
                CreatedByName = v.CreatedBy != null ? v.CreatedBy.FullName : null,
                CreatedByEmail = v.CreatedBy != null ? v.CreatedBy.Email : null,
                CreatedAt = v.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return versions;
    }

    public async Task<ArticleVersionComparisonDto?> GetVersionComparisonAsync(
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

        return new ArticleVersionComparisonDto
        {
            BeforeContent = previousVersion?.ContentSnapshot ?? string.Empty,
            AfterContent = version.ContentSnapshot,
            VersionNumber = versionNumber
        };
    }

    public async Task<ArticleVersionDto> CreateAiVersionAsync(
        Guid articleId,
        string content,
        string changeMessage,
        Guid userId,
        Guid? conversationId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating AI article version for article: {ArticleId}", articleId);

            // Get the most recent User version - AI versions always use the most recent User version as parent
            var mostRecentUserVersion = await _versionRepository.Query()
                .Where(v => v.ArticleId == articleId && v.VersionType == VersionType.User)
                .OrderByDescending(v => v.VersionNumber)
                .FirstOrDefaultAsync(cancellationToken);

            if (mostRecentUserVersion == null)
            {
                throw new InvalidOperationException("Cannot create AI version: no User version exists. Please create a User version first.");
            }

            // VersionNumber is scoped to the parent User version
            // Get the max version number for AI versions with the same parent and increment
            var maxVersionNumber = await _versionRepository.Query()
                .Where(v => v.ArticleId == articleId
                    && v.VersionType == VersionType.AI
                    && v.ParentVersionId == mostRecentUserVersion.Id)
                .MaxAsync(v => (int?)v.VersionNumber, cancellationToken);

            int versionNumber = (maxVersionNumber ?? 0) + 1;

            // Deactivate existing AI versions
            var existingAiVersions = await _versionRepository.Query()
                .Where(v => v.ArticleId == articleId && v.VersionType == VersionType.AI && v.IsActive)
                .ToListAsync(cancellationToken);

            foreach (var existingVersion in existingAiVersions)
            {
                existingVersion.IsActive = false;
                await _versionRepository.AddAsync(existingVersion);
            }

            // Calculate diff from parent User version
            string? contentDiff = null;
            if (!string.IsNullOrEmpty(mostRecentUserVersion.ContentSnapshot))
            {
                var diffs = _diffMatchPatch.diff_main(mostRecentUserVersion.ContentSnapshot, content);
                _diffMatchPatch.diff_cleanupSemantic(diffs);
                contentDiff = _diffMatchPatch.patch_toText(_diffMatchPatch.patch_make(diffs));
            }

            // Create new AI version
            var newVersion = new ArticleVersion
            {
                ArticleId = articleId,
                ContentSnapshot = content,
                ContentDiff = contentDiff,
                VersionNumber = versionNumber,
                CreatedById = userId,
                CreatedAt = DateTimeOffset.UtcNow,
                VersionType = VersionType.AI,
                ParentVersionId = mostRecentUserVersion.Id,
                ChangeMessage = changeMessage,
                IsActive = true,
                ConversationId = conversationId
            };

            await _versionRepository.AddAsync(newVersion);

            _logger.LogInformation("Created AI article version {VersionId} (v{VersionNumber}) for article {ArticleId}",
                newVersion.Id, versionNumber, articleId);

            // Get user details for the DTO
            string? createdByName = null;
            string? createdByEmail = null;
            var user = await _versionRepository.Query()
                .Where(v => v.Id == newVersion.Id)
                .Select(v => new { v.CreatedBy!.FullName, v.CreatedBy.Email })
                .FirstOrDefaultAsync(cancellationToken);

            if (user != null)
            {
                createdByName = user.FullName;
                createdByEmail = user.Email;
            }

            return new ArticleVersionDto
            {
                Id = newVersion.Id,
                VersionNumber = mostRecentUserVersion.Id + "." + newVersion.VersionNumber,
                CreatedBy = userId,
                CreatedByName = createdByName,
                CreatedByEmail = createdByEmail,
                CreatedAt = newVersion.CreatedAt,
                IsNewVersion = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create AI version for article {ArticleId}", articleId);
            throw;
        }
    }
}


