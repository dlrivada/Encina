using Encina.Dapper.Oracle.Scheduling;
using Encina.TestInfrastructure.Extensions;
using Encina.TestInfrastructure.Fixtures;
using Xunit;

namespace Encina.Dapper.Oracle.Tests.Scheduling;

/// <summary>
/// Load tests for <see cref="ScheduledMessageStoreDapper"/>.
/// Verifies behavior under concurrency, volume, and stress conditions.
/// </summary>
[Trait("Category", "Load")]
public sealed class ScheduledMessageStoreDapperLoadTests : IClassFixture<OracleFixture>
{
    private readonly OracleFixture _database;
    private readonly ScheduledMessageStoreDapper _store;

    public ScheduledMessageStoreDapperLoadTests(OracleFixture database)
    {
        _database = database;
        DapperTypeHandlers.RegisterSqliteHandlers();

        // Clear all data before each test to ensure clean state
        _database.ClearAllDataAsync().GetAwaiter().GetResult();

        _store = new ScheduledMessageStoreDapper(_database.CreateConnection());
    }

    #region Concurrency Tests

    [Fact]
    public async Task AddAsync_ConcurrentWrites_AllSucceed()
    {
        // Arrange
        var tasks = new List<Task<Guid>>();
        const int concurrentWrites = 50;

        // Act - Write 50 messages concurrently
        for (int i = 0; i < concurrentWrites; i++)
        {
            var index = i; // Capture for closure
            tasks.Add(Task.Run(async () =>
            {
                var messageId = Guid.NewGuid();
                var message = new ScheduledMessage
                {
                    Id = messageId,
                    RequestType = $"ConcurrentCommand{index}",
                    Content = "{}",
                    ScheduledAtUtc = DateTime.UtcNow.AddHours(-1),
                    CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
                    RetryCount = 0,
                    IsRecurring = false
                };
                await _store.AddAsync(message);
                return messageId;
            }));
        }

        var messageIds = await Task.WhenAll(tasks);

        // Assert - Verify count via GetDueMessagesAsync
        var messages = await _store.GetDueMessagesAsync(100, 5);
        Assert.Equal(concurrentWrites, messages.Count());
    }

    [Fact]
    public async Task MarkAsProcessedAsync_ConcurrentUpdates_AllSucceed()
    {
        // Arrange - Create 30 messages
        const int messageCount = 30;
        var messageIds = new List<Guid>();
        for (int i = 0; i < messageCount; i++)
        {
            var messageId = Guid.NewGuid();
            messageIds.Add(messageId);
            var message = new ScheduledMessage
            {
                Id = messageId,
                RequestType = $"ProcessCommand{i}",
                Content = "{}",
                ScheduledAtUtc = DateTime.UtcNow.AddHours(-1),
                CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
                RetryCount = 0,
                IsRecurring = false
            };
            await _store.AddAsync(message);
        }

        // Act - Mark all as processed concurrently
        var tasks = new List<Task>();
        for (int i = 0; i < messageCount; i++)
        {
            var messageId = messageIds[i];
            tasks.Add(Task.Run(async () => await _store.MarkAsProcessedAsync(messageId)));
        }

        await Task.WhenAll(tasks);

        // Assert - All should be processed (not in due messages)
        var messages = await _store.GetDueMessagesAsync(100, 5);
        Assert.Empty(messages);
    }

    [Fact]
    public async Task MarkAsFailedAsync_ConcurrentRetries_AllIncrement()
    {
        // Arrange - Create 20 messages
        const int messageCount = 20;
        var messageIds = new List<Guid>();
        for (int i = 0; i < messageCount; i++)
        {
            var messageId = Guid.NewGuid();
            messageIds.Add(messageId);
            var message = new ScheduledMessage
            {
                Id = messageId,
                RequestType = $"FailCommand{i}",
                Content = "{}",
                ScheduledAtUtc = DateTime.UtcNow.AddHours(-1),
                CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
                RetryCount = 0,
                IsRecurring = false
            };
            await _store.AddAsync(message);
        }

        // Act - Mark all as failed 3 times concurrently
        const int failuresPerMessage = 3;
        for (int retry = 0; retry < failuresPerMessage; retry++)
        {
            var tasks = new List<Task>();
            for (int i = 0; i < messageCount; i++)
            {
                var messageId = messageIds[i];
                tasks.Add(Task.Run(async () =>
                    await _store.MarkAsFailedAsync(messageId, $"Error {retry}", DateTime.UtcNow.AddSeconds(-10))));
            }
            await Task.WhenAll(tasks);
        }

        // Assert - All have correct retry count
        var messages = await _store.GetDueMessagesAsync(100, 10);
        Assert.Equal(messageCount, messages.Count());
        Assert.All(messages, msg => Assert.Equal(failuresPerMessage, msg.RetryCount));
    }

