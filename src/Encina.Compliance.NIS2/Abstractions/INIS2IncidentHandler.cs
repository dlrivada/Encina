using Encina.Compliance.NIS2.Model;

using LanguageExt;

namespace Encina.Compliance.NIS2.Abstractions;

/// <summary>
/// Handles NIS2 incident reporting and notification timeline validation (Art. 23).
/// </summary>
/// <remarks>
/// <para>
/// Per NIS2 Article 23(4), significant incidents require a phased notification process:
/// </para>
/// <list type="number">
/// <item><description>Early warning within 24 hours of becoming aware (Art. 23(4)(a)).</description></item>
/// <item><description>Incident notification within 72 hours (Art. 23(4)(b)).</description></item>
/// <item><description>Intermediate report upon CSIRT/authority request (Art. 23(4)(c)).</description></item>
/// <item><description>Final report within one month of the notification (Art. 23(4)(d)).</description></item>
/// </list>
/// <para>
/// This handler provides stateless timeline validation. For persistent incident lifecycle
/// management (event-sourced tracking), integrate with <c>Encina.Compliance.BreachNotification</c>.
/// When <c>IBreachNotificationService</c> is registered, the default implementation delegates
/// incident persistence to it.
/// </para>
/// </remarks>
public interface INIS2IncidentHandler
{
    /// <summary>
    /// Reports a cybersecurity incident for NIS2 compliance tracking.
    /// </summary>
    /// <param name="incident">The incident to report.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <see cref="Unit"/> on success; or an <see cref="EncinaError"/> if the report could not be processed.
    /// </returns>
    /// <remarks>
    /// Per Art. 23(1), essential and important entities must notify their CSIRT or competent
    /// authority of any significant incident without undue delay. If <c>IBreachNotificationService</c>
    /// is registered, the incident is also persisted to the breach notification event stream.
    /// </remarks>
    ValueTask<Either<EncinaError, Unit>> ReportIncidentAsync(
        NIS2Incident incident,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether the specified notification phase deadline has not yet been exceeded.
    /// </summary>
    /// <param name="incident">The incident to check.</param>
    /// <param name="phase">The notification phase to validate against.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <c>true</c> if the current time is before the deadline for the specified phase;
    /// <c>false</c> if the deadline has passed; or an <see cref="EncinaError"/> if validation failed.
    /// </returns>
    /// <remarks>
    /// Deadlines per Art. 23(4):
    /// <see cref="NIS2NotificationPhase.EarlyWarning"/> — 24 hours from detection,
    /// <see cref="NIS2NotificationPhase.IncidentNotification"/> — 72 hours from detection,
    /// <see cref="NIS2NotificationPhase.FinalReport"/> — 1 month from incident notification submission.
    /// </remarks>
    ValueTask<Either<EncinaError, bool>> IsWithinNotificationDeadlineAsync(
        NIS2Incident incident,
        NIS2NotificationPhase phase,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the next pending notification phase and its deadline for the given incident.
    /// </summary>
    /// <param name="incident">The incident to evaluate.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A tuple of the next <see cref="NIS2NotificationPhase"/> and its <see cref="DateTimeOffset"/>
    /// deadline; or an <see cref="EncinaError"/> if all phases are complete or evaluation failed.
    /// </returns>
    ValueTask<Either<EncinaError, (NIS2NotificationPhase Phase, DateTimeOffset Deadline)>> GetNextDeadlineAsync(
        NIS2Incident incident,
        CancellationToken cancellationToken = default);
}
