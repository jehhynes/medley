namespace Medley.Domain.Models;

/// <summary>
/// Represents a fragment with its similarity score
/// </summary>
public class FragmentSimilarityResult
{
    /// <summary>
    /// The fragment entity
    /// </summary>
    public required Entities.Fragment Fragment { get; init; }

    /// <summary>
    /// Cosine distance (0-2, lower is more similar)
    /// </summary>
    public double Distance { get; init; }

    /// <summary>
    /// Similarity score (0-1, higher is more similar)
    /// Calculated as 1 - (distance / 2)
    /// </summary>
    public double Similarity => 1 - (Distance / 2);
}
