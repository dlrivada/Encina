using LanguageExt;

namespace Encina.Compliance.DataSubjectRights;

/// <summary>
/// Writes personal data locations into a specific export format for data portability (Article 20).
/// </summary>
/// <remarks>
/// <para>
/// Each implementation handles serialization to a specific format (JSON, CSV, XML).
/// The <see cref="IDataPortabilityExporter"/> selects the appropriate writer based on the
/// <see cref="SupportedFormat"/> property, using a strategy pattern for format selection.
/// </para>
/// <para>
/// Implementations should produce structured, commonly used, and machine-readable output
/// as required by Article 20 GDPR.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class JsonExportFormatWriter : IExportFormatWriter
/// {
///     public ExportFormat SupportedFormat => ExportFormat.JSON;
///
///     public async ValueTask&lt;Either&lt;EncinaError, ExportedData&gt;&gt; WriteAsync(
///         IReadOnlyList&lt;PersonalDataLocation&gt; data, CancellationToken cancellationToken)
///     {
///         var json = JsonSerializer.SerializeToUtf8Bytes(data);
///         return new ExportedData
///         {
///             Content = json,
///             ContentType = "application/json",
///             FileName = $"personal-data-export-{DateTime.UtcNow:yyyyMMdd}.json",
///             Format = ExportFormat.JSON,
///             FieldCount = data.Count
///         };
///     }
/// }
/// </code>
/// </example>
public interface IExportFormatWriter
{
    /// <summary>
    /// Gets the export format that this writer supports.
    /// </summary>
    /// <remarks>
    /// Used by <see cref="IDataPortabilityExporter"/> to select the correct writer
    /// for the requested format.
    /// </remarks>
    ExportFormat SupportedFormat { get; }

    /// <summary>
    /// Writes the specified personal data locations into the supported export format.
    /// </summary>
    /// <param name="data">The personal data locations to serialize.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// An <see cref="ExportedData"/> containing the serialized content, content type, file name,
    /// and field count, or an <see cref="EncinaError"/> on failure.
    /// </returns>
    ValueTask<Either<EncinaError, ExportedData>> WriteAsync(
        IReadOnlyList<PersonalDataLocation> data,
        CancellationToken cancellationToken = default);
}
