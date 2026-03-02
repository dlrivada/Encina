namespace Encina.Compliance.Retention;

/// <summary>
/// Notification published when a legal hold has been applied to a data entity.
/// </summary>
/// <remarks>
/// <para>
/// Published when a legal hold (litigation hold) is placed on a data entity,
/// suspending any automatic deletion regardless of retention policy expiration.
/// Per GDPR Article 17(3)(e), erasure may be restricted when processing is necessary
/// for the establishment, exercise, or defence of legal claims.
/// </para>
/// <para>
/// Handlers implementing <c>INotificationHandler&lt;LegalHoldAppliedNotification&gt;</c>
/// can notify legal teams, update compliance dashboards, flag affected records
/// in downstream systems, or trigger review workflows.
/// </para>
/// </remarks>
/// <param name="HoldId">Unique identifier of the legal hold.</param>
/// <param name="EntityId">Identifier of the data entity placed under legal hold.</param>
/// <param name="Reason">Legal justification for applying the hold.</param>
/// <param name="AppliedAtUtc">Timestamp when the legal hold was applied (UTC).</param>
public sealed record LegalHoldAppliedNotification(
    string HoldId,
    string EntityId,
    string Reason,
    DateTimeOffset AppliedAtUtc) : INotification;
