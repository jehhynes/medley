using Medley.Application.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Medley.Tests.Web.Hubs;

/// <summary>
/// Unit tests for AdminHub
/// </summary>
public class AdminHubTests
{
    private readonly Mock<IHubCallerClients> _mockClients;
    private readonly Mock<IGroupManager> _mockGroups;
    private readonly Mock<HubCallerContext> _mockContext;
    private readonly Mock<ILogger<AdminHub>> _mockLogger;
    private readonly AdminHub _hub;

    public AdminHubTests()
    {
        _mockClients = new Mock<IHubCallerClients>();
        _mockGroups = new Mock<IGroupManager>();
        _mockContext = new Mock<HubCallerContext>();
        _mockLogger = new Mock<ILogger<AdminHub>>();

        _hub = new AdminHub();
        
        // Set up the hub context
        var hubContext = new Mock<HubCallerContext>();
        hubContext.Setup(x => x.ConnectionId).Returns("test-connection-id");
        
        // Use reflection to set the Context property
        var contextProperty = typeof(Hub).GetProperty("Context");
        contextProperty?.SetValue(_hub, hubContext.Object);
    }

    [Fact]
    public async Task JoinAdminGroup_AddsConnectionToGroup()
    {
        // Arrange
        var mockGroups = new Mock<IGroupManager>();
        var hubContext = new Mock<HubCallerContext>();
        hubContext.Setup(x => x.ConnectionId).Returns("test-connection-id");
        
        // Use reflection to set the Groups property
        var groupsProperty = typeof(Hub).GetProperty("Groups");
        groupsProperty?.SetValue(_hub, mockGroups.Object);
        
        // Use reflection to set the Context property
        var contextProperty = typeof(Hub).GetProperty("Context");
        contextProperty?.SetValue(_hub, hubContext.Object);

        // Act
        await _hub.JoinAdminGroup();

        // Assert
        mockGroups.Verify(
            x => x.AddToGroupAsync("test-connection-id", "AdminNotifications", CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task LeaveAdminGroup_RemovesConnectionFromGroup()
    {
        // Arrange
        var mockGroups = new Mock<IGroupManager>();
        var hubContext = new Mock<HubCallerContext>();
        hubContext.Setup(x => x.ConnectionId).Returns("test-connection-id");
        
        // Use reflection to set the Groups property
        var groupsProperty = typeof(Hub).GetProperty("Groups");
        groupsProperty?.SetValue(_hub, mockGroups.Object);
        
        // Use reflection to set the Context property
        var contextProperty = typeof(Hub).GetProperty("Context");
        contextProperty?.SetValue(_hub, hubContext.Object);

        // Act
        await _hub.LeaveAdminGroup();

        // Assert
        mockGroups.Verify(
            x => x.RemoveFromGroupAsync("test-connection-id", "AdminNotifications", CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task OnConnectedAsync_AutomaticallyJoinsGroup()
    {
        // Arrange
        var mockGroups = new Mock<IGroupManager>();
        var hubContext = new Mock<HubCallerContext>();
        hubContext.Setup(x => x.ConnectionId).Returns("test-connection-id");
        
        // Use reflection to set the Groups property
        var groupsProperty = typeof(Hub).GetProperty("Groups");
        groupsProperty?.SetValue(_hub, mockGroups.Object);
        
        // Use reflection to set the Context property
        var contextProperty = typeof(Hub).GetProperty("Context");
        contextProperty?.SetValue(_hub, hubContext.Object);

        // Act
        await _hub.OnConnectedAsync();

        // Assert
        mockGroups.Verify(
            x => x.AddToGroupAsync("test-connection-id", "AdminNotifications", CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task OnDisconnectedAsync_RemovesConnectionFromGroup()
    {
        // Arrange
        var mockGroups = new Mock<IGroupManager>();
        var hubContext = new Mock<HubCallerContext>();
        hubContext.Setup(x => x.ConnectionId).Returns("test-connection-id");
        
        // Use reflection to set the Groups property
        var groupsProperty = typeof(Hub).GetProperty("Groups");
        groupsProperty?.SetValue(_hub, mockGroups.Object);
        
        // Use reflection to set the Context property
        var contextProperty = typeof(Hub).GetProperty("Context");
        contextProperty?.SetValue(_hub, hubContext.Object);

        // Act
        await _hub.OnDisconnectedAsync(null);

        // Assert
        mockGroups.Verify(
            x => x.RemoveFromGroupAsync("test-connection-id", "AdminNotifications", CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public void AdminHub_HasAuthorizeAttribute()
    {
        // Arrange & Act
        var authorizeAttribute = typeof(AdminHub)
            .GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .FirstOrDefault() as AuthorizeAttribute;

        // Assert
        Assert.NotNull(authorizeAttribute);
        Assert.Equal("Admin", authorizeAttribute.Roles);
    }
}

