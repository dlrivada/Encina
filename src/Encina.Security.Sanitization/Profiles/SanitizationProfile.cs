using Encina.Security.Sanitization.Abstractions;

namespace Encina.Security.Sanitization.Profiles;

/// <summary>
/// An immutable sanitization profile that defines which HTML elements, attributes,
/// and protocols are allowed during sanitization.
/// </summary>
/// <remarks>
/// <para>
/// Use <see cref="SanitizationProfiles"/> for common built-in profiles, or
/// <see cref="SanitizationProfileBuilder"/> to create custom profiles with a fluent API.
/// </para>
/// <para>
/// This type is a record for value equality semantics and immutability.
/// All collection properties use <see cref="IReadOnlySet{T}"/> to prevent modification.
/// </para>
/// </remarks>
/// <param name="AllowedTags">The HTML tag names allowed through sanitization (case-insensitive).</param>
/// <param name="AllowedAttributes">The HTML attribute names allowed through sanitization (case-insensitive).</param>
/// <param name="AllowedProtocols">The URI protocols (schemes) allowed in attribute values.</param>
/// <param name="StripComments">Whether HTML comments should be removed.</param>
/// <param name="StripScripts">Whether script elements and event handlers should be removed.</param>
public sealed record SanitizationProfile(
    IReadOnlySet<string> AllowedTags,
    IReadOnlySet<string> AllowedAttributes,
    IReadOnlySet<string> AllowedProtocols,
    bool StripComments,
    bool StripScripts) : ISanitizationProfile
{
    /// <summary>
    /// An empty profile that strips all HTML content.
    /// </summary>
    internal static readonly SanitizationProfile Empty = new(
        AllowedTags: new HashSet<string>(StringComparer.OrdinalIgnoreCase),
        AllowedAttributes: new HashSet<string>(StringComparer.OrdinalIgnoreCase),
        AllowedProtocols: new HashSet<string>(StringComparer.OrdinalIgnoreCase),
        StripComments: true,
        StripScripts: true);
}
