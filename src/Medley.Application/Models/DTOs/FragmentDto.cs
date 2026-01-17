using Medley.Domain.Enums;

namespace Medley.Application.Models.DTOs;

/// <summary>
/// Data transfer object for Fragment entity
/// </summary>
public class FragmentDto
{
    /// <summary>
    /// Unique identifier for the fragment
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Fragment title
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// Brief summary of the fragment
    /// </summary>
    public required string Summary { get; set; }

    /// <summary>
    /// Category of the fragment (e.g., Decision, Action Item, Feature Request)
    /// </summary>
    public required string Category { get; set; }

    /// <summary>
    /// The text content of the fragment
    /// </summary>
    public required string Content { get; set; }

    /// <summary>
    /// Source ID this fragment was extracted from
    /// </summary>
    public Guid? SourceId { get; set; }

    /// <summary>
    /// Source name
    /// </summary>
    public string? SourceName { get; set; }

    /// <summary>
    /// Source type
    /// </summary>
    public SourceType? SourceType { get; set; }

    /// <summary>
    /// Source date
    /// </summary>
    public DateTimeOffset? SourceDate { get; set; }

    /// <summary>
    /// When the fragment was created
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// When the fragment was last modified
    /// </summary>
    public DateTimeOffset? LastModifiedAt { get; set; }

    /// <summary>
    /// Confidence level from the AI extraction
    /// </summary>
    public ConfidenceLevel? Confidence { get; set; }

    /// <summary>
    /// Explanation of factors affecting the confidence score
    /// </summary>
    public string? ConfidenceComment { get; set; }
}

/// <summary>
/// Fragment search result with similarity score
/// </summary>
public class FragmentSearchResult
{
    /// <summary>
    /// Unique identifier for the fragment
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Fragment title
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// Brief summary of the fragment
    /// </summary>
    public required string Summary { get; set; }

    /// <summary>
    /// Category of the fragment
    /// </summary>
    public required string Category { get; set; }

    /// <summary>
    /// Source ID this fragment was extracted from
    /// </summary>
    public Guid? SourceId { get; set; }

    /// <summary>
    /// Source name
    /// </summary>
    public string? SourceName { get; set; }

    /// <summary>
    /// Source type
    /// </summary>
    public SourceType? SourceType { get; set; }

    /// <summary>
    /// Source date
    /// </summary>
    public DateTimeOffset? SourceDate { get; set; }

    /// <summary>
    /// When the fragment was created
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Confidence level from the AI extraction
    /// </summary>
    public ConfidenceLevel? Confidence { get; set; }

    /// <summary>
    /// Explanation of factors affecting the confidence score
    /// </summary>
    public string? ConfidenceComment { get; set; }

    /// <summary>
    /// Similarity score (0-1, where 1 is most similar)
    /// </summary>
    public double Similarity { get; set; }
}

/// <summary>
/// Request to get titles for multiple fragments
/// </summary>
public class FragmentTitlesRequest
{
    /// <summary>
    /// List of fragment IDs to retrieve titles for
    /// </summary>
    public required List<Guid> Ids { get; set; }
}

/// <summary>
/// Fragment title response
/// </summary>
public class FragmentTitleDto
{
    /// <summary>
    /// Fragment ID
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Fragment title
    /// </summary>
    public required string Title { get; set; }
}
