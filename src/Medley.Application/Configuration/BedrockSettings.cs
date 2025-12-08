namespace Medley.Application.Configuration;

/// <summary>
/// AWS Bedrock configuration settings
/// </summary>
public class BedrockSettings
{
    /// <summary>
    /// Claude model ID for AI processing
    /// </summary>
    public string ModelId { get; set; } = "us.anthropic.claude-sonnet-4-5-20250929-v1:0";

    /// <summary>
    /// Temperature setting for AI responses (0.0 to 1.0)
    /// </summary>
    public double Temperature { get; set; } = 0.1;

    /// <summary>
    /// Bedrock operation timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 300;
}