    #endregion

    #region Volume Tests

    [Fact]
    public async Task AddAsync_LargeVolume_AllPersist()
    {
        // Arrange & Act - Add 500 messages
        const int messageCount = 500;
        var messageIds = new List<Guid>();
        for (int i = 0; i < messageCount; i++)
        {
            var messageId = Guid.NewGuid();
            messageIds.Add(messageId);
            var message = new ScheduledMessage
            {
                Id = messageId,
                RequestType = $"VolumeCommand{i}",
                Content = "{}",
                ScheduledAtUtc = DateTime.UtcNow.AddHours(-1),
                CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
                RetryCount = 0,
                IsRecurring = false
            };
            await _store.AddAsync(message);
        }

        // Assert - Verify count (batch 500)
        var messages = await _store.GetDueMessagesAsync(500, 5);
        Assert.Equal(messageCount, messages.Count());
    }

    [Fact]
    public async Task GetDueMessagesAsync_LargeBatch_ReturnsCorrectly()
    {
        // Arrange - Create 200 due messages
        const int dueCount = 200;
        for (int i = 0; i < dueCount; i++)
        {
            var message = new ScheduledMessage
            {
                Id = Guid.NewGuid(),
                RequestType = $"BatchCommand{i}",
                Content = "{}",
                ScheduledAtUtc = DateTime.UtcNow.AddHours(-i - 1),
                CreatedAtUtc = DateTime.UtcNow.AddHours(-i - 2),
                RetryCount = 0,
                IsRecurring = false
            };
            await _store.AddAsync(message);
        }

        // Act - Get all (batch size = 500)
        var messages = await _store.GetDueMessagesAsync(500, 5);

        // Assert
        Assert.Equal(dueCount, messages.Count());
    }

    [Fact]
    public async Task CancelAsync_LargeBatch_AllRemoved()
    {
        // Arrange - Create 150 messages
        const int messageCount = 150;
        var messageIds = new List<Guid>();
        for (int i = 0; i < messageCount; i++)
        {
            var messageId = Guid.NewGuid();
            messageIds.Add(messageId);
            var message = new ScheduledMessage
            {
                Id = messageId,
                RequestType = $"CancelCommand{i}",
                Content = "{}",
                ScheduledAtUtc = DateTime.UtcNow.AddHours(-1),
                CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
                RetryCount = 0,
                IsRecurring = false
            };
            await _store.AddAsync(message);
        }

        // Act - Cancel all
        foreach (var messageId in messageIds)
        {
            await _store.CancelAsync(messageId);
        }

        // Assert - All removed
        var messages = await _store.GetDueMessagesAsync(500, 5);
        Assert.Empty(messages);
    }

    #endregion

    #region Stress Tests

