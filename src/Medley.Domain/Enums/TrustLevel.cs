namespace Medley.Domain.Enums;

/// <summary>
/// Represents the trust level of a speaker's identity
/// </summary>
public enum TrustLevel
{
    /// <summary>
    /// High confidence in speaker identity (e.g., verified email, authenticated user)
    /// </summary>
    High = 1,

    /// <summary>
    /// Medium confidence in speaker identity (e.g., consistent name across meetings)
    /// </summary>
    Medium = 2,

    /// <summary>
    /// Low confidence in speaker identity (e.g., generic labels like "Speaker 1")
    /// </summary>
    Low = 3
}
