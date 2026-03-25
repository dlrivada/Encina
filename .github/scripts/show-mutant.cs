using System.Linq;
using System.Text.Json;

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: dotnet run --file scripts/show-mutant.cs -- <file-fragment> <mutant-id>");
    return;
}

var filter = args[0];
if (!int.TryParse(args[1], out var mutantId))
{
    Console.Error.WriteLine("Mutant id must be an integer.");
    return;
}

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

var htmlPath = Path.Combine(latestReportDir, "reports", "mutation-report.html");
if (!File.Exists(htmlPath))
{
    Console.Error.WriteLine($"Report not found at {htmlPath}.");
    return;
}

var jsonContent = ExtractReportJsonFromHtml(htmlPath);
if (jsonContent is null)
{
    Console.Error.WriteLine("Failed to extract JSON report from HTML file.");
    return;
}

using var document = JsonDocument.Parse(jsonContent);

foreach (var (mutant, filePath) in EnumerateMutants(document.RootElement))
{
    if (!mutant.TryGetProperty("id", out var idElement))
    {
        continue;
    }

    int idValue;
    if (idElement.ValueKind == JsonValueKind.Number)
    {
        if (!idElement.TryGetInt32(out idValue))
        {
            continue;
        }
    }
    else if (idElement.ValueKind == JsonValueKind.String)
    {
        var idString = idElement.GetString();
        if (!int.TryParse(idString, out idValue))
        {
            continue;
        }
    }
    else
    {
        continue;
    }

    if (idValue != mutantId)
    {
        continue;
    }

    if (!filePath.Contains(filter, StringComparison.OrdinalIgnoreCase))
    {
        continue;
    }

    Console.WriteLine($"Report: {htmlPath}");
    Console.WriteLine($"Mutant Id: {idValue}");
    Console.WriteLine($"File: {filePath}");
    Console.WriteLine();
    Console.WriteLine(mutant);
    return;
}

Console.WriteLine($"Mutant {mutantId} not found in files matching '{filter}'.");

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

static IEnumerable<(JsonElement Mutant, string FilePath)> EnumerateMutants(JsonElement root)
{
    if (root.TryGetProperty("files", out var files) && files.ValueKind == JsonValueKind.Object)
    {
        foreach (var entry in files.EnumerateObject())
        {
            if (!entry.Value.TryGetProperty("mutants", out var mutants) || mutants.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var mutant in mutants.EnumerateArray())
            {
                yield return (mutant, entry.Name);
            }
        }
    }
}
