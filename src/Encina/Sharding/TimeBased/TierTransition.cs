namespace Encina.Sharding.TimeBased;

/// <summary>
/// Defines an age-based rule for transitioning a shard from one tier to another.
/// </summary>
/// <remarks>
/// <para>
/// Tier transitions are evaluated by the transition scheduler. When a shard's age
/// (time elapsed since its <see cref="ShardTierInfo.PeriodEnd"/>) exceeds the
/// <see cref="AgeThreshold"/>, the shard is eligible for promotion from
/// <see cref="FromTier"/> to <see cref="ToTier"/>.
/// </para>
/// <para>
/// Transitions must follow the natural tier ordering:
/// <see cref="ShardTier.Hot"/> to <see cref="ShardTier.Warm"/> to
/// <see cref="ShardTier.Cold"/> to <see cref="ShardTier.Archived"/>.
/// </para>
/// </remarks>
/// <param name="FromTier">The source tier that a shard must currently be in.</param>
/// <param name="ToTier">The target tier to transition the shard to.</param>
/// <param name="AgeThreshold">
/// The minimum age (from the shard's period end) before the transition is eligible.
/// </param>
/// <example>
/// <code>
/// // Move shards from Hot to Warm after 30 days, Warm to Cold after 90 days
/// var transitions = new[]
/// {
///     new TierTransition(ShardTier.Hot, ShardTier.Warm, TimeSpan.FromDays(30)),
///     new TierTransition(ShardTier.Warm, ShardTier.Cold, TimeSpan.FromDays(90)),
///     new TierTransition(ShardTier.Cold, ShardTier.Archived, TimeSpan.FromDays(365)),
/// };
/// </code>
/// </example>
public sealed record TierTransition(ShardTier FromTier, ShardTier ToTier, TimeSpan AgeThreshold)
{
    /// <summary>
    /// Gets the minimum age threshold.
    /// </summary>
    public TimeSpan AgeThreshold { get; } = AgeThreshold > TimeSpan.Zero
        ? AgeThreshold
        : throw new ArgumentOutOfRangeException(nameof(AgeThreshold), AgeThreshold, "Age threshold must be positive.");

    /// <summary>
    /// Gets the source tier.
    /// </summary>
    public ShardTier FromTier { get; } = ValidateTierProgression(FromTier, ToTier)
        ? FromTier
        : throw new ArgumentException(
            $"Tier transition must progress forward: {FromTier} -> {ToTier} is not valid.",
            nameof(FromTier));

    private static bool ValidateTierProgression(ShardTier from, ShardTier to)
        => (int)to > (int)from;
}
