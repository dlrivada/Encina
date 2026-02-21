namespace Encina.Security.Sanitization;

/// <summary>
/// Factory methods for sanitization-related <see cref="EncinaError"/> instances.
/// </summary>
/// <remarks>
/// Error codes follow the convention <c>sanitization.{category}</c>.
/// All errors include structured metadata for observability and debugging.
/// </remarks>
public static class SanitizationErrors
{
    private const string MetadataKeyStage = "stage";
    private const string MetadataStageSanitization = "sanitization";

    /// <summary>Error code when the requested sanitization profile is not found.</summary>
    public const string ProfileNotFoundCode = "sanitization.profile_not_found";

    /// <summary>Error code when sanitization of a property fails.</summary>
    public const string PropertyErrorCode = "sanitization.property_error";

    /// <summary>
    /// Creates an error when a sanitization profile cannot be found.
    /// </summary>
    /// <param name="profileName">The profile name that was not found.</param>
    /// <returns>An error indicating the profile was not found.</returns>
    public static EncinaError ProfileNotFound(string profileName) =>
        EncinaErrors.Create(
            code: ProfileNotFoundCode,
            message: $"Sanitization profile '{profileName}' was not found.",
            details: new Dictionary<string, object?>
            {
                ["profileName"] = profileName,
                [MetadataKeyStage] = MetadataStageSanitization
            });

    /// <summary>
    /// Creates an error when sanitization of a specific property fails.
    /// </summary>
    /// <param name="propertyName">The name of the property that failed sanitization.</param>
    /// <param name="exception">The exception that caused the failure, if any.</param>
    /// <returns>An error indicating property sanitization failed.</returns>
    public static EncinaError PropertyError(
        string propertyName,
        Exception? exception = null) =>
        EncinaErrors.Create(
            code: PropertyErrorCode,
            message: $"Sanitization failed for property '{propertyName}'.",
            exception: exception,
            details: new Dictionary<string, object?>
            {
                ["propertyName"] = propertyName,
                [MetadataKeyStage] = MetadataStageSanitization
            });
}
