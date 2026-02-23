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
}
