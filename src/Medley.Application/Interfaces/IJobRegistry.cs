namespace Medley.Application.Interfaces;

/// <summary>
/// Interface for managing recurring job registration and initialization
/// </summary>
public interface IJobRegistry
{
    /// <summary>
    /// Initializes all recurring jobs by registering them with Hangfire and running any missed jobs
    /// </summary>
    void InitializeRecurringJobs();
}
