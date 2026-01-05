using Encina.Messaging.Sagas;

namespace Encina.Messaging.ContractTests.Sagas;

/// <summary>
/// Contract tests that verify all ISagaStore implementations follow the same behavioral contract.
/// These tests ensure consistency across all saga store implementations (EF Core, Dapper, ADO.NET, etc.).
/// Implements IAsyncLifetime to automatically clean up after each test.
/// InitializeAsync and DisposeAsync are non-virtual interface implementations, preventing derived classes from overriding cleanup.
/// </summary>
public abstract class ISagaStoreContractTests : IAsyncLifetime
{
    protected static readonly DateTime FixedUtcNow = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    protected abstract ISagaStore CreateStore();

    /// <summary>
    /// Creates a saga state for testing. Implementations should use the provided timestamps
    /// to ensure contract tests remain implementation-agnostic.
    /// </summary>
    protected abstract ISagaState CreateSagaState(
        Guid sagaId,
        string sagaType,
        string status,
        DateTime? lastUpdatedAtUtc = null,
        DateTime? timeoutAtUtc = null,
        DateTime? completedAtUtc = null);

    /// <summary>
    /// The single override point for derived classes to implement cleanup logic.
    /// Called automatically after each test by DisposeAsync.
    /// </summary>
    protected abstract Task CleanupAsync();

    /// <summary>
    /// Called before each test. Non-virtual to prevent derived classes from bypassing base behavior.
    /// Override <see cref="InitializeTestAsync"/> if initialization is needed.
    /// </summary>
    public Task InitializeAsync() => InitializeTestAsync();

    /// <summary>
    /// Override in derived classes if per-test initialization is needed.
    /// </summary>
    protected virtual Task InitializeTestAsync() => Task.CompletedTask;

    /// <summary>
    /// Called after each test. Non-virtual to ensure cleanup always runs.
    /// Derived classes implement cleanup via <see cref="CleanupAsync"/>.
    /// </summary>
    public async Task DisposeAsync() => await CleanupAsync();

    #region GetAsync Contract

    [Fact]
    public async Task GetAsync_ExistingSaga_ShouldReturnSaga()
    {
        // Arrange
        var store = CreateStore();
        var sagaId = Guid.NewGuid();
        var saga = CreateSagaState(sagaId, "OrderFulfillmentSaga", "Running");
        await store.AddAsync(saga);
        await store.SaveChangesAsync();

        // Act
        var retrieved = await store.GetAsync(sagaId);

        // Assert
        retrieved.ShouldNotBeNull();
        retrieved!.SagaId.ShouldBe(sagaId);
    }

    [Fact]
    public async Task GetAsync_NonExistentSaga_ShouldReturnNull()
    {
        // Arrange
        var store = CreateStore();
        var nonExistentId = Guid.NewGuid();

        // Act
        var retrieved = await store.GetAsync(nonExistentId);

        // Assert
        retrieved.ShouldBeNull();
    }

    #endregion

    #region AddAsync Contract

    [Fact]
    public async Task AddAsync_ValidSaga_ShouldPersistSaga()
    {
        // Arrange
        var store = CreateStore();
        var sagaId = Guid.NewGuid();
        var saga = CreateSagaState(sagaId, "PaymentProcessingSaga", "Running");

        // Act
        await store.AddAsync(saga);
        await store.SaveChangesAsync();

        // Assert
        var retrieved = await store.GetAsync(sagaId);
        retrieved.ShouldNotBeNull();
        retrieved!.SagaId.ShouldBe(sagaId);
    }

    [Fact]
    public async Task AddAsync_ShouldPreserveAllProperties()
    {
        // Arrange
        var store = CreateStore();
        var sagaId = Guid.NewGuid();
        var saga = CreateSagaState(sagaId, "CustomerOnboardingSaga", "Running");

        // Act
        await store.AddAsync(saga);
        await store.SaveChangesAsync();

        // Assert
        var retrieved = await store.GetAsync(sagaId);
        retrieved.ShouldNotBeNull();
        retrieved!.SagaType.ShouldBe(saga.SagaType);
        retrieved.Status.ShouldBe(saga.Status);
        retrieved.Data.ShouldBe(saga.Data);
        retrieved.CurrentStep.ShouldBe(saga.CurrentStep);
    }

