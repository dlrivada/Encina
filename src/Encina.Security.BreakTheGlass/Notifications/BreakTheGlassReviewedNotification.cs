namespace Encina.Security.BreakTheGlass;

/// <summary>
/// Published when a break-the-glass elevation is reviewed, so the application (or a breach
/// notification flow) can react to an unjustified access.
/// </summary>
/// <remarks>It carries no subject id, no user id, no justification and no review note.</remarks>
/// <param name="ElevationId">The elevation id.</param>
/// <param name="TenantId">The tenant, or <see langword="null"/>.</param>
/// <param name="Scope">The scope.</param>
/// <param name="Outcome">The ruling.</param>
/// <param name="ReviewedAtUtc">The review instant.</param>
public sealed record BreakTheGlassReviewedNotification(
    Guid ElevationId,
    string? TenantId,
    string Scope,
    BreakTheGlassReviewOutcome Outcome,
    DateTimeOffset ReviewedAtUtc) : INotification;
