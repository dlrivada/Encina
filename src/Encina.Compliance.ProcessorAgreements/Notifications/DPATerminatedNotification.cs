namespace Encina.Compliance.ProcessorAgreements.Notifications;

/// <summary>
/// Notification published when a Data Processing Agreement is explicitly terminated.
/// </summary>
/// <remarks>
/// <para>
/// Published when a DPA's status is changed to <see cref="Model.DPAStatus.Terminated"/>.
/// Per Article 28(3)(g), upon termination the processor must delete or return all personal data
/// and certify that it has done so, unless Union or Member State law requires storage.
/// </para>
/// <para>
/// Handlers implementing <c>INotificationHandler&lt;DPATerminatedNotification&gt;</c>
/// can use this to:
/// </para>
/// <list type="bullet">
/// <item><description>Initiate data deletion or return procedures with the processor.</description></item>
/// <item><description>Request certification of data deletion from the processor.</description></item>
/// <item><description>Update compliance records and RoPA (Article 30).</description></item>
/// <item><description>Notify affected business units about the termination.</description></item>
/// </list>
/// </remarks>
/// <param name="ProcessorId">The identifier of the processor whose agreement was terminated.</param>
/// <param name="DPAId">The unique identifier of the terminated agreement.</param>
/// <param name="ProcessorName">The display name of the processor.</param>
/// <param name="OccurredAtUtc">The UTC timestamp when the termination occurred.</param>
public sealed record DPATerminatedNotification(
    string ProcessorId,
    string DPAId,
    string ProcessorName,
    DateTimeOffset OccurredAtUtc) : INotification;
