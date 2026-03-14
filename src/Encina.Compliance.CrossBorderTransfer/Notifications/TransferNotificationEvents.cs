namespace Encina.Compliance.CrossBorderTransfer.Notifications;

/// <summary>
/// Notification published when an approved cross-border transfer is approaching its expiration date.
/// </summary>
/// <remarks>
/// <para>
/// Published by <see cref="TransferExpirationMonitor"/> when an approved transfer's
/// <c>ExpiresAtUtc</c> is within the configured alert window. This allows data controllers
/// to renew or revoke the transfer before it expires automatically.
/// </para>
/// <para>
/// Per GDPR Article 44, ongoing transfers require valid legal mechanisms. An expiring
/// transfer approval may disrupt data flows if not renewed in time.
/// </para>
/// <para>
/// Handlers implementing <c>INotificationHandler&lt;TransferExpiringNotification&gt;</c>
/// can subscribe to trigger alerts, send renewal reminders, update compliance dashboards,
/// or initiate review workflows before the transfer authorization lapses.
/// </para>
/// </remarks>
/// <param name="TransferId">Identifier of the approved transfer approaching expiration.</param>
/// <param name="SourceCountryCode">ISO 3166-1 alpha-2 code of the data exporter country.</param>
/// <param name="DestinationCountryCode">ISO 3166-1 alpha-2 code of the data importer country.</param>
/// <param name="DataCategory">The data category of the transfer.</param>
/// <param name="ExpiresAtUtc">Timestamp when the transfer authorization expires (UTC).</param>
/// <param name="DaysUntilExpiration">Number of days remaining until expiration. Zero indicates expiration today.</param>
/// <param name="OccurredAtUtc">Timestamp when this notification was generated (UTC).</param>
public sealed record TransferExpiringNotification(
    Guid TransferId,
    string SourceCountryCode,
    string DestinationCountryCode,
    string DataCategory,
    DateTimeOffset ExpiresAtUtc,
    int DaysUntilExpiration,
    DateTimeOffset OccurredAtUtc) : INotification;

/// <summary>
/// Notification published when an approved cross-border transfer has expired.
/// </summary>
/// <remarks>
/// <para>
/// Published by <see cref="TransferExpirationMonitor"/> when an approved transfer's
/// <c>ExpiresAtUtc</c> has passed. Data flows relying on this authorization are no longer
/// covered by the expired approval and must be re-evaluated or blocked.
/// </para>
/// <para>
/// Per GDPR Article 44, any transfer without a valid legal basis must cease. An expired
/// transfer authorization means the data controller must obtain a new approval, rely on
/// an alternative mechanism, or suspend the data flow.
/// </para>
/// <para>
/// Handlers implementing <c>INotificationHandler&lt;TransferExpiredNotification&gt;</c>
/// can use this to block data flows, notify data protection officers, escalate to
/// management, or log the compliance gap for audit purposes.
/// </para>
/// </remarks>
/// <param name="TransferId">Identifier of the expired transfer.</param>
/// <param name="SourceCountryCode">ISO 3166-1 alpha-2 code of the data exporter country.</param>
/// <param name="DestinationCountryCode">ISO 3166-1 alpha-2 code of the data importer country.</param>
/// <param name="DataCategory">The data category of the transfer.</param>
/// <param name="ExpiredAtUtc">Timestamp when the transfer authorization expired (UTC).</param>
/// <param name="OccurredAtUtc">Timestamp when this notification was generated (UTC).</param>
public sealed record TransferExpiredNotification(
    Guid TransferId,
    string SourceCountryCode,
    string DestinationCountryCode,
    string DataCategory,
    DateTimeOffset ExpiredAtUtc,
    DateTimeOffset OccurredAtUtc) : INotification;

/// <summary>
/// Notification published when a TIA (Transfer Impact Assessment) is approaching its expiration date.
/// </summary>
/// <remarks>
/// <para>
/// Published by <see cref="TransferExpirationMonitor"/> when a TIA's assessment period
/// is nearing its end. Per Schrems II (CJEU C-311/18), TIAs must be periodically reviewed
/// and renewed to reflect changes in the legal landscape of the destination country.
/// </para>
/// <para>
/// Handlers implementing <c>INotificationHandler&lt;TIAExpiringNotification&gt;</c>
/// can subscribe to trigger DPO review workflows, schedule reassessments, or notify
/// the compliance team.
/// </para>
/// </remarks>
/// <param name="TIAId">Identifier of the TIA approaching expiration.</param>
/// <param name="SourceCountryCode">ISO 3166-1 alpha-2 code of the data exporter country.</param>
/// <param name="DestinationCountryCode">ISO 3166-1 alpha-2 code of the data importer country.</param>
/// <param name="DataCategory">The data category assessed by the TIA.</param>
/// <param name="ExpiresAtUtc">Timestamp when the TIA expires (UTC).</param>
/// <param name="DaysUntilExpiration">Number of days remaining until expiration.</param>
/// <param name="OccurredAtUtc">Timestamp when this notification was generated (UTC).</param>
public sealed record TIAExpiringNotification(
    Guid TIAId,
    string SourceCountryCode,
    string DestinationCountryCode,
    string DataCategory,
    DateTimeOffset ExpiresAtUtc,
    int DaysUntilExpiration,
    DateTimeOffset OccurredAtUtc) : INotification;

