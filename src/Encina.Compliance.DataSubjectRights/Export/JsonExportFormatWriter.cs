using System.Text.Json;
using System.Text.Json.Serialization;

using LanguageExt;

using static LanguageExt.Prelude;

namespace Encina.Compliance.DataSubjectRights;

/// <summary>
/// Exports personal data as structured JSON for data portability (Article 20).
/// </summary>
/// <remarks>
/// <para>
/// Produces a JSON document containing an array of personal data entries, each with
/// entity type, entity ID, field name, category, and current value.
/// </para>
/// <para>
/// Uses <see cref="System.Text.Json"/> with camelCase naming and indented formatting for
/// human readability while maintaining machine-readability as required by Article 20.
/// </para>
/// </remarks>
public sealed class JsonExportFormatWriter : IExportFormatWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    /// <inheritdoc />
    public ExportFormat SupportedFormat => ExportFormat.JSON;

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, ExportedData>> WriteAsync(
        IReadOnlyList<PersonalDataLocation> data,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(data);

        try
        {
            var entries = data.Select(d => new JsonPersonalDataEntry
            {
                EntityType = d.EntityType.FullName ?? d.EntityType.Name,
                EntityId = d.EntityId,
                FieldName = d.FieldName,
                Category = d.Category,
                Value = d.CurrentValue
            }).ToList();

            var bytes = JsonSerializer.SerializeToUtf8Bytes(entries, JsonOptions);

            var result = new ExportedData
            {
                Content = bytes,
                ContentType = "application/json",
                FileName = $"personal-data-export-{DateTime.UtcNow:yyyyMMdd}.json",
                Format = ExportFormat.JSON,
                FieldCount = data.Count
            };

            return ValueTask.FromResult<Either<EncinaError, ExportedData>>(Right(result));
        }
        catch (JsonException ex)
        {
            return ValueTask.FromResult<Either<EncinaError, ExportedData>>(
                DSRErrors.ExportFailed("N/A", ExportFormat.JSON, $"JSON serialization failed: {ex.Message}"));
        }
    }

    private sealed class JsonPersonalDataEntry
    {
        public required string EntityType { get; init; }
        public required string EntityId { get; init; }
        public required string FieldName { get; init; }
        public required PersonalDataCategory Category { get; init; }
        public object? Value { get; init; }
    }
}
