namespace Encina.Compliance.ProcessorAgreements.Notifications;

/// <summary>
/// Notification published when a sub-processor is removed from a processor's hierarchy.
/// </summary>
/// <remarks>
/// <para>
/// Published after a sub-processor is successfully removed from the processor registry.
/// Under general authorization (Article 28(2)), the processor must inform the controller
/// of changes to sub-processor arrangements, including removals.
/// </para>
/// <para>
/// Handlers implementing <c>INotificationHandler&lt;SubProcessorRemovedNotification&gt;</c>
/// can use this to:
/// </para>
/// <list type="bullet">
/// <item><description>Notify the controller about sub-processor changes.</description></item>
/// <item><description>Initiate data deletion or return procedures with the removed sub-processor (Article 28(3)(g)).</description></item>
/// <item><description>Update the processing chain documentation for compliance audits.</description></item>
/// <item><description>Terminate the sub-processor's DPA if one exists.</description></item>
/// </list>
/// </remarks>
/// <param name="ProcessorId">The identifier of the parent processor.</param>
/// <param name="SubProcessorId">The identifier of the removed sub-processor.</param>
/// <param name="OccurredAtUtc">The UTC timestamp when the sub-processor was removed.</param>
public sealed record SubProcessorRemovedNotification(
    string ProcessorId,
    string SubProcessorId,
    DateTimeOffset OccurredAtUtc) : INotification;
