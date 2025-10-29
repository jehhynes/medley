namespace Medley.Collector.Models;

/// <summary>
/// Represents folder information for hierarchy building
/// </summary>
public class GoogleDriveFolderInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ParentId { get; set; }
}

/// <summary>
/// Represents a segment of a transcript with timing information
/// </summary>
public class GoogleTranscriptSegment
{
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string Text { get; set; } = string.Empty;
}

/// <summary>
/// Represents a Google Drive video file
/// </summary>
public class GoogleDriveVideo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime? CreatedTime { get; set; }
    public string FolderId { get; set; } = string.Empty;
    public string[] FolderPath { get; set; } = [];
    public string LastModifyingUserDisplayName { get; set; } = string.Empty;
    public string LastModifyingUserEmail { get; set; } = string.Empty;
    public long? DurationMillis { get; set; }
    public List<GoogleTranscriptSegment> Transcript { get; set; } = [];
}
