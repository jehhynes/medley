using System.Linq.Expressions;

namespace Medley.Application.Interfaces;

/// <summary>
/// Service for managing background jobs using Hangfire
/// </summary>
public interface IBackgroundJobService
{
    /// <summary>
    /// Enqueues a job for immediate execution
    /// </summary>
    /// <typeparam name="T">The type of the job method</typeparam>
    /// <param name="methodCall">The method call expression</param>
    /// <returns>Job ID</returns>
    string Enqueue<T>(Expression<Func<T, Task>> methodCall);

    /// <summary>
    /// Schedules a job for delayed execution
    /// </summary>
    /// <typeparam name="T">The type of the job method</typeparam>
    /// <param name="methodCall">The method call expression</param>
    /// <param name="delay">Delay before execution</param>
    /// <returns>Job ID</returns>
    string Schedule<T>(Expression<Func<T, Task>> methodCall, TimeSpan delay);

    /// <summary>
    /// Schedules a recurring job
    /// </summary>
    /// <typeparam name="T">The type of the job method</typeparam>
    /// <param name="recurringJobId">Unique identifier for the recurring job</param>
    /// <param name="methodCall">The method call expression</param>
    /// <param name="cronExpression">Cron expression for scheduling</param>
    void AddOrUpdateRecurring<T>(string recurringJobId, Expression<Func<T, Task>> methodCall, string cronExpression);
}