/// <summary>
/// Notification published when a TIA (Transfer Impact Assessment) has expired.
/// </summary>
/// <remarks>
/// <para>
/// Published by <see cref="TransferExpirationMonitor"/> when a TIA's validity period
/// has elapsed. An expired TIA means the risk assessment for the destination country
/// is no longer current, and any transfers relying on it should be reviewed.
/// </para>
/// <para>
/// Per the EDPB Recommendations 01/2020, TIAs should be reassessed when there are
/// significant changes in the legal framework or surveillance practices of the
/// destination country.
/// </para>
/// </remarks>
/// <param name="TIAId">Identifier of the expired TIA.</param>
/// <param name="SourceCountryCode">ISO 3166-1 alpha-2 code of the data exporter country.</param>
/// <param name="DestinationCountryCode">ISO 3166-1 alpha-2 code of the data importer country.</param>
/// <param name="DataCategory">The data category assessed by the TIA.</param>
/// <param name="ExpiredAtUtc">Timestamp when the TIA expired (UTC).</param>
/// <param name="OccurredAtUtc">Timestamp when this notification was generated (UTC).</param>
public sealed record TIAExpiredNotification(
    Guid TIAId,
    string SourceCountryCode,
    string DestinationCountryCode,
    string DataCategory,
    DateTimeOffset ExpiredAtUtc,
    DateTimeOffset OccurredAtUtc) : INotification;

/// <summary>
/// Notification published when an SCC agreement is approaching its expiration date.
/// </summary>
/// <remarks>
/// <para>
/// Published by <see cref="TransferExpirationMonitor"/> when an SCC agreement's
/// validity period is nearing its end. SCC agreements (Art. 46(2)(c)) provide
/// appropriate safeguards for international data transfers and must remain current.
/// </para>
/// <para>
/// Handlers implementing <c>INotificationHandler&lt;SCCAgreementExpiringNotification&gt;</c>
/// can subscribe to trigger renewal workflows, notify the legal team, or alert
/// data processors about upcoming agreement renewals.
/// </para>
/// </remarks>
/// <param name="AgreementId">Identifier of the SCC agreement approaching expiration.</param>
/// <param name="ProcessorId">Identifier of the data processor party to the agreement.</param>
/// <param name="Module">The SCC module type of the agreement.</param>
/// <param name="ExpiresAtUtc">Timestamp when the SCC agreement expires (UTC).</param>
/// <param name="DaysUntilExpiration">Number of days remaining until expiration.</param>
/// <param name="OccurredAtUtc">Timestamp when this notification was generated (UTC).</param>
public sealed record SCCAgreementExpiringNotification(
    Guid AgreementId,
    string ProcessorId,
    string Module,
    DateTimeOffset ExpiresAtUtc,
    int DaysUntilExpiration,
    DateTimeOffset OccurredAtUtc) : INotification;

/// <summary>
/// Notification published when an SCC agreement has expired.
/// </summary>
/// <remarks>
/// <para>
/// Published by <see cref="TransferExpirationMonitor"/> when an SCC agreement's
/// validity period has elapsed. An expired SCC agreement means the legal basis for
/// transfers under that agreement is no longer valid, and data flows must cease
/// or an alternative mechanism must be established.
/// </para>
/// <para>
/// Per GDPR Article 46, appropriate safeguards must be maintained for the duration
/// of any international data transfer. An expired SCC agreement represents a
/// compliance gap that must be addressed promptly.
/// </para>
/// </remarks>
/// <param name="AgreementId">Identifier of the expired SCC agreement.</param>
/// <param name="ProcessorId">Identifier of the data processor party to the agreement.</param>
/// <param name="Module">The SCC module type of the agreement.</param>
/// <param name="ExpiredAtUtc">Timestamp when the SCC agreement expired (UTC).</param>
/// <param name="OccurredAtUtc">Timestamp when this notification was generated (UTC).</param>
public sealed record SCCAgreementExpiredNotification(
    Guid AgreementId,
    string ProcessorId,
    string Module,
    DateTimeOffset ExpiredAtUtc,
    DateTimeOffset OccurredAtUtc) : INotification;
