using Encina.Dapper.SqlServer.Inbox;
using Encina.TestInfrastructure.Extensions;
using Encina.TestInfrastructure.Fixtures;
using Xunit;

namespace Encina.Dapper.SqlServer.Tests.Inbox;

/// <summary>
/// Load tests for <see cref="InboxStoreDapper"/>.
/// Verifies behavior under concurrency, volume, and stress conditions.
/// </summary>
[Trait("Category", "Load")]
public sealed class InboxStoreDapperLoadTests : IClassFixture<SqlServerFixture>
{
    private readonly SqlServerFixture _database;
    private readonly InboxStoreDapper _store;

    public InboxStoreDapperLoadTests(SqlServerFixture database)
    {
        _database = database;
        

        // Clear all data before each test to ensure clean state
        _database.ClearAllDataAsync().GetAwaiter().GetResult();

        _store = new InboxStoreDapper(_database.CreateConnection());
    }

    #region Concurrency Tests

    [Fact]
    public async Task AddAsync_ConcurrentWrites_AllSucceed()
    {
        // Arrange
        var tasks = new List<Task>();
        const int concurrentWrites = 50;

        // Act - Write 50 messages concurrently
        for (int i = 0; i < concurrentWrites; i++)
        {
            var messageId = $"concurrent-add-{i}";
            var message = new InboxMessage
            {
                MessageId = messageId,
                RequestType = "ConcurrentTest",
                ReceivedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
                RetryCount = 0
            };
            tasks.Add(_store.AddAsync(message));
        }

        await Task.WhenAll(tasks);

        // Assert - All messages persisted
        for (int i = 0; i < concurrentWrites; i++)
        {
            var retrieved = await _store.GetMessageAsync($"concurrent-add-{i}");
            Assert.NotNull(retrieved);
        }
    }

    [Fact]
    public async Task MarkAsProcessedAsync_ConcurrentUpdates_AllSucceed()
    {
        // Arrange - Create 30 messages
        const int messageCount = 30;
        for (int i = 0; i < messageCount; i++)
        {
            var message = new InboxMessage
            {
                MessageId = $"concurrent-process-{i}",
                RequestType = "ConcurrentTest",
                ReceivedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
                RetryCount = 0
            };
            await _store.AddAsync(message);
        }

        // Act - Mark all as processed concurrently
        var tasks = new List<Task>();
        for (int i = 0; i < messageCount; i++)
        {
            var messageId = $"concurrent-process-{i}";
            tasks.Add(_store.MarkAsProcessedAsync(messageId, $"{{\"result\":{i}}}"));
        }

        await Task.WhenAll(tasks);

        // Assert - All marked as processed
        for (int i = 0; i < messageCount; i++)
        {
            var retrieved = await _store.GetMessageAsync($"concurrent-process-{i}");
            Assert.NotNull(retrieved);
            Assert.True(retrieved.IsProcessed);
        }
    }

    [Fact]
    public async Task MarkAsFailedAsync_ConcurrentRetries_AllIncrement()
    {
        // Arrange - Create 20 messages
        const int messageCount = 20;
        for (int i = 0; i < messageCount; i++)
        {
            var message = new InboxMessage
            {
                MessageId = $"concurrent-fail-{i}",
                RequestType = "ConcurrentTest",
                ReceivedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
                RetryCount = 0
            };
            await _store.AddAsync(message);
        }

        // Act - Mark all as failed concurrently (5 retries each)
        const int retriesPerMessage = 5;
        for (int retry = 0; retry < retriesPerMessage; retry++)
        {
            var tasks = new List<Task>();
            for (int i = 0; i < messageCount; i++)
            {
                var messageId = $"concurrent-fail-{i}";
                tasks.Add(_store.MarkAsFailedAsync(messageId, $"Error {retry}", null));
            }
            await Task.WhenAll(tasks);
        }

        // Assert - All have correct retry count
        for (int i = 0; i < messageCount; i++)
        {
            var retrieved = await _store.GetMessageAsync($"concurrent-fail-{i}");
            Assert.NotNull(retrieved);
            Assert.Equal(retriesPerMessage, retrieved.RetryCount);
        }
    }

