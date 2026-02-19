using Amazon.Runtime;

namespace Encina.Secrets.AWSSecretsManager;

/// <summary>
/// Configuration options for the AWS Secrets Manager secret provider.
/// </summary>
public sealed class AWSSecretsManagerOptions
{
    /// <summary>
    /// Gets or sets the AWS region (e.g., <c>us-east-1</c>).
    /// When <c>null</c>, the default region from environment/credentials is used.
    /// </summary>
    public string? Region { get; set; }

    /// <summary>
    /// Gets or sets the AWS credentials provider.
    /// When <c>null</c>, the default credential chain is used.
    /// </summary>
    public AWSCredentials? Credentials { get; set; }

    /// <summary>
    /// Gets or sets the health check configuration.
    /// </summary>
    public ProviderHealthCheckOptions ProviderHealthCheck { get; set; } = new();
}
