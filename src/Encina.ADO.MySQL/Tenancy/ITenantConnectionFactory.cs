using System.Data;

namespace Encina.ADO.MySQL.Tenancy;

/// <summary>
/// Factory for creating tenant-aware database connections for ADO.NET MySQL.
/// </summary>
/// <remarks>
/// <para>
/// This interface provides the contract for creating database connections
/// that are aware of the current tenant context. Implementations route to
/// the appropriate database connection.
/// </para>
/// <para>
/// For shared database scenarios (SharedSchema, SchemaPerTenant), use
/// the default connection string from configuration.
/// </para>
/// <para>
/// For database-per-tenant scenarios, each tenant has their own connection string
/// stored in TenantInfo.ConnectionString.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class MySqlTenantConnectionFactory : ITenantConnectionFactory
/// {
///     private readonly ITenantProvider _tenantProvider;
///     private readonly ITenantStore _tenantStore;
///     private readonly TenancyOptions _options;
///
///     public async ValueTask&lt;IDbConnection&gt; CreateConnectionAsync(CancellationToken ct)
///     {
///         var connectionString = await GetConnectionStringAsync(ct);
///         var connection = new MySqlConnection(connectionString);
///         await connection.OpenAsync(ct);
///         return connection;
///     }
///
///     public async ValueTask&lt;string&gt; GetConnectionStringAsync(CancellationToken ct)
///     {
///         var tenantId = _tenantProvider.GetCurrentTenantId();
///         if (string.IsNullOrEmpty(tenantId))
///             return _options.DefaultConnectionString;
///
///         var tenant = await _tenantStore.GetTenantAsync(tenantId, ct);
///         return tenant?.Strategy == TenantIsolationStrategy.DatabasePerTenant
///             ? tenant.ConnectionString ?? _options.DefaultConnectionString
///             : _options.DefaultConnectionString;
///     }
/// }
/// </code>
/// </example>
public interface ITenantConnectionFactory
{
    /// <summary>
    /// Creates a database connection for the current tenant context.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An open database connection appropriate for the current tenant.</returns>
    /// <remarks>
    /// <para>
    /// The returned connection is already opened and ready for use.
    /// </para>
    /// <para>
    /// For shared database strategies, returns a connection to the default database.
    /// For database-per-tenant strategies, returns a connection to the tenant's database.
    /// </para>
    /// </remarks>
    ValueTask<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a database connection for a specific tenant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An open database connection for the specified tenant.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when tenantId is null or empty.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the tenant is not found.
    /// </exception>
    /// <remarks>
    /// Use this method when you need to access a specific tenant's data
    /// outside the normal tenant context (e.g., admin operations).
    /// </remarks>
    ValueTask<IDbConnection> CreateConnectionForTenantAsync(
        string tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the connection string for the current tenant without creating a connection.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The connection string for the current tenant.</returns>
    ValueTask<string> GetConnectionStringAsync(CancellationToken cancellationToken = default);
}
