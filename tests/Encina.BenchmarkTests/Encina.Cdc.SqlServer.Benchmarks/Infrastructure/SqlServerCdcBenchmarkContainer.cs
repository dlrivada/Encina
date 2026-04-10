using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace Encina.Cdc.SqlServer.Benchmarks.Infrastructure;

/// <summary>
/// Spins up a SQL Server container and enables Change Tracking on a dedicated database so
/// the CDC <c>GetCurrentPositionAsync</c> benchmark can actually read
/// <c>CHANGE_TRACKING_CURRENT_VERSION()</c>.
/// </summary>
/// <remarks>
/// The Encina SQL Server CDC connector uses SQL Server's lightweight Change Tracking
/// feature (not the heavyweight Change Data Capture feature, which requires the SQL Server
/// Agent and is not available in the Linux MSSQL container). Change Tracking just needs
/// <c>ALTER DATABASE ... SET CHANGE_TRACKING = ON</c> and works out of the box on the
/// Developer edition that <c>mcr.microsoft.com/mssql/server:2022-latest</c> ships.
/// </remarks>
public sealed class SqlServerCdcBenchmarkContainer : IAsyncDisposable
{
    private const string DatabaseName = "EncinaCdcBench";

    private readonly MsSqlContainer _container;
    private string? _databaseConnectionString;

    /// <summary>
    /// Initializes a new container definition. The underlying Docker container is not started
    /// until <see cref="Start"/> is called.
    /// </summary>
    public SqlServerCdcBenchmarkContainer()
    {
        _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword("StrongP@ssw0rd!")
            .WithCleanUp(true)
            .Build();
    }

    /// <summary>
    /// Gets the connection string targeting the Change-Tracking-enabled database. Only
    /// valid after <see cref="Start"/>.
    /// </summary>
    public string ConnectionString =>
        _databaseConnectionString
        ?? throw new InvalidOperationException("Container has not been started.");

    /// <summary>
    /// Starts the container synchronously, creates a dedicated database, and enables Change
    /// Tracking on it. Safe to call from <c>[GlobalSetup]</c>.
    /// </summary>
    public void Start()
    {
        _container.StartAsync().GetAwaiter().GetResult();

        var masterConnectionString = _container.GetConnectionString();
        using (var connection = new SqlConnection(masterConnectionString))
        {
            connection.Open();
            using var createCmd = connection.CreateCommand();
            createCmd.CommandText =
                $"IF DB_ID('{DatabaseName}') IS NULL CREATE DATABASE [{DatabaseName}];";
            createCmd.ExecuteNonQuery();

            using var trackingCmd = connection.CreateCommand();
            trackingCmd.CommandText =
                $"ALTER DATABASE [{DatabaseName}] SET CHANGE_TRACKING = ON " +
                "(CHANGE_RETENTION = 2 DAYS, AUTO_CLEANUP = ON);";
            trackingCmd.ExecuteNonQuery();
        }

        var builder = new SqlConnectionStringBuilder(masterConnectionString)
        {
            InitialCatalog = DatabaseName
        };
        _databaseConnectionString = builder.ConnectionString;
    }

    /// <summary>
    /// Stops and removes the container synchronously. Safe to call from <c>[GlobalCleanup]</c>.
    /// </summary>
    public void Stop()
    {
        _container.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync().ConfigureAwait(false);
    }
}
