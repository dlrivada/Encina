using System.Data;
using Encina.Messaging;
using Encina.Messaging.Sagas;
using Npgsql;

namespace Encina.ADO.PostgreSQL.Sagas;

/// <summary>
/// ADO.NET implementation of <see cref="ISagaStore"/> for saga orchestration.
/// Uses raw NpgsqlCommand and NpgsqlDataReader for maximum performance and zero overhead.
/// </summary>
public sealed class SagaStoreADO : ISagaStore
{
    private readonly IDbConnection _connection;
    private readonly string _tableName;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="SagaStoreADO"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="tableName">The saga state table name (default: sagastates).</param>
    /// <param name="timeProvider">The time provider for UTC time (default: <see cref="TimeProvider.System"/>).</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="connection"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="tableName"/> is null or whitespace.</exception>
    public SagaStoreADO(
        IDbConnection connection,
        string tableName = "sagastates",
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
        _tableName = SqlIdentifierValidator.ValidateTableName(tableName);
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public async Task<ISagaState?> GetAsync(Guid sagaId, CancellationToken cancellationToken = default)
    {
        if (sagaId == Guid.Empty)
            throw new ArgumentException(StoreValidationMessages.SagaIdCannotBeEmpty, nameof(sagaId));

        var sql = $@"
            SELECT sagaid, sagatype, data, status, startedatutc, lastupdatedatutc, completedatutc, errormessage, currentstep, timeoutatutc
            FROM {_tableName}
            WHERE sagaid = @SagaId";

        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        AddParameter(command, "@SagaId", sagaId);

        if (_connection.State != ConnectionState.Open)
            await OpenConnectionAsync(cancellationToken);

        using var reader = await ExecuteReaderAsync(command, cancellationToken);
        if (await ReadAsync(reader, cancellationToken))
        {
            return MapToSagaState(reader);
        }

        return null;
    }

    /// <inheritdoc />
    public async Task AddAsync(ISagaState sagaState, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sagaState);

        var sql = $@"
            INSERT INTO {_tableName}
            (sagaid, sagatype, data, status, startedatutc, lastupdatedatutc, completedatutc, errormessage, currentstep, timeoutatutc)
            VALUES
            (@SagaId, @SagaType, @Data, @Status, @StartedAtUtc, @LastUpdatedAtUtc, @CompletedAtUtc, @ErrorMessage, @CurrentStep, @TimeoutAtUtc)";

        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        AddParameter(command, "@SagaId", sagaState.SagaId);
        AddParameter(command, "@SagaType", sagaState.SagaType);
        AddParameter(command, "@Data", sagaState.Data);
        AddParameter(command, "@Status", sagaState.Status);
        AddParameter(command, "@StartedAtUtc", sagaState.StartedAtUtc);
        AddParameter(command, "@LastUpdatedAtUtc", sagaState.LastUpdatedAtUtc);
        AddParameter(command, "@CompletedAtUtc", sagaState.CompletedAtUtc);
        AddParameter(command, "@ErrorMessage", sagaState.ErrorMessage);
        AddParameter(command, "@CurrentStep", sagaState.CurrentStep);
        AddParameter(command, "@TimeoutAtUtc", sagaState.TimeoutAtUtc);

        if (_connection.State != ConnectionState.Open)
            await OpenConnectionAsync(cancellationToken);

        await ExecuteNonQueryAsync(command, cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(ISagaState sagaState, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sagaState);

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var sql = $@"
            UPDATE {_tableName}
            SET sagatype = @SagaType,
                data = @Data,
                status = @Status,
                lastupdatedatutc = @NowUtc,
                completedatutc = @CompletedAtUtc,
                errormessage = @ErrorMessage,
                currentstep = @CurrentStep,
                timeoutatutc = @TimeoutAtUtc
            WHERE sagaid = @SagaId";

        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        AddParameter(command, "@SagaId", sagaState.SagaId);
        AddParameter(command, "@SagaType", sagaState.SagaType);
        AddParameter(command, "@Data", sagaState.Data);
        AddParameter(command, "@Status", sagaState.Status);
        AddParameter(command, "@NowUtc", nowUtc);
        AddParameter(command, "@CompletedAtUtc", sagaState.CompletedAtUtc);
        AddParameter(command, "@ErrorMessage", sagaState.ErrorMessage);
        AddParameter(command, "@CurrentStep", sagaState.CurrentStep);
        AddParameter(command, "@TimeoutAtUtc", sagaState.TimeoutAtUtc);

        if (_connection.State != ConnectionState.Open)
            await OpenConnectionAsync(cancellationToken);

        await ExecuteNonQueryAsync(command, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ISagaState>> GetStuckSagasAsync(
        TimeSpan olderThan,
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        if (olderThan <= TimeSpan.Zero)
            throw new ArgumentException(StoreValidationMessages.OlderThanMustBeGreaterThanZero, nameof(olderThan));
        if (batchSize <= 0)
            throw new ArgumentException(StoreValidationMessages.BatchSizeMustBeGreaterThanZero, nameof(batchSize));

        var thresholdUtc = _timeProvider.GetUtcNow().UtcDateTime.Subtract(olderThan);

        var sql = $@"
            SELECT sagaid, sagatype, data, status, startedatutc, lastupdatedatutc, completedatutc, errormessage, currentstep, timeoutatutc
            FROM {_tableName}
            WHERE (status = @Running OR status = @Compensating)
              AND lastupdatedatutc < @ThresholdUtc
            ORDER BY lastupdatedatutc
            LIMIT @BatchSize";

        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        AddParameter(command, "@Running", "Running");
        AddParameter(command, "@Compensating", "Compensating");
        AddParameter(command, "@ThresholdUtc", thresholdUtc);
        AddParameter(command, "@BatchSize", batchSize);

        var sagas = new List<ISagaState>();

        if (_connection.State != ConnectionState.Open)
            await OpenConnectionAsync(cancellationToken);

        using var reader = await ExecuteReaderAsync(command, cancellationToken);
        while (await ReadAsync(reader, cancellationToken))
        {
            sagas.Add(MapToSagaState(reader));
        }

        return sagas;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ISagaState>> GetExpiredSagasAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        if (batchSize <= 0)
            throw new ArgumentException(StoreValidationMessages.BatchSizeMustBeGreaterThanZero, nameof(batchSize));

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        var sql = $@"
            SELECT sagaid, sagatype, data, status, startedatutc, lastupdatedatutc, completedatutc, errormessage, currentstep, timeoutatutc
            FROM {_tableName}
            WHERE (status = @Running OR status = @Compensating)
              AND timeoutatutc IS NOT NULL
              AND timeoutatutc <= @NowUtc
            ORDER BY timeoutatutc
            LIMIT @BatchSize";

        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        AddParameter(command, "@Running", "Running");
        AddParameter(command, "@Compensating", "Compensating");
        AddParameter(command, "@NowUtc", nowUtc);
        AddParameter(command, "@BatchSize", batchSize);

        var sagas = new List<ISagaState>();

        if (_connection.State != ConnectionState.Open)
            await OpenConnectionAsync(cancellationToken);

        using var reader = await ExecuteReaderAsync(command, cancellationToken);
        while (await ReadAsync(reader, cancellationToken))
        {
            sagas.Add(MapToSagaState(reader));
        }

        return sagas;
    }

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // ADO.NET executes SQL immediately, no need for SaveChanges
        return Task.CompletedTask;
    }

    private static SagaState MapToSagaState(IDataReader reader)
    {
        return new SagaState
        {
            SagaId = reader.GetGuid(reader.GetOrdinal("sagaid")),
            SagaType = reader.GetString(reader.GetOrdinal("sagatype")),
            Data = reader.GetString(reader.GetOrdinal("data")),
            Status = reader.GetString(reader.GetOrdinal("status")),
            StartedAtUtc = reader.GetDateTime(reader.GetOrdinal("startedatutc")),
            LastUpdatedAtUtc = reader.GetDateTime(reader.GetOrdinal("lastupdatedatutc")),
            CompletedAtUtc = reader.IsDBNull(reader.GetOrdinal("completedatutc"))
                ? null
                : reader.GetDateTime(reader.GetOrdinal("completedatutc")),
            ErrorMessage = reader.IsDBNull(reader.GetOrdinal("errormessage"))
                ? null
                : reader.GetString(reader.GetOrdinal("errormessage")),
            CurrentStep = reader.GetInt32(reader.GetOrdinal("currentstep")),
            TimeoutAtUtc = reader.IsDBNull(reader.GetOrdinal("timeoutatutc"))
                ? null
                : reader.GetDateTime(reader.GetOrdinal("timeoutatutc"))
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
        if (command is NpgsqlCommand sqlCommand)
            return await sqlCommand.ExecuteReaderAsync(cancellationToken);

        return await Task.Run(command.ExecuteReader, cancellationToken);
    }

    private static async Task<int> ExecuteNonQueryAsync(IDbCommand command, CancellationToken cancellationToken)
    {
        if (command is NpgsqlCommand sqlCommand)
            return await sqlCommand.ExecuteNonQueryAsync(cancellationToken);

        return await Task.Run(command.ExecuteNonQuery, cancellationToken);
    }

    private static async Task<bool> ReadAsync(IDataReader reader, CancellationToken cancellationToken)
    {
        if (reader is NpgsqlDataReader sqlReader)
            return await sqlReader.ReadAsync(cancellationToken);

        return await Task.Run(reader.Read, cancellationToken);
    }
}
