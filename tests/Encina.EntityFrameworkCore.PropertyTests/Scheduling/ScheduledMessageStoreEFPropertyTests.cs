using System.Diagnostics.CodeAnalysis;
using Encina.EntityFrameworkCore.Scheduling;
using Encina.Messaging.Scheduling;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Encina.EntityFrameworkCore.PropertyTests.Scheduling;

/// <summary>
/// Property-based tests for <see cref="ScheduledMessageStoreEF"/>.
/// Verifies invariants that MUST hold for ALL possible inputs.
/// </summary>
[Trait("Category", "Property")]
[SuppressMessage("Usage", "CA1001:Types that own disposable fields should be disposable", Justification = "IAsyncLifetime handles disposal via DisposeAsync")]
public sealed class ScheduledMessageStoreEFPropertyTests : IAsyncLifetime
{
    private TestDbContext? _dbContext;
    private ScheduledMessageStoreEF? _store;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase($"SchedulingPropertyTests_{Guid.NewGuid()}")
            .Options;

        _dbContext = new TestDbContext(options);
        _store = new ScheduledMessageStoreEF(_dbContext);

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
    /// Property: Messages scheduled in the past ALWAYS returned by GetDueMessagesAsync.
    /// </summary>
    [Fact]
    public async Task Property_AddThenGetDue_PastScheduledAlwaysReturned()
    {
        var now = DateTime.UtcNow;

        var testCases = new[]
        {
            (ScheduledAt: now.AddHours(-2), ShouldAppear: true),   // 2h ago
            (ScheduledAt: now.AddMinutes(-1), ShouldAppear: true), // 1min ago
            (ScheduledAt: now, ShouldAppear: true),                // Now (inclusive)
            (ScheduledAt: now.AddSeconds(1), ShouldAppear: false), // 1s future
            (ScheduledAt: now.AddHours(1), ShouldAppear: false)    // 1h future
        };

        foreach (var (scheduledAt, shouldAppear) in testCases)
        {
            var message = new ScheduledMessage
            {
                Id = Guid.NewGuid(),
                RequestType = "TestScheduling",
                Content = "{}",
                ScheduledAtUtc = scheduledAt,
                CreatedAtUtc = now.AddHours(-3),
                RetryCount = 0
            };

            await _store!.AddAsync(message);
            await _store.SaveChangesAsync();

            // Act
            var due = await _store.GetDueMessagesAsync(100, 5);

            // Assert
            if (shouldAppear)
            {
                due.ShouldContain(m => m.Id == message.Id,
                    $"message scheduled at {scheduledAt} should be in due list");
            }
            else
            {
                due.ShouldNotContain(m => m.Id == message.Id,
                    $"message scheduled in future {scheduledAt} must NOT be in due list");
            }

            await ClearDatabase();
        }
    }

    /// <summary>
    /// Property: Messages scheduled in the future NEVER returned until due.
    /// </summary>
    [Fact]
    public async Task Property_FutureScheduling_NeverReturnedEarly()
    {
        var now = DateTime.UtcNow;

        // Create messages scheduled at various future times
        var messages = Enumerable.Range(1, 20)
            .Select(i => new ScheduledMessage
            {
                Id = Guid.NewGuid(),
                RequestType = $"FutureTest_{i}",
                Content = $"{{\"index\":{i}}}",
                ScheduledAtUtc = now.AddMinutes(i),  // All in future
                CreatedAtUtc = now,
                RetryCount = 0
            })
            .ToList();

        foreach (var message in messages)
        {
            await _store!.AddAsync(message);
        }
        await _store!.SaveChangesAsync();

        // Act
        var due = await _store!.GetDueMessagesAsync(100, 5);

        // Assert - NONE should be due yet
        due.ShouldBeEmpty("messages scheduled in the future must NEVER be returned early");
    }

