using System.Collections.Concurrent;
using Encina.Messaging.Sagas;
using Encina.Testing.Fakes.Models;

namespace Encina.Testing.Fakes.Stores;

/// <summary>
/// Thread-safe in-memory implementation of <see cref="ISagaStore"/> for testing.
/// </summary>
/// <remarks>
/// <para>
/// Provides full implementation of the saga store interface using an in-memory
/// concurrent dictionary. All operations are synchronous but return completed tasks
/// for interface compatibility.
/// </para>
/// </remarks>
public sealed class FakeSagaStore : ISagaStore
{
    private readonly ConcurrentDictionary<Guid, FakeSagaState> _sagas = new();
    private readonly ConcurrentBag<ISagaState> _addedSagas = new();
    private readonly ConcurrentBag<ISagaState> _updatedSagas = new();
    private readonly object _lock = new();

    /// <summary>
    /// Gets all sagas currently in the store.
    /// </summary>
    public IReadOnlyCollection<FakeSagaState> Sagas => _sagas.Values.ToList().AsReadOnly();

    /// <summary>
    /// Gets all sagas that have been added (for verification).
    /// </summary>
    public IReadOnlyList<ISagaState> AddedSagas => _addedSagas.ToList().AsReadOnly();

    /// <summary>
    /// Gets all sagas that have been updated (for verification).
    /// </summary>
    public IReadOnlyList<ISagaState> UpdatedSagas => _updatedSagas.ToList().AsReadOnly();

    /// <summary>
    /// Gets the number of times <see cref="SaveChangesAsync"/> was called.
    /// </summary>
    public int SaveChangesCallCount { get; private set; }

    /// <inheritdoc />
    public Task<ISagaState?> GetAsync(Guid sagaId, CancellationToken cancellationToken = default)
    {
        _sagas.TryGetValue(sagaId, out var saga);
        return Task.FromResult<ISagaState?>(saga);
    }

    /// <inheritdoc />
    public Task AddAsync(ISagaState saga, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(saga);

        var fakeSaga = saga as FakeSagaState ?? new FakeSagaState
        {
            SagaId = saga.SagaId,
            SagaType = saga.SagaType,
            Data = saga.Data,
            Status = saga.Status,
            CurrentStep = saga.CurrentStep,
            StartedAtUtc = saga.StartedAtUtc,
            CompletedAtUtc = saga.CompletedAtUtc,
            ErrorMessage = saga.ErrorMessage,
            LastUpdatedAtUtc = saga.LastUpdatedAtUtc,
            TimeoutAtUtc = saga.TimeoutAtUtc
        };

        _sagas[fakeSaga.SagaId] = fakeSaga;
        _addedSagas.Add(fakeSaga.Clone());

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UpdateAsync(ISagaState saga, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(saga);

        if (_sagas.TryGetValue(saga.SagaId, out var existing))
        {
            existing.SagaType = saga.SagaType;
            existing.Data = saga.Data;
            existing.Status = saga.Status;
            existing.CurrentStep = saga.CurrentStep;
            existing.CompletedAtUtc = saga.CompletedAtUtc;
            existing.ErrorMessage = saga.ErrorMessage;
            existing.LastUpdatedAtUtc = DateTime.UtcNow;
            existing.TimeoutAtUtc = saga.TimeoutAtUtc;

            _updatedSagas.Add(existing.Clone());
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IEnumerable<ISagaState>> GetStuckSagasAsync(
        TimeSpan olderThan,
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow - olderThan;

        var stuckSagas = _sagas.Values
            .Where(s => s.Status == "Running" && s.LastUpdatedAtUtc < cutoff)
            .Take(batchSize)
            .Cast<ISagaState>()
            .ToList();

        return Task.FromResult<IEnumerable<ISagaState>>(stuckSagas);
    }

    /// <inheritdoc />
    public Task<IEnumerable<ISagaState>> GetExpiredSagasAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var expiredSagas = _sagas.Values
            .Where(s => s.TimeoutAtUtc.HasValue &&
                        s.TimeoutAtUtc.Value <= now &&
                        s.Status == "Running")
            .Take(batchSize)
            .Cast<ISagaState>()
            .ToList();

        return Task.FromResult<IEnumerable<ISagaState>>(expiredSagas);
    }

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveChangesCallCount++;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets a saga by its ID.
    /// </summary>
    /// <param name="sagaId">The saga ID.</param>
    /// <returns>The saga if found, otherwise null.</returns>
    public FakeSagaState? GetSaga(Guid sagaId) =>
        _sagas.TryGetValue(sagaId, out var saga) ? saga : null;

    /// <summary>
    /// Clears all sagas and resets verification state.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _sagas.Clear();
            _addedSagas.Clear();
            _updatedSagas.Clear();
            SaveChangesCallCount = 0;
        }
    }

    /// <summary>
    /// Verifies that a saga with the specified type was started.
    /// </summary>
    /// <param name="sagaType">The saga type to look for.</param>
    /// <returns>True if a saga with the specified type was added.</returns>
    public bool WasSagaStarted(string sagaType)
    {
        lock (_lock)
        {
            return _addedSagas.Any(s => s.SagaType == sagaType);
        }
    }

    /// <summary>
    /// Verifies that a saga with the specified type was started.
    /// </summary>
    /// <typeparam name="TSaga">The saga type to look for.</typeparam>
    /// <returns>True if a saga with the specified type was added.</returns>
    public bool WasSagaStarted<TSaga>()
    {
        var typeName = typeof(TSaga).FullName ?? typeof(TSaga).Name;
        lock (_lock)
        {
            return _addedSagas.Any(s => string.Equals(s.SagaType, typeName, StringComparison.Ordinal));
        }
    }

    /// <summary>
    /// Gets all sagas with the specified status.
    /// </summary>
    /// <param name="status">The status to filter by.</param>
    /// <returns>Collection of sagas with the specified status.</returns>
    public IReadOnlyList<FakeSagaState> GetSagasByStatus(string status) =>
        _sagas.Values.Where(s => s.Status == status).ToList().AsReadOnly();
}
