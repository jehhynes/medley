using Hangfire.Server;
using Hangfire.Storage;
using Medley.Application.Interfaces;
using Medley.Application.Services;
using Medley.Domain.Entities;
using Medley.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Medley.Application.Jobs;

/// <summary>
/// Background job for extracting fragments from source content using AI
/// Thin wrapper around FragmentExtractionService for Hangfire execution
/// </summary>
public class FragmentExtractionJob : BaseHangfireJob<FragmentExtractionJob>
{
    private readonly FragmentExtractionService _fragmentExtractionService;
    private readonly INotificationService _notificationService;
    private readonly IRepository<Source> _sourceRepository;
    private readonly IBackgroundJobService _backgroundJobService;

    public FragmentExtractionJob(
        FragmentExtractionService fragmentExtractionService,
        INotificationService notificationService,
        IRepository<Source> sourceRepository,
        IBackgroundJobService backgroundJobService,
        IUnitOfWork unitOfWork,
        ILogger<FragmentExtractionJob> logger) : base(unitOfWork, logger)
    {
        _fragmentExtractionService = fragmentExtractionService;
        _notificationService = notificationService;
        _sourceRepository = sourceRepository;
        _backgroundJobService = backgroundJobService;
    }

    /// <summary>
    /// Executes fragment extraction for a specific source
    /// </summary>
    /// <param name="sourceId">The source ID to extract fragments from</param>
    public async Task ExecuteAsync(PerformContext context, CancellationToken cancellationToken, Guid sourceId)
    {
        try
        {
            using var dlock = context.Connection.AcquireDistributedLock(nameof(FragmentExtractionJob) + "_" + sourceId, TimeSpan.FromSeconds(1));

            int fragmentCount = 0;
            bool success = false;
            string? errorMessage = null;

            // Set status to InProgress before starting extraction (separate transaction for immediate visibility)
            await SetExtractionStatusAsync(sourceId, ExtractionStatus.InProgress, cancellationToken);

            try
            {
                await ExecuteWithTransactionAsync(async () =>
                {
                    _logger.LogInformation("Fragment extraction job started for source {SourceId}", sourceId);

                    var result = await _fragmentExtractionService.ExtractFragmentsAsync(sourceId, cancellationToken);
                    fragmentCount = result.FragmentCount;

                    // Set status to Completed within the same transaction as fragment creation
                    // Zero fragments is a successful outcome, not a failure
                    await SetExtractionStatusInTransactionAsync(sourceId, ExtractionStatus.Completed, cancellationToken);

                    _logger.LogInformation("Fragment extraction job completed for source {SourceId}. Extracted {Count} fragments.", 
                        sourceId, fragmentCount);
                
                    success = true;
                });

                // Enqueue confidence scoring job after successful extraction (outside transaction)
                if (success && fragmentCount > 0)
                {
                    _backgroundJobService.Schedule<FragmentConfidenceScoringJob>(
                        j => j.ExecuteAsync(default!, default, sourceId), TimeSpan.FromSeconds(5));
                    _logger.LogInformation("Enqueued confidence scoring job for source {SourceId}", sourceId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fragment extraction job failed for source {SourceId}", sourceId);
                errorMessage = ex.Message;
                success = false;

                // Set status to Failed (separate transaction since main transaction was rolled back)
                await SetExtractionStatusAsync(sourceId, ExtractionStatus.Failed, cancellationToken);
            }
            finally
            {
                // Send SignalR notification regardless of success/failure
                var message = success 
                    ? $"Successfully extracted {fragmentCount} fragment{(fragmentCount != 1 ? "s" : "")}" 
                    : $"Fragment extraction failed: {errorMessage}";
            
                await _notificationService.SendFragmentExtractionCompleteAsync(sourceId, fragmentCount, success, message);
            }
        }
        catch (DistributedLockTimeoutException)
        {
            // Another job of same type is already running. Just return success.
        }
    }

    /// <summary>
    /// Updates the extraction status for a source (uses separate transaction)
    /// </summary>
    private async Task SetExtractionStatusAsync(Guid sourceId, ExtractionStatus status, CancellationToken cancellationToken = default)
    {
        try
        {
            await ExecuteWithTransactionAsync(async () =>
            {
                await SetExtractionStatusInTransactionAsync(sourceId, status, cancellationToken);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update extraction status for source {SourceId}", sourceId);
            // Don't throw - status update failure shouldn't break the job
        }
    }

    /// <summary>
    /// Updates the extraction status for a source within the current transaction
    /// </summary>
    private async Task SetExtractionStatusInTransactionAsync(Guid sourceId, ExtractionStatus status, CancellationToken cancellationToken = default)
    {
        var source = await _sourceRepository.Query()
            .FirstOrDefaultAsync(s => s.Id == sourceId, cancellationToken);

        if (source != null)
        {
            source.ExtractionStatus = status;
            await _sourceRepository.SaveAsync(source);
            _logger.LogDebug("Updated extraction status for source {SourceId} to {Status}", sourceId, status);
        }
        else
        {
            _logger.LogWarning("Source {SourceId} not found when updating extraction status", sourceId);
        }
    }
}

