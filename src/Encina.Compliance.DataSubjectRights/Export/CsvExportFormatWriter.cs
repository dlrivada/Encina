using System.Globalization;
using System.Text;

using LanguageExt;

using static LanguageExt.Prelude;

namespace Encina.Compliance.DataSubjectRights;

/// <summary>
/// Exports personal data as RFC 4180-compliant CSV for data portability (Article 20).
/// </summary>
/// <remarks>
/// <para>
/// Produces a CSV file with headers: EntityType, EntityId, FieldName, Category, Value.
/// The output includes a UTF-8 BOM for compatibility with spreadsheet applications.
/// </para>
/// <para>
/// Values containing commas, quotes, or newlines are properly escaped per RFC 4180.
/// </para>
/// </remarks>
public sealed class CsvExportFormatWriter : IExportFormatWriter
{
    private static readonly string[] Headers =
    [
        "EntityType",
        "EntityId",
        "FieldName",
        "Category",
        "Value"
    ];

    /// <inheritdoc />
    public ExportFormat SupportedFormat => ExportFormat.CSV;

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, ExportedData>> WriteAsync(
        IReadOnlyList<PersonalDataLocation> data,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(data);

        try
        {
            var sb = new StringBuilder();

            // Header row
            sb.AppendLine(string.Join(',', Headers));

            // Data rows
            foreach (var location in data)
            {
                var fields = new[]
                {
                    EscapeCsv(location.EntityType.FullName ?? location.EntityType.Name),
                    EscapeCsv(location.EntityId),
                    EscapeCsv(location.FieldName),
                    EscapeCsv(location.Category.ToString()),
                    EscapeCsv(location.CurrentValue?.ToString() ?? string.Empty)
                };

                sb.AppendLine(string.Join(',', fields));
            }

            var bytes = Encoding.UTF8.GetPreamble()
                .Concat(Encoding.UTF8.GetBytes(sb.ToString()))
                .ToArray();

            var result = new ExportedData
            {
                Content = bytes,
                ContentType = "text/csv",
                FileName = $"personal-data-export-{DateTime.UtcNow:yyyyMMdd}.csv",
                Format = ExportFormat.CSV,
                FieldCount = data.Count
            };

            return ValueTask.FromResult<Either<EncinaError, ExportedData>>(Right(result));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ValueTask.FromResult<Either<EncinaError, ExportedData>>(
                DSRErrors.ExportFailed("N/A", ExportFormat.CSV, $"CSV generation failed: {ex.Message}"));
        }
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
