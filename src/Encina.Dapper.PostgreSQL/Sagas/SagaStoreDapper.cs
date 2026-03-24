using System.Data;
using Dapper;
using Encina.Messaging;
using Encina.Messaging.Sagas;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.Dapper.PostgreSQL.Sagas;

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
    /// <param name="tableName">The saga state table name (default: sagastates).</param>
    /// <param name="timeProvider">Optional time provider for testability. Defaults to <see cref="TimeProvider.System"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="connection"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="tableName"/> is null or whitespace.</exception>
    public SagaStoreDapper(
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
    public async Task<Either<EncinaError, Option<ISagaState>>> GetAsync(Guid sagaId, CancellationToken cancellationToken = default)
    {
        if (sagaId == Guid.Empty)
            throw new ArgumentException(StoreValidationMessages.SagaIdCannotBeEmpty, nameof(sagaId));

        return await EitherHelpers.TryAsync(async () =>
        {
            var sql = $@"
                SELECT sagaid, sagatype, data, status, startedatutc, lastupdatedatutc, completedatutc, errormessage, currentstep, timeoutatutc
                FROM {_tableName}
                WHERE sagaid = @SagaId";

            var result = await _connection.QuerySingleOrDefaultAsync<SagaState>(sql, new { SagaId = sagaId });

            return result is not null
                ? Option<ISagaState>.Some(result)
                : Option<ISagaState>.None;
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
                (sagaid, sagatype, data, status, startedatutc, lastupdatedatutc, completedatutc, errormessage, currentstep, timeoutatutc)
                VALUES
                (@SagaId, @SagaType, @Data, @Status, @StartedAtUtc, @LastUpdatedAtUtc, @CompletedAtUtc, @ErrorMessage, @CurrentStep, @TimeoutAtUtc)";

            await _connection.ExecuteAsync(sql, sagaState);
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
                SET sagatype = @SagaType,
                    data = @Data,
                    status = @Status,
                    lastupdatedatutc = @NowUtc,
                    completedatutc = @CompletedAtUtc,
                    errormessage = @ErrorMessage,
                    currentstep = @CurrentStep,
                    timeoutatutc = @TimeoutAtUtc
                WHERE sagaid = @SagaId";

            await _connection.ExecuteAsync(
                sql,
                new
                {
                    sagaState.SagaId,
                    sagaState.SagaType,
                    sagaState.Data,
                    sagaState.Status,
                    NowUtc = nowUtc,
                    sagaState.CompletedAtUtc,
                    sagaState.ErrorMessage,
                    sagaState.CurrentStep,
                    sagaState.TimeoutAtUtc
                });
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
                SELECT sagaid, sagatype, data, status, startedatutc, lastupdatedatutc, completedatutc, errormessage, currentstep, timeoutatutc
                FROM {_tableName}
                WHERE (status = @Running OR status = @Compensating)
                  AND timeoutatutc IS NOT NULL
                  AND timeoutatutc <= @NowUtc
                ORDER BY timeoutatutc
                LIMIT @BatchSize";

            var sagas = await _connection.QueryAsync<SagaState>(
                sql,
                new
                {
                    BatchSize = batchSize,
                    Running = "Running",
                    Compensating = "Compensating",
                    NowUtc = nowUtc
                });

            return sagas.Cast<ISagaState>();
        }, "saga.get_expired_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<Either<EncinaError, Unit>> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Dapper executes SQL immediately, no need for SaveChanges
        return Task.FromResult<Either<EncinaError, Unit>>(Unit.Default);
    }
}
