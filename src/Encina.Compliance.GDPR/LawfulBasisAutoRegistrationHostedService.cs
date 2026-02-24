using Encina.Compliance.GDPR.Diagnostics;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Encina.Compliance.GDPR;

/// <summary>
/// Hosted service that auto-registers lawful basis from assembly attributes and default bases at startup.
/// </summary>
/// <remarks>
/// <para>
/// This service is registered automatically when <see cref="LawfulBasisOptions.AutoRegisterFromAttributes"/>
/// is <c>true</c> in the <c>AddEncinaLawfulBasis</c> call. At startup, it:
/// </para>
/// <list type="number">
/// <item><description>Scans assemblies from <see cref="LawfulBasisOptions.AssembliesToScan"/> for
/// types decorated with <see cref="LawfulBasisAttribute"/>.</description></item>
/// <item><description>Registers each discovered type in the <see cref="ILawfulBasisRegistry"/>
/// via <see cref="LawfulBasisRegistration.FromAttribute"/>.</description></item>
/// <item><description>Registers programmatic default bases from
/// <see cref="LawfulBasisOptions.DefaultBases"/>.</description></item>
/// <item><description>Logs a summary with EventId 8211.</description></item>
/// </list>
/// </remarks>
internal sealed class LawfulBasisAutoRegistrationHostedService : IHostedService
{
    private readonly ILawfulBasisRegistry _registry;
    private readonly LawfulBasisAutoRegistrationDescriptor _descriptor;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<LawfulBasisAutoRegistrationHostedService> _logger;

    public LawfulBasisAutoRegistrationHostedService(
        ILawfulBasisRegistry registry,
        LawfulBasisAutoRegistrationDescriptor descriptor,
        TimeProvider timeProvider,
        ILogger<LawfulBasisAutoRegistrationHostedService> logger)
    {
        _registry = registry;
        _descriptor = descriptor;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var count = 0;

        // 1. Auto-register from attributes via InMemoryLawfulBasisRegistry's batch method
        if (_registry is InMemoryLawfulBasisRegistry inMemoryRegistry)
        {
            count = inMemoryRegistry.AutoRegisterFromAssemblies(_descriptor.Assemblies, _timeProvider);
        }
        else
        {
            _logger.LawfulBasisAutoRegistrationSkipped(_registry.GetType().Name);
        }

        // 2. Register programmatic default bases
        var defaultCount = 0;
        foreach (var (requestType, basis) in _descriptor.DefaultBases)
        {
            var registration = new LawfulBasisRegistration
            {
                RequestType = requestType,
                Basis = basis,
                Purpose = $"Default basis from {nameof(LawfulBasisOptions)}.{nameof(LawfulBasisOptions.DefaultBases)}",
                RegisteredAtUtc = _timeProvider.GetUtcNow()
            };

            // Await registration — errors are non-fatal (duplicate registrations are silently accepted)
            await _registry.RegisterAsync(registration, cancellationToken).ConfigureAwait(false);
            defaultCount++;
        }

        _logger.LawfulBasisAutoRegistrationCompleted(
            count + defaultCount,
            _descriptor.Assemblies.Count,
            defaultCount);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
