using System.Reflection;

namespace Encina.Security.ABAC.EEL;

/// <summary>
/// Discovers EEL expressions declared via <see cref="RequireConditionAttribute"/> across
/// one or more assemblies.
/// </summary>
/// <remarks>
/// <para>
/// This class scans exported types for <see cref="RequireConditionAttribute"/> decorations
/// and returns each discovered <c>(Type, Expression)</c> tuple. It is used by both
/// <see cref="EELExpressionPrecompilationService"/> (fail-fast at startup) and
/// <see cref="Testing.EELTestHelper"/> (test-time validation).
/// </para>
/// <para>
/// Types that fail to load (e.g., due to missing dependencies) are silently skipped
/// via <see cref="ReflectionTypeLoadException"/> handling to ensure robust scanning.
/// </para>
/// </remarks>
internal static class EELExpressionDiscovery
{
    /// <summary>
    /// Discovers all EEL expressions declared on types in the specified assemblies.
    /// </summary>
    /// <param name="assemblies">The assemblies to scan for <see cref="RequireConditionAttribute"/> decorations.</param>
    /// <returns>
    /// A read-only list of <c>(RequestType, Expression)</c> tuples representing all
    /// discovered EEL expressions, ordered by type name then expression.
    /// </returns>
    public static IReadOnlyList<(Type RequestType, string Expression)> Discover(
        IEnumerable<Assembly> assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);

        var results = new List<(Type RequestType, string Expression)>();

        foreach (var assembly in assemblies)
        {
            var types = GetExportedTypesSafe(assembly);

            foreach (var type in types)
            {
                var attributes = type
                    .GetCustomAttributes<RequireConditionAttribute>(inherit: true);

                foreach (var attribute in attributes)
                {
                    results.Add((type, attribute.Expression));
                }
            }
        }

        return results
            .OrderBy(r => r.RequestType.FullName, StringComparer.Ordinal)
            .ThenBy(r => r.Expression, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Gets exported types from an assembly, handling <see cref="ReflectionTypeLoadException"/>
    /// gracefully by returning only the types that loaded successfully.
    /// </summary>
    private static IEnumerable<Type> GetExportedTypesSafe(Assembly assembly)
    {
        try
        {
            return assembly.GetExportedTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            // Return the types that loaded successfully, filtering out nulls.
            return ex.Types.Where(t => t is not null)!;
        }
    }
}
