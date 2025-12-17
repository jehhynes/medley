using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Medley.Application.Hubs;

/// <summary>
/// SignalR hub for real-time administrative notifications
/// </summary>
[Authorize(Roles = "Admin")]
public class AdminHub : Hub
{
    /// <summary>
    /// Join the admin notifications group for real-time updates
    /// </summary>
    public async Task JoinAdminGroup()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "AdminNotifications");
    }

    /// <summary>
    /// Leave the admin notifications group
    /// </summary>
    public async Task LeaveAdminGroup()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "AdminNotifications");
    }

    /// <summary>
    /// Override OnConnectedAsync to automatically join the group
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        await JoinAdminGroup();
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Override OnDisconnectedAsync to clean up
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await LeaveAdminGroup();
        await base.OnDisconnectedAsync(exception);
    }
}

