using System.Data;

namespace Encina.Dapper.PostgreSQL.Tenancy;

/// <summary>
/// Factory for creating tenant-specific database connections.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface to provide database-per-tenant connection routing.
/// The factory retrieves the current tenant's connection string and creates
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
/// public class PostgreSqlTenantConnectionFactory : ITenantConnectionFactory
/// {
///     private readonly ITenantProvider _tenantProvider;
///     private readonly ITenantStore _tenantStore;
///     private readonly TenancyOptions _options;
///
///     public async ValueTask&lt;IDbConnection&gt; CreateConnectionAsync(CancellationToken ct)
///     {
///         var tenant = await _tenantProvider.GetCurrentTenantAsync(ct);
///         var connectionString = tenant?.ConnectionString ?? _options.DefaultConnectionString;
///         return new NpgsqlConnection(connectionString);
///     }
/// }
/// </code>
/// </example>
public interface ITenantConnectionFactory
{
    /// <summary>
    /// Creates a database connection for the current tenant.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A database connection configured for the current tenant.</returns>
    /// <remarks>
    /// <para>
    /// The returned connection should be configured with the appropriate
    /// connection string based on the current tenant context:
    /// </para>
    /// <list type="bullet">
    /// <item><b>DatabasePerTenant:</b> Uses the tenant's specific connection string</item>
    /// <item><b>SchemaPerTenant:</b> Uses the default connection string (schema is set elsewhere)</item>
    /// <item><b>SharedSchema:</b> Uses the default connection string</item>
    /// </list>
    /// <para>
    /// If no tenant context is available, the default connection string is used.
    /// </para>
    /// </remarks>
    ValueTask<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a database connection for a specific tenant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A database connection configured for the specified tenant.</returns>
    /// <remarks>
    /// Use this method when you need to explicitly specify the tenant,
    /// such as in background jobs or cross-tenant operations.
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
