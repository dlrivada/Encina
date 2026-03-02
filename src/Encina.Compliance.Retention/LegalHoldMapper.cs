using Encina.Compliance.Retention.Model;

namespace Encina.Compliance.Retention;

/// <summary>
/// Maps between <see cref="LegalHold"/> domain records and
/// <see cref="LegalHoldEntity"/> persistence entities.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="LegalHold.IsActive"/> computed property is not stored in the entity;
/// it is automatically derived in the domain model from <see cref="LegalHold.ReleasedAtUtc"/>
/// being <c>null</c>.
/// </para>
/// <para>
/// All other properties map directly between the domain model and entity without
/// type transformations, since both use primitive types (strings, <see cref="DateTimeOffset"/>).
/// </para>
/// <para>
/// Used by store implementations (ADO.NET, Dapper, EF Core, MongoDB) to persist and
/// retrieve legal holds without coupling to the domain model.
/// </para>
/// </remarks>
public static class LegalHoldMapper
{
    /// <summary>
    /// Converts a domain <see cref="LegalHold"/> to a persistence entity.
    /// </summary>
    /// <param name="hold">The domain legal hold to convert.</param>
    /// <returns>A <see cref="LegalHoldEntity"/> suitable for persistence.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="hold"/> is <c>null</c>.</exception>
    public static LegalHoldEntity ToEntity(LegalHold hold)
    {
        ArgumentNullException.ThrowIfNull(hold);

        return new LegalHoldEntity
        {
            Id = hold.Id,
            EntityId = hold.EntityId,
            Reason = hold.Reason,
            AppliedByUserId = hold.AppliedByUserId,
            AppliedAtUtc = hold.AppliedAtUtc,
            ReleasedAtUtc = hold.ReleasedAtUtc,
            ReleasedByUserId = hold.ReleasedByUserId
        };
    }

    /// <summary>
    /// Converts a persistence entity back to a domain <see cref="LegalHold"/>.
    /// </summary>
    /// <param name="entity">The persistence entity to convert.</param>
    /// <returns>A <see cref="LegalHold"/> domain record.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entity"/> is <c>null</c>.</exception>
    public static LegalHold ToDomain(LegalHoldEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new LegalHold
        {
            Id = entity.Id,
            EntityId = entity.EntityId,
            Reason = entity.Reason,
            AppliedByUserId = entity.AppliedByUserId,
            AppliedAtUtc = entity.AppliedAtUtc,
            ReleasedAtUtc = entity.ReleasedAtUtc,
            ReleasedByUserId = entity.ReleasedByUserId
        };
    }
}
