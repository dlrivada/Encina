using Encina.EntityFrameworkCore.Sagas;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Encina.EntityFrameworkCore.Tests.Sagas;

public class SagaStoreEFTests : IDisposable
{
    private readonly TestDbContext _dbContext;
    private readonly SagaStoreEF _store;

    public SagaStoreEFTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new TestDbContext(options);
        _store = new SagaStoreEF(_dbContext);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnExistingSaga()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var saga = new SagaState
        {
            SagaId = sagaId,
            SagaType = "TestSaga",
            Data = "{\"orderId\":\"123\"}",
            Status = SagaStatus.Running,
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow,
            CurrentStep = 0
        };

        await _dbContext.SagaStates.AddAsync(saga);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _store.GetAsync(sagaId);

        // Assert
        result.ShouldNotBeNull();
        result!.SagaId.ShouldBe(sagaId);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnNullForNonExistentSaga()
    {
        // Act
        var result = await _store.GetAsync(Guid.NewGuid());

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task AddAsync_ShouldAddSagaToStore()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var saga = new SagaState
        {
            SagaId = sagaId,
            SagaType = "TestSaga",
            Data = "{\"orderId\":\"456\"}",
            Status = SagaStatus.Running,
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow,
            CurrentStep = 0
        };

        // Act
        await _store.AddAsync(saga);
        await _store.SaveChangesAsync();

        // Assert
        var stored = await _dbContext.SagaStates.FindAsync(sagaId);
        stored.ShouldNotBeNull();
        stored!.Data.ShouldBe("{\"orderId\":\"456\"}");
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateLastUpdatedAtUtc()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var initialTimestamp = DateTime.UtcNow.AddMinutes(-10);
        var saga = new SagaState
        {
            SagaId = sagaId,
            SagaType = "TestSaga",
            Data = "{\"orderId\":\"789\"}",
            Status = SagaStatus.Running,
            StartedAtUtc = initialTimestamp,
            LastUpdatedAtUtc = initialTimestamp,
            CurrentStep = 1
        };

        await _dbContext.SagaStates.AddAsync(saga);
        await _dbContext.SaveChangesAsync();

        // Act
        saga.CurrentStep = 2;
        await _store.UpdateAsync(saga);
        await _store.SaveChangesAsync();

        // Assert
        var updated = await _dbContext.SagaStates.FindAsync(sagaId);
        updated!.LastUpdatedAtUtc.ShouldBeGreaterThan(initialTimestamp);
        updated.CurrentStep.ShouldBe(2);
    }

    [Fact]
    public async Task GetStuckSagasAsync_ShouldReturnRunningSagasOlderThanThreshold()
    {
        // Arrange
        var stuckSaga = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "StuckSaga",
            Data = "{}",
            Status = SagaStatus.Running,
            StartedAtUtc = DateTime.UtcNow.AddHours(-2),
            LastUpdatedAtUtc = DateTime.UtcNow.AddHours(-2),
            CurrentStep = 1
        };

