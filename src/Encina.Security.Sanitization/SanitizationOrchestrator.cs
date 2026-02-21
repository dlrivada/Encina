using Encina.Security.Sanitization.Abstractions;
using Encina.Security.Sanitization.Attributes;
using Encina.Security.Sanitization.Profiles;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static LanguageExt.Prelude;

namespace Encina.Security.Sanitization;

/// <summary>
/// Orchestrates property-level input sanitization for objects with properties
/// decorated with <see cref="SanitizationAttribute"/> subclasses.
/// </summary>
/// <remarks>
/// <para>
/// This orchestrator discovers sanitizable properties via <see cref="SanitizationPropertyCache"/>
/// and delegates sanitization operations to <see cref="ISanitizer"/>.
/// </para>
/// <para>
/// Supports two modes:
/// <list type="bullet">
/// <item><description><b>Attribute-based</b>: Only properties decorated with sanitization attributes
/// (e.g., <c>[SanitizeHtml]</c>, <c>[SanitizeSql]</c>, <c>[Sanitize]</c>) are sanitized.</description></item>
/// <item><description><b>Auto-sanitize</b>: When <see cref="SanitizationOptions.SanitizeAllStringInputs"/>
/// is <c>true</c>, all string properties are sanitized using the default profile.</description></item>
/// </list>
/// </para>
/// </remarks>
internal sealed class SanitizationOrchestrator
{
    private readonly ISanitizer _sanitizer;
    private readonly SanitizationOptions _options;
    private readonly ILogger<SanitizationOrchestrator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SanitizationOrchestrator"/> class.
    /// </summary>
    /// <param name="sanitizer">The sanitizer for input sanitization operations.</param>
    /// <param name="options">The sanitization configuration options.</param>
    /// <param name="logger">The logger for structured logging.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any parameter is <c>null</c>.
    /// </exception>
    public SanitizationOrchestrator(
        ISanitizer sanitizer,
        IOptions<SanitizationOptions> options,
        ILogger<SanitizationOrchestrator> logger)
    {
        _sanitizer = sanitizer ?? throw new ArgumentNullException(nameof(sanitizer));
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Sanitizes all applicable properties on the given request instance.
    /// </summary>
    /// <typeparam name="TRequest">The request type to sanitize.</typeparam>
    /// <param name="request">The request instance whose properties will be sanitized.</param>
    /// <returns>
    /// <c>Right(Unit)</c> on success, or <c>Left(EncinaError)</c> if a critical sanitization failure occurs.
    /// </returns>
    /// <remarks>
    /// <para>
    /// In attribute-based mode, discovers properties via <see cref="SanitizationPropertyCache"/>
    /// and applies the appropriate sanitization strategy based on the attribute type.
    /// </para>
    /// <para>
    /// In auto-sanitize mode, sanitizes all string properties using the default profile
    /// unless they already have a specific sanitization attribute.
    /// </para>
    /// </remarks>
    internal Either<EncinaError, Unit> Sanitize<TRequest>(TRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestType = typeof(TRequest);

        // Attribute-based sanitization
        var properties = SanitizationPropertyCache.GetProperties(requestType);

        foreach (var prop in properties)
        {
            var value = prop.Getter(request) as string;
            if (value is null)
            {
                continue;
            }

            var result = SanitizeProperty(value, prop.Attribute);

            if (result.IsLeft)
            {
                return result.Match<Either<EncinaError, Unit>>(
                    Right: _ => Unit.Default,
                    Left: e => e);
            }

            var sanitized = result.Match(
                Right: v => v,
                Left: _ => value);

            prop.Setter(request, sanitized);
        }

        // Auto-sanitize mode: sanitize all remaining string properties
        if (_options.SanitizeAllStringInputs)
        {
            var attributePropertyNames = new System.Collections.Generic.HashSet<string>(
                properties.Select(p => p.Property.Name),
                StringComparer.Ordinal);

            var stringProperties = SanitizationPropertyCache.GetStringProperties(requestType);

            foreach (var prop in stringProperties)
            {
                // Skip properties that already have explicit attributes
                if (attributePropertyNames.Contains(prop.Name))
                {
                    continue;
                }

                var value = prop.GetValue(request) as string;
                if (value is null)
                {
                    continue;
                }

                try
                {
                    var profile = _options.DefaultProfile ?? SanitizationProfiles.StrictText;
                    var sanitized = _sanitizer.Custom(value, profile);
                    prop.SetValue(request, sanitized);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Auto-sanitization failed for property '{PropertyName}' on {TypeName}",
                        prop.Name, requestType.Name);

                    return SanitizationErrors.PropertyError(prop.Name, ex);
                }
            }
        }

        return Right<EncinaError, Unit>(Unit.Default);
    }

    /// <summary>
    /// Sanitizes a single property value based on the attribute's sanitization type.
    /// </summary>
    private Either<EncinaError, string> SanitizeProperty(string value, SanitizationAttribute attribute)
    {
        try
        {
            var sanitized = attribute.SanitizationType switch
            {
                SanitizationType.Html => _sanitizer.SanitizeHtml(value),
                SanitizationType.Sql => _sanitizer.SanitizeForSql(value),
                SanitizationType.Shell => _sanitizer.SanitizeForShell(value),
                SanitizationType.StripHtml => _sanitizer.Custom(value, SanitizationProfiles.None),
                SanitizationType.Custom => SanitizeWithCustomProfile(value, attribute),
                _ => value
            };

            return Right<EncinaError, string>(sanitized);
        }
        catch (Exception ex)
        {
            return SanitizationErrors.PropertyError("unknown", ex);
        }
    }

    /// <summary>
    /// Sanitizes using a custom profile resolved from the options.
    /// </summary>
    private string SanitizeWithCustomProfile(string value, SanitizationAttribute attribute)
    {
        var profileName = (attribute as SanitizeAttribute)?.Profile;

        if (profileName is not null && _options.TryGetProfile(profileName, out var profile) && profile is not null)
        {
            return _sanitizer.Custom(value, profile);
        }

        if (profileName is not null)
        {
            _logger.LogWarning(
                "Sanitization profile '{ProfileName}' not found, using default profile",
                profileName);
        }

        // Fall back to default profile
        var defaultProfile = _options.DefaultProfile ?? SanitizationProfiles.StrictText;
        return _sanitizer.Custom(value, defaultProfile);
    }
}
