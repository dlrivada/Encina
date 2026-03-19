namespace Encina.Audit.Marten;

/// <summary>
/// Factory methods for Marten audit store-related <see cref="EncinaError"/> instances.
/// </summary>
/// <remarks>
/// <para>
/// Error codes follow the convention <c>audit.marten.{category}</c>.
/// All errors include structured metadata for observability and debugging.
/// </para>
/// <para>
/// For encryption-related errors that originate from the underlying crypto infrastructure,
/// see <c>CryptoShreddingErrors</c> in <c>Encina.Marten.GDPR</c>.
/// </para>
/// </remarks>
public static class MartenAuditErrors
{
    private const string MetadataKeyStage = "stage";
    private const string MetadataStageAuditMarten = "audit-marten";

    /// <summary>Error code when encryption of audit entry PII fields fails.</summary>
    public const string EncryptionFailedCode = "audit.marten.encryption_failed";

    /// <summary>Error code when decryption of audit entry PII fields fails.</summary>
    public const string DecryptionFailedCode = "audit.marten.decryption_failed";

    /// <summary>Error code when a temporal encryption key is not found.</summary>
    public const string KeyNotFoundCode = "audit.marten.key_not_found";

    /// <summary>Error code when temporal key destruction fails.</summary>
    public const string KeyDestructionFailedCode = "audit.marten.key_destruction_failed";

    /// <summary>Error code when projection processing fails.</summary>
    public const string ProjectionFailedCode = "audit.marten.projection_failed";

    /// <summary>Error code when an audit query fails.</summary>
    public const string QueryFailedCode = "audit.marten.query_failed";

    /// <summary>Error code when the Marten store is unavailable.</summary>
    public const string StoreUnavailableCode = "audit.marten.store_unavailable";

    /// <summary>Error code when a temporal key period identifier is invalid.</summary>
    public const string InvalidPeriodCode = "audit.marten.invalid_period";

    /// <summary>Error code indicating the entry has been crypto-shredded.</summary>
    public const string ShreddedEntryCode = "audit.marten.shredded_entry";

    /// <summary>
    /// Creates an error when encryption of audit entry PII fields fails.
    /// </summary>
    /// <param name="entryId">The audit entry identifier.</param>
    /// <param name="period">The temporal key period that was used.</param>
    /// <param name="exception">The exception that caused the failure, if any.</param>
    /// <returns>An error indicating PII encryption failed for the audit entry.</returns>
    public static EncinaError EncryptionFailed(
        Guid entryId,
        string period,
        Exception? exception = null) =>
        EncinaErrors.Create(
            code: EncryptionFailedCode,
            message: $"Failed to encrypt PII fields for audit entry '{entryId}' using temporal key period '{period}'.",
            exception: exception,
            details: new Dictionary<string, object?>
            {
                ["entryId"] = entryId,
                ["period"] = period,
                [MetadataKeyStage] = MetadataStageAuditMarten
            });

    /// <summary>
    /// Creates an error when decryption of audit entry PII fields fails.
    /// </summary>
    /// <param name="entryId">The audit entry identifier.</param>
    /// <param name="period">The temporal key period that was used.</param>
    /// <param name="exception">The exception that caused the failure, if any.</param>
    /// <returns>An error indicating PII decryption failed for the audit entry.</returns>
    public static EncinaError DecryptionFailed(
        Guid entryId,
        string period,
        Exception? exception = null) =>
        EncinaErrors.Create(
            code: DecryptionFailedCode,
            message: $"Failed to decrypt PII fields for audit entry '{entryId}' from temporal key period '{period}'.",
            exception: exception,
            details: new Dictionary<string, object?>
            {
                ["entryId"] = entryId,
                ["period"] = period,
                [MetadataKeyStage] = MetadataStageAuditMarten
            });

    /// <summary>
    /// Creates an error when a temporal encryption key cannot be found.
    /// </summary>
    /// <param name="period">The temporal key period that was requested.</param>
    /// <returns>An error indicating the temporal key was not found.</returns>
    public static EncinaError KeyNotFound(string period) =>
        EncinaErrors.Create(
            code: KeyNotFoundCode,
            message: $"Temporal encryption key for period '{period}' was not found. It may have been destroyed via crypto-shredding.",
            details: new Dictionary<string, object?>
            {
                ["period"] = period,
                [MetadataKeyStage] = MetadataStageAuditMarten
            });

