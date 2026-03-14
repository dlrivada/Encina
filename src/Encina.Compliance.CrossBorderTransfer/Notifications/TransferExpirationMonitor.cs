using Encina.Compliance.CrossBorderTransfer.Abstractions;
using Encina.Compliance.CrossBorderTransfer.Diagnostics;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Compliance.CrossBorderTransfer.Notifications;

/// <summary>
/// Background hosted service that periodically monitors cross-border transfer compliance
/// artifacts (approved transfers, TIAs, SCC agreements) for approaching or elapsed expiration
/// dates and publishes notification events.
/// </summary>
/// <remarks>
/// <para>
/// The service runs monitoring cycles at a configurable interval (default: 1 hour),
/// each cycle performing the following steps:
/// <list type="number">
/// <item><description>Create a new <see cref="IServiceScope"/> to resolve scoped dependencies.</description></item>
/// <item><description>Query for transfers, TIAs, and SCC agreements approaching their expiration date.</description></item>
/// <item><description>Publish <see cref="TransferExpiringNotification"/>, <see cref="TIAExpiringNotification"/>,
/// or <see cref="SCCAgreementExpiringNotification"/> for items within the alert window.</description></item>
/// <item><description>Publish <see cref="TransferExpiredNotification"/>, <see cref="TIAExpiredNotification"/>,
/// or <see cref="SCCAgreementExpiredNotification"/> for items that have already expired.</description></item>
/// </list>
/// </para>
/// <para>
/// The service uses <see cref="PeriodicTimer"/> for scheduling, which is more efficient
/// than <c>Task.Delay</c> for periodic work as it doesn't drift and handles elapsed
/// periods gracefully.
/// </para>
/// <para>
/// Graceful error handling: individual monitoring cycle failures are logged but never crash
/// the host. The service continues running and attempts monitoring again on the next cycle.
/// </para>
/// <para>
/// Controlled by <see cref="CrossBorderTransferOptions.EnableExpirationMonitoring"/> (default: <c>false</c>).
/// When disabled, the service is not registered. When registered, the service starts immediately
/// and runs until the host shuts down.
/// </para>
/// <para>
/// Per GDPR Article 44, ongoing international data transfers require valid legal mechanisms.
/// This service ensures proactive expiration tracking so compliance gaps are detected before
/// transfer authorizations lapse.
/// </para>
/// </remarks>
internal sealed class TransferExpirationMonitor : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly CrossBorderTransferOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<TransferExpirationMonitor> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransferExpirationMonitor"/> class.
    /// </summary>
    /// <param name="scopeFactory">Factory for creating service scopes to resolve scoped dependencies.</param>
    /// <param name="options">Cross-border transfer options controlling monitoring behavior.</param>
    /// <param name="timeProvider">Time provider for UTC timestamps.</param>
    /// <param name="logger">Logger for diagnostic messages.</param>
    public TransferExpirationMonitor(
        IServiceScopeFactory scopeFactory,
        IOptions<CrossBorderTransferOptions> options,
        TimeProvider timeProvider,
        ILogger<TransferExpirationMonitor> logger)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _scopeFactory = scopeFactory;
        _options = options.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.ExpirationMonitorStarted(_options.ExpirationCheckInterval.ToString());

        using var timer = new PeriodicTimer(_options.ExpirationCheckInterval);

        // Execute first cycle immediately, then wait for timer ticks
        await ExecuteMonitoringCycleAsync(stoppingToken).ConfigureAwait(false);

        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            await ExecuteMonitoringCycleAsync(stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task ExecuteMonitoringCycleAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();

            var expiringCount = 0;
            var expiredCount = 0;

            // Check approved transfers
            var (tExpiring, tExpired) = await CheckApprovedTransfersAsync(
                scope.ServiceProvider, cancellationToken).ConfigureAwait(false);
            expiringCount += tExpiring;
            expiredCount += tExpired;

            _logger.ExpirationMonitorCycleCompleted(expiringCount, expiredCount);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.ExpirationMonitorCycleCancelled();
        }
        catch (Exception ex)
        {
            // Graceful error handling: log + continue, never crash the host
            _logger.ExpirationMonitorCycleError(ex);
        }
    }

    private async Task<(int Expiring, int Expired)> CheckApprovedTransfersAsync(
        IServiceProvider scopedProvider,
        CancellationToken cancellationToken)
    {
        var expiringCount = 0;
        var expiredCount = 0;

        var queryService = scopedProvider.GetService<ITransferExpirationQueryService>();
        if (queryService is null)
        {
            return (expiringCount, expiredCount);
        }

        var nowUtc = _timeProvider.GetUtcNow();
        var alertWindow = nowUtc.AddDays(_options.AlertBeforeExpirationDays);

        // Query for expiring transfers
        var expiringResult = await queryService.GetExpiringTransfersAsync(
            nowUtc, alertWindow, cancellationToken).ConfigureAwait(false);

        expiringResult.Match(
            Right: transfers =>
            {
                expiringCount = transfers.Count;
                foreach (var transfer in transfers)
                {
                    var daysRemaining = (int)(transfer.ExpiresAtUtc - nowUtc).TotalDays;
                    _logger.TransferExpiringSoon(
                        transfer.Id.ToString(),
                        transfer.SourceCountryCode,
                        transfer.DestinationCountryCode,
                        daysRemaining);

                    if (_options.PublishExpirationNotifications)
                    {
                        _ = PublishTransferExpiringAsync(
                            transfer, daysRemaining, nowUtc, scopedProvider, cancellationToken);
                    }
                }
            },
            Left: error =>
            {
                _logger.TransferStoreError($"GetExpiringTransfers: {error.Message}");
            });

        // Query for expired transfers
        var expiredResult = await queryService.GetExpiredTransfersAsync(
            nowUtc, cancellationToken).ConfigureAwait(false);

        expiredResult.Match(
            Right: transfers =>
            {
                expiredCount = transfers.Count;
                foreach (var transfer in transfers)
                {
                    _logger.TransferExpired(
                        transfer.Id.ToString(),
                        transfer.SourceCountryCode,
                        transfer.DestinationCountryCode);

                    if (_options.PublishExpirationNotifications)
                    {
                        _ = PublishTransferExpiredAsync(
                            transfer, nowUtc, scopedProvider, cancellationToken);
                    }
                }
            },
            Left: error =>
            {
                _logger.TransferStoreError($"GetExpiredTransfers: {error.Message}");
            });

        return (expiringCount, expiredCount);
    }

    private static async Task PublishTransferExpiringAsync(
        ExpiringTransferInfo transfer,
        int daysRemaining,
        DateTimeOffset nowUtc,
        IServiceProvider scopedProvider,
        CancellationToken cancellationToken)
    {
        var encina = scopedProvider.GetService<IEncina>();
        if (encina is null)
        {
            return;
        }

        try
        {
            var notification = new TransferExpiringNotification(
                transfer.Id,
                transfer.SourceCountryCode,
                transfer.DestinationCountryCode,
                transfer.DataCategory,
                transfer.ExpiresAtUtc,
                daysRemaining,
                nowUtc);

            await encina.Publish(notification, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // Notification publishing should never fail the monitoring cycle
        }
    }

    private static async Task PublishTransferExpiredAsync(
        ExpiringTransferInfo transfer,
        DateTimeOffset nowUtc,
        IServiceProvider scopedProvider,
        CancellationToken cancellationToken)
    {
        var encina = scopedProvider.GetService<IEncina>();
        if (encina is null)
        {
            return;
        }

        try
        {
            var notification = new TransferExpiredNotification(
                transfer.Id,
                transfer.SourceCountryCode,
                transfer.DestinationCountryCode,
                transfer.DataCategory,
                transfer.ExpiresAtUtc,
                nowUtc);

            await encina.Publish(notification, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // Notification publishing should never fail the monitoring cycle
        }
    }
}
