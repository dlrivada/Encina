namespace Encina.Compliance.Attestation;

/// <summary>
/// Configuration for the <see cref="Providers.HttpAttestationProvider"/>.
/// </summary>
public sealed class HttpAttestationOptions
{
    /// <summary>
    /// Gets or sets the URL of the attestation endpoint (POST).
    /// This property is required and validated at service registration time.
    /// </summary>
    public Uri AttestEndpointUrl { get; set; } = null!;

    /// <summary>
    /// Gets or sets the optional URL of the verification endpoint (POST).
    /// When null, <see cref="Providers.HttpAttestationProvider.VerifyAsync"/> returns
    /// <c>IsValid = false</c> with a descriptive failure reason.
    /// </summary>
    public Uri? VerifyEndpointUrl { get; set; }

    /// <summary>
    /// Gets or sets the authorization header value (e.g., "Bearer &lt;token&gt;").
    /// </summary>
    public string? AuthHeader { get; set; }
}
