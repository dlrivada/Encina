namespace Encina.Marten.GDPR;

/// <summary>
/// The active encryption key of a data subject together with its version, as returned by
/// <see cref="Abstractions.ISubjectKeyProvider.GetOrCreateSubjectKeyAsync"/>.
/// </summary>
/// <remarks>
/// <para>
/// The key material and its version come from one read of the key store, so the version written to an
/// encrypted value's key id (<c>subject:{subjectId}:v{version}</c>) is always the version of the key that
/// encrypted it, even when the key is rotated concurrently (#1646).
/// </para>
/// <para>
/// The key material is secret: never log or serialize it. <see cref="ToString"/> omits it.
/// </para>
/// </remarks>
public sealed record SubjectEncryptionKey
{
    /// <summary>
    /// Version number of the key (monotonically increasing, starting at 1).
    /// </summary>
    public required int Version { get; init; }

    /// <summary>
    /// The AES-256 key material (32 bytes).
    /// </summary>
    public required byte[] KeyMaterial { get; init; }

    /// <summary>
    /// Returns the key version only; the key material is never part of the text.
    /// </summary>
    /// <returns>A description of the key that omits the key material.</returns>
    public override string ToString() => $"SubjectEncryptionKey {{ Version = {Version} }}";
}
