using Encina.Compliance.BreachNotification.Model;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Encina.MongoDB.BreachNotification;

/// <summary>
/// MongoDB document representation of a <see cref="PhasedReport"/>.
/// </summary>
/// <remarks>
/// <para>
/// Maps to the breach_phased_reports collection. Phased reports are stored in a separate
/// collection from breach records, linked by <see cref="BreachId"/>, to support the
/// progressive disclosure requirement of GDPR Article 33(4).
/// </para>
/// <para>
/// Uses snake_case naming convention for MongoDB field names. All properties are primitive
/// types with no enum or complex type transformations required.
/// </para>
/// </remarks>
public sealed class PhasedReportDocument
{
    /// <summary>
    /// Gets or sets the unique identifier for this phased report.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the identifier of the breach this report belongs to.
    /// </summary>
    [BsonElement("breach_id")]
    public string BreachId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sequential report number, starting at 1 for the initial report.
    /// </summary>
    [BsonElement("report_number")]
    public int ReportNumber { get; set; }

    /// <summary>
    /// Gets or sets the content of the phased report.
    /// </summary>
    [BsonElement("content")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when this phased report was submitted (UTC).
    /// </summary>
    [BsonElement("submitted_at_utc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime SubmittedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who submitted this phased report.
    /// </summary>
    [BsonElement("submitted_by_user_id")]
    public string? SubmittedByUserId { get; set; }

    /// <summary>
    /// Creates a <see cref="PhasedReportDocument"/> from a <see cref="PhasedReport"/>.
    /// </summary>
    /// <param name="breachId">The identifier of the breach this report belongs to.</param>
    /// <param name="report">The phased report to convert.</param>
    /// <returns>A new document representation of the phased report.</returns>
    public static PhasedReportDocument FromReport(string breachId, PhasedReport report)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(breachId);
        ArgumentNullException.ThrowIfNull(report);

        return new PhasedReportDocument
        {
            Id = report.Id,
            BreachId = breachId,
            ReportNumber = report.ReportNumber,
            Content = report.Content,
            SubmittedAtUtc = report.SubmittedAtUtc.UtcDateTime,
            SubmittedByUserId = report.SubmittedByUserId
        };
    }

    /// <summary>
    /// Converts this document to a <see cref="PhasedReport"/>.
    /// </summary>
    /// <returns>A phased report domain record.</returns>
    public PhasedReport ToReport() => new()
    {
        Id = Id,
        BreachId = BreachId,
        ReportNumber = ReportNumber,
        Content = Content,
        SubmittedAtUtc = new DateTimeOffset(SubmittedAtUtc, TimeSpan.Zero),
        SubmittedByUserId = SubmittedByUserId
    };
}
