using BenchmarkDotNet.Attributes;
using Encina.ADO.Benchmarks.Infrastructure;
using Encina.ADO.PostgreSQL.Benchmarks.Infrastructure;
using Encina.ADO.PostgreSQL.Outbox;
using Npgsql;

namespace Encina.ADO.PostgreSQL.Benchmarks.Benchmarks;

/// <summary>
/// Measures the cost of the Encina outbox pattern on PostgreSQL — both the "write" path
/// (<see cref="OutboxStoreADO.AddAsync"/>) and the scheduler "read" path
/// (<see cref="OutboxStoreADO.GetPendingMessagesAsync"/>). Both land on the hot path of
/// every transactional message-producing request.
/// </summary>
[MemoryDiagnoser]
public class OutboxStoreBenchmarks
{
    private const int SeedMessageCount = 100;
    private const int BatchSize = 100;

    private PostgreSqlBenchmarkContainer _container = null!;
    private NpgsqlConnection _connection = null!;
    private OutboxStoreADO _store = null!;

    /// <summary>
    /// Boots the container, creates the outbox schema, and seeds 100 pending messages
    /// so the read benchmark has realistic data to scan.
    /// </summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        _container = new PostgreSqlBenchmarkContainer();
        _container.Start();

        _connection = new NpgsqlConnection(_container.ConnectionString);
        _connection.Open();
        AdoSchemaBuilder.CreateOutboxTable(_connection, DatabaseProvider.PostgreSql);

        _store = new OutboxStoreADO(_connection);

        for (int i = 0; i < SeedMessageCount; i++)
        {
            var message = BenchmarkEntityFactory.CreateOutboxMessage();
            _store.AddAsync(message).GetAwaiter().GetResult();
        }
    }

    /// <summary>
    /// Drops the connection and the container.
    /// </summary>
    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _connection?.Dispose();
        _container?.Stop();
    }

    /// <summary>
    /// Measures the cost of inserting a single outbox message — the per-command tax that
    /// every transactional publish pays when the outbox is enabled.
    /// </summary>
    /// <returns>The Either result from the store operation.</returns>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("DocRef:bench:ado-postgresql/outbox-insert")]
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
    [BenchmarkCategory("DocRef:bench:ado-postgresql/outbox-query-pending")]
    public object GetPendingMessagesAsync_Batch100()
    {
        return _store.GetPendingMessagesAsync(BatchSize, maxRetries: 3).GetAwaiter().GetResult();
    }
}
