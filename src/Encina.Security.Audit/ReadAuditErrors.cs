namespace Encina.Security.Audit;

/// <summary>
/// Factory methods for read audit–related <see cref="EncinaError"/> instances.
/// </summary>
/// <remarks>
/// <para>
/// Error codes follow the convention <c>read_audit.{category}</c>.
/// All errors include structured metadata for observability.
/// </para>
/// </remarks>
public static class ReadAuditErrors
{
    private const string MetadataKeyStage = "read_audit";

    // --- Error codes ---

    /// <summary>Error code when a read audit store operation fails.</summary>
    public const string StoreErrorCode = "read_audit.store_error";

    /// <summary>Error code when a read audit entry is not found.</summary>
    public const string NotFoundCode = "read_audit.not_found";

    /// <summary>Error code when an invalid query is provided.</summary>
    public const string InvalidQueryCode = "read_audit.invalid_query";

    /// <summary>Error code when purging read audit entries fails.</summary>
    public const string PurgeFailedCode = "read_audit.purge_failed";

    /// <summary>Error code when a required access purpose is not provided.</summary>
    public const string PurposeRequiredCode = "read_audit.purpose_required";

    // --- Store errors ---

    /// <summary>
    /// Creates an error when a read audit store operation fails.
    /// </summary>
    /// <param name="operation">The store operation that failed (e.g., "LogRead", "Query", "GetAccessHistory").</param>
    /// <param name="message">The error message describing the failure.</param>
    /// <param name="exception">The optional inner exception that caused the failure.</param>
    /// <returns>An error indicating a store operation failure.</returns>
    public static EncinaError StoreError(string operation, string message, Exception? exception = null) =>
        EncinaErrors.Create(
            code: StoreErrorCode,
            message: $"Read audit store operation '{operation}' failed: {message}",
            exception: exception,
            details: new Dictionary<string, object?>
            {
                ["operation"] = operation,
                [MetadataKeyStage] = MetadataKeyStage
            });

    /// <summary>
    /// Creates an error when a read audit entry is not found.
    /// </summary>
    /// <param name="entityType">The entity type that was queried.</param>
    /// <param name="entityId">The entity ID that was queried.</param>
    /// <returns>An error indicating the read audit entry was not found.</returns>
    public static EncinaError NotFound(string entityType, string entityId) =>
        EncinaErrors.Create(
            code: NotFoundCode,
            message: $"No read audit entries found for {entityType} '{entityId}'.",
            details: new Dictionary<string, object?>
            {
                ["entityType"] = entityType,
                ["entityId"] = entityId,
                [MetadataKeyStage] = MetadataKeyStage
            });

    /// <summary>
    /// Creates an error when an invalid query is provided.
    /// </summary>
    /// <param name="reason">Description of why the query is invalid.</param>
    /// <returns>An error indicating the query is invalid.</returns>
    public static EncinaError InvalidQuery(string reason) =>
        EncinaErrors.Create(
            code: InvalidQueryCode,
            message: $"Invalid read audit query: {reason}",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyStage] = MetadataKeyStage
            });

    /// <summary>
    /// Creates an error when purging read audit entries fails.
    /// </summary>
    /// <param name="reason">Description of why the purge failed.</param>
    /// <param name="exception">The optional inner exception that caused the failure.</param>
    /// <returns>An error indicating the purge operation failed.</returns>
    public static EncinaError PurgeFailed(string reason, Exception? exception = null) =>
        EncinaErrors.Create(
            code: PurgeFailedCode,
            message: $"Failed to purge read audit entries: {reason}",
            exception: exception,
            details: new Dictionary<string, object?>
            {
                [MetadataKeyStage] = MetadataKeyStage
            });

    /// <summary>
    /// Creates an error when a required access purpose is not provided.
    /// </summary>
    /// <param name="entityType">The entity type being accessed.</param>
    /// <param name="userId">The user ID attempting access.</param>
    /// <returns>An error indicating the purpose is required but missing.</returns>
    /// <remarks>
    /// Returned when <see cref="ReadAuditOptions.RequirePurpose"/> is <c>true</c>
    /// and the caller did not declare an access purpose via <c>IReadAuditContext</c>.
    /// Supports GDPR Art. 15 compliance by enforcing purpose documentation.
    /// </remarks>
    public static EncinaError PurposeRequired(string entityType, string? userId) =>
        EncinaErrors.Create(
            code: PurposeRequiredCode,
            message: $"Access purpose is required for reading {entityType} but was not provided.",
            details: new Dictionary<string, object?>
            {
                ["entityType"] = entityType,
                ["userId"] = userId,
                [MetadataKeyStage] = MetadataKeyStage,
                ["requirement"] = "gdpr_article_15_purpose"
            });
}
