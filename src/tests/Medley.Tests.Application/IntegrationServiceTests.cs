using Medley.Application.Integrations.Interfaces;
using Medley.Application.Integrations.Services;
using Medley.Application.Interfaces;
using Medley.Domain.Entities;
using Medley.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Medley.Tests.Application.Services;

/// <summary>
/// Unit tests for IntegrationService
/// </summary>
public class IntegrationServiceTests
{
    private readonly Mock<IRepository<Integration>> _mockRepository;
    private readonly Mock<ILogger<IntegrationService>> _mockLogger;
    private readonly Mock<IIntegrationConnectionService> _mockGitHubService;
    private readonly Mock<IIntegrationConnectionService> _mockFellowService;
    //private readonly Mock<INotificationService> _mockNotificationService;
    private readonly IntegrationService _service;

    public IntegrationServiceTests()
    {
        _mockRepository = new Mock<IRepository<Integration>>();
        _mockLogger = new Mock<ILogger<IntegrationService>>();
        //_mockNotificationService = new Mock<INotificationService>();

        // Setup mock connection services
        _mockGitHubService = new Mock<IIntegrationConnectionService>();
        _mockGitHubService.Setup(s => s.IntegrationType).Returns(IntegrationType.GitHub);

        _mockFellowService = new Mock<IIntegrationConnectionService>();
        _mockFellowService.Setup(s => s.IntegrationType).Returns(IntegrationType.Fellow);

        var connectionServices = new List<IIntegrationConnectionService>
        {
            _mockGitHubService.Object,
            _mockFellowService.Object
        };

        _service = new IntegrationService(
            _mockRepository.Object,
            _mockLogger.Object,
            connectionServices
            //_mockNotificationService.Object
            );
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsIntegration()
    {
        // Arrange
        var id = Guid.NewGuid();
        var expectedIntegration = new Integration
        {
            Id = id,
            Type = IntegrationType.GitHub,
            Name = "Test GitHub Integration",
            ApiKey = "test-key",
            BaseUrl = "https://api.github.com"
        };

        _mockRepository.Setup(r => r.GetByIdAsync(id))
            .ReturnsAsync(expectedIntegration);

        // Act
        var result = await _service.GetByIdAsync(id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
        Assert.Equal(IntegrationType.GitHub, result.Type);
        Assert.Equal("Test GitHub Integration", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockRepository.Setup(r => r.GetByIdAsync(id))
            .ReturnsAsync((Integration?)null);

        // Act
        var result = await _service.GetByIdAsync(id);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Query_ReturnsRepositoryQuery()
    {
        // Arrange
        var integrations = new List<Integration>
        {
            new Integration { Id = Guid.NewGuid(), Type = IntegrationType.GitHub, Name = "GitHub 1" },
            new Integration { Id = Guid.NewGuid(), Type = IntegrationType.Fellow, Name = "Fellow 1" }
        }.AsQueryable();

        _mockRepository.Setup(r => r.Query())
            .Returns(integrations);

        // Act
        var result = _service.Query();

        // Assert
        Assert.Equal(2, result.Count());
        Assert.Contains(result, i => i.Type == IntegrationType.GitHub);
        Assert.Contains(result, i => i.Type == IntegrationType.Fellow);
    }

    [Fact]
    public async Task AddAsync_WithValidIntegration_CallsRepositorySave()
    {
        // Arrange
        var integration = new Integration
        {
            Id = Guid.NewGuid(),
            Type = IntegrationType.GitHub,
            Name = "Test Integration"
        };

        _mockRepository.Setup(r => r.Add(integration))
            .Returns(Task.CompletedTask);

        // Act
        await _service.AddAsync(integration);

        // Assert
        _mockRepository.Verify(r => r.Add(integration), Times.Once);
    }

    [Fact]
    public async Task TestConnectionAsync_WithValidIntegration_ReturnsConnected()
    {
        // Arrange
        var integration = new Integration
        {
            Id = Guid.NewGuid(),
            Type = IntegrationType.GitHub,
            Name = "Test GitHub Integration",
            ApiKey = "test-key",
            BaseUrl = "https://api.github.com"
        };

        _mockGitHubService.Setup(s => s.TestConnectionAsync(integration))
            .ReturnsAsync(ConnectionStatus.Connected);

        // Act
        var result = await _service.TestConnectionAsync(integration);

        // Assert
        Assert.Equal(ConnectionStatus.Connected, result);
        _mockGitHubService.Verify(s => s.TestConnectionAsync(integration), Times.Once);
    }

    [Fact]
    public async Task TestConnectionAsync_WithInvalidIntegration_ReturnsError()
    {
        // Arrange
        var integration = new Integration
        {
            Id = Guid.NewGuid(),
            Type = IntegrationType.GitHub,
            Name = "Test GitHub Integration",
            ApiKey = "invalid-key",
            BaseUrl = "https://api.github.com"
        };

        _mockGitHubService.Setup(s => s.TestConnectionAsync(integration))
            .ReturnsAsync(ConnectionStatus.Error);

        // Act
        var result = await _service.TestConnectionAsync(integration);

        // Assert
        Assert.Equal(ConnectionStatus.Error, result);
        _mockGitHubService.Verify(s => s.TestConnectionAsync(integration), Times.Once);
    }

    [Fact]
    public async Task TestConnectionAsync_WithUnknownIntegrationType_ReturnsError()
    {
        // Arrange
        var integration = new Integration
        {
            Id = Guid.NewGuid(),
            Type = IntegrationType.Slack, // Not registered in our mock services
            Name = "Test Slack Integration"
        };

        // Act
        var result = await _service.TestConnectionAsync(integration);

        // Assert
        Assert.Equal(ConnectionStatus.Error, result);
        _mockGitHubService.Verify(s => s.TestConnectionAsync(It.IsAny<Integration>()), Times.Never);
        _mockFellowService.Verify(s => s.TestConnectionAsync(It.IsAny<Integration>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ThrowsNotImplementedException()
    {
        // Arrange
        var integration = new Integration
        {
            Id = Guid.NewGuid(),
            Type = IntegrationType.GitHub,
            Name = "Test Integration"
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotImplementedException>(() => _service.DeleteAsync(integration));
    }
}
