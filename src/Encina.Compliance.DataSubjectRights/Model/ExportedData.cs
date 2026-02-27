namespace Encina.Compliance.DataSubjectRights;

/// <summary>
/// Contains exported personal data in a structured, machine-readable format.
/// </summary>
/// <remarks>
/// <para>
/// Produced by the data portability exporter in response to Article 20 requests.
/// The data is serialized as a byte array in the requested <see cref="Format"/>
/// (JSON, CSV, or XML), along with the appropriate content type and filename.
/// </para>
/// <para>
/// Using <c>byte[]</c> rather than <see cref="System.IO.Stream"/> simplifies
/// the implementation across all 13 database providers and avoids lifecycle management issues.
/// </para>
/// </remarks>
public sealed record ExportedData
{
    /// <summary>
    /// The serialized personal data content.
    /// </summary>
    public required byte[] Content { get; init; }

    /// <summary>
    /// The MIME content type of the exported data.
    /// </summary>
    /// <example>"application/json", "text/csv", "application/xml"</example>
    public required string ContentType { get; init; }

    /// <summary>
    /// Suggested filename for the exported data.
    /// </summary>
    /// <example>"personal-data-export-2026-02-27.json"</example>
    public required string FileName { get; init; }

    /// <summary>
    /// The format in which the data was exported.
    /// </summary>
    public required ExportFormat Format { get; init; }

    /// <summary>
    /// The number of personal data fields included in the export.
    /// </summary>
    public required int FieldCount { get; init; }
}
