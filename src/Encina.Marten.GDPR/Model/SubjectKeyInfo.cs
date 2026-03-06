namespace Encina.Marten.GDPR;

/// <summary>
/// Contains metadata about a specific version of a data subject's encryption key.
/// </summary>
/// <remarks>
/// <para>
/// Each data subject can have multiple key versions over time due to key rotation.
/// The key ID follows the convention <c>subject:{subjectId}:v{version}</c>, which maps
/// directly to <c>EncryptionContext.KeyId</c> for seamless integration with
/// <c>IFieldEncryptor</c> from <c>Encina.Security.Encryption</c>.
/// </para>
/// <para>
/// When a subject exercises their right to be forgotten (GDPR Article 17), all key versions
/// transition to <see cref="SubjectKeyStatus.Deleted"/>, rendering all associated PII
/// permanently unreadable.
/// </para>
/// </remarks>
public sealed record SubjectKeyInfo
{
    /// <summary>
    /// Identifier of the data subject who owns this encryption key.
    /// </summary>
    public required string SubjectId { get; init; }

    /// <summary>
    /// Unique key identifier following the convention <c>subject:{subjectId}:v{version}</c>.
    /// </summary>
    /// <example>subject:user-42:v1</example>
    public required string KeyId { get; init; }

    /// <summary>
    /// Version number of this key (monotonically increasing, starting at 1).
    /// </summary>
    public required int Version { get; init; }

    /// <summary>
    /// Current lifecycle status of this key version.
    /// </summary>
    public required SubjectKeyStatus Status { get; init; }

    /// <summary>
    /// Timestamp when this key version was created (UTC).
    /// </summary>
    public required DateTimeOffset CreatedAtUtc { get; init; }

    /// <summary>
    /// Timestamp when this key version expires (UTC), or <c>null</c> if no expiration is set.
    /// </summary>
    /// <remarks>
    /// Expired keys remain available for decryption of existing events but are not used
    /// for encrypting new events. Key rotation should be triggered before expiration.
    /// </remarks>
    public DateTimeOffset? ExpiresAtUtc { get; init; }
}