    /// <summary>
    /// Creates an error when temporal key destruction fails.
    /// </summary>
    /// <param name="olderThanUtc">The cutoff date for key destruction.</param>
    /// <param name="exception">The exception that caused the failure, if any.</param>
    /// <returns>An error indicating key destruction failed.</returns>
    public static EncinaError KeyDestructionFailed(
        DateTime olderThanUtc,
        Exception? exception = null) =>
        EncinaErrors.Create(
            code: KeyDestructionFailedCode,
            message: $"Failed to destroy temporal keys older than '{olderThanUtc:O}'.",
            exception: exception,
            details: new Dictionary<string, object?>
            {
                ["olderThanUtc"] = olderThanUtc,
                [MetadataKeyStage] = MetadataStageAuditMarten
            });

    /// <summary>
    /// Creates an error when the audit projection processing fails.
    /// </summary>
    /// <param name="projectionName">The name of the projection that failed.</param>
    /// <param name="exception">The exception that caused the failure, if any.</param>
    /// <returns>An error indicating projection processing failed.</returns>
    public static EncinaError ProjectionFailed(
        string projectionName,
        Exception? exception = null) =>
        EncinaErrors.Create(
            code: ProjectionFailedCode,
            message: $"Audit projection '{projectionName}' failed to process events.",
            exception: exception,
            details: new Dictionary<string, object?>
            {
                ["projectionName"] = projectionName,
                [MetadataKeyStage] = MetadataStageAuditMarten
            });

    /// <summary>
    /// Creates an error when an audit query fails.
    /// </summary>
    /// <param name="queryType">The type of query that failed (e.g., "ByEntity", "ByUser", "Flexible").</param>
    /// <param name="exception">The exception that caused the failure, if any.</param>
    /// <returns>An error indicating the audit query failed.</returns>
    public static EncinaError QueryFailed(
        string queryType,
        Exception? exception = null) =>
        EncinaErrors.Create(
            code: QueryFailedCode,
            message: $"Audit query '{queryType}' failed.",
            exception: exception,
            details: new Dictionary<string, object?>
            {
                ["queryType"] = queryType,
                [MetadataKeyStage] = MetadataStageAuditMarten
            });

    /// <summary>
    /// Creates an error when the Marten store is unavailable.
    /// </summary>
    /// <param name="operation">The operation that failed (e.g., "RecordAsync", "QueryAsync").</param>
    /// <param name="exception">The exception that caused the failure, if any.</param>
    /// <returns>An error indicating the Marten store is unavailable.</returns>
    public static EncinaError StoreUnavailable(
        string operation,
        Exception? exception = null) =>
        EncinaErrors.Create(
            code: StoreUnavailableCode,
            message: $"Marten audit store is unavailable for operation '{operation}'.",
            exception: exception,
            details: new Dictionary<string, object?>
            {
                ["operation"] = operation,
                [MetadataKeyStage] = MetadataStageAuditMarten
            });

    /// <summary>
    /// Creates an error when a temporal key period identifier is invalid.
    /// </summary>
    /// <param name="period">The invalid period identifier.</param>
    /// <returns>An error indicating the period identifier is invalid.</returns>
    public static EncinaError InvalidPeriod(string? period) =>
        EncinaErrors.Create(
            code: InvalidPeriodCode,
            message: $"Invalid temporal key period: '{period ?? "(null)"}'. Period must be a non-empty string in the expected format (e.g., '2026-03', '2026-Q1', '2026').",
            details: new Dictionary<string, object?>
            {
                ["period"] = period,
                [MetadataKeyStage] = MetadataStageAuditMarten
            });

    /// <summary>
    /// Creates an error indicating the audit entry has been crypto-shredded.
    /// </summary>
    /// <param name="entryId">The audit entry identifier.</param>
    /// <param name="period">The temporal key period that was destroyed.</param>
    /// <returns>An informational error indicating the entry's PII has been shredded.</returns>
    public static EncinaError ShreddedEntry(
        Guid entryId,
        string period) =>
        EncinaErrors.Create(
            code: ShreddedEntryCode,
            message: $"Audit entry '{entryId}' has been crypto-shredded. PII fields for temporal period '{period}' are permanently unreadable.",
            details: new Dictionary<string, object?>
            {
                ["entryId"] = entryId,
                ["period"] = period,
                [MetadataKeyStage] = MetadataStageAuditMarten
            });
}
