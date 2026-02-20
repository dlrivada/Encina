using System.Data;
using Encina;
using LanguageExt;

namespace Encina.Dapper.Sqlite.Tenancy;

/// <summary>
/// Factory for creating tenant-specific SQLite database connections.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface to provide database-per-tenant connection routing.
/// The factory retrieves the current tenant's connection string and creates
/// the appropriate database connection.
/// </para>
/// <para>
/// For shared database scenarios (SharedSchema), use
/// the default connection string from configuration.
/// </para>
/// <para>
/// For database-per-tenant scenarios, each tenant has their own connection string
/// stored in TenantInfo.ConnectionString.
/// </para>
/// <para>
/// <b>Note:</b> SQLite does not support schema-per-tenant isolation.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class SqliteTenantConnectionFactory : ITenantConnectionFactory
/// {
///     private readonly ITenantProvider _tenantProvider;
///     private readonly ITenantStore _tenantStore;
///     private readonly TenancyOptions _options;
///
///     public async ValueTask&lt;Either&lt;EncinaError, IDbConnection&gt;&gt; CreateConnectionAsync(CancellationToken ct)
///     {
///         var tenant = await _tenantProvider.GetCurrentTenantAsync(ct);
///         var connectionString = tenant?.ConnectionString ?? _options.DefaultConnectionString;
///         return new SqliteConnection(connectionString);
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
    /// <returns>
    /// An <see cref="Either{EncinaError, IDbConnection}"/> containing either the connection
    /// configured for the current tenant, or an error if the connection string is unavailable.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The returned connection should be configured with the appropriate
    /// connection string based on the current tenant context:
    /// </para>
    /// <list type="bullet">
    /// <item><b>DatabasePerTenant:</b> Uses the tenant's specific connection string (e.g., "Data Source=tenant123.db")</item>
    /// <item><b>SharedSchema:</b> Uses the default connection string</item>
    /// </list>
    /// <para>
    /// If no tenant context is available, the default connection string is used.
    /// </para>
    /// </remarks>
    ValueTask<Either<EncinaError, IDbConnection>> CreateConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a database connection for a specific tenant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// An <see cref="Either{EncinaError, IDbConnection}"/> containing either the connection
    /// configured for the specified tenant, or an error if the tenant is not found
    /// or the connection string is unavailable.
    /// </returns>
    /// <remarks>
    /// Use this method when you need to explicitly specify the tenant,
    /// such as in background jobs or cross-tenant operations.
    /// </remarks>
    ValueTask<Either<EncinaError, IDbConnection>> CreateConnectionForTenantAsync(
        string tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the connection string for the current tenant without creating a connection.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// An <see cref="Either{EncinaError, String}"/> containing either the connection string
    /// for the current tenant, or an error if the connection string is unavailable.
    /// </returns>
    ValueTask<Either<EncinaError, string>> GetConnectionStringAsync(CancellationToken cancellationToken = default);
}
