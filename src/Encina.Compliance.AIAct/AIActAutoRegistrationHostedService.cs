using System.Reflection;

using Encina.Compliance.AIAct.Abstractions;
using Encina.Compliance.AIAct.Attributes;
using Encina.Compliance.AIAct.Model;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Encina.Compliance.AIAct;

/// <summary>
/// Hosted service that auto-registers AI systems from assembly attributes at startup.
/// </summary>
/// <remarks>
/// <para>
/// Scans the assemblies specified in <see cref="AIActAutoRegistrationDescriptor"/> for types
/// decorated with <see cref="HighRiskAIAttribute"/> and registers them in the
/// <see cref="IAISystemRegistry"/>. Types with <see cref="RequireHumanOversightAttribute"/>
/// or <see cref="AITransparencyAttribute"/> are noted but only registered if they also
/// carry <see cref="HighRiskAIAttribute"/> (the primary registration driver).
/// </para>
/// <para>
/// For types with <see cref="HighRiskAIAttribute"/>, the service creates an
/// <see cref="AISystemRegistration"/> with the attribute's metadata and calls
/// <see cref="IAISystemRegistry.RegisterSystemAsync"/>. Registration errors
/// (e.g., duplicate system IDs) are logged at debug level and do not prevent
/// other systems from being registered.
/// </para>
/// </remarks>
internal sealed class AIActAutoRegistrationHostedService : IHostedService
{
    private readonly IAISystemRegistry _registry;
    private readonly AIActAutoRegistrationDescriptor _descriptor;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AIActAutoRegistrationHostedService> _logger;

    public AIActAutoRegistrationHostedService(
        IAISystemRegistry registry,
        AIActAutoRegistrationDescriptor descriptor,
        TimeProvider timeProvider,
        ILogger<AIActAutoRegistrationHostedService> logger)
    {
        _registry = registry;
        _descriptor = descriptor;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var registered = 0;
        var skipped = 0;
        var now = _timeProvider.GetUtcNow();

        foreach (var assembly in _descriptor.Assemblies)
        {
            foreach (var type in assembly.GetTypes())
            {
                var highRiskAttr = type.GetCustomAttribute<HighRiskAIAttribute>();
                if (highRiskAttr is null)
                {
                    continue;
                }

                var systemId = highRiskAttr.SystemId ?? type.FullName ?? type.Name;

                if (_registry.IsRegistered(systemId))
                {
                    _logger.LogDebug(
                        "AI Act auto-registration: system '{SystemId}' already registered, skipping",
                        systemId);
                    skipped++;
                    continue;
                }

                var registration = new AISystemRegistration
                {
                    SystemId = systemId,
                    Name = type.Name,
                    Category = highRiskAttr.Category,
                    RiskLevel = AIRiskLevel.HighRisk,
                    Provider = highRiskAttr.Provider,
                    Version = highRiskAttr.Version,
                    Description = highRiskAttr.Description,
                    RegisteredAtUtc = now
                };

                var result = await _registry.RegisterSystemAsync(registration, cancellationToken);

                result.Match(
                    Right: _ => registered++,
                    Left: error => _logger.LogDebug(
                        "AI Act auto-registration: failed to register '{SystemId}': {Error}",
                        systemId,
                        error.Message));
            }
        }

        _logger.LogInformation(
            "AI Act auto-registration completed. SystemsRegistered={SystemsRegistered}, SystemsSkipped={SystemsSkipped}, AssembliesScanned={AssembliesScanned}",
            registered,
            skipped,
            _descriptor.Assemblies.Count);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
