using Encina.ADO.Sqlite.Sagas;
using Encina.TestInfrastructure.Fixtures;

namespace Encina.IntegrationTests.ADO.Sqlite.Sagas;

/// <summary>
/// Integration tests for <see cref="SagaStoreADO"/>.
/// Tests against real SQLite database with proper cleanup.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Provider", "ADO.Sqlite")]
public sealed class SagaStoreADOTests : IClassFixture<SqliteFixture>
{
    private readonly SqliteFixture _database;
    private readonly SagaStoreADO _store;

    public SagaStoreADOTests(SqliteFixture database)
    {
        _database = database;

        // Clear all data before each test to ensure clean state
        _database.ClearAllDataAsync().GetAwaiter().GetResult();

        _store = new SagaStoreADO(_database.CreateConnection());
    }

    [Fact]
    public async Task AddAsync_ValidSaga_ShouldPersist()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var saga = new SagaState
        {
            SagaId = sagaId,
            SagaType = "OrderSaga",
            Data = "{\"orderId\":123}",
            Status = "Running",
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow,
            CurrentStep = 1
        };

        // Act
        await _store.AddAsync(saga);

        // Assert
        var retrieved = await _store.GetAsync(sagaId);
        Assert.NotNull(retrieved);
        Assert.Equal(sagaId, retrieved.SagaId);
        Assert.Equal("OrderSaga", retrieved.SagaType);
        Assert.Equal("{\"orderId\":123}", retrieved.Data);
        Assert.Equal("Running", retrieved.Status);
        Assert.Equal(1, retrieved.CurrentStep);
    }

    [Fact]
    public async Task GetAsync_NonExistentSaga_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _store.GetAsync(nonExistentId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_ExistingSaga_ShouldUpdateFields()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var saga = new SagaState
        {
            SagaId = sagaId,
            SagaType = "OrderSaga",
            Data = "{\"orderId\":123}",
            Status = "Running",
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow,
            CurrentStep = 1
        };
        await _store.AddAsync(saga);

        // Act - Update saga
        saga.Status = "Completed";
        saga.CurrentStep = 5;
        saga.Data = "{\"orderId\":123,\"completed\":true}";
        saga.CompletedAtUtc = DateTime.UtcNow;
        await _store.UpdateAsync(saga);

        // Assert
        var retrieved = await _store.GetAsync(sagaId);
        Assert.NotNull(retrieved);
        Assert.Equal("Completed", retrieved.Status);
        Assert.Equal(5, retrieved.CurrentStep);
        Assert.Equal("{\"orderId\":123,\"completed\":true}", retrieved.Data);
        Assert.NotNull(retrieved.CompletedAtUtc);
    }

    [Fact]
    public async Task UpdateAsync_SetsErrorMessage_WhenFailed()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var saga = new SagaState
        {
            SagaId = sagaId,
            SagaType = "PaymentSaga",
            Data = "{}",
            Status = "Running",
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow,
            CurrentStep = 2
        };
        await _store.AddAsync(saga);

        // Act - Mark as failed
        saga.Status = "Failed";
        saga.ErrorMessage = "Payment gateway timeout";
        await _store.UpdateAsync(saga);

        // Assert
        var retrieved = await _store.GetAsync(sagaId);
        Assert.NotNull(retrieved);
        Assert.Equal("Failed", retrieved.Status);
        Assert.Equal("Payment gateway timeout", retrieved.ErrorMessage);
    }

    [Fact]
    public async Task GetStuckSagasAsync_ReturnsOldRunningSagas()
    {
        // Arrange - Create stuck saga (old LastUpdatedAtUtc)
        var stuckSagaId = Guid.NewGuid();
        var stuckSaga = new SagaState
        {
            SagaId = stuckSagaId,
            SagaType = "StuckSaga",
            Data = "{}",
            Status = "Running",
            StartedAtUtc = DateTime.UtcNow.AddHours(-2),
            LastUpdatedAtUtc = DateTime.UtcNow.AddHours(-2),
            CurrentStep = 1
        };
        await _store.AddAsync(stuckSaga);

        // Create recent saga (should not be stuck)
        var recentSagaId = Guid.NewGuid();
        var recentSaga = new SagaState
        {
            SagaId = recentSagaId,
            SagaType = "RecentSaga",
            Data = "{}",
            Status = "Running",
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow,
            CurrentStep = 1
        };
        await _store.AddAsync(recentSaga);

        // Act - Get sagas older than 1 hour
        var stuckSagas = await _store.GetStuckSagasAsync(TimeSpan.FromHours(1), 10);

        // Assert
        var stuckList = stuckSagas.ToList();
        Assert.Single(stuckList);
        Assert.Equal(stuckSagaId, stuckList[0].SagaId);
    }

    [Fact]
    public async Task GetStuckSagasAsync_ReturnsCompensatingSagas()
    {
        // Arrange - Create stuck compensating saga
        var compensatingId = Guid.NewGuid();
        var compensatingSaga = new SagaState
        {
            SagaId = compensatingId,
            SagaType = "CompensatingSaga",
            Data = "{}",
            Status = "Compensating",
            StartedAtUtc = DateTime.UtcNow.AddHours(-3),
            LastUpdatedAtUtc = DateTime.UtcNow.AddHours(-3),
            CurrentStep = 2
        };
        await _store.AddAsync(compensatingSaga);

        // Act
        var stuckSagas = await _store.GetStuckSagasAsync(TimeSpan.FromHours(1), 10);

        // Assert
        var stuckList = stuckSagas.ToList();
        Assert.Single(stuckList);
        Assert.Equal(compensatingId, stuckList[0].SagaId);
        Assert.Equal("Compensating", stuckList[0].Status);
    }

    [Fact]
    public async Task GetStuckSagasAsync_IgnoresCompletedSagas()
    {
        // Arrange - Create old completed saga
        var completedId = Guid.NewGuid();
        var completedSaga = new SagaState
        {
            SagaId = completedId,
            SagaType = "CompletedSaga",
            Data = "{}",
            Status = "Completed",
            StartedAtUtc = DateTime.UtcNow.AddHours(-5),
            LastUpdatedAtUtc = DateTime.UtcNow.AddHours(-5),
            CompletedAtUtc = DateTime.UtcNow.AddHours(-4),
            CurrentStep = 5
        };
        await _store.AddAsync(completedSaga);

        // Act
        var stuckSagas = await _store.GetStuckSagasAsync(TimeSpan.FromHours(1), 10);

        // Assert
        Assert.Empty(stuckSagas);
    }

    [Fact]
    public async Task GetStuckSagasAsync_RespectsBatchSize()
    {
        // Arrange - Create 5 stuck sagas
        for (var i = 0; i < 5; i++)
        {
            var saga = new SagaState
            {
                SagaId = Guid.NewGuid(),
                SagaType = $"StuckSaga{i}",
                Data = "{}",
                Status = "Running",
                StartedAtUtc = DateTime.UtcNow.AddHours(-2 - i),
                LastUpdatedAtUtc = DateTime.UtcNow.AddHours(-2 - i),
                CurrentStep = 1
            };
            await _store.AddAsync(saga);
        }

        // Act - Request only 3
        var stuckSagas = await _store.GetStuckSagasAsync(TimeSpan.FromHours(1), 3);

        // Assert
        Assert.Equal(3, stuckSagas.Count());
    }

    [Fact]
    public async Task GetStuckSagasAsync_ReturnsOldestFirst()
    {
        // Arrange - Create sagas with different LastUpdatedAtUtc
        var oldestId = Guid.NewGuid();
        var oldestSaga = new SagaState
        {
            SagaId = oldestId,
            SagaType = "OldestSaga",
            Data = "{}",
            Status = "Running",
            StartedAtUtc = DateTime.UtcNow.AddHours(-10),
            LastUpdatedAtUtc = DateTime.UtcNow.AddHours(-10),
            CurrentStep = 1
        };
        await _store.AddAsync(oldestSaga);

        var newerSaga = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "NewerSaga",
            Data = "{}",
            Status = "Running",
            StartedAtUtc = DateTime.UtcNow.AddHours(-5),
            LastUpdatedAtUtc = DateTime.UtcNow.AddHours(-5),
            CurrentStep = 1
        };
        await _store.AddAsync(newerSaga);

        // Act
        var stuckSagas = await _store.GetStuckSagasAsync(TimeSpan.FromHours(1), 10);

        // Assert
        var stuckList = stuckSagas.ToList();
        Assert.Equal(2, stuckList.Count);
        Assert.Equal(oldestId, stuckList[0].SagaId); // Oldest first
    }

    [Fact]
    public async Task GetExpiredSagasAsync_ReturnsTimedOutSagas()
    {
        // Arrange - Create saga that has timed out
        var expiredId = Guid.NewGuid();
        var expiredSaga = new SagaState
        {
            SagaId = expiredId,
            SagaType = "ExpiredSaga",
            Data = "{}",
            Status = "Running",
            StartedAtUtc = DateTime.UtcNow.AddHours(-2),
            LastUpdatedAtUtc = DateTime.UtcNow.AddMinutes(-30),
            TimeoutAtUtc = DateTime.UtcNow.AddHours(-1), // Expired 1 hour ago
            CurrentStep = 2
        };
        await _store.AddAsync(expiredSaga);

        // Create saga with future timeout (should not be returned)
        var activeSaga = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "ActiveSaga",
            Data = "{}",
            Status = "Running",
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow,
            TimeoutAtUtc = DateTime.UtcNow.AddHours(1), // Not expired
            CurrentStep = 1
        };
        await _store.AddAsync(activeSaga);

        // Act
        var expiredSagas = await _store.GetExpiredSagasAsync(10);

        // Assert
        var expiredList = expiredSagas.ToList();
        Assert.Single(expiredList);
        Assert.Equal(expiredId, expiredList[0].SagaId);
    }

    [Fact]
    public async Task GetExpiredSagasAsync_IgnoresSagasWithoutTimeout()
    {
        // Arrange - Create saga without timeout
        var noTimeoutSaga = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "NoTimeoutSaga",
            Data = "{}",
            Status = "Running",
            StartedAtUtc = DateTime.UtcNow.AddHours(-10),
            LastUpdatedAtUtc = DateTime.UtcNow.AddHours(-10),
            TimeoutAtUtc = null, // No timeout
            CurrentStep = 1
        };
        await _store.AddAsync(noTimeoutSaga);

        // Act
        var expiredSagas = await _store.GetExpiredSagasAsync(10);

        // Assert
        Assert.Empty(expiredSagas);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldComplete()
    {
        // Act
        await _store.SaveChangesAsync();

        // Assert - Operation completed without throwing
        Assert.True(true, "SaveChangesAsync completed successfully");
    }

    [Fact]
    public async Task AddAsync_MultipleSagas_AllPersist()
    {
        // Arrange
        var saga1 = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "Saga1",
            Data = "{}",
            Status = "Running",
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow,
            CurrentStep = 1
        };

        var saga2 = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "Saga2",
            Data = "{}",
            Status = "Completed",
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow,
            CompletedAtUtc = DateTime.UtcNow,
            CurrentStep = 3
        };

        // Act
        await _store.AddAsync(saga1);
        await _store.AddAsync(saga2);

        // Assert
        var retrieved1 = await _store.GetAsync(saga1.SagaId);
        var retrieved2 = await _store.GetAsync(saga2.SagaId);
        Assert.NotNull(retrieved1);
        Assert.NotNull(retrieved2);
        Assert.Equal("Running", retrieved1.Status);
        Assert.Equal("Completed", retrieved2.Status);
    }

    [Fact]
    public async Task UpdateAsync_PreservesStartedAtUtc()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var startedTime = DateTime.UtcNow.AddHours(-5);
        var saga = new SagaState
        {
            SagaId = sagaId,
            SagaType = "TestSaga",
            Data = "{}",
            Status = "Running",
            StartedAtUtc = startedTime,
            LastUpdatedAtUtc = startedTime,
            CurrentStep = 1
        };
        await _store.AddAsync(saga);

        // Act - Update saga
        saga.Status = "Completed";
        saga.CompletedAtUtc = DateTime.UtcNow;
        await _store.UpdateAsync(saga);

        // Assert
        var retrieved = await _store.GetAsync(sagaId);
        Assert.NotNull(retrieved);
        // StartedAtUtc should be close to original (DateTime storage may have precision differences)
        Assert.True(Math.Abs((retrieved.StartedAtUtc - startedTime).TotalSeconds) < 2);
    }

    [Fact]
    public async Task GetStuckSagasAsync_EmptyWhenNoStuckSagas()
    {
        // Arrange - Only recent sagas
        var recentSaga = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "RecentSaga",
            Data = "{}",
            Status = "Running",
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow,
            CurrentStep = 1
        };
        await _store.AddAsync(recentSaga);

        // Act
        var stuckSagas = await _store.GetStuckSagasAsync(TimeSpan.FromHours(1), 10);

        // Assert
        Assert.Empty(stuckSagas);
    }

    [Fact]
    public async Task AddAsync_WithAllFields_PersistsCorrectly()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var saga = new SagaState
        {
            SagaId = sagaId,
            SagaType = "ComprehensiveSaga",
            Data = "{\"key\":\"value\",\"number\":42}",
            Status = "Running",
            StartedAtUtc = now.AddMinutes(-30),
            LastUpdatedAtUtc = now,
            CompletedAtUtc = null,
            ErrorMessage = null,
            CurrentStep = 3,
            TimeoutAtUtc = now.AddHours(1)
        };

        // Act
        await _store.AddAsync(saga);

        // Assert
        var retrieved = await _store.GetAsync(sagaId);
        Assert.NotNull(retrieved);
        Assert.Equal("ComprehensiveSaga", retrieved.SagaType);
        Assert.Equal("{\"key\":\"value\",\"number\":42}", retrieved.Data);
        Assert.Equal("Running", retrieved.Status);
        Assert.Equal(3, retrieved.CurrentStep);
        Assert.Null(retrieved.CompletedAtUtc);
        Assert.Null(retrieved.ErrorMessage);
        Assert.NotNull(retrieved.TimeoutAtUtc);
    }

    [Fact]
    public async Task UpdateAsync_ClearsErrorMessageOnRecovery()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var saga = new SagaState
        {
            SagaId = sagaId,
            SagaType = "RecoverableSaga",
            Data = "{}",
            Status = "Failed",
            StartedAtUtc = DateTime.UtcNow.AddHours(-1),
            LastUpdatedAtUtc = DateTime.UtcNow.AddMinutes(-30),
            ErrorMessage = "Previous error",
            CurrentStep = 2
        };
        await _store.AddAsync(saga);

        // Act - Recover saga
        saga.Status = "Running";
        saga.ErrorMessage = null;
        saga.CurrentStep = 3;
        await _store.UpdateAsync(saga);

        // Assert
        var retrieved = await _store.GetAsync(sagaId);
        Assert.NotNull(retrieved);
        Assert.Equal("Running", retrieved.Status);
        Assert.Null(retrieved.ErrorMessage);
        Assert.Equal(3, retrieved.CurrentStep);
    }
}
