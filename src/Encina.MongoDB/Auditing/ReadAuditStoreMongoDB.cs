using System.Text.RegularExpressions;
using Encina.Security.Audit;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using static LanguageExt.Prelude;

namespace Encina.MongoDB.Auditing;

/// <summary>
/// MongoDB implementation of <see cref="IReadAuditStore"/>.
/// </summary>
/// <remarks>
/// <para>
/// This implementation uses MongoDB-specific features:
/// <list type="bullet">
/// <item><description>BSON document serialization via <see cref="ReadAuditEntryDocument"/></description></item>
/// <item><description>Filter builders for type-safe queries</description></item>
/// <item><description>Regex filters for case-insensitive purpose matching</description></item>
/// <item><description>Skip/Limit for pagination</description></item>
/// <item><description>DeleteManyAsync for efficient purge operations</description></item>
/// </list>
/// </para>
/// <para>
/// Each call to <see cref="LogReadAsync"/> immediately persists the read audit entry to the database.
/// </para>
/// </remarks>
public sealed class ReadAuditStoreMongoDB : IReadAuditStore
{
    private readonly IMongoCollection<ReadAuditEntryDocument> _collection;
    private readonly ILogger<ReadAuditStoreMongoDB> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadAuditStoreMongoDB"/> class.
    /// </summary>
    /// <param name="mongoClient">The MongoDB client.</param>
    /// <param name="options">The MongoDB options.</param>
    /// <param name="logger">The logger.</param>
    public ReadAuditStoreMongoDB(
        IMongoClient mongoClient,
        IOptions<EncinaMongoDbOptions> options,
        ILogger<ReadAuditStoreMongoDB> logger)
    {
        ArgumentNullException.ThrowIfNull(mongoClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        var config = options.Value;
        var database = mongoClient.GetDatabase(config.DatabaseName);
        _collection = database.GetCollection<ReadAuditEntryDocument>(config.Collections.ReadAuditEntries);
        _logger = logger;
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Unit>> LogReadAsync(
        ReadAuditEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        try
        {
            var document = ReadAuditEntryDocument.FromEntry(entry);
            await _collection.InsertOneAsync(document, cancellationToken: cancellationToken).ConfigureAwait(false);
            return Right(unit);
        }
        catch (Exception ex)
        {
            return Left(ReadAuditErrors.StoreError("LogRead", ex.Message, ex));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<ReadAuditEntry>>> GetAccessHistoryAsync(
        string entityType,
        string entityId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityType);
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);

        try
        {
            var filterBuilder = Builders<ReadAuditEntryDocument>.Filter;
            var filter = filterBuilder.Eq(d => d.EntityType, entityType)
                       & filterBuilder.Eq(d => d.EntityId, entityId);

            var documents = await _collection
                .Find(filter)
                .SortByDescending(d => d.AccessedAtUtc)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var entries = documents.Select(d => d.ToEntry()).ToList();
            return Right<EncinaError, IReadOnlyList<ReadAuditEntry>>(entries);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<ReadAuditEntry>>(
                ReadAuditErrors.StoreError("GetAccessHistory", ex.Message, ex));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<ReadAuditEntry>>> GetUserAccessHistoryAsync(
        string userId,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        try
        {
            var filterBuilder = Builders<ReadAuditEntryDocument>.Filter;
            var filter = filterBuilder.Eq(d => d.UserId, userId)
                       & filterBuilder.Gte(d => d.AccessedAtUtc, fromUtc.UtcDateTime)
                       & filterBuilder.Lte(d => d.AccessedAtUtc, toUtc.UtcDateTime);

            var documents = await _collection
                .Find(filter)
                .SortByDescending(d => d.AccessedAtUtc)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var entries = documents.Select(d => d.ToEntry()).ToList();
            return Right<EncinaError, IReadOnlyList<ReadAuditEntry>>(entries);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<ReadAuditEntry>>(
                ReadAuditErrors.StoreError("GetUserAccessHistory", ex.Message, ex));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, PagedResult<ReadAuditEntry>>> QueryAsync(
        ReadAuditQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        try
        {
            var pageNumber = Math.Max(1, query.PageNumber);
            var pageSize = Math.Clamp(query.PageSize, 1, ReadAuditQuery.MaxPageSize);
            var skip = (pageNumber - 1) * pageSize;

            // Build filter from query
            var filter = BuildFilter(query);

            // Get total count
            var totalCount = await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            // Get paginated results
            var documents = await _collection
                .Find(filter)
                .SortByDescending(d => d.AccessedAtUtc)
                .Skip(skip)
                .Limit(pageSize)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var entries = documents.Select(d => d.ToEntry()).ToList();

            var result = PagedResult<ReadAuditEntry>.Create(entries, (int)totalCount, pageNumber, pageSize);
            return Right(result);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, PagedResult<ReadAuditEntry>>(
                ReadAuditErrors.StoreError("Query", ex.Message, ex));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, int>> PurgeEntriesAsync(
        DateTimeOffset olderThanUtc,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<ReadAuditEntryDocument>.Filter.Lt(d => d.AccessedAtUtc, olderThanUtc.UtcDateTime);
            var result = await _collection.DeleteManyAsync(filter, cancellationToken).ConfigureAwait(false);

            var deletedCount = (int)result.DeletedCount;
            return Right(deletedCount);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, int>(
                ReadAuditErrors.PurgeFailed(ex.Message, ex));
        }
    }

    private static FilterDefinition<ReadAuditEntryDocument> BuildFilter(ReadAuditQuery query)
    {
        var builder = Builders<ReadAuditEntryDocument>.Filter;
        var filters = new List<FilterDefinition<ReadAuditEntryDocument>>();

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

        if (query.AccessMethod.HasValue)
        {
            filters.Add(builder.Eq(d => d.AccessMethod, (int)query.AccessMethod.Value));
        }

        if (!string.IsNullOrWhiteSpace(query.Purpose))
        {
            filters.Add(builder.Regex(d => d.Purpose, new BsonRegularExpression(Regex.Escape(query.Purpose), "i")));
        }

        if (!string.IsNullOrWhiteSpace(query.CorrelationId))
        {
            filters.Add(builder.Eq(d => d.CorrelationId, query.CorrelationId));
        }

        if (query.FromUtc.HasValue)
        {
            filters.Add(builder.Gte(d => d.AccessedAtUtc, query.FromUtc.Value.UtcDateTime));
        }

        if (query.ToUtc.HasValue)
        {
            filters.Add(builder.Lte(d => d.AccessedAtUtc, query.ToUtc.Value.UtcDateTime));
        }

        return filters.Count == 0
            ? builder.Empty
            : builder.And(filters);
    }
}
