using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reflection;
using System.Text.Json.Serialization;

using Encina.Compliance.DataSubjectRights;

namespace Encina.Marten.GDPR;

/// <summary>
/// The single classifier of <see cref="CryptoShreddedAttribute"/> shapes, shared by the contract modifier, the
/// startup validator, the health check and the logs.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="GetShape"/> applies the reflection rules (cached per type). The contract rules live in
/// <see cref="CryptoShreddedContractRules"/>. Every rule is a small function and all problems of a member are
/// combined, so one failure lists everything to fix.
/// </para>
/// <para>
/// The hierarchy walk uses <c>DeclaredOnly|Public|NonPublic|Instance</c> along the <c>BaseType</c> chain:
/// non-public members are found, an override is classified once (on its most-derived declaration) and a
/// <c>new</c> property that hides an attributed one is reported instead of throwing
/// <see cref="AmbiguousMatchException"/>.
/// </para>
/// </remarks>
internal static class CryptoShreddedPropertyClassifier
{
    internal const BindingFlags DeclaredMembers =
        BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    private static readonly ConcurrentDictionary<Type, CryptoShreddedTypeShape> Shapes = new();
    private static readonly ConcurrentDictionary<Type, bool> Reaches = new();

    private static readonly Func<MemberContext, CryptoShreddedPropertyProblems>[] MemberRules =
    [
        RuleValueType,
        RuleInterface,
        RuleString,
        RulePersonalData,
        RuleReadable,
        RuleSetter,
        RuleSerialized,
        RuleConverter,
        RuleImplementsUnattributed,
        RuleOnDeserialized,
        RuleSubjectId,
    ];

    /// <summary>
    /// Gets the reflection shape of a type (cached).
    /// </summary>
    /// <param name="type">The type to classify.</param>
    /// <returns>The shape; an empty shape for types that hold no crypto-shredded member.</returns>
    internal static CryptoShreddedTypeShape GetShape(Type type) => Shapes.GetOrAdd(type, static t => BuildShape(t));

    /// <summary>Gets whether the type declares or inherits a <c>[CryptoShredded]</c> property.</summary>
    internal static bool IsOwner(Type type) => !IsTerminal(type) && GetShape(type).IsOwner;

    /// <summary>
    /// Gets whether the reflection member graph of <paramref name="type"/> (serialized properties, generic
    /// arguments, array elements and <c>[JsonDerivedType]</c> types, but not the type itself) reaches an owner.
    /// </summary>
    internal static bool ReachesCryptoOwner(Type type) =>
        !IsTerminal(type) && Reaches.GetOrAdd(type, static t => ReachesFrom(t, new HashSet<Type> { t }));

    /// <summary>Clears the caches. Intended for test isolation only.</summary>
    internal static void ResetForTests()
    {
        Shapes.Clear();
        Reaches.Clear();
    }

    /// <summary>
    /// Gets whether a type never holds crypto-shredded data (primitives, enums, strings, non-generic BCL types).
    /// </summary>
    internal static bool IsTerminal(Type type) =>
        type.IsPrimitive
        || type.IsEnum
        || type.IsPointer
        || type.IsGenericParameter
        || type == typeof(string)
        || type == typeof(object)
        || (!type.IsGenericType && !type.IsArray && IsSystemNamespace(type));

    private static bool IsSystemNamespace(Type type) =>
        type.Namespace is { } ns && (ns == "System" || ns.StartsWith("System.", StringComparison.Ordinal));

    private static CryptoShreddedTypeShape BuildShape(Type type)
    {
        if (IsTerminal(type))
        {
            return new CryptoShreddedTypeShape(type, [], [], [], []);
        }

        var collected = CollectAttributedProperties(type);
        var issues = new List<CryptoShreddedPropertyIssue>(collected.HiddenIssues);
        issues.AddRange(AttributeOnlyOnInterfaceIssues(type));

        var members = ImmutableArray.CreateBuilder<CryptoShreddedMember>();
        var fields = ImmutableArray.CreateBuilder<CryptoShreddedField>();
        var deferred = ImmutableArray.CreateBuilder<string>();

        foreach (var property in collected.Attributed)
        {
            var member = ClassifyMember(type, property);
            members.Add(member);
            AddMemberOutcome(type, member, issues, fields, deferred);
        }

        return new CryptoShreddedTypeShape(
            type, members.ToImmutable(), fields.ToImmutable(), [.. issues], deferred.ToImmutable());
    }

