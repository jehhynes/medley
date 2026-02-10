using Hangfire;
using Hangfire.MissionControl;
using Hangfire.Server;
using Medley.Application.Integrations.Services;
using Medley.Application.Interfaces;
using Medley.Domain.Entities;
using Medley.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Medley.Application.Jobs;

/// <summary>
/// Background job for syncing approved articles to Zendesk Help Center
/// </summary>
[MissionLauncher]
public class ZendeskArticleSyncJob : BaseHangfireJob<ZendeskArticleSyncJob>
{
    private readonly IRepository<Article> _articleRepository;
    private readonly IRepository<Organization> _organizationRepository;
    private readonly ZendeskService _zendeskService;
    private readonly IBackgroundJobClient _backgroundJobClient;

    public ZendeskArticleSyncJob(
        IRepository<Article> articleRepository,
        IRepository<Organization> organizationRepository,
        ZendeskService zendeskService,
        IBackgroundJobClient backgroundJobClient,
        IUnitOfWork unitOfWork,
        ILogger<ZendeskArticleSyncJob> logger) : base(unitOfWork, logger)
    {
        _articleRepository = articleRepository;
        _organizationRepository = organizationRepository;
        _zendeskService = zendeskService;
        _backgroundJobClient = backgroundJobClient;
    }

    /// <summary>
    /// Syncs a single article to Zendesk
    /// </summary>
    /// <param name="articleId">ID of the article to sync</param>
    /// <param name="context">Hangfire perform context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task SyncArticleAsync(
        Guid articleId,
        PerformContext context,
        CancellationToken cancellationToken)
    {
        await ExecuteWithTransactionAsync(async () =>
        {
            LogInfo(context, $"Starting sync for article {articleId}");

            // Load article
            var article = await _articleRepository.Query()
                .FirstOrDefaultAsync(a => a.Id == articleId, cancellationToken);

            if (article == null)
            {
                LogWarning(context, $"Article {articleId} not found");
                return;
            }

            // Load organization settings
            var organization = await _organizationRepository.Query()
                .FirstOrDefaultAsync(cancellationToken);

            if (organization == null)
            {
                LogWarning(context, "No organization found");
                return;
            }

            // Check if Zendesk sync is enabled for this organization
            if (!organization.EnableArticleZendeskSync)
            {
                LogInfo(context, $"Zendesk sync is disabled for organization {organization.Name}. Skipping.");
                return;
            }

            // Check article status
            if (article.Status != ArticleStatus.Approved)
            {
                LogWarning(context, $"Article {articleId} is not approved (status: {article.Status}). Skipping sync.");
                return;
            }

            // Verify article has required content
            if (string.IsNullOrWhiteSpace(article.Title))
            {
                LogWarning(context, $"Article {articleId} has no title. Skipping sync.");
                return;
            }

            try
            {
                if (string.IsNullOrWhiteSpace(article.ZendeskArticleId))
                {
                    // Create new article in Zendesk
                    LogInfo(context, $"Creating new article in Zendesk: {article.Title}");
                    var zendeskArticleId = await _zendeskService.CreateArticleAsync(
                        article.Title,
                        article.Content,
                        cancellationToken);

                    article.ZendeskArticleId = zendeskArticleId;
                    article.LastSyncedToZendeskAt = DateTimeOffset.UtcNow;

                    LogSuccess(context, $"Created Zendesk article {zendeskArticleId} for article {articleId}");
                }
                else
                {
                    // Update existing article in Zendesk
                    LogInfo(context, $"Updating existing Zendesk article {article.ZendeskArticleId}: {article.Title}");
                    await _zendeskService.UpdateArticleAsync(
                        article.ZendeskArticleId,
                        article.Title,
                        article.Content,
                        cancellationToken);

                    article.LastSyncedToZendeskAt = DateTimeOffset.UtcNow;

                    LogSuccess(context, $"Updated Zendesk article {article.ZendeskArticleId} for article {articleId}");
                }

                // Save changes will be handled by ExecuteWithTransactionAsync
            }
            catch (HttpRequestException ex)
            {
                LogError(context, ex, $"HTTP error syncing article {articleId} to Zendesk");
                throw; // Allow Hangfire to retry
            }
            catch (Exception ex)
            {
                LogError(context, ex, $"Error syncing article {articleId} to Zendesk");
                throw; // Allow Hangfire to retry
            }
        });
    }

    /// <summary>
    /// Batch sync job that finds and syncs all approved articles that need syncing
    /// </summary>
    /// <param name="context">Hangfire perform context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [Mission]
    public async Task SyncAllApprovedArticlesAsync(
        PerformContext context,
        CancellationToken cancellationToken)
    {
        LogInfo(context, "Starting batch sync of approved articles to Zendesk");

        // Get organization to check if sync is enabled
        var organization = await _organizationRepository.Query()
            .FirstOrDefaultAsync(cancellationToken);

        if (organization == null || !organization.EnableArticleZendeskSync)
        {
            LogInfo(context, "Zendesk sync is disabled or no organization found. Skipping batch sync.");
            return;
        }

        // Find articles that need syncing (never synced before)
        // Note: Updates to already-synced articles are handled by the event-driven sync
        var articlesToSync = await _articleRepository.Query()
            .Where(a => a.Status == ArticleStatus.Approved
                && a.LastSyncedToZendeskAt == null)
            .Select(a => new { a.Id, a.Title })
            .ToListAsync(cancellationToken);

        if (articlesToSync.Count == 0)
        {
            LogInfo(context, "No articles need syncing. All approved articles are up to date.");
            return;
        }

        LogInfo(context, $"Found {articlesToSync.Count} article(s) that need syncing");

        // Process in batches of 10
        const int batchSize = 10;
        var processedCount = 0;

        for (int i = 0; i < articlesToSync.Count; i += batchSize)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                LogInfo(context, "Cancellation requested. Stopping batch sync.");
                break;
            }

            var batch = articlesToSync.Skip(i).Take(batchSize);
            
            foreach (var article in batch)
            {
                try
                {
                    // Enqueue individual sync job for each article
                    _backgroundJobClient.Enqueue<ZendeskArticleSyncJob>(
                        job => job.SyncArticleAsync(article.Id, default!, default));

                    processedCount++;
                    LogInfo(context, $"Enqueued sync job for article {article.Id}: {article.Title}");
                }
                catch (Exception ex)
                {
                    LogError(context, ex, $"Failed to enqueue sync job for article {article.Id}");
                    // Continue with other articles
                }
            }

            // Small delay between batches to avoid overwhelming the system
            if (i + batchSize < articlesToSync.Count)
            {
                await Task.Delay(1000, cancellationToken);
            }
        }

        LogSuccess(context, $"Batch sync completed. Enqueued {processedCount} sync job(s)");
    }
}
