using Microsoft.Extensions.Logging;

namespace Encina.Cdc.MySql.Diagnostics;

/// <summary>
/// High-performance logging methods for the MySQL CDC connector.
/// </summary>
/// <remarks>Event IDs: 5400-5449 (see EventIdRanges.CdcMySql).</remarks>
internal static partial class MySqlCdcLog
{
    /// <summary>Logs, once per options instance, that local endpoints are allowed.</summary>
    [LoggerMessage(
        EventId = 5400,
        Level = LogLevel.Warning,
        Message = "MySqlCdcOptions '{OptionsName}' relaxes endpoint validation (AllowLocalEndpoints=true); use only for local development")]
    public static partial void EndpointValidationRelaxed(ILogger logger, string optionsName);
}
