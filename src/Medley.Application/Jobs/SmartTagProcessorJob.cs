using Hangfire;
using Hangfire.MissionControl;
using Hangfire.Server;
using Medley.Application.Interfaces;
using Medley.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Medley.Application.Jobs;

/// <summary>
/// Background job for automatically tagging sources via the tagging service.
/// Processes sources in batches of 100, with each source in its own transaction.
/// </summary>
[MissionLauncher]
public class SmartTagProcessorJob : BaseHangfireJob<SmartTagProcessorJob>
{
    private const int BatchSize = 10;
    
    private readonly ITaggingService _taggingService;
    private readonly IRepository<Organization> _organizationRepository;
    private readonly IRepository<Source> _sourceRepository;
    private readonly IBackgroundJobClient _backgroundJobClient;

    public SmartTagProcessorJob(
        ITaggingService taggingService,
        IRepository<Organization> organizationRepository,
        IRepository<Source> sourceRepository,
        IBackgroundJobClient backgroundJobClient,
        IUnitOfWork unitOfWork,
        ILogger<SmartTagProcessorJob> logger) : base(unitOfWork, logger)
    {
        _taggingService = taggingService;
        _organizationRepository = organizationRepository;
        _sourceRepository = sourceRepository;
        _backgroundJobClient = backgroundJobClient;
    }

    /// <summary>
    /// Executes the smart tag processing job.
    /// Processes up to 100 sources and reschedules itself if more remain.
    /// </summary>
    /// <param name="sourceId">Optional source ID to process a specific source only</param>
    [DisableMultipleQueuedItemsFilter]
    [Mission]
    public async Task ExecuteAsync(PerformContext context, CancellationToken cancellationToken, Guid? sourceId = null)
    {
        var logMessage = sourceId.HasValue
            ? $"Starting SmartTagProcessor job for source {sourceId.Value}"
            : "Starting SmartTagProcessor job";
        _logger.LogInformation(logMessage);

        try
        {
            // Check organization settings (no transaction needed for read-only check)
            var organization = await _organizationRepository.Query().FirstOrDefaultAsync(cancellationToken);
            if (organization == null)
            {
                throw new InvalidOperationException("No organization found; smart tagging cannot proceed.");
            }

            if (!organization.EnableSmartTagging)
            {
                _logger.LogInformation("Smart tagging is disabled for organization {OrganizationId}. Skipping SmartTagProcessor job.",
                    organization.Id);
                return;
            }

            // Get the batch of sources to process
            var query = _sourceRepository.Query()
                .Where(s => s.TagsGenerated == null);

            // Filter by source if specified
            if (sourceId.HasValue)
            {
                query = query.Where(s => s.Id == sourceId.Value);
            }

            var sourceIds = await query
                .Select(s => s.Id)
                .Take(BatchSize)
                .ToListAsync(cancellationToken);

            if (sourceIds.Count == 0)
            {
                _logger.LogInformation("No sources pending tagging.");
                return;
            }

            _logger.LogInformation("Processing batch of {Count} sources", sourceIds.Count);

            int processedCount = 0;
            int taggedInternalCount = 0;
            int errorCount = 0;

            // Process each source in its own transaction
            foreach (var currentSourceId in sourceIds)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Cancellation requested. Stopping batch processing.");
                    break;
                }

                try
                {
                    await ExecuteWithTransactionAsync(async () =>
                    {
                        var result = await _taggingService.GenerateTagsAsync(currentSourceId, force: false, cancellationToken);
                        
                        if (result.Processed)
                        {
                            processedCount++;
                            if (result.IsInternal == true)
                            {
                                taggedInternalCount++;
                            }
                        }
                    });
                }
                catch (Exception ex)
                {
                    errorCount++;
                    _logger.LogError(ex, "Error processing source {SourceId}", currentSourceId);
                    // Continue processing other sources even if one fails
                }
            }

            _logger.LogInformation(
                "SmartTagProcessor batch completed. Processed: {Processed}/{Total}, Tagged as internal: {TaggedInternal}, Errors: {Errors}",
                processedCount, sourceIds.Count, taggedInternalCount, errorCount);

            if (sourceIds.Count == BatchSize && !sourceId.HasValue)
            {
                _logger.LogInformation("Rescheduling SmartTagProcessor job for remaining sources");
                
                // Continue with the next batch after this job completes
                var currentJobId = context.BackgroundJob.Id;
                _backgroundJobClient.ContinueJobWith<SmartTagProcessorJob>(
                    currentJobId,
                    job => job.ExecuteAsync(default!, default, null));
            }
            else
            {
                var completionMessage = sourceId.HasValue
                    ? $"Source {sourceId.Value} has been processed"
                    : "All sources have been processed";
                _logger.LogInformation(completionMessage);
            }
            
            _logger.LogInformation("SmartTagProcessor job completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SmartTagProcessor job failed");
            throw;
        }
    }
}

