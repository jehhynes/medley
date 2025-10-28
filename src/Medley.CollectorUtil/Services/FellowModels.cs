using System.Text.Json.Serialization;

namespace Medley.CollectorUtil.Services;

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
