using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Medley.Application.Hubs.Clients;

namespace Medley.Application.Hubs;

/// <summary>
/// Strongly-typed SignalR hub for real-time article updates and chat
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
public class ArticleHub : Hub<IArticleClient>
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
        
        await Clients.Group($"Article_{articleId}").ReceiveMessage(new ReceiveMessagePayload(
            articleId,
            userName,
            message,
            DateTimeOffset.UtcNow
        ));
    }

    /// <summary>
    /// Notify all connected clients that an article was created
    /// </summary>
    public async Task NotifyArticleCreated(string articleId, string title, string? parentArticleId)
    {
        await Clients.All.ArticleCreated(new ArticleCreatedPayload(
            articleId,
            title,
            parentArticleId,
            null, // ArticleTypeId not provided in this method
            DateTimeOffset.UtcNow
        ));
    }

    /// <summary>
    /// Notify all connected clients that an article was updated
    /// </summary>
    public async Task NotifyArticleUpdated(string articleId, string title)
    {
        await Clients.All.ArticleUpdated(new ArticleUpdatedPayload(
            articleId,
            title,
            null, // ArticleTypeId not provided in this method
            DateTimeOffset.UtcNow
        ));
    }

    /// <summary>
    /// Notify all connected clients that an article was deleted
    /// </summary>
    public async Task NotifyArticleDeleted(string articleId)
    {
        await Clients.All.ArticleDeleted(new ArticleDeletedPayload(
            articleId,
            DateTimeOffset.UtcNow
        ));
    }

    /// <summary>
    /// Notify all connected clients that an article's assigned user changed
    /// </summary>
    public async Task NotifyArticleAssignmentChanged(string articleId, string? userId, string? userName, string? userInitials, string? userColor)
    {
        await Clients.All.ArticleAssignmentChanged(new ArticleAssignmentChangedPayload(
            articleId,
            userId,
            userName,
            userInitials,
            userColor,
            DateTimeOffset.UtcNow
        ));
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

