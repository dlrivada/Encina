using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Encina.EntityFrameworkCore.Outbox;
using Encina.Messaging.Outbox;

namespace Encina.EntityFrameworkCore.PropertyTests.Outbox;

/// <summary>
/// Property-based tests for <see cref="OutboxStoreEF"/>.
/// Verifies invariants that MUST hold for ALL possible inputs.
/// </summary>
[Trait("Category", "Property")]
[SuppressMessage("Usage", "CA1001:Types that own disposable fields should be disposable", Justification = "IAsyncLifetime handles disposal via DisposeAsync")]
public sealed class OutboxStoreEFPropertyTests : IAsyncLifetime
{
    private TestDbContext? _dbContext;
    private OutboxStoreEF? _store;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase($"OutboxPropertyTests_{Guid.NewGuid()}")
            .Options;

        _dbContext = new TestDbContext(options);
        _store = new OutboxStoreEF(_dbContext);

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
    /// Property: A message that is added can ALWAYS be retrieved in pending messages.
    /// </summary>
    [Fact]
    public async Task Property_AddThenGet_MessageAlwaysRetrievableInPending()
    {
        // Generate random test cases
        var testCases = Enumerable.Range(0, 20).Select(_ => new
        {
            Id = Guid.NewGuid(),
            NotificationType = $"TestNotification_{Guid.NewGuid()}",
            Content = $"{{\"test\":\"{Guid.NewGuid()}\"}}"
        }).ToList();

        foreach (var testCase in testCases)
        {
            // Arrange
            var message = new OutboxMessage
            {
                Id = testCase.Id,
                NotificationType = testCase.NotificationType,
                Content = testCase.Content,
                CreatedAtUtc = DateTime.UtcNow,
                ProcessedAtUtc = null,
                RetryCount = 0
            };

            // Act
            await _store!.AddAsync(message);
            await _store.SaveChangesAsync();

            // Assert - message must be in pending
            var pending = await _store.GetPendingMessagesAsync(
                batchSize: 100,
                maxRetries: 5);

            pending.Should().Contain(m => m.Id == testCase.Id);
        }
    }

    /// <summary>
    /// Property: Once marked as processed, a message NEVER appears in pending again.
    /// </summary>
    [Fact]
    public async Task Property_MarkAsProcessed_NeverReturnedInPending()
    {
        // Generate multiple test messages
        var messages = Enumerable.Range(0, 15).Select(i => new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = $"TestNotification_{i}",
            Content = $"{{\"index\":{i}}}",
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(-i),
            RetryCount = 0
        }).ToList();

        foreach (var message in messages)
        {
            await _store!.AddAsync(message);
        }
        await _store!.SaveChangesAsync();

