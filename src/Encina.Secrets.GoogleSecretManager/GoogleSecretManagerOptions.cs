namespace Encina.Secrets.GoogleSecretManager;

/// <summary>
/// Configuration options for the Google Cloud Secret Manager secret provider.
/// </summary>
public sealed class GoogleSecretManagerOptions
{
    /// <summary>
    /// Gets or sets the Google Cloud project ID (e.g., <c>my-gcp-project</c>).
    /// </summary>
    /// <remarks>
    /// This is required. The project ID is used to construct resource names
    /// in the format <c>projects/{projectId}/secrets/{secretName}</c>.
    /// </remarks>
    public string ProjectId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the health check configuration.
    /// </summary>
    public ProviderHealthCheckOptions ProviderHealthCheck { get; set; } = new();
}