    [Fact]
    public async Task AddAsync_DuplicateSagaId_ShouldNotDuplicate()
    {
        // Arrange
        var store = CreateStore();
        var sagaId = Guid.NewGuid();
        var saga1 = CreateSagaState(sagaId, "Saga1", "Running");
        var saga2 = CreateSagaState(sagaId, "Saga2", "Running");

        // Act
        await store.AddAsync(saga1);
        await store.SaveChangesAsync();

        try
        {
            await store.AddAsync(saga2);
            await store.SaveChangesAsync();
        }
        catch (InvalidOperationException)
        {
            // Expected: some implementations throw InvalidOperationException for duplicates
        }
        catch (ArgumentException)
        {
            // Expected: some implementations throw ArgumentException for duplicates
        }

        // Assert - original saga should be preserved
        var retrieved = await store.GetAsync(sagaId);
        retrieved.ShouldNotBeNull();
        retrieved!.SagaType.ShouldBe("Saga1");
    }

    #endregion

    #region UpdateAsync Contract

    [Fact]
    public async Task UpdateAsync_ExistingSaga_ShouldUpdateState()
    {
        // Arrange
        var store = CreateStore();
        var sagaId = Guid.NewGuid();
        var saga = CreateSagaState(sagaId, "TestSaga", "Running");
        await store.AddAsync(saga);
        await store.SaveChangesAsync();

        // Modify saga state
        saga.Status = "Completed";
        saga.CurrentStep = 3;
        saga.CompletedAtUtc = DateTime.UtcNow;

        // Act
        await store.UpdateAsync(saga);
        await store.SaveChangesAsync();

        // Assert
        var retrieved = await store.GetAsync(sagaId);
        retrieved.ShouldNotBeNull();
        retrieved!.Status.ShouldBe("Completed");
        retrieved.CurrentStep.ShouldBe(3);
        retrieved.CompletedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateLastUpdatedTimestamp()
    {
        // Arrange
        var store = CreateStore();
        var sagaId = Guid.NewGuid();
        var saga = CreateSagaState(sagaId, "TestSaga", "Running");
        await store.AddAsync(saga);
        await store.SaveChangesAsync();

        var originalLastUpdated = saga.LastUpdatedAtUtc;

        saga.CurrentStep = 2;
        saga.LastUpdatedAtUtc = originalLastUpdated.AddTicks(1); // Deterministic timestamp advancement

        // Act
        await store.UpdateAsync(saga);
        await store.SaveChangesAsync();

        // Assert
        var retrieved = await store.GetAsync(sagaId);
        retrieved.ShouldNotBeNull();
        retrieved!.LastUpdatedAtUtc.ShouldBeGreaterThan(originalLastUpdated);
    }

    [Fact]
    public async Task UpdateAsync_CompletedSaga_ShouldAcceptStatusChange()
    {
        // Arrange
        var store = CreateStore();
        var sagaId = Guid.NewGuid();
        var saga = CreateSagaState(sagaId, "TestSaga", "Completed");
        var originalCompletedAt = FixedUtcNow;
        saga.CompletedAtUtc = originalCompletedAt;
        await store.AddAsync(saga);
        await store.SaveChangesAsync();

        // Act - Store accepts all updates; business validation is in the domain layer
        saga.Status = "Running";
        await store.UpdateAsync(saga);
        await store.SaveChangesAsync();

        // Assert - Store should persist the update (no business logic validation at store level)
        var retrieved = await store.GetAsync(sagaId);
        retrieved.ShouldNotBeNull();
        retrieved!.Status.ShouldBe("Running", "Store should persist status changes without business validation");
    }

    #endregion

    #region GetStuckSagasAsync Contract

    [Fact]
    public async Task GetStuckSagasAsync_NoStuckSagas_ShouldReturnEmptyCollection()
    {
        // Arrange
        var store = CreateStore();

        // Act
        var stuck = await store.GetStuckSagasAsync(olderThan: TimeSpan.FromMinutes(30), batchSize: 10);

        // Assert
        stuck.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetStuckSagasAsync_ShouldReturnOldRunningSagas()
    {
        // Arrange
        var store = CreateStore();
        var stuckSagaId = Guid.NewGuid();
        var recentSagaId = Guid.NewGuid();

        var stuckSaga = CreateSagaState(
            stuckSagaId,
            "StuckSaga",
            SagaStatus.Running,
            lastUpdatedAtUtc: FixedUtcNow.AddHours(-1));

        var recentSaga = CreateSagaState(
            recentSagaId,
            "RecentSaga",
            SagaStatus.Running,
            lastUpdatedAtUtc: FixedUtcNow);

        await store.AddAsync(stuckSaga);
        await store.AddAsync(recentSaga);
        await store.SaveChangesAsync();

        // Act
        var stuck = await store.GetStuckSagasAsync(olderThan: TimeSpan.FromMinutes(30), batchSize: 10);

        // Assert
        stuck.ShouldContain(s => s.SagaId == stuckSagaId);
        stuck.ShouldNotContain(s => s.SagaId == recentSagaId);
    }

    [Fact]
    public async Task GetStuckSagasAsync_ShouldRespectBatchSize()
    {
        // Arrange
        var store = CreateStore();
        for (int i = 0; i < 10; i++)
        {
            var saga = CreateSagaState(
                Guid.NewGuid(),
                $"Saga{i}",
                SagaStatus.Running,
                lastUpdatedAtUtc: FixedUtcNow.AddHours(-1));
            await store.AddAsync(saga);
        }
        await store.SaveChangesAsync();

        // Act
        var stuck = await store.GetStuckSagasAsync(olderThan: TimeSpan.FromMinutes(30), batchSize: 5);

        // Assert
        stuck.Count().ShouldBeLessThanOrEqualTo(5);
    }

    [Fact]
    public async Task GetStuckSagasAsync_ShouldExcludeCompletedSagas()
    {
        // Arrange
        var store = CreateStore();
        var completedSagaId = Guid.NewGuid();
        var runningSagaId = Guid.NewGuid();

        var completedSaga = CreateSagaState(
            completedSagaId,
            "CompletedSaga",
            SagaStatus.Completed,
            lastUpdatedAtUtc: FixedUtcNow.AddHours(-1),
            completedAtUtc: FixedUtcNow.AddHours(-1));

        var runningSaga = CreateSagaState(
            runningSagaId,
            "RunningSaga",
            SagaStatus.Running,
            lastUpdatedAtUtc: FixedUtcNow.AddHours(-1));

        await store.AddAsync(completedSaga);
        await store.AddAsync(runningSaga);
        await store.SaveChangesAsync();

        // Act
        var stuck = await store.GetStuckSagasAsync(olderThan: TimeSpan.FromMinutes(30), batchSize: 10);

        // Assert
        stuck.ShouldNotContain(s => s.SagaId == completedSagaId);
        stuck.ShouldContain(s => s.SagaId == runningSagaId);
    }

    #endregion

    #region GetExpiredSagasAsync Contract

    [Fact]
    public async Task GetExpiredSagasAsync_NoExpiredSagas_ShouldReturnEmptyCollection()
    {
        // Arrange
        var store = CreateStore();

        // Act
        var expired = await store.GetExpiredSagasAsync(batchSize: 10);

        // Assert
        expired.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetExpiredSagasAsync_ShouldReturnTimedOutSagas()
    {
        // Arrange
        var store = CreateStore();
        var expiredSagaId = Guid.NewGuid();
        var validSagaId = Guid.NewGuid();

        var expiredSaga = CreateSagaState(
            expiredSagaId,
            "ExpiredSaga",
            SagaStatus.Running,
            timeoutAtUtc: FixedUtcNow.AddHours(-1)); // Already timed out

        var validSaga = CreateSagaState(
            validSagaId,
            "ValidSaga",
            SagaStatus.Running,
            timeoutAtUtc: FixedUtcNow.AddHours(1)); // Not yet timed out

        await store.AddAsync(expiredSaga);
        await store.AddAsync(validSaga);
        await store.SaveChangesAsync();

        // Act
        var expired = await store.GetExpiredSagasAsync(batchSize: 10);

        // Assert
        expired.ShouldContain(s => s.SagaId == expiredSagaId);
        expired.ShouldNotContain(s => s.SagaId == validSagaId);
    }

    [Fact]
    public async Task GetExpiredSagasAsync_ShouldRespectBatchSize()
    {
        // Arrange
        var store = CreateStore();
        for (int i = 0; i < 10; i++)
        {
            var saga = CreateSagaState(
                Guid.NewGuid(),
                $"Saga{i}",
                SagaStatus.Running,
                timeoutAtUtc: FixedUtcNow.AddHours(-1));
            await store.AddAsync(saga);
        }
        await store.SaveChangesAsync();

        // Act
        var expired = await store.GetExpiredSagasAsync(batchSize: 5);

        // Assert
        expired.Count().ShouldBeLessThanOrEqualTo(5);
    }

    #endregion

    #region SaveChangesAsync Contract

    [Fact]
    public async Task SaveChangesAsync_ShouldPersistChanges()
    {
        // Arrange
        var store = CreateStore();
        var sagaId = Guid.NewGuid();
        await store.AddAsync(CreateSagaState(sagaId, "TestSaga", "Running"));

        // Act
        await store.SaveChangesAsync();

        // Assert
        var retrieved = await store.GetAsync(sagaId);
        retrieved.ShouldNotBeNull();
    }

    #endregion
}

/// <summary>
/// In-memory implementation of ISagaState for contract testing.
/// </summary>
internal sealed class InMemorySagaStateForContract : ISagaState
{
    public Guid SagaId { get; set; }
    public string SagaType { get; set; } = "";
    public string Data { get; set; } = "";
    public string Status { get; set; } = "";
    public int CurrentStep { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime LastUpdatedAtUtc { get; set; }
    public DateTime? TimeoutAtUtc { get; set; }
}

/// <summary>
/// In-memory implementation of ISagaStore for contract testing.
/// </summary>
internal sealed class InMemorySagaStoreForContract : ISagaStore
{
    private readonly List<InMemorySagaStateForContract> _sagas = [];

    public Task<ISagaState?> GetAsync(Guid sagaId, CancellationToken cancellationToken = default)
    {
        var saga = _sagas.FirstOrDefault(s => s.SagaId == sagaId);
        return Task.FromResult<ISagaState?>(saga);
    }

    public Task AddAsync(ISagaState saga, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(saga);

        if (saga is not InMemorySagaStateForContract inMemorySaga)
        {
            throw new ArgumentException(
                $"Saga must be of type {nameof(InMemorySagaStateForContract)}, but was {saga.GetType().Name}.",
                nameof(saga));
        }

        if (_sagas.Any(s => s.SagaId == inMemorySaga.SagaId))
        {
            throw new InvalidOperationException($"Saga with id {inMemorySaga.SagaId} already exists.");
        }

        _sagas.Add(inMemorySaga);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(ISagaState saga, CancellationToken cancellationToken = default)
    {
        if (saga is InMemorySagaStateForContract inMemorySaga)
        {
            var existing = _sagas.FirstOrDefault(s => s.SagaId == inMemorySaga.SagaId);
            if (existing != null)
            {
                var index = _sagas.IndexOf(existing);
                _sagas[index] = inMemorySaga;
            }
        }
        return Task.CompletedTask;
    }

    public Task<IEnumerable<ISagaState>> GetStuckSagasAsync(
        TimeSpan olderThan,
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.Subtract(olderThan);
        var stuck = _sagas
            .Where(s => s.Status == SagaStatus.Running || s.Status == SagaStatus.Compensating)
            .Where(s => s.LastUpdatedAtUtc < cutoff)
            .Take(batchSize)
            .Cast<ISagaState>()
            .ToList();

        return Task.FromResult<IEnumerable<ISagaState>>(stuck);
    }

    public Task<IEnumerable<ISagaState>> GetExpiredSagasAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var expired = _sagas
            .Where(s => s.TimeoutAtUtc.HasValue && s.TimeoutAtUtc <= now)
            .Where(s => s.Status != SagaStatus.Completed && s.Status != SagaStatus.Failed)
            .Take(batchSize)
            .Cast<ISagaState>()
            .ToList();

        return Task.FromResult<IEnumerable<ISagaState>>(expired);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public void Clear() => _sagas.Clear();
}

/// <summary>
/// Concrete implementation of contract tests for in-memory saga store.
/// </summary>
public sealed class InMemorySagaStoreContractTests : ISagaStoreContractTests
{
    private InMemorySagaStoreForContract? _currentStore;

    protected override ISagaStore CreateStore()
    {
        _currentStore = new InMemorySagaStoreForContract();
        return _currentStore;
    }

    protected override ISagaState CreateSagaState(
        Guid sagaId,
        string sagaType,
        string status,
        DateTime? lastUpdatedAtUtc = null,
        DateTime? timeoutAtUtc = null,
        DateTime? completedAtUtc = null)
    {
        var now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        return new InMemorySagaStateForContract
        {
            SagaId = sagaId,
            SagaType = sagaType,
            Status = status,
            Data = $"{{\"sagaType\":\"{sagaType}\"}}",
            CurrentStep = 0,
            StartedAtUtc = now,
            LastUpdatedAtUtc = lastUpdatedAtUtc ?? now,
            TimeoutAtUtc = timeoutAtUtc,
            CompletedAtUtc = completedAtUtc
        };
    }

    protected override Task CleanupAsync()
    {
        _currentStore?.Clear();
        return Task.CompletedTask;
    }
}
