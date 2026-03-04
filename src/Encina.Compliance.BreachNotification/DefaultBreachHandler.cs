using Encina.Compliance.BreachNotification.Model;

using LanguageExt;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using static LanguageExt.Prelude;

namespace Encina.Compliance.BreachNotification;

/// <summary>
/// Default implementation of <see cref="IBreachHandler"/> that orchestrates the full
/// breach notification lifecycle per GDPR Articles 33 and 34.
/// </summary>
/// <remarks>
/// <para>
/// The handler coordinates between <see cref="IBreachRecordStore"/>,
/// <see cref="IBreachAuditStore"/>, <see cref="IBreachNotifier"/>, and the optional
/// <see cref="IEncina"/> publishing infrastructure to implement the complete breach
/// notification workflow.
/// </para>
/// <para>
/// Each method follows a consistent pattern:
/// <list type="number">
/// <item><description>Validate input parameters.</description></item>
/// <item><description>Record an audit entry (started).</description></item>
/// <item><description>Execute the core operation.</description></item>
/// <item><description>Update breach status.</description></item>
/// <item><description>Record an audit entry (completed/failed).</description></item>
/// <item><description>Publish notification if applicable.</description></item>
/// </list>
/// </para>
/// <para>
/// Audit recording and notification publishing are wrapped in try/catch blocks
/// to ensure they never fail the main operation.
/// </para>
/// </remarks>
public sealed class DefaultBreachHandler : IBreachHandler
{
    private readonly IBreachRecordStore _recordStore;
    private readonly IBreachAuditStore _auditStore;
    private readonly IBreachNotifier _notifier;
    private readonly BreachNotificationOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<DefaultBreachHandler> _logger;
    private readonly IEncina? _encina;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultBreachHandler"/> class.
    /// </summary>
    /// <param name="recordStore">Store for breach record persistence.</param>
    /// <param name="auditStore">Store for audit trail entries.</param>
    /// <param name="notifier">Notifier for delivering breach notifications.</param>
    /// <param name="options">Configuration options for the breach notification module.</param>
    /// <param name="serviceProvider">Service provider for resolving optional dependencies.</param>
    /// <param name="timeProvider">Time provider for deterministic time-based operations.</param>
    /// <param name="logger">Logger for diagnostic messages.</param>
    public DefaultBreachHandler(
        IBreachRecordStore recordStore,
        IBreachAuditStore auditStore,
        IBreachNotifier notifier,
        IOptions<BreachNotificationOptions> options,
        IServiceProvider serviceProvider,
        TimeProvider timeProvider,
        ILogger<DefaultBreachHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(recordStore);
        ArgumentNullException.ThrowIfNull(auditStore);
        ArgumentNullException.ThrowIfNull(notifier);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _recordStore = recordStore;
        _auditStore = auditStore;
        _notifier = notifier;
        _options = options.Value;
        _timeProvider = timeProvider;
        _logger = logger;

        _encina = serviceProvider.GetService<IEncina>();
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, BreachRecord>> HandleDetectedBreachAsync(
        PotentialBreach breach,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(breach);

        var detectedAtUtc = breach.SecurityEvent.OccurredAtUtc;

        var record = BreachRecord.Create(
            nature: breach.Description,
            approximateSubjectsAffected: 0,
            categoriesOfDataAffected: [],
            dpoContactDetails: string.Empty,
            likelyConsequences: string.Empty,
            measuresTaken: string.Empty,
            detectedAtUtc: detectedAtUtc,
            severity: breach.Severity);

        _logger.LogInformation(
            "Handling detected breach from rule '{RuleName}' — creating record '{BreachId}' with 72h deadline at {Deadline}",
            breach.DetectionRuleName, record.Id, record.NotificationDeadlineUtc);

        // Persist the breach record
        var storeResult = await _recordStore.RecordBreachAsync(record, cancellationToken);

        return await storeResult.MatchAsync(
            RightAsync: async _ =>
            {
                // Record audit entry
                await RecordAuditAsync(
                    record.Id,
                    "BreachDetected",
                    $"Breach detected by rule '{breach.DetectionRuleName}': {breach.Description}",
                    cancellationToken: cancellationToken);

                // Publish notification
                await PublishNotificationAsync(
                    new BreachDetectedNotification(
                        record.Id,
                        record.Severity,
                        record.Nature,
                        detectedAtUtc),
                    cancellationToken);

                return Right<EncinaError, BreachRecord>(record);
            },
            Left: error =>
            {
                _logger.LogError("Failed to persist breach record '{BreachId}': {Error}", record.Id, error.Message);
                return Left<EncinaError, BreachRecord>(error);
            });
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, NotificationResult>> NotifyAuthorityAsync(
        string breachId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(breachId);

        // Retrieve the breach record
        var getResult = await _recordStore.GetBreachAsync(breachId, cancellationToken);

        return await getResult.MatchAsync(
            RightAsync: async option =>
            {
                if (option.IsNone)
                {
                    return Left<EncinaError, NotificationResult>(
                        BreachNotificationErrors.NotFound(breachId));
                }

                var record = option.Match(r => r, () => default!);

                if (record.Status == BreachStatus.Resolved || record.Status == BreachStatus.Closed)
                {
                    return Left<EncinaError, NotificationResult>(
                        BreachNotificationErrors.AlreadyResolved(breachId));
                }

                await RecordAuditAsync(
                    breachId,
                    "AuthorityNotificationStarted",
                    "Initiating supervisory authority notification per Article 33",
                    cancellationToken: cancellationToken);

                // Delegate notification delivery
                var notifyResult = await _notifier.NotifyAuthorityAsync(record, cancellationToken);

                return await notifyResult.MatchAsync(
                    RightAsync: async result =>
                    {
                        // Update the breach record
                        var now = _timeProvider.GetUtcNow();
                        var updated = record with
                        {
                            NotifiedAuthorityAtUtc = now,
                            Status = BreachStatus.AuthorityNotified
                        };

                        await _recordStore.UpdateBreachAsync(updated, cancellationToken);

                        await RecordAuditAsync(
                            breachId,
                            "AuthorityNotified",
                            $"Supervisory authority notified. Outcome: {result.Outcome}",
                            cancellationToken: cancellationToken);

                        await PublishNotificationAsync(
                            new AuthorityNotifiedNotification(
                                breachId,
                                now,
                                result.Recipient ?? "supervisory-authority"),
                            cancellationToken);

                        return Right<EncinaError, NotificationResult>(result);
                    },
                    Left: error =>
                    {
                        _logger.LogError(
                            "Authority notification failed for breach '{BreachId}': {Error}",
                            breachId, error.Message);
                        return Left<EncinaError, NotificationResult>(error);
                    });
            },
            Left: error => Left<EncinaError, NotificationResult>(error));
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, NotificationResult>> NotifySubjectsAsync(
        string breachId,
        IEnumerable<string> subjectIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(breachId);
        ArgumentNullException.ThrowIfNull(subjectIds);

        var getResult = await _recordStore.GetBreachAsync(breachId, cancellationToken);

        return await getResult.MatchAsync(
            RightAsync: async option =>
            {
                if (option.IsNone)
                {
                    return Left<EncinaError, NotificationResult>(
                        BreachNotificationErrors.NotFound(breachId));
                }

                var record = option.Match(r => r, () => default!);

                if (record.Status == BreachStatus.Resolved || record.Status == BreachStatus.Closed)
                {
                    return Left<EncinaError, NotificationResult>(
                        BreachNotificationErrors.AlreadyResolved(breachId));
                }

                // Check for exemptions per Article 34(3)
                if (record.SubjectNotificationExemption != SubjectNotificationExemption.None)
                {
                    _logger.LogInformation(
                        "Data subject notification for breach '{BreachId}' exempted: {Exemption}",
                        breachId, record.SubjectNotificationExemption);

                    var exemptResult = new NotificationResult
                    {
                        Outcome = NotificationOutcome.Exempted,
                        BreachId = breachId
                    };

                    await RecordAuditAsync(
                        breachId,
                        "SubjectNotificationExempted",
                        $"Exemption applied: {record.SubjectNotificationExemption} per Article 34(3)",
                        cancellationToken: cancellationToken);

                    return Right<EncinaError, NotificationResult>(exemptResult);
                }

                await RecordAuditAsync(
                    breachId,
                    "SubjectNotificationStarted",
                    "Initiating data subject notification per Article 34",
                    cancellationToken: cancellationToken);

                // Delegate notification delivery
                var notifyResult = await _notifier.NotifyDataSubjectsAsync(record, subjectIds, cancellationToken);

                return await notifyResult.MatchAsync(
                    RightAsync: async result =>
                    {
                        var now = _timeProvider.GetUtcNow();
                        var updated = record with
                        {
                            NotifiedSubjectsAtUtc = now,
                            Status = BreachStatus.SubjectsNotified
                        };

                        await _recordStore.UpdateBreachAsync(updated, cancellationToken);

                        var subjectCount = subjectIds.Count();

                        await RecordAuditAsync(
                            breachId,
                            "SubjectsNotified",
                            $"Data subjects notified ({subjectCount} subjects). Outcome: {result.Outcome}",
                            cancellationToken: cancellationToken);

                        await PublishNotificationAsync(
                            new SubjectsNotifiedNotification(breachId, now, subjectCount),
                            cancellationToken);

                        return Right<EncinaError, NotificationResult>(result);
                    },
                    Left: error =>
                    {
                        _logger.LogError(
                            "Subject notification failed for breach '{BreachId}': {Error}",
                            breachId, error.Message);
                        return Left<EncinaError, NotificationResult>(error);
                    });
            },
            Left: error => Left<EncinaError, NotificationResult>(error));
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, PhasedReport>> AddPhasedReportAsync(
        string breachId,
        string content,
        string? userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(breachId);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        var getResult = await _recordStore.GetBreachAsync(breachId, cancellationToken);

        return await getResult.MatchAsync(
            RightAsync: async option =>
            {
                if (option.IsNone)
                {
                    return Left<EncinaError, PhasedReport>(
                        BreachNotificationErrors.NotFound(breachId));
                }

                var record = option.Match(r => r, () => default!);

                if (record.Status == BreachStatus.Resolved || record.Status == BreachStatus.Closed)
                {
                    return Left<EncinaError, PhasedReport>(
                        BreachNotificationErrors.AlreadyResolved(breachId));
                }

                var reportNumber = record.PhasedReports.Count + 1;
                var now = _timeProvider.GetUtcNow();

                var report = PhasedReport.Create(
                    breachId: breachId,
                    reportNumber: reportNumber,
                    content: content,
                    submittedAtUtc: now,
                    submittedByUserId: userId);

                var addResult = await _recordStore.AddPhasedReportAsync(breachId, report, cancellationToken);

                return await addResult.MatchAsync(
                    RightAsync: async _ =>
                    {
                        _logger.LogInformation(
                            "Added phased report #{ReportNumber} to breach '{BreachId}'",
                            reportNumber, breachId);

                        await RecordAuditAsync(
                            breachId,
                            "PhasedReportSubmitted",
                            $"Phased report #{reportNumber} submitted per Article 33(4)",
                            userId,
                            cancellationToken);

                        return Right<EncinaError, PhasedReport>(report);
                    },
                    Left: error =>
                    {
                        _logger.LogError(
                            "Failed to add phased report to breach '{BreachId}': {Error}",
                            breachId, error.Message);
                        return Left<EncinaError, PhasedReport>(
                            BreachNotificationErrors.PhasedReportFailed(breachId, error.Message));
                    });
            },
            Left: error => Left<EncinaError, PhasedReport>(error));
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> ResolveBreachAsync(
        string breachId,
        string resolutionSummary,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(breachId);
        ArgumentException.ThrowIfNullOrWhiteSpace(resolutionSummary);

        var getResult = await _recordStore.GetBreachAsync(breachId, cancellationToken);

        return await getResult.MatchAsync(
            RightAsync: async option =>
            {
                if (option.IsNone)
                {
                    return Left<EncinaError, Unit>(
                        BreachNotificationErrors.NotFound(breachId));
                }

                var record = option.Match(r => r, () => default!);

                if (record.Status == BreachStatus.Resolved || record.Status == BreachStatus.Closed)
                {
                    return Left<EncinaError, Unit>(
                        BreachNotificationErrors.AlreadyResolved(breachId));
                }

                var now = _timeProvider.GetUtcNow();
                var updated = record with
                {
                    Status = BreachStatus.Resolved,
                    ResolvedAtUtc = now,
                    ResolutionSummary = resolutionSummary
                };

                var updateResult = await _recordStore.UpdateBreachAsync(updated, cancellationToken);

                return await updateResult.MatchAsync(
                    RightAsync: async _ =>
                    {
                        _logger.LogInformation("Breach '{BreachId}' resolved", breachId);

                        await RecordAuditAsync(
                            breachId,
                            "BreachResolved",
                            $"Breach resolved: {resolutionSummary}",
                            cancellationToken: cancellationToken);

                        await PublishNotificationAsync(
                            new BreachResolvedNotification(breachId, now, resolutionSummary),
                            cancellationToken);

                        return Right<EncinaError, Unit>(unit);
                    },
                    Left: error =>
                    {
                        _logger.LogError(
                            "Failed to update breach '{BreachId}' to Resolved: {Error}",
                            breachId, error.Message);
                        return Left<EncinaError, Unit>(error);
                    });
            },
            Left: error => Left<EncinaError, Unit>(error));
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, DeadlineStatus>> GetDeadlineStatusAsync(
        string breachId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(breachId);

        var getResult = await _recordStore.GetBreachAsync(breachId, cancellationToken);

        return getResult.Match(
            Right: option => option.Match(
                Some: record =>
                {
                    var now = _timeProvider.GetUtcNow();
                    var remainingHours = (record.NotificationDeadlineUtc - now).TotalHours;

                    var status = new DeadlineStatus
                    {
                        BreachId = breachId,
                        DetectedAtUtc = record.DetectedAtUtc,
                        DeadlineUtc = record.NotificationDeadlineUtc,
                        RemainingHours = remainingHours,
                        IsOverdue = remainingHours < 0,
                        Status = record.Status
                    };

                    return Right<EncinaError, DeadlineStatus>(status);
                },
                None: () => Left<EncinaError, DeadlineStatus>(
                    BreachNotificationErrors.NotFound(breachId))),
            Left: error => Left<EncinaError, DeadlineStatus>(error));
    }

    private async ValueTask RecordAuditAsync(
        string breachId,
        string action,
        string? detail = null,
        string? performedByUserId = null,
        CancellationToken cancellationToken = default)
    {
        if (!_options.TrackAuditTrail)
        {
            return;
        }

        try
        {
            var entry = BreachAuditEntry.Create(
                breachId: breachId,
                action: action,
                detail: detail,
                performedByUserId: performedByUserId);

            await _auditStore.RecordAsync(entry, cancellationToken);
        }
        catch (Exception ex)
        {
            // Audit recording should never fail the main operation
            _logger.LogWarning(ex, "Failed to record audit entry '{Action}' for breach '{BreachId}'", action, breachId);
        }
    }

    private async ValueTask PublishNotificationAsync<TNotification>(
        TNotification notification,
        CancellationToken cancellationToken)
        where TNotification : INotification
    {
        if (_encina is null || !_options.PublishNotifications)
        {
            return;
        }

        try
        {
            await _encina.Publish(notification, cancellationToken);
        }
        catch (Exception ex)
        {
            // Notification publishing should never fail the main operation
            _logger.LogWarning(
                ex,
                "Failed to publish {NotificationType} notification",
                typeof(TNotification).Name);
        }
    }
}
