namespace Medley.Application.Integrations.Models.Fellow;

/// <summary>
/// Response model for Fellow.ai /me endpoint
/// </summary>
public class FellowMeResponse
{
    public FellowUser? User { get; set; }
    public FellowWorkspace? Workspace { get; set; }
}

/// <summary>
/// Model for a Fellow.ai user
/// </summary>
public class FellowUser
{
    public string Id { get; set; }
    public string? Email { get; set; }
    public string? FullName { get; set; }
}

/// <summary>
/// Model for a Fellow.ai workspace
/// </summary>
public class FellowWorkspace
{
    public string Id { get; set; }
    public string? Name { get; set; }
    public string? Subdomain { get; set; }
}

/// <summary>
/// Response model for Fellow.ai recording API (single recording)
/// </summary>
public class FellowRecordingResponse
{
    public FellowRecording? Recording { get; set; }
}

/// <summary>
/// Response model for Fellow.ai recordings API (multiple recordings)
/// </summary>
public class FellowRecordingsResponse
{
    public FellowRecordingsData? Recordings { get; set; }
}

/// <summary>
/// Data container for Fellow.ai recordings with pagination
/// </summary>
public class FellowRecordingsData
{
    public FellowPageInfo? PageInfo { get; set; }
    public List<FellowRecording>? Data { get; set; }
}

/// <summary>
/// Pagination info for Fellow.ai API
/// </summary>
public class FellowPageInfo
{
    public string? Cursor { get; set; }
    public int PageSize { get; set; }
}

/// <summary>
/// Model for a Fellow.ai recording
/// </summary>
public class FellowRecording
{
    public string? Id { get; set; }
    public string? Title { get; set; }
    public string? CreatedAt { get; set; }
    public string? UpdatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public string? EndedAt { get; set; }
    public string? EventCallUrl { get; set; }
    public string? EventGuid { get; set; }
    public string? NoteId { get; set; }
    public FellowTranscript? Transcript { get; set; }
}

/// <summary>
/// Model for a Fellow.ai transcript
/// </summary>
public class FellowTranscript
{
    public List<FellowSpeechSegment>? SpeechSegments { get; set; }
    public string? LanguageCode { get; set; }
}

/// <summary>
/// Model for a Fellow.ai speech segment
/// </summary>
public class FellowSpeechSegment
{
    public int Start { get; set; }
    public int End { get; set; }
    public string? Speaker { get; set; }
    public string? Text { get; set; }
}

/// <summary>
/// Request options for fetching recordings
/// </summary>
public class FellowRecordingsRequestOptions
{
    public FellowPaginationOptions? Pagination { get; set; }
    public FellowIncludeOptions? Include { get; set; }
    public FellowRecordingFilters? Filters { get; set; }
}

/// <summary>
/// Pagination options for recordings request
/// </summary>
public class FellowPaginationOptions
{
    public string? Cursor { get; set; }
    public int? PageSize { get; set; }
}

/// <summary>
/// Include options for recordings request
/// </summary>
public class FellowIncludeOptions
{
    public bool? Transcript { get; set; }
}

/// <summary>
/// Filter options for recordings request
/// </summary>
public class FellowRecordingFilters
{
    public string? EventGuid { get; set; }
    public string? CreatedAtStart { get; set; }
    public string? CreatedAtEnd { get; set; }
    public string? UpdatedAtStart { get; set; }
    public string? UpdatedAtEnd { get; set; }
    public string? ChannelId { get; set; }
    public string? Title { get; set; }
}
