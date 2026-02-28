using System.Collections.Concurrent;
using System.Reflection;

using Encina.Compliance.Anonymization.Model;

using LanguageExt;

using static LanguageExt.Prelude;

namespace Encina.Compliance.Anonymization;

/// <summary>
/// Default implementation of <see cref="IRiskAssessor"/> that calculates k-anonymity,
/// l-diversity, and t-closeness metrics for anonymized datasets.
/// </summary>
/// <remarks>
/// <para>
/// Computes three complementary privacy metrics:
/// <list type="bullet">
/// <item>
/// <term>K-Anonymity</term>
/// <description>Groups records by quasi-identifiers and reports the minimum group size.
/// Higher k = stronger privacy. Target: k ≥ 5 for general, k ≥ 10 for sensitive data.</description>
/// </item>
/// <item>
/// <term>L-Diversity</term>
/// <description>Counts distinct sensitive values per equivalence class.
/// Prevents homogeneity attacks. Target: l ≥ 3.</description>
/// </item>
/// <item>
/// <term>T-Closeness</term>
/// <description>Measures Earth Mover's Distance between class and global distributions.
/// Lower t = stronger privacy. Target: t ≤ 0.15.</description>
/// </item>
/// </list>
/// </para>
/// <para>
/// Property metadata is cached per type in a static <see cref="ConcurrentDictionary{TKey,TValue}"/>
/// to avoid repeated reflection overhead.
/// </para>
/// </remarks>
public sealed class DefaultRiskAssessor : IRiskAssessor
{
    private const int DefaultKTarget = 5;
    private const int DefaultLTarget = 3;
    private const double DefaultTTarget = 0.15;

    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PropertyCache = new();

    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of <see cref="DefaultRiskAssessor"/>.
    /// </summary>
    /// <param name="timeProvider">Optional time provider for testable timestamps. Defaults to <see cref="TimeProvider.System"/>.</param>
    public DefaultRiskAssessor(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc/>
    public ValueTask<Either<EncinaError, RiskAssessmentResult>> AssessAsync<T>(
        IReadOnlyList<T> dataset,
        IReadOnlyList<string> quasiIdentifiers,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dataset);
        ArgumentNullException.ThrowIfNull(quasiIdentifiers);

        if (dataset.Count < 2)
        {
            return ValueTask.FromResult<Either<EncinaError, RiskAssessmentResult>>(
                Left<EncinaError, RiskAssessmentResult>(
                    AnonymizationErrors.RiskAssessmentFailed(
                        "Dataset must contain at least 2 records for meaningful risk assessment.")));
        }

        if (quasiIdentifiers.Count == 0)
        {
            return ValueTask.FromResult<Either<EncinaError, RiskAssessmentResult>>(
                Left<EncinaError, RiskAssessmentResult>(
                    AnonymizationErrors.RiskAssessmentFailed(
                        "At least one quasi-identifier must be specified.")));
        }

        try
        {
            var properties = GetProperties(typeof(T));
            var qiProperties = ResolveProperties(properties, quasiIdentifiers);

            if (qiProperties.Count == 0)
            {
                return ValueTask.FromResult<Either<EncinaError, RiskAssessmentResult>>(
                    Left<EncinaError, RiskAssessmentResult>(
                        AnonymizationErrors.RiskAssessmentFailed(
                            $"None of the specified quasi-identifiers were found on type '{typeof(T).Name}'.")));
            }

            // Build equivalence classes based on quasi-identifier values
            var equivalenceClasses = BuildEquivalenceClasses(dataset, qiProperties);

            // Calculate k-anonymity (minimum class size)
            var kValue = CalculateKAnonymity(equivalenceClasses);

            // Calculate l-diversity (minimum distinct sensitive values per class)
            // Use all non-QI properties as potential sensitive attributes
            var sensitiveProperties = properties
                .Where(p => p.CanRead && !qiProperties.Contains(p))
                .ToList();
            var lValue = CalculateLDiversity(dataset, equivalenceClasses, sensitiveProperties);

            // Calculate t-closeness (maximum EMD across classes)
            var tDistance = CalculateTCloseness(dataset, equivalenceClasses, sensitiveProperties);

            // Calculate re-identification probability
            var reIdProbability = kValue > 0 ? 1.0 / kValue : 1.0;

            // Determine acceptability and generate recommendations
            var recommendations = new List<string>();
            var isAcceptable = true;

            if (kValue < DefaultKTarget)
            {
                isAcceptable = false;
                recommendations.Add(
                    $"K-anonymity value {kValue} is below target {DefaultKTarget}. " +
                    "Consider increasing generalization granularity for quasi-identifiers.");
            }

            if (lValue < DefaultLTarget)
            {
                isAcceptable = false;
                recommendations.Add(
                    $"L-diversity value {lValue} is below target {DefaultLTarget}. " +
                    "Consider adding more diversity to sensitive attributes within equivalence classes.");
            }

            if (tDistance > DefaultTTarget)
            {
                isAcceptable = false;
                recommendations.Add(
                    $"T-closeness distance {tDistance:F4} exceeds target {DefaultTTarget}. " +
                    "Consider adjusting anonymization to reduce distribution skew within equivalence classes.");
            }

            var result = new RiskAssessmentResult
            {
                KAnonymityValue = kValue,
                LDiversityValue = lValue,
                TClosenessDistance = tDistance,
                ReIdentificationProbability = reIdProbability,
                IsAcceptable = isAcceptable,
                AssessedAtUtc = _timeProvider.GetUtcNow(),
                Recommendations = recommendations.AsReadOnly()
            };

            return ValueTask.FromResult<Either<EncinaError, RiskAssessmentResult>>(
                Right<EncinaError, RiskAssessmentResult>(result));
        }
        catch (Exception ex)
        {
            return ValueTask.FromResult<Either<EncinaError, RiskAssessmentResult>>(
                Left<EncinaError, RiskAssessmentResult>(
                    AnonymizationErrors.RiskAssessmentFailed(ex.Message, ex)));
        }
    }

