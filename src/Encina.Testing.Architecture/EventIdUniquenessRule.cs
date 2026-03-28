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
/// enforcing uniqueness across assembly boundaries.
/// </para>
/// <para>
/// Three validations are provided:
/// <list type="number">
/// <item><description>Global uniqueness — no two assemblies share the same EventId</description></item>
/// <item><description>Range compliance — each assembly's EventIds fall within its registered range</description></item>
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

        var results = new List<(string AssemblyName, string TypeName, string MethodName, int EventId)>();

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
                    BindingFlags.Static | BindingFlags.Instance))
                {
                    var attr = method.GetCustomAttribute<LoggerMessageAttribute>();
                    if (attr is null || attr.EventId < 0)
                    {
                        continue;
                    }

                    results.Add((
                        assemblyName,
                        type.FullName ?? type.Name,
                        method.Name,
                        attr.EventId));
                }
            }
        }

        return results;
    }

    /// <summary>
    /// Validates that all <c>[LoggerMessage]</c> EventIds are globally unique
    /// across the provided assemblies.
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
            .Where(g => g.Select(e => e.AssemblyName).Distinct().Count() > 1);

        foreach (var group in duplicates)
        {
            var locations = string.Join(", ", group.Select(e =>
                $"{e.AssemblyName}::{e.TypeName}.{e.MethodName}"));
            violations.Add(
                $"EventId {group.Key} is duplicated across assemblies: [{locations}]");
        }

        return violations;
    }

    /// <summary>
    /// Validates that each assembly's EventIds fall within a registered range
    /// in <see cref="EventIdRanges"/>.
    /// </summary>
    /// <param name="assemblies">The assemblies to validate.</param>
    /// <param name="assemblyToRangeName">
    /// A mapping from assembly name to the <see cref="EventIdRanges"/> field name
    /// that defines the allowed range for that assembly.
    /// </param>
    /// <returns>A list of violations describing out-of-range EventIds.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public static IReadOnlyList<string> AssertEventIdsWithinRegisteredRanges(
        IReadOnlyList<Assembly> assemblies,
        IReadOnlyDictionary<string, string> assemblyToRangeName)
    {
        ArgumentNullException.ThrowIfNull(assemblies);
        ArgumentNullException.ThrowIfNull(assemblyToRangeName);

        var allRanges = EventIdRanges.GetAllRanges()
            .ToDictionary(r => r.Name, r => (r.Min, r.Max));

        var eventIds = ExtractEventIds(assemblies);
        var violations = new List<string>();

        foreach (var group in eventIds.GroupBy(e => e.AssemblyName))
        {
            if (!assemblyToRangeName.TryGetValue(group.Key, out var rangeName))
            {
                violations.Add(
                    $"Assembly '{group.Key}' has {group.Count()} EventIds but is not mapped to any registered range.");
                continue;
            }

            if (!allRanges.TryGetValue(rangeName, out var range))
            {
                violations.Add(
                    $"Assembly '{group.Key}' is mapped to range '{rangeName}' which does not exist in EventIdRanges.");
                continue;
            }

            foreach (var entry in group)
            {
                if (entry.EventId < range.Min || entry.EventId > range.Max)
                {
                    violations.Add(
                        $"EventId {entry.EventId} in {entry.AssemblyName}::{entry.TypeName}.{entry.MethodName} " +
                        $"is outside registered range '{rangeName}' ({range.Min}-{range.Max}).");
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
