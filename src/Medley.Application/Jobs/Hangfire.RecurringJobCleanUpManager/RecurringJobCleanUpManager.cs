using Cronos;
using System.Collections;

namespace Hangfire.RecurringJobCleanUpManager;

public class RecurringJobCleanUpManager : IEnumerable
{
    private readonly RecurringJobCollection jobCollection;
    private readonly IRecurringJobManager recurringJobManager;
    private readonly RecurringJobRepository recurringJobRepository;

    public RecurringJobCleanUpManager(IRecurringJobManager recurringJobManager, RecurringJobRepository recurringJobRepository)
    {
        jobCollection = new RecurringJobCollection();
        this.recurringJobManager = recurringJobManager;
        this.recurringJobRepository = recurringJobRepository;
    }

    public RecurringJobCleanUpManager(IRecurringJobManager recurringJobManager)
        : this(recurringJobManager, new RecurringJobRepository())
    {
    }

    public IEnumerator GetEnumerator()
    {
        return jobCollection.GetEnumerator();
    }

    public void Add(RecurringJobDescriptor definition)
    {
        jobCollection.Add(definition);
    }

    public void SyncAndRunMissedJobs()
    {
        var preExistingRecurringJobs = recurringJobRepository.GetRecurringJobs();

        foreach (var job in preExistingRecurringJobs)
            RemoveJobIfItIsRemovedFromTheCode(job);

        foreach (var job in jobCollection.RecurringJobs)
            recurringJobManager.AddOrUpdate(job.Id, job.Job, job.CronExpression, job.RecurringJobOptions);

        RunMissedJobs(preExistingRecurringJobs);
    }

    private void RemoveJobIfItIsRemovedFromTheCode(Storage.RecurringJobDto job)
    {
        if (!jobCollection.ContainsId(job.Id))
            recurringJobManager.RemoveIfExists(job.Id);
    }

    //Based on: https://discuss.hangfire.io/t/run-missed-recurring-jobs-at-system-startup/331
    private void RunMissedJobs(List<Storage.RecurringJobDto> recurringJobs)
    {
        foreach (var job in recurringJobs)
        {
            var parts = job.Cron.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            var format = CronFormat.Standard;
            if (parts.Length == 6) format |= CronFormat.IncludeSeconds;

            var cronSchedule = CronExpression.Parse(job.Cron, format);
            DateTime? nextExecuting = null;
             
            var lastExecution = job.LastExecution;
            if (lastExecution.HasValue)
            {
                nextExecuting = cronSchedule.GetNextOccurrence(lastExecution.Value, TimeZoneInfo.FindSystemTimeZoneById(job.TimeZoneId));
            }

            if (nextExecuting != null && nextExecuting < job.NextExecution
                && nextExecuting > DateTime.UtcNow.AddHours(1)) //Don't trigger missed jobs if they will naturally be run within the next hour anyway
            {
                recurringJobManager.Trigger(job.Id);
            }
        }
    }
}
