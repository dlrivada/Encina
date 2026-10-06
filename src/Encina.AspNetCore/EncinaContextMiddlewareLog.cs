using Microsoft.Extensions.Logging;

namespace Encina.AspNetCore;

/// <summary>
/// Log messages of <see cref="EncinaContextMiddleware"/>.
/// </summary>
/// <remarks>
/// Event IDs: 202-203 (see <c>EventIdRanges.AspNetCore</c>). No path, header or other request data
/// is logged.
/// </remarks>
internal static partial class EncinaContextMiddlewareLog
{
    [LoggerMessage(EventId = 202, Level = LogLevel.Critical,
        Message = "UseEncinaContext() runs before UseRouting(): a request reached a SignalR hub endpoint with no endpoint resolved before the middleware, so its connection skip was missed. Every later request is answered 500 until the pipeline order is fixed (UseRouting, then UseRewriter and the error handlers, then UseAuthentication, then UseEncinaContext).")]
    public static partial void EncinaContextBeforeRouting(ILogger logger);

    [LoggerMessage(EventId = 203, Level = LogLevel.Warning,
        Message = "A request was re-routed to a SignalR hub endpoint inside UseEncinaContext() (a rewrite or a re-execute registered after it); the request ran with a request identity. Register UseRewriter and the error handlers before UseEncinaContext().")]
    public static partial void EncinaContextPathChanged(ILogger logger);
}
