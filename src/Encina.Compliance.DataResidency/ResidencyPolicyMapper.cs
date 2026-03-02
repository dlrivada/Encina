using System.Globalization;

using Encina.Compliance.DataResidency.Model;

namespace Encina.Compliance.DataResidency;

/// <summary>
/// Maps between <see cref="ResidencyPolicyDescriptor"/> domain records and
/// <see cref="ResidencyPolicyEntity"/> persistence entities.
/// </summary>
/// <remarks>
/// <para>
/// This mapper handles two key type transformations:
/// <list type="bullet">
/// <item>
/// <description>
/// <see cref="ResidencyPolicyDescriptor.AllowedRegions"/>
/// (<see cref="IReadOnlyList{T}"/> of <see cref="Region"/>) ↔
/// <see cref="ResidencyPolicyEntity.AllowedRegionCodes"/> (<see cref="string"/>),
/// using comma-separated ISO region codes.
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="ResidencyPolicyDescriptor.AllowedTransferBases"/>
/// (<see cref="IReadOnlyList{T}"/> of <see cref="TransferLegalBasis"/>) ↔
/// <see cref="ResidencyPolicyEntity.AllowedTransferBasesValue"/> (<see cref="string"/>),
/// using comma-separated integer enum values.
/// </description>
/// </item>
/// </list>
/// </para>
/// <para>
/// The entity also includes <see cref="ResidencyPolicyEntity.CreatedAtUtc"/> and
/// <see cref="ResidencyPolicyEntity.LastModifiedAtUtc"/> persistence-only timestamps.
/// When converting to an entity, <see cref="ResidencyPolicyEntity.CreatedAtUtc"/> defaults
/// to <see cref="DateTimeOffset.UtcNow"/> and <see cref="ResidencyPolicyEntity.LastModifiedAtUtc"/>
/// is <c>null</c>. Callers should set these explicitly when updating existing entities.
/// </para>
/// <para>
/// Used by store implementations (ADO.NET, Dapper, EF Core, MongoDB) to persist and
/// retrieve residency policies without coupling to the domain model.
/// </para>
/// </remarks>
public static class ResidencyPolicyMapper
{
    /// <summary>
    /// Converts a domain <see cref="ResidencyPolicyDescriptor"/> to a persistence entity.
    /// </summary>
    /// <param name="policy">The domain policy to convert.</param>
    /// <returns>A <see cref="ResidencyPolicyEntity"/> suitable for persistence.</returns>
    /// <remarks>
    /// <see cref="ResidencyPolicyEntity.CreatedAtUtc"/> is set to <see cref="DateTimeOffset.UtcNow"/>.
    /// <see cref="ResidencyPolicyEntity.LastModifiedAtUtc"/> is set to <c>null</c>.
    /// Store implementations should override these values when updating existing policies.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="policy"/> is <c>null</c>.</exception>
    public static ResidencyPolicyEntity ToEntity(ResidencyPolicyDescriptor policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        return new ResidencyPolicyEntity
        {
            DataCategory = policy.DataCategory,
            AllowedRegionCodes = string.Join(",", policy.AllowedRegions.Select(r => r.Code)),
            RequireAdequacyDecision = policy.RequireAdequacyDecision,
            AllowedTransferBasesValue = policy.AllowedTransferBases.Count > 0
                ? string.Join(",", policy.AllowedTransferBases.Select(b => ((int)b).ToString(CultureInfo.InvariantCulture)))
                : null,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            LastModifiedAtUtc = null
        };
    }

    /// <summary>
    /// Converts a persistence entity back to a domain <see cref="ResidencyPolicyDescriptor"/>.
    /// </summary>
    /// <param name="entity">The persistence entity to convert.</param>
    /// <returns>
    /// A <see cref="ResidencyPolicyDescriptor"/> if the entity state is valid
    /// (all region codes resolve and all transfer bases are defined enum values),
    /// or <c>null</c> if the entity contains invalid values.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entity"/> is <c>null</c>.</exception>
    public static ResidencyPolicyDescriptor? ToDomain(ResidencyPolicyEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        // Parse allowed region codes
        var regionCodes = entity.AllowedRegionCodes
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var regions = new List<Region>(regionCodes.Length);
        foreach (var code in regionCodes)
        {
            var region = RegionRegistry.GetByCode(code);
            if (region is null)
            {
                return null;
            }

            regions.Add(region);
        }

        // Parse allowed transfer bases
        var transferBases = new List<TransferLegalBasis>();
        if (!string.IsNullOrEmpty(entity.AllowedTransferBasesValue))
        {
            var baseParts = entity.AllowedTransferBasesValue
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var part in baseParts)
            {
                if (!int.TryParse(part, out var basisValue) ||
                    !Enum.IsDefined(typeof(TransferLegalBasis), basisValue))
                {
                    return null;
                }

                transferBases.Add((TransferLegalBasis)basisValue);
            }
        }

        return new ResidencyPolicyDescriptor
        {
            DataCategory = entity.DataCategory,
            AllowedRegions = regions,
            RequireAdequacyDecision = entity.RequireAdequacyDecision,
            AllowedTransferBases = transferBases
        };
    }
}
