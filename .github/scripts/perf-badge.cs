// perf-badge.cs — Generate shields.io-compatible badge JSON from latest.json
//
// Usage:
//   dotnet run .github/scripts/perf-badge.cs -- \
//     --input docs/benchmarks/data/latest.json \
//     --output docs/benchmarks/badge.json
//
// Design notes (ADR-025 §4.2):
// - Badge shows: "{stableMethods} stable / {totalMethods}" with color:
//   green if regressionCount==0, yellow if <=3, red if >3.
// - Also emits badge.svg as a standalone SVG (no external deps).
#pragma warning disable CA1305

using System.Text.Json;
using System.Text.Json.Nodes;

var inputPath = "docs/benchmarks/data/latest.json";
var outputPath = "docs/benchmarks/badge.json";

for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--input" && i + 1 < args.Length) inputPath = args[++i];
    if (args[i] == "--output" && i + 1 < args.Length) outputPath = args[++i];
}

if (!File.Exists(inputPath))
{
    Console.Error.WriteLine($"Input not found: {inputPath}");
    Environment.Exit(2);
}

var latest = JsonNode.Parse(File.ReadAllText(inputPath));
var overall = latest?["overall"];

var total = overall?["totalMethods"]?.GetValue<int>() ?? 0;
var stable = overall?["stableMethods"]?.GetValue<int>() ?? 0;
var regressions = overall?["regressionCount"]?.GetValue<int>() ?? 0;
var modules = overall?["totalModules"]?.GetValue<int>() ?? 0;

// Badge label and message
var label = "benchmarks";
string message;
string color;

if (total == 0)
{
    message = "no data";
    color = "lightgrey";
}
else if (regressions == 0)
{
    message = $"{stable} stable / {total} methods";
    color = "brightgreen";
}
else if (regressions <= 3)
{
    message = $"{regressions} regression(s) / {total} methods";
    color = "yellow";
}
else
{
    message = $"{regressions} regressions / {total} methods";
    color = "red";
}

// shields.io JSON endpoint format
var badge = new JsonObject
{
    ["schemaVersion"] = 1,
    ["label"] = label,
    ["message"] = message,
    ["color"] = color
};

var dir = Path.GetDirectoryName(outputPath);
if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
File.WriteAllText(outputPath, badge.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

// Also emit a standalone SVG badge (same as coverage badge pattern)
var svgPath = Path.ChangeExtension(outputPath, ".svg");
var labelWidth = label.Length * 7 + 10;
var messageWidth = message.Length * 7 + 10;
var totalWidth = labelWidth + messageWidth;
var svg = $@"<svg xmlns=""http://www.w3.org/2000/svg"" width=""{totalWidth}"" height=""20"">
  <linearGradient id=""b"" x2=""0"" y2=""100%"">
    <stop offset=""0"" stop-color=""#bbb"" stop-opacity="".1""/>
    <stop offset=""1"" stop-opacity="".1""/>
  </linearGradient>
  <mask id=""m""><rect width=""{totalWidth}"" height=""20"" rx=""3"" fill=""#fff""/></mask>
  <g mask=""url(#m)"">
    <rect width=""{labelWidth}"" height=""20"" fill=""#555""/>
    <rect x=""{labelWidth}"" width=""{messageWidth}"" height=""20"" fill=""{color switch { "brightgreen" => "#4c1", "yellow" => "#dfb317", "red" => "#e05d44", _ => "#9f9f9f" }}""/>
    <rect width=""{totalWidth}"" height=""20"" fill=""url(#b)""/>
  </g>
  <g fill=""#fff"" text-anchor=""middle"" font-family=""sans-serif"" font-size=""11"">
    <text x=""{labelWidth / 2}"" y=""15"" fill=""#010101"" fill-opacity="".3"">{label}</text>
    <text x=""{labelWidth / 2}"" y=""14"">{label}</text>
    <text x=""{labelWidth + messageWidth / 2}"" y=""15"" fill=""#010101"" fill-opacity="".3"">{message}</text>
    <text x=""{labelWidth + messageWidth / 2}"" y=""14"">{message}</text>
  </g>
</svg>";
File.WriteAllText(svgPath, svg);

Console.WriteLine($"Badge: {label} | {message} ({color})");
Console.WriteLine($"  JSON: {outputPath}");
Console.WriteLine($"  SVG:  {svgPath}");
