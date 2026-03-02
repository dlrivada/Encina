using System.Data;
using Encina.Compliance.Retention;
using Encina.Compliance.Retention.Model;
using Encina.Messaging;
using LanguageExt;
using Microsoft.Data.SqlClient;
using static LanguageExt.Prelude;

namespace Encina.ADO.SqlServer.Retention;

/// <summary>
/// ADO.NET implementation of <see cref="IRetentionRecordStore"/> for SQL Server.
/// Tracks the retention lifecycle of individual data entities per GDPR Article 5(1)(e).
/// </summary>
public sealed class RetentionRecordStoreADO : IRetentionRecordStore
{
    private readonly IDbConnection _connection;
    private readonly string _tableName;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="RetentionRecordStoreADO"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="tableName">The retention records table name (default: RetentionRecords).</param>
    /// <param name="timeProvider">The time provider for UTC time (default: <see cref="TimeProvider.System"/>).</param>
    public RetentionRecordStoreADO(
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

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@Id", entity.Id);
            AddParameter(command, "@EntityId", entity.EntityId);
            AddParameter(command, "@DataCategory", entity.DataCategory);
            AddParameter(command, "@PolicyId", entity.PolicyId);
            AddParameter(command, "@CreatedAtUtc", entity.CreatedAtUtc.UtcDateTime);
            AddParameter(command, "@ExpiresAtUtc", entity.ExpiresAtUtc.UtcDateTime);
            AddParameter(command, "@StatusValue", entity.StatusValue);
            AddParameter(command, "@DeletedAtUtc", entity.DeletedAtUtc?.UtcDateTime);
            AddParameter(command, "@LegalHoldId", entity.LegalHoldId);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            await ExecuteNonQueryAsync(command, cancellationToken);
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

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@Id", recordId);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            if (await ReadAsync(reader, cancellationToken))
            {
                var entity = MapToEntity(reader);
                var domain = RetentionRecordMapper.ToDomain(entity);
                return domain is not null
                    ? Right<EncinaError, Option<RetentionRecord>>(Some(domain))
                    : Right<EncinaError, Option<RetentionRecord>>(None);
            }

            return Right<EncinaError, Option<RetentionRecord>>(None);
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

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@EntityId", entityId);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            var records = new List<RetentionRecord>();
            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            while (await ReadAsync(reader, cancellationToken))
            {
                var entity = MapToEntity(reader);
                var domain = RetentionRecordMapper.ToDomain(entity);
                if (domain is not null)
                    records.Add(domain);
            }

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

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@NowUtc", nowUtc.UtcDateTime);
            AddParameter(command, "@ActiveStatus", (int)RetentionStatus.Active);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            var records = new List<RetentionRecord>();
            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            while (await ReadAsync(reader, cancellationToken))
            {
                var entity = MapToEntity(reader);
                var domain = RetentionRecordMapper.ToDomain(entity);
                if (domain is not null)
                    records.Add(domain);
            }

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

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@NowUtc", nowUtc.UtcDateTime);
            AddParameter(command, "@WindowEnd", windowEnd.UtcDateTime);
            AddParameter(command, "@ActiveStatus", (int)RetentionStatus.Active);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            var records = new List<RetentionRecord>();
            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            while (await ReadAsync(reader, cancellationToken))
            {
                var entity = MapToEntity(reader);
                var domain = RetentionRecordMapper.ToDomain(entity);
                if (domain is not null)
                    records.Add(domain);
            }

            return Right<EncinaError, IReadOnlyList<RetentionRecord>>(records);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "retention.store_error",
                message: $"Failed to get expiring retention records: {ex.Message}",
                details: new Dictionary<string, object?> { ["within"] = within }));
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
            var deletedAtUtc = newStatus == RetentionStatus.Deleted
                ? (object?)_timeProvider.GetUtcNow().UtcDateTime
                : null;

            var sql = $@"
                UPDATE {_tableName}
                SET StatusValue = @StatusValue,
                    DeletedAtUtc = @DeletedAtUtc
                WHERE Id = @Id";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@Id", recordId);
            AddParameter(command, "@StatusValue", (int)newStatus);
            AddParameter(command, "@DeletedAtUtc", deletedAtUtc);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            await ExecuteNonQueryAsync(command, cancellationToken);
            return Right(Unit.Default);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "retention.store_error",
                message: $"Failed to update retention record status: {ex.Message}",
                details: new Dictionary<string, object?> { ["recordId"] = recordId, ["newStatus"] = newStatus }));
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

            using var command = _connection.CreateCommand();
            command.CommandText = sql;

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            var records = new List<RetentionRecord>();
            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            while (await ReadAsync(reader, cancellationToken))
            {
                var entity = MapToEntity(reader);
                var domain = RetentionRecordMapper.ToDomain(entity);
                if (domain is not null)
                    records.Add(domain);
            }

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

    private static RetentionRecordEntity MapToEntity(IDataReader reader)
    {
        var deletedAtOrd = reader.GetOrdinal("DeletedAtUtc");
        var legalHoldIdOrd = reader.GetOrdinal("LegalHoldId");
        var policyIdOrd = reader.GetOrdinal("PolicyId");

        return new RetentionRecordEntity
        {
            Id = reader.GetString(reader.GetOrdinal("Id")),
            EntityId = reader.GetString(reader.GetOrdinal("EntityId")),
            DataCategory = reader.GetString(reader.GetOrdinal("DataCategory")),
            PolicyId = reader.IsDBNull(policyIdOrd) ? null : reader.GetString(policyIdOrd),
            CreatedAtUtc = new DateTimeOffset(reader.GetDateTime(reader.GetOrdinal("CreatedAtUtc")), TimeSpan.Zero),
            ExpiresAtUtc = new DateTimeOffset(reader.GetDateTime(reader.GetOrdinal("ExpiresAtUtc")), TimeSpan.Zero),
            StatusValue = reader.GetInt32(reader.GetOrdinal("StatusValue")),
            DeletedAtUtc = reader.IsDBNull(deletedAtOrd)
                ? null
                : new DateTimeOffset(reader.GetDateTime(deletedAtOrd), TimeSpan.Zero),
            LegalHoldId = reader.IsDBNull(legalHoldIdOrd) ? null : reader.GetString(legalHoldIdOrd)
        };
    }

    private static void AddParameter(IDbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    private static Task OpenConnectionAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    private static async Task<IDataReader> ExecuteReaderAsync(IDbCommand command, CancellationToken cancellationToken)
    {
        if (command is SqlCommand sqlCommand)
            return await sqlCommand.ExecuteReaderAsync(cancellationToken);

        return await Task.Run(command.ExecuteReader, cancellationToken);
    }

    private static async Task<int> ExecuteNonQueryAsync(IDbCommand command, CancellationToken cancellationToken)
    {
        if (command is SqlCommand sqlCommand)
            return await sqlCommand.ExecuteNonQueryAsync(cancellationToken);

        return await Task.Run(command.ExecuteNonQuery, cancellationToken);
    }

    private static async Task<bool> ReadAsync(IDataReader reader, CancellationToken cancellationToken)
    {
        if (reader is SqlDataReader sqlReader)
            return await sqlReader.ReadAsync(cancellationToken);

        return await Task.Run(reader.Read, cancellationToken);
    }
}
