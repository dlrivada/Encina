using System.Diagnostics;

namespace SimpleMediator;

/// <summary>
/// Provides the activity source consumed by telemetry-oriented behaviors.
/// </summary>
internal static class MediatorDiagnostics
{
    internal static readonly ActivitySource ActivitySource = new("SimpleMediator", "1.0");
}
