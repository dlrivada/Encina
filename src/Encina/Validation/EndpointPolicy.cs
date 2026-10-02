namespace Encina.Validation;

/// <summary>
/// The rules <see cref="EndpointValidator"/> applies to one configured endpoint.
/// </summary>
/// <remarks>
/// <para>
/// Link-local, cloud metadata and unspecified hosts are always rejected; there is no opt-out.
/// Loopback hosts are rejected unless <see cref="AllowLocalEndpoints"/> is set. Private network
/// hosts (RFC 1918, fc00::/7) are allowed unless <see cref="RejectPrivateNetworks"/> is set.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var policy = EndpointPolicy.ForHttps(options.AllowInsecureHttp, options.AllowLocalEndpoints);
/// var error = EndpointValidator.ValidateUrl(options.VaultAddress, nameof(options.VaultAddress), policy);
/// </code>
/// </example>
public sealed class EndpointPolicy
{
    /// <summary>
    /// Gets the URI schemes the endpoint may use (compared case-insensitively). Ignored by
    /// <see cref="EndpointValidator.ValidateHost(string?, string, EndpointPolicy)"/>.
    /// </summary>
    public required IReadOnlyList<string> AllowedSchemes { get; init; }

    /// <summary>
    /// Gets a value indicating whether loopback hosts (<c>localhost</c>, 127.0.0.0/8, ::1, local
    /// sockets) are accepted. Intended for local development and sidecars only.
    /// </summary>
    public bool AllowLocalEndpoints { get; init; }

    /// <summary>
    /// Gets a value indicating whether private network hosts (RFC 1918, fc00::/7) are rejected.
    /// Defaults to <c>false</c>.
    /// </summary>
    public bool RejectPrivateNetworks { get; init; }

    /// <summary>
    /// Gets the name of the options property that allows plain HTTP, quoted in error messages.
    /// Defaults to <c>AllowInsecureHttp</c>.
    /// </summary>
    public string InsecureHttpOptOutName { get; init; } = "AllowInsecureHttp";

    /// <summary>
    /// Gets the name of the options property that allows loopback and private hosts, quoted in
    /// error messages. Defaults to <c>AllowLocalEndpoints</c>.
    /// </summary>
    public string LocalEndpointsOptOutName { get; init; } = "AllowLocalEndpoints";

    /// <summary>
    /// Creates a policy for an HTTPS endpoint.
    /// </summary>
    /// <param name="allowInsecureHttp">Whether plain <c>http</c> is accepted in addition to <c>https</c>.</param>
    /// <param name="allowLocalEndpoints">Whether loopback hosts are accepted.</param>
    /// <returns>The policy.</returns>
    public static EndpointPolicy ForHttps(bool allowInsecureHttp, bool allowLocalEndpoints) => new()
    {
        AllowedSchemes = allowInsecureHttp ? [Uri.UriSchemeHttps, Uri.UriSchemeHttp] : [Uri.UriSchemeHttps],
        AllowLocalEndpoints = allowLocalEndpoints,
    };
}
