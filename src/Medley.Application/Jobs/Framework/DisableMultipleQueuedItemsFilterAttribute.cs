using Hangfire.Client;
using Hangfire.Common;
using Hangfire.Server;
using Hangfire.Storage;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Medley.Application.Jobs;

/// <summary>
/// Prevent recurring job from being queued again if it is already in the queue
/// Based on https://gist.github.com/odinserj/a8332a3f486773baa009
/// </summary>
public class DisableMultipleQueuedItemsFilterAttribute : JobFilterAttribute, IClientFilter, IServerFilter
{
    private static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan FingerprintTimeout = TimeSpan.FromHours(1);

    public void OnCreating(CreatingContext filterContext)
    {
        if (!AddFingerprintIfNotExists(filterContext.Connection, filterContext.Job))
        {
            filterContext.Canceled = true;
        }
    }

    public void OnPerformed(PerformedContext filterContext)
    {
        RemoveFingerprint(filterContext.Connection, filterContext.BackgroundJob.Job);

        var jobType = filterContext.BackgroundJob.Job.Type;
        //if (typeof(IRequeuableHangfireTask).IsAssignableFrom(jobType))
        //{
        //    if (filterContext.Items.ContainsKey(nameof(RequeueModel)) && filterContext.Items[nameof(RequeueModel)] is RequeueModel m)
        //    {
        //        var requeuer = JobRequeuerFactory.Create(filterContext.BackgroundJob.Job.Type);
        //        if (m.RequeueAt != null)
        //            requeuer.Schedule(m.Database, m.RequeueAt.Value);
        //        else
        //            requeuer.Enqueue(m.Database);
        //    }
        //}
    }

    private static bool AddFingerprintIfNotExists(IStorageConnection connection, Job job)
    {
        using (connection.AcquireDistributedLock(GetFingerprintLockKey(job), LockTimeout))
        {
            var fingerprint = connection.GetAllEntriesFromHash(GetFingerprintKey(job));

            DateTimeOffset timestamp;

            if (fingerprint != null && 
                fingerprint.ContainsKey("Timestamp") &&
                DateTimeOffset.TryParse(fingerprint["Timestamp"], null, DateTimeStyles.RoundtripKind, out timestamp) &&
                DateTimeOffset.UtcNow <= timestamp.Add(FingerprintTimeout))
            {
                // Actual fingerprint found, returning.
                return false;
            }

            // Fingerprint does not exist, it is invalid (no `Timestamp` key),
            // or it is not actual (timeout expired).
            connection.SetRangeInHash(GetFingerprintKey(job), new Dictionary<string, string>
            {
                { "Timestamp", DateTimeOffset.UtcNow.ToString("o") }
            });

            return true;
        }
    }

    private static void RemoveFingerprint(IStorageConnection connection, Job job)
    {
        using (connection.AcquireDistributedLock(GetFingerprintLockKey(job), LockTimeout))
        using (var transaction = connection.CreateWriteTransaction())
        {
            transaction.RemoveHash(GetFingerprintKey(job));
            transaction.Commit();
        }
    }

    private static string GetFingerprintLockKey(Job job)
    {
        return string.Format("{0}:lock", GetFingerprintKey(job));
    }

    private static string GetFingerprintKey(Job job)
    {
        return string.Format("fingerprint:{0}", GetFingerprint(job));
    }

    private static string GetFingerprint(Job job)
    {
        var parameters = string.Empty;
        if (job?.Args != null)
        {
            parameters = string.Join(".", job.Args);
        }
        if (job?.Type == null || job.Method == null)
        {
            return string.Empty;
        }
        var payload = $"{job.Type.FullName}.{job.Method.Name}.{parameters}";
        var hash = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(payload));
        var fingerprint = Convert.ToBase64String(hash);
        return fingerprint;
    }

    void IClientFilter.OnCreated(CreatedContext filterContext)
    {
    }

    void IServerFilter.OnPerforming(PerformingContext filterContext)
    {
    }
}

//public static class RequeueExtensions
//{
//    public static void Requeue(this PerformContext performContext, Database database, DateTimeOffset? requeueAt = null)
//    {
//        if (!typeof(IRequeuableHangfireTask).IsAssignableFrom(performContext.BackgroundJob.Job.Type))
//            throw new Exception("You can only requeue jobs that implement IRequeuableHangfireTask");

//        performContext.Items[nameof(RequeueModel)] = new RequeueModel() { Database = database, RequeueAt = requeueAt };
//    }
//}
