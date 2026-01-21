namespace Encina.Tenancy;

/// <summary>
/// Provides access to the current tenant context.
/// </summary>
/// <remarks>
/// <para>
/// This interface is the primary entry point for tenant information during request processing.
/// It bridges the request context (which holds the tenant ID) with the tenant metadata store.
/// </para>
/// <para>
/// The provider is registered as scoped since it depends on the current request context.
/// </para>
/// <para><b>Usage Patterns:</b></para>
/// <list type="bullet">
/// <item><b>Query Filters:</b> Inject to get tenant ID for WHERE clauses</item>
/// <item><b>Connection Routing:</b> Inject to get connection string for database-per-tenant</item>
/// <item><b>Schema Selection:</b> Inject to get schema name for schema-per-tenant</item>
/// <item><b>Business Logic:</b> Inject when tenant-specific logic is needed</item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// public class OrderService
/// {
///     private readonly ITenantProvider _tenantProvider;
///     private readonly IOrderRepository _orderRepository;
///
///     public OrderService(ITenantProvider tenantProvider, IOrderRepository orderRepository)
///     {
///         _tenantProvider = tenantProvider;
///         _orderRepository = orderRepository;
///     }
///
///     public async Task&lt;Order&gt; CreateOrderAsync(CreateOrderRequest request)
///     {
///         var tenantId = _tenantProvider.GetCurrentTenantId();
///         if (tenantId is null)
///             throw new InvalidOperationException("Tenant context required");
///
///         // TenantId is automatically assigned by the repository
///         var order = new Order { CustomerName = request.CustomerName };
///         return await _orderRepository.AddAsync(order);
///     }
///
///     public async Task ConfigureTenantAsync()
///     {
///         var tenant = await _tenantProvider.GetCurrentTenantAsync();
///         if (tenant?.Strategy == TenantIsolationStrategy.DatabasePerTenant)
///         {
///             // Special handling for dedicated database tenants
///         }
///     }
/// }
/// </code>
/// </example>
public interface ITenantProvider
{
    /// <summary>
    /// Gets the current tenant ID from the request context.
    /// </summary>
    /// <returns>
    /// The current tenant ID, or <c>null</c> if no tenant context is available.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method returns the tenant ID from <see cref="IRequestContext.TenantId"/>.
    /// It does not perform any I/O and is safe to call frequently.
    /// </para>
    /// <para>
    /// Returns <c>null</c> when:
    /// <list type="bullet">
    /// <item>No request context is available (e.g., background jobs without context)</item>
    /// <item>The request context has no tenant ID set</item>
    /// </list>
    /// </para>
    /// </remarks>
    string? GetCurrentTenantId();

    /// <summary>
    /// Gets the full tenant information for the current tenant.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>
    /// The tenant information, or <c>null</c> if no tenant context is available
    /// or the tenant is not found in the store.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method retrieves the tenant ID from the request context and then
    /// looks up the full tenant metadata from <see cref="ITenantStore"/>.
    /// </para>
    /// <para>
    /// Results may be cached by the underlying <see cref="ITenantStore"/> implementation.
    /// </para>
    /// <para>
    /// Returns <c>null</c> when:
    /// <list type="bullet">
    /// <item><see cref="GetCurrentTenantId"/> returns <c>null</c></item>
    /// <item>The tenant ID is not found in the <see cref="ITenantStore"/></item>
    /// </list>
    /// </para>
    /// </remarks>
    ValueTask<TenantInfo?> GetCurrentTenantAsync(CancellationToken cancellationToken = default);
}
