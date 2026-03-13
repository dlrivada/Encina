namespace Encina.Compliance.ProcessorAgreements.Notifications;

/// <summary>
/// Notification published when a Data Processing Agreement is approaching its expiration date.
/// </summary>
/// <remarks>
/// <para>
/// Published by the <c>CheckDPAExpirationHandler</c> when it detects that an agreement's
/// <see cref="Model.DataProcessingAgreement.ExpiresAtUtc"/> is within the configured
/// expiration warning threshold.
/// </para>
/// <para>
/// Handlers implementing <c>INotificationHandler&lt;DPAExpiringNotification&gt;</c>
/// can use this to:
/// </para>
/// <list type="bullet">
/// <item><description>Alert compliance teams about upcoming renewal deadlines.</description></item>
/// <item><description>Initiate DPA renewal workflows with the processor.</description></item>
/// <item><description>Send reminders to the processor's contact email.</description></item>
/// <item><description>Escalate to management if renewal is not initiated within a defined period.</description></item>
/// </list>
/// </remarks>
/// <param name="ProcessorId">The identifier of the processor whose agreement is expiring.</param>
/// <param name="DPAId">The unique identifier of the expiring agreement.</param>
/// <param name="ProcessorName">The display name of the processor.</param>
/// <param name="ExpiresAtUtc">The UTC timestamp when the agreement will expire.</param>
/// <param name="DaysUntilExpiration">The number of days remaining until expiration.</param>
/// <param name="OccurredAtUtc">The UTC timestamp when this notification was generated.</param>
public sealed record DPAExpiringNotification(
    string ProcessorId,
    string DPAId,
    string ProcessorName,
    DateTimeOffset ExpiresAtUtc,
    int DaysUntilExpiration,
    DateTimeOffset OccurredAtUtc) : INotification;
