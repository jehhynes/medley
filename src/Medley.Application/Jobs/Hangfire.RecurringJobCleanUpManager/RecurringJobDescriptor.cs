using Hangfire.Common;
using System.Linq.Expressions;

namespace Hangfire.RecurringJobCleanUpManager;

public class RecurringJobDescriptor : IEquatable<RecurringJobDescriptor>
{
    public RecurringJobDescriptor(string id, Job job, string cronExpression, RecurringJobOptions recurringJobOptions)
    {
        Job = job ?? throw new ArgumentNullException(nameof(job));
        CronExpression = cronExpression ?? throw new ArgumentNullException(nameof(cronExpression));
        RecurringJobOptions = recurringJobOptions ?? throw new ArgumentNullException(nameof(recurringJobOptions));
        Id = id ?? throw new ArgumentNullException(nameof(id));
    }

    public string Id { get; }

    public Job Job { get; }

    public RecurringJobOptions RecurringJobOptions { get; }

    public string CronExpression { get; }


    public bool Equals(RecurringJobDescriptor other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return string.Equals(Id, other.Id) &&
               Job.Type == other.Job.Type &&
               Equals(Job.Method, other.Job.Method) &&
               //Equals(RecurringJobOptions.QueueName, other.RecurringJobOptions.QueueName) &&
               Equals(RecurringJobOptions.TimeZone, other.RecurringJobOptions.TimeZone) &&
               string.Equals(CronExpression, other.CronExpression);
    }

    //public static RecurringJobDescriptor Create<T>(string id, Expression<Action<T>> methodCall, string cronExpression,
    //    string queue, TimeZoneInfo timeZone)
    //{
    //    var options = new RecurringJobOptions
    //    {
    //        QueueName = queue,
    //        TimeZone = timeZone,
    //    };
    //    return Create(id, methodCall, cronExpression, options);
    //}

    //public static RecurringJobDescriptor Create<T>(string id, Expression<Action<T>> methodCall, string cronExpression,
    //    string queue)
    //{
    //    var options = new RecurringJobOptions
    //    {
    //        QueueName = queue,
    //        TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time")
    //    };
    //    return Create(id, methodCall, cronExpression, options);
    //}

    public static RecurringJobDescriptor Create<T>(string id, Expression<Action<T>> methodCall, string cronExpression)
    {
        var options = new RecurringJobOptions()
        {
            TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time")
        };
        return Create(id, methodCall, cronExpression, options);
    }

    private static RecurringJobDescriptor Create<T>(string id, Expression<Action<T>> methodCall, string cronExpression,
        RecurringJobOptions options)
    {
        var job = Job.FromExpression(methodCall);
        return new RecurringJobDescriptor(id, job, cronExpression, options);
    }


    //public static RecurringJobDescriptor Create<T>(Expression<Action<T>> methodCall, string cronExpression,
    //    string queue, TimeZoneInfo timeZone) => Create(typeof(T).Name, methodCall, cronExpression, queue, timeZone);

    //public static RecurringJobDescriptor Create<T>(Expression<Action<T>> methodCall, string cronExpression,
    //    string queue) => Create(typeof(T).Name, methodCall, cronExpression, queue);

    public static RecurringJobDescriptor Create<T>(Expression<Action<T>> methodCall, string cronExpression)
        => Create(typeof(T).Name, methodCall, cronExpression);

    //private static RecurringJobDescriptor Create<T>(Expression<Action<T>> methodCall, string cronExpression,
    //    RecurringJobOptions options) => Create(typeof(T).Name, methodCall, cronExpression, options);

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((RecurringJobDescriptor) obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Id != null ? Id.GetHashCode() : 0;
            hashCode = (hashCode * 397) ^ (Job != null ? Job.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (RecurringJobOptions != null ? RecurringJobOptions.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (CronExpression != null ? CronExpression.GetHashCode() : 0);
            return hashCode;
        }
    }
}
