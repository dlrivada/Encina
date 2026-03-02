namespace Encina.Compliance.Retention;

/// <summary>
/// Notification published when a legal hold has been released from a data entity.
/// </summary>
/// <remarks>
/// <para>
/// Published when a legal hold (litigation hold) is lifted, re-enabling automatic
/// deletion by the retention enforcement process if the data entity's retention
/// period has expired. The data entity will be evaluated in the next enforcement
/// cycle and deleted if eligible.
/// </para>
/// <para>
/// Handlers implementing <c>INotificationHandler&lt;LegalHoldReleasedNotification&gt;</c>
/// can notify legal teams, update compliance dashboards, or trigger immediate
/// retention evaluation for the affected entity.
/// </para>
/// </remarks>
/// <param name="HoldId">Unique identifier of the released legal hold.</param>
/// <param name="EntityId">Identifier of the data entity released from legal hold.</param>
/// <param name="ReleasedAtUtc">Timestamp when the legal hold was released (UTC).</param>
public sealed record LegalHoldReleasedNotification(
    string HoldId,
    string EntityId,
    DateTimeOffset ReleasedAtUtc) : INotification;