        // Mark each as processed and verify it's gone
        foreach (var message in messages)
        {
            // Act
            await _store.MarkAsProcessedAsync(message.Id);
            await _store.SaveChangesAsync();

            // Assert
            var pending = await _store.GetPendingMessagesAsync(100, 5);
            pending.Should().NotContain(m => m.Id == message.Id,
                "processed messages must NEVER appear in pending");
        }
    }

    /// <summary>
    /// Property: GetPendingMessages ALWAYS returns at most batchSize messages.
    /// </summary>
    [Fact]
    public async Task Property_BatchSize_AlwaysRespectsLimit()
    {
        // Test with various batch sizes
        var batchSizes = new[] { 1, 5, 10, 20, 50 };

        foreach (var batchSize in batchSizes)
        {
            // Arrange - create more messages than batch size
            var messageCount = batchSize + Random.Shared.Next(1, 20);
            await ClearDatabase();

            for (int i = 0; i < messageCount; i++)
            {
                await _store!.AddAsync(new OutboxMessage
                {
                    Id = Guid.NewGuid(),
                    NotificationType = $"TestNotification_{i}",
                    Content = $"{{\"index\":{i}}}",
                    CreatedAtUtc = DateTime.UtcNow,
                    RetryCount = 0
                });
            }
            await _store!.SaveChangesAsync();

            // Act
            var pending = await _store!.GetPendingMessagesAsync(batchSize, 5);

            // Assert
            pending.Count().Should().BeLessThanOrEqualTo(batchSize,
                $"batch size {batchSize} must ALWAYS be respected");
        }
    }

    /// <summary>
    /// Property: Messages with RetryCount >= maxRetries NEVER appear in pending.
    /// </summary>
    [Fact]
    public async Task Property_RetryExhaustion_ExcludedFromPending()
    {
        const int maxRetries = 3;

        // Create messages with different retry counts
        var testCases = new[]
        {
            (RetryCount: 0, ShouldAppear: true),
            (RetryCount: 1, ShouldAppear: true),
            (RetryCount: 2, ShouldAppear: true),
            (RetryCount: 3, ShouldAppear: false),  // Exhausted
            (RetryCount: 4, ShouldAppear: false),  // Exhausted
            (RetryCount: 10, ShouldAppear: false)  // Exhausted
        };

        foreach (var (retryCount, shouldAppear) in testCases)
        {
            var message = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                NotificationType = $"Test_{retryCount}",
                Content = "{}",
                CreatedAtUtc = DateTime.UtcNow,
                RetryCount = retryCount
            };

            await _store!.AddAsync(message);
            await _store.SaveChangesAsync();

            // Act
            var pending = await _store.GetPendingMessagesAsync(100, maxRetries);

            // Assert
            if (shouldAppear)
            {
                pending.Should().Contain(m => m.Id == message.Id,
                    $"message with {retryCount} retries < {maxRetries} must appear");
            }
            else
            {
                pending.Should().NotContain(m => m.Id == message.Id,
                    $"message with {retryCount} retries >= {maxRetries} must NEVER appear");
            }

            // Clear for next test
            await ClearDatabase();
        }
    }

    /// <summary>
    /// Property: Pending messages ALWAYS ordered by CreatedAtUtc ascending.
    /// </summary>
    [Fact]
    public async Task Property_Ordering_AlwaysCreatedAtUtcAscending()
    {
        // Create messages with random timestamps
        var baseTime = DateTime.UtcNow.AddHours(-10);
        var messages = Enumerable.Range(0, 20)
            .Select(i => new OutboxMessage
            {
                Id = Guid.NewGuid(),
                NotificationType = $"Test_{i}",
                Content = $"{{\"index\":{i}}}",
                CreatedAtUtc = baseTime.AddMinutes(Random.Shared.Next(-100, 100)),
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
        var pending = (await _store!.GetPendingMessagesAsync(100, 5)).ToList();

        // Assert - must be ordered by CreatedAtUtc ascending
        for (int i = 1; i < pending.Count; i++)
        {
            pending[i].CreatedAtUtc.Should().BeOnOrAfter(pending[i - 1].CreatedAtUtc,
                "pending messages must ALWAYS be ordered by CreatedAtUtc ascending");
        }
    }

    /// <summary>
    /// Property: Messages with NextRetryAtUtc in the future NEVER appear in pending.
    /// </summary>
    [Fact]
    public async Task Property_NextRetry_FutureRetriesExcluded()
    {
        var now = DateTime.UtcNow;

        var testCases = new[]
        {
            (NextRetry: (DateTime?)null, ShouldAppear: true),           // No retry scheduled
            (NextRetry: (DateTime?)now.AddMinutes(-10), ShouldAppear: true),  // Past retry
            (NextRetry: (DateTime?)now.AddSeconds(-1), ShouldAppear: true),   // Just passed
            (NextRetry: (DateTime?)now.AddMinutes(5), ShouldAppear: false),   // Future
            (NextRetry: (DateTime?)now.AddHours(1), ShouldAppear: false)      // Far future
        };

        foreach (var (nextRetry, shouldAppear) in testCases)
        {
            var message = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                NotificationType = "TestRetry",
                Content = "{}",
                CreatedAtUtc = now.AddMinutes(-30),
                RetryCount = 1,
                NextRetryAtUtc = nextRetry
            };

            await _store!.AddAsync(message);
            await _store.SaveChangesAsync();

            // Act
            var pending = await _store.GetPendingMessagesAsync(100, 5);

            // Assert
            if (shouldAppear)
            {
                pending.Should().Contain(m => m.Id == message.Id,
                    $"message with NextRetryAtUtc={nextRetry} should appear");
            }
            else
            {
                pending.Should().NotContain(m => m.Id == message.Id,
                    $"message with future NextRetryAtUtc={nextRetry} must NEVER appear");
            }

            await ClearDatabase();
        }
    }

    /// <summary>
    /// Property: MarkAsFailed ALWAYS increments RetryCount.
    /// </summary>
    [Fact]
    public async Task Property_MarkAsFailed_AlwaysIncrementsRetryCount()
    {
        var testCases = Enumerable.Range(0, 10).Select(i => new
        {
            InitialRetryCount = i,
            ErrorMessage = $"Error_{Guid.NewGuid()}"
        }).ToList();

        foreach (var testCase in testCases)
        {
            // Arrange
            var message = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                NotificationType = "TestFail",
                Content = "{}",
                CreatedAtUtc = DateTime.UtcNow,
                RetryCount = testCase.InitialRetryCount
            };

            await _store!.AddAsync(message);
            await _store.SaveChangesAsync();

            // Act
            await _store.MarkAsFailedAsync(
                message.Id,
                testCase.ErrorMessage,
                DateTime.UtcNow.AddMinutes(5));
            await _store.SaveChangesAsync();

            // Assert
            var retrieved = _dbContext!.Set<OutboxMessage>().First(m => m.Id == message.Id);
            retrieved.RetryCount.Should().Be(testCase.InitialRetryCount + 1,
                "MarkAsFailed must ALWAYS increment RetryCount by exactly 1");
            retrieved.ErrorMessage.Should().Be(testCase.ErrorMessage);

            await ClearDatabase();
        }
    }

    /// <summary>
    /// Property: MarkAsProcessed ALWAYS clears ErrorMessage.
    /// </summary>
    [Fact]
    public async Task Property_MarkAsProcessed_AlwaysClearsError()
    {
        var errorMessages = new[]
        {
            "Connection timeout",
            "Serialization failed",
            null,  // Already no error
            "",    // Empty error
            "Multiple\nLine\nError"
        };

        foreach (var errorMessage in errorMessages)
        {
            // Arrange
            var message = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                NotificationType = "TestClearError",
                Content = "{}",
                CreatedAtUtc = DateTime.UtcNow,
                ErrorMessage = errorMessage,
                RetryCount = 2
            };

            await _store!.AddAsync(message);
            await _store.SaveChangesAsync();

            // Act
            await _store.MarkAsProcessedAsync(message.Id);
            await _store.SaveChangesAsync();

            // Assert
            var retrieved = _dbContext!.Set<OutboxMessage>().First(m => m.Id == message.Id);
            retrieved.ErrorMessage.Should().BeNull(
                "MarkAsProcessed must ALWAYS clear ErrorMessage");
            retrieved.ProcessedAtUtc.Should().NotBeNull();

            await ClearDatabase();
        }
    }

    /// <summary>
    /// Property: Concurrent adds ALWAYS succeed without corruption.
    /// </summary>
    [Fact]
    public async Task Property_ConcurrentAdds_AlwaysSucceed()
    {
        const int concurrentWrites = 50;

        var messages = Enumerable.Range(0, concurrentWrites)
            .Select(i => new OutboxMessage
            {
                Id = Guid.NewGuid(),
                NotificationType = $"Concurrent_{i}",
                Content = $"{{\"index\":{i}}}",
                CreatedAtUtc = DateTime.UtcNow,
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
        var allMessages = _dbContext!.Set<OutboxMessage>().ToList();
        allMessages.Should().HaveCount(concurrentWrites,
            "ALL concurrent adds must succeed");

        foreach (var original in messages)
        {
            allMessages.Should().Contain(m => m.Id == original.Id,
                "every added message must be retrievable");
        }
    }

    /// <summary>
    /// Property: IsProcessed ALWAYS reflects ProcessedAtUtc and ErrorMessage state.
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
            var message = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                NotificationType = "TestIsProcessed",
                Content = "{}",
                CreatedAtUtc = DateTime.UtcNow,
                ProcessedAtUtc = processedAt,
                ErrorMessage = error,
                RetryCount = 0
            };

            message.IsProcessed.Should().Be(expected,
                $"IsProcessed with ProcessedAt={processedAt}, Error={error} must be {expected}");
        }
    }

    private async Task ClearDatabase()
    {
        var allMessages = await _dbContext!.Set<OutboxMessage>().ToListAsync();
        _dbContext.Set<OutboxMessage>().RemoveRange(allMessages);
        await _dbContext.SaveChangesAsync();
    }

    private sealed class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions options) : base(options) { }

        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OutboxMessage>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.NotificationType).IsRequired();
                entity.Property(e => e.Content).IsRequired();
            });
        }
    }
}
