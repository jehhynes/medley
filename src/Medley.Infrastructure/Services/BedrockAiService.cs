using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using Medley.Application.Configuration;
using Medley.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace Medley.Infrastructure.Services;

/// <summary>
/// AWS Bedrock implementation of AI processing service
/// </summary>
public class BedrockAiService : IAiProcessingService
{
    private readonly AmazonBedrockRuntimeClient _bedrockClient;
    private readonly BedrockSettings _bedrockSettings;
    private readonly ILogger<BedrockAiService> _logger;

    public BedrockAiService(
        AmazonBedrockRuntimeClient bedrockClient,
        IOptions<BedrockSettings> bedrockSettings,
        ILogger<BedrockAiService> logger)
    {
        _bedrockClient = bedrockClient;
        _bedrockSettings = bedrockSettings.Value;
        _logger = logger;
    }

    public async Task<string> ProcessPromptAsync(string prompt, int? maxTokens = null, double? temperature = null)
    {
        try
        {
            var request = new InvokeModelRequest
            {
                ModelId = _bedrockSettings.ModelId,
                Body = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
                {
                    anthropic_version = "bedrock-2023-05-31",
                    max_tokens = maxTokens ?? _bedrockSettings.MaxTokens,
                    temperature = temperature ?? _bedrockSettings.Temperature,
                    messages = new[]
                    {
                        new
                        {
                            role = "user",
                            content = prompt
                        }
                    }
                }))),
                ContentType = "application/json",
                Accept = "application/json"
            };

            var response = await _bedrockClient.InvokeModelAsync(request);
            
            using var reader = new StreamReader(response.Body);
            var responseText = await reader.ReadToEndAsync();
            
            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseText);
            var content = jsonResponse.GetProperty("content")[0].GetProperty("text").GetString();
            
            return content ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process prompt with Bedrock");
            throw;
        }
    }

    public async Task<string> ProcessStructuredPromptAsync(string prompt, int? maxTokens = null, double? temperature = null)
    {
        // For structured prompts, we add JSON formatting instructions
        var structuredPrompt = $@"{prompt}

Please ensure your response is valid JSON and follows the exact format specified above.";
        
        return await ProcessPromptAsync(structuredPrompt, maxTokens, temperature);
    }
}
