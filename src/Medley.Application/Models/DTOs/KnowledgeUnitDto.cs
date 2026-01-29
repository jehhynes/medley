using Medley.Domain.Enums;

namespace Medley.Application.Models.DTOs;

/// <summary>
/// Data transfer object for KnowledgeUnit entity
/// </summary>
public class KnowledgeUnitDto
{
    /// <summary>
    /// Unique identifier for the knowledge unit
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Title of the knowledge unit
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// Brief summary of the knowledge unit
    /// </summary>
    public required string Summary { get; set; }

    /// <summary>
    /// The synthesized content of the knowledge unit
    /// </summary>
    public required string Content { get; set; }

    /// <summary>
    /// Category of the knowledge unit
    /// </summary>
    public required string Category { get; set; }

    /// <summary>
    /// Icon for the knowledge unit category
    /// </summary>
    public string? CategoryIcon { get; set; }

    /// <summary>
    /// Confidence level of the knowledge unit
    /// </summary>
    public required ConfidenceLevel Confidence { get; set; }

    /// <summary>
    /// Explanation of factors affecting the confidence score
    /// </summary>
    public string? ConfidenceComment { get; set; }

    /// <summary>
    /// Message or reasoning from the clustering process
    /// </summary>
    public string? ClusteringComment { get; set; }

    /// <summary>
    /// Number of fragments that belong to this knowledge unit
    /// </summary>
    public required int FragmentCount { get; set; }

    /// <summary>
    /// When the knowledge unit was created
    /// </summary>
    public required DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// When the knowledge unit was last updated
    /// </summary>
    public required DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>
/// Knowledge unit search result with similarity score
/// </summary>
public class KnowledgeUnitSearchResult
{
    /// <summary>
    /// Unique identifier for the knowledge unit
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Title of the knowledge unit
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// Brief summary of the knowledge unit
    /// </summary>
    public required string Summary { get; set; }

    /// <summary>
    /// Category of the knowledge unit
    /// </summary>
    public required string Category { get; set; }

    /// <summary>
    /// Icon for the knowledge unit category
    /// </summary>
    public string? CategoryIcon { get; set; }

    /// <summary>
    /// Confidence level of the knowledge unit
    /// </summary>
    public required ConfidenceLevel Confidence { get; set; }

    /// <summary>
    /// Explanation of factors affecting the confidence score
    /// </summary>
    public string? ConfidenceComment { get; set; }

    /// <summary>
    /// Number of fragments that belong to this knowledge unit
    /// </summary>
    public required int FragmentCount { get; set; }

    /// <summary>
    /// When the knowledge unit was last updated
    /// </summary>
    public required DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// Similarity score (0-1, where 1 is most similar)
    /// </summary>
    public required double Similarity { get; set; }
}

/// <summary>
/// Request to update knowledge unit confidence level
/// </summary>
public class UpdateKnowledgeUnitConfidenceRequest
{
    /// <summary>
    /// New confidence level
    /// </summary>
    public required ConfidenceLevel Confidence { get; set; }

    /// <summary>
    /// Optional comment explaining the confidence level
    /// </summary>
    public string? ConfidenceComment { get; set; }
}

/// <summary>
/// Response for knowledge unit delete operation
/// </summary>
public class DeleteKnowledgeUnitResponse
{
    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public required bool Success { get; set; }

    /// <summary>
    /// Message describing the result
    /// </summary>
    public string? Message { get; set; }
}
