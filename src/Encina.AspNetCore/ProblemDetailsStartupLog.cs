using Microsoft.Extensions.Logging;

namespace Encina.AspNetCore;

/// <summary>
/// Startup log messages about <see cref="EncinaAspNetCoreOptions.IncludeErrorMessageInProblemDetails"/>.
/// </summary>
/// <remarks>
/// Event IDs: 204-205 (see <c>EventIdRanges.AspNetCore</c>). No error message or exception text is logged.
/// </remarks>
internal static partial class ProblemDetailsStartupLog
{
    [LoggerMessage(EventId = 204, Level = LogLevel.Warning,
        Message = "EncinaAspNetCoreOptions.IncludeErrorMessageInProblemDetails is true outside the Development environment; the option is ignored and Problem Details responses do not carry error messages or exception details.")]
    public static partial void ErrorMessageOptionIgnored(ILogger logger);

    [LoggerMessage(EventId = 205, Level = LogLevel.Warning,
        Message = "EncinaAspNetCoreOptions.IncludeErrorMessageInProblemDetails is honoured in the Development environment; Problem Details responses expose error messages and exception details to clients.")]
    public static partial void ErrorMessageOptionHonoured(ILogger logger);
}
