using Encina.Compliance.BreachNotification.Abstractions;
using Encina.Compliance.BreachNotification.Model;
using Encina.Compliance.NIS2.Abstractions;
using Encina.Compliance.NIS2.Diagnostics;
using Encina.Compliance.NIS2.Model;

using LanguageExt;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using static LanguageExt.Prelude;

namespace Encina.Compliance.NIS2;

/// <summary>
/// Default implementation of <see cref="INIS2IncidentHandler"/> providing stateless
/// timeline validation for NIS2 incident notification obligations (Art. 23).
/// </summary>
/// <remarks>
/// <para>
/// Records OpenTelemetry metrics for incident reports, deadline checks, and notification phases.
/// </para>
/// <para>
/// If <see cref="IBreachNotificationService"/> from <c>Encina.Compliance.BreachNotification</c>
/// is registered in the DI container, incident reports are also forwarded to it for
/// persistent event-sourced lifecycle tracking. This bridges NIS2 Art. 23 incident notification
/// with GDPR Art. 33 breach notification, enabling unified compliance tracking.
/// </para>
/// </remarks>
internal sealed class DefaultNIS2IncidentHandler : INIS2IncidentHandler
{
    private readonly IOptions<NIS2Options> _options;
    private readonly TimeProvider _timeProvider;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DefaultNIS2IncidentHandler> _logger;

