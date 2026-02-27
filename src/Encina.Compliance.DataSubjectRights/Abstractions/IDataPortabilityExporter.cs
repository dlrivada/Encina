using LanguageExt;

namespace Encina.Compliance.DataSubjectRights;

/// <summary>
/// Exports personal data for a data subject in a portable format (Article 20).
/// </summary>
/// <remarks>
/// <para>
/// The portability exporter coordinates data export by locating personal data via
/// <see cref="IPersonalDataLocator"/>, filtering for portable fields
/// (<see cref="PersonalDataAttribute.Portable"/> = <c>true</c>), and delegating
/// format-specific serialization to <see cref="IExportFormatWriter"/> implementations.
/// </para>
/// <para>
/// Per Article 20, the data subject has the right to receive their personal data in a
/// structured, commonly used, and machine-readable format. This right applies only to
/// data processed by automated means on the basis of consent or contract.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var result = await exporter.ExportAsync("subject-123", ExportFormat.JSON, cancellationToken);
///
/// result.Match(
///     Right: response => File.WriteAllBytes(
///         response.ExportedData.FileName, response.ExportedData.Content),
///     Left: error => Console.WriteLine($"Export failed: {error.Message}"));
/// </code>
/// </example>
public interface IDataPortabilityExporter
{
    /// <summary>
    /// Exports all portable personal data for the specified data subject in the requested format.
    /// </summary>
    /// <param name="subjectId">The identifier of the data subject whose data should be exported.</param>
    /// <param name="format">The desired export format (JSON, CSV, or XML).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="PortabilityResponse"/> containing the exported data in the requested format,
    /// or an <see cref="EncinaError"/> on failure.
    /// </returns>
    ValueTask<Either<EncinaError, PortabilityResponse>> ExportAsync(
        string subjectId,
        ExportFormat format,
        CancellationToken cancellationToken = default);
}
