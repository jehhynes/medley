using System.Linq.Expressions;
using Hangfire;
using Medley.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Medley.Infrastructure.Services;

/// <summary>
/// Implementation of background job service using Hangfire
/// </summary>
public class BackgroundJobService : IBackgroundJobService
{
    private readonly ILogger<BackgroundJobService> _logger;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IRecurringJobManager _recurringJobManager;

    public BackgroundJobService(
        ILogger<BackgroundJobService> logger,
        IBackgroundJobClient backgroundJobClient,
        IRecurringJobManager recurringJobManager)
    {
        _logger = logger;
        _backgroundJobClient = backgroundJobClient;
        _recurringJobManager = recurringJobManager;
    }

    public string Enqueue<T>(Expression<Func<T, Task>> methodCall)
    {
        try
        {
            var jobId = _backgroundJobClient.Enqueue(methodCall);
            _logger.LogInformation("Enqueued job {JobId}", jobId);
            return jobId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enqueue job");
            throw;
        }
    }

    public string Schedule<T>(Expression<Func<T, Task>> methodCall, TimeSpan delay)
    {
        try
        {
            var jobId = _backgroundJobClient.Schedule(methodCall, delay);
            _logger.LogInformation("Scheduled job {JobId} with delay {Delay}", jobId, delay);
            return jobId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to schedule job");
            throw;
        }
    }

    public void AddOrUpdateRecurring<T>(string recurringJobId, Expression<Func<T, Task>> methodCall, string cronExpression)
    {
        try
        {
            _recurringJobManager.AddOrUpdate(recurringJobId, methodCall, cronExpression);
            _logger.LogInformation("Added or updated recurring job {RecurringJobId} with cron {CronExpression}", 
                recurringJobId, cronExpression);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add or update recurring job {RecurringJobId}", recurringJobId);
            throw;
        }
    }
}
