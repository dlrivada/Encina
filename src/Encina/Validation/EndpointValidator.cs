using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Frozen;
using System.Globalization;
using System.Net;
using System.Net.Sockets;

namespace Encina.Validation;

/// <summary>
/// Validates configured outbound endpoints (URLs and host names) against server-side request
/// forgery (SSRF): scheme allow-list, loopback, link-local, cloud metadata and unspecified hosts.
/// </summary>
/// <remarks>
/// <para>
/// Every method returns <c>null</c> when the endpoint is acceptable, or an error message that
/// names the property and the reason. Messages never echo the configured value, so a credential
/// embedded in a URL or connection string cannot leak through an
/// <c>OptionsValidationException</c>.
/// </para>
/// <para>
/// Hosts are normalised before classification: surrounding brackets and trailing dots are removed,
/// international names are converted to their ASCII (IDNA) form, and IPv4-mapped, IPv4-compatible
/// and NAT64 (64:ff9b::/96) IPv6 addresses are classified by their embedded IPv4 address. Decimal,
/// hexadecimal and octal IPv4 forms are parsed the way the operating system resolver parses them.
/// </para>
/// <para>
/// The checks only see the literal host. A DNS name that resolves to an internal address
/// (DNS rebinding) cannot be detected at configuration time.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public ValidateOptionsResult Validate(string? name, MyOptions options)
/// {
///     var policy = EndpointPolicy.ForHttps(options.AllowInsecureHttp, options.AllowLocalEndpoints);
///     var error = EndpointValidator.ValidateUri(options.Endpoint, nameof(options.Endpoint), policy);
///     return error is null ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(error);
/// }
/// </code>
/// </example>
public static class EndpointValidator
{
    // fd00:ec2::254 — AWS EC2 instance metadata service over IPv6.
    private static readonly IPAddress AwsMetadataIPv6 = IPAddress.Parse("fd00:ec2::254");

    // Letters, digits, hyphen, dot and underscore (container and service names may use '_').
    private static readonly SearchValues<char> HostNameChars =
        SearchValues.Create("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._");

    private static readonly FrozenSet<string> LoopbackNames = FrozenSet.Create(
        StringComparer.OrdinalIgnoreCase,
        "localhost",
        "ip6-localhost",
        "ip6-loopback",
        "localhost.localdomain",
        "localhost4",
        "localhost4.localdomain4",
        "localhost6",
        "localhost6.localdomain6");

    private static readonly FrozenSet<string> MetadataNames = FrozenSet.Create(
        StringComparer.OrdinalIgnoreCase,
        "metadata",
        "metadata.google.internal",
        "metadata.goog",
        "instance-data",
        "instance-data.ec2.internal");

    /// <summary>
    /// Validates an absolute endpoint URI: scheme and host.
    /// </summary>
    /// <param name="uri">The configured URI; <c>null</c> is reported as not configured.</param>
    /// <param name="propertyName">The options property name used in the error message.</param>
    /// <param name="policy">The policy to apply.</param>
    /// <returns><c>null</c> when the URI is acceptable; otherwise the error message.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="propertyName"/> or <paramref name="policy"/> is <c>null</c>.</exception>
    public static string? ValidateUri(Uri? uri, string propertyName, EndpointPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(propertyName);
        ArgumentNullException.ThrowIfNull(policy);

        if (uri is null)
        {
            return $"{propertyName} must be configured.";
        }

        if (!uri.IsAbsoluteUri)
        {
            return $"{propertyName} must be an absolute URI.";
        }

        return CheckScheme(uri.Scheme, propertyName, policy)
            ?? CheckHostKind(ClassifyHost(uri.IdnHost), propertyName, policy);
    }

