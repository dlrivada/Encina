using System.Data;
using Dapper;
using Encina.Compliance.Retention;
using Encina.Compliance.Retention.Model;
using Encina.Messaging;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.Dapper.MySQL.Retention;

/// <summary>
/// Dapper implementation of <see cref="IRetentionRecordStore"/> for MySQL.
/// Tracks the retention lifecycle of individual data entities.
/// </summary>
/// <remarks>
/// <para>
/// Per GDPR Article 5(1)(e) (storage limitation), this store enables controllers to
/// demonstrate that personal data is not kept longer than necessary.
/// </para>
/// <para>
/// MySQL-specific considerations:
/// <list type="bullet">
/// <item><description>DateTimeOffset values are written via <c>.UtcDateTime</c> (DATETIME type).</description></item>
/// <item><description>Integer values require <c>Convert.ToInt32()</c> for safe casting from dynamic rows.</description></item>
/// <item><description>PascalCase column names in SQL queries.</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class RetentionRecordStoreDapper : IRetentionRecordStore
{
    private readonly IDbConnection _connection;
    private readonly string _tableName;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="RetentionRecordStoreDapper"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="tableName">The retention records table name (default: RetentionRecords).</param>
    /// <param name="timeProvider">The time provider for UTC time (default: <see cref="TimeProvider.System"/>).</param>
    public RetentionRecordStoreDapper(
        IDbConnection connection,
        string tableName = "RetentionRecords",
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
        _tableName = SqlIdentifierValidator.ValidateTableName(tableName);
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> CreateAsync(
        RetentionRecord record,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        try
        {
            var entity = RetentionRecordMapper.ToEntity(record);
            var sql = $@"
                INSERT INTO {_tableName}
                (Id, EntityId, DataCategory, PolicyId, CreatedAtUtc, ExpiresAtUtc, StatusValue, DeletedAtUtc, LegalHoldId)
                VALUES
                (@Id, @EntityId, @DataCategory, @PolicyId, @CreatedAtUtc, @ExpiresAtUtc, @StatusValue, @DeletedAtUtc, @LegalHoldId)";

            await _connection.ExecuteAsync(sql, new
            {
                entity.Id,
                entity.EntityId,
                entity.DataCategory,
                entity.PolicyId,
                CreatedAtUtc = entity.CreatedAtUtc.UtcDateTime,
                ExpiresAtUtc = entity.ExpiresAtUtc.UtcDateTime,
                entity.StatusValue,
                DeletedAtUtc = entity.DeletedAtUtc?.UtcDateTime,
                entity.LegalHoldId
            });

            return Right(Unit.Default);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "retention.store_error",
                message: $"Failed to create retention record: {ex.Message}",
                details: new Dictionary<string, object?> { ["recordId"] = record.Id }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Option<RetentionRecord>>> GetByIdAsync(
        string recordId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recordId);

        try
        {
            var sql = $@"
                SELECT Id, EntityId, DataCategory, PolicyId, CreatedAtUtc, ExpiresAtUtc, StatusValue, DeletedAtUtc, LegalHoldId
                FROM {_tableName}
                WHERE Id = @Id";

            var rows = await _connection.QueryAsync(sql, new { Id = recordId });
            var row = rows.FirstOrDefault();

            if (row is null)
            {
                return Right<EncinaError, Option<RetentionRecord>>(None);
            }

            var entity = MapToEntity(row);
            var domain = RetentionRecordMapper.ToDomain(entity);

            return domain is null
                ? Right<EncinaError, Option<RetentionRecord>>(None)
                : Right<EncinaError, Option<RetentionRecord>>(Some(domain));
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "retention.store_error",
                message: $"Failed to get retention record: {ex.Message}",
                details: new Dictionary<string, object?> { ["recordId"] = recordId }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<RetentionRecord>>> GetByEntityIdAsync(
        string entityId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);

        try
        {
            var sql = $@"
                SELECT Id, EntityId, DataCategory, PolicyId, CreatedAtUtc, ExpiresAtUtc, StatusValue, DeletedAtUtc, LegalHoldId
                FROM {_tableName}
                WHERE EntityId = @EntityId";

            var rows = await _connection.QueryAsync(sql, new { EntityId = entityId });
            var records = rows
                .Select(row => RetentionRecordMapper.ToDomain(MapToEntity(row)))
                .Where(r => r is not null)
                .Cast<RetentionRecord>()
                .ToList();

            return Right<EncinaError, IReadOnlyList<RetentionRecord>>(records);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "retention.store_error",
                message: $"Failed to get retention records by entity: {ex.Message}",
                details: new Dictionary<string, object?> { ["entityId"] = entityId }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<RetentionRecord>>> GetExpiredRecordsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var nowUtc = _timeProvider.GetUtcNow();
            var sql = $@"
                SELECT Id, EntityId, DataCategory, PolicyId, CreatedAtUtc, ExpiresAtUtc, StatusValue, DeletedAtUtc, LegalHoldId
                FROM {_tableName}
                WHERE ExpiresAtUtc < @NowUtc AND StatusValue = @ActiveStatus
                ORDER BY ExpiresAtUtc ASC";

            var rows = await _connection.QueryAsync(sql, new
            {
                NowUtc = nowUtc.UtcDateTime,
                ActiveStatus = (int)RetentionStatus.Active
            });

            var records = rows
                .Select(row => RetentionRecordMapper.ToDomain(MapToEntity(row)))
                .Where(r => r is not null)
                .Cast<RetentionRecord>()
                .ToList();

            return Right<EncinaError, IReadOnlyList<RetentionRecord>>(records);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "retention.store_error",
                message: $"Failed to get expired retention records: {ex.Message}",
                details: new Dictionary<string, object?>()));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<RetentionRecord>>> GetExpiringWithinAsync(
        TimeSpan within,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var nowUtc = _timeProvider.GetUtcNow();
            var windowEnd = nowUtc.Add(within);
            var sql = $@"
                SELECT Id, EntityId, DataCategory, PolicyId, CreatedAtUtc, ExpiresAtUtc, StatusValue, DeletedAtUtc, LegalHoldId
                FROM {_tableName}
                WHERE ExpiresAtUtc BETWEEN @NowUtc AND @WindowEnd AND StatusValue = @ActiveStatus
                ORDER BY ExpiresAtUtc ASC";

            var rows = await _connection.QueryAsync(sql, new
            {
                NowUtc = nowUtc.UtcDateTime,
                WindowEnd = windowEnd.UtcDateTime,
                ActiveStatus = (int)RetentionStatus.Active
            });

            var records = rows
                .Select(row => RetentionRecordMapper.ToDomain(MapToEntity(row)))
                .Where(r => r is not null)
                .Cast<RetentionRecord>()
                .ToList();

            return Right<EncinaError, IReadOnlyList<RetentionRecord>>(records);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "retention.store_error",
                message: $"Failed to get expiring retention records: {ex.Message}",
                details: new Dictionary<string, object?> { ["within"] = within.ToString() }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> UpdateStatusAsync(
        string recordId,
        RetentionStatus newStatus,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recordId);

        try
        {
            var nowUtc = _timeProvider.GetUtcNow();
            DateTime? deletedAtUtc = newStatus == RetentionStatus.Deleted ? nowUtc.UtcDateTime : null;

            var sql = $@"
                UPDATE {_tableName}
                SET StatusValue = @StatusValue,
                    DeletedAtUtc = @DeletedAtUtc
                WHERE Id = @Id";

            await _connection.ExecuteAsync(sql, new
            {
                Id = recordId,
                StatusValue = (int)newStatus,
                DeletedAtUtc = deletedAtUtc
            });

            return Right(Unit.Default);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "retention.store_error",
                message: $"Failed to update retention record status: {ex.Message}",
                details: new Dictionary<string, object?> { ["recordId"] = recordId, ["newStatus"] = newStatus.ToString() }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<RetentionRecord>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sql = $@"
                SELECT Id, EntityId, DataCategory, PolicyId, CreatedAtUtc, ExpiresAtUtc, StatusValue, DeletedAtUtc, LegalHoldId
                FROM {_tableName}";

            var rows = await _connection.QueryAsync(sql);
            var records = rows
                .Select(row => RetentionRecordMapper.ToDomain(MapToEntity(row)))
                .Where(r => r is not null)
                .Cast<RetentionRecord>()
                .ToList();

            return Right<EncinaError, IReadOnlyList<RetentionRecord>>(records);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "retention.store_error",
                message: $"Failed to get all retention records: {ex.Message}",
                details: new Dictionary<string, object?>()));
        }
    }

    private static RetentionRecordEntity MapToEntity(dynamic row)
    {
        return new RetentionRecordEntity
        {
            Id = (string)row.Id,
            EntityId = (string)row.EntityId,
            DataCategory = (string)row.DataCategory,
            PolicyId = row.PolicyId is null or DBNull ? null : (string)row.PolicyId,
            CreatedAtUtc = new DateTimeOffset((DateTime)row.CreatedAtUtc, TimeSpan.Zero),
            ExpiresAtUtc = new DateTimeOffset((DateTime)row.ExpiresAtUtc, TimeSpan.Zero),
            StatusValue = Convert.ToInt32(row.StatusValue),
            DeletedAtUtc = row.DeletedAtUtc is null or DBNull
                ? null
                : new DateTimeOffset((DateTime)row.DeletedAtUtc, TimeSpan.Zero),
            LegalHoldId = row.LegalHoldId is null or DBNull ? null : (string)row.LegalHoldId
        };
    }
}
