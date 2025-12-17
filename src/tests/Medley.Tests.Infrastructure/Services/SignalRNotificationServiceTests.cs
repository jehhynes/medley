using Medley.Application.Interfaces;
using Medley.Domain.Enums;
using Medley.Infrastructure.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Medley.Tests.Infrastructure.Services;

/// <summary>
/// Unit tests for SignalRNotificationService
/// </summary>
public class SignalRNotificationServiceTests
{
    private readonly Mock<IHubContext<AdminHub>> _mockHubContext;
    private readonly Mock<IHubClients> _mockHubClients;
    private readonly Mock<IClientProxy> _mockClientProxy;
    private readonly Mock<ILogger<SignalRNotificationService>> _mockLogger;
    private readonly SignalRNotificationService _service;

    public SignalRNotificationServiceTests()
    {
        _mockHubContext = new Mock<IHubContext<AdminHub>>();
        _mockHubClients = new Mock<IHubClients>();
        _mockClientProxy = new Mock<IClientProxy>();
        _mockLogger = new Mock<ILogger<SignalRNotificationService>>();

        _mockHubContext.Setup(x => x.Clients.Group("IntegrationStatus"))
            .Returns(_mockClientProxy.Object);

        _service = new SignalRNotificationService(
            _mockHubContext.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task SendIntegrationStatusUpdateAsync_WithValidData_SendsMessage()
    {
        // Arrange
        var integrationId = Guid.NewGuid();
        var status = ConnectionStatus.Connected;
        var message = "Connection successful";

        // Act
        await _service.SendIntegrationStatusUpdateAsync(integrationId, status, message);

        // Assert
        _mockClientProxy.Verify(
            x => x.SendAsync("IntegrationStatusUpdate", integrationId, "Connected", message, CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task SendIntegrationStatusUpdateAsync_WithNullMessage_SendsEmptyMessage()
    {
        // Arrange
        var integrationId = Guid.NewGuid();
        var status = ConnectionStatus.Error;

        // Act
        await _service.SendIntegrationStatusUpdateAsync(integrationId, status, null);

        // Assert
        _mockClientProxy.Verify(
            x => x.SendAsync("IntegrationStatusUpdate", integrationId, "Error", string.Empty, CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task SendIntegrationStatusUpdateAsync_WithException_LogsError()
    {
        // Arrange
        var integrationId = Guid.NewGuid();
        var status = ConnectionStatus.Connected;
        var message = "Test message";

        _mockClientProxy.Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<object[]>(), CancellationToken.None))
            .ThrowsAsync(new Exception("SignalR error"));

        // Act
        await _service.SendIntegrationStatusUpdateAsync(integrationId, status, message);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to send integration status update")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData(ConnectionStatus.Connected, "Connected")]
    [InlineData(ConnectionStatus.Disconnected, "Disconnected")]
    [InlineData(ConnectionStatus.Error, "Error")]
    public async Task SendIntegrationStatusUpdateAsync_WithDifferentStatuses_SendsCorrectStatus(ConnectionStatus status, string expectedStatus)
    {
        // Arrange
        var integrationId = Guid.NewGuid();
        var message = "Test message";

        // Act
        await _service.SendIntegrationStatusUpdateAsync(integrationId, status, message);

        // Assert
        _mockClientProxy.Verify(
            x => x.SendAsync("IntegrationStatusUpdate", integrationId, expectedStatus, message, CancellationToken.None),
            Times.Once);
    }
}
