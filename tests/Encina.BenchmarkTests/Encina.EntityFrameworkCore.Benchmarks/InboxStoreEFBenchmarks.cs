using BenchmarkDotNet.Attributes;
using Encina.EntityFrameworkCore.Inbox;
using Microsoft.EntityFrameworkCore;

namespace Encina.EntityFrameworkCore.Benchmarks;

/// <summary>
/// Benchmarks for EF Core-based Inbox implementation.
/// </summary>
/// <remarks>
/// Measures performance of core inbox operations:
/// - Retrieving messages by ID (idempotency check)
/// - Adding new messages
/// - Marking messages as processed
/// - Retrieving expired messages for cleanup
/// </remarks>
[MemoryDiagnoser]
[MarkdownExporter]
#pragma warning disable CA1001 // BenchmarkDotNet handles disposal via GlobalCleanup
public class InboxStoreEFBenchmarks
#pragma warning restore CA1001
{
    private BenchmarkDbContext _context = null!;
    private InboxStoreEF _store = null!;
    private string _testMessageId = null!;

    /// <summary>
    /// Global setup: Create in-memory database and store instance.
    /// </summary>
    [GlobalSetup]
    public async Task GlobalSetup()
    {
        var options = new DbContextOptionsBuilder<BenchmarkDbContext>()
            .UseInMemoryDatabase(databaseName: "InboxBenchmarks")
            .Options;

        _context = new BenchmarkDbContext(options);
        await _context.Database.EnsureCreatedAsync();
        _store = new InboxStoreEF(_context);
        _testMessageId = Guid.NewGuid().ToString();
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
    /// Iteration setup: Clean inbox table before each benchmark iteration.
    /// </summary>
    [IterationSetup]
    public void IterationSetup()
    {
        // Clear all messages for consistent benchmarks
        _context.InboxMessages.RemoveRange(_context.InboxMessages);
        _context.SaveChanges();
    }

    /// <summary>
    /// Baseline benchmark: Retrieve a message by ID (idempotency check).
    /// </summary>
    [Benchmark(Baseline = true, Description = "GetMessageAsync")]
    public async Task GetMessage()
    {
        // Setup: Add a message
        await _store.AddAsync(new InboxMessage
        {
            MessageId = _testMessageId,
            RequestType = "BenchmarkCommand",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7)
        });
        await _context.SaveChangesAsync();

        // Benchmark
        await _store.GetMessageAsync(_testMessageId);
    }

    /// <summary>
    /// Benchmark: Add a new message to the inbox.
    /// </summary>
    [Benchmark(Description = "AddAsync")]
    public async Task AddMessage()
    {
        var message = new InboxMessage
        {
            MessageId = Guid.NewGuid().ToString(),
            RequestType = "BenchmarkCommand",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            RetryCount = 0
        };

        await _store.AddAsync(message);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Benchmark: Mark a message as successfully processed.
    /// </summary>
    [Benchmark(Description = "MarkAsProcessedAsync")]
    public async Task MarkAsProcessed()
    {
        // Setup
        var messageId = Guid.NewGuid().ToString();
        await _store.AddAsync(new InboxMessage
        {
            MessageId = messageId,
            RequestType = "ProcessTest",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7)
        });
        await _context.SaveChangesAsync();

        // Benchmark
        await _store.MarkAsProcessedAsync(messageId, "{\"result\":\"success\"}");
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Benchmark: Retrieve expired messages for cleanup.
    /// </summary>
    [Benchmark(Description = "GetExpiredMessagesAsync")]
    public async Task GetExpiredMessages()
    {
        // Setup: Add 100 messages (50 expired, 50 active)
        var now = DateTime.UtcNow;
        for (int i = 0; i < 100; i++)
        {
            var isExpired = i % 2 == 0;
            await _store.AddAsync(new InboxMessage
            {
                MessageId = Guid.NewGuid().ToString(),
                RequestType = $"CleanupTest{i}",
                ReceivedAtUtc = now.AddDays(-10),
                ExpiresAtUtc = isExpired ? now.AddDays(-1) : now.AddDays(7)
            });
        }
        await _context.SaveChangesAsync();

        // Benchmark
        await _store.GetExpiredMessagesAsync(50);
    }
}
