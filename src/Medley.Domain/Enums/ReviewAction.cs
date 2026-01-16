namespace Medley.Domain.Enums;

/// <summary>
/// Represents the review action taken on an AI-generated version
/// </summary>
public enum ReviewAction
{
    /// <summary>
    /// Not reviewed yet (default for newly created AI versions)
    /// </summary>
    None = 0,
    
    /// <summary>
    /// AI version was accepted and converted to a User version
    /// </summary>
    Accepted = 1,
    
    /// <summary>
    /// AI version was rejected by the user
    /// </summary>
    Rejected = 2
}
