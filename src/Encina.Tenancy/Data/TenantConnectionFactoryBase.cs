using Microsoft.Extensions.Options;

namespace Encina.Tenancy;

/// <summary>
/// Base implementation for tenant connection factories.
/// </summary>
/// <typeparam name="TConnection">The type of database connection.</typeparam>
/// <remarks>
/// <para>
/// This abstract base class provides common functionality for resolving
/// connection strings based on tenant context. Derived classes implement
/// the actual connection creation logic specific to their database provider.
/// </para>
/// <para><b>Connection String Resolution Order:</b></para>
/// <list type="number">
/// <item>Explicit <paramref name="tenantId"/> parameter</item>
/// <item>Current tenant from <see cref="ITenantProvider.GetCurrentTenantId"/></item>
/// <item>Tenant's <see cref="TenantInfo.ConnectionString"/> from store</item>
/// <item><see cref="TenantConnectionOptions.DefaultConnectionString"/></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// public class SqlServerTenantConnectionFactory : TenantConnectionFactoryBase&lt;SqlConnection&gt;
/// {
///     public SqlServerTenantConnectionFactory(
///         ITenantProvider tenantProvider,
///         ITenantStore tenantStore,
///         IOptions&lt;TenantConnectionOptions&gt; options)
///         : base(tenantProvider, tenantStore, options)
///     {
///     }
///
///     protected override async ValueTask&lt;SqlConnection&gt; CreateConnectionCoreAsync(
///         string connectionString,
///         CancellationToken cancellationToken)
///     {
///         var connection = new SqlConnection(connectionString);
///         if (Options.AutoOpenConnections)
///         {
///             await connection.OpenAsync(cancellationToken);
///         }
///         return connection;
///     }
/// }
/// </code>
/// </example>
public abstract class TenantConnectionFactoryBase<TConnection> : ITenantConnectionFactory<TConnection>
    where TConnection : class, IDisposable
{
    private readonly ITenantProvider _tenantProvider;
    private readonly ITenantStore _tenantStore;

    /// <summary>
    /// Gets the connection options.
    /// </summary>
    protected TenantConnectionOptions Options { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantConnectionFactoryBase{TConnection}"/> class.
    /// </summary>
    /// <param name="tenantProvider">The tenant provider.</param>
    /// <param name="tenantStore">The tenant store.</param>
    /// <param name="options">The connection options.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any parameter is null.
    /// </exception>
    protected TenantConnectionFactoryBase(
        ITenantProvider tenantProvider,
        ITenantStore tenantStore,
        IOptions<TenantConnectionOptions> options)
    {
        ArgumentNullException.ThrowIfNull(tenantProvider);
        ArgumentNullException.ThrowIfNull(tenantStore);
        ArgumentNullException.ThrowIfNull(options);

        _tenantProvider = tenantProvider;
        _tenantStore = tenantStore;
        Options = options.Value;
    }

    /// <inheritdoc />
    public async ValueTask<TConnection> CreateConnectionAsync(
        string? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        var connectionString = await GetConnectionStringAsync(tenantId, cancellationToken);
        return await CreateConnectionCoreAsync(connectionString, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<string> GetConnectionStringAsync(
        string? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        // Use provided tenant ID or get from current context
        tenantId ??= _tenantProvider.GetCurrentTenantId();

        // If no tenant context, use default connection string
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return GetDefaultConnectionStringOrThrow();
        }

        // Get tenant info from store
        var tenant = await _tenantStore.GetTenantAsync(tenantId, cancellationToken);

        // Use tenant's connection string if available
        if (tenant is not null && !string.IsNullOrWhiteSpace(tenant.ConnectionString))
        {
            return tenant.ConnectionString;
        }

        // Fall back to default connection string
        return GetDefaultConnectionStringOrThrow();
    }

    /// <summary>
    /// Creates the actual database connection.
    /// </summary>
    /// <param name="connectionString">The resolved connection string.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created and optionally opened connection.</returns>
    /// <remarks>
    /// Implementations should:
    /// <list type="bullet">
    /// <item>Create a new connection instance</item>
    /// <item>Open the connection if <see cref="TenantConnectionOptions.AutoOpenConnections"/> is true</item>
    /// <item>Handle any provider-specific configuration</item>
    /// </list>
    /// </remarks>
    protected abstract ValueTask<TConnection> CreateConnectionCoreAsync(
        string connectionString,
        CancellationToken cancellationToken);

    private string GetDefaultConnectionStringOrThrow()
    {
        if (!string.IsNullOrWhiteSpace(Options.DefaultConnectionString))
        {
            return Options.DefaultConnectionString;
        }

        if (Options.ThrowOnMissingConnectionString)
        {
            throw new InvalidOperationException(
                "No tenant context and no default connection string configured. " +
                "Either provide a tenant ID, ensure a tenant context is available, " +
                "or configure TenantConnectionOptions.DefaultConnectionString.");
        }

        // This shouldn't happen if ThrowOnMissingConnectionString is true,
        // but we need to satisfy the compiler
        throw new InvalidOperationException("No connection string available.");
    }
}
