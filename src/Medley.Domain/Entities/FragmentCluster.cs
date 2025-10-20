using System.ComponentModel.DataAnnotations;

namespace Medley.Domain.Entities;

/// <summary>
/// Cluster for Fragment entities
/// </summary>
public class FragmentCluster : BusinessEntity
{
    [MaxLength(200)]
    public required string Name { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    public virtual ICollection<Fragment> Fragments { get; set; } = new List<Fragment>();
}