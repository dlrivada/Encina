using Encina.Security.Sanitization.Abstractions;
using Encina.Security.Sanitization.Profiles;
using Encina.Security.Sanitization.Sanitizers;
using Microsoft.Extensions.Options;

namespace Encina.Security.Sanitization;

/// <summary>
/// Default implementation of <see cref="ISanitizer"/> that delegates to context-specific
/// sanitizers based on the target output context.
/// </summary>
/// <remarks>
/// <para>
/// This is the primary sanitizer registered by <c>AddEncinaSanitization</c>.
/// It coordinates between specialized sanitizers:
/// </para>
/// <list type="bullet">
/// <item><description>HTML → Ganss.Xss <c>HtmlSanitizer</c> via <see cref="HtmlSanitizerWrapper"/></description></item>
/// <item><description>SQL → <see cref="SqlSanitizer"/> (defense-in-depth)</description></item>
/// <item><description>Shell → <see cref="ShellSanitizer"/> (OS-aware)</description></item>
/// <item><description>JSON → <see cref="JsonSanitizer"/> (RFC 8259)</description></item>
/// <item><description>XML → <see cref="XmlSanitizer"/> (XML 1.0 entities)</description></item>
/// </list>
/// <para>
/// Thread-safe. All internal sanitizers are either stateless (static) or use
/// thread-safe caching (<see cref="HtmlSanitizerWrapper"/>).
/// </para>
/// </remarks>
public sealed class DefaultSanitizer : ISanitizer
{
    private readonly SanitizationOptions _options;
    private readonly HtmlSanitizerWrapper _htmlWrapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultSanitizer"/> class.
    /// </summary>
    /// <param name="options">The sanitization options containing profiles and configuration.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options"/> is <c>null</c>.
    /// </exception>
    public DefaultSanitizer(IOptions<SanitizationOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
        _htmlWrapper = new HtmlSanitizerWrapper(_options.HtmlSanitizerConfigurator);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Uses the <see cref="SanitizationOptions.DefaultProfile"/> for sanitization.
    /// Falls back to <see cref="SanitizationProfiles.StrictText"/> when no default profile is configured.
    /// </remarks>
    public string SanitizeHtml(string input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var profile = _options.DefaultProfile ?? SanitizationProfiles.StrictText;
        return _htmlWrapper.Sanitize(input, profile);
    }

    /// <inheritdoc />
    public string SanitizeForSql(string input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return SqlSanitizer.Sanitize(input);
    }

    /// <inheritdoc />
    public string SanitizeForShell(string input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return ShellSanitizer.Sanitize(input);
    }

    /// <inheritdoc />
    public string SanitizeForJson(string input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return JsonSanitizer.Sanitize(input);
    }

    /// <inheritdoc />
    public string SanitizeForXml(string input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return XmlSanitizer.Sanitize(input);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Applies HTML sanitization using the specified <paramref name="profile"/>.
    /// The profile controls which tags, attributes, and protocols are allowed.
    /// </remarks>
    public string Custom(string input, ISanitizationProfile profile)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(profile);

        return _htmlWrapper.Sanitize(input, profile);
    }
}
