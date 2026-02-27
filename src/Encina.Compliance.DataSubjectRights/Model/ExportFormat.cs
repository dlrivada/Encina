namespace Encina.Compliance.DataSubjectRights;

/// <summary>
/// Supported export formats for data portability under GDPR Article 20.
/// </summary>
/// <remarks>
/// <para>
/// Article 20 requires that personal data be provided in a "structured, commonly used
/// and machine-readable format." These formats satisfy that requirement while offering
/// flexibility for different downstream systems.
/// </para>
/// </remarks>
public enum ExportFormat
{
    /// <summary>
    /// JavaScript Object Notation — structured, widely supported, machine-readable.
    /// </summary>
    /// <remarks>Uses System.Text.Json for serialization.</remarks>
    JSON,

    /// <summary>
    /// Comma-Separated Values — tabular format, RFC 4180 compliant.
    /// </summary>
    /// <remarks>Best for spreadsheet import and simple data exchange.</remarks>
    CSV,

    /// <summary>
    /// Extensible Markup Language — structured with schema support.
    /// </summary>
    /// <remarks>Suitable for systems requiring formal schema validation.</remarks>
    XML
}
