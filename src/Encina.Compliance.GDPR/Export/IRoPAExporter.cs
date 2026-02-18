using LanguageExt;

namespace Encina.Compliance.GDPR.Export;

/// <summary>
/// Exports Records of Processing Activities (RoPA) for compliance auditing.
/// </summary>
/// <remarks>
/// <para>
/// Article 30 GDPR requires controllers to maintain a record of processing activities.
/// Implementations of this interface produce formatted exports suitable for submission
/// to supervisory authorities (DPAs) or internal compliance reviews.
/// </para>
/// <para>
/// Two built-in implementations are provided:
/// <list type="bullet">
/// <item><see cref="JsonRoPAExporter"/> — Structured JSON for system integration and archival</item>
/// <item><see cref="CsvRoPAExporter"/> — Tabular CSV for human review and spreadsheet import</item>
/// </list>
/// </para>
/// </remarks>
public interface IRoPAExporter
{
    /// <summary>
    /// Gets the content type produced by this exporter (e.g., <c>application/json</c>, <c>text/csv</c>).
    /// </summary>
    string ContentType { get; }

    /// <summary>
    /// Gets the file extension for exports produced by this exporter (e.g., <c>.json</c>, <c>.csv</c>).
    /// </summary>
    string FileExtension { get; }

    /// <summary>
    /// Exports the given processing activities as a formatted byte array.
    /// </summary>
    /// <param name="activities">The processing activities to export.</param>
    /// <param name="metadata">Metadata to include in the export (controller info, export date, etc.).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A <see cref="RoPAExportResult"/> containing the exported content, or an
    /// <see cref="EncinaError"/> if the export fails.
    /// </returns>
    ValueTask<Either<EncinaError, RoPAExportResult>> ExportAsync(
        IReadOnlyList<ProcessingActivity> activities,
        RoPAExportMetadata metadata,
        CancellationToken cancellationToken = default);
}
