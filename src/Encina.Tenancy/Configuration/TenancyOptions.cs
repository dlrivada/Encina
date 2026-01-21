namespace Encina.Tenancy;

/// <summary>
/// Configuration options for Encina multi-tenancy.
/// </summary>
/// <remarks>
/// <para>
/// Configure these options when calling <c>AddEncinaTenancy()</c>:
/// </para>
/// <code>
/// services.AddEncinaTenancy(options =>
/// {
///     options.DefaultStrategy = TenantIsolationStrategy.SharedSchema;
///     options.RequireTenant = true;
///     options.TenantIdPropertyName = "TenantId";
/// });
/// </code>
/// </remarks>
public sealed class TenancyOptions
{
    /// <summary>
    /// Gets or sets the default isolation strategy for tenants.
    /// </summary>
    /// <value>The default is <see cref="TenantIsolationStrategy.SharedSchema"/>.</value>
    /// <remarks>
    /// This strategy is used when a tenant's <see cref="TenantInfo"/> does not specify
    /// its own strategy, or for new tenants created without explicit configuration.
    /// </remarks>
    public TenantIsolationStrategy DefaultStrategy { get; set; } = TenantIsolationStrategy.SharedSchema;

    /// <summary>
    /// Gets or sets a value indicating whether a tenant context is required for all requests.
    /// </summary>
    /// <value>The default is <c>false</c>.</value>
    /// <remarks>
    /// <para>
    /// When <c>true</c>:
    /// <list type="bullet">
    /// <item>Middleware will reject requests without a valid tenant ID</item>
    /// <item>Repository operations will throw if no tenant context is available</item>
    /// <item>Useful for pure multi-tenant applications with no shared data</item>
    /// </list>
    /// </para>
    /// <para>
    /// When <c>false</c>:
    /// <list type="bullet">
    /// <item>Requests without tenant context are allowed</item>
    /// <item>Useful for applications with both tenant-scoped and global data</item>
    /// <item>Background jobs and health checks can run without tenant context</item>
    /// </list>
    /// </para>
    /// </remarks>
    public bool RequireTenant { get; set; }

    /// <summary>
    /// Gets or sets the property name used for tenant identification in entities.
    /// </summary>
    /// <value>The default is <c>"TenantId"</c>.</value>
    /// <remarks>
    /// This is used by convention-based entity configuration in EF Core
    /// and for column mapping in Dapper/ADO.NET.
    /// </remarks>
    public string TenantIdPropertyName { get; set; } = "TenantId";

    /// <summary>
    /// Gets or sets the default connection string for shared database scenarios.
    /// </summary>
    /// <value>The default is <c>null</c>.</value>
    /// <remarks>
    /// <para>
    /// This connection string is used when:
    /// <list type="bullet">
    /// <item>Tenant uses <see cref="TenantIsolationStrategy.SharedSchema"/></item>
    /// <item>Tenant uses <see cref="TenantIsolationStrategy.SchemaPerTenant"/></item>
    /// <item>Tenant's <see cref="TenantInfo.ConnectionString"/> is null or empty</item>
    /// </list>
    /// </para>
    /// <para>
    /// For <see cref="TenantIsolationStrategy.DatabasePerTenant"/>, each tenant
    /// should have its own connection string in <see cref="TenantInfo"/>.
    /// </para>
    /// </remarks>
    public string? DefaultConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the default schema name for schema-per-tenant scenarios.
    /// </summary>
    /// <value>The default is <c>"dbo"</c>.</value>
    /// <remarks>
    /// This schema is used when a tenant's <see cref="TenantInfo.SchemaName"/> is null
    /// and the tenant uses <see cref="TenantIsolationStrategy.SchemaPerTenant"/>.
    /// </remarks>
    public string DefaultSchemaName { get; set; } = "dbo";

    /// <summary>
    /// Gets or sets a value indicating whether to validate tenant existence
    /// when resolving tenant context.
    /// </summary>
    /// <value>The default is <c>false</c>.</value>
    /// <remarks>
    /// <para>
    /// When <c>true</c>, the middleware will call <see cref="ITenantStore.ExistsAsync"/>
    /// to verify the tenant exists before proceeding. This adds latency but ensures
    /// only valid tenants can access the system.
    /// </para>
    /// <para>
    /// When <c>false</c>, the tenant ID is accepted without validation against the store.
    /// The tenant lookup happens lazily when <see cref="ITenantProvider.GetCurrentTenantAsync"/>
    /// is called.
    /// </para>
    /// </remarks>
    public bool ValidateTenantOnRequest { get; set; }

    /// <summary>
    /// Gets the collection of statically configured tenants.
    /// </summary>
    /// <value>An empty list by default.</value>
    /// <remarks>
    /// <para>
    /// Use this to configure tenants directly in code or appsettings.json
    /// when using <see cref="InMemoryTenantStore"/>.
    /// </para>
    /// <code>
    /// services.AddEncinaTenancy(options =>
    /// {
    ///     options.Tenants.Add(new TenantInfo(
    ///         TenantId: "tenant-1",
    ///         Name: "Acme Corp",
    ///         Strategy: TenantIsolationStrategy.SharedSchema));
    ///
    ///     options.Tenants.Add(new TenantInfo(
    ///         TenantId: "tenant-2",
    ///         Name: "Enterprise Inc",
    ///         Strategy: TenantIsolationStrategy.DatabasePerTenant,
    ///         ConnectionString: "Server=enterprise-db;..."));
    /// });
    /// </code>
    /// </remarks>
    public List<TenantInfo> Tenants { get; } = [];
}
