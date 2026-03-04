namespace Encina.Compliance.BreachNotification;

/// <summary>
/// Notification published when a personal data breach has been resolved and all
/// remedial actions have been completed.
/// </summary>
/// <remarks>
/// <para>
/// Published after <c>IBreachHandler.ResolveBreachAsync</c> marks a breach as resolved.
/// This signals the end of the active breach management lifecycle.
/// </para>
/// <para>
/// Per GDPR Article 33(3)(d), the controller must describe the measures taken to
/// address the breach. The <see cref="ResolutionSummary"/> captures this information
/// and is recorded in the breach audit trail.
/// </para>
/// <para>
/// Handlers implementing <c>INotificationHandler&lt;BreachResolvedNotification&gt;</c>
/// can use this to close incident tickets, update compliance dashboards, or
/// generate post-incident reports.
/// </para>
/// </remarks>
/// <param name="BreachId">Identifier of the breach that was resolved.</param>
/// <param name="ResolvedAtUtc">Timestamp when the breach was resolved (UTC).</param>
/// <param name="ResolutionSummary">Summary of the resolution measures and outcomes.</param>
public sealed record BreachResolvedNotification(
    string BreachId,
    DateTimeOffset ResolvedAtUtc,
    string ResolutionSummary) : INotification;
