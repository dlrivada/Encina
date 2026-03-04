using Encina.Compliance.BreachNotification.Model;

namespace Encina.Compliance.BreachNotification;

/// <summary>
/// Maps between <see cref="BreachAuditEntry"/> domain records and
/// <see cref="BreachAuditEntryEntity"/> persistence entities.
/// </summary>
/// <remarks>
/// <para>
/// This is a straightforward property-to-property mapper with no type transformations.
/// All properties are primitive types compatible across all storage providers.
/// </para>
/// <para>
/// Used by store implementations (ADO.NET, Dapper, EF Core, MongoDB) to persist and
/// retrieve breach audit entries without coupling to the domain model.
/// </para>
/// </remarks>
public static class BreachAuditEntryMapper
{
    /// <summary>
    /// Converts a domain <see cref="BreachAuditEntry"/> to a persistence entity.
    /// </summary>
    /// <param name="entry">The domain audit entry to convert.</param>
    /// <returns>A <see cref="BreachAuditEntryEntity"/> suitable for persistence.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entry"/> is <c>null</c>.</exception>
    public static BreachAuditEntryEntity ToEntity(BreachAuditEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        return new BreachAuditEntryEntity
        {
            Id = entry.Id,
            BreachId = entry.BreachId,
            Action = entry.Action,
            Detail = entry.Detail,
            PerformedByUserId = entry.PerformedByUserId,
            OccurredAtUtc = entry.OccurredAtUtc
        };
    }

    /// <summary>
    /// Converts a persistence entity back to a domain <see cref="BreachAuditEntry"/>.
    /// </summary>
    /// <param name="entity">The persistence entity to convert.</param>
    /// <returns>A <see cref="BreachAuditEntry"/> converted from the entity.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entity"/> is <c>null</c>.</exception>
    public static BreachAuditEntry ToDomain(BreachAuditEntryEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new BreachAuditEntry
        {
            Id = entity.Id,
            BreachId = entity.BreachId,
            Action = entity.Action,
            Detail = entity.Detail,
            PerformedByUserId = entity.PerformedByUserId,
            OccurredAtUtc = entity.OccurredAtUtc
        };
    }
}
