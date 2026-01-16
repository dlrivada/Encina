using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Encina.Benchmarks.Infrastructure;
using Encina.Messaging.Inbox;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ADOInbox = Encina.ADO.Sqlite.Inbox;
using DapperInbox = Encina.Dapper.Sqlite.Inbox;
using EFInbox = Encina.EntityFrameworkCore.Inbox;

namespace Encina.Benchmarks.ProviderComparison;

/// <summary>
/// Benchmarks comparing Inbox performance across different data access providers.
/// Tests EF Core, Dapper, and ADO.NET implementations to answer:
/// - Which provider is fastest for idempotent message storage?
/// - Which provider is fastest for duplicate detection?
/// - Which provider is fastest for batch operations?
/// - What's the memory allocation difference?
/// </summary>
[MemoryDiagnoser]
[RankColumn]
[SimpleJob(RuntimeMoniker.Net90)]
#pragma warning disable CA1001 // BenchmarkDotNet handles disposal via GlobalCleanup
public class InboxProviderComparisonBenchmarks
#pragma warning restore CA1001
{
    /// <summary>
    /// The data access provider to benchmark.
    /// </summary>
    [Params("ADO", "Dapper", "EFCore")]
    public string Provider { get; set; } = "ADO";

    private SqliteConnection _connection = null!;
    private BenchmarkDbContext? _context;
    private IInboxStore _store = null!;

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
            _context!.Database.ExecuteSqlRaw("DELETE FROM InboxMessages");
        }
        else
        {
            using var command = _connection.CreateCommand();
            command.CommandText = "DELETE FROM InboxMessages";
            command.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// Benchmarks adding a single message to the inbox.
    /// Tests the overhead of inserting one record for deduplication.
    /// </summary>
    [Benchmark(Description = "AddAsync single message")]
    public async Task AddAsync_Single()
    {
        var message = new DapperInbox.InboxMessage
        {
            MessageId = Guid.NewGuid().ToString(),
            RequestType = "BenchmarkRequest",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            RetryCount = 0
        };

        await _store.AddAsync(message);
        await _store.SaveChangesAsync();
    }

    /// <summary>
    /// Benchmarks adding 10 messages in a batch.
    /// Tests bulk insert performance for inbox deduplication.
    /// </summary>
    [Benchmark(Description = "AddAsync 10 messages")]
    public async Task AddAsync_Batch10()
    {
        for (var i = 0; i < 10; i++)
        {
            await _store.AddAsync(new DapperInbox.InboxMessage
            {
                MessageId = $"batch-{i}-{Guid.NewGuid()}",
                RequestType = $"BatchRequest{i}",
                ReceivedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
                RetryCount = 0
            });
        }
        await _store.SaveChangesAsync();
    }

    /// <summary>
    /// Benchmarks adding 100 messages in a batch.
    /// Tests large batch insert performance for inbox.
    /// </summary>
    [Benchmark(Description = "AddAsync 100 messages")]
    public async Task AddAsync_Batch100()
    {
        for (var i = 0; i < 100; i++)
        {
            await _store.AddAsync(new DapperInbox.InboxMessage
            {
                MessageId = $"batch-{i}-{Guid.NewGuid()}",
                RequestType = $"BatchRequest{i}",
                ReceivedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
                RetryCount = 0
            });
        }
        await _store.SaveChangesAsync();
    }

    /// <summary>
    /// Benchmarks checking if a message exists (duplicate detection).
    /// Tests read performance for idempotency checks.
    /// </summary>
    [Benchmark(Description = "GetMessageAsync (duplicate check)")]
    public async Task GetMessage_DuplicateCheck()
    {
        // Setup: Add a test message
        var messageId = Guid.NewGuid().ToString();
        await _store.AddAsync(new DapperInbox.InboxMessage
        {
            MessageId = messageId,
            RequestType = "DuplicateTest",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            RetryCount = 0
        });
        await _store.SaveChangesAsync();

        // Benchmark: Check for duplicate
        await _store.GetMessageAsync(messageId);
    }

    /// <summary>
    /// Benchmarks marking a message as processed.
    /// Tests update performance with response storage.
    /// </summary>
    [Benchmark(Description = "MarkAsProcessedAsync")]
    public async Task MarkAsProcessed()
    {
        // Setup
        var messageId = Guid.NewGuid().ToString();
        await _store.AddAsync(new DapperInbox.InboxMessage
        {
            MessageId = messageId,
            RequestType = "ProcessTest",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            RetryCount = 0
        });
        await _store.SaveChangesAsync();

        // Benchmark
        await _store.MarkAsProcessedAsync(messageId, "{\"result\":\"success\"}");
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
        var messageId = Guid.NewGuid().ToString();
        await _store.AddAsync(new DapperInbox.InboxMessage
        {
            MessageId = messageId,
            RequestType = "FailTest",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            RetryCount = 0
        });
        await _store.SaveChangesAsync();

        // Benchmark
        await _store.MarkAsFailedAsync(messageId, "Benchmark error", null);
        await _store.SaveChangesAsync();
    }

    /// <summary>
    /// Benchmarks querying expired messages for cleanup.
    /// Tests read performance with filtering.
    /// </summary>
    [Benchmark(Description = "GetExpiredMessagesAsync batch=10")]
    public async Task GetExpiredMessages_Batch10()
    {
        // Setup: Add 50 expired messages
        for (var i = 0; i < 50; i++)
        {
            var messageId = $"expired-{i}-{Guid.NewGuid()}";
            await _store.AddAsync(new DapperInbox.InboxMessage
            {
                MessageId = messageId,
                RequestType = $"ExpiredRequest{i}",
                ReceivedAtUtc = DateTime.UtcNow.AddDays(-10),
                ExpiresAtUtc = DateTime.UtcNow.AddDays(-1),
                RetryCount = 0,
                ProcessedAtUtc = DateTime.UtcNow.AddDays(-9)
            });
        }
        await _store.SaveChangesAsync();

        // Benchmark
        await _store.GetExpiredMessagesAsync(10);
    }

    /// <summary>
    /// Benchmarks removing expired messages in batch.
    /// Tests delete performance.
    /// </summary>
    [Benchmark(Description = "RemoveExpiredMessagesAsync batch=10")]
    public async Task RemoveExpiredMessages_Batch10()
    {
        // Setup: Add 10 expired messages
        var messageIds = new List<string>();
        for (var i = 0; i < 10; i++)
        {
            var messageId = $"remove-{i}-{Guid.NewGuid()}";
            messageIds.Add(messageId);
            await _store.AddAsync(new DapperInbox.InboxMessage
            {
                MessageId = messageId,
                RequestType = $"RemoveRequest{i}",
                ReceivedAtUtc = DateTime.UtcNow.AddDays(-10),
                ExpiresAtUtc = DateTime.UtcNow.AddDays(-1),
                RetryCount = 0,
                ProcessedAtUtc = DateTime.UtcNow.AddDays(-9)
            });
        }
        await _store.SaveChangesAsync();

        // Benchmark
        await _store.RemoveExpiredMessagesAsync(messageIds);
        await _store.SaveChangesAsync();
    }

    private async Task<IInboxStore> SetupAdoStore()
    {
        await SqliteSchemaBuilder.CreateInboxSchemaAsync(_connection);
        return new ADOInbox.InboxStoreADO(_connection);
    }

    private async Task<IInboxStore> SetupDapperStore()
    {
        await SqliteSchemaBuilder.CreateInboxSchemaAsync(_connection);
        return new DapperInbox.InboxStoreDapper(_connection);
    }

    private async Task<IInboxStore> SetupEfCoreStore()
    {
        var options = new DbContextOptionsBuilder<BenchmarkDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new BenchmarkDbContext(options);
        await _context.Database.EnsureCreatedAsync();
        return new EFInbox.InboxStoreEF(_context);
    }
}
