namespace Medley.Application.Interfaces;

/// <summary>
/// Helper service for embedding operations including conditional normalization
/// </summary>
public interface IEmbeddingHelper
{
    /// <summary>
    /// Determines if the current embedding model requires normalization
    /// </summary>
    bool ShouldNormalizeEmbeddings();

    /// <summary>
    /// Conditionally normalizes an embedding vector based on the current model configuration
    /// </summary>
    /// <param name="vector">The embedding vector to potentially normalize</param>
    /// <returns>The normalized or original vector depending on model configuration</returns>
    float[] ProcessEmbedding(float[] vector);
}

