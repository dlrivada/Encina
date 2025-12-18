using System.Data;
using Dapper;
using SimpleMediator.Messaging.Sagas;

namespace SimpleMediator.Dapper.Oracle.Sagas;

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
    /// <param name="tableName">The saga state table name (default: SagaStates).</param>
    public SagaStoreDapper(IDbConnection connection, string tableName = "SagaStates")
    {
        _connection = connection;
        _tableName = tableName;
    }

    /// <inheritdoc />
    public async Task<ISagaState?> GetAsync(Guid sagaId, CancellationToken cancellationToken = default)
    {
        var sql = $@"
            SELECT *
            FROM {_tableName}
            WHERE SagaId = :SagaId";

        return await _connection.QuerySingleOrDefaultAsync<SagaState>(sql, new { SagaId = sagaId });
    }

    /// <inheritdoc />
    public async Task AddAsync(ISagaState sagaState, CancellationToken cancellationToken = default)
    {
        var sql = $@"
            INSERT INTO {_tableName}
            (SagaId, SagaType, Data, Status, StartedAtUtc, LastUpdatedAtUtc, CompletedAtUtc, ErrorMessage, CurrentStep)
            VALUES
            (:SagaId, :SagaType, :Data, :Status, :StartedAtUtc, :LastUpdatedAtUtc, :CompletedAtUtc, :ErrorMessage, :CurrentStep)";

        await _connection.ExecuteAsync(sql, sagaState);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(ISagaState sagaState, CancellationToken cancellationToken = default)
    {
        var sql = $@"
            UPDATE {_tableName}
            SET SagaType = :SagaType,
                Data = :Data,
                Status = :Status,
                LastUpdatedAtUtc = SYS_EXTRACT_UTC(SYSTIMESTAMP),
                CompletedAtUtc = :CompletedAtUtc,
                ErrorMessage = :ErrorMessage,
                CurrentStep = :CurrentStep
            WHERE SagaId = :SagaId";

        await _connection.ExecuteAsync(sql, sagaState);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ISagaState>> GetStuckSagasAsync(
        TimeSpan olderThan,
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        var thresholdUtc = DateTime.UtcNow.Subtract(olderThan);

        var sql = $@"
            SELECT *
            FROM {_tableName}
            WHERE (Status = :Running OR Status = :Compensating)
              AND LastUpdatedAtUtc < :ThresholdUtc
            ORDER BY LastUpdatedAtUtc
            FETCH FIRST :BatchSize ROWS ONLY";

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
    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Dapper executes SQL immediately, no need for SaveChanges
        return Task.CompletedTask;
    }
}
