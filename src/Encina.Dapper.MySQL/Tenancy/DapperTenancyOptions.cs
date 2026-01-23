namespace Encina.Dapper.MySQL.Tenancy;

/// <summary>
/// Configuration options for Dapper multi-tenancy support.
/// </summary>
/// <remarks>
/// <para>
/// These options control how tenant filtering is applied to SQL queries
/// and how entities are managed in a multi-tenant environment.
/// </para>
/// <para>
/// Configure these options when calling <c>AddEncinaDapperWithTenancy()</c>:
/// </para>
/// <code>
/// services.AddEncinaDapperWithTenancy(config =>
/// {
///     config.UseOutbox = true;
/// }, tenancy =>
/// {
///     tenancy.AutoFilterTenantQueries = true;
///     tenancy.AutoAssignTenantId = true;
///     tenancy.ThrowOnMissingTenantContext = true;
/// });
/// </code>
/// </remarks>
public sealed class DapperTenancyOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to automatically filter
    /// queries by tenant ID for tenant entities.
    /// </summary>
    /// <value>The default is <c>true</c>.</value>
    /// <remarks>
    /// When enabled, all queries on entities implementing ITenantEntity
    /// will automatically include a <c>WHERE TenantId = @tenantId</c> filter.
    /// </remarks>
    public bool AutoFilterTenantQueries { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to automatically assign
    /// the current tenant ID to new entities on insert.
    /// </summary>
    /// <value>The default is <c>true</c>.</value>
    /// <remarks>
    /// When enabled, the repository will set the <c>TenantId</c> property
    /// automatically before INSERT operations for entities implementing ITenantEntity.
    /// </remarks>
    public bool AutoAssignTenantId { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to validate that the
    /// current tenant matches the entity's tenant on UPDATE/DELETE operations.
    /// </summary>
    /// <value>The default is <c>true</c>.</value>
    /// <remarks>
    /// When enabled, the repository will verify that the entity's <c>TenantId</c>
    /// matches the current tenant context before allowing modifications.
    /// This prevents cross-tenant data access.
    /// </remarks>
    public bool ValidateTenantOnModify { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to throw an exception
    /// when no tenant context is available for operations that require it.
    /// </summary>
    /// <value>The default is <c>true</c>.</value>
    /// <remarks>
    /// <para>
    /// When <c>true</c>, operations on tenant entities without a current
    /// tenant context will throw an <see cref="InvalidOperationException"/>.
    /// </para>
    /// <para>
    /// When <c>false</c>, operations will proceed without tenant filtering,
    /// which may expose data across tenants.
    /// </para>
    /// </remarks>
    public bool ThrowOnMissingTenantContext { get; set; } = true;

    /// <summary>
    /// Gets or sets the column name used for tenant identification in database tables.
    /// </summary>
    /// <value>The default is <c>"TenantId"</c>.</value>
    /// <remarks>
    /// This should match the column name in your database schema.
    /// The value is validated as a safe SQL identifier.
    /// </remarks>
    public string TenantColumnName { get; set; } = "TenantId";
}
