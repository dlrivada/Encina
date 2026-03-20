using Microsoft.Extensions.Logging;

using GDPR = Encina.Compliance.GDPR;

namespace Encina.Compliance.LawfulBasis.Diagnostics;

/// <summary>
/// High-performance structured log messages for the lawful basis compliance module.
/// </summary>
/// <remarks>
/// <para>
/// Uses the <c>[LoggerMessage]</c> source generator for zero-allocation logging in hot paths.
/// Event IDs are allocated in the 8350-8399 range to avoid collisions with other
/// Encina subsystems (GDPR uses 8100-8199, Consent uses 8200-8259, DSR uses 8300-8349,
/// Anonymization uses 8400-8499, Retention uses 8500-8599, CrossBorderTransfer uses 9300-9359).
/// </para>
/// </remarks>
internal static partial class LawfulBasisLogMessages
{
    // =====================================================
    // Validation lifecycle (8350-8352)
    // =====================================================

    /// <summary>Lawful basis validation started for a request type.</summary>
    [LoggerMessage(
        EventId = 8350,
        Level = LogLevel.Debug,
        Message = "Lawful basis validation started. RequestType={RequestType}")]
    internal static partial void ValidationStarted(this ILogger logger, Type requestType);

    /// <summary>Lawful basis validation passed successfully.</summary>
    [LoggerMessage(
        EventId = 8351,
        Level = LogLevel.Information,
        Message = "Lawful basis validation passed. RequestType={RequestType}, Basis={Basis}")]
    internal static partial void ValidationPassed(this ILogger logger, Type requestType, GDPR.LawfulBasis basis);

    /// <summary>Lawful basis validation failed.</summary>
    [LoggerMessage(
        EventId = 8352,
        Level = LogLevel.Warning,
        Message = "Lawful basis validation failed. RequestType={RequestType}, Reason={Reason}")]
    internal static partial void ValidationFailed(this ILogger logger, Type requestType, string reason);

    // =====================================================
    // Consent checks (8353-8355)
    // =====================================================

    /// <summary>Consent status check started for consent-based processing.</summary>
    [LoggerMessage(
        EventId = 8353,
        Level = LogLevel.Debug,
        Message = "Consent check started. RequestType={RequestType}, SubjectId={SubjectId}")]
    internal static partial void ConsentCheckStarted(this ILogger logger, Type requestType, string subjectId);

    /// <summary>Consent check passed — active consent confirmed.</summary>
    [LoggerMessage(
        EventId = 8354,
        Level = LogLevel.Information,
        Message = "Consent check passed. RequestType={RequestType}, SubjectId={SubjectId}")]
    internal static partial void ConsentCheckPassed(this ILogger logger, Type requestType, string subjectId);

    /// <summary>Consent check failed — no active consent or provider error.</summary>
    [LoggerMessage(
        EventId = 8355,
        Level = LogLevel.Warning,
        Message = "Consent check failed. RequestType={RequestType}, SubjectId={SubjectId}, Reason={Reason}")]
    internal static partial void ConsentCheckFailed(this ILogger logger, Type requestType, string subjectId, string reason);

    // =====================================================
    // LIA checks (8356-8358)
    // =====================================================

    /// <summary>Legitimate Interest Assessment check started.</summary>
    [LoggerMessage(
        EventId = 8356,
        Level = LogLevel.Debug,
        Message = "LIA check started. RequestType={RequestType}, LIAReference={LIAReference}")]
    internal static partial void LIACheckStarted(this ILogger logger, Type requestType, string liaReference);

    /// <summary>LIA check passed — assessment approved.</summary>
    [LoggerMessage(
        EventId = 8357,
        Level = LogLevel.Information,
        Message = "LIA check passed. RequestType={RequestType}, LIAReference={LIAReference}")]
    internal static partial void LIACheckPassed(this ILogger logger, Type requestType, string liaReference);

    /// <summary>LIA check failed — assessment not approved.</summary>
    [LoggerMessage(
        EventId = 8358,
        Level = LogLevel.Warning,
        Message = "LIA check failed. RequestType={RequestType}, LIAReference={LIAReference}, Reason={Reason}")]
    internal static partial void LIACheckFailed(this ILogger logger, Type requestType, string liaReference, string reason);

    // =====================================================
    // Attribute resolution (8359)
    // =====================================================

