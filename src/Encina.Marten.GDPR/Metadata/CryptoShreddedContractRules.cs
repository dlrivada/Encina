using System.Reflection;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Encina.Marten.GDPR;

/// <summary>
/// The System.Text.Json contract rules of the classifier: what the serializer actually writes and reads for
/// a type, checked against its reflection shape.
/// </summary>
/// <remarks>
/// The rules run for every <see cref="JsonTypeInfoKind"/>, so an owner written by a converter or as a
/// collection is rejected instead of skipped. Owners get the owner rules; any type whose member graph reaches
/// an owner gets the container rules (converters, source-generated contracts, computed members, hashed sets
/// of value-equality owners and dictionary keys).
/// </remarks>
internal static class CryptoShreddedContractRules
{
    private const string TypeLevelMember = "(type)";

    /// <summary>
    /// Returns every problem of the contract, including the reflection problems of its shape.
    /// </summary>
    /// <param name="typeInfo">The contract System.Text.Json built for the type.</param>
    /// <param name="shape">The reflection shape of the same type.</param>
    /// <returns>The issues, deduplicated by member name.</returns>
    internal static IReadOnlyList<CryptoShreddedPropertyIssue> Classify(JsonTypeInfo typeInfo, CryptoShreddedTypeShape shape)
    {
        var problems = new Dictionary<string, CryptoShreddedPropertyProblems>(StringComparer.Ordinal);
        foreach (var issue in shape.Issues)
        {
            Add(problems, issue.PropertyName, issue.Problems);
        }

        if (shape.IsOwner)
        {
            OwnerRules(typeInfo, shape, problems);
        }

        if (CryptoShreddedPropertyClassifier.ReachesCryptoOwner(typeInfo.Type))
        {
            ContainerRules(typeInfo, shape.IsOwner, problems);
        }

        var typeName = CryptoShreddedPropertyClassifier.TypeName(typeInfo.Type);
        return [.. problems.Select(p => new CryptoShreddedPropertyIssue(typeName, p.Key, p.Value))];
    }

    private static void OwnerRules(
        JsonTypeInfo typeInfo, CryptoShreddedTypeShape shape, Dictionary<string, CryptoShreddedPropertyProblems> problems)
    {
        var typeLevel = OwnerTypeLevelProblems(typeInfo);
        foreach (var member in shape.Members)
        {
            Add(problems, member.Property.Name, typeLevel | MemberContractProblems(typeInfo, member));
        }

        if (typeInfo.Kind == JsonTypeInfoKind.Object)
        {
            AddComputedMembers(typeInfo, shape, problems, CryptoShreddedPropertyProblems.ComputedMemberBesideCryptoShredded);
        }
    }

    private static CryptoShreddedPropertyProblems OwnerTypeLevelProblems(JsonTypeInfo typeInfo)
    {
        var problems = typeInfo.Kind == JsonTypeInfoKind.Object
            ? CryptoShreddedPropertyProblems.None
            : CryptoShreddedPropertyProblems.OwnerNotSerializedAsObject;
        return problems | SourceGeneratedProblem(typeInfo);
    }