    /// <summary>
    /// Property: Rescheduled recurring message ALWAYS has updated ScheduledAtUtc.
    /// </summary>
    [Fact]
    public async Task Property_Rescheduling_AlwaysUpdatesScheduledAtUtc()
    {
        var testCases = Enumerable.Range(1, 15).Select(i => new
        {
            OriginalSchedule = DateTime.UtcNow.AddHours(-i),
            NewSchedule = DateTime.UtcNow.AddHours(i)
        }).ToList();

        foreach (var testCase in testCases)
        {
            await ClearDatabase();

            // Arrange - create and process a recurring message
            var message = new ScheduledMessage
            {
                Id = Guid.NewGuid(),
                RequestType = "RecurringTest",
                Content = "{}",
                ScheduledAtUtc = testCase.OriginalSchedule,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-1),
                IsRecurring = true,
                CronExpression = "0 0 * * *",
                RetryCount = 0
            };

            await _store!.AddAsync(message);
            await _store.SaveChangesAsync();

            // Mark as processed
            await _store.MarkAsProcessedAsync(message.Id);
            await _store.SaveChangesAsync();

            // Act - reschedule
            await _store.RescheduleRecurringMessageAsync(message.Id, testCase.NewSchedule);
            await _store.SaveChangesAsync();

            // Assert
            var rescheduled = _dbContext!.Set<ScheduledMessage>().First(m => m.Id == message.Id);
            rescheduled.ScheduledAtUtc.ShouldBe(testCase.NewSchedule,
                "rescheduled message must ALWAYS have updated ScheduledAtUtc");
            rescheduled.ProcessedAtUtc.ShouldBeNull(
                "rescheduled message must have ProcessedAtUtc cleared");
            rescheduled.RetryCount.ShouldBe(0,
                "rescheduled message must have RetryCount reset");
        }
    }

    /// <summary>
    /// Property: GetDueMessages ALWAYS respects batch size.
    /// </summary>
    [Fact]
    public async Task Property_BatchSize_AlwaysRespectsLimit()
    {
        var batchSizes = new[] { 1, 5, 10, 25, 50 };

        foreach (var batchSize in batchSizes)
        {
            await ClearDatabase();

            // Create more due messages than batch size
            var messageCount = batchSize + Random.Shared.Next(5, 20);
            var pastTime = DateTime.UtcNow.AddHours(-1);

            for (int i = 0; i < messageCount; i++)
            {
                await _store!.AddAsync(new ScheduledMessage
                {
                    Id = Guid.NewGuid(),
                    RequestType = "BatchTest",
                    Content = $"{{\"index\":{i}}}",
                    ScheduledAtUtc = pastTime,
                    CreatedAtUtc = pastTime,
                    RetryCount = 0
                });
            }
            await _store!.SaveChangesAsync();

            // Act
            var due = await _store!.GetDueMessagesAsync(batchSize, 5);

            // Assert
            due.Count().ShouldBeLessThanOrEqualTo(batchSize,
                $"batch size {batchSize} must ALWAYS be respected");
        }
    }

    /// <summary>
    /// Property: Due messages ALWAYS ordered by ScheduledAtUtc ascending.
    /// </summary>
    [Fact]
    public async Task Property_Ordering_DueOrderedByScheduledAtUtc()
    {
        var baseTime = DateTime.UtcNow.AddHours(-10);

        // Create due messages with random scheduled times
        var messages = Enumerable.Range(0, 20)
            .Select(i => new ScheduledMessage
            {
                Id = Guid.NewGuid(),
                RequestType = "OrderTest",
                Content = $"{{\"index\":{i}}}",
                ScheduledAtUtc = baseTime.AddMinutes(Random.Shared.Next(-100, 0)),
                CreatedAtUtc = baseTime.AddDays(-1),
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
        var due = (await _store!.GetDueMessagesAsync(100, 5)).ToList();

        // Assert - must be ordered by ScheduledAtUtc ascending
        for (int i = 1; i < due.Count; i++)
        {
            due[i].ScheduledAtUtc.ShouldBeGreaterThanOrEqualTo(due[i - 1].ScheduledAtUtc,
                "due messages must ALWAYS be ordered by ScheduledAtUtc ascending");
        }
    }

    /// <summary>
    /// Property: Messages with RetryCount >= maxRetries NEVER appear in due.
    /// </summary>
    [Fact]
    public async Task Property_RetryExhaustion_ExcludedFromDue()
    {
        const int maxRetries = 3;
        var pastTime = DateTime.UtcNow.AddHours(-1);

        var testCases = new[]
        {
            (RetryCount: 0, ShouldAppear: true),
            (RetryCount: 1, ShouldAppear: true),
            (RetryCount: 2, ShouldAppear: true),
            (RetryCount: 3, ShouldAppear: false),  // Exhausted
            (RetryCount: 5, ShouldAppear: false),  // Exhausted
            (RetryCount: 10, ShouldAppear: false)  // Exhausted
        };

        foreach (var (retryCount, shouldAppear) in testCases)
        {
            var message = new ScheduledMessage
            {
                Id = Guid.NewGuid(),
                RequestType = $"RetryTest_{retryCount}",
                Content = "{}",
                ScheduledAtUtc = pastTime,
                CreatedAtUtc = pastTime,
                RetryCount = retryCount
            };

            await _store!.AddAsync(message);
            await _store.SaveChangesAsync();

            // Act
            var due = await _store.GetDueMessagesAsync(100, maxRetries);

            // Assert
            if (shouldAppear)
            {
                due.ShouldContain(m => m.Id == message.Id,
                    $"message with {retryCount} retries < {maxRetries} must appear");
            }
            else
            {
                due.ShouldNotContain(m => m.Id == message.Id,
                    $"message with {retryCount} retries >= {maxRetries} must NEVER appear");
            }

            await ClearDatabase();
        }
    }

    /// <summary>
    /// Property: Messages with NextRetryAtUtc in future NEVER appear in due.
    /// </summary>
    [Fact]
    public async Task Property_NextRetry_FutureRetriesExcluded()
    {
        var now = DateTime.UtcNow;
        var pastSchedule = now.AddHours(-2);

        var testCases = new[]
        {
            (NextRetry: (DateTime?)null, ShouldAppear: true),
            (NextRetry: (DateTime?)now.AddMinutes(-5), ShouldAppear: true),
            (NextRetry: (DateTime?)now, ShouldAppear: true),
            (NextRetry: (DateTime?)now.AddMinutes(5), ShouldAppear: false),
            (NextRetry: (DateTime?)now.AddHours(1), ShouldAppear: false)
        };

        foreach (var (nextRetry, shouldAppear) in testCases)
        {
            var message = new ScheduledMessage
            {
                Id = Guid.NewGuid(),
                RequestType = "NextRetryTest",
                Content = "{}",
                ScheduledAtUtc = pastSchedule,
                CreatedAtUtc = pastSchedule,
                RetryCount = 1,
                NextRetryAtUtc = nextRetry
            };

            await _store!.AddAsync(message);
            await _store.SaveChangesAsync();

            // Act
            var due = await _store.GetDueMessagesAsync(100, 5);

            // Assert
            if (shouldAppear)
            {
                due.ShouldContain(m => m.Id == message.Id,
                    $"message with NextRetryAtUtc={nextRetry} should appear");
            }
            else
            {
                due.ShouldNotContain(m => m.Id == message.Id,
                    $"message with future NextRetryAtUtc={nextRetry} must NOT appear");
            }

            await ClearDatabase();
        }
    }

    /// <summary>
    /// Property: MarkAsProcessed ALWAYS sets both ProcessedAtUtc and LastExecutedAtUtc.
    /// </summary>
    [Fact]
    public async Task Property_MarkAsProcessed_SetsBothTimestamps()
    {
        var testCases = Enumerable.Range(0, 10).Select(i => Guid.NewGuid()).ToList();

        foreach (var messageId in testCases)
        {
            await ClearDatabase();

            // Arrange
            var message = new ScheduledMessage
            {
                Id = messageId,
                RequestType = "ProcessedTest",
                Content = "{}",
                ScheduledAtUtc = DateTime.UtcNow.AddHours(-1),
                CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
                RetryCount = 0
            };

            await _store!.AddAsync(message);
            await _store.SaveChangesAsync();

            // Act
            await _store.MarkAsProcessedAsync(messageId);
            await _store.SaveChangesAsync();

            // Assert
            var processed = _dbContext!.Set<ScheduledMessage>().First(m => m.Id == messageId);
            processed.ProcessedAtUtc.ShouldNotBeNull(
                "MarkAsProcessed must ALWAYS set ProcessedAtUtc");
            processed.LastExecutedAtUtc.ShouldNotBeNull(
                "MarkAsProcessed must ALWAYS set LastExecutedAtUtc");
            processed.ErrorMessage.ShouldBeNull(
                "MarkAsProcessed must ALWAYS clear ErrorMessage");
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
            await ClearDatabase();

            // Arrange
            var message = new ScheduledMessage
            {
                Id = Guid.NewGuid(),
                RequestType = "FailTest",
                Content = "{}",
                ScheduledAtUtc = DateTime.UtcNow.AddHours(-1),
                CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
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
            var failed = _dbContext!.Set<ScheduledMessage>().First(m => m.Id == message.Id);
            failed.RetryCount.ShouldBe(testCase.InitialRetryCount + 1,
                "MarkAsFailed must ALWAYS increment RetryCount by exactly 1");
            failed.ErrorMessage.ShouldBe(testCase.ErrorMessage);
            failed.NextRetryAtUtc.ShouldNotBeNull();
        }
    }

    /// <summary>
    /// Property: CancelAsync ALWAYS removes the message.
    /// </summary>
    [Fact]
    public async Task Property_Cancel_AlwaysRemovesMessage()
    {
        var testCases = Enumerable.Range(0, 15)
            .Select(_ => Guid.NewGuid())
            .ToList();

        foreach (var messageId in testCases)
        {
            await ClearDatabase();

            // Arrange
            var message = new ScheduledMessage
            {
                Id = messageId,
                RequestType = "CancelTest",
                Content = "{}",
                ScheduledAtUtc = DateTime.UtcNow.AddHours(1),
                CreatedAtUtc = DateTime.UtcNow,
                RetryCount = 0
            };

            await _store!.AddAsync(message);
            await _store.SaveChangesAsync();

            // Verify it exists
            var exists = _dbContext!.Set<ScheduledMessage>().Any(m => m.Id == messageId);
            exists.ShouldBeTrue("message should exist before cancellation");

            // Act
            await _store.CancelAsync(messageId);
            await _store.SaveChangesAsync();

            // Assert
            var stillExists = _dbContext.Set<ScheduledMessage>().Any(m => m.Id == messageId);
            stillExists.ShouldBeFalse("CancelAsync must ALWAYS remove the message completely");
        }
    }

    /// <summary>
    /// Property: IsDue() ALWAYS correctly identifies due messages.
    /// </summary>
    [Fact]
    public async Task Property_IsDue_CorrectlyIdentifies()
    {
        var now = DateTime.UtcNow;

        var testCases = new[]
        {
            (ScheduledAt: now.AddHours(-1), Processed: false, Expected: true),
            (ScheduledAt: now, Processed: false, Expected: true),
            (ScheduledAt: now.AddSeconds(1), Processed: false, Expected: false),
            (ScheduledAt: now.AddHours(-1), Processed: true, Expected: false)
        };

        foreach (var (scheduledAt, processed, expected) in testCases)
        {
            var message = new ScheduledMessage
            {
                Id = Guid.NewGuid(),
                RequestType = "IsDueTest",
                Content = "{}",
                ScheduledAtUtc = scheduledAt,
                CreatedAtUtc = now.AddHours(-2),
                ProcessedAtUtc = processed ? DateTime.UtcNow : null,
                RetryCount = 0
            };

            var isDue = message.IsDue();
            isDue.ShouldBe(expected,
                $"message with ScheduledAt={scheduledAt}, Processed={processed} should have IsDue={expected}");
        }
    }

    /// <summary>
    /// Property: IsDeadLettered ALWAYS reflects retry exhaustion.
    /// </summary>
    [Fact]
    public async Task Property_IsDeadLettered_ReflectsRetryExhaustion()
    {
        const int maxRetries = 3;

        var testCases = new[]
        {
            (RetryCount: 0, Processed: false, Expected: false),
            (RetryCount: 2, Processed: false, Expected: false),
            (RetryCount: 3, Processed: false, Expected: true),
            (RetryCount: 5, Processed: false, Expected: true),
            (RetryCount: 3, Processed: true, Expected: false)  // Processed = not dead lettered
        };

        foreach (var (retryCount, processed, expected) in testCases)
        {
            var message = new ScheduledMessage
            {
                Id = Guid.NewGuid(),
                RequestType = "DeadLetterTest",
                Content = "{}",
                ScheduledAtUtc = DateTime.UtcNow.AddHours(-1),
                CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
                RetryCount = retryCount,
                ProcessedAtUtc = processed ? DateTime.UtcNow : null
            };

            var isDeadLettered = message.IsDeadLettered(maxRetries);
            isDeadLettered.ShouldBe(expected,
                $"message with RetryCount={retryCount}, Processed={processed} should have IsDeadLettered={expected}");
        }
    }

    /// <summary>
    /// Property: Recurring flag ALWAYS preserved across operations.
    /// </summary>
    [Fact]
    public async Task Property_Recurring_AlwaysPreserved()
    {
        var message = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "RecurringPreserveTest",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddHours(-1),
            CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
            IsRecurring = true,
            CronExpression = "0 0 * * *",
            RetryCount = 0
        };

        await _store!.AddAsync(message);
        await _store.SaveChangesAsync();

        // Mark as failed
        await _store.MarkAsFailedAsync(message.Id, "Test error", DateTime.UtcNow.AddMinutes(5));
        await _store.SaveChangesAsync();

        var afterFail = _dbContext!.Set<ScheduledMessage>().First(m => m.Id == message.Id);
        afterFail.IsRecurring.ShouldBeTrue("IsRecurring must be preserved after MarkAsFailed");
        afterFail.CronExpression.ShouldBe("0 0 * * *");

        // Reschedule
        await _store.RescheduleRecurringMessageAsync(message.Id, DateTime.UtcNow.AddHours(1));
        await _store.SaveChangesAsync();

        var afterReschedule = _dbContext.Set<ScheduledMessage>().First(m => m.Id == message.Id);
        afterReschedule.IsRecurring.ShouldBeTrue("IsRecurring must be preserved after reschedule");
        afterReschedule.CronExpression.ShouldBe("0 0 * * *");
    }

    /// <summary>
    /// Property: CorrelationId ALWAYS preserved across all operations.
    /// </summary>
    [Fact]
    public async Task Property_CorrelationId_AlwaysPreserved()
    {
        var correlationId = Guid.NewGuid().ToString();

        var message = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "CorrelationTest",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddHours(-1),
            CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
            CorrelationId = correlationId,
            RetryCount = 0
        };

        await _store!.AddAsync(message);
        await _store.SaveChangesAsync();

        // Mark as failed
        await _store.MarkAsFailedAsync(message.Id, "Error", DateTime.UtcNow.AddMinutes(5));
        await _store.SaveChangesAsync();

        var retrieved = _dbContext!.Set<ScheduledMessage>().First(m => m.Id == message.Id);
        retrieved.CorrelationId.ShouldBe(correlationId,
            "CorrelationId must ALWAYS be preserved");
    }

    /// <summary>
    /// Property: Concurrent adds ALWAYS succeed without corruption.
    /// </summary>
    [Fact]
    public async Task Property_ConcurrentAdds_AlwaysSucceed()
    {
        const int concurrentWrites = 50;
        var now = DateTime.UtcNow;

        var messages = Enumerable.Range(0, concurrentWrites)
            .Select(i => new ScheduledMessage
            {
                Id = Guid.NewGuid(),
                RequestType = $"ConcurrentTest_{i}",
                Content = $"{{\"index\":{i}}}",
                ScheduledAtUtc = now.AddMinutes(Random.Shared.Next(-30, 30)),
                CreatedAtUtc = now,
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
        var allMessages = _dbContext!.Set<ScheduledMessage>().ToList();
        allMessages.Count.ShouldBe(concurrentWrites,
            "ALL concurrent adds must succeed");

        foreach (var original in messages)
        {
            allMessages.ShouldContain(m => m.Id == original.Id,
                "every added message must be retrievable");
        }
    }

    private async Task ClearDatabase()
    {
        var allMessages = await _dbContext!.Set<ScheduledMessage>().ToListAsync();
        _dbContext.Set<ScheduledMessage>().RemoveRange(allMessages);
        await _dbContext.SaveChangesAsync();
    }

    private sealed class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions options) : base(options) { }

        public DbSet<ScheduledMessage> ScheduledMessages => Set<ScheduledMessage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ScheduledMessage>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.RequestType).IsRequired();
                entity.Property(e => e.Content).IsRequired();
            });
        }
    }
}
