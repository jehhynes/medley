using Hangfire.Console;
using Hangfire.Server;
using Medley.Application.Interfaces;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Medley.Application.Jobs;

/// <summary>
/// Base class for Hangfire background jobs with UnitOfWork support
/// </summary>
/// <typeparam name="TJob">The job type for logging purposes</typeparam>
public abstract class BaseHangfireJob<TJob> where TJob : class
{
    protected readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TJob> _logger;
    private readonly string _jobName;

    protected BaseHangfireJob(IUnitOfWork unitOfWork, ILogger<TJob> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _jobName = typeof(TJob).Name;
    }

    /// <summary>
    /// Logs a message to both Hangfire Console and ILogger at Information level
    /// </summary>
    protected void LogInfo(PerformContext? context, string message)
    {
        _logger.LogInformation(message);
        context?.WriteLine(message);
    }

    /// <summary>
    /// Logs a message to both Hangfire Console and ILogger at Warning level
    /// </summary>
    protected void LogWarning(PerformContext context, string message)
    {
        _logger.LogWarning(message);
        context.SetTextColor(ConsoleTextColor.Yellow);
        context.WriteLine($"⚠ {message}");
        context.ResetTextColor();
    }

    /// <summary>
    /// Logs a message to both Hangfire Console and ILogger at Error level
    /// </summary>
    protected void LogError(PerformContext context, string message)
    {
        _logger.LogError(message);
        context.SetTextColor(ConsoleTextColor.Red);
        context.WriteLine($"✗ {message}");
        context.ResetTextColor();
    }

    /// <summary>
    /// Logs a message to both Hangfire Console and ILogger at Error level with exception
    /// </summary>
    protected void LogError(PerformContext context, Exception ex, string message)
    {
        _logger.LogError(ex, message);
        context.SetTextColor(ConsoleTextColor.Red);
        context.WriteLine($"✗ {message}: {ex.Message}");
        context.ResetTextColor();
    }

    /// <summary>
    /// Logs a success message to both Hangfire Console and ILogger
    /// </summary>
    protected void LogSuccess(PerformContext context, string message)
    {
        _logger.LogInformation(message);
        context.SetTextColor(ConsoleTextColor.Green);
        context.WriteLine($"✓ {message}");
        context.ResetTextColor();
    }

    /// <summary>
    /// Logs a debug message to ILogger only (not shown in Hangfire Console)
    /// </summary>
    protected void LogDebug(string message)
    {
        _logger.LogDebug(message);
    }

    /// <summary>
    /// Executes the job with proper transaction management
    /// </summary>
    /// <param name="jobAction">The job action to execute</param>
    /// <param name="isolationLevel">Transaction isolation level (default: ReadCommitted)</param>
    protected async Task ExecuteWithTransactionAsync(
        Func<Task> jobAction,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
    {
        _logger.LogInformation("Starting Hangfire job: {JobName}", _jobName);

        try
        {
            // Begin transaction
            await _unitOfWork.BeginTransactionAsync(isolationLevel);
            
            _logger.LogDebug("Transaction started with {IsolationLevel} isolation for job {JobName}", 
                isolationLevel, _jobName);

            // Execute the job action
            await jobAction();

            // Save changes and commit
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
            
            _logger.LogDebug("Transaction committed successfully for job {JobName}", _jobName);
            _logger.LogInformation("Hangfire job completed successfully: {JobName}", _jobName);
        }
        catch (Exception ex)
        {
            // Rollback transaction on error
            try
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogWarning(ex, "Transaction rolled back due to error in job {JobName}", _jobName);
            }
            catch (Exception rollbackEx)
            {
                _logger.LogError(rollbackEx, "Failed to rollback transaction for job {JobName}", _jobName);
            }

            _logger.LogError(ex, "Hangfire job failed: {JobName}", _jobName);
            throw;
        }
    }

    /// <summary>
    /// Executes the job with proper transaction management and returns a result
    /// </summary>
    /// <typeparam name="TResult">The result type</typeparam>
    /// <param name="jobAction">The job action to execute</param>
    /// <param name="isolationLevel">Transaction isolation level (default: ReadCommitted)</param>
    /// <returns>The result of the job execution</returns>
    protected async Task<TResult> ExecuteWithTransactionAsync<TResult>(
        Func<Task<TResult>> jobAction,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
    {
        _logger.LogInformation("Starting Hangfire job: {JobName}", _jobName);

        try
        {
            // Begin transaction
            await _unitOfWork.BeginTransactionAsync(isolationLevel);
            
            _logger.LogDebug("Transaction started with {IsolationLevel} isolation for job {JobName}", 
                isolationLevel, _jobName);

            // Execute the job action
            var result = await jobAction();

            // Save changes and commit
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
            
            _logger.LogDebug("Transaction committed successfully for job {JobName}", _jobName);
            _logger.LogInformation("Hangfire job completed successfully: {JobName}", _jobName);
            return result;
        }
        catch (Exception ex)
        {
            // Rollback transaction on error
            try
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogWarning(ex, "Transaction rolled back due to error in job {JobName}", _jobName);
            }
            catch (Exception rollbackEx)
            {
                _logger.LogError(rollbackEx, "Failed to rollback transaction for job {JobName}", _jobName);
            }

            _logger.LogError(ex, "Hangfire job failed: {JobName}", _jobName);
            throw;
        }
    }
}
