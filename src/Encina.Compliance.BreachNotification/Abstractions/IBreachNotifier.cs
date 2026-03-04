using Encina.Compliance.BreachNotification.Model;

using LanguageExt;

namespace Encina.Compliance.BreachNotification;

/// <summary>
/// Dispatches breach notifications to supervisory authorities and data subjects.
/// </summary>
/// <remarks>
/// <para>
/// The breach notifier handles the actual delivery of notifications required by GDPR
/// Articles 33 and 34. It provides two distinct notification paths:
/// <list type="bullet">
/// <item><description>
/// <see cref="NotifyAuthorityAsync"/> — Notifies the supervisory authority per Article 33.
/// </description></item>
/// <item><description>
/// <see cref="NotifyDataSubjectsAsync"/> — Notifies affected data subjects per Article 34.
/// </description></item>
/// </list>
/// </para>
/// <para>
/// Per GDPR Article 33(1), the controller must notify the supervisory authority
/// "without undue delay and, where feasible, not later than 72 hours after having
/// become aware of it." The authority notification must include all information
/// specified in Article 33(3): nature of breach, DPO contact, likely consequences,
/// and measures taken.
/// </para>
/// <para>
/// Per GDPR Article 34(1), when a breach "is likely to result in a high risk to the
/// rights and freedoms of natural persons," the controller must communicate the breach
/// to the data subject "without undue delay." Article 34(3) provides exemptions when
/// encryption or other measures render data unintelligible, when subsequent measures
/// eliminate the high risk, or when individual notification would involve disproportionate effort.
/// </para>
/// <para>
/// Implementations should handle the specifics of notification delivery (e.g., email,
/// API calls to authority portals, SMS) and return a <see cref="NotificationResult"/>
/// documenting the outcome.
/// </para>
/// <para>
/// All methods follow Railway Oriented Programming (ROP) using <c>Either&lt;EncinaError, T&gt;</c>
/// to provide explicit error handling without exceptions for business logic.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Notify the supervisory authority about a breach
/// var authorityResult = await notifier.NotifyAuthorityAsync(breachRecord, cancellationToken);
///
/// authorityResult.Match(
///     Right: result => Console.WriteLine($"Authority notified: {result.Outcome}"),
///     Left: error => Console.WriteLine($"Authority notification failed: {error.Message}"));
///
/// // Notify affected data subjects
/// var subjectIds = new[] { "user-001", "user-002", "user-003" };
/// var subjectResult = await notifier.NotifyDataSubjectsAsync(breachRecord, subjectIds, cancellationToken);
/// </code>
/// </example>
public interface IBreachNotifier
{
    /// <summary>
    /// Notifies the supervisory authority about a personal data breach.
    /// </summary>
    /// <param name="breach">The breach record containing all Article 33(3) required information.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="NotificationResult"/> documenting the notification outcome,
    /// or an <see cref="EncinaError"/> if the notification could not be attempted.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Per Article 33(3), the notification must include:
    /// (a) the nature of the breach including categories and approximate number of subjects,
    /// (b) the DPO contact details,
    /// (c) the likely consequences, and
    /// (d) the measures taken or proposed.
    /// All of these are available in the <paramref name="breach"/> record.
    /// </para>
    /// <para>
    /// If the 72-hour deadline has passed, the notification should include the reasons
    /// for the delay per Article 33(1). See <see cref="BreachRecord.DelayReason"/>.
    /// </para>
    /// </remarks>
    ValueTask<Either<EncinaError, NotificationResult>> NotifyAuthorityAsync(
        BreachRecord breach,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Notifies affected data subjects about a personal data breach.
    /// </summary>
    /// <param name="breach">The breach record containing breach details to communicate.</param>
    /// <param name="subjectIds">The identifiers of the data subjects to notify.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="NotificationResult"/> documenting the notification outcome,
    /// or an <see cref="EncinaError"/> if the notification could not be attempted.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Per Article 34(1), the communication must describe "in clear and plain language"
    /// the nature of the breach and contain at least: (b) the DPO contact details,
    /// (c) the likely consequences, and (d) the measures taken or proposed.
    /// </para>
    /// <para>
    /// This method should only be called when:
    /// <list type="bullet">
    /// <item><description>The breach is likely to result in a high risk to rights and freedoms (Art. 34(1)).</description></item>
    /// <item><description>No exemption applies per Art. 34(3): encryption, mitigating measures, or disproportionate effort.</description></item>
    /// </list>
    /// Check <see cref="BreachRecord.SubjectNotificationExemption"/> before calling.
    /// </para>
    /// </remarks>
    ValueTask<Either<EncinaError, NotificationResult>> NotifyDataSubjectsAsync(
        BreachRecord breach,
        IEnumerable<string> subjectIds,
        CancellationToken cancellationToken = default);
}
