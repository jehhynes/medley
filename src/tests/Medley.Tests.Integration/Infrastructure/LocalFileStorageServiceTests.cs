using Medley.Application.Configuration;
using Medley.Application.Enums;
using Medley.Application.Interfaces;
using Medley.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Medley.Tests.Integration.Infrastructure;

public class LocalFileStorageServiceTests
{
    private readonly Mock<IOptions<FileStorageSettings>> _mockOptions;
    private readonly Mock<ILogger<LocalFileStorageService>> _mockLogger;
    private readonly FileStorageSettings _settings;
    private readonly string _tempDirectory;

    public LocalFileStorageServiceTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "MedleyTests", Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDirectory);

        _settings = new FileStorageSettings
        {
            Provider = FileStorageProvider.Local,
            LocalPath = _tempDirectory,
            FolderStructure = new FolderStructureSettings
            {
                Documents = "documents/",
                Uploads = "uploads/",
                Temp = "temp/"
            }
        };

        _mockOptions = new Mock<IOptions<FileStorageSettings>>();
        _mockOptions.Setup(x => x.Value).Returns(_settings);
        _mockLogger = new Mock<ILogger<LocalFileStorageService>>();
    }

    [Fact]
    public async Task UploadAsync_ShouldCreateFileAndReturnKey()
    {
        // Arrange
        var service = new LocalFileStorageService(_mockOptions.Object, _mockLogger.Object);
        var content = "Test file content";
        var fileName = "test.txt";
        var contentType = "text/plain";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));

        // Act
        var result = await service.UploadAsync(stream, fileName, contentType);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains("uploads/", result);
        Assert.Contains("test_", result); // The filename gets sanitized and gets a UUID suffix

        var fullPath = Path.Combine(_tempDirectory, result);
        Assert.True(File.Exists(fullPath));
        Assert.Equal(content, await File.ReadAllTextAsync(fullPath));
    }

    [Fact]
    public async Task DownloadAsync_ShouldReturnFileStream()
    {
        // Arrange
        var service = new LocalFileStorageService(_mockOptions.Object, _mockLogger.Object);
        var content = "Test download content";
        var fileName = "download.txt";
        var contentType = "text/plain";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));

        var fileKey = await service.UploadAsync(stream, fileName, contentType);

        // Act
        var downloadStream = await service.DownloadAsync(fileKey);

        // Assert
        Assert.NotNull(downloadStream);
        using var reader = new StreamReader(downloadStream);
        var downloadedContent = await reader.ReadToEndAsync();
        Assert.Equal(content, downloadedContent);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveFile()
    {
        // Arrange
        var service = new LocalFileStorageService(_mockOptions.Object, _mockLogger.Object);
        var content = "Test delete content";
        var fileName = "delete.txt";
        var contentType = "text/plain";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));

        var fileKey = await service.UploadAsync(stream, fileName, contentType);
        var fullPath = Path.Combine(_tempDirectory, fileKey);
        Assert.True(File.Exists(fullPath));

        // Act
        var result = await service.DeleteAsync(fileKey);

        // Assert
        Assert.True(result);
        Assert.False(File.Exists(fullPath));
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnCorrectStatus()
    {
        // Arrange
        var service = new LocalFileStorageService(_mockOptions.Object, _mockLogger.Object);
        var content = "Test exists content";
        var fileName = "exists.txt";
        var contentType = "text/plain";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));

        var fileKey = await service.UploadAsync(stream, fileName, contentType);

        // Act & Assert
        Assert.True(await service.ExistsAsync(fileKey));
        Assert.False(await service.ExistsAsync("nonexistent.txt"));
    }

    [Fact]
    public async Task GetDownloadUrlAsync_ShouldReturnFileUrl()
    {
        // Arrange
        var service = new LocalFileStorageService(_mockOptions.Object, _mockLogger.Object);
        var content = "Test url content";
        var fileName = "url.txt";
        var contentType = "text/plain";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));

        var fileKey = await service.UploadAsync(stream, fileName, contentType);
        var expiry = TimeSpan.FromMinutes(5);

        // Act
        var url = await service.GetDownloadUrlAsync(fileKey, expiry);

        // Assert
        Assert.NotNull(url);
        Assert.NotEmpty(url);
        Assert.StartsWith("file://", url);
    }

    private void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }
}
