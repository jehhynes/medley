namespace Medley.Application.Models.DTOs.Llm;

/// <summary>
/// Fragment data for tool responses
/// </summary>
public class FragmentData
{
    public required Guid Id { get; set; }
    public required string Title { get; set; }
    public string? Summary { get; set; }
    public string? Category { get; set; }
    public string? Confidence { get; set; }
    public string? ConfidenceComment { get; set; }
    public SourceData? Source { get; set; }
}

/// <summary>
/// Fragment data with content included
/// </summary>
public class FragmentWithContentData : FragmentData
{
    public required string Content { get; set; }
}

/// <summary>
/// Fragment data for system prompts (includes content and instructions)
/// </summary>
public class PlanFragmentData
{
    public required Guid Id { get; set; }
    public required string Title { get; set; }
    public string? Summary { get; set; }
    public string? Category { get; set; }
    public required string Content { get; set; }
    public string? Instructions { get; set; }
    public string? Confidence { get; set; }
    public SourceData? Source { get; set; }
}

/// <summary>
/// Source data for fragments
/// </summary>
public class SourceData
{
    public required DateTimeOffset Date { get; set; }
    public required string SourceType { get; set; }
    public string? Scope { get; set; }
    public string? PrimarySpeaker { get; set; }
    public string? TrustLevel { get; set; }
    public List<TagData> Tags { get; set; } = new();
}

/// <summary>
/// Tag data for sources
/// </summary>
public class TagData
{
    public required string Type { get; set; }
    public required string Value { get; set; }
}
