namespace Encina.Messaging.Tenancy;

/// <summary>
/// Configuration options for tenancy integration with messaging patterns.
/// </summary>
/// <remarks>
/// <para>
/// These options control how multi-tenancy is integrated with the messaging infrastructure
/// when using provider-specific implementations (e.g., Entity Framework Core, Dapper).
/// </para>
/// <para>
/// <b>Note</b>: This class defines the messaging-level tenancy configuration.
/// Provider-specific options (like EF Core query filters) are defined in the provider packages.
/// </para>
/// </remarks>
public sealed class TenancyMessagingOptions
{
    /// <summary>
    /// Gets or sets whether to automatically assign tenant ID to new entities.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When enabled, entities implementing <c>ITenantEntity</c> will automatically
    /// have their <c>TenantId</c> set from the current tenant context during save.
    /// </para>
    /// <para>
    /// If the entity already has a <c>TenantId</c> value, it will be preserved.
    /// </para>
    /// </remarks>
    /// <value>Default: true</value>
    public bool AutoAssignTenantId { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to validate tenant ownership on save operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When enabled, modifications to existing tenant entities will be validated
    /// to ensure the entity belongs to the current tenant context.
    /// </para>
    /// <para>
    /// An exception will be thrown if a tenant tries to modify another tenant's data.
    /// </para>
    /// </remarks>
    /// <value>Default: true</value>
    public bool ValidateTenantOnSave { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to use global query filters for tenant isolation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When enabled (provider-dependent), queries are automatically filtered
    /// to only return data belonging to the current tenant.
    /// </para>
    /// <para>
    /// For Entity Framework Core, this uses <c>HasQueryFilter</c> on tenant entities.
    /// </para>
    /// </remarks>
    /// <value>Default: true</value>
    public bool UseQueryFilters { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to throw exceptions when tenant context is missing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When enabled, operations requiring a tenant context (like saving tenant entities)
    /// will throw an exception if no tenant is resolved.
    /// </para>
    /// <para>
    /// Disable this for scenarios where admin/system operations run without tenant context.
    /// </para>
    /// </remarks>
    /// <value>Default: true</value>
    public bool ThrowOnMissingTenantContext { get; set; } = true;
}
