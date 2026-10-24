using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Encina.Tenancy.AspNetCore.Diagnostics;

/// <summary>
/// High-performance log messages for ASP.NET Core tenant resolution.
/// </summary>
/// <remarks>
/// <para>
/// Event IDs: 3850-3853 (see <c>EventIdRanges.TenancyAspNetCore</c>, range 3850-3899).
/// </para>
/// <para>
/// No message carries a tenant value or an <c>EncinaError.Message</c>: only resolver type names
/// and configuration names are recorded.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
internal static partial class TenancyAspNetCoreLogMessages
{
    /// <summary>
    /// Logs that the tenant header was ignored because the peer is not a trusted proxy.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="headerName">The configured header name.</param>
    [LoggerMessage(
        EventId = 3850,
        Level = LogLevel.Debug,
        Message = "Tenant header {HeaderName} ignored: the request peer is not a trusted proxy")]
    public static partial void HeaderIgnoredUntrustedPeer(ILogger logger, string headerName);

    /// <summary>
    /// Logs that a client-supplied tenant disagrees with the verified tenant and the request is denied.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="authoritativeResolver">The type name of the resolver that supplied the verified tenant.</param>
    /// <param name="conflictingResolver">The type name of the resolver that supplied the different tenant.</param>
    [LoggerMessage(
        EventId = 3851,
        Level = LogLevel.Warning,
        Message = "Tenant mismatch: resolver {ConflictingResolver} disagrees with the verified tenant of resolver {AuthoritativeResolver}; request denied")]
    public static partial void TenantMismatch(ILogger logger, string authoritativeResolver, string conflictingResolver);

    /// <summary>
    /// Logs that the tenant header is honored from any peer, an explicit opt-out of the trusted-proxy check.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="headerName">The configured header name.</param>
    [LoggerMessage(
        EventId = 3852,
        Level = LogLevel.Warning,
        Message = "Tenant header {HeaderName} is honored from any peer (HeaderResolver.TrustAnyPeer is enabled): only safe behind network isolation")]
    public static partial void HeaderTrustAnyPeerEnabled(ILogger logger, string headerName);

    /// <summary>
    /// Logs that tenant resolution does not require a tenant, an explicit opt-out of the default.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    [LoggerMessage(
        EventId = 3853,
        Level = LogLevel.Warning,
        Message = "TenancyOptions.RequireTenant is disabled: requests without a resolved tenant proceed without tenant isolation")]
    public static partial void TenantNotRequired(ILogger logger);
}
