using Amazon.S3;
using Amazon.S3.Model;
using Medley.Application.Configuration;
using Medley.Application.Interfaces;
using Medley.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Medley.Tests.Integration.Infrastructure;

public class S3FileStorageServiceTests
{
    private readonly Mock<IAmazonS3> _mockS3Client;
    private readonly Mock<IOptions<S3Settings>> _mockOptions;
    private readonly Mock<ILogger<S3FileStorageService>> _mockLogger;
    private readonly S3Settings _settings;

    public S3FileStorageServiceTests()
    {
        _settings = new S3Settings
        {
            BucketName = "test-bucket",
            FolderStructure = new FolderStructureSettings
            {
                Documents = "documents/",
                Uploads = "uploads/",
                Temp = "temp/"
            },
            TimeoutSeconds = 30,
            MaxRetryAttempts = 3
        };

        _mockS3Client = new Mock<IAmazonS3>();
        _mockOptions = new Mock<IOptions<S3Settings>>();
        _mockOptions.Setup(x => x.Value).Returns(_settings);
        _mockLogger = new Mock<ILogger<S3FileStorageService>>();
    }

    [Fact]
    public async Task UploadAsync_ShouldCallS3PutObject()
    {
        // Arrange
        var service = new S3FileStorageService(_mockS3Client.Object, _mockOptions.Object, _mockLogger.Object);
        var content = "Test S3 content";
        var fileName = "test-s3.txt";
        var contentType = "text/plain";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));

        _mockS3Client.Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), default))
            .ReturnsAsync(new PutObjectResponse());

        // Act
        var result = await service.UploadAsync(stream, fileName, contentType);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains("uploads/", result);
        Assert.Contains("test-s3_", result); // The filename gets sanitized and gets a UUID suffix

        _mockS3Client.Verify(x => x.PutObjectAsync(
            It.Is<PutObjectRequest>(r => 
                r.BucketName == _settings.BucketName &&
                r.ContentType == contentType &&
                r.ServerSideEncryptionMethod == ServerSideEncryptionMethod.AES256),
            default), Times.Once);
    }

    [Fact]
    public async Task DownloadAsync_ShouldCallS3GetObject()
    {
        // Arrange
        var service = new S3FileStorageService(_mockS3Client.Object, _mockOptions.Object, _mockLogger.Object);
        var fileKey = "uploads/2024/01/01/test-file.txt";
        var responseStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("Downloaded content"));

        var mockResponse = new GetObjectResponse
        {
            ResponseStream = responseStream
        };

        _mockS3Client.Setup(x => x.GetObjectAsync(It.IsAny<GetObjectRequest>(), default))
            .ReturnsAsync(mockResponse);

        // Act
        var result = await service.DownloadAsync(fileKey);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(responseStream, result);

        _mockS3Client.Verify(x => x.GetObjectAsync(
            It.Is<GetObjectRequest>(r => 
                r.BucketName == _settings.BucketName &&
                r.Key == fileKey),
            default), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldCallS3DeleteObject()
    {
        // Arrange
        var service = new S3FileStorageService(_mockS3Client.Object, _mockOptions.Object, _mockLogger.Object);
        var fileKey = "uploads/2024/01/01/delete-file.txt";

        _mockS3Client.Setup(x => x.DeleteObjectAsync(It.IsAny<DeleteObjectRequest>(), default))
            .ReturnsAsync(new DeleteObjectResponse());

        // Act
        var result = await service.DeleteAsync(fileKey);

        // Assert
        Assert.True(result);

        _mockS3Client.Verify(x => x.DeleteObjectAsync(
            It.Is<DeleteObjectRequest>(r => 
                r.BucketName == _settings.BucketName &&
                r.Key == fileKey),
            default), Times.Once);
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnTrueWhenFileExists()
    {
        // Arrange
        var service = new S3FileStorageService(_mockS3Client.Object, _mockOptions.Object, _mockLogger.Object);
        var fileKey = "uploads/2024/01/01/exists-file.txt";

        _mockS3Client.Setup(x => x.GetObjectMetadataAsync(It.IsAny<GetObjectMetadataRequest>(), default))
            .ReturnsAsync(new GetObjectMetadataResponse());

        // Act
        var result = await service.ExistsAsync(fileKey);

        // Assert
        Assert.True(result);

        _mockS3Client.Verify(x => x.GetObjectMetadataAsync(
            It.Is<GetObjectMetadataRequest>(r => 
                r.BucketName == _settings.BucketName &&
                r.Key == fileKey),
            default), Times.Once);
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnFalseWhenFileNotFound()
    {
        // Arrange
        var service = new S3FileStorageService(_mockS3Client.Object, _mockOptions.Object, _mockLogger.Object);
        var fileKey = "uploads/2024/01/01/nonexistent-file.txt";

        _mockS3Client.Setup(x => x.GetObjectMetadataAsync(It.IsAny<GetObjectMetadataRequest>(), default))
            .ThrowsAsync(new AmazonS3Exception("Not Found") { StatusCode = System.Net.HttpStatusCode.NotFound });

        // Act
        var result = await service.ExistsAsync(fileKey);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetDownloadUrlAsync_ShouldCallS3GetPreSignedURL()
    {
        // Arrange
        var service = new S3FileStorageService(_mockS3Client.Object, _mockOptions.Object, _mockLogger.Object);
        var fileKey = "uploads/2024/01/01/url-file.txt";
        var expiry = TimeSpan.FromMinutes(5);
        var expectedUrl = "https://test-bucket.s3.amazonaws.com/uploads/2024/01/01/url-file.txt?signature=abc123";

        _mockS3Client.Setup(x => x.GetPreSignedURLAsync(It.IsAny<GetPreSignedUrlRequest>()))
            .ReturnsAsync(expectedUrl);

        // Act
        var result = await service.GetDownloadUrlAsync(fileKey, expiry);

        // Assert
        Assert.Equal(expectedUrl, result);

        _mockS3Client.Verify(x => x.GetPreSignedURLAsync(
            It.Is<GetPreSignedUrlRequest>(r => 
                r.BucketName == _settings.BucketName &&
                r.Key == fileKey &&
                r.Verb == HttpVerb.GET)),
            Times.Once);
    }
}
