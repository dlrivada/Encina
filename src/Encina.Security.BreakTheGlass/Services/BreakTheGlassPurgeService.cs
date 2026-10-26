using Encina.Diagnostics;
using Encina.Security.BreakTheGlass.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Security.BreakTheGlass;

/// <summary>
/// Deletes closed and reviewed elevations older than <see cref="BreakTheGlassOptions.RetentionDays"/>.
/// </summary>
/// <remarks>
/// <para>
/// Opt-in: <c>AddEncinaBreakTheGlass</c> always registers this service, and at start it reads the built
/// <see cref="BreakTheGlassOptions"/>; when <see cref="BreakTheGlassOptions.EnableAutoPurge"/> is
/// <see langword="false"/> it logs that it is disabled and exits. Deleting review evidence is
/// irreversible and the justification can hold health data, so the controller enables the purge once its
/// retention policy is fixed (a required production setting).
/// </para>
/// <para>
/// It runs on its own cadence, <see cref="BreakTheGlassOptions.PurgeIntervalHours"/> (default 24 hours),
/// separate from the expiry sweep. The store is resolved from a scope of its own per run. A failure is
/// logged and counted and the purge is retried at the next interval.
/// </para>
/// </remarks>
internal sealed class BreakTheGlassPurgeService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<BreakTheGlassOptions> _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<BreakTheGlassPurgeService> _logger;

    public BreakTheGlassPurgeService(
        IServiceScopeFactory scopeFactory,
        IOptions<BreakTheGlassOptions> options,
        TimeProvider timeProvider,
        ILogger<BreakTheGlassPurgeService> logger)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);
        _scopeFactory = scopeFactory;
        _options = options;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = _options.Value;
        if (!options.EnableAutoPurge)
        {
            BreakTheGlassLogMessages.PurgeDisabled(_logger);
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromHours(options.PurgeIntervalHours), _timeProvider);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
            {
                await PurgeOnceAsync(stoppingToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // The host is stopping.
        }
    }

    /// <summary>
    /// Runs one purge.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the purge.</param>
    /// <returns>The number of rows deleted, or <see langword="null"/> when the purge failed.</returns>
    internal async Task<int?> PurgeOnceAsync(CancellationToken cancellationToken)
    {
        var options = _options.Value;
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var store = scope.ServiceProvider.GetRequiredService<IBreakTheGlassStore>();
            var cutoff = _timeProvider.GetUtcNow().AddDays(-options.RetentionDays);

            var purged = await store.PurgeAsync(cutoff, cancellationToken).ConfigureAwait(false);
            return purged.Match<int?>(
                Right: count =>
                {
                    BreakTheGlassLogMessages.PurgeCompleted(_logger, count, options.RetentionDays);
                    return count;
                },
                Left: error =>
                {
                    BreakTheGlassLogMessages.PurgeFailed(_logger, error.GetCode().IfNone("encina.unknown"));
                    BreakTheGlassDiagnostics.RecordStoreFailure(BreakTheGlassDiagnostics.OperationPurge);
                    return null;
                });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            BreakTheGlassLogMessages.PurgeThrew(_logger, ex.ForLogging());
            BreakTheGlassDiagnostics.RecordStoreFailure(BreakTheGlassDiagnostics.OperationPurge);
            return null;
        }
    }
}
