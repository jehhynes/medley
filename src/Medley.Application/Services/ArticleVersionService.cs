using DiffMatchPatch;
using Medley.Application.Interfaces;
using Medley.Domain.Entities;
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

    public async Task<ArticleVersionDto> CaptureVersionAsync(
        Guid articleId, 
        string newContent, 
        string? previousContent, 
        Guid? userId, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get the next version number
            var maxVersion = await _versionRepository.Query()
                .Where(v => v.ArticleId == articleId)
                .MaxAsync(v => (int?)v.VersionNumber, cancellationToken) ?? 0;

            var nextVersion = maxVersion + 1;

            // Generate diff if there's previous content
            string? diffPatch = null;
            if (!string.IsNullOrEmpty(previousContent))
            {
                var diffs = _diffMatchPatch.diff_main(previousContent, newContent);
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
                CreatedAt = DateTimeOffset.UtcNow
            };

            await _versionRepository.SaveAsync(version);

            _logger.LogInformation(
                "Captured version {VersionNumber} for article {ArticleId} by user {UserId}",
                nextVersion, articleId, userId);

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
                VersionNumber = version.VersionNumber,
                CreatedBy = userId,
                CreatedByName = createdByName,
                CreatedByEmail = createdByEmail,
                CreatedAt = version.CreatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Failed to capture version for article {ArticleId}", articleId);
            throw;
        }
    }

    public async Task<List<ArticleVersionDto>> GetVersionHistoryAsync(
        Guid articleId, 
        CancellationToken cancellationToken = default)
    {
        var versions = await _versionRepository.Query()
            .Where(v => v.ArticleId == articleId)
            .Include(v => v.CreatedBy)
            .OrderByDescending(v => v.VersionNumber)
            .Select(v => new ArticleVersionDto
            {
                Id = v.Id,
                VersionNumber = v.VersionNumber,
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
            .FirstOrDefaultAsync(cancellationToken);

        if (version == null)
        {
            return null;
        }

        // Get the previous version
        var previousVersion = await _versionRepository.Query()
            .Where(v => v.ArticleId == version.ArticleId && v.VersionNumber == version.VersionNumber - 1)
            .FirstOrDefaultAsync(cancellationToken);

        return new ArticleVersionComparisonDto
        {
            BeforeContent = previousVersion?.ContentSnapshot ?? string.Empty,
            AfterContent = version.ContentSnapshot,
            VersionNumber = version.VersionNumber,
            PreviousVersionNumber = previousVersion?.VersionNumber
        };
    }
}


