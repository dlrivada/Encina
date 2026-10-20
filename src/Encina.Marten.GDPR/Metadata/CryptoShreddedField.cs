using System.Linq.Expressions;
using System.Reflection;

using Encina.Compliance.DataSubjectRights;

namespace Encina.Marten.GDPR;

/// <summary>
/// A correctly configured <see cref="CryptoShreddedAttribute"/> property of a closed owner type, with compiled
/// accessors for the property and its subject-id sibling.
/// </summary>
/// <remarks>
/// Accessors are compiled on the declaring type of the property, so private, <c>init</c> and
/// base-declared accessors work. Only the classifier creates instances, and only for properties that have
/// no problem.
/// </remarks>
internal sealed class CryptoShreddedField
{
    internal CryptoShreddedField(PropertyInfo property, PropertyInfo subjectIdProperty, PersonalDataAttribute personalData)
    {
        Property = property;
        DeclaringType = property.DeclaringType!;
        SubjectIdProperty = subjectIdProperty;
        PersonalData = personalData;
        Getter = CompileGetter(property);
        Setter = CompileSetter(property);
        SubjectIdGetter = CompileGetter(subjectIdProperty);
    }

    /// <summary>The attributed property (its most-derived declaration).</summary>
    internal PropertyInfo Property { get; }

    /// <summary>The type that declares <see cref="Property"/>.</summary>
    internal Type DeclaringType { get; }

    /// <summary>The property name.</summary>
    internal string Name => Property.Name;

    /// <summary>Reads the property value from an owner.</summary>
    internal Func<object, object?> Getter { get; }

    /// <summary>Writes the property value on an owner.</summary>
    internal Action<object, object?> Setter { get; }

    /// <summary>The subject-id sibling (its most-derived declaration).</summary>
    internal PropertyInfo SubjectIdProperty { get; }

    /// <summary>Reads the subject-id sibling from an owner.</summary>
    internal Func<object, object?> SubjectIdGetter { get; }

    /// <summary>The companion <see cref="PersonalDataAttribute"/>.</summary>
    internal PersonalDataAttribute PersonalData { get; }

    /// <summary>
    /// Reads the subject id from the owner and converts it to its invariant string form.
    /// </summary>
    /// <param name="owner">The object that declares the property.</param>
    /// <returns>The subject id, or <c>null</c> when it is missing (<c>null</c>, an empty Guid or a blank string).</returns>
    /// <exception cref="InvalidOperationException">The runtime value is not a supported subject-id type.</exception>
    internal string? ResolveSubjectId(object owner) =>
        SubjectIdConversion.ToInvariantString(SubjectIdGetter(owner), SubjectIdProperty);

    internal static Func<object, object?> CompileGetter(PropertyInfo property)
    {
        var target = Expression.Parameter(typeof(object), "target");
        var body = Expression.Convert(
            Expression.Property(Expression.Convert(target, property.DeclaringType!), property),
            typeof(object));
        return Expression.Lambda<Func<object, object?>>(body, target).Compile();
    }

    internal static Action<object, object?> CompileSetter(PropertyInfo property)
    {
        var target = Expression.Parameter(typeof(object), "target");
        var value = Expression.Parameter(typeof(object), "value");
        var assign = Expression.Assign(
            Expression.Property(Expression.Convert(target, property.DeclaringType!), property),
            Expression.Convert(value, property.PropertyType));
        return Expression.Lambda<Action<object, object?>>(assign, target, value).Compile();
    }
}
