namespace Encina.Compliance.ProcessorAgreements.Notifications;

/// <summary>
/// Notification published when a Data Processing Agreement has expired.
/// </summary>
/// <remarks>
/// <para>
/// Published by the <c>CheckDPAExpirationHandler</c> when it detects that an agreement's
/// <see cref="Model.DataProcessingAgreement.ExpiresAtUtc"/> has passed. The agreement's status
/// is transitioned to <see cref="Model.DPAStatus.Expired"/>.
/// </para>
/// <para>
/// An expired agreement means the contractual basis required by Article 28(3) is no longer valid.
/// The <c>ProcessorValidationPipelineBehavior</c> will block or warn requests targeting
/// the affected processor, depending on the configured enforcement mode.
/// </para>
/// <para>
/// Handlers implementing <c>INotificationHandler&lt;DPAExpiredNotification&gt;</c>
/// can use this to:
/// </para>
/// <list type="bullet">
/// <item><description>Alert compliance teams that processing operations may be affected.</description></item>
/// <item><description>Trigger urgent DPA renewal or termination workflows.</description></item>
/// <item><description>Log audit entries per the accountability principle (Article 5(2)).</description></item>
/// <item><description>Notify affected business units about potential processing disruptions.</description></item>
/// </list>
/// </remarks>
/// <param name="ProcessorId">The identifier of the processor whose agreement has expired.</param>
/// <param name="DPAId">The unique identifier of the expired agreement.</param>
/// <param name="ProcessorName">The display name of the processor.</param>
/// <param name="ExpiredAtUtc">The UTC timestamp when the agreement expired.</param>
/// <param name="OccurredAtUtc">The UTC timestamp when this notification was generated.</param>
public sealed record DPAExpiredNotification(
    string ProcessorId,
    string DPAId,
    string ProcessorName,
    DateTimeOffset ExpiredAtUtc,
    DateTimeOffset OccurredAtUtc) : INotification;
