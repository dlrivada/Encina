using Encina.Validation;

using Microsoft.Extensions.Options;

namespace Encina.Compliance.Attestation.Validation;

/// <summary>
/// Validates <see cref="HttpAttestationOptions"/> to prevent SSRF attacks through
/// <see cref="EndpointValidator"/>.
/// </summary>
/// <remarks>
/// Endpoints must use HTTPS and must not target loopback or private network (RFC 1918, fc00::/7)
/// addresses unless <see cref="HttpAttestationOptions.AllowInsecureHttp"/> is set. Link-local,
/// cloud metadata and unspecified addresses are always rejected, including their IPv4-mapped
/// IPv6 forms.
/// </remarks>
internal sealed class HttpAttestationOptionsValidator : IValidateOptions<HttpAttestationOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, HttpAttestationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var policy = new EndpointPolicy
        {
            AllowedSchemes = options.AllowInsecureHttp ? [Uri.UriSchemeHttps, Uri.UriSchemeHttp] : [Uri.UriSchemeHttps],
            AllowLocalEndpoints = options.AllowInsecureHttp,
            RejectPrivateNetworks = !options.AllowInsecureHttp,
            LocalEndpointsOptOutName = nameof(HttpAttestationOptions.AllowInsecureHttp),
        };

        var error = EndpointValidator.ValidateUri(options.AttestEndpointUrl, nameof(options.AttestEndpointUrl), policy);
        if (error is null && options.VerifyEndpointUrl is not null)
        {
            error = EndpointValidator.ValidateUri(options.VerifyEndpointUrl, nameof(options.VerifyEndpointUrl), policy);
        }

        return error is null ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(error);
    }
}
