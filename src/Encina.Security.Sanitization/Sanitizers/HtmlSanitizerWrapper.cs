using System.Collections.Concurrent;
using Encina.Security.Sanitization.Abstractions;
using Ganss.Xss;

namespace Encina.Security.Sanitization.Sanitizers;

/// <summary>
/// Wraps <see cref="HtmlSanitizer"/> from Ganss.Xss with profile-based configuration
/// and thread-safe instance caching.
/// </summary>
/// <remarks>
/// <para>
/// Each <see cref="ISanitizationProfile"/> maps to a dedicated <see cref="HtmlSanitizer"/>
/// instance that is configured once and reused for subsequent calls with the same profile.
/// </para>
/// <para>
/// Pass-through profiles (no allowed tags, no strip flags) bypass sanitization entirely
/// and return the input unchanged.
/// </para>
/// </remarks>
internal sealed class HtmlSanitizerWrapper
{
    private readonly ConcurrentDictionary<ISanitizationProfile, HtmlSanitizer> _cache = new();
    private readonly Action<HtmlSanitizer>? _configurator;

    /// <summary>
    /// Initializes a new instance of the <see cref="HtmlSanitizerWrapper"/> class.
    /// </summary>
    /// <param name="configurator">
    /// An optional callback applied to every <see cref="HtmlSanitizer"/> instance
    /// after profile configuration, allowing direct customization of Ganss.Xss behavior.
    /// </param>
    internal HtmlSanitizerWrapper(Action<HtmlSanitizer>? configurator = null)
    {
        _configurator = configurator;
    }

    /// <summary>
    /// Sanitizes HTML input according to the specified profile.
    /// </summary>
    /// <param name="input">The HTML string to sanitize.</param>
    /// <param name="profile">The sanitization profile defining allowed content.</param>
    /// <returns>The sanitized HTML string.</returns>
    internal string Sanitize(string input, ISanitizationProfile profile)
    {
        // Pass-through: no allowed tags and no stripping requested means "don't sanitize"
        if (profile.AllowedTags.Count == 0
            && !profile.StripComments
            && !profile.StripScripts)
        {
            return input;
        }

        var sanitizer = _cache.GetOrAdd(profile, CreateSanitizer);
        return sanitizer.Sanitize(input);
    }

    private HtmlSanitizer CreateSanitizer(ISanitizationProfile profile)
    {
        var sanitizer = new HtmlSanitizer();

        // Clear all defaults — we configure exclusively from the profile
        sanitizer.AllowedTags.Clear();
        sanitizer.AllowedAttributes.Clear();
        sanitizer.AllowedSchemes.Clear();

        foreach (var tag in profile.AllowedTags)
        {
            sanitizer.AllowedTags.Add(tag);
        }

        foreach (var attr in profile.AllowedAttributes)
        {
            sanitizer.AllowedAttributes.Add(attr);
        }

        foreach (var protocol in profile.AllowedProtocols)
        {
            sanitizer.AllowedSchemes.Add(protocol);
        }

        // Apply optional user customizations after profile configuration
        _configurator?.Invoke(sanitizer);

        return sanitizer;
    }
}
