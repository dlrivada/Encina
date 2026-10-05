using Microsoft.Extensions.Logging;

namespace Encina.Marten.GDPR.Diagnostics;

/// <summary>
/// Source-generated log messages of the nested crypto-shredding pipeline (#1698).
/// Event IDs: 8470-8484 (see EventIdRanges.MartenGDPRCryptoShredding).
/// </summary>
/// <remarks>
/// No template carries a subject id, a value, a collection index, a dictionary key, a Marten stream id or an
/// <c>EncinaError.Message</c>. Exceptions are passed through <c>ForLogging()</c>.
/// </remarks>
internal static partial class CryptoShreddingLog
{
    /// <summary>Startup validation found misconfigured properties. Event ID: 8470.</summary>
    [LoggerMessage(EventId = 8470, Level = LogLevel.Error,
        Message = "Crypto-shredding startup validation failed; the host does not start. IssueCount={IssueCount}, TypeCount={TypeCount}")]
    internal static partial void StartupValidationFailed(this ILogger logger, int issueCount, int typeCount);

    /// <summary>The crypto-shredding infrastructure is wired wrongly. Event ID: 8471.</summary>
    [LoggerMessage(EventId = 8471, Level = LogLevel.Critical,
        Message = "Crypto-shredding infrastructure is invalid. Problem={Problem}, ComponentType={ComponentType}")]
    internal static partial void CryptoShreddingInfrastructureInvalid(this ILogger logger, string problem, string componentType);

    /// <summary>Some types of a scanned assembly could not be loaded. Event ID: 8472.</summary>
    [LoggerMessage(EventId = 8472, Level = LogLevel.Warning,
        Message = "Some types could not be loaded during the crypto-shredding startup scan. AssemblyName={AssemblyName}, LoaderExceptionCount={LoaderExceptionCount}")]
    internal static partial void StartupTypeLoadPartial(this ILogger logger, string assemblyName, int loaderExceptionCount);

    /// <summary>A check of an open generic type is deferred to its closed types. Event ID: 8473.</summary>
    [LoggerMessage(EventId = 8473, Level = LogLevel.Debug,
        Message = "Crypto-shredding check of an open generic type deferred to its closed types. TypeName={TypeName}, PropertyName={PropertyName}")]
    internal static partial void OpenGenericCheckDeferred(this ILogger logger, string typeName, string propertyName);

    /// <summary>The placeholder of a forgotten subject was written as the tombstone. Event ID: 8474.</summary>
    [LoggerMessage(EventId = 8474, Level = LogLevel.Debug,
        Message = "Forgotten subject's placeholder written as the tombstone. DeclaringType={DeclaringType}, PropertyName={PropertyName}")]
    internal static partial void ForgottenSubjectTombstoneWritten(this ILogger logger, string declaringType, string propertyName);

    /// <summary>The forgotten-subject handler threw; the read continues. Event ID: 8475.</summary>
    [LoggerMessage(EventId = 8475, Level = LogLevel.Warning,
        Message = "Forgotten subject handler failed. DocumentType={DocumentType}")]
    internal static partial void ForgottenSubjectHandlerFailed(this ILogger logger, Exception exception, string documentType);

    /// <summary>A crypto field was processed outside a serializer call. Event ID: 8476.</summary>
    [LoggerMessage(EventId = 8476, Level = LogLevel.Debug,
        Message = "Crypto-shredding used an implicit one-shot scope (options used outside the serializer). Operation={Operation}, DeclaringType={DeclaringType}")]
    internal static partial void ImplicitCryptoScopeUsed(this ILogger logger, string operation, string declaringType);

    /// <summary>The default handler met a forgotten subject. Event ID: 8477.</summary>
    [LoggerMessage(EventId = 8477, Level = LogLevel.Information,
        Message = "Encountered a forgotten subject; its fields read as the placeholder. DocumentType={DocumentType}, FieldPath={FieldPath}")]
    internal static partial void ForgottenSubjectEncountered(this ILogger logger, string documentType, string fieldPath);

    /// <summary>A subject-id property is itself marked as personal data. Event ID: 8478.</summary>
    [LoggerMessage(EventId = 8478, Level = LogLevel.Warning,
        Message = "A crypto-shredding subject-id property is marked [PersonalData]; use pseudonymous ids. DeclaringType={DeclaringType}, SubjectIdProperty={SubjectIdProperty}")]
    internal static partial void SubjectIdPropertyIsPersonalData(this ILogger logger, string declaringType, string subjectIdProperty);

    /// <summary>A crypto-shredding erasure was requested. Event ID: 8479.</summary>
    [LoggerMessage(EventId = 8479, Level = LogLevel.Debug,
        Message = "Crypto-shredding erasure requested. EntityType={EntityType}, FieldName={FieldName}")]
    internal static partial void ErasureRequested(this ILogger logger, string entityType, string fieldName);

    /// <summary>The subject was already forgotten; erasure succeeds. Event ID: 8480.</summary>
    [LoggerMessage(EventId = 8480, Level = LogLevel.Debug,
        Message = "Subject already forgotten; crypto-shredding erasure is a no-op. EntityType={EntityType}, FieldName={FieldName}")]
    internal static partial void ErasureSubjectAlreadyForgotten(this ILogger logger, string entityType, string fieldName);

    /// <summary>The personal data locator started. Event ID: 8481.</summary>
    [LoggerMessage(EventId = 8481, Level = LogLevel.Debug,
        Message = "Locating crypto-shredded personal data in the Marten event store")]
    internal static partial void PersonalDataLocateStarted(this ILogger logger);

    /// <summary>The personal data locator completed. Event ID: 8482.</summary>
    [LoggerMessage(EventId = 8482, Level = LogLevel.Debug,
        Message = "Located crypto-shredded personal data. Count={Count}")]
    internal static partial void PersonalDataLocateCompleted(this ILogger logger, int count);

    /// <summary>The personal data locator failed. Event ID: 8483.</summary>
    [LoggerMessage(EventId = 8483, Level = LogLevel.Error,
        Message = "Failed to locate crypto-shredded personal data. EventSequence={EventSequence}")]
    internal static partial void PersonalDataLocateFailed(this ILogger logger, Exception exception, long? eventSequence);

    /// <summary>The in-memory key store is in use: keys are lost on restart. Event ID: 8484.</summary>
    [LoggerMessage(EventId = 8484, Level = LogLevel.Warning,
        Message = "The in-memory subject key store is in use: every key is lost on restart and older encrypted data then fails to read. Use the PostgreSQL key store in production")]
    internal static partial void InMemoryKeyStoreInUse(this ILogger logger);
}
