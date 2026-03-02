namespace Encina.Compliance.DataResidency.Model;

/// <summary>
/// Result of a cross-border data transfer validation.
/// </summary>
/// <remarks>
/// <para>
/// When data is transferred between regions, the <c>ICrossBorderTransferValidator</c>
/// evaluates the transfer against GDPR Chapter V requirements and produces this result.
/// The result indicates whether the transfer is allowed, the legal basis (if applicable),
/// any required safeguards, and warnings or denial reasons.
/// </para>
/// <para>
/// The validation hierarchy follows GDPR preference order:
/// 1. Adequacy decision (Art. 45) — no additional safeguards needed
/// 2. Appropriate safeguards (Art. 46) — SCCs, BCRs, codes of conduct
/// 3. Derogations (Art. 49) — explicit consent, public interest, vital interests
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Allowed transfer (intra-EEA)
/// var result = TransferValidationResult.Allowed(TransferLegalBasis.AdequacyDecision);
///
/// // Blocked transfer
/// var result = TransferValidationResult.Denied("No adequacy decision and no appropriate safeguards configured.");
/// </code>
/// </example>
public sealed record TransferValidationResult
{
    /// <summary>
    /// Whether the cross-border transfer is allowed.
    /// </summary>
    public required bool IsAllowed { get; init; }

    /// <summary>
    /// The legal basis under which the transfer is permitted.
    /// </summary>
    /// <remarks>
    /// <c>null</c> when the transfer is denied or when no specific legal basis applies
    /// (e.g., intra-EEA transfers where GDPR applies directly).
    /// </remarks>
    public TransferLegalBasis? LegalBasis { get; init; }

    /// <summary>
    /// Safeguards required for this transfer to be compliant.
    /// </summary>
    /// <remarks>
    /// Examples: "Transfer Impact Assessment required", "Supplementary measures per Schrems II",
    /// "Data encryption in transit and at rest". Empty when no additional safeguards are needed
    /// (e.g., transfers based on adequacy decisions).
    /// </remarks>
    public required IReadOnlyList<string> RequiredSafeguards { get; init; }

    /// <summary>
    /// Non-blocking warnings about the transfer.
    /// </summary>
    /// <remarks>
    /// Warnings highlight potential compliance concerns that do not block the transfer
    /// but should be reviewed. Examples: "Adequacy decision expires in 90 days",
    /// "Country under EU monitoring for data protection adequacy".
    /// </remarks>
    public required IReadOnlyList<string> Warnings { get; init; }

    /// <summary>
    /// Reason why the transfer was denied.
    /// </summary>
    /// <remarks>
    /// <c>null</c> when the transfer is allowed. Contains a human-readable explanation
    /// when <see cref="IsAllowed"/> is <c>false</c>.
    /// </remarks>
    public string? DenialReason { get; init; }

    /// <summary>
    /// Creates a result indicating the transfer is allowed with the specified legal basis.
    /// </summary>
    /// <param name="legalBasis">The legal basis under which the transfer is permitted.</param>
    /// <param name="requiredSafeguards">Any safeguards required for compliance.</param>
    /// <param name="warnings">Any non-blocking warnings.</param>
    /// <returns>An allowed <see cref="TransferValidationResult"/>.</returns>
    public static TransferValidationResult Allow(
        TransferLegalBasis legalBasis,
        IReadOnlyList<string>? requiredSafeguards = null,
        IReadOnlyList<string>? warnings = null) =>
        new()
        {
            IsAllowed = true,
            LegalBasis = legalBasis,
            RequiredSafeguards = requiredSafeguards ?? [],
            Warnings = warnings ?? []
        };

    /// <summary>
    /// Creates a result indicating the transfer is denied.
    /// </summary>
    /// <param name="reason">Human-readable reason for the denial.</param>
    /// <returns>A denied <see cref="TransferValidationResult"/>.</returns>
    public static TransferValidationResult Deny(string reason) =>
        new()
        {
            IsAllowed = false,
            DenialReason = reason,
            RequiredSafeguards = [],
            Warnings = []
        };
}
