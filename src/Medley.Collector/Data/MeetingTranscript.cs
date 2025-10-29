using System.ComponentModel.DataAnnotations;

namespace Medley.Collector.Data;

public class MeetingTranscript
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(500)]
    public required string Title { get; set; }

    [Required]
    [MaxLength(200)]
    public required string ExternalId { get; set; }

    [Required]
    [MaxLength(50)]
    public required TranscriptSource Source { get; set; }
    
    [MaxLength(1000)]
    public string? SourceDetail { get; set; }
    
    public DateTime? Date { get; set; }
    
    public string? Participants { get; set; }
    
    [Required]
    public required string Content { get; set; }

    [Required]
    public DateTime DownloadedAt { get; set; } = DateTime.UtcNow;
    
    public bool? IsSelected { get; set; }
    
    public int? LengthInMinutes { get; set; }
    
    public int? TranscriptLength { get; set; }
    
    public MeetingScope? Scope { get; set; }
    
    public bool IsArchived { get; set; } = false;
    
    public DateTime? ExportedAt { get; set; }
    
    // Navigation properties
    public ICollection<ApiKey> ApiKeys { get; set; } = new List<ApiKey>();
}

public enum TranscriptSource
{
    Fellow = 0,
    Google = 1
}

public enum MeetingScope
{
    Internal = 0,
    External = 1
}

