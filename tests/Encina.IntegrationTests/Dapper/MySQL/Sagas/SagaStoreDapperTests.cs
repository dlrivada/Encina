using Encina.Dapper.MySQL.Sagas;
using Encina.Messaging.Sagas;
using Encina.TestInfrastructure.Extensions;
using Encina.TestInfrastructure.Fixtures;
using LanguageExt;
using Xunit;

namespace Encina.IntegrationTests.Dapper.MySQL.Sagas;

/// <summary>
/// Integration tests for <see cref="SagaStoreDapper"/>.
/// Tests against real MySQL database via Testcontainers with proper cleanup.
/// </summary>
[Collection("Dapper-MySQL")]
[Trait("Category", "Integration")]
public sealed class SagaStoreDapperTests : IAsyncLifetime
{
    private readonly MySqlFixture _database;
    private SagaStoreDapper _store = null!;

    public SagaStoreDapperTests(MySqlFixture database)
    {
        _database = database;
    }

    public async ValueTask InitializeAsync()
    {
        await _database.ClearAllDataAsync();
        _store = new SagaStoreDapper(_database.CreateConnection());
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

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
        (await _store.AddAsync(saga)).ShouldBeRight();

        // Assert
        var retrievedOpt = (await _store.GetAsync(sagaId)).ShouldBeRight();
        Assert.True(retrievedOpt.IsSome);
        var retrieved = retrievedOpt.Match(Some: s => s, None: () => throw new InvalidOperationException("Expected Some"));
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
        var resultOpt = (await _store.GetAsync(nonExistentId)).ShouldBeRight();

        // Assert
        Assert.True(resultOpt.IsNone);
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
        (await _store.AddAsync(saga)).ShouldBeRight();

        // Act - Update saga
        saga.Status = "Completed";
        saga.CurrentStep = 5;
        saga.Data = "{\"orderId\":123,\"completed\":true}";
        saga.CompletedAtUtc = DateTime.UtcNow;
        (await _store.UpdateAsync(saga)).ShouldBeRight();

        // Assert
        var retrievedOpt = (await _store.GetAsync(sagaId)).ShouldBeRight();
        Assert.True(retrievedOpt.IsSome);
        var retrieved = retrievedOpt.Match(Some: s => s, None: () => throw new InvalidOperationException("Expected Some"));
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
        (await _store.AddAsync(saga)).ShouldBeRight();

        // Act - Mark as failed
        saga.Status = "Failed";
        saga.ErrorMessage = "Payment gateway timeout";
        (await _store.UpdateAsync(saga)).ShouldBeRight();

        // Assert
        var retrievedOpt = (await _store.GetAsync(sagaId)).ShouldBeRight();
        Assert.True(retrievedOpt.IsSome);
        var retrieved = retrievedOpt.Match(Some: s => s, None: () => throw new InvalidOperationException("Expected Some"));
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
        (await _store.AddAsync(stuckSaga)).ShouldBeRight();

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
        (await _store.AddAsync(recentSaga)).ShouldBeRight();

        // Act - Get sagas older than 1 hour
        var stuckSagas = (await _store.GetStuckSagasAsync(TimeSpan.FromHours(1), 10)).ShouldBeRight();

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
        (await _store.AddAsync(compensatingSaga)).ShouldBeRight();

        // Act
        var stuckSagas = (await _store.GetStuckSagasAsync(TimeSpan.FromHours(1), 10)).ShouldBeRight();

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
        (await _store.AddAsync(completedSaga)).ShouldBeRight();

        // Act
        var stuckSagas = (await _store.GetStuckSagasAsync(TimeSpan.FromHours(1), 10)).ShouldBeRight();

        // Assert
        Assert.Empty(stuckSagas);
    }

    [Fact]
    public async Task GetStuckSagasAsync_RespectsBatchSize()
    {
        // Arrange - Create 5 stuck sagas
        for (int i = 0; i < 5; i++)
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
            (await _store.AddAsync(saga)).ShouldBeRight();
        }

