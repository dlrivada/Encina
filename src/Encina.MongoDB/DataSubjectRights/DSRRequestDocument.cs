using Encina.Compliance.DataSubjectRights;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Encina.MongoDB.DataSubjectRights;

/// <summary>
/// MongoDB document representation of a DSR request.
/// </summary>
public sealed class DSRRequestDocument
{
    /// <summary>
    /// Gets or sets the unique identifier of the DSR request.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the data subject identifier.
    /// </summary>
    [BsonElement("subject_id")]
    public string SubjectId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the integer value of the <see cref="DataSubjectRight"/> enum.
    /// </summary>
    [BsonElement("right_type_value")]
    public int RightTypeValue { get; set; }

    /// <summary>
    /// Gets or sets the integer value of the <see cref="DSRRequestStatus"/> enum.
    /// </summary>
    [BsonElement("status_value")]
    public int StatusValue { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the request was received.
    /// </summary>
    [BsonElement("received_at_utc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime ReceivedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC deadline for completing the request.
    /// </summary>
    [BsonElement("deadline_at_utc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime DeadlineAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the request was completed, if applicable.
    /// </summary>
    [BsonElement("completed_at_utc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    [BsonIgnoreIfNull]
    public DateTime? CompletedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the reason for extending the deadline, if applicable.
    /// </summary>
    [BsonElement("extension_reason")]
    [BsonIgnoreIfNull]
    public string? ExtensionReason { get; set; }

    /// <summary>
    /// Gets or sets the extended deadline UTC timestamp, if the request was extended.
    /// </summary>
    [BsonElement("extended_deadline_at_utc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    [BsonIgnoreIfNull]
    public DateTime? ExtendedDeadlineAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the reason for rejecting the request, if applicable.
    /// </summary>
    [BsonElement("rejection_reason")]
    [BsonIgnoreIfNull]
    public string? RejectionReason { get; set; }

    /// <summary>
    /// Gets or sets additional details about the request.
    /// </summary>
    [BsonElement("request_details")]
    [BsonIgnoreIfNull]
    public string? RequestDetails { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the subject's identity was verified, if applicable.
    /// </summary>
    [BsonElement("verified_at_utc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    [BsonIgnoreIfNull]
    public DateTime? VerifiedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who processed the request, if applicable.
    /// </summary>
    [BsonElement("processed_by_user_id")]
    [BsonIgnoreIfNull]
    public string? ProcessedByUserId { get; set; }

    /// <summary>
    /// Creates a document from a domain DSR request.
    /// </summary>
    public static DSRRequestDocument FromDomain(DSRRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new DSRRequestDocument
        {
            Id = request.Id,
            SubjectId = request.SubjectId,
            RightTypeValue = (int)request.RightType,
            StatusValue = (int)request.Status,
            ReceivedAtUtc = request.ReceivedAtUtc.UtcDateTime,
            DeadlineAtUtc = request.DeadlineAtUtc.UtcDateTime,
            CompletedAtUtc = request.CompletedAtUtc?.UtcDateTime,
            ExtensionReason = request.ExtensionReason,
            ExtendedDeadlineAtUtc = request.ExtendedDeadlineAtUtc?.UtcDateTime,
            RejectionReason = request.RejectionReason,
            RequestDetails = request.RequestDetails,
            VerifiedAtUtc = request.VerifiedAtUtc?.UtcDateTime,
            ProcessedByUserId = request.ProcessedByUserId
        };
    }

    /// <summary>
    /// Converts this document to a domain DSR request.
    /// </summary>
    public DSRRequest? ToDomain()
    {
        if (!Enum.IsDefined(typeof(DataSubjectRight), RightTypeValue) ||
            !Enum.IsDefined(typeof(DSRRequestStatus), StatusValue))
        {
            return null;
        }

        return new DSRRequest
        {
            Id = Id,
            SubjectId = SubjectId,
            RightType = (DataSubjectRight)RightTypeValue,
            Status = (DSRRequestStatus)StatusValue,
            ReceivedAtUtc = new DateTimeOffset(ReceivedAtUtc, TimeSpan.Zero),
            DeadlineAtUtc = new DateTimeOffset(DeadlineAtUtc, TimeSpan.Zero),
            CompletedAtUtc = CompletedAtUtc.HasValue ? new DateTimeOffset(CompletedAtUtc.Value, TimeSpan.Zero) : null,
            ExtensionReason = ExtensionReason,
            ExtendedDeadlineAtUtc = ExtendedDeadlineAtUtc.HasValue ? new DateTimeOffset(ExtendedDeadlineAtUtc.Value, TimeSpan.Zero) : null,
            RejectionReason = RejectionReason,
            RequestDetails = RequestDetails,
            VerifiedAtUtc = VerifiedAtUtc.HasValue ? new DateTimeOffset(VerifiedAtUtc.Value, TimeSpan.Zero) : null,
            ProcessedByUserId = ProcessedByUserId
        };
    }
}
