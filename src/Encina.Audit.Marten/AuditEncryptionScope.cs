namespace Encina.Audit.Marten;

/// <summary>
/// Defines which fields of audit entries are encrypted with temporal keys.
/// </summary>
/// <remarks>
/// <para>
/// Controls the trade-off between privacy and queryability after crypto-shredding.
/// When temporal keys are destroyed, encrypted fields become permanently unreadable
/// and are replaced with <see cref="MartenAuditOptions.ShreddedPlaceholder"/> in query results.
/// </para>
/// <para>
/// <b>PII fields</b> (encrypted with <see cref="PiiFieldsOnly"/>):
/// <c>UserId</c>, <c>IpAddress</c>, <c>UserAgent</c>, <c>RequestPayload</c>,
/// <c>ResponsePayload</c>, <c>Metadata</c>.
/// </para>
/// <para>
/// <b>Structural fields</b> (always plaintext):
/// <c>Action</c>, <c>EntityType</c>, <c>EntityId</c>, <c>Outcome</c>,
/// <c>TimestampUtc</c>, <c>CorrelationId</c>, <c>TenantId</c>,
/// <c>RequestPayloadHash</c>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddEncinaAuditMarten(options =>
/// {
///     // Only PII fields are encrypted — structural fields remain queryable after shredding
///     options.EncryptionScope = AuditEncryptionScope.PiiFieldsOnly;
///
///     // All fields encrypted — nothing queryable after shredding (maximum privacy)
///     // options.EncryptionScope = AuditEncryptionScope.AllFields;
/// });
/// </code>
/// </example>
public enum AuditEncryptionScope
{
    /// <summary>
    /// Encrypt only PII-sensitive fields (UserId, IpAddress, UserAgent, payloads, Metadata).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Recommended for most compliance scenarios. After crypto-shredding:
    /// <list type="bullet">
    /// <item>Structural fields remain queryable — compliance officers can see
    /// "someone did X to entity Y at time Z" without knowing who.</item>
    /// <item>PII fields show <see cref="MartenAuditOptions.ShreddedPlaceholder"/>.</item>
    /// <item>Audit entry counts, outcome distributions, and timeline analysis remain possible.</item>
    /// </list>
    /// </para>
    /// </remarks>
    PiiFieldsOnly = 0,

    /// <summary>
    /// Encrypt all fields in the audit entry event payload.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Maximum privacy mode. After crypto-shredding, the entire audit entry becomes
    /// unreadable and projections cannot reconstruct any meaningful data.
    /// Use only when even structural metadata (action, entity type) is considered sensitive.
    /// </para>
    /// <para>
    /// <b>Warning:</b> This significantly limits post-shredding forensic and compliance
    /// analysis capabilities.
    /// </para>
    /// </remarks>
    AllFields = 1
}
