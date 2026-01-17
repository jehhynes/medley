using Medley.Application.Hubs;
using Medley.Application.Hubs.Clients;
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
    private readonly Mock<IHubContext<AdminHub, IAdminClient>> _mockHubContext;
    private readonly Mock<IHubClients<IAdminClient>> _mockHubClients;
    private readonly Mock<IAdminClient> _mockClientProxy;
    private readonly Mock<ILogger<SignalRNotificationService>> _mockLogger;
    private readonly SignalRNotificationService _service;

    public SignalRNotificationServiceTests()
    {
        _mockHubContext = new Mock<IHubContext<AdminHub, IAdminClient>>();
        _mockHubClients = new Mock<IHubClients<IAdminClient>>();
        _mockClientProxy = new Mock<IAdminClient>();
        _mockLogger = new Mock<ILogger<SignalRNotificationService>>();

        _mockHubContext.Setup(x => x.Clients).Returns(_mockHubClients.Object);
        _mockHubClients.Setup(x => x.Group("AdminNotifications")).Returns(_mockClientProxy.Object);
        _mockHubClients.Setup(x => x.All).Returns(_mockClientProxy.Object);

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
            x => x.IntegrationStatusUpdate(It.Is<IntegrationStatusUpdatePayload>(p =>
                p.IntegrationId == integrationId &&
                p.Status == "Connected" &&
                p.Message == message)),
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
            x => x.IntegrationStatusUpdate(It.Is<IntegrationStatusUpdatePayload>(p =>
                p.IntegrationId == integrationId &&
                p.Status == "Error" &&
                p.Message == string.Empty)),
            Times.Once);
    }

    [Fact]
    public async Task SendIntegrationStatusUpdateAsync_WithException_LogsError()
    {
        // Arrange
        var integrationId = Guid.NewGuid();
        var status = ConnectionStatus.Connected;
        var message = "Test message";

        _mockClientProxy.Setup(x => x.IntegrationStatusUpdate(It.IsAny<IntegrationStatusUpdatePayload>()))
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
            x => x.IntegrationStatusUpdate(It.Is<IntegrationStatusUpdatePayload>(p =>
                p.IntegrationId == integrationId &&
                p.Status == expectedStatus &&
                p.Message == message)),
            Times.Once);
    }

    [Fact]
    public async Task SendFragmentExtractionCompleteAsync_WithValidData_SendsMessage()
    {
        // Arrange
        var sourceId = Guid.NewGuid();
        var fragmentCount = 42;
        var success = true;
        var message = "Extraction complete";

        // Act
        await _service.SendFragmentExtractionCompleteAsync(sourceId, fragmentCount, success, message);

        // Assert
        _mockClientProxy.Verify(
            x => x.FragmentExtractionComplete(It.Is<FragmentExtractionCompletePayload>(p =>
                p.SourceId == sourceId &&
                p.FragmentCount == fragmentCount &&
                p.Success == success &&
                p.Message == message)),
            Times.Once);
    }

    [Fact]
    public async Task SendFragmentExtractionCompleteAsync_WithNullMessage_SendsEmptyMessage()
    {
        // Arrange
        var sourceId = Guid.NewGuid();
        var fragmentCount = 10;
        var success = false;

        // Act
        await _service.SendFragmentExtractionCompleteAsync(sourceId, fragmentCount, success, null);

        // Assert
        _mockClientProxy.Verify(
            x => x.FragmentExtractionComplete(It.Is<FragmentExtractionCompletePayload>(p =>
                p.SourceId == sourceId &&
                p.FragmentCount == fragmentCount &&
                p.Success == success &&
                p.Message == string.Empty)),
            Times.Once);
    }
}
