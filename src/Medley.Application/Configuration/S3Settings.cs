namespace Medley.Application.Configuration;

/// <summary>
/// AWS S3 configuration settings
/// </summary>
public class S3Settings
{
    /// <summary>
    /// S3 bucket name for file storage
    /// </summary>
    public string BucketName { get; set; } = string.Empty;

    /// <summary>
    /// Folder structure configuration for S3 organization
    /// </summary>
    public FolderStructureSettings FolderStructure { get; set; } = new();

    /// <summary>
    /// S3 operation timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum retry attempts for S3 operations
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;
}
