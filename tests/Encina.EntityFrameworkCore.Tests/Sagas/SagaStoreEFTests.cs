using Encina.EntityFrameworkCore.Sagas;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
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
        result.Should().NotBeNull();
        result!.SagaId.Should().Be(sagaId);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnNullForNonExistentSaga()
    {
        // Act
        var result = await _store.GetAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
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
        stored.Should().NotBeNull();
        stored!.Data.Should().Be("{\"orderId\":\"456\"}");
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateLastUpdatedAtUtc()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var saga = new SagaState
        {
            SagaId = sagaId,
            SagaType = "TestSaga",
            Data = "{\"orderId\":\"789\"}",
            Status = SagaStatus.Running,
            StartedAtUtc = DateTime.UtcNow.AddMinutes(-10),
            LastUpdatedAtUtc = DateTime.UtcNow.AddMinutes(-10),
            CurrentStep = 1
        };

        await _dbContext.SagaStates.AddAsync(saga);
        await _dbContext.SaveChangesAsync();

        var beforeUpdate = saga.LastUpdatedAtUtc;

        // Act
        await Task.Delay(100); // Ensure time difference
        saga.CurrentStep = 2;
        await _store.UpdateAsync(saga);
        await _store.SaveChangesAsync();

        // Assert
        var updated = await _dbContext.SagaStates.FindAsync(sagaId);
        updated!.LastUpdatedAtUtc.Should().BeAfter(beforeUpdate);
        updated.CurrentStep.Should().Be(2);
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
        stuck.Should().HaveCount(1);
        stuck.First().SagaId.Should().Be(stuckSaga.SagaId);
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
        stuck.Should().HaveCount(1);
        stuck.First().SagaId.Should().Be(compensatingSaga.SagaId);
    }

    [Fact]
    public async Task GetStuckSagasAsync_ShouldRespectBatchSize()
    {
        // Arrange
        for (int i = 0; i < 10; i++)
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
        stuck.Should().HaveCount(5);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }
}
