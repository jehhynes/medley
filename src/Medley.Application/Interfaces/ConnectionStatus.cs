namespace Medley.Application.Interfaces;

/// <summary>
/// Represents the connection status of an integration
/// </summary>
public enum ConnectionStatus
{
    /// <summary>
    /// Connection status is unknown
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Integration is connected and working
    /// </summary>
    Connected = 1,

    /// <summary>
    /// Integration is disconnected or not configured
    /// </summary>
    Disconnected = 2,

    /// <summary>
    /// Integration has an error
    /// </summary>
    Error = 3
}
