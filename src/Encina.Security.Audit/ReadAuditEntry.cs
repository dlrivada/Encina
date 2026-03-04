namespace Encina.Security.Audit;

/// <summary>
/// Represents an immutable audit trail entry for a read access operation on a sensitive entity.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ReadAuditEntry"/> captures information about who accessed sensitive data, when,
/// and for what purpose. Unlike <see cref="AuditEntry"/> which tracks CUD (Create, Update, Delete)
/// operations at the CQRS pipeline level, <see cref="ReadAuditEntry"/> tracks read operations
/// at the data access (repository) level.
/// </para>
/// <para>
/// This record supports compliance requirements across multiple regulations:
/// <list type="bullet">
/// <item><b>GDPR Art. 15</b> — Right of access: the <see cref="Purpose"/> field records why data was accessed</item>
/// <item><b>HIPAA §164.312(b)</b> — Audit controls: tracks access to protected health information</item>
/// <item><b>SOX §302/§404</b> — Internal controls: records access to financial data</item>
/// <item><b>PCI-DSS Req. 10.2</b> — Logging: monitors access to cardholder data</item>
/// </list>
/// </para>
/// <para>
/// Key differences from <see cref="AuditEntry"/>:
/// <list type="bullet">
/// <item>No request/response payload fields — reads don't modify data</item>
/// <item>No payload hash — tamper detection is irrelevant for reads</item>
/// <item>Includes <see cref="Purpose"/> — required by GDPR Art. 15 for access justification</item>
/// <item>Includes <see cref="AccessMethod"/> — categorizes how the data was accessed</item>
/// <item>Includes <see cref="EntityCount"/> — tracks bulk read volume for exfiltration detection</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var entry = new ReadAuditEntry
/// {
///     Id = Guid.NewGuid(),
///     EntityType = "Patient",
///     EntityId = "PAT-12345",
///     UserId = context.UserId,
///     TenantId = context.TenantId,
///     AccessedAtUtc = timeProvider.GetUtcNow(),
///     CorrelationId = context.CorrelationId,
///     Purpose = "Patient care review",
///     AccessMethod = ReadAccessMethod.Repository,
///     EntityCount = 1
/// };
///
/// await readAuditStore.LogReadAsync(entry, cancellationToken);
/// </code>
/// </example>
public sealed record ReadAuditEntry
{
    /// <summary>
    /// Unique identifier for this read audit entry.
    /// </summary>
    /// <remarks>
    /// Auto-generated GUID that uniquely identifies each read access record.
    /// Used as the primary key in persistent stores.
    /// </remarks>
    public required Guid Id { get; init; }

    /// <summary>
    /// The type of entity that was accessed (e.g., "Patient", "FinancialRecord", "Customer").
    /// </summary>
    /// <remarks>
    /// Typically the short name of the entity class (e.g., <c>typeof(Patient).Name</c>).
    /// Used to filter access history by entity type and for compliance reporting.
    /// </remarks>
    public required string EntityType { get; init; }

    /// <summary>
    /// The specific entity identifier that was accessed.
    /// </summary>
    /// <remarks>
    /// The string representation of the entity's primary key.
    /// <c>null</c> for bulk operations (e.g., <c>GetAllAsync</c>, <c>FindAsync</c>)
    /// where no specific entity is targeted.
    /// </remarks>
    public required string? EntityId { get; init; }

    /// <summary>
    /// User ID who accessed the data.
    /// </summary>
    /// <remarks>
    /// <c>null</c> for system-initiated or background access.
    /// Propagated from <see cref="IRequestContext.UserId"/>.
    /// Critical for answering "who accessed this data?" queries required by GDPR Art. 15.
    /// </remarks>
    public string? UserId { get; init; }

    /// <summary>
    /// Tenant ID for multi-tenant applications.
    /// </summary>
    /// <remarks>
    /// <c>null</c> for single-tenant applications or cross-tenant operations.
    /// Propagated from <see cref="IRequestContext.TenantId"/>.
    /// Enables tenant-isolated access audit queries.
    /// </remarks>
    public string? TenantId { get; init; }

    /// <summary>
    /// UTC timestamp when the data was accessed.
    /// </summary>
    /// <remarks>
    /// Captured at the moment the read operation completes.
    /// Always stored in UTC for consistent cross-timezone queries.
    /// </remarks>
    public required DateTimeOffset AccessedAtUtc { get; init; }

    /// <summary>
    /// Correlation ID for distributed tracing.
    /// </summary>
    /// <remarks>
    /// Links this read access record to related operations across services.
    /// Propagated from <see cref="IRequestContext.CorrelationId"/>.
    /// </remarks>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// The declared purpose for accessing this data.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Supports GDPR Art. 15 compliance — when a data subject exercises their right of access,
    /// the controller must provide information about the purposes of processing.
    /// Recording the access purpose at read time creates an auditable trail of data usage.
    /// </para>
    /// <para>
    /// <c>null</c> when purpose tracking is not enforced or not provided.
    /// When <c>ReadAuditOptions.RequirePurpose</c> is <c>true</c>,
    /// a warning is logged for accesses without a declared purpose.
    /// </para>
    /// </remarks>
    public string? Purpose { get; init; }

    /// <summary>
    /// How the data was accessed.
    /// </summary>
    /// <remarks>
    /// Categorizes the access vector for security analysis.
    /// Defaults to <see cref="ReadAccessMethod.Repository"/> when populated by the repository decorator.
    /// See <see cref="ReadAccessMethod"/> for all supported categories.
    /// </remarks>
    public required ReadAccessMethod AccessMethod { get; init; }

    /// <summary>
    /// Number of entities returned by the read operation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For single-entity reads (<c>GetByIdAsync</c>), this is <c>1</c> when found or <c>0</c> when not found.
    /// For bulk reads (<c>FindAsync</c>, <c>GetAllAsync</c>, <c>GetPagedAsync</c>), this reflects
    /// the actual number of entities returned.
    /// </para>
    /// <para>
    /// High entity counts may indicate data exfiltration attempts and can trigger
    /// security alerts when combined with breach detection rules.
    /// </para>
    /// </remarks>
    public required int EntityCount { get; init; }

    /// <summary>
    /// Additional custom metadata for extensibility.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Allows applications to attach domain-specific context to read access records
    /// without schema changes. Examples include department identifiers, device information,
    /// or application-specific access classifications.
    /// </para>
    /// <para>
    /// Returns an empty dictionary if no metadata is set.
    /// </para>
    /// </remarks>
    public IReadOnlyDictionary<string, object?> Metadata { get; init; } = new Dictionary<string, object?>();
}