    private static CryptoShreddedPropertyProblems MemberContractProblems(JsonTypeInfo typeInfo, CryptoShreddedMember member)
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Object)
        {
            return CryptoShreddedPropertyProblems.None;
        }

        var jsonProperty = Find(typeInfo, member.Property);
        var problems = jsonProperty is null
            ? MissingFromContract(member.Property)
            : PropertyProblems(typeInfo, jsonProperty, member.Property);
        return problems | SubjectIdContractProblems(typeInfo, member);
    }

    // A property the reflection shape says is serialized but the contract lacks was dropped by the producer of
    // the contract (a source-generated context in Serialization mode writes an empty property list).
    private static CryptoShreddedPropertyProblems MissingFromContract(PropertyInfo property) =>
        CryptoShreddedPropertyClassifier.IsReflectionSerialized(property)
            ? CryptoShreddedPropertyProblems.SourceGeneratedContract
            : CryptoShreddedPropertyProblems.None;

    private static CryptoShreddedPropertyProblems PropertyProblems(
        JsonTypeInfo typeInfo, JsonPropertyInfo jsonProperty, PropertyInfo property)
    {
        var problems = jsonProperty.CustomConverter is null
            ? CryptoShreddedPropertyProblems.None
            : CryptoShreddedPropertyProblems.CustomConverterOnProperty;

        if (jsonProperty.AssociatedParameter is { } parameter)
        {
            return problems | ConstructorProblem(typeInfo, parameter, property);
        }

        return jsonProperty.Set is null && property.SetMethod is not null
            ? problems | CryptoShreddedPropertyProblems.NotDeserializable
            : problems;
    }

    private static CryptoShreddedPropertyProblems ConstructorProblem(
        JsonTypeInfo typeInfo, JsonParameterInfo parameter, PropertyInfo property) =>
        typeInfo.ConstructorAttributeProvider is ConstructorInfo constructor
        && RecordConstructorInspector.StoresUnchanged(constructor, parameter.Position, property)
            ? CryptoShreddedPropertyProblems.None
            : CryptoShreddedPropertyProblems.BoundToConstructorParameter;

    private static CryptoShreddedPropertyProblems SubjectIdContractProblems(JsonTypeInfo typeInfo, CryptoShreddedMember member)
    {
        if (member.SubjectIdProperty is null
            || !CryptoShreddedPropertyClassifier.IsReflectionSerialized(member.SubjectIdProperty))
        {
            return CryptoShreddedPropertyProblems.None;
        }

        var jsonProperty = Find(typeInfo, member.SubjectIdProperty);
        return jsonProperty is null || (jsonProperty.Set is null && jsonProperty.AssociatedParameter is null)
            ? CryptoShreddedPropertyProblems.SubjectIdPropertyNotRoundTripped
            : CryptoShreddedPropertyProblems.None;
    }

    private static void ContainerRules(
        JsonTypeInfo typeInfo, bool isOwner, Dictionary<string, CryptoShreddedPropertyProblems> problems)
    {
        Add(problems, TypeLevelMember, ContainerTypeLevelProblems(typeInfo));

        if (typeInfo.Kind == JsonTypeInfoKind.Object)
        {
            foreach (var jsonProperty in typeInfo.Properties)
            {
                Add(problems, jsonProperty.Name, ContainerMemberProblems(jsonProperty));
            }

            if (!isOwner)
            {
                AddComputedMembers(typeInfo, shape: null, problems, CryptoShreddedPropertyProblems.ComputedMemberOverCryptoGraph);
            }
        }
    }

    private static CryptoShreddedPropertyProblems ContainerTypeLevelProblems(JsonTypeInfo typeInfo)
    {
        var problems = SourceGeneratedProblem(typeInfo);
        problems |= typeInfo.Kind == JsonTypeInfoKind.None
            ? CryptoShreddedPropertyProblems.ConverterOverCryptoGraph
            : CryptoShreddedPropertyProblems.None;
        return problems | CollectionShapeProblems(typeInfo.Type);
    }

    private static CryptoShreddedPropertyProblems ContainerMemberProblems(JsonPropertyInfo jsonProperty)
    {
        var problems = jsonProperty.CustomConverter is not null && ReachesOrIsOwner(jsonProperty.PropertyType)
            ? CryptoShreddedPropertyProblems.ConverterOverCryptoGraph
            : CryptoShreddedPropertyProblems.None;
        return problems | CollectionShapeProblems(jsonProperty.PropertyType);
    }

    /// <summary>Problems of a collection type that holds owners: hashed sets of value-equality owners and owner keys.</summary>
    internal static CryptoShreddedPropertyProblems CollectionShapeProblems(Type type)
    {
        var problems = HashedSetElement(type) is { } element && HasValueEquality(element)
            ? CryptoShreddedPropertyProblems.OwnerInHashedCollection
            : CryptoShreddedPropertyProblems.None;
        return DictionaryKey(type) is { } key && ReachesOrIsOwner(key)
            ? problems | CryptoShreddedPropertyProblems.DictionaryKeyCarriesCryptoShredded
            : problems;
    }

    private static void AddComputedMembers(
        JsonTypeInfo typeInfo,
        CryptoShreddedTypeShape? shape,
        Dictionary<string, CryptoShreddedPropertyProblems> problems,
        CryptoShreddedPropertyProblems flag)
    {
        foreach (var jsonProperty in typeInfo.Properties)
        {
            if (IsComputed(jsonProperty) && !IsAttributedMember(shape, jsonProperty))
            {
                Add(problems, jsonProperty.Name, flag);
            }
        }
    }

    private static bool IsComputed(JsonPropertyInfo jsonProperty) =>
        jsonProperty.Get is not null && jsonProperty.Set is null && jsonProperty.AssociatedParameter is null;

    private static bool IsAttributedMember(CryptoShreddedTypeShape? shape, JsonPropertyInfo jsonProperty) =>
        shape is not null
        && jsonProperty.AttributeProvider is PropertyInfo property
        && shape.Members.Any(m => m.Property.Name == property.Name);

    private static CryptoShreddedPropertyProblems SourceGeneratedProblem(JsonTypeInfo typeInfo) =>
        typeInfo.OriginatingResolver is JsonSerializerContext
            ? CryptoShreddedPropertyProblems.SourceGeneratedContract
            : CryptoShreddedPropertyProblems.None;

    private static JsonPropertyInfo? Find(JsonTypeInfo typeInfo, PropertyInfo property) =>
        typeInfo.Properties.FirstOrDefault(p => p.AttributeProvider is PropertyInfo candidate && candidate.Name == property.Name);

    private static bool ReachesOrIsOwner(Type type) =>
        CryptoShreddedPropertyClassifier.IsOwner(type) || CryptoShreddedPropertyClassifier.ReachesCryptoOwner(type);

    /// <summary>The element type of a hashed or sorted set (<c>ISet&lt;T&gt;</c> or <c>IReadOnlySet&lt;T&gt;</c>).</summary>
    internal static Type? HashedSetElement(Type type) =>
        (GenericInterfaceArguments(type, typeof(ISet<>)) ?? GenericInterfaceArguments(type, typeof(IReadOnlySet<>)))?[0];

    private static Type? DictionaryKey(Type type) =>
        (GenericInterfaceArguments(type, typeof(IDictionary<,>)) ?? GenericInterfaceArguments(type, typeof(IReadOnlyDictionary<,>)))?[0];

    private static Type[]? GenericInterfaceArguments(Type type, Type genericInterface)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == genericInterface)
        {
            return type.GetGenericArguments();
        }

        return type.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == genericInterface)?
            .GetGenericArguments();
    }

    /// <summary>
    /// Gets whether an owner's hash or order can depend on its properties: a record, an override of
    /// <c>Equals</c>/<c>GetHashCode</c>, or <c>IComparable</c>.
    /// </summary>
    internal static bool HasValueEquality(Type type)
    {
        if (!CryptoShreddedPropertyClassifier.IsOwner(type))
        {
            return false;
        }

        return type.GetMethod("<Clone>$", BindingFlags.Public | BindingFlags.Instance) is not null
            || OverridesObjectMethod(type, nameof(GetHashCode), Type.EmptyTypes)
            || OverridesObjectMethod(type, nameof(Equals), [typeof(object)])
            || typeof(IComparable).IsAssignableFrom(type)
            || type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IComparable<>));
    }

    private static bool OverridesObjectMethod(Type type, string name, Type[] parameters) =>
        type.GetMethod(name, BindingFlags.Public | BindingFlags.Instance, parameters) is { } method
        && method.DeclaringType != typeof(object);

    private static void Add(
        Dictionary<string, CryptoShreddedPropertyProblems> problems, string member, CryptoShreddedPropertyProblems flags)
    {
        if (flags == CryptoShreddedPropertyProblems.None)
        {
            return;
        }

        problems[member] = problems.TryGetValue(member, out var existing) ? existing | flags : flags;
    }
}
