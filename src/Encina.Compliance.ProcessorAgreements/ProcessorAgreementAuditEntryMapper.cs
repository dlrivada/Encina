using Encina.Compliance.ProcessorAgreements.Model;

namespace Encina.Compliance.ProcessorAgreements;

/// <summary>
/// Maps between <see cref="ProcessorAgreementAuditEntry"/> domain records and
/// <see cref="ProcessorAgreementAuditEntryEntity"/> persistence entities.
/// </summary>
/// <remarks>
/// <para>
/// Unlike the DPIA audit entry mapper, no type transformations are required because
/// all identifiers in processor agreements are already strings in both the domain
/// and persistence models.
/// </para>
/// <para>
/// Used by store implementations (ADO.NET, Dapper, EF Core, MongoDB) to persist and
/// retrieve processor agreement audit entries without coupling to the domain model.
/// </para>
/// </remarks>
public static class ProcessorAgreementAuditEntryMapper
{
    /// <summary>
    /// Converts a domain <see cref="ProcessorAgreementAuditEntry"/> to a persistence entity.
    /// </summary>
    /// <param name="entry">The domain record to convert.</param>
    /// <returns>A <see cref="ProcessorAgreementAuditEntryEntity"/> suitable for persistence.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entry"/> is <c>null</c>.</exception>
    public static ProcessorAgreementAuditEntryEntity ToEntity(ProcessorAgreementAuditEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        return new ProcessorAgreementAuditEntryEntity
        {
            Id = entry.Id,
            ProcessorId = entry.ProcessorId,
            DPAId = entry.DPAId,
            Action = entry.Action,
            Detail = entry.Detail,
            PerformedByUserId = entry.PerformedByUserId,
            OccurredAtUtc = entry.OccurredAtUtc,
            TenantId = entry.TenantId,
            ModuleId = entry.ModuleId
        };
    }

    /// <summary>
    /// Converts a persistence entity back to a domain <see cref="ProcessorAgreementAuditEntry"/>.
    /// </summary>
    /// <param name="entity">The persistence entity to convert.</param>
    /// <returns>A <see cref="ProcessorAgreementAuditEntry"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entity"/> is <c>null</c>.</exception>
    /// <remarks>
    /// Since all fields are strings (no GUID or enum conversions), this method always returns
    /// a valid domain record and never returns <c>null</c>.
    /// </remarks>
    public static ProcessorAgreementAuditEntry ToDomain(ProcessorAgreementAuditEntryEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new ProcessorAgreementAuditEntry
        {
            Id = entity.Id,
            ProcessorId = entity.ProcessorId,
            DPAId = entity.DPAId,
            Action = entity.Action,
            Detail = entity.Detail,
            PerformedByUserId = entity.PerformedByUserId,
            OccurredAtUtc = entity.OccurredAtUtc,
            TenantId = entity.TenantId,
            ModuleId = entity.ModuleId
        };
    }
}
