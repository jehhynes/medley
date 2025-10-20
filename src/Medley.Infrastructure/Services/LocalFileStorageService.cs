using Medley.Application.Configuration;
using Medley.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Medley.Infrastructure.Services;

/// <summary>
/// Local file system implementation of file storage service
/// </summary>
public class LocalFileStorageService : IFileStorageService
{
    private readonly FileStorageSettings _settings;
    private readonly ILogger<LocalFileStorageService> _logger;
    private readonly string _basePath;

    public LocalFileStorageService(IOptions<FileStorageSettings> settings, ILogger<LocalFileStorageService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        _basePath = Path.GetFullPath(_settings.LocalPath);
        
        // Ensure base directory exists
        Directory.CreateDirectory(_basePath);
        
        // Ensure folder structure exists
        CreateFolderStructure();
    }

    public async Task<string> UploadAsync(Stream content, string fileName, string contentType)
    {
        try
        {
            var fileKey = GenerateFileKey(fileName);
            var fullPath = Path.Combine(_basePath, fileKey);
            
            // Ensure directory exists
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
            await content.CopyToAsync(fileStream);
            
            _logger.LogInformation("File uploaded successfully: {FileKey}", fileKey);
            return fileKey;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file: {FileName}", fileName);
            throw;
        }
    }

    public async Task<Stream> DownloadAsync(string fileKey)
    {
        try
        {
            var fullPath = Path.Combine(_basePath, fileKey);
            
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"File not found: {fileKey}");
            }

            var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
            _logger.LogInformation("File downloaded successfully: {FileKey}", fileKey);
            return fileStream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download file: {FileKey}", fileKey);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(string fileKey)
    {
        try
        {
            var fullPath = Path.Combine(_basePath, fileKey);
            
            if (!File.Exists(fullPath))
            {
                _logger.LogWarning("File not found for deletion: {FileKey}", fileKey);
                return false;
            }

            await Task.Run(() => File.Delete(fullPath));
            _logger.LogInformation("File deleted successfully: {FileKey}", fileKey);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file: {FileKey}", fileKey);
            return false;
        }
    }

    public async Task<bool> ExistsAsync(string fileKey)
    {
        try
        {
            var fullPath = Path.Combine(_basePath, fileKey);
            return await Task.FromResult(File.Exists(fullPath));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check file existence: {FileKey}", fileKey);
            return false;
        }
    }

    public async Task<string> GetDownloadUrlAsync(string fileKey, TimeSpan expiry)
    {
        try
        {
            var fullPath = Path.Combine(_basePath, fileKey);
            
            if (!await ExistsAsync(fileKey))
            {
                throw new FileNotFoundException($"File not found: {fileKey}");
            }

            // For local storage, we'll return a temporary file URL
            // In a real implementation, you might want to create a temporary copy
            // or use a different approach for secure access
            var tempUrl = $"file://{fullPath}";
            _logger.LogInformation("Generated download URL for file: {FileKey}", fileKey);
            return await Task.FromResult(tempUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate download URL for file: {FileKey}", fileKey);
            throw;
        }
    }

    private string GenerateFileKey(string fileName)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyy/MM/dd");
        var guid = Guid.NewGuid().ToString("N")[..8];
        var extension = Path.GetExtension(fileName);
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        
        // Sanitize filename
        var sanitizedName = string.Join("_", nameWithoutExtension.Split(Path.GetInvalidFileNameChars()));
        
        return Path.Combine(_settings.FolderStructure.Uploads, timestamp, $"{sanitizedName}_{guid}{extension}")
            .Replace('\\', '/');
    }

    private void CreateFolderStructure()
    {
        var folders = new[]
        {
            _settings.FolderStructure.Documents,
            _settings.FolderStructure.Uploads,
            _settings.FolderStructure.Temp
        };

        foreach (var folder in folders)
        {
            var folderPath = Path.Combine(_basePath, folder.TrimEnd('/'));
            Directory.CreateDirectory(folderPath);
        }
    }
}
