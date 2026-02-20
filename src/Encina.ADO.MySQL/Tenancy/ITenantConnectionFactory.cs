using System.Data;
using Encina;
using LanguageExt;

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
/// <para>
/// All methods return <see cref="Either{EncinaError, T}"/> following the Railway Oriented
/// Programming pattern instead of throwing exceptions for runtime configuration errors.
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
///     public async ValueTask&lt;Either&lt;EncinaError, IDbConnection&gt;&gt; CreateConnectionAsync(CancellationToken ct)
///     {
///         var connectionStringResult = await GetConnectionStringAsync(ct);
///         return await connectionStringResult.MatchAsync&lt;Either&lt;EncinaError, IDbConnection&gt;&gt;(
///             RightAsync: async cs =&gt;
///             {
///                 var connection = new MySqlConnection(cs);
///                 await connection.OpenAsync(ct);
///                 return Either&lt;EncinaError, IDbConnection&gt;.Right(connection);
///             },
///             Left: error =&gt; error);
///     }
///
///     public async ValueTask&lt;Either&lt;EncinaError, string&gt;&gt; GetConnectionStringAsync(CancellationToken ct)
///     {
///         var tenantId = _tenantProvider.GetCurrentTenantId();
///         if (string.IsNullOrEmpty(tenantId))
///             return GetDefaultConnectionString();
///
///         var tenant = await _tenantStore.GetTenantAsync(tenantId, ct);
///         return tenant is null
///             ? GetDefaultConnectionString()
///             : GetConnectionStringForTenant(tenant);
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
    /// <returns>
    /// An <see cref="Either{EncinaError, IDbConnection}"/> containing an open database connection
    /// appropriate for the current tenant on success, or an <see cref="EncinaError"/> on failure.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The returned connection is already opened and ready for use.
    /// </para>
    /// <para>
    /// For shared database strategies, returns a connection to the default database.
    /// For database-per-tenant strategies, returns a connection to the tenant's database.
    /// </para>
    /// </remarks>
    ValueTask<Either<EncinaError, IDbConnection>> CreateConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a database connection for a specific tenant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// An <see cref="Either{EncinaError, IDbConnection}"/> containing an open database connection
    /// for the specified tenant on success, or an <see cref="EncinaError"/> if the tenant is not found
    /// or the connection string is not configured.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when tenantId is null or empty.
    /// </exception>
    /// <remarks>
    /// Use this method when you need to access a specific tenant's data
    /// outside the normal tenant context (e.g., admin operations).
    /// </remarks>
    ValueTask<Either<EncinaError, IDbConnection>> CreateConnectionForTenantAsync(
        string tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the connection string for the current tenant without creating a connection.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// An <see cref="Either{EncinaError, String}"/> containing the connection string for the current
    /// tenant on success, or an <see cref="EncinaError"/> if the connection string is not configured.
    /// </returns>
    ValueTask<Either<EncinaError, string>> GetConnectionStringAsync(CancellationToken cancellationToken = default);
}
