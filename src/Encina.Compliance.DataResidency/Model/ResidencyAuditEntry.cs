namespace Encina.Compliance.DataResidency.Model;

/// <summary>
/// Represents an entry in the data residency audit trail for demonstrating compliance.
/// </summary>
/// <remarks>
/// <para>
/// Each audit entry records a specific action taken by the residency enforcement system:
/// policy checks, cross-border transfer validations, data location recordings,
/// policy violations, and region routing decisions.
/// </para>
/// <para>
/// Per GDPR Article 5(2) (accountability principle), controllers must demonstrate
/// compliance with data protection principles. Per Article 30, controllers must
/// maintain records of processing activities including transfers of personal data
/// to a third country. Residency audit entries provide a complete, immutable record
/// of all residency enforcement decisions and cross-border transfer validations.
/// </para>
/// <para>
/// Audit entries should never be modified or deleted. They serve as legal evidence
/// of data residency compliance and may be required during regulatory audits or
/// supervisory authority inquiries (Article 58).
/// </para>
/// </remarks>
public sealed record ResidencyAuditEntry
{
    /// <summary>
    /// Unique identifier for this audit entry.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Identifier of the data entity affected by this action.
    /// </summary>
    /// <remarks>
    /// <c>null</c> for actions that are not entity-specific, such as
    /// global policy checks or enforcement summaries.
    /// </remarks>
    public string? EntityId { get; init; }

    /// <summary>
    /// The data category affected by this action.
    /// </summary>
    /// <remarks>
    /// Examples: "personal-data", "financial-records", "healthcare-data".
    /// Corresponds to the categories used in residency policies.
    /// </remarks>
    public required string DataCategory { get; init; }

    /// <summary>
    /// The source region code for this action.
    /// </summary>
    /// <remarks>
    /// The region from which the data is being transferred or where the
    /// request originated. Stored as a string code for serialization compatibility.
    /// </remarks>
    public required string SourceRegion { get; init; }

    /// <summary>
    /// The target region code for this action.
    /// </summary>
    /// <remarks>
    /// <c>null</c> when no cross-border transfer is involved (e.g., policy checks
    /// for data staying within the same region). For cross-border transfers, this
    /// is the destination region.
    /// </remarks>
    public string? TargetRegion { get; init; }

    /// <summary>
    /// The type of action that was performed.
    /// </summary>
    public required ResidencyAction Action { get; init; }

    /// <summary>
    /// The outcome of the action.
    /// </summary>
    public required ResidencyOutcome Outcome { get; init; }

    /// <summary>
    /// The legal basis applied for the action, if applicable.
    /// </summary>
    /// <remarks>
    /// <c>null</c> when no legal basis is needed (e.g., intra-EEA transfers)
    /// or when the action was blocked. Contains the GDPR article reference
    /// or transfer mechanism name (e.g., "AdequacyDecision", "StandardContractualClauses").
    /// </remarks>
    public string? LegalBasis { get; init; }

    /// <summary>
    /// The type of the request that triggered this action.
    /// </summary>
    /// <remarks>
    /// <c>null</c> for actions not triggered by a pipeline request.
    /// Contains the fully qualified type name of the request.
    /// </remarks>
    public string? RequestType { get; init; }

    /// <summary>
    /// Identifier of the user or system that triggered this action.
    /// </summary>
    /// <remarks>
    /// <c>null</c> for automated system actions (e.g., pipeline enforcement,
    /// scheduled policy checks).
    /// </remarks>
    public string? UserId { get; init; }

    /// <summary>
    /// Timestamp when the action occurred (UTC).
    /// </summary>
    public required DateTimeOffset TimestampUtc { get; init; }

    /// <summary>
    /// Additional details about the action.
    /// </summary>
    /// <remarks>
    /// <c>null</c> when no additional context is needed. For violations, this
    /// may contain the denial reason. For transfers, this may contain the
    /// safeguards applied.
    /// </remarks>
    public string? Details { get; init; }

    /// <summary>
    /// Creates a new residency audit entry with a generated unique identifier
    /// and the current UTC timestamp.
    /// </summary>
    /// <param name="dataCategory">The data category affected.</param>
    /// <param name="sourceRegion">The source region code.</param>
    /// <param name="action">The type of action performed.</param>
    /// <param name="outcome">The outcome of the action.</param>
    /// <param name="entityId">Identifier of the affected entity, if applicable.</param>
    /// <param name="targetRegion">The target region code, if applicable.</param>
    /// <param name="legalBasis">The legal basis applied, if applicable.</param>
    /// <param name="requestType">The request type that triggered the action.</param>
    /// <param name="userId">Identifier of the user who triggered the action.</param>
    /// <param name="details">Additional details about the action.</param>
    /// <returns>A new <see cref="ResidencyAuditEntry"/> with a generated GUID identifier.</returns>
    public static ResidencyAuditEntry Create(
        string dataCategory,
        string sourceRegion,
        ResidencyAction action,
        ResidencyOutcome outcome,
        string? entityId = null,
        string? targetRegion = null,
        string? legalBasis = null,
        string? requestType = null,
        string? userId = null,
        string? details = null) =>
        new()
        {
            Id = Guid.NewGuid().ToString("N"),
            DataCategory = dataCategory,
            SourceRegion = sourceRegion,
            Action = action,
            Outcome = outcome,
            EntityId = entityId,
            TargetRegion = targetRegion,
            LegalBasis = legalBasis,
            RequestType = requestType,
            UserId = userId,
            TimestampUtc = DateTimeOffset.UtcNow,
            Details = details
        };
}
