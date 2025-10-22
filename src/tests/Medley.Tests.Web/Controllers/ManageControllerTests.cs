using Medley.Application.Integrations.Interfaces;
using Medley.Domain.Entities;
using Medley.Domain.Enums;
using Medley.Web.Areas.Integrations.Controllers;
using Medley.Web.Areas.Integrations.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Medley.Tests.Web.Controllers;

/// <summary>
/// Integration tests for ManageController
/// </summary>
public class ManageControllerTests
{
    private readonly Mock<IIntegrationService> _mockIntegrationService;
    private readonly Mock<ILogger<ManageController>> _mockLogger;
    private readonly ManageController _controller;

    public ManageControllerTests()
    {
        _mockIntegrationService = new Mock<IIntegrationService>();
        _mockLogger = new Mock<ILogger<ManageController>>();

        _controller = new ManageController(
            _mockIntegrationService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Index_WithNoFilters_ReturnsViewWithAllIntegrations()
    {
        // Arrange
        var integrations = new List<Integration>
        {
            new Integration { Id = Guid.NewGuid(), Type = IntegrationType.GitHub, DisplayName = "GitHub 1", Status = ConnectionStatus.Connected },
            new Integration { Id = Guid.NewGuid(), Type = IntegrationType.Fellow, DisplayName = "Fellow 1", Status = ConnectionStatus.Disconnected }
        }.AsQueryable();

        _mockIntegrationService.Setup(s => s.Query())
            .Returns(integrations);

        // Act
        var result = await _controller.Index(null, null, null);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ManageViewModel>(viewResult.Model);
        Assert.Equal(2, model.Integrations.Count);
        Assert.Contains(model.Integrations, i => i.Type == IntegrationType.GitHub);
        Assert.Contains(model.Integrations, i => i.Type == IntegrationType.Fellow);
    }

    [Fact]
    public async Task Index_WithSearchFilter_FiltersByDisplayName()
    {
        // Arrange
        var integrations = new List<Integration>
        {
            new Integration { Id = Guid.NewGuid(), Type = IntegrationType.GitHub, DisplayName = "GitHub Integration", Status = ConnectionStatus.Connected },
            new Integration { Id = Guid.NewGuid(), Type = IntegrationType.Fellow, DisplayName = "Fellow Integration", Status = ConnectionStatus.Connected }
        }.AsQueryable();

        _mockIntegrationService.Setup(s => s.Query())
            .Returns(integrations);

        // Act
        var result = await _controller.Index("GitHub", null, null);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ManageViewModel>(viewResult.Model);
        Assert.Equal(1, model.Integrations.Count);
        Assert.Equal("GitHub Integration", model.Integrations.First().DisplayName);
        Assert.Equal("GitHub", model.SearchTerm);
    }

    [Fact]
    public async Task Index_WithTypeFilter_FiltersByIntegrationType()
    {
        // Arrange
        var integrations = new List<Integration>
        {
            new Integration { Id = Guid.NewGuid(), Type = IntegrationType.GitHub, DisplayName = "GitHub 1", Status = ConnectionStatus.Connected },
            new Integration { Id = Guid.NewGuid(), Type = IntegrationType.Fellow, DisplayName = "Fellow 1", Status = ConnectionStatus.Connected }
        }.AsQueryable();

        _mockIntegrationService.Setup(s => s.Query())
            .Returns(integrations);

        // Act
        var result = await _controller.Index(null, IntegrationType.GitHub, null);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ManageViewModel>(viewResult.Model);
        Assert.Equal(1, model.Integrations.Count);
        Assert.Equal(IntegrationType.GitHub, model.Integrations.First().Type);
        Assert.Equal(IntegrationType.GitHub, model.SelectedType);
    }

    [Fact]
    public async Task Index_WithStatusFilter_FiltersByConnectionStatus()
    {
        // Arrange
        var integrations = new List<Integration>
        {
            new Integration { Id = Guid.NewGuid(), Type = IntegrationType.GitHub, DisplayName = "GitHub 1", Status = ConnectionStatus.Connected },
            new Integration { Id = Guid.NewGuid(), Type = IntegrationType.Fellow, DisplayName = "Fellow 1", Status = ConnectionStatus.Disconnected }
        }.AsQueryable();

        _mockIntegrationService.Setup(s => s.Query())
            .Returns(integrations);

        // Act
        var result = await _controller.Index(null, null, ConnectionStatus.Connected);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ManageViewModel>(viewResult.Model);
        Assert.Equal(1, model.Integrations.Count);
        Assert.Equal(ConnectionStatus.Connected, model.Integrations.First().Status);
        Assert.Equal(ConnectionStatus.Connected, model.SelectedStatus);
    }

    [Fact]
    public async Task Add_ReturnsViewWithViewModel()
    {
        // Act
        var result = _controller.Add();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<AddIntegrationViewModel>(viewResult.Model);
        Assert.NotNull(model.IntegrationTypes);
    }

    [Fact]
    public async Task TestConnection_WithValidId_ReturnsJsonResult()
    {
        // Arrange
        var id = Guid.NewGuid();
        var integration = new Integration
        {
            Id = id,
            Type = IntegrationType.GitHub,
            DisplayName = "Test Integration",
            ConfigurationJson = "{\"apiKey\":\"test-key\"}"
        };

        _mockIntegrationService.Setup(s => s.GetByIdAsync(id))
            .ReturnsAsync(integration);
        _mockIntegrationService.Setup(s => s.TestConnectionAsync(integration))
            .ReturnsAsync(ConnectionStatus.Connected);

        // Act
        var result = await _controller.TestConnection(id);

        // Assert
        var jsonResult = Assert.IsType<JsonResult>(result);
        var data = jsonResult.Value;
        Assert.NotNull(data);
        
        // Use reflection to check the properties since it's an anonymous type
        var dataType = data.GetType();
        var successProperty = dataType.GetProperty("success");
        var statusProperty = dataType.GetProperty("status");
        
        Assert.NotNull(successProperty);
        Assert.NotNull(statusProperty);
        Assert.True((bool)successProperty.GetValue(data)!);
        Assert.Equal("Connected", statusProperty.GetValue(data)!.ToString());
    }

    [Fact]
    public async Task TestConnection_WithInvalidId_ReturnsErrorJson()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockIntegrationService.Setup(s => s.GetByIdAsync(id))
            .ReturnsAsync((Integration?)null);

        // Act
        var result = await _controller.TestConnection(id);

        // Assert
        var jsonResult = Assert.IsType<JsonResult>(result);
        var data = jsonResult.Value;
        Assert.NotNull(data);
        
        // Use reflection to check the properties since it's an anonymous type
        var dataType = data.GetType();
        var successProperty = dataType.GetProperty("success");
        var errorProperty = dataType.GetProperty("error");
        
        Assert.NotNull(successProperty);
        Assert.NotNull(errorProperty);
        Assert.False((bool)successProperty.GetValue(data)!);
        Assert.Equal("Integration not found.", errorProperty.GetValue(data)!.ToString());
    }

    [Fact]
    public async Task TestConnection_WithException_ReturnsErrorJson()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockIntegrationService.Setup(s => s.GetByIdAsync(id))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.TestConnection(id);

        // Assert
        var jsonResult = Assert.IsType<JsonResult>(result);
        var data = jsonResult.Value;
        Assert.NotNull(data);
        
        // Use reflection to check the properties since it's an anonymous type
        var dataType = data.GetType();
        var successProperty = dataType.GetProperty("success");
        var errorProperty = dataType.GetProperty("error");
        
        Assert.NotNull(successProperty);
        Assert.NotNull(errorProperty);
        Assert.False((bool)successProperty.GetValue(data)!);
        Assert.Equal("An error occurred while testing the connection.", errorProperty.GetValue(data)!.ToString());
    }
}
