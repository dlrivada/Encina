using Encina.Messaging;
using Encina.Security.Audit;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using static LanguageExt.Prelude;

namespace Encina.MongoDB.Auditing;

/// <summary>
/// MongoDB implementation of <see cref="IAuditStore"/>.
/// </summary>
/// <remarks>
/// <para>
/// This implementation uses MongoDB-specific features:
/// <list type="bullet">
/// <item><description>BSON document serialization</description></item>
/// <item><description>Filter builders for type-safe queries</description></item>
/// <item><description>Indexes on frequently queried fields for performance</description></item>
/// <item><description>Skip/Limit for pagination</description></item>
/// <item><description>DeleteManyAsync for efficient purge operations</description></item>
/// </list>
/// </para>
/// <para>
/// Each call to <see cref="RecordAsync"/> immediately persists the audit entry to the database.
/// </para>
/// </remarks>
public sealed class AuditStoreMongoDB : IAuditStore
{
    private readonly IMongoCollection<AuditEntryDocument> _collection;
    private readonly ILogger<AuditStoreMongoDB> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditStoreMongoDB"/> class.
    /// </summary>
    /// <param name="mongoClient">The MongoDB client.</param>
    /// <param name="options">The MongoDB options.</param>
    /// <param name="logger">The logger.</param>
    public AuditStoreMongoDB(
        IMongoClient mongoClient,
        IOptions<EncinaMongoDbOptions> options,
        ILogger<AuditStoreMongoDB> logger)
    {
        ArgumentNullException.ThrowIfNull(mongoClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        var config = options.Value;
        var database = mongoClient.GetDatabase(config.DatabaseName);
        _collection = database.GetCollection<AuditEntryDocument>(config.Collections.SecurityAuditEntries);
        _logger = logger;
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Unit>> RecordAsync(
        AuditEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        try
        {
            var document = AuditEntryDocument.FromEntry(entry);
            await _collection.InsertOneAsync(document, cancellationToken: cancellationToken).ConfigureAwait(false);
            Log.AddedSecurityAuditEntry(_logger, entry.Id, entry.EntityType, entry.EntityId);
            return Right(unit);
        }
        catch (Exception ex)
        {
            Log.FailedToRecordAuditEntry(_logger, ex, entry.Id);
            return Left(EncinaError.New($"Failed to record audit entry: {ex.Message}"));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<AuditEntry>>> GetByEntityAsync(
        string entityType,
        string? entityId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityType);

        try
        {
            var filterBuilder = Builders<AuditEntryDocument>.Filter;
            var filter = filterBuilder.Eq(d => d.EntityType, entityType);

            if (entityId is not null)
            {
                filter &= filterBuilder.Eq(d => d.EntityId, entityId);
            }

            var documents = await _collection
                .Find(filter)
                .SortByDescending(d => d.TimestampUtc)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var entries = documents.Select(d => d.ToEntry()).ToList();
            return Right<EncinaError, IReadOnlyList<AuditEntry>>(entries);
        }
        catch (Exception ex)
        {
            Log.FailedToQueryAuditEntriesByEntity(_logger, ex, entityType);
            return Left<EncinaError, IReadOnlyList<AuditEntry>>(
                EncinaError.New($"Failed to query audit entries: {ex.Message}"));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<AuditEntry>>> GetByUserAsync(
        string userId,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        try
        {
            var filterBuilder = Builders<AuditEntryDocument>.Filter;
            var filter = filterBuilder.Eq(d => d.UserId, userId);

            if (fromUtc.HasValue)
            {
                filter &= filterBuilder.Gte(d => d.TimestampUtc, fromUtc.Value);
            }

            if (toUtc.HasValue)
            {
                filter &= filterBuilder.Lte(d => d.TimestampUtc, toUtc.Value);
            }

            var documents = await _collection
                .Find(filter)
                .SortByDescending(d => d.TimestampUtc)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var entries = documents.Select(d => d.ToEntry()).ToList();
            return Right<EncinaError, IReadOnlyList<AuditEntry>>(entries);
        }
        catch (Exception ex)
        {
            Log.FailedToQueryAuditEntriesByUser(_logger, ex, userId);
            return Left<EncinaError, IReadOnlyList<AuditEntry>>(
                EncinaError.New($"Failed to query audit entries: {ex.Message}"));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<AuditEntry>>> GetByCorrelationIdAsync(
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        try
        {
            var filter = Builders<AuditEntryDocument>.Filter.Eq(d => d.CorrelationId, correlationId);

            var documents = await _collection
                .Find(filter)
                .SortBy(d => d.TimestampUtc) // Ascending for correlation ID to show chronological order
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var entries = documents.Select(d => d.ToEntry()).ToList();
            return Right<EncinaError, IReadOnlyList<AuditEntry>>(entries);
        }
        catch (Exception ex)
        {
            Log.FailedToQueryAuditEntriesByCorrelationId(_logger, ex, correlationId);
            return Left<EncinaError, IReadOnlyList<AuditEntry>>(
                EncinaError.New($"Failed to query audit entries: {ex.Message}"));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, PagedResult<AuditEntry>>> QueryAsync(
        AuditQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        try
        {
            var pageNumber = Math.Max(1, query.PageNumber);
            var pageSize = Math.Clamp(query.PageSize, 1, AuditQuery.MaxPageSize);
            var skip = (pageNumber - 1) * pageSize;

            // Build filter from query
            var filter = BuildFilter(query);

            // Get total count
            var totalCount = await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            // Get paginated results
            var documents = await _collection
                .Find(filter)
                .SortByDescending(d => d.TimestampUtc)
                .Skip(skip)
                .Limit(pageSize)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var entries = documents.Select(d => d.ToEntry()).ToList();

            // Apply duration filter in memory (Duration is computed, not stored)
            if (query.MinDuration.HasValue || query.MaxDuration.HasValue)
            {
                var filtered = entries.AsEnumerable();

                if (query.MinDuration.HasValue)
                {
                    filtered = filtered.Where(e => e.Duration >= query.MinDuration.Value);
                }

                if (query.MaxDuration.HasValue)
                {
                    filtered = filtered.Where(e => e.Duration <= query.MaxDuration.Value);
                }

                entries = filtered.ToList();
            }

            var result = PagedResult<AuditEntry>.Create(entries, (int)totalCount, pageNumber, pageSize);
            return Right(result);
        }
        catch (Exception ex)
        {
            Log.FailedToExecuteAuditQuery(_logger, ex);
            return Left<EncinaError, PagedResult<AuditEntry>>(
                EncinaError.New($"Failed to query audit entries: {ex.Message}"));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, int>> PurgeEntriesAsync(
        DateTime olderThanUtc,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<AuditEntryDocument>.Filter.Lt(d => d.TimestampUtc, olderThanUtc);
            var result = await _collection.DeleteManyAsync(filter, cancellationToken).ConfigureAwait(false);

            var deletedCount = (int)result.DeletedCount;
            Log.PurgedAuditEntries(_logger, deletedCount, olderThanUtc);
            return Right(deletedCount);
        }
        catch (Exception ex)
        {
            Log.FailedToPurgeAuditEntries(_logger, ex, olderThanUtc);
            return Left<EncinaError, int>(
                EncinaError.New($"Failed to purge audit entries: {ex.Message}"));
        }
    }

    private static FilterDefinition<AuditEntryDocument> BuildFilter(AuditQuery query)
    {
        var builder = Builders<AuditEntryDocument>.Filter;
        var filters = new List<FilterDefinition<AuditEntryDocument>>();

        if (!string.IsNullOrWhiteSpace(query.UserId))
        {
            filters.Add(builder.Eq(d => d.UserId, query.UserId));
        }

        if (!string.IsNullOrWhiteSpace(query.TenantId))
        {
            filters.Add(builder.Eq(d => d.TenantId, query.TenantId));
        }

        if (!string.IsNullOrWhiteSpace(query.EntityType))
        {
            filters.Add(builder.Eq(d => d.EntityType, query.EntityType));
        }

        if (!string.IsNullOrWhiteSpace(query.EntityId))
        {
            filters.Add(builder.Eq(d => d.EntityId, query.EntityId));
        }

        if (!string.IsNullOrWhiteSpace(query.Action))
        {
            filters.Add(builder.Eq(d => d.Action, query.Action));
        }

        if (query.Outcome.HasValue)
        {
            filters.Add(builder.Eq(d => d.Outcome, (int)query.Outcome.Value));
        }

        if (!string.IsNullOrWhiteSpace(query.CorrelationId))
        {
            filters.Add(builder.Eq(d => d.CorrelationId, query.CorrelationId));
        }

        if (query.FromUtc.HasValue)
        {
            filters.Add(builder.Gte(d => d.TimestampUtc, query.FromUtc.Value));
        }

        if (query.ToUtc.HasValue)
        {
            filters.Add(builder.Lte(d => d.TimestampUtc, query.ToUtc.Value));
        }

        if (!string.IsNullOrWhiteSpace(query.IpAddress))
        {
            filters.Add(builder.Eq(d => d.IpAddress, query.IpAddress));
        }

        return filters.Count == 0
            ? builder.Empty
            : builder.And(filters);
    }
}
