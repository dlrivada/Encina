using System.Reflection;

using Encina.Compliance.DataSubjectRights;
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
/// readable <c>string</c> property on the declaring type</description></item>
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
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                // Some types may fail to load; process what we can
                types = ex.Types.Where(t => t is not null).ToArray()!;
                _logger.LogWarning(
                    ex,
                    "Some types in assembly {AssemblyName} could not be loaded during crypto-shredding scan",
                    assembly.GetName().Name);
            }

            foreach (var type in types)
            {
                var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                var hasCryptoShredded = false;

                foreach (var property in properties)
                {
                    var cryptoAttr = property.GetCustomAttribute<CryptoShreddedAttribute>();
                    if (cryptoAttr is null)
                    {
                        continue;
                    }

                    hasCryptoShredded = true;

                    // Validate [PersonalData] co-existence
                    var personalDataAttr = property.GetCustomAttribute<PersonalDataAttribute>();
                    if (personalDataAttr is null)
                    {
                        var error = $"Property '{property.Name}' on type '{type.FullName}' has [CryptoShredded] "
                            + "but is missing [PersonalData]. Both attributes are required.";
                        validationErrors.Add(error);
                        _logger.LogError("{ValidationError}", error);
                    }

                    // Validate SubjectIdProperty reference
                    var subjectIdProp = type.GetProperty(
                        cryptoAttr.SubjectIdProperty,
                        BindingFlags.Public | BindingFlags.Instance);

                    if (subjectIdProp is null)
                    {
                        var error = $"Property '{property.Name}' on type '{type.FullName}' references "
                            + $"SubjectIdProperty='{cryptoAttr.SubjectIdProperty}' which does not exist "
                            + "as a public instance property on the declaring type.";
                        validationErrors.Add(error);
                        _logger.LogError("{ValidationError}", error);
                    }
                    else if (subjectIdProp.PropertyType != typeof(string))
                    {
                        var error = $"Property '{property.Name}' on type '{type.FullName}' references "
                            + $"SubjectIdProperty='{cryptoAttr.SubjectIdProperty}' which is of type "
                            + $"'{subjectIdProp.PropertyType.Name}' instead of 'string'.";
                        validationErrors.Add(error);
                        _logger.LogError("{ValidationError}", error);
                    }
                }

                if (hasCryptoShredded)
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
}
