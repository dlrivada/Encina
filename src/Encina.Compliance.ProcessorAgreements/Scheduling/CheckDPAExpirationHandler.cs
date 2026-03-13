using Encina.Compliance.ProcessorAgreements.Diagnostics;
using Encina.Compliance.ProcessorAgreements.Model;
using Encina.Compliance.ProcessorAgreements.Notifications;

using LanguageExt;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using static LanguageExt.Prelude;

namespace Encina.Compliance.ProcessorAgreements.Scheduling;

/// <summary>
/// Handles the <see cref="CheckDPAExpirationCommand"/> by querying for expiring and expired
/// Data Processing Agreements and publishing notifications for each.
/// </summary>
/// <remarks>
/// <para>
/// This handler is invoked by the Encina.Scheduling infrastructure at the interval configured in
/// <see cref="ProcessorAgreementOptions.ExpirationCheckInterval"/>. It performs two queries:
/// </para>
/// <list type="number">
/// <item><description><b>Expiring DPAs</b>: Active agreements whose <c>ExpiresAtUtc</c> falls within
/// <see cref="ProcessorAgreementOptions.ExpirationWarningDays"/> from now. Publishes
/// <see cref="DPAExpiringNotification"/> for each.</description></item>
/// <item><description><b>Expired DPAs</b>: Active agreements whose <c>ExpiresAtUtc</c> has already passed.
/// Transitions their status to <see cref="DPAStatus.Expired"/> and publishes
/// <see cref="DPAExpiredNotification"/> for each.</description></item>
/// </list>
/// <para>
/// The handler resolves processor names via <see cref="IProcessorRegistry"/> to include
/// human-readable context in published notifications.
/// </para>
/// <para>
/// When <see cref="ProcessorAgreementOptions.TrackAuditTrail"/> is enabled, audit entries
/// are recorded for each DPA status transition via <see cref="IProcessorAuditStore"/>.
/// Audit failures are non-blocking per the DPIA pattern.
/// </para>
/// </remarks>
public sealed class CheckDPAExpirationHandler : ICommandHandler<CheckDPAExpirationCommand, Unit>
{
    private readonly IDPAStore _dpaStore;
    private readonly IProcessorRegistry _processorRegistry;
    private readonly IProcessorAuditStore _auditStore;
    private readonly IEncina _encina;
    private readonly ProcessorAgreementOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<CheckDPAExpirationHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CheckDPAExpirationHandler"/> class.
    /// </summary>
    /// <param name="dpaStore">The DPA store for querying and updating agreements.</param>
    /// <param name="processorRegistry">The processor registry for resolving processor names.</param>
    /// <param name="auditStore">The audit store for recording expiration actions.</param>
    /// <param name="encina">The Encina mediator for publishing notifications.</param>
    /// <param name="options">Configuration options controlling warning thresholds.</param>
    /// <param name="timeProvider">Time provider for deterministic time access.</param>
    /// <param name="logger">Logger for structured diagnostic output.</param>
    public CheckDPAExpirationHandler(
        IDPAStore dpaStore,
        IProcessorRegistry processorRegistry,
        IProcessorAuditStore auditStore,
        IEncina encina,
        IOptions<ProcessorAgreementOptions> options,
        TimeProvider timeProvider,
        ILogger<CheckDPAExpirationHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(dpaStore);
        ArgumentNullException.ThrowIfNull(processorRegistry);
        ArgumentNullException.ThrowIfNull(auditStore);
        ArgumentNullException.ThrowIfNull(encina);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _dpaStore = dpaStore;
        _processorRegistry = processorRegistry;
        _auditStore = auditStore;
        _encina = encina;
        _options = options.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> Handle(
        CheckDPAExpirationCommand request,
        CancellationToken cancellationToken)
    {
        var startedAt = System.Diagnostics.Stopwatch.GetTimestamp();
        using var activity = ProcessorAgreementDiagnostics.StartExpirationCheck();

        var nowUtc = _timeProvider.GetUtcNow();
        var warningThreshold = nowUtc.AddDays(_options.ExpirationWarningDays);

        _logger.ExpirationCheckStarted(_options.ExpirationWarningDays, warningThreshold);

        // Step 1: Check for agreements approaching expiration (still active, expiring within threshold)
        var expiringResult = await _dpaStore
            .GetExpiringAsync(warningThreshold, cancellationToken)
            .ConfigureAwait(false);

        if (expiringResult.IsLeft)
        {
            var error = expiringResult.Match(Left: e => e, Right: _ => default!);
            _logger.ExpirationCheckError("GetExpiring", error.Message);
            ProcessorAgreementDiagnostics.RecordError(activity, "GetExpiring");
            RecordExpirationMetrics(startedAt);
            return Left<EncinaError, Unit>(error);
        }

        var expiringAgreements = expiringResult.Match(
            Right: agreements => agreements,
            Left: _ => (IReadOnlyList<DataProcessingAgreement>)[]);

        // Separate truly expired from merely approaching expiration
        var expiredAgreements = new List<DataProcessingAgreement>();
        var approachingAgreements = new List<DataProcessingAgreement>();

        foreach (var agreement in expiringAgreements)
        {
            if (agreement.ExpiresAtUtc is not null && agreement.ExpiresAtUtc <= nowUtc)
            {
                expiredAgreements.Add(agreement);
            }
            else
            {
                approachingAgreements.Add(agreement);
            }
        }

        // Step 2: Process expired agreements — update status and publish notifications
        foreach (var expired in expiredAgreements)
        {
            var processorName = await ResolveProcessorNameAsync(expired.ProcessorId, cancellationToken)
                .ConfigureAwait(false);

            // Transition status to Expired
            var updatedAgreement = expired with
            {
                Status = DPAStatus.Expired,
                LastUpdatedAtUtc = nowUtc
            };

            var updateResult = await _dpaStore
                .UpdateAsync(updatedAgreement, cancellationToken)
                .ConfigureAwait(false);

            if (updateResult.IsLeft)
            {
                var updateError = updateResult.Match(Left: e => e, Right: _ => default!);
                _logger.ExpirationCheckError("UpdateExpired", updateError.Message);
                continue;
            }

            _logger.DPAExpiredDetected(expired.ProcessorId, expired.Id, expired.ExpiresAtUtc!.Value);

            // Record audit entry for DPA expiration
            await RecordAuditAsync(
                expired.ProcessorId,
                expired.Id,
                "DPAExpired",
                $"DPA '{expired.Id}' for processor '{processorName}' expired at {expired.ExpiresAtUtc.Value:O}.",
                expired.TenantId,
                expired.ModuleId,
                nowUtc,
                cancellationToken).ConfigureAwait(false);

            var notification = new DPAExpiredNotification(
                expired.ProcessorId,
                expired.Id,
                processorName,
                expired.ExpiresAtUtc!.Value,
                nowUtc);

            await _encina.Publish(notification, cancellationToken).ConfigureAwait(false);
        }

        // Step 3: Process approaching agreements — publish warning notifications
        foreach (var approaching in approachingAgreements)
        {
            var processorName = await ResolveProcessorNameAsync(approaching.ProcessorId, cancellationToken)
                .ConfigureAwait(false);

            var daysUntilExpiration = (int)(approaching.ExpiresAtUtc!.Value - nowUtc).TotalDays;

            _logger.DPAExpiringDetected(approaching.ProcessorId, approaching.Id, daysUntilExpiration);

            // Record audit entry for DPA expiring warning
            await RecordAuditAsync(
                approaching.ProcessorId,
                approaching.Id,
                "DPAExpiring",
                $"DPA '{approaching.Id}' for processor '{processorName}' expires in {daysUntilExpiration} day(s).",
                approaching.TenantId,
                approaching.ModuleId,
                nowUtc,
                cancellationToken).ConfigureAwait(false);

            var notification = new DPAExpiringNotification(
                approaching.ProcessorId,
                approaching.Id,
                processorName,
                approaching.ExpiresAtUtc!.Value,
                daysUntilExpiration,
                nowUtc);

            await _encina.Publish(notification, cancellationToken).ConfigureAwait(false);
        }

        _logger.ExpirationCheckCompleted(expiredAgreements.Count, approachingAgreements.Count);

        ProcessorAgreementDiagnostics.RecordCompleted(activity);
        RecordExpirationMetrics(startedAt);

        return Right<EncinaError, Unit>(unit);
    }

    private static void RecordExpirationMetrics(long startedAt)
    {
        var elapsed = System.Diagnostics.Stopwatch.GetElapsedTime(startedAt);
        ProcessorAgreementDiagnostics.ExpirationCheckTotal.Add(1);
        ProcessorAgreementDiagnostics.ExpirationCheckDuration.Record(elapsed.TotalMilliseconds);
    }

    private async ValueTask<string> ResolveProcessorNameAsync(
        string processorId,
        CancellationToken cancellationToken)
    {
        var processorResult = await _processorRegistry
            .GetProcessorAsync(processorId, cancellationToken)
            .ConfigureAwait(false);

        return processorResult.Match(
            Right: option => option.Match(
                Some: p => p.Name,
                None: () => processorId),
            Left: _ => processorId);
    }

    private async ValueTask RecordAuditAsync(
        string processorId,
        string dpaId,
        string action,
        string detail,
        string? tenantId,
        string? moduleId,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        if (!_options.TrackAuditTrail)
        {
            return;
        }

        var auditEntry = new ProcessorAgreementAuditEntry
        {
            Id = Guid.NewGuid().ToString(),
            ProcessorId = processorId,
            DPAId = dpaId,
            Action = action,
            Detail = detail,
            PerformedByUserId = "System",
            OccurredAtUtc = nowUtc,
            TenantId = tenantId,
            ModuleId = moduleId
        };

        try
        {
            await _auditStore.RecordAsync(auditEntry, cancellationToken).ConfigureAwait(false);
            _logger.AuditEntryRecorded(processorId, action, "System");
        }
        catch (Exception ex)
        {
            _logger.AuditEntryFailed(processorId, action, ex);
            // Non-blocking: audit failure does not prevent the primary operation
        }
    }
}
