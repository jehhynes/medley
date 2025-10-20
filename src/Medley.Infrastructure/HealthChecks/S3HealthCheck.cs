using Amazon.S3;
using Amazon.S3.Model;
using Medley.Application.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Medley.Infrastructure.HealthChecks;

/// <summary>
/// Health check for AWS S3 service
/// </summary>
public class S3HealthCheck : IHealthCheck
{
    private readonly IAmazonS3 _s3Client;
    private readonly S3Settings _s3Settings;
    private readonly ILogger<S3HealthCheck> _logger;

    public S3HealthCheck(IAmazonS3 s3Client, IOptions<S3Settings> s3Settings, ILogger<S3HealthCheck> logger)
    {
        _s3Client = s3Client;
        _s3Settings = s3Settings.Value;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if bucket exists and is accessible
            var request = new GetBucketLocationRequest
            {
                BucketName = _s3Settings.BucketName
            };

            var response = await _s3Client.GetBucketLocationAsync(request, cancellationToken);
            
            _logger.LogDebug("S3 health check passed for bucket: {BucketName}", _s3Settings.BucketName);
            
            return HealthCheckResult.Healthy($"S3 bucket '{_s3Settings.BucketName}' is accessible", new Dictionary<string, object>
            {
                ["bucket_name"] = _s3Settings.BucketName,
                ["region"] = response.Location.Value
            });
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("S3 bucket not found: {BucketName}", _s3Settings.BucketName);
            return HealthCheckResult.Unhealthy($"S3 bucket '{_s3Settings.BucketName}' not found", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "S3 health check failed for bucket: {BucketName}", _s3Settings.BucketName);
            return HealthCheckResult.Unhealthy($"S3 health check failed: {ex.Message}", ex);
        }
    }
}
