// Lists the shard mutation reports the aggregate job of mutation-tests.yml
// downloaded and names the matrix shards that uploaded none (#1441, #1682).
//
// Every shard writes its index into shard-id.txt next to its
// mutation-report.json before uploading the report directory, so a report
// is identified by its content, not by where actions/download-artifact put
// it: with a `pattern:` that matches one artifact, download-artifact
// extracts it straight into `path` (shards/mutation-report.json) instead of
// shards/<artifact name>/, which made the old directory-name check report
// the custom shard as missing although its report was there (run
// 37149399906).
//
// Usage:
//   dotnet run --file .github/scripts/mutation-shard-reports.cs -- \
//     --shards <dir> --matrix <matrix JSON> --reports-out <file> --missing-out <file>
//   dotnet run --file .github/scripts/mutation-shard-reports.cs -- --self-test
//
// --reports-out receives one report path per line (sorted), --missing-out one
// shard index per line, in matrix order. Exit code 0 when every report is
// identified; 1 when a report has no shard-id.txt, names a shard that is not
// in the matrix, or shares its shard with another report (the outputs are
// not written then); 2 on bad arguments.
using System.Text.Json;

if (args is ["--self-test"])
{
    return SelfTest.Run();
}

var options = ParseArguments(args);
if (options is null)
{
    Console.Error.WriteLine("Usage: mutation-shard-reports.cs --shards <dir> --matrix <json> --reports-out <file> --missing-out <file> | --self-test");
    return 2;
}

var result = ShardReports.Find(options.Value.ShardsDirectory, ShardReports.ReadMatrixIndexes(options.Value.MatrixJson));
foreach (var error in result.Errors)
{
    Console.WriteLine($"::error title=Shard report not identified::{error}");
}

if (result.Errors.Count > 0)
{
    return 1;
}

File.WriteAllLines(options.Value.ReportsOut, result.Reports);
File.WriteAllLines(options.Value.MissingOut, result.Missing);
Console.WriteLine($"{result.Reports.Count} shard report(s) identified; {result.Missing.Count} matrix shard(s) without a report{(result.Missing.Count > 0 ? ": " + string.Join(' ', result.Missing) : string.Empty)}.");
return 0;

static (string ShardsDirectory, string MatrixJson, string ReportsOut, string MissingOut)? ParseArguments(string[] rawArgs)
{
    var values = new Dictionary<string, string>(StringComparer.Ordinal);
    for (var index = 0; index + 1 < rawArgs.Length; index += 2)
    {
        values[rawArgs[index]] = rawArgs[index + 1];
    }

    string[] required = ["--shards", "--matrix", "--reports-out", "--missing-out"];
    if (rawArgs.Length != required.Length * 2 || !required.All(values.ContainsKey))
    {
        return null;
    }

    return (values["--shards"], values["--matrix"], values["--reports-out"], values["--missing-out"]);
}

internal readonly record struct ShardReportResult(IReadOnlyList<string> Reports, IReadOnlyList<string> Missing, IReadOnlyList<string> Errors);

internal static class ShardReports
{
    public static IReadOnlyList<string> ReadMatrixIndexes(string matrixJson)
    {
        using var document = JsonDocument.Parse(matrixJson);
        return document.RootElement.EnumerateArray()
            .Select(entry => entry.GetProperty("idx").GetString() ?? string.Empty)
            .ToList();
    }

    public static ShardReportResult Find(string shardsDirectory, IReadOnlyList<string> matrixIndexes)
    {
        var reports = Directory.Exists(shardsDirectory)
            ? Directory.EnumerateFiles(shardsDirectory, "mutation-report.json", SearchOption.AllDirectories)
                .Select(path => path.Replace('\\', '/'))
                .Order(StringComparer.Ordinal)
                .ToList()
            : [];

        var errors = new List<string>();
        var reportByShard = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var report in reports)
        {
            var shardId = ReadShardId(report);
            if (shardId is null)
            {
                errors.Add($"{report} has no shard-id.txt next to it; the shard upload must write its matrix index there.");
            }
            else if (!matrixIndexes.Contains(shardId))
            {
                errors.Add($"{report} names shard '{shardId}', which is not in this run's matrix ({string.Join(' ', matrixIndexes)}).");
            }
            else if (!reportByShard.TryAdd(shardId, report))
            {
                errors.Add($"{report} and {reportByShard[shardId]} both name shard '{shardId}'.");
            }
        }