    private static void AddMemberOutcome(
        Type type,
        CryptoShreddedMember member,
        List<CryptoShreddedPropertyIssue> issues,
        ImmutableArray<CryptoShreddedField>.Builder fields,
        ImmutableArray<string>.Builder deferred)
    {
        if (member.Problems != CryptoShreddedPropertyProblems.None)
        {
            issues.Add(new CryptoShreddedPropertyIssue(TypeName(type), member.Property.Name, member.Problems));
            return;
        }

        if (type.ContainsGenericParameters)
        {
            deferred.Add(member.Property.Name);
            return;
        }

        fields.Add(new CryptoShreddedField(member.Property, member.SubjectIdProperty!, PersonalDataOf(member.Property)!));
    }

    private static CryptoShreddedMember ClassifyMember(Type type, PropertyInfo property)
    {
        var subjectId = FindSubjectIdProperty(type, AttributeOf(property)!.SubjectIdProperty);
        var context = new MemberContext(type, property, subjectId);
        var problems = CryptoShreddedPropertyProblems.None;
        foreach (var rule in MemberRules)
        {
            problems |= rule(context);
        }

        return new CryptoShreddedMember(property, subjectId, problems);
    }

    /// <summary>
    /// Walks the hierarchy and returns the most-derived declaration of every attributed property, plus an
    /// issue for each attributed property hidden by an unattributed <c>new</c> property.
    /// </summary>
    private static (List<PropertyInfo> Attributed, List<CryptoShreddedPropertyIssue> HiddenIssues) CollectAttributedProperties(Type type)
    {
        var attributed = new List<PropertyInfo>();
        var hidden = new List<CryptoShreddedPropertyIssue>();
        var declared = new Dictionary<string, PropertyInfo>(StringComparer.Ordinal);
        var seenBaseDefinitions = new HashSet<MethodInfo>();

        for (var current = type; current is not null && current != typeof(object); current = current.BaseType)
        {
            foreach (var property in current.GetProperties(DeclaredMembers))
            {
                VisitProperty(type, property, declared, seenBaseDefinitions, attributed, hidden);
            }
        }

        return (attributed, hidden);
    }

    private static void VisitProperty(
        Type type,
        PropertyInfo property,
        Dictionary<string, PropertyInfo> declared,
        HashSet<MethodInfo> seenBaseDefinitions,
        List<PropertyInfo> attributed,
        List<CryptoShreddedPropertyIssue> hidden)
    {
        var isOverrideOfSeen = BaseDefinition(property) is { } baseDefinition && !seenBaseDefinitions.Add(baseDefinition);
        if (declared.TryGetValue(property.Name, out var derived))
        {
            ReportHiddenIfNeeded(type, property, derived, isOverrideOfSeen, hidden);
            return;
        }

        declared[property.Name] = property;
        if (AttributeOf(property) is not null)
        {
            attributed.Add(property);
        }
    }

    private static void ReportHiddenIfNeeded(
        Type type, PropertyInfo baseProperty, PropertyInfo derived, bool isOverride, List<CryptoShreddedPropertyIssue> hidden)
    {
        if (!isOverride && AttributeOf(baseProperty) is not null && AttributeOf(derived) is null)
        {
            hidden.Add(new CryptoShreddedPropertyIssue(
                TypeName(type), baseProperty.Name, CryptoShreddedPropertyProblems.HiddenByDerivedProperty));
        }
    }

    private static IEnumerable<CryptoShreddedPropertyIssue> AttributeOnlyOnInterfaceIssues(Type type)
    {
        if (type.IsInterface)
        {
            return [];
        }

        return type.GetInterfaces()
            .SelectMany(interfaceType => UnattributedImplementations(type, interfaceType))
            .Select(name => new CryptoShreddedPropertyIssue(
                TypeName(type), name, CryptoShreddedPropertyProblems.AttributeOnlyOnInterface));
    }

    private static IEnumerable<string> UnattributedImplementations(Type type, Type interfaceType)
    {
        foreach (var interfaceProperty in interfaceType.GetProperties())
        {
            if (AttributeOf(interfaceProperty) is not null
                && ImplementingProperty(type, interfaceType, interfaceProperty) is { } implementation
                && AttributeOf(implementation) is null)
            {
                yield return implementation.Name;
            }
        }
    }

    private static PropertyInfo? ImplementingProperty(Type type, Type interfaceType, PropertyInfo interfaceProperty)
    {
        var target = InterfaceTarget(type, interfaceType, interfaceProperty.GetMethod);
        return target is null ? null : FindPropertyByGetter(target);
    }

