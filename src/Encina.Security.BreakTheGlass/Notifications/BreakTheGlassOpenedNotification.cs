namespace Encina.Security.BreakTheGlass;

/// <summary>
/// Published when a break-the-glass elevation is opened, so the application can alert the treating
/// professional or the data protection officer.
/// </summary>
/// <remarks>It carries no subject id, no user id and no justification.</remarks>
/// <param name="ElevationId">The elevation id.</param>
/// <param name="TenantId">The tenant, or <see langword="null"/>.</param>
/// <param name="Scope">The scope.</param>
/// <param name="OpenedAtUtc">The opening instant.</param>
/// <param name="ExpiresAtUtc">The expiry instant.</param>
public sealed record BreakTheGlassOpenedNotification(
    Guid ElevationId,
    string? TenantId,
    string Scope,
    DateTimeOffset OpenedAtUtc,
    DateTimeOffset ExpiresAtUtc) : INotification;
