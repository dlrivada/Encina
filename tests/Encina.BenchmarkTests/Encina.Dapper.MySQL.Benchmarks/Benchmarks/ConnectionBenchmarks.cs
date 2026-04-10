using BenchmarkDotNet.Attributes;
using Dapper;
using Encina.Dapper.MySQL.Benchmarks.Infrastructure;
using MySqlConnector;

namespace Encina.Dapper.MySQL.Benchmarks.Benchmarks;

/// <summary>
/// Measures the baseline cost of opening a MySQL connection and executing a trivial Dapper
/// <c>ExecuteScalar</c>. This captures the Dapper-over-MySqlConnector overhead compared to
/// the raw ADO.NET equivalent in <c>Encina.ADO.MySQL.Benchmarks</c>.
/// </summary>
[MemoryDiagnoser]
public class ConnectionBenchmarks
{
    private MySqlBenchmarkContainer _container = null!;
    private string _connectionString = null!;

    /// <summary>
    /// Boots a MySQL container once before the first iteration.
    /// </summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        _container = new MySqlBenchmarkContainer();
        _container.Start();
        _connectionString = _container.ConnectionString;
    }

    /// <summary>
    /// Tears down the MySQL container after the last iteration.
    /// </summary>
    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _container?.Stop();
    }

    /// <summary>
    /// Baseline: open a fresh <see cref="MySqlConnection"/>, execute <c>SELECT 1</c> via
    /// Dapper's <see cref="SqlMapper.ExecuteScalar"/>, and dispose.
    /// </summary>
    /// <returns>The scalar result of <c>SELECT 1</c>.</returns>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("DocRef:bench:dapper-mysql/connection-open")]
    public object OpenAndExecuteScalar()
    {
        using var connection = new MySqlConnection(_connectionString);
        connection.Open();
        return connection.ExecuteScalar<int>("SELECT 1");
    }
}
