namespace Encina.Security.Audit;

/// <summary>
/// Notification published when sensitive data marked with <c>IReadAuditable</c>
/// is accessed through the audited repository.
/// </summary>
/// <remarks>
/// <para>
/// Published by the <c>AuditedRepository</c> decorator after each audited read operation
/// on an entity implementing <c>IReadAuditable</c>.
/// </para>
/// <para>
/// Handlers implementing <c>INotificationHandler&lt;SensitiveDataAccessedNotification&gt;</c>
/// can use this to trigger real-time alerting, security dashboards, compliance monitoring,
/// or integration with external SIEM systems.
/// </para>
/// <para>
/// This notification is published asynchronously and should not block the read operation.
/// </para>
/// </remarks>
/// <param name="EntityType">The type of entity that was accessed (e.g., "Patient", "FinancialRecord").</param>
/// <param name="EntityId">The specific entity identifier that was accessed, or <c>null</c> for bulk operations.</param>
/// <param name="UserId">The user ID who accessed the data, or <c>null</c> for system-initiated access.</param>
/// <param name="AccessedAtUtc">Timestamp when the data was accessed (UTC).</param>
public sealed record SensitiveDataAccessedNotification(
    string EntityType,
    string? EntityId,
    string? UserId,
    DateTimeOffset AccessedAtUtc) : INotification;