    /// <summary>
    /// Parses and validates an absolute endpoint URL: scheme and host.
    /// </summary>
    /// <param name="url">The configured URL; <c>null</c> or whitespace is reported as not configured.</param>
    /// <param name="propertyName">The options property name used in the error message.</param>
    /// <param name="policy">The policy to apply.</param>
    /// <returns><c>null</c> when the URL is acceptable; otherwise the error message.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="propertyName"/> or <paramref name="policy"/> is <c>null</c>.</exception>
    public static string? ValidateUrl(string? url, string propertyName, EndpointPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(propertyName);
        ArgumentNullException.ThrowIfNull(policy);

        if (string.IsNullOrWhiteSpace(url))
        {
            return $"{propertyName} must be configured.";
        }

        return Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)
            ? ValidateUri(uri, propertyName, policy)
            : $"{propertyName} must be an absolute URI.";
    }

    /// <summary>
    /// Validates a bare host name or IP address (no scheme, no port). The policy's
    /// <see cref="EndpointPolicy.AllowedSchemes"/> is ignored.
    /// </summary>
    /// <param name="host">The configured host; <c>null</c> or whitespace is reported as not configured.</param>
    /// <param name="propertyName">The options property name used in the error message.</param>
    /// <param name="policy">The policy to apply.</param>
    /// <returns><c>null</c> when the host is acceptable; otherwise the error message.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="propertyName"/> or <paramref name="policy"/> is <c>null</c>.</exception>
    public static string? ValidateHost(string? host, string propertyName, EndpointPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(propertyName);
        ArgumentNullException.ThrowIfNull(policy);

        return string.IsNullOrWhiteSpace(host)
            ? $"{propertyName} must be configured."
            : CheckHostKind(ClassifyHost(host), propertyName, policy);
    }

    /// <summary>
    /// Classifies a host name or IP address literal.
    /// </summary>
    /// <param name="host">The host, optionally in brackets (IPv6) and with a trailing dot.</param>
    /// <returns>The classification; <see cref="EndpointHostKind.Invalid"/> for a missing or malformed host.</returns>
    public static EndpointHostKind ClassifyHost(string? host)
    {
        var normalized = NormalizeHost(host);
        if (normalized is null)
        {
            return EndpointHostKind.Invalid;
        }

        if (IPAddress.TryParse(normalized, out var address))
        {
            return ClassifyAddress(address);
        }

        var ascii = ToAsciiHost(normalized);
        if (ascii is null)
        {
            return EndpointHostKind.Invalid;
        }

        return IPAddress.TryParse(ascii, out address) ? ClassifyAddress(address) : ClassifyName(ascii);
    }

    /// <summary>
    /// Classifies an IP address. IPv4-mapped, IPv4-compatible and NAT64 (64:ff9b::/96) IPv6
    /// addresses are classified by their embedded IPv4 address.
    /// </summary>
    /// <param name="address">The address.</param>
    /// <returns>The classification.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="address"/> is <c>null</c>.</exception>
    public static EndpointHostKind ClassifyAddress(IPAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            return ClassifyIPv4(BinaryPrimitives.ReadUInt32BigEndian(address.GetAddressBytes()));
        }

        return ClassifyIPv6(address);
    }

    private static string? CheckScheme(string scheme, string propertyName, EndpointPolicy policy)
    {
        if (policy.AllowedSchemes.Contains(scheme, StringComparer.OrdinalIgnoreCase))
        {
            return null;
        }

        var httpsAllowed = policy.AllowedSchemes.Contains(Uri.UriSchemeHttps, StringComparer.OrdinalIgnoreCase);
        if (httpsAllowed && scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
        {
            return $"{propertyName} must use HTTPS. Set {policy.InsecureHttpOptOutName} = true to allow plain HTTP (development/testing only).";
        }

        return $"{propertyName} must use one of the allowed schemes ({string.Join(", ", policy.AllowedSchemes)}).";
    }

    // crap-exempt: single-question switch — maps one host classification to its policy error.
    private static string? CheckHostKind(EndpointHostKind kind, string propertyName, EndpointPolicy policy) => kind switch
    {
        EndpointHostKind.Invalid => $"{propertyName} must specify a valid host name or IP address.",
        EndpointHostKind.Unspecified => $"{propertyName} must not target an unspecified address (0.0.0.0, ::).",
        EndpointHostKind.LinkLocal => $"{propertyName} must not target a link-local address (169.254.0.0/16, fe80::/10).",
        EndpointHostKind.CloudMetadata => $"{propertyName} must not target a cloud instance metadata endpoint.",
        EndpointHostKind.Loopback when !policy.AllowLocalEndpoints =>
            $"{propertyName} must not target localhost or a loopback address. Set {policy.LocalEndpointsOptOutName} = true to allow local endpoints (development/testing only).",
        EndpointHostKind.Private when policy.RejectPrivateNetworks =>
            $"{propertyName} must not target a private network address (RFC 1918, fc00::/7). Set {policy.LocalEndpointsOptOutName} = true to allow private network addresses (development/testing only).",
        _ => null,
    };

    private static string? NormalizeHost(string? host)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            return null;
        }

        var value = host.Trim();
        if (value.Length > 1 && value[0] == '[' && value[^1] == ']')
        {
            value = value[1..^1];
        }

        value = value.TrimEnd('.');
        return value.Length == 0 ? null : value;
    }

    private static string? ToAsciiHost(string host)
    {
        string ascii;
        try
        {
            ascii = new IdnMapping().GetAscii(host).TrimEnd('.');
        }
        catch (ArgumentException)
        {
            return null;
        }

        return ascii.Length == 0 || ascii.AsSpan().ContainsAnyExcept(HostNameChars) ? null : ascii;
    }

    private static EndpointHostKind ClassifyName(string asciiHost)
    {
        if (LoopbackNames.Contains(asciiHost)
            || asciiHost.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase))
        {
            return EndpointHostKind.Loopback;
        }

        return MetadataNames.Contains(asciiHost) ? EndpointHostKind.CloudMetadata : EndpointHostKind.Public;
    }

    // crap-exempt: single-question switch — classifies one IPv4 address into its range.
    private static EndpointHostKind ClassifyIPv4(uint value) => value switch
    {
        0xA9FEA9FE or 0x646464C8 or 0xA83F8110 => EndpointHostKind.CloudMetadata, // 169.254.169.254, 100.100.100.200, 168.63.129.16
        _ when value >> 24 == 0 => EndpointHostKind.Unspecified,     // 0.0.0.0/8
        _ when value >> 24 == 127 => EndpointHostKind.Loopback,      // 127.0.0.0/8
        _ when value >> 16 == 0xA9FE => EndpointHostKind.LinkLocal,  // 169.254.0.0/16
        _ when IsPrivateIPv4(value) => EndpointHostKind.Private,
        _ => EndpointHostKind.Public,
    };

    private static bool IsPrivateIPv4(uint value) =>
        value >> 24 == 10          // 10.0.0.0/8
        || value >> 20 == 0xAC1    // 172.16.0.0/12
        || value >> 16 == 0xC0A8;  // 192.168.0.0/16

    private static EndpointHostKind ClassifyIPv6(IPAddress address)
    {
        var bytes = address.GetAddressBytes();
        var bare = new IPAddress(bytes); // drops the scope id (fe80::1%eth0) before comparing

        if (bare.Equals(IPAddress.IPv6Loopback))
        {
            return EndpointHostKind.Loopback;
        }

        // The IPv4-compatible prefix also covers "::", whose embedded 0.0.0.0 is unspecified.
        return TryGetEmbeddedIPv4(bytes, out var embedded)
            ? ClassifyIPv4(embedded)
            : ClassifyIPv6Prefix(bare, bytes);
    }

    private static EndpointHostKind ClassifyIPv6Prefix(IPAddress bare, byte[] bytes)
    {
        if (bytes[0] == 0xFE && (bytes[1] & 0xC0) == 0x80)
        {
            return EndpointHostKind.LinkLocal; // fe80::/10 (ff80:: multicast does not match)
        }

        if (bare.Equals(AwsMetadataIPv6))
        {
            return EndpointHostKind.CloudMetadata;
        }

        return (bytes[0] & 0xFE) == 0xFC ? EndpointHostKind.Private : EndpointHostKind.Public; // fc00::/7
    }

    // IPv4-mapped (::ffff:a.b.c.d), IPv4-compatible (::a.b.c.d) and NAT64 (64:ff9b::a.b.c.d).
    private static bool TryGetEmbeddedIPv4(byte[] bytes, out uint value)
    {
        var prefix = bytes.AsSpan(0, 12);
        var embeds = prefix.SequenceEqual(MappedPrefix)
            || prefix.SequenceEqual(CompatiblePrefix)
            || prefix.SequenceEqual(Nat64Prefix);

        value = embeds ? BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(12, 4)) : 0;
        return embeds;
    }

    private static ReadOnlySpan<byte> MappedPrefix => [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0xFF, 0xFF];

    private static ReadOnlySpan<byte> CompatiblePrefix => [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];

    private static ReadOnlySpan<byte> Nat64Prefix => [0, 0x64, 0xFF, 0x9B, 0, 0, 0, 0, 0, 0, 0, 0];
}
