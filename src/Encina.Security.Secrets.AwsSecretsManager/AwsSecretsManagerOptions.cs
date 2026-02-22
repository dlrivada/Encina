using Amazon;
using Amazon.Runtime;
using Amazon.SecretsManager;

namespace Encina.Security.Secrets.AwsSecretsManager;

/// <summary>
/// Configuration options for the AWS Secrets Manager secret provider.
/// </summary>
/// <remarks>
/// <para>
/// Use this class to configure the AWS Secrets Manager connection, including the region,
/// authentication credentials, and client behavior options.
/// </para>
/// <para>
/// When <see cref="Credentials"/> is <c>null</c>, the AWS SDK default credential chain is used,
/// which supports IAM roles, environment variables, shared credential files, and other
/// AWS identity sources automatically.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddAwsSecretsManager(
///     aws =>
///     {
///         aws.Region = RegionEndpoint.USEast1;
///         aws.Credentials = new BasicAWSCredentials("AKIA...", "secret...");
///         aws.ClientConfig = new AmazonSecretsManagerConfig
///         {
///             MaxErrorRetry = 3
///         };
///     });
/// </code>
/// </example>
public sealed class AwsSecretsManagerOptions
{
    /// <summary>
    /// Gets or sets the AWS region endpoint for Secrets Manager.
    /// </summary>
    /// <value>
    /// When <c>null</c> (default), the region is resolved from the AWS SDK default chain
    /// (environment variables, config file, instance metadata).
    /// </value>
    public RegionEndpoint? Region { get; set; }

    /// <summary>
    /// Gets or sets optional <see cref="AWSCredentials"/> for authenticating to AWS.
    /// </summary>
    /// <value>
    /// When <c>null</c> (default), the AWS SDK default credential chain is used,
    /// which automatically discovers credentials from the environment.
    /// </value>
    public AWSCredentials? Credentials { get; set; }

    /// <summary>
    /// Gets or sets optional <see cref="AmazonSecretsManagerConfig"/> for configuring the underlying
    /// <c>AmazonSecretsManagerClient</c> behavior, including retry policies and timeouts.
    /// </summary>
    /// <value>
    /// When <c>null</c> (default), the AWS SDK default configuration is used.
    /// </value>
    public AmazonSecretsManagerConfig? ClientConfig { get; set; }
}
