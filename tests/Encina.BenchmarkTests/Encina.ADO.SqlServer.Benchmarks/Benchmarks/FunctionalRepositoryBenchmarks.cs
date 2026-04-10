using BenchmarkDotNet.Attributes;
using Encina.ADO.Benchmarks.Infrastructure;
using Encina.ADO.SqlServer.Benchmarks.Infrastructure;
using Encina.ADO.SqlServer.Repository;
using Microsoft.Data.SqlClient;

namespace Encina.ADO.SqlServer.Benchmarks.Benchmarks;

/// <summary>
/// Measures the cost of the core read (<c>GetByIdAsync</c>) and write (<c>AddAsync</c>) paths
/// on <see cref="FunctionalRepositoryADO{TEntity, TId}"/> against SQL Server. These two
/// operations dominate the per-request cost of any repository-based command handler.
/// </summary>
[MemoryDiagnoser]
public class FunctionalRepositoryBenchmarks
{
    private SqlServerBenchmarkContainer _container = null!;
    private SqlConnection _connection = null!;
    private FunctionalRepositoryADO<BenchmarkRepositoryEntity, Guid> _repository = null!;
    private Guid _seededId;

    /// <summary>
    /// Boots the container, creates the benchmark-entity table, and seeds a single row
    /// whose ID is captured for the <c>GetByIdAsync</c> benchmark.
    /// </summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        _container = new SqlServerBenchmarkContainer();
        _container.Start();

        _connection = new SqlConnection(_container.ConnectionString);
        _connection.Open();
        AdoSchemaBuilder.CreateBenchmarkEntityTable(_connection, DatabaseProvider.SqlServer);

        _repository = new FunctionalRepositoryADO<BenchmarkRepositoryEntity, Guid>(
            _connection,
            new BenchmarkRepositoryEntitySqlServerMapping());

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
    [BenchmarkCategory("DocRef:bench:ado-sqlserver/repo-get-by-id")]
    public object GetByIdAsync()
    {
        return _repository.GetByIdAsync(_seededId).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Measures the cost of inserting a new row via the functional repository.
    /// </summary>
    /// <returns>The Either result containing the inserted entity.</returns>
    [Benchmark]
    [BenchmarkCategory("DocRef:bench:ado-sqlserver/repo-add")]
    public object AddAsync()
    {
        var entity = BenchmarkEntityFactory.CreateRepositoryEntity();
        return _repository.AddAsync(entity).GetAwaiter().GetResult();
    }
}
