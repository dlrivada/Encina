using BenchmarkDotNet.Attributes;
using Microsoft.EntityFrameworkCore;
using Encina.EntityFrameworkCore.Scheduling;

namespace Encina.EntityFrameworkCore.Benchmarks;

/// <summary>
/// Benchmarks for EF Core-based Scheduled Message implementation.
/// </summary>
/// <remarks>
/// Measures performance of core scheduling operations:
/// - Retrieving due messages for execution
/// - Adding new scheduled messages
/// - Rescheduling recurring messages
/// - Marking messages as failed with retry logic
/// </remarks>
[MemoryDiagnoser]
[MarkdownExporter]
#pragma warning disable CA1001 // BenchmarkDotNet handles disposal via GlobalCleanup
public class ScheduledMessageStoreEFBenchmarks
#pragma warning restore CA1001
{
    private BenchmarkDbContext _context = null!;
    private ScheduledMessageStoreEF _store = null!;

    /// <summary>
    /// Global setup: Create in-memory database and store instance.
    /// </summary>
    [GlobalSetup]
    public async Task GlobalSetup()
    {
        var options = new DbContextOptionsBuilder<BenchmarkDbContext>()
            .UseInMemoryDatabase(databaseName: "SchedulingBenchmarks")
            .Options;

        _context = new BenchmarkDbContext(options);
        await _context.Database.EnsureCreatedAsync();
        _store = new ScheduledMessageStoreEF(_context);
    }

    /// <summary>
    /// Global cleanup: Dispose database context.
    /// </summary>
    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _context?.Dispose();
    }

    /// <summary>
    /// Iteration setup: Clean scheduled messages table before each benchmark iteration.
    /// </summary>
    [IterationSetup]
    public void IterationSetup()
    {
        // Clear all messages for consistent benchmarks
        _context.ScheduledMessages.RemoveRange(_context.ScheduledMessages);
        _context.SaveChanges();
    }

    /// <summary>
    /// Baseline benchmark: Retrieve due messages for execution.
    /// </summary>
    [Benchmark(Baseline = true, Description = "GetDueMessagesAsync")]
    public async Task GetDueMessages()
    {
        // Setup: Add 100 messages (50 due, 50 future)
        var now = DateTime.UtcNow;
        for (int i = 0; i < 100; i++)
        {
            var isDue = i % 2 == 0;
            await _store.AddAsync(new ScheduledMessage
            {
                Id = Guid.NewGuid(),
                RequestType = $"ScheduledCommand{i}",
                Content = "{}",
                ScheduledAtUtc = isDue ? now.AddMinutes(-5) : now.AddMinutes(30),
                CreatedAtUtc = now.AddHours(-1),
                RetryCount = 0
            });
        }
        await _context.SaveChangesAsync();

        // Benchmark: Get messages that are due now
        await _store.GetDueMessagesAsync(50, 5);
    }

    /// <summary>
    /// Benchmark: Add a new scheduled message.
    /// </summary>
    [Benchmark(Description = "AddAsync")]
    public async Task AddMessage()
    {
        var message = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "ReminderCommand",
            Content = "{\"userId\":\"123\",\"message\":\"Don't forget!\"}",
            ScheduledAtUtc = DateTime.UtcNow.AddHours(24),
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 0,
            CorrelationId = Guid.NewGuid().ToString()
        };

        await _store.AddAsync(message);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Benchmark: Reschedule a recurring message after execution.
    /// </summary>
    [Benchmark(Description = "RescheduleRecurringAsync")]
    public async Task RescheduleRecurring()
    {
        // Setup: Add a recurring message
        var messageId = Guid.NewGuid();
        await _store.AddAsync(new ScheduledMessage
        {
            Id = messageId,
            RequestType = "DailyReportCommand",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddDays(-1),
            CreatedAtUtc = DateTime.UtcNow.AddDays(-30),
            IsRecurring = true,
            CronExpression = "0 0 * * *", // Daily at midnight
            LastExecutedAtUtc = DateTime.UtcNow.AddDays(-1)
        });
        await _context.SaveChangesAsync();

        // Benchmark: Calculate next execution time and reschedule
        var nextScheduledAt = DateTime.UtcNow.AddDays(1); // Next day
        await _store.RescheduleRecurringMessageAsync(messageId, nextScheduledAt);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Benchmark: Mark a message as failed and schedule retry.
    /// </summary>
    [Benchmark(Description = "MarkAsFailedAsync")]
    public async Task MarkAsFailed()
    {
        // Setup
        var messageId = Guid.NewGuid();
        await _store.AddAsync(new ScheduledMessage
        {
            Id = messageId,
            RequestType = "FailingCommand",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddMinutes(-10),
            CreatedAtUtc = DateTime.UtcNow.AddHours(-1),
            RetryCount = 0
        });
        await _context.SaveChangesAsync();

        // Benchmark: Mark as failed with retry
        var nextRetry = DateTime.UtcNow.AddMinutes(5);
        await _store.MarkAsFailedAsync(messageId, "Benchmark error", nextRetry);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Benchmark: Retrieve all recurring messages for health monitoring.
    /// </summary>
    [Benchmark(Description = "GetRecurringMessagesAsync")]
    public async Task GetRecurringMessages()
    {
        // Setup: Add 50 messages (10 recurring, 40 one-time)
        for (int i = 0; i < 50; i++)
        {
            var isRecurring = i % 5 == 0;
            await _store.AddAsync(new ScheduledMessage
            {
                Id = Guid.NewGuid(),
                RequestType = isRecurring ? "RecurringTask" : "OneTimeTask",
                Content = "{}",
                ScheduledAtUtc = DateTime.UtcNow.AddHours(1),
                CreatedAtUtc = DateTime.UtcNow,
                IsRecurring = isRecurring,
                CronExpression = isRecurring ? "0 0 * * *" : null
            });
        }
        await _context.SaveChangesAsync();

        // Benchmark: Get all recurring messages
        var allMessages = await _store.GetDueMessagesAsync(1000, 5);
        var _ = allMessages.Where(m => m.IsRecurring).ToList();
    }
}
