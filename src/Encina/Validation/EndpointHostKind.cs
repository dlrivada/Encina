namespace Encina.Validation;

/// <summary>
/// Classification of the host of a configured endpoint, as computed by
/// <see cref="EndpointValidator.ClassifyHost(string?)"/>.
/// </summary>
/// <remarks>
/// The classification only inspects the literal host: an IP address (in any form the operating
/// system resolver accepts, including IPv4-mapped IPv6, decimal and octal IPv4) or a well-known
/// name such as <c>localhost</c>. A DNS name that later resolves to an internal address
/// (DNS rebinding) cannot be detected at configuration time and is reported as <see cref="Public"/>.
/// </remarks>
public enum EndpointHostKind
{
    /// <summary>The host is a DNS name or a publicly routable IP address.</summary>
    Public = 0,

    /// <summary>The host is an RFC 1918 IPv4 address (10/8, 172.16/12, 192.168/16) or an IPv6 unique local address (fc00::/7).</summary>
    Private = 1,

    /// <summary>The host is a loopback address (127.0.0.0/8, ::1), a local socket path, or a loopback name such as <c>localhost</c>.</summary>
    Loopback = 2,

    /// <summary>The host is a link-local address (169.254.0.0/16, fe80::/10).</summary>
    LinkLocal = 3,

    /// <summary>The host is a cloud instance metadata endpoint (for example 169.254.169.254, fd00:ec2::254, 100.100.100.200 or metadata.google.internal).</summary>
    CloudMetadata = 4,

    /// <summary>The host is an unspecified address (0.0.0.0/8, ::).</summary>
    Unspecified = 5,

    /// <summary>The host is missing or is not a syntactically valid host name or IP address.</summary>
    Invalid = 6,
}
