using Encina.Diagnostics;

namespace Encina.Security.Audit;

/// <summary>
/// Factory methods for operation audit pipeline <see cref="EncinaError"/> instances.
/// </summary>
public static class OperationAuditErrors
{
    /// <summary>
    /// Error code of a request that failed because its audit entry could not be built or written
    /// while <see cref="OperationAuditOptions.FailureMode"/> is <see cref="OperationAuditFailureMode.FailClosed"/>.
    /// </summary>
    public const string WriteFailedCode = "audit.write_failed";

    /// <summary>
    /// Error code of a request whose audit entry was not built because its payload could not be masked,
    /// while <see cref="OperationAuditOptions.FailureMode"/> is <see cref="OperationAuditFailureMode.FailClosed"/>.
    /// Nothing is stored in that case.
    /// </summary>
    public const string MaskingFailedCode = "audit.masking_failed";

    /// <summary>
    /// Error code of an operation audit store operation that failed in the store itself (a database or
    /// provider failure), as opposed to a request whose audit entry was dropped by the pipeline.
    /// </summary>
    public const string StoreErrorCode = "audit.store_error";

    /// <summary>
    /// Creates the error returned when an operation audit store operation fails.
    /// </summary>
    /// <param name="operation">The store operation that failed (for example <c>Record</c> or <c>Query</c>).</param>
    /// <param name="exception">
    /// The exception that caused the failure. It is attached to the error and its type name appears in the
    /// message; its message text never does, because it can carry personal data.
    /// </param>
    /// <returns>An error carrying <see cref="StoreErrorCode"/> and <paramref name="exception"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="operation"/> or <paramref name="exception"/> is <c>null</c>.</exception>
    public static EncinaError StoreError(string operation, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(exception);

        return EncinaErrors.Create(
            code: StoreErrorCode,
            message: $"Operation audit store operation '{operation}' failed ({ActivityStatusDescriptions.ForException(exception)}).",
            exception: exception,
            details: new Dictionary<string, object?>
            {
                ["operation"] = operation
            });
    }

    /// <summary>
    /// Creates the error returned when the payload of a request could not be masked for its audit entry.
    /// </summary>
    /// <param name="requestType">The name of the audited request type.</param>
    /// <param name="causeCode">The exception type name that caused the failure; never a message.</param>
    /// <returns>An error carrying <see cref="MaskingFailedCode"/>.</returns>
    public static EncinaError MaskingFailed(string requestType, string causeCode)
    {
        ArgumentNullException.ThrowIfNull(requestType);
        ArgumentNullException.ThrowIfNull(causeCode);

        return EncinaErrors.Create(
            code: MaskingFailedCode,
            message: $"The audit payload of '{requestType}' could not be masked, so no audit entry was stored.",
            details: new Dictionary<string, object?>
            {
                ["requestType"] = requestType,
                ["cause"] = causeCode
            });
    }

    /// <summary>
    /// Creates the error returned when the audit entry of a request could not be recorded.
    /// </summary>
    /// <param name="requestType">The name of the audited request type.</param>
    /// <param name="causeCode">The error code or exception type name that caused the failure; never a message.</param>
    /// <returns>An error carrying <see cref="WriteFailedCode"/>.</returns>
    public static EncinaError WriteFailed(string requestType, string causeCode)
    {
        ArgumentNullException.ThrowIfNull(requestType);
        ArgumentNullException.ThrowIfNull(causeCode);

        return EncinaErrors.Create(
            code: WriteFailedCode,
            message: $"The audit entry for '{requestType}' could not be recorded.",
            details: new Dictionary<string, object?>
            {
                ["requestType"] = requestType,
                ["cause"] = causeCode
            });
    }
}
