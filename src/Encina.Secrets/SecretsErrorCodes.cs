namespace Encina.Secrets;

/// <summary>
/// Error code constants and factory methods for secret management operations.
/// </summary>
/// <remarks>
/// <para>
/// Error codes follow the convention <c>encina.secrets.{category}</c>.
/// Use these constants when creating <see cref="EncinaError"/> instances
/// or when matching errors in application code.
/// </para>
/// <para>
/// All factory methods return <see cref="EncinaError"/> instances suitable for use
/// as <c>Left</c> values in <c>Either&lt;EncinaError, T&gt;</c> results.
/// </para>
/// </remarks>
public static class SecretsErrorCodes
{
    /// <summary>Error code when a requested secret is not found.</summary>
    public const string NotFoundCode = "encina.secrets.not_found";

    /// <summary>Error code when access to a secret is denied due to insufficient permissions.</summary>
    public const string AccessDeniedCode = "encina.secrets.access_denied";

    /// <summary>Error code when a secret name is invalid (empty, too long, or contains forbidden characters).</summary>
    public const string InvalidNameCode = "encina.secrets.invalid_name";

    /// <summary>Error code when the secret provider is unavailable (network error, authentication failure, etc.).</summary>
    public const string ProviderUnavailableCode = "encina.secrets.provider_unavailable";

    /// <summary>Error code when a specific version of a secret is not found.</summary>
    public const string VersionNotFoundCode = "encina.secrets.version_not_found";

    /// <summary>Error code for generic operation failures not covered by more specific codes.</summary>
    public const string OperationFailedCode = "encina.secrets.operation_failed";

    /// <summary>
    /// Creates an error for a secret that was not found.
    /// </summary>
    /// <param name="name">The name of the secret that was not found.</param>
    /// <returns>An error indicating the secret does not exist.</returns>
    public static EncinaError NotFound(string name) =>
        EncinaErrors.Create(
            code: NotFoundCode,
            message: $"Secret '{name}' was not found.",
            details: new Dictionary<string, object?>
            {
                ["secretName"] = name,
                ["stage"] = "secrets"
            });

    /// <summary>
    /// Creates an error for denied access to a secret.
    /// </summary>
    /// <param name="name">The name of the secret that was denied.</param>
    /// <param name="reason">The reason access was denied.</param>
    /// <returns>An error indicating access to the secret was denied.</returns>
    public static EncinaError AccessDenied(string name, string? reason = null) =>
        EncinaErrors.Create(
            code: AccessDeniedCode,
            message: reason is not null
                ? $"Access denied to secret '{name}': {reason}."
                : $"Access denied to secret '{name}'.",
            details: new Dictionary<string, object?>
            {
                ["secretName"] = name,
                ["reason"] = reason,
                ["stage"] = "secrets"
            });

    /// <summary>
    /// Creates an error for an invalid secret name.
    /// </summary>
    /// <param name="name">The invalid secret name.</param>
    /// <param name="reason">The reason the name is invalid.</param>
    /// <returns>An error indicating the secret name is invalid.</returns>
    public static EncinaError InvalidName(string name, string reason) =>
        EncinaErrors.Create(
            code: InvalidNameCode,
            message: $"Secret name '{name}' is invalid: {reason}.",
            details: new Dictionary<string, object?>
            {
                ["secretName"] = name,
                ["reason"] = reason,
                ["stage"] = "secrets"
            });

    /// <summary>
    /// Creates an error when the secret provider is unavailable.
    /// </summary>
    /// <param name="providerName">The name of the unavailable provider.</param>
    /// <param name="exception">The underlying exception, if available.</param>
    /// <returns>An error indicating the provider is unavailable.</returns>
    public static EncinaError ProviderUnavailable(string providerName, Exception? exception = null) =>
        EncinaErrors.Create(
            code: ProviderUnavailableCode,
            message: $"Secret provider '{providerName}' is unavailable.",
            exception: exception,
            details: new Dictionary<string, object?>
            {
                ["providerName"] = providerName,
                ["stage"] = "secrets"
            });

    /// <summary>
    /// Creates an error for a specific version of a secret that was not found.
    /// </summary>
    /// <param name="name">The name of the secret.</param>
    /// <param name="version">The version that was not found.</param>
    /// <returns>An error indicating the secret version does not exist.</returns>
    public static EncinaError VersionNotFound(string name, string version) =>
        EncinaErrors.Create(
            code: VersionNotFoundCode,
            message: $"Version '{version}' of secret '{name}' was not found.",
            details: new Dictionary<string, object?>
            {
                ["secretName"] = name,
                ["version"] = version,
                ["stage"] = "secrets"
            });

    /// <summary>
    /// Creates an error for a generic operation failure.
    /// </summary>
    /// <param name="operation">The operation that failed.</param>
    /// <param name="reason">The reason for the failure.</param>
    /// <param name="exception">The underlying exception, if available.</param>
    /// <returns>An error indicating the operation failed.</returns>
    public static EncinaError OperationFailed(string operation, string reason, Exception? exception = null) =>
        EncinaErrors.Create(
            code: OperationFailedCode,
            message: $"Secret operation '{operation}' failed: {reason}.",
            exception: exception,
            details: new Dictionary<string, object?>
            {
                ["operation"] = operation,
                ["reason"] = reason,
                ["stage"] = "secrets"
            });
}
