using Encina.Dapper.Sqlite.Outbox;
using Encina.TestInfrastructure.Fixtures;
using Encina.TestInfrastructure.PropertyTests;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.Extensions.Time.Testing;

namespace Encina.Dapper.Sqlite.Tests.Outbox;

/// <summary>
/// Property-based integration tests for <see cref="OutboxStoreDapper"/>.
/// These tests verify invariants hold across various inputs and scenarios.
/// Uses real SQLite database with FakeTimeProvider for deterministic time control.
/// </summary>
[Trait("Category", "Integration")]
[Trait("TestType", "Property")]
[Trait("Provider", "Dapper.Sqlite")]
[Collection("Database")]
public sealed class OutboxStoreDapperPropertyTests : DapperSqlitePropertyTestBase<OutboxStoreDapper>
{
    public OutboxStoreDapperPropertyTests(SqliteFixture fixture) : base(fixture) { }

    /// <inheritdoc />
    protected override OutboxStoreDapper CreateStore(TimeProvider timeProvider)
        => new(Fixture.CreateConnection(), "OutboxMessages", timeProvider);

    #region FsCheck + Bogus Property Tests

    /// <summary>
    /// Unit test: Validates that MessageDataGenerators produces valid outbox data.
    /// Pure unit test without DB calls for fast execution.
    /// </summary>
    [Theory]
    [Trait("Category", "Unit")]
    [InlineData(42)]
    [InlineData(123)]
    [InlineData(9999)]
    public void GenerateOutboxData_ShouldProduceValidData(int seed)
    {
        // Act
        var data = MessageDataGenerators.GenerateOutboxData(seed);

        // Assert - Validate generated data properties
        // FixedUtcReference in MessageDataGenerators is 2026-01-01 12:00:00 UTC
        // Dates are generated between FixedUtcReference.AddDays(-7) and FixedUtcReference
        var expectedMinDate = new DateTime(2025, 12, 25, 12, 0, 0, DateTimeKind.Utc);
        var expectedMaxDate = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        Assert.NotEqual(Guid.Empty, data.Id);
        Assert.False(string.IsNullOrWhiteSpace(data.NotificationType));
        Assert.NotNull(data.Content);
        Assert.True(data.RetryCount >= 0);
        Assert.True(data.CreatedAtUtc >= expectedMinDate && data.CreatedAtUtc <= expectedMaxDate,
            $"CreatedAtUtc {data.CreatedAtUtc} should be within generator range ({expectedMinDate} - {expectedMaxDate})");
    }

    /// <summary>
    /// Integration test: Generated messages should always persist with correct Id.
    /// Uses representative seeds for Bogus data generation.
    /// </summary>
    [Theory]
    [InlineData(42)]
    [InlineData(123)]
    [InlineData(9999)]
    public async Task AddAsync_BogusGeneratedMessage_ShouldPersistId(int seed)
    {
        // Arrange - Generate realistic message data using Bogus
        var data = MessageDataGenerators.GenerateOutboxData(seed);

        var message = new OutboxMessage
        {
            Id = data.Id,
            NotificationType = data.NotificationType,
            Content = data.Content,
            CreatedAtUtc = Now,
            RetryCount = data.RetryCount
        };

        // Act
        await Store.AddAsync(message);
        var pending = await Store.GetPendingMessagesAsync(100, 10);

        // Assert
        var retrieved = pending.FirstOrDefault(m => m.Id == data.Id);
        Assert.NotNull(retrieved);
        Assert.Equal(data.NotificationType, retrieved.NotificationType);
        Assert.Equal(data.Content, retrieved.Content);
    }

    /// <summary>
    /// Property: Same seed produces reproducible outbox message data (determinism test).
    /// </summary>
    [Property(MaxTest = 50)]
    public bool SameSeed_ProducesReproducibleOutboxData(PositiveInt seed)
    {
        var data1 = MessageDataGenerators.GenerateOutboxData(seed.Get);
        var data2 = MessageDataGenerators.GenerateOutboxData(seed.Get);

        return data1.Id == data2.Id &&
               data1.NotificationType == data2.NotificationType;
    }

    /// <summary>
    /// Property: Generated notification types should be valid Encina notification patterns.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool GeneratedNotificationType_ShouldBeValidPattern(PositiveInt seed)
    {
        var data = MessageDataGenerators.GenerateOutboxData(seed.Get);

        // NotificationType should not be null or empty
        if (string.IsNullOrWhiteSpace(data.NotificationType))
            return false;

        // NotificationType should follow naming conventions (end with Event or Notification)
        return data.NotificationType.EndsWith("Event", StringComparison.Ordinal) ||
               data.NotificationType.EndsWith("Notification", StringComparison.Ordinal);
    }

