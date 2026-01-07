using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Testcontainers.PostgreSql;

namespace Encina.Aspire.POC.Tests.Benchmarks;

/// <summary>
/// Benchmarks comparing container initialization and test execution time
/// between Aspire.Hosting.Testing and Testcontainers approaches.
/// </summary>
/// <remarks>
/// <para>
/// This benchmark measures:
/// <list type="bullet">
/// <item><description>Container startup time</description></item>
/// <item><description>Basic CRUD operation execution time</description></item>
/// <item><description>Memory allocation patterns</description></item>
/// </list>
/// </para>
/// <para>
/// Note: Aspire orchestration has additional overhead compared to direct
/// Testcontainers usage because it manages the entire distributed application
/// lifecycle, not just individual containers.
/// </para>
/// </remarks>
[SimpleJob(RuntimeMoniker.Net90)]  // Use .NET 9 moniker until BenchmarkDotNet supports .NET 10
[MemoryDiagnoser]
[RankColumn]
[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "BenchmarkDotNet requires instance methods")]
public class ContainerStartupBenchmark
{
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(60);

    #region Testcontainers Benchmarks

    /// <summary>
    /// Measures Testcontainers PostgreSQL container startup time.
    /// </summary>
    [Benchmark(Baseline = true, Description = "Testcontainers: Startup")]
    public async Task<string> Testcontainers_Startup()
    {
        var container = new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase("benchmark_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithCleanUp(true)
            .Build();

        try
        {
            await container.StartAsync();
            return container.GetConnectionString();
        }
        finally
        {
            await container.StopAsync();
            await container.DisposeAsync();
        }
    }

    /// <summary>
    /// Measures Testcontainers startup + CRUD operations.
    /// </summary>
    [Benchmark(Description = "Testcontainers: Startup + CRUD")]
    public async Task Testcontainers_StartupAndCrud()
    {
        var container = new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase("benchmark_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithCleanUp(true)
            .Build();

        try
        {
            await container.StartAsync();
            var connectionString = container.GetConnectionString();

            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();

            // Create table
            await connection.ExecuteAsync("""
                CREATE TABLE IF NOT EXISTS benchmark_entities (
                    id SERIAL PRIMARY KEY,
                    name VARCHAR(100) NOT NULL
                )
                """);

            // Insert
            var id = await connection.ExecuteScalarAsync<int>("""
                INSERT INTO benchmark_entities (name) VALUES ('Test') RETURNING id
                """);

            // Select
            _ = await connection.QuerySingleAsync<string>(
                "SELECT name FROM benchmark_entities WHERE id = @Id",
                new { Id = id });

            // Update
            await connection.ExecuteAsync(
                "UPDATE benchmark_entities SET name = 'Updated' WHERE id = @Id",
                new { Id = id });

            // Delete
            await connection.ExecuteAsync(
                "DELETE FROM benchmark_entities WHERE id = @Id",
                new { Id = id });
        }
        finally
        {
            await container.StopAsync();
            await container.DisposeAsync();
        }
    }

    #endregion

    #region Aspire Benchmarks

    /// <summary>
    /// Measures Aspire.Hosting.Testing PostgreSQL startup time.
    /// </summary>
    [Benchmark(Description = "Aspire: Startup")]
    public async Task<string> Aspire_Startup()
    {
        // Create the distributed application testing builder using our TestAppHost entry point
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<TestAppHost>();

        DistributedApplication? app = null;
        try
        {
            // Build and start the application
            app = await builder.BuildAsync();
            await app.StartAsync().WaitAsync(StartupTimeout);

            // Wait for database to be ready
            await app.ResourceNotifications
                .WaitForResourceAsync(TestAppHost.PostgresDatabaseName, KnownResourceStates.Running)
                .WaitAsync(StartupTimeout);

            // Get connection string
            return await app.GetConnectionStringAsync(TestAppHost.PostgresDatabaseName)
                ?? throw new InvalidOperationException("Connection string is null");
        }
        finally
        {
            if (app is not null)
            {
                await app.StopAsync();
                await app.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Measures Aspire startup + CRUD operations.
    /// </summary>
    [Benchmark(Description = "Aspire: Startup + CRUD")]
    public async Task Aspire_StartupAndCrud()
    {
        // Create the distributed application testing builder using our TestAppHost entry point
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<TestAppHost>();

        DistributedApplication? app = null;
        try
        {
            // Build and start the application
            app = await builder.BuildAsync();
            await app.StartAsync().WaitAsync(StartupTimeout);

            // Wait for database to be ready
            await app.ResourceNotifications
                .WaitForResourceAsync(TestAppHost.PostgresDatabaseName, KnownResourceStates.Running)
                .WaitAsync(StartupTimeout);

            // Get connection string
            var connectionString = await app.GetConnectionStringAsync(TestAppHost.PostgresDatabaseName);

            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();

            // Create table
            await connection.ExecuteAsync("""
                CREATE TABLE IF NOT EXISTS benchmark_entities (
                    id SERIAL PRIMARY KEY,
                    name VARCHAR(100) NOT NULL
                )
                """);

            // Insert
            var id = await connection.ExecuteScalarAsync<int>("""
                INSERT INTO benchmark_entities (name) VALUES ('Test') RETURNING id
                """);

            // Select
            _ = await connection.QuerySingleAsync<string>(
                "SELECT name FROM benchmark_entities WHERE id = @Id",
                new { Id = id });

            // Update
            await connection.ExecuteAsync(
                "UPDATE benchmark_entities SET name = 'Updated' WHERE id = @Id",
                new { Id = id });

            // Delete
            await connection.ExecuteAsync(
                "DELETE FROM benchmark_entities WHERE id = @Id",
                new { Id = id });
        }
        finally
        {
            if (app is not null)
            {
                await app.StopAsync();
                await app.DisposeAsync();
            }
        }
    }

    #endregion
}