        var recentSaga = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "RecentSaga",
            Data = "{}",
            Status = SagaStatus.Running,
            StartedAtUtc = DateTime.UtcNow.AddMinutes(-5),
            LastUpdatedAtUtc = DateTime.UtcNow.AddMinutes(-5),
            CurrentStep = 1
        };

        var completedSaga = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "CompletedSaga",
            Data = "{}",
            Status = SagaStatus.Completed,
            StartedAtUtc = DateTime.UtcNow.AddHours(-3),
            LastUpdatedAtUtc = DateTime.UtcNow.AddHours(-2),
            CompletedAtUtc = DateTime.UtcNow.AddHours(-2),
            CurrentStep = 3
        };

        await _dbContext.SagaStates.AddRangeAsync(stuckSaga, recentSaga, completedSaga);
        await _dbContext.SaveChangesAsync();

        // Act
        var stuck = await _store.GetStuckSagasAsync(
            olderThan: TimeSpan.FromHours(1),
            batchSize: 10);

        // Assert
        stuck.Count().ShouldBe(1);
        stuck.First().SagaId.ShouldBe(stuckSaga.SagaId);
    }

    [Fact]
    public async Task GetStuckSagasAsync_ShouldIncludeCompensatingSagas()
    {
        // Arrange
        var compensatingSaga = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "CompensatingSaga",
            Data = "{}",
            Status = SagaStatus.Compensating,
            StartedAtUtc = DateTime.UtcNow.AddHours(-2),
            LastUpdatedAtUtc = DateTime.UtcNow.AddHours(-2),
            CurrentStep = 2
        };

        await _dbContext.SagaStates.AddAsync(compensatingSaga);
        await _dbContext.SaveChangesAsync();

        // Act
        var stuck = await _store.GetStuckSagasAsync(
            olderThan: TimeSpan.FromHours(1),
            batchSize: 10);

        // Assert
        stuck.Count().ShouldBe(1);
        stuck.First().SagaId.ShouldBe(compensatingSaga.SagaId);
    }

    [Fact]
    public async Task GetStuckSagasAsync_ShouldRespectBatchSize()
    {
        // Arrange
        for (var i = 0; i < 10; i++)
        {
            await _dbContext.SagaStates.AddAsync(new SagaState
            {
                SagaId = Guid.NewGuid(),
                SagaType = $"StuckSaga{i}",
                Data = "{}",
                Status = SagaStatus.Running,
                StartedAtUtc = DateTime.UtcNow.AddHours(-2),
                LastUpdatedAtUtc = DateTime.UtcNow.AddHours(-2),
                CurrentStep = 0
            });
        }
        await _dbContext.SaveChangesAsync();

        // Act
        var stuck = await _store.GetStuckSagasAsync(
            olderThan: TimeSpan.FromHours(1),
            batchSize: 5);

        // Assert
        stuck.Count().ShouldBe(5);
    }

    [Fact]
    public async Task GetExpiredSagasAsync_ShouldReturnSagasWithExpiredTimeout()
    {
        // Arrange
        var expiredSaga = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "ExpiredSaga",
            Data = "{}",
            Status = SagaStatus.Running,
            StartedAtUtc = DateTime.UtcNow.AddHours(-2),
            LastUpdatedAtUtc = DateTime.UtcNow.AddHours(-2),
            TimeoutAtUtc = DateTime.UtcNow.AddHours(-1), // Expired 1 hour ago
            CurrentStep = 1
        };

        var activeWithTimeout = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "ActiveWithTimeout",
            Data = "{}",
            Status = SagaStatus.Running,
            StartedAtUtc = DateTime.UtcNow.AddMinutes(-30),
            LastUpdatedAtUtc = DateTime.UtcNow.AddMinutes(-30),
            TimeoutAtUtc = DateTime.UtcNow.AddHours(1), // Still valid
            CurrentStep = 1
        };

        var sagaWithoutTimeout = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "NoTimeout",
            Data = "{}",
            Status = SagaStatus.Running,
            StartedAtUtc = DateTime.UtcNow.AddHours(-3),
            LastUpdatedAtUtc = DateTime.UtcNow.AddHours(-3),
            TimeoutAtUtc = null, // No timeout configured
            CurrentStep = 1
        };

        await _dbContext.SagaStates.AddRangeAsync(expiredSaga, activeWithTimeout, sagaWithoutTimeout);
        await _dbContext.SaveChangesAsync();

        // Act
        var expired = await _store.GetExpiredSagasAsync(batchSize: 10);

        // Assert
        expired.Count().ShouldBe(1);
        expired.First().SagaId.ShouldBe(expiredSaga.SagaId);
    }

    [Fact]
    public async Task GetExpiredSagasAsync_ShouldNotReturnCompletedSagas()
    {
        // Arrange
        var expiredButCompleted = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "ExpiredCompleted",
            Data = "{}",
            Status = SagaStatus.Completed,
            StartedAtUtc = DateTime.UtcNow.AddHours(-2),
            LastUpdatedAtUtc = DateTime.UtcNow.AddHours(-1),
            CompletedAtUtc = DateTime.UtcNow.AddHours(-1),
            TimeoutAtUtc = DateTime.UtcNow.AddMinutes(-30), // Expired but already completed
            CurrentStep = 3
        };

        await _dbContext.SagaStates.AddAsync(expiredButCompleted);
        await _dbContext.SaveChangesAsync();

        // Act
        var expired = await _store.GetExpiredSagasAsync(batchSize: 10);

        // Assert
        expired.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetExpiredSagasAsync_ShouldIncludeCompensatingSagas()
    {
        // Arrange
        var compensatingExpired = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "CompensatingExpired",
            Data = "{}",
            Status = SagaStatus.Compensating,
            StartedAtUtc = DateTime.UtcNow.AddHours(-2),
            LastUpdatedAtUtc = DateTime.UtcNow.AddHours(-1),
            TimeoutAtUtc = DateTime.UtcNow.AddMinutes(-30), // Expired while compensating
            CurrentStep = 2
        };

        await _dbContext.SagaStates.AddAsync(compensatingExpired);
        await _dbContext.SaveChangesAsync();

        // Act
        var expired = await _store.GetExpiredSagasAsync(batchSize: 10);

        // Assert
        expired.Count().ShouldBe(1);
        expired.First().SagaId.ShouldBe(compensatingExpired.SagaId);
    }

    [Fact]
    public async Task GetExpiredSagasAsync_ShouldRespectBatchSize()
    {
        // Arrange
        for (var i = 0; i < 10; i++)
        {
            await _dbContext.SagaStates.AddAsync(new SagaState
            {
                SagaId = Guid.NewGuid(),
                SagaType = $"ExpiredSaga{i}",
                Data = "{}",
                Status = SagaStatus.Running,
                StartedAtUtc = DateTime.UtcNow.AddHours(-2),
                LastUpdatedAtUtc = DateTime.UtcNow.AddHours(-2),
                TimeoutAtUtc = DateTime.UtcNow.AddHours(-1).AddMinutes(-i), // All expired
                CurrentStep = 0
            });
        }
        await _dbContext.SaveChangesAsync();

        // Act
        var expired = await _store.GetExpiredSagasAsync(batchSize: 5);

        // Assert
        expired.Count().ShouldBe(5);
    }

    [Fact]
    public async Task GetExpiredSagasAsync_ShouldOrderByTimeoutAtUtc()
    {
        // Arrange
        var earlierExpired = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "EarlierExpired",
            Data = "{}",
            Status = SagaStatus.Running,
            StartedAtUtc = DateTime.UtcNow.AddHours(-3),
            LastUpdatedAtUtc = DateTime.UtcNow.AddHours(-3),
            TimeoutAtUtc = DateTime.UtcNow.AddHours(-2), // Expired 2 hours ago
            CurrentStep = 0
        };

        var laterExpired = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "LaterExpired",
            Data = "{}",
            Status = SagaStatus.Running,
            StartedAtUtc = DateTime.UtcNow.AddHours(-2),
            LastUpdatedAtUtc = DateTime.UtcNow.AddHours(-2),
            TimeoutAtUtc = DateTime.UtcNow.AddHours(-1), // Expired 1 hour ago
            CurrentStep = 0
        };

        // Add in reverse order to test ordering
        await _dbContext.SagaStates.AddRangeAsync(laterExpired, earlierExpired);
        await _dbContext.SaveChangesAsync();

        // Act
        var expired = (await _store.GetExpiredSagasAsync(batchSize: 10)).ToList();

        // Assert
        expired.Count.ShouldBe(2);
        expired[0].SagaId.ShouldBe(earlierExpired.SagaId);
        expired[1].SagaId.ShouldBe(laterExpired.SagaId);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }
}
