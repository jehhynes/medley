using Medley.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Medley.Domain.Entities;

public class Finding : BusinessEntity
{
    public required string Content { get; set; }
    public required FindingType Type { get; set; }
    public required float ConfidenceScore { get; set; }
    
    // Navigation properties
    public virtual ICollection<Observation> Observations { get; set; } = new List<Observation>();
    public virtual ICollection<Insight> Insights { get; set; } = new List<Insight>();
}