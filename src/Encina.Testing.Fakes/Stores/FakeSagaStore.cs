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
    /// Gets a snapshot of all sagas currently in the store.
    /// </summary>
    /// <returns>A point-in-time copy of all sagas.</returns>
    public IReadOnlyCollection<FakeSagaState> GetSagas() => _sagas.Values.ToList().AsReadOnly();

    /// <summary>
    /// Gets a snapshot of all sagas that have been added (for verification).
    /// </summary>
    /// <returns>A point-in-time copy of added sagas.</returns>
    public IReadOnlyList<ISagaState> GetAddedSagas() => _addedSagas.ToList().AsReadOnly();

    /// <summary>
    /// Gets a snapshot of all sagas that have been updated (for verification).
    /// </summary>
    /// <returns>A point-in-time copy of updated sagas.</returns>
    public IReadOnlyList<ISagaState> GetUpdatedSagas() => _updatedSagas.ToList().AsReadOnly();

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
    public Task AddAsync(ISagaState sagaState, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sagaState);

        var fakeSaga = sagaState as FakeSagaState ?? new FakeSagaState
        {
            SagaId = sagaState.SagaId,
            SagaType = sagaState.SagaType,
            Data = sagaState.Data,
            Status = sagaState.Status,
            CurrentStep = sagaState.CurrentStep,
            StartedAtUtc = sagaState.StartedAtUtc,
            CompletedAtUtc = sagaState.CompletedAtUtc,
            ErrorMessage = sagaState.ErrorMessage,
            LastUpdatedAtUtc = sagaState.LastUpdatedAtUtc,
            TimeoutAtUtc = sagaState.TimeoutAtUtc
        };

        _sagas[fakeSaga.SagaId] = fakeSaga;
        _addedSagas.Add(fakeSaga.Clone());

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UpdateAsync(ISagaState sagaState, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sagaState);

        if (_sagas.TryGetValue(sagaState.SagaId, out var existing))
        {
            existing.SagaType = sagaState.SagaType;
            existing.Data = sagaState.Data;
            existing.Status = sagaState.Status;
            existing.CurrentStep = sagaState.CurrentStep;
            existing.CompletedAtUtc = sagaState.CompletedAtUtc;
            existing.ErrorMessage = sagaState.ErrorMessage;
            existing.LastUpdatedAtUtc = DateTime.UtcNow;
            existing.TimeoutAtUtc = sagaState.TimeoutAtUtc;

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
