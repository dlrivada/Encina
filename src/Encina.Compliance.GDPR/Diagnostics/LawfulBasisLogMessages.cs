using Microsoft.Extensions.Logging;

namespace Encina.Compliance.GDPR.Diagnostics;

/// <summary>
/// High-performance structured log messages for lawful basis validation.
/// </summary>
/// <remarks>
/// <para>
/// Uses the <c>[LoggerMessage]</c> source generator for zero-allocation logging in hot paths.
/// Event IDs are allocated in the 8120-8133 block within the GDPR range
/// (see <c>EventIdRanges.ComplianceGDPR</c>).
/// </para>
/// <para>
/// Events 8113-8116 remain in <see cref="GDPRLogMessages"/> for auto-registration and health checks.
/// </para>
/// </remarks>
internal static partial class LawfulBasisLogMessages
{
    // =====================================================
    // Validation lifecycle (8120-8122)
    // =====================================================

    /// <summary>Lawful basis validation started for a request type.</summary>
    [LoggerMessage(
        EventId = 8120,
        Level = LogLevel.Debug,
        Message = "Lawful basis validation started. RequestType={RequestType}")]
    internal static partial void ValidationStarted(this ILogger logger, Type requestType);

    /// <summary>Lawful basis validation passed successfully.</summary>
    [LoggerMessage(
        EventId = 8121,
        Level = LogLevel.Information,
        Message = "Lawful basis validation passed. RequestType={RequestType}, Basis={Basis}")]
    internal static partial void ValidationPassed(this ILogger logger, Type requestType, LawfulBasis basis);

    /// <summary>Lawful basis validation failed.</summary>
    [LoggerMessage(
        EventId = 8122,
        Level = LogLevel.Warning,
        Message = "Lawful basis validation failed. RequestType={RequestType}, Reason={Reason}")]
    internal static partial void ValidationFailed(this ILogger logger, Type requestType, string reason);

    // =====================================================
    // Consent checks (8123-8124)
    // =====================================================

    /// <summary>Consent status check started for consent-based processing.</summary>
    [LoggerMessage(
        EventId = 8123,
        Level = LogLevel.Debug,
        Message = "Consent check started. RequestType={RequestType}, SubjectId={SubjectId}")]
    internal static partial void ConsentCheckStarted(this ILogger logger, Type requestType, string subjectId);

    /// <summary>Consent check failed — no active consent or provider error.</summary>
    [LoggerMessage(
        EventId = 8124,
        Level = LogLevel.Warning,
        Message = "Consent check failed. RequestType={RequestType}, SubjectId={SubjectId}, Reason={Reason}")]
    internal static partial void ConsentCheckFailed(this ILogger logger, Type requestType, string subjectId, string reason);

    // =====================================================
    // LIA checks (8125-8126)
    // =====================================================

    /// <summary>Legitimate Interest Assessment check started.</summary>
    [LoggerMessage(
        EventId = 8125,
        Level = LogLevel.Debug,
        Message = "LIA check started. RequestType={RequestType}, LIAReference={LIAReference}")]
    internal static partial void LIACheckStarted(this ILogger logger, Type requestType, string liaReference);

    /// <summary>Legitimate Interest Assessment check failed — LIA not approved.</summary>
    [LoggerMessage(
        EventId = 8126,
        Level = LogLevel.Warning,
        Message = "LIA check failed. RequestType={RequestType}, LIAReference={LIAReference}, Reason={Reason}")]
    internal static partial void LIACheckFailed(this ILogger logger, Type requestType, string liaReference, string reason);

    // =====================================================
    // Attribute resolution (8127)
    // =====================================================

    /// <summary>[LawfulBasis] and [ProcessingActivity] declare different bases; using [LawfulBasis].</summary>
    [LoggerMessage(
        EventId = 8127,
        Level = LogLevel.Warning,
        Message = "Attribute conflict: [LawfulBasis] and [ProcessingActivity] have different bases. Using [LawfulBasis]. RequestType={RequestType}")]
    internal static partial void AttributeConflictDetected(this ILogger logger, Type requestType);

    // =====================================================
    // Enforcement-level events (8128-8129)
    // =====================================================

    /// <summary>No lawful basis declared for a request that processes personal data.</summary>
    [LoggerMessage(
        EventId = 8128,
        Level = LogLevel.Warning,
        Message = "No lawful basis declared for request that processes personal data. RequestType={RequestType}")]
    internal static partial void BasisNotDeclared(this ILogger logger, Type requestType);

    /// <summary>Consent-based processing declared but no <c>IConsentStatusProvider</c> is registered.</summary>
    [LoggerMessage(
        EventId = 8129,
        Level = LogLevel.Warning,
        Message = "Consent-based processing declared but no IConsentStatusProvider is registered. RequestType={RequestType}")]
    internal static partial void ProviderNotRegistered(this ILogger logger, Type requestType);

    // =====================================================
    // Additional pipeline events (8130-8133)
    // =====================================================

    /// <summary>Validation skipped because request has no GDPR attributes.</summary>
    [LoggerMessage(
        EventId = 8130,
        Level = LogLevel.Trace,
        Message = "Lawful basis validation skipped (no GDPR attributes). RequestType={RequestType}")]
    internal static partial void ValidationSkipped(this ILogger logger, Type requestType);

    /// <summary>Consent check passed — active consent confirmed.</summary>
    [LoggerMessage(
        EventId = 8131,
        Level = LogLevel.Information,
        Message = "Consent check passed. RequestType={RequestType}, SubjectId={SubjectId}")]
    internal static partial void ConsentCheckPassed(this ILogger logger, Type requestType, string subjectId);

    /// <summary>LIA check passed — assessment approved.</summary>
    [LoggerMessage(
        EventId = 8132,
        Level = LogLevel.Information,
        Message = "LIA check passed. RequestType={RequestType}, LIAReference={LIAReference}")]
    internal static partial void LIACheckPassed(this ILogger logger, Type requestType, string liaReference);

    /// <summary>Lawful basis violation in warn mode — processing allowed despite failure.</summary>
    [LoggerMessage(
        EventId = 8133,
        Level = LogLevel.Warning,
        Message = "Lawful basis violation (warn mode — processing allowed). RequestType={RequestType}, Detail={Detail}")]
    internal static partial void EnforcementWarning(this ILogger logger, Type requestType, string detail);
}
