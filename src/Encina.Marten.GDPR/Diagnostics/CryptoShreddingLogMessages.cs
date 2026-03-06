using Microsoft.Extensions.Logging;

namespace Encina.Marten.GDPR.Diagnostics;

/// <summary>
/// High-performance structured log messages for the crypto-shredding subsystem.
/// </summary>
/// <remarks>
/// <para>
/// Uses <c>LoggerMessage.Define</c> to avoid boxing and string formatting overhead
/// in hot paths. All methods are extension methods on <see cref="ILogger"/> for ergonomic use.
/// </para>
/// <para>
/// Event IDs are allocated in the 8400-8449 range to avoid collisions with other
/// Encina subsystems (Security 8000-8004, GDPR 8100-8112, Consent 8200-8250).
/// </para>
/// </remarks>
internal static class CryptoShreddingLogMessages
{
    // -- 8400: PII field encrypted --

    private static readonly Action<ILogger, string, string, string, Exception?> PiiFieldEncryptedDef =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Debug,
            new EventId(8400, nameof(PiiFieldEncrypted)),
            "PII field encrypted. SubjectId={SubjectId}, PropertyName={PropertyName}, EventType={EventType}");

    internal static void PiiFieldEncrypted(this ILogger logger, string subjectId, string propertyName, string eventType)
        => PiiFieldEncryptedDef(logger, subjectId, propertyName, eventType, null);

    // -- 8401: PII field decrypted --

    private static readonly Action<ILogger, string, string, string, Exception?> PiiFieldDecryptedDef =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Debug,
            new EventId(8401, nameof(PiiFieldDecrypted)),
            "PII field decrypted. SubjectId={SubjectId}, PropertyName={PropertyName}, EventType={EventType}");

    internal static void PiiFieldDecrypted(this ILogger logger, string subjectId, string propertyName, string eventType)
        => PiiFieldDecryptedDef(logger, subjectId, propertyName, eventType, null);

    // -- 8402: Subject forgotten --

    private static readonly Action<ILogger, string, int, Exception?> SubjectForgottenDef =
        LoggerMessage.Define<string, int>(
            LogLevel.Information,
            new EventId(8402, nameof(SubjectForgotten)),
            "Subject forgotten (crypto-shredded). SubjectId={SubjectId}, KeysDeleted={KeysDeleted}");

    internal static void SubjectForgotten(this ILogger logger, string subjectId, int keysDeleted)
        => SubjectForgottenDef(logger, subjectId, keysDeleted, null);

    // -- 8403: Key rotated --

    private static readonly Action<ILogger, string, int, int, Exception?> KeyRotatedDef =
        LoggerMessage.Define<string, int, int>(
            LogLevel.Information,
            new EventId(8403, nameof(KeyRotated)),
            "Encryption key rotated. SubjectId={SubjectId}, OldVersion={OldVersion}, NewVersion={NewVersion}");

    internal static void KeyRotated(this ILogger logger, string subjectId, int oldVersion, int newVersion)
        => KeyRotatedDef(logger, subjectId, oldVersion, newVersion, null);

    // -- 8404: Forgotten subject accessed --

    private static readonly Action<ILogger, string, string, string, Exception?> ForgottenSubjectAccessedDef =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Warning,
            new EventId(8404, nameof(ForgottenSubjectAccessed)),
            "Attempt to decrypt data for forgotten subject. SubjectId={SubjectId}, PropertyName={PropertyName}, EventType={EventType}");

    internal static void ForgottenSubjectAccessed(this ILogger logger, string subjectId, string propertyName, string eventType)
        => ForgottenSubjectAccessedDef(logger, subjectId, propertyName, eventType, null);

    // -- 8405: Encryption failed --

    private static readonly Action<ILogger, string, string, string, Exception?> EncryptionFailedDef =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Error,
            new EventId(8405, nameof(EncryptionFailed)),
            "Failed to encrypt PII field. SubjectId={SubjectId}, PropertyName={PropertyName}, EventType={EventType}");

    internal static void EncryptionFailed(this ILogger logger, string subjectId, string propertyName, string eventType, Exception? exception = null)
        => EncryptionFailedDef(logger, subjectId, propertyName, eventType, exception);

    // -- 8406: Decryption failed --

    private static readonly Action<ILogger, string, string, string, Exception?> DecryptionFailedDef =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Error,
            new EventId(8406, nameof(DecryptionFailed)),
            "Failed to decrypt PII field. SubjectId={SubjectId}, PropertyName={PropertyName}, EventType={EventType}");

    internal static void DecryptionFailed(this ILogger logger, string subjectId, string propertyName, string eventType, Exception? exception = null)
        => DecryptionFailedDef(logger, subjectId, propertyName, eventType, exception);

    // -- 8407: Key store error --

    private static readonly Action<ILogger, string, Exception?> KeyStoreErrorDef =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(8407, nameof(KeyStoreError)),
            "Key store operation failed. Operation={Operation}");

    internal static void KeyStoreError(this ILogger logger, string operation, Exception? exception = null)
        => KeyStoreErrorDef(logger, operation, exception);

    // -- 8408: Metadata cache built --

    private static readonly Action<ILogger, int, int, Exception?> MetadataCacheBuiltDef =
        LoggerMessage.Define<int, int>(
            LogLevel.Information,
            new EventId(8408, nameof(MetadataCacheBuilt)),
            "Crypto-shredded metadata cache built. TypeCount={TypeCount}, FieldCount={FieldCount}");

    internal static void MetadataCacheBuilt(this ILogger logger, int typeCount, int fieldCount)
        => MetadataCacheBuiltDef(logger, typeCount, fieldCount, null);

    // -- 8409: Attribute misconfigured --

    private static readonly Action<ILogger, string, string, Exception?> AttributeMisconfiguredDef =
        LoggerMessage.Define<string, string>(
            LogLevel.Warning,
            new EventId(8409, nameof(AttributeMisconfigured)),
            "CryptoShredded attribute misconfigured. PropertyName={PropertyName}, DeclaringType={DeclaringType}");

    internal static void AttributeMisconfigured(this ILogger logger, string propertyName, string declaringType)
        => AttributeMisconfiguredDef(logger, propertyName, declaringType, null);

    // -- 8410: Serializer wrapped --

    private static readonly Action<ILogger, string, Exception?> SerializerWrappedDef =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(8410, nameof(SerializerWrapped)),
            "Marten serializer wrapped with crypto-shredding decorator. InnerSerializerType={InnerSerializerType}");

    internal static void SerializerWrapped(this ILogger logger, string innerSerializerType)
        => SerializerWrappedDef(logger, innerSerializerType, null);

    // -- 8411: Auto-registration completed --

    private static readonly Action<ILogger, int, int, Exception?> AutoRegistrationCompletedDef =
        LoggerMessage.Define<int, int>(
            LogLevel.Information,
            new EventId(8411, nameof(AutoRegistrationCompleted)),
            "Crypto-shredding auto-registration completed. TypeCount={TypeCount}, AssemblyCount={AssemblyCount}");

    internal static void AutoRegistrationCompleted(this ILogger logger, int typeCount, int assemblyCount)
        => AutoRegistrationCompletedDef(logger, typeCount, assemblyCount, null);

    // -- 8412: Auto-registration skipped --

    private static readonly Action<ILogger, string, Exception?> AutoRegistrationSkippedDef =
        LoggerMessage.Define<string>(
            LogLevel.Debug,
            new EventId(8412, nameof(AutoRegistrationSkipped)),
            "Type skipped during crypto-shredding auto-registration. TypeName={TypeName}");

    internal static void AutoRegistrationSkipped(this ILogger logger, string typeName)
        => AutoRegistrationSkippedDef(logger, typeName, null);

    // -- 8413: Health check completed --

    private static readonly Action<ILogger, string, int, Exception?> HealthCheckCompletedDef =
        LoggerMessage.Define<string, int>(
            LogLevel.Debug,
            new EventId(8413, nameof(HealthCheckCompleted)),
            "Crypto-shredding health check completed. Status={Status}, CachedTypeCount={CachedTypeCount}");

    internal static void HealthCheckCompleted(this ILogger logger, string status, int cachedTypeCount)
        => HealthCheckCompletedDef(logger, status, cachedTypeCount, null);

    // -- 8414: Key rotation scheduled --

    private static readonly Action<ILogger, string, int, Exception?> KeyRotationScheduledDef =
        LoggerMessage.Define<string, int>(
            LogLevel.Information,
            new EventId(8414, nameof(KeyRotationScheduled)),
            "Key rotation scheduled. SubjectId={SubjectId}, CurrentVersion={CurrentVersion}");

    internal static void KeyRotationScheduled(this ILogger logger, string subjectId, int currentVersion)
        => KeyRotationScheduledDef(logger, subjectId, currentVersion, null);

    // -- 8415: Re-encryption started --

    private static readonly Action<ILogger, string, int, Exception?> ReEncryptionStartedDef =
        LoggerMessage.Define<string, int>(
            LogLevel.Information,
            new EventId(8415, nameof(ReEncryptionStarted)),
            "Re-encryption started for subject after key rotation. SubjectId={SubjectId}, NewVersion={NewVersion}");

    internal static void ReEncryptionStarted(this ILogger logger, string subjectId, int newVersion)
        => ReEncryptionStartedDef(logger, subjectId, newVersion, null);
}
