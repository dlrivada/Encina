namespace Encina.Security.Audit;

/// <summary>
/// Represents an immutable audit trail entry for a single operation.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="AuditEntry"/> captures comprehensive information about each audited operation,
/// supporting compliance requirements such as SOX, HIPAA, and GDPR.
/// </para>
/// <para>
/// Key features:
/// <list type="bullet">
/// <item><b>Immutability</b> - All properties are init-only to ensure audit integrity</item>
/// <item><b>Tamper detection</b> - <see cref="RequestPayloadHash"/> provides SHA-256 hash of the original request</item>
/// <item><b>Correlation</b> - <see cref="CorrelationId"/> enables distributed tracing across services</item>
/// <item><b>Multi-tenancy</b> - <see cref="TenantId"/> supports tenant-isolated audit queries</item>
/// <item><b>Extensibility</b> - <see cref="Metadata"/> allows custom fields without schema changes</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var entry = new AuditEntry
/// {
///     Id = Guid.NewGuid(),
///     CorrelationId = context.CorrelationId,
///     UserId = context.UserId,
///     TenantId = context.TenantId,
///     Action = "Create",
///     EntityType = "Order",
///     EntityId = "ORD-12345",
///     Outcome = AuditOutcome.Success,
///     TimestampUtc = DateTime.UtcNow,
///     RequestPayloadHash = "a1b2c3d4..."
/// };
/// </code>
/// </example>
public sealed record AuditEntry
{
    /// <summary>
    /// Unique identifier for this audit entry.
    /// </summary>
    /// <remarks>
    /// Auto-generated GUID that uniquely identifies each audit record.
    /// Used as the primary key in persistent stores.
    /// </remarks>
    public required Guid Id { get; init; }

    /// <summary>
    /// Correlation ID for distributed tracing.
    /// </summary>
    /// <remarks>
    /// Links this audit entry to related operations across services.
    /// Typically propagated from <see cref="IRequestContext.CorrelationId"/>.
    /// </remarks>
    public required string CorrelationId { get; init; }

    /// <summary>
    /// User ID who initiated the operation.
    /// </summary>
    /// <remarks>
    /// <c>null</c> for unauthenticated or system-initiated operations.
    /// Critical for access audits and user activity reports.
    /// </remarks>
    public string? UserId { get; init; }

    /// <summary>
    /// Tenant ID for multi-tenant applications.
    /// </summary>
    /// <remarks>
    /// <c>null</c> for single-tenant applications or cross-tenant operations.
    /// Enables tenant-scoped audit queries and data isolation.
    /// </remarks>
    public string? TenantId { get; init; }

    /// <summary>
    /// The action performed (e.g., "Create", "Update", "Delete", "Get").
    /// </summary>
    /// <remarks>
    /// Typically extracted from the request type name (e.g., <c>CreateOrderCommand</c> → "Create")
    /// or overridden via the <c>[Auditable]</c> attribute.
    /// </remarks>
    public required string Action { get; init; }

    /// <summary>
    /// The type of entity being operated on (e.g., "Order", "Customer", "Product").
    /// </summary>
    /// <remarks>
    /// Typically extracted from the request type name (e.g., <c>CreateOrderCommand</c> → "Order")
    /// or overridden via the <c>[Auditable]</c> attribute.
    /// </remarks>
    public required string EntityType { get; init; }

    /// <summary>
    /// The specific entity identifier, if applicable.
    /// </summary>
    /// <remarks>
    /// <c>null</c> for operations that don't target a specific entity (e.g., list operations).
    /// Extracted from common request properties like <c>Id</c>, <c>EntityId</c>, or <c>[Entity]Id</c>.
    /// </remarks>
    public string? EntityId { get; init; }

    /// <summary>
    /// The outcome of the operation.
    /// </summary>
    /// <remarks>
    /// Mapped from the <c>Either</c> result:
    /// <list type="bullet">
    /// <item><see cref="AuditOutcome.Success"/> - <c>Either.Right</c></item>
    /// <item><see cref="AuditOutcome.Failure"/> - <c>Either.Left</c> with validation errors</item>
    /// <item><see cref="AuditOutcome.Denied"/> - <c>Either.Left</c> with authorization errors</item>
    /// <item><see cref="AuditOutcome.Error"/> - <c>Either.Left</c> with system errors</item>
    /// </list>
    /// </remarks>
    public required AuditOutcome Outcome { get; init; }

    /// <summary>
    /// Error message when <see cref="Outcome"/> is not <see cref="AuditOutcome.Success"/>.
    /// </summary>
    /// <remarks>
    /// Contains the error message from <see cref="EncinaError.Message"/> for failed operations.
    /// <c>null</c> for successful operations.
    /// </remarks>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// UTC timestamp when the operation was executed.
    /// </summary>
    /// <remarks>
    /// Captured at the time of audit entry creation, representing when the operation completed.
    /// Always stored in UTC for consistent cross-timezone queries.
    /// </remarks>
    public required DateTime TimestampUtc { get; init; }

    /// <summary>
    /// IP address of the client that initiated the request.
    /// </summary>
    /// <remarks>
    /// <c>null</c> for non-HTTP operations or when IP cannot be determined.
    /// Extracted from <c>X-Forwarded-For</c> header or <c>HttpContext.Connection.RemoteIpAddress</c>.
    /// </remarks>
    public string? IpAddress { get; init; }

    /// <summary>
    /// User-Agent header from the HTTP request.
    /// </summary>
    /// <remarks>
    /// <c>null</c> for non-HTTP operations or when User-Agent is not present.
    /// Useful for identifying client applications and browsers.
    /// </remarks>
    public string? UserAgent { get; init; }

    /// <summary>
    /// SHA-256 hash of the sanitized request payload.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Provides tamper detection without storing sensitive data.
    /// The hash is computed from the JSON-serialized request after PII masking.
    /// </para>
    /// <para>
    /// <c>null</c> when payload hashing is disabled via <c>AuditOptions.IncludePayloadHash</c>.
    /// </para>
    /// </remarks>
    public string? RequestPayloadHash { get; init; }

    /// <summary>
    /// Additional custom metadata for extensibility.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Allows applications to attach domain-specific audit data without schema changes.
    /// Examples include workflow states, approval chains, or compliance flags.
    /// </para>
    /// <para>
    /// Returns an empty dictionary if no metadata is set.
    /// </para>
    /// </remarks>
    public IReadOnlyDictionary<string, object?> Metadata { get; init; } = new Dictionary<string, object?>();
}
