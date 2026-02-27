using System.Xml.Linq;

using LanguageExt;

using static LanguageExt.Prelude;

namespace Encina.Compliance.DataSubjectRights;

/// <summary>
/// Exports personal data as XML for data portability (Article 20).
/// </summary>
/// <remarks>
/// <para>
/// Produces an XML document with a <c>&lt;PersonalData&gt;</c> root element containing
/// <c>&lt;DataField&gt;</c> elements for each personal data location. The structure is:
/// </para>
/// <code>
/// &lt;PersonalData&gt;
///   &lt;DataField&gt;
///     &lt;EntityType&gt;...&lt;/EntityType&gt;
///     &lt;EntityId&gt;...&lt;/EntityId&gt;
///     &lt;FieldName&gt;...&lt;/FieldName&gt;
///     &lt;Category&gt;...&lt;/Category&gt;
///     &lt;Value&gt;...&lt;/Value&gt;
///   &lt;/DataField&gt;
///   ...
/// &lt;/PersonalData&gt;
/// </code>
/// <para>
/// Uses <see cref="XDocument"/> for well-formed XML output with proper encoding.
/// </para>
/// </remarks>
public sealed class XmlExportFormatWriter : IExportFormatWriter
{
    /// <inheritdoc />
    public ExportFormat SupportedFormat => ExportFormat.XML;

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, ExportedData>> WriteAsync(
        IReadOnlyList<PersonalDataLocation> data,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(data);

        try
        {
            var document = new XDocument(
                new XDeclaration("1.0", "utf-8", "yes"),
                new XElement("PersonalData",
                    data.Select(d => new XElement("DataField",
                        new XElement("EntityType", d.EntityType.FullName ?? d.EntityType.Name),
                        new XElement("EntityId", d.EntityId),
                        new XElement("FieldName", d.FieldName),
                        new XElement("Category", d.Category.ToString()),
                        new XElement("Value", d.CurrentValue?.ToString() ?? string.Empty)))));

            using var stream = new MemoryStream();
            document.Save(stream);
            var bytes = stream.ToArray();

            var result = new ExportedData
            {
                Content = bytes,
                ContentType = "application/xml",
                FileName = $"personal-data-export-{DateTime.UtcNow:yyyyMMdd}.xml",
                Format = ExportFormat.XML,
                FieldCount = data.Count
            };

            return ValueTask.FromResult<Either<EncinaError, ExportedData>>(Right(result));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ValueTask.FromResult<Either<EncinaError, ExportedData>>(
                DSRErrors.ExportFailed("N/A", ExportFormat.XML, $"XML generation failed: {ex.Message}"));
        }
    }
}
