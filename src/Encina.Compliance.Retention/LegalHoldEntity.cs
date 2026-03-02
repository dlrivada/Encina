namespace Encina.Compliance.Retention;

/// <summary>
/// Persistence entity for <see cref="Model.LegalHold"/>.
/// </summary>
/// <remarks>
/// <para>
/// This entity provides a database-agnostic representation of a legal hold,
/// using primitive types suitable for any storage provider (ADO.NET, Dapper, EF Core, MongoDB).
/// </para>
/// <para>
/// The <see cref="Model.LegalHold.IsActive"/> computed property is not stored in the entity;
/// it is derived in the domain model from <see cref="ReleasedAtUtc"/> being <c>null</c>.
/// </para>
/// <para>
/// Use <see cref="LegalHoldMapper"/> to convert between this entity and
/// <see cref="Model.LegalHold"/>.
/// </para>
/// </remarks>
public sealed class LegalHoldEntity
{
    /// <summary>
    /// Unique identifier for this legal hold.
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Identifier of the data entity protected by this hold.
    /// </summary>
    /// <remarks>
    /// An INDEX should be created on this column for efficient entity lookups.
    /// </remarks>
    public required string EntityId { get; set; }

    /// <summary>
    /// Human-readable reason for applying the legal hold.
    /// </summary>
    public required string Reason { get; set; }

    /// <summary>
    /// Identifier of the user who applied the legal hold.
    /// </summary>
    public string? AppliedByUserId { get; set; }

    /// <summary>
    /// Timestamp when the legal hold was applied (UTC).
    /// </summary>
    public DateTimeOffset AppliedAtUtc { get; set; }

    /// <summary>
    /// Timestamp when the legal hold was released (UTC), if applicable.
    /// </summary>
    /// <remarks>
    /// <c>null</c> while the hold is still active. Active holds are identified by
    /// filtering on <c>ReleasedAtUtc IS NULL</c>.
    /// </remarks>
    public DateTimeOffset? ReleasedAtUtc { get; set; }

    /// <summary>
    /// Identifier of the user who released the legal hold, if applicable.
    /// </summary>
    public string? ReleasedByUserId { get; set; }
}
