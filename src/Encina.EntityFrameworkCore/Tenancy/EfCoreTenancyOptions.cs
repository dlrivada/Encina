namespace Encina.EntityFrameworkCore.Tenancy;

/// <summary>
/// Configuration options for Entity Framework Core multi-tenancy support.
/// </summary>
/// <remarks>
/// <para>
/// All tenancy features are opt-in and disabled by default.
/// Enable only what you need for your application.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddEncinaEntityFrameworkCore&lt;AppDbContext&gt;(config =>
/// {
///     config.UseTenancy(tenancy =>
///     {
///         tenancy.AutoAssignTenantId = true;
///         tenancy.ValidateTenantOnSave = true;
///     });
/// });
/// </code>
/// </example>
public sealed class EfCoreTenancyOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to automatically assign tenant ID
    /// to new entities implementing <see cref="Encina.Tenancy.ITenantEntity"/>.
    /// </summary>
    /// <value>The default is <c>true</c> when tenancy is enabled.</value>
    /// <remarks>
    /// <para>
    /// When enabled, <c>SaveChangesAsync</c> will automatically set the <c>TenantId</c>
    /// property on newly added entities to the current tenant ID from <see cref="Encina.Tenancy.ITenantProvider"/>.
    /// </para>
    /// <para>
    /// If no tenant context is available and <see cref="Encina.Tenancy.TenancyOptions.RequireTenant"/>
    /// is <c>true</c>, an exception will be thrown.
    /// </para>
    /// </remarks>
    public bool AutoAssignTenantId { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to validate that modified entities
    /// belong to the current tenant on save.
    /// </summary>
    /// <value>The default is <c>true</c> when tenancy is enabled.</value>
    /// <remarks>
    /// <para>
    /// When enabled, <c>SaveChangesAsync</c> will verify that any modified or deleted
    /// <see cref="Encina.Tenancy.ITenantEntity"/> entities have a TenantId matching
    /// the current tenant context.
    /// </para>
    /// <para>
    /// This provides an additional layer of security against cross-tenant data access.
    /// </para>
    /// </remarks>
    public bool ValidateTenantOnSave { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to apply global query filters
    /// for tenant isolation in LINQ queries.
    /// </summary>
    /// <value>The default is <c>true</c> when tenancy is enabled.</value>
    /// <remarks>
    /// <para>
    /// When enabled, a global query filter is applied to all entities implementing
    /// <see cref="Encina.Tenancy.ITenantEntity"/> to filter by current tenant ID.
    /// </para>
    /// <para>
    /// Filters can be bypassed using <c>IgnoreQueryFilters()</c> in specific queries
    /// when needed (e.g., for admin operations).
    /// </para>
    /// </remarks>
    public bool UseQueryFilters { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to throw when attempting to save
    /// tenant entities without a tenant context and <c>RequireTenant</c> is true.
    /// </summary>
    /// <value>The default is <c>true</c>.</value>
    /// <remarks>
    /// When <c>false</c>, entities will be saved without tenant ID assignment,
    /// which may cause data isolation issues.
    /// </remarks>
    public bool ThrowOnMissingTenantContext { get; set; } = true;
}
