using Encina.Security.ABAC.Diagnostics;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Security.ABAC;

/// <summary>
/// Logs once at startup (Warning, EventId 9085) when the final <see cref="ABACOptions"/> disable
/// enforcement, so a host that runs its <see cref="RequirePolicyAttribute"/> and
/// <see cref="RequireConditionAttribute"/> requests without any ABAC evaluation says so.
/// </summary>
/// <remarks>
/// It reads <see cref="IOptions{TOptions}"/> when the host starts, not the options instance seen by
/// <see cref="ServiceCollectionExtensions.AddEncinaABAC"/>, so a later <c>Configure</c> call that
/// changes the mode is honoured. <see cref="ServiceCollectionExtensions.AddEncinaABAC"/> always
/// registers it, once.
/// </remarks>
internal sealed class ABACEnforcementModeStartupCheck : IHostedService
{
    private readonly ABACOptions _options;
    private readonly ILogger<ABACEnforcementModeStartupCheck> _logger;

    public ABACEnforcementModeStartupCheck(
        IOptions<ABACOptions> options,
        ILogger<ABACEnforcementModeStartupCheck> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options.Value;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_options.EnforcementMode == ABACEnforcementMode.Disabled)
        {
            ABACLogMessages.EnforcementDisabled(_logger);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
