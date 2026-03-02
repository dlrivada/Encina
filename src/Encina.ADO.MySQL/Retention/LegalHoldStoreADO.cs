using System.Data;
using Encina.Compliance.Retention;
using Encina.Compliance.Retention.Model;
using Encina.Messaging;
using LanguageExt;
using MySqlConnector;
using static LanguageExt.Prelude;

namespace Encina.ADO.MySQL.Retention;

/// <summary>
/// ADO.NET implementation of <see cref="ILegalHoldStore"/> for MySQL.
/// Manages litigation holds per GDPR Article 17(3)(e) legal claims exemption.
/// </summary>
public sealed class LegalHoldStoreADO : ILegalHoldStore
{
    private readonly IDbConnection _connection;
    private readonly string _tableName;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="LegalHoldStoreADO"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="tableName">The legal holds table name (default: LegalHolds).</param>
    /// <param name="timeProvider">The time provider for UTC time (default: <see cref="TimeProvider.System"/>).</param>
    public LegalHoldStoreADO(
        IDbConnection connection,
        string tableName = "LegalHolds",
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
        _tableName = SqlIdentifierValidator.ValidateTableName(tableName);
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> CreateAsync(
        LegalHold hold,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(hold);

        try
        {
            var entity = LegalHoldMapper.ToEntity(hold);
            var sql = $@"
                INSERT INTO {_tableName}
                (Id, EntityId, Reason, AppliedByUserId, AppliedAtUtc, ReleasedAtUtc, ReleasedByUserId)
                VALUES
                (@Id, @EntityId, @Reason, @AppliedByUserId, @AppliedAtUtc, @ReleasedAtUtc, @ReleasedByUserId)";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@Id", entity.Id);
            AddParameter(command, "@EntityId", entity.EntityId);
            AddParameter(command, "@Reason", entity.Reason);
            AddParameter(command, "@AppliedByUserId", entity.AppliedByUserId);
            AddParameter(command, "@AppliedAtUtc", entity.AppliedAtUtc.UtcDateTime);
            AddParameter(command, "@ReleasedAtUtc", entity.ReleasedAtUtc?.UtcDateTime);
            AddParameter(command, "@ReleasedByUserId", entity.ReleasedByUserId);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            await ExecuteNonQueryAsync(command, cancellationToken);
            return Right(Unit.Default);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "retention.store_error",
                message: $"Failed to create legal hold: {ex.Message}",
                details: new Dictionary<string, object?> { ["holdId"] = hold.Id }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Option<LegalHold>>> GetByIdAsync(
        string holdId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(holdId);

        try
        {
            var sql = $@"
                SELECT Id, EntityId, Reason, AppliedByUserId, AppliedAtUtc, ReleasedAtUtc, ReleasedByUserId
                FROM {_tableName}
                WHERE Id = @Id";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@Id", holdId);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            if (await ReadAsync(reader, cancellationToken))
            {
                var entity = MapToEntity(reader);
                return Right<EncinaError, Option<LegalHold>>(Some(LegalHoldMapper.ToDomain(entity)));
            }

            return Right<EncinaError, Option<LegalHold>>(None);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "retention.store_error",
                message: $"Failed to get legal hold: {ex.Message}",
                details: new Dictionary<string, object?> { ["holdId"] = holdId }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<LegalHold>>> GetByEntityIdAsync(
        string entityId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);

        try
        {
            var sql = $@"
                SELECT Id, EntityId, Reason, AppliedByUserId, AppliedAtUtc, ReleasedAtUtc, ReleasedByUserId
                FROM {_tableName}
                WHERE EntityId = @EntityId";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@EntityId", entityId);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            var holds = new List<LegalHold>();
            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            while (await ReadAsync(reader, cancellationToken))
            {
                var entity = MapToEntity(reader);
                holds.Add(LegalHoldMapper.ToDomain(entity));
            }

            return Right<EncinaError, IReadOnlyList<LegalHold>>(holds);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "retention.store_error",
                message: $"Failed to get legal holds by entity: {ex.Message}",
                details: new Dictionary<string, object?> { ["entityId"] = entityId }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, bool>> IsUnderHoldAsync(
        string entityId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);

        try
        {
            var sql = $@"
                SELECT CASE WHEN EXISTS (
                    SELECT 1 FROM {_tableName} WHERE EntityId = @EntityId AND ReleasedAtUtc IS NULL
                ) THEN 1 ELSE 0 END";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@EntityId", entityId);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            if (await ReadAsync(reader, cancellationToken))
            {
                return Right(reader.GetInt32(0) != 0);
            }

            return Right(false);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "retention.store_error",
                message: $"Failed to check legal hold status: {ex.Message}",
                details: new Dictionary<string, object?> { ["entityId"] = entityId }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<LegalHold>>> GetActiveHoldsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sql = $@"
                SELECT Id, EntityId, Reason, AppliedByUserId, AppliedAtUtc, ReleasedAtUtc, ReleasedByUserId
                FROM {_tableName}
                WHERE ReleasedAtUtc IS NULL";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            var holds = new List<LegalHold>();
            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            while (await ReadAsync(reader, cancellationToken))
            {
                var entity = MapToEntity(reader);
                holds.Add(LegalHoldMapper.ToDomain(entity));
            }

            return Right<EncinaError, IReadOnlyList<LegalHold>>(holds);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "retention.store_error",
                message: $"Failed to get active legal holds: {ex.Message}",
                details: new Dictionary<string, object?>()));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> ReleaseAsync(
        string holdId,
        string? releasedByUserId,
        DateTimeOffset releasedAtUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(holdId);

        try
        {
            var sql = $@"
                UPDATE {_tableName}
                SET ReleasedAtUtc = @ReleasedAtUtc,
                    ReleasedByUserId = @ReleasedByUserId
                WHERE Id = @Id";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@Id", holdId);
            AddParameter(command, "@ReleasedAtUtc", releasedAtUtc.UtcDateTime);
            AddParameter(command, "@ReleasedByUserId", releasedByUserId);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            await ExecuteNonQueryAsync(command, cancellationToken);
            return Right(Unit.Default);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "retention.store_error",
                message: $"Failed to release legal hold: {ex.Message}",
                details: new Dictionary<string, object?> { ["holdId"] = holdId }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<LegalHold>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sql = $@"
                SELECT Id, EntityId, Reason, AppliedByUserId, AppliedAtUtc, ReleasedAtUtc, ReleasedByUserId
                FROM {_tableName}";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            var holds = new List<LegalHold>();
            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            while (await ReadAsync(reader, cancellationToken))
            {
                var entity = MapToEntity(reader);
                holds.Add(LegalHoldMapper.ToDomain(entity));
            }

            return Right<EncinaError, IReadOnlyList<LegalHold>>(holds);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "retention.store_error",
                message: $"Failed to get all legal holds: {ex.Message}",
                details: new Dictionary<string, object?>()));
        }
    }

    private static LegalHoldEntity MapToEntity(IDataReader reader)
    {
        var appliedByOrd = reader.GetOrdinal("AppliedByUserId");
        var releasedAtOrd = reader.GetOrdinal("ReleasedAtUtc");
        var releasedByOrd = reader.GetOrdinal("ReleasedByUserId");

        return new LegalHoldEntity
        {
            Id = reader.GetString(reader.GetOrdinal("Id")),
            EntityId = reader.GetString(reader.GetOrdinal("EntityId")),
            Reason = reader.GetString(reader.GetOrdinal("Reason")),
            AppliedByUserId = reader.IsDBNull(appliedByOrd) ? null : reader.GetString(appliedByOrd),
            AppliedAtUtc = new DateTimeOffset(reader.GetDateTime(reader.GetOrdinal("AppliedAtUtc")), TimeSpan.Zero),
            ReleasedAtUtc = reader.IsDBNull(releasedAtOrd)
                ? null
                : new DateTimeOffset(reader.GetDateTime(releasedAtOrd), TimeSpan.Zero),
            ReleasedByUserId = reader.IsDBNull(releasedByOrd) ? null : reader.GetString(releasedByOrd)
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
        if (command is MySqlCommand mysqlCommand)
            return await mysqlCommand.ExecuteReaderAsync(cancellationToken);

        return await Task.Run(command.ExecuteReader, cancellationToken);
    }

    private static async Task<int> ExecuteNonQueryAsync(IDbCommand command, CancellationToken cancellationToken)
    {
        if (command is MySqlCommand mysqlCommand)
            return await mysqlCommand.ExecuteNonQueryAsync(cancellationToken);

        return await Task.Run(command.ExecuteNonQuery, cancellationToken);
    }

    private static async Task<bool> ReadAsync(IDataReader reader, CancellationToken cancellationToken)
    {
        if (reader is MySqlDataReader mysqlReader)
            return await mysqlReader.ReadAsync(cancellationToken);

        return await Task.Run(reader.Read, cancellationToken);
    }
}
