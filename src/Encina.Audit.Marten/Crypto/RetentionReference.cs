using System.Xml;

namespace Encina.Audit.Marten.Crypto;

/// <summary>
/// The reference to a retention-policy execution that a key destruction answers.
/// </summary>
/// <param name="PolicyId">The identifier of the retention policy that expired the data.</param>
/// <param name="CutoffUtc">The cutoff the policy applied: periods that ended before it were destroyed (UTC).</param>
/// <param name="ExecutedAtUtc">When the retention execution ran (UTC).</param>
/// <param name="Actor">Who executed the retention (a system identifier such as <c>system</c>, or a user).</param>
/// <remarks>
/// <para>
/// <see cref="ITemporalKeyProvider.DestroyKeysBeforeAsync"/> records this reference in the
/// <see cref="TemporalKeyDestroyedMarker"/> of every period it destroys. The audit projections show the expired
/// placeholder (<c>[EXPIRED]</c>) for such a period and keep projecting without pausing the shard: retention expiry
/// is an expected, lawful event (GDPR Art. 5(1)(e)) and the marker still says why the field is unreadable
/// (Art. 5(2)). <c>[SHREDDED]</c> stays reserved for a recorded erasure request (<see cref="ErasureReference"/>)
/// (issue #2770, decision Q3).
/// </para>
/// <para>
/// The audit stores' purge (<c>PurgeEntriesAsync</c>) passes one built from the executed purge.
/// </para>
/// </remarks>
public sealed record RetentionReference(string PolicyId, DateTimeOffset CutoffUtc, DateTimeOffset ExecutedAtUtc, string Actor)
    : KeyDestructionReference
{
    /// <summary>The actor recorded for a retention execution the audit stores run themselves.</summary>
    internal const string SystemActor = "system";

    /// <summary>
    /// Builds the reference of a purge the audit stores execute: the policy is identified by the configured retention
    /// period (ISO 8601 duration, for example <c>audit-retention:P2555D</c>), the actor is <c>system</c>.
    /// </summary>
    internal static RetentionReference ForPurge(TimeSpan retentionPeriod, DateTimeOffset cutoffUtc, DateTimeOffset executedAtUtc) =>
        new($"audit-retention:{XmlConvert.ToString(retentionPeriod)}", cutoffUtc, executedAtUtc, SystemActor);

    /// <summary>
    /// Gets a value indicating whether the reference identifies a retention execution: a non-blank policy id and
    /// actor, and a cutoff and execution date.
    /// </summary>
    public override bool IsValid =>
        !string.IsNullOrWhiteSpace(PolicyId)
        && !string.IsNullOrWhiteSpace(Actor)
        && CutoffUtc != default
        && ExecutedAtUtc != default;
}
