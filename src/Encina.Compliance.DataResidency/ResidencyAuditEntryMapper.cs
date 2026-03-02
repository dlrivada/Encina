using Encina.Compliance.DataResidency.Model;

namespace Encina.Compliance.DataResidency;

/// <summary>
/// Maps between <see cref="ResidencyAuditEntry"/> domain records and
/// <see cref="ResidencyAuditEntryEntity"/> persistence entities.
/// </summary>
/// <remarks>
/// <para>
/// This mapper handles two key type transformations:
/// <list type="bullet">
/// <item>
/// <description>
/// <see cref="ResidencyAuditEntry.Action"/> (<see cref="ResidencyAction"/>) ↔
/// <see cref="ResidencyAuditEntryEntity.ActionValue"/> (<see cref="int"/>),
/// using integer casting for cross-provider compatibility.
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="ResidencyAuditEntry.Outcome"/> (<see cref="ResidencyOutcome"/>) ↔
/// <see cref="ResidencyAuditEntryEntity.OutcomeValue"/> (<see cref="int"/>),
/// using integer casting for cross-provider compatibility.
/// </description>
/// </item>
/// </list>
/// </para>
/// <para>
/// Used by store implementations (ADO.NET, Dapper, EF Core, MongoDB) to persist and
/// retrieve residency audit entries without coupling to the domain model.
/// </para>
/// </remarks>
public static class ResidencyAuditEntryMapper
{
    /// <summary>
    /// Converts a domain <see cref="ResidencyAuditEntry"/> to a persistence entity.
    /// </summary>
    /// <param name="entry">The domain audit entry to convert.</param>
    /// <returns>A <see cref="ResidencyAuditEntryEntity"/> suitable for persistence.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entry"/> is <c>null</c>.</exception>
    public static ResidencyAuditEntryEntity ToEntity(ResidencyAuditEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        return new ResidencyAuditEntryEntity
        {
            Id = entry.Id,
            EntityId = entry.EntityId,
            DataCategory = entry.DataCategory,
            SourceRegion = entry.SourceRegion,
            TargetRegion = entry.TargetRegion,
            ActionValue = (int)entry.Action,
            OutcomeValue = (int)entry.Outcome,
            LegalBasis = entry.LegalBasis,
            RequestType = entry.RequestType,
            UserId = entry.UserId,
            TimestampUtc = entry.TimestampUtc,
            Details = entry.Details
        };
    }

    /// <summary>
    /// Converts a persistence entity back to a domain <see cref="ResidencyAuditEntry"/>.
    /// </summary>
    /// <param name="entity">The persistence entity to convert.</param>
    /// <returns>
    /// A <see cref="ResidencyAuditEntry"/> if the entity state is valid (enum values are defined),
    /// or <c>null</c> if the entity contains invalid enum values.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entity"/> is <c>null</c>.</exception>
    public static ResidencyAuditEntry? ToDomain(ResidencyAuditEntryEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (!Enum.IsDefined(typeof(ResidencyAction), entity.ActionValue))
        {
            return null;
        }

        if (!Enum.IsDefined(typeof(ResidencyOutcome), entity.OutcomeValue))
        {
            return null;
        }

        return new ResidencyAuditEntry
        {
            Id = entity.Id,
            EntityId = entity.EntityId,
            DataCategory = entity.DataCategory,
            SourceRegion = entity.SourceRegion,
            TargetRegion = entity.TargetRegion,
            Action = (ResidencyAction)entity.ActionValue,
            Outcome = (ResidencyOutcome)entity.OutcomeValue,
            LegalBasis = entity.LegalBasis,
            RequestType = entity.RequestType,
            UserId = entity.UserId,
            TimestampUtc = entity.TimestampUtc,
            Details = entity.Details
        };
    }
}
