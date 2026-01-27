using System.Data;
using Encina.Tenancy;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using Microsoft.Data.Sqlite;

namespace Encina.NBomber.Scenarios.Database;

/// <summary>
/// EF Core provider factory for SQLite.
/// SQLite does not require a container - uses in-memory or file-based database.
/// </summary>
public sealed class EFCoreSqliteProviderFactory : DatabaseProviderFactoryBase
{
    private EFCoreSqliteFixture? _fixture;

    /// <inheritdoc />
    public override string ProviderName => "efcore-sqlite";

    /// <inheritdoc />
    public override ProviderCategory Category => ProviderCategory.EFCore;

    /// <inheritdoc />
    public override DatabaseType DatabaseType => DatabaseType.Sqlite;

    /// <inheritdoc />
    public override string ConnectionString => _fixture?.ConnectionString ?? string.Empty;

    /// <inheritdoc />
    protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
    {
        _fixture = new EFCoreSqliteFixture();
        await _fixture.InitializeAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override IDbConnection CreateConnection()
    {
        EnsureInitialized();
        var connection = new SqliteConnection(ConnectionString);
        connection.Open();
        return connection;
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
/// EF Core provider factory for SQL Server using Testcontainers.
/// </summary>
public sealed class EFCoreSqlServerProviderFactory : DatabaseProviderFactoryBase
{
    private EFCoreSqlServerFixture? _fixture;

    /// <inheritdoc />
    public override string ProviderName => "efcore-sqlserver";

    /// <inheritdoc />
    public override ProviderCategory Category => ProviderCategory.EFCore;

    /// <inheritdoc />
    public override DatabaseType DatabaseType => DatabaseType.SqlServer;

    /// <inheritdoc />
    public override string ConnectionString => _fixture?.ConnectionString ?? string.Empty;

    /// <inheritdoc />
    protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
    {
        _fixture = new EFCoreSqlServerFixture();
        await _fixture.InitializeAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override IDbConnection CreateConnection()
    {
        EnsureInitialized();
        var connection = new Microsoft.Data.SqlClient.SqlConnection(ConnectionString);
        connection.Open();
        return connection;
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
/// EF Core provider factory for PostgreSQL using Testcontainers.
/// </summary>
public sealed class EFCorePostgreSqlProviderFactory : DatabaseProviderFactoryBase
{
    private EFCorePostgreSqlFixture? _fixture;

    /// <inheritdoc />
    public override string ProviderName => "efcore-postgresql";

    /// <inheritdoc />
    public override ProviderCategory Category => ProviderCategory.EFCore;

    /// <inheritdoc />
    public override DatabaseType DatabaseType => DatabaseType.PostgreSQL;

    /// <inheritdoc />
    public override string ConnectionString => _fixture?.ConnectionString ?? string.Empty;

    /// <inheritdoc />
    protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
    {
        _fixture = new EFCorePostgreSqlFixture();
        await _fixture.InitializeAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override IDbConnection CreateConnection()
    {
        EnsureInitialized();
        var connection = new Npgsql.NpgsqlConnection(ConnectionString);
        connection.Open();
        return connection;
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
/// EF Core provider factory for MySQL using Testcontainers.
/// Note: Pomelo.EntityFrameworkCore.MySql v10.0.0 is not yet available.
/// </summary>
public sealed class EFCoreMySqlProviderFactory : DatabaseProviderFactoryBase
{
    private EFCoreMySqlFixture? _fixture;

    /// <inheritdoc />
    public override string ProviderName => "efcore-mysql";

    /// <inheritdoc />
    public override ProviderCategory Category => ProviderCategory.EFCore;

    /// <inheritdoc />
    public override DatabaseType DatabaseType => DatabaseType.MySQL;

    /// <inheritdoc />
    public override string ConnectionString => _fixture?.ConnectionString ?? string.Empty;

    /// <inheritdoc />
    protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
    {
        _fixture = new EFCoreMySqlFixture();
        await _fixture.InitializeAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override IDbConnection CreateConnection()
    {
        EnsureInitialized();
        var connection = new MySqlConnector.MySqlConnection(ConnectionString);
        connection.Open();
        return connection;
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
