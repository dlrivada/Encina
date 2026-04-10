using BenchmarkDotNet.Attributes;
using Encina.MongoDB.Benchmarks.Infrastructure;
using Encina.MongoDB.Repository;
using MongoDB.Driver;

namespace Encina.MongoDB.Benchmarks.Benchmarks;

/// <summary>
/// Measures the cost of the core read (<c>GetByIdAsync</c>) and write (<c>AddAsync</c>) paths
/// on <see cref="FunctionalRepositoryMongoDB{TEntity, TId}"/>. These two operations dominate
/// the per-request cost of any repository-based command handler targeting MongoDB.
/// </summary>
[MemoryDiagnoser]
public class FunctionalRepositoryBenchmarks
{
    private MongoDbBenchmarkContainer _container = null!;
    private FunctionalRepositoryMongoDB<BenchmarkRepositoryEntity, Guid> _repository = null!;
    private Guid _seededId;

    /// <summary>
    /// Boots the container, resolves a typed collection, and seeds a single row whose ID is
    /// captured for the <c>GetByIdAsync</c> benchmark.
    /// </summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        _container = new MongoDbBenchmarkContainer();
        _container.Start();

        var client = new MongoClient(_container.ConnectionString);
        var database = client.GetDatabase("encina_bench");
        var collection = database.GetCollection<BenchmarkRepositoryEntity>("benchmark_entities");

        _repository = new FunctionalRepositoryMongoDB<BenchmarkRepositoryEntity, Guid>(
            collection,
            entity => entity.Id);

        var seedEntity = BenchmarkEntityFactory.CreateRepositoryEntity();
        _seededId = seedEntity.Id;
        _repository.AddAsync(seedEntity).GetAwaiter().GetResult();
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
    /// Measures a primary-key lookup against the pre-seeded row.
    /// </summary>
    /// <returns>The Either result containing the entity.</returns>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("DocRef:bench:mongodb/repo-get-by-id")]
    public object GetByIdAsync()
    {
        return _repository.GetByIdAsync(_seededId).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Measures the cost of inserting a new document via the functional repository.
    /// </summary>
    /// <returns>The Either result containing the inserted entity.</returns>
    [Benchmark]
    [BenchmarkCategory("DocRef:bench:mongodb/repo-add")]
    public object AddAsync()
    {
        var entity = BenchmarkEntityFactory.CreateRepositoryEntity();
        return _repository.AddAsync(entity).GetAwaiter().GetResult();
    }
}
