using Medley.Application.Configuration;
using Medley.Application.Interfaces;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Medley.Infrastructure.Services;

/// <summary>
/// OpenAI API implementation of AI processing service
/// </summary>
public class OpenAiService : IAiProcessingService
{
    private readonly OpenAiSettings _openAiSettings;
    private readonly ILogger<OpenAiService> _logger;
    private readonly IChatClient _chatClient;

    public OpenAiService(
        IChatClient chatClient,
        IOptions<OpenAiSettings> openAiSettings,
        ILogger<OpenAiService> logger)
    {
        _openAiSettings = openAiSettings.Value;
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
            _logger.LogError(ex, "Failed to process prompt with OpenAI");
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

            if (response.TryGetResult(out T? result) && result != null)
            {
                return result;
            }

            throw new InvalidOperationException($"Failed to get structured result of type {typeof(T).Name} from AI response");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process structured prompt with OpenAI");
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
            Temperature = (float)(temperature ?? _openAiSettings.Temperature),
        };

        if (responseFormat != null)
        {
            options.ResponseFormat = responseFormat;
        }

        return options;
    }
}
