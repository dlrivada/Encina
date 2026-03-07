using Encina.Messaging.Sagas;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Encina.MongoDB.Sagas;

/// <summary>
/// MongoDB implementation of <see cref="ISagaStore"/>.
/// </summary>
public sealed class SagaStoreMongoDB : ISagaStore
{
    private static readonly string[] ActiveSagaStatuses = ["Running", "Compensating"];

    private readonly IMongoCollection<SagaState> _collection;
    private readonly ILogger<SagaStoreMongoDB> _logger;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="SagaStoreMongoDB"/> class.
    /// </summary>
    /// <param name="mongoClient">The MongoDB client.</param>
    /// <param name="options">The MongoDB options.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="timeProvider">The time provider. Defaults to <see cref="TimeProvider.System"/> if not specified.</param>
    public SagaStoreMongoDB(
        IMongoClient mongoClient,
        IOptions<EncinaMongoDbOptions> options,
        ILogger<SagaStoreMongoDB> logger,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(mongoClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        var config = options.Value;
        var database = mongoClient.GetDatabase(config.DatabaseName);
        _collection = database.GetCollection<SagaState>(config.Collections.Sagas);
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Option<ISagaState>>> GetAsync(Guid sagaId, CancellationToken cancellationToken = default)
    {
        return await EitherHelpers.TryAsync(async () =>
        {
            var saga = await _collection
                .Find(s => s.SagaId == sagaId)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (saga is not null)
            {
                Log.RetrievedSaga(_logger, sagaId);
                return Option<ISagaState>.Some(saga);
            }

            Log.SagaNotFound(_logger, sagaId);
            return Option<ISagaState>.None;
        }, "saga.get_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> AddAsync(ISagaState sagaState, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sagaState);

        return await EitherHelpers.TryAsync(async () =>
        {
            var mongoSaga = sagaState as SagaState ?? new SagaState
            {
                SagaId = sagaState.SagaId,
                SagaType = sagaState.SagaType,
                Data = sagaState.Data,
                Status = sagaState.Status,
                CurrentStep = sagaState.CurrentStep,
                StartedAtUtc = sagaState.StartedAtUtc,
                CompletedAtUtc = sagaState.CompletedAtUtc,
                ErrorMessage = sagaState.ErrorMessage,
                LastUpdatedAtUtc = sagaState.LastUpdatedAtUtc
            };

            await _collection.InsertOneAsync(mongoSaga, cancellationToken: cancellationToken).ConfigureAwait(false);
            Log.CreatedSaga(_logger, sagaState.SagaId, sagaState.SagaType);
        }, "saga.add_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> UpdateAsync(ISagaState sagaState, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sagaState);

        return await EitherHelpers.TryAsync(async () =>
        {
            var filter = Builders<SagaState>.Filter.Eq(s => s.SagaId, sagaState.SagaId);
            var update = Builders<SagaState>.Update
                .Set(s => s.Data, sagaState.Data)
                .Set(s => s.Status, sagaState.Status)
                .Set(s => s.CurrentStep, sagaState.CurrentStep)
                .Set(s => s.CompletedAtUtc, sagaState.CompletedAtUtc)
                .Set(s => s.ErrorMessage, sagaState.ErrorMessage)
                .Set(s => s.LastUpdatedAtUtc, _timeProvider.GetUtcNow().UtcDateTime);

            var result = await _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken).ConfigureAwait(false);

            if (result.ModifiedCount == 0)
            {
                Log.SagaNotFoundForStateUpdate(_logger, sagaState.SagaId);
            }
            else
            {
                Log.UpdatedSagaState(_logger, sagaState.SagaId, sagaState.CurrentStep);
            }
        }, "saga.update_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, IEnumerable<ISagaState>>> GetStuckSagasAsync(
        TimeSpan olderThan,
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        return await EitherHelpers.TryAsync(async () =>
        {
            var threshold = _timeProvider.GetUtcNow().UtcDateTime.Subtract(olderThan);

            var filter = Builders<SagaState>.Filter.And(
                Builders<SagaState>.Filter.Eq(s => s.CompletedAtUtc, null),
                Builders<SagaState>.Filter.Lt(s => s.LastUpdatedAtUtc, threshold)
            );

            var sagas = await _collection
                .Find(filter)
                .Limit(batchSize)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            Log.RetrievedStuckSagas(_logger, sagas.Count);
            return (IEnumerable<ISagaState>)sagas;
        }, "saga.get_stuck_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, IEnumerable<ISagaState>>> GetExpiredSagasAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        return await EitherHelpers.TryAsync(async () =>
        {
            var now = _timeProvider.GetUtcNow().UtcDateTime;

            var filter = Builders<SagaState>.Filter.And(
                Builders<SagaState>.Filter.In(s => s.Status, ActiveSagaStatuses),
                Builders<SagaState>.Filter.Ne(s => s.TimeoutAtUtc, null),
                Builders<SagaState>.Filter.Lte(s => s.TimeoutAtUtc, now)
            );

            var sagas = await _collection
                .Find(filter)
                .SortBy(s => s.TimeoutAtUtc)
                .Limit(batchSize)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            Log.RetrievedExpiredSagas(_logger, sagas.Count);
            return (IEnumerable<ISagaState>)sagas;
        }, "saga.get_expired_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<Either<EncinaError, Unit>> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // MongoDB operations are immediately persisted, no SaveChanges needed
        return Task.FromResult<Either<EncinaError, Unit>>(Unit.Default);
    }
}
