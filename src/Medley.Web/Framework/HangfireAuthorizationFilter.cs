using Hangfire.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using System.Security.Claims;

namespace Medley.Web;

/// <summary>
/// Authorization filter for Hangfire dashboard
/// </summary>
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        
        // Require authentication and admin role 
        if (!httpContext.User.Identity?.IsAuthenticated == true)
        {
            return false;
        }

        return httpContext.User.IsInRole("Admin");
    }
}
