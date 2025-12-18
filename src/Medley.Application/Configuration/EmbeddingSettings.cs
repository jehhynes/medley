namespace Medley.Application.Configuration;

/// <summary>
/// Configuration settings for embeddings
/// </summary>
public class EmbeddingSettings
{
    /// <summary>
    /// The embedding provider to use (e.g., "Ollama", "OpenAI")
    /// </summary>
    public string Provider { get; set; } = "Ollama";

    /// <summary>
    /// The dimension of the embedding vectors
    /// </summary>
    public int Dimensions { get; set; } = 2000;

    /// <summary>
    /// Settings for Ollama provider
    /// </summary>
    public OllamaEmbeddingSettings Ollama { get; set; } = new();

    /// <summary>
    /// Settings for OpenAI provider
    /// </summary>
    public OpenAIEmbeddingSettings OpenAI { get; set; } = new();
}

/// <summary>
/// Configuration settings for Ollama embeddings
/// </summary>
public class OllamaEmbeddingSettings
{
    /// <summary>
    /// Base URL for Ollama API
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost:11434";

    /// <summary>
    /// Model name for embeddings (e.g., "qwen3-embedding:4b")
    /// </summary>
    public string Model { get; set; } = "qwen3-embedding:4b";
}

/// <summary>
/// Configuration settings for OpenAI embeddings
/// </summary>
public class OpenAIEmbeddingSettings
{
    /// <summary>
    /// OpenAI API key
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Model name for embeddings (e.g., "text-embedding-3-large")
    /// </summary>
    public string Model { get; set; } = "text-embedding-3-large";

    /// <summary>
    /// Optional organization ID
    /// </summary>
    public string? OrganizationId { get; set; }
}

