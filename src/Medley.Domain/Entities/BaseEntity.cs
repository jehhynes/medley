using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Medley.Domain.Entities;

public abstract class BaseEntity
{
    [Key]
    public Guid Id { get; set; }
}
