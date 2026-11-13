using Encina;
using LanguageExt;
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
/// <para><b>Connection String Resolution (fail closed):</b></para>
/// <list type="number">
/// <item>The tenant id is the explicit <c>tenantId</c> argument, or else <see cref="ITenantProvider.GetCurrentTenantId"/>.
/// A null, empty or whitespace id means no tenant.</item>
/// <item>No tenant: <see cref="TenancyErrors.TenantRequired"/> when <see cref="TenancyOptions.RequireTenant"/> is
/// <c>true</c> (the default); <see cref="TenantConnectionOptions.DefaultConnectionString"/> only when it is <c>false</c>.</item>
/// <item>A tenant id the <see cref="ITenantStore"/> does not know: <see cref="TenancyErrors.TenantNotFound"/>, and no
/// connection is created. There is no opt-out.</item>
/// <item>A known tenant: its <see cref="TenantInfo.ConnectionString"/>, or
/// <see cref="TenantConnectionOptions.DefaultConnectionString"/> when it has none.</item>
/// </list>
/// <para>An exception thrown by the tenant store propagates (ADR-006); it never falls back to the default.</para>
/// </remarks>
/// <example>
/// <code>
/// public class SqlServerTenantConnectionFactory : TenantConnectionFactoryBase&lt;SqlConnection&gt;
/// {
///     public SqlServerTenantConnectionFactory(
///         ITenantProvider tenantProvider,
///         ITenantStore tenantStore,
///         IOptions&lt;TenantConnectionOptions&gt; options,
///         IOptions&lt;TenancyOptions&gt; tenancyOptions)
///         : base(tenantProvider, tenantStore, options, tenancyOptions)
///     {
///     }
///
///     // CreateConnectionCoreAsync stays as ValueTask&lt;T&gt; (no Either) since
///     // the base class wraps the result in Either at the CreateConnectionAsync level.
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
    private readonly bool _requireTenant;

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
    /// <param name="tenancyOptions">
    /// The tenancy options; <see cref="TenancyOptions.RequireTenant"/> decides whether a missing tenant is refused.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any parameter is null.
    /// </exception>
    protected TenantConnectionFactoryBase(
        ITenantProvider tenantProvider,
        ITenantStore tenantStore,
        IOptions<TenantConnectionOptions> options,
        IOptions<TenancyOptions> tenancyOptions)
    {
        ArgumentNullException.ThrowIfNull(tenantProvider);
        ArgumentNullException.ThrowIfNull(tenantStore);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(tenancyOptions);

        _tenantProvider = tenantProvider;
        _tenantStore = tenantStore;
        Options = options.Value;
        _requireTenant = tenancyOptions.Value.RequireTenant;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, TConnection>> CreateConnectionAsync(
        string? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        var connectionStringResult = await GetConnectionStringAsync(tenantId, cancellationToken);

        return await connectionStringResult.MatchAsync<Either<EncinaError, TConnection>>(
            RightAsync: async cs =>
            {
                var connection = await CreateConnectionCoreAsync(cs, cancellationToken);
                return connection;
            },
            Left: error => error);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, string>> GetConnectionStringAsync(
        string? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        // Use provided tenant ID or get from current context
        tenantId ??= _tenantProvider.GetCurrentTenantId();

        // No tenant: refused unless RequireTenant was explicitly turned off (fail closed)
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return _requireTenant ? TenancyErrors.TenantRequired() : GetDefaultConnectionString();
        }

        // Get tenant info from store; a store exception propagates (ADR-006)
        var tenant = await _tenantStore.GetTenantAsync(tenantId, cancellationToken);

        // An unknown tenant never reaches the default database
        if (tenant is null)
        {
            return TenancyErrors.TenantNotFound();
        }

        // A known tenant uses its own connection string, or the default when it has none
        return string.IsNullOrWhiteSpace(tenant.ConnectionString)
            ? GetDefaultConnectionString()
            : tenant.ConnectionString;
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

    private Either<EncinaError, string> GetDefaultConnectionString()
    {
        if (!string.IsNullOrWhiteSpace(Options.DefaultConnectionString))
        {
            return Options.DefaultConnectionString;
        }

        return EncinaError.New(
            "No tenant context and no default connection string configured. " +
            "Either provide a tenant ID, ensure a tenant context is available, " +
            "or configure TenantConnectionOptions.DefaultConnectionString.");
    }
}
