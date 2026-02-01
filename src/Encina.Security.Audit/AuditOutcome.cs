namespace Encina.Security.Audit;

/// <summary>
/// Represents the outcome of an audited operation.
/// </summary>
/// <remarks>
/// <para>
/// This enum captures the high-level result of a request for audit logging purposes.
/// It provides a standardized way to categorize operation outcomes across the system.
/// </para>
/// <para>
/// The values are designed to support compliance reporting and security analysis:
/// <list type="bullet">
/// <item><see cref="Success"/> - Operation completed successfully</item>
/// <item><see cref="Failure"/> - Operation failed due to validation or business rules</item>
/// <item><see cref="Denied"/> - Operation was denied due to authorization failure</item>
/// <item><see cref="Error"/> - Operation failed due to a system error or exception</item>
/// </list>
/// </para>
/// </remarks>
public enum AuditOutcome
{
    /// <summary>
    /// The operation completed successfully.
    /// </summary>
    /// <remarks>
    /// Maps to <c>Either.Right</c> in the Encina pipeline.
    /// Indicates the request was processed and returned a successful result.
    /// </remarks>
    Success = 0,

    /// <summary>
    /// The operation failed due to validation errors or business rule violations.
    /// </summary>
    /// <remarks>
    /// Maps to <c>Either.Left</c> with validation-related error codes.
    /// Examples include invalid input, constraint violations, or business rule failures.
    /// </remarks>
    Failure = 1,

    /// <summary>
    /// The operation was denied due to authorization failure.
    /// </summary>
    /// <remarks>
    /// Maps to <c>Either.Left</c> with authorization-related error codes.
    /// Indicates the user lacks permission to perform the requested action.
    /// These entries are particularly important for security audits.
    /// </remarks>
    Denied = 2,

    /// <summary>
    /// The operation failed due to a system error or unhandled exception.
    /// </summary>
    /// <remarks>
    /// Represents unexpected failures such as database connectivity issues,
    /// external service failures, or unhandled exceptions.
    /// These entries may require immediate attention from operations teams.
    /// </remarks>
    Error = 3
}
