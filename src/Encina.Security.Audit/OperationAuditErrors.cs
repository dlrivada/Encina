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
