using System.Diagnostics.Metrics;

namespace Encina.Security.BreakTheGlass.Diagnostics;

/// <summary>
/// The counters the gate and the background services emit.
/// </summary>
/// <remarks>
/// Tags are the scope name, the denying gate and the failing operation only: never a tenant, user,
/// subject or elevation id (SPEC-002 REQ-062).
/// </remarks>
internal static class BreakTheGlassDiagnostics
{
    /// <summary>The meter name.</summary>
    public const string MeterName = "Encina.Security.BreakTheGlass";

    /// <summary>Tag: the named scope of the elevation.</summary>
    public const string TagScope = "encina.break_glass.scope";

    /// <summary>Tag: the gate that denied the request.</summary>
    public const string TagGate = "encina.break_glass.gate";

    /// <summary>Tag: the operation whose store call failed.</summary>
    public const string TagOperation = "encina.break_glass.operation";

    /// <summary>Operation tag value: the elevation lookup of the gate.</summary>
    public const string OperationAccessLookup = "access_lookup";

    /// <summary>Operation tag value: the expiry sweep.</summary>
    public const string OperationExpirySweep = "expiry_sweep";

    /// <summary>Operation tag value: the purge.</summary>
    public const string OperationPurge = "purge";

    private static readonly Meter Meter = new(MeterName);

    /// <summary>Requests that an elevation let through (<c>encina.break_glass.accesses</c>).</summary>
    public static readonly Counter<long> Accesses = Meter.CreateCounter<long>(
        "encina.break_glass.accesses", description: "Requests denied by a gate and let through by a break-the-glass elevation.");

    /// <summary>Store failures of the gate lookup and of the background services (<c>encina.break_glass.store_failures</c>).</summary>
    public static readonly Counter<long> StoreFailures = Meter.CreateCounter<long>(
        "encina.break_glass.store_failures", description: "Break-the-glass store calls that failed, by operation.");

    /// <summary>Records one granted access.</summary>
    public static void RecordAccess(string scope, string gate) =>
        Accesses.Add(1, new KeyValuePair<string, object?>(TagScope, scope), new KeyValuePair<string, object?>(TagGate, gate));

    /// <summary>Records one failed store call.</summary>
    public static void RecordStoreFailure(string operation) =>
        StoreFailures.Add(1, new KeyValuePair<string, object?>(TagOperation, operation));
}
