using System.Data;
using Encina;
using Encina.Tenancy;
using LanguageExt;
using Microsoft.Extensions.Options;
using MySqlConnector;

namespace Encina.Dapper.MySQL.Tenancy;

/// <summary>
/// MySQL implementation of <see cref="ITenantConnectionFactory"/> for multi-tenant
/// database connection management.
/// </summary>
/// <remarks>
/// <para>
/// This factory creates <see cref="MySqlConnection"/> instances configured for the current
/// tenant's database based on their isolation strategy:
/// </para>
/// <list type="bullet">
/// <item><b>DatabasePerTenant:</b> Uses the tenant's specific connection string</item>
/// <item><b>SchemaPerTenant:</b> Uses the default connection string (schema is set elsewhere)</item>
/// <item><b>SharedSchema:</b> Uses the default connection string</item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Register in DI
/// services.AddScoped&lt;ITenantConnectionFactory, TenantConnectionFactory&gt;();
///
/// // Use in a service
/// public class DataService(ITenantConnectionFactory connectionFactory)
/// {
///     public async Task&lt;IEnumerable&lt;Order&gt;&gt; GetOrdersAsync(CancellationToken ct)
///     {
///         var connectionResult = await connectionFactory.CreateConnectionAsync(ct);
///         return connectionResult.Match(
///             Right: connection =&gt; connection.QueryAsync&lt;Order&gt;("SELECT * FROM Orders"),
///             Left: error =&gt; throw new InvalidOperationException(error.Message));
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
    /// <param name="options">The tenancy options containing default connection string.</param>
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
    public async ValueTask<Either<EncinaError, IDbConnection>> CreateConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connectionStringResult = await GetConnectionStringAsync(cancellationToken).ConfigureAwait(false);
        return connectionStringResult.Map(cs => (IDbConnection)new MySqlConnection(cs));
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IDbConnection>> CreateConnectionForTenantAsync(
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(tenantId);

        var tenant = await _tenantStore.GetTenantAsync(tenantId, cancellationToken).ConfigureAwait(false);

        if (tenant is null)
        {
            return EncinaError.New($"Tenant '{tenantId}' not found.");
        }

        return GetConnectionStringForTenant(tenant)
            .Map(cs => (IDbConnection)new MySqlConnection(cs));
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, string>> GetConnectionStringAsync(CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantProvider.GetCurrentTenantAsync(cancellationToken).ConfigureAwait(false);
        return GetConnectionStringForTenant(tenant);
    }

    private Either<EncinaError, string> GetConnectionStringForTenant(TenantInfo? tenant)
    {
        // If tenant has a dedicated database with its own connection string, use it
        if (tenant?.HasDedicatedDatabase == true &&
            !string.IsNullOrEmpty(tenant.ConnectionString))
        {
            return tenant.ConnectionString;
        }

        // Otherwise, use the default connection string
        if (string.IsNullOrEmpty(_options.DefaultConnectionString))
        {
            return EncinaError.New(
                "No connection string available. Either configure a tenant-specific connection string " +
                "or set TenancyOptions.DefaultConnectionString.");
        }

        return _options.DefaultConnectionString;
    }
}
