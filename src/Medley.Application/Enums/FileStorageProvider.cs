namespace Medley.Application.Enums;

/// <summary>
/// File storage provider options for the application
/// </summary>
public enum FileStorageProvider
{
    /// <summary>
    /// Local file system storage (for development)
    /// </summary>
    Local = 0,

    /// <summary>
    /// AWS S3 cloud storage (for production)
    /// </summary>
    S3 = 1
}
