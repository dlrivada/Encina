namespace Encina.Security.Audit;

/// <summary>
/// Persistence entity for <see cref="ReadAuditEntry"/> records.
/// </summary>
/// <remarks>
/// <para>
/// This POCO maps to the "ReadAuditEntries" table and stores read access audit trail
/// information for entities implementing <c>IReadAuditable</c>. Used by EF Core,
/// ADO.NET, and Dapper store implementations.
/// </para>
/// <para>
/// Key differences from <c>AuditEntryEntity</c> (write audit):
/// <list type="bullet">
/// <item><description>No request/response payload fields — reads don't modify data</description></item>
/// <item><description>Includes <see cref="Purpose"/> for GDPR Art. 15 compliance</description></item>
/// <item><description>Includes <see cref="AccessMethod"/> stored as <see cref="int"/> (enum ordinal)</description></item>
/// <item><description>Includes <see cref="EntityCount"/> for bulk read volume tracking</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class ReadAuditEntryEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for this read audit entry.
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the type of entity that was accessed (e.g., "Patient", "FinancialRecord").
    /// </summary>
    public required string EntityType { get; set; }

    /// <summary>
    /// Gets or sets the specific entity identifier, or <c>null</c> for bulk operations.
    /// </summary>
    public string? EntityId { get; set; }

    /// <summary>
    /// Gets or sets the user ID who accessed the data, or <c>null</c> for system access.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Gets or sets the tenant ID for multi-tenant applications.
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the data was accessed.
    /// </summary>
    public required DateTimeOffset AccessedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the correlation ID for distributed tracing.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the declared purpose for accessing the data (GDPR Art. 15).
    /// </summary>
    public string? Purpose { get; set; }

    /// <summary>
    /// Gets or sets the access method as an integer (ordinal of <see cref="ReadAccessMethod"/>).
    /// </summary>
    public int AccessMethod { get; set; }

    /// <summary>
    /// Gets or sets the number of entities returned by the read operation.
    /// </summary>
    public int EntityCount { get; set; }

    /// <summary>
    /// Gets or sets the JSON-serialized additional metadata.
    /// </summary>
    /// <remarks>
    /// Stored as JSON string in the database for flexibility.
    /// Deserialized to <c>Dictionary&lt;string, object?&gt;</c> when mapping to <see cref="ReadAuditEntry"/>.
    /// </remarks>
    public string? Metadata { get; set; }
}
