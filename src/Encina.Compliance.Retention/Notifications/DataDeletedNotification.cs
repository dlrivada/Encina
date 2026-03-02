namespace Encina.Compliance.Retention;

/// <summary>
/// Notification published when a data record has been automatically deleted
/// by the retention enforcement process.
/// </summary>
/// <remarks>
/// <para>
/// Published after successful deletion of a data record whose retention period
/// has expired. Per GDPR Article 5(1)(e), personal data shall be kept for no longer
/// than is necessary for the purposes for which it is processed.
/// </para>
/// <para>
/// Per Article 19, the controller must communicate the erasure to each recipient
/// to whom the personal data has been disclosed. Handlers implementing
/// <c>INotificationHandler&lt;DataDeletedNotification&gt;</c> can propagate
/// deletion to downstream systems, external processors, or audit logs.
/// </para>
/// </remarks>
/// <param name="EntityId">Identifier of the data entity that was deleted.</param>
/// <param name="DataCategory">The data category of the deleted record.</param>
/// <param name="DeletedAtUtc">Timestamp when the deletion was performed (UTC).</param>
/// <param name="PolicyId">Identifier of the retention policy that triggered the deletion.</param>
public sealed record DataDeletedNotification(
    string EntityId,
    string DataCategory,
    DateTimeOffset DeletedAtUtc,
    string? PolicyId) : INotification;
