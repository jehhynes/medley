using Amazon.BedrockRuntime;
using Medley.Application.Configuration;
using Medley.Application.Services;
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
    private readonly AiCallContext _aiCallContext;

    public BedrockHealthCheck(
        AmazonBedrockRuntimeClient bedrockClient, 
        IOptions<BedrockSettings> bedrockSettings, 
        ILogger<BedrockHealthCheck> logger,
        AiCallContext aiCallContext)
    {
        _bedrockClient = bedrockClient;
        _bedrockSettings = bedrockSettings.Value;
        _logger = logger;
        _chatClient = bedrockClient.AsIChatClient(_bedrockSettings.ModelId);
        _aiCallContext = aiCallContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Perform a simple test invocation to verify Bedrock connectivity using IChatClient
            ChatResponse response;
            using (_aiCallContext.SetContext(nameof(BedrockHealthCheck), nameof(CheckHealthAsync), null, null))
            {
                response = await _chatClient.GetResponseAsync("Hello", new ChatOptions
                {
                    MaxOutputTokens = 10,
                    Temperature = 0.1f
                }, cancellationToken);
            }
            
            _logger.LogDebug("Bedrock health check passed for model: {ModelId}", _bedrockSettings.ModelId);
            
            return HealthCheckResult.Healthy($"Bedrock model '{_bedrockSettings.ModelId}' is accessible", new Dictionary<string, object>
            {
                ["model_id"] = _bedrockSettings.ModelId,
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
