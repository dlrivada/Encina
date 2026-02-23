using System.Reflection;

using Encina.Compliance.Consent.Diagnostics;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Compliance.Consent;

/// <summary>
/// Hosted service that scans configured assemblies for <see cref="RequireConsentAttribute"/>
/// at startup and validates discovered purposes against <see cref="ConsentOptions.PurposeDefinitions"/>.
/// </summary>
/// <remarks>
/// <para>
/// This service runs once at application startup. It discovers all request types decorated
/// with <see cref="RequireConsentAttribute"/> in the configured assemblies and verifies that
/// every referenced purpose is defined in <see cref="ConsentOptions.PurposeDefinitions"/>.
/// </para>
/// <para>
/// When <see cref="ConsentOptions.FailOnUnknownPurpose"/> is <c>true</c>, the service throws
/// an <see cref="InvalidOperationException"/> if any unknown purposes are found, preventing
/// the application from starting with misconfigured consent requirements.
/// </para>
/// <para>
/// Follows the same pattern as <c>GDPRAutoRegistrationHostedService</c> in the GDPR module.
/// </para>
/// </remarks>
internal sealed class ConsentAutoRegistrationHostedService : IHostedService
{
    private readonly ConsentAutoRegistrationDescriptor _descriptor;
    private readonly ConsentOptions _options;
    private readonly ILogger<ConsentAutoRegistrationHostedService> _logger;

    public ConsentAutoRegistrationHostedService(
        ConsentAutoRegistrationDescriptor descriptor,
        IOptions<ConsentOptions> options,
        ILogger<ConsentAutoRegistrationHostedService> logger)
    {
        _descriptor = descriptor;
        _options = options.Value;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.AutoRegisterFromAttributes || _descriptor.Assemblies.Count == 0)
        {
            _logger.ConsentAutoRegistrationSkipped();
            return Task.CompletedTask;
        }

        var discoveredPurposes = DiscoverPurposesFromAssemblies(_descriptor.Assemblies);
        var unknownPurposes = ValidatePurposes(discoveredPurposes);

        _logger.ConsentAutoRegistrationCompleted(discoveredPurposes.Count, _descriptor.Assemblies.Count);

        if (unknownPurposes.Count > 0 && _options.FailOnUnknownPurpose)
        {
            _logger.ConsentAutoRegistrationFailed(unknownPurposes.Count);

            throw new InvalidOperationException(
                $"Consent auto-registration failed: {unknownPurposes.Count} unknown purpose(s) found " +
                $"in [RequireConsent] attributes: [{string.Join(", ", unknownPurposes)}]. " +
                "Either add them to ConsentOptions.PurposeDefinitions or set FailOnUnknownPurpose to false.");
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Scans assemblies for types decorated with <see cref="RequireConsentAttribute"/>
    /// and extracts all unique purposes.
    /// </summary>
    private static Dictionary<string, List<string>> DiscoverPurposesFromAssemblies(IReadOnlyList<Assembly> assemblies)
    {
        // Maps purpose -> list of request type names that reference it
        var purposeToTypes = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        foreach (var assembly in assemblies)
        {
            foreach (var type in assembly.GetTypes())
            {
                var attribute = type.GetCustomAttribute<RequireConsentAttribute>();
                if (attribute is null)
                {
                    continue;
                }

                foreach (var purpose in attribute.Purposes)
                {
                    if (!purposeToTypes.TryGetValue(purpose, out var typeList))
                    {
                        typeList = [];
                        purposeToTypes[purpose] = typeList;
                    }

                    typeList.Add(type.FullName ?? type.Name);
                }
            }
        }

        return purposeToTypes;
    }

    /// <summary>
    /// Validates discovered purposes against <see cref="ConsentOptions.PurposeDefinitions"/>.
    /// Logs warnings for unknown purposes.
    /// </summary>
    /// <returns>The list of unknown purpose identifiers.</returns>
    private List<string> ValidatePurposes(Dictionary<string, List<string>> discoveredPurposes)
    {
        var unknownPurposes = new List<string>();

        // If no purpose definitions are configured, skip validation
        if (_options.PurposeDefinitions.Count == 0)
        {
            return unknownPurposes;
        }

        foreach (var (purpose, requestTypes) in discoveredPurposes)
        {
            if (_options.PurposeDefinitions.Contains(purpose))
            {
                continue;
            }

            unknownPurposes.Add(purpose);

            foreach (var requestType in requestTypes)
            {
                _logger.UnknownConsentPurposeDetected(purpose, requestType);
            }
        }

        return unknownPurposes;
    }
}
