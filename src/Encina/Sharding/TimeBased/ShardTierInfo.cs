using System.Globalization;
using System.Text.Json.Serialization;

namespace Encina.Sharding.TimeBased;

/// <summary>
/// Metadata describing a time-based shard's identity, tier, period boundaries, and connection details.
/// </summary>
/// <remarks>
/// <para>
/// Each <see cref="ShardTierInfo"/> represents a single shard in a time-based sharding scheme,
/// combining the shard identifier with its current storage tier, time boundaries, and
/// connection information. The <see cref="PeriodStart"/> and <see cref="PeriodEnd"/> form a
/// half-open interval <c>[PeriodStart, PeriodEnd)</c>.
/// </para>
/// <para>
/// Only shards in the <see cref="ShardTier.Hot"/> tier accept writes. Shards in other tiers
/// are read-only at the application level, and the <see cref="IsReadOnly"/> property reflects this.
/// </para>
/// <para>
/// <see cref="ConnectionString"/> is a credential. It is marked <see cref="JsonIgnoreAttribute"/>
/// and masked by <see cref="ToString"/>, so logging, string interpolation and System.Text.Json
/// serialization never write it. As a consequence, a <see cref="ShardTierInfo"/> cannot be read
/// back from its own JSON: the serialized form carries no connection string, and the constructor
/// rejects a missing one. Build tier metadata from configuration binding, code or a tier store,
/// never from System.Text.Json.
/// </para>
/// </remarks>
/// <param name="ShardId">Unique identifier for this shard (e.g., <c>"orders-2026-02"</c>).</param>
/// <param name="CurrentTier">The current storage tier of this shard.</param>
/// <param name="PeriodStart">Inclusive start date of the time period covered by this shard.</param>
/// <param name="PeriodEnd">Exclusive end date of the time period covered by this shard.</param>
/// <param name="IsReadOnly">
/// Whether write operations are blocked for this shard. Automatically <see langword="true"/> for
/// non-<see cref="ShardTier.Hot"/> tiers.
/// </param>
/// <param name="ConnectionString">
/// Database connection string for this shard.
/// WARNING: contains sensitive credential data; it is excluded from JSON serialization and
/// from <see cref="ToString"/>.
/// </param>
/// <param name="CreatedAtUtc">UTC timestamp when this shard metadata was first created.</param>
/// <param name="LastTransitionAtUtc">
/// UTC timestamp of the most recent tier transition. <see langword="null"/> if no transitions have occurred.
/// </param>
public sealed record ShardTierInfo(
    string ShardId,
    ShardTier CurrentTier,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    bool IsReadOnly,
    string ConnectionString,
    DateTime CreatedAtUtc,
    DateTime? LastTransitionAtUtc = null)
{
    /// <summary>
    /// Gets the unique shard identifier.
    /// </summary>
    public string ShardId { get; } = !string.IsNullOrWhiteSpace(ShardId)
        ? ShardId
        : throw new ArgumentException("Shard ID cannot be null or whitespace.", nameof(ShardId));

    /// <summary>
    /// Gets the database connection string for this shard.
    /// </summary>
    /// <remarks>Excluded from JSON serialization and masked by <see cref="ToString"/>.</remarks>
    [JsonIgnore]
    public string ConnectionString { get; } = !string.IsNullOrWhiteSpace(ConnectionString)
        ? ConnectionString
        : throw new ArgumentException("Connection string cannot be null or whitespace.", nameof(ConnectionString));

    /// <summary>
    /// Gets the inclusive period start date.
    /// </summary>
    public DateOnly PeriodStart { get; } = PeriodStart < PeriodEnd
        ? PeriodStart
        : throw new ArgumentException("PeriodStart must be earlier than PeriodEnd.", nameof(PeriodStart));

    /// <summary>
    /// Returns a description of the shard tier metadata that never contains the connection string.
    /// </summary>
    /// <returns>
    /// The shard identifier, tier, period boundaries (<c>yyyy-MM-dd</c>), read-only flag and
    /// timestamps (ISO 8601, invariant culture), with <c>ConnectionString = ***</c>.
    /// </returns>
    public override string ToString() =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"ShardTierInfo {{ ShardId = {ShardId}, CurrentTier = {CurrentTier}, PeriodStart = {PeriodStart:yyyy-MM-dd}, PeriodEnd = {PeriodEnd:yyyy-MM-dd}, IsReadOnly = {IsReadOnly}, ConnectionString = ***, CreatedAtUtc = {CreatedAtUtc:O}, LastTransitionAtUtc = {LastTransitionAtUtc:O} }}");
}
