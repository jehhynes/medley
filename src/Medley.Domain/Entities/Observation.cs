using System.ComponentModel.DataAnnotations;
using Medley.Domain.Enums;

namespace Medley.Domain.Entities;

/// <summary>
/// Raw signals extracted automatically (mentions, sentiments, key phrases)
/// </summary>
public class Observation : BusinessEntity
{
    [MaxLength(8000)]
    public required string Content { get; set; }

    [DataType("vector(1536)")]
    public float[]? Embedding { get; set; }

    [MaxLength(100)]
    public string? Category { get; set; }
    
    [MaxLength(1000)]
    public string? SourceContext { get; set; }
    
    public float ConfidenceScore { get; set; }
    
    public ObservationType Type { get; set; }
    
    public DateTimeOffset? LastModifiedAt { get; set; }

    // Navigation properties
    public Source? Source { get; set; }
    public ICollection<Finding> Findings { get; set; } = new List<Finding>();
}


