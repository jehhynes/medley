using System.ComponentModel.DataAnnotations;

namespace Medley.Domain.Entities;

/// <summary>
/// Represents a tenant organization for multi-tenant data isolation
/// </summary>
public class Organization : BaseEntity
{
    [MaxLength(200)]
    public required string Name { get; set; }
}


