using BenchmarkDotNet.Attributes;
using Encina.ADO.PostgreSQL.Benchmarks.Infrastructure;
using Npgsql;

namespace Encina.ADO.PostgreSQL.Benchmarks.Benchmarks;

/// <summary>
/// Measures the baseline cost of opening a PostgreSQL connection through Npgsql.
/// Establishes the floor below which no ADO.NET repository/store operation can go — everything
/// else in this assembly is paid on top of this number.
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
    /// Baseline: open a fresh <see cref="NpgsqlConnection"/>, execute <c>SELECT 1</c>, and dispose.
    /// This captures the per-operation connection-pool checkout cost plus a minimal round-trip.
    /// </summary>
    /// <returns>The scalar result of <c>SELECT 1</c>.</returns>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("DocRef:bench:ado-postgresql/connection-open")]
    public object OpenAndClose()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1";
        return command.ExecuteScalar() ?? 0;
    }
}
