namespace Medley.Application.Interfaces;

/// <summary>
/// File storage service abstraction for handling file operations
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Upload a file to storage
    /// </summary>
    /// <param name="content">File content stream</param>
    /// <param name="fileName">Name of the file</param>
    /// <param name="contentType">MIME type of the file</param>
    /// <returns>File key/path for future reference</returns>
    Task<string> UploadAsync(Stream content, string fileName, string contentType);

    /// <summary>
    /// Download a file from storage
    /// </summary>
    /// <param name="fileKey">File key/path returned from upload</param>
    /// <returns>File content stream</returns>
    Task<Stream> DownloadAsync(string fileKey);

    /// <summary>
    /// Delete a file from storage
    /// </summary>
    /// <param name="fileKey">File key/path to delete</param>
    /// <returns>True if deletion was successful</returns>
    Task<bool> DeleteAsync(string fileKey);

    /// <summary>
    /// Check if a file exists in storage
    /// </summary>
    /// <param name="fileKey">File key/path to check</param>
    /// <returns>True if file exists</returns>
    Task<bool> ExistsAsync(string fileKey);

    /// <summary>
    /// Get a download URL for a file (pre-signed for S3, temporary for local)
    /// </summary>
    /// <param name="fileKey">File key/path</param>
    /// <param name="expiry">URL expiry time</param>
    /// <returns>Download URL</returns>
    Task<string> GetDownloadUrlAsync(string fileKey, TimeSpan expiry);
}
