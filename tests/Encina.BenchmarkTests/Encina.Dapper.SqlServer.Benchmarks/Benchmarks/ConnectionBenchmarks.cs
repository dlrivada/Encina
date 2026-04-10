using BenchmarkDotNet.Attributes;
using Dapper;
using Encina.Dapper.SqlServer.Benchmarks.Infrastructure;
using Microsoft.Data.SqlClient;

namespace Encina.Dapper.SqlServer.Benchmarks.Benchmarks;

/// <summary>
/// Measures the baseline cost of opening a SQL Server connection and executing a trivial
/// Dapper <c>ExecuteScalar</c>. This captures the Dapper-over-Microsoft.Data.SqlClient
/// overhead compared to the raw ADO.NET equivalent in <c>Encina.ADO.SqlServer.Benchmarks</c>.
/// </summary>
[MemoryDiagnoser]
public class ConnectionBenchmarks
{
    private SqlServerBenchmarkContainer _container = null!;
    private string _connectionString = null!;

    /// <summary>
    /// Boots a SQL Server container once before the first iteration.
    /// </summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        _container = new SqlServerBenchmarkContainer();
        _container.Start();
        _connectionString = _container.ConnectionString;
    }

    /// <summary>
    /// Tears down the SQL Server container after the last iteration.
    /// </summary>
    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _container?.Stop();
    }

    /// <summary>
    /// Baseline: open a fresh <see cref="SqlConnection"/>, execute <c>SELECT 1</c> via
    /// Dapper's <see cref="SqlMapper.ExecuteScalar"/>, and dispose.
    /// </summary>
    /// <returns>The scalar result of <c>SELECT 1</c>.</returns>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("DocRef:bench:dapper-sqlserver/connection-open")]
    public object OpenAndExecuteScalar()
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        return connection.ExecuteScalar<int>("SELECT 1");
    }
}
