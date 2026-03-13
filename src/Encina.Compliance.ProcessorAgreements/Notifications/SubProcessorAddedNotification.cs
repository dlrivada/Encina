namespace Encina.Compliance.ProcessorAgreements.Notifications;

/// <summary>
/// Notification published when a sub-processor is added to a processor's hierarchy.
/// </summary>
/// <remarks>
/// <para>
/// Published after a sub-processor is successfully registered with a
/// <see cref="Model.Processor.ParentProcessorId"/> referencing the parent processor.
/// Per Article 28(2), the processor must have prior specific or general written
/// authorization from the controller before engaging another processor.
/// </para>
/// <para>
/// The <see cref="Depth"/> field indicates the position in the sub-processor chain,
/// bounded by the configured <c>MaxSubProcessorDepth</c>. Per Article 28(4),
/// sub-processors must meet the same data protection obligations as the original processor.
/// </para>
/// <para>
/// Handlers implementing <c>INotificationHandler&lt;SubProcessorAddedNotification&gt;</c>
/// can use this to:
/// </para>
/// <list type="bullet">
/// <item><description>Notify the controller about new sub-processor engagement (required under general authorization).</description></item>
/// <item><description>Initiate DPA setup for the new sub-processor.</description></item>
/// <item><description>Verify that the sub-processor meets data protection requirements (Article 28(4)).</description></item>
/// <item><description>Update the processing chain documentation for compliance audits.</description></item>
/// </list>
/// </remarks>
/// <param name="ProcessorId">The identifier of the parent processor.</param>
/// <param name="SubProcessorId">The identifier of the newly added sub-processor.</param>
/// <param name="Depth">The depth of the sub-processor in the hierarchy (1 = direct sub-processor, 2 = sub-sub-processor, etc.).</param>
/// <param name="OccurredAtUtc">The UTC timestamp when the sub-processor was added.</param>
public sealed record SubProcessorAddedNotification(
    string ProcessorId,
    string SubProcessorId,
    int Depth,
    DateTimeOffset OccurredAtUtc) : INotification;