    /// <summary>
    /// Groups records by their quasi-identifier values to form equivalence classes.
    /// </summary>
    private static Dictionary<string, List<int>> BuildEquivalenceClasses<T>(
        IReadOnlyList<T> dataset,
        List<PropertyInfo> qiProperties)
    {
        var classes = new Dictionary<string, List<int>>();

        for (var i = 0; i < dataset.Count; i++)
        {
            var record = dataset[i];
            var key = BuildClassKey(record!, qiProperties);

            if (!classes.TryGetValue(key, out var indices))
            {
                indices = [];
                classes[key] = indices;
            }

            indices.Add(i);
        }

        return classes;
    }

    /// <summary>
    /// Builds a composite key from the quasi-identifier values of a record.
    /// </summary>
    private static string BuildClassKey<T>(T record, List<PropertyInfo> qiProperties)
    {
        var values = qiProperties
            .Select(p => p.GetValue(record)?.ToString() ?? "(null)");
        return string.Join("|", values);
    }

    /// <summary>
    /// K-anonymity: the minimum equivalence class size.
    /// </summary>
    private static int CalculateKAnonymity(Dictionary<string, List<int>> equivalenceClasses)
    {
        if (equivalenceClasses.Count == 0)
        {
            return 0;
        }

        return equivalenceClasses.Values.Min(c => c.Count);
    }

