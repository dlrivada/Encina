namespace Encina.Security.BreakTheGlass;

/// <summary>
/// One break-the-glass elevation: a user's justified, time-boxed access to one data subject's data
/// within one named scope.
/// </summary>
/// <remarks>
/// <para>
/// The record holds the subject id and the free-text justification because the reviewer must see
/// whose data was opened and why. Both are personal data: <see cref="ToString"/> omits them and
/// nothing in this package copies them to logs, activity tags, metrics, notifications or audit
/// metadata.
/// </para>
/// </remarks>
public sealed record BreakTheGlassElevation
{
    /// <summary>Gets the id of the elevation.</summary>
    public required Guid ElevationId { get; init; }

    /// <summary>Gets the tenant the elevation belongs to, or <see langword="null"/> when tenancy is off.</summary>
    public string? TenantId { get; init; }

    /// <summary>Gets the id of the user who opened the elevation and exercises it.</summary>
    public required string UserId { get; init; }

    /// <summary>Gets the id of the data subject whose data the elevation opens.</summary>
    public required string SubjectId { get; init; }

    /// <summary>Gets the named scope the elevation applies to.</summary>
    public required string Scope { get; init; }

    /// <summary>Gets the justification the opener gave (may contain health data).</summary>
    public required string Justification { get; init; }

    /// <summary>Gets the instant (UTC) the elevation was opened.</summary>
    public required DateTimeOffset OpenedAtUtc { get; init; }

    /// <summary>Gets the instant (UTC) the elevation stops working.</summary>
    public required DateTimeOffset ExpiresAtUtc { get; init; }

    /// <summary>Gets the lifecycle status.</summary>
    public BreakTheGlassStatus Status { get; init; } = BreakTheGlassStatus.Active;

    /// <summary>Gets the instant (UTC) the elevation was closed, or <see langword="null"/> while active.</summary>
    public DateTimeOffset? ClosedAtUtc { get; init; }

    /// <summary>Gets the fixed reason code the elevation was closed with, or <see langword="null"/> while active.</summary>
    public string? CloseReason { get; init; }

    /// <summary>Gets the instant (UTC) the expiry was recorded in the audit trail, or <see langword="null"/> when not yet recorded.</summary>
    public DateTimeOffset? ExpiryRecordedAtUtc { get; init; }

    /// <summary>Gets the review outcome, or <see langword="null"/> while unreviewed.</summary>
    public BreakTheGlassReviewOutcome? ReviewOutcome { get; init; }

    /// <summary>Gets the user id of the reviewer, or <see langword="null"/> while unreviewed.</summary>
    public string? ReviewedByUserId { get; init; }

    /// <summary>Gets the instant (UTC) of the review, or <see langword="null"/> while unreviewed.</summary>
    public DateTimeOffset? ReviewedAtUtc { get; init; }

    /// <summary>Gets the reviewer's note, or <see langword="null"/>.</summary>
    public string? ReviewNote { get; init; }

    /// <summary>
    /// Determines whether the elevation grants access at the given instant.
    /// </summary>
    /// <param name="nowUtc">The instant to check.</param>
    /// <returns><see langword="true"/> when the status is <see cref="BreakTheGlassStatus.Active"/> and <paramref name="nowUtc"/> is before <see cref="ExpiresAtUtc"/>.</returns>
    public bool IsEffectiveAt(DateTimeOffset nowUtc) => Status == BreakTheGlassStatus.Active && nowUtc < ExpiresAtUtc;

    /// <summary>
    /// Returns a description that omits the user id, the subject id, the justification and the review note.
    /// </summary>
    /// <returns>The elevation id, scope, status and expiry.</returns>
    public override string ToString() =>
        $"BreakTheGlassElevation {{ ElevationId = {ElevationId}, Scope = {Scope}, Status = {Status}, ExpiresAtUtc = {ExpiresAtUtc:O} }}";
}
