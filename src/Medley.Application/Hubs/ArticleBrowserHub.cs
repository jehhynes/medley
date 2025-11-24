using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Medley.Application.Hubs;

/// <summary>
/// SignalR hub for real-time article browser updates and chat
/// </summary>
[Authorize]
public class ArticleBrowserHub : Hub
{
    /// <summary>
    /// Join a specific article's chat room
    /// </summary>
    public async Task JoinArticle(string articleId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Article_{articleId}");
    }

    /// <summary>
    /// Leave a specific article's chat room
    /// </summary>
    public async Task LeaveArticle(string articleId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Article_{articleId}");
    }

    /// <summary>
    /// Send a chat message to an article's room
    /// </summary>
    public async Task SendMessage(string articleId, string message)
    {
        var userName = Context.User?.Identity?.Name ?? "Anonymous";
        
        await Clients.Group($"Article_{articleId}").SendAsync("ReceiveMessage", new
        {
            ArticleId = articleId,
            UserName = userName,
            Message = message,
            Timestamp = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Notify all connected clients that an article was created
    /// </summary>
    public async Task NotifyArticleCreated(string articleId, string title, string? parentArticleId)
    {
        await Clients.All.SendAsync("ArticleCreated", new
        {
            ArticleId = articleId,
            Title = title,
            ParentArticleId = parentArticleId,
            Timestamp = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Notify all connected clients that an article was updated
    /// </summary>
    public async Task NotifyArticleUpdated(string articleId, string title)
    {
        await Clients.All.SendAsync("ArticleUpdated", new
        {
            ArticleId = articleId,
            Title = title,
            Timestamp = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Notify all connected clients that an article was deleted
    /// </summary>
    public async Task NotifyArticleDeleted(string articleId)
    {
        await Clients.All.SendAsync("ArticleDeleted", new
        {
            ArticleId = articleId,
            Timestamp = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Override OnConnectedAsync to handle connection
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Override OnDisconnectedAsync to clean up
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}