    #endregion

    #region Volume Tests

    [Fact]
    public async Task AddAsync_LargeVolume_AllPersist()
    {
        // Arrange & Act - Add 500 messages
        const int messageCount = 500;
        for (int i = 0; i < messageCount; i++)
        {
            var message = new InboxMessage
            {
                MessageId = $"volume-test-{i}",
                RequestType = "VolumeTest",
                ReceivedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
                RetryCount = 0
            };
            await _store.AddAsync(message);
        }

        // Assert - Verify random samples
        var sampleIndices = new[] { 0, 100, 250, 400, 499 };
        foreach (var index in sampleIndices)
        {
            var retrieved = await _store.GetMessageAsync($"volume-test-{index}");
            Assert.NotNull(retrieved);
        }
    }

    [Fact]
    public async Task GetExpiredMessagesAsync_LargeBatch_ReturnsCorrectly()
    {
        // Arrange - Create 200 expired messages
        const int expiredCount = 200;
        for (int i = 0; i < expiredCount; i++)
        {
            var message = new InboxMessage
            {
                MessageId = $"batch-expired-{i}",
                RequestType = "BatchTest",
                ReceivedAtUtc = DateTime.UtcNow.AddDays(-40),
                ProcessedAtUtc = DateTime.UtcNow.AddDays(-35),
                ExpiresAtUtc = DateTime.UtcNow.AddDays(-i - 1),
                RetryCount = 0
            };
            await _store.AddAsync(message);
        }

        // Act - Get all expired (batch size = 500)
        var expired = await _store.GetExpiredMessagesAsync(500);

        // Assert
        Assert.Equal(expiredCount, expired.Count());
    }

    [Fact]
    public async Task RemoveExpiredMessagesAsync_LargeBatch_AllRemoved()
    {
        // Arrange - Create 150 expired messages
        const int expiredCount = 150;
        var messageIds = new List<string>();
        for (int i = 0; i < expiredCount; i++)
        {
            var messageId = $"remove-batch-{i}";
            messageIds.Add(messageId);
            var message = new InboxMessage
            {
                MessageId = messageId,
                RequestType = "RemoveTest",
                ReceivedAtUtc = DateTime.UtcNow.AddDays(-40),
                ProcessedAtUtc = DateTime.UtcNow.AddDays(-35),
                ExpiresAtUtc = DateTime.UtcNow.AddDays(-1),
                RetryCount = 0
            };
            await _store.AddAsync(message);
        }

        // Act - Remove all in one batch
        await _store.RemoveExpiredMessagesAsync(messageIds);

        // Assert - Verify random samples are gone
        var sampleIndices = new[] { 0, 50, 100, 149 };
        foreach (var index in sampleIndices)
        {
            var retrieved = await _store.GetMessageAsync($"remove-batch-{index}");
            Assert.Null(retrieved);
        }
    }

    #endregion

    #region Stress Tests

