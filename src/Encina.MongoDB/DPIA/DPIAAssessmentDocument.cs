using System.Text.Json;
using Encina.Compliance.DPIA;
using Encina.Compliance.DPIA.Model;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Encina.MongoDB.DPIA;

/// <summary>
/// MongoDB BSON document for <see cref="DPIAAssessmentEntity"/>.
/// </summary>
/// <remarks>
/// <para>
/// Maps <see cref="DPIAAssessment"/> domain records to a MongoDB-native document format
/// with BSON annotations for proper serialization and indexing.
/// </para>
/// <para>
/// Key type transformations:
/// <list type="bullet">
/// <item><description><see cref="DPIAAssessment.Id"/> (Guid) → <see cref="Id"/> (string, GUID "D" format).</description></item>
/// <item><description><see cref="DPIAAssessment.Status"/> (enum) → <see cref="StatusValue"/> (int).</description></item>
/// <item><description><see cref="DPIAAssessment.Result"/> (DPIAResult) → <see cref="ResultJson"/> (JSON string).</description></item>
/// <item><description><see cref="DPIAAssessment.DPOConsultation"/> (DPOConsultation) → <see cref="DPOConsultationJson"/> (JSON string).</description></item>
/// <item><description>DateTimeOffset → DateTime (UTC) for MongoDB native date storage.</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class DPIAAssessmentDocument
{
    /// <summary>
    /// Unique identifier, stored as a GUID string.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The fully-qualified type name of the request this assessment covers.
    /// </summary>
    [BsonElement("request_type_name")]
    public string RequestTypeName { get; set; } = string.Empty;

    /// <summary>
    /// Integer value of the <see cref="DPIAAssessmentStatus"/> enum.
    /// </summary>
    [BsonElement("status")]
    public int StatusValue { get; set; }

    /// <summary>
    /// The type of processing covered by this assessment.
    /// </summary>
    [BsonElement("processing_type")]
    public string? ProcessingType { get; set; }

    /// <summary>
    /// The reason or justification for conducting this assessment.
    /// </summary>
    [BsonElement("reason")]
    public string? Reason { get; set; }

    /// <summary>
    /// JSON representation of the <see cref="DPIAResult"/>.
    /// </summary>
    [BsonElement("result_json")]
    public string? ResultJson { get; set; }

    /// <summary>
    /// JSON representation of the <see cref="DPOConsultation"/>.
    /// </summary>
    [BsonElement("dpo_consultation_json")]
    public string? DPOConsultationJson { get; set; }

    /// <summary>
    /// Timestamp when this assessment was created (UTC).
    /// </summary>
    [BsonElement("created_at_utc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Timestamp when this assessment was approved (UTC), or null if not yet approved.
    /// </summary>
    [BsonElement("approved_at_utc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? ApprovedAtUtc { get; set; }

    /// <summary>
    /// Timestamp for the next scheduled review (UTC).
    /// </summary>
    [BsonElement("next_review_at_utc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? NextReviewAtUtc { get; set; }

    /// <summary>
    /// Creates a document from a domain <see cref="DPIAAssessment"/>.
    /// </summary>
    /// <param name="assessment">The domain assessment to convert.</param>
    /// <returns>A <see cref="DPIAAssessmentDocument"/> suitable for MongoDB persistence.</returns>
    public static DPIAAssessmentDocument FromAssessment(DPIAAssessment assessment)
    {
        ArgumentNullException.ThrowIfNull(assessment);

        return new DPIAAssessmentDocument
        {
            Id = assessment.Id.ToString("D"),
            RequestTypeName = assessment.RequestTypeName,
            StatusValue = (int)assessment.Status,
            ProcessingType = assessment.ProcessingType,
            Reason = assessment.Reason,
            ResultJson = assessment.Result is not null
                ? JsonSerializer.Serialize(assessment.Result)
                : null,
            DPOConsultationJson = assessment.DPOConsultation is not null
                ? JsonSerializer.Serialize(assessment.DPOConsultation)
                : null,
            CreatedAtUtc = assessment.CreatedAtUtc.UtcDateTime,
            ApprovedAtUtc = assessment.ApprovedAtUtc?.UtcDateTime,
            NextReviewAtUtc = assessment.NextReviewAtUtc?.UtcDateTime
        };
    }

    /// <summary>
    /// Converts this document back to a domain <see cref="DPIAAssessment"/>.
    /// </summary>
    /// <returns>
    /// A <see cref="DPIAAssessment"/> if valid, or <c>null</c> if the document contains
    /// invalid values (invalid GUID, undefined enum, or malformed JSON).
    /// </returns>
    public DPIAAssessment? ToAssessment()
    {
        if (!Guid.TryParse(Id, out var id))
            return null;

        if (!Enum.IsDefined(typeof(DPIAAssessmentStatus), StatusValue))
            return null;

        DPIAResult? result = null;
        if (!string.IsNullOrEmpty(ResultJson))
        {
            try
            {
                result = JsonSerializer.Deserialize<DPIAResult>(ResultJson);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        DPOConsultation? consultation = null;
        if (!string.IsNullOrEmpty(DPOConsultationJson))
        {
            try
            {
                consultation = JsonSerializer.Deserialize<DPOConsultation>(DPOConsultationJson);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        return new DPIAAssessment
        {
            Id = id,
            RequestTypeName = RequestTypeName,
            Status = (DPIAAssessmentStatus)StatusValue,
            ProcessingType = ProcessingType,
            Reason = Reason,
            Result = result,
            DPOConsultation = consultation,
            CreatedAtUtc = new DateTimeOffset(CreatedAtUtc, TimeSpan.Zero),
            ApprovedAtUtc = ApprovedAtUtc.HasValue
                ? new DateTimeOffset(ApprovedAtUtc.Value, TimeSpan.Zero)
                : null,
            NextReviewAtUtc = NextReviewAtUtc.HasValue
                ? new DateTimeOffset(NextReviewAtUtc.Value, TimeSpan.Zero)
                : null
        };
    }
}