    /// <summary>
    /// L-diversity: the minimum number of distinct sensitive values across all equivalence classes.
    /// </summary>
    private static int CalculateLDiversity<T>(
        IReadOnlyList<T> dataset,
        Dictionary<string, List<int>> equivalenceClasses,
        List<PropertyInfo> sensitiveProperties)
    {
        if (sensitiveProperties.Count == 0 || equivalenceClasses.Count == 0)
        {
            return 1;
        }

        var minDiversity = int.MaxValue;

        foreach (var classIndices in equivalenceClasses.Values)
        {
            // Count distinct values across all sensitive attributes for this class
            var distinctValues = new System.Collections.Generic.HashSet<string>();

            foreach (var index in classIndices)
            {
                var record = dataset[index];
                foreach (var prop in sensitiveProperties)
                {
                    var value = prop.GetValue(record!)?.ToString() ?? "(null)";
                    distinctValues.Add($"{prop.Name}:{value}");
                }
            }

            // L-diversity per sensitive attribute
            foreach (var prop in sensitiveProperties)
            {
                var propDistinct = classIndices
                    .Select(i => prop.GetValue(dataset[i]!)?.ToString() ?? "(null)")
                    .Distinct()
                    .Count();

                minDiversity = Math.Min(minDiversity, propDistinct);
            }
        }

        return minDiversity == int.MaxValue ? 1 : minDiversity;
    }

    /// <summary>
    /// T-closeness: the maximum Earth Mover's Distance between any equivalence class
    /// distribution and the global distribution for sensitive attributes.
    /// </summary>
    private static double CalculateTCloseness<T>(
        IReadOnlyList<T> dataset,
        Dictionary<string, List<int>> equivalenceClasses,
        List<PropertyInfo> sensitiveProperties)
    {
        if (sensitiveProperties.Count == 0 || equivalenceClasses.Count == 0)
        {
            return 0.0;
        }

        var maxDistance = 0.0;

        foreach (var prop in sensitiveProperties)
        {
            // Build global distribution
            var globalValues = dataset
                .Select(r => prop.GetValue(r!)?.ToString() ?? "(null)")
                .ToList();
            var globalDistribution = BuildDistribution(globalValues);

            // Compare each class distribution against global
            foreach (var classIndices in equivalenceClasses.Values)
            {
                var classValues = classIndices
                    .Select(i => prop.GetValue(dataset[i]!)?.ToString() ?? "(null)")
                    .ToList();
                var classDistribution = BuildDistribution(classValues);

                var emd = CalculateEarthMoversDistance(classDistribution, globalDistribution);
                maxDistance = Math.Max(maxDistance, emd);
            }
        }

        return maxDistance;
    }

    /// <summary>
    /// Builds a normalized frequency distribution from a list of values.
    /// </summary>
    private static Dictionary<string, double> BuildDistribution(List<string> values)
    {
        var counts = new Dictionary<string, int>();

        foreach (var value in values)
        {
            counts[value] = counts.TryGetValue(value, out var count)
                ? count + 1
                : 1;
        }

        var total = (double)values.Count;
        return counts.ToDictionary(kvp => kvp.Key, kvp => kvp.Value / total);
    }

    /// <summary>
    /// Calculates a simplified Earth Mover's Distance between two distributions
    /// using the total variation distance (sum of absolute differences / 2).
    /// </summary>
    private static double CalculateEarthMoversDistance(
        Dictionary<string, double> distribution1,
        Dictionary<string, double> distribution2)
    {
        var allKeys = distribution1.Keys.Union(distribution2.Keys);
        var totalDiff = 0.0;

        foreach (var key in allKeys)
        {
            distribution1.TryGetValue(key, out var prob1);
            distribution2.TryGetValue(key, out var prob2);
            totalDiff += Math.Abs(prob1 - prob2);
        }

        // Total variation distance = sum of absolute differences / 2
        return totalDiff / 2.0;
    }

    private static PropertyInfo[] GetProperties(Type type) =>
        PropertyCache.GetOrAdd(type, t =>
            t.GetProperties(BindingFlags.Public | BindingFlags.Instance));

    private static List<PropertyInfo> ResolveProperties(
        PropertyInfo[] allProperties,
        IReadOnlyList<string> propertyNames)
    {
        var nameSet = new System.Collections.Generic.HashSet<string>(propertyNames);
        return allProperties
            .Where(p => p.CanRead && nameSet.Contains(p.Name))
            .ToList();
    }
}
