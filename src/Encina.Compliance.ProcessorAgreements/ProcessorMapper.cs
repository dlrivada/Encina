using Encina.Compliance.ProcessorAgreements.Model;

namespace Encina.Compliance.ProcessorAgreements;

/// <summary>
/// Maps between <see cref="Processor"/> domain records and
/// <see cref="ProcessorEntity"/> persistence entities.
/// </summary>
/// <remarks>
/// <para>
/// This mapper handles the following type transformations:
/// <list type="bullet">
/// <item>
/// <description>
/// <see cref="Processor.SubProcessorAuthorizationType"/> (<see cref="SubProcessorAuthorizationType"/>) ↔
/// <see cref="ProcessorEntity.SubProcessorAuthorizationTypeValue"/> (<see cref="int"/>).
/// </description>
/// </item>
/// </list>
/// </para>
/// <para>
/// Invalid enum values in a persistence entity result in a <c>null</c> return from
/// <see cref="ToDomain"/>.
/// </para>
/// <para>
/// Used by store implementations (ADO.NET, Dapper, EF Core, MongoDB) to persist and
/// retrieve processors without coupling to the domain model.
/// </para>
/// </remarks>
public static class ProcessorMapper
{
    /// <summary>
    /// Converts a domain <see cref="Processor"/> to a persistence entity.
    /// </summary>
    /// <param name="processor">The domain record to convert.</param>
    /// <returns>A <see cref="ProcessorEntity"/> suitable for persistence.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="processor"/> is <c>null</c>.</exception>
    public static ProcessorEntity ToEntity(Processor processor)
    {
        ArgumentNullException.ThrowIfNull(processor);

        return new ProcessorEntity
        {
            Id = processor.Id,
            Name = processor.Name,
            Country = processor.Country,
            ContactEmail = processor.ContactEmail,
            ParentProcessorId = processor.ParentProcessorId,
            Depth = processor.Depth,
            SubProcessorAuthorizationTypeValue = (int)processor.SubProcessorAuthorizationType,
            TenantId = processor.TenantId,
            ModuleId = processor.ModuleId,
            CreatedAtUtc = processor.CreatedAtUtc,
            LastUpdatedAtUtc = processor.LastUpdatedAtUtc
        };
    }

    /// <summary>
    /// Converts a persistence entity back to a domain <see cref="Processor"/>.
    /// </summary>
    /// <param name="entity">The persistence entity to convert.</param>
    /// <returns>
    /// A <see cref="Processor"/> if the entity state is valid (all enum values are defined),
    /// or <c>null</c> if the entity contains invalid values.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entity"/> is <c>null</c>.</exception>
    public static Processor? ToDomain(ProcessorEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (!Enum.IsDefined(typeof(SubProcessorAuthorizationType), entity.SubProcessorAuthorizationTypeValue))
        {
            return null;
        }

        return new Processor
        {
            Id = entity.Id,
            Name = entity.Name,
            Country = entity.Country,
            ContactEmail = entity.ContactEmail,
            ParentProcessorId = entity.ParentProcessorId,
            Depth = entity.Depth,
            SubProcessorAuthorizationType = (SubProcessorAuthorizationType)entity.SubProcessorAuthorizationTypeValue,
            TenantId = entity.TenantId,
            ModuleId = entity.ModuleId,
            CreatedAtUtc = entity.CreatedAtUtc,
            LastUpdatedAtUtc = entity.LastUpdatedAtUtc
        };
    }
}
