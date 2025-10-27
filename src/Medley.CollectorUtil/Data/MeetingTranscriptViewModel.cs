namespace Medley.CollectorUtil.Data;

public class MeetingTranscriptViewModel
{
    public int Id { get; set; }
    public bool IsSelected { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime? Date { get; set; }
    public string? Participants { get; set; }
    public string ApiKeyNames { get; set; } = string.Empty;
    
    public static MeetingTranscriptViewModel FromMeetingTranscript(MeetingTranscript transcript)
    {
        return new MeetingTranscriptViewModel
        {
            Id = transcript.Id,
            IsSelected = transcript.IsSelected,
            Title = transcript.Title,
            Date = transcript.Date,
            Participants = transcript.Participants,
            ApiKeyNames = string.Join(", ", transcript.ApiKeys.Select(a => a.Name))
        };
    }
}
