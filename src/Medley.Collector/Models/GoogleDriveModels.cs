namespace Medley.Collector.Models;

/// <summary>
/// Represents folder information for hierarchy building
/// </summary>
public class FolderInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ParentId { get; set; }
}

/// <summary>
/// Represents a Google Drive video file
/// </summary>
public class DriveVideo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime? CreatedTime { get; set; }
    public string ParentFolderId { get; set; } = string.Empty;
    public string[] FolderPath { get; set; } = [];
    public string LastModifyingUserName { get; set; } = string.Empty;
    public string LastModifyingUserEmail { get; set; } = string.Empty;
    public long? DurationMillis { get; set; }
}
