using Encina.Security.Audit;

namespace Encina.EntityFrameworkCore.Auditing;

/// <summary>
/// Entity Framework Core entity for persisting security audit entries.
/// </summary>
/// <remarks>
/// <para>
/// This entity maps to the "SecurityAuditEntries" table and stores comprehensive audit trail
/// information for CQRS request/response operations, supporting compliance requirements
/// such as SOX, HIPAA, and GDPR.
/// </para>
/// <para>
/// The entity is optimized for common query patterns:
/// <list type="bullet">
/// <item><description>Entity history lookups by type and ID (composite index)</description></item>
/// <item><description>Time-based queries for auditing reports (timestamp index)</description></item>
/// <item><description>User activity tracking (filtered index on UserId)</description></item>
/// <item><description>Correlation tracking for request tracing (filtered index on CorrelationId)</description></item>
/// <item><description>Tenant-scoped queries for multi-tenant applications (filtered index on TenantId)</description></item>
/// <item><description>Action-based filtering (filtered index on Action)</description></item>
/// <item><description>Outcome-based filtering (index on Outcome)</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class AuditEntryEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for this audit entry.
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the correlation ID for distributed tracing.
    /// </summary>
    public required string CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the user ID who initiated the operation, or <c>null</c> if unauthenticated.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Gets or sets the tenant ID for multi-tenant applications.
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Gets or sets the action performed (e.g., "Create", "Update", "Delete", "Get").
    /// </summary>
    public required string Action { get; set; }

    /// <summary>
    /// Gets or sets the type of entity being operated on (e.g., "Order", "Customer").
    /// </summary>
    public required string EntityType { get; set; }

    /// <summary>
    /// Gets or sets the specific entity identifier, if applicable.
    /// </summary>
    public string? EntityId { get; set; }

    /// <summary>
    /// Gets or sets the outcome of the operation.
    /// </summary>
    public AuditOutcome Outcome { get; set; }

    /// <summary>
    /// Gets or sets the error message when outcome is not Success.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the operation completed (for backward compatibility).
    /// </summary>
    public DateTime TimestampUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the operation started.
    /// </summary>
    public DateTimeOffset StartedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the operation completed.
    /// </summary>
    public DateTimeOffset CompletedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the IP address of the client that initiated the request.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Gets or sets the User-Agent header from the HTTP request.
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Gets or sets the SHA-256 hash of the sanitized request payload.
    /// </summary>
    public string? RequestPayloadHash { get; set; }

    /// <summary>
    /// Gets or sets the full JSON representation of the request payload after sensitive data redaction.
    /// </summary>
    public string? RequestPayload { get; set; }

    /// <summary>
    /// Gets or sets the full JSON representation of the response payload after sensitive data redaction.
    /// </summary>
    public string? ResponsePayload { get; set; }

    /// <summary>
    /// Gets or sets the JSON-serialized additional metadata.
    /// </summary>
    /// <remarks>
    /// Stored as JSON string in the database for flexibility.
    /// Deserialized to <c>Dictionary&lt;string, object?&gt;</c> when mapping to <see cref="AuditEntry"/>.
    /// </remarks>
    public string? Metadata { get; set; }
}
