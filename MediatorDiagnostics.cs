using System.Diagnostics;

namespace SimpleMediator;

/// <summary>
/// Contiene la fuente de actividades utilizada por los behaviors de telemetría.
/// </summary>
internal static class MediatorDiagnostics
{
    internal static readonly ActivitySource ActivitySource = new("SimpleMediator", "1.0");
}
