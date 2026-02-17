namespace Encina.Sharding.Routing;

/// <summary>
/// Maps a geographic region to a shard with an optional fallback region.
/// </summary>
/// <param name="RegionCode">The region code (e.g., "us-east", "eu-west").</param>
/// <param name="ShardId">The shard that serves this region.</param>
/// <param name="FallbackRegionCode">Optional fallback region code if this region's shard is unavailable.</param>
/// <remarks>
/// <para>
/// Fallback chains are resolved by <see cref="GeoShardRouter"/> in order: if the primary region's shard
/// is not found in the topology, the router follows the <paramref name="FallbackRegionCode"/> link
/// to the next region. Circular fallback chains are detected and produce error code
/// <c>encina.sharding.region_not_found</c>.
/// </para>
/// <para>
/// Region codes are case-sensitive and should follow a consistent naming convention
/// (e.g., ISO 3166-1 codes or cloud provider region identifiers like <c>us-east-1</c>).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Multi-region setup with fallback for data residency compliance
/// var regions = new[]
/// {
///     new GeoRegion("us-east", "shard-us", FallbackRegionCode: "us-west"),
///     new GeoRegion("us-west", "shard-us-west"),
///     new GeoRegion("eu-west", "shard-eu", FallbackRegionCode: "eu-central"),
///     new GeoRegion("eu-central", "shard-eu-central"),
///     new GeoRegion("ap-southeast", "shard-ap", FallbackRegionCode: "us-west")
/// };
/// </code>
/// </example>
public sealed record GeoRegion(
    string RegionCode,
    string ShardId,
    string? FallbackRegionCode = null)
{
    /// <summary>
    /// Gets the region code.
    /// </summary>
    public string RegionCode { get; } = !string.IsNullOrWhiteSpace(RegionCode)
        ? RegionCode
        : throw new ArgumentException("Region code cannot be null or whitespace.", nameof(RegionCode));

    /// <summary>
    /// Gets the shard that serves this region.
    /// </summary>
    public string ShardId { get; } = !string.IsNullOrWhiteSpace(ShardId)
        ? ShardId
        : throw new ArgumentException("Shard ID cannot be null or whitespace.", nameof(ShardId));
}
