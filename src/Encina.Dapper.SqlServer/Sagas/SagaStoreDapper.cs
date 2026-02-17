using System.Data;
using Dapper;
using Encina.Messaging;
using Encina.Messaging.Sagas;

namespace Encina.Dapper.SqlServer.Sagas;

/// <summary>
/// Dapper implementation of <see cref="ISagaStore"/> for saga orchestration.
/// Provides persistence and retrieval of saga state for distributed transactions.
/// </summary>
public sealed class SagaStoreDapper : ISagaStore
{
    private readonly IDbConnection _connection;
    private readonly string _tableName;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="SagaStoreDapper"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="tableName">The saga state table name (default: SagaStates).</param>
    /// <param name="timeProvider">Optional time provider for testability. Defaults to <see cref="TimeProvider.System"/>.</param>
    public SagaStoreDapper(IDbConnection connection, string tableName = "SagaStates", TimeProvider? timeProvider = null)
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
            SELECT *
            FROM {_tableName}
            WHERE SagaId = @SagaId";

        return await _connection.QuerySingleOrDefaultAsync<SagaState>(sql, new { SagaId = sagaId });
    }

    /// <inheritdoc />
    public async Task AddAsync(ISagaState sagaState, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sagaState);

        var sql = $@"
            INSERT INTO {_tableName}
            (SagaId, SagaType, Data, Status, StartedAtUtc, LastUpdatedAtUtc, CompletedAtUtc, ErrorMessage, CurrentStep, TimeoutAtUtc)
            VALUES
            (@SagaId, @SagaType, @Data, @Status, @StartedAtUtc, @LastUpdatedAtUtc, @CompletedAtUtc, @ErrorMessage, @CurrentStep, @TimeoutAtUtc)";

        await _connection.ExecuteAsync(sql, sagaState);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(ISagaState sagaState, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sagaState);

        var sql = $@"
            UPDATE {_tableName}
            SET SagaType = @SagaType,
                Data = @Data,
                Status = @Status,
                LastUpdatedAtUtc = GETUTCDATE(),
                CompletedAtUtc = @CompletedAtUtc,
                ErrorMessage = @ErrorMessage,
                CurrentStep = @CurrentStep,
                TimeoutAtUtc = @TimeoutAtUtc
            WHERE SagaId = @SagaId";

        await _connection.ExecuteAsync(sql, sagaState);
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
            SELECT TOP (@BatchSize) *
            FROM {_tableName}
            WHERE (Status = @Running OR Status = @Compensating)
              AND LastUpdatedAtUtc < @ThresholdUtc
            ORDER BY LastUpdatedAtUtc";

        var sagas = await _connection.QueryAsync<SagaState>(
            sql,
            new
            {
                BatchSize = batchSize,
                Running = "Running",
                Compensating = "Compensating",
                ThresholdUtc = thresholdUtc
            });

        return sagas.Cast<ISagaState>();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ISagaState>> GetExpiredSagasAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        if (batchSize <= 0)
            throw new ArgumentException(StoreValidationMessages.BatchSizeMustBeGreaterThanZero, nameof(batchSize));

        var sql = $@"
            SELECT TOP (@BatchSize) *
            FROM {_tableName}
            WHERE (Status = @Running OR Status = @Compensating)
              AND TimeoutAtUtc IS NOT NULL
              AND TimeoutAtUtc <= GETUTCDATE()
            ORDER BY TimeoutAtUtc";

        var sagas = await _connection.QueryAsync<SagaState>(
            sql,
            new
            {
                BatchSize = batchSize,
                Running = "Running",
                Compensating = "Compensating"
            });

        return sagas.Cast<ISagaState>();
    }

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Dapper executes SQL immediately, no need for SaveChanges
        return Task.CompletedTask;
    }
}
