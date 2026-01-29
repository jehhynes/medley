using Hangfire.RecurringJobCleanUpManager;
using Hangfire;
using Microsoft.Extensions.Logging;
using Medley.Application.Interfaces;

namespace Medley.Application.Jobs;

public class JobRegistry : IJobRegistry
{
    private readonly IRecurringJobManager _recurringJobManager;
    private readonly ILogger<JobRegistry> _logger;

    public JobRegistry(IRecurringJobManager recurringJobManager, ILogger<JobRegistry> logger)
    {
        _recurringJobManager = recurringJobManager;
        _logger = logger;
    }

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
    IEnumerable<RecurringJobDescriptor> GetJobDescriptors()
    {
        //https://github.com/HangfireIO/Cronos
        var jobs = new List<RecurringJobDescriptor>
        {
            //RecurringJobDescriptor.Create<IntegrationHealthCheckJob>(j => j.CheckAllIntegrationsHealthAsync(default!, default), Daily(6, 0)),
            //RecurringJobDescriptor.Create<FellowTranscriptSyncJob>(j => j.SyncTranscriptsAsync(default!, default), Hourly(0)),
            RecurringJobDescriptor.Create<EmbeddingGenerationJob>(j => j.GenerateFragmentEmbeddings(default!, default, null), Daily(2, 0)), // Run nightly at 2 AM to pickup missed fragments
            RecurringJobDescriptor.Create<SmartTagProcessorJob>(j => j.ExecuteAsync(default!, default, null), Daily(3, 0)), // Run nightly at 3 AM to pickup missed sources
            RecurringJobDescriptor.Create<SpeakerExtractionJob>(j => j.ExecuteAsync(default!, default, null), Daily(4, 0)), // Run nightly at 4 AM to extract speakers
            
            //RecurringJobDescriptor.Create<KnowledgeUnitClusteringJob>(j => j.ExecuteAsync(default!, default), MinuteInterval(1))
        };

        return jobs;
    }
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

    /// <summary>
    /// Initializes all recurring jobs by registering them with Hangfire and running any missed jobs
    /// </summary>
    public void InitializeRecurringJobs()
    {
        try
        {
            _logger.LogInformation("Starting recurring job initialization");

            var manager = new RecurringJobCleanUpManager(_recurringJobManager);
            
            foreach (var descriptor in GetJobDescriptors())
            {
                manager.Add(descriptor);
                _logger.LogDebug("Added recurring job: {JobId}", descriptor.Id);
            }

            manager.SyncAndRunMissedJobs();
            
            _logger.LogInformation("Recurring job initialization completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during recurring job initialization");
            throw;
        }
    }

    static string Daily(int hour, int minute) => $"{minute} {hour} * * *";
    static string Hourly(int minute) => $"{minute} */1 * * *";
    static string MinuteInterval(int minuteInterval) => $"*/{minuteInterval} * * * *";
}