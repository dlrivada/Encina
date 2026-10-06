using System.Diagnostics;
using System.Net;

namespace Encina;

/// <summary>
/// Normalizes the client-controlled members of <see cref="InboundRequestInfo"/> and the other ids a
/// scope carries. Never refuses: an unusable value is replaced or dropped.
/// </summary>
internal static class InboundRequestNormalizer
{
    /// <summary>
    /// The metadata key of the caller's IP address (the key <c>Encina.AspNetCore</c> and
    /// <c>Encina.Security.Audit</c> read).
    /// </summary>
    internal const string IpAddressKey = "Encina.Audit.IpAddress";

    /// <summary>
    /// The metadata key of the caller's user agent.
    /// </summary>
    internal const string UserAgentKey = "Encina.Audit.UserAgent";

    /// <summary>
    /// The metadata key of the caller's data region hint.
    /// </summary>
    internal const string DataRegionKey = "Encina.DataResidency.Region";

    /// <summary>
    /// Returns <paramref name="value"/> when it is a usable id (not blank, at most
    /// <see cref="InboundRequestInfo.MaxIdLength"/> characters, no control characters); otherwise
    /// <see langword="null"/>.
    /// </summary>
    internal static string? Id(string? value) => Bounded(value, InboundRequestInfo.MaxIdLength);

    /// <summary>
    /// Returns <paramref name="requested"/> when usable, otherwise the current activity id when
    /// usable, otherwise a new GUID.
    /// </summary>
    internal static string CorrelationId(string? requested) =>
        Id(requested) ?? Id(Activity.Current?.Id) ?? Guid.NewGuid().ToString("N");

    /// <summary>
    /// Returns the idempotency key when usable (at most <see cref="InboundRequestInfo.MaxIdempotencyKeyLength"/>
    /// characters, no control characters); otherwise <see langword="null"/>.
    /// </summary>
    internal static string? IdempotencyKey(string? value) => Bounded(value, InboundRequestInfo.MaxIdempotencyKeyLength);

    /// <summary>
    /// Strips control characters from the user agent, then truncates it to
    /// <see cref="InboundRequestInfo.MaxUserAgentLength"/>; <see langword="null"/> when nothing is left.
    /// </summary>
    internal static string? UserAgent(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var stripped = string.Concat(value.Where(static character => !char.IsControl(character))).Trim();
        if (stripped.Length == 0)
        {
            return null;
        }

        return stripped.Length > InboundRequestInfo.MaxUserAgentLength ? stripped[..InboundRequestInfo.MaxUserAgentLength] : stripped;
    }

    /// <summary>
    /// Returns the canonical form of the IP address when it parses; otherwise <see langword="null"/>.
    /// </summary>
    internal static string? IpAddress(string? value) =>
        !string.IsNullOrWhiteSpace(value) && IPAddress.TryParse(value.Trim(), out var address) ? address.ToString() : null;

    /// <summary>
    /// Returns the data region when usable (at most <see cref="InboundRequestInfo.MaxDataRegionLength"/>
    /// characters, no control characters); otherwise <see langword="null"/>.
    /// </summary>
    internal static string? DataRegion(string? value) => Bounded(value, InboundRequestInfo.MaxDataRegionLength);

    /// <summary>
    /// Builds the metadata of an inbound context from the normalized IP address, user agent and data region.
    /// </summary>
    internal static IEnumerable<KeyValuePair<string, object?>> Metadata(InboundRequestInfo request)
    {
        if (IpAddress(request.IpAddress) is { } ipAddress)
        {
            yield return new KeyValuePair<string, object?>(IpAddressKey, ipAddress);
        }

        if (UserAgent(request.UserAgent) is { } userAgent)
        {
            yield return new KeyValuePair<string, object?>(UserAgentKey, userAgent);
        }

        if (DataRegion(request.DataRegion) is { } dataRegion)
        {
            yield return new KeyValuePair<string, object?>(DataRegionKey, dataRegion);
        }
    }

    private static string? Bounded(string? value, int maxLength) =>
        string.IsNullOrWhiteSpace(value) || value.Length > maxLength || value.Any(char.IsControl) ? null : value;
}
