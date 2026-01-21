namespace Encina.Tenancy;

/// <summary>
/// Specifies how tenant data is isolated in the database.
/// </summary>
/// <remarks>
/// <para>
/// The isolation strategy determines how tenant data is separated and accessed.
/// Each strategy has different trade-offs in terms of isolation, cost, and complexity.
/// </para>
/// <para><b>Strategy Comparison:</b></para>
/// <list type="table">
/// <listheader>
///   <term>Strategy</term>
///   <description>Best For</description>
/// </listheader>
/// <item>
///   <term><see cref="SharedSchema"/></term>
///   <description>Cost-effective SaaS with many small tenants. Simple operations, easy backup/restore.</description>
/// </item>
/// <item>
///   <term><see cref="SchemaPerTenant"/></term>
///   <description>Medium isolation needs. Easier data management per tenant while sharing database infrastructure.</description>
/// </item>
/// <item>
///   <term><see cref="DatabasePerTenant"/></term>
///   <description>Maximum isolation for compliance (HIPAA, GDPR). Enterprise clients with strict data separation requirements.</description>
/// </item>
/// </list>
/// </remarks>
public enum TenantIsolationStrategy
{
    /// <summary>
    /// All tenants share the same database and schema.
    /// Tenant data is separated by a TenantId column in each table.
    /// </summary>
    /// <remarks>
    /// <para><b>Pros:</b></para>
    /// <list type="bullet">
    /// <item>Lowest infrastructure cost (single database)</item>
    /// <item>Simplest deployment and operations</item>
    /// <item>Easy cross-tenant queries for analytics</item>
    /// <item>Simple backup and restore</item>
    /// </list>
    /// <para><b>Cons:</b></para>
    /// <list type="bullet">
    /// <item>Requires careful query filtering (handled automatically by Encina)</item>
    /// <item>Potential for noisy neighbor issues</item>
    /// <item>Less isolation for compliance-heavy scenarios</item>
    /// </list>
    /// </remarks>
    SharedSchema = 0,

    /// <summary>
    /// Each tenant has its own schema within a shared database.
    /// Tenant data is separated by database schema (e.g., tenant_abc.Orders).
    /// </summary>
    /// <remarks>
    /// <para><b>Pros:</b></para>
    /// <list type="bullet">
    /// <item>Better logical isolation than shared schema</item>
    /// <item>Per-tenant schema customization possible</item>
    /// <item>Easier per-tenant backup/restore than shared schema</item>
    /// <item>Shared infrastructure costs</item>
    /// </list>
    /// <para><b>Cons:</b></para>
    /// <list type="bullet">
    /// <item>Schema management complexity</item>
    /// <item>Connection pooling challenges</item>
    /// <item>Cross-tenant queries require schema switching</item>
    /// <item>Not supported by all databases equally</item>
    /// </list>
    /// </remarks>
    SchemaPerTenant = 1,

    /// <summary>
    /// Each tenant has its own dedicated database.
    /// Tenant data is completely separated at the database level.
    /// </summary>
    /// <remarks>
    /// <para><b>Pros:</b></para>
    /// <list type="bullet">
    /// <item>Maximum data isolation</item>
    /// <item>Per-tenant performance tuning</item>
    /// <item>Easy per-tenant backup, restore, and migration</item>
    /// <item>Compliance-friendly (HIPAA, GDPR, SOC2)</item>
    /// <item>No noisy neighbor issues</item>
    /// </list>
    /// <para><b>Cons:</b></para>
    /// <list type="bullet">
    /// <item>Highest infrastructure cost</item>
    /// <item>Complex connection management</item>
    /// <item>Cross-tenant queries require multiple connections</item>
    /// <item>More complex deployment and operations</item>
    /// </list>
    /// </remarks>
    DatabasePerTenant = 2
}