        // Act - Request only 3
        var stuckSagas = (await _store.GetStuckSagasAsync(TimeSpan.FromHours(1), 3)).ShouldBeRight();

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
        (await _store.AddAsync(oldestSaga)).ShouldBeRight();

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
        (await _store.AddAsync(newerSaga)).ShouldBeRight();

        // Act
        var stuckSagas = (await _store.GetStuckSagasAsync(TimeSpan.FromHours(1), 10)).ShouldBeRight();

        // Assert
        var stuckList = stuckSagas.ToList();
        Assert.Equal(2, stuckList.Count);
        Assert.Equal(oldestId, stuckList[0].SagaId); // Oldest first
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldComplete()
    {
        // Act & Assert - Should not throw
        (await _store.SaveChangesAsync()).ShouldBeRight();
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
        (await _store.AddAsync(saga1)).ShouldBeRight();
        (await _store.AddAsync(saga2)).ShouldBeRight();

        // Assert
        var retrieved1Opt = (await _store.GetAsync(saga1.SagaId)).ShouldBeRight();
        var retrieved2Opt = (await _store.GetAsync(saga2.SagaId)).ShouldBeRight();
        Assert.True(retrieved1Opt.IsSome);
        var retrieved1 = retrieved1Opt.Match(Some: s => s, None: () => throw new InvalidOperationException("Expected Some"));
        Assert.True(retrieved2Opt.IsSome);
        var retrieved2 = retrieved2Opt.Match(Some: s => s, None: () => throw new InvalidOperationException("Expected Some"));
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
        (await _store.AddAsync(saga)).ShouldBeRight();

        // Act - Update saga
        saga.Status = "Completed";
        saga.CompletedAtUtc = DateTime.UtcNow;
        (await _store.UpdateAsync(saga)).ShouldBeRight();

        // Assert
        var retrievedOpt = (await _store.GetAsync(sagaId)).ShouldBeRight();
        Assert.True(retrievedOpt.IsSome);
        var retrieved = retrievedOpt.Match(Some: s => s, None: () => throw new InvalidOperationException("Expected Some"));
        // Compare with tolerance due to database datetime precision differences
        var timeDifference = Math.Abs((startedTime - retrieved.StartedAtUtc).TotalMilliseconds);
        Assert.True(timeDifference < 1, $"StartedAtUtc should be preserved. Expected: {startedTime:O}, Actual: {retrieved.StartedAtUtc:O}, Diff: {timeDifference}ms");
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
        (await _store.AddAsync(recentSaga)).ShouldBeRight();

        // Act
        var stuckSagas = (await _store.GetStuckSagasAsync(TimeSpan.FromHours(1), 10)).ShouldBeRight();

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
            CurrentStep = 3
        };

        // Act
        (await _store.AddAsync(saga)).ShouldBeRight();

        // Assert
        var retrievedOpt = (await _store.GetAsync(sagaId)).ShouldBeRight();
        Assert.True(retrievedOpt.IsSome);
        var retrieved = retrievedOpt.Match(Some: s => s, None: () => throw new InvalidOperationException("Expected Some"));
        Assert.Equal("ComprehensiveSaga", retrieved.SagaType);
        Assert.Equal("{\"key\":\"value\",\"number\":42}", retrieved.Data);
        Assert.Equal("Running", retrieved.Status);
        Assert.Equal(3, retrieved.CurrentStep);
        Assert.Null(retrieved.CompletedAtUtc);
        Assert.Null(retrieved.ErrorMessage);
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
        (await _store.AddAsync(saga)).ShouldBeRight();

        // Act - Recover saga
        saga.Status = "Running";
        saga.ErrorMessage = null;
        saga.CurrentStep = 3;
        (await _store.UpdateAsync(saga)).ShouldBeRight();

        // Assert
        var retrievedOpt = (await _store.GetAsync(sagaId)).ShouldBeRight();
        Assert.True(retrievedOpt.IsSome);
        var retrieved = retrievedOpt.Match(Some: s => s, None: () => throw new InvalidOperationException("Expected Some"));
        Assert.Equal("Running", retrieved.Status);
        Assert.Null(retrieved.ErrorMessage);
        Assert.Equal(3, retrieved.CurrentStep);
    }
}

