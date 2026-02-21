namespace Encina.Security.Sanitization.Abstractions;

/// <summary>
/// Defines an immutable sanitization profile that controls which HTML elements,
/// attributes, and protocols are allowed during sanitization.
/// </summary>
/// <remarks>
/// <para>
/// Profiles provide fine-grained control over HTML sanitization behavior. Each profile
/// specifies a whitelist of allowed tags, attributes, and URI protocols. Content not
/// matching the whitelist is stripped during sanitization.
/// </para>
/// <para>
/// Use the built-in profiles from <c>SanitizationProfiles</c> for common scenarios,
/// or create custom profiles via <c>SanitizationProfileBuilder</c> for application-specific needs.
/// </para>
/// <para>
/// Implementations must be immutable and thread-safe.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Using a built-in profile
/// string result = sanitizer.Custom(input, SanitizationProfiles.BasicFormatting);
///
/// // Registering a custom profile
/// options.AddProfile("BlogPost", profile =>
/// {
///     profile.AllowTags("p", "h1", "h2", "h3", "a", "img", "ul", "ol", "li");
///     profile.AllowAttributes("href", "src", "alt", "class");
///     profile.AllowProtocols("https", "mailto");
///     profile.StripScripts = true;
/// });
/// </code>
/// </example>
public interface ISanitizationProfile
{
    /// <summary>
    /// Gets the set of HTML tag names that are allowed through sanitization.
    /// </summary>
    /// <remarks>
    /// Tag names are case-insensitive. Tags not in this set will be stripped
    /// during sanitization, though their text content may be preserved depending
    /// on the sanitizer implementation.
    /// </remarks>
    IReadOnlySet<string> AllowedTags { get; }

    /// <summary>
    /// Gets the set of HTML attribute names that are allowed through sanitization.
    /// </summary>
    /// <remarks>
    /// Attribute names are case-insensitive. Attributes not in this set will be
    /// removed from elements during sanitization. Event handler attributes
    /// (e.g., <c>onclick</c>, <c>onerror</c>) should never be included.
    /// </remarks>
    IReadOnlySet<string> AllowedAttributes { get; }

    /// <summary>
    /// Gets the set of URI protocols (schemes) that are allowed in attribute values.
    /// </summary>
    /// <remarks>
    /// Controls which URI schemes are permitted in attributes like <c>href</c> and <c>src</c>.
    /// Common safe values include <c>https</c>, <c>mailto</c>, and <c>tel</c>.
    /// The <c>javascript:</c> protocol should never be included.
    /// </remarks>
    IReadOnlySet<string> AllowedProtocols { get; }

    /// <summary>
    /// Gets a value indicating whether HTML comments should be stripped during sanitization.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, all HTML comments (<c>&lt;!-- ... --&gt;</c>) are removed.
    /// Comments can be used to bypass certain security filters, so stripping them
    /// is recommended for most profiles.
    /// </remarks>
    bool StripComments { get; }

    /// <summary>
    /// Gets a value indicating whether script elements and event handlers should be stripped.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, <c>&lt;script&gt;</c> elements, <c>javascript:</c> URIs,
    /// and all <c>on*</c> event handler attributes are removed. This should be <c>true</c>
    /// for virtually all sanitization profiles.
    /// </remarks>
    bool StripScripts { get; }
}
