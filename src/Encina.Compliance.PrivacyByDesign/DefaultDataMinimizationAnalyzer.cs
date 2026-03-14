using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;

using Encina.Compliance.PrivacyByDesign.Diagnostics;
using Encina.Compliance.PrivacyByDesign.Model;

using LanguageExt;

using Microsoft.Extensions.Logging;

using static LanguageExt.Prelude;

namespace Encina.Compliance.PrivacyByDesign;

/// <summary>
/// Default implementation of <see cref="IDataMinimizationAnalyzer"/> that uses reflection
/// to inspect request type properties for privacy attributes.
/// </summary>
/// <remarks>
/// <para>
/// Uses a static <see cref="ConcurrentDictionary{TKey, TValue}"/> to cache reflection metadata
/// per request type, ensuring zero overhead after the first invocation for each type.
/// </para>
/// <para>
/// The analyzer inspects each property of the request type for:
/// </para>
/// <list type="bullet">
/// <item><description><see cref="NotStrictlyNecessaryAttribute"/>: Marks a field as not required for the processing purpose.</description></item>
/// <item><description><see cref="PurposeLimitationAttribute"/>: Declares the purpose for which a field is collected.</description></item>
/// <item><description><see cref="PrivacyDefaultAttribute"/>: Declares the privacy-respecting default value for a field.</description></item>
/// </list>
/// <para>
/// Per GDPR Article 25(2), "only personal data which are necessary for each specific purpose
/// of the processing are processed." This analyzer provides the evidence for that assessment.
/// </para>
/// </remarks>
internal sealed class DefaultDataMinimizationAnalyzer : IDataMinimizationAnalyzer
{
    internal static readonly ConcurrentDictionary<Type, FieldMetadataCache> MetadataCache = new();

    private readonly TimeProvider _timeProvider;
    private readonly ILogger<DefaultDataMinimizationAnalyzer> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultDataMinimizationAnalyzer"/> class.
    /// </summary>
    /// <param name="timeProvider">Time provider for deterministic timestamps.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    public DefaultDataMinimizationAnalyzer(
        TimeProvider timeProvider,
        ILogger<DefaultDataMinimizationAnalyzer> logger)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, MinimizationReport>> AnalyzeAsync<TRequest>(
        TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : notnull
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            var requestType = typeof(TRequest);
            var cache = MetadataCache.GetOrAdd(requestType, static type => FieldMetadataCache.Build(type));

            var necessaryFields = new List<PrivacyFieldInfo>();
            var unnecessaryFields = new List<UnnecessaryFieldInfo>();
            var recommendations = new List<string>();

            for (var i = 0; i < cache.Properties.Length; i++)
            {
                var property = cache.Properties[i];
                var notNecessary = cache.NotStrictlyNecessary[i];
                var purposeAttr = cache.PurposeLimitation[i];

                if (notNecessary is not null)
                {
                    var value = property.GetValue(request);
                    var hasValue = value is not null && !Equals(value, GetDefaultValue(property.PropertyType));

                    unnecessaryFields.Add(new UnnecessaryFieldInfo(
                        FieldName: property.Name,
                        Reason: notNecessary.Reason,
                        HasValue: hasValue,
                        Severity: notNecessary.Severity));

                    if (hasValue)
                    {
                        recommendations.Add(
                            $"Consider removing or making optional the field '{property.Name}': {notNecessary.Reason}");
                    }
                }
                else
                {
                    necessaryFields.Add(new PrivacyFieldInfo(
                        FieldName: property.Name,
                        Purpose: purposeAttr?.Purpose,
                        IsRequired: true));
                }
            }

            var totalFields = necessaryFields.Count + unnecessaryFields.Count;
            var score = totalFields > 0
                ? (double)necessaryFields.Count / totalFields
                : 1.0;

            var report = new MinimizationReport
            {
                RequestTypeName = requestType.FullName ?? requestType.Name,
                NecessaryFields = necessaryFields,
                UnnecessaryFields = unnecessaryFields,
                MinimizationScore = score,
                Recommendations = recommendations,
                AnalyzedAtUtc = _timeProvider.GetUtcNow(),
            };

            _logger.PbDAnalysisCompleted(report.RequestTypeName, score, necessaryFields.Count, unnecessaryFields.Count);

            // Record minimization violation metrics
            var violationsWithValue = unnecessaryFields.Count(static f => f.HasValue);
            if (violationsWithValue > 0)
            {
                PrivacyByDesignDiagnostics.MinimizationViolationsTotal.Add(
                    violationsWithValue,
                    new TagList { { PrivacyByDesignDiagnostics.TagRequestType, report.RequestTypeName } });
            }

            return ValueTask.FromResult(Right<EncinaError, MinimizationReport>(report));
        }
        catch (Exception ex)
        {
            _logger.PbDAnalysisError(typeof(TRequest).FullName ?? typeof(TRequest).Name, ex);
            return ValueTask.FromResult(Left<EncinaError, MinimizationReport>(
                PrivacyByDesignErrors.StoreError("AnalyzeMinimization", ex.Message, ex)));
        }
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, IReadOnlyList<DefaultPrivacyFieldInfo>>> InspectDefaultsAsync<TRequest>(
        TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : notnull
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            var requestType = typeof(TRequest);
            var cache = MetadataCache.GetOrAdd(requestType, static type => FieldMetadataCache.Build(type));

            var results = new List<DefaultPrivacyFieldInfo>();

            for (var i = 0; i < cache.Properties.Length; i++)
            {
                var privacyDefault = cache.PrivacyDefault[i];
                if (privacyDefault is null)
                {
                    continue;
                }

                var property = cache.Properties[i];
                var actualValue = property.GetValue(request);
                var matchesDefault = Equals(actualValue, privacyDefault.DefaultValue);

                results.Add(new DefaultPrivacyFieldInfo(
                    FieldName: property.Name,
                    DeclaredDefault: privacyDefault.DefaultValue,
                    ActualValue: actualValue,
                    MatchesDefault: matchesDefault));
            }

            var matchingCount = results.Count(static f => f.MatchesDefault);
            _logger.PbDDefaultsInspectionCompleted(
                requestType.FullName ?? requestType.Name, results.Count, matchingCount);

            // Record default override metrics
            var overrideCount = results.Count - matchingCount;
            if (overrideCount > 0)
            {
                PrivacyByDesignDiagnostics.DefaultOverridesTotal.Add(
                    overrideCount,
                    new TagList { { PrivacyByDesignDiagnostics.TagRequestType, requestType.FullName ?? requestType.Name } });
            }

            return ValueTask.FromResult(Right<EncinaError, IReadOnlyList<DefaultPrivacyFieldInfo>>(results));
        }
        catch (Exception ex)
        {
            _logger.PbDDefaultsInspectionError(typeof(TRequest).FullName ?? typeof(TRequest).Name, ex);
            return ValueTask.FromResult(Left<EncinaError, IReadOnlyList<DefaultPrivacyFieldInfo>>(
                PrivacyByDesignErrors.StoreError("InspectDefaults", ex.Message, ex)));
        }
    }

    /// <summary>
    /// Gets the CLR default value for a type (null for reference types, 0/false for value types).
    /// </summary>
    private static object? GetDefaultValue(Type type) =>
        type.IsValueType ? Activator.CreateInstance(type) : null;
}
