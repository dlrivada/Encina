using System.Data;
using Encina.Tenancy;
using Encina.TestInfrastructure.Fixtures;
using Encina.TestInfrastructure.Schemas;
using Microsoft.Data.Sqlite;

namespace Encina.NBomber.Scenarios.Database;

/// <summary>
/// Dapper provider factory for SQLite.
/// SQLite does not require a container - uses in-memory or file-based database.
/// </summary>
public sealed class DapperSqliteProviderFactory : DatabaseProviderFactoryBase
{
    private SqliteConnection? _connection;
    private string _connectionString = "Data Source=:memory:";

    /// <inheritdoc />
    public override string ProviderName => "dapper-sqlite";

    /// <inheritdoc />
    public override ProviderCategory Category => ProviderCategory.Dapper;

    /// <inheritdoc />
    public override DatabaseType DatabaseType => DatabaseType.Sqlite;

    /// <inheritdoc />
    public override string ConnectionString => _connectionString;

    /// <inheritdoc />
    protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
    {
        // SQLite in-memory requires keeping connection open
        _connection = new SqliteConnection(_connectionString);
        await _connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await SqliteSchema.CreateOutboxSchemaAsync(_connection).ConfigureAwait(false);
        await SqliteSchema.CreateInboxSchemaAsync(_connection).ConfigureAwait(false);
        await SqliteSchema.CreateSagaSchemaAsync(_connection).ConfigureAwait(false);
        await SqliteSchema.CreateSchedulingSchemaAsync(_connection).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override IDbConnection CreateConnection()
    {
        EnsureInitialized();
        // Return the same connection for SQLite in-memory
        return _connection!;
    }

    /// <inheritdoc />
    public override object? CreateUnitOfWork() => null;

    /// <inheritdoc />
    public override ITenantProvider CreateTenantProvider() => new InMemoryTenantProvider();

    /// <inheritdoc />
    public override object? CreateReadWriteSelector() => null;

    /// <inheritdoc />
    public override async Task ClearDataAsync(CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        await SqliteSchema.ClearAllDataAsync(_connection!).ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override async ValueTask DisposeCoreAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync().ConfigureAwait(false);
        }
    }
}

/// <summary>
/// Dapper provider factory for SQL Server using Testcontainers.
/// </summary>
public sealed class DapperSqlServerProviderFactory : DatabaseProviderFactoryBase
{
    private SqlServerFixture? _fixture;

    /// <inheritdoc />
    public override string ProviderName => "dapper-sqlserver";

    /// <inheritdoc />
    public override ProviderCategory Category => ProviderCategory.Dapper;

    /// <inheritdoc />
    public override DatabaseType DatabaseType => DatabaseType.SqlServer;

    /// <inheritdoc />
    public override string ConnectionString => _fixture?.ConnectionString ?? string.Empty;

    /// <inheritdoc />
    protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
    {
        _fixture = new SqlServerFixture();
        await _fixture.InitializeAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override IDbConnection CreateConnection()
    {
        EnsureInitialized();
        return _fixture!.CreateConnection();
    }

    /// <inheritdoc />
    public override object? CreateUnitOfWork() => null;

    /// <inheritdoc />
    public override ITenantProvider CreateTenantProvider() => new InMemoryTenantProvider();

    /// <inheritdoc />
    public override object? CreateReadWriteSelector() => null;

    /// <inheritdoc />
    public override async Task ClearDataAsync(CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        await _fixture!.ClearAllDataAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override async ValueTask DisposeCoreAsync()
    {
        if (_fixture is not null)
        {
            await _fixture.DisposeAsync().ConfigureAwait(false);
        }
    }
}

/// <summary>
/// Dapper provider factory for PostgreSQL using Testcontainers.
/// </summary>
public sealed class DapperPostgreSqlProviderFactory : DatabaseProviderFactoryBase
{
    private PostgreSqlFixture? _fixture;

    /// <inheritdoc />
    public override string ProviderName => "dapper-postgresql";

    /// <inheritdoc />
    public override ProviderCategory Category => ProviderCategory.Dapper;

    /// <inheritdoc />
    public override DatabaseType DatabaseType => DatabaseType.PostgreSQL;

    /// <inheritdoc />
    public override string ConnectionString => _fixture?.ConnectionString ?? string.Empty;

    /// <inheritdoc />
    protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
    {
        _fixture = new PostgreSqlFixture();
        await _fixture.InitializeAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override IDbConnection CreateConnection()
    {
        EnsureInitialized();
        return _fixture!.CreateConnection();
    }

    /// <inheritdoc />
    public override object? CreateUnitOfWork() => null;

    /// <inheritdoc />
    public override ITenantProvider CreateTenantProvider() => new InMemoryTenantProvider();

    /// <inheritdoc />
    public override object? CreateReadWriteSelector() => null;

    /// <inheritdoc />
    public override async Task ClearDataAsync(CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        await _fixture!.ClearAllDataAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override async ValueTask DisposeCoreAsync()
    {
        if (_fixture is not null)
        {
            await _fixture.DisposeAsync().ConfigureAwait(false);
        }
    }
}

/// <summary>
/// Dapper provider factory for MySQL using Testcontainers.
/// </summary>
public sealed class DapperMySqlProviderFactory : DatabaseProviderFactoryBase
{
    private MySqlFixture? _fixture;

    /// <inheritdoc />
    public override string ProviderName => "dapper-mysql";

    /// <inheritdoc />
    public override ProviderCategory Category => ProviderCategory.Dapper;

    /// <inheritdoc />
    public override DatabaseType DatabaseType => DatabaseType.MySQL;

    /// <inheritdoc />
    public override string ConnectionString => _fixture?.ConnectionString ?? string.Empty;

    /// <inheritdoc />
    protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
    {
        _fixture = new MySqlFixture();
        await _fixture.InitializeAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override IDbConnection CreateConnection()
    {
        EnsureInitialized();
        return _fixture!.CreateConnection();
    }

    /// <inheritdoc />
    public override object? CreateUnitOfWork() => null;

    /// <inheritdoc />
    public override ITenantProvider CreateTenantProvider() => new InMemoryTenantProvider();

    /// <inheritdoc />
    public override object? CreateReadWriteSelector() => null;

    /// <inheritdoc />
    public override async Task ClearDataAsync(CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        await _fixture!.ClearAllDataAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override async ValueTask DisposeCoreAsync()
    {
        if (_fixture is not null)
        {
            await _fixture.DisposeAsync().ConfigureAwait(false);
        }
    }
}
