namespace Encina.Compliance.GDPR.Export;

/// <summary>
/// Factory methods for RoPA export-related <see cref="EncinaError"/> instances.
/// </summary>
public static class RoPAExportErrors
{
    /// <summary>Error code when serialization fails during export.</summary>
    public const string SerializationFailedCode = "gdpr.ropa_export_serialization_failed";

    /// <summary>
    /// Creates an error when RoPA export serialization fails.
    /// </summary>
    /// <param name="format">The export format that failed (e.g., "JSON", "CSV").</param>
    /// <param name="reason">The underlying error message.</param>
    /// <returns>An error indicating serialization failure.</returns>
    public static EncinaError SerializationFailed(string format, string reason) =>
        EncinaErrors.Create(
            code: SerializationFailedCode,
            message: $"RoPA export serialization failed for format '{format}': {reason}",
            details: new Dictionary<string, object?>
            {
                ["format"] = format,
                ["stage"] = "gdpr_ropa_export",
                ["reason"] = reason
            });
}
