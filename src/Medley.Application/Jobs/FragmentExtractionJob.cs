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

    public FragmentExtractionJob(
        FragmentExtractionService fragmentExtractionService,
        INotificationService notificationService,
        IRepository<Source> sourceRepository,
        IUnitOfWork unitOfWork,
        ILogger<FragmentExtractionJob> logger) : base(unitOfWork, logger)
    {
        _fragmentExtractionService = fragmentExtractionService;
        _notificationService = notificationService;
        _sourceRepository = sourceRepository;
    }

    /// <summary>
    /// Executes fragment extraction for a specific source
    /// </summary>
    /// <param name="sourceId">The source ID to extract fragments from</param>
    public async Task ExecuteAsync(Guid sourceId)
    {
        int fragmentCount = 0;
        bool success = false;
        string? errorMessage = null;

        // Set status to InProgress before starting extraction (separate transaction for immediate visibility)
        await SetExtractionStatusAsync(sourceId, ExtractionStatus.InProgress);

        try
        {
            await ExecuteWithTransactionAsync(async () =>
            {
                _logger.LogInformation("Fragment extraction job started for source {SourceId}", sourceId);

                fragmentCount = await _fragmentExtractionService.ExtractFragmentsAsync(sourceId);

                // Set status to Completed within the same transaction as fragment creation
                await SetExtractionStatusInTransactionAsync(sourceId, ExtractionStatus.Completed);

                _logger.LogInformation("Fragment extraction job completed for source {SourceId}. Extracted {Count} fragments.", 
                    sourceId, fragmentCount);
                
                success = true;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fragment extraction job failed for source {SourceId}", sourceId);
            errorMessage = ex.Message;
            success = false;

            // Set status to Failed (separate transaction since main transaction was rolled back)
            await SetExtractionStatusAsync(sourceId, ExtractionStatus.Failed);
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

    /// <summary>
    /// Updates the extraction status for a source (uses separate transaction)
    /// </summary>
    private async Task SetExtractionStatusAsync(Guid sourceId, ExtractionStatus status)
    {
        try
        {
            await ExecuteWithTransactionAsync(async () =>
            {
                await SetExtractionStatusInTransactionAsync(sourceId, status);
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
    private async Task SetExtractionStatusInTransactionAsync(Guid sourceId, ExtractionStatus status)
    {
        var source = await _sourceRepository.Query()
            .FirstOrDefaultAsync(s => s.Id == sourceId);

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

