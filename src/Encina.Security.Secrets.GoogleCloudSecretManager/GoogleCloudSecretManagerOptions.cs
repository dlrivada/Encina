namespace Encina.Security.Secrets.GoogleCloudSecretManager;

/// <summary>
/// Configuration options for the Google Cloud Secret Manager provider.
/// </summary>
/// <remarks>
/// <para>
/// Use this class to configure the Google Cloud Secret Manager connection,
/// specifying the GCP project that contains your secrets.
/// </para>
/// <para>
/// <see cref="ProjectId"/> is <b>required</b>. The provider will throw
/// <see cref="InvalidOperationException"/> at startup if it is not set.
/// </para>
/// <para>
/// Authentication uses Application Default Credentials (ADC) by default.
/// To use custom credentials, pre-register a configured
/// <c>SecretManagerServiceClient</c> in the DI container before calling
/// <see cref="ServiceCollectionExtensions.AddGoogleCloudSecretManager"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddGoogleCloudSecretManager(
///     gcp =>
///     {
///         gcp.ProjectId = "my-gcp-project";
///     });
/// </code>
/// </example>
public sealed class GoogleCloudSecretManagerOptions
{
    /// <summary>
    /// Gets or sets the Google Cloud project ID that contains the secrets.
    /// </summary>
    /// <value>
    /// The GCP project ID (e.g., <c>"my-gcp-project"</c>).
    /// This property is <b>required</b>.
    /// </value>
    public string ProjectId { get; set; } = "";
}
