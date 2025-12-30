namespace Medley.Application.Configuration;

/// <summary>
/// Semantic Kernel configuration settings for chat functionality
/// </summary>
public class SemanticKernelSettings
{
    /// <summary>
    /// Maximum tokens for AI responses
    /// </summary>
    public int MaxTokens { get; set; } = 4096;

    /// <summary>
    /// Temperature setting for AI responses (0.0 to 1.0)
    /// </summary>
    public double Temperature { get; set; } = 0.1;

    /// <summary>
    /// Maximum number of history messages to include in context
    /// </summary>
    public int MaxHistoryMessages { get; set; } = 20;
}