    private static MethodInfo? InterfaceTarget(Type type, Type interfaceType, MethodInfo? interfaceMethod)
    {
        if (interfaceMethod is null || type.ContainsGenericParameters)
        {
            return null;
        }

        var map = type.GetInterfaceMap(interfaceType);
        var index = Array.IndexOf(map.InterfaceMethods, interfaceMethod);
        return index < 0 ? null : map.TargetMethods[index];
    }

    private static PropertyInfo? FindPropertyByGetter(MethodInfo getter) =>
        getter.DeclaringType?.GetProperties(DeclaredMembers).FirstOrDefault(p => p.GetMethod == getter);

    // -- Member rules ------------------------------------------------------------------------------------------

    private static CryptoShreddedPropertyProblems RuleValueType(MemberContext c) =>
        c.Owner.IsValueType ? CryptoShreddedPropertyProblems.DeclaredOnValueType : CryptoShreddedPropertyProblems.None;

    private static CryptoShreddedPropertyProblems RuleInterface(MemberContext c) =>
        c.Owner.IsInterface ? CryptoShreddedPropertyProblems.DeclaredOnInterface : CryptoShreddedPropertyProblems.None;

    private static CryptoShreddedPropertyProblems RuleString(MemberContext c) =>
        c.Property.PropertyType == typeof(string) ? CryptoShreddedPropertyProblems.None : CryptoShreddedPropertyProblems.NotString;

    private static CryptoShreddedPropertyProblems RulePersonalData(MemberContext c) =>
        PersonalDataOf(c.Property) is null ? CryptoShreddedPropertyProblems.MissingPersonalData : CryptoShreddedPropertyProblems.None;

    private static CryptoShreddedPropertyProblems RuleReadable(MemberContext c)
    {
        var problems = c.Property.GetMethod is null ? CryptoShreddedPropertyProblems.NotReadable : CryptoShreddedPropertyProblems.None;
        return c.Property.GetIndexParameters().Length > 0 ? problems | CryptoShreddedPropertyProblems.Indexer : problems;
    }

    private static CryptoShreddedPropertyProblems RuleSetter(MemberContext c) =>
        c.Property.SetMethod is null ? CryptoShreddedPropertyProblems.NoSetter : CryptoShreddedPropertyProblems.None;

    private static CryptoShreddedPropertyProblems RuleSerialized(MemberContext c) =>
        IsReflectionSerialized(c.Property) ? CryptoShreddedPropertyProblems.None : CryptoShreddedPropertyProblems.NotSerialized;

    private static CryptoShreddedPropertyProblems RuleConverter(MemberContext c) =>
        c.Property.IsDefined(typeof(JsonConverterAttribute), inherit: true)
            ? CryptoShreddedPropertyProblems.CustomConverterOnProperty
            : CryptoShreddedPropertyProblems.None;

    private static CryptoShreddedPropertyProblems RuleOnDeserialized(MemberContext c) =>
        typeof(IJsonOnDeserialized).IsAssignableFrom(c.Owner)
            ? CryptoShreddedPropertyProblems.OwnerImplementsOnDeserialized
            : CryptoShreddedPropertyProblems.None;

    private static CryptoShreddedPropertyProblems RuleImplementsUnattributed(MemberContext c)
    {
        if (Attribute.GetCustomAttribute(c.Property, typeof(CryptoShreddedAttribute), inherit: false) is null
            || c.Property.GetMethod is not { } getter)
        {
            return CryptoShreddedPropertyProblems.None;
        }

        return OverridesUnattributed(getter) || ImplementsUnattributedInterfaceMember(c.Owner, getter)
            ? CryptoShreddedPropertyProblems.ImplementsUnattributedMember
            : CryptoShreddedPropertyProblems.None;
    }

    private static bool OverridesUnattributed(MethodInfo getter)
    {
        var baseDefinition = getter.GetBaseDefinition();
        return baseDefinition != getter
            && FindPropertyByGetter(baseDefinition) is { } baseProperty
            && Attribute.GetCustomAttribute(baseProperty, typeof(CryptoShreddedAttribute), inherit: false) is null;
    }

    private static bool ImplementsUnattributedInterfaceMember(Type owner, MethodInfo getter)
    {
        if (owner.IsInterface || owner.ContainsGenericParameters)
        {
            return false;
        }

        return owner.GetInterfaces().Any(interfaceType => InterfacePropertyFor(owner, interfaceType, getter) is { } interfaceProperty
            && AttributeOf(interfaceProperty) is null);
    }

    private static PropertyInfo? InterfacePropertyFor(Type owner, Type interfaceType, MethodInfo getter)
    {
        var map = owner.GetInterfaceMap(interfaceType);
        var index = Array.IndexOf(map.TargetMethods, getter);
        return index < 0 ? null : interfaceType.GetProperties().FirstOrDefault(p => p.GetMethod == map.InterfaceMethods[index]);
    }

