namespace Encina.Compliance.DataSubjectRights;

/// <summary>
/// Maps between <see cref="DSRAuditEntry"/> domain records and
/// <see cref="DSRAuditEntryEntity"/> persistence entities.
/// </summary>
/// <remarks>
/// <para>
/// This mapper converts between the immutable domain audit entry records and the
/// mutable persistence entities used by store implementations.
/// </para>
/// <para>
/// Used by store implementations (ADO.NET, Dapper, EF Core, MongoDB) to persist and
/// retrieve DSR audit trail entries without coupling to the domain model.
/// </para>
/// </remarks>
public static class DSRAuditEntryMapper
{
    /// <summary>
    /// Converts a domain <see cref="DSRAuditEntry"/> to a persistence entity.
    /// </summary>
    /// <param name="entry">The domain audit entry to convert.</param>
    /// <returns>A <see cref="DSRAuditEntryEntity"/> suitable for persistence.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entry"/> is <c>null</c>.</exception>
    /// <remarks>
    /// A new GUID is generated for the entity's <see cref="DSRAuditEntryEntity.Id"/> to ensure
    /// unique persistence-layer identifiers.
    /// </remarks>
    public static DSRAuditEntryEntity ToEntity(DSRAuditEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        return new DSRAuditEntryEntity
        {
            Id = Guid.NewGuid().ToString("D"),
            DSRRequestId = entry.DSRRequestId,
            Action = entry.Action,
            Detail = entry.Detail,
            PerformedByUserId = entry.PerformedByUserId,
            OccurredAtUtc = entry.OccurredAtUtc
        };
    }

    /// <summary>
    /// Converts a persistence entity back to a domain <see cref="DSRAuditEntry"/>.
    /// </summary>
    /// <param name="entity">The persistence entity to convert.</param>
    /// <returns>A <see cref="DSRAuditEntry"/> reconstructed from the persistence entity.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entity"/> is <c>null</c>.</exception>
    public static DSRAuditEntry ToDomain(DSRAuditEntryEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new DSRAuditEntry
        {
            Id = entity.Id,
            DSRRequestId = entity.DSRRequestId,
            Action = entity.Action,
            Detail = entity.Detail,
            PerformedByUserId = entity.PerformedByUserId,
            OccurredAtUtc = entity.OccurredAtUtc
        };
    }
}
