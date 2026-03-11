using System.Text.Json;

using Encina.Compliance.DPIA.Model;

namespace Encina.Compliance.DPIA;

/// <summary>
/// Maps between <see cref="DPIAAssessment"/> domain records and
/// <see cref="DPIAAssessmentEntity"/> persistence entities.
/// </summary>
/// <remarks>
/// <para>
/// This mapper handles the following type transformations:
/// <list type="bullet">
/// <item>
/// <description>
/// <see cref="DPIAAssessment.Id"/> (<see cref="Guid"/>) ↔
/// <see cref="DPIAAssessmentEntity.Id"/> (<see cref="string"/>), using <c>Guid.ToString("D")</c>.
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="DPIAAssessment.Status"/> (<see cref="DPIAAssessmentStatus"/>) ↔
/// <see cref="DPIAAssessmentEntity.StatusValue"/> (<see cref="int"/>).
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="DPIAAssessment.Result"/> (<see cref="DPIAResult"/>) ↔
/// <see cref="DPIAAssessmentEntity.ResultJson"/> (<see cref="string"/>),
/// using JSON serialization for portable storage.
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="DPIAAssessment.DPOConsultation"/> (<see cref="DPOConsultation"/>) ↔
/// <see cref="DPIAAssessmentEntity.DPOConsultationJson"/> (<see cref="string"/>),
/// using JSON serialization for portable storage.
/// </description>
/// </item>
/// </list>
/// </para>
/// <para>
/// The <see cref="DPIAAssessment.RequestType"/> (CLR type reference) is not persisted —
/// it is a runtime-only property. The <see cref="DPIAAssessment.AuditTrail"/> is not mapped —
/// audit entries are stored separately via <see cref="DPIAAuditEntryMapper"/>.
/// </para>
/// <para>
/// Invalid enum values in a persistence entity result in a <c>null</c> return from
/// <see cref="ToDomain"/>. Invalid JSON in <see cref="DPIAAssessmentEntity.ResultJson"/>
/// or <see cref="DPIAAssessmentEntity.DPOConsultationJson"/> also results in a <c>null</c> return.
/// </para>
/// <para>
/// Used by store implementations (ADO.NET, Dapper, EF Core, MongoDB) to persist and
/// retrieve DPIA assessments without coupling to the domain model.
/// </para>
/// </remarks>
public static class DPIAAssessmentMapper
{
    /// <summary>
    /// Converts a domain <see cref="DPIAAssessment"/> to a persistence entity.
    /// </summary>
    /// <param name="assessment">The domain record to convert.</param>
    /// <returns>A <see cref="DPIAAssessmentEntity"/> suitable for persistence.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="assessment"/> is <c>null</c>.</exception>
    /// <remarks>
    /// The <see cref="DPIAAssessment.RequestType"/> and <see cref="DPIAAssessment.AuditTrail"/>
    /// properties are not included in the entity — <c>RequestType</c> is a runtime-only reference,
    /// and audit entries are persisted separately via <see cref="DPIAAuditEntryMapper"/>.
    /// </remarks>
    public static DPIAAssessmentEntity ToEntity(DPIAAssessment assessment)
    {
        ArgumentNullException.ThrowIfNull(assessment);

        return new DPIAAssessmentEntity
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
            CreatedAtUtc = assessment.CreatedAtUtc,
            ApprovedAtUtc = assessment.ApprovedAtUtc,
            NextReviewAtUtc = assessment.NextReviewAtUtc,
            TenantId = assessment.TenantId,
            ModuleId = assessment.ModuleId
        };
    }

    /// <summary>
    /// Converts a persistence entity back to a domain <see cref="DPIAAssessment"/>.
    /// </summary>
    /// <param name="entity">The persistence entity to convert.</param>
    /// <returns>
    /// A <see cref="DPIAAssessment"/> if the entity state is valid (all enum values are defined,
    /// GUID is parseable, and JSON deserialization succeeds), or <c>null</c> if the entity
    /// contains invalid values.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entity"/> is <c>null</c>.</exception>
    /// <remarks>
    /// The returned <see cref="DPIAAssessment.RequestType"/> will be <c>null</c> (not persisted).
    /// The returned <see cref="DPIAAssessment.AuditTrail"/> will be an empty list — audit entries
    /// must be loaded separately and composed by the store implementation.
    /// </remarks>
    public static DPIAAssessment? ToDomain(DPIAAssessmentEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (!Guid.TryParse(entity.Id, out var id))
        {
            return null;
        }

        if (!Enum.IsDefined(typeof(DPIAAssessmentStatus), entity.StatusValue))
        {
            return null;
        }

        DPIAResult? result = null;
        if (!string.IsNullOrEmpty(entity.ResultJson))
        {
            try
            {
                result = JsonSerializer.Deserialize<DPIAResult>(entity.ResultJson);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        DPOConsultation? consultation = null;
        if (!string.IsNullOrEmpty(entity.DPOConsultationJson))
        {
            try
            {
                consultation = JsonSerializer.Deserialize<DPOConsultation>(entity.DPOConsultationJson);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        return new DPIAAssessment
        {
            Id = id,
            RequestTypeName = entity.RequestTypeName,
            Status = (DPIAAssessmentStatus)entity.StatusValue,
            ProcessingType = entity.ProcessingType,
            Reason = entity.Reason,
            Result = result,
            DPOConsultation = consultation,
            CreatedAtUtc = entity.CreatedAtUtc,
            ApprovedAtUtc = entity.ApprovedAtUtc,
            NextReviewAtUtc = entity.NextReviewAtUtc,
            TenantId = entity.TenantId,
            ModuleId = entity.ModuleId
        };
    }
}
