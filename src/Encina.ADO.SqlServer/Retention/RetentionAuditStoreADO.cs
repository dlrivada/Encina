using System.Data;
using Encina.Compliance.Retention;
using Encina.Compliance.Retention.Model;
using Encina.Messaging;
using LanguageExt;
using Microsoft.Data.SqlClient;
using static LanguageExt.Prelude;

namespace Encina.ADO.SqlServer.Retention;

/// <summary>
/// ADO.NET implementation of <see cref="IRetentionAuditStore"/> for SQL Server.
/// Provides immutable audit trail for GDPR Article 5(2) accountability.
/// </summary>
public sealed class RetentionAuditStoreADO : IRetentionAuditStore
{
    private readonly IDbConnection _connection;
    private readonly string _tableName;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="RetentionAuditStoreADO"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="tableName">The audit entries table name (default: RetentionAuditEntries).</param>
    /// <param name="timeProvider">The time provider for UTC time (default: <see cref="TimeProvider.System"/>).</param>
    public RetentionAuditStoreADO(
        IDbConnection connection,
        string tableName = "RetentionAuditEntries",
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
        _tableName = SqlIdentifierValidator.ValidateTableName(tableName);
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> RecordAsync(
        RetentionAuditEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        try
        {
            var entity = RetentionAuditEntryMapper.ToEntity(entry);
            var sql = $@"
                INSERT INTO {_tableName}
                (Id, Action, EntityId, DataCategory, Detail, PerformedByUserId, OccurredAtUtc)
                VALUES
                (@Id, @Action, @EntityId, @DataCategory, @Detail, @PerformedByUserId, @OccurredAtUtc)";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@Id", entity.Id);
            AddParameter(command, "@Action", entity.Action);
            AddParameter(command, "@EntityId", entity.EntityId);
            AddParameter(command, "@DataCategory", entity.DataCategory);
            AddParameter(command, "@Detail", entity.Detail);
            AddParameter(command, "@PerformedByUserId", entity.PerformedByUserId);
            AddParameter(command, "@OccurredAtUtc", entity.OccurredAtUtc.UtcDateTime);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            await ExecuteNonQueryAsync(command, cancellationToken);
            return Right(Unit.Default);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "retention.audit_store_error",
                message: $"Failed to record retention audit entry: {ex.Message}",
                details: new Dictionary<string, object?> { ["entityId"] = entry.EntityId, ["action"] = entry.Action }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<RetentionAuditEntry>>> GetByEntityIdAsync(
        string entityId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);

        try
        {
            var sql = $@"
                SELECT Id, Action, EntityId, DataCategory, Detail, PerformedByUserId, OccurredAtUtc
                FROM {_tableName}
                WHERE EntityId = @EntityId
                ORDER BY OccurredAtUtc DESC";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@EntityId", entityId);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            var entries = new List<RetentionAuditEntry>();
            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            while (await ReadAsync(reader, cancellationToken))
            {
                var entity = MapToEntity(reader);
                entries.Add(RetentionAuditEntryMapper.ToDomain(entity));
            }

            return Right<EncinaError, IReadOnlyList<RetentionAuditEntry>>(entries);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "retention.audit_store_error",
                message: $"Failed to get retention audit trail: {ex.Message}",
                details: new Dictionary<string, object?> { ["entityId"] = entityId }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<RetentionAuditEntry>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sql = $@"
                SELECT Id, Action, EntityId, DataCategory, Detail, PerformedByUserId, OccurredAtUtc
                FROM {_tableName}
                ORDER BY OccurredAtUtc DESC";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            var entries = new List<RetentionAuditEntry>();
            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            while (await ReadAsync(reader, cancellationToken))
            {
                var entity = MapToEntity(reader);
                entries.Add(RetentionAuditEntryMapper.ToDomain(entity));
            }

            return Right<EncinaError, IReadOnlyList<RetentionAuditEntry>>(entries);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "retention.audit_store_error",
                message: $"Failed to get all retention audit entries: {ex.Message}",
                details: new Dictionary<string, object?>()));
        }
    }

    private static RetentionAuditEntryEntity MapToEntity(IDataReader reader)
    {
        var entityIdOrd = reader.GetOrdinal("EntityId");
        var dataCategoryOrd = reader.GetOrdinal("DataCategory");
        var detailOrd = reader.GetOrdinal("Detail");
        var performedByOrd = reader.GetOrdinal("PerformedByUserId");

        return new RetentionAuditEntryEntity
        {
            Id = reader.GetString(reader.GetOrdinal("Id")),
            Action = reader.GetString(reader.GetOrdinal("Action")),
            EntityId = reader.IsDBNull(entityIdOrd) ? null : reader.GetString(entityIdOrd),
            DataCategory = reader.IsDBNull(dataCategoryOrd) ? null : reader.GetString(dataCategoryOrd),
            Detail = reader.IsDBNull(detailOrd) ? null : reader.GetString(detailOrd),
            PerformedByUserId = reader.IsDBNull(performedByOrd) ? null : reader.GetString(performedByOrd),
            OccurredAtUtc = new DateTimeOffset(reader.GetDateTime(reader.GetOrdinal("OccurredAtUtc")), TimeSpan.Zero)
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
