using Encina.EntityFrameworkCore.Outbox;
using Encina.Messaging.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shouldly;

namespace Encina.GuardTests.EntityFrameworkCore.Stores;

/// <summary>
/// Deep guard tests for <see cref="OutboxStoreEF"/> that exercise validation paths,
/// error handling, type checking, and edge cases beyond simple null-check guards.
/// </summary>
[Trait("Category", "Guard")]
[Trait("Provider", "EntityFrameworkCore")]
public sealed class OutboxStoreEFDeepGuardTests
{
    #region Type Validation (invalid_type error path)

    [Fact]
    public async Task AddAsync_NonEFOutboxMessage_ReturnsInvalidTypeError()
    {
        // Arrange - exercises: null check (line 42), type check (line 44),
        // error creation (lines 46-47), return Left
        var store = CreateStore();
        var mockMessage = Substitute.For<IOutboxMessage>();
        mockMessage.Id.Returns(Guid.NewGuid());
        mockMessage.NotificationType.Returns("TestEvent");

        // Act
        var result = await store.AddAsync(mockMessage);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error =>
            {
                error.Message.ShouldContain("OutboxMessage");
                error.Message.ShouldContain("got");
            });
    }

    [Fact]
    public async Task AddAsync_NonEFOutboxMessage_ErrorContainsGotKeyword()
    {
        // Arrange - verifies the error message includes "got" indicating wrong type was passed
        var store = CreateStore();
        var mockMessage = Substitute.For<IOutboxMessage>();

        // Act
        var result = await store.AddAsync(mockMessage);

        // Assert - the error message includes "got" with the concrete type name
        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error =>
            {
                error.Message.ShouldContain("got");
                error.Message.ShouldContain("OutboxMessage");
            });
    }

    #endregion

    #region Operations on Non-Existent Messages

    [Fact]
    public async Task MarkAsProcessedAsync_EmptyGuid_ReturnsRightNoSideEffects()
    {
        // Arrange - exercises: TryAsync (line 82), query (lines 84-85),
        // null check early return (lines 87-88)
        var store = CreateStore();

        // Act
        var result = await store.MarkAsProcessedAsync(Guid.Empty);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task MarkAsProcessedAsync_NonExistentGuid_ReturnsRightNoSideEffects()
    {
        // Arrange - exercises full query path + null check
        var store = CreateStore();

        // Act
        var result = await store.MarkAsProcessedAsync(Guid.NewGuid());

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task MarkAsFailedAsync_EmptyGuid_ReturnsRightNoSideEffects()
    {
        // Arrange - exercises: null check on errorMessage (line 102),
        // TryAsync (line 104), query (lines 106-107), null check (lines 109-110)
        var store = CreateStore();

        // Act
        var result = await store.MarkAsFailedAsync(Guid.Empty, "error occurred", null);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task MarkAsFailedAsync_NonExistentGuid_WithNextRetry_ReturnsRight()
    {
        // Arrange - exercises full path with nextRetryAtUtc parameter
        var store = CreateStore();

        // Act
        var result = await store.MarkAsFailedAsync(
            Guid.NewGuid(),
            "Connection refused",
            DateTime.UtcNow.AddMinutes(10));

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    #endregion

    #region Full Lifecycle

    [Fact]
    public async Task AddThenMarkAsProcessed_FullPath_ExercisesMultipleCodePaths()
    {
        // Arrange - exercises constructor, AddAsync happy path, MarkAsProcessedAsync with existing message
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2026, 4, 1, 8, 0, 0, TimeSpan.Zero));
        var store = CreateStoreWithDb(out var dbContext, fakeTime);
        var messageId = Guid.NewGuid();
        var message = new OutboxMessage
        {
            Id = messageId,
            NotificationType = "OrderPlaced",
            Content = "{\"orderId\":\"123\"}",
            CreatedAtUtc = DateTime.UtcNow
        };

        // Act - Add (exercises lines 42-53)
        var addResult = await store.AddAsync(message);
        await dbContext.SaveChangesAsync();

        addResult.IsRight.ShouldBeTrue();

        // Act - Mark as processed (exercises lines 82-92: query, find, set timestamp, clear error)
        var processResult = await store.MarkAsProcessedAsync(messageId);

        // Assert
        processResult.IsRight.ShouldBeTrue();
        var updated = await dbContext.Set<OutboxMessage>().FirstAsync(m => m.Id == messageId);
        updated.ProcessedAtUtc.ShouldBe(new DateTime(2026, 4, 1, 8, 0, 0, DateTimeKind.Utc));
        updated.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public async Task AddThenMarkAsFailed_SetsErrorAndIncrementsRetry()
    {
        // Arrange - exercises the full MarkAsFailedAsync path with existing message
        var store = CreateStoreWithDb(out var dbContext);
        var messageId = Guid.NewGuid();
        var message = new OutboxMessage
        {
            Id = messageId,
            NotificationType = "OrderPlaced",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 1
        };
        await dbContext.Set<OutboxMessage>().AddAsync(message);
        await dbContext.SaveChangesAsync();

        var nextRetry = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act - exercises lines 104-115: query, find, set error, increment, set next retry
        var result = await store.MarkAsFailedAsync(messageId, "Timeout error", nextRetry);

        // Assert
        result.IsRight.ShouldBeTrue();
        var updated = await dbContext.Set<OutboxMessage>().FirstAsync(m => m.Id == messageId);
        updated.ErrorMessage.ShouldBe("Timeout error");
        updated.RetryCount.ShouldBe(2);
        updated.NextRetryAtUtc.ShouldBe(nextRetry);
    }

    #endregion

    #region GetPendingMessagesAsync Edge Cases

    [Fact]
    public async Task GetPendingMessagesAsync_ZeroBatchSize_ReturnsEmpty()
    {
        // Arrange - exercises: TryAsync, time provider, query with Take(0), ToList
        var store = CreateStore();

        // Act
        var result = await store.GetPendingMessagesAsync(batchSize: 0, maxRetries: 5);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: messages => messages.ShouldBeEmpty(),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task GetPendingMessagesAsync_ZeroMaxRetries_ReturnsEmpty()
    {
        // Arrange - exercises: query where RetryCount < 0 is always false
        var store = CreateStoreWithDb(out var dbContext);
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "TestEvent",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 0
        };
        await dbContext.Set<OutboxMessage>().AddAsync(message);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await store.GetPendingMessagesAsync(batchSize: 10, maxRetries: 0);

        // Assert - RetryCount (0) < maxRetries (0) is false, so nothing returned
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: messages => messages.ShouldBeEmpty(),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task GetPendingMessagesAsync_ExcludesProcessedMessages()
    {
        // Arrange - exercises the Where clause filtering ProcessedAtUtc == null
        var store = CreateStoreWithDb(out var dbContext);
        var processed = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "TestEvent",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow,
            ProcessedAtUtc = DateTime.UtcNow
        };
        var pending = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "TestEvent",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow,
            ProcessedAtUtc = null
        };
        await dbContext.Set<OutboxMessage>().AddRangeAsync(processed, pending);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await store.GetPendingMessagesAsync(batchSize: 10, maxRetries: 5);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: messages =>
            {
                var list = messages.ToList();
                list.Count.ShouldBe(1);
                list[0].Id.ShouldBe(pending.Id);
            },
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task GetPendingMessagesAsync_ExcludesOverRetriedMessages()
    {
        // Arrange - exercises RetryCount < maxRetries filter
        var store = CreateStoreWithDb(out var dbContext);
        var overRetried = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "TestEvent",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 5
        };
        await dbContext.Set<OutboxMessage>().AddAsync(overRetried);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await store.GetPendingMessagesAsync(batchSize: 10, maxRetries: 5);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: messages => messages.ShouldBeEmpty(),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task GetPendingMessagesAsync_RespectsNextRetryAtUtc()
    {
        // Arrange - exercises the NextRetryAtUtc <= now filter
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2026, 3, 15, 12, 0, 0, TimeSpan.Zero));
        var store = CreateStoreWithDb(out var dbContext, fakeTime);

        var futureRetry = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "TestEvent",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow,
            NextRetryAtUtc = new DateTime(2026, 3, 16, 0, 0, 0, DateTimeKind.Utc) // future
        };
        var readyRetry = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "TestEvent",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow,
            NextRetryAtUtc = new DateTime(2026, 3, 15, 10, 0, 0, DateTimeKind.Utc) // past
        };
        await dbContext.Set<OutboxMessage>().AddRangeAsync(futureRetry, readyRetry);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await store.GetPendingMessagesAsync(batchSize: 10, maxRetries: 5);

        // Assert - only the ready-for-retry message should be returned
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: messages =>
            {
                var list = messages.ToList();
                list.Count.ShouldBe(1);
                list[0].Id.ShouldBe(readyRetry.Id);
            },
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    #endregion

    #region Cancellation

    [Fact]
    public async Task GetPendingMessagesAsync_CancelledToken_ThrowsOperationCancelledException()
    {
        // Arrange
        var store = CreateStore();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(async () =>
            await store.GetPendingMessagesAsync(10, 5, cts.Token));
    }

    #endregion

    #region SaveChangesAsync

    [Fact]
    public async Task SaveChangesAsync_EmptyContext_ReturnsRight()
    {
        // Arrange - exercises TryAsync wrapper + SaveChangesAsync
        var store = CreateStore();

        // Act
        var result = await store.SaveChangesAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    #endregion

    #region Test Infrastructure

    private static OutboxStoreEF CreateStore()
    {
        var options = new DbContextOptionsBuilder<TestDeepOutboxDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var dbContext = new TestDeepOutboxDbContext(options);
        return new OutboxStoreEF(dbContext);
    }

    private static OutboxStoreEF CreateStoreWithDb(out TestDeepOutboxDbContext dbContext, TimeProvider? timeProvider = null)
    {
        var options = new DbContextOptionsBuilder<TestDeepOutboxDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        dbContext = new TestDeepOutboxDbContext(options);
        return new OutboxStoreEF(dbContext, timeProvider);
    }

    internal sealed class TestDeepOutboxDbContext : DbContext
    {
        public TestDeepOutboxDbContext(DbContextOptions<TestDeepOutboxDbContext> options) : base(options)
        {
        }

        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OutboxMessage>(entity =>
            {
                entity.HasKey(e => e.Id);
            });
        }
    }

    #endregion
}
