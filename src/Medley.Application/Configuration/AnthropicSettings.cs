namespace Medley.Application.Configuration;

/// <summary>
/// Anthropic API configuration settings
/// </summary>
public class AnthropicSettings
{
    /// <summary>
    /// Anthropic API key
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Claude model ID for AI processing
    /// </summary>
    public string ModelId { get; set; } = "claude-sonnet-4-6";

    /// <summary>
    /// Temperature setting for AI responses (0.0 to 1.0)
    /// </summary>
    public double Temperature { get; set; } = 0.1;

    /// <summary>
    /// API operation timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 300;
}
