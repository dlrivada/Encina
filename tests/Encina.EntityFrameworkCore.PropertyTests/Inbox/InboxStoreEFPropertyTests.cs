using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Encina.EntityFrameworkCore.Inbox;
using Encina.Messaging.Inbox;

namespace Encina.EntityFrameworkCore.PropertyTests.Inbox;

/// <summary>
/// Property-based tests for <see cref="InboxStoreEF"/>.
/// Verifies invariants that MUST hold for ALL possible inputs.
/// </summary>
[Trait("Category", "Property")]
[SuppressMessage("Usage", "CA1001:Types that own disposable fields should be disposable", Justification = "IAsyncLifetime handles disposal via DisposeAsync")]
public sealed class InboxStoreEFPropertyTests : IAsyncLifetime
{
    private TestDbContext? _dbContext;
    private InboxStoreEF? _store;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase($"InboxPropertyTests_{Guid.NewGuid()}")
            .Options;

        _dbContext = new TestDbContext(options);
        _store = new InboxStoreEF(_dbContext);

        await _dbContext.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        if (_dbContext != null)
        {
            await _dbContext.Database.EnsureDeletedAsync();
            await _dbContext.DisposeAsync();
        }
    }

    /// <summary>
    /// Property: A message that is added can ALWAYS be retrieved by its MessageId.
    /// </summary>
    [Fact]
    public async Task Property_AddThenGet_AlwaysRetrievableById()
    {
        // Generate random test cases
        var testCases = Enumerable.Range(0, 20).Select(_ => new
        {
            MessageId = Guid.NewGuid().ToString(),
            RequestType = $"TestRequest_{Guid.NewGuid()}",
            ExpiresAt = DateTime.UtcNow.AddDays(Random.Shared.Next(1, 30))
        }).ToList();

        foreach (var testCase in testCases)
        {
            // Arrange
            var message = new InboxMessage
            {
                MessageId = testCase.MessageId,
                RequestType = testCase.RequestType,
                ReceivedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = testCase.ExpiresAt,
                RetryCount = 0
            };

            // Act
            await _store!.AddAsync(message);
            await _store.SaveChangesAsync();

            // Assert
            var retrieved = await _store.GetMessageAsync(testCase.MessageId);
            retrieved.Should().NotBeNull("added message must ALWAYS be retrievable");
            retrieved!.MessageId.Should().Be(testCase.MessageId);
            retrieved.RequestType.Should().Be(testCase.RequestType);
        }
    }

    /// <summary>
    /// Property: Expired messages ALWAYS returned by GetExpiredMessagesAsync.
    /// </summary>
    [Fact]
    public async Task Property_Expiration_ExpiredAlwaysReturned()
    {
        var now = DateTime.UtcNow;

        var testCases = new[]
        {
            (ExpiresAt: now.AddHours(-2), ShouldAppear: true),   // Expired 2h ago
            (ExpiresAt: now.AddMinutes(-1), ShouldAppear: true), // Just expired
            (ExpiresAt: now, ShouldAppear: true),                // Expires now (inclusive)
            (ExpiresAt: now.AddMinutes(1), ShouldAppear: false), // Expires in 1min
            (ExpiresAt: now.AddDays(1), ShouldAppear: false)     // Expires tomorrow
        };

        foreach (var (expiresAt, shouldAppear) in testCases)
        {
            var message = new InboxMessage
            {
                MessageId = Guid.NewGuid().ToString(),
                RequestType = "TestExpiration",
                ReceivedAtUtc = now.AddHours(-3),
                ExpiresAtUtc = expiresAt,
                RetryCount = 0
            };

            await _store!.AddAsync(message);
            await _store.SaveChangesAsync();

            // Act
            var expired = await _store.GetExpiredMessagesAsync(100);

            // Assert
            if (shouldAppear)
            {
                expired.Should().Contain(m => m.MessageId == message.MessageId,
                    $"message with ExpiresAtUtc={expiresAt} should be in expired list");
            }
            else
            {
                expired.Should().NotContain(m => m.MessageId == message.MessageId,
                    $"message with future ExpiresAtUtc={expiresAt} must NOT be in expired list");
            }

            await ClearDatabase();
        }
    }

    /// <summary>
    /// Property: Same MessageId ALWAYS returns the same message (idempotency key).
    /// </summary>
    [Fact]
    public async Task Property_Idempotency_SameMessageIdAlwaysSameMessage()
    {
        const string messageId = "idempotency-test-123";

        // Add message
        var original = new InboxMessage
        {
            MessageId = messageId,
            RequestType = "TestIdempotency",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            RetryCount = 0
        };

        await _store!.AddAsync(original);
        await _store.SaveChangesAsync();

        // Retrieve multiple times
        for (int i = 0; i < 10; i++)
        {
            var retrieved = await _store.GetMessageAsync(messageId);

            retrieved.Should().NotBeNull();
            retrieved!.MessageId.Should().Be(messageId,
                "same MessageId must ALWAYS return the same message");
            retrieved.RequestType.Should().Be("TestIdempotency");
        }
    }

    /// <summary>
    /// Property: Once marked as processed, message ALWAYS has ProcessedAtUtc set.
    /// </summary>
    [Fact]
    public async Task Property_Processing_MarkedProcessedHasTimestamp()
    {
        var testCases = Enumerable.Range(0, 15).Select(i => new
        {
            MessageId = $"msg-{i}",
            Response = $"{{\"result\":\"{Guid.NewGuid()}\"}}"
        }).ToList();

        foreach (var testCase in testCases)
        {
            // Arrange
            var message = new InboxMessage
            {
                MessageId = testCase.MessageId,
                RequestType = "TestProcessing",
                ReceivedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
                RetryCount = 0
            };

            await _store!.AddAsync(message);
            await _store.SaveChangesAsync();

            // Act
            await _store.MarkAsProcessedAsync(testCase.MessageId, testCase.Response);
            await _store.SaveChangesAsync();

            // Assert
            var processed = await _store.GetMessageAsync(testCase.MessageId);
            processed.Should().NotBeNull();
            processed!.ProcessedAtUtc.Should().NotBeNull(
                "processed message must ALWAYS have ProcessedAtUtc set");
            processed.Response.Should().Be(testCase.Response);
            processed.ErrorMessage.Should().BeNull(
                "successful processing must ALWAYS clear ErrorMessage");
        }
    }

    /// <summary>
    /// Property: GetExpiredMessages ALWAYS respects batch size limit.
    /// </summary>
    [Fact]
    public async Task Property_BatchSize_AlwaysRespectsLimit()
    {
        var batchSizes = new[] { 1, 5, 10, 25 };

        foreach (var batchSize in batchSizes)
        {
            await ClearDatabase();

            // Create more expired messages than batch size
            var messageCount = batchSize + Random.Shared.Next(5, 15);
            var now = DateTime.UtcNow;

            for (int i = 0; i < messageCount; i++)
            {
                await _store!.AddAsync(new InboxMessage
                {
                    MessageId = $"batch-test-{i}",
                    RequestType = "BatchTest",
                    ReceivedAtUtc = now.AddDays(-10),
                    ExpiresAtUtc = now.AddDays(-1),  // All expired
                    RetryCount = 0
                });
            }
            await _store!.SaveChangesAsync();

            // Act
            var expired = await _store!.GetExpiredMessagesAsync(batchSize);

            // Assert
            expired.Count().Should().BeLessThanOrEqualTo(batchSize,
                $"batch size {batchSize} must ALWAYS be respected");
        }
    }

    /// <summary>
    /// Property: Expired messages ALWAYS ordered by ExpiresAtUtc ascending.
    /// </summary>
    [Fact]
    public async Task Property_Ordering_ExpiredOrderedByExpiresAtUtc()
    {
        var now = DateTime.UtcNow;
        var baseExpiry = now.AddDays(-10);

        // Create expired messages with random expiry times
        var messages = Enumerable.Range(0, 20)
            .Select(i => new InboxMessage
            {
                MessageId = $"order-test-{i}",
                RequestType = "OrderTest",
                ReceivedAtUtc = now.AddDays(-15),
                ExpiresAtUtc = baseExpiry.AddMinutes(Random.Shared.Next(-100, 0)),
                RetryCount = 0
            })
            .OrderBy(_ => Random.Shared.Next()) // Randomize insertion order
            .ToList();

        foreach (var message in messages)
        {
            await _store!.AddAsync(message);
        }
        await _store!.SaveChangesAsync();

        // Act
        var expired = (await _store!.GetExpiredMessagesAsync(100)).ToList();

        // Assert - must be ordered by ExpiresAtUtc ascending
        for (int i = 1; i < expired.Count; i++)
        {
            expired[i].ExpiresAtUtc.Should().BeOnOrAfter(expired[i - 1].ExpiresAtUtc,
                "expired messages must ALWAYS be ordered by ExpiresAtUtc ascending");
        }
    }

    /// <summary>
    /// Property: RemoveExpiredMessages ALWAYS removes ALL specified messages.
    /// </summary>
    [Fact]
    public async Task Property_Remove_AlwaysRemovesAllSpecified()
    {
        // Create a set of expired messages
        var messageCount = Random.Shared.Next(10, 25);
        var messageIds = new List<string>();

        for (int i = 0; i < messageCount; i++)
        {
            var messageId = $"remove-test-{i}";
            messageIds.Add(messageId);

            await _store!.AddAsync(new InboxMessage
            {
                MessageId = messageId,
                RequestType = "RemoveTest",
                ReceivedAtUtc = DateTime.UtcNow.AddDays(-30),
                ExpiresAtUtc = DateTime.UtcNow.AddDays(-1),
                RetryCount = 0
            });
        }
        await _store!.SaveChangesAsync();

        // Remove random subset
        var toRemove = messageIds
            .OrderBy(_ => Random.Shared.Next())
            .Take(messageCount / 2)
            .ToList();

        // Act
        await _store!.RemoveExpiredMessagesAsync(toRemove);
        await _store.SaveChangesAsync();

        // Assert - removed messages should be gone
        foreach (var messageId in toRemove)
        {
            var retrieved = await _store.GetMessageAsync(messageId);
            retrieved.Should().BeNull(
                $"removed message {messageId} must NOT be retrievable");
        }

        // Not removed messages should still exist
        var notRemoved = messageIds.Except(toRemove);
        foreach (var messageId in notRemoved)
        {
            var retrieved = await _store.GetMessageAsync(messageId);
            retrieved.Should().NotBeNull(
                $"non-removed message {messageId} must still be retrievable");
        }
    }

    /// <summary>
    /// Property: MarkAsFailed ALWAYS increments RetryCount and sets error.
    /// </summary>
    [Fact]
    public async Task Property_MarkAsFailed_AlwaysIncrementsAndSetsError()
    {
        var testCases = Enumerable.Range(0, 10).Select(i => new
        {
            InitialRetryCount = i,
            ErrorMessage = $"Error at retry {i}: {Guid.NewGuid()}"
        }).ToList();

        foreach (var testCase in testCases)
        {
            // Arrange
            var messageId = $"fail-test-{testCase.InitialRetryCount}";
            var message = new InboxMessage
            {
                MessageId = messageId,
                RequestType = "FailTest",
                ReceivedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
                RetryCount = testCase.InitialRetryCount
            };

            await _store!.AddAsync(message);
            await _store.SaveChangesAsync();

            // Act
            await _store.MarkAsFailedAsync(
                messageId,
                testCase.ErrorMessage,
                DateTime.UtcNow.AddMinutes(5));
            await _store.SaveChangesAsync();

            // Assert
            var failed = await _store.GetMessageAsync(messageId);
            failed.Should().NotBeNull();
            failed!.RetryCount.Should().Be(testCase.InitialRetryCount + 1,
                "MarkAsFailed must ALWAYS increment RetryCount by exactly 1");
            failed.ErrorMessage.Should().Be(testCase.ErrorMessage);
            failed.NextRetryAtUtc.Should().NotBeNull();

            await ClearDatabase();
        }
    }

    /// <summary>
    /// Property: IsProcessed ALWAYS reflects correct state.
    /// </summary>
    [Fact]
    public async Task Property_IsProcessed_ReflectsState()
    {
        var testCases = new[]
        {
            (ProcessedAt: (DateTime?)null, Error: (string?)null, Expected: false),
            (ProcessedAt: (DateTime?)null, Error: "Error", Expected: false),
            (ProcessedAt: (DateTime?)DateTime.UtcNow, Error: (string?)null, Expected: true),
            (ProcessedAt: (DateTime?)DateTime.UtcNow, Error: "Error", Expected: false)
        };

        foreach (var (processedAt, error, expected) in testCases)
        {
            var message = new InboxMessage
            {
                MessageId = Guid.NewGuid().ToString(),
                RequestType = "IsProcessedTest",
                ReceivedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
                ProcessedAtUtc = processedAt,
                ErrorMessage = error,
                RetryCount = 0
            };

            message.IsProcessed.Should().Be(expected,
                $"IsProcessed with ProcessedAt={processedAt}, Error={error} must be {expected}");
        }
    }

    /// <summary>
    /// Property: IsExpired ALWAYS correctly identifies expired messages.
    /// </summary>
    [Fact]
    public async Task Property_IsExpired_CorrectlyIdentifies()
    {
        var now = DateTime.UtcNow;

        var testCases = new[]
        {
            (ExpiresAt: now.AddHours(-1), Expected: true),
            (ExpiresAt: now.AddSeconds(-1), Expected: true),
            (ExpiresAt: now, Expected: true),  // Inclusive
            (ExpiresAt: now.AddSeconds(1), Expected: false),
            (ExpiresAt: now.AddDays(1), Expected: false)
        };

        foreach (var (expiresAt, expected) in testCases)
        {
            var message = new InboxMessage
            {
                MessageId = Guid.NewGuid().ToString(),
                RequestType = "IsExpiredTest",
                ReceivedAtUtc = now.AddDays(-2),
                ExpiresAtUtc = expiresAt,
                RetryCount = 0
            };

            // Note: IsExpired() is called at message creation time,
            // so we need to account for slight time differences
            var isExpired = message.IsExpired();
            isExpired.Should().Be(expected,
                $"message with ExpiresAtUtc={expiresAt} should have IsExpired={expected}");
        }
    }

    /// <summary>
    /// Property: Concurrent adds with different MessageIds ALWAYS succeed.
    /// </summary>
    [Fact]
    public async Task Property_ConcurrentAdds_AlwaysSucceed()
    {
        const int concurrentWrites = 50;
        var now = DateTime.UtcNow;

        var messages = Enumerable.Range(0, concurrentWrites)
            .Select(i => new InboxMessage
            {
                MessageId = $"concurrent-{i}",
                RequestType = $"ConcurrentTest_{i}",
                ReceivedAtUtc = now,
                ExpiresAtUtc = now.AddDays(7),
                RetryCount = 0
            })
            .ToList();

        // Act - concurrent writes
        var tasks = messages.Select(async msg =>
        {
            await _store!.AddAsync(msg);
            await _store.SaveChangesAsync();
        });

        await Task.WhenAll(tasks);

        // Assert - all messages must be present
        foreach (var original in messages)
        {
            var retrieved = await _store!.GetMessageAsync(original.MessageId);
            retrieved.Should().NotBeNull(
                $"message {original.MessageId} must be retrievable after concurrent add");
        }
    }

    private async Task ClearDatabase()
    {
        var allMessages = await _dbContext!.Set<InboxMessage>().ToListAsync();
        _dbContext.Set<InboxMessage>().RemoveRange(allMessages);
        await _dbContext.SaveChangesAsync();
    }

    private sealed class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions options) : base(options) { }

        public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<InboxMessage>(entity =>
            {
                entity.HasKey(e => e.MessageId);
                entity.Property(e => e.RequestType).IsRequired();
            });
        }
    }
}
