using Amazon.BedrockRuntime;
using Medley.Application.Configuration;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Medley.Infrastructure.HealthChecks;

/// <summary>
/// Health check for AWS Bedrock service
/// </summary>
public class BedrockHealthCheck : IHealthCheck
{
    private readonly AmazonBedrockRuntimeClient _bedrockClient;
    private readonly BedrockSettings _bedrockSettings;
    private readonly ILogger<BedrockHealthCheck> _logger;
    private readonly IChatClient _chatClient;

    public BedrockHealthCheck(AmazonBedrockRuntimeClient bedrockClient, IOptions<BedrockSettings> bedrockSettings, ILogger<BedrockHealthCheck> logger)
    {
        _bedrockClient = bedrockClient;
        _bedrockSettings = bedrockSettings.Value;
        _logger = logger;
        _chatClient = bedrockClient.AsIChatClient(_bedrockSettings.ModelId);
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Perform a simple test invocation to verify Bedrock connectivity using IChatClient
            var response = await _chatClient.GetResponseAsync("Hello", new ChatOptions
            {
                MaxOutputTokens = 10,
                Temperature = 0.1f
            }, cancellationToken);
            
            _logger.LogDebug("Bedrock health check passed for model: {ModelId}", _bedrockSettings.ModelId);
            
            return HealthCheckResult.Healthy($"Bedrock model '{_bedrockSettings.ModelId}' is accessible", new Dictionary<string, object>
            {
                ["model_id"] = _bedrockSettings.ModelId,
                ["max_tokens"] = _bedrockSettings.MaxTokens,
                ["temperature"] = _bedrockSettings.Temperature
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bedrock health check failed for model: {ModelId}", _bedrockSettings.ModelId);
            return HealthCheckResult.Unhealthy($"Bedrock health check failed: {ex.Message}", ex);
        }
    }
}
