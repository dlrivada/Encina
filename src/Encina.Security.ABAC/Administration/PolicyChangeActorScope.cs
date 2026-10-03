namespace Encina.Security.ABAC.Administration;

/// <summary>
/// An explicit, ambient system-actor scope that internal callers open so that a policy change
/// made without a request principal (the startup policy seeding) is allowed and recorded as
/// made by the system actor.
/// </summary>
/// <remarks>
/// The scope flows with the async execution context and is restored when disposed, so it never
/// leaks to unrelated callers. It is internal on purpose: application code cannot open it, so a
/// policy change without a principal stays refused everywhere else.
/// </remarks>
internal static class PolicyChangeActorScope
{
    private static readonly AsyncLocal<bool> SystemActor = new();

    /// <summary>Gets a value indicating whether a system-actor scope is open on this execution flow.</summary>
    internal static bool IsSystemActorActive => SystemActor.Value;

    /// <summary>Opens a system-actor scope; dispose the result to close it.</summary>
    /// <returns>The handle that restores the previous state when disposed.</returns>
    internal static IDisposable BeginSystemActor()
    {
        var previous = SystemActor.Value;
        SystemActor.Value = true;
        return new Restorer(previous);
    }

    private sealed class Restorer(bool previous) : IDisposable
    {
        public void Dispose() => SystemActor.Value = previous;
    }
}
