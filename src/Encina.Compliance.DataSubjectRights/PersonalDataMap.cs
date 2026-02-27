using System.Collections.Concurrent;
using System.Reflection;

namespace Encina.Compliance.DataSubjectRights;

/// <summary>
/// Provides a read-only, thread-safe map of entity types to their <see cref="PersonalDataField"/> metadata.
/// </summary>
/// <remarks>
/// <para>
/// The map is built once at startup by scanning assemblies for properties decorated with
/// <see cref="PersonalDataAttribute"/>. After construction, the map is immutable and safe for
/// concurrent access without locking.
/// </para>
/// <para>
/// This avoids runtime reflection in hot paths (e.g., pipeline behaviors, erasure operations)
/// by caching attribute metadata at startup.
/// </para>
/// </remarks>
internal sealed class PersonalDataMap
{
    private readonly IReadOnlyDictionary<Type, IReadOnlyList<PersonalDataField>> _map;

    /// <summary>
    /// Initializes a new instance of the <see cref="PersonalDataMap"/> class from pre-scanned data.
    /// </summary>
    /// <param name="map">The pre-built mapping of entity types to their personal data fields.</param>
    internal PersonalDataMap(IReadOnlyDictionary<Type, IReadOnlyList<PersonalDataField>> map)
    {
        ArgumentNullException.ThrowIfNull(map);
        _map = map;
    }

    /// <summary>
    /// Gets the personal data fields for the specified entity type.
    /// </summary>
    /// <param name="entityType">The entity type to look up.</param>
    /// <returns>
    /// A read-only list of <see cref="PersonalDataField"/> entries for the type,
    /// or an empty list if the type has no <see cref="PersonalDataAttribute"/> decorated properties.
    /// </returns>
    internal IReadOnlyList<PersonalDataField> GetFields(Type entityType)
    {
        ArgumentNullException.ThrowIfNull(entityType);
        return _map.TryGetValue(entityType, out var fields)
            ? fields
            : [];
    }

    /// <summary>
    /// Gets all registered entity types that have personal data fields.
    /// </summary>
    internal IEnumerable<Type> RegisteredTypes => _map.Keys;

    /// <summary>
    /// Gets the total number of registered entity types.
    /// </summary>
    internal int TypeCount => _map.Count;

    /// <summary>
    /// Builds a <see cref="PersonalDataMap"/> by scanning the specified assemblies for types
    /// with properties decorated with <see cref="PersonalDataAttribute"/>.
    /// </summary>
    /// <param name="assemblies">The assemblies to scan for personal data attributes.</param>
    /// <returns>An immutable <see cref="PersonalDataMap"/> containing all discovered fields.</returns>
    internal static PersonalDataMap BuildFromAssemblies(IEnumerable<Assembly> assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);

        var map = new Dictionary<Type, IReadOnlyList<PersonalDataField>>();

        foreach (var assembly in assemblies)
        {
            foreach (var type in assembly.GetTypes())
            {
                var fields = ScanType(type);
                if (fields.Count > 0)
                {
                    map[type] = fields;
                }
            }
        }

        return new PersonalDataMap(map);
    }

    /// <summary>
    /// Builds a <see cref="PersonalDataMap"/> by scanning the specified types for properties
    /// decorated with <see cref="PersonalDataAttribute"/>.
    /// </summary>
    /// <param name="types">The types to scan for personal data attributes.</param>
    /// <returns>An immutable <see cref="PersonalDataMap"/> containing all discovered fields.</returns>
    internal static PersonalDataMap BuildFromTypes(IEnumerable<Type> types)
    {
        ArgumentNullException.ThrowIfNull(types);

        var map = new Dictionary<Type, IReadOnlyList<PersonalDataField>>();

        foreach (var type in types)
        {
            var fields = ScanType(type);
            if (fields.Count > 0)
            {
                map[type] = fields;
            }
        }

        return new PersonalDataMap(map);
    }

    private static List<PersonalDataField> ScanType(Type type)
    {
        var fields = new List<PersonalDataField>();

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var attribute = property.GetCustomAttribute<PersonalDataAttribute>();
            if (attribute is null)
            {
                continue;
            }

            fields.Add(new PersonalDataField
            {
                PropertyName = property.Name,
                Category = attribute.Category,
                IsErasable = attribute.Erasable,
                IsPortable = attribute.Portable,
                HasLegalRetention = attribute.LegalRetention
            });
        }

        return fields;
    }
}
