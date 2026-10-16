using System.Text.RegularExpressions;

namespace Encina.Caching;

/// <summary>
/// Redacts the caller-identifying segments of a cache key or key pattern so it can be logged.
/// </summary>
/// <remarks>
/// <para>
/// A cache key built by <see cref="DefaultCacheKeyGenerator"/> or <see cref="CacheConfiguration{TRequest}"/>
/// carries the tenant id (<c>t:{tenantId}</c>) and, for a per-user entry, the user id
/// (<c>u:{userId}</c>). Both are personal data, so no log entry may contain them. Every log site that
/// writes a cache key or key pattern goes through this helper; the real key is still used for the
/// cache itself.
/// </para>
/// <para>
/// The identifying segments are replaced by <c>***</c> (<c>t:***</c>, <c>u:***</c>). A hash is
/// deliberately not used: a hash of a user id is still pseudonymous personal data and links entries
/// across logs.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// CacheKeyRedactor.Redact("encina:t:tenant-a:u:alice:GetMyAppointments:3F2A");
/// // "encina:t:***:u:***:GetMyAppointments:3F2A"
/// </code>
/// </example>
public static partial class CacheKeyRedactor
{
    /// <summary>The text that replaces an identifying value.</summary>
    public const string Mask = "***";

    /// <summary>
    /// Returns <paramref name="key"/> with its tenant and user segments redacted.
    /// </summary>
    /// <param name="key">The cache key or key pattern.</param>
    /// <param name="tenantId">
    /// The tenant id the key was built from, when known. Every occurrence is masked, which also covers
    /// an id that itself contains the <c>:</c> segment separator. Only the <c>t:{id}</c> form is masked.
    /// </param>
    /// <param name="userId">The user id the key was built from, when known; handled like <paramref name="tenantId"/>.</param>
    /// <returns>The redacted key, or an empty string when <paramref name="key"/> is null or empty.</returns>
    public static string Redact(string? key, string? tenantId = null, string? userId = null)
    {
        if (string.IsNullOrEmpty(key))
        {
            return string.Empty;
        }

        var redacted = MaskKnown(MaskKnown(key, "u:", userId), "t:", tenantId);
        return IdentifyingSegment().Replace(redacted, "${kind}:" + Mask);
    }

    // Masks "{kind}{id}" in full, so an id that contains the separator does not leak its tail.
    private static string MaskKnown(string key, string kind, string? id) =>
        string.IsNullOrEmpty(id) ? key : key.Replace(kind + id, kind + Mask, StringComparison.Ordinal);

    // A segment that starts with "t:" or "u:" (at the start of the key or after a separator) up to the next separator.
    [GeneratedRegex("(?<=^|:)(?<kind>[tu]):[^:]*", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex IdentifyingSegment();
}
