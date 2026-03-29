namespace Medley.Application.Configuration;

/// <summary>
/// OpenAI API configuration settings
/// </summary>
public class OpenAiSettings
{
    /// <summary>
    /// OpenAI API key
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Model ID for AI processing (e.g. gpt-4o, gpt-4o-mini)
    /// </summary>
    public string ModelId { get; set; } = "gpt-5.4-mini";

    /// <summary>
    /// Temperature setting for AI responses (0.0 to 1.0)
    /// </summary>
    public double Temperature { get; set; } = 0.1;

    /// <summary>
    /// API operation timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 300;
}
