using Encina;
using LanguageExt;

namespace Encina.Tenancy;

/// <summary>
/// Factory for creating tenant-specific database connections.
/// </summary>
/// <typeparam name="TConnection">The type of database connection (e.g., DbConnection, IDbConnection).</typeparam>
/// <remarks>
/// <para>
/// This interface abstracts the creation of database connections based on tenant context.
/// It is primarily used for <see cref="TenantIsolationStrategy.DatabasePerTenant"/> scenarios
/// where each tenant has its own database.
/// </para>
/// <para><b>Implementation Guidelines:</b></para>
/// <list type="bullet">
/// <item>Implementations should handle connection pooling appropriately</item>
/// <item>Connection strings are resolved from <see cref="ITenantStore"/> via <see cref="TenantInfo.ConnectionString"/></item>
/// <item>For shared schema/schema-per-tenant, use <see cref="TenancyOptions.DefaultConnectionString"/></item>
/// </list>
/// <para><b>Provider Implementations:</b></para>
/// <list type="bullet">
/// <item><c>Encina.EntityFrameworkCore</c>: Creates <c>DbConnection</c> for EF Core contexts</item>
/// <item><c>Encina.Dapper.*</c>: Creates <c>IDbConnection</c> for Dapper repositories</item>
/// <item><c>Encina.ADO.*</c>: Creates <c>DbConnection</c> for ADO.NET operations</item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // SQL Server implementation
/// public class SqlServerTenantConnectionFactory : ITenantConnectionFactory&lt;SqlConnection&gt;
/// {
///     private readonly ITenantProvider _tenantProvider;
///     private readonly TenancyOptions _options;
///
///     public SqlServerTenantConnectionFactory(
///         ITenantProvider tenantProvider,
///         IOptions&lt;TenancyOptions&gt; options)
///     {
///         _tenantProvider = tenantProvider;
///         _options = options.Value;
///     }
///
///     public async ValueTask&lt;Either&lt;EncinaError, SqlConnection&gt;&gt; CreateConnectionAsync(
///         string? tenantId = null,
///         CancellationToken cancellationToken = default)
///     {
///         var connectionStringResult = await GetConnectionStringAsync(tenantId, cancellationToken);
///         return await connectionStringResult.MatchAsync&lt;Either&lt;EncinaError, SqlConnection&gt;&gt;(
///             RightAsync: async cs =&gt;
///             {
///                 var connection = new SqlConnection(cs);
///                 await connection.OpenAsync(cancellationToken);
///                 return connection;
///             },
///             Left: error =&gt; error);
///     }
///
///     public async ValueTask&lt;Either&lt;EncinaError, string&gt;&gt; GetConnectionStringAsync(
///         string? tenantId = null,
///         CancellationToken cancellationToken = default)
///     {
///         tenantId ??= _tenantProvider.GetCurrentTenantId();
///
///         if (tenantId is null)
///             return GetDefaultConnectionString();
///
///         var tenant = await _tenantProvider.GetCurrentTenantAsync(cancellationToken);
///
///         return tenant?.ConnectionString
///             ?? _options.DefaultConnectionString
///             ?? EncinaError.New($"No connection string for tenant '{tenantId}'").ToString();
///     }
///
///     private Either&lt;EncinaError, string&gt; GetDefaultConnectionString()
///     {
///         if (!string.IsNullOrWhiteSpace(_options.DefaultConnectionString))
///             return _options.DefaultConnectionString;
///
///         return EncinaError.New("No tenant context and no default connection string");
///     }
/// }
/// </code>
/// </example>
public interface ITenantConnectionFactory<TConnection>
    where TConnection : class, IDisposable
{
    /// <summary>
    /// Creates a database connection for the specified or current tenant.
    /// </summary>
    /// <param name="tenantId">
    /// The tenant ID to create a connection for.
    /// If <c>null</c>, uses the current tenant from <see cref="ITenantProvider"/>.
    /// </param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>
    /// An <see cref="Either{EncinaError, TConnection}"/> containing an open database connection
    /// for the tenant on success, or an <see cref="EncinaError"/> if no connection string can be resolved.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The returned connection is typically already opened.
    /// The caller is responsible for disposing the connection.
    /// </para>
    /// <para>
    /// Connection resolution order:
    /// <list type="number">
    /// <item>If <paramref name="tenantId"/> provided, use that tenant's connection string</item>
    /// <item>Otherwise, use current tenant from <see cref="ITenantProvider.GetCurrentTenantId"/></item>
    /// <item>If tenant has <see cref="TenantInfo.ConnectionString"/>, use it</item>
    /// <item>Otherwise, use <see cref="TenancyOptions.DefaultConnectionString"/></item>
    /// </list>
    /// </para>
    /// </remarks>
    ValueTask<Either<EncinaError, TConnection>> CreateConnectionAsync(string? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the connection string for the specified or current tenant without creating a connection.
    /// </summary>
    /// <param name="tenantId">
    /// The tenant ID to get the connection string for.
    /// If <c>null</c>, uses the current tenant from <see cref="ITenantProvider"/>.
    /// </param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>
    /// An <see cref="Either{EncinaError, String}"/> containing the connection string for the tenant
    /// on success, or an <see cref="EncinaError"/> if no connection string can be resolved.
    /// </returns>
    /// <remarks>
    /// Useful for scenarios where you need the connection string but don't want to create
    /// a connection immediately (e.g., EF Core DbContext configuration).
    /// </remarks>
    ValueTask<Either<EncinaError, string>> GetConnectionStringAsync(string? tenantId = null, CancellationToken cancellationToken = default);
}
