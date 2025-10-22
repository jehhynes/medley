using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Medley.Application.Hubs;

/// <summary>
/// SignalR hub for real-time integration status updates
/// </summary>
[Authorize(Roles = "Admin")]
public class IntegrationStatusHub : Hub
{
    /// <summary>
    /// Join the integration status group for real-time updates
    /// </summary>
    public async Task JoinIntegrationStatusGroup()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "IntegrationStatus");
    }

    /// <summary>
    /// Leave the integration status group
    /// </summary>
    public async Task LeaveIntegrationStatusGroup()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "IntegrationStatus");
    }

    /// <summary>
    /// Override OnConnectedAsync to automatically join the group
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        await JoinIntegrationStatusGroup();
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Override OnDisconnectedAsync to clean up
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await LeaveIntegrationStatusGroup();
        await base.OnDisconnectedAsync(exception);
    }
}
