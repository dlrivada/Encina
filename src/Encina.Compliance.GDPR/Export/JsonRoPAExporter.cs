using System.Text.Json;
using System.Text.Json.Serialization;

using LanguageExt;

using static LanguageExt.Prelude;

namespace Encina.Compliance.GDPR.Export;

/// <summary>
/// Exports Records of Processing Activities as structured JSON.
/// </summary>
/// <remarks>
/// <para>
/// Produces a JSON document suitable for system integration, archival, and automated
/// compliance reporting. The output follows a structured format with metadata header
/// and an array of processing activity records.
/// </para>
/// <para>
/// Example output structure:
/// <code>
/// {
///   "metadata": {
///     "controllerName": "Acme Corp",
///     "controllerEmail": "privacy@acme.com",
///     "exportedAtUtc": "2026-02-17T10:30:00+00:00",
///     "activityCount": 5
///   },
///   "activities": [ ... ]
/// }
/// </code>
/// </para>
/// </remarks>
public sealed class JsonRoPAExporter : IRoPAExporter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    /// <inheritdoc />
    public string ContentType => "application/json";

    /// <inheritdoc />
    public string FileExtension => ".json";

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
            var document = new RoPAJsonDocument
            {
                Metadata = new RoPAJsonMetadata
                {
                    ControllerName = metadata.ControllerName,
                    ControllerEmail = metadata.ControllerEmail,
                    ExportedAtUtc = metadata.ExportedAtUtc,
                    ActivityCount = activities.Count,
                    DataProtectionOfficer = metadata.DataProtectionOfficer is not null
                        ? new RoPAJsonDpo
                        {
                            Name = metadata.DataProtectionOfficer.Name,
                            Email = metadata.DataProtectionOfficer.Email,
                            Phone = metadata.DataProtectionOfficer.Phone
                        }
                        : null
                },
                Activities = activities.Select(MapActivity).ToList()
            };

            var bytes = JsonSerializer.SerializeToUtf8Bytes(document, JsonOptions);

            var result = new RoPAExportResult(
                Content: bytes,
                ContentType: ContentType,
                FileExtension: FileExtension,
                ActivityCount: activities.Count,
                ExportedAtUtc: metadata.ExportedAtUtc);

            return ValueTask.FromResult<Either<EncinaError, RoPAExportResult>>(Right(result));
        }
        catch (JsonException ex)
        {
            var error = RoPAExportErrors.SerializationFailed("JSON", ex.Message);
            return ValueTask.FromResult<Either<EncinaError, RoPAExportResult>>(error);
        }
    }

    private static RoPAJsonActivity MapActivity(ProcessingActivity activity) => new()
    {
        Id = activity.Id,
        Name = activity.Name,
        Purpose = activity.Purpose,
        LawfulBasis = activity.LawfulBasis,
        CategoriesOfDataSubjects = activity.CategoriesOfDataSubjects,
        CategoriesOfPersonalData = activity.CategoriesOfPersonalData,
        Recipients = activity.Recipients,
        ThirdCountryTransfers = activity.ThirdCountryTransfers,
        Safeguards = activity.Safeguards,
        RetentionDays = (int)activity.RetentionPeriod.TotalDays,
        SecurityMeasures = activity.SecurityMeasures,
        RequestType = activity.RequestType.FullName ?? activity.RequestType.Name,
        CreatedAtUtc = activity.CreatedAtUtc,
        LastUpdatedAtUtc = activity.LastUpdatedAtUtc
    };

    // Internal DTOs for JSON serialization (avoid serializing Type directly)

    private sealed class RoPAJsonDocument
    {
        public required RoPAJsonMetadata Metadata { get; init; }
        public required List<RoPAJsonActivity> Activities { get; init; }
    }

    private sealed class RoPAJsonMetadata
    {
        public required string ControllerName { get; init; }
        public required string ControllerEmail { get; init; }
        public required DateTimeOffset ExportedAtUtc { get; init; }
        public required int ActivityCount { get; init; }
        public RoPAJsonDpo? DataProtectionOfficer { get; init; }
    }

    private sealed class RoPAJsonDpo
    {
        public required string Name { get; init; }
        public required string Email { get; init; }
        public string? Phone { get; init; }
    }

    private sealed class RoPAJsonActivity
    {
        public required Guid Id { get; init; }
        public required string Name { get; init; }
        public required string Purpose { get; init; }
        public required LawfulBasis LawfulBasis { get; init; }
        public required IReadOnlyList<string> CategoriesOfDataSubjects { get; init; }
        public required IReadOnlyList<string> CategoriesOfPersonalData { get; init; }
        public required IReadOnlyList<string> Recipients { get; init; }
        public string? ThirdCountryTransfers { get; init; }
        public string? Safeguards { get; init; }
        public required int RetentionDays { get; init; }
        public required string SecurityMeasures { get; init; }
        public required string RequestType { get; init; }
        public required DateTimeOffset CreatedAtUtc { get; init; }
        public required DateTimeOffset LastUpdatedAtUtc { get; init; }
    }
}
