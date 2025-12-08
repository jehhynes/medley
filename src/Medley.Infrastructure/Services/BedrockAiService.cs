using Amazon.BedrockRuntime;
using Medley.Application.Configuration;
using Medley.Application.Interfaces;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Medley.Infrastructure.Services;

/// <summary>
/// AWS Bedrock implementation of AI processing service
/// </summary>
public class BedrockAiService : IAiProcessingService
{
    private readonly BedrockSettings _bedrockSettings;
    private readonly ILogger<BedrockAiService> _logger;
    private readonly IChatClient _chatClient;

    public BedrockAiService(
        AmazonBedrockRuntimeClient bedrockClient,
        IOptions<BedrockSettings> bedrockSettings,
        ILogger<BedrockAiService> logger)
    {
        _bedrockSettings = bedrockSettings.Value;
        _logger = logger;
        _chatClient = bedrockClient.AsIChatClient(_bedrockSettings.ModelId);
    }

    public async Task<string> ProcessPromptAsync(
        string userPrompt, 
        string? systemPrompt = null, 
        string? assistantPrompt = null,
        double? temperature = null)
    {
        try
        {
            var messages = BuildMessages(userPrompt, systemPrompt, assistantPrompt);
            var options = BuildChatOptions(temperature);

            var response = await _chatClient.GetResponseAsync(messages, options);

            return response.Text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process prompt with Bedrock");
            throw;
        }
    }

    public async Task<T> ProcessStructuredPromptAsync<T>(
        string userPrompt, 
        string? systemPrompt = null, 
        string? assistantPrompt = null,
        double? temperature = null)
    {
        try
        {
            var messages = BuildMessages(userPrompt, systemPrompt, assistantPrompt);
            var options = BuildChatOptions(temperature, ChatResponseFormat.Json);

            var response = await _chatClient.GetResponseAsync<T>(messages, options, useJsonSchemaResponseFormat: false);

            string originalText = null;

            //Strip the markdown json fences that Claude likes to add before deserializing
            if (response.Messages.Count == 1 && response.Messages.Single().Contents.Count == 1 && response.Messages.Single().Contents.Single() is TextContent textContent
                && textContent.Text.StartsWith("```json"))
            {
                var startIndex = textContent.Text.IndexOf('{');
                var endIndex = textContent.Text.LastIndexOf('}') + 1;
                if (startIndex >= 0 && endIndex > startIndex)
                {
                    originalText = textContent.Text;

                    textContent.Text = textContent.Text.Substring(startIndex, endIndex - startIndex);
                }
            }
            
            // Try to get the structured result
            if (response.TryGetResult(out T? result) && result != null)
            {
                return result;
            }
            
            throw new InvalidOperationException($"Failed to get structured result of type {typeof(T).Name} from AI response");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process structured prompt with Bedrock");
            throw;
        }
    }

    /// <summary>
    /// Builds a list of chat messages from the provided prompts
    /// </summary>
    private List<ChatMessage> BuildMessages(string userPrompt, string? systemPrompt, string? assistantPrompt)
    {
        var messages = new List<ChatMessage>();
        
        // Add system message if provided
        if (!string.IsNullOrWhiteSpace(systemPrompt))
        {
            messages.Add(new ChatMessage(ChatRole.System, systemPrompt));
        }
        
        // Add user message
        messages.Add(new ChatMessage(ChatRole.User, userPrompt));
        
        // Add assistant prefill if provided
        if (!string.IsNullOrWhiteSpace(assistantPrompt))
        {
            messages.Add(new ChatMessage(ChatRole.Assistant, assistantPrompt));
        }
        
        return messages;
    }

    /// <summary>
    /// Builds chat options with the specified parameters
    /// </summary>
    private ChatOptions BuildChatOptions(double? temperature, ChatResponseFormat? responseFormat = null)
    {
        var options = new ChatOptions
        {
            Temperature = (float)(temperature ?? _bedrockSettings.Temperature),
        };

        if (responseFormat != null)
        {
            options.ResponseFormat = responseFormat;
        }

        return options;
    }
}
