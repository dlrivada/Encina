using BenchmarkDotNet.Attributes;
using Encina.Benchmarks.Infrastructure;
using Encina.EntityFrameworkCore.Outbox;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Encina.Benchmarks.Outbox;

/// <summary>
/// Benchmarks for EF Core-based Outbox implementation.
/// </summary>
[MemoryDiagnoser]
[RankColumn]
#pragma warning disable CA1001 // BenchmarkDotNet handles disposal via GlobalCleanup
public class OutboxEfCoreBenchmarks
#pragma warning restore CA1001
{
    private SqliteConnection _connection = null!;
    private BenchmarkDbContext _context = null!;
    private OutboxStoreEF _store = null!;
    private Guid _testMessageId;

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
        _store = new OutboxStoreEF(_context);

        _testMessageId = Guid.NewGuid();
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
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM OutboxMessages");
    }

    [Benchmark(Baseline = true, Description = "AddAsync single message")]
    public async Task AddAsync_Single()
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

    [Benchmark(Description = "AddAsync 10 messages")]
    public async Task AddAsync_Batch10()
    {
        for (var i = 0; i < 10; i++)
        {
            await _store.AddAsync(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                NotificationType = $"BatchEvent{i}",
                Content = "{}",
                CreatedAtUtc = DateTime.UtcNow
            });
        }
        await _context.SaveChangesAsync();
    }

    [Benchmark(Description = "GetPendingMessagesAsync batch=10")]
    public async Task GetPendingMessages_Batch10()
    {
        // Setup: Add 50 messages
        for (var i = 0; i < 50; i++)
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

    [Benchmark(Description = "MarkAsFailedAsync")]
    public async Task MarkAsFailed()
    {
        // Setup
        var id = Guid.NewGuid();
        await _store.AddAsync(new OutboxMessage
        {
            Id = id,
            NotificationType = "FailTest",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Benchmark
        await _store.MarkAsFailedAsync(id, "Benchmark error", null);
        await _context.SaveChangesAsync();
    }
}
