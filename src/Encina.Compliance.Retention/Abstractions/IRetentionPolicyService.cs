using Encina.Compliance.Retention.Model;
using Encina.Compliance.Retention.ReadModels;
using LanguageExt;

namespace Encina.Compliance.Retention.Abstractions;

/// <summary>
/// Service interface for managing retention policy lifecycle operations via event-sourced aggregates.
/// </summary>
/// <remarks>
/// <para>
/// Provides a clean API for creating, updating, deactivating, and querying retention policies.
/// The implementation wraps the event-sourced <c>RetentionPolicyAggregate</c> via
/// <c>IAggregateRepository&lt;RetentionPolicyAggregate&gt;</c>, handling aggregate loading,
/// command execution, persistence, and cache management.
/// </para>
/// <para>
/// This service replaces the legacy <c>IRetentionPolicyStore</c> and <c>IRetentionPolicy</c>
/// interfaces with a single CQRS-oriented API. The event stream serves as the audit trail,
/// eliminating the need for separate audit recording per GDPR Article 5(2) accountability.
/// </para>
/// <para>
/// <b>Commands</b> (write operations via aggregate):
/// <list type="bullet">
///   <item><description><see cref="CreatePolicyAsync"/> — Creates a new retention policy for a data category (Art. 5(1)(e))</description></item>
///   <item><description><see cref="UpdatePolicyAsync"/> — Updates an existing policy's parameters</description></item>
///   <item><description><see cref="DeactivatePolicyAsync"/> — Deactivates a policy, preventing new record tracking</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Queries</b> (read operations via read model repository):
/// <list type="bullet">
///   <item><description><see cref="GetPolicyAsync"/> — Retrieves a policy by ID</description></item>
///   <item><description><see cref="GetPolicyByCategoryAsync"/> — Retrieves a policy by data category</description></item>
///   <item><description><see cref="GetActivePoliciesAsync"/> — Lists all active policies</description></item>
///   <item><description><see cref="GetRetentionPeriodAsync"/> — Resolves the retention period for a data category</description></item>
///   <item><description><see cref="GetPolicyHistoryAsync"/> — Retrieves full event history</description></item>
/// </list>
/// </para>
/// </remarks>
public interface IRetentionPolicyService
{
    // ========================================================================
    // Command operations (write-side via RetentionPolicyAggregate)
    // ========================================================================

    /// <summary>
    /// Creates a new retention policy for a specific data category.
    /// </summary>
    /// <param name="dataCategory">The data category this policy applies to (e.g., "customer-data", "financial-records").</param>
    /// <param name="retentionPeriod">How long data in this category should be retained.</param>
    /// <param name="autoDelete">Whether the enforcement service should automatically delete expired data.</param>
    /// <param name="policyType">The trigger mechanism for the retention period.</param>
    /// <param name="reason">Optional reason or justification for this retention period.</param>
    /// <param name="legalBasis">Optional legal basis for the retention requirement.</param>
    /// <param name="tenantId">Optional tenant identifier for multi-tenancy.</param>
    /// <param name="moduleId">Optional module identifier for modular monolith.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or the identifier of the newly created policy aggregate.</returns>
    /// <remarks>
    /// Per GDPR Article 5(1)(e), controllers must establish explicit retention periods for all
    /// categories of personal data. Each data category should have at most one active policy
    /// to avoid ambiguity during enforcement.
    /// </remarks>
    ValueTask<Either<EncinaError, Guid>> CreatePolicyAsync(
        string dataCategory,
        TimeSpan retentionPeriod,
        bool autoDelete,
        RetentionPolicyType policyType,
        string? reason = null,
        string? legalBasis = null,
        string? tenantId = null,
        string? moduleId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing retention policy with new parameters.
    /// </summary>
    /// <param name="policyId">The retention policy aggregate identifier.</param>
    /// <param name="retentionPeriod">The updated retention period.</param>
    /// <param name="autoDelete">The updated auto-deletion setting.</param>
    /// <param name="reason">Updated reason or justification.</param>
    /// <param name="legalBasis">Updated legal basis.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or Unit on success.</returns>
    /// <remarks>
    /// Only active policies can be updated. Existing retention records tracked under this policy
    /// retain their original retention period; the update only affects newly tracked records.
    /// </remarks>
    ValueTask<Either<EncinaError, Unit>> UpdatePolicyAsync(
        Guid policyId,
        TimeSpan retentionPeriod,
        bool autoDelete,
        string? reason,
        string? legalBasis,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates a retention policy, preventing new retention records from being tracked under it.
    /// </summary>
    /// <param name="policyId">The retention policy aggregate identifier.</param>
    /// <param name="reason">The reason for deactivating this policy.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or Unit on success.</returns>
    /// <remarks>
    /// Existing retention records tracked under this policy continue their lifecycle (expiration,
    /// deletion) unaffected. Deactivation is preferred over deletion to maintain a complete
    /// audit trail per GDPR Article 5(2) accountability.
    /// </remarks>
    ValueTask<Either<EncinaError, Unit>> DeactivatePolicyAsync(
        Guid policyId,
        string reason,
        CancellationToken cancellationToken = default);

    // ========================================================================
    // Query operations (read-side via RetentionPolicyReadModel)
    // ========================================================================

    /// <summary>
    /// Retrieves a retention policy by its aggregate identifier.
    /// </summary>
    /// <param name="policyId">The retention policy aggregate identifier.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error (including not-found) or the policy read model.</returns>
    ValueTask<Either<EncinaError, RetentionPolicyReadModel>> GetPolicyAsync(
        Guid policyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a retention policy by its data category.
    /// </summary>
    /// <param name="dataCategory">The data category to search for.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error (including not-found) or the policy read model.</returns>
    ValueTask<Either<EncinaError, RetentionPolicyReadModel>> GetPolicyByCategoryAsync(
        string dataCategory,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all active retention policies.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A read-only list of active policy read models.</returns>
    ValueTask<Either<EncinaError, IReadOnlyList<RetentionPolicyReadModel>>> GetActivePoliciesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves the retention period for a data category, falling back to the default if no policy exists.
    /// </summary>
    /// <param name="dataCategory">The data category to resolve the retention period for.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>
    /// Either an error or the resolved retention period. If no explicit policy exists and a
    /// <see cref="RetentionOptions.DefaultRetentionPeriod"/> is configured, that value is returned.
    /// If neither exists, an error is returned.
    /// </returns>
    /// <remarks>
    /// Per GDPR Article 5(1)(e), controllers should establish explicit retention periods for all
    /// categories of personal data. A missing policy indicates a gap in compliance coverage.
    /// </remarks>
    ValueTask<Either<EncinaError, TimeSpan>> GetRetentionPeriodAsync(
        string dataCategory,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the full event history for a retention policy aggregate.
    /// </summary>
    /// <param name="policyId">The retention policy aggregate identifier.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>
    /// Either an error or the list of domain events that have been applied to this policy,
    /// ordered chronologically. Provides a complete audit trail for GDPR Article 5(2) accountability.
    /// </returns>
    ValueTask<Either<EncinaError, IReadOnlyList<object>>> GetPolicyHistoryAsync(
        Guid policyId,
        CancellationToken cancellationToken = default);
}
