namespace Encina.Compliance.Retention.Model;

/// <summary>
/// Represents a legal hold (litigation hold) that suspends data deletion for a specific entity.
/// </summary>
/// <remarks>
/// <para>
/// A legal hold prevents the deletion of data that may be relevant to ongoing or
/// anticipated litigation, regulatory investigations, or audits. When a legal hold
/// is active, the retention enforcement process must skip the affected data regardless
/// of whether the retention period has expired.
/// </para>
/// <para>
/// Per GDPR Article 17(3)(e), the right to erasure does not apply to the extent that
/// processing is necessary for the establishment, exercise, or defence of legal claims.
/// Legal holds implement this exemption in a controlled, auditable manner.
/// </para>
/// <para>
/// Legal holds are applied by authorized users (e.g., legal counsel) and must be explicitly
/// released when no longer required. The application and release of holds are recorded in
/// the retention audit trail for compliance evidence.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var hold = LegalHold.Create(
///     entityId: "invoice-12345",
///     reason: "Pending tax audit for fiscal year 2024",
///     appliedByUserId: "legal-counsel@company.com");
///
/// await legalHoldManager.ApplyHoldAsync(hold.EntityId, hold, cancellationToken);
/// </code>
/// </example>
public sealed record LegalHold
{
    /// <summary>
    /// Unique identifier for this legal hold.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Identifier of the data entity protected by this hold.
    /// </summary>
    /// <remarks>
    /// Corresponds to the <see cref="RetentionRecord.EntityId"/> of the retention records
    /// that should be protected from deletion.
    /// </remarks>
    public required string EntityId { get; init; }

    /// <summary>
    /// Human-readable reason for applying the legal hold.
    /// </summary>
    /// <remarks>
    /// Should describe the litigation, investigation, or audit requiring the hold.
    /// Examples: "Pending tax audit for fiscal year 2024",
    /// "Litigation: Smith v. Company (Case #2024-456)".
    /// </remarks>
    public required string Reason { get; init; }

    /// <summary>
    /// Identifier of the user who applied the legal hold.
    /// </summary>
    /// <remarks>
    /// <c>null</c> for system-applied holds (e.g., automated regulatory compliance).
    /// </remarks>
    public string? AppliedByUserId { get; init; }

    /// <summary>
    /// Timestamp when the legal hold was applied (UTC).
    /// </summary>
    public required DateTimeOffset AppliedAtUtc { get; init; }

    /// <summary>
    /// Timestamp when the legal hold was released (UTC).
    /// </summary>
    /// <remarks>
    /// <c>null</c> while the hold is still active. Set when the hold is explicitly
    /// released by an authorized user after the legal matter is resolved.
    /// </remarks>
    public DateTimeOffset? ReleasedAtUtc { get; init; }

    /// <summary>
    /// Identifier of the user who released the legal hold.
    /// </summary>
    /// <remarks>
    /// <c>null</c> while the hold is active, or for system-released holds.
    /// </remarks>
    public string? ReleasedByUserId { get; init; }

    /// <summary>
    /// Indicates whether the legal hold is currently active.
    /// </summary>
    /// <remarks>
    /// A hold is active when <see cref="ReleasedAtUtc"/> is <c>null</c> (has not been released).
    /// Active holds prevent deletion of the associated data entity.
    /// </remarks>
    public bool IsActive => ReleasedAtUtc is null;

    /// <summary>
    /// Creates a new legal hold with a generated unique identifier and the current UTC timestamp.
    /// </summary>
    /// <param name="entityId">Identifier of the data entity to protect.</param>
    /// <param name="reason">Reason for applying the hold.</param>
    /// <param name="appliedByUserId">Identifier of the user applying the hold.</param>
    /// <returns>A new active <see cref="LegalHold"/> with a generated GUID identifier.</returns>
    public static LegalHold Create(
        string entityId,
        string reason,
        string? appliedByUserId = null) =>
        new()
        {
            Id = Guid.NewGuid().ToString("N"),
            EntityId = entityId,
            Reason = reason,
            AppliedByUserId = appliedByUserId,
            AppliedAtUtc = DateTimeOffset.UtcNow
        };
}
