namespace Encina.Sharding.Routing;

/// <summary>
/// Configuration options for <see cref="GeoShardRouter"/>.
/// </summary>
public sealed class GeoShardRouterOptions
{
    /// <summary>
    /// Gets or sets the default region code used when no region can be resolved from the shard key.
    /// </summary>
    /// <value>Null by default (no default region).</value>
    public string? DefaultRegion { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the router requires an exact region match.
    /// When true, fallback chains are not followed.
    /// </summary>
    /// <value>The default is false (fallback chains are followed).</value>
    public bool RequireExactMatch { get; set; }
}
