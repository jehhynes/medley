using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Medley.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Pgvector;

namespace Medley.Domain.Entities;

/// <summary>
/// Raw signals extracted automatically (mentions, sentiments, key phrases)
/// </summary>
public class Observation : BusinessEntity
{
    [MaxLength(8000)]
    public required string Content { get; set; }

    [Column(TypeName = "vector(2000)")]
    public Vector? Embedding { get; set; }

    [MaxLength(100)]
    public required string Category { get; set; }
        
    public required float ConfidenceScore { get; set; }
    
    public required ObservationType Type { get; set; }
    
    public DateTimeOffset? LastModifiedAt { get; set; }

    /// <summary>
    /// The source this observation was extracted from
    /// </summary>
    protected Guid? SourceId { get; set; }

    /// <summary>
    /// Navigation property to the source
    /// </summary>
    [ForeignKey(nameof(SourceId))]
    public Source? Source { get; set; }

    /// <summary>
    /// The cluster this observation belongs to
    /// </summary>
    protected Guid? ObservationClusterId { get; set; }

    /// <summary>
    /// Navigation property to the observation cluster
    /// </summary>
    [ForeignKey(nameof(ObservationClusterId))]
    public ObservationCluster? ObservationCluster { get; set; }

    public ICollection<Finding> Findings { get; set; } = new List<Finding>();
}


