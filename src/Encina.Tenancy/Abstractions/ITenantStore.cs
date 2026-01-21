namespace Encina.Tenancy;

/// <summary>
/// Provides access to tenant metadata.
/// </summary>
/// <remarks>
/// <para>
/// This interface abstracts how tenant information is stored and retrieved.
/// Implementations can use various backends:
/// </para>
/// <list type="bullet">
/// <item><b>In-Memory:</b> For development, testing, or static tenant configurations</item>
/// <item><b>Database:</b> For dynamic tenant management with a tenant catalog database</item>
/// <item><b>Configuration:</b> For appsettings.json-based tenant configuration</item>
/// <item><b>External Service:</b> For centralized tenant management across microservices</item>
/// </list>
/// <para>
/// The store is typically registered as a singleton and should be thread-safe.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Custom database-backed implementation
/// public class DatabaseTenantStore : ITenantStore
/// {
///     private readonly TenantDbContext _context;
///
///     public DatabaseTenantStore(TenantDbContext context)
///     {
///         _context = context;
///     }
///
///     public async ValueTask&lt;TenantInfo?&gt; GetTenantAsync(
///         string tenantId,
///         CancellationToken cancellationToken = default)
///     {
///         var tenant = await _context.Tenants
///             .AsNoTracking()
///             .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);
///
///         return tenant?.ToTenantInfo();
///     }
///
///     public async ValueTask&lt;IReadOnlyList&lt;TenantInfo&gt;&gt; GetAllTenantsAsync(
///         CancellationToken cancellationToken = default)
///     {
///         return await _context.Tenants
///             .AsNoTracking()
///             .Select(t => t.ToTenantInfo())
///             .ToListAsync(cancellationToken);
///     }
/// }
/// </code>
/// </example>
public interface ITenantStore
{
    /// <summary>
    /// Retrieves tenant information by tenant ID.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>
    /// The tenant information if found; otherwise, <c>null</c>.
    /// </returns>
    /// <remarks>
    /// Implementations should cache results when appropriate for performance.
    /// </remarks>
    ValueTask<TenantInfo?> GetTenantAsync(string tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all registered tenants.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A read-only list of all tenant information.</returns>
    /// <remarks>
    /// <para>
    /// Use with caution in production with many tenants.
    /// Consider pagination for large tenant counts.
    /// </para>
    /// <para>
    /// Useful for administrative tasks, health checks, and migrations.
    /// </para>
    /// </remarks>
    ValueTask<IReadOnlyList<TenantInfo>> GetAllTenantsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a tenant exists.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns><c>true</c> if the tenant exists; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// This method should be efficient for validation purposes.
    /// Default implementation delegates to <see cref="GetTenantAsync"/>.
    /// </remarks>
    ValueTask<bool> ExistsAsync(string tenantId, CancellationToken cancellationToken = default)
        => new(GetTenantAsync(tenantId, cancellationToken).AsTask().ContinueWith(t => t.Result is not null));
}
