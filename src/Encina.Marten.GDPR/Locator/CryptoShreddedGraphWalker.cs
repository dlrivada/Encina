using System.Collections;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Encina.Marten.GDPR;

/// <summary>
/// One <c>[CryptoShredded]</c> field found in an object graph.
/// </summary>
/// <param name="Owner">The object that declares the field.</param>
/// <param name="Field">The field.</param>
/// <param name="Path">
/// The field path: dots for members, <c>[]</c> for sequence elements, <c>{}</c> for dictionary values
/// (<c>Contact.Email</c>, <c>Items[].Note</c>, <c>Notes{}.Text</c>). A path never contains an index or a key.
/// </param>
internal readonly record struct CryptoShreddedOccurrence(object Owner, CryptoShreddedField Field, string Path);

/// <summary>
/// Walks a deserialized object graph over its System.Text.Json contracts and yields every crypto field.
/// </summary>
/// <remarks>
/// The walk is iterative, visits each object once (reference identity, so cycles and shared instances are safe)
/// and stops at the options' <c>MaxDepth</c> (64 when unset). It follows object members through their original
/// getters (never the encrypting wrappers of crypto fields), sequence elements and dictionary values only.
/// </remarks>
internal static class CryptoShreddedGraphWalker
{
    private const int DefaultMaxDepth = 64;

    /// <summary>Yields every crypto field of the graph rooted at <paramref name="root"/>.</summary>
    internal static IEnumerable<CryptoShreddedOccurrence> Walk(object root, JsonSerializerOptions options, CryptoShreddingTypePlanRegistry registry)
    {
        var maxDepth = options.MaxDepth == 0 ? DefaultMaxDepth : options.MaxDepth;
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        var stack = new Stack<(object Value, string Path, int Depth)>();
        stack.Push((root, string.Empty, 0));

        while (stack.Count > 0)
        {
            var (value, path, depth) = stack.Pop();
            var occurrences = ShouldVisit(value, depth, maxDepth, visited)
                ? Visit(value, path, depth, options, registry, stack)
                : [];
            foreach (var occurrence in occurrences)
            {
                yield return occurrence;
            }
        }
    }

    private static bool ShouldVisit(object value, int depth, int maxDepth, HashSet<object> visited) =>
        depth <= maxDepth && !CryptoShreddedPropertyClassifier.IsTerminal(value.GetType()) && visited.Add(value);

    private static List<CryptoShreddedOccurrence> Visit(
        object value, string path, int depth, JsonSerializerOptions options, CryptoShreddingTypePlanRegistry registry,
        Stack<(object Value, string Path, int Depth)> stack)
    {
        var typeInfo = options.GetTypeInfo(value.GetType());
        switch (typeInfo.Kind)
        {
            case JsonTypeInfoKind.Object:
                return VisitObject(value, path, depth, typeInfo, registry.Find(typeInfo.Type), stack);
            case JsonTypeInfoKind.Enumerable:
                PushAll(Elements(value), path + "[]", depth, stack);
                return [];
            case JsonTypeInfoKind.Dictionary:
                PushAll(DictionaryValues(value), path + "{}", depth, stack);
                return [];
            default:
                return [];
        }
    }

    private static List<CryptoShreddedOccurrence> VisitObject(
        object value, string path, int depth, JsonTypeInfo typeInfo, CryptoShreddingTypePlan? plan,
        Stack<(object Value, string Path, int Depth)> stack)
    {
        CryptoShreddedField[] fields = plan is null ? [] : [.. plan.Fields];
        var cryptoNames = fields.Select(f => f.Name).ToHashSet(StringComparer.Ordinal);
        foreach (var property in typeInfo.Properties)
        {
            PushMember(value, path, depth, property, cryptoNames, stack);
        }

        return [.. fields.Select(field => new CryptoShreddedOccurrence(value, field, Join(path, field.Name)))];
    }

    private static void PushMember(
        object value, string path, int depth, JsonPropertyInfo property, HashSet<string> cryptoNames,
        Stack<(object Value, string Path, int Depth)> stack)
    {
        if (property.Get is { } getter && !IsCryptoProperty(property, cryptoNames) && getter(value) is { } child)
        {
            stack.Push((child, Join(path, MemberName(property)), depth + 1));
        }
    }

    private static string MemberName(JsonPropertyInfo property) =>
        property.AttributeProvider is MemberInfo member ? member.Name : property.Name;

    private static bool IsCryptoProperty(JsonPropertyInfo property, HashSet<string> cryptoNames) =>
        property.AttributeProvider is PropertyInfo info && cryptoNames.Contains(info.Name);

    private static void PushAll(IEnumerable<object?> items, string path, int depth, Stack<(object Value, string Path, int Depth)> stack)
    {
        foreach (var item in items)
        {
            if (item is not null)
            {
                stack.Push((item, path, depth + 1));
            }
        }
    }

    private static IEnumerable<object?> Elements(object value) =>
        value is IEnumerable enumerable ? enumerable.Cast<object?>() : [];

    private static IEnumerable<object?> DictionaryValues(object value)
    {
        if (value is IDictionary dictionary)
        {
            return dictionary.Values.Cast<object?>();
        }

        return Elements(value).Select(entry => entry?.GetType().GetProperty("Value")?.GetValue(entry));
    }

    private static string Join(string path, string name) => path.Length == 0 ? name : $"{path}.{name}";
}
