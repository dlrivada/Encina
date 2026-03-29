// coverage-history.cs — Appends a coverage snapshot to the history file
// Usage: dotnet run .github/scripts/coverage-history.cs -- --latest <path> --history <path>
// Requires: .NET 10+ (C# 14 file-based app)
#pragma warning disable CA1305 // IFormatProvider not relevant for standalone scripts

using System.Text.Json;
using System.Text.Json.Nodes;

var latestPath = "docs/coverage/data/latest.json";
var historyPath = "docs/coverage/data/history.json";

for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--latest" && i + 1 < args.Length) latestPath = args[++i];
    if (args[i] == "--history" && i + 1 < args.Length) historyPath = args[++i];
}

if (!File.Exists(latestPath))
{
    Console.WriteLine($"No latest.json found at {latestPath}, skipping history update.");
    return;
}

// Read latest coverage data
var latestJson = JsonNode.Parse(File.ReadAllText(latestPath));
if (latestJson is null)
{
    Console.WriteLine("Failed to parse latest.json");
    return;
}

var overall = latestJson["overall"];
if (overall is null)
{
    Console.WriteLine("No 'overall' section in latest.json");
    return;
}

// Build history entry (lightweight — only overall + per-category + link to full snapshot)
var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
var snapshotFile = DateTime.UtcNow.ToString("yyyy-MM-ddTHHmmssZ") + ".json";
var entry = new JsonObject
{
    ["timestamp"] = timestamp,
    ["file"] = snapshotFile,
    ["coverage"] = (int)(overall["coverage"]?.GetValue<double>() ?? 0),
    ["lines"] = overall["lines"]?.GetValue<int>() ?? 0,
    ["covered"] = Math.Round(overall["covered"]?.GetValue<double>() ?? 0, 2)
};

// Include runId if present (allows downloading raw Cobertura XML artifacts later)
var runId = latestJson["runId"];
if (runId is not null)
    entry["runId"] = runId.GetValue<long>();

// Add per-category breakdown
var categories = new JsonObject();
if (latestJson["categories"] is JsonArray cats)
{
    foreach (var cat in cats)
    {
        var name = cat?["name"]?.GetValue<string>();
        var cov = cat?["coverage"]?.GetValue<double>() ?? 0;
        if (name is not null) categories[name] = (int)cov;
    }
}
entry["categories"] = categories;

// Load or create history array
JsonArray history;
if (File.Exists(historyPath))
{
    var existing = JsonNode.Parse(File.ReadAllText(historyPath));
    history = existing as JsonArray ?? [];
}
else
{
    history = [];
}

// Append and keep last 100 entries
history.Add(entry);
while (history.Count > 100)
    history.RemoveAt(0);

// Write back
var dir = Path.GetDirectoryName(historyPath);
if (dir is not null) Directory.CreateDirectory(dir);

File.WriteAllText(historyPath, history.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"History updated: {history.Count} entries, latest coverage = {entry["coverage"]}%");
