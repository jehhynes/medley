namespace Medley.Application.Models.DTOs;

/// <summary>
/// Data transfer object for tool result content
/// </summary>
public class ToolResultContentDto
{
    /// <summary>
    /// The tool call ID this result belongs to
    /// </summary>
    public required string ToolCallId { get; init; }

    /// <summary>
    /// Name of the tool that was called
    /// </summary>
    public string? ToolName { get; init; }

    /// <summary>
    /// The full content of the tool result
    /// </summary>
    public required object Content { get; init; }
}
