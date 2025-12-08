using Amazon;
using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using Medley.Application.Configuration;
using Medley.Application.Interfaces;
using Medley.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Text.Json;
using Xunit;

namespace Medley.Tests.Integration.Infrastructure;

public class BedrockAiServiceTests
{
    private readonly Mock<AmazonBedrockRuntimeClient> _mockBedrockClient;
    private readonly Mock<IOptions<BedrockSettings>> _mockOptions;
    private readonly Mock<ILogger<BedrockAiService>> _mockLogger;
    private readonly BedrockSettings _settings;

    public BedrockAiServiceTests()
    {
        _settings = new BedrockSettings
        {
            ModelId = "us.anthropic.claude-sonnet-4-5-20250929-v1:0",
            Temperature = 0.1,
            TimeoutSeconds = 60
        };

        _mockBedrockClient = new Mock<AmazonBedrockRuntimeClient>(new AmazonBedrockRuntimeConfig { RegionEndpoint = Amazon.RegionEndpoint.USEast1 });
        _mockOptions = new Mock<IOptions<BedrockSettings>>();
        _mockOptions.Setup(x => x.Value).Returns(_settings);
        _mockLogger = new Mock<ILogger<BedrockAiService>>();
    }

    [Fact]
    public async Task ProcessPromptAsync_ShouldCallBedrockInvokeModel()
    {
        // Arrange
        var service = new BedrockAiService(_mockBedrockClient.Object, _mockOptions.Object, _mockLogger.Object);
        var prompt = "Test prompt";
        var expectedResponse = "Test response";

        var mockResponse = new InvokeModelResponse
        {
            Body = CreateMockBedrockResponse(expectedResponse)
        };

        _mockBedrockClient.Setup(x => x.InvokeModelAsync(It.IsAny<InvokeModelRequest>(), default))
            .ReturnsAsync(mockResponse);

        // Act
        var result = await service.ProcessPromptAsync(prompt);

        // Assert
        Assert.Equal(expectedResponse, result);

        _mockBedrockClient.Verify(x => x.InvokeModelAsync(
            It.Is<InvokeModelRequest>(r => 
                r.ModelId == _settings.ModelId &&
                r.ContentType == "application/json" &&
                r.Accept == "application/json"),
            default), Times.Once);
    }

    [Fact]
    public async Task ProcessPromptAsync_ShouldUseCustomParameters()
    {
        // Arrange
        var service = new BedrockAiService(_mockBedrockClient.Object, _mockOptions.Object, _mockLogger.Object);
        var userPrompt = "Test prompt";
        var systemPrompt = "You are a helpful assistant";
        var temperature = 0.5;
        var expectedResponse = "Test response";

        var mockResponse = new InvokeModelResponse
        {
            Body = CreateMockBedrockResponse(expectedResponse)
        };

        _mockBedrockClient.Setup(x => x.InvokeModelAsync(It.IsAny<InvokeModelRequest>(), default))
            .ReturnsAsync(mockResponse);

        // Act
        var result = await service.ProcessPromptAsync(userPrompt, systemPrompt, null, temperature);

        // Assert
        Assert.Equal(expectedResponse, result);

        _mockBedrockClient.Verify(x => x.InvokeModelAsync(
            It.Is<InvokeModelRequest>(r => 
                r.ModelId == _settings.ModelId &&
                GetBodyAsString(r.Body).Contains($"\"temperature\":{temperature}") &&
                GetBodyAsString(r.Body).Contains("You are a helpful assistant")),
            default), Times.Once);
    }

    [Fact]
    public async Task ProcessStructuredPromptAsync_ShouldAddJsonInstructions()
    {
        // Arrange
        var service = new BedrockAiService(_mockBedrockClient.Object, _mockOptions.Object, _mockLogger.Object);
        var userPrompt = "Extract data";
        var jsonSchema = "{\"type\": \"object\", \"properties\": {\"data\": {\"type\": \"string\"}}}";
        var expectedResponse = "{\"data\": \"extracted\"}";

        var mockResponse = new InvokeModelResponse
        {
            Body = CreateMockBedrockResponse(expectedResponse)
        };

        _mockBedrockClient.Setup(x => x.InvokeModelAsync(It.IsAny<InvokeModelRequest>(), default))
            .ReturnsAsync(mockResponse);

        // Act
        var result = await service.ProcessStructuredPromptAsync<TestDataResponse>(userPrompt, jsonSchema);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("extracted", result.Data);

        _mockBedrockClient.Verify(x => x.InvokeModelAsync(
            It.Is<InvokeModelRequest>(r => 
                GetBodyAsString(r.Body).Contains("Extract data") &&
                GetBodyAsString(r.Body).Contains("You must respond with valid JSON")),
            default), Times.Once);
    }

    private class TestDataResponse
    {
        public string Data { get; set; } = string.Empty;
    }

    [Fact]
    public async Task ProcessPromptAsync_ShouldHandleBedrockException()
    {
        // Arrange
        var service = new BedrockAiService(_mockBedrockClient.Object, _mockOptions.Object, _mockLogger.Object);
        var prompt = "Test prompt";

        _mockBedrockClient.Setup(x => x.InvokeModelAsync(It.IsAny<InvokeModelRequest>(), default))
            .ThrowsAsync(new AmazonBedrockRuntimeException("Bedrock error"));

        // Act & Assert
        await Assert.ThrowsAsync<AmazonBedrockRuntimeException>(() => service.ProcessPromptAsync(prompt));
    }

    private MemoryStream CreateMockBedrockResponse(string content)
    {
        var response = new
        {
            content = new[]
            {
                new
                {
                    text = content
                }
            }
        };

        var json = JsonSerializer.Serialize(response);
        return new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
    }

    private string GetBodyAsString(MemoryStream body)
    {
        if (body == null) return string.Empty;
        
        // Create a copy to avoid disposal issues
        var bytes = body.ToArray();
        return System.Text.Encoding.UTF8.GetString(bytes);
    }
}
