using Encina.Security.Sanitization.Abstractions;

namespace Encina.Security.Sanitization.Profiles;

/// <summary>
/// Fluent builder for creating custom <see cref="ISanitizationProfile"/> instances.
/// </summary>
/// <remarks>
/// <para>
/// Use this builder to create application-specific profiles that control exactly
/// which HTML elements, attributes, and protocols are permitted during sanitization.
/// </para>
/// <para>
/// The builder produces immutable <see cref="SanitizationProfile"/> records.
/// By default, scripts and comments are stripped unless explicitly configured otherwise.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var profile = new SanitizationProfileBuilder()
///     .AllowTags("p", "a", "img", "h1", "h2")
///     .AllowAttributes("href", "src", "alt", "class")
///     .AllowProtocols("https", "mailto")
///     .WithStripScripts(true)
///     .Build();
/// </code>
/// </example>
public sealed class SanitizationProfileBuilder
{
    private readonly HashSet<string> _allowedTags = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _allowedAttributes = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _allowedProtocols = new(StringComparer.OrdinalIgnoreCase);
    private bool _stripComments = true;
    private bool _stripScripts = true;

    /// <summary>
    /// Adds HTML tag names to the set of allowed tags.
    /// </summary>
    /// <param name="tags">The tag names to allow (case-insensitive).</param>
    /// <returns>This builder for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tags"/> is <c>null</c>.</exception>
    public SanitizationProfileBuilder AllowTags(params string[] tags)
    {
        ArgumentNullException.ThrowIfNull(tags);

        foreach (var tag in tags)
        {
            _allowedTags.Add(tag);
        }

        return this;
    }

    /// <summary>
    /// Adds HTML attribute names to the set of allowed attributes.
    /// </summary>
    /// <param name="attributes">The attribute names to allow (case-insensitive).</param>
    /// <returns>This builder for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="attributes"/> is <c>null</c>.</exception>
    public SanitizationProfileBuilder AllowAttributes(params string[] attributes)
    {
        ArgumentNullException.ThrowIfNull(attributes);

        foreach (var attr in attributes)
        {
            _allowedAttributes.Add(attr);
        }

        return this;
    }

    /// <summary>
    /// Adds URI protocols to the set of allowed protocols.
    /// </summary>
    /// <param name="protocols">The protocol names to allow (e.g., "https", "mailto").</param>
    /// <returns>This builder for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="protocols"/> is <c>null</c>.</exception>
    public SanitizationProfileBuilder AllowProtocols(params string[] protocols)
    {
        ArgumentNullException.ThrowIfNull(protocols);

        foreach (var protocol in protocols)
        {
            _allowedProtocols.Add(protocol);
        }

        return this;
    }

    /// <summary>
    /// Sets whether HTML comments should be stripped during sanitization.
    /// </summary>
    /// <param name="strip"><c>true</c> to strip comments (default); <c>false</c> to preserve them.</param>
    /// <returns>This builder for chaining.</returns>
    public SanitizationProfileBuilder WithStripComments(bool strip)
    {
        _stripComments = strip;
        return this;
    }

    /// <summary>
    /// Sets whether script elements and event handlers should be stripped during sanitization.
    /// </summary>
    /// <param name="strip"><c>true</c> to strip scripts (default); <c>false</c> to preserve them.</param>
    /// <returns>This builder for chaining.</returns>
    /// <remarks>
    /// <b>Warning:</b> Setting this to <c>false</c> removes XSS protection.
    /// Only disable script stripping for trusted, already-sanitized content.
    /// </remarks>
    public SanitizationProfileBuilder WithStripScripts(bool strip)
    {
        _stripScripts = strip;
        return this;
    }

    /// <summary>
    /// Builds an immutable <see cref="ISanitizationProfile"/> from the current configuration.
    /// </summary>
    /// <returns>A new immutable sanitization profile.</returns>
    public ISanitizationProfile Build() => new SanitizationProfile(
        AllowedTags: new HashSet<string>(_allowedTags, StringComparer.OrdinalIgnoreCase),
        AllowedAttributes: new HashSet<string>(_allowedAttributes, StringComparer.OrdinalIgnoreCase),
        AllowedProtocols: new HashSet<string>(_allowedProtocols, StringComparer.OrdinalIgnoreCase),
        StripComments: _stripComments,
        StripScripts: _stripScripts);
}
