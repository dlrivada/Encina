using System.Diagnostics.CodeAnalysis;
using Encina.Compliance.BreachNotification;
using Encina.Compliance.BreachNotification.Model;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Encina.MongoDB.BreachNotification;

/// <summary>
/// MongoDB document representation of a <see cref="BreachRecord"/>.
/// </summary>
/// <remarks>
/// <para>
/// Maps to the breach_records collection. Each document tracks a personal data breach
/// throughout its lifecycle, from detection through authority notification, data subject
/// notification, phased reporting, and resolution per GDPR Articles 33 and 34.
/// </para>
/// <para>
/// Uses snake_case naming convention for MongoDB field names. Enums are stored as integers:
/// <see cref="SeverityValue"/> maps to <see cref="BreachSeverity"/>,
/// <see cref="StatusValue"/> maps to <see cref="BreachStatus"/>, and
/// <see cref="SubjectNotificationExemptionValue"/> maps to <see cref="SubjectNotificationExemption"/>.
/// </para>
/// <para>
/// Unlike the relational providers (which store <c>CategoriesOfDataAffected</c> as a JSON string),
/// MongoDB stores it as a native BSON array for efficient querying and indexing.
/// </para>
/// </remarks>
public sealed class BreachRecordDocument
{
    /// <summary>
    /// Gets or sets the unique identifier for this breach record.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the nature of the personal data breach.
    /// </summary>
    [BsonElement("nature")]
    public string Nature { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the approximate number of data subjects affected by the breach.
    /// </summary>
    [BsonElement("approximate_subjects_affected")]
    public int ApproximateSubjectsAffected { get; set; }

    /// <summary>
    /// Gets or sets the categories of personal data affected by the breach.
    /// </summary>
    /// <remarks>
    /// Stored as a native BSON array, not a JSON string. This allows MongoDB-native
    /// array operations and indexing on individual category values.
    /// </remarks>
    [BsonElement("categories_of_data_affected")]
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "MongoDB BSON deserialization requires mutable setter")]
    [SuppressMessage("Design", "CA1002:Do not expose generic lists", Justification = "MongoDB BSON driver maps List<T> to native BSON arrays")]
    public List<string> CategoriesOfDataAffected { get; set; } = [];

    /// <summary>
    /// Gets or sets the name and contact details of the DPO or other contact point.
    /// </summary>
    [BsonElement("dpo_contact_details")]
    public string DPOContactDetails { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the likely consequences of the breach.
    /// </summary>
    [BsonElement("likely_consequences")]
    public string LikelyConsequences { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the measures taken or proposed to address the breach.
    /// </summary>
    [BsonElement("measures_taken")]
    public string MeasuresTaken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the breach was detected (UTC).
    /// </summary>
    [BsonElement("detected_at_utc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime DetectedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the deadline for notifying the supervisory authority (UTC).
    /// </summary>
    [BsonElement("notification_deadline_utc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime NotificationDeadlineUtc { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the supervisory authority was notified (UTC).
    /// </summary>
    [BsonElement("notified_authority_at_utc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? NotifiedAuthorityAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when data subjects were notified (UTC).
    /// </summary>
    [BsonElement("notified_subjects_at_utc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? NotifiedSubjectsAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the severity as an integer value.
    /// </summary>
    /// <remarks>
    /// Maps to <see cref="BreachSeverity"/> enum values:
    /// 0 = Low, 1 = Medium, 2 = High, 3 = Critical.
    /// </remarks>
    [BsonElement("severity")]
    public int SeverityValue { get; set; }

    /// <summary>
    /// Gets or sets the lifecycle status as an integer value.
    /// </summary>
    /// <remarks>
    /// Maps to <see cref="BreachStatus"/> enum values:
    /// 0 = Detected, 1 = Investigating, 2 = AuthorityNotified,
    /// 3 = SubjectsNotified, 4 = Resolved, 5 = Closed.
    /// </remarks>
    [BsonElement("status")]
    public int StatusValue { get; set; }

    /// <summary>
    /// Gets or sets the reason for delaying notification beyond the 72-hour deadline.
    /// </summary>
    [BsonElement("delay_reason")]
    public string? DelayReason { get; set; }

    /// <summary>
    /// Gets or sets the subject notification exemption as an integer value.
    /// </summary>
    /// <remarks>
    /// Maps to <see cref="SubjectNotificationExemption"/> enum values:
    /// 0 = None, 1 = EncryptionProtected, 2 = MitigatingMeasures, 3 = DisproportionateEffort.
    /// </remarks>
    [BsonElement("subject_notification_exemption")]
    public int SubjectNotificationExemptionValue { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the breach was resolved (UTC).
    /// </summary>
    [BsonElement("resolved_at_utc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? ResolvedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the summary of the resolution measures and outcomes.
    /// </summary>
    [BsonElement("resolution_summary")]
    public string? ResolutionSummary { get; set; }

    /// <summary>
    /// Creates a <see cref="BreachRecordDocument"/> from a <see cref="BreachRecord"/>.
    /// </summary>
    /// <param name="record">The breach record to convert.</param>
    /// <returns>A new document representation of the breach record.</returns>
    /// <remarks>
    /// Uses <see cref="BreachRecordMapper.ToEntity"/> for consistent enum-to-int conversion,
    /// then maps entity fields to MongoDB document fields. <see cref="CategoriesOfDataAffected"/>
    /// is stored as a native BSON array (not the JSON string used by relational providers).
    /// </remarks>
    public static BreachRecordDocument FromRecord(BreachRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        var entity = BreachRecordMapper.ToEntity(record);

        return new BreachRecordDocument
        {
            Id = entity.Id,
            Nature = entity.Nature,
            ApproximateSubjectsAffected = entity.ApproximateSubjectsAffected,
            CategoriesOfDataAffected = record.CategoriesOfDataAffected.ToList(),
            DPOContactDetails = entity.DPOContactDetails,
            LikelyConsequences = entity.LikelyConsequences,
            MeasuresTaken = entity.MeasuresTaken,
            DetectedAtUtc = record.DetectedAtUtc.UtcDateTime,
            NotificationDeadlineUtc = record.NotificationDeadlineUtc.UtcDateTime,
            NotifiedAuthorityAtUtc = record.NotifiedAuthorityAtUtc?.UtcDateTime,
            NotifiedSubjectsAtUtc = record.NotifiedSubjectsAtUtc?.UtcDateTime,
            SeverityValue = entity.SeverityValue,
            StatusValue = entity.StatusValue,
            DelayReason = entity.DelayReason,
            SubjectNotificationExemptionValue = entity.SubjectNotificationExemptionValue,
            ResolvedAtUtc = record.ResolvedAtUtc?.UtcDateTime,
            ResolutionSummary = entity.ResolutionSummary
        };
    }

    /// <summary>
    /// Converts this document to a <see cref="BreachRecord"/>.
    /// </summary>
    /// <returns>
    /// A <see cref="BreachRecord"/> if all enum values are valid, or <c>null</c> if the document
    /// contains invalid enum values that cannot be mapped to domain types.
    /// </returns>
    /// <remarks>
    /// The returned <see cref="BreachRecord.PhasedReports"/> will be an empty list.
    /// Phased reports are stored in a separate collection and must be loaded independently
    /// by the store implementation.
    /// </remarks>
    public BreachRecord? ToRecord()
    {
        if (!Enum.IsDefined(typeof(BreachStatus), StatusValue))
        {
            return null;
        }

        if (!Enum.IsDefined(typeof(BreachSeverity), SeverityValue))
        {
            return null;
        }

        if (!Enum.IsDefined(typeof(SubjectNotificationExemption), SubjectNotificationExemptionValue))
        {
            return null;
        }

        return new BreachRecord
        {
            Id = Id,
            Nature = Nature,
            ApproximateSubjectsAffected = ApproximateSubjectsAffected,
            CategoriesOfDataAffected = CategoriesOfDataAffected ?? [],
            DPOContactDetails = DPOContactDetails,
            LikelyConsequences = LikelyConsequences,
            MeasuresTaken = MeasuresTaken,
            DetectedAtUtc = new DateTimeOffset(DetectedAtUtc, TimeSpan.Zero),
            NotificationDeadlineUtc = new DateTimeOffset(NotificationDeadlineUtc, TimeSpan.Zero),
            NotifiedAuthorityAtUtc = NotifiedAuthorityAtUtc.HasValue
                ? new DateTimeOffset(NotifiedAuthorityAtUtc.Value, TimeSpan.Zero)
                : null,
            NotifiedSubjectsAtUtc = NotifiedSubjectsAtUtc.HasValue
                ? new DateTimeOffset(NotifiedSubjectsAtUtc.Value, TimeSpan.Zero)
                : null,
            Severity = (BreachSeverity)SeverityValue,
            Status = (BreachStatus)StatusValue,
            DelayReason = DelayReason,
            SubjectNotificationExemption = (SubjectNotificationExemption)SubjectNotificationExemptionValue,
            ResolvedAtUtc = ResolvedAtUtc.HasValue
                ? new DateTimeOffset(ResolvedAtUtc.Value, TimeSpan.Zero)
                : null,
            ResolutionSummary = ResolutionSummary
        };
    }
}