    [Fact]
    public async Task MixedOperations_HighConcurrency_NoDataCorruption()
    {
        // Arrange - Create 30 messages
        const int messageCount = 30;
        var messageIds = new List<Guid>();
        for (int i = 0; i < messageCount; i++)
        {
            var messageId = Guid.NewGuid();
            messageIds.Add(messageId);
            var message = new ScheduledMessage
            {
                Id = messageId,
                RequestType = $"StressCommand{i}",
                Content = "{}",
                ScheduledAtUtc = DateTime.UtcNow.AddHours(-1),
                CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
                RetryCount = 0,
                IsRecurring = false
            };
            await _store.AddAsync(message);
        }

        // Act - Mix of concurrent operations
        var tasks = new List<Task>();

        // 10 reads
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(async () => await _store.GetDueMessagesAsync(10, 5)));
        }

        // 10 updates (mark as processed)
        for (int i = 0; i < 10; i++)
        {
            var messageId = messageIds[i];
            tasks.Add(Task.Run(async () => await _store.MarkAsProcessedAsync(messageId)));
        }

        // 10 failures
        for (int i = 10; i < 20; i++)
        {
            var messageId = messageIds[i];
            tasks.Add(Task.Run(async () =>
                await _store.MarkAsFailedAsync(messageId, "Stress error", DateTime.UtcNow.AddHours(1))));
        }

        // 10 cancellations
        for (int i = 20; i < 30; i++)
        {
            var messageId = messageIds[i];
            tasks.Add(Task.Run(async () => await _store.CancelAsync(messageId)));
        }

        await Task.WhenAll(tasks);

        // Assert - Data integrity: 10 canceled, 10 processed, 10 failed (not due - future retry)
        var messages = await _store.GetDueMessagesAsync(100, 5);
        Assert.Empty(messages); // All processed or canceled or failed with future retry
    }

    [Fact]
    public async Task LargePayload_Processing_HandlesCorrectly()
    {
        // Arrange - Create message with 50KB content
        var messageId = Guid.NewGuid();
        var largeContent = new string('X', 50_000);
        var message = new ScheduledMessage
        {
            Id = messageId,
            RequestType = "LargePayloadCommand",
            Content = largeContent,
            ScheduledAtUtc = DateTime.UtcNow.AddHours(-1),
            CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
            RetryCount = 0,
            IsRecurring = false
        };
        await _store.AddAsync(message);

        // Act - Retrieve
        var messages = await _store.GetDueMessagesAsync(10, 5);

        // Assert - Large payload persisted correctly
        var retrieved = messages.FirstOrDefault(m => m.Id == messageId);
        Assert.NotNull(retrieved);
        Assert.Equal(50_000, retrieved.Content.Length);
    }

    [Fact]
    public async Task GetDueMessages_HighVolume_PerformanceRemainsSteady()
    {
        // Arrange - Create 1000 messages (500 due, 500 future)
        const int totalMessages = 1000;
        for (int i = 0; i < totalMessages; i++)
        {
            var message = new ScheduledMessage
            {
                Id = Guid.NewGuid(),
                RequestType = $"PerfCommand{i}",
                Content = "{}",
                ScheduledAtUtc = DateTime.UtcNow.AddHours(i < 500 ? -1 : 1),
                CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
                RetryCount = 0,
                IsRecurring = false
            };
            await _store.AddAsync(message);
        }

        // Act - Query due messages
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var messages = await _store.GetDueMessagesAsync(100, 5);
        stopwatch.Stop();

        // Assert - Performance acceptable (< 500ms for 1000 messages)
        Assert.True(stopwatch.ElapsedMilliseconds < 500, $"Query took {stopwatch.ElapsedMilliseconds}ms");
        Assert.Equal(100, messages.Count()); // Limited by batch size
    }

    #endregion

    #region Recurring Messages Under Load

    [Fact]
    public async Task RecurringMessages_ConcurrentRescheduling_MaintainConsistency()
    {
        // Arrange - Create 20 recurring messages
        const int messageCount = 20;
        var messageIds = new List<Guid>();
        for (int i = 0; i < messageCount; i++)
        {
            var messageId = Guid.NewGuid();
            messageIds.Add(messageId);
            var message = new ScheduledMessage
            {
                Id = messageId,
                RequestType = $"RecurringCommand{i}",
                Content = "{}",
                ScheduledAtUtc = DateTime.UtcNow.AddHours(-1),
                CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
                ProcessedAtUtc = DateTime.UtcNow.AddMinutes(-30),
                RetryCount = 0,
                IsRecurring = true,
                CronExpression = "0 * * * *"
            };
            await _store.AddAsync(message);
        }

        // Act - Reschedule all concurrently 3 times
        const int reschedules = 3;
        for (int round = 0; round < reschedules; round++)
        {
            var tasks = new List<Task>();
            for (int i = 0; i < messageCount; i++)
            {
                var messageId = messageIds[i];
                var nextRun = DateTime.UtcNow.AddHours(round % 2 == 0 ? -1 : 1); // Alternate due/not due
                tasks.Add(Task.Run(async () =>
                    await _store.RescheduleRecurringMessageAsync(messageId, nextRun)));
            }
            await Task.WhenAll(tasks);
        }

        // Assert - All messages still exist (final state depends on last reschedule)
        var messages = await _store.GetDueMessagesAsync(100, 5);
        Assert.Equal(messageCount, messages.Count()); // Last reschedule was to past (due)
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
            var message = new ScheduledMessage
            {
                Id = Guid.NewGuid(),
                RequestType = $"PerfCommand{i}",
                Content = "{}",
                ScheduledAtUtc = DateTime.UtcNow.AddHours(-1),
                CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
                RetryCount = 0,
                IsRecurring = false
            };
            await _store.AddAsync(message);
        }

        stopwatch.Stop();
        return stopwatch.Elapsed;
    }

    #endregion
}
