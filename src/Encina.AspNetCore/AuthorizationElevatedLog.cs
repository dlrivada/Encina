using Microsoft.Extensions.Logging;

namespace Encina.AspNetCore;

/// <summary>
/// Log message of <see cref="AuthorizationPipelineBehavior{TRequest, TResponse}"/> for an elevation.
/// </summary>
/// <remarks>
/// Event IDs: 204 (see <c>EventIdRanges.AspNetCore</c>). The message records the request type, the
/// identity kind and the elevation id only: never the user id, a subject id or an error message.
/// </remarks>
internal static partial class AuthorizationElevatedLog
{
    [LoggerMessage(EventId = 204, Level = LogLevel.Warning,
        Message = "Authorization denial for {RequestType} was overridden by an emergency elevation. IdentityKind: {IdentityKind}, ElevationId: {ElevationId}")]
    public static partial void AuthorizationElevated(ILogger logger, string requestType, IdentityKind identityKind, Guid elevationId);
}
