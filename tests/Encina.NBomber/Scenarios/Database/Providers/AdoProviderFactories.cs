using System.Data;
using Encina.Tenancy;
using Encina.TestInfrastructure.Fixtures;
using Encina.TestInfrastructure.Schemas;
using Microsoft.Data.Sqlite;
using Microsoft.Data.SqlClient;
using MySqlConnector;
using Npgsql;

namespace Encina.NBomber.Scenarios.Database;

/// <summary>
/// ADO.NET provider factory for SQLite.
/// SQLite does not require a container - uses in-memory or file-based database.
/// </summary>
public sealed class AdoSqliteProviderFactory : DatabaseProviderFactoryBase
{
    private SqliteConnection? _connection;
    private string _connectionString = "Data Source=:memory:";

    /// <inheritdoc />
    public override string ProviderName => "ado-sqlite";

    /// <inheritdoc />
    public override ProviderCategory Category => ProviderCategory.ADO;

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
/// ADO.NET provider factory for SQL Server using Testcontainers.
/// </summary>
public sealed class AdoSqlServerProviderFactory : DatabaseProviderFactoryBase
{
    private SqlServerFixture? _fixture;

    /// <inheritdoc />
    public override string ProviderName => "ado-sqlserver";

    /// <inheritdoc />
    public override ProviderCategory Category => ProviderCategory.ADO;

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
/// ADO.NET provider factory for PostgreSQL using Testcontainers.
/// </summary>
public sealed class AdoPostgreSqlProviderFactory : DatabaseProviderFactoryBase
{
    private PostgreSqlFixture? _fixture;

    /// <inheritdoc />
    public override string ProviderName => "ado-postgresql";

    /// <inheritdoc />
    public override ProviderCategory Category => ProviderCategory.ADO;

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
/// ADO.NET provider factory for MySQL using Testcontainers.
/// </summary>
public sealed class AdoMySqlProviderFactory : DatabaseProviderFactoryBase
{
    private MySqlFixture? _fixture;

    /// <inheritdoc />
    public override string ProviderName => "ado-mysql";

    /// <inheritdoc />
    public override ProviderCategory Category => ProviderCategory.ADO;

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

/// <summary>
/// Simple in-memory tenant provider for load testing.
/// Thread-safe implementation using AsyncLocal for per-async-context tenant isolation.
/// </summary>
internal sealed class InMemoryTenantProvider : ITenantProvider
{
    private static readonly AsyncLocal<string?> _tenantId = new();

    /// <summary>
    /// Sets the current tenant ID for the async context.
    /// </summary>
    /// <param name="tenantId">The tenant ID to set.</param>
    public static void SetTenant(string tenantId)
    {
        _tenantId.Value = tenantId;
    }

    /// <summary>
    /// Clears the current tenant ID.
    /// </summary>
    public static void ClearTenant()
    {
        _tenantId.Value = null;
    }

    /// <inheritdoc />
    public string? GetCurrentTenantId() => _tenantId.Value;

    /// <inheritdoc />
    public ValueTask<TenantInfo?> GetCurrentTenantAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        if (string.IsNullOrEmpty(tenantId))
        {
            return ValueTask.FromResult<TenantInfo?>(null);
        }

        // Create a minimal TenantInfo for load testing (using shared schema strategy)
        var tenantInfo = new TenantInfo(
            tenantId,
            tenantId,
            TenantIsolationStrategy.SharedSchema);
        return ValueTask.FromResult<TenantInfo?>(tenantInfo);
    }
}
