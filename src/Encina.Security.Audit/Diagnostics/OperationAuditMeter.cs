using System.Diagnostics.Metrics;

namespace Encina.Security.Audit.Diagnostics;

/// <summary>
/// Provides the meter and instruments for operation audit metrics.
/// </summary>
/// <remarks>
/// A dedicated <see cref="Meter"/> (<c>Encina.OperationAudit</c>); counters use tag dimensions (<c>error_code</c>,
/// <c>request_type</c>) and never carry a message or payload. Register the meter name with OpenTelemetry to export it.
/// </remarks>
internal static class OperationAuditMeter
{
    /// <summary>The meter name for external registration with OpenTelemetry.</summary>
    internal const string MeterName = "Encina.OperationAudit";

    /// <summary>The meter version.</summary>
    internal const string MeterVersion = "1.0";

    /// <summary>The error code tag.</summary>
    internal const string TagErrorCode = "error_code";

    /// <summary>The audited request type tag.</summary>
    internal const string TagRequestType = "request_type";

    internal static readonly Meter Meter = new(MeterName, MeterVersion);

    /// <summary>
    /// Total number of audit entries not built because the payload could not be masked, tagged with
    /// <c>error_code</c> and <c>request_type</c>; counted in both failure modes.
    /// </summary>
    internal static readonly Counter<long> MaskingFailuresTotal =
        Meter.CreateCounter<long>("operation_audit.masking_failures.total",
            description: "Total number of audit entries not built because the payload could not be masked.");
}
