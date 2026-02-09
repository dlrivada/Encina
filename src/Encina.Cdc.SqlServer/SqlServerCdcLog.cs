using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Encina.Cdc.SqlServer;

/// <summary>
/// High-performance logging methods using LoggerMessage source generators
/// for SQL Server CDC provider.
/// </summary>
[ExcludeFromCodeCoverage]
internal static partial class SqlServerCdcLog
{
    /// <summary>Logs when the Change Tracking version is below the minimum valid version.</summary>
    [LoggerMessage(
        EventId = 200,
        Level = LogLevel.Warning,
        Message = "Change Tracking version {Version} is below minimum valid version {MinVersion} for {Schema}.{Table}. Resetting to minimum")]
    public static partial void VersionBelowMinimum(
        ILogger logger,
        long version,
        long minVersion,
        string schema,
        string table);
}
