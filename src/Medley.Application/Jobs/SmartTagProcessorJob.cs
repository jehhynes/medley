using Medley.Application.Interfaces;
using Medley.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Medley.Application.Jobs;

/// <summary>
/// Background job for automatically tagging sources via the tagging service.
/// Processes sources in batches of 100, with each source in its own transaction.
/// </summary>
public class SmartTagProcessorJob : BaseHangfireJob<SmartTagProcessorJob>
{
    private const int BatchSize = 10;
    
    private readonly ITaggingService _taggingService;
    private readonly IRepository<Organization> _organizationRepository;
    private readonly IRepository<Source> _sourceRepository;
    private readonly IBackgroundJobService _backgroundJobService;

    public SmartTagProcessorJob(
        ITaggingService taggingService,
        IRepository<Organization> organizationRepository,
        IRepository<Source> sourceRepository,
        IBackgroundJobService backgroundJobService,
        IUnitOfWork unitOfWork,
        ILogger<SmartTagProcessorJob> logger) : base(unitOfWork, logger)
    {
        _taggingService = taggingService;
        _organizationRepository = organizationRepository;
        _sourceRepository = sourceRepository;
        _backgroundJobService = backgroundJobService;
    }

    /// <summary>
    /// Executes the smart tag processing job.
    /// Processes up to 100 sources and reschedules itself if more remain.
    /// </summary>
    [DisableMultipleQueuedItemsFilter]
    public async Task ExecuteAsync()
    {
        _logger.LogInformation("Starting SmartTagProcessor job");

        try
        {
            // Check organization settings (no transaction needed for read-only check)
            var organization = await _organizationRepository.Query().FirstOrDefaultAsync();
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
            var sourceIds = await _sourceRepository.Query()
                .Where(s => s.TagsGenerated == null)
                .Select(s => s.Id)
                .Take(BatchSize)
                .ToListAsync();

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
            foreach (var sourceId in sourceIds)
            {
                try
                {
                    await ExecuteWithTransactionAsync(async () =>
                    {
                        var result = await _taggingService.GenerateTagsAsync(sourceId, force: false);
                        
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
                    _logger.LogError(ex, "Error processing source {SourceId}", sourceId);
                    // Continue processing other sources even if one fails
                }
            }

            _logger.LogInformation(
                "SmartTagProcessor batch completed. Processed: {Processed}/{Total}, Tagged as internal: {TaggedInternal}, Errors: {Errors}",
                processedCount, sourceIds.Count, taggedInternalCount, errorCount);

            if (sourceIds.Count == BatchSize)
            {
                _logger.LogInformation("Rescheduling SmartTagProcessor job for remaining sources");
                
                // Schedule the next batch to run in 1 second
                _backgroundJobService.Schedule<SmartTagProcessorJob>(
                    job => job.ExecuteAsync(),
                    TimeSpan.FromSeconds(1));
            }
            else
            {
                _logger.LogInformation("All sources have been processed");
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

