namespace Encina.Compliance.Consent;

/// <summary>
/// Domain event published when a new consent version is published for a processing purpose.
/// </summary>
/// <remarks>
/// <para>
/// This event is raised after a new <see cref="ConsentVersion"/> is successfully published
/// via <see cref="IConsentVersionManager.PublishNewVersionAsync"/>. It enables downstream
/// processes to react to consent term changes — for example, triggering reconsent
/// notifications or updating consent forms.
/// </para>
/// <para>
/// When <see cref="RequiresExplicitReconsent"/> is <c>true</c>, data subjects who
/// consented under previous versions must provide fresh consent under the new terms
/// before data processing can continue. Systems should use this event to initiate
/// reconsent workflows proactively.
/// </para>
/// </remarks>
/// <param name="Purpose">The processing purpose whose consent terms changed.</param>
/// <param name="OccurredAtUtc">Timestamp when the new version was published (UTC).</param>
/// <param name="NewVersionId">Unique identifier of the newly published consent version.</param>
/// <param name="RequiresExplicitReconsent">
/// Whether existing consents under previous versions require explicit renewal.
/// </param>
public sealed record ConsentVersionChangedEvent(
    string Purpose,
    DateTimeOffset OccurredAtUtc,
    string NewVersionId,
    bool RequiresExplicitReconsent) : INotification;
