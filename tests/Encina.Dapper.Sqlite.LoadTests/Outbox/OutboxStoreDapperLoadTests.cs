using System.Collections.Concurrent;
using System.Diagnostics;
using Encina.Dapper.Sqlite.Outbox;
using Encina.TestInfrastructure.Extensions;
using Encina.TestInfrastructure.Fixtures;
using Xunit.Abstractions;

namespace Encina.Dapper.Sqlite.Tests.Outbox;

/// <summary>
/// Load tests for <see cref="OutboxStoreDapper"/>.
/// Verifies behavior under high concurrency, race conditions, and volume.
/// </summary>
[Trait("Category", "Load")]
[Trait("TestType", "Load")]
[Trait("Provider", "Dapper.Sqlite")]
public sealed class OutboxStoreDapperLoadTests : IClassFixture<SqliteFixture>
{
    private readonly ITestOutputHelper _output;
    private readonly SqliteFixture _fixture;
    private readonly OutboxStoreDapper _store;

    public OutboxStoreDapperLoadTests(SqliteFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
        DapperTypeHandlers.RegisterSqliteHandlers();

        // Clear all data before each test to ensure clean state
        _fixture.ClearAllDataAsync().GetAwaiter().GetResult();

        _store = new OutboxStoreDapper(_fixture.CreateConnection());
    }

    #region Concurrent Write Tests

    /// <summary>
    /// Load test: 100 concurrent writes should all succeed without data corruption.
    /// </summary>
    [Fact]
    public async Task ConcurrentWrites_100Messages_AllSucceed()
    {
        // Arrange
        var messageCount = 100;
        var messages = Enumerable.Range(0, messageCount)
            .Select(i => new OutboxMessage
            {
                Id = Guid.NewGuid(),
                NotificationType = $"ConcurrentEvent{i}",
                Content = $"{{\"index\":{i}}}",
                CreatedAtUtc = DateTime.UtcNow
            })
            .ToList();

        var sw = Stopwatch.StartNew();

        // Act - Write all messages concurrently
        var tasks = messages.Select(m => _store.AddAsync(m));
        await Task.WhenAll(tasks);

        sw.Stop();

        // Assert
        var pending = await _store.GetPendingMessagesAsync(messageCount + 10, 10);
        Assert.Equal(messageCount, pending.Count());

        _output.WriteLine($"✅ {messageCount} concurrent writes completed in {sw.ElapsedMilliseconds}ms ({messageCount * 1000.0 / sw.ElapsedMilliseconds:F2} ops/sec)");
    }

    /// <summary>
    /// Load test: 500 concurrent writes with high contention.
    /// </summary>
    [Fact]
    public async Task ConcurrentWrites_500Messages_HighContention()
    {
        // Arrange
        var messageCount = 500;
        var sw = Stopwatch.StartNew();

        // Act - Maximum concurrency
        var tasks = Enumerable.Range(0, messageCount)
            .Select(async i =>
            {
                await _store.AddAsync(new OutboxMessage
                {
                    Id = Guid.NewGuid(),
                    NotificationType = $"HighContentionEvent{i}",
                    Content = "{}",
                    CreatedAtUtc = DateTime.UtcNow
                });
            });

        await Task.WhenAll(tasks);
        sw.Stop();

        // Assert
        var pending = await _store.GetPendingMessagesAsync(messageCount + 10, 10);
        Assert.Equal(messageCount, pending.Count());

        _output.WriteLine($"✅ {messageCount} high-contention writes completed in {sw.ElapsedMilliseconds}ms ({messageCount * 1000.0 / sw.ElapsedMilliseconds:F2} ops/sec)");
    }

    #endregion

    #region Concurrent Read Tests

