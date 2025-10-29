using System.Text.Json.Serialization;

namespace Medley.Collector.Models;

public class FellowMeResponse
{
    [JsonPropertyName("user")]
    public FellowUser? User { get; set; }
    
    [JsonPropertyName("workspace")]
    public FellowWorkspace? Workspace { get; set; }
}

public class FellowUser
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }
    
    [JsonPropertyName("email")]
    public string? Email { get; set; }
    
    [JsonPropertyName("full_name")]
    public string? FullName { get; set; }
}

public class FellowWorkspace
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }
    
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [JsonPropertyName("subdomain")]
    public string? Subdomain { get; set; }
}

public class FellowRecordingsResponse
{
    [JsonPropertyName("recordings")]
    public FellowRecordingsData? Recordings { get; set; }
}

public class FellowRecordingsData
{
    [JsonPropertyName("page_info")]
    public FellowPageInfo? PageInfo { get; set; }
    
    [JsonPropertyName("data")]
    public List<FellowRecording>? Data { get; set; }
}

public class FellowPageInfo
{
    [JsonPropertyName("cursor")]
    public string? Cursor { get; set; }
    
    [JsonPropertyName("page_size")]
    public int PageSize { get; set; }
}

public class FellowRecording
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }
    
    [JsonPropertyName("title")]
    public string? Title { get; set; }
    
    [JsonPropertyName("created_at")]
    public DateTime? CreatedAt { get; set; }
    
    [JsonPropertyName("updated_at")]
    public DateTime? UpdatedAt { get; set; }
    
    [JsonPropertyName("started_at")]
    public DateTime? StartedAt { get; set; }
    
    [JsonPropertyName("ended_at")]
    public DateTime? EndedAt { get; set; }
    
    [JsonPropertyName("event_call_url")]
    public string? EventCallUrl { get; set; }
    
    [JsonPropertyName("event_guid")]
    public string? EventGuid { get; set; }
    
    [JsonPropertyName("note_id")]
    public string? NoteId { get; set; }
    
    [JsonPropertyName("transcript")]
    public FellowTranscript? Transcript { get; set; }
    
    [JsonPropertyName("note")]
    public FellowNote? Note { get; set; }
}

public class FellowTranscript
{
    [JsonPropertyName("speech_segments")]
    public List<FellowSpeechSegment>? SpeechSegments { get; set; }
    
    [JsonPropertyName("language_code")]
    public string? LanguageCode { get; set; }
}

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

public class FellowNotesResponse
{
    [JsonPropertyName("notes")]
    public FellowNotesData? Notes { get; set; }
}

public class FellowNotesData
{
    [JsonPropertyName("page_info")]
    public FellowPageInfo? PageInfo { get; set; }
    
    [JsonPropertyName("data")]
    public List<FellowNote>? Data { get; set; }
}

public class FellowNote
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }
    
    [JsonPropertyName("created_at")]
    public DateTime? CreatedAt { get; set; }
    
    [JsonPropertyName("updated_at")]
    public DateTime? UpdatedAt { get; set; }
    
    [JsonPropertyName("title")]
    public string? Title { get; set; }
    
    [JsonPropertyName("event_guid")]
    public string? EventGuid { get; set; }
    
    [JsonPropertyName("event_start")]
    public DateTime? EventStart { get; set; }
    
    [JsonPropertyName("event_end")]
    public DateTime? EventEnd { get; set; }
    
    [JsonPropertyName("event_is_all_day")]
    public bool? EventIsAllDay { get; set; }
    
    [JsonPropertyName("recording_ids")]
    public List<string>? RecordingIds { get; set; }
    
    [JsonPropertyName("event_attendees")]
    public List<FellowAttendee>? EventAttendees { get; set; }
    
    [JsonPropertyName("content_markdown")]
    public string? ContentMarkdown { get; set; }
}

public class FellowAttendee
{
    [JsonPropertyName("email")]
    public string? Email { get; set; }
}
