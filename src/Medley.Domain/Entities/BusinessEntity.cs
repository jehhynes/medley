using System.ComponentModel.DataAnnotations;

namespace Medley.Domain.Entities;

/// <summary>
/// Base class for business entities that belong to an organization and track creation time
/// </summary>
public abstract class BusinessEntity : BaseEntity
{
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}