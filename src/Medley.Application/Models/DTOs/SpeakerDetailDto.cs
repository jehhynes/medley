using Medley.Domain.Enums;

namespace Medley.Application.Models.DTOs;

public class SpeakerDetailDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Email { get; set; }
    public bool? IsInternal { get; set; }
    public TrustLevel? TrustLevel { get; set; }
    public int SourceCount { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
}