    #endregion

    #region Store Invariant Property Tests

    /// <summary>
    /// Property: Any message added to outbox can be retrieved via GetPendingMessagesAsync.
    /// Invariant: AddAsync followed by GetPendingMessagesAsync always includes the added message.
    /// </summary>
    [Theory]
    [InlineData("OrderCreatedEvent", "{\"orderId\":123}")]
    [InlineData("PaymentProcessedEvent", "{\"amount\":99.99}")]
    [InlineData("CustomerRegisteredEvent", "{\"email\":\"test@example.com\"}")]
    [InlineData("", "")]
    [InlineData("SpecialChars", "' \" \\ / \n \r \t")]
    public async Task AddedMessage_AlwaysRetrievableInPending(string notificationType, string content)
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var message = new OutboxMessage
        {
            Id = messageId,
            NotificationType = notificationType,
            Content = content,
            CreatedAtUtc = Now
        };

        // Act
        await Store.AddAsync(message);
        var pending = await Store.GetPendingMessagesAsync(100, 10);

        // Assert
        var retrieved = pending.FirstOrDefault(m => m.Id == messageId);
        Assert.NotNull(retrieved);
        Assert.Equal(message.NotificationType, retrieved.NotificationType);
        Assert.Equal(message.Content, retrieved.Content);
    }

    /// <summary>
    /// Property: Marking a message as processed removes it from pending.
    /// Invariant: MarkAsProcessedAsync always removes message from GetPendingMessagesAsync results.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task ProcessedMessage_NeverAppearsInPending(int messageCount)
    {
        // Arrange
        var messageIds = new List<Guid>();
        for (var i = 0; i < messageCount; i++)
        {
            var id = Guid.NewGuid();
            messageIds.Add(id);
            await Store.AddAsync(new OutboxMessage
            {
                Id = id,
                NotificationType = $"Event{i}",
                Content = "{}",
                CreatedAtUtc = Now
            });
        }

        // Act - Mark all as processed
        foreach (var id in messageIds)
        {
            await Store.MarkAsProcessedAsync(id);
        }

        var pending = await Store.GetPendingMessagesAsync(100, 10);

        // Assert - None should appear in pending
        foreach (var id in messageIds)
        {
            Assert.DoesNotContain(pending, m => m.Id == id);
        }
    }

    /// <summary>
    /// Property: Retry count increases monotonically with each MarkAsFailedAsync call.
    /// Invariant: RetryCount(n+1) = RetryCount(n) + 1
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public async Task RetryCount_IncreasesMonotonically(int failureCount)
    {
        // Arrange
        var messageId = Guid.NewGuid();
        await Store.AddAsync(new OutboxMessage
        {
            Id = messageId,
            NotificationType = "TestEvent",
            Content = "{}",
            CreatedAtUtc = Now,
            RetryCount = 0
        });

        // Act - Fail N times
        for (var i = 0; i < failureCount; i++)
        {
            await Store.MarkAsFailedAsync(messageId, $"Error {i}", null);
        }

        // Assert
        var pending = await Store.GetPendingMessagesAsync(100, 100);
        var retrieved = pending.FirstOrDefault(m => m.Id == messageId);

        Assert.NotNull(retrieved);
        Assert.Equal(failureCount, retrieved.RetryCount);
    }

    /// <summary>
    /// Property: Batch size parameter always limits results correctly.
    /// Invariant: GetPendingMessagesAsync(batchSize: N).Count() ≤ N
    /// </summary>
    [Theory]
    [InlineData(10, 5)]
    [InlineData(10, 10)]
    [InlineData(10, 20)]
    [InlineData(5, 3)]
    public async Task BatchSize_AlwaysLimitsResults(int messageCount, int batchSize)
    {
        // Arrange - Add N messages with sequential timestamps
        for (var i = 0; i < messageCount; i++)
        {
            await Store.AddAsync(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                NotificationType = $"Event{i}",
                Content = "{}",
                CreatedAtUtc = Now.AddSeconds(i)
            });
        }

        // Act
        var pending = await Store.GetPendingMessagesAsync(batchSize, 10);

        // Assert
        Assert.True(pending.Count() <= batchSize);
        Assert.True(pending.Count() <= messageCount);
    }

    /// <summary>
    /// Property: MaxRetries filter correctly excludes messages.
    /// Invariant: All messages in GetPendingMessagesAsync(maxRetries: N) have RetryCount < N
    /// </summary>
    [Theory]
    [InlineData(3)]
    [InlineData(5)]
    public async Task MaxRetries_AlwaysFiltersCorrectly(int maxRetries)
    {
        // Arrange - Add messages with various retry counts
        for (var i = 0; i <= maxRetries + 2; i++)
        {
            await Store.AddAsync(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                NotificationType = $"Event{i}",
                Content = "{}",
                CreatedAtUtc = Now,
                RetryCount = i
            });
        }

        // Act
        var pending = await Store.GetPendingMessagesAsync(100, maxRetries);

        // Assert - All retrieved messages have RetryCount < maxRetries
        Assert.All(pending, m => Assert.True(m.RetryCount < maxRetries));
    }

    /// <summary>
    /// Property: Messages are always returned in chronological order.
    /// Invariant: GetPendingMessagesAsync results are ordered by CreatedAtUtc ascending.
    /// </summary>
    [Theory]
    [InlineData(5)]
    [InlineData(10)]
    public async Task GetPending_AlwaysReturnsChronologicalOrder(int messageCount)
    {
        // Arrange - Add messages with sequential timestamps
        for (var i = 0; i < messageCount; i++)
        {
            await Store.AddAsync(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                NotificationType = $"Event{i}",
                Content = "{}",
                CreatedAtUtc = Now.AddSeconds(i)
            });
        }

        // Act
        var pending = (await Store.GetPendingMessagesAsync(100, 10)).ToList();

        // Assert - Verify ordering
        if (pending.Count > 1)
        {
            for (var i = 0; i < pending.Count - 1; i++)
            {
                Assert.True(pending[i].CreatedAtUtc <= pending[i + 1].CreatedAtUtc,
                    $"Message at index {i} has timestamp {pending[i].CreatedAtUtc}, " +
                    $"which is after message at index {i + 1} with timestamp {pending[i + 1].CreatedAtUtc}");
            }
        }
    }

    /// <summary>
    /// Property: NextRetryAtUtc filtering works correctly.
    /// Invariant: Messages with NextRetryAtUtc > NOW are excluded from pending.
    /// </summary>
    [Theory]
    [InlineData(10)]
    [InlineData(60)]
    [InlineData(300)]
    public async Task NextRetryAtUtc_CorrectlyFilters(int futureSeconds)
    {
        // Arrange
        var readyMessageId = Guid.NewGuid();
        var futureMessageId = Guid.NewGuid();

        // Message ready for retry (past)
        await Store.AddAsync(new OutboxMessage
        {
            Id = readyMessageId,
            NotificationType = "ReadyEvent",
            Content = "{}",
            CreatedAtUtc = Now,
            NextRetryAtUtc = Now.AddSeconds(-10)
        });

        // Message not ready (future)
        await Store.AddAsync(new OutboxMessage
        {
            Id = futureMessageId,
            NotificationType = "FutureEvent",
            Content = "{}",
            CreatedAtUtc = Now,
            NextRetryAtUtc = Now.AddSeconds(futureSeconds)
        });

        // Act
        var pending = await Store.GetPendingMessagesAsync(100, 10);

        // Assert
        Assert.Contains(pending, m => m.Id == readyMessageId);
        Assert.DoesNotContain(pending, m => m.Id == futureMessageId);
    }

    /// <summary>
    /// Property: SaveChangesAsync is idempotent (can be called multiple times safely).
    /// Invariant: Multiple SaveChangesAsync calls have same effect as one call.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public async Task SaveChanges_IsIdempotent(int callCount)
    {
        // Arrange - Add a message and capture initial state
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "TestNotification",
            Content = """{"test":"data"}""",
            CreatedAtUtc = Now,
            RetryCount = 0
        };
        await Store.AddAsync(message);

        var initialState = (await Store.GetPendingMessagesAsync(100, 10)).ToList();
        var initialIds = initialState.Select(m => m.Id).OrderBy(id => id).ToList();

        // Act - Call SaveChangesAsync N times, verify no exception
        for (var i = 0; i < callCount; i++)
        {
            var exception = await Record.ExceptionAsync(() => Store.SaveChangesAsync());
            Assert.Null(exception);
        }

        // Assert - State should remain unchanged (idempotent)
        var finalState = (await Store.GetPendingMessagesAsync(100, 10)).ToList();
        var finalIds = finalState.Select(m => m.Id).OrderBy(id => id).ToList();

        Assert.Equal(initialState.Count, finalState.Count);
        Assert.Equal(initialIds, finalIds);
    }

    #endregion
}
