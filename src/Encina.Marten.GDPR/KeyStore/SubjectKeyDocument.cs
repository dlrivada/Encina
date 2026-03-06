namespace Encina.Marten.GDPR;

/// <summary>
/// Marten document entity for persisting per-subject encryption key material in PostgreSQL.
/// </summary>
/// <remarks>
/// <para>
/// Each document represents a single versioned encryption key for a data subject. The
/// <see cref="Id"/> follows the convention <c>"subject:{subjectId}:v{version}"</c>,
/// which maps directly to the <c>kid</c> field used by <c>IFieldEncryptor</c> from
/// <c>Encina.Security.Encryption</c>.
/// </para>
/// <para>
/// Documents are managed by <c>PostgreSqlSubjectKeyProvider</c> through Marten's
/// <c>IDocumentSession</c>. When a data subject exercises their right to be forgotten,
/// all documents for that subject are hard-deleted from PostgreSQL.
/// </para>
/// <para>
/// A computed index on <see cref="SubjectId"/> ensures efficient lookups when querying
/// all key versions for a given subject.
/// </para>
/// </remarks>
internal sealed class SubjectKeyDocument
{
    /// <summary>
    /// Unique key identifier following the convention <c>"subject:{subjectId}:v{version}"</c>.
    /// </summary>
    /// <remarks>
    /// Serves as the Marten document identity and matches the <c>kid</c> field stored
    /// in encrypted PII values for version-specific decryption.
    /// </remarks>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Identifier of the data subject who owns this encryption key.
    /// </summary>
    public string SubjectId { get; set; } = string.Empty;

    /// <summary>
    /// AES-256 key material (32 bytes) used for field-level encryption/decryption.
    /// </summary>
    /// <remarks>
    /// Key material is generated via <see cref="System.Security.Cryptography.RandomNumberGenerator"/>
    /// and must NEVER be logged or exposed in diagnostics.
    /// </remarks>
    public byte[] KeyMaterial { get; set; } = [];

    /// <summary>
    /// Version number of this key (monotonically increasing, starting at 1).
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Current lifecycle status of this key version.
    /// </summary>
    public SubjectKeyStatus Status { get; set; }

    /// <summary>
    /// Timestamp when this key version was created (UTC).
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>
    /// Timestamp when this key version expires (UTC), or <c>null</c> if no expiration is set.
    /// </summary>
    public DateTimeOffset? ExpiresAtUtc { get; set; }
}
