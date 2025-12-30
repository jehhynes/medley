namespace Medley.Domain.Enums;

/// <summary>
/// Type of article version
/// </summary>
public enum VersionType
{
    /// <summary>
    /// Official user version in main history
    /// </summary>
    User = 0,
    
    /// <summary>
    /// AI-generated draft version (temporary)
    /// </summary>
    AI = 1
}

