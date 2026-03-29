using Medley.Application.Configuration;
using Medley.Application.Interfaces;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Medley.Infrastructure.Services;

/// <summary>
/// Anthropic API implementation of AI processing service
/// </summary>
public class AnthropicAiService : IAiProcessingService
{
    private readonly AnthropicSettings _anthropicSettings;
    private readonly ILogger<AnthropicAiService> _logger;
    private readonly IChatClient _chatClient;

    public AnthropicAiService(
        IChatClient chatClient,
        IOptions<AnthropicSettings> anthropicSettings,
        ILogger<AnthropicAiService> logger)
    {
        _anthropicSettings = anthropicSettings.Value;
        _logger = logger;
        _chatClient = chatClient;
    }

    public async Task<string> ProcessPromptAsync(
        string userPrompt,
        string? systemPrompt = null,
        double? temperature = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var messages = BuildMessages(userPrompt, systemPrompt);
            var options = BuildChatOptions(temperature);

            var response = await _chatClient.GetResponseAsync(messages, options, cancellationToken);

            return response.Text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process prompt with Anthropic");
            throw;
        }
    }

    public async Task<T> ProcessStructuredPromptAsync<T>(
        string userPrompt,
        string? systemPrompt = null,
        double? temperature = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var messages = BuildMessages(userPrompt, systemPrompt);

            var options = BuildChatOptions(temperature, ChatResponseFormat.ForJsonSchema<T>());
            
            var response = await _chatClient.GetResponseAsync<T>(messages, options);

            // Strip markdown JSON fences that Claude likes to add before deserializing
            if (response.Messages.Count == 1 && response.Messages.Single().Contents.Count == 1
                && response.Messages.Single().Contents.Single() is TextContent textContent
                && textContent.Text.StartsWith("```json"))
            {
                var startIndex = textContent.Text.IndexOf('{');
                var endIndex = textContent.Text.LastIndexOf('}') + 1;
                if (startIndex >= 0 && endIndex > startIndex)
                {
                    textContent.Text = textContent.Text.Substring(startIndex, endIndex - startIndex);
                }
            }

            if (response.TryGetResult(out T? result) && result != null)
            {
                return result;
            }

            throw new InvalidOperationException($"Failed to get structured result of type {typeof(T).Name} from AI response");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process structured prompt with Anthropic");
            throw;
        }
    }

    private List<ChatMessage> BuildMessages(string userPrompt, string? systemPrompt)
    {
        var messages = new List<ChatMessage>();

        if (!string.IsNullOrWhiteSpace(systemPrompt))
        {
            messages.Add(new ChatMessage(ChatRole.System, systemPrompt));
        }

        messages.Add(new ChatMessage(ChatRole.User, userPrompt));

        return messages;
    }

    private ChatOptions BuildChatOptions(double? temperature, ChatResponseFormat? responseFormat = null)
    {
        var options = new ChatOptions
        {
            Temperature = (float)(temperature ?? _anthropicSettings.Temperature),
        };

        if (responseFormat != null)
        {
            options.ResponseFormat = responseFormat;
        }

        return options;
    }
}
