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
    if (args[i] == "--self-test")
    {
        Environment.ExitCode = RunSelfTest();
        return;
    }
}

// One lightweight history entry from a latest.json "overall" section (and its runId). The attainment
// object (#2542) is copied when the snapshot has one; entries from older snapshots simply lack it.
// DeepClone: a JsonNode that already has a parent cannot be added to another node.
static JsonObject BuildEntry(JsonNode latestJson, JsonNode overall, string timestamp, string snapshotFile)
{
    var entry = new JsonObject
    {
        ["timestamp"] = timestamp,
        ["file"] = snapshotFile,
        ["coverage"] = (int)(overall["coverage"]?.GetValue<double>() ?? 0),
        ["obligations"] = overall["obligations"]?.GetValue<int>() ?? overall["lines"]?.GetValue<int>() ?? 0,
        ["met"] = overall["met"]?.GetValue<int>() ?? (int)(overall["covered"]?.GetValue<double>() ?? 0)
    };

    // Include runId if present (allows downloading raw Cobertura XML artifacts later)
    var runId = latestJson["runId"];
    if (runId is not null)
        entry["runId"] = runId.GetValue<long>();

    // Add per-flag overall coverage (for trend chart filtering by test type)
    if (overall["perFlag"] is JsonObject flagObj)
    {
        var perFlag = new JsonObject();
        foreach (var (flagName, flagNode) in flagObj)
            perFlag[flagName] = Math.Round(flagNode?["coverage"]?.GetValue<double>() ?? 0, 2);
        entry["perFlag"] = perFlag;
    }

    if (overall["attainment"] is JsonObject attainment)
        entry["attainment"] = attainment.DeepClone();

    return entry;
}

static int RunSelfTest()
{
    var failures = new List<string>();
    var passed = 0;
    void Check(bool condition, string name)
    {
        if (condition) passed++;
        else failures.Add(name);
    }

    var withAttainment = JsonNode.Parse("""
        { "runId": 42, "overall": { "coverage": 61.5, "obligations": 10, "met": 6,
          "perFlag": { "unit": { "coverage": 70.123 } },
          "attainment": { "pairs": 4, "met": 2, "ratio": 0.5, "lineWeighted": 0.9, "meanGap": 10, "linesToCover": 20, "noData": 1, "unweighted": 0, "zeroTarget": 3 } } }
        """)!;
    var entry = BuildEntry(withAttainment, withAttainment["overall"]!, "t", "f.json");
    Check(entry["attainment"]?["ratio"]?.GetValue<double>() == 0.5, "attainment is copied when present");
    Check(entry["attainment"]?["pairs"]?.GetValue<int>() == 4 && entry["attainment"]?["zeroTarget"]?.GetValue<int>() == 3, "attainment keeps its fields");
    Check(entry["attainment"]?.Parent == entry && withAttainment["overall"]!["attainment"] is not null, "the copy is a clone: the source keeps its node");
    Check(entry["coverage"]?.GetValue<int>() == 61 && entry["met"]?.GetValue<int>() == 6 && entry["runId"]?.GetValue<long>() == 42, "the existing fields are unchanged");
    Check(entry["perFlag"]?["unit"]?.GetValue<double>() == 70.12, "per-flag coverage is rounded to two decimals");

    var oldShape = JsonNode.Parse("""{ "overall": { "coverage": 50.2, "obligations": 8, "met": 4 } }""")!;
    var oldEntry = BuildEntry(oldShape, oldShape["overall"]!, "t", "f.json");
    Check(!oldEntry.ContainsKey("attainment"), "a snapshot without attainment gives an entry without it");
    Check(!oldEntry.ContainsKey("runId") && !oldEntry.ContainsKey("perFlag"), "optional fields are omitted when absent");

    foreach (var failure in failures) Console.WriteLine($"  FAIL: {failure}");
    if (failures.Count > 0)
    {
        Console.WriteLine($"Self-test FAILED ({failures.Count} of {passed + failures.Count} checks)");
        return 1;
    }
    Console.WriteLine($"Self-test passed ({passed} checks)");
    return 0;
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

// Build history entry (lightweight — overall + per-flag + link to full snapshot)
var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
var snapshotFile = DateTime.UtcNow.ToString("yyyy-MM-ddTHHmmssZ") + ".json";
var entry = BuildEntry(latestJson, overall, timestamp, snapshotFile);

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
