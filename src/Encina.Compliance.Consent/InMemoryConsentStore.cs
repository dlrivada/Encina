using System.Collections.Concurrent;

using Encina.Compliance.Consent.Diagnostics;

using LanguageExt;

using Microsoft.Extensions.Logging;

using static LanguageExt.Prelude;

namespace Encina.Compliance.Consent;

/// <summary>
/// In-memory implementation of <see cref="IConsentStore"/> for development, testing, and simple deployments.
/// </summary>
/// <remarks>
/// <para>
/// Consent records are stored in a <see cref="ConcurrentDictionary{TKey,TValue}"/> keyed by
/// a composite key of <c>(SubjectId, Purpose)</c>, ensuring thread-safe concurrent access.
/// </para>
/// <para>
/// The store uses <see cref="TimeProvider"/> for testable time-based operations such as
/// expiration checking. Inject a custom <see cref="TimeProvider"/> in tests to control time.
/// </para>
/// <para>
/// When an <see cref="IEncina"/> instance is provided, the store publishes domain events
/// (<see cref="ConsentGrantedEvent"/>, <see cref="ConsentWithdrawnEvent"/>) after successful
/// store operations. Event publishing is fire-and-forget — failures do not affect the store operation.
/// </para>
/// <para>
/// <b>Not suitable for production</b>: Records are lost when the process restarts.
/// For production use, consider database-backed implementations via one of the 13 supported providers.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var store = new InMemoryConsentStore(TimeProvider.System, logger);
///
/// var consent = new ConsentRecord
/// {
///     Id = Guid.NewGuid(),
///     SubjectId = "user-123",
///     Purpose = ConsentPurposes.Marketing,
///     Status = ConsentStatus.Active,
///     ConsentVersionId = "marketing-v2",
///     GivenAtUtc = DateTimeOffset.UtcNow,
///     Source = "web-form",
///     Metadata = new Dictionary&lt;string, object?&gt;()
/// };
///
/// await store.RecordConsentAsync(consent);
/// </code>
/// </example>
public sealed class InMemoryConsentStore : IConsentStore
{
    private readonly ConcurrentDictionary<(string SubjectId, string Purpose), ConsentRecord> _records = new();
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<InMemoryConsentStore> _logger;
    private readonly IEncina? _encina;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryConsentStore"/> class.
    /// </summary>
    /// <param name="timeProvider">Time provider for expiration checks.</param>
    /// <param name="logger">Logger for structured consent store logging.</param>
    /// <param name="encina">
    /// Optional Encina mediator for publishing domain events.
    /// When <c>null</c>, no events are published (suitable for testing or simple deployments).
    /// </param>
    public InMemoryConsentStore(
        TimeProvider timeProvider,
        ILogger<InMemoryConsentStore> logger,
        IEncina? encina = null)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _timeProvider = timeProvider;
        _logger = logger;
        _encina = encina;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> RecordConsentAsync(
        ConsentRecord consent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(consent);

        var key = (consent.SubjectId, consent.Purpose);
        _records[key] = consent;

        _logger.ConsentRecorded(consent.SubjectId, consent.Purpose);

        await PublishEventAsync(
            new ConsentGrantedEvent(
                consent.SubjectId,
                consent.Purpose,
                _timeProvider.GetUtcNow(),
                consent.ConsentVersionId,
                consent.Source,
                consent.ExpiresAtUtc),
            consent.SubjectId,
            cancellationToken).ConfigureAwait(false);

        return unit;
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Option<ConsentRecord>>> GetConsentAsync(
        string subjectId,
        string purpose,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);

        var key = (subjectId, purpose);

        if (!_records.TryGetValue(key, out var record))
        {
            _logger.ConsentNotFound(subjectId, purpose);
            return ValueTask.FromResult<Either<EncinaError, Option<ConsentRecord>>>(
                Right<EncinaError, Option<ConsentRecord>>(None));
        }

        // Check for expiration at read time
        record = CheckExpiration(record);
        _logger.ConsentFetched(subjectId, purpose, record.Status.ToString());

        return ValueTask.FromResult<Either<EncinaError, Option<ConsentRecord>>>(
            Right<EncinaError, Option<ConsentRecord>>(Some(record)));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, IReadOnlyList<ConsentRecord>>> GetAllConsentsAsync(
        string subjectId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectId);

        IReadOnlyList<ConsentRecord> result = _records.Values
            .Where(r => r.SubjectId == subjectId)
            .Select(CheckExpiration)
            .ToList()
            .AsReadOnly();

        return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<ConsentRecord>>>(Right(result));
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> WithdrawConsentAsync(
        string subjectId,
        string purpose,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);

        var key = (subjectId, purpose);

        if (!_records.TryGetValue(key, out var existing))
        {
            return ConsentErrors.MissingConsent(subjectId, purpose);
        }

        var withdrawnAtUtc = _timeProvider.GetUtcNow();

        var withdrawn = existing with
        {
            Status = ConsentStatus.Withdrawn,
            WithdrawnAtUtc = withdrawnAtUtc
        };

        _records[key] = withdrawn;
        _logger.ConsentWithdrawn(subjectId, purpose);

        await PublishEventAsync(
            new ConsentWithdrawnEvent(
                subjectId,
                purpose,
                withdrawnAtUtc),
            subjectId,
            cancellationToken).ConfigureAwait(false);

        return unit;
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, bool>> HasValidConsentAsync(
        string subjectId,
        string purpose,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);

        var key = (subjectId, purpose);

        if (!_records.TryGetValue(key, out var record))
        {
            return ValueTask.FromResult<Either<EncinaError, bool>>(Right<EncinaError, bool>(false));
        }

        record = CheckExpiration(record);
        var isValid = record.Status == ConsentStatus.Active;

        return ValueTask.FromResult<Either<EncinaError, bool>>(Right<EncinaError, bool>(isValid));
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, BulkOperationResult>> BulkRecordConsentAsync(
        IEnumerable<ConsentRecord> consents,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(consents);

        var consentList = consents as IReadOnlyList<ConsentRecord> ?? consents.ToList();
        var successCount = 0;
        var errors = new List<BulkOperationError>();

        foreach (var consent in consentList)
        {
            try
            {
                var key = (consent.SubjectId, consent.Purpose);
                _records[key] = consent;
                _logger.ConsentRecorded(consent.SubjectId, consent.Purpose);

                await PublishEventAsync(
                    new ConsentGrantedEvent(
                        consent.SubjectId,
                        consent.Purpose,
                        _timeProvider.GetUtcNow(),
                        consent.ConsentVersionId,
                        consent.Source,
                        consent.ExpiresAtUtc),
                    consent.SubjectId,
                    cancellationToken).ConfigureAwait(false);

                successCount++;
            }
            catch (Exception ex)
            {
                var identifier = $"{consent.SubjectId}:{consent.Purpose}";
                errors.Add(new BulkOperationError(
                    identifier,
                    EncinaErrors.Create(
                        code: "consent.bulk_record_failed",
                        message: $"Failed to record consent for {identifier}: {ex.Message}")));
            }
        }

        _logger.BulkConsentRecorded(successCount, errors.Count);

        return errors.Count == 0
            ? BulkOperationResult.Success(successCount)
            : BulkOperationResult.Partial(successCount, errors);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, BulkOperationResult>> BulkWithdrawConsentAsync(
        string subjectId,
        IEnumerable<string> purposes,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectId);
        ArgumentNullException.ThrowIfNull(purposes);

        var purposeList = purposes as IReadOnlyList<string> ?? purposes.ToList();
        var successCount = 0;
        var errors = new List<BulkOperationError>();

        foreach (var purpose in purposeList)
        {
            var key = (subjectId, purpose);

            if (!_records.TryGetValue(key, out var existing))
            {
                errors.Add(new BulkOperationError(
                    $"{subjectId}:{purpose}",
                    ConsentErrors.MissingConsent(subjectId, purpose)));
                continue;
            }

            var withdrawnAtUtc = _timeProvider.GetUtcNow();

            var withdrawn = existing with
            {
                Status = ConsentStatus.Withdrawn,
                WithdrawnAtUtc = withdrawnAtUtc
            };

            _records[key] = withdrawn;
            _logger.ConsentWithdrawn(subjectId, purpose);

            await PublishEventAsync(
                new ConsentWithdrawnEvent(
                    subjectId,
                    purpose,
                    withdrawnAtUtc),
                subjectId,
                cancellationToken).ConfigureAwait(false);

            successCount++;
        }

        _logger.BulkConsentWithdrawn(subjectId, successCount, errors.Count);

        return errors.Count == 0
            ? BulkOperationResult.Success(successCount)
            : BulkOperationResult.Partial(successCount, errors);
    }

    /// <summary>
    /// Gets all consent records in the store.
    /// </summary>
    /// <returns>All stored consent records.</returns>
    /// <remarks>Intended for testing and diagnostics only.</remarks>
    public IReadOnlyList<ConsentRecord> GetAllRecords()
    {
        return _records.Values.ToList();
    }

    /// <summary>
    /// Clears all consent records from the store.
    /// </summary>
    /// <remarks>Intended for testing only to reset state between tests.</remarks>
    public void Clear()
    {
        _records.Clear();
    }

    /// <summary>
    /// Gets the number of consent records in the store.
    /// </summary>
    public int Count => _records.Count;

    private ConsentRecord CheckExpiration(ConsentRecord record)
    {
        if (record.Status == ConsentStatus.Active &&
            record.ExpiresAtUtc.HasValue &&
            _timeProvider.GetUtcNow() >= record.ExpiresAtUtc.Value)
        {
            var expired = record with { Status = ConsentStatus.Expired };
            var key = (record.SubjectId, record.Purpose);
            _records[key] = expired;
            _logger.ConsentExpiredDetected(record.SubjectId, record.Purpose);
            return expired;
        }

        return record;
    }

    /// <summary>
    /// Publishes a domain event via the Encina mediator (fire-and-forget).
    /// </summary>
    /// <remarks>
    /// Event publishing failures are logged as warnings but do not affect the store operation.
    /// When no <see cref="IEncina"/> instance is configured, this method is a no-op.
    /// </remarks>
    private async ValueTask PublishEventAsync<TNotification>(
        TNotification notification,
        string subjectId,
        CancellationToken cancellationToken)
        where TNotification : INotification
    {
        if (_encina is null)
        {
            return;
        }

        var result = await _encina.Publish(notification, cancellationToken).ConfigureAwait(false);

        result.Match(
            Right: _ => _logger.ConsentEventPublished(typeof(TNotification).Name, subjectId),
            Left: error => _logger.ConsentEventPublishFailed(typeof(TNotification).Name, error.Message));
    }
}
