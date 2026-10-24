namespace Encina.Security.BreakTheGlass;

/// <summary>
/// The ruling a second person gives when reviewing a break-the-glass elevation.
/// </summary>
/// <remarks>
/// The numeric values are persisted by the provider stores and must not change.
/// </remarks>
public enum BreakTheGlassReviewOutcome
{
    /// <summary>The reviewer ruled the emergency access justified.</summary>
    Justified = 0,

    /// <summary>The reviewer ruled the emergency access unjustified; an active elevation is revoked.</summary>
    Unjustified = 1
}
