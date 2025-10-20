namespace Medley.Domain.Models;

/// <summary>
/// Represents a related entity with its similarity score
/// </summary>
public class BaseSimilarityResult<T>
{
	/// <summary>
	/// The related entity
	/// </summary>
	public required T RelatedEntity { get; init; }

	/// <summary>
	/// Cosine distance (0-2, lower is more similar)
	/// </summary>
	public required double Distance { get; init; }

	/// <summary>
	/// Similarity score (0-1, higher is more similar)
	/// Calculated as 1 - (distance / 2)
	/// </summary>
	public double Similarity => 1 - (Distance / 2);
}
