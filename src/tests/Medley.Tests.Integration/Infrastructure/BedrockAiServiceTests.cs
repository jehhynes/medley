using Amazon.BedrockRuntime;
using Medley.Application.Configuration;
using Medley.Application.Interfaces;
using Medley.Infrastructure.Services;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Text.Json;
using Xunit;

namespace Medley.Tests.Integration.Infrastructure;

public class BedrockAiServiceTests
{
    private readonly Mock<IChatClient> _mockChatClient;
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

        _mockChatClient = new Mock<IChatClient>();
        _mockOptions = new Mock<IOptions<BedrockSettings>>();
        _mockOptions.Setup(x => x.Value).Returns(_settings);
        _mockLogger = new Mock<ILogger<BedrockAiService>>();
    }

    [Fact]
    public async Task ProcessPromptAsync_ShouldReturnResponseText()
    {
        // Arrange
        var service = new BedrockAiService(_mockChatClient.Object, _mockOptions.Object, _mockLogger.Object);
        var prompt = "Test prompt";
        var expectedResponse = "Test response";

        var mockResponse = new ChatResponse([new ChatMessage(ChatRole.Assistant, expectedResponse)]);

        _mockChatClient.Setup(x => x.GetResponseAsync(
            It.IsAny<IEnumerable<ChatMessage>>(), 
            It.IsAny<ChatOptions?>(), 
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse);

        // Act
        var result = await service.ProcessPromptAsync(prompt);

        // Assert
        Assert.Equal(expectedResponse, result);

        _mockChatClient.Verify(x => x.GetResponseAsync(
            It.Is<IEnumerable<ChatMessage>>(msgs => msgs.Any(m => m.Role == ChatRole.User && m.Text == prompt)),
            It.IsAny<ChatOptions?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessPromptAsync_ShouldIncludeSystemPrompt()
    {
        // Arrange
        var service = new BedrockAiService(_mockChatClient.Object, _mockOptions.Object, _mockLogger.Object);
        var userPrompt = "Test prompt";
        var systemPrompt = "You are a helpful assistant";
        var temperature = 0.5;
        var expectedResponse = "Test response";

        var mockResponse = new ChatResponse([new ChatMessage(ChatRole.Assistant, expectedResponse)]);

        _mockChatClient.Setup(x => x.GetResponseAsync(
            It.IsAny<IEnumerable<ChatMessage>>(), 
            It.IsAny<ChatOptions?>(), 
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse);

        // Act
        var result = await service.ProcessPromptAsync(userPrompt, systemPrompt, null, temperature);

        // Assert
        Assert.Equal(expectedResponse, result);

        _mockChatClient.Verify(x => x.GetResponseAsync(
            It.Is<IEnumerable<ChatMessage>>(msgs => 
                msgs.Any(m => m.Role == ChatRole.System && m.Text == systemPrompt) &&
                msgs.Any(m => m.Role == ChatRole.User && m.Text == userPrompt)),
            It.Is<ChatOptions?>(opts => opts != null && opts.Temperature == (float)temperature),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessStructuredPromptAsync_ShouldReturnDeserializedObject()
    {
        // Arrange
        var service = new BedrockAiService(_mockChatClient.Object, _mockOptions.Object, _mockLogger.Object);
        var userPrompt = "Extract data";
        var expectedJson = "{\"data\": \"extracted\"}";

        var mockResponse = new ChatResponse([new ChatMessage(ChatRole.Assistant, expectedJson)]);

        _mockChatClient.Setup(x => x.GetResponseAsync(
            It.IsAny<IEnumerable<ChatMessage>>(), 
            It.IsAny<ChatOptions?>(), 
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse);

        // Act
        var result = await service.ProcessStructuredPromptAsync<TestDataResponse>(userPrompt);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("extracted", result.Data);
    }

    private class TestDataResponse
    {
        public string Data { get; set; } = string.Empty;
    }

    [Fact]
    public async Task ProcessPromptAsync_ShouldRethrowExceptions()
    {
        // Arrange
        var service = new BedrockAiService(_mockChatClient.Object, _mockOptions.Object, _mockLogger.Object);
        var prompt = "Test prompt";

        _mockChatClient.Setup(x => x.GetResponseAsync(
            It.IsAny<IEnumerable<ChatMessage>>(), 
            It.IsAny<ChatOptions?>(), 
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("AI service error"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ProcessPromptAsync(prompt));
    }
}
