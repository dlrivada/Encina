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
/// Event IDs are allocated in the 8450-8499 range reserved for Marten GDPR crypto-shredding
/// (see <c>EventIdRanges.MartenGDPRCryptoShredding</c>).
/// </para>
/// </remarks>
internal static class CryptoShreddingLogMessages
{
    // -- 8450: PII field encrypted --

    private static readonly Action<ILogger, string, string, string, Exception?> PiiFieldEncryptedDef =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Debug,
            new EventId(8450, nameof(PiiFieldEncrypted)),
            "PII field encrypted. SubjectId={SubjectId}, PropertyName={PropertyName}, EventType={EventType}");

    internal static void PiiFieldEncrypted(this ILogger logger, string subjectId, string propertyName, string eventType)
        => PiiFieldEncryptedDef(logger, subjectId, propertyName, eventType, null);

    // -- 8451: PII field decrypted --

    private static readonly Action<ILogger, string, string, string, Exception?> PiiFieldDecryptedDef =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Debug,
            new EventId(8451, nameof(PiiFieldDecrypted)),
            "PII field decrypted. SubjectId={SubjectId}, PropertyName={PropertyName}, EventType={EventType}");

    internal static void PiiFieldDecrypted(this ILogger logger, string subjectId, string propertyName, string eventType)
        => PiiFieldDecryptedDef(logger, subjectId, propertyName, eventType, null);

    // -- 8452: Subject forgotten --

    private static readonly Action<ILogger, string, int, Exception?> SubjectForgottenDef =
        LoggerMessage.Define<string, int>(
            LogLevel.Information,
            new EventId(8452, nameof(SubjectForgotten)),
            "Subject forgotten (crypto-shredded). SubjectId={SubjectId}, KeysDeleted={KeysDeleted}");

    internal static void SubjectForgotten(this ILogger logger, string subjectId, int keysDeleted)
        => SubjectForgottenDef(logger, subjectId, keysDeleted, null);

    // -- 8453: Key rotated --

    private static readonly Action<ILogger, string, int, int, Exception?> KeyRotatedDef =
        LoggerMessage.Define<string, int, int>(
            LogLevel.Information,
            new EventId(8453, nameof(KeyRotated)),
            "Encryption key rotated. SubjectId={SubjectId}, OldVersion={OldVersion}, NewVersion={NewVersion}");

    internal static void KeyRotated(this ILogger logger, string subjectId, int oldVersion, int newVersion)
        => KeyRotatedDef(logger, subjectId, oldVersion, newVersion, null);

    // -- 8454: Forgotten subject accessed --

    private static readonly Action<ILogger, string, string, string, Exception?> ForgottenSubjectAccessedDef =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Warning,
            new EventId(8454, nameof(ForgottenSubjectAccessed)),
            "Attempt to decrypt data for forgotten subject. SubjectId={SubjectId}, PropertyName={PropertyName}, EventType={EventType}");

    internal static void ForgottenSubjectAccessed(this ILogger logger, string subjectId, string propertyName, string eventType)
        => ForgottenSubjectAccessedDef(logger, subjectId, propertyName, eventType, null);

    // -- 8455: Encryption failed --

    private static readonly Action<ILogger, string, string, string, Exception?> EncryptionFailedDef =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Error,
            new EventId(8455, nameof(EncryptionFailed)),
            "Failed to encrypt PII field. SubjectId={SubjectId}, PropertyName={PropertyName}, EventType={EventType}");

    internal static void EncryptionFailed(this ILogger logger, string subjectId, string propertyName, string eventType, Exception? exception = null)
        => EncryptionFailedDef(logger, subjectId, propertyName, eventType, exception);

    // -- 8456: Decryption failed --

    private static readonly Action<ILogger, string, string, string, Exception?> DecryptionFailedDef =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Error,
            new EventId(8456, nameof(DecryptionFailed)),
            "Failed to decrypt PII field. SubjectId={SubjectId}, PropertyName={PropertyName}, EventType={EventType}");

    internal static void DecryptionFailed(this ILogger logger, string subjectId, string propertyName, string eventType, Exception? exception = null)
        => DecryptionFailedDef(logger, subjectId, propertyName, eventType, exception);

    // -- 8457: Key store error --

    private static readonly Action<ILogger, string, Exception?> KeyStoreErrorDef =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(8457, nameof(KeyStoreError)),
            "Key store operation failed. Operation={Operation}");

    internal static void KeyStoreError(this ILogger logger, string operation, Exception? exception = null)
        => KeyStoreErrorDef(logger, operation, exception);

    // -- 8458: Metadata cache built --

    private static readonly Action<ILogger, int, int, Exception?> MetadataCacheBuiltDef =
        LoggerMessage.Define<int, int>(
            LogLevel.Information,
            new EventId(8458, nameof(MetadataCacheBuilt)),
            "Crypto-shredded metadata cache built. TypeCount={TypeCount}, FieldCount={FieldCount}");

    internal static void MetadataCacheBuilt(this ILogger logger, int typeCount, int fieldCount)
        => MetadataCacheBuiltDef(logger, typeCount, fieldCount, null);

    // -- 8459: Attribute misconfigured --

    private static readonly Action<ILogger, string, string, Exception?> AttributeMisconfiguredDef =
        LoggerMessage.Define<string, string>(
            LogLevel.Warning,
            new EventId(8459, nameof(AttributeMisconfigured)),
            "CryptoShredded attribute misconfigured. PropertyName={PropertyName}, DeclaringType={DeclaringType}");

    internal static void AttributeMisconfigured(this ILogger logger, string propertyName, string declaringType)
        => AttributeMisconfiguredDef(logger, propertyName, declaringType, null);

    // -- 8460: Serializer wrapped --

    private static readonly Action<ILogger, string, Exception?> SerializerWrappedDef =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(8460, nameof(SerializerWrapped)),
            "Marten serializer wrapped with crypto-shredding decorator. InnerSerializerType={InnerSerializerType}");

    internal static void SerializerWrapped(this ILogger logger, string innerSerializerType)
        => SerializerWrappedDef(logger, innerSerializerType, null);

    // -- 8461: Auto-registration completed --

    private static readonly Action<ILogger, int, int, Exception?> AutoRegistrationCompletedDef =
        LoggerMessage.Define<int, int>(
            LogLevel.Information,
            new EventId(8461, nameof(AutoRegistrationCompleted)),
            "Crypto-shredding auto-registration completed. TypeCount={TypeCount}, AssemblyCount={AssemblyCount}");

    internal static void AutoRegistrationCompleted(this ILogger logger, int typeCount, int assemblyCount)
        => AutoRegistrationCompletedDef(logger, typeCount, assemblyCount, null);

    // -- 8462: Auto-registration skipped --

    private static readonly Action<ILogger, string, Exception?> AutoRegistrationSkippedDef =
        LoggerMessage.Define<string>(
            LogLevel.Debug,
            new EventId(8462, nameof(AutoRegistrationSkipped)),
            "Type skipped during crypto-shredding auto-registration. TypeName={TypeName}");

    internal static void AutoRegistrationSkipped(this ILogger logger, string typeName)
        => AutoRegistrationSkippedDef(logger, typeName, null);

    // -- 8463: Health check completed --

    private static readonly Action<ILogger, string, int, Exception?> HealthCheckCompletedDef =
        LoggerMessage.Define<string, int>(
            LogLevel.Debug,
            new EventId(8463, nameof(HealthCheckCompleted)),
            "Crypto-shredding health check completed. Status={Status}, CachedTypeCount={CachedTypeCount}");

    internal static void HealthCheckCompleted(this ILogger logger, string status, int cachedTypeCount)
        => HealthCheckCompletedDef(logger, status, cachedTypeCount, null);

    // -- 8464: Key rotation scheduled --

    private static readonly Action<ILogger, string, int, Exception?> KeyRotationScheduledDef =
        LoggerMessage.Define<string, int>(
            LogLevel.Information,
            new EventId(8464, nameof(KeyRotationScheduled)),
            "Key rotation scheduled. SubjectId={SubjectId}, CurrentVersion={CurrentVersion}");

    internal static void KeyRotationScheduled(this ILogger logger, string subjectId, int currentVersion)
        => KeyRotationScheduledDef(logger, subjectId, currentVersion, null);

    // -- 8465: Re-encryption started --

    private static readonly Action<ILogger, string, int, Exception?> ReEncryptionStartedDef =
        LoggerMessage.Define<string, int>(
            LogLevel.Information,
            new EventId(8465, nameof(ReEncryptionStarted)),
            "Re-encryption started for subject after key rotation. SubjectId={SubjectId}, NewVersion={NewVersion}");

    internal static void ReEncryptionStarted(this ILogger logger, string subjectId, int newVersion)
        => ReEncryptionStartedDef(logger, subjectId, newVersion, null);
}
