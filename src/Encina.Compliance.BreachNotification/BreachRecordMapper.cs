using System.Text.Json;

using Encina.Compliance.BreachNotification.Model;

namespace Encina.Compliance.BreachNotification;

/// <summary>
/// Maps between <see cref="BreachRecord"/> domain records and
/// <see cref="BreachRecordEntity"/> persistence entities.
/// </summary>
/// <remarks>
/// <para>
/// This mapper handles the following type transformations:
/// <list type="bullet">
/// <item>
/// <description>
/// <see cref="BreachRecord.Status"/> (<see cref="BreachStatus"/>) ↔
/// <see cref="BreachRecordEntity.StatusValue"/> (<see cref="int"/>).
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="BreachRecord.Severity"/> (<see cref="BreachSeverity"/>) ↔
/// <see cref="BreachRecordEntity.SeverityValue"/> (<see cref="int"/>).
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="BreachRecord.SubjectNotificationExemption"/> (<see cref="SubjectNotificationExemption"/>) ↔
/// <see cref="BreachRecordEntity.SubjectNotificationExemptionValue"/> (<see cref="int"/>).
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="BreachRecord.CategoriesOfDataAffected"/> (<see cref="IReadOnlyList{T}"/>) ↔
/// <see cref="BreachRecordEntity.CategoriesOfDataAffected"/> (<see cref="string"/>),
/// using JSON serialization for portable storage.
/// </description>
/// </item>
/// </list>
/// </para>
/// <para>
/// Invalid enum values in a persistence entity result in a <c>null</c> return from
/// <see cref="ToDomain"/>. The <see cref="BreachRecord.PhasedReports"/> collection is
/// NOT mapped by this mapper — phased reports are stored in a separate table and must
/// be loaded independently.
/// </para>
/// <para>
/// Used by store implementations (ADO.NET, Dapper, EF Core, MongoDB) to persist and
/// retrieve breach records without coupling to the domain model.
/// </para>
/// </remarks>
public static class BreachRecordMapper
{
    /// <summary>
    /// Converts a domain <see cref="BreachRecord"/> to a persistence entity.
    /// </summary>
    /// <param name="record">The domain record to convert.</param>
    /// <returns>A <see cref="BreachRecordEntity"/> suitable for persistence.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="record"/> is <c>null</c>.</exception>
    /// <remarks>
    /// The <see cref="BreachRecord.PhasedReports"/> collection is not included in the
    /// entity — phased reports are persisted separately via <see cref="PhasedReportMapper"/>.
    /// </remarks>
    public static BreachRecordEntity ToEntity(BreachRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        return new BreachRecordEntity
        {
            Id = record.Id,
            Nature = record.Nature,
            ApproximateSubjectsAffected = record.ApproximateSubjectsAffected,
            CategoriesOfDataAffected = JsonSerializer.Serialize(record.CategoriesOfDataAffected),
            DPOContactDetails = record.DPOContactDetails,
            LikelyConsequences = record.LikelyConsequences,
            MeasuresTaken = record.MeasuresTaken,
            DetectedAtUtc = record.DetectedAtUtc,
            NotificationDeadlineUtc = record.NotificationDeadlineUtc,
            NotifiedAuthorityAtUtc = record.NotifiedAuthorityAtUtc,
            NotifiedSubjectsAtUtc = record.NotifiedSubjectsAtUtc,
            SeverityValue = (int)record.Severity,
            StatusValue = (int)record.Status,
            DelayReason = record.DelayReason,
            SubjectNotificationExemptionValue = (int)record.SubjectNotificationExemption,
            ResolvedAtUtc = record.ResolvedAtUtc,
            ResolutionSummary = record.ResolutionSummary
        };
    }

    /// <summary>
    /// Converts a persistence entity back to a domain <see cref="BreachRecord"/>.
    /// </summary>
    /// <param name="entity">The persistence entity to convert.</param>
    /// <returns>
    /// A <see cref="BreachRecord"/> if the entity state is valid (all enum values are defined
    /// and JSON deserialization succeeds), or <c>null</c> if the entity contains invalid values.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entity"/> is <c>null</c>.</exception>
    /// <remarks>
    /// The returned <see cref="BreachRecord.PhasedReports"/> will be an empty list.
    /// Phased reports must be loaded separately and composed by the store implementation.
    /// </remarks>
    public static BreachRecord? ToDomain(BreachRecordEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (!Enum.IsDefined(typeof(BreachStatus), entity.StatusValue))
        {
            return null;
        }

        if (!Enum.IsDefined(typeof(BreachSeverity), entity.SeverityValue))
        {
            return null;
        }

        if (!Enum.IsDefined(typeof(SubjectNotificationExemption), entity.SubjectNotificationExemptionValue))
        {
            return null;
        }

        IReadOnlyList<string>? categories = null;
        if (!string.IsNullOrEmpty(entity.CategoriesOfDataAffected))
        {
            categories = JsonSerializer.Deserialize<List<string>>(entity.CategoriesOfDataAffected);
        }

        return new BreachRecord
        {
            Id = entity.Id,
            Nature = entity.Nature,
            ApproximateSubjectsAffected = entity.ApproximateSubjectsAffected,
            CategoriesOfDataAffected = categories ?? [],
            DPOContactDetails = entity.DPOContactDetails,
            LikelyConsequences = entity.LikelyConsequences,
            MeasuresTaken = entity.MeasuresTaken,
            DetectedAtUtc = entity.DetectedAtUtc,
            NotificationDeadlineUtc = entity.NotificationDeadlineUtc,
            NotifiedAuthorityAtUtc = entity.NotifiedAuthorityAtUtc,
            NotifiedSubjectsAtUtc = entity.NotifiedSubjectsAtUtc,
            Severity = (BreachSeverity)entity.SeverityValue,
            Status = (BreachStatus)entity.StatusValue,
            DelayReason = entity.DelayReason,
            SubjectNotificationExemption = (SubjectNotificationExemption)entity.SubjectNotificationExemptionValue,
            ResolvedAtUtc = entity.ResolvedAtUtc,
            ResolutionSummary = entity.ResolutionSummary
        };
    }
}
