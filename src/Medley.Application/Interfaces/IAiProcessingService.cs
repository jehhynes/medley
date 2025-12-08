namespace Medley.Application.Interfaces;

/// <summary>
/// AI processing service abstraction for handling AI operations
/// </summary>
public interface IAiProcessingService
{
    /// <summary>
    /// Process a prompt and return the AI response
    /// </summary>
    /// <param name="userPrompt">The user prompt (required)</param>
    /// <param name="systemPrompt">Optional system prompt to set context and behavior</param>
    /// <param name="assistantPrompt">Optional assistant prompt for prefilling the response</param>
    /// <param name="temperature">Temperature setting (0.0 to 1.0)</param>
    /// <returns>AI response text</returns>
    Task<string> ProcessPromptAsync(
        string userPrompt, 
        string? systemPrompt = null, 
        string? assistantPrompt = null,
        double? temperature = null);

    /// <summary>
    /// Process a structured prompt with JSON response
    /// </summary>
    /// <typeparam name="T">The type to deserialize the JSON response into</typeparam>
    /// <param name="userPrompt">The user prompt (required)</param>
    /// <param name="jsonSchema">JSON schema defining the expected response structure</param>
    /// <param name="systemPrompt">Optional system prompt to set context and behavior</param>
    /// <param name="assistantPrompt">Optional assistant prompt for prefilling the response</param>
    /// <param name="temperature">Temperature setting (0.0 to 1.0)</param>
    /// <returns>Deserialized object of type T</returns>
    Task<T> ProcessStructuredPromptAsync<T>(
        string userPrompt, 
        string? systemPrompt = null, 
        string? assistantPrompt = null,
        double? temperature = null);
}
