using Google.Api.Gax.ResourceNames;
using Google.Cloud.SecretManager.V1;
using Grpc.Core;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Encina.Secrets.GoogleSecretManager.Health;

/// <summary>
/// Health check for Google Cloud Secret Manager connectivity.
/// </summary>
/// <remarks>
/// Verifies connectivity by listing secrets with a page size of 1.
/// </remarks>
public sealed class GoogleSecretManagerHealthCheck : IHealthCheck
{
    /// <summary>
    /// Default health check name.
    /// </summary>
    public const string DefaultName = "encina-secrets-gcp";

    private static readonly string[] DefaultTags = ["encina", "secrets", "gcp", "ready"];

    private readonly SecretManagerServiceClient _client;
    private readonly GoogleSecretManagerOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleSecretManagerHealthCheck"/> class.
    /// </summary>
    /// <param name="client">The Google Secret Manager client.</param>
    /// <param name="options">The provider options.</param>
    public GoogleSecretManagerHealthCheck(SecretManagerServiceClient client, IOptions<GoogleSecretManagerOptions> options)
    {
        _client = client;
        _options = options.Value;
    }

    /// <summary>
    /// Gets the default tags for the Google Secret Manager health check.
    /// </summary>
    internal static IEnumerable<string> Tags => DefaultTags;

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new ListSecretsRequest
            {
                ParentAsProjectName = new ProjectName(_options.ProjectId),
                PageSize = 1
            };

            var response = _client.ListSecretsAsync(request);

            // Access the first page to verify connectivity
            await foreach (var _ in response.WithCancellation(cancellationToken))
            {
                break; // Only need to verify we can make the call
            }

            return HealthCheckResult.Healthy("Google Cloud Secret Manager is accessible.");
        }
        catch (RpcException ex)
        {
            return HealthCheckResult.Unhealthy(
                $"Google Cloud Secret Manager is not accessible. StatusCode: {ex.StatusCode}.",
                ex);
        }
    }
}
