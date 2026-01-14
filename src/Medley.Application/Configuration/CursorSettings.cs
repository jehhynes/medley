namespace Medley.Application.Configuration;

/// <summary>
/// Configuration settings for Cursor CLI integration
/// </summary>
public class CursorSettings
{
    /// <summary>
    /// Whether Cursor integration is enabled
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// SSH host for the Linux server running Cursor CLI
    /// </summary>
    public string SshHost { get; set; } = string.Empty;

    /// <summary>
    /// SSH username for authentication
    /// </summary>
    public string SshUser { get; set; } = string.Empty;

    /// <summary>
    /// Path to SSH private key file for authentication
    /// </summary>
    public string SshKeyPath { get; set; } = string.Empty;

    /// <summary>
    /// SSH port (default: 22)
    /// </summary>
    public int SshPort { get; set; } = 22;

    /// <summary>
    /// Cursor workspace directory (entrypoint for Cursor CLI)
    /// </summary>
    public string WorkspaceDirectory { get; set; } = "/home/medley/workspace";

    /// <summary>
    /// Full path to the Cursor agent command (default: "agent" to use PATH)
    /// Example: /home/cursor/.local/bin/agent
    /// </summary>
    public string AgentPath { get; set; } = "agent";

    /// <summary>
    /// Timeout in seconds for Cursor operations (default: 300 = 5 minutes)
    /// </summary>
    public int TimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// Model to use for Cursor Agent operations (default: claude-4.5-sonnet-thinking)
    /// </summary>
    public string Model { get; set; } = "claude-4.5-sonnet-thinking";
}
