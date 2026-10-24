namespace Encina.Security.BreakTheGlass;

/// <summary>
/// A request to open a break-the-glass elevation.
/// </summary>
/// <param name="SubjectId">The data subject whose data the caller needs.</param>
/// <param name="Scope">The named scope to open.</param>
/// <param name="Justification">Why the access is needed now (may contain health data).</param>
/// <param name="Duration">How long the elevation should last.</param>
public sealed record BreakTheGlassRequest(string SubjectId, string Scope, string Justification, TimeSpan Duration)
{
    /// <summary>
    /// Returns a description that omits the subject id and the justification.
    /// </summary>
    /// <returns>The scope and the duration.</returns>
    public override string ToString() => $"BreakTheGlassRequest {{ Scope = {Scope}, Duration = {Duration} }}";
}
