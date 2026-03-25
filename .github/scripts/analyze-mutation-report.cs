using System.Linq;
using System.Text.Json;

if (args.Length == 0)
{
    Console.Error.WriteLine("Usage: dotnet run --file scripts/analyze-mutation-report.cs <file-fragment>");
    return;
}

var filter = args[0];

string? searchRoot = Directory.GetCurrentDirectory();
string? outputRoot = null;
var mutationOutputPath = Path.Combine("artifacts", "mutation");

while (!string.IsNullOrEmpty(searchRoot))
{
    var candidate = Path.Combine(searchRoot, mutationOutputPath);
    if (Directory.Exists(candidate))
    {
        outputRoot = candidate;
        break;
    }

    searchRoot = Directory.GetParent(searchRoot)?.FullName;
}

if (outputRoot is null)
{
    Console.Error.WriteLine("Stryker output folder not found (expected at artifacts/mutation). Run Stryker before using this script.");
    return;
}

var latestReportDir = Directory
    .EnumerateDirectories(outputRoot)
    .OrderByDescending(path => path)
    .FirstOrDefault();

if (latestReportDir is null)
{
    Console.Error.WriteLine("No Stryker runs found at artifacts/mutation.");
    return;
}

var reportPath = Path.Combine(latestReportDir, "reports", "mutation-report.json");
string? jsonContent = null;

if (File.Exists(reportPath))
{
    jsonContent = File.ReadAllText(reportPath);
}
else
{
    var htmlPath = Path.Combine(latestReportDir, "reports", "mutation-report.html");
    if (!File.Exists(htmlPath))
    {
        Console.Error.WriteLine($"Report not found. Expected either {reportPath} or {htmlPath}.");
        return;
    }

    jsonContent = ExtractReportJsonFromHtml(htmlPath);
    if (jsonContent is null)
    {
        Console.Error.WriteLine($"Failed to extract JSON report from {htmlPath}.");
        return;
    }

    reportPath = htmlPath;
}

using var document = JsonDocument.Parse(jsonContent);
var survivors = new List<(JsonElement Mutant, JsonElement? Location, string FilePath)>();

foreach (var (mutant, filePath) in EnumerateMutants(document.RootElement))
{
    if (!mutant.TryGetProperty("status", out var statusElement))
    {
        continue;
    }

    var status = statusElement.GetString();
    if (!string.Equals(status, "Survived", StringComparison.OrdinalIgnoreCase))
    {
        continue;
    }

    if (!string.IsNullOrWhiteSpace(filter) && !string.IsNullOrWhiteSpace(filePath) && !filePath.Contains(filter, StringComparison.OrdinalIgnoreCase))
    {
        continue;
    }

    JsonElement? location = mutant.TryGetProperty("location", out var locationElement) ? locationElement : null;
    var effectivePath = !string.IsNullOrWhiteSpace(filePath)
        ? filePath
        : TryGetFileName(location);

    if (string.IsNullOrWhiteSpace(effectivePath) || !effectivePath.Contains(filter, StringComparison.OrdinalIgnoreCase))
    {
        continue;
    }

    survivors.Add((mutant, location, effectivePath!));
}

Console.WriteLine($"Latest report: {reportPath}");
Console.WriteLine($"Survivors matching '{filter}': {survivors.Count}");

