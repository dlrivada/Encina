namespace Encina.Compliance.Consent;

/// <summary>
/// Factory methods for consent compliance-related <see cref="EncinaError"/> instances.
/// </summary>
/// <remarks>
/// Error codes follow the convention <c>consent.{category}</c>.
/// All errors include structured metadata for observability.
/// </remarks>
public static class ConsentErrors
{
    private const string MetadataKeySubjectId = "subjectId";
    private const string MetadataKeyPurpose = "purpose";
    private const string MetadataKeyStage = "stage";
    private const string MetadataStageConsent = "consent_compliance";

    /// <summary>Error code when consent is missing for a required processing purpose.</summary>
    public const string MissingConsentCode = "consent.missing";

    /// <summary>Error code when consent has expired.</summary>
    public const string ConsentExpiredCode = "consent.expired";

    /// <summary>Error code when consent has been withdrawn.</summary>
    public const string ConsentWithdrawnCode = "consent.withdrawn";

    /// <summary>Error code when consent requires reconsent due to version changes.</summary>
    public const string RequiresReconsentCode = "consent.requires_reconsent";

    /// <summary>Error code when the consent version does not match the current version.</summary>
    public const string VersionMismatchCode = "consent.version_mismatch";

    /// <summary>Error code when a consent aggregate is not found.</summary>
    public const string ConsentNotFoundCode = "consent.not_found";

    /// <summary>Error code for invalid consent aggregate state transitions.</summary>
    public const string InvalidStateTransitionCode = "consent.invalid_state_transition";

    /// <summary>Error code when a consent service operation fails.</summary>
    public const string ServiceErrorCode = "consent.service_error";

    /// <summary>Error code when consent event history is not available.</summary>
    public const string EventHistoryUnavailableCode = "consent.event_history_unavailable";

    /// <summary>
    /// Creates an error when consent is missing for a required processing purpose.
    /// </summary>
    /// <param name="subjectId">The identifier of the data subject.</param>
    /// <param name="purpose">The processing purpose that requires consent.</param>
    /// <returns>An error indicating that consent is missing.</returns>
    public static EncinaError MissingConsent(string subjectId, string purpose) =>
        EncinaErrors.Create(
            code: MissingConsentCode,
            message: $"No consent found for subject '{subjectId}' and purpose '{purpose}'. "
                + "Processing cannot proceed without valid consent (Article 6(1)(a)).",
            details: new Dictionary<string, object?>
            {
                [MetadataKeySubjectId] = subjectId,
                [MetadataKeyPurpose] = purpose,
                [MetadataKeyStage] = MetadataStageConsent,
                ["requirement"] = "article_6_1_a"
            });

