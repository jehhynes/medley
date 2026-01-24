using Medley.Domain.Enums;

namespace Medley.Application.Models.DTOs;

/// <summary>
/// Data transfer object for Source entity
/// </summary>
public class SourceDto
{
    /// <summary>
    /// Unique identifier for the source
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Source type
    /// </summary>
    public required SourceType Type { get; set; }

    /// <summary>
    /// Type of metadata stored in MetadataJson
    /// </summary>
    public required SourceMetadataType MetadataType { get; set; }

    /// <summary>
    /// Source name
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// External ID from the integration
    /// </summary>
    public string? ExternalId { get; set; }

    /// <summary>
    /// Text content of the source (e.g., transcript data)
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// Speech segments for Fellow sources (replaces Content when available)
    /// </summary>
    public List<SpeechSegmentDto>? SpeechSegments { get; set; }

    /// <summary>
    /// Speakers in this source
    /// </summary>
    public List<SpeakerDto>? Speakers { get; set; }

    /// <summary>
    /// JSON metadata from the external source
    /// </summary>
    public required string MetadataJson { get; set; }

    /// <summary>
    /// Date associated with the source (e.g., meeting date)
    /// </summary>
    public required DateTimeOffset Date { get; set; }

    /// <summary>
    /// Indicates whether this source is an internal meeting/document
    /// </summary>
    public bool? IsInternal { get; set; }

    /// <summary>
    /// Timestamp when smart tags were generated for this source
    /// </summary>
    public DateTimeOffset? TagsGenerated { get; set; }

    /// <summary>
    /// Primary speaker name for this source
    /// </summary>
    public string? PrimarySpeakerName { get; set; }

    /// <summary>
    /// Primary speaker trust level
    /// </summary>
    public TrustLevel? PrimarySpeakerTrustLevel { get; set; }

    /// <summary>
    /// Status of fragment extraction for this source
    /// </summary>
    public required ExtractionStatus ExtractionStatus { get; set; }

    /// <summary>
    /// Message from the fragment extraction process
    /// </summary>
    public string? ExtractionMessage { get; set; }

    /// <summary>
    /// Integration name this source was imported from
    /// </summary>
    public required string IntegrationName { get; set; }

    /// <summary>
    /// Number of fragments extracted from this source
    /// </summary>
    public required int FragmentsCount { get; set; }

    /// <summary>
    /// When the source was created
    /// </summary>
    public required DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Tags associated with this source
    /// </summary>
    public required List<SourceTagDto> Tags { get; set; } = new();
}

/// <summary>
/// Summary information about a source (for nested references)
/// </summary>
public class SourceSummaryDto
{
    /// <summary>
    /// Unique identifier for the source
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Source name
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Source type
    /// </summary>
    public required SourceType Type { get; set; }

    /// <summary>
    /// Date associated with the source
    /// </summary>
    public required DateTimeOffset Date { get; set; }

    /// <summary>
    /// Integration name
    /// </summary>
    public required string IntegrationName { get; set; }

    /// <summary>
    /// Number of fragments
    /// </summary>
    public required int FragmentsCount { get; set; }

    /// <summary>
    /// Extraction status
    /// </summary>
    public required ExtractionStatus ExtractionStatus { get; set; }

    /// <summary>
    /// Primary speaker name for this source
    /// </summary>
    public string? PrimarySpeakerName { get; set; }

    /// <summary>
    /// Primary speaker trust level
    /// </summary>
    public TrustLevel? PrimarySpeakerTrustLevel { get; set; }
}

/// <summary>
/// Tag information for a source
/// </summary>
public class SourceTagDto
{
    /// <summary>
    /// Tag type ID
    /// </summary>
    public required Guid TagTypeId { get; set; }

    /// <summary>
    /// Tag type name
    /// </summary>
    public required string TagType { get; set; }

    /// <summary>
    /// Tag value
    /// </summary>
    public required string Value { get; set; }

    /// <summary>
    /// Allowed value from tag options (if applicable)
    /// </summary>
    public string? AllowedValue { get; set; }
}

/// <summary>
/// Response after extracting fragments from a source
/// </summary>
public class FragmentExtractionResponse
{
    /// <summary>
    /// Indicates if the extraction was successful
    /// </summary>
    public required bool Success { get; set; }

    /// <summary>
    /// Background job ID for tracking
    /// </summary>
    public string? JobId { get; set; }

    /// <summary>
    /// Status message
    /// </summary>
    public required string Message { get; set; }
}

/// <summary>
/// Response after tagging a source
/// </summary>
public class TaggingResponse
{
    /// <summary>
    /// Indicates if the tagging was successful
    /// </summary>
    public required bool Success { get; set; }

    /// <summary>
    /// Indicates if the source was skipped
    /// </summary>
    public required bool Skipped { get; set; }

    /// <summary>
    /// Status message
    /// </summary>
    public required string Message { get; set; }

    /// <summary>
    /// Indicates if this is an internal source
    /// </summary>
    public required bool? IsInternal { get; set; }

    /// <summary>
    /// Number of tags generated
    /// </summary>
    public required int TagCount { get; set; }
}

/// <summary>
/// Speech segment from a Fellow source
/// </summary>
public class SpeechSegmentDto
{
    /// <summary>
    /// Speaker ID (null for unknown speakers)
    /// </summary>
    public Guid? SpeakerId { get; set; }

    /// <summary>
    /// Speaker name
    /// </summary>
    public required string SpeakerName { get; set; }

    /// <summary>
    /// Merged text content from adjacent segments by the same speaker
    /// </summary>
    public required string Text { get; set; }
}

/// <summary>
/// Speaker information
/// </summary>
public class SpeakerDto
{
    /// <summary>
    /// Speaker ID
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Speaker name
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Speaker email
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Whether the speaker is internal
    /// </summary>
    public bool? IsInternal { get; set; }

    /// <summary>
    /// Trust level
    /// </summary>
    public string? TrustLevel { get; set; }
}

/// <summary>
/// Response for source delete operation
/// </summary>
public class DeleteSourceResponse
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