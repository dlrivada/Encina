using Encina.Security.Sanitization.Abstractions;

namespace Encina.Security.Sanitization.Profiles;

/// <summary>
/// Provides built-in sanitization profiles for common use cases.
/// </summary>
/// <remarks>
/// <para>
/// Profiles are ordered by restrictiveness (most restrictive first):
/// <see cref="None"/> → <see cref="StrictText"/> → <see cref="BasicFormatting"/>
/// → <see cref="RichText"/> → <see cref="Markdown"/>.
/// </para>
/// <para>
/// All profiles follow the OWASP XSS Prevention Cheat Sheet recommendations.
/// For custom profiles, use <see cref="SanitizationProfileBuilder"/>.
/// </para>
/// </remarks>
public static class SanitizationProfiles
{
    /// <summary>
    /// No sanitization applied. Passes input through unchanged.
    /// </summary>
    /// <remarks>
    /// <b>Warning:</b> Use only when input is already trusted or will be encoded on output.
    /// This profile does not strip scripts, comments, or any HTML elements.
    /// </remarks>
    public static ISanitizationProfile None { get; } = new SanitizationProfile(
        AllowedTags: new HashSet<string>(StringComparer.OrdinalIgnoreCase),
        AllowedAttributes: new HashSet<string>(StringComparer.OrdinalIgnoreCase),
        AllowedProtocols: new HashSet<string>(StringComparer.OrdinalIgnoreCase),
        StripComments: false,
        StripScripts: false);

    /// <summary>
    /// Strips all HTML tags, leaving only plain text content.
    /// </summary>
    /// <remarks>
    /// Removes all HTML elements, attributes, comments, and scripts.
    /// Use for fields that should contain only plain text (e.g., names, titles, search terms).
    /// </remarks>
    public static ISanitizationProfile StrictText { get; } = SanitizationProfile.Empty;

    /// <summary>
    /// Allows basic text formatting tags only.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Allowed tags: <c>b</c>, <c>i</c>, <c>u</c>, <c>em</c>, <c>strong</c>,
    /// <c>br</c>, <c>p</c>, <c>span</c>.
    /// </para>
    /// <para>
    /// No attributes or protocols are allowed. Scripts and comments are stripped.
    /// Suitable for simple text content with basic emphasis.
    /// </para>
    /// </remarks>
    public static ISanitizationProfile BasicFormatting { get; } = new SanitizationProfile(
        AllowedTags: new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "b", "i", "u", "em", "strong", "br", "p", "span"
        },
        AllowedAttributes: new HashSet<string>(StringComparer.OrdinalIgnoreCase),
        AllowedProtocols: new HashSet<string>(StringComparer.OrdinalIgnoreCase),
        StripComments: true,
        StripScripts: true);

    /// <summary>
    /// Allows rich text formatting including headings, lists, links, and images.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Allowed tags: all from <see cref="BasicFormatting"/> plus <c>h1</c>-<c>h6</c>,
    /// <c>ul</c>, <c>ol</c>, <c>li</c>, <c>a</c>, <c>img</c>, <c>blockquote</c>,
    /// <c>code</c>, <c>pre</c>, <c>hr</c>, <c>sub</c>, <c>sup</c>, <c>table</c>,
    /// <c>thead</c>, <c>tbody</c>, <c>tr</c>, <c>th</c>, <c>td</c>.
    /// </para>
    /// <para>
    /// Allowed attributes: <c>href</c>, <c>src</c>, <c>alt</c>, <c>title</c>, <c>class</c>.
    /// </para>
    /// <para>
    /// Allowed protocols: <c>https</c>, <c>mailto</c>.
    /// </para>
    /// <para>
    /// Suitable for rich text editors (WYSIWYG) and CMS content.
    /// </para>
    /// </remarks>
    public static ISanitizationProfile RichText { get; } = new SanitizationProfile(
        AllowedTags: new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "b", "i", "u", "em", "strong", "br", "p", "span",
            "h1", "h2", "h3", "h4", "h5", "h6",
            "ul", "ol", "li",
            "a", "img",
            "blockquote", "code", "pre", "hr",
            "sub", "sup",
            "table", "thead", "tbody", "tr", "th", "td"
        },
        AllowedAttributes: new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "href", "src", "alt", "title", "class"
        },
        AllowedProtocols: new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "https", "mailto"
        },
        StripComments: true,
        StripScripts: true);

    /// <summary>
    /// Allows a safe subset of HTML typically generated from Markdown rendering.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Includes all of <see cref="RichText"/> plus <c>dl</c>, <c>dt</c>, <c>dd</c>,
    /// <c>abbr</c>, <c>details</c>, <c>summary</c>.
    /// </para>
    /// <para>
    /// Adds <c>id</c> attribute for heading anchors and <c>target</c> for link behavior.
    /// Suitable for rendered Markdown output (e.g., README files, documentation).
    /// </para>
    /// </remarks>
    public static ISanitizationProfile Markdown { get; } = new SanitizationProfile(
        AllowedTags: new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "b", "i", "u", "em", "strong", "br", "p", "span",
            "h1", "h2", "h3", "h4", "h5", "h6",
            "ul", "ol", "li",
            "a", "img",
            "blockquote", "code", "pre", "hr",
            "sub", "sup",
            "table", "thead", "tbody", "tr", "th", "td",
            "dl", "dt", "dd",
            "abbr", "details", "summary"
        },
        AllowedAttributes: new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "href", "src", "alt", "title", "class", "id", "target"
        },
        AllowedProtocols: new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "https", "mailto"
        },
        StripComments: true,
        StripScripts: true);
}
