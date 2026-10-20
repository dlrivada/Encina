using Microsoft.Extensions.Logging;

namespace Encina.Marten.GDPR.Diagnostics;

/// <summary>
/// High-performance structured log messages for the crypto-shredding subsystem.
/// </summary>
/// <remarks>
/// <para>
/// The existing messages use <c>LoggerMessage.Define</c> (one literal <c>new EventId(n, ...)</c> each,
/// scanned by the allocation test, #1125) to avoid boxing and string formatting overhead
/// in hot paths; the newer messages use the <c>[LoggerMessage]</c> source generator (see also
/// <see cref="CryptoShreddingLog"/>, Event IDs 8470-8484).
/// </para>
/// <para>
/// Event IDs are allocated in the 8450-8499 range reserved for Marten GDPR crypto-shredding
/// (see <c>EventIdRanges.MartenGDPRCryptoShredding</c>).
/// </para>
/// </remarks>
internal static partial class CryptoShreddingLogMessages
{
    // Note: none of these templates carry the data subject's own identifier, a value, a collection index or a
    // dictionary key — they are personal data and must never reach a log sink (#1429, following #1314).
    // Correlate via the declaring type, property name, reason and error code instead.

    // -- 8450: PII field encrypted --

    private static readonly Action<ILogger, string, string, Exception?> PiiFieldEncryptedDef =
        LoggerMessage.Define<string, string>(
            LogLevel.Debug,
            new EventId(8450, nameof(PiiFieldEncrypted)),
            "PII field encrypted. DeclaringType={DeclaringType}, PropertyName={PropertyName}");

    internal static void PiiFieldEncrypted(this ILogger logger, string declaringType, string propertyName)
        => PiiFieldEncryptedDef(logger, declaringType, propertyName, null);

    // -- 8451: PII field decrypted --

    private static readonly Action<ILogger, string, string, Exception?> PiiFieldDecryptedDef =
        LoggerMessage.Define<string, string>(
            LogLevel.Debug,
            new EventId(8451, nameof(PiiFieldDecrypted)),
            "PII field decrypted. DeclaringType={DeclaringType}, PropertyName={PropertyName}");

    internal static void PiiFieldDecrypted(this ILogger logger, string declaringType, string propertyName)
        => PiiFieldDecryptedDef(logger, declaringType, propertyName, null);

    // -- 8452: Subject forgotten --

    private static readonly Action<ILogger, int, Exception?> SubjectForgottenDef =
        LoggerMessage.Define<int>(
            LogLevel.Information,
            new EventId(8452, nameof(SubjectForgotten)),
            "Subject forgotten (crypto-shredded). KeysDeleted={KeysDeleted}");

    internal static void SubjectForgotten(this ILogger logger, int keysDeleted)
        => SubjectForgottenDef(logger, keysDeleted, null);

    // -- 8453: Key rotated --

    private static readonly Action<ILogger, int, int, Exception?> KeyRotatedDef =
        LoggerMessage.Define<int, int>(
            LogLevel.Information,
            new EventId(8453, nameof(KeyRotated)),
            "Encryption key rotated. OldVersion={OldVersion}, NewVersion={NewVersion}");

    internal static void KeyRotated(this ILogger logger, int oldVersion, int newVersion)
        => KeyRotatedDef(logger, oldVersion, newVersion, null);

    // -- 8454: Forgotten subject accessed --

    private static readonly Action<ILogger, string, string, Exception?> ForgottenSubjectAccessedDef =
        LoggerMessage.Define<string, string>(
            LogLevel.Information,
            new EventId(8454, nameof(ForgottenSubjectAccessed)),
            "Data of a forgotten subject read as the placeholder. DeclaringType={DeclaringType}, PropertyName={PropertyName}");

    internal static void ForgottenSubjectAccessed(this ILogger logger, string declaringType, string propertyName)
        => ForgottenSubjectAccessedDef(logger, declaringType, propertyName, null);

    // -- 8455: Encryption failed --

