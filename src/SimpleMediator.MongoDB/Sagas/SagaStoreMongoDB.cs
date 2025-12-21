using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SimpleMediator.Messaging.Sagas;

namespace SimpleMediator.MongoDB.Sagas;

/// <summary>
/// MongoDB implementation of <see cref="ISagaStore"/>.
/// </summary>
public sealed class SagaStoreMongoDB : ISagaStore
{
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
        IOptions<SimpleMediatorMongoDbOptions> options,
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
            _logger.LogDebug("Found saga {SagaId} with status {Status}", sagaId, saga.Status);
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
        _logger.LogDebug("Added saga {SagaId} of type {SagaType}", saga.SagaId, saga.SagaType);
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
            _logger.LogWarning("Saga {SagaId} not found for update", saga.SagaId);
        }
        else
        {
            _logger.LogDebug("Updated saga {SagaId} to status {Status}", saga.SagaId, saga.Status);
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

        _logger.LogDebug("Retrieved {Count} stuck sagas older than {OlderThan}", sagas.Count, olderThan);
        return sagas;
    }

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // MongoDB operations are immediately persisted, no SaveChanges needed
        return Task.CompletedTask;
    }
}
