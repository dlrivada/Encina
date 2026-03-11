using Encina.Compliance.DPIA.Model;

namespace Encina.Compliance.DPIA;

/// <summary>
/// Maps between <see cref="DPIAAuditEntry"/> domain records and
/// <see cref="DPIAAuditEntryEntity"/> persistence entities.
/// </summary>
/// <remarks>
/// <para>
/// This mapper handles the following type transformations:
/// <list type="bullet">
/// <item>
/// <description>
/// <see cref="DPIAAuditEntry.Id"/> (<see cref="Guid"/>) ↔
/// <see cref="DPIAAuditEntryEntity.Id"/> (<see cref="string"/>), using <c>Guid.ToString("D")</c>.
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="DPIAAuditEntry.AssessmentId"/> (<see cref="Guid"/>) ↔
/// <see cref="DPIAAuditEntryEntity.AssessmentId"/> (<see cref="string"/>), using <c>Guid.ToString("D")</c>.
/// </description>
/// </item>
/// </list>
/// </para>
/// <para>
/// Invalid GUID values in a persistence entity result in a <c>null</c> return from
/// <see cref="ToDomain"/>.
/// </para>
/// <para>
/// Used by store implementations (ADO.NET, Dapper, EF Core, MongoDB) to persist and
/// retrieve DPIA audit entries without coupling to the domain model.
/// </para>
/// </remarks>
public static class DPIAAuditEntryMapper
{
    /// <summary>
    /// Converts a domain <see cref="DPIAAuditEntry"/> to a persistence entity.
    /// </summary>
    /// <param name="entry">The domain record to convert.</param>
    /// <returns>A <see cref="DPIAAuditEntryEntity"/> suitable for persistence.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entry"/> is <c>null</c>.</exception>
    public static DPIAAuditEntryEntity ToEntity(DPIAAuditEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        return new DPIAAuditEntryEntity
        {
            Id = entry.Id.ToString("D"),
            AssessmentId = entry.AssessmentId.ToString("D"),
            Action = entry.Action,
            PerformedBy = entry.PerformedBy,
            OccurredAtUtc = entry.OccurredAtUtc,
            Details = entry.Details,
            TenantId = entry.TenantId,
            ModuleId = entry.ModuleId
        };
    }

    /// <summary>
    /// Converts a persistence entity back to a domain <see cref="DPIAAuditEntry"/>.
    /// </summary>
    /// <param name="entity">The persistence entity to convert.</param>
    /// <returns>
    /// A <see cref="DPIAAuditEntry"/> if the entity state is valid (both GUID values are parseable),
    /// or <c>null</c> if the entity contains invalid values.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entity"/> is <c>null</c>.</exception>
    public static DPIAAuditEntry? ToDomain(DPIAAuditEntryEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (!Guid.TryParse(entity.Id, out var id))
        {
            return null;
        }

        if (!Guid.TryParse(entity.AssessmentId, out var assessmentId))
        {
            return null;
        }

        return new DPIAAuditEntry
        {
            Id = id,
            AssessmentId = assessmentId,
            Action = entity.Action,
            PerformedBy = entity.PerformedBy,
            OccurredAtUtc = entity.OccurredAtUtc,
            Details = entity.Details,
            TenantId = entity.TenantId,
            ModuleId = entity.ModuleId
        };
    }
}
