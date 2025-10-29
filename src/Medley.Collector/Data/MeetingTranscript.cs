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
    public required string Source { get; set; }
    
    public DateTime? Date { get; set; }
    
    public string? Participants { get; set; }
    
    [Required]
    public required string Content { get; set; }

    [Required]
    public DateTime DownloadedAt { get; set; } = DateTime.UtcNow;
    
    public bool? IsSelected { get; set; }
    
    public int? LengthInMinutes { get; set; }
    
    public int? TranscriptLength { get; set; }
    
    // Navigation properties
    public ICollection<ApiKey> ApiKeys { get; set; } = new List<ApiKey>();
}
