namespace Encina.Compliance.Retention;

/// <summary>
/// Notification published when a data record is approaching its retention expiration date.
/// </summary>
/// <remarks>
/// <para>
/// Published during retention enforcement when a record's expiration date is within
/// the configured alert window (<c>RetentionOptions.AlertBeforeExpirationDays</c>).
/// Per GDPR Recital 39, appropriate measures should be taken to ensure that personal
/// data are not kept longer than necessary.
/// </para>
/// <para>
/// Handlers implementing <c>INotificationHandler&lt;DataExpiringNotification&gt;</c>
/// can subscribe to trigger alerts, send email notifications to data controllers,
/// update compliance dashboards, or initiate review workflows before deletion.
/// </para>
/// </remarks>
/// <param name="EntityId">Identifier of the data entity approaching expiration.</param>
/// <param name="DataCategory">The data category of the expiring record.</param>
/// <param name="ExpiresAtUtc">Timestamp when the data entity's retention period expires (UTC).</param>
/// <param name="DaysUntilExpiration">Number of days remaining until expiration. Zero indicates expiration today.</param>
/// <param name="OccurredAtUtc">Timestamp when this notification was generated (UTC).</param>
public sealed record DataExpiringNotification(
    string EntityId,
    string DataCategory,
    DateTimeOffset ExpiresAtUtc,
    int DaysUntilExpiration,
    DateTimeOffset OccurredAtUtc) : INotification;
