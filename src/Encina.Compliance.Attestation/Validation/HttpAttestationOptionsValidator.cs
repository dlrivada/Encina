using System.Net;
using System.Net.Sockets;

using Microsoft.Extensions.Options;

namespace Encina.Compliance.Attestation.Validation;

/// <summary>
/// Validates <see cref="HttpAttestationOptions"/> to prevent SSRF attacks.
/// Enforces HTTPS-only endpoints and rejects loopback and link-local addresses
/// unless <see cref="HttpAttestationOptions.AllowInsecureHttp"/> is set.
/// </summary>
internal sealed class HttpAttestationOptionsValidator : IValidateOptions<HttpAttestationOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, HttpAttestationOptions options)
    {
        if (options.AttestEndpointUrl is null)
        {
            return ValidateOptionsResult.Fail(
                "HttpAttestationOptions.AttestEndpointUrl must be configured.");
        }

        if (!options.AllowInsecureHttp)
        {
            var attestError = ValidateUrl(options.AttestEndpointUrl, nameof(options.AttestEndpointUrl));
            if (attestError is not null)
                return ValidateOptionsResult.Fail(attestError);

            if (options.VerifyEndpointUrl is not null)
            {
                var verifyError = ValidateUrl(options.VerifyEndpointUrl, nameof(options.VerifyEndpointUrl));
                if (verifyError is not null)
                    return ValidateOptionsResult.Fail(verifyError);
            }
        }

        return ValidateOptionsResult.Success;
    }

    private static string? ValidateUrl(Uri uri, string propertyName)
    {
        if (!uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
        {
            return $"{propertyName} must use HTTPS. "
                + "Set AllowInsecureHttp = true to allow plain HTTP (development/testing only).";
        }

        // Reject localhost by hostname
        if (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
        {
            return $"{propertyName} must not target localhost. "
                + "Set AllowInsecureHttp = true to allow local addresses (development/testing only).";
        }

        // Reject loopback and link-local IP addresses
        if (uri.HostNameType is UriHostNameType.IPv4 or UriHostNameType.IPv6
            && IPAddress.TryParse(uri.Host, out var ip))
        {
            if (IPAddress.IsLoopback(ip))
            {
                return $"{propertyName} must not target a loopback address ({uri.Host}). "
                    + "Set AllowInsecureHttp = true to allow this in development.";
            }

            // IPv4 link-local: 169.254.0.0/16
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                var bytes = ip.GetAddressBytes();
                if (bytes[0] == 169 && bytes[1] == 254)
                {
                    return $"{propertyName} must not target a link-local address (169.254.x.x). "
                        + "These are reserved for APIPA and should never reach an external attestation endpoint.";
                }
            }

            // IPv6 link-local: fe80::/10
            if (ip.AddressFamily == AddressFamily.InterNetworkV6)
            {
                var bytes = ip.GetAddressBytes();
                if ((bytes[0] & 0xFE) == 0xFE && (bytes[1] & 0xC0) == 0x80)
                {
                    return $"{propertyName} must not target an IPv6 link-local address (fe80::/10).";
                }
            }

            // IPv4 RFC 1918 private ranges: 10.0.0.0/8, 172.16.0.0/12, 192.168.0.0/16
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                var bytes = ip.GetAddressBytes();
                if (bytes[0] == 10
                    || (bytes[0] == 172 && (bytes[1] & 0xF0) == 16)
                    || (bytes[0] == 192 && bytes[1] == 168))
                {
                    return $"{propertyName} must not target a private RFC 1918 address ({uri.Host}). "
                        + "Set AllowInsecureHttp = true to allow private network addresses (development/testing only).";
                }
            }

            // IPv6 ULA: fc00::/7 (first byte & 0xFE == 0xFC)
            if (ip.AddressFamily == AddressFamily.InterNetworkV6)
            {
                var bytes = ip.GetAddressBytes();
                if ((bytes[0] & 0xFE) == 0xFC)
                {
                    return $"{propertyName} must not target an IPv6 ULA address (fc00::/7). "
                        + "Set AllowInsecureHttp = true to allow private network addresses (development/testing only).";
                }
            }
        }

        return null;
    }
}