    public DefaultNIS2IncidentHandler(
        IOptions<NIS2Options> options,
        TimeProvider timeProvider,
        IServiceProvider serviceProvider,
        ILogger<DefaultNIS2IncidentHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options;
        _timeProvider = timeProvider;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> ReportIncidentAsync(
        NIS2Incident incident,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(incident);

        var incidentIdStr = incident.Id.ToString();
        var severityName = incident.Severity.ToString();

        using var activity = NIS2Diagnostics.StartIncidentReport(incidentIdStr, severityName);

        try
        {
            var now = _timeProvider.GetUtcNow();

            if (incident.IsSignificant)
            {
                var earlyWarningDeadline = incident.EarlyWarningDeadlineUtc;
                if (now > earlyWarningDeadline)
                {
                    var hoursOverdue = (now - earlyWarningDeadline).TotalHours;
                    _logger.IncidentDeadlineExceeded(incidentIdStr,
                        NIS2NotificationPhase.EarlyWarning.ToString(), hoursOverdue);
                }
            }

            // Forward to BreachNotificationService for persistent event-sourced tracking
            // if available — bridges NIS2 Art. 23 with GDPR Art. 33
            await ForwardToBreachNotificationAsync(incident, cancellationToken).ConfigureAwait(false);

            // Record metrics
            NIS2Diagnostics.IncidentReportsTotal.Add(1,
                new KeyValuePair<string, object?>(NIS2Diagnostics.TagIncidentSeverity, severityName),
                new KeyValuePair<string, object?>(NIS2Diagnostics.TagOutcome, "reported"));

            _logger.IncidentReported(incidentIdStr, severityName, incident.IsSignificant);
            NIS2Diagnostics.RecordCompleted(activity);

            return Right<EncinaError, Unit>(Unit.Default);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.IncidentHandlingError(incidentIdStr, ex);
            NIS2Diagnostics.RecordFailed(activity, ex.Message);

            NIS2Diagnostics.IncidentReportsTotal.Add(1,
                new KeyValuePair<string, object?>(NIS2Diagnostics.TagIncidentSeverity, severityName),
                new KeyValuePair<string, object?>(NIS2Diagnostics.TagOutcome, "error"));

            return NIS2Errors.IncidentReportFailed(incident.Id, ex.Message, ex);
        }
    }

    /// <summary>
    /// Awaited forwarding to <see cref="IBreachNotificationService"/> for persistent
    /// event-sourced breach lifecycle tracking. Uses resilience protection for the
    /// external call. Never fails the NIS2 incident pipeline.
    /// </summary>
    private async ValueTask ForwardToBreachNotificationAsync(
        NIS2Incident incident,
        CancellationToken cancellationToken)
    {
        var breachService = _serviceProvider.GetService<IBreachNotificationService>();
        if (breachService is null)
        {
            return;
        }

        var timeout = _options.Value.ExternalCallTimeout;
        var incidentIdStr = incident.Id.ToString();

        await NIS2ResilienceHelper.ExecuteAsync(
            _serviceProvider,
            async ct =>
            {
                var breachSeverity = incident.Severity switch
                {
                    NIS2IncidentSeverity.Critical => BreachSeverity.Critical,
                    NIS2IncidentSeverity.High => BreachSeverity.High,
                    NIS2IncidentSeverity.Medium => BreachSeverity.Medium,
                    _ => BreachSeverity.Low
                };

                var result = await breachService.RecordBreachAsync(
                    nature: $"NIS2 Incident: {incident.Description}",
                    severity: breachSeverity,
                    detectedByRule: "NIS2IncidentHandler",
                    estimatedAffectedSubjects: 0,
                    description: $"NIS2 incident '{incidentIdStr}' — {incident.InitialAssessment}. "
                        + $"Affected services: {string.Join(", ", incident.AffectedServices)}.",
                    cancellationToken: ct).ConfigureAwait(false);

                _ = result.Match(
                    Right: breachId =>
                    {
                        _logger.IncidentForwardedToBreachNotification(incidentIdStr, breachId.ToString());
                        return Unit.Default;
                    },
                    Left: error =>
                    {
                        _logger.IncidentBreachForwardingFailed(incidentIdStr, error.Message);
                        return Unit.Default;
                    });
            },
            timeout,
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, bool>> IsWithinNotificationDeadlineAsync(
        NIS2Incident incident,
        NIS2NotificationPhase phase,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(incident);

        var incidentIdStr = incident.Id.ToString();
        var phaseName = phase.ToString();

        try
        {
            var now = _timeProvider.GetUtcNow();
            var deadline = GetDeadlineForPhase(incident, phase);

            if (deadline is null)
            {
                NIS2Diagnostics.DeadlineChecksTotal.Add(1,
                    new KeyValuePair<string, object?>(NIS2Diagnostics.TagNotificationPhase, phaseName),
                    new KeyValuePair<string, object?>(NIS2Diagnostics.TagOutcome, "no_deadline"));

                return ValueTask.FromResult(Right<EncinaError, bool>(false));
            }

            var isWithin = now <= deadline.Value;

            NIS2Diagnostics.DeadlineChecksTotal.Add(1,
                new KeyValuePair<string, object?>(NIS2Diagnostics.TagNotificationPhase, phaseName),
                new KeyValuePair<string, object?>(NIS2Diagnostics.TagOutcome, isWithin ? "within" : "exceeded"));

            _logger.IncidentDeadlineChecked(incidentIdStr, phaseName, isWithin);

            if (!isWithin)
            {
                var hoursOverdue = (now - deadline.Value).TotalHours;
                _logger.IncidentDeadlineExceeded(incidentIdStr, phaseName, hoursOverdue);
            }

            return ValueTask.FromResult(Right<EncinaError, bool>(isWithin));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.IncidentHandlingError(incidentIdStr, ex);

            return ValueTask.FromResult<Either<EncinaError, bool>>(
                NIS2Errors.IncidentReportFailed(incident.Id, ex.Message, ex));
        }
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, (NIS2NotificationPhase Phase, DateTimeOffset Deadline)>> GetNextDeadlineAsync(
        NIS2Incident incident,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(incident);

        var incidentIdStr = incident.Id.ToString();

        // Check phases in order: EarlyWarning → IncidentNotification → FinalReport
        if (incident.EarlyWarningAtUtc is null)
        {
            _logger.IncidentNextDeadline(incidentIdStr,
                NIS2NotificationPhase.EarlyWarning.ToString(),
                incident.EarlyWarningDeadlineUtc.ToString("O"));

            return ValueTask.FromResult(
                Right<EncinaError, (NIS2NotificationPhase, DateTimeOffset)>(
                    (NIS2NotificationPhase.EarlyWarning, incident.EarlyWarningDeadlineUtc)));
        }

        if (incident.IncidentNotificationAtUtc is null)
        {
            _logger.IncidentNextDeadline(incidentIdStr,
                NIS2NotificationPhase.IncidentNotification.ToString(),
                incident.IncidentNotificationDeadlineUtc.ToString("O"));

            return ValueTask.FromResult(
                Right<EncinaError, (NIS2NotificationPhase, DateTimeOffset)>(
                    (NIS2NotificationPhase.IncidentNotification, incident.IncidentNotificationDeadlineUtc)));
        }

        if (incident.FinalReportAtUtc is null && incident.FinalReportDeadlineUtc is not null)
        {
            _logger.IncidentNextDeadline(incidentIdStr,
                NIS2NotificationPhase.FinalReport.ToString(),
                incident.FinalReportDeadlineUtc.Value.ToString("O"));

            return ValueTask.FromResult(
                Right<EncinaError, (NIS2NotificationPhase, DateTimeOffset)>(
                    (NIS2NotificationPhase.FinalReport, incident.FinalReportDeadlineUtc.Value)));
        }

        _logger.IncidentAllPhasesComplete(incidentIdStr);

        return ValueTask.FromResult<Either<EncinaError, (NIS2NotificationPhase, DateTimeOffset)>>(
            NIS2Errors.AllPhasesComplete(incident.Id));
    }

    private static DateTimeOffset? GetDeadlineForPhase(NIS2Incident incident, NIS2NotificationPhase phase) =>
        phase switch
        {
            NIS2NotificationPhase.EarlyWarning => incident.EarlyWarningDeadlineUtc,
            NIS2NotificationPhase.IncidentNotification => incident.IncidentNotificationDeadlineUtc,
            NIS2NotificationPhase.FinalReport => incident.FinalReportDeadlineUtc,
            NIS2NotificationPhase.IntermediateReport => null, // On-demand, no fixed deadline
            _ => null
        };
}
