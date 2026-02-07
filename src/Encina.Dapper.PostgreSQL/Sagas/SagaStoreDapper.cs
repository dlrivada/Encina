using System.Data;
using Dapper;
using Encina.Messaging;
using Encina.Messaging.Sagas;

namespace Encina.Dapper.PostgreSQL.Sagas;

/// <summary>
/// Dapper implementation of <see cref="ISagaStore"/> for saga orchestration.
/// Provides persistence and retrieval of saga state for distributed transactions.
/// </summary>
public sealed class SagaStoreDapper : ISagaStore
{
    private readonly IDbConnection _connection;
    private readonly string _tableName;

    /// <summary>
    /// Initializes a new instance of the <see cref="SagaStoreDapper"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="tableName">The saga state table name (default: sagastates).</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="connection"/> or <paramref name="tableName"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="tableName"/> is empty or whitespace.</exception>
    public SagaStoreDapper(IDbConnection connection, string tableName = "sagastates")
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
        _tableName = SqlIdentifierValidator.ValidateTableName(tableName);
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

        return await _connection.QuerySingleOrDefaultAsync<SagaState>(sql, new { SagaId = sagaId });
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

        await _connection.ExecuteAsync(sql, sagaState);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(ISagaState sagaState, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sagaState);

        var sql = $@"
            UPDATE {_tableName}
            SET sagatype = @SagaType,
                data = @Data,
                status = @Status,
                lastupdatedatutc = NOW() AT TIME ZONE 'UTC',
                completedatutc = @CompletedAtUtc,
                errormessage = @ErrorMessage,
                currentstep = @CurrentStep,
                timeoutatutc = @TimeoutAtUtc
            WHERE sagaid = @SagaId";

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

        var thresholdUtc = DateTime.UtcNow.Subtract(olderThan);

        var sql = $@"
            SELECT sagaid, sagatype, data, status, startedatutc, lastupdatedatutc, completedatutc, errormessage, currentstep, timeoutatutc
            FROM {_tableName}
            WHERE (status = @Running OR status = @Compensating)
              AND lastupdatedatutc < @ThresholdUtc
            ORDER BY lastupdatedatutc
            LIMIT @BatchSize";

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
            SELECT sagaid, sagatype, data, status, startedatutc, lastupdatedatutc, completedatutc, errormessage, currentstep, timeoutatutc
            FROM {_tableName}
            WHERE (status = @Running OR status = @Compensating)
              AND timeoutatutc IS NOT NULL
              AND timeoutatutc <= NOW() AT TIME ZONE 'UTC'
            ORDER BY timeoutatutc
            LIMIT @BatchSize";

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
