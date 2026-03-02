namespace Encina.Compliance.DataResidency.Model;

/// <summary>
/// Represents a geographic region for data residency and sovereignty enforcement.
/// </summary>
/// <remarks>
/// <para>
/// A region encapsulates the geographic, legal, and regulatory characteristics of a
/// data processing location. It captures whether the region is within the EU/EEA,
/// whether it has an adequacy decision from the European Commission, and its overall
/// data protection level.
/// </para>
/// <para>
/// Regions are used throughout the data residency pipeline to determine whether data
/// can be processed in a given location, whether cross-border transfers are allowed,
/// and what legal basis is required for international transfers under GDPR Chapter V.
/// </para>
/// <para>
/// Well-known regions are available as static properties on <see cref="RegionRegistry"/>.
/// Custom regions can be created via the <see cref="Create"/> factory method for
/// private cloud zones or internal data centers.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Use well-known regions
/// var region = RegionRegistry.EU;
///
/// // Create a custom region
/// var custom = Region.Create(
///     code: "AZURE-WESTEU",
///     country: "NL",
///     isEU: true,
///     isEEA: true,
///     hasAdequacyDecision: true,
///     protectionLevel: DataProtectionLevel.High);
/// </code>
/// </example>
public sealed record Region : IEquatable<Region>
{
    /// <summary>
    /// Region code identifier (ISO 3166-1 alpha-2, regional code, or custom identifier).
    /// </summary>
    /// <remarks>
    /// Examples: "DE" (Germany), "US" (United States), "EU" (European Union),
    /// "AZURE-WESTEU" (custom cloud region). Comparison is case-insensitive.
    /// </remarks>
    public required string Code { get; init; }

    /// <summary>
    /// ISO 3166-1 alpha-2 country code for the primary country of this region.
    /// </summary>
    /// <remarks>
    /// For multi-country regions (e.g., "EU"), this represents the primary or
    /// representative country. For custom regions, use the country where the
    /// data center is physically located.
    /// </remarks>
    public required string Country { get; init; }

    /// <summary>
    /// Whether this region is within the European Union.
    /// </summary>
    /// <remarks>
    /// EU member states benefit from free data movement within the Union
    /// under GDPR Article 1(3). Currently includes 27 member states.
    /// </remarks>
    public required bool IsEU { get; init; }

    /// <summary>
    /// Whether this region is within the European Economic Area.
    /// </summary>
    /// <remarks>
    /// The EEA includes all 27 EU member states plus Iceland, Liechtenstein,
    /// and Norway. GDPR applies throughout the EEA, and data flows freely
    /// between all EEA countries.
    /// </remarks>
    public required bool IsEEA { get; init; }

    /// <summary>
    /// Whether the European Commission has issued an adequacy decision for this region.
    /// </summary>
    /// <remarks>
    /// Per GDPR Article 45, data can be transferred to countries with an adequacy
    /// decision without any specific authorization. As of 2025, adequacy decisions
    /// cover: Andorra, Argentina, Canada (commercial), Faroe Islands, Guernsey,
    /// Israel, Isle of Man, Japan, Jersey, New Zealand, Republic of Korea,
    /// Switzerland, United Kingdom, Uruguay, and the United States (Data Privacy Framework).
    /// </remarks>
    public required bool HasAdequacyDecision { get; init; }

    /// <summary>
    /// The overall data protection level of this region.
    /// </summary>
    public required DataProtectionLevel ProtectionLevel { get; init; }

    /// <summary>
    /// Creates a new <see cref="Region"/> with the specified characteristics.
    /// </summary>
    /// <param name="code">Region code identifier.</param>
    /// <param name="country">ISO 3166-1 alpha-2 country code.</param>
    /// <param name="isEU">Whether the region is within the EU.</param>
    /// <param name="isEEA">Whether the region is within the EEA.</param>
    /// <param name="hasAdequacyDecision">Whether the region has an EU adequacy decision.</param>
    /// <param name="protectionLevel">The data protection level of the region.</param>
    /// <returns>A new <see cref="Region"/> instance.</returns>
    public static Region Create(
        string code,
        string country,
        bool isEU = false,
        bool isEEA = false,
        bool hasAdequacyDecision = false,
        DataProtectionLevel protectionLevel = DataProtectionLevel.Unknown) =>
        new()
        {
            Code = code,
            Country = country,
            IsEU = isEU,
            IsEEA = isEEA,
            HasAdequacyDecision = hasAdequacyDecision || isEU || isEEA,
            ProtectionLevel = protectionLevel
        };

    /// <summary>
    /// Determines whether this region and the specified region are equal
    /// based on case-insensitive <see cref="Code"/> comparison.
    /// </summary>
    /// <param name="other">The region to compare with.</param>
    /// <returns><c>true</c> if the regions have the same code (case-insensitive); otherwise, <c>false</c>.</returns>
    public bool Equals(Region? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return string.Equals(Code, other.Code, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Returns a hash code based on the case-insensitive <see cref="Code"/>.
    /// </summary>
    /// <returns>A hash code for the current region.</returns>
    public override int GetHashCode() =>
        StringComparer.OrdinalIgnoreCase.GetHashCode(Code);

    /// <summary>
    /// Returns the region code as the string representation.
    /// </summary>
    /// <returns>The <see cref="Code"/> value.</returns>
    public override string ToString() => Code;
}
