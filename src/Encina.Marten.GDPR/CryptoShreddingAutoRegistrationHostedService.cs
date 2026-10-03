using System.Reflection;
using Encina.Compliance.DataSubjectRights;
using Encina.Diagnostics;
using Encina.Marten.GDPR.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Marten.GDPR;

/// <summary>
/// Hosted service that scans configured assemblies for <see cref="CryptoShreddedAttribute"/>
/// at startup and validates their configuration.
/// </summary>
/// <remarks>
/// <para>
/// This service runs once at application startup. It discovers all properties decorated
/// with <see cref="CryptoShreddedAttribute"/> in the configured assemblies and validates that:
/// </para>
/// <list type="bullet">
/// <item><description>Each crypto-shredded property also has <see cref="PersonalDataAttribute"/></description></item>
/// <item><description>The <see cref="CryptoShreddedAttribute.SubjectIdProperty"/> references a valid,
/// readable property on the declaring type, of a supported subject-id type (<c>string</c>, <c>Guid</c>,
/// an integer type or a strongly-typed id); any other type is a configuration error (#1174)</description></item>
/// <item><description>Each crypto-shredded property is a <c>string</c> with a setter or init accessor, so the
/// serializer can replace its value with the ciphertext; a getter-only property is a configuration error (#1646)</description></item>
/// </list>
/// <para>
/// Pre-populates the <see cref="CryptoShreddedPropertyCache"/> so that the first serialization
/// call does not incur reflection overhead.
/// </para>
/// </remarks>
internal sealed class CryptoShreddingAutoRegistrationHostedService : IHostedService
{
    private readonly CryptoShreddingAutoRegistrationDescriptor _descriptor;
    private readonly CryptoShreddingOptions _options;
    private readonly ILogger<CryptoShreddingAutoRegistrationHostedService> _logger;

    public CryptoShreddingAutoRegistrationHostedService(
        CryptoShreddingAutoRegistrationDescriptor descriptor,
        IOptions<CryptoShreddingOptions> options,
        ILogger<CryptoShreddingAutoRegistrationHostedService> logger)
    {
        _descriptor = descriptor;
        _options = options.Value;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.AutoRegisterFromAttributes || _descriptor.Assemblies.Count == 0)
        {
            _logger.LogDebug("Crypto-shredding auto-registration skipped (disabled or no assemblies configured)");
            return Task.CompletedTask;
        }

        var discoveredTypes = DiscoverAndValidate(_descriptor.Assemblies);

        _logger.AutoRegistrationCompleted(discoveredTypes, _descriptor.Assemblies.Count);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Scans assemblies for event types with <see cref="CryptoShreddedAttribute"/> and validates
    /// co-existence with <see cref="PersonalDataAttribute"/> and valid subject ID references.
    /// </summary>
    /// <returns>The number of event types that have at least one valid crypto-shredded property.</returns>
    private int DiscoverAndValidate(IReadOnlyList<Assembly> assemblies)
    {
        var typesWithCryptoShredding = 0;
        var validationErrors = new List<string>();

        foreach (var assembly in assemblies)
        {
            foreach (var type in LoadTypes(assembly))
            {
                if (ScanType(type, validationErrors))
                {
                    // Pre-populate the static property cache for this type
                    CryptoShreddedPropertyCache.GetFields(type);
                    typesWithCryptoShredding++;
                }
            }
        }

        if (validationErrors.Count > 0)
        {
            throw new InvalidOperationException(
                $"Crypto-shredding auto-registration failed with {validationErrors.Count} validation error(s): "
                + string.Join(" | ", validationErrors));
        }

        return typesWithCryptoShredding;
    }

