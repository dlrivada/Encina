using System.Collections.Concurrent;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Encina.Security.Audit;

/// <summary>
/// Extracts audit metadata from request types using naming conventions and reflection.
/// </summary>
internal static partial class RequestMetadataExtractor
{
    // Cache for entity ID property lookups
    private static readonly ConcurrentDictionary<Type, PropertyInfo?> EntityIdPropertyCache = new();

    // Compiled regex for extracting action and entity from type names
    // Matches patterns like: CreateOrderCommand, UpdateCustomerCommand, DeleteProductCommand, GetUserQuery
    [GeneratedRegex(@"^(Create|Update|Delete|Get|List|Find|Search|Add|Remove|Set|Clear|Execute|Process|Send|Validate|Check|Verify)(.+?)(Command|Query)?$", RegexOptions.Compiled)]
    private static partial Regex TypeNamePattern();

    /// <summary>
    /// Extracts the entity type and action from a request type name using naming conventions.
    /// </summary>
    /// <param name="requestType">The request type to extract metadata from.</param>
    /// <returns>A tuple containing the entity type and action.</returns>
    /// <remarks>
    /// <para>
    /// Recognizes common naming patterns:
    /// <list type="bullet">
    /// <item><c>CreateOrderCommand</c> → ("Order", "Create")</item>
    /// <item><c>UpdateCustomerCommand</c> → ("Customer", "Update")</item>
    /// <item><c>DeleteProductCommand</c> → ("Product", "Delete")</item>
    /// <item><c>GetUserQuery</c> → ("User", "Get")</item>
    /// <item><c>ListOrdersQuery</c> → ("Orders", "List")</item>
    /// </list>
    /// </para>
    /// <para>
    /// If the pattern doesn't match, returns the full type name as entity and "Unknown" as action.
    /// </para>
    /// </remarks>
    public static (string EntityType, string Action) ExtractFromTypeName(Type requestType)
    {
        ArgumentNullException.ThrowIfNull(requestType);

        var typeName = requestType.Name;
        var match = TypeNamePattern().Match(typeName);

        if (match.Success)
        {
            var action = match.Groups[1].Value;
            var entity = match.Groups[2].Value;
            return (entity, action);
        }

        // Fallback: try to strip common suffixes
        var entityType = typeName;
        if (entityType.EndsWith("Command", StringComparison.Ordinal))
        {
            entityType = entityType[..^7];
        }
        else if (entityType.EndsWith("Query", StringComparison.Ordinal))
        {
            entityType = entityType[..^5];
        }

        return (entityType, "Unknown");
    }

    /// <summary>
    /// Attempts to extract an entity ID from a request object.
    /// </summary>
    /// <param name="request">The request object to extract the ID from.</param>
    /// <returns>The entity ID as a string, or <c>null</c> if not found.</returns>
    /// <remarks>
    /// <para>
    /// Looks for properties named (in order of priority):
    /// <list type="number">
    /// <item><c>Id</c></item>
    /// <item><c>EntityId</c></item>
    /// <item><c>[EntityType]Id</c> (e.g., <c>OrderId</c>, <c>CustomerId</c>)</item>
    /// </list>
    /// </para>
    /// <para>
    /// Property lookups are cached per type for performance.
    /// </para>
    /// </remarks>
    public static string? TryExtractEntityId(object request)
    {
        if (request is null)
        {
            return null;
        }

        var requestType = request.GetType();
        var property = EntityIdPropertyCache.GetOrAdd(requestType, FindEntityIdProperty);

        if (property is null)
        {
            return null;
        }

        var value = property.GetValue(request);
        return value?.ToString();
    }

    private static PropertyInfo? FindEntityIdProperty(Type type)
    {
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        // Priority 1: "Id" property
        var idProperty = properties.FirstOrDefault(p =>
            p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase) &&
            p.CanRead);

        if (idProperty is not null)
        {
            return idProperty;
        }

        // Priority 2: "EntityId" property
        var entityIdProperty = properties.FirstOrDefault(p =>
            p.Name.Equals("EntityId", StringComparison.OrdinalIgnoreCase) &&
            p.CanRead);

        if (entityIdProperty is not null)
        {
            return entityIdProperty;
        }

        // Priority 3: Property ending with "Id" (e.g., OrderId, CustomerId)
        var suffixIdProperty = properties.FirstOrDefault(p =>
            p.Name.EndsWith("Id", StringComparison.OrdinalIgnoreCase) &&
            p.Name.Length > 2 &&
            p.CanRead);

        return suffixIdProperty;
    }

    /// <summary>
    /// Clears the entity ID property cache. Primarily for testing.
    /// </summary>
    internal static void ClearCache()
    {
        EntityIdPropertyCache.Clear();
    }
}
