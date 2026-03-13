namespace Encina.Compliance.ProcessorAgreements.Notifications;

/// <summary>
/// Notification published when a new processor is registered in the processor registry.
/// </summary>
/// <remarks>
/// <para>
/// Published after a processor is successfully registered via <c>IProcessorRegistry.RegisterProcessorAsync</c>.
/// This marks the beginning of the processor's lifecycle in the system.
/// </para>
/// <para>
/// Handlers implementing <c>INotificationHandler&lt;ProcessorRegisteredNotification&gt;</c>
/// can use this to:
/// </para>
/// <list type="bullet">
/// <item><description>Initiate DPA onboarding workflows for the new processor.</description></item>
/// <item><description>Notify compliance teams about new processor relationships.</description></item>
/// <item><description>Update compliance dashboards and processor inventories.</description></item>
/// <item><description>Trigger due diligence assessments per Article 28(1).</description></item>
/// </list>
/// </remarks>
/// <param name="ProcessorId">The unique identifier of the registered processor.</param>
/// <param name="ProcessorName">The display name of the registered processor.</param>
/// <param name="OccurredAtUtc">The UTC timestamp when the registration occurred.</param>
public sealed record ProcessorRegisteredNotification(
    string ProcessorId,
    string ProcessorName,
    DateTimeOffset OccurredAtUtc) : INotification;
