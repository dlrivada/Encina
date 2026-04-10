using BenchmarkDotNet.Attributes;
using Encina.Dapper.Benchmarks.Infrastructure;
using Encina.Dapper.PostgreSQL.Benchmarks.Infrastructure;
using Encina.Dapper.PostgreSQL.Repository;
using Npgsql;

namespace Encina.Dapper.PostgreSQL.Benchmarks.Benchmarks;

/// <summary>
/// Measures the cost of the core read (<c>GetByIdAsync</c>) and write (<c>AddAsync</c>) paths
/// on <see cref="FunctionalRepositoryDapper{TEntity, TId}"/> against PostgreSQL. These two
/// operations dominate the per-request cost of any repository-based command handler.
/// </summary>
[MemoryDiagnoser]
public class FunctionalRepositoryBenchmarks
{
    private PostgreSqlBenchmarkContainer _container = null!;
    private NpgsqlConnection _connection = null!;
    private FunctionalRepositoryDapper<BenchmarkRepositoryEntity, Guid> _repository = null!;
    private Guid _seededId;

    /// <summary>
    /// Boots the container, creates the benchmark-entity table, and seeds a single row
    /// whose ID is captured for the <c>GetByIdAsync</c> benchmark.
    /// </summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        _container = new PostgreSqlBenchmarkContainer();
        _container.Start();

        _connection = new NpgsqlConnection(_container.ConnectionString);
        _connection.Open();
        DapperSchemaBuilder.CreateBenchmarkEntityTable(_connection, DatabaseProvider.PostgreSql);

        _repository = new FunctionalRepositoryDapper<BenchmarkRepositoryEntity, Guid>(
            _connection,
            new BenchmarkRepositoryEntityPostgreSqlMapping());

        var seedEntity = BenchmarkEntityFactory.CreateRepositoryEntity();
        _seededId = seedEntity.Id;
        _repository.AddAsync(seedEntity).GetAwaiter().GetResult();
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
    /// Measures a primary-key lookup against the pre-seeded row.
    /// </summary>
    /// <returns>The Either result containing the entity.</returns>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("DocRef:bench:dapper-postgresql/repo-get-by-id")]
    public object GetByIdAsync()
    {
        return _repository.GetByIdAsync(_seededId).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Measures the cost of inserting a new row via the functional repository.
    /// </summary>
    /// <returns>The Either result containing the inserted entity.</returns>
    [Benchmark]
    [BenchmarkCategory("DocRef:bench:dapper-postgresql/repo-add")]
    public object AddAsync()
    {
        var entity = BenchmarkEntityFactory.CreateRepositoryEntity();
        return _repository.AddAsync(entity).GetAwaiter().GetResult();
    }
}
