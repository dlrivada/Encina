namespace Encina.UnitTests.Security.ABAC;

/// <summary>
/// Serializes the tests that register an <see cref="System.Diagnostics.ActivityListener"/> on the
/// process-global ABAC <see cref="System.Diagnostics.ActivitySource"/> with the "without listeners"
/// assertion in <c>ABACDiagnosticsTests</c>. A listener registered by a class running in parallel
/// made <c>StartEvaluation_NoListeners_ReturnsNull</c> fail intermittently (#1858).
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ABACActivityListenerIsolation
{
    /// <summary>The xUnit collection name.</summary>
    public const string Name = "ABAC-ActivityListenerIsolation";
}