    /// <summary>
    /// Load test: 100 concurrent reads should all return consistent data.
    /// </summary>
    [Fact]
    public async Task ConcurrentReads_100Threads_ConsistentResults()
    {
        // Arrange - Add test data
        var messageCount = 50;
        for (var i = 0; i < messageCount; i++)
        {
            await _store.AddAsync(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                NotificationType = $"Event{i}",
                Content = "{}",
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        var readCount = 100;
        var results = new ConcurrentBag<int>();
        var sw = Stopwatch.StartNew();

        // Act - Read concurrently
        var tasks = Enumerable.Range(0, readCount)
            .Select(async _ =>
            {
                var pending = await _store.GetPendingMessagesAsync(100, 10);
                results.Add(pending.Count());
            });

        await Task.WhenAll(tasks);
        sw.Stop();

        // Assert - All reads should return same count
        Assert.All(results, count => Assert.Equal(messageCount, count));

        _output.WriteLine($"✅ {readCount} concurrent reads completed in {sw.ElapsedMilliseconds}ms ({readCount * 1000.0 / sw.ElapsedMilliseconds:F2} ops/sec)");
    }

    #endregion

    #region Concurrent Update Tests

    /// <summary>
    /// Load test: Concurrent updates (mark as processed) should not interfere with each other.
    /// </summary>
    [Fact]
    public async Task ConcurrentUpdates_MarkAsProcessed_NoInterference()
    {
        // Arrange - Add messages
        var messageCount = 100;
        var messageIds = new List<Guid>();

        for (var i = 0; i < messageCount; i++)
        {
            var id = Guid.NewGuid();
            messageIds.Add(id);
            await _store.AddAsync(new OutboxMessage
            {
                Id = id,
                NotificationType = $"Event{i}",
                Content = "{}",
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        var sw = Stopwatch.StartNew();

        // Act - Mark all as processed concurrently
        var tasks = messageIds.Select(id => _store.MarkAsProcessedAsync(id));
        await Task.WhenAll(tasks);

        sw.Stop();

        // Assert - All should be processed
        var pending = await _store.GetPendingMessagesAsync(1000, 10);
        Assert.Empty(pending);

        _output.WriteLine($"✅ {messageCount} concurrent updates completed in {sw.ElapsedMilliseconds}ms ({messageCount * 1000.0 / sw.ElapsedMilliseconds:F2} ops/sec)");
    }

    /// <summary>
    /// Load test: Concurrent failures should increment retry count correctly.
    /// </summary>
    [Fact]
    public async Task ConcurrentUpdates_MarkAsFailed_CorrectRetryCount()
    {
        // Arrange - Add one message
        var messageId = Guid.NewGuid();
        await _store.AddAsync(new OutboxMessage
        {
            Id = messageId,
            NotificationType = "TestEvent",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 0
        });

        var failureCount = 10;
        var sw = Stopwatch.StartNew();

        // Act - Mark as failed concurrently (race condition test)
        var tasks = Enumerable.Range(0, failureCount)
            .Select(i => _store.MarkAsFailedAsync(messageId, $"Error {i}", null));

        await Task.WhenAll(tasks);
        sw.Stop();

        // Assert - RetryCount should be incremented 10 times (but due to race conditions, might vary)
        var pending = await _store.GetPendingMessagesAsync(100, 100);
        var message = pending.First(m => m.Id == messageId);

        // SQLite handles concurrency with serialization, so all 10 should succeed
        Assert.Equal(failureCount, message.RetryCount);

        _output.WriteLine($"✅ {failureCount} concurrent failures completed in {sw.ElapsedMilliseconds}ms - Final RetryCount: {message.RetryCount}");
    }

    #endregion

    #region Mixed Operation Tests

    /// <summary>
    /// Load test: Mix of writes, reads, and updates under high concurrency.
    /// </summary>
    [Fact]
    public async Task MixedOperations_HighConcurrency_AllSucceed()
    {
        // Arrange
        var writeCount = 50;
        var readCount = 50;
        var updateCount = 25;

        // Pre-populate some data for updates
        var existingIds = new List<Guid>();
        for (var i = 0; i < updateCount; i++)
        {
            var id = Guid.NewGuid();
            existingIds.Add(id);
            await _store.AddAsync(new OutboxMessage
            {
                Id = id,
                NotificationType = $"ExistingEvent{i}",
                Content = "{}",
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        var sw = Stopwatch.StartNew();

        // Act - Mix operations concurrently
        var writeTasks = Enumerable.Range(0, writeCount).Select(async i =>
        {
            await _store.AddAsync(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                NotificationType = $"NewEvent{i}",
                Content = "{}",
                CreatedAtUtc = DateTime.UtcNow
            });
        });

        var readTasks = Enumerable.Range(0, readCount).Select(async _ =>
        {
            await _store.GetPendingMessagesAsync(100, 10);
        });

        var updateTasks = existingIds.Select(id => _store.MarkAsProcessedAsync(id));

        await Task.WhenAll(writeTasks.Concat(readTasks).Concat(updateTasks));
        sw.Stop();

        // Assert
        var pending = await _store.GetPendingMessagesAsync(1000, 10);
        Assert.Equal(writeCount, pending.Count()); // Only new writes are pending (existing ones were marked as processed)

        var totalOps = writeCount + readCount + updateCount;
        _output.WriteLine($"✅ {totalOps} mixed operations ({writeCount}W + {readCount}R + {updateCount}U) completed in {sw.ElapsedMilliseconds}ms ({totalOps * 1000.0 / sw.ElapsedMilliseconds:F2} ops/sec)");
    }

    #endregion

    #region Volume Tests

    /// <summary>
    /// Load test: Handle 1000 messages efficiently.
    /// </summary>
    [Fact]
    public async Task VolumeTest_1000Messages_EfficientProcessing()
    {
        // Arrange
        var messageCount = 1000;
        var sw = Stopwatch.StartNew();

        // Act - Add messages
        for (var i = 0; i < messageCount; i++)
        {
            await _store.AddAsync(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                NotificationType = $"VolumeEvent{i}",
                Content = $"{{\"index\":{i}}}",
                CreatedAtUtc = DateTime.UtcNow.AddSeconds(i)
            });
        }

        var insertTime = sw.ElapsedMilliseconds;

        // Read in batches
        sw.Restart();
        var batch1 = await _store.GetPendingMessagesAsync(100, 10);
        var batch2 = await _store.GetPendingMessagesAsync(500, 10);
        var batch3 = await _store.GetPendingMessagesAsync(1000, 10);
        var readTime = sw.ElapsedMilliseconds;

        sw.Stop();

        // Assert
        Assert.Equal(100, batch1.Count());
        Assert.Equal(500, batch2.Count());
        Assert.Equal(1000, batch3.Count());

        _output.WriteLine($"✅ Volume test: {messageCount} messages");
        _output.WriteLine($"   Insert: {insertTime}ms ({messageCount * 1000.0 / insertTime:F2} ops/sec)");
        _output.WriteLine($"   Read batches: {readTime}ms");
    }

    /// <summary>
    /// Load test: Batch processing with pagination.
    /// </summary>
    [Fact]
    public async Task VolumeTest_BatchProcessing_WithPagination()
    {
        // Arrange - Add 500 messages
        var totalMessages = 500;
        for (var i = 0; i < totalMessages; i++)
        {
            await _store.AddAsync(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                NotificationType = $"BatchEvent{i}",
                Content = "{}",
                CreatedAtUtc = DateTime.UtcNow.AddSeconds(i)
            });
        }

        // Act - Process in batches of 50
        var batchSize = 50;
        var processedCount = 0;
        var batchCount = 0;
        var sw = Stopwatch.StartNew();

        while (processedCount < totalMessages)
        {
            var batch = await _store.GetPendingMessagesAsync(batchSize, 10);
            if (!batch.Any()) break;

            // Mark batch as processed
            var tasks = batch.Select(m => _store.MarkAsProcessedAsync(m.Id));
            await Task.WhenAll(tasks);

            processedCount += batch.Count();
            batchCount++;
        }

        sw.Stop();

        // Assert
        Assert.Equal(totalMessages, processedCount);
        Assert.Equal(totalMessages / batchSize, batchCount);

        var remaining = await _store.GetPendingMessagesAsync(1000, 10);
        Assert.Empty(remaining);

        _output.WriteLine($"✅ Batch processing: {totalMessages} messages in {batchCount} batches of {batchSize}");
        _output.WriteLine($"   Total time: {sw.ElapsedMilliseconds}ms ({totalMessages * 1000.0 / sw.ElapsedMilliseconds:F2} ops/sec)");
    }

    #endregion

    #region Stress Tests

    /// <summary>
    /// Stress test: Rapid succession of operations.
    /// </summary>
    [Fact]
    public async Task StressTest_RapidOperations_NoErrors()
    {
        // Arrange
        var iterations = 100;
        var sw = Stopwatch.StartNew();
        var errors = new ConcurrentBag<Exception>();

        // Act - Rapid fire operations
        var tasks = Enumerable.Range(0, iterations).Select(async i =>
        {
            try
            {
                var id = Guid.NewGuid();

                // Add
                await _store.AddAsync(new OutboxMessage
                {
                    Id = id,
                    NotificationType = $"StressEvent{i}",
                    Content = "{}",
                    CreatedAtUtc = DateTime.UtcNow
                });

                // Read
                await _store.GetPendingMessagesAsync(10, 10);

                // Update
                if (i % 2 == 0)
                {
                    await _store.MarkAsProcessedAsync(id);
                }
                else
                {
                    await _store.MarkAsFailedAsync(id, "Stress test error", null);
                }

                // Read again
                await _store.GetPendingMessagesAsync(10, 10);
            }
            catch (Exception ex)
            {
                errors.Add(ex);
            }
        });

        await Task.WhenAll(tasks);
        sw.Stop();

        // Assert - No errors
        Assert.Empty(errors);

        var totalOps = iterations * 4; // Add + 2 reads + update per iteration
        _output.WriteLine($"✅ Stress test: {totalOps} rapid operations in {sw.ElapsedMilliseconds}ms ({totalOps * 1000.0 / sw.ElapsedMilliseconds:F2} ops/sec)");
        _output.WriteLine($"   Errors: {errors.Count}");
    }

    #endregion

    #region Performance Baseline Tests

    /// <summary>
    /// Performance baseline: Measure average operation times.
    /// </summary>
    [Fact]
    public async Task PerformanceBaseline_MeasureAverageTimes()
    {
        // Arrange
        var iterations = 100;

        // Measure AddAsync
        var addTimes = new List<long>();
        for (var i = 0; i < iterations; i++)
        {
            var sw = Stopwatch.StartNew();
            await _store.AddAsync(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                NotificationType = "PerfTest",
                Content = "{}",
                CreatedAtUtc = DateTime.UtcNow
            });
            sw.Stop();
            addTimes.Add(sw.ElapsedTicks);
        }

        // Measure GetPendingMessagesAsync
        var getTimes = new List<long>();
        for (var i = 0; i < iterations; i++)
        {
            var sw = Stopwatch.StartNew();
            await _store.GetPendingMessagesAsync(10, 10);
            sw.Stop();
            getTimes.Add(sw.ElapsedTicks);
        }

        // Calculate averages
        var avgAddMs = addTimes.Average() / TimeSpan.TicksPerMillisecond;
        var avgGetMs = getTimes.Average() / TimeSpan.TicksPerMillisecond;

        _output.WriteLine($"📊 Performance Baseline ({iterations} iterations):");
        _output.WriteLine($"   AddAsync: {avgAddMs:F3}ms average");
        _output.WriteLine($"   GetPendingMessagesAsync: {avgGetMs:F3}ms average");

        // Assert - Reasonable performance (< 10ms per operation on average)
        Assert.True(avgAddMs < 10, $"AddAsync too slow: {avgAddMs}ms average");
        Assert.True(avgGetMs < 10, $"GetPendingMessagesAsync too slow: {avgGetMs}ms average");
    }

    #endregion

    #region Retry Workflow Load Test

    /// <summary>
    /// Load test: Realistic retry workflow under load.
    /// </summary>
    [Fact]
    public async Task RetryWorkflow_HighVolume_CorrectBehavior()
    {
        // Arrange - Add 200 messages
        var messageCount = 200;
        var messageIds = new List<Guid>();

        for (var i = 0; i < messageCount; i++)
        {
            var id = Guid.NewGuid();
            messageIds.Add(id);
            await _store.AddAsync(new OutboxMessage
            {
                Id = id,
                NotificationType = $"RetryEvent{i}",
                Content = "{}",
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        var sw = Stopwatch.StartNew();

        // Act - Simulate retry workflow
        // 50% succeed immediately, 30% fail once then succeed, 20% fail twice then succeed
        var tasks = messageIds.Select(async (id, index) =>
        {
            if (index % 2 == 0)
            {
                // Succeed immediately
                await _store.MarkAsProcessedAsync(id);
            }
            else if (index % 5 != 0)
            {
                // Fail once, then succeed
                await _store.MarkAsFailedAsync(id, "Transient error", null);
                await _store.MarkAsProcessedAsync(id);
            }
            else
            {
                // Fail twice, then succeed
                await _store.MarkAsFailedAsync(id, "Error 1", null);
                await _store.MarkAsFailedAsync(id, "Error 2", null);
                await _store.MarkAsProcessedAsync(id);
            }
        });

        await Task.WhenAll(tasks);
        sw.Stop();

        // Assert - All processed
        var pending = await _store.GetPendingMessagesAsync(1000, 10);
        Assert.Empty(pending);

        _output.WriteLine($"✅ Retry workflow: {messageCount} messages with mixed retry patterns");
        _output.WriteLine($"   Completed in {sw.ElapsedMilliseconds}ms ({messageCount * 1000.0 / sw.ElapsedMilliseconds:F2} messages/sec)");
    }

    #endregion
}
