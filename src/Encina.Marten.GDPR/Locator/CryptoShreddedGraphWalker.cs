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
            if (depth > maxDepth || CryptoShreddedPropertyClassifier.IsTerminal(value.GetType()) || !visited.Add(value))
            {
                continue;
            }

            foreach (var occurrence in Visit(value, path, depth, options, registry, stack))
            {
                yield return occurrence;
            }
        }
    }

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
        var occurrences = new List<CryptoShreddedOccurrence>();
        var cryptoNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var field in plan?.Fields ?? [])
        {
            cryptoNames.Add(field.Name);
            occurrences.Add(new CryptoShreddedOccurrence(value, field, Join(path, field.Name)));
        }

        foreach (var property in typeInfo.Properties)
        {
            if (property.Get is not null && !IsCryptoProperty(property, cryptoNames) && property.Get(value) is { } child)
            {
                var name = property.AttributeProvider is MemberInfo member ? member.Name : property.Name;
                stack.Push((child, Join(path, name), depth + 1));
            }
        }

        return occurrences;
    }

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
