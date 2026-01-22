using Medley.Domain.Enums;

namespace Medley.Application.Models.DTOs;

public class UpdateSpeakerRequest
{
    public bool? IsInternal { get; set; }
    public TrustLevel? TrustLevel { get; set; }
}
