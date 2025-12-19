namespace Medley.Web.Extensions;

/// <summary>
/// Extension methods for HttpContext
/// </summary>
public static class HttpContextExtensions
{
    /// <summary>
    /// Registers an action to be executed after the database transaction commits
    /// </summary>
    /// <param name="httpContext">The HTTP context</param>
    /// <param name="action">The action to execute after commit</param>
    public static void RegisterPostCommitAction(this HttpContext httpContext, Func<Task> action)
    {
        const string key = "PostCommitActions";
        
        if (!httpContext.Items.ContainsKey(key))
        {
            httpContext.Items[key] = new List<Func<Task>>();
        }
        
        var actions = (List<Func<Task>>)httpContext.Items[key]!;
        actions.Add(action);
    }
}

