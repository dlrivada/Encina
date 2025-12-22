using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Encina.Benchmarks.Infrastructure;
using Encina.Messaging.Scheduling;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using DapperScheduling = Encina.Dapper.Sqlite.Scheduling;
using EFScheduling = Encina.EntityFrameworkCore.Scheduling;

namespace Encina.Benchmarks.ProviderComparison;

/// <summary>
/// Benchmarks comparing Scheduling performance across different data access providers.
/// Tests EF Core and Dapper implementations to answer:
/// - Which provider is fastest for scheduling messages?
/// - Which provider is fastest for querying due messages?
/// - Which provider is fastest for marking messages as executed/failed?
/// - What's the memory allocation difference?
/// </summary>
/// <remarks>
/// <para>
/// <b>Note</b>: ADO.NET providers do NOT support Scheduling.
/// Only Dapper and EF Core providers implement the Scheduling pattern.
/// </para>
/// <para>
/// Scheduling patterns are used for delayed/recurring command execution.
/// Typical operations:
/// <list type="bullet">
/// <item><description>Schedule message for future execution (AddAsync)</description></item>
/// <item><description>Query messages that are due (GetDueMessagesAsync)</description></item>
/// <item><description>Mark as executed (MarkAsProcessedAsync)</description></item>
/// <item><description>Mark as failed and retry (MarkAsFailedAsync)</description></item>
/// <item><description>Cleanup old messages (CancelAsync)</description></item>
/// </list>
/// </para>
/// </remarks>
[MemoryDiagnoser]
[RankColumn]
[SimpleJob(RuntimeMoniker.Net90)]
#pragma warning disable CA1001 // BenchmarkDotNet handles disposal via GlobalCleanup
public class SchedulingProviderComparisonBenchmarks
#pragma warning restore CA1001
{
    /// <summary>
    /// The data access provider to benchmark.
    /// Only Dapper and EFCore support Scheduling (ADO.NET does NOT).
    /// </summary>
    [Params("Dapper", "EFCore")]
    public string Provider { get; set; } = "Dapper";

    private SqliteConnection _connection = null!;
    private BenchmarkDbContext? _context;
    private IScheduledMessageStore _store = null!;

    /// <summary>
    /// Sets up the database connection and store based on the selected provider.
    /// </summary>
    [GlobalSetup]
    public async Task GlobalSetup()
    {
        // Register Dapper type handlers
        DapperTypeHandlers.Register();

        // Create in-memory SQLite connection
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        // Create store based on provider parameter
        _store = Provider switch
        {
            "Dapper" => await SetupDapperStore(),
            "EFCore" => await SetupEfCoreStore(),
            _ => throw new InvalidOperationException($"Unknown provider: {Provider}")
        };
    }

    /// <summary>
    /// Cleans up resources after benchmarks complete.
    /// </summary>
    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _context?.Dispose();
        _connection?.Dispose();
    }

    /// <summary>
    /// Cleans the table before each iteration to ensure consistent results.
    /// </summary>
    [IterationSetup]
    public async Task IterationSetup()
    {
        if (Provider == "EFCore")
        {
            await _context!.Database.ExecuteSqlRawAsync("DELETE FROM ScheduledMessages");
        }
        else
        {
            using var command = _connection.CreateCommand();
            command.CommandText = "DELETE FROM ScheduledMessages";
            command.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// Benchmarks adding a single scheduled message.
    /// Tests the overhead of scheduling one message for future execution.
    /// </summary>
    [Benchmark(Baseline = true, Description = "AddAsync single scheduled message")]
    public async Task AddAsync_Single()
    {
        var message = CreateScheduledMessage(
            requestType: "SendReminderCommand",
            content: "{\"userId\":123,\"message\":\"Your appointment is tomorrow\"}",
            scheduledAt: DateTime.UtcNow.AddHours(1));

        await _store.AddAsync(message);
        await _store.SaveChangesAsync();
    }

    /// <summary>
    /// Benchmarks adding 10 scheduled messages in a batch.
    /// Tests bulk insert performance for scheduled messages.
    /// </summary>
    [Benchmark(Description = "AddAsync 10 scheduled messages")]
    public async Task AddAsync_Batch10()
    {
        for (int i = 0; i < 10; i++)
        {
            await _store.AddAsync(CreateScheduledMessage(
                requestType: $"BatchCommand{i}",
                content: "{}",
                scheduledAt: DateTime.UtcNow.AddHours(i)));
        }
        await _store.SaveChangesAsync();
    }

    /// <summary>
    /// Benchmarks adding 100 scheduled messages in a batch.
    /// Tests large batch insert performance for scheduled messages.
    /// </summary>
    [Benchmark(Description = "AddAsync 100 scheduled messages")]
    public async Task AddAsync_Batch100()
    {
        for (int i = 0; i < 100; i++)
        {
            await _store.AddAsync(CreateScheduledMessage(
                requestType: $"BatchCommand{i}",
                content: "{}",
                scheduledAt: DateTime.UtcNow.AddHours(i)));
        }
        await _store.SaveChangesAsync();
    }

    /// <summary>
    /// Benchmarks querying due messages with batch size of 10.
    /// Tests read performance with filtering, ordering, and retry logic.
    /// </summary>
    [Benchmark(Description = "GetDueMessagesAsync batch=10")]
    public async Task GetDueMessages_Batch10()
    {
        // Setup: Add 50 messages (30 due now, 20 future)
        var now = DateTime.UtcNow;
        for (int i = 0; i < 50; i++)
        {
            var isDue = i < 30;
            await _store.AddAsync(CreateScheduledMessage(
                requestType: $"DueCommand{i}",
                content: "{}",
                scheduledAt: isDue ? now.AddMinutes(-i) : now.AddHours(i)));
        }
        await _store.SaveChangesAsync();

        // Benchmark
        await _store.GetDueMessagesAsync(batchSize: 10, maxRetries: 5);
    }

    /// <summary>
    /// Benchmarks querying due messages with batch size of 100.
    /// Tests read performance with larger result sets.
    /// </summary>
    [Benchmark(Description = "GetDueMessagesAsync batch=100")]
    public async Task GetDueMessages_Batch100()
    {
        // Setup: Add 500 messages (300 due now, 200 future)
        var now = DateTime.UtcNow;
        for (int i = 0; i < 500; i++)
        {
            var isDue = i < 300;
            await _store.AddAsync(CreateScheduledMessage(
                requestType: $"DueCommand{i}",
                content: "{}",
                scheduledAt: isDue ? now.AddMinutes(-i) : now.AddHours(i)));
        }
        await _store.SaveChangesAsync();

        // Benchmark
        await _store.GetDueMessagesAsync(batchSize: 100, maxRetries: 5);
    }

    /// <summary>
    /// Benchmarks marking a message as processed.
    /// Tests update performance for successful execution.
    /// </summary>
    [Benchmark(Description = "MarkAsProcessedAsync")]
    public async Task MarkAsProcessed()
    {
        // Setup
        var id = Guid.NewGuid();
        await _store.AddAsync(CreateScheduledMessage(
            id: id,
            requestType: "ExecuteCommand",
            content: "{}",
            scheduledAt: DateTime.UtcNow.AddMinutes(-5)));
        await _store.SaveChangesAsync();

        // Benchmark
        await _store.MarkAsProcessedAsync(id);
        await _store.SaveChangesAsync();
    }

    /// <summary>
    /// Benchmarks marking a message as failed with retry scheduling.
    /// Tests update with error tracking and retry logic performance.
    /// </summary>
    [Benchmark(Description = "MarkAsFailedAsync with retry")]
    public async Task MarkAsFailed()
    {
        // Setup
        var id = Guid.NewGuid();
        await _store.AddAsync(CreateScheduledMessage(
            id: id,
            requestType: "FailCommand",
            content: "{}",
            scheduledAt: DateTime.UtcNow.AddMinutes(-5)));
        await _store.SaveChangesAsync();

        // Benchmark
        await _store.MarkAsFailedAsync(
            messageId: id,
            errorMessage: "Execution failed - simulated error",
            nextRetryAt: DateTime.UtcNow.AddMinutes(5));
        await _store.SaveChangesAsync();
    }

    /// <summary>
    /// Benchmarks rescheduling a recurring message.
    /// Tests update performance for recurring message patterns.
    /// </summary>
    [Benchmark(Description = "RescheduleRecurringMessageAsync")]
    public async Task RescheduleRecurringMessage()
    {
        // Setup: Add a recurring message
        var id = Guid.NewGuid();
        var message = CreateScheduledMessage(
            id: id,
            requestType: "RecurringCommand",
            content: "{}",
            scheduledAt: DateTime.UtcNow.AddMinutes(-5));

        if (Provider == "Dapper")
        {
            ((DapperScheduling.ScheduledMessage)message).IsRecurring = true;
            ((DapperScheduling.ScheduledMessage)message).CronExpression = "0 0 * * *"; // Daily
        }
        else // EFCore
        {
            ((EFScheduling.ScheduledMessage)message).IsRecurring = true;
            ((EFScheduling.ScheduledMessage)message).CronExpression = "0 0 * * *"; // Daily
        }

        await _store.AddAsync(message);
        await _store.SaveChangesAsync();

        // Benchmark
        await _store.RescheduleRecurringMessageAsync(id, DateTime.UtcNow.AddDays(1));
        await _store.SaveChangesAsync();
    }

    /// <summary>
    /// Benchmarks canceling a scheduled message.
    /// Tests delete/cancel performance.
    /// </summary>
    [Benchmark(Description = "CancelAsync")]
    public async Task CancelScheduledMessage()
    {
        // Setup
        var id = Guid.NewGuid();
        await _store.AddAsync(CreateScheduledMessage(
            id: id,
            requestType: "CancelCommand",
            content: "{}",
            scheduledAt: DateTime.UtcNow.AddHours(1)));
        await _store.SaveChangesAsync();

        // Benchmark
        await _store.CancelAsync(id);
        await _store.SaveChangesAsync();
    }

    /// <summary>
    /// Creates a scheduled message instance for benchmarking.
    /// </summary>
    private IScheduledMessage CreateScheduledMessage(
        Guid? id = null,
        string requestType = "TestCommand",
        string content = "{}",
        DateTime? scheduledAt = null)
    {
        var now = DateTime.UtcNow;

        if (Provider == "Dapper")
        {
            return new DapperScheduling.ScheduledMessage
            {
                Id = id ?? Guid.NewGuid(),
                RequestType = requestType,
                Content = content,
                ScheduledAtUtc = scheduledAt ?? now.AddHours(1),
                CreatedAtUtc = now,
                RetryCount = 0
            };
        }
        else // EFCore
        {
            return new EFScheduling.ScheduledMessage
            {
                Id = id ?? Guid.NewGuid(),
                RequestType = requestType,
                Content = content,
                ScheduledAtUtc = scheduledAt ?? now.AddHours(1),
                CreatedAtUtc = now,
                RetryCount = 0
            };
        }
    }

    private async Task<IScheduledMessageStore> SetupDapperStore()
    {
        await SqliteSchemaBuilder.CreateSchedulingSchemaAsync(_connection);
        return new DapperScheduling.ScheduledMessageStoreDapper(_connection);
    }

    private async Task<IScheduledMessageStore> SetupEfCoreStore()
    {
        var options = new DbContextOptionsBuilder<BenchmarkDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new BenchmarkDbContext(options);
        await _context.Database.EnsureCreatedAsync();
        return new EFScheduling.ScheduledMessageStoreEF(_context);
    }
}
