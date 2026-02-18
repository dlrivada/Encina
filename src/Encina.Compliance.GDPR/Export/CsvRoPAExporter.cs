using System.Globalization;
using System.Text;

using LanguageExt;

using static LanguageExt.Prelude;

namespace Encina.Compliance.GDPR.Export;

/// <summary>
/// Exports Records of Processing Activities as CSV for human review and spreadsheet import.
/// </summary>
/// <remarks>
/// <para>
/// Produces an RFC 4180-compliant CSV document suitable for submission to supervisory
/// authorities (DPAs), compliance auditors, or import into spreadsheet applications.
/// </para>
/// <para>
/// The CSV includes a metadata header section (lines starting with <c>#</c>) followed
/// by a standard header row and one data row per processing activity.
/// </para>
/// <para>
/// List fields (data subjects, data categories, recipients) are joined with <c>;</c>
/// (semicolon) separator to avoid conflicts with the CSV comma delimiter.
/// </para>
/// </remarks>
public sealed class CsvRoPAExporter : IRoPAExporter
{
    private const char ListSeparator = ';';

    private static readonly string[] Headers =
    [
        "Id",
        "Name",
        "Purpose",
        "LawfulBasis",
        "CategoriesOfDataSubjects",
        "CategoriesOfPersonalData",
        "Recipients",
        "ThirdCountryTransfers",
        "Safeguards",
        "RetentionDays",
        "SecurityMeasures",
        "RequestType",
        "CreatedAtUtc",
        "LastUpdatedAtUtc"
    ];

    /// <inheritdoc />
    public string ContentType => "text/csv";

    /// <inheritdoc />
    public string FileExtension => ".csv";

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, RoPAExportResult>> ExportAsync(
        IReadOnlyList<ProcessingActivity> activities,
        RoPAExportMetadata metadata,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activities);
        ArgumentNullException.ThrowIfNull(metadata);

        try
        {
            var sb = new StringBuilder();

            // Metadata header (comment lines)
            sb.AppendLine(CultureInfo.InvariantCulture, $"# Records of Processing Activities (RoPA) - Article 30 GDPR");
            sb.AppendLine(CultureInfo.InvariantCulture, $"# Controller: {metadata.ControllerName}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"# Contact: {metadata.ControllerEmail}");

            if (metadata.DataProtectionOfficer is not null)
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"# DPO: {metadata.DataProtectionOfficer.Name} ({metadata.DataProtectionOfficer.Email})");
            }

            sb.AppendLine(CultureInfo.InvariantCulture, $"# Exported: {metadata.ExportedAtUtc:O}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"# Activities: {activities.Count}");
            sb.AppendLine();

            // Header row
            sb.AppendLine(string.Join(',', Headers));

            // Data rows
            foreach (var activity in activities)
            {
                sb.AppendLine(FormatRow(activity));
            }

            var bytes = Encoding.UTF8.GetPreamble()
                .Concat(Encoding.UTF8.GetBytes(sb.ToString()))
                .ToArray();

            var result = new RoPAExportResult(
                Content: bytes,
                ContentType: ContentType,
                FileExtension: FileExtension,
                ActivityCount: activities.Count,
                ExportedAtUtc: metadata.ExportedAtUtc);

            return ValueTask.FromResult<Either<EncinaError, RoPAExportResult>>(Right(result));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var error = RoPAExportErrors.SerializationFailed("CSV", ex.Message);
            return ValueTask.FromResult<Either<EncinaError, RoPAExportResult>>(error);
        }
    }

    private static string FormatRow(ProcessingActivity activity)
    {
        var fields = new string[]
        {
            activity.Id.ToString(),
            EscapeCsv(activity.Name),
            EscapeCsv(activity.Purpose),
            activity.LawfulBasis.ToString(),
            EscapeCsv(JoinList(activity.CategoriesOfDataSubjects)),
            EscapeCsv(JoinList(activity.CategoriesOfPersonalData)),
            EscapeCsv(JoinList(activity.Recipients)),
            EscapeCsv(activity.ThirdCountryTransfers ?? string.Empty),
            EscapeCsv(activity.Safeguards ?? string.Empty),
            ((int)activity.RetentionPeriod.TotalDays).ToString(CultureInfo.InvariantCulture),
            EscapeCsv(activity.SecurityMeasures),
            EscapeCsv(activity.RequestType.FullName ?? activity.RequestType.Name),
            activity.CreatedAtUtc.ToString("O", CultureInfo.InvariantCulture),
            activity.LastUpdatedAtUtc.ToString("O", CultureInfo.InvariantCulture)
        };

        return string.Join(',', fields);
    }

    private static string JoinList(IReadOnlyList<string> values) =>
        string.Join(ListSeparator, values);

    private static string EscapeCsv(string value)
    {
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r') || value.Contains(ListSeparator))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