    /// <summary>[LawfulBasis] and [ProcessingActivity] declare different bases; using [LawfulBasis].</summary>
    [LoggerMessage(
        EventId = 8359,
        Level = LogLevel.Warning,
        Message = "Attribute conflict: [LawfulBasis] and [ProcessingActivity] have different bases. Using [LawfulBasis]. RequestType={RequestType}")]
    internal static partial void AttributeConflictDetected(this ILogger logger, Type requestType);

    // =====================================================
    // Enforcement-level events (8360-8362)
    // =====================================================

    /// <summary>No lawful basis declared for a request that processes personal data.</summary>
    [LoggerMessage(
        EventId = 8360,
        Level = LogLevel.Warning,
        Message = "No lawful basis declared for request that processes personal data. RequestType={RequestType}")]
    internal static partial void BasisNotDeclared(this ILogger logger, Type requestType);

    /// <summary>Consent-based processing declared but no <c>IConsentStatusProvider</c> is registered.</summary>
    [LoggerMessage(
        EventId = 8361,
        Level = LogLevel.Warning,
        Message = "Consent-based processing declared but no IConsentStatusProvider is registered. RequestType={RequestType}")]
    internal static partial void ProviderNotRegistered(this ILogger logger, Type requestType);

    /// <summary>Lawful basis violation in warn mode — processing allowed despite failure.</summary>
    [LoggerMessage(
        EventId = 8362,
        Level = LogLevel.Warning,
        Message = "Lawful basis violation (warn mode — processing allowed). RequestType={RequestType}, Detail={Detail}")]
    internal static partial void EnforcementWarning(this ILogger logger, Type requestType, string detail);

    // =====================================================
    // Validation skip (8363)
    // =====================================================

    /// <summary>Validation skipped because request has no GDPR attributes.</summary>
    [LoggerMessage(
        EventId = 8363,
        Level = LogLevel.Trace,
        Message = "Lawful basis validation skipped (no GDPR attributes). RequestType={RequestType}")]
    internal static partial void ValidationSkipped(this ILogger logger, Type requestType);

    // =====================================================
    // Auto-registration (8370-8371)
    // =====================================================

    /// <summary>Lawful basis auto-registration completed.</summary>
    [LoggerMessage(
        EventId = 8370,
        Level = LogLevel.Information,
        Message = "Lawful basis auto-registration completed. TotalRegistered={TotalRegistered}, AssembliesScanned={AssembliesScanned}, DefaultBasesApplied={DefaultBasesApplied}")]
    internal static partial void LawfulBasisAutoRegistrationCompleted(
        this ILogger logger, int totalRegistered, int assembliesScanned, int defaultBasesApplied);

    /// <summary>Individual lawful basis auto-registration for a request type.</summary>
    [LoggerMessage(
        EventId = 8371,
        Level = LogLevel.Debug,
        Message = "Lawful basis auto-registered. RequestType={RequestType}, Basis={Basis}")]
    internal static partial void LawfulBasisAutoRegistered(
        this ILogger logger, string requestType, GDPR.LawfulBasis basis);

    // =====================================================
    // Health check (8380)
    // =====================================================

    /// <summary>Lawful basis health check completed.</summary>
    [LoggerMessage(
        EventId = 8380,
        Level = LogLevel.Debug,
        Message = "Lawful basis health check completed. Status={Status}, RegistrationsCount={RegistrationsCount}")]
    internal static partial void LawfulBasisHealthCheckCompleted(
        this ILogger logger, string status, int registrationsCount);

    // =====================================================
    // Service operations (8385-8389)
    // =====================================================

    /// <summary>Service store operation failed.</summary>
    [LoggerMessage(
        EventId = 8385,
        Level = LogLevel.Error,
        Message = "Lawful basis store operation failed. Operation={Operation}")]
    internal static partial void StoreOperationFailed(
        this ILogger logger, string operation, Exception? exception = null);

    /// <summary>Invalid state transition in aggregate.</summary>
    [LoggerMessage(
        EventId = 8386,
        Level = LogLevel.Warning,
        Message = "Invalid state transition in lawful basis aggregate. Operation={Operation}, Detail={Detail}")]
    internal static partial void InvalidStateTransition(
        this ILogger logger, string operation, string detail);
}
