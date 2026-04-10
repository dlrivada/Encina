using BenchmarkDotNet.Attributes;
using Encina.MongoDB.Benchmarks.Infrastructure;
using Encina.MongoDB.Outbox;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Encina.MongoDB.Benchmarks.Benchmarks;

/// <summary>
/// Measures the cost of the Encina outbox pattern on MongoDB — both the "write" path
/// (<see cref="OutboxStoreMongoDB.AddAsync"/>) and the scheduler "read" path
/// (<see cref="OutboxStoreMongoDB.GetPendingMessagesAsync"/>). Both land on the hot path of
/// every transactional message-producing request.
/// </summary>
[MemoryDiagnoser]
public class OutboxStoreBenchmarks
{
    private const int SeedMessageCount = 100;
    private const int BatchSize = 100;

    private MongoDbBenchmarkContainer _container = null!;
    private OutboxStoreMongoDB _store = null!;

    /// <summary>
    /// Boots the container, wires an <see cref="OutboxStoreMongoDB"/> against a dedicated
    /// database/collection, and seeds 100 pending messages so the read benchmark has realistic
    /// data to scan.
    /// </summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        _container = new MongoDbBenchmarkContainer();
        _container.Start();

        var client = new MongoClient(_container.ConnectionString);

        var options = Options.Create(new EncinaMongoDbOptions
        {
            ConnectionString = _container.ConnectionString,
            DatabaseName = "encina_bench"
        });

        _store = new OutboxStoreMongoDB(
            client,
            options,
            NullLogger<OutboxStoreMongoDB>.Instance);

        for (int i = 0; i < SeedMessageCount; i++)
        {
            var message = BenchmarkEntityFactory.CreateOutboxMessage();
            _store.AddAsync(message).GetAwaiter().GetResult();
        }
    }

    /// <summary>
    /// Drops the container.
    /// </summary>
    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _container?.Stop();
    }

    /// <summary>
    /// Measures the cost of inserting a single outbox message — the per-command tax that
    /// every transactional publish pays when the outbox is enabled.
    /// </summary>
    /// <returns>The Either result from the store operation.</returns>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("DocRef:bench:mongodb/outbox-insert")]
    public object AddAsync_Single()
    {
        var message = BenchmarkEntityFactory.CreateOutboxMessage();
        return _store.AddAsync(message).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Measures the cost of reading a 100-row pending batch — the per-tick cost of the
    /// outbox publisher background service when under steady load.
    /// </summary>
    /// <returns>The Either result containing the fetched messages.</returns>
    [Benchmark]
    [BenchmarkCategory("DocRef:bench:mongodb/outbox-query-pending")]
    public object GetPendingMessagesAsync_Batch100()
    {
        return _store.GetPendingMessagesAsync(BatchSize, maxRetries: 3).GetAwaiter().GetResult();
    }
}
