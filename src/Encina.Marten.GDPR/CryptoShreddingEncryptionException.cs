namespace Encina.Marten.GDPR;

/// <summary>
/// Why a <c>[CryptoShredded]</c> value could not be encrypted during serialization.
/// </summary>
public enum CryptoShreddingEncryptionFailureReason
{
    /// <summary>
    /// The subject-id sibling of the owner is <c>null</c>, <see cref="Guid.Empty"/>, or an empty or whitespace
    /// string, so there is no data subject whose key could encrypt the value.
    /// </summary>
    SubjectIdMissing = 0,

    /// <summary>The runtime value of the subject-id sibling is not a supported subject-id type.</summary>
    SubjectIdInvalid = 1,

    /// <summary>
    /// The <see cref="Abstractions.ISubjectKeyProvider"/> returned an error (for example the subject was forgotten
    /// or the key store is unreachable), threw, or returned an unusable key.
    /// </summary>
    KeyUnavailable = 2,
}

/// <summary>
/// Thrown by <see cref="CryptoShredderSerializer"/> when a non-null <c>[CryptoShredded]</c> value, at any depth,
/// cannot be encrypted, so the document is never serialized and Marten's append or save fails before anything
/// is stored.
/// </summary>
/// <remarks>
/// <para>
/// The event store is append-only: a value written in plaintext once can never be crypto-shredded afterwards.
/// The serializer therefore fails closed (AGENTS.md, "Compliance and security gates fail closed").
/// </para>
/// <para>
/// The message names the root document type, the declaring type, the property and, for a key failure, the
/// error code. It never carries the subject id, the plaintext or the message of an underlying exception; no
/// inner exception is attached.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// try
/// {
///     session.Events.Append(streamId, new PatientNoteAdded(patientId, note));
///     await session.SaveChangesAsync(ct);
/// }
/// catch (CryptoShreddingEncryptionException ex)
/// {
///     logger.LogError("Not stored: {Reason} on {Type}.{Property}", ex.Reason, ex.DeclaringTypeName, ex.PropertyName);
/// }
/// </code>
/// </example>
public sealed class CryptoShreddingEncryptionException : InvalidOperationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CryptoShreddingEncryptionException"/> class.
    /// </summary>
    /// <param name="documentType">The root type being serialized, or <c>null</c> when unknown (outside a serializer call).</param>
    /// <param name="declaringType">The type that declares the property.</param>
    /// <param name="propertyName">The <c>[CryptoShredded]</c> property that could not be encrypted.</param>
    /// <param name="reason">Why the value could not be encrypted.</param>
    /// <param name="errorCode">The key-provider error code for <see cref="CryptoShreddingEncryptionFailureReason.KeyUnavailable"/>; otherwise <c>null</c>.</param>
    public CryptoShreddingEncryptionException(
        Type? documentType,
        Type declaringType,
        string propertyName,
        CryptoShreddingEncryptionFailureReason reason,
        string? errorCode = null)
        : base(BuildMessage(documentType, declaringType, propertyName, reason, errorCode))
    {
        DocumentTypeName = CryptoShreddingDecryptionException.NameOf(documentType);
        DeclaringTypeName = CryptoShreddingDecryptionException.NameOf(declaringType);
        PropertyName = propertyName;
        Reason = reason;
        ErrorCode = errorCode;
    }

    /// <summary>Gets the full name of the root type being serialized, or <c>"unknown"</c> outside a serializer call.</summary>
    public string DocumentTypeName { get; }

    /// <summary>Gets the full name of the type that declares the property.</summary>
    public string DeclaringTypeName { get; }

    /// <summary>Gets the name of the property that could not be encrypted.</summary>
    public string PropertyName { get; }

    /// <summary>Gets why the value could not be encrypted.</summary>
    public CryptoShreddingEncryptionFailureReason Reason { get; }

    /// <summary>Gets the key-provider error code (for example <c>crypto.subject_forgotten</c>), or <c>null</c>.</summary>
    public string? ErrorCode { get; }

    private static string BuildMessage(
        Type? documentType, Type declaringType, string propertyName, CryptoShreddingEncryptionFailureReason reason, string? errorCode)
    {
        ArgumentNullException.ThrowIfNull(declaringType);
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);

        var cause = reason switch
        {
            CryptoShreddingEncryptionFailureReason.SubjectIdMissing =>
                "its subject id is missing (null, an empty Guid, or an empty or whitespace string)",
            CryptoShreddingEncryptionFailureReason.SubjectIdInvalid =>
                "its subject id is not of a supported subject-id type",
            _ => $"the subject's encryption key could not be obtained (error code '{errorCode ?? "unknown"}')",
        };

        return $"Cannot encrypt [CryptoShredded] property '{CryptoShreddingDecryptionException.NameOf(declaringType)}.{propertyName}' "
            + $"while serializing '{CryptoShreddingDecryptionException.NameOf(documentType)}': {cause}. "
            + "Nothing was serialized, so the personal data was not stored.";
    }
}
