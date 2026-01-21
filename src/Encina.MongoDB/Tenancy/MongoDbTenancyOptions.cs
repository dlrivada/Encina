namespace Encina.MongoDB.Tenancy;

/// <summary>
/// Configuration options for MongoDB multi-tenancy support.
/// </summary>
/// <remarks>
/// <para>
/// These options control how tenant filtering is applied to MongoDB queries
/// and how entities are managed in a multi-tenant environment.
/// </para>
/// <para>
/// Configure these options when calling <c>AddEncinaMongoDBWithTenancy()</c>:
/// </para>
/// <code>
/// services.AddEncinaMongoDBWithTenancy(config =>
/// {
///     config.UseOutbox = true;
/// }, tenancy =>
/// {
///     tenancy.AutoFilterTenantQueries = true;
///     tenancy.AutoAssignTenantId = true;
///     tenancy.EnableDatabasePerTenant = false;
/// });
/// </code>
/// </remarks>
public sealed class MongoDbTenancyOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to automatically filter
    /// queries by tenant ID for tenant entities.
    /// </summary>
    /// <value>The default is <c>true</c>.</value>
    /// <remarks>
    /// When enabled, all queries on entities implementing tenant filtering
    /// will automatically include a tenant filter using <c>Builders&lt;T&gt;.Filter.Eq("TenantId", tenantId)</c>.
    /// </remarks>
    public bool AutoFilterTenantQueries { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to automatically assign
    /// the current tenant ID to new entities on insert.
    /// </summary>
    /// <value>The default is <c>true</c>.</value>
    /// <remarks>
    /// When enabled, the repository will set the <c>TenantId</c> property
    /// automatically before INSERT operations for tenant entities.
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
    /// Gets or sets the field name used for tenant identification in MongoDB documents.
    /// </summary>
    /// <value>The default is <c>"TenantId"</c>.</value>
    /// <remarks>
    /// This should match the field name in your MongoDB documents.
    /// </remarks>
    public string TenantFieldName { get; set; } = "TenantId";

    /// <summary>
    /// Gets or sets a value indicating whether to enable database-per-tenant isolation.
    /// </summary>
    /// <value>The default is <c>false</c>.</value>
    /// <remarks>
    /// <para>
    /// When enabled, each tenant uses a separate MongoDB database.
    /// The database name is determined by <see cref="DatabaseNamePattern"/>.
    /// </para>
    /// <para>
    /// When disabled (default), all tenants share a single database
    /// and tenant isolation is achieved through document-level filtering.
    /// </para>
    /// </remarks>
    public bool EnableDatabasePerTenant { get; set; }

    /// <summary>
    /// Gets or sets the pattern for generating tenant-specific database names.
    /// </summary>
    /// <value>The default is <c>"{baseName}_{tenantId}"</c>.</value>
    /// <remarks>
    /// <para>
    /// Placeholders:
    /// <list type="bullet">
    /// <item><c>{baseName}</c> - The base database name from <see cref="EncinaMongoDbOptions.DatabaseName"/></item>
    /// <item><c>{tenantId}</c> - The current tenant's identifier</item>
    /// </list>
    /// </para>
    /// <para>
    /// Only used when <see cref="EnableDatabasePerTenant"/> is <c>true</c>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Default pattern: "MyApp_tenant-123" for tenant "tenant-123"
    /// options.DatabaseNamePattern = "{baseName}_{tenantId}";
    ///
    /// // Custom pattern: "tenant_tenant-123"
    /// options.DatabaseNamePattern = "tenant_{tenantId}";
    /// </code>
    /// </example>
    public string DatabaseNamePattern { get; set; } = "{baseName}_{tenantId}";

    /// <summary>
    /// Generates the database name for a specific tenant.
    /// </summary>
    /// <param name="baseName">The base database name.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <returns>The database name for the tenant.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="baseName"/> or <paramref name="tenantId"/> is null or empty.
    /// </exception>
    public string GetDatabaseName(string baseName, string tenantId)
    {
        ArgumentException.ThrowIfNullOrEmpty(baseName);
        ArgumentException.ThrowIfNullOrEmpty(tenantId);

        return DatabaseNamePattern
            .Replace("{baseName}", baseName, StringComparison.Ordinal)
            .Replace("{tenantId}", tenantId, StringComparison.Ordinal);
    }
}