    private static CryptoShreddedPropertyProblems RuleSubjectId(MemberContext c)
    {
        var subjectId = c.SubjectIdProperty;
        if (subjectId is null)
        {
            return CryptoShreddedPropertyProblems.SubjectIdPropertyNotFound;
        }

        var problems = subjectId.GetMethod is null
            ? CryptoShreddedPropertyProblems.SubjectIdPropertyNotReadable
            : CryptoShreddedPropertyProblems.None;
        problems |= AttributeOf(subjectId) is not null
            ? CryptoShreddedPropertyProblems.SubjectIdPropertyIsCryptoShredded
            : CryptoShreddedPropertyProblems.None;
        problems |= IsReflectionSerialized(subjectId)
            ? CryptoShreddedPropertyProblems.None
            : CryptoShreddedPropertyProblems.SubjectIdPropertyNotRoundTripped;
        return problems | SubjectIdTypeProblem(subjectId.PropertyType);
    }

    private static CryptoShreddedPropertyProblems SubjectIdTypeProblem(Type subjectIdType) =>
        subjectIdType.ContainsGenericParameters || SubjectIdConversion.IsSupportedType(subjectIdType)
            ? CryptoShreddedPropertyProblems.None
            : CryptoShreddedPropertyProblems.SubjectIdTypeUnsupported;

    // -- Helpers -----------------------------------------------------------------------------------------------

    /// <summary>
    /// Finds the subject-id sibling: the most-derived declaration of that name along the <c>BaseType</c> chain.
    /// </summary>
    internal static PropertyInfo? FindSubjectIdProperty(Type type, string name)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            var match = current.GetProperties(DeclaredMembers)
                .FirstOrDefault(p => p.Name == name && p.GetIndexParameters().Length == 0);
            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets whether System.Text.Json writes the property by default: a public getter, or <c>[JsonInclude]</c>,
    /// and no <c>[JsonIgnore]</c> with <see cref="JsonIgnoreCondition.Always"/>.
    /// </summary>
    internal static bool IsReflectionSerialized(PropertyInfo property)
    {
        if (property.GetCustomAttribute<JsonIgnoreAttribute>(inherit: true) is { Condition: JsonIgnoreCondition.Always })
        {
            return false;
        }

        return property.GetMethod is { IsPublic: true } || property.IsDefined(typeof(JsonIncludeAttribute), inherit: true);
    }

    internal static CryptoShreddedAttribute? AttributeOf(PropertyInfo property) =>
        (CryptoShreddedAttribute?)Attribute.GetCustomAttribute(property, typeof(CryptoShreddedAttribute), inherit: true);

    internal static PersonalDataAttribute? PersonalDataOf(PropertyInfo property) =>
        (PersonalDataAttribute?)Attribute.GetCustomAttribute(property, typeof(PersonalDataAttribute), inherit: true);

    internal static string TypeName(Type type) => type.FullName ?? type.Name;

    private static MethodInfo? BaseDefinition(PropertyInfo property) =>
        (property.GetMethod ?? property.SetMethod)?.GetBaseDefinition();

    private static bool ReachesFrom(Type type, HashSet<Type> inProgress)
    {
        foreach (var component in ComponentTypes(type))
        {
            if (IsTerminal(component) || !inProgress.Add(component))
            {
                continue;
            }

            if (IsOwner(component) || ReachesFrom(component, inProgress))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// The types reachable in one step: array element, generic arguments, serialized property types and
    /// <c>[JsonDerivedType]</c> types.
    /// </summary>
    internal static IEnumerable<Type> ComponentTypes(Type type)
    {
        if (type.GetElementType() is { } element)
        {
            yield return element;
        }

        foreach (var argument in type.IsGenericType ? type.GetGenericArguments() : [])
        {
            yield return argument;
        }

        foreach (var derived in type.GetCustomAttributes<JsonDerivedTypeAttribute>(inherit: false))
        {
            yield return derived.DerivedType;
        }

        foreach (var property in SerializedProperties(type))
        {
            yield return property.PropertyType;
        }
    }

    private static IEnumerable<PropertyInfo> SerializedProperties(Type type)
    {
        if (type.IsArray || type.IsInterface && type.IsGenericType)
        {
            return [];
        }

        return type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(p => p.GetIndexParameters().Length == 0 && IsReflectionSerialized(p));
    }

    private readonly record struct MemberContext(Type Owner, PropertyInfo Property, PropertyInfo? SubjectIdProperty);
}
