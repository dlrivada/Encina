namespace Encina;

/// <summary>
/// The grant an <see cref="IAccessElevationGate"/> returns when an emergency elevation lets a denied
/// request continue.
/// </summary>
/// <remarks>
/// The record carries no subject id and no justification, so it is safe to log and to tag.
/// </remarks>
/// <param name="ElevationId">The id of the elevation that allowed the request.</param>
/// <param name="Scope">The named scope the elevation was opened for.</param>
/// <param name="ExpiresAtUtc">The instant (UTC) at which the elevation stops working.</param>
public sealed record AccessElevation(Guid ElevationId, string Scope, DateTimeOffset ExpiresAtUtc);