        var missing = matrixIndexes.Where(index => !reportByShard.ContainsKey(index)).ToList();
        return new ShardReportResult(reports, missing, errors);
    }

    private static string? ReadShardId(string reportPath)
    {
        var markerPath = Path.Combine(Path.GetDirectoryName(reportPath) ?? ".", "shard-id.txt");
        if (!File.Exists(markerPath))
        {
            return null;
        }

        var shardId = File.ReadAllText(markerPath).Trim();
        return shardId.Length == 0 ? null : shardId;
    }
}

// Fixtures for the layouts actions/download-artifact produces, and the
// failures the aggregate must name. Run with --self-test; exit 0 when every
// case passes.
internal static class SelfTest
{
    public static int Run()
    {
        var root = Path.Combine(Path.GetTempPath(), "mutation-shard-reports-" + Guid.NewGuid().ToString("N"));
        var failures = 0;
        try
        {
            failures += Check(root, "single artifact extracted flat (run 37149399906, custom shard)",
                ["custom"], [("", "custom")], expectReports: 1, expectMissing: [], expectErrors: 0);
            failures += Check(root, "several artifacts, one subdirectory each, one shard missing",
                ["0", "1", "2"], [("mutation-report-shard-0", "0"), ("mutation-report-shard-2", "2")], expectReports: 2, expectMissing: ["1"], expectErrors: 0);
            failures += Check(root, "every shard uploaded",
                ["0", "1"], [("mutation-report-shard-0", "0"), ("mutation-report-shard-1", "1")], expectReports: 2, expectMissing: [], expectErrors: 0);
            failures += Check(root, "no artifact downloaded",
                ["0", "1"], [], expectReports: 0, expectMissing: ["0", "1"], expectErrors: 0);
            failures += Check(root, "report without shard-id.txt",
                ["0"], [("mutation-report-shard-0", null)], expectReports: 1, expectMissing: ["0"], expectErrors: 1);
            failures += Check(root, "shard-id.txt names a shard outside the matrix",
                ["0"], [("mutation-report-shard-7", "7")], expectReports: 1, expectMissing: ["0"], expectErrors: 1);
            failures += Check(root, "two reports name the same shard",
                ["0", "1"], [("mutation-report-shard-0", "0"), ("mutation-report-shard-1", "0")], expectReports: 2, expectMissing: ["1"], expectErrors: 1);
            failures += CheckMatrixParsing();
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }

        Console.WriteLine(failures == 0 ? "Self-test passed." : $"Self-test FAILED: {failures} case(s).");
        return failures == 0 ? 0 : 1;
    }

    private static int Check(
        string root,
        string name,
        string[] matrix,
        (string Directory, string? ShardId)[] uploads,
        int expectReports,
        string[] expectMissing,
        int expectErrors)
    {
        var shards = Path.Combine(root, Guid.NewGuid().ToString("N"), "shards");
        Directory.CreateDirectory(shards);
        foreach (var (directory, shardId) in uploads)
        {
            var reportDirectory = Path.Combine(shards, directory);
            Directory.CreateDirectory(reportDirectory);
            File.WriteAllText(Path.Combine(reportDirectory, "mutation-report.json"), "{\"files\":{}}");
            if (shardId is not null)
            {
                File.WriteAllText(Path.Combine(reportDirectory, "shard-id.txt"), shardId + "\n");
            }
        }

        var result = ShardReports.Find(shards, matrix);
        var passed = result.Reports.Count == expectReports
            && result.Missing.SequenceEqual(expectMissing)
            && result.Errors.Count == expectErrors;
        Console.WriteLine($"{(passed ? "PASS" : "FAIL")}  {name}: reports={result.Reports.Count} missing=[{string.Join(' ', result.Missing)}] errors={result.Errors.Count}");
        foreach (var error in result.Errors)
        {
            Console.WriteLine($"      {error}");
        }

        return passed ? 0 : 1;
    }

    private static int CheckMatrixParsing()
    {
        const string matrix = "[{\"idx\":\"custom\",\"scope\":\"**/Dispatchers/Strategies/*.cs\",\"timeout\":87}]";
        var indexes = ShardReports.ReadMatrixIndexes(matrix);
        var passed = indexes.SequenceEqual(["custom"]);
        Console.WriteLine($"{(passed ? "PASS" : "FAIL")}  matrix JSON of select-matrix: idx=[{string.Join(' ', indexes)}]");
        return passed ? 0 : 1;
    }
}