    /// <summary>
    /// Creates an error when consent has expired for a processing purpose.
    /// </summary>
    /// <param name="subjectId">The identifier of the data subject.</param>
    /// <param name="purpose">The processing purpose whose consent has expired.</param>
    /// <param name="expiredAtUtc">The timestamp when the consent expired.</param>
    /// <returns>An error indicating that consent has expired.</returns>
    public static EncinaError ConsentExpired(string subjectId, string purpose, DateTimeOffset expiredAtUtc) =>
        EncinaErrors.Create(
            code: ConsentExpiredCode,
            message: $"Consent for subject '{subjectId}' and purpose '{purpose}' expired at {expiredAtUtc:O}. "
                + "Fresh consent is required before processing can resume.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeySubjectId] = subjectId,
                [MetadataKeyPurpose] = purpose,
                [MetadataKeyStage] = MetadataStageConsent,
                ["expiredAtUtc"] = expiredAtUtc,
                ["requirement"] = "consent_expiration"
            });

    /// <summary>
    /// Creates an error when consent has been withdrawn by the data subject.
    /// </summary>
    /// <param name="subjectId">The identifier of the data subject.</param>
    /// <param name="purpose">The processing purpose whose consent was withdrawn.</param>
    /// <param name="withdrawnAtUtc">The timestamp when consent was withdrawn.</param>
    /// <returns>An error indicating that consent has been withdrawn.</returns>
    public static EncinaError ConsentWithdrawn(string subjectId, string purpose, DateTimeOffset withdrawnAtUtc) =>
        EncinaErrors.Create(
            code: ConsentWithdrawnCode,
            message: $"Consent for subject '{subjectId}' and purpose '{purpose}' was withdrawn at {withdrawnAtUtc:O}. "
                + "Processing must cease (Article 7(3)).",
            details: new Dictionary<string, object?>
            {
                [MetadataKeySubjectId] = subjectId,
                [MetadataKeyPurpose] = purpose,
                [MetadataKeyStage] = MetadataStageConsent,
                ["withdrawnAtUtc"] = withdrawnAtUtc,
                ["requirement"] = "article_7_3"
            });

    /// <summary>
    /// Creates an error when consent needs to be renewed due to changed consent terms.
    /// </summary>
    /// <param name="subjectId">The identifier of the data subject.</param>
    /// <param name="purpose">The processing purpose that requires reconsent.</param>
    /// <param name="currentVersionId">The current consent version identifier.</param>
    /// <param name="consentedVersionId">The version the data subject originally consented to.</param>
    /// <returns>An error indicating that reconsent is required.</returns>
    public static EncinaError RequiresReconsent(
        string subjectId,
        string purpose,
        string currentVersionId,
        string consentedVersionId) =>
        EncinaErrors.Create(
            code: RequiresReconsentCode,
            message: $"Consent for subject '{subjectId}' and purpose '{purpose}' was given under version "
                + $"'{consentedVersionId}' but current version is '{currentVersionId}'. Reconsent is required.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeySubjectId] = subjectId,
                [MetadataKeyPurpose] = purpose,
                [MetadataKeyStage] = MetadataStageConsent,
                ["currentVersionId"] = currentVersionId,
                ["consentedVersionId"] = consentedVersionId,
                ["requirement"] = "consent_version_reconsent"
            });

    /// <summary>
    /// Creates an error when the consent version does not match the expected version.
    /// </summary>
    /// <param name="subjectId">The identifier of the data subject.</param>
    /// <param name="purpose">The processing purpose with the version mismatch.</param>
    /// <param name="expectedVersionId">The expected consent version identifier.</param>
    /// <param name="actualVersionId">The actual consent version the data subject agreed to.</param>
    /// <returns>An error indicating a consent version mismatch.</returns>
    public static EncinaError VersionMismatch(
        string subjectId,
        string purpose,
        string expectedVersionId,
        string actualVersionId) =>
        EncinaErrors.Create(
            code: VersionMismatchCode,
            message: $"Consent version mismatch for subject '{subjectId}' and purpose '{purpose}': "
                + $"expected '{expectedVersionId}', found '{actualVersionId}'.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeySubjectId] = subjectId,
                [MetadataKeyPurpose] = purpose,
                [MetadataKeyStage] = MetadataStageConsent,
                ["expectedVersionId"] = expectedVersionId,
                ["actualVersionId"] = actualVersionId,
                ["requirement"] = "consent_version_integrity"
            });

    // --- Service-level errors ---

    /// <summary>
    /// Creates an error when a consent aggregate is not found.
    /// </summary>
    /// <param name="consentId">The consent identifier that was not found.</param>
    /// <returns>An error indicating the consent was not found.</returns>
    public static EncinaError ConsentNotFound(Guid consentId) =>
        EncinaErrors.Create(
            code: ConsentNotFoundCode,
            message: $"Consent '{consentId}' was not found.",
            details: new Dictionary<string, object?>
            {
                ["consentId"] = consentId.ToString(),
                [MetadataKeyStage] = MetadataStageConsent
            });

    /// <summary>
    /// Creates an error for an invalid consent aggregate state transition.
    /// </summary>
    /// <param name="from">The current state.</param>
    /// <param name="to">The attempted target state.</param>
    /// <returns>An error indicating the state transition is not valid.</returns>
    public static EncinaError InvalidStateTransition(string from, string to) =>
        EncinaErrors.Create(
            code: InvalidStateTransitionCode,
            message: $"Invalid consent state transition from '{from}' to '{to}'.",
            details: new Dictionary<string, object?>
            {
                ["fromState"] = from,
                ["toState"] = to,
                [MetadataKeyStage] = MetadataStageConsent
            });

    /// <summary>
    /// Creates an error when a consent service operation fails.
    /// </summary>
    /// <param name="operation">The operation that failed (e.g., "GrantConsent", "WithdrawConsent").</param>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <returns>An error wrapping the infrastructure failure.</returns>
    public static EncinaError ServiceError(string operation, Exception exception) =>
        EncinaErrors.Create(
            code: ServiceErrorCode,
            message: $"Consent service operation '{operation}' failed: {exception.Message}",
            exception: exception,
            details: new Dictionary<string, object?>
            {
                ["operation"] = operation,
                [MetadataKeyStage] = MetadataStageConsent
            });

    /// <summary>
    /// Creates an error when consent event history cannot be retrieved.
    /// </summary>
    /// <param name="consentId">The consent identifier.</param>
    /// <returns>An error indicating event history is not available through the current repository.</returns>
    public static EncinaError EventHistoryUnavailable(Guid consentId) =>
        EncinaErrors.Create(
            code: EventHistoryUnavailableCode,
            message: $"Event history for consent '{consentId}' is not available. "
                + "Event stream access requires Marten-specific APIs.",
            details: new Dictionary<string, object?>
            {
                ["consentId"] = consentId.ToString(),
                [MetadataKeyStage] = MetadataStageConsent
            });
}
