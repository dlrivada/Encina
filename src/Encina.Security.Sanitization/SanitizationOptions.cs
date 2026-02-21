using Encina.Security.Sanitization.Abstractions;
using Encina.Security.Sanitization.Profiles;
using GanssHtmlSanitizer = Ganss.Xss.HtmlSanitizer;

namespace Encina.Security.Sanitization;

/// <summary>
/// Configuration options for the Encina input sanitization and output encoding pipeline.
/// </summary>
/// <remarks>
/// <para>
/// These options control the behavior of <c>InputSanitizationPipelineBehavior</c>
/// and <c>OutputEncodingPipelineBehavior</c>.
/// Register via <c>AddEncinaSanitization(options => { ... })</c>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddEncinaSanitization(options =>
/// {
///     options.SanitizeAllStringInputs = false;
///     options.DefaultProfile = SanitizationProfiles.StrictText;
///     options.EncodeAllOutputs = false;
///     options.AddHealthCheck = true;
///
///     options.AddProfile("BlogPost", profile =>
///     {
///         profile.AllowTags("p", "h1", "h2", "a", "img");
///         profile.AllowAttributes("href", "src", "alt");
///         profile.AllowProtocols("https", "mailto");
///         profile.StripScripts = true;
///     });
/// });
/// </code>
/// </example>
public sealed class SanitizationOptions
{
    private readonly Dictionary<string, ISanitizationProfile> _profiles = new(StringComparer.OrdinalIgnoreCase);
    private Action<GanssHtmlSanitizer>? _htmlSanitizerConfigurator;

    /// <summary>
    /// Gets or sets whether to automatically sanitize all string properties on incoming requests.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <c>true</c>, all <c>string</c> properties on request objects are sanitized
    /// using the <see cref="DefaultProfile"/> unless explicitly opted out with attributes.
    /// </para>
    /// <para>
    /// When <c>false</c> (default), only properties decorated with sanitization attributes
    /// (e.g., <c>[SanitizeHtml]</c>, <c>[Sanitize]</c>) are sanitized.
    /// </para>
    /// </remarks>
    public bool SanitizeAllStringInputs { get; set; }

    /// <summary>
    /// Gets or sets the default sanitization profile applied when no specific profile is specified.
    /// </summary>
    /// <remarks>
    /// Used as the fallback profile for <see cref="SanitizeAllStringInputs"/> mode and
    /// for <c>[Sanitize]</c> attributes without an explicit <c>Profile</c> property.
    /// If <c>null</c>, the <c>StrictText</c> built-in profile is used.
    /// </remarks>
    public ISanitizationProfile? DefaultProfile { get; set; }

    /// <summary>
    /// Gets or sets whether to automatically encode all string properties on outgoing responses.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <c>true</c>, all <c>string</c> properties on response objects are HTML-encoded
    /// unless explicitly opted out.
    /// </para>
    /// <para>
    /// When <c>false</c> (default), only properties decorated with encoding attributes
    /// (e.g., <c>[EncodeForHtml]</c>) are encoded.
    /// </para>
    /// </remarks>
    public bool EncodeAllOutputs { get; set; }

    /// <summary>
    /// Gets or sets whether to register the sanitization health check.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, a health check is registered that verifies sanitization services
    /// are resolvable and operational.
    /// Default is <c>false</c>.
    /// </remarks>
    public bool AddHealthCheck { get; set; }

    /// <summary>
    /// Gets or sets whether to emit OpenTelemetry traces for sanitization and encoding operations.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, activities are created for each pipeline behavior invocation
    /// using the <c>Encina.Security.Sanitization</c> ActivitySource.
    /// Default is <c>false</c>.
    /// </remarks>
    public bool EnableTracing { get; set; }

    /// <summary>
    /// Gets or sets whether to emit OpenTelemetry metrics for sanitization and encoding operations.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, counters and histograms are recorded for each pipeline behavior invocation
    /// using the <c>Encina.Security.Sanitization</c> Meter.
    /// Default is <c>false</c>.
    /// </remarks>
    public bool EnableMetrics { get; set; }

    /// <summary>
    /// Gets the registered custom sanitization profiles.
    /// </summary>
    internal IReadOnlyDictionary<string, ISanitizationProfile> Profiles => _profiles;

    /// <summary>
    /// Registers a custom sanitization profile with the specified name.
    /// </summary>
    /// <param name="name">The unique name for the profile (case-insensitive).</param>
    /// <param name="profile">The sanitization profile to register.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name"/> or <paramref name="profile"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name"/> is empty or whitespace.
    /// </exception>
    public void AddProfile(string name, ISanitizationProfile profile)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        _profiles[name] = profile;
    }

    /// <summary>
    /// Registers a custom sanitization profile using a fluent builder.
    /// </summary>
    /// <param name="name">The unique name for the profile (case-insensitive).</param>
    /// <param name="configure">An action that configures the profile using <see cref="SanitizationProfileBuilder"/>.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name"/> or <paramref name="configure"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name"/> is empty or whitespace.
    /// </exception>
    /// <example>
    /// <code>
    /// options.AddProfile("BlogPost", profile =>
    /// {
    ///     profile.AllowTags("p", "h1", "h2", "a", "img");
    ///     profile.AllowAttributes("href", "src", "alt");
    ///     profile.AllowProtocols("https", "mailto");
    ///     profile.WithStripScripts(true);
    /// });
    /// </code>
    /// </example>
    public void AddProfile(string name, Action<SanitizationProfileBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(configure);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var builder = new SanitizationProfileBuilder();
        configure(builder);
        _profiles[name] = builder.Build();
    }

    /// <summary>
    /// Tries to retrieve a registered sanitization profile by name.
    /// </summary>
    /// <param name="name">The profile name to look up (case-insensitive).</param>
    /// <param name="profile">
    /// When this method returns, contains the profile associated with the specified name,
    /// or <c>null</c> if the name was not found.
    /// </param>
    /// <returns><c>true</c> if the profile was found; otherwise, <c>false</c>.</returns>
    public bool TryGetProfile(string name, out ISanitizationProfile? profile)
        => _profiles.TryGetValue(name, out profile);

    /// <summary>
    /// Configures the underlying Ganss.Xss <see cref="GanssHtmlSanitizer"/> directly.
    /// </summary>
    /// <param name="configure">
    /// An action applied to every <see cref="GanssHtmlSanitizer"/> instance after
    /// profile-based configuration. Use this for advanced settings not covered by
    /// <see cref="ISanitizationProfile"/> (e.g., CSS properties, custom event handlers).
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="configure"/> is <c>null</c>.
    /// </exception>
    /// <example>
    /// <code>
    /// options.UseHtmlSanitizer(sanitizer =>
    /// {
    ///     sanitizer.AllowedCssProperties.Add("color");
    ///     sanitizer.AllowedCssProperties.Add("font-size");
    /// });
    /// </code>
    /// </example>
    public void UseHtmlSanitizer(Action<GanssHtmlSanitizer> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        _htmlSanitizerConfigurator = configure;
    }

    /// <summary>
    /// Gets the optional Ganss.Xss HtmlSanitizer configurator callback.
    /// </summary>
    internal Action<GanssHtmlSanitizer>? HtmlSanitizerConfigurator => _htmlSanitizerConfigurator;
}
