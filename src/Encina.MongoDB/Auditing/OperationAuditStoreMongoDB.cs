using Encina.Diagnostics;
using Encina.Messaging;
using Encina.Security.Audit;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using static LanguageExt.Prelude;

namespace Encina.MongoDB.Auditing;

/// <summary>
/// MongoDB implementation of <see cref="IOperationAuditStore"/>.
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
public sealed class OperationAuditStoreMongoDB : IOperationAuditStore
{
    private readonly IMongoCollection<OperationAuditEntryDocument> _collection;
    private readonly ILogger<OperationAuditStoreMongoDB> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OperationAuditStoreMongoDB"/> class.
    /// </summary>
    /// <param name="mongoClient">The MongoDB client.</param>
    /// <param name="options">The MongoDB options.</param>
    /// <param name="logger">The logger.</param>
    public OperationAuditStoreMongoDB(
        IMongoClient mongoClient,
        IOptions<EncinaMongoDbOptions> options,
        ILogger<OperationAuditStoreMongoDB> logger)
    {
        ArgumentNullException.ThrowIfNull(mongoClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        var config = options.Value;
        var database = mongoClient.GetDatabase(config.DatabaseName);
        _collection = database.GetCollection<OperationAuditEntryDocument>(config.Collections.OperationAuditEntries);
        _logger = logger;
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Unit>> RecordAsync(
        OperationAuditEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        try
        {
            var document = OperationAuditEntryDocument.FromEntry(entry);
            await _collection.InsertOneAsync(document, cancellationToken: cancellationToken).ConfigureAwait(false);
            Log.AddedOperationAuditEntry(_logger, entry.Id, entry.EntityType, entry.EntityId);
            return Right(unit);
        }
        catch (Exception ex)
        {
            Log.FailedToRecordAuditEntry(_logger, ex.ForLogging(), entry.Id);
            return Left(EncinaError.New($"Failed to record audit entry: {ex.Message}"));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<OperationAuditEntry>>> GetByEntityAsync(
        string entityType,
        string? entityId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityType);

        try
        {
            var filterBuilder = Builders<OperationAuditEntryDocument>.Filter;
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
            return Right<EncinaError, IReadOnlyList<OperationAuditEntry>>(entries);
        }
        catch (Exception ex)
        {
            Log.FailedToQueryAuditEntriesByEntity(_logger, ex.ForLogging(), entityType);
            return Left<EncinaError, IReadOnlyList<OperationAuditEntry>>(
                EncinaError.New($"Failed to query audit entries: {ex.Message}"));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<OperationAuditEntry>>> GetByUserAsync(
        string userId,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        try
        {
            var filterBuilder = Builders<OperationAuditEntryDocument>.Filter;
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
            return Right<EncinaError, IReadOnlyList<OperationAuditEntry>>(entries);
        }
        catch (Exception ex)
        {
            Log.FailedToQueryAuditEntriesByUser(_logger, ex.ForLogging(), userId);
            return Left<EncinaError, IReadOnlyList<OperationAuditEntry>>(
                EncinaError.New($"Failed to query audit entries: {ex.Message}"));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<OperationAuditEntry>>> GetByCorrelationIdAsync(
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        try
        {
            var filter = Builders<OperationAuditEntryDocument>.Filter.Eq(d => d.CorrelationId, correlationId);

            var documents = await _collection
                .Find(filter)
                .SortBy(d => d.TimestampUtc) // Ascending for correlation ID to show chronological order
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var entries = documents.Select(d => d.ToEntry()).ToList();
            return Right<EncinaError, IReadOnlyList<OperationAuditEntry>>(entries);
        }
        catch (Exception ex)
        {
            Log.FailedToQueryAuditEntriesByCorrelationId(_logger, ex.ForLogging(), correlationId);
            return Left<EncinaError, IReadOnlyList<OperationAuditEntry>>(
                EncinaError.New($"Failed to query audit entries: {ex.Message}"));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, PagedResult<OperationAuditEntry>>> QueryAsync(
        OperationAuditQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        try
        {
            var pageNumber = Math.Max(1, query.PageNumber);
            var pageSize = Math.Clamp(query.PageSize, 1, OperationAuditQuery.MaxPageSize);

            // Build filter from query
            var filter = BuildFilter(query);

            var page = query.MinDuration.HasValue || query.MaxDuration.HasValue
                ? await QueryFilteredByDurationAsync(filter, query, pageNumber, pageSize, cancellationToken)
                    .ConfigureAwait(false)
                : await QueryPageAsync(filter, pageNumber, pageSize, cancellationToken).ConfigureAwait(false);

            return Right(page);
        }
        catch (Exception ex)
        {
            Log.FailedToExecuteAuditQuery(_logger, ex.ForLogging());
            return Left<EncinaError, PagedResult<OperationAuditEntry>>(
                EncinaError.New($"Failed to query audit entries: {ex.Message}"));
        }
    }

    private async Task<PagedResult<OperationAuditEntry>> QueryPageAsync(
        FilterDefinition<OperationAuditEntryDocument> filter,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var totalCount = await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        var documents = await _collection
            .Find(filter)
            .SortByDescending(d => d.TimestampUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var entries = documents.Select(d => d.ToEntry()).ToList();
        return PagedResult<OperationAuditEntry>.Create(entries, (int)totalCount, pageNumber, pageSize);
    }

    // Duration is computed (CompletedAtUtc - StartedAtUtc), not stored, so it is filtered in memory
    // before paging: the page and the total count are those of the filtered set, as in the other providers.
    private async Task<PagedResult<OperationAuditEntry>> QueryFilteredByDurationAsync(
        FilterDefinition<OperationAuditEntryDocument> filter,
        OperationAuditQuery query,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var documents = await _collection
            .Find(filter)
            .SortByDescending(d => d.TimestampUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var matching = ApplyDurationFilter(documents.Select(d => d.ToEntry()), query);
        var items = matching.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
        return PagedResult<OperationAuditEntry>.Create(items, matching.Count, pageNumber, pageSize);
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, int>> PurgeEntriesAsync(
        DateTime olderThanUtc,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<OperationAuditEntryDocument>.Filter.Lt(d => d.TimestampUtc, olderThanUtc);
            var result = await _collection.DeleteManyAsync(filter, cancellationToken).ConfigureAwait(false);

            var deletedCount = (int)result.DeletedCount;
            Log.PurgedAuditEntries(_logger, deletedCount, olderThanUtc);
            return Right(deletedCount);
        }
        catch (Exception ex)
        {
            Log.FailedToPurgeAuditEntries(_logger, ex.ForLogging(), olderThanUtc);
            return Left<EncinaError, int>(
                EncinaError.New($"Failed to purge audit entries: {ex.Message}"));
        }
    }

    private static FilterDefinition<OperationAuditEntryDocument> BuildFilter(OperationAuditQuery query)
    {
        var builder = Builders<OperationAuditEntryDocument>.Filter;
        var filters = new List<FilterDefinition<OperationAuditEntryDocument>>();

        AddIf(filters, !string.IsNullOrWhiteSpace(query.UserId), () => builder.Eq(d => d.UserId, query.UserId));
        AddIf(filters, !string.IsNullOrWhiteSpace(query.TenantId), () => builder.Eq(d => d.TenantId, query.TenantId));
        AddIf(filters, !string.IsNullOrWhiteSpace(query.EntityType), () => builder.Eq(d => d.EntityType, query.EntityType));
        AddIf(filters, !string.IsNullOrWhiteSpace(query.EntityId), () => builder.Eq(d => d.EntityId, query.EntityId));
        AddIf(filters, !string.IsNullOrWhiteSpace(query.Action), () => builder.Eq(d => d.Action, query.Action));
        AddIf(filters, query.Outcome.HasValue, () => builder.Eq(d => d.Outcome, (int)query.Outcome!.Value));
        AddIf(filters, !string.IsNullOrWhiteSpace(query.CorrelationId), () => builder.Eq(d => d.CorrelationId, query.CorrelationId));
        AddIf(filters, query.FromUtc.HasValue, () => builder.Gte(d => d.TimestampUtc, query.FromUtc!.Value));
        AddIf(filters, query.ToUtc.HasValue, () => builder.Lte(d => d.TimestampUtc, query.ToUtc!.Value));
        AddIf(filters, !string.IsNullOrWhiteSpace(query.IpAddress), () => builder.Eq(d => d.IpAddress, query.IpAddress));

        return filters.Count == 0
            ? builder.Empty
            : builder.And(filters);
    }

    private static void AddIf(
        List<FilterDefinition<OperationAuditEntryDocument>> filters,
        bool condition,
        Func<FilterDefinition<OperationAuditEntryDocument>> createFilter)
    {
        if (condition)
        {
            filters.Add(createFilter());
        }
    }

    // Duration is computed, not stored, so it is filtered in memory.
    private static List<OperationAuditEntry> ApplyDurationFilter(IEnumerable<OperationAuditEntry> entries, OperationAuditQuery query) =>
        entries
            .Where(e => !query.MinDuration.HasValue || e.Duration >= query.MinDuration.Value)
            .Where(e => !query.MaxDuration.HasValue || e.Duration <= query.MaxDuration.Value)
            .ToList();
}
