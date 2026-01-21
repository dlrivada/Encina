namespace Encina.Tenancy;

/// <summary>
/// Marker interface for entities that are tenant-scoped.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface on domain entities that should be isolated per tenant.
/// When using <see cref="TenantIsolationStrategy.SharedSchema"/>, entities implementing
/// this interface will have automatic query filtering and tenant ID assignment.
/// </para>
/// <para><b>Automatic Behaviors:</b></para>
/// <list type="bullet">
/// <item><b>Query Filtering:</b> All queries automatically include <c>WHERE TenantId = @currentTenantId</c></item>
/// <item><b>Insert Assignment:</b> New entities automatically get <see cref="TenantId"/> set from current context</item>
/// <item><b>Update Validation:</b> Updates are validated to prevent cross-tenant modifications</item>
/// </list>
/// <para>
/// For <see cref="TenantIsolationStrategy.SchemaPerTenant"/> and <see cref="TenantIsolationStrategy.DatabasePerTenant"/>,
/// this interface is optional but recommended for documentation and potential strategy migration.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class Order : ITenantEntity
/// {
///     public Guid Id { get; set; }
///     public string TenantId { get; set; } = null!;
///     public string CustomerName { get; set; } = null!;
///     public decimal Total { get; set; }
/// }
///
/// // With EF Core, queries are automatically filtered:
/// // SELECT * FROM Orders WHERE TenantId = 'current-tenant'
/// var orders = await context.Orders.ToListAsync();
/// </code>
/// </example>
public interface ITenantEntity
{
    /// <summary>
    /// Gets or sets the tenant identifier for this entity.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This property is automatically set when saving new entities
    /// and used for filtering when querying.
    /// </para>
    /// <para>
    /// <b>Important:</b> Do not manually set this property in application code.
    /// Let the tenancy infrastructure manage it automatically.
    /// </para>
    /// </remarks>
    string TenantId { get; set; }
}
