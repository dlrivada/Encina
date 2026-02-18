namespace Encina.Compliance.GDPR.Export;

/// <summary>
/// Result of a RoPA export operation.
/// </summary>
/// <param name="Content">The exported content as a byte array.</param>
/// <param name="ContentType">MIME content type (e.g., <c>application/json</c>).</param>
/// <param name="FileExtension">Suggested file extension (e.g., <c>.json</c>).</param>
/// <param name="ActivityCount">Number of processing activities included in the export.</param>
/// <param name="ExportedAtUtc">UTC timestamp when the export was generated.</param>
public sealed record RoPAExportResult(
    byte[] Content,
    string ContentType,
    string FileExtension,
    int ActivityCount,
    DateTimeOffset ExportedAtUtc);
