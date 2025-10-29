using Medley.Collector.Data;

namespace Medley.Collector.Models;

public class MeetingTranscriptViewModel
{
    public int Id { get; set; }
    public bool? IsSelected { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime? Date { get; set; }
    public string? Participants { get; set; }
    public string Source { get; set; } = string.Empty;
    public string? Scope { get; set; }
    public int? LengthInMinutes { get; set; }
    public int? TranscriptLength { get; set; }
    
    public static MeetingTranscriptViewModel FromMeetingTranscript(MeetingTranscript transcript)
    {
        string source;
        
        if (transcript.Source == TranscriptSource.Google)
        {
            // For Google transcripts, use the SourceDetail field (folder path)
            source = !string.IsNullOrWhiteSpace(transcript.SourceDetail) 
                ? transcript.SourceDetail 
                : "Google Meet";
        }
        else
        {
            // For Fellow transcripts, show API key names
            source = string.Join(", ", transcript.ApiKeys.Select(a => a.Name));
        }
        
        return new MeetingTranscriptViewModel
        {
            Id = transcript.Id,
            IsSelected = transcript.IsSelected,
            Title = transcript.Title,
            Date = transcript.Date,
            Participants = transcript.Participants,
            Source = source,
            Scope = transcript.Scope?.ToString(),
            LengthInMinutes = transcript.LengthInMinutes,
            TranscriptLength = transcript.TranscriptLength
        };
    }
}
