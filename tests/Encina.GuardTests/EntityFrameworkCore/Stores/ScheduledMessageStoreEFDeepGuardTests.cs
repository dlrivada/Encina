using Encina.EntityFrameworkCore.Scheduling;
using Encina.Messaging.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shouldly;

namespace Encina.GuardTests.EntityFrameworkCore.Stores;

/// <summary>
/// Deep guard tests for <see cref="ScheduledMessageStoreEF"/> that exercise validation paths,
/// error handling, type checking, and edge cases beyond simple null-check guards.
/// </summary>
[Trait("Category", "Guard")]
[Trait("Provider", "EntityFrameworkCore")]
public sealed class ScheduledMessageStoreEFDeepGuardTests
{
    #region Type Validation (invalid_type error path)

    [Fact]
    public async Task AddAsync_NonEFScheduledMessage_ReturnsInvalidTypeError()
    {
        // Arrange - exercises: null check (line 37), type check (line 39),
        // error creation (lines 41-42), return Left
        var store = CreateStore();
        var mockMessage = Substitute.For<IScheduledMessage>();
        mockMessage.Id.Returns(Guid.NewGuid());
        mockMessage.RequestType.Returns("TestCommand");

        // Act
        var result = await store.AddAsync(mockMessage);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error =>
            {
                error.Message.ShouldContain("ScheduledMessage");
                error.Message.ShouldContain("got");
            });
    }

    #endregion

    #region Operations on Non-Existent Messages

    [Fact]
    public async Task MarkAsProcessedAsync_EmptyGuid_ReturnsRightNoSideEffects()
    {
        // Arrange - exercises: TryAsync (line 78), query (lines 80-81),
        // null check early return (lines 83-84)
        var store = CreateStore();

        // Act
        var result = await store.MarkAsProcessedAsync(Guid.Empty);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task MarkAsProcessedAsync_NonExistentGuid_ReturnsRight()
    {
        // Arrange
        var store = CreateStore();

        // Act
        var result = await store.MarkAsProcessedAsync(Guid.NewGuid());

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task MarkAsFailedAsync_NonExistentGuid_ReturnsRight()
    {
        // Arrange - exercises: null check on errorMessage (line 100),
        // TryAsync (line 102), query (lines 104-105), null check (lines 107-108)
        var store = CreateStore();

        // Act
        var result = await store.MarkAsFailedAsync(Guid.NewGuid(), "error message", null);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task RescheduleRecurringMessageAsync_NonExistentGuid_ReturnsRight()
    {
        // Arrange - exercises: TryAsync (line 122), query (lines 124-125),
        // null check early return (lines 127-128)
        var store = CreateStore();

        // Act
        var result = await store.RescheduleRecurringMessageAsync(
            Guid.NewGuid(),
            DateTime.UtcNow.AddHours(1));

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task CancelAsync_NonExistentGuid_ReturnsRight()
    {
        // Arrange - exercises: TryAsync (line 141), query (lines 143-144),
        // null check (line 146) - skips Remove
        var store = CreateStore();

        // Act
        var result = await store.CancelAsync(Guid.NewGuid());

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task CancelAsync_EmptyGuid_ReturnsRight()
    {
        // Arrange
        var store = CreateStore();

        // Act
        var result = await store.CancelAsync(Guid.Empty);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    #endregion

    #region Full Lifecycle

    [Fact]
    public async Task AddThenMarkAsProcessed_SetsTimestampsCorrectly()
    {
        // Arrange - exercises: AddAsync happy path, MarkAsProcessedAsync with existing message
        // (lines 78-90: query, find, set ProcessedAtUtc + LastExecutedAtUtc, clear ErrorMessage)
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2026, 5, 1, 9, 0, 0, TimeSpan.Zero));
        var store = CreateStoreWithDb(out var dbContext, fakeTime);
        var messageId = Guid.NewGuid();
        var message = new ScheduledMessage
        {
            Id = messageId,
            RequestType = "SendReminderCommand",
            Content = "{\"userId\":\"user-1\"}",
            ScheduledAtUtc = new DateTime(2026, 5, 1, 8, 0, 0, DateTimeKind.Utc),
            CreatedAtUtc = DateTime.UtcNow
        };

        // Act - Add
        var addResult = await store.AddAsync(message);
        await dbContext.SaveChangesAsync();
        addResult.IsRight.ShouldBeTrue();

        // Act - Mark as processed
        var processResult = await store.MarkAsProcessedAsync(messageId);

        // Assert - both ProcessedAtUtc and LastExecutedAtUtc should be set
        processResult.IsRight.ShouldBeTrue();
        var updated = await dbContext.Set<ScheduledMessage>().FirstAsync(m => m.Id == messageId);
        updated.ProcessedAtUtc.ShouldBe(new DateTime(2026, 5, 1, 9, 0, 0, DateTimeKind.Utc));
        updated.LastExecutedAtUtc.ShouldBe(new DateTime(2026, 5, 1, 9, 0, 0, DateTimeKind.Utc));
        updated.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public async Task AddThenMarkAsFailed_SetsErrorAndIncrementsRetry()
    {
        // Arrange - exercises full MarkAsFailedAsync path with existing message
        var store = CreateStoreWithDb(out var dbContext);
        var messageId = Guid.NewGuid();
        var message = new ScheduledMessage
        {
            Id = messageId,
            RequestType = "ProcessPaymentCommand",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 1
        };
        await dbContext.Set<ScheduledMessage>().AddAsync(message);
        await dbContext.SaveChangesAsync();

        var nextRetry = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act - exercises lines 102-113
        var result = await store.MarkAsFailedAsync(messageId, "Payment gateway timeout", nextRetry);

        // Assert
        result.IsRight.ShouldBeTrue();
        var updated = await dbContext.Set<ScheduledMessage>().FirstAsync(m => m.Id == messageId);
        updated.ErrorMessage.ShouldBe("Payment gateway timeout");
        updated.RetryCount.ShouldBe(2);
        updated.NextRetryAtUtc.ShouldBe(nextRetry);
    }

    [Fact]
    public async Task RescheduleRecurringMessage_ResetsAllFields()
    {
        // Arrange - exercises full RescheduleRecurringMessageAsync path (lines 122-135):
        // query, find, set ScheduledAtUtc, clear ProcessedAtUtc, clear ErrorMessage,
        // reset RetryCount to 0, clear NextRetryAtUtc
        var store = CreateStoreWithDb(out var dbContext);
        var messageId = Guid.NewGuid();
        var message = new ScheduledMessage
        {
            Id = messageId,
            RequestType = "DailyCleanupCommand",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddDays(-1),
            CreatedAtUtc = DateTime.UtcNow.AddDays(-2),
            ProcessedAtUtc = DateTime.UtcNow,
            ErrorMessage = "old error",
            RetryCount = 3,
            NextRetryAtUtc = DateTime.UtcNow.AddMinutes(30),
            IsRecurring = true,
            CronExpression = "0 0 * * *"
        };
        await dbContext.Set<ScheduledMessage>().AddAsync(message);
        await dbContext.SaveChangesAsync();

        var newScheduleTime = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var result = await store.RescheduleRecurringMessageAsync(messageId, newScheduleTime);

        // Assert - all fields should be reset
        result.IsRight.ShouldBeTrue();
        var updated = await dbContext.Set<ScheduledMessage>().FirstAsync(m => m.Id == messageId);
        updated.ScheduledAtUtc.ShouldBe(newScheduleTime);
        updated.ProcessedAtUtc.ShouldBeNull();
        updated.ErrorMessage.ShouldBeNull();
        updated.RetryCount.ShouldBe(0);
        updated.NextRetryAtUtc.ShouldBeNull();
    }

    [Fact]
    public async Task CancelAsync_ExistingMessage_RemovesFromStore()
    {
        // Arrange - exercises full CancelAsync path with existing message
        // (lines 141-150: query, find, Remove)
        var store = CreateStoreWithDb(out var dbContext);
        var messageId = Guid.NewGuid();
        var message = new ScheduledMessage
        {
            Id = messageId,
            RequestType = "TestCommand",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddHours(1),
            CreatedAtUtc = DateTime.UtcNow
        };
        await dbContext.Set<ScheduledMessage>().AddAsync(message);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await store.CancelAsync(messageId);
        await dbContext.SaveChangesAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        var remaining = await dbContext.Set<ScheduledMessage>().CountAsync();
        remaining.ShouldBe(0);
    }

    #endregion

    #region GetDueMessagesAsync Edge Cases

    [Fact]
    public async Task GetDueMessagesAsync_ZeroBatchSize_ReturnsEmpty()
    {
        // Arrange - exercises: TryAsync, time provider, query with Take(0)
        var store = CreateStore();

        // Act
        var result = await store.GetDueMessagesAsync(batchSize: 0, maxRetries: 5);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: messages => messages.ShouldBeEmpty(),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task GetDueMessagesAsync_ZeroMaxRetries_ReturnsEmpty()
    {
        // Arrange - exercises query with RetryCount < 0 always false
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero));
        var store = CreateStoreWithDb(out var dbContext, fakeTime);
        var message = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "TestCommand",
            Content = "{}",
            ScheduledAtUtc = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 0
        };
        await dbContext.Set<ScheduledMessage>().AddAsync(message);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await store.GetDueMessagesAsync(batchSize: 10, maxRetries: 0);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: messages => messages.ShouldBeEmpty(),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task GetDueMessagesAsync_ExcludesFutureScheduledMessages()
    {
        // Arrange - exercises ScheduledAtUtc <= now filter
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2026, 3, 15, 12, 0, 0, TimeSpan.Zero));
        var store = CreateStoreWithDb(out var dbContext, fakeTime);

        var futureMsg = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "FutureCommand",
            Content = "{}",
            ScheduledAtUtc = new DateTime(2026, 3, 16, 0, 0, 0, DateTimeKind.Utc), // tomorrow
            CreatedAtUtc = DateTime.UtcNow
        };
        var dueMsg = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "DueCommand",
            Content = "{}",
            ScheduledAtUtc = new DateTime(2026, 3, 15, 10, 0, 0, DateTimeKind.Utc), // 2 hours ago
            CreatedAtUtc = DateTime.UtcNow
        };
        await dbContext.Set<ScheduledMessage>().AddRangeAsync(futureMsg, dueMsg);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await store.GetDueMessagesAsync(batchSize: 10, maxRetries: 5);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: messages =>
            {
                var list = messages.ToList();
                list.Count.ShouldBe(1);
                list[0].Id.ShouldBe(dueMsg.Id);
            },
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task GetDueMessagesAsync_ExcludesProcessedMessages()
    {
        // Arrange - exercises ProcessedAtUtc == null filter
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero));
        var store = CreateStoreWithDb(out var dbContext, fakeTime);

        var processed = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "DoneCommand",
            Content = "{}",
            ScheduledAtUtc = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            CreatedAtUtc = DateTime.UtcNow,
            ProcessedAtUtc = DateTime.UtcNow
        };
        await dbContext.Set<ScheduledMessage>().AddAsync(processed);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await store.GetDueMessagesAsync(batchSize: 10, maxRetries: 5);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: messages => messages.ShouldBeEmpty(),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    #endregion

    #region Cancellation

    [Fact]
    public async Task GetDueMessagesAsync_CancelledToken_ThrowsOperationCancelledException()
    {
        // Arrange
        var store = CreateStore();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(async () =>
            await store.GetDueMessagesAsync(10, 5, cts.Token));
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

    private static ScheduledMessageStoreEF CreateStore()
    {
        var options = new DbContextOptionsBuilder<TestDeepScheduledDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var dbContext = new TestDeepScheduledDbContext(options);
        return new ScheduledMessageStoreEF(dbContext);
    }

    private static ScheduledMessageStoreEF CreateStoreWithDb(out TestDeepScheduledDbContext dbContext, TimeProvider? timeProvider = null)
    {
        var options = new DbContextOptionsBuilder<TestDeepScheduledDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        dbContext = new TestDeepScheduledDbContext(options);
        return new ScheduledMessageStoreEF(dbContext, timeProvider);
    }

    internal sealed class TestDeepScheduledDbContext : DbContext
    {
        public TestDeepScheduledDbContext(DbContextOptions<TestDeepScheduledDbContext> options) : base(options)
        {
        }

        public DbSet<ScheduledMessage> ScheduledMessages => Set<ScheduledMessage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ScheduledMessage>(entity =>
            {
                entity.HasKey(e => e.Id);
            });
        }
    }

    #endregion
}
