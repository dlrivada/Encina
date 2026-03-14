namespace Encina.Compliance.CrossBorderTransfer.Model;

/// <summary>
/// Represents the result of validating an international data transfer against
/// GDPR Chapter V requirements and the Schrems II judgment.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="TransferValidationOutcome"/> captures whether a proposed transfer is allowed,
/// the legal basis under which it was authorized (or the reason it was blocked), and any
/// additional requirements such as supplementary measures or Transfer Impact Assessments.
/// </para>
/// <para>
/// Use the factory methods <see cref="Allow"/> and <see cref="Block"/> to create instances,
/// ensuring consistent initialization of required fields.
/// </para>
/// </remarks>
public sealed record TransferValidationOutcome
{
    /// <summary>
    /// Indicates whether the proposed transfer is allowed to proceed.
    /// </summary>
    public required bool IsAllowed { get; init; }

    /// <summary>
    /// The legal basis under which the transfer was authorized, or <see cref="TransferBasis.Blocked"/>
    /// if the transfer is not allowed.
    /// </summary>
    public required TransferBasis Basis { get; init; }

    /// <summary>
    /// Descriptions of supplementary measures required for the transfer to proceed.
    /// </summary>
    /// <remarks>
    /// Empty when no supplementary measures are needed (e.g., adequacy decision transfers).
    /// When populated, these measures must be implemented before the transfer is authorized.
    /// </remarks>
    public required IReadOnlyList<string> SupplementaryMeasuresRequired { get; init; }

    /// <summary>
    /// Indicates whether a Transfer Impact Assessment (TIA) is required before the transfer
    /// can be authorized.
    /// </summary>
    /// <remarks>
    /// A TIA is typically required for transfers based on SCCs or BCRs to countries without
    /// an adequacy decision, as mandated by the Schrems II judgment.
    /// </remarks>
    public required bool TIARequired { get; init; }

    /// <summary>
    /// The reason the transfer was blocked, if applicable.
    /// </summary>
    /// <remarks>
    /// <c>null</c> when <see cref="IsAllowed"/> is <c>true</c>. Contains a human-readable
    /// explanation of why the transfer cannot proceed.
    /// </remarks>
    public string? BlockReason { get; init; }

    /// <summary>
    /// The SCC module required for the transfer, if the basis is <see cref="TransferBasis.SCCs"/>.
    /// </summary>
    /// <remarks>
    /// <c>null</c> when the transfer basis is not SCCs. When populated, indicates which
    /// SCC module (<see cref="SCCModule"/>) must be executed between the parties.
    /// </remarks>
    public SCCModule? SCCModuleRequired { get; init; }

    /// <summary>
    /// Non-blocking warnings about the transfer that should be logged or surfaced to the user.
    /// </summary>
    /// <remarks>
    /// Warnings do not prevent the transfer from proceeding but indicate conditions that
    /// should be monitored, such as upcoming SCC expiration, pending TIA reassessment,
    /// or changes in the destination country's legal framework.
    /// </remarks>
    public required IReadOnlyList<string> Warnings { get; init; }

    /// <summary>
    /// Creates an outcome indicating the transfer is allowed under the specified legal basis.
    /// </summary>
    /// <param name="basis">The legal basis authorizing the transfer.</param>
    /// <param name="supplementaryMeasuresRequired">Descriptions of required supplementary measures, if any.</param>
    /// <param name="tiaRequired">Whether a TIA is required.</param>
    /// <param name="sccModuleRequired">The SCC module required, if applicable.</param>
    /// <param name="warnings">Non-blocking warnings, if any.</param>
    /// <returns>A <see cref="TransferValidationOutcome"/> indicating an allowed transfer.</returns>
    public static TransferValidationOutcome Allow(
        TransferBasis basis,
        IReadOnlyList<string>? supplementaryMeasuresRequired = null,
        bool tiaRequired = false,
        SCCModule? sccModuleRequired = null,
        IReadOnlyList<string>? warnings = null) =>
        new()
        {
            IsAllowed = true,
            Basis = basis,
            SupplementaryMeasuresRequired = supplementaryMeasuresRequired ?? [],
            TIARequired = tiaRequired,
            SCCModuleRequired = sccModuleRequired,
            Warnings = warnings ?? []
        };

    /// <summary>
    /// Creates an outcome indicating the transfer is blocked.
    /// </summary>
    /// <param name="reason">A human-readable explanation of why the transfer is blocked.</param>
    /// <param name="warnings">Non-blocking warnings providing additional context.</param>
    /// <returns>A <see cref="TransferValidationOutcome"/> indicating a blocked transfer.</returns>
    public static TransferValidationOutcome Block(
        string reason,
        IReadOnlyList<string>? warnings = null) =>
        new()
        {
            IsAllowed = false,
            Basis = TransferBasis.Blocked,
            SupplementaryMeasuresRequired = [],
            TIARequired = false,
            BlockReason = reason,
            Warnings = warnings ?? []
        };
}
