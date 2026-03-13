namespace Encina.Compliance.ProcessorAgreements.Notifications;

/// <summary>
/// Notification published when a Data Processing Agreement is signed and becomes active.
/// </summary>
/// <remarks>
/// <para>
/// Published after a DPA is successfully added via <c>IDPAStore.AddAsync</c> with
/// <see cref="Model.DPAStatus.Active"/> status. This marks the establishment of the
/// contractual relationship required by Article 28(3).
/// </para>
/// <para>
/// Handlers implementing <c>INotificationHandler&lt;DPASignedNotification&gt;</c>
/// can use this to:
/// </para>
/// <list type="bullet">
/// <item><description>Schedule expiration monitoring for the new agreement.</description></item>
/// <item><description>Notify stakeholders about the new contractual relationship.</description></item>
/// <item><description>Update Records of Processing Activities (RoPA) per Article 30.</description></item>
/// <item><description>Verify SCC requirements for cross-border transfers (Articles 46-49).</description></item>
/// </list>
/// </remarks>
/// <param name="ProcessorId">The identifier of the processor covered by the agreement.</param>
/// <param name="DPAId">The unique identifier of the signed agreement.</param>
/// <param name="ProcessorName">The display name of the processor.</param>
/// <param name="SignedAtUtc">The UTC timestamp when the agreement was signed.</param>
/// <param name="OccurredAtUtc">The UTC timestamp when this notification was generated.</param>
public sealed record DPASignedNotification(
    string ProcessorId,
    string DPAId,
    string ProcessorName,
    DateTimeOffset SignedAtUtc,
    DateTimeOffset OccurredAtUtc) : INotification;
