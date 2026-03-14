using Encina.Compliance.PrivacyByDesign.Diagnostics;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Compliance.PrivacyByDesign;

/// <summary>
/// Hosted service that pre-populates the <see cref="IPurposeRegistry"/> with
/// purpose definitions configured via <see cref="PrivacyByDesignOptions.AddPurpose(string, Action{PurposeBuilder})"/>.
/// </summary>
/// <remarks>
/// Runs once at startup and registers all purposes from the options configuration.
/// Each purpose is registered using <see cref="IPurposeRegistry.RegisterPurposeAsync"/>,
/// which is idempotent — duplicate registrations (by name + moduleId) are safely ignored.
/// </remarks>
internal sealed class PurposeRegistrationHostedService : IHostedService
{
    private readonly IPurposeRegistry _registry;
    private readonly PrivacyByDesignOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<PurposeRegistrationHostedService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PurposeRegistrationHostedService"/> class.
    /// </summary>
    public PurposeRegistrationHostedService(
        IPurposeRegistry registry,
        IOptions<PrivacyByDesignOptions> options,
        TimeProvider timeProvider,
        ILogger<PurposeRegistrationHostedService> logger)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _registry = registry;
        _options = options.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var builders = _options.PurposeBuilders;
        if (builders.Count == 0)
        {
            return;
        }

        _logger.PbDPurposeRegistrationStarting(builders.Count);

        var registered = 0;
        var failed = 0;

        foreach (var builder in builders)
        {
            var purpose = builder.Build(_timeProvider);
            var result = await _registry.RegisterPurposeAsync(purpose, cancellationToken)
                .ConfigureAwait(false);

            result.Match(
                Right: _ =>
                {
                    registered++;
                    PrivacyByDesignDiagnostics.PurposeRegistrationsTotal.Add(1);
                    _logger.PbDPurposeRegistered(
                        purpose.Name, purpose.ModuleId ?? "(global)", purpose.PurposeId);
                },
                Left: error =>
                {
                    failed++;
                    PrivacyByDesignDiagnostics.PurposeRegistrationFailuresTotal.Add(1);
                    _logger.PbDPurposeRegistrationFailed(
                        purpose.Name, purpose.ModuleId ?? "(global)", error.Message);
                });
        }

        _logger.PbDPurposeRegistrationCompleted(registered, failed);
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
