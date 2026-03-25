using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

// Usage: dotnet run --file scripts/list_survivors.cs [path-to-mutation-report.json]

var reportPath = args.Length > 0
    ? args[0]
    : ResolveDefaultReportPath();

if (!File.Exists(reportPath))
{
    Console.Error.WriteLine($"No pude encontrar el archivo de reporte en '{reportPath}'.");
    return;
}

static string ResolveDefaultReportPath()
{
    var mutationOutputPath = Path.Combine("artifacts", "mutation");

    if (Directory.Exists(mutationOutputPath))
    {
        var latest = Directory
            .EnumerateDirectories(mutationOutputPath)
            .OrderByDescending(path => path)
            .FirstOrDefault();

        if (latest is not null)
        {
            var candidate = Path.Combine(latest, "reports", "mutation-report.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }
    }

    return Path.Combine(mutationOutputPath, "latest", "reports", "mutation-report.json");
}

using var document = JsonDocument.Parse(File.ReadAllText(reportPath));
var root = document.RootElement;
if (!root.TryGetProperty("files", out var filesElement))
{
    Console.Error.WriteLine("El reporte no contiene la sección 'files'.");
    return;
}

var survivors = new List<(string Path, int Line, string Mutator, string Replacement)>();
var summaries = new Dictionary<string, MutationSummary>(StringComparer.OrdinalIgnoreCase);

foreach (var fileProperty in filesElement.EnumerateObject())
{
    var filePath = fileProperty.Name;
    if (!fileProperty.Value.TryGetProperty("mutants", out var mutantsElement))
    {
        continue;
    }

    var summary = summaries.TryGetValue(filePath, out var existing)
        ? existing
        : new MutationSummary();

    foreach (var mutant in mutantsElement.EnumerateArray())
    {
        if (!mutant.TryGetProperty("status", out var statusElement))
        {
            continue;
        }

        var status = statusElement.GetString();
        switch (status)
        {
            case "Killed":
                summary.Killed++;
                break;
            case "Survived":
                summary.Survived++;
                break;
            case "NoCoverage":
                summary.NoCoverage++;
                break;
            case "TimedOut":
                summary.TimedOut++;
                break;
        }

        if (!string.Equals(status, "Survived", StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }

        if (!mutant.TryGetProperty("location", out var locationElement) ||
            !locationElement.TryGetProperty("start", out var startElement) ||
            !startElement.TryGetProperty("line", out var lineElement))
        {
            continue;
        }

        var line = lineElement.GetInt32();
        var mutator = mutant.TryGetProperty("mutatorName", out var mutatorElement)
            ? mutatorElement.GetString() ?? string.Empty
            : string.Empty;
        var replacement = mutant.TryGetProperty("replacement", out var replacementElement)
            ? replacementElement.GetString() ?? string.Empty
            : string.Empty;

        survivors.Add((filePath, line, mutator, replacement));
    }

    summaries[filePath] = summary;
}

survivors.Sort((a, b) =>
{
    var pathCompare = string.Compare(a.Path, b.Path, StringComparison.OrdinalIgnoreCase);
    if (pathCompare != 0)
    {
        return pathCompare;
    }

    return a.Line.CompareTo(b.Line);
});

foreach (var survivor in survivors)
{
    Console.WriteLine($"{survivor.Path}:{survivor.Line} | {survivor.Mutator} -> {survivor.Replacement}");
}

Console.WriteLine($"Total survivors: {survivors.Count}\n");

if (summaries.Count == 0)
{
    Console.WriteLine("No se encontraron mutantes en el reporte.");
    return;
}

Console.WriteLine("Puntuación por fichero (ascendente):");
Console.WriteLine("-----------------------------------");

foreach (var entry in summaries
             .Select(pair => (Path: pair.Key, Score: pair.Value.Score, Total: pair.Value.TotalCount, pair.Value))
             .Where(item => item.Total > 0)
             .OrderBy(item => item.Score))
{
    var stats = entry.Value;
    Console.WriteLine(
        $"{entry.Path} -> {entry.Score,6:F2}% (killed={stats.Killed}, survived={stats.Survived}, noCov={stats.NoCoverage}, timeout={stats.TimedOut})");
}

internal sealed class MutationSummary
{
    public int Killed { get; set; }

    public int Survived { get; set; }

    public int NoCoverage { get; set; }

    public int TimedOut { get; set; }

    public int TotalCount => Killed + Survived + NoCoverage + TimedOut;

    public double Score => TotalCount == 0
        ? 100d
        : (double)Killed / TotalCount * 100d;
}
