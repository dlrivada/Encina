namespace Encina.Marten.GDPR;

/// <summary>
/// Contains the outcome of a subject encryption key rotation operation.
/// </summary>
/// <remarks>
/// <para>
/// Key rotation creates a new version of the subject's encryption key. The old key version
/// transitions to <see cref="SubjectKeyStatus.Rotated"/> and remains available for decrypting
/// events encrypted with that version. New events are encrypted with the latest key version
/// (forward-only encryption).
/// </para>
/// <para>
/// Key rotation is recommended as a periodic security measure and may be required by
/// organizational security policies. It does NOT re-encrypt existing events — old events
/// continue to use the key version they were originally encrypted with.
/// </para>
/// </remarks>
public sealed record KeyRotationResult
{
    /// <summary>
    /// Identifier of the data subject whose key was rotated.
    /// </summary>
    public required string SubjectId { get; init; }

    /// <summary>
    /// Key identifier of the previous (now rotated) key version.
    /// </summary>
    /// <example>subject:user-42:v1</example>
    public required string OldKeyId { get; init; }

    /// <summary>
    /// Key identifier of the new active key version.
    /// </summary>
    /// <example>subject:user-42:v2</example>
    public required string NewKeyId { get; init; }

    /// <summary>
    /// Version number of the previous (now rotated) key.
    /// </summary>
    public required int OldVersion { get; init; }

    /// <summary>
    /// Version number of the new active key.
    /// </summary>
    public required int NewVersion { get; init; }

    /// <summary>
    /// Timestamp when the key rotation was completed (UTC).
    /// </summary>
    public required DateTimeOffset RotatedAtUtc { get; init; }
}
