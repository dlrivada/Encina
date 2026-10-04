namespace Encina.Marten.GDPR;

/// <summary>
/// Why a stored <c>[CryptoShredded]</c> value could not be read.
/// </summary>
public enum CryptoShreddingDecryptionFailureReason
{
    /// <summary>The subject-id sibling of the constructed owner is missing.</summary>
    SubjectIdMissing = 0,

    /// <summary>The runtime value of the subject-id sibling is not a supported subject-id type.</summary>
    SubjectIdInvalid = 1,

    /// <summary>The key could not be obtained (key store error, key not found, unusable key, or the provider threw).</summary>
    KeyUnavailable = 2,

    /// <summary>The authentication tag or the associated data did not match (tampering, a token copied from another subject, or a tombstone on a live subject).</summary>
    IntegrityCheckFailed = 3,

    /// <summary>The stored value is not a v2 token or the tombstone.</summary>
    EnvelopeMalformed = 4,

    /// <summary>The decrypted value could not be written back to the property.</summary>
    PropertyNotWritable = 5,
}

/// <summary>
/// Thrown by <see cref="CryptoShredderSerializer"/> when a stored <c>[CryptoShredded]</c> value cannot be read.
/// Only a genuinely forgotten subject reads as the anonymized placeholder; every other failure surfaces.
/// </summary>
/// <remarks>
/// <para>
/// Reads fail closed (AGENTS.md: errors are never swallowed in background infrastructure): a key-store outage
/// must not bake the placeholder into projections, snapshots or read models, and tampering must not look like
/// erasure. With Marten's <c>SkipSerializationErrors</c> turned off by the configurator, the async daemon pauses
/// the shard and resumes after recovery.
/// </para>
/// <para>
/// The message carries type names, the property name, the reason and the error code only; never a subject id,
/// a value or the message of another exception, and no inner exception is attached.
/// </para>
/// </remarks>
public sealed class CryptoShreddingDecryptionException : InvalidOperationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CryptoShreddingDecryptionException"/> class.
    /// </summary>
    /// <param name="documentType">The root type being deserialized, or <c>null</c> when unknown.</param>
    /// <param name="declaringType">The type that declares the property.</param>
    /// <param name="propertyName">The property whose value could not be read.</param>
    /// <param name="reason">Why the value could not be read.</param>
    /// <param name="errorCode">The error code of the failure, or <c>null</c>.</param>
    public CryptoShreddingDecryptionException(
        Type? documentType,
        Type declaringType,
        string propertyName,
        CryptoShreddingDecryptionFailureReason reason,
        string? errorCode = null)
        : base(BuildMessage(documentType, declaringType, propertyName, reason, errorCode))
    {
        DocumentTypeName = documentType is null ? "unknown" : documentType.FullName ?? documentType.Name;
        DeclaringTypeName = declaringType.FullName ?? declaringType.Name;
        PropertyName = propertyName;
        Reason = reason;
        ErrorCode = errorCode;
    }

    /// <summary>Gets the full name of the root type being deserialized, or <c>"unknown"</c>.</summary>
    public string DocumentTypeName { get; }

    /// <summary>Gets the full name of the type that declares the property.</summary>
    public string DeclaringTypeName { get; }

    /// <summary>Gets the name of the property whose value could not be read.</summary>
    public string PropertyName { get; }

    /// <summary>Gets why the value could not be read.</summary>
    public CryptoShreddingDecryptionFailureReason Reason { get; }

    /// <summary>Gets the error code of the failure (for example <c>crypto.integrity_check_failed</c>), or <c>null</c>.</summary>
    public string? ErrorCode { get; }

    private static string BuildMessage(
        Type? documentType, Type declaringType, string propertyName, CryptoShreddingDecryptionFailureReason reason, string? errorCode)
    {
        ArgumentNullException.ThrowIfNull(declaringType);
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);

        return $"Cannot read [CryptoShredded] property '{declaringType.FullName ?? declaringType.Name}.{propertyName}' "
            + $"while deserializing '{documentType?.FullName ?? documentType?.Name ?? "unknown"}': {reason} "
            + $"(error code '{errorCode ?? "none"}').";
    }
}
