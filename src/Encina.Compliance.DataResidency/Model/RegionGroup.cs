namespace Encina.Compliance.DataResidency.Model;

/// <summary>
/// Represents a named group of <see cref="Region"/> instances for data residency policies.
/// </summary>
/// <remarks>
/// <para>
/// Region groups simplify policy configuration by allowing policies to reference
/// a collection of regions by name (e.g., "EU", "EEA", "Adequate") instead of
/// listing individual countries. This is particularly useful for policies that
/// apply to all EU member states or all countries with adequacy decisions.
/// </para>
/// <para>
/// Pre-built groups are available as static properties: <see cref="EUGroup"/>,
/// <see cref="EEAGroup"/>, and <see cref="AdequateGroup"/>. Custom groups can
/// be created via the constructor for organization-specific region groupings
/// (e.g., "APAC offices", "Approved cloud regions").
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Use pre-built groups
/// var isInEU = RegionGroup.EUGroup.Contains(RegionRegistry.DE); // true
/// var isInEU2 = RegionGroup.EUGroup.Contains(RegionRegistry.US); // false
///
/// // Create a custom group
/// var apacOffices = new RegionGroup
/// {
///     Name = "APAC Offices",
///     Regions = new HashSet&lt;Region&gt; { RegionRegistry.JP, RegionRegistry.KR, RegionRegistry.SG, RegionRegistry.AU }
/// };
/// </code>
/// </example>
public sealed record RegionGroup
{
    /// <summary>
    /// Display name for this region group.
    /// </summary>
    /// <remarks>
    /// Examples: "European Union", "European Economic Area", "Adequate Countries",
    /// "APAC Offices", "Approved Cloud Regions".
    /// </remarks>
    public required string Name { get; init; }

    /// <summary>
    /// The set of regions belonging to this group.
    /// </summary>
    /// <remarks>
    /// Uses <see cref="IReadOnlySet{T}"/> for efficient <see cref="Contains"/> lookups.
    /// Region equality is based on case-insensitive <see cref="Region.Code"/> comparison.
    /// </remarks>
    public required IReadOnlySet<Region> Regions { get; init; }

    /// <summary>
    /// Determines whether the specified region is a member of this group.
    /// </summary>
    /// <param name="region">The region to check.</param>
    /// <returns><c>true</c> if the region is in this group; otherwise, <c>false</c>.</returns>
    public bool Contains(Region region) => Regions.Contains(region);

    /// <summary>
    /// Pre-built group containing all 27 EU member states.
    /// </summary>
    /// <remarks>
    /// Data flows freely between all regions in this group under GDPR Article 1(3).
    /// </remarks>
    public static RegionGroup EUGroup { get; } = new()
    {
        Name = "European Union",
        Regions = new HashSet<Region>(RegionRegistry.EUMemberStates)
    };

    /// <summary>
    /// Pre-built group containing all 30 EEA countries (27 EU + IS, LI, NO).
    /// </summary>
    /// <remarks>
    /// GDPR applies throughout the EEA. Data flows freely between all EEA countries.
    /// </remarks>
    public static RegionGroup EEAGroup { get; } = new()
    {
        Name = "European Economic Area",
        Regions = new HashSet<Region>(RegionRegistry.EEACountries)
    };

    /// <summary>
    /// Pre-built group containing all countries with an EU adequacy decision (non-EEA).
    /// </summary>
    /// <remarks>
    /// Per GDPR Article 45, data can be transferred to these countries without
    /// additional authorization. For a combined set of all freely-transferable
    /// destinations, combine this group with <see cref="EEAGroup"/>.
    /// </remarks>
    public static RegionGroup AdequateGroup { get; } = new()
    {
        Name = "Adequate Countries",
        Regions = new HashSet<Region>(RegionRegistry.AdequacyCountries)
    };
}
