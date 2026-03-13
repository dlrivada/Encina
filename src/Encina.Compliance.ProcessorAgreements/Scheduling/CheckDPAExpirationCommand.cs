using LanguageExt;

namespace Encina.Compliance.ProcessorAgreements.Scheduling;

/// <summary>
/// Scheduled command that checks for expiring and expired Data Processing Agreements.
/// </summary>
/// <remarks>
/// <para>
/// This command is designed to be executed periodically by the Encina.Scheduling infrastructure
/// rather than a dedicated <c>IHostedService</c>. The scheduling interval is controlled by
/// <see cref="ProcessorAgreementOptions.ExpirationCheckInterval"/>.
/// </para>
/// <para>
/// When executed, the <see cref="CheckDPAExpirationHandler"/>:
/// <list type="number">
/// <item><description>Queries <see cref="IDPAStore.GetExpiringAsync"/> for agreements approaching expiration
/// within <see cref="ProcessorAgreementOptions.ExpirationWarningDays"/>.</description></item>
/// <item><description>Queries <see cref="IDPAStore.GetByStatusAsync"/> for already-expired agreements.</description></item>
/// <item><description>Publishes <c>DPAExpiringNotification</c> for each approaching agreement.</description></item>
/// <item><description>Publishes <c>DPAExpiredNotification</c> for each expired agreement.</description></item>
/// </list>
/// </para>
/// <para>
/// Enable via <see cref="ProcessorAgreementOptions.EnableExpirationMonitoring"/>:
/// <code>
/// services.AddEncinaProcessorAgreements(options =>
/// {
///     options.EnableExpirationMonitoring = true;
///     options.ExpirationCheckInterval = TimeSpan.FromHours(1);
///     options.ExpirationWarningDays = 30;
/// });
/// </code>
/// </para>
/// </remarks>
public sealed record CheckDPAExpirationCommand : ICommand<Unit>;
