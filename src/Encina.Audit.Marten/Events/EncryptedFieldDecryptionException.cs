using System.Security.Cryptography;

namespace Encina.Audit.Marten.Events;

/// <summary>
/// Why a stored <see cref="EncryptedField"/> could not be read.
/// </summary>
/// <remarks>
/// The reason is for the operator (a property of the exception, never part of its message), so a caller
/// that only sees the message cannot tell the causes apart.
/// </remarks>
public enum EncryptedFieldDecryptionFailureReason
{
    /// <summary>The value starts with the encryption marker but is not a readable envelope (invalid JSON, a missing or wrongly typed <c>ct</c>/<c>n</c>/<c>t</c>, or invalid Base64).</summary>
    EnvelopeMalformed = 0,

    /// <summary>The value does not carry the <c>__enc</c> marker although every stored value is expected to be encrypted.</summary>
    NotEncrypted = 1,

    /// <summary>The key material, the nonce or the tag has the wrong size, so the envelope cannot be authenticated.</summary>
    InvalidKeyOrParameters = 2,

    /// <summary>The authentication tag did not verify (tampered ciphertext, tampered tag or a key that does not match).</summary>
    IntegrityCheckFailed = 3,

    /// <summary>The period's key was destroyed but its destruction marker carries neither a complete erasure reference nor a complete retention reference, or carries both (an error, tampering, or a marker written before the references existed), so the destruction is not a recorded erasure or retention expiry.</summary>
    KeyDestroyedWithoutErasureRecord = 4,
}

/// <summary>
/// Thrown when a stored <see cref="EncryptedField"/> cannot be read for any reason other than a
/// key destruction recorded as an erasure request or a retention expiry. Only such a destruction (its marker carries
/// an <see cref="Crypto.ErasureReference"/> or a <see cref="Crypto.RetentionReference"/>, not both) is not a failure:
/// it reads as the configured placeholder (<c>[SHREDDED]</c> or <c>[EXPIRED]</c>). A key destroyed without either
/// reference fails with <see cref="EncryptedFieldDecryptionFailureReason.KeyDestroyedWithoutErasureRecord"/>.
/// </summary>
/// <remarks>
/// <para>
/// Integrity wins over availability for the audit trail (ADR-020, ADR-034, issue #2770): damaged or tampered
/// audit data is never presented as lawful erasure (<c>[SHREDDED]</c>) nor as plaintext. The audit projections
/// let this exception escape, so Marten's async daemon pauses the shard until an operator repairs or excludes
/// the event and rebuilds the projection. The recovery steps are in the "Recover from an unreadable audit
/// event" section of <c>docs/features/audit-marten.md</c>.
/// </para>
/// <para>
/// The message is fixed: it carries the <see cref="Code"/> only, never the message of another exception,
/// a key identifier, the envelope or any plaintext, and no inner exception is attached. The cause is available
/// only as <see cref="Reason"/>. Derives from <see cref="CryptographicException"/> so the public
/// <see cref="EncryptedField.Decrypt(byte[])"/> contract stays "throws <see cref="CryptographicException"/>".
/// </para>
/// </remarks>
public sealed class EncryptedFieldDecryptionException : CryptographicException
{
    /// <summary>
    /// The error code carried by every failure, for the operator to find it in the daemon logs.
    /// </summary>
    public const string Code = "audit.marten.encrypted_field_unreadable";

    /// <summary>
    /// Initializes a new instance of the <see cref="EncryptedFieldDecryptionException"/> class.
    /// </summary>
    /// <param name="reason">Why the value could not be read.</param>
    public EncryptedFieldDecryptionException(EncryptedFieldDecryptionFailureReason reason)
        : base($"A stored encrypted audit field cannot be read (error code '{Code}'). The audit projection stops until the event is repaired or excluded.")
    {
        Reason = reason;
    }

    /// <summary>Gets why the value could not be read.</summary>
    public EncryptedFieldDecryptionFailureReason Reason { get; }
}
