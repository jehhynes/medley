using System.ComponentModel.DataAnnotations;
using Medley.Domain.Enums;

namespace Medley.Domain.Entities;

/// <summary>
/// Actionable, high-level conclusions for strategic action
/// </summary>
public class Insight : BusinessEntity
{
    [MaxLength(200)]
    public required string Title { get; set; }

    [MaxLength(4000)]
    public required string Content { get; set; }

    public InsightType Type { get; set; }
    
    public InsightPriority Priority { get; set; }
    
    public InsightStatus Status { get; set; } = InsightStatus.Draft;

    // Navigation properties
    public ICollection<Finding> Findings { get; set; } = new List<Finding>();
}


