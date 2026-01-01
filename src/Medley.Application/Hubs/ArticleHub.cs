using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Medley.Application.Hubs;

/// <summary>
/// SignalR hub for real-time article updates and chat
/// 
/// Events sent by server (via IHubContext in background jobs):
/// - ChatMessageProcessing: Sent when AI agent starts processing a message
/// - ChatMessageReceived: Sent when a user message is received and saved
/// - ChatMessageStreaming: Sent for each text chunk as the AI generates a response
/// - ChatToolInvoked: Sent when the AI agent invokes a tool/function (e.g., search_fragments)
/// - ChatMessageComplete: Sent when the AI response is complete and saved to database
/// - ChatError: Sent when an error occurs during chat processing
/// - PlanGenerated: Sent when an improvement plan is created
/// </summary>
[Authorize]
public class ArticleHub : Hub
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

