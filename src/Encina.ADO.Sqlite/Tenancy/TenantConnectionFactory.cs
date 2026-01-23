using System.Data;
using Encina.Tenancy;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace Encina.ADO.Sqlite.Tenancy;

/// <summary>
/// SQLite implementation of <see cref="ITenantConnectionFactory"/> for ADO.NET.
/// </summary>
/// <remarks>
/// <para>
/// This factory creates database connections based on the current tenant context
/// and tenant isolation strategy:
/// </para>
/// <list type="bullet">
/// <item>
/// <term>SharedSchema</term>
/// <description>Uses the default connection string</description>
/// </item>
/// <item>
/// <term>SchemaPerTenant</term>
/// <description>Uses the default connection string (schema is handled separately)</description>
/// </item>
/// <item>
/// <term>DatabasePerTenant</term>
/// <description>Uses the tenant's specific connection string if available</description>
/// </item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Registration
/// services.AddEncinaADOSqliteWithTenancy(config =&gt; { });
///
/// // Usage
/// public class OrderService(ITenantConnectionFactory connectionFactory)
/// {
///     public async Task&lt;IDbConnection&gt; GetConnectionAsync(CancellationToken ct)
///     {
///         return await connectionFactory.CreateConnectionAsync(ct);
///     }
/// }
/// </code>
/// </example>
public sealed class TenantConnectionFactory : ITenantConnectionFactory
{
    private readonly ITenantProvider _tenantProvider;
    private readonly ITenantStore _tenantStore;
    private readonly TenancyOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantConnectionFactory"/> class.
    /// </summary>
    /// <param name="tenantProvider">The tenant provider for current tenant context.</param>
    /// <param name="tenantStore">The tenant store for retrieving tenant information.</param>
    /// <param name="options">The tenancy configuration options.</param>
    public TenantConnectionFactory(
        ITenantProvider tenantProvider,
        ITenantStore tenantStore,
        IOptions<TenancyOptions> options)
    {
        ArgumentNullException.ThrowIfNull(tenantProvider);
        ArgumentNullException.ThrowIfNull(tenantStore);
        ArgumentNullException.ThrowIfNull(options);

        _tenantProvider = tenantProvider;
        _tenantStore = tenantStore;
        _options = options.Value;
    }

    /// <inheritdoc/>
    public async ValueTask<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connectionString = await GetConnectionStringAsync(cancellationToken).ConfigureAwait(false);
        var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return connection;
    }

    /// <inheritdoc/>
    public async ValueTask<IDbConnection> CreateConnectionForTenantAsync(
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(tenantId);

        var tenant = await _tenantStore.GetTenantAsync(tenantId, cancellationToken).ConfigureAwait(false);

        if (tenant is null)
        {
            throw new InvalidOperationException($"Tenant '{tenantId}' not found.");
        }

        var connectionString = GetConnectionStringForTenant(tenant);
        var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return connection;
    }

    /// <inheritdoc/>
    public async ValueTask<string> GetConnectionStringAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();

        // No tenant context - use default
        if (string.IsNullOrEmpty(tenantId))
        {
            return GetDefaultConnectionString();
        }

        var tenant = await _tenantStore.GetTenantAsync(tenantId, cancellationToken).ConfigureAwait(false);

        // Tenant not found - use default
        if (tenant is null)
        {
            return GetDefaultConnectionString();
        }

        return GetConnectionStringForTenant(tenant);
    }

    private string GetConnectionStringForTenant(TenantInfo tenant)
    {
        // Only DatabasePerTenant strategy uses tenant-specific connection strings
        if (tenant.Strategy == TenantIsolationStrategy.DatabasePerTenant)
        {
            // Use tenant's connection string if available, otherwise fall back to default
            if (!string.IsNullOrEmpty(tenant.ConnectionString))
            {
                return tenant.ConnectionString;
            }
        }

        // SharedSchema and SchemaPerTenant use the default connection string
        return GetDefaultConnectionString();
    }

    private string GetDefaultConnectionString()
    {
        if (string.IsNullOrEmpty(_options.DefaultConnectionString))
        {
            throw new InvalidOperationException(
                "No default connection string configured. " +
                "Set TenancyOptions.DefaultConnectionString in your configuration.");
        }

        return _options.DefaultConnectionString;
    }
}
