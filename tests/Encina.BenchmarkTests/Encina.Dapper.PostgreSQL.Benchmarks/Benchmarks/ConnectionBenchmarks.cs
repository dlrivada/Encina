using BenchmarkDotNet.Attributes;
using Dapper;
using Encina.Dapper.PostgreSQL.Benchmarks.Infrastructure;
using Npgsql;

namespace Encina.Dapper.PostgreSQL.Benchmarks.Benchmarks;

/// <summary>
/// Measures the baseline cost of opening a PostgreSQL connection and executing a trivial
/// Dapper <c>ExecuteScalar</c>. This captures the Dapper-over-Npgsql overhead compared to
/// the raw ADO.NET equivalent in <c>Encina.ADO.PostgreSQL.Benchmarks</c>.
/// </summary>
[MemoryDiagnoser]
public class ConnectionBenchmarks
{
    private PostgreSqlBenchmarkContainer _container = null!;
    private string _connectionString = null!;

    /// <summary>
    /// Boots a PostgreSQL container once before the first iteration.
    /// </summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        _container = new PostgreSqlBenchmarkContainer();
        _container.Start();
        _connectionString = _container.ConnectionString;
    }

    /// <summary>
    /// Tears down the PostgreSQL container after the last iteration.
    /// </summary>
    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _container?.Stop();
    }

    /// <summary>
    /// Baseline: open a fresh <see cref="NpgsqlConnection"/>, execute <c>SELECT 1</c> via
    /// Dapper's <see cref="SqlMapper.ExecuteScalar"/>, and dispose.
    /// </summary>
    /// <returns>The scalar result of <c>SELECT 1</c>.</returns>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("DocRef:bench:dapper-postgresql/connection-open")]
    public object OpenAndExecuteScalar()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();
        return connection.ExecuteScalar<int>("SELECT 1");
    }
}
