namespace Medley.Application.Interfaces;

/// <summary>
/// AI processing service abstraction for handling AI operations
/// </summary>
public interface IAiProcessingService
{
    /// <summary>
    /// Process a prompt and return the AI response
    /// </summary>
    /// <param name="prompt">The prompt to send to the AI service</param>
    /// <param name="maxTokens">Maximum tokens for the response</param>
    /// <param name="temperature">Temperature setting (0.0 to 1.0)</param>
    /// <returns>AI response text</returns>
    Task<string> ProcessPromptAsync(string prompt, int? maxTokens = null, double? temperature = null);

    /// <summary>
    /// Process a structured prompt with JSON response
    /// </summary>
    /// <param name="prompt">The prompt to send to the AI service</param>
    /// <param name="maxTokens">Maximum tokens for the response</param>
    /// <param name="temperature">Temperature setting (0.0 to 1.0)</param>
    /// <returns>AI response as JSON string</returns>
    Task<string> ProcessStructuredPromptAsync(string prompt, int? maxTokens = null, double? temperature = null);
}
