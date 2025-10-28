using System.ComponentModel.DataAnnotations;

namespace Medley.CollectorUtil.Data;

public class MeetingTranscript
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    public string MeetingId { get; set; } = string.Empty;
    
    public DateTime? Date { get; set; }
    
    public string? Participants { get; set; }
    
    [Required]
    public string FullJson { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public bool? IsSelected { get; set; }
    
    public int? LengthInMinutes { get; set; }
    
    public int? TranscriptLength { get; set; }
    
    // Navigation properties
    public ICollection<ApiKey> ApiKeys { get; set; } = new List<ApiKey>();
}
