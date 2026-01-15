using System.Text.Json.Serialization;

namespace Medley.Application.Integrations.Models.Collector;

/// <summary>
/// Represents a Google Drive video file from the Collector export
/// </summary>
public class GoogleDriveVideoImportModel
{
    [JsonPropertyName("Id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("CreatedTime")]
    public DateTime? CreatedTime { get; set; }
    
    [JsonPropertyName("FolderId")]
    public string FolderId { get; set; } = string.Empty;
    
    [JsonPropertyName("FolderPath")]
    public string[] FolderPath { get; set; } = [];
    
    [JsonPropertyName("LastModifyingUserDisplayName")]
    public string LastModifyingUserDisplayName { get; set; } = string.Empty;
    
    [JsonPropertyName("LastModifyingUserEmail")]
    public string LastModifyingUserEmail { get; set; } = string.Empty;
    
    [JsonPropertyName("DurationMillis")]
    public long? DurationMillis { get; set; }
    
    [JsonPropertyName("Transcript")]
    public List<GoogleTranscriptSegmentImportModel> Transcript { get; set; } = [];
}

/// <summary>
/// Represents a segment of a Google Meet transcript with timing information
/// </summary>
public class GoogleTranscriptSegmentImportModel
{
    [JsonPropertyName("StartTime")]
    public TimeSpan StartTime { get; set; }
    
    [JsonPropertyName("EndTime")]
    public TimeSpan EndTime { get; set; }
    
    [JsonPropertyName("Text")]
    public string Text { get; set; } = string.Empty;
}

/// <summary>
/// Represents a Fellow.ai recording from the Collector export (with extended note data)
/// </summary>
public class FellowRecordingImportModel
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }
    
    [JsonPropertyName("title")]
    public required string Title { get; set; }
    
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
    public FellowTranscriptImportModel? Transcript { get; set; }
    
    [JsonPropertyName("note")]
    public FellowNoteImportModel? Note { get; set; }
}

/// <summary>
/// Represents a Fellow.ai transcript
/// </summary>
public class FellowTranscriptImportModel
{
    [JsonPropertyName("speech_segments")]
    public List<FellowSpeechSegmentImportModel>? SpeechSegments { get; set; }
    
    [JsonPropertyName("language_code")]
    public string? LanguageCode { get; set; }
}

/// <summary>
/// Represents a Fellow.ai speech segment
/// </summary>
public class FellowSpeechSegmentImportModel
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
/// Represents a Fellow.ai note (meeting notes and metadata)
/// </summary>
public class FellowNoteImportModel
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
    public List<FellowAttendeeImportModel>? EventAttendees { get; set; }
    
    [JsonPropertyName("content_markdown")]
    public string? ContentMarkdown { get; set; }
}

/// <summary>
/// Represents a Fellow.ai event attendee
/// </summary>
public class FellowAttendeeImportModel
{
    [JsonPropertyName("email")]
    public string? Email { get; set; }
}

