using Encina.Compliance.Retention.Model;

namespace Encina.Compliance.Retention;

/// <summary>
/// Maps between <see cref="RetentionAuditEntry"/> domain records and
/// <see cref="RetentionAuditEntryEntity"/> persistence entities.
/// </summary>
/// <remarks>
/// <para>
/// All properties map directly between the domain model and entity without
/// type transformations, since both use primitive types (strings, <see cref="DateTimeOffset"/>).
/// </para>
/// <para>
/// Used by store implementations (ADO.NET, Dapper, EF Core, MongoDB) to persist and
/// retrieve retention audit entries without coupling to the domain model.
/// </para>
/// </remarks>
public static class RetentionAuditEntryMapper
{
    /// <summary>
    /// Converts a domain <see cref="RetentionAuditEntry"/> to a persistence entity.
    /// </summary>
    /// <param name="entry">The domain audit entry to convert.</param>
    /// <returns>A <see cref="RetentionAuditEntryEntity"/> suitable for persistence.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entry"/> is <c>null</c>.</exception>
    public static RetentionAuditEntryEntity ToEntity(RetentionAuditEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        return new RetentionAuditEntryEntity
        {
            Id = entry.Id,
            Action = entry.Action,
            EntityId = entry.EntityId,
            DataCategory = entry.DataCategory,
            Detail = entry.Detail,
            PerformedByUserId = entry.PerformedByUserId,
            OccurredAtUtc = entry.OccurredAtUtc
        };
    }

    /// <summary>
    /// Converts a persistence entity back to a domain <see cref="RetentionAuditEntry"/>.
    /// </summary>
    /// <param name="entity">The persistence entity to convert.</param>
    /// <returns>A <see cref="RetentionAuditEntry"/> domain record.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entity"/> is <c>null</c>.</exception>
    public static RetentionAuditEntry ToDomain(RetentionAuditEntryEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new RetentionAuditEntry
        {
            Id = entity.Id,
            Action = entity.Action,
            EntityId = entity.EntityId,
            DataCategory = entity.DataCategory,
            Detail = entity.Detail,
            PerformedByUserId = entity.PerformedByUserId,
            OccurredAtUtc = entity.OccurredAtUtc
        };
    }
}