    [Fact]
    public async Task MixedOperations_HighConcurrency_NoDataCorruption()
    {
        // Arrange - Create 30 messages
        const int messageCount = 30;
        for (int i = 0; i < messageCount; i++)
        {
            var message = new InboxMessage
            {
                MessageId = $"stress-{i}",
                RequestType = "StressTest",
                ReceivedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
                RetryCount = 0
            };
            await _store.AddAsync(message);
        }

        // Act - Mix of concurrent operations
        var tasks = new List<Task>();

        // 10 reads
        for (int i = 0; i < 10; i++)
        {
            var messageId = $"stress-{i}";
            tasks.Add(Task.Run(async () => await _store.GetMessageAsync(messageId)));
        }

        // 10 updates (mark as processed)
        for (int i = 10; i < 20; i++)
        {
            var messageId = $"stress-{i}";
            tasks.Add(Task.Run(async () => await _store.MarkAsProcessedAsync(messageId, "{\"ok\":true}")));
        }

        // 10 failures
        for (int i = 20; i < 30; i++)
        {
            var messageId = $"stress-{i}";
            tasks.Add(Task.Run(async () => await _store.MarkAsFailedAsync(messageId, "Stress test error", null)));
        }

        await Task.WhenAll(tasks);

        // Assert - Data integrity maintained
        for (int i = 0; i < 10; i++)
        {
            var retrieved = await _store.GetMessageAsync($"stress-{i}");
            Assert.NotNull(retrieved);
        }

        for (int i = 10; i < 20; i++)
        {
            var retrieved = await _store.GetMessageAsync($"stress-{i}");
            Assert.NotNull(retrieved);
            Assert.True(retrieved.IsProcessed);
        }

        for (int i = 20; i < 30; i++)
        {
            var retrieved = await _store.GetMessageAsync($"stress-{i}");
            Assert.NotNull(retrieved);
            Assert.Equal(1, retrieved.RetryCount);
        }
    }

    [Fact]
    public async Task LargePayload_Processing_HandlesCorrectly()
    {
        // Arrange - Create message with 50KB response payload
        var messageId = "large-payload-stress";
        var message = new InboxMessage
        {
            MessageId = messageId,
            RequestType = "LargePayloadTest",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            RetryCount = 0
        };
        await _store.AddAsync(message);

        // Act - Store large response (50KB)
        var largeResponse = new string('X', 50_000);
        await _store.MarkAsProcessedAsync(messageId, largeResponse);

        // Assert - Large payload persisted correctly
        var retrieved = await _store.GetMessageAsync(messageId);
        Assert.NotNull(retrieved);
        Assert.Equal(50_000, retrieved.Response!.Length);
    }

    #endregion

    #region Idempotency Under Load

    [Fact]
    public async Task DuplicateRequests_HighConcurrency_IdempotentBehavior()
    {
        // Arrange - Same message ID used multiple times
        var messageId = "idempotency-test";
        var message = new InboxMessage
        {
            MessageId = messageId,
            RequestType = "IdempotencyTest",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            RetryCount = 0
        };
        await _store.AddAsync(message);
        await _store.MarkAsProcessedAsync(messageId, "{\"orderId\":999}");

        // Act - 20 concurrent reads (simulating duplicate requests)
        var tasks = new List<Task<bool>>();
        for (int i = 0; i < 20; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                var existing = await _store.GetMessageAsync(messageId);
                return existing?.IsProcessed ?? false;
            }));
        }

        var results = await Task.WhenAll(tasks);

        // Assert - All reads return processed = true
        Assert.All(results, isProcessed => Assert.True(isProcessed));
    }

    #endregion

    #region Performance Degradation

    [Fact]
    public async Task SequentialWrites_Performance_LinearDegradation()
    {
        // Arrange & Act - Measure time for batches of 50
        var batch1Time = await MeasureWriteTime(0, 50);
        var batch2Time = await MeasureWriteTime(50, 100);

        // Assert - Performance should be relatively consistent
        // (Batch 2 should not be significantly slower than Batch 1)
        var degradationRatio = (double)batch2Time.TotalMilliseconds / batch1Time.TotalMilliseconds;
        Assert.True(degradationRatio < 2.0, $"Performance degraded by {degradationRatio:F2}x");
    }

    private async Task<TimeSpan> MeasureWriteTime(int startIndex, int endIndex)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        for (int i = startIndex; i < endIndex; i++)
        {
            var message = new InboxMessage
            {
                MessageId = $"perf-{i}",
                RequestType = "PerfTest",
                ReceivedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
                RetryCount = 0
            };
            await _store.AddAsync(message);
        }

        stopwatch.Stop();
        return stopwatch.Elapsed;
    }

    #endregion
}
