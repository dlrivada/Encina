namespace Encina.Compliance.CrossBorderTransfer.Errors;

/// <summary>
/// Factory methods for cross-border transfer-related <see cref="EncinaError"/> instances.
/// </summary>
/// <remarks>
/// <para>
/// Error codes follow the convention <c>crossborder.{category}</c>.
/// All errors include structured metadata for observability and GDPR article references.
/// </para>
/// <para>
/// Relevant GDPR articles:
/// Article 44 — General principle for transfers.
/// Article 45 — Transfers on the basis of an adequacy decision.
/// Article 46 — Transfers subject to appropriate safeguards (SCCs, BCRs).
/// Article 49 — Derogations for specific situations.
/// Schrems II (CJEU C-311/18) — Supplementary measures and TIA requirements.
/// </para>
/// </remarks>
public static class CrossBorderTransferErrors
{
    private const string MetadataKeyTIAId = "tiaId";
    private const string MetadataKeySCCAgreementId = "sccAgreementId";
    private const string MetadataKeyTransferId = "transferId";
    private const string MetadataKeyStage = "cross_border_transfer";

    // --- Error codes ---

    /// <summary>Error code when a TIA is not found.</summary>
    public const string TIANotFoundCode = "crossborder.tia_not_found";

    /// <summary>Error code when a TIA is already completed and cannot be modified.</summary>
    public const string TIAAlreadyCompletedCode = "crossborder.tia_already_completed";

    /// <summary>Error code when a TIA has not been risk-assessed.</summary>
    public const string TIANotAssessedCode = "crossborder.tia_not_assessed";

    /// <summary>Error code when an SCC agreement is not found.</summary>
    public const string SCCAgreementNotFoundCode = "crossborder.scc_not_found";

    /// <summary>Error code when an SCC agreement is already revoked.</summary>
    public const string SCCAgreementAlreadyRevokedCode = "crossborder.scc_already_revoked";

    /// <summary>Error code when an approved transfer is not found.</summary>
    public const string TransferNotFoundCode = "crossborder.transfer_not_found";

    /// <summary>Error code when an approved transfer is already revoked.</summary>
    public const string TransferAlreadyRevokedCode = "crossborder.transfer_already_revoked";

    /// <summary>Error code when a transfer is blocked due to missing compliance mechanisms.</summary>
    public const string TransferBlockedCode = "crossborder.transfer_blocked";

    /// <summary>Error code for invalid aggregate state transitions.</summary>
    public const string InvalidStateTransitionCode = "crossborder.invalid_state_transition";

    /// <summary>Error code when a store or repository operation fails.</summary>
    public const string StoreErrorCode = "crossborder.store_error";

    // --- TIA errors ---

