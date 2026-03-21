namespace Encina.Compliance.Attestation;

/// <summary>
/// Configuration for the <see cref="Providers.HttpAttestationProvider"/>.
/// </summary>
public sealed class HttpAttestationOptions
{
    /// <summary>
    /// Gets or sets the URL of the attestation endpoint (POST).
    /// </summary>
    public Uri AttestEndpointUrl { get; set; } = null!;

    /// <summary>
    /// Gets or sets the optional URL of the verification endpoint (POST).
    /// When null, verification is a no-op returning success.
    /// </summary>
    public Uri? VerifyEndpointUrl { get; set; }

    /// <summary>
    /// Gets or sets the authorization header value (e.g., "Bearer &lt;token&gt;").
    /// </summary>
    public string? AuthHeader { get; set; }
}
