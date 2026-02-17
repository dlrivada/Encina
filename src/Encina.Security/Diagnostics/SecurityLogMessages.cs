using Microsoft.Extensions.Logging;

namespace Encina.Security.Diagnostics;

/// <summary>
/// High-performance structured log messages for the security pipeline.
/// </summary>
/// <remarks>
/// <para>
/// Uses <c>LoggerMessage.Define</c> to avoid boxing and string formatting overhead
/// in hot paths. All methods are extension methods on <see cref="ILogger"/> for ergonomic use.
/// </para>
/// <para>
/// Event IDs are allocated in the 8000–8099 range to avoid collisions with other
/// Encina subsystems.
/// </para>
/// </remarks>
internal static class SecurityLogMessages
{
    // -- 8000: Authorization started --

    private static readonly Action<ILogger, string, int, Exception?> AuthorizationStartedDef =
        LoggerMessage.Define<string, int>(
            LogLevel.Debug,
            new EventId(8000, nameof(AuthorizationStarted)),
            "Security authorization started. RequestType={RequestType}, AttributeCount={AttributeCount}");

    internal static void AuthorizationStarted(this ILogger logger, string requestType, int attributeCount)
        => AuthorizationStartedDef(logger, requestType, attributeCount, null);

    // -- 8001: Authorization allowed --

    private static readonly Action<ILogger, string?, string, Exception?> AuthorizationAllowedDef =
        LoggerMessage.Define<string?, string>(
            LogLevel.Information,
            new EventId(8001, nameof(AuthorizationAllowed)),
            "Security authorization allowed. UserId={UserId}, RequestType={RequestType}");

    internal static void AuthorizationAllowed(this ILogger logger, string? userId, string requestType)
        => AuthorizationAllowedDef(logger, userId, requestType, null);

    // -- 8002: Authorization denied --

    private static readonly Action<ILogger, string?, string, string, Exception?> AuthorizationDeniedDef =
        LoggerMessage.Define<string?, string, string>(
            LogLevel.Warning,
            new EventId(8002, nameof(AuthorizationDenied)),
            "Security authorization denied. UserId={UserId}, RequestType={RequestType}, DenialReason={DenialReason}");

    internal static void AuthorizationDenied(this ILogger logger, string? userId, string requestType, string denialReason)
        => AuthorizationDeniedDef(logger, userId, requestType, denialReason, null);

    // -- 8003: Allow anonymous bypass --

    private static readonly Action<ILogger, string, Exception?> AllowAnonymousBypassDef =
        LoggerMessage.Define<string>(
            LogLevel.Debug,
            new EventId(8003, nameof(AllowAnonymousBypass)),
            "Security bypassed via [AllowAnonymous]. RequestType={RequestType}");

    internal static void AllowAnonymousBypass(this ILogger logger, string requestType)
        => AllowAnonymousBypassDef(logger, requestType, null);

    // -- 8004: Missing security context --

    private static readonly Action<ILogger, string, Exception?> MissingSecurityContextDef =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(8004, nameof(MissingSecurityContext)),
            "Security context not available. RequestType={RequestType}");

    internal static void MissingSecurityContext(this ILogger logger, string requestType)
        => MissingSecurityContextDef(logger, requestType, null);
}
