using System.Text.Json;

using Encina.Audit.Marten.Crypto;
using Encina.Audit.Marten.Projections;
using Encina.Security.Audit;

using LanguageExt;

using Marten;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using static LanguageExt.Prelude;

namespace Encina.Audit.Marten;

/// <summary>
/// Event-sourced <see cref="IAuditStore"/> implementation using Marten (PostgreSQL) with temporal
/// crypto-shredding for compliance-grade audit trails.
/// </summary>
/// <remarks>
/// <para>
/// This store appends encrypted audit events to Marten event streams and queries against
/// async-projected read model documents. PII-sensitive fields are encrypted with temporal
/// keys before event persistence. Non-PII structural fields remain in plaintext for querying.
/// </para>
/// <para>
/// <b>Key behavioral differences from database-backed stores:</b>
/// <list type="bullet">
/// <item><b>Immutability</b>: Events are append-only. No UPDATE or DELETE operations.</item>
/// <item><b>Crypto-shredding</b>: <see cref="PurgeEntriesAsync"/> destroys temporal encryption
/// keys instead of deleting entries, rendering PII permanently unreadable while preserving
/// the event stream integrity.</item>
/// <item><b>Eventual consistency</b>: Query results come from async-projected read models,
/// which may lag slightly behind the latest appended events.</item>
/// </list>
/// </para>
/// <para>
/// Stream ID format: <c>"audit:{EntityType}:{EntityId}"</c> when an entity ID is present,
/// or <c>"audit:{EntityType}"</c> for operations without a specific entity target.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Registration
/// services.AddEncinaAuditMarten(options =>
/// {
///     options.TemporalGranularity = TemporalKeyGranularity.Monthly;
///     options.RetentionPeriod = TimeSpan.FromDays(2555); // 7 years (SOX)
/// });
///
/// // Usage (via IAuditStore)
/// var result = await auditStore.RecordAsync(entry, cancellationToken);
///
/// // Crypto-shredding old entries
/// var purged = await auditStore.PurgeEntriesAsync(DateTime.UtcNow.AddYears(-7), cancellationToken);
/// </code>
/// </example>
public sealed class MartenAuditStore : IAuditStore
{
    private static readonly JsonSerializerOptions MetadataJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly IDocumentSession _session;
    private readonly AuditEventEncryptor _encryptor;
    private readonly ITemporalKeyProvider _keyProvider;
    private readonly MartenAuditOptions _options;
    private readonly ILogger<MartenAuditStore> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MartenAuditStore"/> class.
    /// </summary>
    /// <param name="session">The Marten document session for event and document persistence.</param>
    /// <param name="encryptor">The encryptor that maps audit entries to encrypted events.</param>
    /// <param name="keyProvider">The temporal key provider for crypto-shredding operations.</param>
    /// <param name="options">The Marten audit configuration options.</param>
    /// <param name="logger">Logger for structured diagnostic logging.</param>
    public MartenAuditStore(
        IDocumentSession session,
        AuditEventEncryptor encryptor,
        ITemporalKeyProvider keyProvider,
        IOptions<MartenAuditOptions> options,
        ILogger<MartenAuditStore> logger)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(encryptor);
        ArgumentNullException.ThrowIfNull(keyProvider);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _session = session;
        _encryptor = encryptor;
        _keyProvider = keyProvider;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// Encrypts PII fields using a temporal encryption key for the entry's timestamp period,
    /// then appends the encrypted event to a Marten event stream.
    /// </para>
    /// <para>
    /// Stream ID: <c>"audit:{EntityType}:{EntityId}"</c> or <c>"audit:{EntityType}"</c>.
    /// </para>
    /// </remarks>
    public async ValueTask<Either<EncinaError, Unit>> RecordAsync(
        AuditEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        try
        {
            var encryptResult = await _encryptor.EncryptAuditEntryAsync(entry, cancellationToken)
                .ConfigureAwait(false);

            return await encryptResult.MatchAsync<Either<EncinaError, Unit>>(
                RightAsync: async encryptedEvent =>
                {
                    var streamId = BuildStreamId("audit", entry.EntityType, entry.EntityId);

                    _session.Events.Append(streamId, encryptedEvent);
                    await _session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                    _logger.LogDebug(
                        "Recorded encrypted audit entry {EntryId} to stream {StreamId}",
                        entry.Id,
                        streamId);

                    return Right(unit);
                },
                Left: error => error).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record audit entry {EntryId}", entry.Id);
            return Left(MartenAuditErrors.StoreUnavailable("RecordAsync", ex));
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Queries the async-projected <see cref="AuditEntryReadModel"/> documents filtered by entity type
    /// and optional entity ID, ordered by <c>TimestampUtc</c> descending.
    /// </remarks>
    public async ValueTask<Either<EncinaError, IReadOnlyList<AuditEntry>>> GetByEntityAsync(
        string entityType,
        string? entityId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityType);

        try
        {
            var query = _session.Query<AuditEntryReadModel>()
                .Where(m => m.EntityType == entityType);

            if (entityId is not null)
            {
                query = query.Where(m => m.EntityId == entityId);
            }

            var readModels = await query
                .OrderByDescending(m => m.TimestampUtc)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var entries = readModels.Select(MapToAuditEntry).ToList();

            return Right<EncinaError, IReadOnlyList<AuditEntry>>(entries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query audit entries by entity {EntityType}/{EntityId}", entityType, entityId);
            return Left(MartenAuditErrors.QueryFailed("ByEntity", ex));
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Queries the async-projected <see cref="AuditEntryReadModel"/> documents filtered by user ID
    /// and optional date range, ordered by <c>TimestampUtc</c> descending.
    /// <para>
    /// <b>Note</b>: After crypto-shredding, entries whose <c>UserId</c> contains <c>[SHREDDED]</c>
    /// will NOT match this filter, as the original user ID is permanently lost.
    /// </para>
    /// </remarks>
    public async ValueTask<Either<EncinaError, IReadOnlyList<AuditEntry>>> GetByUserAsync(
        string userId,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        try
        {
            var query = _session.Query<AuditEntryReadModel>()
                .Where(m => m.UserId == userId);

            if (fromUtc.HasValue)
            {
                query = query.Where(m => m.TimestampUtc >= fromUtc.Value);
            }

            if (toUtc.HasValue)
            {
                query = query.Where(m => m.TimestampUtc <= toUtc.Value);
            }

            var readModels = await query
                .OrderByDescending(m => m.TimestampUtc)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var entries = readModels.Select(MapToAuditEntry).ToList();

            return Right<EncinaError, IReadOnlyList<AuditEntry>>(entries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query audit entries by user {UserId}", userId);
            return Left(MartenAuditErrors.QueryFailed("ByUser", ex));
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Queries the async-projected <see cref="AuditEntryReadModel"/> documents filtered by
    /// correlation ID, ordered by <c>TimestampUtc</c> ascending (chronological order).
    /// </remarks>
    public async ValueTask<Either<EncinaError, IReadOnlyList<AuditEntry>>> GetByCorrelationIdAsync(
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        try
        {
            var readModels = await _session.Query<AuditEntryReadModel>()
                .Where(m => m.CorrelationId == correlationId)
                .OrderBy(m => m.TimestampUtc)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var entries = readModels.Select(MapToAuditEntry).ToList();

            return Right<EncinaError, IReadOnlyList<AuditEntry>>(entries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query audit entries by correlation {CorrelationId}", correlationId);
            return Left(MartenAuditErrors.QueryFailed("ByCorrelationId", ex));
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Queries the async-projected <see cref="AuditEntryReadModel"/> documents with flexible
    /// filtering and pagination. All <see cref="AuditQuery"/> filter properties are applied
    /// as Marten LINQ predicates. Results are ordered by <c>TimestampUtc</c> descending.
    /// <para>
    /// <b>Note on IpAddress / Duration filters</b>: <c>IpAddress</c> is a PII field —
    /// after crypto-shredding it contains <c>[SHREDDED]</c>, so filtering by original IP
    /// will not match shredded entries. <c>Duration</c> is computed from structural timestamps
    /// and remains functional after shredding.
    /// </para>
    /// </remarks>
    public async ValueTask<Either<EncinaError, PagedResult<AuditEntry>>> QueryAsync(
        AuditQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        try
        {
            var pageNumber = Math.Max(1, query.PageNumber);
            var pageSize = Math.Clamp(query.PageSize, 1, AuditQuery.MaxPageSize);

            var q = _session.Query<AuditEntryReadModel>().AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.UserId))
            {
                q = q.Where(m => m.UserId == query.UserId);
            }

            if (!string.IsNullOrWhiteSpace(query.TenantId))
            {
                q = q.Where(m => m.TenantId == query.TenantId);
            }

            if (!string.IsNullOrWhiteSpace(query.EntityType))
            {
                q = q.Where(m => m.EntityType == query.EntityType);
            }

            if (!string.IsNullOrWhiteSpace(query.EntityId))
            {
                q = q.Where(m => m.EntityId == query.EntityId);
            }

            if (!string.IsNullOrWhiteSpace(query.Action))
            {
                q = q.Where(m => m.Action == query.Action);
            }

            if (query.Outcome.HasValue)
            {
                q = q.Where(m => m.Outcome == query.Outcome.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.CorrelationId))
            {
                q = q.Where(m => m.CorrelationId == query.CorrelationId);
            }

            if (query.FromUtc.HasValue)
            {
                q = q.Where(m => m.TimestampUtc >= query.FromUtc.Value);
            }

            if (query.ToUtc.HasValue)
            {
                q = q.Where(m => m.TimestampUtc <= query.ToUtc.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.IpAddress))
            {
                q = q.Where(m => m.IpAddress == query.IpAddress);
            }

            // Duration filters: computed from structural timestamps (survive shredding)
            // Marten LINQ doesn't support computed TimeSpan subtraction, so we filter in-memory
            // after materialization for MinDuration/MaxDuration
            var hasDurationFilter = query.MinDuration.HasValue || query.MaxDuration.HasValue;

            // Get total count before pagination (without duration filter for accurate count)
            // We apply duration filter post-materialization
            int totalCount;
            IReadOnlyList<AuditEntryReadModel> readModels;

            if (hasDurationFilter)
            {
                // Must materialize all then filter in-memory for duration
                var allMatches = await q
                    .OrderByDescending(m => m.TimestampUtc)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

                var filtered = allMatches.AsEnumerable();

                if (query.MinDuration.HasValue)
                {
                    filtered = filtered.Where(m => (m.CompletedAtUtc - m.StartedAtUtc) >= query.MinDuration.Value);
                }

                if (query.MaxDuration.HasValue)
                {
                    filtered = filtered.Where(m => (m.CompletedAtUtc - m.StartedAtUtc) <= query.MaxDuration.Value);
                }

                var filteredList = filtered.ToList();
                totalCount = filteredList.Count;
                readModels = filteredList
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();
            }
            else
            {
                // Use Marten's server-side count and pagination
                totalCount = await q.CountAsync(cancellationToken).ConfigureAwait(false);
                readModels = await q
                    .OrderByDescending(m => m.TimestampUtc)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);
            }

            var items = readModels.Select(MapToAuditEntry).ToList();
            var result = PagedResult<AuditEntry>.Create(items, totalCount, pageNumber, pageSize);

            return Right(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute flexible audit query");
            return Left(MartenAuditErrors.QueryFailed("Flexible", ex));
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// <b>Crypto-shredding semantics</b>: This method destroys temporal encryption keys for
    /// all time periods older than <paramref name="olderThanUtc"/>, rather than deleting events.
    /// The encrypted events remain in the immutable event store, but without key material,
    /// PII fields cannot be decrypted.
    /// </para>
    /// <para>
    /// After key destruction, async projection rebuilds will show <c>[SHREDDED]</c> placeholder
    /// values for the affected entries' PII fields.
    /// </para>
    /// <para>
    /// Returns the number of temporal key periods whose keys were destroyed.
    /// </para>
    /// </remarks>
    public async ValueTask<Either<EncinaError, int>> PurgeEntriesAsync(
        DateTime olderThanUtc,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Starting crypto-shredding of audit entries older than {CutoffUtc} with granularity {Granularity}",
                olderThanUtc,
                _options.TemporalGranularity);

            var result = await _keyProvider.DestroyKeysBeforeAsync(
                olderThanUtc,
                _options.TemporalGranularity,
                cancellationToken).ConfigureAwait(false);

            return result.Match<Either<EncinaError, int>>(
                Right: destroyedCount =>
                {
                    _logger.LogInformation(
                        "Crypto-shredded {DestroyedCount} temporal key periods older than {CutoffUtc}",
                        destroyedCount,
                        olderThanUtc);
                    return Right(destroyedCount);
                },
                Left: error =>
                {
                    _logger.LogError(
                        "Crypto-shredding failed for entries older than {CutoffUtc}: {ErrorMessage}",
                        olderThanUtc,
                        error.Message);
                    return Left(error);
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to crypto-shred audit entries older than {CutoffUtc}", olderThanUtc);
            return Left(MartenAuditErrors.KeyDestructionFailed(olderThanUtc, ex));
        }
    }

    /// <summary>
    /// Builds a Marten event stream ID from the entity type and optional entity ID.
    /// </summary>
    private static string BuildStreamId(string prefix, string entityType, string? entityId)
    {
        return entityId is not null
            ? $"{prefix}:{entityType}:{entityId}"
            : $"{prefix}:{entityType}";
    }

    /// <summary>
    /// Maps an <see cref="AuditEntryReadModel"/> projected document to an <see cref="AuditEntry"/>
    /// domain record.
    /// </summary>
    private static AuditEntry MapToAuditEntry(AuditEntryReadModel model)
    {
        IReadOnlyDictionary<string, object?> metadata = model.MetadataJson is not null
                && model.MetadataJson != MartenAuditOptions.DefaultShreddedPlaceholder
            ? JsonSerializer.Deserialize<Dictionary<string, object?>>(model.MetadataJson, MetadataJsonOptions)
                ?? new Dictionary<string, object?>()
            : new Dictionary<string, object?>();

        return new AuditEntry
        {
            Id = model.Id,
            CorrelationId = model.CorrelationId,
            UserId = model.UserId,
            TenantId = model.TenantId,
            Action = model.Action,
            EntityType = model.EntityType,
            EntityId = model.EntityId,
            Outcome = model.Outcome,
            ErrorMessage = model.ErrorMessage,
            TimestampUtc = model.TimestampUtc,
            StartedAtUtc = model.StartedAtUtc,
            CompletedAtUtc = model.CompletedAtUtc,
            RequestPayloadHash = model.RequestPayloadHash,
            IpAddress = model.IpAddress,
            UserAgent = model.UserAgent,
            RequestPayload = model.RequestPayload,
            ResponsePayload = model.ResponsePayload,
            Metadata = metadata
        };
    }
}
