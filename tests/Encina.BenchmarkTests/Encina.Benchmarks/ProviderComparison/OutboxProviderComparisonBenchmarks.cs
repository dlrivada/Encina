using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Encina.Benchmarks.Infrastructure;
using Encina.Messaging.Outbox;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ADOOutbox = Encina.ADO.Sqlite.Outbox;
using DapperOutbox = Encina.Dapper.Sqlite.Outbox;
using EFOutbox = Encina.EntityFrameworkCore.Outbox;

namespace Encina.Benchmarks.ProviderComparison;

/// <summary>
/// Benchmarks comparing Outbox performance across different data access providers.
/// Tests EF Core, Dapper, and ADO.NET implementations to answer:
/// - Which provider is fastest for single inserts?
/// - Which provider is fastest for batch operations?
/// - Which provider is fastest for queries?
/// - What's the memory allocation difference?
/// </summary>
[MemoryDiagnoser]
[RankColumn]
[SimpleJob(RuntimeMoniker.Net90)]
#pragma warning disable CA1001 // BenchmarkDotNet handles disposal via GlobalCleanup
public class OutboxProviderComparisonBenchmarks
#pragma warning restore CA1001
{
    /// <summary>
    /// The data access provider to benchmark.
    /// </summary>
    [Params("ADO", "Dapper", "EFCore")]
    public string Provider { get; set; } = "ADO";

    private SqliteConnection _connection = null!;
    private BenchmarkDbContext? _context;
    private IOutboxStore _store = null!;

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
            "ADO" => await SetupAdoStore(),
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
    /// <remarks>
    /// Note: IterationSetup must be synchronous in BenchmarkDotNet.
    /// </remarks>
    [IterationSetup]
    public void IterationSetup()
    {
        if (Provider == "EFCore")
        {
            _context!.Database.ExecuteSqlRaw("DELETE FROM OutboxMessages");
        }
        else
        {
            using var command = _connection.CreateCommand();
            command.CommandText = "DELETE FROM OutboxMessages";
            command.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// Benchmarks adding a single message to the outbox.
    /// Tests the overhead of inserting one record.
    /// </summary>
    [Benchmark(Description = "AddAsync single message")]
    public async Task AddAsync_Single()
    {
        var message = new DapperOutbox.OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "BenchmarkEvent",
            Content = "{\"test\":true}",
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 0
        };

        await _store.AddAsync(message);
        await _store.SaveChangesAsync();
    }

    /// <summary>
    /// Benchmarks adding 10 messages in a batch.
    /// Tests bulk insert performance.
    /// </summary>
    [Benchmark(Description = "AddAsync 10 messages")]
    public async Task AddAsync_Batch10()
    {
        for (var i = 0; i < 10; i++)
        {
            await _store.AddAsync(new DapperOutbox.OutboxMessage
            {
                Id = Guid.NewGuid(),
                NotificationType = $"BatchEvent{i}",
                Content = "{}",
                CreatedAtUtc = DateTime.UtcNow,
                RetryCount = 0
            });
        }
        await _store.SaveChangesAsync();
    }

    /// <summary>
    /// Benchmarks adding 100 messages in a batch.
    /// Tests large batch insert performance.
    /// </summary>
    [Benchmark(Description = "AddAsync 100 messages")]
    public async Task AddAsync_Batch100()
    {
        for (var i = 0; i < 100; i++)
        {
            await _store.AddAsync(new DapperOutbox.OutboxMessage
            {
                Id = Guid.NewGuid(),
                NotificationType = $"BatchEvent{i}",
                Content = "{}",
                CreatedAtUtc = DateTime.UtcNow,
                RetryCount = 0
            });
        }
        await _store.SaveChangesAsync();
    }

    /// <summary>
    /// Benchmarks querying pending messages with batch size of 10.
    /// Tests read performance with filtering and ordering.
    /// </summary>
    [Benchmark(Description = "GetPendingMessages batch=10")]
    public async Task GetPendingMessages_Batch10()
    {
        // Setup: Add 50 messages
        for (var i = 0; i < 50; i++)
        {
            await _store.AddAsync(new DapperOutbox.OutboxMessage
            {
                Id = Guid.NewGuid(),
                NotificationType = $"QueryEvent{i}",
                Content = "{}",
                CreatedAtUtc = DateTime.UtcNow,
                RetryCount = 0
            });
        }
        await _store.SaveChangesAsync();

        // Benchmark
        await _store.GetPendingMessagesAsync(10, 5);
    }

    /// <summary>
    /// Benchmarks querying pending messages with batch size of 100.
    /// Tests read performance with larger result sets.
    /// </summary>
    [Benchmark(Description = "GetPendingMessages batch=100")]
    public async Task GetPendingMessages_Batch100()
    {
        // Setup: Add 500 messages
        for (var i = 0; i < 500; i++)
        {
            await _store.AddAsync(new DapperOutbox.OutboxMessage
            {
                Id = Guid.NewGuid(),
                NotificationType = $"QueryEvent{i}",
                Content = "{}",
                CreatedAtUtc = DateTime.UtcNow,
                RetryCount = 0
            });
        }
        await _store.SaveChangesAsync();

        // Benchmark
        await _store.GetPendingMessagesAsync(100, 5);
    }

    /// <summary>
    /// Benchmarks marking a message as processed.
    /// Tests update performance.
    /// </summary>
    [Benchmark(Description = "MarkAsProcessedAsync")]
    public async Task MarkAsProcessed()
    {
        // Setup
        var id = Guid.NewGuid();
        await _store.AddAsync(new DapperOutbox.OutboxMessage
        {
            Id = id,
            NotificationType = "ProcessTest",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 0
        });
        await _store.SaveChangesAsync();

        // Benchmark
        await _store.MarkAsProcessedAsync(id);
        await _store.SaveChangesAsync();
    }

    /// <summary>
    /// Benchmarks marking a message as failed.
    /// Tests update with error tracking performance.
    /// </summary>
    [Benchmark(Description = "MarkAsFailedAsync")]
    public async Task MarkAsFailed()
    {
        // Setup
        var id = Guid.NewGuid();
        await _store.AddAsync(new DapperOutbox.OutboxMessage
        {
            Id = id,
            NotificationType = "FailTest",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 0
        });
        await _store.SaveChangesAsync();

        // Benchmark
        await _store.MarkAsFailedAsync(id, "Benchmark error", null);
        await _store.SaveChangesAsync();
    }

    private async Task<IOutboxStore> SetupAdoStore()
    {
        await SqliteSchemaBuilder.CreateOutboxSchemaAsync(_connection);
        return new ADOOutbox.OutboxStoreADO(_connection);
    }

    private async Task<IOutboxStore> SetupDapperStore()
    {
        await SqliteSchemaBuilder.CreateOutboxSchemaAsync(_connection);
        return new DapperOutbox.OutboxStoreDapper(_connection);
    }

    private async Task<IOutboxStore> SetupEfCoreStore()
    {
        var options = new DbContextOptionsBuilder<BenchmarkDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new BenchmarkDbContext(options);
        await _context.Database.EnsureCreatedAsync();
        return new EFOutbox.OutboxStoreEF(_context);
    }
}
