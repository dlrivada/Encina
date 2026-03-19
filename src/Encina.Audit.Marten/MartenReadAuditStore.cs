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
/// Event-sourced <see cref="IReadAuditStore"/> implementation using Marten (PostgreSQL) with temporal
/// crypto-shredding for compliance-grade read access audit trails.
/// </summary>
/// <remarks>
/// <para>
/// This store appends encrypted read audit events to Marten event streams and queries against
/// async-projected read model documents. PII-sensitive fields (<c>UserId</c>, <c>Purpose</c>,
/// <c>Metadata</c>) are encrypted with temporal keys before event persistence.
/// </para>
/// <para>
/// <b>Key behavioral differences from database-backed stores:</b>
/// <list type="bullet">
/// <item><b>Immutability</b>: Events are append-only. No UPDATE or DELETE operations.</item>
/// <item><b>Crypto-shredding</b>: <see cref="PurgeEntriesAsync"/> destroys temporal encryption
/// keys, rendering PII permanently unreadable while preserving event stream integrity.</item>
/// <item><b>Eventual consistency</b>: Query results come from async-projected read models.</item>
/// </list>
/// </para>
/// <para>
/// Stream ID format: <c>"read-audit:{EntityType}:{EntityId}"</c> when an entity ID is present,
/// or <c>"read-audit:{EntityType}"</c> for bulk operations.
/// </para>
/// </remarks>
public sealed class MartenReadAuditStore : IReadAuditStore
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
    private readonly ILogger<MartenReadAuditStore> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MartenReadAuditStore"/> class.
    /// </summary>
    /// <param name="session">The Marten document session for event and document persistence.</param>
    /// <param name="encryptor">The encryptor that maps read audit entries to encrypted events.</param>
    /// <param name="keyProvider">The temporal key provider for crypto-shredding operations.</param>
    /// <param name="options">The Marten audit configuration options.</param>
    /// <param name="logger">Logger for structured diagnostic logging.</param>
    public MartenReadAuditStore(
        IDocumentSession session,
        AuditEventEncryptor encryptor,
        ITemporalKeyProvider keyProvider,
        IOptions<MartenAuditOptions> options,
        ILogger<MartenReadAuditStore> logger)
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
    /// Encrypts PII fields using a temporal key for the entry's timestamp period,
    /// then appends the encrypted event to a Marten event stream.
    /// Stream ID: <c>"read-audit:{EntityType}:{EntityId}"</c> or <c>"read-audit:{EntityType}"</c>.
    /// </remarks>
    public async ValueTask<Either<EncinaError, Unit>> LogReadAsync(
        ReadAuditEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        try
        {
            var encryptResult = await _encryptor.EncryptReadAuditEntryAsync(entry, cancellationToken)
                .ConfigureAwait(false);

            return await encryptResult.MatchAsync<Either<EncinaError, Unit>>(
                RightAsync: async encryptedEvent =>
                {
                    var streamId = BuildStreamId("read-audit", entry.EntityType, entry.EntityId);

                    _session.Events.Append(streamId, encryptedEvent);
                    await _session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                    _logger.LogDebug(
                        "Recorded encrypted read audit entry {EntryId} to stream {StreamId}",
                        entry.Id,
                        streamId);

                    return Right(unit);
                },
                Left: error => error).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record read audit entry {EntryId}", entry.Id);
            return Left(MartenAuditErrors.StoreUnavailable("LogReadAsync", ex));
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Queries async-projected <see cref="ReadAuditEntryReadModel"/> documents filtered by
    /// entity type and entity ID, ordered by <c>AccessedAtUtc</c> descending.
    /// Answers the GDPR Art. 15 question: "Who accessed this entity's data?"
    /// </remarks>
    public async ValueTask<Either<EncinaError, IReadOnlyList<ReadAuditEntry>>> GetAccessHistoryAsync(
        string entityType,
        string entityId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityType);
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);

        try
        {
            var readModels = await _session.Query<ReadAuditEntryReadModel>()
                .Where(m => m.EntityType == entityType && m.EntityId == entityId)
                .OrderByDescending(m => m.AccessedAtUtc)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var entries = readModels.Select(MapToReadAuditEntry).ToList();

            return Right<EncinaError, IReadOnlyList<ReadAuditEntry>>(entries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query access history for {EntityType}/{EntityId}", entityType, entityId);
            return Left(MartenAuditErrors.QueryFailed("GetAccessHistory", ex));
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Queries async-projected <see cref="ReadAuditEntryReadModel"/> documents filtered by
    /// user ID and date range, ordered by <c>AccessedAtUtc</c> descending.
    /// Answers the HIPAA question: "What did this user access in this time period?"
    /// </remarks>
    public async ValueTask<Either<EncinaError, IReadOnlyList<ReadAuditEntry>>> GetUserAccessHistoryAsync(
        string userId,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        try
        {
            var readModels = await _session.Query<ReadAuditEntryReadModel>()
                .Where(m => m.UserId == userId)
                .Where(m => m.AccessedAtUtc >= fromUtc)
                .Where(m => m.AccessedAtUtc <= toUtc)
                .OrderByDescending(m => m.AccessedAtUtc)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var entries = readModels.Select(MapToReadAuditEntry).ToList();

            return Right<EncinaError, IReadOnlyList<ReadAuditEntry>>(entries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query user access history for {UserId}", userId);
            return Left(MartenAuditErrors.QueryFailed("GetUserAccessHistory", ex));
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Queries async-projected <see cref="ReadAuditEntryReadModel"/> documents with flexible
    /// filtering and pagination. All <see cref="ReadAuditQuery"/> filter properties are applied
    /// as Marten LINQ predicates. Results ordered by <c>AccessedAtUtc</c> descending.
    /// </remarks>
    public async ValueTask<Either<EncinaError, PagedResult<ReadAuditEntry>>> QueryAsync(
        ReadAuditQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        try
        {
            var pageNumber = Math.Max(1, query.PageNumber);
            var pageSize = Math.Clamp(query.PageSize, 1, ReadAuditQuery.MaxPageSize);

            var q = _session.Query<ReadAuditEntryReadModel>().AsQueryable();

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

            if (query.AccessMethod.HasValue)
            {
                q = q.Where(m => m.AccessMethod == query.AccessMethod.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.Purpose))
            {
                // Purpose is a PII field — after shredding, purpose contains "[SHREDDED]".
                // StringComparison.OrdinalIgnoreCase is not directly supported by Marten LINQ.
                // Use exact match here; for case-insensitive search, use QueryAsync overloads.
                q = q.Where(m => m.Purpose != null && m.Purpose.Contains(query.Purpose));
            }

            if (!string.IsNullOrWhiteSpace(query.CorrelationId))
            {
                q = q.Where(m => m.CorrelationId == query.CorrelationId);
            }

            if (query.FromUtc.HasValue)
            {
                q = q.Where(m => m.AccessedAtUtc >= query.FromUtc.Value);
            }

            if (query.ToUtc.HasValue)
            {
                q = q.Where(m => m.AccessedAtUtc <= query.ToUtc.Value);
            }

            var totalCount = await q.CountAsync(cancellationToken).ConfigureAwait(false);

            var readModels = await q
                .OrderByDescending(m => m.AccessedAtUtc)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var items = readModels.Select(MapToReadAuditEntry).ToList();
            var result = PagedResult<ReadAuditEntry>.Create(items, totalCount, pageNumber, pageSize);

            return Right(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute flexible read audit query");
            return Left(MartenAuditErrors.QueryFailed("FlexibleReadAudit", ex));
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <b>Crypto-shredding semantics</b>: Destroys temporal encryption keys for all time periods
    /// older than <paramref name="olderThanUtc"/>. After key destruction, PII fields in affected
    /// read audit entries become permanently unreadable during async projection rebuilds.
    /// </remarks>
    public async ValueTask<Either<EncinaError, int>> PurgeEntriesAsync(
        DateTimeOffset olderThanUtc,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Starting crypto-shredding of read audit entries older than {CutoffUtc}",
                olderThanUtc);

            var result = await _keyProvider.DestroyKeysBeforeAsync(
                olderThanUtc.UtcDateTime,
                _options.TemporalGranularity,
                cancellationToken).ConfigureAwait(false);

            return result.Match<Either<EncinaError, int>>(
                Right: destroyedCount =>
                {
                    _logger.LogInformation(
                        "Crypto-shredded {DestroyedCount} temporal key periods for read audit entries older than {CutoffUtc}",
                        destroyedCount,
                        olderThanUtc);
                    return Right(destroyedCount);
                },
                Left: error =>
                {
                    _logger.LogError(
                        "Crypto-shredding failed for read audit entries older than {CutoffUtc}: {ErrorMessage}",
                        olderThanUtc,
                        error.Message);
                    return Left(error);
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to crypto-shred read audit entries older than {CutoffUtc}", olderThanUtc);
            return Left(MartenAuditErrors.KeyDestructionFailed(olderThanUtc.UtcDateTime, ex));
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
    /// Maps a <see cref="ReadAuditEntryReadModel"/> projected document to a <see cref="ReadAuditEntry"/>
    /// domain record.
    /// </summary>
    private static ReadAuditEntry MapToReadAuditEntry(ReadAuditEntryReadModel model)
    {
        IReadOnlyDictionary<string, object?> metadata = model.MetadataJson is not null
                && model.MetadataJson != MartenAuditOptions.DefaultShreddedPlaceholder
            ? JsonSerializer.Deserialize<Dictionary<string, object?>>(model.MetadataJson, MetadataJsonOptions)
                ?? new Dictionary<string, object?>()
            : new Dictionary<string, object?>();

        return new ReadAuditEntry
        {
            Id = model.Id,
            EntityType = model.EntityType,
            EntityId = model.EntityId,
            UserId = model.UserId,
            TenantId = model.TenantId,
            AccessedAtUtc = model.AccessedAtUtc,
            CorrelationId = model.CorrelationId,
            Purpose = model.Purpose,
            AccessMethod = model.AccessMethod,
            EntityCount = model.EntityCount,
            Metadata = metadata
        };
    }
}
