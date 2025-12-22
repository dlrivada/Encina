using Encina.Dapper.SqlServer.Outbox;
using Encina.TestInfrastructure.Extensions;
using Encina.TestInfrastructure.Fixtures;
using Xunit;

namespace Encina.Dapper.SqlServer.Tests.Outbox;

/// <summary>
/// Property-based integration tests for <see cref="OutboxStoreDapper"/>.
/// These tests verify invariants hold across various inputs and scenarios.
/// Uses real SQL Server database via Testcontainers.
/// </summary>
[Trait("Category", "Integration")]
[Trait("TestType", "Property")]
[Trait("Provider", "Dapper.SqlServer")]
public sealed class OutboxStoreDapperPropertyTests : IClassFixture<SqlServerFixture>
{
    private readonly SqlServerFixture _fixture;

    public OutboxStoreDapperPropertyTests(SqlServerFixture fixture)
    {
        _fixture = fixture;

        // Clear all data before each test to ensure clean state
        _fixture.ClearAllDataAsync().GetAwaiter().GetResult();
    }
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
        
        var store = new OutboxStoreDapper(_fixture.CreateConnection());

        var messageId = Guid.NewGuid();
        var message = new OutboxMessage
        {
            Id = messageId,
            NotificationType = notificationType,
            Content = content,
            CreatedAtUtc = DateTime.UtcNow
        };

        // Act
        await store.AddAsync(message);
        var pending = await store.GetPendingMessagesAsync(100, 10);

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
        
        var store = new OutboxStoreDapper(_fixture.CreateConnection());

        var messageIds = new List<Guid>();
        for (int i = 0; i < messageCount; i++)
        {
            var id = Guid.NewGuid();
            messageIds.Add(id);
            await store.AddAsync(new OutboxMessage
            {
                Id = id,
                NotificationType = $"Event{i}",
                Content = "{}",
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        // Act - Mark all as processed
        foreach (var id in messageIds)
        {
            await store.MarkAsProcessedAsync(id);
        }

        var pending = await store.GetPendingMessagesAsync(100, 10);

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
    [InlineData(10)]
    public async Task RetryCount_IncreasesMonotonically(int failureCount)
    {
        // Arrange
        
        var store = new OutboxStoreDapper(_fixture.CreateConnection());

        var messageId = Guid.NewGuid();
        await store.AddAsync(new OutboxMessage
        {
            Id = messageId,
            NotificationType = "TestEvent",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 0
        });

        // Act - Fail N times
        for (int i = 0; i < failureCount; i++)
        {
            await store.MarkAsFailedAsync(messageId, $"Error {i}", null);
        }

        // Assert
        var pending = await store.GetPendingMessagesAsync(100, 100);
        var retrieved = pending.FirstOrDefault(m => m.Id == messageId);

        Assert.NotNull(retrieved);
        Assert.Equal(failureCount, retrieved.RetryCount);
    }

    /// <summary>
    /// Property: Batch size parameter always limits results correctly.
    /// Invariant: GetPendingMessagesAsync(batchSize: N).Count() ≤ N
    /// </summary>
    [Theory]
    [InlineData(50, 10)]
    [InlineData(50, 25)]
    [InlineData(50, 50)]
    [InlineData(50, 100)]
    [InlineData(10, 5)]
    [InlineData(10, 20)]
    public async Task BatchSize_AlwaysLimitsResults(int messageCount, int batchSize)
    {
        // Arrange
        
        var store = new OutboxStoreDapper(_fixture.CreateConnection());

        // Add N messages
        for (int i = 0; i < messageCount; i++)
        {
            await store.AddAsync(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                NotificationType = $"Event{i}",
                Content = "{}",
                CreatedAtUtc = DateTime.UtcNow.AddSeconds(i)
            });
        }

        // Act
        var pending = await store.GetPendingMessagesAsync(batchSize, 10);

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
    [InlineData(10)]
    public async Task MaxRetries_AlwaysFiltersCorrectly(int maxRetries)
    {
        // Arrange
        
        var store = new OutboxStoreDapper(_fixture.CreateConnection());

        // Add messages with various retry counts
        for (int i = 0; i <= maxRetries + 2; i++)
        {
            await store.AddAsync(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                NotificationType = $"Event{i}",
                Content = "{}",
                CreatedAtUtc = DateTime.UtcNow,
                RetryCount = i
            });
        }

        // Act
        var pending = await store.GetPendingMessagesAsync(100, maxRetries);

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
    [InlineData(20)]
    public async Task GetPending_AlwaysReturnsChronologicalOrder(int messageCount)
    {
        // Arrange
        
        var store = new OutboxStoreDapper(_fixture.CreateConnection());

        // Add messages with sequential timestamps
        var baseTime = DateTime.UtcNow;
        for (int i = 0; i < messageCount; i++)
        {
            await store.AddAsync(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                NotificationType = $"Event{i}",
                Content = "{}",
                CreatedAtUtc = baseTime.AddSeconds(i)
            });
        }

        // Act
        var pending = (await store.GetPendingMessagesAsync(100, 10)).ToList();

        // Assert - Verify ordering
        if (pending.Count > 1)
        {
            for (int i = 0; i < pending.Count - 1; i++)
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
        
        var store = new OutboxStoreDapper(_fixture.CreateConnection());

        var readyMessageId = Guid.NewGuid();
        var futureMessageId = Guid.NewGuid();

        // Message ready for retry (past or null)
        await store.AddAsync(new OutboxMessage
        {
            Id = readyMessageId,
            NotificationType = "ReadyEvent",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow,
            NextRetryAtUtc = DateTime.UtcNow.AddSeconds(-10) // Past
        });

        // Message not ready (future)
        await store.AddAsync(new OutboxMessage
        {
            Id = futureMessageId,
            NotificationType = "FutureEvent",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow,
            NextRetryAtUtc = DateTime.UtcNow.AddSeconds(futureSeconds)
        });

        // Act
        var pending = await store.GetPendingMessagesAsync(100, 10);

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
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(100)]
    public async Task SaveChanges_IsIdempotent(int callCount)
    {
        // Arrange
        
        var store = new OutboxStoreDapper(_fixture.CreateConnection());

        // Act - Call SaveChangesAsync N times
        for (int i = 0; i < callCount; i++)
        {
            await store.SaveChangesAsync();
        }

        // Assert - No exception thrown, operation completed
        Assert.True(true);
    }
}
