using System.Reflection;

using Encina.Compliance.DataSubjectRights.Diagnostics;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Compliance.DataSubjectRights;

/// <summary>
/// Hosted service that scans configured assemblies for <see cref="PersonalDataAttribute"/>
/// at startup and builds a personal data map for DSR operations.
/// </summary>
/// <remarks>
/// <para>
/// This service runs once at application startup. It discovers all entity types with properties
/// decorated with <see cref="PersonalDataAttribute"/> in the configured assemblies and logs
/// the discovered personal data fields for observability.
/// </para>
/// <para>
/// The discovered metadata is used by <see cref="IPersonalDataLocator"/> implementations to
/// know which fields contain personal data, their categories, and their erasure/portability flags.
/// </para>
/// <para>
/// Follows the same pattern as <c>ConsentAutoRegistrationHostedService</c> in the Consent module
/// and <c>GDPRAutoRegistrationHostedService</c> in the GDPR module.
/// </para>
/// </remarks>
internal sealed class DSRAutoRegistrationHostedService : IHostedService
{
    private readonly DSRAutoRegistrationDescriptor _descriptor;
    private readonly DataSubjectRightsOptions _options;
    private readonly ILogger<DSRAutoRegistrationHostedService> _logger;

    public DSRAutoRegistrationHostedService(
        DSRAutoRegistrationDescriptor descriptor,
        IOptions<DataSubjectRightsOptions> options,
        ILogger<DSRAutoRegistrationHostedService> logger)
    {
        _descriptor = descriptor;
        _options = options.Value;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.AutoRegisterFromAttributes || _descriptor.Assemblies.Count == 0)
        {
            _logger.DSRAutoRegistrationSkipped();
            return Task.CompletedTask;
        }

        var totalFieldCount = DiscoverPersonalDataFields(_descriptor.Assemblies);

        _logger.DSRAutoRegistrationCompleted(totalFieldCount, _descriptor.Assemblies.Count);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Scans assemblies for types with properties decorated with <see cref="PersonalDataAttribute"/>
    /// and logs the discovered fields.
    /// </summary>
    /// <returns>The total number of personal data fields discovered.</returns>
    private int DiscoverPersonalDataFields(IReadOnlyList<Assembly> assemblies)
    {
        var totalFieldCount = 0;

        foreach (var assembly in assemblies)
        {
            foreach (var type in assembly.GetTypes())
            {
                var personalDataProperties = type
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.GetCustomAttribute<PersonalDataAttribute>() is not null)
                    .ToList();

                if (personalDataProperties.Count == 0)
                {
                    continue;
                }

                var entityTypeName = type.FullName ?? type.Name;
                _logger.PersonalDataEntityDiscovered(entityTypeName, personalDataProperties.Count);

                foreach (var property in personalDataProperties)
                {
                    var attribute = property.GetCustomAttribute<PersonalDataAttribute>()!;
                    _logger.PersonalDataFieldDiscovered(
                        entityTypeName,
                        property.Name,
                        attribute.Category.ToString());
                }

                totalFieldCount += personalDataProperties.Count;
            }
        }

        return totalFieldCount;
    }
}
