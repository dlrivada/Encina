using Encina.EntityFrameworkCore.Inbox;
using Encina.Messaging.Inbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shouldly;

namespace Encina.GuardTests.EntityFrameworkCore.Stores;

/// <summary>
/// Deep guard tests for <see cref="InboxStoreEF"/> that exercise validation paths,
/// error handling, type checking, and edge cases beyond simple null-check guards.
/// </summary>
[Trait("Category", "Guard")]
[Trait("Provider", "EntityFrameworkCore")]
public sealed class InboxStoreEFDeepGuardTests
{
    #region Type Validation (invalid_type error path)

    [Fact]
    public async Task AddAsync_NonEFInboxMessage_ReturnsInvalidTypeError()
    {
        // Arrange - Uses a mock IInboxMessage that is NOT an EF InboxMessage
        // This exercises: type check (line 55), error creation (lines 57-58), return (line 58)
        var store = CreateStore();
        var mockMessage = Substitute.For<IInboxMessage>();
        mockMessage.MessageId.Returns("msg-1");
        mockMessage.RequestType.Returns("TestType");

        // Act
        var result = await store.AddAsync(mockMessage);

        // Assert - Should return Left with error, not throw
        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error =>
            {
                error.Message.ShouldContain("InboxMessage");
            });
    }

    #endregion

    #region Operations on Non-Existent Messages

    [Fact]
    public async Task MarkAsProcessedAsync_NonExistentMessageId_ReturnsRightWithNoSideEffects()
    {
        // Arrange - exercises: null check (line 70), TryAsync wrapper (line 73),
        // FirstOrDefaultAsync query (lines 75-76), null check early-return (lines 78-79)
        var store = CreateStore();

        // Act
        var result = await store.MarkAsProcessedAsync("non-existent-id", "some response");

        // Assert - method should succeed (Right) even if message not found
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task MarkAsFailedAsync_NonExistentMessageId_ReturnsRightWithNoSideEffects()
    {
        // Arrange - exercises: null checks (lines 94-95), TryAsync (line 97),
        // query (lines 99-100), null check (lines 102-103)
        var store = CreateStore();

        // Act
        var result = await store.MarkAsFailedAsync("non-existent-id", "some error", DateTime.UtcNow.AddMinutes(5));

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task IncrementRetryCountAsync_NonExistentMessageId_ReturnsRightWithNoSideEffects()
    {
        // Arrange - exercises: null check (line 114), TryAsync (line 116),
        // query (lines 118-119), null check (line 121)
        var store = CreateStore();

        // Act
        var result = await store.IncrementRetryCountAsync("non-existent-id");

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task GetMessageAsync_NonExistentMessageId_ReturnsNoneOption()
    {
        // Arrange - exercises: null check (line 37), TryAsync (line 39),
        // query (lines 41-42), None path (lines 44-46)
        var store = CreateStore();

        // Act
        var result = await store.GetMessageAsync("non-existent-id");

        // Assert - Should be Right containing None
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: option => option.IsNone.ShouldBeTrue(),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    #endregion

    #region Full Lifecycle (many source lines)

    [Fact]
    public async Task AddThenGetMessage_FullPath_ExercisesAddAndGetLogic()
    {
        // Arrange - Exercises constructor (lines 28-31), AddAsync full path (lines 53, 55, 61-64),
        // GetMessageAsync Some path (lines 37, 39-47)
        var store = CreateStoreWithDb(out var dbContext);
        var message = new InboxMessage
        {
            MessageId = "lifecycle-msg-1",
            RequestType = "TestCommand",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7)
        };

        // Act - Add
        var addResult = await store.AddAsync(message);
        await dbContext.SaveChangesAsync();

        // Assert - Add succeeded
        addResult.IsRight.ShouldBeTrue();

        // Act - Get
        var getResult = await store.GetMessageAsync("lifecycle-msg-1");

        // Assert - Get returns Some with matching data
        getResult.IsRight.ShouldBeTrue();
        getResult.Match(
            Right: option =>
            {
                option.IsSome.ShouldBeTrue();
                option.IfSome(m =>
                {
                    m.MessageId.ShouldBe("lifecycle-msg-1");
                    m.RequestType.ShouldBe("TestCommand");
                });
            },
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task MarkAsProcessed_ExistingMessage_SetsTimestampAndClearsError()
    {
        // Arrange - Exercises the full mark-as-processed path including:
        // null checks, query, finding message, setting Response, ProcessedAtUtc, clearing ErrorMessage
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2026, 3, 15, 10, 0, 0, TimeSpan.Zero));
        var store = CreateStoreWithDb(out var dbContext, fakeTime);
        var message = new InboxMessage
        {
            MessageId = "process-msg",
            RequestType = "TestCommand",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            ErrorMessage = "previous-error"
        };
        await dbContext.Set<InboxMessage>().AddAsync(message);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await store.MarkAsProcessedAsync("process-msg", "{\"result\":\"ok\"}");

        // Assert
        result.IsRight.ShouldBeTrue();
        var updated = await dbContext.Set<InboxMessage>().FirstAsync(m => m.MessageId == "process-msg");
        updated.Response.ShouldBe("{\"result\":\"ok\"}");
        updated.ProcessedAtUtc.ShouldBe(new DateTime(2026, 3, 15, 10, 0, 0, DateTimeKind.Utc));
        updated.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public async Task MarkAsFailed_ExistingMessage_SetsErrorAndIncrementsRetry()
    {
        // Arrange - Exercises: query, finding message, setting ErrorMessage,
        // incrementing RetryCount, setting NextRetryAtUtc
        var store = CreateStoreWithDb(out var dbContext);
        var message = new InboxMessage
        {
            MessageId = "fail-msg",
            RequestType = "TestCommand",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            RetryCount = 2
        };
        await dbContext.Set<InboxMessage>().AddAsync(message);
        await dbContext.SaveChangesAsync();

        var nextRetry = DateTime.UtcNow.AddMinutes(30);

        // Act
        var result = await store.MarkAsFailedAsync("fail-msg", "Connection timeout", nextRetry);

        // Assert
        result.IsRight.ShouldBeTrue();
        var updated = await dbContext.Set<InboxMessage>().FirstAsync(m => m.MessageId == "fail-msg");
        updated.ErrorMessage.ShouldBe("Connection timeout");
        updated.RetryCount.ShouldBe(3);
        updated.NextRetryAtUtc.ShouldBe(nextRetry);
    }

    [Fact]
    public async Task IncrementRetryCount_ExistingMessage_IncrementsCount()
    {
        // Arrange - Exercises query + finding message + incrementing RetryCount
        var store = CreateStoreWithDb(out var dbContext);
        var message = new InboxMessage
        {
            MessageId = "retry-msg",
            RequestType = "TestCommand",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            RetryCount = 0
        };
        await dbContext.Set<InboxMessage>().AddAsync(message);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await store.IncrementRetryCountAsync("retry-msg");

        // Assert
        result.IsRight.ShouldBeTrue();
        var updated = await dbContext.Set<InboxMessage>().FirstAsync(m => m.MessageId == "retry-msg");
        updated.RetryCount.ShouldBe(1);
    }

    #endregion

    #region Batch and Edge Cases

    [Fact]
    public async Task GetExpiredMessagesAsync_ZeroBatchSize_ReturnsEmptyCollection()
    {
        // Arrange - exercises: TryAsync, time provider, query with Take(0),
        // ToListAsync, cast to IEnumerable
        var store = CreateStore();

        // Act
        var result = await store.GetExpiredMessagesAsync(0);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: messages => messages.ShouldBeEmpty(),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task GetExpiredMessagesAsync_NoExpiredMessages_ReturnsEmpty()
    {
        // Arrange - exercises full query path with Where, OrderBy, Take, ToList
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var store = CreateStoreWithDb(out var dbContext, fakeTime);

        // Add message that expires in the future
        var message = new InboxMessage
        {
            MessageId = "future-msg",
            RequestType = "TestCommand",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };
        await dbContext.Set<InboxMessage>().AddAsync(message);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await store.GetExpiredMessagesAsync(10);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: messages => messages.ShouldBeEmpty(),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task GetExpiredMessagesAsync_WithExpiredMessages_ReturnsOrderedByExpiry()
    {
        // Arrange - exercises the full expired message query path with actual data
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero));
        var store = CreateStoreWithDb(out var dbContext, fakeTime);

        var msg1 = new InboxMessage
        {
            MessageId = "expired-1",
            RequestType = "Cmd",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc)
        };
        var msg2 = new InboxMessage
        {
            MessageId = "expired-2",
            RequestType = "Cmd",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc)
        };
        await dbContext.Set<InboxMessage>().AddRangeAsync(msg1, msg2);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await store.GetExpiredMessagesAsync(10);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: messages =>
            {
                var list = messages.ToList();
                list.Count.ShouldBe(2);
                list[0].MessageId.ShouldBe("expired-2"); // earlier expiry first
            },
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task RemoveExpiredMessagesAsync_EmptyCollection_ReturnsRight()
    {
        // Arrange - exercises: null check, TryAsync, Where with empty Contains, ToList, RemoveRange
        var store = CreateStore();

        // Act
        var result = await store.RemoveExpiredMessagesAsync([]);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task RemoveExpiredMessagesAsync_WithMatchingIds_RemovesMessages()
    {
        // Arrange - exercises full remove path: query, filter, remove range
        var store = CreateStoreWithDb(out var dbContext);
        var msg = new InboxMessage
        {
            MessageId = "to-remove",
            RequestType = "Cmd",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow
        };
        await dbContext.Set<InboxMessage>().AddAsync(msg);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await store.RemoveExpiredMessagesAsync(["to-remove"]);
        await dbContext.SaveChangesAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        var remaining = await dbContext.Set<InboxMessage>().CountAsync();
        remaining.ShouldBe(0);
    }

    #endregion

    #region Cancellation Token Handling

    [Fact]
    public async Task GetMessageAsync_CancelledToken_ThrowsOperationCancelledException()
    {
        // Arrange - exercises the query path up to the cancellation point
        var store = CreateStore();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(async () =>
            await store.GetMessageAsync("any-id", cts.Token));
    }

    #endregion

    #region TimeProvider Integration

    [Fact]
    public void Constructor_WithCustomTimeProvider_UsesProvidedProvider()
    {
        // Arrange - exercises constructor lines 28-31 (all 4 lines)
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero));
        var options = new DbContextOptionsBuilder<TestDeepInboxDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestDeepInboxDbContext(options);

        // Act & Assert - Should not throw, uses custom provider
        var store = new InboxStoreEF(dbContext, fakeTime);
        store.ShouldNotBeNull();
    }

    #endregion

    #region SaveChangesAsync

    [Fact]
    public async Task SaveChangesAsync_WithNoChanges_ReturnsRight()
    {
        // Arrange - exercises: TryAsync wrapper, SaveChangesAsync on empty context
        var store = CreateStore();

        // Act
        var result = await store.SaveChangesAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    #endregion

    #region Test Infrastructure

    private static InboxStoreEF CreateStore()
    {
        var options = new DbContextOptionsBuilder<TestDeepInboxDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var dbContext = new TestDeepInboxDbContext(options);
        return new InboxStoreEF(dbContext);
    }

    private static InboxStoreEF CreateStoreWithDb(out TestDeepInboxDbContext dbContext, TimeProvider? timeProvider = null)
    {
        var options = new DbContextOptionsBuilder<TestDeepInboxDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        dbContext = new TestDeepInboxDbContext(options);
        return new InboxStoreEF(dbContext, timeProvider);
    }

    internal sealed class TestDeepInboxDbContext : DbContext
    {
        public TestDeepInboxDbContext(DbContextOptions<TestDeepInboxDbContext> options) : base(options)
        {
        }

        public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<InboxMessage>(entity =>
            {
                entity.HasKey(e => e.MessageId);
            });
        }
    }

    #endregion
}
