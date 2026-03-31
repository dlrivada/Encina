using Encina.EntityFrameworkCore;
using Encina.EntityFrameworkCore.Sagas;
using Encina.Testing.Shouldly;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.EntityFrameworkCore.Sagas;

/// <summary>
/// Tests for <see cref="SagaStoreEF"/> exercising custom TimeProvider injection
/// and full lifecycle flows that cover timestamp-dependent code paths.
/// </summary>
[Trait("Category", "Unit")]
public class SagaStoreEFTimeProviderTests : IDisposable
{
    private readonly TestDbContext _dbContext;
    private readonly FakeTimeProvider _timeProvider;
    private readonly SagaStoreEF _store;

    public SagaStoreEFTimeProviderTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new TestDbContext(options);
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 3, 15, 12, 0, 0, TimeSpan.Zero));
        _store = new SagaStoreEF(_dbContext, _timeProvider);
    }

    [Fact]
    public async Task UpdateAsync_UsesInjectedTimeProvider()
    {
        // Arrange
        var baseTime = _timeProvider.GetUtcNow().UtcDateTime;
        var sagaId = Guid.NewGuid();
        var saga = new SagaState
        {
            SagaId = sagaId,
            SagaType = "OrderSaga",
            Data = "{\"orderId\":\"abc\"}",
            Status = SagaStatus.Running,
            StartedAtUtc = baseTime.AddMinutes(-30),
            LastUpdatedAtUtc = baseTime.AddMinutes(-30),
            CurrentStep = 0
        };

        await _dbContext.SagaStates.AddAsync(saga);
        await _dbContext.SaveChangesAsync();

        // Advance time before update
        _timeProvider.Advance(TimeSpan.FromMinutes(15));
        var expectedUpdateTime = _timeProvider.GetUtcNow().UtcDateTime;

        // Act
        saga.CurrentStep = 1;
        (await _store.UpdateAsync(saga)).ShouldBeRight();
        (await _store.SaveChangesAsync()).ShouldBeRight();

        // Assert
        var updated = await _dbContext.SagaStates.FindAsync(sagaId);
        updated!.LastUpdatedAtUtc.ShouldBe(expectedUpdateTime, TimeSpan.FromSeconds(1));
        updated.CurrentStep.ShouldBe(1);
    }

    [Fact]
    public async Task GetStuckSagasAsync_UsesInjectedTimeProvider()
    {
        // Arrange
        var baseTime = _timeProvider.GetUtcNow().UtcDateTime;

        // Saga last updated 2 hours ago
        var stuckSaga = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "StuckSaga",
            Data = "{}",
            Status = SagaStatus.Running,
            StartedAtUtc = baseTime.AddHours(-3),
            LastUpdatedAtUtc = baseTime.AddHours(-2),
            CurrentStep = 1
        };

        // Saga last updated 20 minutes ago
        var recentSaga = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "RecentSaga",
            Data = "{}",
            Status = SagaStatus.Running,
            StartedAtUtc = baseTime.AddMinutes(-25),
            LastUpdatedAtUtc = baseTime.AddMinutes(-20),
            CurrentStep = 1
        };

        await _dbContext.SagaStates.AddRangeAsync(stuckSaga, recentSaga);
        await _dbContext.SaveChangesAsync();

        // Act - threshold 1 hour: only the 2h-old saga should be returned
        var stuck = (await _store.GetStuckSagasAsync(
            olderThan: TimeSpan.FromHours(1),
            batchSize: 10)).ShouldBeRight();

        // Assert
        stuck.Count().ShouldBe(1);
        stuck.First().SagaId.ShouldBe(stuckSaga.SagaId);
    }

    [Fact]
    public async Task GetStuckSagasAsync_AfterTimeAdvance_FindsNewlyStuckSagas()
    {
        // Arrange
        var baseTime = _timeProvider.GetUtcNow().UtcDateTime;

        var saga = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "BecomesStuck",
            Data = "{}",
            Status = SagaStatus.Running,
            StartedAtUtc = baseTime.AddMinutes(-10),
            LastUpdatedAtUtc = baseTime.AddMinutes(-10),
            CurrentStep = 0
        };

        await _dbContext.SagaStates.AddAsync(saga);
        await _dbContext.SaveChangesAsync();

        // Act 1 - not stuck yet (only 10 min old, threshold is 1 hour)
        var before = (await _store.GetStuckSagasAsync(
            olderThan: TimeSpan.FromHours(1),
            batchSize: 10)).ShouldBeRight();
        before.ShouldBeEmpty();

        // Advance time by 2 hours
        _timeProvider.Advance(TimeSpan.FromHours(2));

        // Act 2 - now it's stuck
        var after = (await _store.GetStuckSagasAsync(
            olderThan: TimeSpan.FromHours(1),
            batchSize: 10)).ShouldBeRight();
        after.Count().ShouldBe(1);
    }

    [Fact]
    public async Task GetExpiredSagasAsync_UsesInjectedTimeProvider()
    {
        // Arrange
        var baseTime = _timeProvider.GetUtcNow().UtcDateTime;

        var expiredSaga = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "ExpiredSaga",
            Data = "{}",
            Status = SagaStatus.Running,
            StartedAtUtc = baseTime.AddHours(-3),
            LastUpdatedAtUtc = baseTime.AddHours(-2),
            TimeoutAtUtc = baseTime.AddHours(-1), // expired 1 hour ago
            CurrentStep = 1
        };

        var notYetExpired = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "NotExpiredYet",
            Data = "{}",
            Status = SagaStatus.Running,
            StartedAtUtc = baseTime.AddMinutes(-30),
            LastUpdatedAtUtc = baseTime.AddMinutes(-30),
            TimeoutAtUtc = baseTime.AddHours(1), // 1 hour from now
            CurrentStep = 1
        };

        await _dbContext.SagaStates.AddRangeAsync(expiredSaga, notYetExpired);
        await _dbContext.SaveChangesAsync();

        // Act
        var expired = (await _store.GetExpiredSagasAsync(batchSize: 10)).ShouldBeRight();

        // Assert
        expired.Count().ShouldBe(1);
        expired.First().SagaId.ShouldBe(expiredSaga.SagaId);
    }

    [Fact]
    public async Task GetExpiredSagasAsync_AfterTimeAdvance_FindsNewlyExpiredSagas()
    {
        // Arrange
        var baseTime = _timeProvider.GetUtcNow().UtcDateTime;

        var saga = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "SoonExpiringSaga",
            Data = "{}",
            Status = SagaStatus.Compensating,
            StartedAtUtc = baseTime.AddHours(-2),
            LastUpdatedAtUtc = baseTime.AddHours(-1),
            TimeoutAtUtc = baseTime.AddMinutes(30), // 30 min in future
            CurrentStep = 2
        };

        await _dbContext.SagaStates.AddAsync(saga);
        await _dbContext.SaveChangesAsync();

        // Act 1 - not expired yet
        var before = (await _store.GetExpiredSagasAsync(batchSize: 10)).ShouldBeRight();
        before.ShouldBeEmpty();

        // Advance time by 1 hour
        _timeProvider.Advance(TimeSpan.FromHours(1));

        // Act 2 - now expired
        var after = (await _store.GetExpiredSagasAsync(batchSize: 10)).ShouldBeRight();
        after.Count().ShouldBe(1);
        after.First().SagaId.ShouldBe(saga.SagaId);
    }

    [Fact]
    public async Task FullLifecycle_CreateUpdateAndComplete()
    {
        // Arrange
        var baseTime = _timeProvider.GetUtcNow().UtcDateTime;
        var sagaId = Guid.NewGuid();
        var saga = new SagaState
        {
            SagaId = sagaId,
            SagaType = "OrderFulfillmentSaga",
            Data = "{\"orderId\":\"ORD-001\",\"step\":\"inventory\"}",
            Status = SagaStatus.Running,
            StartedAtUtc = baseTime,
            LastUpdatedAtUtc = baseTime,
            CurrentStep = 0,
            CorrelationId = "corr-123",
            TimeoutAtUtc = baseTime.AddHours(2)
        };

        // Step 1: Add saga
        (await _store.AddAsync(saga)).ShouldBeRight();
        (await _store.SaveChangesAsync()).ShouldBeRight();

        // Step 2: Verify it exists
        var found = (await _store.GetAsync(sagaId)).ShouldBeRight();
        found.IsSome.ShouldBeTrue();

        // Step 3: Advance step
        _timeProvider.Advance(TimeSpan.FromMinutes(5));
        saga.CurrentStep = 1;
        saga.Data = "{\"orderId\":\"ORD-001\",\"step\":\"payment\"}";
        (await _store.UpdateAsync(saga)).ShouldBeRight();
        (await _store.SaveChangesAsync()).ShouldBeRight();

        var afterStep1 = await _dbContext.SagaStates.FindAsync(sagaId);
        afterStep1!.CurrentStep.ShouldBe(1);
        afterStep1.LastUpdatedAtUtc.ShouldBeGreaterThan(baseTime);

        // Step 4: Complete saga
        _timeProvider.Advance(TimeSpan.FromMinutes(10));
        saga.Status = SagaStatus.Completed;
        saga.CompletedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
        saga.CurrentStep = 2;
        (await _store.UpdateAsync(saga)).ShouldBeRight();
        (await _store.SaveChangesAsync()).ShouldBeRight();

        var completed = await _dbContext.SagaStates.FindAsync(sagaId);
        completed!.Status.ShouldBe(SagaStatus.Completed);
        completed.CompletedAtUtc.ShouldNotBeNull();

        // Completed saga should not appear as stuck or expired
        var stuck = (await _store.GetStuckSagasAsync(TimeSpan.FromMinutes(1), 10)).ShouldBeRight();
        stuck.ShouldBeEmpty();

        var expired = (await _store.GetExpiredSagasAsync(10)).ShouldBeRight();
        expired.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetStuckSagasAsync_ExcludesTimedOutSagas()
    {
        // Arrange
        var baseTime = _timeProvider.GetUtcNow().UtcDateTime;

        var timedOutSaga = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "TimedOutSaga",
            Data = "{}",
            Status = SagaStatus.TimedOut,
            StartedAtUtc = baseTime.AddHours(-4),
            LastUpdatedAtUtc = baseTime.AddHours(-3),
            CurrentStep = 1
        };

        await _dbContext.SagaStates.AddAsync(timedOutSaga);
        await _dbContext.SaveChangesAsync();

        // Act - TimedOut status should not be in stuck results (only Running/Compensating)
        var stuck = (await _store.GetStuckSagasAsync(
            olderThan: TimeSpan.FromHours(1),
            batchSize: 10)).ShouldBeRight();

        // Assert
        stuck.ShouldBeEmpty();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }
}
