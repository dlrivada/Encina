using System.Reflection;

using Encina.Diagnostics;

using Microsoft.Extensions.Logging;

namespace Encina.Testing.Architecture;

/// <summary>
/// Architecture rules that validate EventId uniqueness across Encina assemblies.
/// </summary>
/// <remarks>
/// <para>
/// These rules complement the <c>[LoggerMessage]</c> source generator's built-in
/// SYSLIB1006 diagnostic (which only checks duplicates within a single class) by
/// enforcing uniqueness across classes and assembly boundaries.
/// </para>
/// <para>
/// Three validations are provided:
/// <list type="number">
/// <item><description>Global uniqueness — no two methods share an EventId, whether in the same assembly or in different ones</description></item>
/// <item><description>Range compliance — each assembly's EventIds fall within one of its registered ranges</description></item>
/// <item><description>Range overlap detection — no two registered ranges overlap</description></item>
/// </list>
/// </para>
/// </remarks>
public static class EventIdUniquenessRule
{
    /// <summary>
    /// Extracts all <c>[LoggerMessage]</c> EventIds from the given assemblies.
    /// </summary>
    /// <param name="assemblies">The assemblies to scan.</param>
    /// <returns>A list of (AssemblyName, TypeName, MethodName, EventId) tuples.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="assemblies"/> is null.</exception>
    public static IReadOnlyList<(string AssemblyName, string TypeName, string MethodName, int EventId)>
        ExtractEventIds(IReadOnlyList<Assembly> assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);

