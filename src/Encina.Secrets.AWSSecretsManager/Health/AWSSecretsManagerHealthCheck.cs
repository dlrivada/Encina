using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Encina.Secrets.AWSSecretsManager.Health;

/// <summary>
/// Health check for AWS Secrets Manager connectivity.
/// </summary>
public sealed class AWSSecretsManagerHealthCheck : IHealthCheck
{
    /// <summary>
    /// Default health check name.
    /// </summary>
    public const string DefaultName = "encina-secrets-aws";

    private static readonly string[] DefaultTags = ["encina", "secrets", "aws", "ready"];

    private readonly IAmazonSecretsManager _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="AWSSecretsManagerHealthCheck"/> class.
    /// </summary>
    /// <param name="client">The AWS Secrets Manager client.</param>
    public AWSSecretsManagerHealthCheck(IAmazonSecretsManager client)
    {
        _client = client;
    }

    /// <summary>
    /// Gets the default tags for the AWS health check.
    /// </summary>
    internal static IEnumerable<string> Tags => DefaultTags;

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await _client.ListSecretsAsync(
                new ListSecretsRequest { MaxResults = 1 },
                cancellationToken);

            return HealthCheckResult.Healthy("AWS Secrets Manager is accessible.");
        }
        catch (AmazonSecretsManagerException ex)
        {
            return HealthCheckResult.Unhealthy(
                $"AWS Secrets Manager is not accessible. ErrorCode: {ex.ErrorCode}.",
                ex);
        }
    }
}
