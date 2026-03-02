using Encina.Compliance.Retention.Model;

namespace Encina.Compliance.Retention;

/// <summary>
/// Maps between <see cref="RetentionPolicy"/> domain records and
/// <see cref="RetentionPolicyEntity"/> persistence entities.
/// </summary>
/// <remarks>
/// <para>
/// This mapper handles two key type transformations:
/// <list type="bullet">
/// <item>
/// <description>
/// <see cref="RetentionPolicy.RetentionPeriod"/> (<see cref="TimeSpan"/>) ↔
/// <see cref="RetentionPolicyEntity.RetentionPeriodTicks"/> (<see cref="long"/>),
/// using <see cref="TimeSpan.Ticks"/> for lossless, portable storage.
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="RetentionPolicy.PolicyType"/> (<see cref="RetentionPolicyType"/>) ↔
/// <see cref="RetentionPolicyEntity.PolicyTypeValue"/> (<see cref="int"/>),
/// using integer casting for cross-provider compatibility.
/// </description>
/// </item>
/// </list>
/// </para>
/// <para>
/// Used by store implementations (ADO.NET, Dapper, EF Core, MongoDB) to persist and
/// retrieve retention policies without coupling to the domain model.
/// </para>
/// </remarks>
public static class RetentionPolicyMapper
{
    /// <summary>
    /// Converts a domain <see cref="RetentionPolicy"/> to a persistence entity.
    /// </summary>
    /// <param name="policy">The domain policy to convert.</param>
    /// <returns>A <see cref="RetentionPolicyEntity"/> suitable for persistence.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="policy"/> is <c>null</c>.</exception>
    public static RetentionPolicyEntity ToEntity(RetentionPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        return new RetentionPolicyEntity
        {
            Id = policy.Id,
            DataCategory = policy.DataCategory,
            RetentionPeriodTicks = policy.RetentionPeriod.Ticks,
            AutoDelete = policy.AutoDelete,
            Reason = policy.Reason,
            LegalBasis = policy.LegalBasis,
            PolicyTypeValue = (int)policy.PolicyType,
            CreatedAtUtc = policy.CreatedAtUtc,
            LastModifiedAtUtc = policy.LastModifiedAtUtc
        };
    }

    /// <summary>
    /// Converts a persistence entity back to a domain <see cref="RetentionPolicy"/>.
    /// </summary>
    /// <param name="entity">The persistence entity to convert.</param>
    /// <returns>
    /// A <see cref="RetentionPolicy"/> if the entity state is valid (enum values are defined),
    /// or <c>null</c> if the entity contains invalid enum values.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entity"/> is <c>null</c>.</exception>
    public static RetentionPolicy? ToDomain(RetentionPolicyEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (!Enum.IsDefined(typeof(RetentionPolicyType), entity.PolicyTypeValue))
        {
            return null;
        }

        return new RetentionPolicy
        {
            Id = entity.Id,
            DataCategory = entity.DataCategory,
            RetentionPeriod = new TimeSpan(entity.RetentionPeriodTicks),
            AutoDelete = entity.AutoDelete,
            Reason = entity.Reason,
            LegalBasis = entity.LegalBasis,
            PolicyType = (RetentionPolicyType)entity.PolicyTypeValue,
            CreatedAtUtc = entity.CreatedAtUtc,
            LastModifiedAtUtc = entity.LastModifiedAtUtc
        };
    }
}
