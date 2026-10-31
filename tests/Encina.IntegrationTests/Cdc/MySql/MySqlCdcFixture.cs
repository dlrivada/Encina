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
/// so CDC tests own this collection fixture. It starts MySQL 8.4 (LTS), the repository's 9.x image
/// (<see cref="MySqlFixture.Mysql9Image"/>) and a MySQL 8.4 server with binary logging switched off.
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

    private const string RootPassword = "mysql";
    private const string ReplicationUser = "cdc_user";
    private const string ReplicationPassword = "cdc_Password1!";

    private static readonly string[] BinlogFlags =
    [
        "--log-bin=mysql-bin",
        "--binlog-format=ROW",
        "--gtid-mode=ON",
        "--enforce-gtid-consistency=ON",
        "--server-id=1",
    ];

    private readonly Dictionary<string, MySqlContainer> _containers = [];
    private readonly Dictionary<string, MySqlCdcServer> _servers = [];

    /// <summary>Gets the server registered under <paramref name="key"/> (<see cref="Server84"/>, <see cref="Server9"/> or <see cref="ServerNoBinlog"/>).</summary>
    public MySqlCdcServer GetServer(string key) => _servers[key];

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        var definitions = new (string Key, string Image, string[] Flags)[]
        {
            (Server84, Mysql84Image, BinlogFlags),
            (Server9, MySqlFixture.Mysql9Image, BinlogFlags),
            (ServerNoBinlog, Mysql84Image, ["--skip-log-bin", "--server-id=1"]),
        };

        foreach (var (key, image, flags) in definitions)
        {
            _containers[key] = new MySqlBuilder(image)
                .WithDatabase(DatabaseName)
                .WithUsername("root")
                .WithPassword(RootPassword)
                .WithCommand(flags)
                .WithCleanUp(true)
                .Build();
        }

        await Task.WhenAll(_containers.Values.Select(c => c.StartAsync()));

        foreach (var (key, container) in _containers)
        {
            var host = container.Hostname;
            var port = container.GetMappedPublicPort(3306);
            var admin = $"Server={host};Port={port};User ID=root;Password={RootPassword};Database={DatabaseName};SslMode=None;AllowPublicKeyRetrieval=true;Allow User Variables=true";
            var replication = $"Server={host};Port={port};User ID={ReplicationUser};Password={ReplicationPassword};Database={DatabaseName};SslMode=None;AllowPublicKeyRetrieval=true";
            _servers[key] = new MySqlCdcServer(host, port, replication, admin);

            // mysql_native_password is disabled in 8.4 and removed in 9.0, so the user authenticates with caching_sha2_password.
            await ExecuteAsync(key, $"CREATE USER IF NOT EXISTS '{ReplicationUser}'@'%' IDENTIFIED WITH caching_sha2_password BY '{ReplicationPassword}'");
            await ExecuteAsync(key, $"GRANT REPLICATION SLAVE, REPLICATION CLIENT, SELECT ON *.* TO '{ReplicationUser}'@'%'");
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        foreach (var container in _containers.Values)
        {
            await container.DisposeAsync();
        }
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
    public async Task ExecuteAsync(string key, string sql, CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(GetServer(key).AdminConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
