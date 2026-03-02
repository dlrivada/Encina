using Encina.Compliance.Retention.Model;

using LanguageExt;

namespace Encina.Compliance.Retention;

/// <summary>
/// Service for resolving retention periods and checking data expiration status.
/// </summary>
/// <remarks>
/// <para>
/// The retention policy service provides the application-level logic for determining
/// how long data should be retained and whether specific entities have exceeded their
/// retention period. It sits above the <see cref="IRetentionPolicyStore"/> (raw persistence)
/// and adds business rules such as default retention periods and time-based comparisons.
/// </para>
/// <para>
/// Per GDPR Article 5(1)(e) (storage limitation), personal data shall be kept for no longer
/// than is necessary for the purposes for which it is processed. This service enables
/// controllers to query retention requirements for any data category and determine whether
/// specific data entities are eligible for deletion.
/// </para>
/// <para>
/// All methods follow Railway Oriented Programming (ROP) using <c>Either&lt;EncinaError, T&gt;</c>
/// to provide explicit error handling without exceptions for business logic.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Resolve the retention period for a data category
/// var period = await retentionPolicy.GetRetentionPeriodAsync("financial-records", cancellationToken);
///
/// // Check if a specific entity has exceeded its retention period
/// var expired = await retentionPolicy.IsExpiredAsync("order-12345", "financial-records", cancellationToken);
/// </code>
/// </example>
public interface IRetentionPolicy
{
    /// <summary>
    /// Resolves the retention period for a specific data category.
    /// </summary>
    /// <param name="dataCategory">The data category to look up (e.g., "financial-records", "customer-data").</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// The <see cref="TimeSpan"/> retention period defined for the category,
    /// or an <see cref="EncinaError"/> if no policy is defined and no default is configured.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Looks up the <see cref="RetentionPolicy"/> for the given category via
    /// <see cref="IRetentionPolicyStore.GetByCategoryAsync"/>. If no explicit policy exists,
    /// falls back to the default retention period configured in <c>RetentionOptions</c>.
    /// If no default is configured either, returns a <c>NoPolicyForCategory</c> error.
    /// </para>
    /// <para>
    /// Per Recital 39, appropriate time limits should be established by the controller
    /// for erasure or periodic review. This method enables programmatic resolution
    /// of those time limits.
    /// </para>
    /// </remarks>
    ValueTask<Either<EncinaError, TimeSpan>> GetRetentionPeriodAsync(
        string dataCategory,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a specific data entity has exceeded its retention period.
    /// </summary>
    /// <param name="entityId">The identifier of the data entity.</param>
    /// <param name="dataCategory">The data category of the entity.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <c>true</c> if the entity's retention period has expired (i.e., <see cref="RetentionRecord.ExpiresAtUtc"/>
    /// is in the past), <c>false</c> if still within retention period,
    /// or an <see cref="EncinaError"/> if the entity has no retention record.
    /// </returns>
    /// <remarks>
    /// This method checks the <see cref="RetentionRecord"/> for the entity and compares
    /// the <see cref="RetentionRecord.ExpiresAtUtc"/> against the current UTC time.
    /// It does NOT check for legal holds — use <see cref="ILegalHoldManager.IsUnderHoldAsync"/>
    /// to verify legal hold status before initiating deletion.
    /// </remarks>
    ValueTask<Either<EncinaError, bool>> IsExpiredAsync(
        string entityId,
        string dataCategory,
        CancellationToken cancellationToken = default);
}
