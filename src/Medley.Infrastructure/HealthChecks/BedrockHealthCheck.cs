using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using Medley.Application.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace Medley.Infrastructure.HealthChecks;

/// <summary>
/// Health check for AWS Bedrock service
/// </summary>
public class BedrockHealthCheck : IHealthCheck
{
    private readonly AmazonBedrockRuntimeClient _bedrockClient;
    private readonly BedrockSettings _bedrockSettings;
    private readonly ILogger<BedrockHealthCheck> _logger;

    public BedrockHealthCheck(AmazonBedrockRuntimeClient bedrockClient, IOptions<BedrockSettings> bedrockSettings, ILogger<BedrockHealthCheck> logger)
    {
        _bedrockClient = bedrockClient;
        _bedrockSettings = bedrockSettings.Value;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Perform a simple test invocation to verify Bedrock connectivity
            var testRequest = new InvokeModelRequest
            {
                ModelId = _bedrockSettings.ModelId,
                Body = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
                {
                    anthropic_version = "bedrock-2023-05-31",
                    max_tokens = 10,
                    temperature = 0.1,
                    messages = new[]
                    {
                        new
                        {
                            role = "user",
                            content = "Hello"
                        }
                    }
                }))),
                ContentType = "application/json",
                Accept = "application/json"
            };

            var response = await _bedrockClient.InvokeModelAsync(testRequest, cancellationToken);
            
            _logger.LogDebug("Bedrock health check passed for model: {ModelId}", _bedrockSettings.ModelId);
            
            return HealthCheckResult.Healthy($"Bedrock model '{_bedrockSettings.ModelId}' is accessible", new Dictionary<string, object>
            {
                ["model_id"] = _bedrockSettings.ModelId,
                ["max_tokens"] = _bedrockSettings.MaxTokens,
                ["temperature"] = _bedrockSettings.Temperature
            });
        }
        catch (AmazonBedrockRuntimeException ex)
        {
            _logger.LogError(ex, "Bedrock health check failed for model: {ModelId}", _bedrockSettings.ModelId);
            return HealthCheckResult.Unhealthy($"Bedrock model '{_bedrockSettings.ModelId}' is not accessible: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bedrock health check failed for model: {ModelId}", _bedrockSettings.ModelId);
            return HealthCheckResult.Unhealthy($"Bedrock health check failed: {ex.Message}", ex);
        }
    }
}
