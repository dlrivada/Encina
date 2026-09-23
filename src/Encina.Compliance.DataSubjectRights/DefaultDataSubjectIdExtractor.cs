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
/// A matching property's value is converted to a stable, culture-invariant string. Supported
/// shapes are <see cref="string"/>, <see cref="Guid"/> (<c>"D"</c> format), integer types, strongly-typed
/// ids that implement <see cref="IFormattable"/>, and wrappers (record struct, record class or struct)
/// exposing a public <c>Value</c> property of one of those primitive types, which is unwrapped.
/// </para>
/// <para>
/// The fallback to <see cref="IRequestContext.UserId"/> only happens when <em>no</em> matching
/// property exists at all. A matching property whose value is <c>null</c>, <see cref="Guid.Empty"/>
/// or an empty string is a missing subject (returns <c>null</c>); numeric <c>0</c> is a valid id.
/// A matching property of an unsupported type (for example <see cref="double"/>, an enum, or a
/// wrapper without a supported <c>Value</c>) is a configuration error and throws
/// <see cref="InvalidOperationException"/> rather than silently falling back to the authenticated
/// caller (project history: #1149).
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

        // No matching property at all — fall back to the authenticated caller.
        if (property is null)
        {
            return context.UserId;
        }

        // A matching property was found: convert its value (or treat null/unconvertible
        // types per SubjectIdConversion's contract) instead of falling back to context.UserId.
        var value = property.GetValue(request);
        return SubjectIdConversion.ToInvariantString(value, property);
    }

    private static PropertyInfo? ResolveProperty(Type requestType)
    {
        // Priority 1: Check for RestrictProcessingAttribute with explicit SubjectIdProperty
        var restrictAttribute = requestType.GetCustomAttribute<RestrictProcessingAttribute>();
        if (restrictAttribute?.SubjectIdProperty is { Length: > 0 } propertyName)
        {
            var specified = requestType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (specified is not null)
            {
                return specified;
            }
        }

        // Priority 2: Look for SubjectId property
        var subjectIdProp = requestType.GetProperty("SubjectId", BindingFlags.Public | BindingFlags.Instance);
        if (subjectIdProp is not null)
        {
            return subjectIdProp;
        }

        // Priority 3: Look for UserId property
        var userIdProp = requestType.GetProperty("UserId", BindingFlags.Public | BindingFlags.Instance);
        if (userIdProp is not null)
        {
            return userIdProp;
        }

        // Priority 4: Fall back to IRequestContext.UserId (handled by the caller)
        return null;
    }
}
