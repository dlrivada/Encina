using BenchmarkDotNet.Attributes;
using Microsoft.EntityFrameworkCore;
using Encina.EntityFrameworkCore.Outbox;

namespace Encina.EntityFrameworkCore.Benchmarks;

/// <summary>
/// Benchmarks for EF Core-based Outbox implementation.
/// </summary>
/// <remarks>
/// Measures performance of core outbox operations:
/// - Adding messages (single and batch)
/// - Retrieving pending messages with pagination
/// - Marking messages as processed
/// - Marking messages as failed with retry logic
/// </remarks>
[MemoryDiagnoser]
[MarkdownExporter]
#pragma warning disable CA1001 // BenchmarkDotNet handles disposal via GlobalCleanup
public class OutboxStoreEFBenchmarks
#pragma warning restore CA1001
{
    private BenchmarkDbContext _context = null!;
    private OutboxStoreEF _store = null!;

    /// <summary>
    /// Global setup: Create in-memory database and store instance.
    /// </summary>
    [GlobalSetup]
    public async Task GlobalSetup()
    {
        var options = new DbContextOptionsBuilder<BenchmarkDbContext>()
            .UseInMemoryDatabase(databaseName: "OutboxBenchmarks")
            .Options;

        _context = new BenchmarkDbContext(options);
        await _context.Database.EnsureCreatedAsync();
        _store = new OutboxStoreEF(_context);
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
    /// Iteration setup: Clean outbox table before each benchmark iteration.
    /// </summary>
    [IterationSetup]
    public void IterationSetup()
    {
        // Clear all messages for consistent benchmarks
        _context.OutboxMessages.RemoveRange(_context.OutboxMessages);
        _context.SaveChanges();
    }

    /// <summary>
    /// Baseline benchmark: Add a single message to the outbox.
    /// </summary>
    [Benchmark(Baseline = true, Description = "AddAsync single message")]
    public async Task AddMessage()
    {
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "BenchmarkEvent",
            Content = "{\"test\":true}",
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 0
        };

        await _store.AddAsync(message);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Benchmark: Retrieve 10 pending messages with batching.
    /// </summary>
    [Benchmark(Description = "GetPendingMessagesAsync batch=10")]
    public async Task GetPendingMessages_Batch10()
    {
        // Setup: Add 50 messages
        for (int i = 0; i < 50; i++)
        {
            await _store.AddAsync(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                NotificationType = $"QueryEvent{i}",
                Content = "{}",
                CreatedAtUtc = DateTime.UtcNow
            });
        }
        await _context.SaveChangesAsync();

        // Benchmark
        await _store.GetPendingMessagesAsync(10, 5);
    }

    /// <summary>
    /// Benchmark: Retrieve 100 pending messages with batching.
    /// </summary>
    [Benchmark(Description = "GetPendingMessagesAsync batch=100")]
    public async Task GetPendingMessages_Batch100()
    {
        // Setup: Add 500 messages
        for (int i = 0; i < 500; i++)
        {
            await _store.AddAsync(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                NotificationType = $"LargeQueryEvent{i}",
                Content = "{}",
                CreatedAtUtc = DateTime.UtcNow
            });
        }
        await _context.SaveChangesAsync();

        // Benchmark
        await _store.GetPendingMessagesAsync(100, 5);
    }

    /// <summary>
    /// Benchmark: Mark a message as successfully processed.
    /// </summary>
    [Benchmark(Description = "MarkAsProcessedAsync")]
    public async Task MarkAsProcessed()
    {
        // Setup
        var id = Guid.NewGuid();
        await _store.AddAsync(new OutboxMessage
        {
            Id = id,
            NotificationType = "ProcessTest",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Benchmark
        await _store.MarkAsProcessedAsync(id);
        await _context.SaveChangesAsync();
    }
}
