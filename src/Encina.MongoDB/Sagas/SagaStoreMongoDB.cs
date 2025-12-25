using Encina.Messaging.Sagas;
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

    /// <summary>
    /// Initializes a new instance of the <see cref="SagaStoreMongoDB"/> class.
    /// </summary>
    /// <param name="mongoClient">The MongoDB client.</param>
    /// <param name="options">The MongoDB options.</param>
    /// <param name="logger">The logger.</param>
    public SagaStoreMongoDB(
        IMongoClient mongoClient,
        IOptions<EncinaMongoDbOptions> options,
        ILogger<SagaStoreMongoDB> logger)
    {
        ArgumentNullException.ThrowIfNull(mongoClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        var config = options.Value;
        var database = mongoClient.GetDatabase(config.DatabaseName);
        _collection = database.GetCollection<SagaState>(config.Collections.Sagas);
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ISagaState?> GetAsync(Guid sagaId, CancellationToken cancellationToken = default)
    {
        var saga = await _collection
            .Find(s => s.SagaId == sagaId)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (saga is not null)
        {
            Log.RetrievedSaga(_logger, sagaId);
        }
        else
        {
            Log.SagaNotFound(_logger, sagaId);
        }

        return saga;
    }

    /// <inheritdoc />
    public async Task AddAsync(ISagaState saga, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(saga);

        var mongoSaga = saga as SagaState ?? new SagaState
        {
            SagaId = saga.SagaId,
            SagaType = saga.SagaType,
            Data = saga.Data,
            Status = saga.Status,
            CurrentStep = saga.CurrentStep,
            StartedAtUtc = saga.StartedAtUtc,
            CompletedAtUtc = saga.CompletedAtUtc,
            ErrorMessage = saga.ErrorMessage,
            LastUpdatedAtUtc = saga.LastUpdatedAtUtc
        };

        await _collection.InsertOneAsync(mongoSaga, cancellationToken: cancellationToken).ConfigureAwait(false);
        Log.CreatedSaga(_logger, saga.SagaId, saga.SagaType);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(ISagaState saga, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(saga);

        var filter = Builders<SagaState>.Filter.Eq(s => s.SagaId, saga.SagaId);
        var update = Builders<SagaState>.Update
            .Set(s => s.Data, saga.Data)
            .Set(s => s.Status, saga.Status)
            .Set(s => s.CurrentStep, saga.CurrentStep)
            .Set(s => s.CompletedAtUtc, saga.CompletedAtUtc)
            .Set(s => s.ErrorMessage, saga.ErrorMessage)
            .Set(s => s.LastUpdatedAtUtc, DateTime.UtcNow);

        var result = await _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (result.ModifiedCount == 0)
        {
            Log.SagaNotFoundForStateUpdate(_logger, saga.SagaId);
        }
        else
        {
            Log.UpdatedSagaState(_logger, saga.SagaId, saga.CurrentStep);
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ISagaState>> GetStuckSagasAsync(
        TimeSpan olderThan,
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        var threshold = DateTime.UtcNow.Subtract(olderThan);

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
        return sagas;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ISagaState>> GetExpiredSagasAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

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
        return sagas;
    }

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // MongoDB operations are immediately persisted, no SaveChanges needed
        return Task.CompletedTask;
    }
}