    /// <summary>
    /// Creates an error when a Transfer Impact Assessment is not found.
    /// </summary>
    /// <param name="id">The TIA identifier that was not found.</param>
    /// <returns>An error indicating the TIA was not found.</returns>
    /// <remarks>
    /// A TIA is required under Schrems II for transfers based on SCCs or BCRs
    /// to countries without an adequacy decision.
    /// </remarks>
    public static EncinaError TIANotFound(Guid id) =>
        EncinaErrors.Create(
            code: TIANotFoundCode,
            message: $"Transfer Impact Assessment '{id}' was not found.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyTIAId] = id.ToString(),
                [MetadataKeyStage] = MetadataKeyStage,
                ["requirement"] = "schrems_ii_tia"
            });

    /// <summary>
    /// Creates an error when a TIA is already completed and cannot be modified.
    /// </summary>
    /// <param name="id">The TIA identifier.</param>
    /// <returns>An error indicating the TIA is already completed.</returns>
    public static EncinaError TIAAlreadyCompleted(Guid id) =>
        EncinaErrors.Create(
            code: TIAAlreadyCompletedCode,
            message: $"Transfer Impact Assessment '{id}' is already completed and cannot be modified.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyTIAId] = id.ToString(),
                [MetadataKeyStage] = MetadataKeyStage
            });

    /// <summary>
    /// Creates an error when a TIA has not been risk-assessed and an operation requires it.
    /// </summary>
    /// <param name="id">The TIA identifier.</param>
    /// <returns>An error indicating the TIA has not been assessed.</returns>
    /// <remarks>
    /// A risk assessment must be completed before a TIA can be submitted for DPO review
    /// or used to authorize transfers.
    /// </remarks>
    public static EncinaError TIANotAssessed(Guid id) =>
        EncinaErrors.Create(
            code: TIANotAssessedCode,
            message: $"Transfer Impact Assessment '{id}' has not been risk-assessed. "
                + "A risk assessment is required before proceeding.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyTIAId] = id.ToString(),
                [MetadataKeyStage] = MetadataKeyStage,
                ["requirement"] = "edpb_recommendations_01_2020"
            });

    // --- SCC agreement errors ---

    /// <summary>
    /// Creates an error when an SCC agreement is not found.
    /// </summary>
    /// <param name="id">The SCC agreement identifier that was not found.</param>
    /// <returns>An error indicating the SCC agreement was not found.</returns>
    /// <remarks>
    /// Per GDPR Article 46(2)(c), a valid SCC agreement must be in place
    /// before data can be transferred on the basis of Standard Contractual Clauses.
    /// </remarks>
    public static EncinaError SCCAgreementNotFound(Guid id) =>
        EncinaErrors.Create(
            code: SCCAgreementNotFoundCode,
            message: $"SCC Agreement '{id}' was not found.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeySCCAgreementId] = id.ToString(),
                [MetadataKeyStage] = MetadataKeyStage,
                ["requirement"] = "article_46_2c_scc"
            });

    /// <summary>
    /// Creates an error when an SCC agreement is already revoked.
    /// </summary>
    /// <param name="id">The SCC agreement identifier.</param>
    /// <returns>An error indicating the SCC agreement is already revoked.</returns>
    public static EncinaError SCCAgreementAlreadyRevoked(Guid id) =>
        EncinaErrors.Create(
            code: SCCAgreementAlreadyRevokedCode,
            message: $"SCC Agreement '{id}' has already been revoked and cannot be modified.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeySCCAgreementId] = id.ToString(),
                [MetadataKeyStage] = MetadataKeyStage
            });

    // --- Approved transfer errors ---

    /// <summary>
    /// Creates an error when an approved transfer is not found.
    /// </summary>
    /// <param name="id">The approved transfer identifier that was not found.</param>
    /// <returns>An error indicating the approved transfer was not found.</returns>
    public static EncinaError TransferNotFound(Guid id) =>
        EncinaErrors.Create(
            code: TransferNotFoundCode,
            message: $"Approved transfer '{id}' was not found.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyTransferId] = id.ToString(),
                [MetadataKeyStage] = MetadataKeyStage
            });

    /// <summary>
    /// Creates an error when an approved transfer is already revoked.
    /// </summary>
    /// <param name="id">The approved transfer identifier.</param>
    /// <returns>An error indicating the transfer is already revoked.</returns>
    public static EncinaError TransferAlreadyRevoked(Guid id) =>
        EncinaErrors.Create(
            code: TransferAlreadyRevokedCode,
            message: $"Approved transfer '{id}' has already been revoked.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyTransferId] = id.ToString(),
                [MetadataKeyStage] = MetadataKeyStage
            });

    /// <summary>
    /// Creates an error when a transfer is blocked because no valid legal basis could be established.
    /// </summary>
    /// <param name="reason">The reason the transfer was blocked.</param>
    /// <returns>An error indicating the transfer is blocked.</returns>
    /// <remarks>
    /// Per GDPR Article 44, personal data may only be transferred to a third country
    /// if appropriate safeguards are in place. A blocked transfer indicates that no
    /// adequacy decision, SCC, BCR, or derogation applies.
    /// </remarks>
    public static EncinaError TransferBlocked(string reason) =>
        EncinaErrors.Create(
            code: TransferBlockedCode,
            message: $"Transfer is blocked: {reason}",
            details: new Dictionary<string, object?>
            {
                ["reason"] = reason,
                [MetadataKeyStage] = MetadataKeyStage,
                ["requirement"] = "article_44_general_principle"
            });

    /// <summary>
    /// Creates an error when no approved transfer is found for a route.
    /// </summary>
    /// <param name="source">Source country code.</param>
    /// <param name="destination">Destination country code.</param>
    /// <param name="dataCategory">Data category.</param>
    /// <returns>An error indicating no approved transfer was found for the route.</returns>
    public static EncinaError TransferNotFoundByRoute(string source, string destination, string dataCategory) =>
        EncinaErrors.Create(
            code: TransferNotFoundCode,
            message: $"No approved transfer found for route '{source}' → '{destination}' with data category '{dataCategory}'.",
            details: new Dictionary<string, object?>
            {
                ["sourceCountryCode"] = source,
                ["destinationCountryCode"] = destination,
                ["dataCategory"] = dataCategory,
                [MetadataKeyStage] = MetadataKeyStage
            });

    /// <summary>
    /// Creates an error when no TIA is found for a route.
    /// </summary>
    /// <param name="source">Source country code.</param>
    /// <param name="destination">Destination country code.</param>
    /// <param name="dataCategory">Data category.</param>
    /// <returns>An error indicating no TIA was found for the route.</returns>
    public static EncinaError TIANotFoundByRoute(string source, string destination, string dataCategory) =>
        EncinaErrors.Create(
            code: TIANotFoundCode,
            message: $"No Transfer Impact Assessment found for route '{source}' → '{destination}' with data category '{dataCategory}'.",
            details: new Dictionary<string, object?>
            {
                ["sourceCountryCode"] = source,
                ["destinationCountryCode"] = destination,
                ["dataCategory"] = dataCategory,
                [MetadataKeyStage] = MetadataKeyStage,
                ["requirement"] = "schrems_ii_tia"
            });

    // --- Generic errors ---

    /// <summary>
    /// Creates an error for an invalid aggregate state transition.
    /// </summary>
    /// <param name="from">The current state.</param>
    /// <param name="to">The attempted target state.</param>
    /// <returns>An error indicating the state transition is not valid.</returns>
    public static EncinaError InvalidStateTransition(string from, string to) =>
        EncinaErrors.Create(
            code: InvalidStateTransitionCode,
            message: $"Invalid state transition from '{from}' to '{to}'.",
            details: new Dictionary<string, object?>
            {
                ["fromState"] = from,
                ["toState"] = to,
                [MetadataKeyStage] = MetadataKeyStage
            });

    /// <summary>
    /// Creates an error when a store or repository operation fails.
    /// </summary>
    /// <param name="operation">The operation that failed (e.g., "Load", "Save").</param>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <returns>An error wrapping the infrastructure failure.</returns>
    public static EncinaError StoreError(string operation, Exception exception) =>
        EncinaErrors.Create(
            code: StoreErrorCode,
            message: $"Cross-border transfer store operation '{operation}' failed: {exception.Message}",
            exception: exception,
            details: new Dictionary<string, object?>
            {
                ["operation"] = operation,
                [MetadataKeyStage] = MetadataKeyStage
            });
}
