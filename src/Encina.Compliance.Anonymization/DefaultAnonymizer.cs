using System.Collections.Concurrent;
using System.Reflection;

using Encina.Compliance.Anonymization.Model;

using LanguageExt;

using static LanguageExt.Prelude;

namespace Encina.Compliance.Anonymization;

/// <summary>
/// Default implementation of <see cref="IAnonymizer"/> that applies anonymization techniques
/// to data objects using reflection-based field discovery.
/// </summary>
/// <remarks>
/// <para>
/// For each data type, property metadata is cached in a static <see cref="ConcurrentDictionary{TKey,TValue}"/>
/// to avoid repeated reflection overhead. Properties are matched against the
/// <see cref="AnonymizationProfile.FieldRules"/> by name (case-sensitive).
/// </para>
/// <para>
/// The anonymizer delegates to registered <see cref="IAnonymizationTechnique"/> implementations
/// for the actual value transformation. Techniques are resolved by their
/// <see cref="IAnonymizationTechnique.Technique"/> property.
/// </para>
/// <para>
/// Registered via <c>TryAdd</c> in DI, allowing users to override with custom implementations.
/// </para>
/// </remarks>
public sealed class DefaultAnonymizer : IAnonymizer
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PropertyCache = new();

    private readonly Dictionary<AnonymizationTechnique, IAnonymizationTechnique> _techniques;

    /// <summary>
    /// Initializes a new instance of <see cref="DefaultAnonymizer"/> with the available techniques.
    /// </summary>
    /// <param name="techniques">The registered anonymization technique implementations.</param>
    public DefaultAnonymizer(IEnumerable<IAnonymizationTechnique> techniques)
    {
        ArgumentNullException.ThrowIfNull(techniques);

        _techniques = techniques.ToDictionary(t => t.Technique, t => t);
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, T>> AnonymizeAsync<T>(
        T data,
        AnonymizationProfile profile,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(profile);

        try
        {
            var properties = GetProperties(typeof(T));
            var rulesByField = profile.FieldRules.ToDictionary(r => r.FieldName, r => r);

            // Create a shallow copy to avoid mutating the original
            var copy = ShallowCopy(data);

            foreach (var property in properties)
            {
                if (!property.CanWrite || !rulesByField.TryGetValue(property.Name, out var rule))
                {
                    continue;
                }

                if (!_techniques.TryGetValue(rule.Technique, out var technique))
                {
                    return Left<EncinaError, T>(
                        AnonymizationErrors.TechniqueNotRegistered(rule.Technique));
                }

                if (!technique.CanApply(property.PropertyType))
                {
                    return Left<EncinaError, T>(
                        AnonymizationErrors.TechniqueNotApplicable(
                            rule.Technique, property.Name, property.PropertyType));
                }

                var currentValue = property.GetValue(copy);
                var result = await technique.ApplyAsync(
                    currentValue, property.PropertyType, rule.Parameters, cancellationToken)
                    .ConfigureAwait(false);

                if (result.IsLeft)
                {
                    return Left<EncinaError, T>(
                        (EncinaError)result.Case);
                }

                var anonymizedValue = result.Match(Right: v => v, Left: _ => currentValue);
                property.SetValue(copy, anonymizedValue);
            }

            return Right<EncinaError, T>(copy);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, T>(
                AnonymizationErrors.AnonymizationFailed(
                    typeof(T).Name, ex.Message, ex));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, AnonymizationResult>> AnonymizeFieldsAsync<T>(
        T data,
        AnonymizationProfile profile,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(profile);

        try
        {
            var properties = GetProperties(typeof(T));
            var rulesByField = profile.FieldRules.ToDictionary(r => r.FieldName, r => r);
            var techniqueApplied = new Dictionary<string, AnonymizationTechnique>();
            var skippedCount = 0;

            foreach (var property in properties)
            {
                if (!property.CanWrite || !rulesByField.TryGetValue(property.Name, out var rule))
                {
                    skippedCount++;
                    continue;
                }

                if (!_techniques.TryGetValue(rule.Technique, out var technique))
                {
                    return Left<EncinaError, AnonymizationResult>(
                        AnonymizationErrors.TechniqueNotRegistered(rule.Technique));
                }

                if (!technique.CanApply(property.PropertyType))
                {
                    skippedCount++;
                    continue;
                }

                var currentValue = property.GetValue(data);
                var result = await technique.ApplyAsync(
                    currentValue, property.PropertyType, rule.Parameters, cancellationToken)
                    .ConfigureAwait(false);

                if (result.IsLeft)
                {
                    return Left<EncinaError, AnonymizationResult>(
                        (EncinaError)result.Case);
                }

                techniqueApplied[property.Name] = rule.Technique;
            }

            return Right<EncinaError, AnonymizationResult>(new AnonymizationResult
            {
                OriginalFieldCount = properties.Length,
                AnonymizedFieldCount = techniqueApplied.Count,
                SkippedFieldCount = skippedCount,
                TechniqueApplied = techniqueApplied
            });
        }
        catch (Exception ex)
        {
            return Left<EncinaError, AnonymizationResult>(
                AnonymizationErrors.AnonymizationFailed(
                    typeof(T).Name, ex.Message, ex));
        }
    }

    /// <inheritdoc/>
    public ValueTask<Either<EncinaError, bool>> IsAnonymizedAsync<T>(
        T data,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(data);

        try
        {
            var properties = GetProperties(typeof(T));
            var nullOrDefaultCount = 0;

            foreach (var property in properties)
            {
                if (!property.CanRead)
                {
                    continue;
                }

                var value = property.GetValue(data);

                if (value is null)
                {
                    nullOrDefaultCount++;
                    continue;
                }

                if (property.PropertyType.IsValueType)
                {
                    var defaultValue = Activator.CreateInstance(property.PropertyType);
                    if (value.Equals(defaultValue))
                    {
                        nullOrDefaultCount++;
                    }
                }
                else if (value is string str && (str.Contains('*') || string.IsNullOrEmpty(str)))
                {
                    // Heuristic: masked strings contain asterisks
                    nullOrDefaultCount++;
                }
            }

            // Heuristic: if more than half the fields are null/default/masked,
            // the data appears to be anonymized
            var threshold = properties.Length / 2.0;
            var isAnonymized = nullOrDefaultCount > threshold;

            return ValueTask.FromResult<Either<EncinaError, bool>>(
                Right<EncinaError, bool>(isAnonymized));
        }
        catch (Exception ex)
        {
            return ValueTask.FromResult<Either<EncinaError, bool>>(
                Left<EncinaError, bool>(
                    AnonymizationErrors.AnonymizationFailed(
                        typeof(T).Name, ex.Message, ex)));
        }
    }

    private static PropertyInfo[] GetProperties(Type type) =>
        PropertyCache.GetOrAdd(type, t =>
            t.GetProperties(BindingFlags.Public | BindingFlags.Instance));

    private static T ShallowCopy<T>(T source)
    {
        var type = typeof(T);
        var properties = GetProperties(type);

        // Use MemberwiseClone via reflection if available, otherwise manual copy
        var cloneMethod = type.GetMethod(
            "MemberwiseClone",
            BindingFlags.Instance | BindingFlags.NonPublic);

        if (cloneMethod is not null)
        {
            return (T)cloneMethod.Invoke(source, null)!;
        }

        // Fallback: create new instance and copy properties
        var copy = Activator.CreateInstance<T>();
        foreach (var property in properties)
        {
            if (property is { CanRead: true, CanWrite: true })
            {
                property.SetValue(copy, property.GetValue(source));
            }
        }

        return copy;
    }
}
