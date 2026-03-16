using System.Diagnostics;

using Encina.Compliance.DPIA.Abstractions;
using Encina.Compliance.DPIA.Diagnostics;
using Encina.Compliance.DPIA.Model;
using Encina.Compliance.DPIA.ReadModels;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Compliance.DPIA;

/// <summary>
/// Background service that periodically checks for expired DPIA assessments
/// and logs reminders for review.
/// </summary>
/// <remarks>
/// <para>
/// Per GDPR Article 35(11), the controller must review assessments periodically.
/// This service identifies assessments that have exceeded their review period
/// and publishes <see cref="DPIAAssessmentExpired"/> notifications.
/// </para>
/// <para>
/// The service runs review cycles at a configurable interval
/// (<see cref="DPIAOptions.ExpirationCheckInterval"/>, default: 1 hour),
/// using <see cref="PeriodicTimer"/> for efficient scheduling.
/// </para>
/// <para>
/// Graceful error handling: individual cycle failures are logged but never crash
/// the host. The service continues running and attempts the check again on the next cycle.
/// </para>
/// <para>
/// Controlled by <see cref="DPIAOptions.EnableExpirationMonitoring"/> (default: <c>false</c>).
/// </para>
/// </remarks>
internal sealed class DPIAReviewReminderService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly DPIAOptions _options;
    private readonly ILogger<DPIAReviewReminderService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DPIAReviewReminderService"/> class.
    /// </summary>
    /// <param name="scopeFactory">Factory for creating service scopes to resolve scoped dependencies.</param>
    /// <param name="options">DPIA configuration options controlling monitoring behavior.</param>
    /// <param name="logger">Logger for diagnostic messages.</param>
    public DPIAReviewReminderService(
        IServiceScopeFactory scopeFactory,
        IOptions<DPIAOptions> options,
        ILogger<DPIAReviewReminderService> logger)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.EnableExpirationMonitoring)
        {
            _logger.ReviewReminderDisabled();
            return;
        }

        _logger.ReviewReminderStarted(_options.ExpirationCheckInterval);

        using var timer = new PeriodicTimer(_options.ExpirationCheckInterval);

        // Execute first cycle immediately, then wait for timer ticks
        await ExecuteReminderCycleAsync(stoppingToken).ConfigureAwait(false);

        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            await ExecuteReminderCycleAsync(stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task ExecuteReminderCycleAsync(CancellationToken cancellationToken)
    {
        var startTimestamp = Stopwatch.GetTimestamp();
        using var activity = DPIADiagnostics.StartReviewReminderCycle();

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();

            var service = scope.ServiceProvider.GetRequiredService<IDPIAService>();
            var timeProvider = scope.ServiceProvider.GetService<TimeProvider>() ?? TimeProvider.System;
            var nowUtc = timeProvider.GetUtcNow();

            _logger.ReviewReminderCycleStarting();

            var expiredResult = await service
                .GetExpiredAssessmentsAsync(cancellationToken)
                .ConfigureAwait(false);

            expiredResult.Match(
                Right: expired =>
                {
                    if (expired.Count > 0)
                    {
                        _logger.ReviewReminderExpiredFound(expired.Count);

                        foreach (var assessment in expired)
                        {
                            _logger.ReviewReminderAssessmentExpired(
                                assessment.RequestTypeName,
                                assessment.Id,
                                assessment.NextReviewAtUtc);
                        }

                        DPIADiagnostics.ExpiredAssessmentsDetected.Add(expired.Count);

                        // Publish notifications if IEncina is available
                        if (_options.PublishNotifications)
                        {
                            _ = PublishExpiredNotificationsAsync(
                                expired, nowUtc, scope.ServiceProvider, cancellationToken);
                        }
                    }
                    else
                    {
                        _logger.ReviewReminderCycleEmpty();
                    }

                    DPIADiagnostics.RecordPassed(activity);
                    DPIADiagnostics.ReviewReminderCyclesTotal.Add(1,
                        new KeyValuePair<string, object?>(DPIADiagnostics.TagOutcome, "completed"));
                },
                Left: error =>
                {
                    _logger.ReviewReminderCycleFailed(
                        new InvalidOperationException(error.Message));

                    DPIADiagnostics.RecordFailed(activity, error.Message);
                    DPIADiagnostics.ReviewReminderCyclesTotal.Add(1,
                        new KeyValuePair<string, object?>(DPIADiagnostics.TagOutcome, "failed"));
                });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.ReviewReminderCycleCancelled();
            DPIADiagnostics.RecordFailed(activity, "cancelled");
            DPIADiagnostics.ReviewReminderCyclesTotal.Add(1,
                new KeyValuePair<string, object?>(DPIADiagnostics.TagOutcome, "cancelled"));
        }
        catch (Exception ex)
        {
            // Graceful error handling: log + continue, never crash the host
            _logger.ReviewReminderCycleFailed(ex);
            DPIADiagnostics.RecordFailed(activity, ex.Message);
            DPIADiagnostics.ReviewReminderCyclesTotal.Add(1,
                new KeyValuePair<string, object?>(DPIADiagnostics.TagOutcome, "failed"));
        }
    }

    private async Task PublishExpiredNotificationsAsync(
        IReadOnlyList<DPIAReadModel> expiredAssessments,
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
            foreach (var assessment in expiredAssessments)
            {
                var notification = new DPIAAssessmentExpired(
                    assessment.Id,
                    assessment.RequestTypeName,
                    nowUtc);

                await encina.Publish(notification, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            // Notification publishing should never fail the reminder cycle
            _logger.ReviewReminderCycleFailed(ex);
        }
    }
}
