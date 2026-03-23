using System.Text.Json.Serialization;

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
    /// <remarks>
    /// By default, only HTTPS URLs are accepted. Set <see cref="AllowInsecureHttp"/>
    /// to <c>true</c> to allow HTTP in development or testing scenarios.
    /// </remarks>
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
    /// <remarks>
    /// This value is excluded from JSON serialization and the default <see cref="ToString()"/>
    /// output to prevent accidental exposure in logs or diagnostics endpoints.
    /// </remarks>
    [JsonIgnore]
    public string? AuthHeader { get; set; }

    /// <summary>
    /// Gets or sets whether to allow non-HTTPS endpoint URLs.
    /// Default is <c>false</c> (HTTPS required).
    /// </summary>
    /// <remarks>
    /// Set to <c>true</c> only for development or testing scenarios.
    /// When <c>false</c>, loopback addresses (127.x.x.x, [::1]) and
    /// link-local addresses (169.254.x.x) are also rejected.
    /// </remarks>
    public bool AllowInsecureHttp { get; set; }

    /// <inheritdoc />
    /// <remarks>
    /// The <see cref="AuthHeader"/> value is redacted to prevent accidental exposure.
    /// </remarks>
    public override string ToString() =>
        $"HttpAttestationOptions {{ AttestEndpointUrl = {AttestEndpointUrl}, " +
        $"VerifyEndpointUrl = {VerifyEndpointUrl}, " +
        $"AuthHeader = {(AuthHeader is null ? "null" : "[REDACTED]")}, " +
        $"AllowInsecureHttp = {AllowInsecureHttp} }}";
}
