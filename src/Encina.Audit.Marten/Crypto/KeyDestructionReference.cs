namespace Encina.Audit.Marten.Crypto;

/// <summary>
/// The recorded reason a temporal key destruction took place: either a data-erasure request
/// (<see cref="ErasureReference"/>) or a retention-policy expiry (<see cref="RetentionReference"/>).
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ITemporalKeyProvider.DestroyKeysBeforeAsync"/> writes the reference into the
/// <see cref="TemporalKeyDestroyedMarker"/> of every period it destroys. The audit projections read it back to decide
/// what a field of that period shows (issue #2770): <c>[SHREDDED]</c> for an erasure, <c>[EXPIRED]</c> for a retention
/// expiry, and a typed failure that pauses the shard when the destruction carries neither.
/// </para>
/// <para>
/// The type is closed: only <see cref="ErasureReference"/> and <see cref="RetentionReference"/> derive from it, so a
/// single destruction can never carry both references.
/// </para>
/// </remarks>
public abstract record KeyDestructionReference
{
    private protected KeyDestructionReference()
    {
    }

    /// <summary>
    /// Gets a value indicating whether the reference is complete enough to be recorded (non-blank identifier and
    /// actor, and its dates set).
    /// </summary>
    public abstract bool IsValid { get; }
}
