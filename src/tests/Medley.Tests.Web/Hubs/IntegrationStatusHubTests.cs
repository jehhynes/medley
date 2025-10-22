using Medley.Application.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Medley.Tests.Web.Hubs;

/// <summary>
/// Unit tests for IntegrationStatusHub
/// </summary>
public class IntegrationStatusHubTests
{
    private readonly Mock<IHubCallerClients> _mockClients;
    private readonly Mock<IGroupManager> _mockGroups;
    private readonly Mock<HubCallerContext> _mockContext;
    private readonly Mock<ILogger<IntegrationStatusHub>> _mockLogger;
    private readonly IntegrationStatusHub _hub;

    public IntegrationStatusHubTests()
    {
        _mockClients = new Mock<IHubCallerClients>();
        _mockGroups = new Mock<IGroupManager>();
        _mockContext = new Mock<HubCallerContext>();
        _mockLogger = new Mock<ILogger<IntegrationStatusHub>>();

        _hub = new IntegrationStatusHub();
        
        // Set up the hub context
        var hubContext = new Mock<HubCallerContext>();
        hubContext.Setup(x => x.ConnectionId).Returns("test-connection-id");
        
        // Use reflection to set the Context property
        var contextProperty = typeof(Hub).GetProperty("Context");
        contextProperty?.SetValue(_hub, hubContext.Object);
    }

    [Fact]
    public async Task JoinIntegrationStatusGroup_AddsConnectionToGroup()
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
        await _hub.JoinIntegrationStatusGroup();

        // Assert
        mockGroups.Verify(
            x => x.AddToGroupAsync("test-connection-id", "IntegrationStatus", CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task LeaveIntegrationStatusGroup_RemovesConnectionFromGroup()
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
        await _hub.LeaveIntegrationStatusGroup();

        // Assert
        mockGroups.Verify(
            x => x.RemoveFromGroupAsync("test-connection-id", "IntegrationStatus", CancellationToken.None),
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
            x => x.AddToGroupAsync("test-connection-id", "IntegrationStatus", CancellationToken.None),
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
            x => x.RemoveFromGroupAsync("test-connection-id", "IntegrationStatus", CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public void IntegrationStatusHub_HasAuthorizeAttribute()
    {
        // Arrange & Act
        var authorizeAttribute = typeof(IntegrationStatusHub)
            .GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .FirstOrDefault() as AuthorizeAttribute;

        // Assert
        Assert.NotNull(authorizeAttribute);
        Assert.Equal("Admin", authorizeAttribute.Roles);
    }
}
