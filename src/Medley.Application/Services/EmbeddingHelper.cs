using Medley.Application.Configuration;
using Medley.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Medley.Application.Services;

/// <summary>
/// Helper service for embedding operations including conditional normalization
/// </summary>
public class EmbeddingHelper : IEmbeddingHelper
{
    private readonly EmbeddingSettings _embeddingSettings;
    private readonly ILogger<EmbeddingHelper> _logger;

    public EmbeddingHelper(
        IOptions<EmbeddingSettings> embeddingSettings,
        ILogger<EmbeddingHelper> logger)
    {
        _embeddingSettings = embeddingSettings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Determines if the current embedding model requires normalization
    /// Currently only qwen3 models require normalization
    /// </summary>
    public bool ShouldNormalizeEmbeddings()
    {
        return _embeddingSettings.Provider.Equals("Ollama", StringComparison.OrdinalIgnoreCase)
               && _embeddingSettings.Ollama.Model.Contains("qwen3", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Conditionally normalizes an embedding vector based on the current model configuration
    /// </summary>
    /// <param name="vector">The embedding vector to potentially normalize</param>
    /// <returns>The normalized or original vector depending on model configuration</returns>
    public float[] ProcessEmbedding(float[] vector)
    {
        if (!ShouldNormalizeEmbeddings())
        {
            return vector;
        }

        var normalized = NormalizeVector(vector);
        
         _logger.LogDebug("Normalized embedding for qwen3 model");

        return normalized;
    }

    /// <summary>
    /// Normalizes a vector to unit length (L2 normalization)
    /// This optimization allows cosine similarity to be computed as a simple dot product
    /// </summary>
    private static float[] NormalizeVector(float[] vector)
    {
        var magnitude = Math.Sqrt(vector.Sum(x => x * x));
        return magnitude == 0 ? vector : vector.Select(x => (float)(x / magnitude)).ToArray();
    }
}

