using Medley.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Medley.Web.Extensions;

/// <summary>
/// Extension methods for HttpContext
/// </summary>
public static class HttpContextExtensions
{
    /// <summary>
    /// Registers an action to be executed after the database transaction commits.
    /// This is a convenience method that delegates to IUnitOfWork.RegisterPostCommitAction.
    /// </summary>
    /// <param name="httpContext">The HTTP context</param>
    /// <param name="action">The action to execute after commit</param>
    public static void RegisterPostCommitAction(this HttpContext httpContext, Func<Task> action)
    {
        var unitOfWork = httpContext.RequestServices.GetService(typeof(IUnitOfWork)) as IUnitOfWork;
        
        if (unitOfWork == null)
        {
            throw new InvalidOperationException("IUnitOfWork service is not available in the request services.");
        }
        
        unitOfWork.RegisterPostCommitAction(action);
    }
}

