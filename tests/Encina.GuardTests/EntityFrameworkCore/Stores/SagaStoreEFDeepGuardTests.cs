using Encina.EntityFrameworkCore.Sagas;
using Encina.Messaging.Sagas;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shouldly;
using SagaStatus = Encina.EntityFrameworkCore.Sagas.SagaStatus;

namespace Encina.GuardTests.EntityFrameworkCore.Stores;

/// <summary>
/// Deep guard tests for <see cref="SagaStoreEF"/> that exercise validation paths,
/// error handling, type checking, and edge cases beyond simple null-check guards.
/// </summary>
[Trait("Category", "Guard")]
[Trait("Provider", "EntityFrameworkCore")]
public sealed class SagaStoreEFDeepGuardTests
{
    #region Type Validation (invalid_type error path)

    [Fact]
    public async Task AddAsync_NonEFSagaState_ReturnsInvalidTypeError()
    {
        // Arrange - exercises: null check (line 51), type check (line 53),
        // error creation (lines 55-56), return Left
        var store = CreateStore();
        var mockSaga = Substitute.For<ISagaState>();
        mockSaga.SagaId.Returns(Guid.NewGuid());

        // Act
        var result = await store.AddAsync(mockSaga);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error =>
            {
                error.Message.ShouldContain("SagaState");
                error.Message.ShouldContain("got");
            });
    }

    [Fact]
    public async Task UpdateAsync_NonEFSagaState_ReturnsInvalidTypeError()
    {
        // Arrange - exercises: null check (line 68), type check (line 70),
        // Task.FromResult error path (lines 72-74)
        var store = CreateStore();
        var mockSaga = Substitute.For<ISagaState>();

        // Act
        var result = await store.UpdateAsync(mockSaga);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error =>
            {
                error.Message.ShouldContain("SagaState");
            });
    }

    #endregion

    #region Operations on Non-Existent Sagas

    [Fact]
    public async Task GetAsync_EmptyGuid_ReturnsNoneOption()
    {
        // Arrange - exercises: TryAsync (line 37), query (lines 39-40),
        // None path (lines 42-44)
        var store = CreateStore();

        // Act
        var result = await store.GetAsync(Guid.Empty);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: option => option.IsNone.ShouldBeTrue(),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task GetAsync_NonExistentGuid_ReturnsNoneOption()
    {
        // Arrange - exercises full query + None return path
        var store = CreateStore();

        // Act
        var result = await store.GetAsync(Guid.NewGuid());

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: option => option.IsNone.ShouldBeTrue(),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    #endregion

    #region Full Lifecycle

    [Fact]
    public async Task AddThenGet_EFSagaState_ReturnsMatchingSaga()
    {
        // Arrange - exercises: constructor, AddAsync happy path (lines 51-62),
        // GetAsync Some path (lines 37-45)
        var store = CreateStoreWithDb(out var dbContext);
        var sagaId = Guid.NewGuid();
        var saga = new SagaState
        {
            SagaId = sagaId,
            SagaType = "OrderProcessingSaga",
            Data = "{\"orderId\":\"ABC-123\"}",
            Status = SagaStatus.Running,
            CurrentStep = 0,
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow
        };

        // Act - Add
        var addResult = await store.AddAsync(saga);
        await dbContext.SaveChangesAsync();
        addResult.IsRight.ShouldBeTrue();

        // Act - Get
        var getResult = await store.GetAsync(sagaId);

        // Assert
        getResult.IsRight.ShouldBeTrue();
        getResult.Match(
            Right: option =>
            {
                option.IsSome.ShouldBeTrue();
                option.IfSome(s =>
                {
                    s.SagaId.ShouldBe(sagaId);
                    s.Status.ShouldBe("Running");
                });
            },
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task UpdateAsync_EFSagaState_UpdatesLastUpdatedAtUtc()
    {
        // Arrange - exercises: UpdateAsync happy path (lines 68-80):
        // null check, type check, set LastUpdatedAtUtc, return Right
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2026, 6, 1, 14, 30, 0, TimeSpan.Zero));
        var store = CreateStoreWithDb(out var dbContext, fakeTime);
        var saga = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "OrderProcessingSaga",
            Data = "{}",
            Status = SagaStatus.Running,
            CurrentStep = 0,
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow
        };
        await dbContext.Set<SagaState>().AddAsync(saga);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await store.UpdateAsync(saga);

        // Assert
        result.IsRight.ShouldBeTrue();
        saga.LastUpdatedAtUtc.ShouldBe(new DateTime(2026, 6, 1, 14, 30, 0, DateTimeKind.Utc));
    }

    #endregion

    #region GetStuckSagasAsync Edge Cases

    [Fact]
    public async Task GetStuckSagasAsync_ZeroBatchSize_ReturnsEmpty()
    {
        // Arrange - exercises: TryAsync, time subtraction, query with Take(0), ToList
        var store = CreateStore();

        // Act
        var result = await store.GetStuckSagasAsync(TimeSpan.FromHours(1), batchSize: 0);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: sagas => sagas.ShouldBeEmpty(),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task GetStuckSagasAsync_NoStuckSagas_ReturnsEmpty()
    {
        // Arrange - exercises full query path with no matching sagas
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2026, 3, 15, 12, 0, 0, TimeSpan.Zero));
        var store = CreateStoreWithDb(out var dbContext, fakeTime);

        // Add a recently-updated Running saga that should NOT be considered stuck
        var recentSaga = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "RecentSaga",
            Data = "{}",
            Status = SagaStatus.Running,
            CurrentStep = 1,
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = new DateTime(2026, 3, 15, 11, 30, 0, DateTimeKind.Utc) // 30 min ago
        };
        await dbContext.Set<SagaState>().AddAsync(recentSaga);
        await dbContext.SaveChangesAsync();

        // Act - looking for sagas stuck for more than 1 hour
        var result = await store.GetStuckSagasAsync(TimeSpan.FromHours(1), batchSize: 10);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: sagas => sagas.ShouldBeEmpty(),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task GetStuckSagasAsync_WithStuckSagas_ReturnsOnlyRunningOrCompensating()
    {
        // Arrange - exercises the Where clause filtering by status + time threshold
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2026, 3, 15, 12, 0, 0, TimeSpan.Zero));
        var store = CreateStoreWithDb(out var dbContext, fakeTime);

        var stuckRunning = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "StuckSaga",
            Data = "{}",
            Status = SagaStatus.Running,
            CurrentStep = 1,
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = new DateTime(2026, 3, 15, 8, 0, 0, DateTimeKind.Utc) // 4 hours ago
        };
        var completedSaga = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "CompletedSaga",
            Data = "{}",
            Status = SagaStatus.Completed, // Should NOT be returned
            CurrentStep = 3,
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = new DateTime(2026, 3, 15, 8, 0, 0, DateTimeKind.Utc)
        };
        await dbContext.Set<SagaState>().AddRangeAsync(stuckRunning, completedSaga);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await store.GetStuckSagasAsync(TimeSpan.FromHours(1), batchSize: 10);

        // Assert - only the Running saga should be returned
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: sagas =>
            {
                var list = sagas.ToList();
                list.Count.ShouldBe(1);
                list[0].SagaId.ShouldBe(stuckRunning.SagaId);
            },
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    #endregion

    #region GetExpiredSagasAsync Edge Cases

    [Fact]
    public async Task GetExpiredSagasAsync_ZeroBatchSize_ReturnsEmpty()
    {
        // Arrange - exercises: TryAsync, time provider, query with Take(0)
        var store = CreateStore();

        // Act
        var result = await store.GetExpiredSagasAsync(batchSize: 0);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: sagas => sagas.ShouldBeEmpty(),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task GetExpiredSagasAsync_SagasWithNoTimeout_ReturnsEmpty()
    {
        // Arrange - exercises the TimeoutAtUtc != null filter
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero));
        var store = CreateStoreWithDb(out var dbContext, fakeTime);

        var sagaNoTimeout = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "NoTimeoutSaga",
            Data = "{}",
            Status = SagaStatus.Running,
            CurrentStep = 1,
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow,
            TimeoutAtUtc = null
        };
        await dbContext.Set<SagaState>().AddAsync(sagaNoTimeout);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await store.GetExpiredSagasAsync(batchSize: 10);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: sagas => sagas.ShouldBeEmpty(),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task GetExpiredSagasAsync_WithExpiredTimeout_ReturnsSaga()
    {
        // Arrange - exercises full expired saga query path
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero));
        var store = CreateStoreWithDb(out var dbContext, fakeTime);

        var expiredSaga = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "ExpiredSaga",
            Data = "{}",
            Status = SagaStatus.Running,
            CurrentStep = 1,
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow,
            TimeoutAtUtc = new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc) // 2 hours ago
        };
        await dbContext.Set<SagaState>().AddAsync(expiredSaga);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await store.GetExpiredSagasAsync(batchSize: 10);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: sagas =>
            {
                var list = sagas.ToList();
                list.Count.ShouldBe(1);
                list[0].SagaId.ShouldBe(expiredSaga.SagaId);
            },
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    #endregion

    #region Cancellation

    [Fact]
    public async Task GetAsync_CancelledToken_ThrowsOperationCancelledException()
    {
        // Arrange
        var store = CreateStore();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(async () =>
            await store.GetAsync(Guid.NewGuid(), cts.Token));
    }

    [Fact]
    public async Task GetStuckSagasAsync_CancelledToken_ThrowsOperationCancelledException()
    {
        // Arrange
        var store = CreateStore();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(async () =>
            await store.GetStuckSagasAsync(TimeSpan.FromHours(1), 10, cts.Token));
    }

    #endregion

    #region SaveChangesAsync

    [Fact]
    public async Task SaveChangesAsync_EmptyContext_ReturnsRight()
    {
        // Arrange
        var store = CreateStore();

        // Act
        var result = await store.SaveChangesAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    #endregion

    #region Test Infrastructure

    private static SagaStoreEF CreateStore()
    {
        var options = new DbContextOptionsBuilder<TestDeepSagaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var dbContext = new TestDeepSagaDbContext(options);
        return new SagaStoreEF(dbContext);
    }

    private static SagaStoreEF CreateStoreWithDb(out TestDeepSagaDbContext dbContext, TimeProvider? timeProvider = null)
    {
        var options = new DbContextOptionsBuilder<TestDeepSagaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        dbContext = new TestDeepSagaDbContext(options);
        return new SagaStoreEF(dbContext, timeProvider);
    }

    internal sealed class TestDeepSagaDbContext : DbContext
    {
        public TestDeepSagaDbContext(DbContextOptions<TestDeepSagaDbContext> options) : base(options)
        {
        }

        public DbSet<SagaState> Sagas => Set<SagaState>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SagaState>(entity =>
            {
                entity.HasKey(e => e.SagaId);
            });
        }
    }

    #endregion
}