    private static readonly Action<ILogger, string, string, string, string, Exception?> EncryptionFailedDef =
        LoggerMessage.Define<string, string, string, string>(
            LogLevel.Error,
            new EventId(8455, nameof(EncryptionFailed)),
            "Failed to encrypt PII field; nothing is stored. DeclaringType={DeclaringType}, PropertyName={PropertyName}, Reason={Reason}, ErrorCode={ErrorCode}");

    // The exception, when present, must already be redacted with ForLogging() (#1557).
    internal static void EncryptionFailed(
        this ILogger logger, string declaringType, string propertyName, string reason, string errorCode, Exception? exception = null)
        => EncryptionFailedDef(logger, declaringType, propertyName, reason, errorCode, exception);

    // -- 8456: Decryption failed --

    private static readonly Action<ILogger, string, string, string, string, Exception?> DecryptionFailedDef =
        LoggerMessage.Define<string, string, string, string>(
            LogLevel.Error,
            new EventId(8456, nameof(DecryptionFailed)),
            "Failed to decrypt PII field. DeclaringType={DeclaringType}, PropertyName={PropertyName}, Reason={Reason}, ErrorCode={ErrorCode}");

    // The exception, when present, must already be redacted with ForLogging() (#1557).
    internal static void DecryptionFailed(
        this ILogger logger, string declaringType, string propertyName, string reason, string errorCode, Exception? exception = null)
        => DecryptionFailedDef(logger, declaringType, propertyName, reason, errorCode, exception);

    // -- 8457: Key store error --

