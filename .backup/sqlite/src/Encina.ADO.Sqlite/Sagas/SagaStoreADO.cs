using System.Data;
using System.Globalization;
using Encina.Messaging;
using Encina.Messaging.Sagas;
using LanguageExt;
using Microsoft.Data.Sqlite;

namespace Encina.ADO.Sqlite.Sagas;

/// <summary>
/// ADO.NET implementation of <see cref="ISagaStore"/> for saga orchestration.
/// Uses raw SqliteCommand and SqliteDataReader for maximum performance and zero overhead.
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
    /// <param name="tableName">The saga state table name (default: SagaStates).</param>
    /// <param name="timeProvider">The time provider for UTC time (default: <see cref="TimeProvider.System"/>).</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="connection"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="tableName"/> is null or whitespace.</exception>
    public SagaStoreADO(
        IDbConnection connection,
        string tableName = "SagaStates",
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
        _tableName = SqlIdentifierValidator.ValidateTableName(tableName);
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Option<ISagaState>>> GetAsync(Guid sagaId, CancellationToken cancellationToken = default)
    {
        if (sagaId == Guid.Empty)
            throw new ArgumentException(StoreValidationMessages.SagaIdCannotBeEmpty, nameof(sagaId));

        return await EitherHelpers.TryAsync(async () =>
        {
            var sql = $@"
                SELECT *
                FROM {_tableName}
                WHERE SagaId = @SagaId";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@SagaId", sagaId.ToString());

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            if (await ReadAsync(reader, cancellationToken))
            {
                return Option<ISagaState>.Some(MapToSagaState(reader));
            }

            return Option<ISagaState>.None;
        }, "saga.get_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> AddAsync(ISagaState sagaState, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sagaState);

        return await EitherHelpers.TryAsync(async () =>
        {
            var sql = $@"
                INSERT INTO {_tableName}
                (SagaId, SagaType, Data, Status, StartedAtUtc, LastUpdatedAtUtc, CompletedAtUtc, ErrorMessage, CurrentStep, TimeoutAtUtc)
                VALUES
                (@SagaId, @SagaType, @Data, @Status, @StartedAtUtc, @LastUpdatedAtUtc, @CompletedAtUtc, @ErrorMessage, @CurrentStep, @TimeoutAtUtc)";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@SagaId", sagaState.SagaId.ToString());
            AddParameter(command, "@SagaType", sagaState.SagaType);
            AddParameter(command, "@Data", sagaState.Data);
            AddParameter(command, "@Status", sagaState.Status);
            AddParameter(command, "@StartedAtUtc", sagaState.StartedAtUtc.ToString("O"));
            AddParameter(command, "@LastUpdatedAtUtc", sagaState.LastUpdatedAtUtc.ToString("O"));
            AddParameter(command, "@CompletedAtUtc", sagaState.CompletedAtUtc?.ToString("O"));
            AddParameter(command, "@ErrorMessage", sagaState.ErrorMessage);
            AddParameter(command, "@CurrentStep", sagaState.CurrentStep);
            AddParameter(command, "@TimeoutAtUtc", sagaState.TimeoutAtUtc?.ToString("O"));

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            await ExecuteNonQueryAsync(command, cancellationToken);
        }, "saga.add_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> UpdateAsync(ISagaState sagaState, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sagaState);

        return await EitherHelpers.TryAsync(async () =>
        {
            var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
            var sql = $@"
                UPDATE {_tableName}
                SET SagaType = @SagaType,
                    Data = @Data,
                    Status = @Status,
                    LastUpdatedAtUtc = @NowUtc,
                    CompletedAtUtc = @CompletedAtUtc,
                    ErrorMessage = @ErrorMessage,
                    CurrentStep = @CurrentStep,
                    TimeoutAtUtc = @TimeoutAtUtc
                WHERE SagaId = @SagaId";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@SagaId", sagaState.SagaId.ToString());
            AddParameter(command, "@SagaType", sagaState.SagaType);
            AddParameter(command, "@Data", sagaState.Data);
            AddParameter(command, "@Status", sagaState.Status);
            AddParameter(command, "@NowUtc", nowUtc.ToString("O"));
            AddParameter(command, "@CompletedAtUtc", sagaState.CompletedAtUtc?.ToString("O"));
            AddParameter(command, "@ErrorMessage", sagaState.ErrorMessage);
            AddParameter(command, "@CurrentStep", sagaState.CurrentStep);
            AddParameter(command, "@TimeoutAtUtc", sagaState.TimeoutAtUtc?.ToString("O"));

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            await ExecuteNonQueryAsync(command, cancellationToken);
        }, "saga.update_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, IEnumerable<ISagaState>>> GetStuckSagasAsync(
        TimeSpan olderThan,
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        if (olderThan <= TimeSpan.Zero)
            throw new ArgumentException(StoreValidationMessages.OlderThanMustBeGreaterThanZero, nameof(olderThan));
        if (batchSize <= 0)
            throw new ArgumentException(StoreValidationMessages.BatchSizeMustBeGreaterThanZero, nameof(batchSize));

        return await EitherHelpers.TryAsync(async () =>
        {
            var thresholdUtc = _timeProvider.GetUtcNow().UtcDateTime.Subtract(olderThan);

            var sql = $@"
                SELECT *
                FROM {_tableName}
                WHERE (Status = @Running OR Status = @Compensating)
                  AND LastUpdatedAtUtc < @ThresholdUtc
                ORDER BY LastUpdatedAtUtc
                LIMIT @BatchSize";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@Running", "Running");
            AddParameter(command, "@Compensating", "Compensating");
            AddParameter(command, "@ThresholdUtc", thresholdUtc.ToString("O"));
            AddParameter(command, "@BatchSize", batchSize);

            var sagas = new List<ISagaState>();

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            while (await ReadAsync(reader, cancellationToken))
            {
                sagas.Add(MapToSagaState(reader));
            }

            return (IEnumerable<ISagaState>)sagas;
        }, "saga.get_stuck_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, IEnumerable<ISagaState>>> GetExpiredSagasAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        if (batchSize <= 0)
            throw new ArgumentException(StoreValidationMessages.BatchSizeMustBeGreaterThanZero, nameof(batchSize));

        return await EitherHelpers.TryAsync(async () =>
        {
            var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

            var sql = $@"
                SELECT *
                FROM {_tableName}
                WHERE (Status = @Running OR Status = @Compensating)
                  AND TimeoutAtUtc IS NOT NULL
                  AND TimeoutAtUtc <= @NowUtc
                ORDER BY TimeoutAtUtc
                LIMIT @BatchSize";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@Running", "Running");
            AddParameter(command, "@Compensating", "Compensating");
            AddParameter(command, "@NowUtc", nowUtc.ToString("O"));
            AddParameter(command, "@BatchSize", batchSize);

            var sagas = new List<ISagaState>();

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            while (await ReadAsync(reader, cancellationToken))
            {
                sagas.Add(MapToSagaState(reader));
            }

            return (IEnumerable<ISagaState>)sagas;
        }, "saga.get_expired_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<Either<EncinaError, Unit>> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // ADO.NET executes SQL immediately, no need for SaveChanges
        return Task.FromResult<Either<EncinaError, Unit>>(Unit.Default);
    }

    private static SagaState MapToSagaState(IDataReader reader)
    {
        return new SagaState
        {
            SagaId = Guid.Parse(reader.GetString(reader.GetOrdinal("SagaId"))),
            SagaType = reader.GetString(reader.GetOrdinal("SagaType")),
            Data = reader.GetString(reader.GetOrdinal("Data")),
            Status = reader.GetString(reader.GetOrdinal("Status")),
            StartedAtUtc = DateTime.Parse(reader.GetString(reader.GetOrdinal("StartedAtUtc")), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            LastUpdatedAtUtc = DateTime.Parse(reader.GetString(reader.GetOrdinal("LastUpdatedAtUtc")), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            CompletedAtUtc = reader.IsDBNull(reader.GetOrdinal("CompletedAtUtc"))
                ? null
                : DateTime.Parse(reader.GetString(reader.GetOrdinal("CompletedAtUtc")), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            ErrorMessage = reader.IsDBNull(reader.GetOrdinal("ErrorMessage"))
                ? null
                : reader.GetString(reader.GetOrdinal("ErrorMessage")),
            CurrentStep = reader.GetInt32(reader.GetOrdinal("CurrentStep")),
            TimeoutAtUtc = reader.IsDBNull(reader.GetOrdinal("TimeoutAtUtc"))
                ? null
                : DateTime.Parse(reader.GetString(reader.GetOrdinal("TimeoutAtUtc")), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
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
        if (command is SqliteCommand sqlCommand)
            return await sqlCommand.ExecuteReaderAsync(cancellationToken);

        return await Task.Run(command.ExecuteReader, cancellationToken);
    }

    private static async Task<int> ExecuteNonQueryAsync(IDbCommand command, CancellationToken cancellationToken)
    {
        if (command is SqliteCommand sqlCommand)
            return await sqlCommand.ExecuteNonQueryAsync(cancellationToken);

        return await Task.Run(command.ExecuteNonQuery, cancellationToken);
    }

    private static async Task<bool> ReadAsync(IDataReader reader, CancellationToken cancellationToken)
    {
        if (reader is SqliteDataReader sqlReader)
            return await sqlReader.ReadAsync(cancellationToken);

        return await Task.Run(reader.Read, cancellationToken);
    }
}
