using System.Text.Json.Serialization;

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
/// <para>
/// <see cref="ConnectionString"/> is a credential. It is marked <see cref="JsonIgnoreAttribute"/>
/// and masked by <see cref="ToString"/>, so logging, string interpolation and System.Text.Json
/// serialization never write it. As a consequence, a JSON round trip loses the connection string:
/// a <see cref="TenantInfo"/> deserialized from JSON always has a <c>null</c>
/// <see cref="ConnectionString"/>. Store tenant metadata with System.Text.Json only when the
/// connection string comes from another source (for example a secret store or configuration binding).
/// </para>
/// </remarks>
/// <param name="TenantId">The unique identifier for the tenant.</param>
/// <param name="Name">The display name of the tenant.</param>
/// <param name="Strategy">The isolation strategy used for this tenant's data.</param>
/// <param name="ConnectionString">
/// The connection string for database-per-tenant strategy.
/// <c>null</c> when using shared schema or schema-per-tenant strategies.
/// WARNING: contains sensitive credential data; it is excluded from JSON serialization and
/// from <see cref="ToString"/>.
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
    [property: JsonIgnore] string? ConnectionString = null,
    string? SchemaName = null)
{
    /// <summary>
    /// Returns a description of the tenant that never contains the connection string.
    /// </summary>
    /// <returns>
    /// The tenant identifier, name, strategy and schema name, plus
    /// <c>ConnectionString = ***</c> when a connection string is set.
    /// </returns>
    public override string ToString() =>
        ConnectionString is null
            ? $"TenantInfo {{ TenantId = {TenantId}, Name = {Name}, Strategy = {Strategy}, SchemaName = {SchemaName} }}"
            : $"TenantInfo {{ TenantId = {TenantId}, Name = {Name}, Strategy = {Strategy}, SchemaName = {SchemaName}, ConnectionString = *** }}";

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
