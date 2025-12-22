using BenchmarkDotNet.Attributes;
using Encina.Benchmarks.Infrastructure;
using Encina.EntityFrameworkCore.Inbox;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Encina.Benchmarks.Inbox;

/// <summary>
/// Benchmarks for EF Core-based Inbox implementation.
/// </summary>
[MemoryDiagnoser]
[RankColumn]
#pragma warning disable CA1001 // BenchmarkDotNet handles disposal via GlobalCleanup
public class InboxEfCoreBenchmarks
#pragma warning restore CA1001
{
    private SqliteConnection _connection = null!;
    private BenchmarkDbContext _context = null!;
    private InboxStoreEF _store = null!;

    private static readonly string[] s_singleMessageId = ["remove-bench"];

    [Params(1, 10, 100)]
    public int MessageCount { get; set; }

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<BenchmarkDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new BenchmarkDbContext(options);
        await _context.Database.EnsureCreatedAsync();
        _store = new InboxStoreEF(_context);
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _context?.Dispose();
        _connection?.Dispose();
    }

    [IterationSetup]
    public async Task IterationSetup()
    {
        // Clean table before each iteration
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM InboxMessages");
    }

    [Benchmark(Baseline = true, Description = "AddAsync single message")]
    public async Task AddAsync_Single()
    {
        var message = new InboxMessage
        {
            MessageId = "bench-msg-0",
            RequestType = "BenchmarkRequest",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            RetryCount = 0
        };

        await _store.AddAsync(message);
        await _context.SaveChangesAsync();
    }

    [Benchmark(Description = "AddAsync batch")]
    public async Task AddAsync_Batch()
    {
        for (var i = 0; i < MessageCount; i++)
        {
            await _store.AddAsync(new InboxMessage
            {
                MessageId = $"bench-msg-{i}",
                RequestType = "BenchmarkRequest",
                ReceivedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
                RetryCount = 0
            });
        }
        await _context.SaveChangesAsync();
    }

    [Benchmark(Description = "GetMessageAsync (hit)")]
    public async Task GetMessageAsync_Hit()
    {
        // Setup
        var message = new InboxMessage
        {
            MessageId = "get-bench",
            RequestType = "BenchmarkRequest",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            RetryCount = 0
        };
        await _store.AddAsync(message);
        await _context.SaveChangesAsync();

        // Benchmark
        await _store.GetMessageAsync("get-bench");
    }

    [Benchmark(Description = "GetMessageAsync (miss)")]
    public async Task GetMessageAsync_Miss()
    {
        await _store.GetMessageAsync("non-existent-id");
    }

    [Benchmark(Description = "MarkAsProcessedAsync")]
    public async Task MarkAsProcessed()
    {
        // Setup
        var message = new InboxMessage
        {
            MessageId = "process-bench",
            RequestType = "BenchmarkRequest",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            RetryCount = 0
        };
        await _store.AddAsync(message);
        await _context.SaveChangesAsync();

        // Benchmark
        await _store.MarkAsProcessedAsync("process-bench", "{\"result\":\"success\"}");
        await _context.SaveChangesAsync();
    }

    [Benchmark(Description = "MarkAsProcessedAsync (large response 10KB)")]
    public async Task MarkAsProcessed_LargeResponse()
    {
        // Setup
        var message = new InboxMessage
        {
            MessageId = "large-bench",
            RequestType = "BenchmarkRequest",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            RetryCount = 0
        };
        await _store.AddAsync(message);
        await _context.SaveChangesAsync();

        // Benchmark
        var largeResponse = new string('X', 10_000);
        await _store.MarkAsProcessedAsync("large-bench", largeResponse);
        await _context.SaveChangesAsync();
    }

    [Benchmark(Description = "MarkAsFailedAsync")]
    public async Task MarkAsFailed()
    {
        // Setup
        var message = new InboxMessage
        {
            MessageId = "fail-bench",
            RequestType = "BenchmarkRequest",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            RetryCount = 0
        };
        await _store.AddAsync(message);
        await _context.SaveChangesAsync();

        // Benchmark
        await _store.MarkAsFailedAsync("fail-bench", "Temporary error", DateTime.UtcNow.AddMinutes(5));
        await _context.SaveChangesAsync();
    }

    [Benchmark(Description = "MarkAsFailedAsync (5 retries)")]
    public async Task MarkAsFailed_MultipleRetries()
    {
        // Setup
        var message = new InboxMessage
        {
            MessageId = "retry-bench",
            RequestType = "BenchmarkRequest",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            RetryCount = 0
        };
        await _store.AddAsync(message);
        await _context.SaveChangesAsync();

        // Benchmark
        for (var i = 0; i < 5; i++)
        {
            await _store.MarkAsFailedAsync("retry-bench", $"Error {i}", DateTime.UtcNow.AddMinutes(5));
            await _context.SaveChangesAsync();
        }
    }

    [Benchmark(Description = "GetExpiredMessagesAsync (empty)")]
    public async Task GetExpiredMessages_Empty()
    {
        await _store.GetExpiredMessagesAsync(100);
    }

    [Benchmark(Description = "GetExpiredMessagesAsync (batch)")]
    public async Task GetExpiredMessages_Batch()
    {
        // Setup
        for (var i = 0; i < MessageCount; i++)
        {
            var message = new InboxMessage
            {
                MessageId = $"expired-bench-{i}",
                RequestType = "BenchmarkRequest",
                ReceivedAtUtc = DateTime.UtcNow.AddDays(-40),
                ProcessedAtUtc = DateTime.UtcNow.AddDays(-35),
                ExpiresAtUtc = DateTime.UtcNow.AddDays(-i - 1),
                RetryCount = 0
            };
            await _store.AddAsync(message);
        }
        await _context.SaveChangesAsync();

        // Benchmark
        await _store.GetExpiredMessagesAsync(100);
    }

    [Benchmark(Description = "RemoveExpiredMessagesAsync (single)")]
    public async Task RemoveExpiredMessages_Single()
    {
        // Setup
        var message = new InboxMessage
        {
            MessageId = "remove-bench",
            RequestType = "BenchmarkRequest",
            ReceivedAtUtc = DateTime.UtcNow.AddDays(-40),
            ProcessedAtUtc = DateTime.UtcNow.AddDays(-35),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(-1),
            RetryCount = 0
        };
        await _store.AddAsync(message);
        await _context.SaveChangesAsync();

        // Benchmark
        await _store.RemoveExpiredMessagesAsync(s_singleMessageId);
        await _context.SaveChangesAsync();
    }

    [Benchmark(Description = "RemoveExpiredMessagesAsync (batch)")]
    public async Task RemoveExpiredMessages_Batch()
    {
        // Setup
        var messageIds = new List<string>();
        for (var i = 0; i < MessageCount; i++)
        {
            var messageId = $"remove-batch-{i}";
            messageIds.Add(messageId);
            var message = new InboxMessage
            {
                MessageId = messageId,
                RequestType = "BenchmarkRequest",
                ReceivedAtUtc = DateTime.UtcNow.AddDays(-40),
                ProcessedAtUtc = DateTime.UtcNow.AddDays(-35),
                ExpiresAtUtc = DateTime.UtcNow.AddDays(-1),
                RetryCount = 0
            };
            await _store.AddAsync(message);
        }
        await _context.SaveChangesAsync();

        // Benchmark
        await _store.RemoveExpiredMessagesAsync(messageIds);
        await _context.SaveChangesAsync();
    }

    [Benchmark(Description = "Full workflow: Add → Process")]
    public async Task FullWorkflow_Success()
    {
        // Add
        var message = new InboxMessage
        {
            MessageId = "workflow-success",
            RequestType = "BenchmarkRequest",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            RetryCount = 0
        };
        await _store.AddAsync(message);
        await _context.SaveChangesAsync();

        // Check
        await _store.GetMessageAsync("workflow-success");

        // Process
        await _store.MarkAsProcessedAsync("workflow-success", "{\"result\":\"ok\"}");
        await _context.SaveChangesAsync();
    }

    [Benchmark(Description = "Full workflow: Add → Fail → Retry → Process")]
    public async Task FullWorkflow_WithRetry()
    {
        // Add
        var message = new InboxMessage
        {
            MessageId = "workflow-retry",
            RequestType = "BenchmarkRequest",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            RetryCount = 0
        };
        await _store.AddAsync(message);
        await _context.SaveChangesAsync();

        // Fail
        await _store.MarkAsFailedAsync("workflow-retry", "Temporary error", DateTime.UtcNow.AddMinutes(5));
        await _context.SaveChangesAsync();

        // Retry check
        await _store.GetMessageAsync("workflow-retry");

        // Success
        await _store.MarkAsProcessedAsync("workflow-retry", "{\"result\":\"success\"}");
        await _context.SaveChangesAsync();
    }

    [Benchmark(Description = "Idempotent request (duplicate detection)")]
    public async Task IdempotentRequest()
    {
        // Setup
        var message = new InboxMessage
        {
            MessageId = "idempotent-bench",
            RequestType = "BenchmarkRequest",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            RetryCount = 0
        };
        await _store.AddAsync(message);
        await _store.MarkAsProcessedAsync("idempotent-bench", "{\"orderId\":123}");
        await _context.SaveChangesAsync();

        // Benchmark: Duplicate request
        var existing = await _store.GetMessageAsync("idempotent-bench");
        _ = existing?.Response;
    }
}
