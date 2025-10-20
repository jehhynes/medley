using Medley.Application.Enums;

namespace Medley.Application.Configuration;

/// <summary>
/// Configuration settings for file storage operations
/// </summary>
public class FileStorageSettings
{
    /// <summary>
    /// The storage provider to use (Local or S3)
    /// </summary>
    public FileStorageProvider Provider { get; set; } = FileStorageProvider.Local;

    /// <summary>
    /// Local file system path (used when Provider = Local)
    /// </summary>
    public string LocalPath { get; set; } = string.Empty;

    /// <summary>
    /// Folder structure configuration for organized storage
    /// </summary>
    public FolderStructureSettings FolderStructure { get; set; } = new();
}

/// <summary>
/// Folder structure configuration for file organization
/// </summary>
public class FolderStructureSettings
{
    /// <summary>
    /// Documents folder path
    /// </summary>
    public string Documents { get; set; } = "documents/";

    /// <summary>
    /// Uploads folder path
    /// </summary>
    public string Uploads { get; set; } = "uploads/";

    /// <summary>
    /// Temporary files folder path
    /// </summary>
    public string Temp { get; set; } = "temp/";
}
