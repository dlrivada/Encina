using Encina.Compliance.GDPR.Diagnostics;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Encina.Compliance.GDPR;

/// <summary>
/// Hosted service that auto-registers processing activities from assembly attributes at startup.
/// </summary>
internal sealed class GDPRAutoRegistrationHostedService : IHostedService
{
    private readonly IProcessingActivityRegistry _registry;
    private readonly GDPRAutoRegistrationDescriptor _descriptor;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<GDPRAutoRegistrationHostedService> _logger;

    public GDPRAutoRegistrationHostedService(
        IProcessingActivityRegistry registry,
        GDPRAutoRegistrationDescriptor descriptor,
        TimeProvider timeProvider,
        ILogger<GDPRAutoRegistrationHostedService> logger)
    {
        _registry = registry;
        _descriptor = descriptor;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_registry is InMemoryProcessingActivityRegistry inMemoryRegistry)
        {
            var count = inMemoryRegistry.AutoRegisterFromAssemblies(_descriptor.Assemblies, _timeProvider);
            _logger.AutoRegistrationCompleted(count, _descriptor.Assemblies.Count);
        }
        else
        {
            _logger.AutoRegistrationSkipped(_registry.GetType().Name);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
