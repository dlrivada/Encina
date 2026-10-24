using System.Linq.Expressions;
using System.Reflection;

namespace Encina.Security.BreakTheGlass;

/// <summary>
/// The per-request-type result of reading <see cref="BreakTheGlassEligibleAttribute"/>: the scope and a
/// compiled subject-id accessor, built once per type.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
internal static class BreakTheGlassEligibility<TRequest>
    where TRequest : notnull
{
    /// <summary>
    /// The eligibility of <typeparamref name="TRequest"/>, or <see langword="null"/> when the type is not marked.
    /// </summary>
    /// <remarks>
    /// A marked type with a missing, unreadable or unconvertible subject property makes the type
    /// initializer throw on first use: a misconfigured type never silently matches.
    /// </remarks>
#pragma warning disable CA1000 // Static members on a generic type are the per-type cache
    public static BreakTheGlassEligibilityInfo<TRequest>? Info { get; } = Build();
#pragma warning restore CA1000

    private static BreakTheGlassEligibilityInfo<TRequest>? Build()
    {
        var attribute = typeof(TRequest).GetCustomAttribute<BreakTheGlassEligibleAttribute>(inherit: true);
        if (attribute is null)
        {
            return null;
        }

        var property = FindSubjectProperty(attribute.SubjectIdProperty);

        if (!BreakTheGlassSubjectIdConversion.IsSupportedType(property.PropertyType))
        {
            throw new InvalidOperationException(
                $"The subject id property of '{typeof(TRequest).Name}' has a type that cannot be converted to a subject id.");
        }

        var parameter = Expression.Parameter(typeof(TRequest), "request");
        var read = Expression.Lambda<Func<TRequest, object?>>(
            Expression.Convert(Expression.Property(parameter, property), typeof(object)),
            parameter).Compile();

        return new BreakTheGlassEligibilityInfo<TRequest>(attribute.Scope, request => BreakTheGlassSubjectIdConversion.ToInvariantString(read(request), property));
    }

    private static PropertyInfo FindSubjectProperty(string? propertyName)
    {
        var property = string.IsNullOrWhiteSpace(propertyName)
            ? null
            : typeof(TRequest).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        if (property is null || !property.CanRead || property.GetIndexParameters().Length != 0)
        {
            throw new InvalidOperationException(
                $"Request type '{typeof(TRequest).Name}' is marked [BreakTheGlassEligible] but names no readable public instance property in SubjectIdProperty.");
        }

        return property;
    }

}

/// <summary>
/// The scope and subject accessor of an eligible request type.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <param name="Scope">The scope named by the attribute.</param>
/// <param name="GetSubjectId">Reads the subject id from a request (may return null or blank).</param>
internal sealed record BreakTheGlassEligibilityInfo<TRequest>(string Scope, Func<TRequest, string?> GetSubjectId)
    where TRequest : notnull;
