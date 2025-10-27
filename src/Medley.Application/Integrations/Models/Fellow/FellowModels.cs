using System.Text.Json.Serialization;

namespace Medley.Application.Integrations.Models.Fellow;

/// <summary>
/// Response model for Fellow.ai /me endpoint
/// </summary>
public class FellowMeResponse
{
    [JsonPropertyName("user")]
    public FellowUser? User { get; set; }
    
    [JsonPropertyName("workspace")]
    public FellowWorkspace? Workspace { get; set; }
}

/// <summary>
/// Model for a Fellow.ai user
/// </summary>
public class FellowUser
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
    
    [JsonPropertyName("email")]
    public string? Email { get; set; }
    
    [JsonPropertyName("full_name")]
    public string? FullName { get; set; }
}

/// <summary>
/// Model for a Fellow.ai workspace
/// </summary>
public class FellowWorkspace
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
    
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [JsonPropertyName("subdomain")]
    public string? Subdomain { get; set; }
}

/// <summary>
/// Response model for Fellow.ai recording API (single recording)
/// </summary>
public class FellowRecordingResponse
{
    [JsonPropertyName("recording")]
    public FellowRecording? Recording { get; set; }
}

/// <summary>
/// Response model for Fellow.ai recordings API (multiple recordings)
/// </summary>
public class FellowRecordingsResponse
{
    [JsonPropertyName("recordings")]
    public FellowRecordingsData? Recordings { get; set; }
}

/// <summary>
/// Data container for Fellow.ai recordings with pagination
/// </summary>
public class FellowRecordingsData
{
    [JsonPropertyName("page_info")]
    public FellowPageInfo? PageInfo { get; set; }
    
    [JsonPropertyName("data")]
    public List<FellowRecording>? Data { get; set; }
}

/// <summary>
/// Pagination info for Fellow.ai API
/// </summary>
public class FellowPageInfo
{
    [JsonPropertyName("cursor")]
    public string? Cursor { get; set; }
    
    [JsonPropertyName("page_size")]
    public int PageSize { get; set; }
}

/// <summary>
/// Model for a Fellow.ai recording
/// </summary>
public class FellowRecording
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }
    
    [JsonPropertyName("title")]
    public string? Title { get; set; }
    
    [JsonPropertyName("created_at")]
    public DateTimeOffset? CreatedAt { get; set; }
    
    [JsonPropertyName("updated_at")]
    public DateTimeOffset? UpdatedAt { get; set; }
    
    [JsonPropertyName("started_at")]
    public DateTimeOffset? StartedAt { get; set; }
    
    [JsonPropertyName("ended_at")]
    public DateTimeOffset? EndedAt { get; set; }
    
    [JsonPropertyName("event_call_url")]
    public string? EventCallUrl { get; set; }
    
    [JsonPropertyName("event_guid")]
    public string? EventGuid { get; set; }
    
    [JsonPropertyName("note_id")]
    public string? NoteId { get; set; }
    
    [JsonPropertyName("transcript")]
    public FellowTranscript? Transcript { get; set; }
}

/// <summary>
/// Model for a Fellow.ai transcript
/// </summary>
public class FellowTranscript
{
    [JsonPropertyName("speech_segments")]
    public List<FellowSpeechSegment>? SpeechSegments { get; set; }
    
    [JsonPropertyName("language_code")]
    public string? LanguageCode { get; set; }
}

/// <summary>
/// Model for a Fellow.ai speech segment
/// </summary>
public class FellowSpeechSegment
{
    [JsonPropertyName("start")]
    public decimal Start { get; set; }
    
    [JsonPropertyName("end")]
    public decimal End { get; set; }
    
    [JsonPropertyName("speaker")]
    public string? Speaker { get; set; }
    
    [JsonPropertyName("text")]
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
    public DateTimeOffset? CreatedAtStart { get; set; }
    public DateTimeOffset? CreatedAtEnd { get; set; }
    public DateTimeOffset? UpdatedAtStart { get; set; }
    public DateTimeOffset? UpdatedAtEnd { get; set; }
    public string? ChannelId { get; set; }
    public string? Title { get; set; }
}
