using Encina.Compliance.ProcessorAgreements.Abstractions;
using Encina.Compliance.ProcessorAgreements.Aggregates;
using Encina.Compliance.ProcessorAgreements.Diagnostics;
using Encina.Compliance.ProcessorAgreements.Model;
using Encina.Compliance.ProcessorAgreements.Notifications;
using Encina.Compliance.ProcessorAgreements.ReadModels;
using Encina.Marten;

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
/// Transitions their status to <see cref="DPAStatus.Expired"/> via the event-sourced aggregate and publishes
/// <see cref="DPAExpiredNotification"/> for each.</description></item>
/// </list>
/// <para>
/// The handler resolves processor names via <see cref="IProcessorService"/> to include
/// human-readable context in published notifications.
/// </para>
/// <para>
/// Audit trail is maintained automatically through the event stream — each <c>DPAExpired</c> event
/// is persisted in the Marten event store, providing a full accountability trail per GDPR Article 5(2).
/// </para>
/// </remarks>
public sealed class CheckDPAExpirationHandler : ICommandHandler<CheckDPAExpirationCommand, Unit>
{
    private readonly IDPAService _dpaService;
    private readonly IProcessorService _processorService;
    private readonly IAggregateRepository<DPAAggregate> _dpaRepository;
    private readonly IEncina _encina;
    private readonly ProcessorAgreementOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<CheckDPAExpirationHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CheckDPAExpirationHandler"/> class.
    /// </summary>
    /// <param name="dpaService">The DPA service for querying agreements.</param>
    /// <param name="processorService">The processor service for resolving processor names.</param>
    /// <param name="dpaRepository">The aggregate repository for DPA status transitions.</param>
    /// <param name="encina">The Encina mediator for publishing notifications.</param>
    /// <param name="options">Configuration options controlling warning thresholds.</param>
    /// <param name="timeProvider">Time provider for deterministic time access.</param>
    /// <param name="logger">Logger for structured diagnostic output.</param>
    public CheckDPAExpirationHandler(
        IDPAService dpaService,
        IProcessorService processorService,
        IAggregateRepository<DPAAggregate> dpaRepository,
        IEncina encina,
        IOptions<ProcessorAgreementOptions> options,
        TimeProvider timeProvider,
        ILogger<CheckDPAExpirationHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(dpaService);
        ArgumentNullException.ThrowIfNull(processorService);
        ArgumentNullException.ThrowIfNull(dpaRepository);
        ArgumentNullException.ThrowIfNull(encina);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _dpaService = dpaService;
        _processorService = processorService;
        _dpaRepository = dpaRepository;
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

        _logger.ExpirationCheckStarted(_options.ExpirationWarningDays, nowUtc.AddDays(_options.ExpirationWarningDays));

        // Step 1: Query for agreements approaching expiration via the DPA service
        var expiringResult = await _dpaService
            .GetExpiringDPAsAsync(cancellationToken)
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
            Left: _ => (IReadOnlyList<DPAReadModel>)[]);

        // Separate truly expired from merely approaching expiration
        var expiredAgreements = new List<DPAReadModel>();
        var approachingAgreements = new List<DPAReadModel>();

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

        // Step 2: Process expired agreements — transition status via aggregate and publish notifications
        foreach (var expired in expiredAgreements)
        {
            var processorName = await ResolveProcessorNameAsync(expired.ProcessorId, cancellationToken)
                .ConfigureAwait(false);

            // Transition status to Expired via the event-sourced aggregate
            var loadResult = await _dpaRepository.LoadAsync(expired.Id, cancellationToken)
                .ConfigureAwait(false);

            var transitioned = await loadResult.MatchAsync(
                RightAsync: async aggregate =>
                {
                    aggregate.MarkExpired(nowUtc);
                    var saveResult = await _dpaRepository.SaveAsync(aggregate, cancellationToken)
                        .ConfigureAwait(false);
                    return saveResult.IsRight;
                },
                Left: _ => false);

            if (!transitioned)
            {
                _logger.ExpirationCheckError("UpdateExpired", $"Failed to expire DPA '{expired.Id}'.");
                continue;
            }

            _logger.DPAExpiredDetected(expired.ProcessorId.ToString(), expired.Id.ToString(), expired.ExpiresAtUtc!.Value);

            var notification = new DPAExpiredNotification(
                expired.ProcessorId.ToString(),
                expired.Id.ToString(),
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

            _logger.DPAExpiringDetected(approaching.ProcessorId.ToString(), approaching.Id.ToString(), daysUntilExpiration);

            var notification = new DPAExpiringNotification(
                approaching.ProcessorId.ToString(),
                approaching.Id.ToString(),
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
        Guid processorId,
        CancellationToken cancellationToken)
    {
        var processorResult = await _processorService
            .GetProcessorAsync(processorId, cancellationToken)
            .ConfigureAwait(false);

        return processorResult.Match(
            Right: p => p.Name,
            Left: _ => processorId.ToString());
    }
}