        return EnumerateLoggerMessages(assemblies)
            .Where(m => m.EventId >= 0)
            .ToList();
    }

    /// <summary>
    /// Validates that every <c>[LoggerMessage]</c> method declares an explicit EventId.
    /// </summary>
    /// <remarks>
    /// Without an EventId the source generator derives one from a hash of the method name, which lands
    /// outside every registered range and is invisible to <see cref="ExtractEventIds"/>.
    /// </remarks>
    /// <param name="assemblies">The assemblies to validate.</param>
    /// <returns>A list of violations, one per method without an explicit EventId.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="assemblies"/> is null.</exception>
    public static IReadOnlyList<string> AssertEveryLoggerMessageHasEventId(IReadOnlyList<Assembly> assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);

        return EnumerateLoggerMessages(assemblies)
            .Where(m => m.EventId < 0)
            .Select(m => $"{m.AssemblyName}::{m.TypeName}.{m.MethodName} has [LoggerMessage] without an EventId.")
            .ToList();
    }

    private static IEnumerable<(string AssemblyName, string TypeName, string MethodName, int EventId)>
        EnumerateLoggerMessages(IReadOnlyList<Assembly> assemblies)
    {
        foreach (var assembly in assemblies)
        {
            var assemblyName = assembly.GetName().Name ?? assembly.FullName ?? "Unknown";

            foreach (var type in GetLoadableTypes(assembly))
            {
                // S3011: Non-public reflection is intentional here — [LoggerMessage] methods
                // are commonly declared as private/internal partial methods, so we must scan
                // non-public members to discover all EventId allocations across the codebase.
                foreach (var method in type.GetMethods(
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    var attr = method.GetCustomAttribute<LoggerMessageAttribute>();
                    if (attr is not null)
                    {
                        yield return (assemblyName, type.FullName ?? type.Name, method.Name, attr.EventId);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Validates that every <c>[LoggerMessage]</c> EventId in the provided assemblies
    /// is declared by exactly one method, whether the duplicates live in the same
    /// assembly or in different ones.
    /// </summary>
    /// <param name="assemblies">The assemblies to validate.</param>
    /// <returns>
    /// A list of violations. Each violation describes a duplicate EventId
    /// with the assemblies/methods that share it.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="assemblies"/> is null.</exception>
    public static IReadOnlyList<string> AssertEventIdsAreGloballyUnique(
        IReadOnlyList<Assembly> assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);

        var eventIds = ExtractEventIds(assemblies);
        var violations = new List<string>();

        var duplicates = eventIds
            .GroupBy(e => e.EventId)
            .Where(g => g.Count() > 1)
            .OrderBy(g => g.Key);

        foreach (var group in duplicates)
        {
            var locations = string.Join(", ", group.Select(e =>
                $"{e.AssemblyName}::{e.TypeName}.{e.MethodName}"));
            var assemblyNames = group.Select(e => e.AssemblyName).Distinct().ToList();
            var scope = assemblyNames.Count == 1
                ? $"within assembly '{assemblyNames[0]}'"
                : "across assemblies";
            violations.Add(
                $"EventId {group.Key} is duplicated {scope}: [{locations}]");
        }

        return violations;
    }

    /// <summary>
    /// Validates that each assembly's EventIds fall within a registered range
    /// in <see cref="EventIdRanges"/>.
    /// </summary>
    /// <param name="assemblies">The assemblies to validate.</param>
    /// <param name="assemblyToRangeNames">
    /// A mapping from assembly name to the <see cref="EventIdRanges"/> field names
    /// that define the allowed ranges for that assembly. A package that owns several
    /// sub-ranges (for example one per feature) lists all of them; an EventId is
    /// compliant when it falls inside any of the listed ranges.
    /// </param>
    /// <returns>A list of violations describing unmapped assemblies, unknown range names and out-of-range EventIds.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public static IReadOnlyList<string> AssertEventIdsWithinRegisteredRanges(
        IReadOnlyList<Assembly> assemblies,
        IReadOnlyDictionary<string, IReadOnlyList<string>> assemblyToRangeNames)
    {
        ArgumentNullException.ThrowIfNull(assemblies);
        ArgumentNullException.ThrowIfNull(assemblyToRangeNames);

        var allRanges = EventIdRanges.GetAllRanges()
            .ToDictionary(r => r.Name, r => (r.Min, r.Max));

        var eventIds = ExtractEventIds(assemblies);
        var violations = new List<string>();

        foreach (var group in eventIds.GroupBy(e => e.AssemblyName))
        {
            if (!assemblyToRangeNames.TryGetValue(group.Key, out var rangeNames) || rangeNames.Count == 0)
            {
                violations.Add(
                    $"Assembly '{group.Key}' has {group.Count()} EventIds but is not mapped to any registered range.");
                continue;
            }

            var unknown = rangeNames.Where(n => !allRanges.ContainsKey(n)).ToList();
            if (unknown.Count > 0)
            {
                violations.Add(
                    $"Assembly '{group.Key}' is mapped to range(s) '{string.Join("', '", unknown)}' which do not exist in EventIdRanges.");
                continue;
            }

            var ranges = rangeNames.Select(n => (Name: n, allRanges[n].Min, allRanges[n].Max)).ToList();
            var described = string.Join(", ", ranges.Select(r => $"'{r.Name}' ({r.Min}-{r.Max})"));

            foreach (var entry in group.OrderBy(e => e.EventId))
            {
                if (!ranges.Any(r => entry.EventId >= r.Min && entry.EventId <= r.Max))
                {
                    violations.Add(
                        $"EventId {entry.EventId} in {entry.AssemblyName}::{entry.TypeName}.{entry.MethodName} " +
                        $"is outside its registered range(s) {described}.");
                }
            }
        }

        return violations;
    }

    /// <summary>
    /// Validates that no registered ranges in <see cref="EventIdRanges"/> overlap.
    /// </summary>
    /// <returns>A list of violations describing overlapping ranges.</returns>
    public static IReadOnlyList<string> AssertNoRangeOverlaps()
    {
        var ranges = EventIdRanges.GetAllRanges();
        var violations = new List<string>();

        for (var i = 0; i < ranges.Count; i++)
        {
            for (var j = i + 1; j < ranges.Count; j++)
            {
                var a = ranges[i];
                var b = ranges[j];

                if (a.Min <= b.Max && b.Min <= a.Max)
                {
                    violations.Add(
                        $"Range '{a.Name}' ({a.Min}-{a.Max}) overlaps with " +
                        $"'{b.Name}' ({b.Min}-{b.Max}).");
                }
            }
        }

        return violations;
    }

    /// <summary>
    /// Returns a human-readable allocation table of all registered ranges
    /// and their current usage.
    /// </summary>
    /// <param name="assemblies">Optional assemblies to include usage statistics.</param>
    /// <returns>A formatted string with the allocation table.</returns>
    public static string GenerateAllocationReport(IReadOnlyList<Assembly>? assemblies = null)
    {
        var ranges = EventIdRanges.GetAllRanges();
        var eventIds = assemblies is not null ? ExtractEventIds(assemblies) : [];

        var usageByRange = new Dictionary<string, int>();

        foreach (var range in ranges)
        {
            var count = eventIds.Count(e => e.EventId >= range.Min && e.EventId <= range.Max);
            usageByRange[range.Name] = count;
        }

        var lines = new List<string>
        {
            "EventId Allocation Report",
            new('=', 80),
            $"{"Range",-35} {"Min",6}-{"Max",-6} {"Capacity",8} {"Used",6} {"Free",6}",
            new('-', 80),
        };

        foreach (var range in ranges)
        {
            var capacity = range.Max - range.Min + 1;
            var used = usageByRange.GetValueOrDefault(range.Name);
            var free = capacity - used;
            lines.Add($"{range.Name,-35} {range.Min,6}-{range.Max,-6} {capacity,8} {used,6} {free,6}");
        }

        lines.Add(new string('=', 80));

        return string.Join(Environment.NewLine, lines);
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t is not null)!;
        }
    }
}