    private Type[] LoadTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            // Some types may fail to load; process what we can
            _logger.LogWarning(
                ex.ForLogging(),
                "Some types in assembly {AssemblyName} could not be loaded during crypto-shredding scan",
                assembly.GetName().Name);
            return ex.Types.Where(t => t is not null).ToArray()!;
        }
    }

    /// <summary>Validates every crypto-shredded property of <paramref name="type"/>; returns whether it has any.</summary>
    private bool ScanType(Type type, List<string> validationErrors)
    {
        var hasCryptoShredded = false;

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var cryptoAttr = property.GetCustomAttribute<CryptoShreddedAttribute>();
            if (cryptoAttr is null)
            {
                continue;
            }

            hasCryptoShredded = true;
            ValidateProperty(type, property, cryptoAttr, validationErrors);
        }

        return hasCryptoShredded;
    }

    private void ValidateProperty(
        Type type,
        PropertyInfo property,
        CryptoShreddedAttribute cryptoAttr,
        List<string> validationErrors)
    {
        // Validate [PersonalData] co-existence
        if (property.GetCustomAttribute<PersonalDataAttribute>() is null)
        {
            ReportError(
                validationErrors,
                $"Property '{property.Name}' on type '{type.FullName}' has [CryptoShredded] "
                + "but is missing [PersonalData]. Both attributes are required.");
        }

        // Only string properties can be encrypted; any other type would be skipped (stored in plaintext).
        if (property.PropertyType != typeof(string))
        {
            ReportError(
                validationErrors,
                $"Property '{property.Name}' on type '{type.FullName}' has [CryptoShredded] "
                + $"but is of type '{FormatTypeName(property.PropertyType)}'. Only string properties can be encrypted.");
        }

        // The serializer overwrites the value with its ciphertext; a getter-only property would be stored
        // in plaintext, so serialization refuses it and the scan rejects it here (#1646).
        if (!CryptoShreddedPropertyCache.CanSetProperty(type, property))
        {
            ReportError(
                validationErrors,
                $"Property '{property.Name}' on type '{type.FullName}' has [CryptoShredded] "
                + "but has no setter or init accessor, so its value cannot be replaced with the ciphertext. "
                + "Add a setter or an init accessor (positional record properties already have one).");
        }

        ValidateSubjectIdProperty(type, property, cryptoAttr, validationErrors);
    }

    private void ValidateSubjectIdProperty(
        Type type,
        PropertyInfo property,
        CryptoShreddedAttribute cryptoAttr,
        List<string> validationErrors)
    {
        var subjectIdProp = type.GetProperty(
            cryptoAttr.SubjectIdProperty,
            BindingFlags.Public | BindingFlags.Instance);

        var error = DescribeSubjectIdProblem(type, property, cryptoAttr, subjectIdProp);
        if (error is not null)
        {
            ReportError(validationErrors, error);
        }
    }

    private static string? DescribeSubjectIdProblem(
        Type type,
        PropertyInfo property,
        CryptoShreddedAttribute cryptoAttr,
        PropertyInfo? subjectIdProp)
    {
        var prefix = $"Property '{property.Name}' on type '{type.FullName}' references "
            + $"SubjectIdProperty='{cryptoAttr.SubjectIdProperty}' which ";

        if (subjectIdProp is null)
        {
            return prefix + "does not exist as a public instance property on the declaring type.";
        }

        if (!subjectIdProp.CanRead)
        {
            return prefix + "is not readable (it has no getter), so the subject id cannot be read.";
        }

        if (SubjectIdConversion.IsSupportedType(subjectIdProp.PropertyType))
        {
            return null;
        }

        return prefix + $"is of type '{FormatTypeName(subjectIdProp.PropertyType)}'. Supported subject-id types "
            + "are string, Guid, integer types, strongly-typed ids implementing IFormattable, and wrappers "
            + "exposing a public 'Value' property of one of those types.";
    }

    private static string FormatTypeName(Type type) =>
        Nullable.GetUnderlyingType(type) is { } underlying ? underlying.Name + "?" : type.Name;

    private void ReportError(List<string> validationErrors, string error)
    {
        validationErrors.Add(error);
        _logger.LogError("{ValidationError}", error);
    }
}
