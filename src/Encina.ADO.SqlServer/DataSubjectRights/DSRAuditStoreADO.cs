using System.Data;
using Encina.Compliance.DataSubjectRights;
using Encina.Messaging;
using LanguageExt;
using Microsoft.Data.SqlClient;
using static LanguageExt.Prelude;

namespace Encina.ADO.SqlServer.DataSubjectRights;

/// <summary>
/// ADO.NET implementation of <see cref="IDSRAuditStore"/> for SQL Server.
/// Uses raw SqlCommand and SqlDataReader for maximum performance.
/// </summary>
public sealed class DSRAuditStoreADO : IDSRAuditStore
{
    private readonly IDbConnection _connection;
    private readonly string _tableName;

    /// <summary>
    /// Initializes a new instance of the <see cref="DSRAuditStoreADO"/> class.
    /// </summary>
    public DSRAuditStoreADO(
        IDbConnection connection,
        string tableName = "DSRAuditEntries")
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
        _tableName = SqlIdentifierValidator.ValidateTableName(tableName);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> RecordAsync(
        DSRAuditEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        try
        {
            var entity = DSRAuditEntryMapper.ToEntity(entry);

            var sql = $@"
                INSERT INTO {_tableName}
                (Id, DSRRequestId, Action, Detail, PerformedByUserId, OccurredAtUtc)
                VALUES
                (@Id, @DSRRequestId, @Action, @Detail, @PerformedByUserId, @OccurredAtUtc)";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@Id", entity.Id);
            AddParameter(command, "@DSRRequestId", entity.DSRRequestId);
            AddParameter(command, "@Action", entity.Action);
            AddParameter(command, "@Detail", entity.Detail);
            AddParameter(command, "@PerformedByUserId", entity.PerformedByUserId);
            AddParameter(command, "@OccurredAtUtc", entity.OccurredAtUtc);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            await ExecuteNonQueryAsync(command, cancellationToken);
            return Right(Unit.Default);
        }
        catch (Exception ex)
        {
            return Left(DSRErrors.StoreError("Record", ex.Message));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<DSRAuditEntry>>> GetAuditTrailAsync(
        string dsrRequestId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dsrRequestId);

        try
        {
            var sql = $@"
                SELECT Id, DSRRequestId, Action, Detail, PerformedByUserId, OccurredAtUtc
                FROM {_tableName}
                WHERE DSRRequestId = @DSRRequestId
                ORDER BY OccurredAtUtc";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@DSRRequestId", dsrRequestId);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            var results = new List<DSRAuditEntry>();
            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            while (await ReadAsync(reader, cancellationToken))
            {
                results.Add(MapToDomain(reader));
            }

            return Right<EncinaError, IReadOnlyList<DSRAuditEntry>>(results);
        }
        catch (Exception ex)
        {
            return Left(DSRErrors.StoreError("GetAuditTrail", ex.Message));
        }
    }

    private static DSRAuditEntry MapToDomain(IDataReader reader)
    {
        var entity = new DSRAuditEntryEntity
        {
            Id = reader.GetString(reader.GetOrdinal("Id")),
            DSRRequestId = reader.GetString(reader.GetOrdinal("DSRRequestId")),
            Action = reader.GetString(reader.GetOrdinal("Action")),
            Detail = reader.IsDBNull(reader.GetOrdinal("Detail"))
                ? null
                : reader.GetString(reader.GetOrdinal("Detail")),
            PerformedByUserId = reader.IsDBNull(reader.GetOrdinal("PerformedByUserId"))
                ? null
                : reader.GetString(reader.GetOrdinal("PerformedByUserId")),
            OccurredAtUtc = (DateTimeOffset)reader.GetValue(reader.GetOrdinal("OccurredAtUtc"))
        };

        return DSRAuditEntryMapper.ToDomain(entity);
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