    private static readonly Action<ILogger, string, Exception?> KeyStoreErrorDef =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(8457, nameof(KeyStoreError)),
            "Key store operation failed. Operation={Operation}");

    internal static void KeyStoreError(this ILogger logger, string operation, Exception? exception = null)
        => KeyStoreErrorDef(logger, operation, exception);

    // -- 8458: Crypto contract built --

    private static readonly Action<ILogger, string, int, Exception?> CryptoContractBuiltDef =
        LoggerMessage.Define<string, int>(
            LogLevel.Debug,
            new EventId(8458, nameof(CryptoContractBuilt)),
            "Crypto-shredding contract built. DeclaringType={DeclaringType}, FieldCount={FieldCount}");

    internal static void CryptoContractBuilt(this ILogger logger, string declaringType, int fieldCount)
        => CryptoContractBuiltDef(logger, declaringType, fieldCount, null);

    // -- 8459: Attribute misconfigured --

    private static readonly Action<ILogger, string, string, string, Exception?> AttributeMisconfiguredDef =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Error,
            new EventId(8459, nameof(AttributeMisconfigured)),
            "CryptoShredded property misconfigured; nothing of this type is stored. "
            + "DeclaringType={DeclaringType}, PropertyName={PropertyName}, Problems={Problems}");

    internal static void AttributeMisconfigured(
        this ILogger logger, string declaringType, string propertyName, string problems)
        => AttributeMisconfiguredDef(logger, declaringType, propertyName, problems, null);

    // -- 8460: Contract modifier installed --

    private static readonly Action<ILogger, int, Exception?> SerializerWrappedDef =
        LoggerMessage.Define<int>(
            LogLevel.Information,
            new EventId(8460, nameof(SerializerWrapped)),
            "Crypto-shredding contract modifier installed on Marten's System.Text.Json serializer. OptionsCount={OptionsCount}");

    internal static void SerializerWrapped(this ILogger logger, int optionsCount)
        => SerializerWrappedDef(logger, optionsCount, null);

    // -- 8461: Startup validation completed --

    private static readonly Action<ILogger, int, Exception?> StartupValidationCompletedDef =
        LoggerMessage.Define<int>(
            LogLevel.Information,
            new EventId(8461, nameof(StartupValidationCompleted)),
            "Crypto-shredding startup validation completed. TypeCount={TypeCount}");

    internal static void StartupValidationCompleted(this ILogger logger, int typeCount)
        => StartupValidationCompletedDef(logger, typeCount, null);

    // -- 8462: Startup validation skipped (explicit opt-out) --

    private static readonly Action<ILogger, Exception?> StartupValidationSkippedDef =
        LoggerMessage.Define(
            LogLevel.Warning,
            new EventId(8462, nameof(StartupValidationSkipped)),
            "Crypto-shredding startup validation is disabled (ValidateOnStartup = false); misconfigured types fail on first use");

    internal static void StartupValidationSkipped(this ILogger logger)
        => StartupValidationSkippedDef(logger, null);

    // -- 8463: Health check completed --

    private static readonly Action<ILogger, string, int, int, Exception?> HealthCheckCompletedDef =
        LoggerMessage.Define<string, int, int>(
            LogLevel.Debug,
            new EventId(8463, nameof(HealthCheckCompleted)),
            "Crypto-shredding health check completed. Status={Status}, CryptoContractCount={CryptoContractCount}, MisconfiguredTypeCount={MisconfiguredTypeCount}");

    internal static void HealthCheckCompleted(this ILogger logger, string status, int cryptoContractCount, int misconfiguredTypeCount)
        => HealthCheckCompletedDef(logger, status, cryptoContractCount, misconfiguredTypeCount, null);

    // -- 8464: Key rotation scheduled --

    private static readonly Action<ILogger, int, Exception?> KeyRotationScheduledDef =
        LoggerMessage.Define<int>(
            LogLevel.Information,
            new EventId(8464, nameof(KeyRotationScheduled)),
            "Key rotation scheduled. CurrentVersion={CurrentVersion}");

    internal static void KeyRotationScheduled(this ILogger logger, int currentVersion)
        => KeyRotationScheduledDef(logger, currentVersion, null);

    // -- 8465: Re-encryption started --

    private static readonly Action<ILogger, int, Exception?> ReEncryptionStartedDef =
        LoggerMessage.Define<int>(
            LogLevel.Information,
            new EventId(8465, nameof(ReEncryptionStarted)),
            "Re-encryption started after key rotation. NewVersion={NewVersion}");

    internal static void ReEncryptionStarted(this ILogger logger, int newVersion)
        => ReEncryptionStartedDef(logger, newVersion, null);

    // -- 8466: Subject id missing --

    private static readonly Action<ILogger, string, string, string, string, Exception?> SubjectIdMissingDef =
        LoggerMessage.Define<string, string, string, string>(
            LogLevel.Error,
            new EventId(8466, nameof(SubjectIdMissing)),
            "The subject id of a PII field is missing. "
            + "Operation={Operation}, DeclaringType={DeclaringType}, PropertyName={PropertyName}, SubjectIdProperty={SubjectIdProperty}");

    internal static void SubjectIdMissing(
        this ILogger logger, string operation, string declaringType, string propertyName, string subjectIdProperty)
        => SubjectIdMissingDef(logger, operation, declaringType, propertyName, subjectIdProperty, null);

    // -- 8467: Subject key created --

    /// <summary>
    /// Logs the creation of a subject's first encryption key. Event ID: 8467 (see EventIdRanges.MartenGDPRCryptoShredding).
    /// </summary>
    [LoggerMessage(
        EventId = 8467,
        Level = LogLevel.Debug,
        Message = "Created initial encryption key. Version={Version}")]
    internal static partial void KeyCreated(this ILogger logger, int version);

    // -- 8468: Concurrent key write resolved --

    /// <summary>
    /// Logs that a key insert hit a key version another writer had already stored, and the stored key was
    /// returned instead. Event ID: 8468 (see EventIdRanges.MartenGDPRCryptoShredding).
    /// </summary>
    [LoggerMessage(
        EventId = 8468,
        Level = LogLevel.Warning,
        Message = "Key version already stored by a concurrent writer; the stored key is used. Operation={Operation}, Version={Version}")]
    internal static partial void ConcurrentKeyWriteResolved(this ILogger logger, string operation, int version);

    // -- 8469: Leftover keys erased --

    /// <summary>
    /// Logs that a repeated erasure of an already forgotten subject found and deleted key documents.
    /// Event ID: 8469 (see EventIdRanges.MartenGDPRCryptoShredding).
    /// </summary>
    [LoggerMessage(
        EventId = 8469,
        Level = LogLevel.Warning,
        Message = "Subject was already forgotten; leftover encryption keys deleted. KeysDeleted={KeysDeleted}")]
    internal static partial void LeftoverKeysErased(this ILogger logger, int keysDeleted);
}
