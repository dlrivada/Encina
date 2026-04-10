using BenchmarkDotNet.Attributes;
using Encina.MongoDB.Benchmarks.Infrastructure;
using Encina.MongoDB.BulkOperations;
using MongoDB.Driver;

namespace Encina.MongoDB.Benchmarks.Benchmarks;

/// <summary>
/// Measures the cost of <see cref="BulkOperationsMongoDB{TEntity, TId}.BulkInsertAsync"/>
/// backed by MongoDB's native <c>BulkWrite</c> protocol. This is the fast-path for
/// import/replay/seeding workloads where the single-document repository INSERT would be
/// orders of magnitude slower.
/// </summary>
[MemoryDiagnoser]
public class BulkOperationsBenchmarks
{
    private const int RowCount = 100;

    private MongoDbBenchmarkContainer _container = null!;
    private BulkOperationsMongoDB<BenchmarkRepositoryEntity, Guid> _bulk = null!;
    private List<BenchmarkRepositoryEntity> _entities = null!;

    /// <summary>
    /// Boots the container, resolves a typed collection, and pre-builds the row set so the
    /// benchmark only measures the bulk-write round-trip (not materialization).
    /// </summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        _container = new MongoDbBenchmarkContainer();
        _container.Start();

        var client = new MongoClient(_container.ConnectionString);
        var database = client.GetDatabase("encina_bench");
        var collection = database.GetCollection<BenchmarkRepositoryEntity>("benchmark_entities_bulk");

        _bulk = new BulkOperationsMongoDB<BenchmarkRepositoryEntity, Guid>(
            collection,
            entity => entity.Id);

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
    /// Drops the container.
    /// </summary>
    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _container?.Stop();
    }

    /// <summary>
    /// Measures a 100-document bulk insert via
    /// <see cref="BulkOperationsMongoDB{TEntity, TId}.BulkInsertAsync"/>.
    /// </summary>
    /// <returns>The Either result containing the inserted document count.</returns>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("DocRef:bench:mongodb/bulk-insert-100")]
    public object BulkInsert_100Rows()
    {
        return _bulk.BulkInsertAsync(_entities).GetAwaiter().GetResult();
    }
}
