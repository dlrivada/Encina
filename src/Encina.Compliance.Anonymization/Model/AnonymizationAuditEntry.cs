namespace Encina.Compliance.Anonymization.Model;

/// <summary>
/// Represents an entry in the anonymization audit trail for demonstrating compliance.
/// </summary>
/// <remarks>
/// <para>
/// Each audit entry records a specific anonymization, pseudonymization, tokenization,
/// or key management action. The audit trail is immutable and provides evidence
/// of compliance with GDPR obligations regarding data protection measures.
/// </para>
/// <para>
/// Per GDPR Article 5(2) (accountability principle), controllers must demonstrate
/// compliance with data protection principles. Audit entries provide a complete,
/// immutable record of all anonymization-related actions applied to personal data.
/// </para>
/// <para>
/// Audit entries should never be modified or deleted. They serve as legal evidence
/// of the anonymization and pseudonymization measures applied and may be required
/// during regulatory audits or data subject access requests (Article 15).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var entry = AnonymizationAuditEntry.Create(
///     operation: AnonymizationOperation.Pseudonymized,
///     technique: AnonymizationTechnique.DataMasking,
///     fieldName: "Email",
///     subjectId: "user-123",
///     keyId: "key-2025-01",
///     performedByUserId: "admin@company.com");
///
/// await auditStore.AddEntryAsync(entry, cancellationToken);
/// </code>
/// </example>
public sealed record AnonymizationAuditEntry
{
    /// <summary>
    /// Unique identifier for this audit entry.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Identifier of the data subject whose data was affected.
    /// </summary>
    /// <remarks>
    /// <c>null</c> for operations that are not subject-specific, such as
    /// <see cref="AnonymizationOperation.KeyRotated"/> or
    /// <see cref="AnonymizationOperation.RiskAssessed"/>.
    /// </remarks>
    public string? SubjectId { get; init; }

    /// <summary>
    /// The type of anonymization operation that was performed.
    /// </summary>
    public required AnonymizationOperation Operation { get; init; }

    /// <summary>
    /// The anonymization technique applied during the operation.
    /// </summary>
    /// <remarks>
    /// <c>null</c> for operations that do not apply a specific technique,
    /// such as <see cref="AnonymizationOperation.Detokenized"/>,
    /// <see cref="AnonymizationOperation.KeyRotated"/>, or
    /// <see cref="AnonymizationOperation.RiskAssessed"/>.
    /// </remarks>
    public AnonymizationTechnique? Technique { get; init; }

    /// <summary>
    /// The name of the data field that was anonymized, pseudonymized, or tokenized.
    /// </summary>
    /// <remarks>
    /// <c>null</c> for operations that affect multiple fields or are not field-specific,
    /// such as <see cref="AnonymizationOperation.KeyRotated"/>.
    /// </remarks>
    public string? FieldName { get; init; }

    /// <summary>
    /// Identifier of the cryptographic key used in the operation.
    /// </summary>
    /// <remarks>
    /// <c>null</c> for operations that do not involve cryptographic keys,
    /// such as <see cref="AnonymizationOperation.Anonymized"/> (irreversible,
    /// no key required).
    /// </remarks>
    public string? KeyId { get; init; }

    /// <summary>
    /// Timestamp when the operation was performed (UTC).
    /// </summary>
    public required DateTimeOffset PerformedAtUtc { get; init; }

    /// <summary>
    /// Identifier of the user or system that performed the operation.
    /// </summary>
    /// <remarks>
    /// <c>null</c> for automated system actions (e.g., scheduled key rotation
    /// or pipeline-triggered anonymization).
    /// </remarks>
    public string? PerformedByUserId { get; init; }

    /// <summary>
    /// Creates a new anonymization audit entry with a generated unique identifier
    /// and the current UTC timestamp.
    /// </summary>
    /// <param name="operation">The type of anonymization operation performed.</param>
    /// <param name="technique">The anonymization technique applied, if applicable.</param>
    /// <param name="fieldName">The name of the affected field, if applicable.</param>
    /// <param name="subjectId">The identifier of the affected data subject, if applicable.</param>
    /// <param name="keyId">The identifier of the cryptographic key used, if applicable.</param>
    /// <param name="performedByUserId">The identifier of the actor who performed the operation.</param>
    /// <returns>A new <see cref="AnonymizationAuditEntry"/> with a generated GUID identifier.</returns>
    public static AnonymizationAuditEntry Create(
        AnonymizationOperation operation,
        AnonymizationTechnique? technique = null,
        string? fieldName = null,
        string? subjectId = null,
        string? keyId = null,
        string? performedByUserId = null) =>
        new()
        {
            Id = Guid.NewGuid().ToString("N"),
            SubjectId = subjectId,
            Operation = operation,
            Technique = technique,
            FieldName = fieldName,
            KeyId = keyId,
            PerformedAtUtc = DateTimeOffset.UtcNow,
            PerformedByUserId = performedByUserId
        };
}
