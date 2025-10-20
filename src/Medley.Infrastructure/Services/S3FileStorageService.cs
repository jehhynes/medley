using Amazon.S3;
using Amazon.S3.Model;
using Medley.Application.Configuration;
using Medley.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Medley.Infrastructure.Services;

/// <summary>
/// AWS S3 implementation of file storage service
/// </summary>
public class S3FileStorageService : IFileStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly S3Settings _s3Settings;
    private readonly ILogger<S3FileStorageService> _logger;

    public S3FileStorageService(
        IAmazonS3 s3Client,
        IOptions<S3Settings> s3Settings,
        ILogger<S3FileStorageService> logger)
    {
        _s3Client = s3Client;
        _s3Settings = s3Settings.Value;
        _logger = logger;
    }

    public async Task<string> UploadAsync(Stream content, string fileName, string contentType)
    {
        try
        {
            var fileKey = GenerateFileKey(fileName);
            
            var request = new PutObjectRequest
            {
                BucketName = _s3Settings.BucketName,
                Key = fileKey,
                InputStream = content,
                ContentType = contentType,
                ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256
            };

            var response = await _s3Client.PutObjectAsync(request);
            
            _logger.LogInformation("File uploaded successfully to S3: {FileKey}", fileKey);
            return fileKey;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file to S3: {FileName}", fileName);
            throw;
        }
    }

    public async Task<Stream> DownloadAsync(string fileKey)
    {
        try
        {
            var request = new GetObjectRequest
            {
                BucketName = _s3Settings.BucketName,
                Key = fileKey
            };

            var response = await _s3Client.GetObjectAsync(request);
            
            _logger.LogInformation("File downloaded successfully from S3: {FileKey}", fileKey);
            return response.ResponseStream;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("File not found in S3: {FileKey}", fileKey);
            throw new FileNotFoundException($"File not found: {fileKey}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download file from S3: {FileKey}", fileKey);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(string fileKey)
    {
        try
        {
            var request = new DeleteObjectRequest
            {
                BucketName = _s3Settings.BucketName,
                Key = fileKey
            };

            await _s3Client.DeleteObjectAsync(request);
            
            _logger.LogInformation("File deleted successfully from S3: {FileKey}", fileKey);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file from S3: {FileKey}", fileKey);
            return false;
        }
    }

    public async Task<bool> ExistsAsync(string fileKey)
    {
        try
        {
            var request = new GetObjectMetadataRequest
            {
                BucketName = _s3Settings.BucketName,
                Key = fileKey
            };

            await _s3Client.GetObjectMetadataAsync(request);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check file existence in S3: {FileKey}", fileKey);
            return false;
        }
    }

    public async Task<string> GetDownloadUrlAsync(string fileKey, TimeSpan expiry)
    {
        try
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _s3Settings.BucketName,
                Key = fileKey,
                Expires = DateTime.UtcNow.Add(expiry),
                Verb = HttpVerb.GET
            };

            var url = await _s3Client.GetPreSignedURLAsync(request);
            
            _logger.LogInformation("Generated pre-signed URL for S3 file: {FileKey}", fileKey);
            return url;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate pre-signed URL for S3 file: {FileKey}", fileKey);
            throw;
        }
    }

    private string GenerateFileKey(string fileName)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyy/MM/dd");
        var guid = Guid.NewGuid().ToString("N")[..8];
        var extension = Path.GetExtension(fileName);
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        
        // Sanitize filename for S3
        var sanitizedName = string.Join("_", nameWithoutExtension.Split(Path.GetInvalidFileNameChars()));
        
        return $"{_s3Settings.FolderStructure.Uploads}{timestamp}/{sanitizedName}_{guid}{extension}";
    }
}
