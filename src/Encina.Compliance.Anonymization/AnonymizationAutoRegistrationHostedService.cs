using System.Reflection;

using Encina.Compliance.Anonymization.Diagnostics;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Compliance.Anonymization;

/// <summary>
/// Hosted service that scans configured assemblies for anonymization attributes
/// (<see cref="AnonymizeAttribute"/>, <see cref="PseudonymizeAttribute"/>,
/// <see cref="TokenizeAttribute"/>) at startup and logs the discovered fields.
/// </summary>
/// <remarks>
/// <para>
/// This service runs once at application startup. It discovers all response types with properties
/// decorated with anonymization attributes in the configured assemblies and logs
/// the discovered fields for observability.
/// </para>
/// <para>
/// The discovered metadata provides visibility into which response types have declarative
/// anonymization configured, aiding in compliance auditing and development diagnostics.
/// </para>
/// </remarks>
internal sealed class AnonymizationAutoRegistrationHostedService : IHostedService
{
    private readonly AnonymizationAutoRegistrationDescriptor _descriptor;
    private readonly AnonymizationOptions _options;
    private readonly ILogger<AnonymizationAutoRegistrationHostedService> _logger;

    public AnonymizationAutoRegistrationHostedService(
        AnonymizationAutoRegistrationDescriptor descriptor,
        IOptions<AnonymizationOptions> options,
        ILogger<AnonymizationAutoRegistrationHostedService> logger)
    {
        _descriptor = descriptor;
        _options = options.Value;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.AutoRegisterFromAttributes || _descriptor.Assemblies.Count == 0)
        {
            _logger.AnonymizationAutoRegistrationSkipped();
            return Task.CompletedTask;
        }

        var totalFieldCount = DiscoverAnonymizationFields(_descriptor.Assemblies);

        _logger.AnonymizationAutoRegistrationCompleted(totalFieldCount, _descriptor.Assemblies.Count);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Scans assemblies for types with properties decorated with anonymization attributes
    /// and logs the discovered fields.
    /// </summary>
    /// <returns>The total number of anonymization fields discovered.</returns>
    private int DiscoverAnonymizationFields(IReadOnlyList<Assembly> assemblies)
    {
        var totalFieldCount = 0;

        foreach (var assembly in assemblies)
        {
            foreach (var type in assembly.GetTypes())
            {
                var decoratedProperties = type
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p =>
                        p.GetCustomAttribute<AnonymizeAttribute>() is not null ||
                        p.GetCustomAttribute<PseudonymizeAttribute>() is not null ||
                        p.GetCustomAttribute<TokenizeAttribute>() is not null)
                    .ToList();

                if (decoratedProperties.Count == 0)
                {
                    continue;
                }

                var entityTypeName = type.FullName ?? type.Name;
                _logger.AnonymizationEntityDiscovered(entityTypeName, decoratedProperties.Count);

                foreach (var property in decoratedProperties)
                {
                    var transformationType = GetTransformationType(property);
                    _logger.AnonymizationFieldDiscovered(
                        entityTypeName,
                        property.Name,
                        transformationType);
                }

                totalFieldCount += decoratedProperties.Count;
            }
        }

        return totalFieldCount;
    }

    private static string GetTransformationType(PropertyInfo property)
    {
        if (property.GetCustomAttribute<AnonymizeAttribute>() is not null)
        {
            return "Anonymize";
        }

        if (property.GetCustomAttribute<PseudonymizeAttribute>() is not null)
        {
            return "Pseudonymize";
        }

        return "Tokenize";
    }
}
