using System.Diagnostics;

using Encina.Caching;
using Encina.Compliance.NIS2.Abstractions;
using Encina.Compliance.NIS2.Diagnostics;
using Encina.Compliance.NIS2.Model;

using LanguageExt;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using static LanguageExt.Prelude;

namespace Encina.Compliance.NIS2;

/// <summary>
/// Default implementation of <see cref="INIS2ComplianceValidator"/> that aggregates results
/// from all registered <see cref="INIS2MeasureEvaluator"/> implementations.
/// </summary>
/// <remarks>
/// <para>
/// Records OpenTelemetry metrics and activity spans for aggregate compliance validations
/// and individual measure evaluations.
/// </para>
/// </remarks>
internal sealed class DefaultNIS2ComplianceValidator : INIS2ComplianceValidator
{
    private readonly IEnumerable<INIS2MeasureEvaluator> _evaluators;
    private readonly IOptions<NIS2Options> _options;
    private readonly TimeProvider _timeProvider;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DefaultNIS2ComplianceValidator> _logger;

    public DefaultNIS2ComplianceValidator(
        IEnumerable<INIS2MeasureEvaluator> evaluators,
        IOptions<NIS2Options> options,
        TimeProvider timeProvider,
        IServiceProvider serviceProvider,
        ILogger<DefaultNIS2ComplianceValidator> logger)
    {
        ArgumentNullException.ThrowIfNull(evaluators);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _evaluators = evaluators;
        _options = options;
        _timeProvider = timeProvider;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    private const string CacheKeyPrefix = "nis2:compliance:";

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, NIS2ComplianceResult>> ValidateAsync(
        CancellationToken cancellationToken = default)
    {
        var opts = _options.Value;
        var entityTypeName = opts.EntityType.ToString();
        var sectorName = opts.Sector.ToString();
        var evaluatorList = _evaluators.ToList();

        // Resolve cache provider once for this validation run
        var cache = opts.ComplianceCacheTTL > TimeSpan.Zero
            ? _serviceProvider.GetService<ICacheProvider>()
            : null;

        // Build cache key — includes TenantId when multi-tenancy is active
        var tenantId = _serviceProvider.GetService<IRequestContext>()?.TenantId;
        var cacheKey = tenantId is not null
            ? $"{CacheKeyPrefix}{tenantId}:{entityTypeName}:{sectorName}"
            : $"{CacheKeyPrefix}{entityTypeName}:{sectorName}";

        // Try cache first
        if (cache is not null)
        {
            var cached = await TryGetFromCacheAsync(cache, cacheKey, opts.ExternalCallTimeout, cancellationToken)
                .ConfigureAwait(false);
            if (cached is not null)
            {
                _logger.ComplianceCacheHit(cacheKey);
                return Right<EncinaError, NIS2ComplianceResult>(cached);
            }
        }

        using var activity = NIS2Diagnostics.StartComplianceCheck(entityTypeName, sectorName);
        var startTimestamp = Stopwatch.GetTimestamp();

        _logger.ComplianceValidationStarted(entityTypeName, sectorName, evaluatorList.Count);

        try
        {
            var context = new NIS2MeasureContext
            {
                Options = opts,
                TimeProvider = _timeProvider,
                ServiceProvider = _serviceProvider,
                TenantId = tenantId
            };

            var results = new List<NIS2MeasureResult>();

            foreach (var evaluator in evaluatorList)
            {
                var measureName = evaluator.Measure.ToString();

                using var measureActivity = NIS2Diagnostics.StartMeasureEvaluation(measureName);
                var measureStart = Stopwatch.GetTimestamp();

                try
                {
                    var result = await evaluator.EvaluateAsync(context, cancellationToken);
                    var measureResult = result.Match(
                        Right: r => r,
                        Left: error => NIS2MeasureResult.NotSatisfied(
                            evaluator.Measure,
                            $"Evaluation failed: {error.Message}",
                            [$"Resolve evaluation error: {error.Message}"]));

                    results.Add(measureResult);

                    // Record measure metrics
                    var measureOutcome = measureResult.IsSatisfied ? "satisfied" : "not_satisfied";
                    NIS2Diagnostics.MeasureEvaluationsTotal.Add(1,
                        new KeyValuePair<string, object?>(NIS2Diagnostics.TagMeasure, measureName),
                        new KeyValuePair<string, object?>(NIS2Diagnostics.TagOutcome, measureOutcome));

                    var measureElapsed = Stopwatch.GetElapsedTime(measureStart).TotalMilliseconds;
                    NIS2Diagnostics.MeasureEvaluationDuration.Record(measureElapsed,
                        new KeyValuePair<string, object?>(NIS2Diagnostics.TagMeasure, measureName));

                    _logger.MeasureEvaluated(measureName, measureResult.IsSatisfied, measureResult.Details);

                    if (measureResult.IsSatisfied)
                    {
                        NIS2Diagnostics.RecordCompleted(measureActivity);
                    }
                    else
                    {
                        NIS2Diagnostics.RecordFailed(measureActivity, measureResult.Details);
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.MeasureEvaluationFailed(measureName, ex);
                    NIS2Diagnostics.RecordFailed(measureActivity, ex.Message);

                    NIS2Diagnostics.MeasureEvaluationsTotal.Add(1,
                        new KeyValuePair<string, object?>(NIS2Diagnostics.TagMeasure, measureName),
                        new KeyValuePair<string, object?>(NIS2Diagnostics.TagOutcome, "error"));

                    results.Add(NIS2MeasureResult.NotSatisfied(
                        evaluator.Measure,
                        $"Evaluation failed: {ex.Message}",
                        [$"Resolve evaluation error: {ex.Message}"]));
                }
            }

            var complianceResult = NIS2ComplianceResult.Create(
                opts.EntityType,
                opts.Sector,
                results,
                _timeProvider.GetUtcNow());

            // Record aggregate compliance metrics
            var complianceOutcome = complianceResult.IsCompliant ? "compliant" : "non_compliant";
            NIS2Diagnostics.ComplianceChecksTotal.Add(1,
                new KeyValuePair<string, object?>(NIS2Diagnostics.TagOutcome, complianceOutcome),
                new KeyValuePair<string, object?>(NIS2Diagnostics.TagEntityType, entityTypeName),
                new KeyValuePair<string, object?>(NIS2Diagnostics.TagSector, sectorName));

            var elapsedMs = Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;
            NIS2Diagnostics.ComplianceCheckDuration.Record(elapsedMs,
                new KeyValuePair<string, object?>(NIS2Diagnostics.TagEntityType, entityTypeName));

            var satisfiedCount = results.Count(r => r.IsSatisfied);
            _logger.ComplianceValidationCompleted(
                complianceResult.IsCompliant,
                satisfiedCount,
                complianceResult.MissingCount,
                complianceResult.CompliancePercentage);

            NIS2Diagnostics.RecordCompleted(activity);

            // Store in cache if available — awaited to ensure the result is cached
            // before returning, preventing duplicate evaluations on concurrent requests
            if (cache is not null)
            {
                await TrySetInCacheAsync(cache, cacheKey, complianceResult, opts.ComplianceCacheTTL,
                    opts.ExternalCallTimeout, cancellationToken).ConfigureAwait(false);
            }

            return Right<EncinaError, NIS2ComplianceResult>(complianceResult);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.ComplianceValidationError(ex);
            NIS2Diagnostics.RecordFailed(activity, ex.Message);

            NIS2Diagnostics.ComplianceChecksTotal.Add(1,
                new KeyValuePair<string, object?>(NIS2Diagnostics.TagOutcome, "error"),
                new KeyValuePair<string, object?>(NIS2Diagnostics.TagEntityType, entityTypeName),
                new KeyValuePair<string, object?>(NIS2Diagnostics.TagSector, sectorName));

            return NIS2Errors.ComplianceCheckFailed(0, ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<NIS2Measure>>> GetMissingRequirementsAsync(
        CancellationToken cancellationToken = default)
    {
        var result = await ValidateAsync(cancellationToken);

        return result.Match(
            Right: r =>
            {
                _logger.MissingRequirementsQueried(r.MissingCount);
                return Right<EncinaError, IReadOnlyList<NIS2Measure>>(r.MissingMeasures);
            },
            Left: error => Left<EncinaError, IReadOnlyList<NIS2Measure>>(error));
    }

    /// <summary>
    /// Attempts to retrieve a cached compliance result using resilience protection.
    /// Returns <c>null</c> if key is not found or on any failure.
    /// </summary>
    private async ValueTask<NIS2ComplianceResult?> TryGetFromCacheAsync(
        ICacheProvider cache,
        string cacheKey,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        return await NIS2ResilienceHelper.ExecuteAsync(
            _serviceProvider,
            async ct => await cache.GetAsync<NIS2ComplianceResult>(cacheKey, ct).ConfigureAwait(false),
            fallback: (NIS2ComplianceResult?)null,
            timeout,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Awaited cache storage with resilience protection. Never throws.
    /// </summary>
    private async ValueTask TrySetInCacheAsync(
        ICacheProvider cache,
        string cacheKey,
        NIS2ComplianceResult result,
        TimeSpan ttl,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        await NIS2ResilienceHelper.ExecuteAsync(
            _serviceProvider,
            async ct =>
            {
                await cache.SetAsync(cacheKey, result, ttl, ct).ConfigureAwait(false);
                _logger.ComplianceResultCached(cacheKey, (int)ttl.TotalMinutes);
            },
            timeout,
            cancellationToken).ConfigureAwait(false);
    }
}
