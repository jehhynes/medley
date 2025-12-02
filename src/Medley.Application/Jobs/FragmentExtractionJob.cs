using Medley.Application.Interfaces;
using Medley.Application.Services;
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

    public FragmentExtractionJob(
        FragmentExtractionService fragmentExtractionService,
        INotificationService notificationService,
        IUnitOfWork unitOfWork,
        ILogger<FragmentExtractionJob> logger) : base(unitOfWork, logger)
    {
        _fragmentExtractionService = fragmentExtractionService;
        _notificationService = notificationService;
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

        try
        {
            await ExecuteWithTransactionAsync(async () =>
            {
                _logger.LogInformation("Fragment extraction job started for source {SourceId}", sourceId);

                fragmentCount = await _fragmentExtractionService.ExtractFragmentsAsync(sourceId);

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
}

