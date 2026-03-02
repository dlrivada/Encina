namespace Encina.Compliance.DataResidency;

/// <summary>
/// Persistence entity for <see cref="Model.ResidencyPolicyDescriptor"/>.
/// </summary>
/// <remarks>
/// <para>
/// This entity provides a database-agnostic representation of a residency policy,
/// using primitive types suitable for any storage provider (ADO.NET, Dapper, EF Core, MongoDB).
/// </para>
/// <para>
/// Key type transformations:
/// <list type="bullet">
/// <item>
/// <description>
/// <see cref="Model.ResidencyPolicyDescriptor.AllowedRegions"/>
/// (<see cref="System.Collections.Generic.IReadOnlyList{T}"/> of <see cref="Model.Region"/>)
/// is stored as <see cref="AllowedRegionCodes"/> — a comma-separated string of ISO region codes
/// (e.g., "DE,FR,ES,IT").
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="Model.ResidencyPolicyDescriptor.AllowedTransferBases"/>
/// (<see cref="System.Collections.Generic.IReadOnlyList{T}"/> of <see cref="Model.TransferLegalBasis"/>)
/// is stored as <see cref="AllowedTransferBasesValue"/> — a comma-separated string of integer
/// enum values (e.g., "0,1,2").
/// </description>
/// </item>
/// </list>
/// </para>
/// <para>
/// The entity also includes <see cref="CreatedAtUtc"/> and <see cref="LastModifiedAtUtc"/>
/// persistence-only timestamps that are not part of the domain model.
/// </para>
/// <para>
/// Use <see cref="ResidencyPolicyMapper"/> to convert between this entity and
/// <see cref="Model.ResidencyPolicyDescriptor"/>.
/// </para>
/// </remarks>
public sealed class ResidencyPolicyEntity
{
    /// <summary>
    /// The data category this policy applies to. Acts as the primary key.
    /// </summary>
    /// <remarks>
    /// A UNIQUE constraint or PRIMARY KEY should be created on this column.
    /// </remarks>
    public required string DataCategory { get; set; }

    /// <summary>
    /// Comma-separated ISO region codes for allowed regions.
    /// </summary>
    /// <remarks>
    /// Example: "DE,FR,ES,IT,NL". Each code is resolved to a <see cref="Model.Region"/>
    /// via <see cref="Model.RegionRegistry.GetByCode"/> during domain mapping.
    /// </remarks>
    public required string AllowedRegionCodes { get; set; }

    /// <summary>
    /// Whether an EU adequacy decision is required for cross-border transfers.
    /// </summary>
    public bool RequireAdequacyDecision { get; set; }

    /// <summary>
    /// Comma-separated integer values of <see cref="Model.TransferLegalBasis"/> enum entries.
    /// </summary>
    /// <remarks>
    /// Example: "0,1" for AdequacyDecision and StandardContractualClauses.
    /// <c>null</c> or empty when all transfer bases are allowed.
    /// </remarks>
    public string? AllowedTransferBasesValue { get; set; }

    /// <summary>
    /// Timestamp when this policy was created (UTC).
    /// </summary>
    /// <remarks>
    /// Persistence-only field — not present in the domain model.
    /// </remarks>
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>
    /// Timestamp when this policy was last modified (UTC), if applicable.
    /// </summary>
    /// <remarks>
    /// Persistence-only field — not present in the domain model.
    /// <c>null</c> if the policy has never been modified after creation.
    /// </remarks>
    public DateTimeOffset? LastModifiedAtUtc { get; set; }
}
