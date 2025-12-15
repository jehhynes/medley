using System.ComponentModel;

namespace Medley.Application.Models;

/// <summary>
/// DTO for AI response containing segment-based chunks.
/// Chunks are defined by their starting segment index only - each chunk implicitly 
/// ends at the next chunk's start index.
/// </summary>
public class SegmentChunkingResponse
{
    [Description("Any comments about the chunking strategy or content")]
    public string? Message { get; set; }

    public List<SegmentChunk> Chunks { get; set; } = new();
}

/// <summary>
/// DTO for a chunk defined by a starting segment index
/// </summary>
public class SegmentChunk
{
    [Description("The index of the first speech segment in this chunk")]
    public int StartIndex { get; set; }

    [Description("Brief summary of the topics covered in this chunk")]
    public string? Summary { get; set; }
}

/// <summary>
/// Internal representation of a speech segment
/// </summary>
public class SpeechSegment
{
    public int Index { get; set; }
    public string? Speaker { get; set; }
    public string Text { get; set; } = string.Empty;
    public int CharacterCount { get; set; }
}

/// <summary>
/// Internal representation of a content chunk ready for processing
/// </summary>
public class ContentChunk
{
    public int Index { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? Summary { get; set; }
}

