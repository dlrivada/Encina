using System.Diagnostics;

using Encina.Compliance.Retention.Diagnostics;
using Encina.Compliance.Retention.Model;

using LanguageExt;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using static LanguageExt.Prelude;

namespace Encina.Compliance.Retention;

/// <summary>
/// Default implementation of <see cref="IRetentionPolicy"/> that resolves retention periods
/// from the policy store with optional fallback to configured defaults.
/// </summary>
/// <remarks>
/// <para>
/// Resolves retention periods by querying <see cref="IRetentionPolicyStore.GetByCategoryAsync"/>
/// for category-specific policies. If no explicit policy exists, falls back to
/// <see cref="RetentionOptions.DefaultRetentionPeriod"/>. If no default is configured,
/// returns a <c>NoPolicyForCategory</c> error.
/// </para>
/// <para>
/// Expiration checks use <see cref="TimeProvider.GetUtcNow()"/> for deterministic testability,
/// comparing the current UTC time against retention records retrieved from the record store.
/// </para>
/// <para>
/// Per GDPR Article 5(1)(e), controllers must establish explicit retention periods for all
/// categories of personal data. This service enables programmatic resolution of those periods.
/// </para>
/// </remarks>
public sealed class DefaultRetentionPolicy : IRetentionPolicy
{
    private readonly IRetentionPolicyStore _policyStore;
    private readonly IRetentionRecordStore _recordStore;
    private readonly TimeProvider _timeProvider;
    private readonly RetentionOptions _options;
    private readonly ILogger<DefaultRetentionPolicy> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultRetentionPolicy"/> class.
    /// </summary>
    /// <param name="policyStore">Store for retention policy lookups.</param>
    /// <param name="recordStore">Store for retention record lookups.</param>
    /// <param name="timeProvider">Time provider for deterministic time-based comparisons.</param>
    /// <param name="options">Configuration options for the retention module.</param>
    /// <param name="logger">Logger for diagnostic messages.</param>
    public DefaultRetentionPolicy(
        IRetentionPolicyStore policyStore,
        IRetentionRecordStore recordStore,
        TimeProvider timeProvider,
        IOptions<RetentionOptions> options,
        ILogger<DefaultRetentionPolicy> logger)
    {
        ArgumentNullException.ThrowIfNull(policyStore);
        ArgumentNullException.ThrowIfNull(recordStore);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _policyStore = policyStore;
        _recordStore = recordStore;
        _timeProvider = timeProvider;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, TimeSpan>> GetRetentionPeriodAsync(
        string dataCategory,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataCategory);

        using var activity = RetentionDiagnostics.StartPolicyResolution(dataCategory);

        var policyResult = await _policyStore.GetByCategoryAsync(dataCategory, cancellationToken);

        return policyResult.Match(
            Right: optPolicy => optPolicy.Match(
                Some: policy =>
                {
                    _logger.RetentionPeriodResolved(dataCategory, policy.RetentionPeriod.TotalDays, policy.Id);
                    RetentionDiagnostics.RecordCompleted(activity);
                    RetentionDiagnostics.PolicyResolutionsTotal.Add(1, new KeyValuePair<string, object?>(RetentionDiagnostics.TagOutcome, "completed"));
                    return Right<EncinaError, TimeSpan>(policy.RetentionPeriod);
                },
                None: () =>
                {
                    if (_options.DefaultRetentionPeriod.HasValue)
                    {
                        _logger.RetentionPeriodResolvedFromDefault(dataCategory, _options.DefaultRetentionPeriod.Value.TotalDays);
                        RetentionDiagnostics.RecordCompleted(activity);
                        RetentionDiagnostics.PolicyResolutionsTotal.Add(1, new KeyValuePair<string, object?>(RetentionDiagnostics.TagOutcome, "completed"));
                        return Right<EncinaError, TimeSpan>(_options.DefaultRetentionPeriod.Value);
                    }

                    _logger.RetentionNoPolicyForCategory(dataCategory);
                    RetentionDiagnostics.RecordFailed(activity, "no_policy_for_category");
                    RetentionDiagnostics.PolicyResolutionsTotal.Add(1, new KeyValuePair<string, object?>(RetentionDiagnostics.TagOutcome, "failed"));
                    return Left<EncinaError, TimeSpan>(RetentionErrors.NoPolicyForCategory(dataCategory));
                }),
            Left: error =>
            {
                RetentionDiagnostics.RecordFailed(activity, error.Message);
                RetentionDiagnostics.PolicyResolutionsTotal.Add(1, new KeyValuePair<string, object?>(RetentionDiagnostics.TagOutcome, "failed"));
                return Left<EncinaError, TimeSpan>(error);
            });
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, bool>> IsExpiredAsync(
        string entityId,
        string dataCategory,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);
        ArgumentException.ThrowIfNullOrWhiteSpace(dataCategory);

        var recordsResult = await _recordStore.GetByEntityIdAsync(entityId, cancellationToken);

        return recordsResult.Match(
            Right: records =>
            {
                var record = records.FirstOrDefault(r => r.DataCategory == dataCategory);
                if (record is null)
                {
                    _logger.RetentionRecordNotFound(entityId, dataCategory);
                    return Left<EncinaError, bool>(RetentionErrors.RecordNotFound(entityId));
                }

                var now = _timeProvider.GetUtcNow();
                var isExpired = record.ExpiresAtUtc < now;

                _logger.RetentionExpirationChecked(entityId, dataCategory, isExpired, record.ExpiresAtUtc);

                return Right<EncinaError, bool>(isExpired);
            },
            Left: error => Left<EncinaError, bool>(error));
    }
}
