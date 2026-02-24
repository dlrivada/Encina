using Microsoft.Extensions.Logging;

namespace Encina.Compliance.GDPR.Diagnostics;

/// <summary>
/// High-performance structured log messages for lawful basis validation.
/// </summary>
/// <remarks>
/// <para>
/// Uses the <c>[LoggerMessage]</c> source generator for zero-allocation logging in hot paths.
/// Event IDs are allocated in the 8200-8220 range for lawful basis validation.
/// </para>
/// <para>
/// Events 8211-8213 remain in <see cref="GDPRLogMessages"/> for auto-registration and health checks.
/// </para>
/// </remarks>
internal static partial class LawfulBasisLogMessages
{
    // =====================================================
    // Validation lifecycle (8200-8202)
    // =====================================================

    /// <summary>Lawful basis validation started for a request type.</summary>
    [LoggerMessage(
        EventId = 8200,
        Level = LogLevel.Debug,
        Message = "Lawful basis validation started. RequestType={RequestType}")]
    internal static partial void ValidationStarted(this ILogger logger, Type requestType);

    /// <summary>Lawful basis validation passed successfully.</summary>
    [LoggerMessage(
        EventId = 8201,
        Level = LogLevel.Information,
        Message = "Lawful basis validation passed. RequestType={RequestType}, Basis={Basis}")]
    internal static partial void ValidationPassed(this ILogger logger, Type requestType, LawfulBasis basis);

    /// <summary>Lawful basis validation failed.</summary>
    [LoggerMessage(
        EventId = 8202,
        Level = LogLevel.Warning,
        Message = "Lawful basis validation failed. RequestType={RequestType}, Reason={Reason}")]
    internal static partial void ValidationFailed(this ILogger logger, Type requestType, string reason);

    // =====================================================
    // Consent checks (8203-8204)
    // =====================================================

    /// <summary>Consent status check started for consent-based processing.</summary>
    [LoggerMessage(
        EventId = 8203,
        Level = LogLevel.Debug,
        Message = "Consent check started. RequestType={RequestType}, SubjectId={SubjectId}")]
    internal static partial void ConsentCheckStarted(this ILogger logger, Type requestType, string subjectId);

    /// <summary>Consent check failed — no active consent or provider error.</summary>
    [LoggerMessage(
        EventId = 8204,
        Level = LogLevel.Warning,
        Message = "Consent check failed. RequestType={RequestType}, SubjectId={SubjectId}, Reason={Reason}")]
    internal static partial void ConsentCheckFailed(this ILogger logger, Type requestType, string subjectId, string reason);

    // =====================================================
    // LIA checks (8205-8206)
    // =====================================================

    /// <summary>Legitimate Interest Assessment check started.</summary>
    [LoggerMessage(
        EventId = 8205,
        Level = LogLevel.Debug,
        Message = "LIA check started. RequestType={RequestType}, LIAReference={LIAReference}")]
    internal static partial void LIACheckStarted(this ILogger logger, Type requestType, string liaReference);

    /// <summary>Legitimate Interest Assessment check failed — LIA not approved.</summary>
    [LoggerMessage(
        EventId = 8206,
        Level = LogLevel.Warning,
        Message = "LIA check failed. RequestType={RequestType}, LIAReference={LIAReference}, Reason={Reason}")]
    internal static partial void LIACheckFailed(this ILogger logger, Type requestType, string liaReference, string reason);

    // =====================================================
    // Attribute resolution (8207)
    // =====================================================

    /// <summary>[LawfulBasis] and [ProcessingActivity] declare different bases; using [LawfulBasis].</summary>
    [LoggerMessage(
        EventId = 8207,
        Level = LogLevel.Warning,
        Message = "Attribute conflict: [LawfulBasis] and [ProcessingActivity] have different bases. Using [LawfulBasis]. RequestType={RequestType}")]
    internal static partial void AttributeConflictDetected(this ILogger logger, Type requestType);

    // =====================================================
    // Enforcement-level events (8208-8209)
    // =====================================================

    /// <summary>No lawful basis declared for a request that processes personal data.</summary>
    [LoggerMessage(
        EventId = 8208,
        Level = LogLevel.Warning,
        Message = "No lawful basis declared for request that processes personal data. RequestType={RequestType}")]
    internal static partial void BasisNotDeclared(this ILogger logger, Type requestType);

    /// <summary>Consent-based processing declared but no <c>IConsentStatusProvider</c> is registered.</summary>
    [LoggerMessage(
        EventId = 8209,
        Level = LogLevel.Warning,
        Message = "Consent-based processing declared but no IConsentStatusProvider is registered. RequestType={RequestType}")]
    internal static partial void ProviderNotRegistered(this ILogger logger, Type requestType);

    // =====================================================
    // Additional pipeline events (8210, 8214-8216)
    // Note: 8211-8213 are reserved in GDPRLogMessages
    // for auto-registration and health checks.
    // =====================================================

    /// <summary>Validation skipped because request has no GDPR attributes.</summary>
    [LoggerMessage(
        EventId = 8210,
        Level = LogLevel.Trace,
        Message = "Lawful basis validation skipped (no GDPR attributes). RequestType={RequestType}")]
    internal static partial void ValidationSkipped(this ILogger logger, Type requestType);

    /// <summary>Consent check passed — active consent confirmed.</summary>
    [LoggerMessage(
        EventId = 8214,
        Level = LogLevel.Information,
        Message = "Consent check passed. RequestType={RequestType}, SubjectId={SubjectId}")]
    internal static partial void ConsentCheckPassed(this ILogger logger, Type requestType, string subjectId);

    /// <summary>LIA check passed — assessment approved.</summary>
    [LoggerMessage(
        EventId = 8215,
        Level = LogLevel.Information,
        Message = "LIA check passed. RequestType={RequestType}, LIAReference={LIAReference}")]
    internal static partial void LIACheckPassed(this ILogger logger, Type requestType, string liaReference);

    /// <summary>Lawful basis violation in warn mode — processing allowed despite failure.</summary>
    [LoggerMessage(
        EventId = 8216,
        Level = LogLevel.Warning,
        Message = "Lawful basis violation (warn mode — processing allowed). RequestType={RequestType}, Detail={Detail}")]
    internal static partial void EnforcementWarning(this ILogger logger, Type requestType, string detail);
}
