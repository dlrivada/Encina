using System.Collections.Concurrent;
using System.Reflection;

namespace Encina.Compliance.DataSubjectRights;

/// <summary>
/// Default implementation of <see cref="IDataSubjectIdExtractor"/> that uses reflection
/// to extract the data subject identifier from request types.
/// </summary>
/// <remarks>
/// <para>
/// The extraction strategy follows this priority order:
/// <list type="number">
/// <item>If the request type has <see cref="RestrictProcessingAttribute"/> with a
/// <see cref="RestrictProcessingAttribute.SubjectIdProperty"/>, use that property.</item>
/// <item>Look for a property named <c>SubjectId</c> on the request type.</item>
/// <item>Look for a property named <c>UserId</c> on the request type.</item>
/// <item>Fall back to <see cref="IRequestContext.UserId"/>.</item>
/// </list>
/// </para>
/// <para>
/// Property lookups are cached per request type using a <see cref="ConcurrentDictionary{TKey,TValue}"/>
/// to avoid repeated reflection in hot paths.
/// </para>
/// </remarks>
public sealed class DefaultDataSubjectIdExtractor : IDataSubjectIdExtractor
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo?> PropertyCache = new();

    /// <inheritdoc />
    public string? ExtractSubjectId<TRequest>(TRequest request, IRequestContext context)
        where TRequest : notnull
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        var requestType = typeof(TRequest);
        var property = PropertyCache.GetOrAdd(requestType, ResolveProperty);

        if (property is not null)
        {
            var value = property.GetValue(request);
            if (value is string stringValue && !string.IsNullOrWhiteSpace(stringValue))
            {
                return stringValue;
            }
        }

        return context.UserId;
    }

    private static PropertyInfo? ResolveProperty(Type requestType)
    {
        // Priority 1: Check for RestrictProcessingAttribute with explicit SubjectIdProperty
        var restrictAttribute = requestType.GetCustomAttribute<RestrictProcessingAttribute>();
        if (restrictAttribute?.SubjectIdProperty is { Length: > 0 } propertyName)
        {
            var specified = requestType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (specified is not null && specified.PropertyType == typeof(string))
            {
                return specified;
            }
        }

        // Priority 2: Look for SubjectId property
        var subjectIdProp = requestType.GetProperty("SubjectId", BindingFlags.Public | BindingFlags.Instance);
        if (subjectIdProp is not null && subjectIdProp.PropertyType == typeof(string))
        {
            return subjectIdProp;
        }

        // Priority 3: Look for UserId property
        var userIdProp = requestType.GetProperty("UserId", BindingFlags.Public | BindingFlags.Instance);
        if (userIdProp is not null && userIdProp.PropertyType == typeof(string))
        {
            return userIdProp;
        }

        // Priority 4: Fall back to IRequestContext.UserId (handled by the caller)
        return null;
    }
}
