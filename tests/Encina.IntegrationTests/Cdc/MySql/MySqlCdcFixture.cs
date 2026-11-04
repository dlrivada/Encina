using Encina.TestInfrastructure.Fixtures;
using MySqlConnector;
using Testcontainers.MySql;

namespace Encina.IntegrationTests.Cdc.MySql;

/// <summary>Connection facts of one MySQL server started by <see cref="MySqlCdcFixture"/>.</summary>
/// <param name="Host">The host the container is published on.</param>
/// <param name="Port">The published TCP port.</param>
/// <param name="ConnectionString">Connection string of the replication user (<c>REPLICATION SLAVE</c>, <c>REPLICATION CLIENT</c>).</param>
/// <param name="AdminConnectionString">Connection string of <c>root</c>, used to run DDL and DML.</param>
public sealed record MySqlCdcServer(string Host, int Port, string ConnectionString, string AdminConnectionString);

/// <summary>
/// Shared MySQL containers started with the binary log in ROW format and GTIDs enabled, which the
/// binlog replication connector needs. The standard <c>MySqlFixture</c> starts without those flags,
/// so CDC tests own this collection fixture. It starts MySQL 8.4 (LTS) and the repository's 9.x image
/// (<see cref="MySqlFixture.Mysql9Image"/>) with it, and a MySQL 8.4 server with binary logging switched
/// off on first use.
/// </summary>
public sealed class MySqlCdcFixture : IAsyncLifetime
{
    /// <summary>Key of the MySQL 8.4 LTS server.</summary>
    public const string Server84 = "8.4";

    /// <summary>Key of the MySQL 9.x server.</summary>
    public const string Server9 = "9.x";

    /// <summary>Key of the MySQL 8.4 server started with <c>--skip-log-bin</c>.</summary>
    public const string ServerNoBinlog = "8.4-no-binlog";

    /// <summary>The MySQL 8.4 LTS image.</summary>
    public const string Mysql84Image = "mysql:8.4";

    /// <summary>The database the tests write to.</summary>
    public const string DatabaseName = "cdc_it";

    /// <summary>The replication user the connector authenticates as.</summary>
    public const string ReplicationUser = "cdc_user";

    /// <summary>The password of <see cref="ReplicationUser"/> (test-only, never a real secret).</summary>
    public const string ReplicationPassword = "cdc_Password1!";

    private const string RootPassword = "mysql";

    private static readonly string[] BinlogFlags =
    [
        "--log-bin=mysql-bin",
        "--binlog-format=ROW",
        "--gtid-mode=ON",
        "--enforce-gtid-consistency=ON",
        "--server-id=1",
    ];

    private static readonly string[] NoBinlogFlags = ["--skip-log-bin", "--server-id=1"];

    private readonly Dictionary<string, MySqlCdcServer> _servers = [];
    private readonly List<MySqlContainer> _containers = [];
    private readonly Lock _containersLock = new();

    // The no-binlog server serves a single test, so it starts on first use instead of with the fixture.
    private readonly Lazy<Task<MySqlCdcServer>> _noBinlogServer;

    /// <summary>Initializes a new instance of the <see cref="MySqlCdcFixture"/> class.</summary>
    public MySqlCdcFixture()
    {
        _noBinlogServer = new Lazy<Task<MySqlCdcServer>>(
            () => StartServerAsync(Mysql84Image, NoBinlogFlags),
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <summary>
    /// Gets the server registered under <paramref name="key"/> (<see cref="Server84"/>, <see cref="Server9"/>
    /// or <see cref="ServerNoBinlog"/>); the no-binlog server is started by the first call that asks for it.
    /// </summary>
    public Task<MySqlCdcServer> GetServerAsync(string key) =>
        key == ServerNoBinlog ? _noBinlogServer.Value : Task.FromResult(_servers[key]);

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        var eager = await Task.WhenAll(
            StartServerAsync(Mysql84Image, BinlogFlags),
            StartServerAsync(MySqlFixture.Mysql9Image, BinlogFlags));

        _servers[Server84] = eager[0];
        _servers[Server9] = eager[1];
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        // A lazy server whose start faulted has nothing to wait for; its container is still disposed below.
        if (_noBinlogServer.IsValueCreated)
        {
            try
            {
                await _noBinlogServer.Value;
            }
            catch (Exception)
            {
                // The faulting start already failed the test that asked for it.
            }
        }

        MySqlContainer[] containers;
        lock (_containersLock)
        {
            containers = [.. _containers];
        }

        foreach (var container in containers)
        {
            await container.DisposeAsync();
        }
    }

    private async Task<MySqlCdcServer> StartServerAsync(string image, string[] flags)
    {
        var container = new MySqlBuilder(image)
            .WithDatabase(DatabaseName)
            .WithUsername("root")
            .WithPassword(RootPassword)
            .WithCommand(flags)
            .WithCleanUp(true)
            .Build();

        lock (_containersLock)
        {
            _containers.Add(container);
        }

        await container.StartAsync();

        var host = container.Hostname;
        var port = container.GetMappedPublicPort(3306);
        var admin = $"Server={host};Port={port};User ID=root;Password={RootPassword};Database={DatabaseName};SslMode=None;AllowPublicKeyRetrieval=true;Allow User Variables=true";
        var replication = $"Server={host};Port={port};User ID={ReplicationUser};Password={ReplicationPassword};Database={DatabaseName};SslMode=None;AllowPublicKeyRetrieval=true";
        var server = new MySqlCdcServer(host, port, replication, admin);

        // mysql_native_password is disabled in 8.4 and removed in 9.0, so the user authenticates with caching_sha2_password.
        await ExecuteAsync(server, $"CREATE USER IF NOT EXISTS '{ReplicationUser}'@'%' IDENTIFIED WITH caching_sha2_password BY '{ReplicationPassword}'");
        await ExecuteAsync(server, $"GRANT REPLICATION SLAVE, REPLICATION CLIENT, SELECT ON *.* TO '{ReplicationUser}'@'%'");
        return server;
    }

    /// <summary>Recreates the <c>cdc_orders</c> table empty on the server.</summary>
    public async Task ResetAsync(string key, CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(key, "DROP TABLE IF EXISTS cdc_orders", cancellationToken);
        await ExecuteAsync(
            key,
            "CREATE TABLE cdc_orders (id INT PRIMARY KEY, name VARCHAR(100) NOT NULL)",
            cancellationToken);
    }

    /// <summary>Executes a non-query statement as <c>root</c> on a fresh connection.</summary>
    public async Task ExecuteAsync(string key, string sql, CancellationToken cancellationToken = default) =>
        await ExecuteAsync(await GetServerAsync(key), sql, cancellationToken);

    private static async Task ExecuteAsync(MySqlCdcServer server, string sql, CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(server.AdminConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
