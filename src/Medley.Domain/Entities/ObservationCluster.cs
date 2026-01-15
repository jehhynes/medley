using System.ComponentModel.DataAnnotations;

namespace Medley.Domain.Entities;

/// <summary>
/// Cluster for Observation entities 
/// </summary>
public class ObservationCluster : BusinessEntity
{
    [MaxLength(200)]
    public required string Name { get; set; }

    [MaxLength(1000)]
    public required string Description { get; set; }

    public virtual ICollection<Observation> Observations { get; set; } = new List<Observation>();
}


