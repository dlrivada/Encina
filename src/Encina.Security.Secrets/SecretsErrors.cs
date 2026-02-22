namespace Encina.Security.Secrets;

/// <summary>
/// Factory methods for secrets-related <see cref="EncinaError"/> instances.
/// </summary>
/// <remarks>
/// Error codes follow the convention <c>secrets.{category}</c>.
/// All errors include structured metadata for observability and debugging.
/// </remarks>
public static class SecretsErrors
{
    private const string MetadataKeyStage = "stage";
    private const string MetadataStageSecrets = "secrets";

    /// <summary>Error code when a requested secret is not found.</summary>
    public const string NotFoundCode = "secrets.not_found";

    /// <summary>Error code when access to a secret is denied due to insufficient permissions.</summary>
    public const string AccessDeniedCode = "secrets.access_denied";

    /// <summary>Error code when secret rotation fails.</summary>
    public const string RotationFailedCode = "secrets.rotation_failed";

    /// <summary>Error code when caching a secret fails.</summary>
    public const string CacheFailureCode = "secrets.cache_failure";

    /// <summary>Error code when deserializing a secret value to the target type fails.</summary>
    public const string DeserializationFailedCode = "secrets.deserialization_failed";

    /// <summary>Error code when the secret provider is unavailable (network error, authentication failure, etc.).</summary>
    public const string ProviderUnavailableCode = "secrets.provider_unavailable";

    /// <summary>Error code when secret injection into a request property fails.</summary>
    public const string InjectionFailedCode = "secrets.injection_failed";

    /// <summary>Error code when all providers in a failover chain have been exhausted.</summary>
    public const string FailoverExhaustedCode = "secrets.failover_exhausted";

    /// <summary>Error code when audit recording for a secret access fails.</summary>
    public const string AuditFailedCode = "secrets.audit_failed";

    /// <summary>
    /// Creates an error for a secret that was not found.
    /// </summary>
    /// <param name="secretName">The name of the secret that was not found.</param>
    /// <returns>An error indicating the secret does not exist.</returns>
    public static EncinaError NotFound(string secretName) =>
        EncinaErrors.Create(
            code: NotFoundCode,
            message: $"Secret '{secretName}' was not found.",
            details: new Dictionary<string, object?>
            {
                ["secretName"] = secretName,
                [MetadataKeyStage] = MetadataStageSecrets
            });

    /// <summary>
    /// Creates an error for denied access to a secret.
    /// </summary>
    /// <param name="secretName">The name of the secret that was denied.</param>
    /// <param name="reason">The reason access was denied.</param>
    /// <returns>An error indicating access to the secret was denied.</returns>
    public static EncinaError AccessDenied(string secretName, string? reason = null) =>
        EncinaErrors.Create(
            code: AccessDeniedCode,
            message: reason is not null
                ? $"Access denied to secret '{secretName}': {reason}."
                : $"Access denied to secret '{secretName}'.",
            details: new Dictionary<string, object?>
            {
                ["secretName"] = secretName,
                ["reason"] = reason,
                [MetadataKeyStage] = MetadataStageSecrets
            });

    /// <summary>
    /// Creates an error when secret rotation fails.
    /// </summary>
    /// <param name="secretName">The name of the secret that failed to rotate.</param>
    /// <param name="reason">The reason for the failure.</param>
    /// <param name="exception">The underlying exception, if available.</param>
    /// <returns>An error indicating rotation failed.</returns>
    public static EncinaError RotationFailed(string secretName, string? reason = null, Exception? exception = null) =>
        EncinaErrors.Create(
            code: RotationFailedCode,
            message: reason is not null
                ? $"Rotation failed for secret '{secretName}': {reason}."
                : $"Rotation failed for secret '{secretName}'.",
            exception: exception,
            details: new Dictionary<string, object?>
            {
                ["secretName"] = secretName,
                ["reason"] = reason,
                [MetadataKeyStage] = MetadataStageSecrets
            });

    /// <summary>
    /// Creates an error when caching a secret fails.
    /// </summary>
    /// <param name="secretName">The name of the secret that failed to cache.</param>
    /// <param name="exception">The underlying exception, if available.</param>
    /// <returns>An error indicating the cache operation failed.</returns>
    public static EncinaError CacheFailure(string secretName, Exception? exception = null) =>
        EncinaErrors.Create(
            code: CacheFailureCode,
            message: $"Cache operation failed for secret '{secretName}'.",
            exception: exception,
            details: new Dictionary<string, object?>
            {
                ["secretName"] = secretName,
                [MetadataKeyStage] = MetadataStageSecrets
            });

    /// <summary>
    /// Creates an error when deserializing a secret value to the target type fails.
    /// </summary>
    /// <param name="secretName">The name of the secret.</param>
    /// <param name="targetType">The type the secret was being deserialized to.</param>
    /// <param name="exception">The underlying exception, if available.</param>
    /// <returns>An error indicating deserialization failed.</returns>
    public static EncinaError DeserializationFailed(string secretName, Type targetType, Exception? exception = null) =>
        EncinaErrors.Create(
            code: DeserializationFailedCode,
            message: $"Failed to deserialize secret '{secretName}' to type '{targetType.Name}'.",
            exception: exception,
            details: new Dictionary<string, object?>
            {
                ["secretName"] = secretName,
                ["targetType"] = targetType.FullName,
                [MetadataKeyStage] = MetadataStageSecrets
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
                [MetadataKeyStage] = MetadataStageSecrets
            });

    /// <summary>
    /// Creates an error when secret injection into a request property fails.
    /// </summary>
    /// <param name="secretName">The name of the secret that failed to inject.</param>
    /// <param name="propertyName">The name of the property.</param>
    /// <param name="exception">The underlying exception, if available.</param>
    /// <returns>An error indicating injection failed.</returns>
    public static EncinaError InjectionFailed(string secretName, string propertyName, Exception? exception = null) =>
        EncinaErrors.Create(
            code: InjectionFailedCode,
            message: $"Failed to inject secret '{secretName}' into property '{propertyName}'.",
            exception: exception,
            details: new Dictionary<string, object?>
            {
                ["secretName"] = secretName,
                ["propertyName"] = propertyName,
                [MetadataKeyStage] = MetadataStageSecrets
            });

    /// <summary>
    /// Creates an error when all providers in a failover chain have been exhausted.
    /// </summary>
    /// <param name="secretName">The name of the secret being requested.</param>
    /// <param name="providerCount">The number of providers that were tried.</param>
    /// <returns>An error indicating all failover providers failed.</returns>
    public static EncinaError FailoverExhausted(string secretName, int providerCount) =>
        EncinaErrors.Create(
            code: FailoverExhaustedCode,
            message: $"All {providerCount} providers failed to retrieve secret '{secretName}'.",
            details: new Dictionary<string, object?>
            {
                ["secretName"] = secretName,
                ["providerCount"] = providerCount,
                [MetadataKeyStage] = MetadataStageSecrets
            });

    /// <summary>
    /// Creates an error when audit recording for a secret access fails.
    /// </summary>
    /// <param name="secretName">The name of the secret.</param>
    /// <param name="exception">The underlying exception, if available.</param>
    /// <returns>An error indicating audit recording failed.</returns>
    public static EncinaError AuditFailed(string secretName, Exception? exception = null) =>
        EncinaErrors.Create(
            code: AuditFailedCode,
            message: $"Audit recording failed for secret '{secretName}'.",
            exception: exception,
            details: new Dictionary<string, object?>
            {
                ["secretName"] = secretName,
                [MetadataKeyStage] = MetadataStageSecrets
            });
}
