namespace Encina.Tenancy;

/// <summary>
/// Contains metadata about a tenant.
/// </summary>
/// <remarks>
/// <para>
/// This record holds all the information needed to identify and configure
/// database access for a specific tenant. It is retrieved from <see cref="ITenantStore"/>
/// and used by connection factories and query filters.
/// </para>
/// <para>
/// The <see cref="ConnectionString"/> and <see cref="SchemaName"/> properties are
/// used based on the <see cref="Strategy"/>:
/// </para>
/// <list type="bullet">
/// <item><see cref="TenantIsolationStrategy.SharedSchema"/>: Neither property is used (shared connection, shared schema)</item>
/// <item><see cref="TenantIsolationStrategy.SchemaPerTenant"/>: <see cref="SchemaName"/> determines the schema to use</item>
/// <item><see cref="TenantIsolationStrategy.DatabasePerTenant"/>: <see cref="ConnectionString"/> determines the database to connect to</item>
/// </list>
/// </remarks>
/// <param name="TenantId">The unique identifier for the tenant.</param>
/// <param name="Name">The display name of the tenant.</param>
/// <param name="Strategy">The isolation strategy used for this tenant's data.</param>
/// <param name="ConnectionString">
/// The connection string for database-per-tenant strategy.
/// <c>null</c> when using shared schema or schema-per-tenant strategies.
/// </param>
/// <param name="SchemaName">
/// The schema name for schema-per-tenant strategy.
/// <c>null</c> when using shared schema or database-per-tenant strategies.
/// </param>
/// <example>
/// <code>
/// // Shared schema tenant
/// var tenant1 = new TenantInfo(
///     TenantId: "tenant-123",
///     Name: "Acme Corp",
///     Strategy: TenantIsolationStrategy.SharedSchema);
///
/// // Schema-per-tenant
/// var tenant2 = new TenantInfo(
///     TenantId: "tenant-456",
///     Name: "Contoso Ltd",
///     Strategy: TenantIsolationStrategy.SchemaPerTenant,
///     SchemaName: "contoso");
///
/// // Database-per-tenant
/// var tenant3 = new TenantInfo(
///     TenantId: "tenant-789",
///     Name: "Enterprise Inc",
///     Strategy: TenantIsolationStrategy.DatabasePerTenant,
///     ConnectionString: "Server=enterprise-db;Database=EnterpriseData;...");
/// </code>
/// </example>
public sealed record TenantInfo(
    string TenantId,
    string Name,
    TenantIsolationStrategy Strategy,
    string? ConnectionString = null,
    string? SchemaName = null)
{
    /// <summary>
    /// Gets a value indicating whether this tenant uses a dedicated database.
    /// </summary>
    public bool HasDedicatedDatabase => Strategy == TenantIsolationStrategy.DatabasePerTenant
                                        && !string.IsNullOrWhiteSpace(ConnectionString);

    /// <summary>
    /// Gets a value indicating whether this tenant uses a dedicated schema.
    /// </summary>
    public bool HasDedicatedSchema => Strategy == TenantIsolationStrategy.SchemaPerTenant
                                      && !string.IsNullOrWhiteSpace(SchemaName);

    /// <summary>
    /// Gets a value indicating whether this tenant uses shared schema isolation.
    /// </summary>
    public bool UsesSharedSchema => Strategy == TenantIsolationStrategy.SharedSchema;
}
