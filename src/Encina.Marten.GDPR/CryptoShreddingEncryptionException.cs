namespace Encina.Marten.GDPR;

/// <summary>
/// Why a <c>[CryptoShredded]</c> value could not be encrypted during event serialization.
/// </summary>
public enum CryptoShreddingEncryptionFailureReason
{
    /// <summary>
    /// The subject-id property of the event is <c>null</c>, <see cref="Guid.Empty"/>, or an empty or
    /// whitespace string, so there is no data subject whose key could encrypt the value.
    /// </summary>
    SubjectIdMissing = 0,

    /// <summary>
    /// The <see cref="Abstractions.ISubjectKeyProvider"/> returned an error (for example the subject was
    /// forgotten or the key store is unreachable), threw, or returned an unusable key.
    /// </summary>
    KeyUnavailable = 1,

    /// <summary>
    /// The property cannot hold its ciphertext: it has no setter or init accessor, is declared on a struct
    /// (the serializer cannot write to the boxed event), is not a <c>string</c>, lacks <c>[PersonalData]</c>,
    /// or references a subject-id property that does not exist or cannot be read.
    /// </summary>
    PropertyMisconfigured = 2
}

/// <summary>
/// Thrown by <see cref="CryptoShredderSerializer"/> when a non-null <c>[CryptoShredded]</c> value cannot be
/// encrypted, so the event is never serialized and Marten's append fails before anything is stored.
/// </summary>
/// <remarks>
/// <para>
/// The event store is append-only: a value written in plaintext once can never be crypto-shredded
/// afterwards. The serializer therefore fails closed (AGENTS.md, "Compliance and security gates fail
/// closed"; #1646) instead of storing personal data unencrypted.
/// </para>
/// <para>
/// The message names the event type, the property and, for a key failure, the error code. It never
/// carries the subject id, the plaintext or the message of an underlying exception, so it is safe to log.
/// The underlying exception is not attached for the same reason; the serializer logs its type and stack
/// trace through <c>ForLogging()</c> before throwing.
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
///     logger.LogError("Event not stored: {Reason} on {EventType}.{Property}", ex.Reason, ex.EventTypeName, ex.PropertyName);
/// }
/// </code>
/// </example>
public sealed class CryptoShreddingEncryptionException : InvalidOperationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CryptoShreddingEncryptionException"/> class.
    /// </summary>
    /// <param name="eventType">The event type that was being serialized.</param>
    /// <param name="propertyName">
    /// The <c>[CryptoShredded]</c> property that could not be encrypted; for
    /// <see cref="CryptoShreddingEncryptionFailureReason.PropertyMisconfigured"/> the comma-separated names of
    /// every misconfigured property of the type.
    /// </param>
    /// <param name="reason">Why the value could not be encrypted.</param>
    /// <param name="errorCode">
    /// The error code of the key-provider failure for <see cref="CryptoShreddingEncryptionFailureReason.KeyUnavailable"/>;
    /// otherwise <c>null</c>.
    /// </param>
    public CryptoShreddingEncryptionException(
        Type eventType,
        string propertyName,
        CryptoShreddingEncryptionFailureReason reason,
        string? errorCode = null)
        : base(BuildMessage(eventType, propertyName, reason, errorCode))
    {
        EventTypeName = eventType.FullName ?? eventType.Name;
        PropertyName = propertyName;
        Reason = reason;
        ErrorCode = errorCode;
    }

    /// <summary>
    /// Gets the full name of the event type that was being serialized.
    /// </summary>
    public string EventTypeName { get; }

    /// <summary>
    /// Gets the name of the <c>[CryptoShredded]</c> property that could not be encrypted, or the
    /// comma-separated names of every misconfigured property for
    /// <see cref="CryptoShreddingEncryptionFailureReason.PropertyMisconfigured"/>.
    /// </summary>
    public string PropertyName { get; }

    /// <summary>
    /// Gets why the value could not be encrypted.
    /// </summary>
    public CryptoShreddingEncryptionFailureReason Reason { get; }

    /// <summary>
    /// Gets the error code of the key-provider failure (for example <c>crypto.key_store_error</c> or
    /// <c>crypto.subject_forgotten</c>), or <c>null</c> when the failure is not a key failure.
    /// </summary>
    public string? ErrorCode { get; }

    private static string BuildMessage(
        Type eventType,
        string propertyName,
        CryptoShreddingEncryptionFailureReason reason,
        string? errorCode)
    {
        ArgumentNullException.ThrowIfNull(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);

        var cause = reason switch
        {
            CryptoShreddingEncryptionFailureReason.SubjectIdMissing =>
                "its subject id is missing (null, an empty Guid, or an empty or whitespace string)",
            CryptoShreddingEncryptionFailureReason.KeyUnavailable =>
                $"the subject's encryption key could not be obtained (error code '{errorCode ?? "unknown"}')",
            _ =>
                "the property is misconfigured for crypto-shredding: it must be a public string property of a class "
                + "or record class with a setter or init accessor, carry [PersonalData], and reference a readable "
                + "subject-id property of a supported type",
        };

        return $"Cannot encrypt [CryptoShredded] property '{propertyName}' on event type "
            + $"'{eventType.FullName ?? eventType.Name}': {cause}. The event was not serialized, so the "
            + "personal data was not stored.";
    }
}
