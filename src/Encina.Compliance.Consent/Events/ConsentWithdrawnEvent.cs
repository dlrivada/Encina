namespace Encina.Compliance.Consent;

/// <summary>
/// Domain event published when a data subject withdraws consent for a specific purpose.
/// </summary>
/// <remarks>
/// <para>
/// This event is raised after a consent record is successfully updated to
/// <see cref="ConsentStatus.Withdrawn"/> via <see cref="IConsentStore.WithdrawConsentAsync"/>.
/// It enables downstream processes to stop data processing for the withdrawn purpose.
/// </para>
/// <para>
/// Per GDPR Article 7(3), withdrawing consent must be as easy as giving it. This event
/// allows systems to react immediately — for example, by unsubscribing from marketing
/// communications, stopping data collection, or triggering data deletion workflows.
/// </para>
/// </remarks>
/// <param name="SubjectId">Identifier of the data subject who withdrew consent.</param>
/// <param name="Purpose">The processing purpose for which consent was withdrawn.</param>
/// <param name="OccurredAtUtc">Timestamp when the withdrawal was recorded (UTC).</param>
public sealed record ConsentWithdrawnEvent(
    string SubjectId,
    string Purpose,
    DateTimeOffset OccurredAtUtc) : INotification;
