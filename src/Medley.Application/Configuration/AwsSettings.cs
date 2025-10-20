namespace Medley.Application.Configuration;

/// <summary>
/// Root AWS configuration settings
/// </summary>
public class AwsSettings
{
    /// <summary>
    /// AWS region (e.g., us-east-1, us-west-2)
    /// </summary>
    public string Region { get; set; } = "us-east-1";

    /// <summary>
    /// AWS access key ID (for development - use IAM roles in production)
    /// </summary>
    public string AccessKeyId { get; set; } = string.Empty;

    /// <summary>
    /// AWS secret access key (for development - use IAM roles in production)
    /// </summary>
    public string SecretAccessKey { get; set; } = string.Empty;

    /// <summary>
    /// S3-specific configuration
    /// </summary>
    public S3Settings S3 { get; set; } = new();

    /// <summary>
    /// Bedrock-specific configuration
    /// </summary>
    public BedrockSettings Bedrock { get; set; } = new();
}
