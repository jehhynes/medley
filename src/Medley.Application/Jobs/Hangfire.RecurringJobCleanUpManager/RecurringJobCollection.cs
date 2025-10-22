using System.Collections;

namespace Hangfire.RecurringJobCleanUpManager;

public class RecurringJobCollection : IEnumerable<RecurringJobDescriptor>
{
    public RecurringJobCollection()
    {
        RecurringJobs = new List<RecurringJobDescriptor>();
    }

    public List<RecurringJobDescriptor> RecurringJobs { get; }

    public int Count => RecurringJobs.Count;

    public IEnumerator<RecurringJobDescriptor> GetEnumerator()
    {
        return ((IEnumerable<RecurringJobDescriptor>) RecurringJobs).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return RecurringJobs.GetEnumerator();
    }

    public void Add(RecurringJobDescriptor definition)
    {
        if (ContainsId(definition.Id))
            throw new Exception($"Error, you can't add two identical ids: {definition.Id}");
        RecurringJobs.Add(definition);
    }

    public bool ContainsId(string id)
    {
        return RecurringJobs.Exists(job => job.Id == id);
    }
}
