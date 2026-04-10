using BenchmarkDotNet.Attributes;
using Encina.ADO.Benchmarks.Infrastructure;
using Encina.ADO.MySQL.Benchmarks.Infrastructure;
using Encina.ADO.MySQL.BulkOperations;
using MySqlConnector;

namespace Encina.ADO.MySQL.Benchmarks.Benchmarks;

/// <summary>
/// Measures the cost of <see cref="BulkOperationsMySQL{TEntity, TId}.BulkInsertAsync"/>
/// backed by <c>MySqlBulkCopy</c>. This is the fast-path for import/replay/seeding workloads
/// where the single-row repository INSERT would be orders of magnitude slower.
/// </summary>
[MemoryDiagnoser]
public class BulkOperationsBenchmarks
{
    private const int RowCount = 100;

    private MySqlBenchmarkContainer _container = null!;
    private MySqlConnection _connection = null!;
    private BulkOperationsMySQL<BenchmarkRepositoryEntity, Guid> _bulk = null!;
    private List<BenchmarkRepositoryEntity> _entities = null!;

    /// <summary>
    /// Boots the container, creates the benchmark-entity table, and pre-builds the row set
    /// so the benchmark only measures the bulk-copy round-trip (not materialization).
    /// </summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        _container = new MySqlBenchmarkContainer();
        _container.Start();

        _connection = new MySqlConnection(_container.ConnectionString);
        _connection.Open();
        AdoSchemaBuilder.CreateBenchmarkEntityTable(_connection, DatabaseProvider.MySql);

        _bulk = new BulkOperationsMySQL<BenchmarkRepositoryEntity, Guid>(
            _connection,
            new BenchmarkRepositoryEntityMySqlMapping());

        _entities = BenchmarkEntityFactory.CreateRepositoryEntities(RowCount);
    }

    /// <summary>
    /// Regenerates a fresh row set before each iteration so that repeat inserts don't
    /// collide on primary keys.
    /// </summary>
    [IterationSetup]
    public void IterationSetup()
    {
        _entities = BenchmarkEntityFactory.CreateRepositoryEntities(RowCount);
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
    /// Measures a 100-row bulk insert via <see cref="BulkOperationsMySQL{TEntity, TId}.BulkInsertAsync"/>.
    /// </summary>
    /// <returns>The Either result containing the inserted row count.</returns>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("DocRef:bench:ado-mysql/bulk-insert-100")]
    public object BulkInsert_100Rows()
    {
        return _bulk.BulkInsertAsync(_entities).GetAwaiter().GetResult();
    }
}