foreach (var (mutant, location, filePath) in survivors)
{
    var mutatorName = mutant.TryGetProperty("mutatorName", out var mutatorNameElement)
        ? mutatorNameElement.GetString()
        : "<unknown mutator>";
    var replacement = mutant.TryGetProperty("replacement", out var replacementValue) ? replacementValue.GetString() : null;
    var description = mutant.TryGetProperty("description", out var descriptionValue) ? descriptionValue.GetString() : null;

    var file = !string.IsNullOrWhiteSpace(filePath)
        ? filePath
        : "<unknown file>";

    var startLine = GetLine(location, "start");
    var endLine = GetLine(location, "end");
    if (endLine < 0)
    {
        endLine = startLine;
    }

    Console.WriteLine();
    var idValue = mutant.TryGetProperty("id", out var idElement)
        ? idElement.ToString()
        : "<unknown id>";

    Console.WriteLine($"  Id: {idValue}");
    Console.WriteLine($"  File: {file}");
    Console.WriteLine(startLine >= 0 ? $"  Range: L{startLine}-L{endLine}" : "  Range: <unknown>");
    Console.WriteLine($"  Mutator: {mutatorName}");
    if (!string.IsNullOrWhiteSpace(description))
    {
        Console.WriteLine($"  Description: {description}");
    }
    if (!string.IsNullOrWhiteSpace(replacement))
    {
        Console.WriteLine($"  Replacement: {replacement}");
    }
}

static string? ExtractReportJsonFromHtml(string htmlPath)
{
    var htmlContent = File.ReadAllText(htmlPath);
    const string marker = "app.report";
    var markerIndex = htmlContent.IndexOf(marker, StringComparison.Ordinal);
    if (markerIndex < 0)
    {
        return null;
    }

    var equalsIndex = htmlContent.IndexOf('=', markerIndex);
    if (equalsIndex < 0)
    {
        return null;
    }

    var braceStart = htmlContent.IndexOf('{', equalsIndex);
    if (braceStart < 0)
    {
        return null;
    }

    var inString = false;
    var escape = false;
    var depth = 0;

    for (var i = braceStart; i < htmlContent.Length; i++)
    {
        var ch = htmlContent[i];

        if (inString)
        {
            if (escape)
            {
                escape = false;
            }
            else if (ch == '\\')
            {
                escape = true;
            }
            else if (ch == '"')
            {
                inString = false;
            }

            continue;
        }

        switch (ch)
        {
            case '"':
                inString = true;
                break;
            case '{':
                depth++;
                break;
            case '}':
                depth--;
                if (depth == 0)
                {
                    var endExclusive = i + 1;
                    return htmlContent.Substring(braceStart, endExclusive - braceStart);
                }

                break;
        }
    }

    return null;
}

static IEnumerable<(JsonElement Mutant, string? FilePath)> EnumerateMutants(JsonElement root)
{
    if (root.TryGetProperty("mutants", out var rootMutants) && rootMutants.ValueKind == JsonValueKind.Array)
    {
        foreach (var mutant in rootMutants.EnumerateArray())
        {
            yield return (mutant, TryGetFileName(mutant.TryGetProperty("location", out var loc) ? loc : null));
        }
    }

    if (root.TryGetProperty("files", out var files) && files.ValueKind == JsonValueKind.Object)
    {
        foreach (var entry in files.EnumerateObject())
        {
            if (!entry.Value.TryGetProperty("mutants", out var fileMutants) || fileMutants.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var mutant in fileMutants.EnumerateArray())
            {
                var filePath = TryGetFileName(mutant.TryGetProperty("location", out var loc) ? loc : null) ?? entry.Name;
                yield return (mutant, filePath);
            }
        }
    }
}

static string? TryGetFileName(JsonElement? location)
{
    if (location is JsonElement element
        && element.ValueKind == JsonValueKind.Object
        && element.TryGetProperty("fileName", out var fileNameElement)
        && fileNameElement.ValueKind == JsonValueKind.String)
    {
        return fileNameElement.GetString();
    }

    return null;
}

static int GetLine(JsonElement? location, string propertyName)
{
    if (location is JsonElement element
        && element.ValueKind == JsonValueKind.Object
        && element.TryGetProperty(propertyName, out var position)
        && position.ValueKind == JsonValueKind.Object
        && position.TryGetProperty("line", out var lineElement)
        && lineElement.TryGetInt32(out var line))
    {
        return line;
    }

    return -1;
}
