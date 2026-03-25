#!/usr/bin/env dotnet run
#:package ClosedXML@0.104.2

using ClosedXML.Excel;
using System.Text.RegularExpressions;

// Read TSV data
var tsvPath = args.Length > 0 ? args[0] : "/tmp/all_issues_only.tsv";
var outputPath = args.Length > 1 ? args[1] : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Encina_Issues_History.xlsx");

var lines = File.ReadAllLines(tsvPath);
Console.WriteLine($"Read {lines.Length} issues from {tsvPath}");

// Parse issue type from title prefix
static string GetIssueType(string title)
{
    if (title.StartsWith("[FEATURE]", StringComparison.OrdinalIgnoreCase)) return "Feature";
    if (title.StartsWith("[BUG]", StringComparison.OrdinalIgnoreCase)) return "Bug";
    if (title.StartsWith("[DEBT]", StringComparison.OrdinalIgnoreCase)) return "Technical Debt";
    if (title.StartsWith("[TEST]", StringComparison.OrdinalIgnoreCase)) return "Testing";
    if (title.StartsWith("[DECISION]", StringComparison.OrdinalIgnoreCase)) return "Decision";
    if (title.StartsWith("[DOC]", StringComparison.OrdinalIgnoreCase)) return "Documentation";
    if (title.StartsWith("[EPIC]", StringComparison.OrdinalIgnoreCase)) return "Epic";
    if (title.StartsWith("[REFACTOR]", StringComparison.OrdinalIgnoreCase)) return "Refactor";

    // Infer from labels
    return "Other";
}

static string InferTypeFromLabels(string type, string labels)
{
    if (type != "Other") return type;
    if (labels.Contains("bug")) return "Bug";
    if (labels.Contains("enhancement")) return "Feature";
    if (labels.Contains("technical-debt")) return "Technical Debt";
    if (labels.Contains("documentation")) return "Documentation";
    if (labels.Contains("epic")) return "Epic";
    return "Other";
}

// Determine if issue has observability phase
static string HasObservability(string title, string labels)
{
    if (labels.Contains("area-observability") || labels.Contains("area-otel"))
        return "Yes";
    // Features that typically include observability phases
    if (title.Contains("Observability", StringComparison.OrdinalIgnoreCase) ||
        title.Contains("OpenTelemetry", StringComparison.OrdinalIgnoreCase) ||
        title.Contains("metrics", StringComparison.OrdinalIgnoreCase) ||
        title.Contains("tracing", StringComparison.OrdinalIgnoreCase))
        return "Yes";
    // Major features with observability phases (known from implementation)
    if (labels.Contains("complexity-very-high") || labels.Contains("complexity-high"))
        return "Likely";
    return "No";
}

// Determine if issue has testing phase
static string HasTesting(string title, string labels)
{
    if (title.StartsWith("[TEST]", StringComparison.OrdinalIgnoreCase))
        return "Yes (dedicated)";
    if (labels.Contains("area-testing"))
        return "Yes";
    // All features get tests
    if (title.StartsWith("[FEATURE]", StringComparison.OrdinalIgnoreCase))
        return "Yes";
    if (labels.Contains("enhancement"))
        return "Yes";
    return "N/A";
}

// Determine if issue has documentation phase
static string HasDocumentation(string title, string labels)
{
    if (labels.Contains("documentation"))
        return "Yes (dedicated)";
    if (title.StartsWith("[DOC]", StringComparison.OrdinalIgnoreCase))
        return "Yes (dedicated)";
    if (title.StartsWith("[FEATURE]", StringComparison.OrdinalIgnoreCase))
        return "Yes";
    if (labels.Contains("enhancement"))
        return "Yes";
    return "N/A";
}

// Extract affected providers
static string GetAffectedProviders(string labels)
{
    var providers = new List<string>();
    if (labels.Contains("provider-efcore")) providers.Add("EF Core");
    if (labels.Contains("provider-dapper")) providers.Add("Dapper");
    if (labels.Contains("provider-ado")) providers.Add("ADO.NET");
    if (labels.Contains("provider-mongodb")) providers.Add("MongoDB");
    if (labels.Contains("provider-sqlite")) providers.Add("SQLite");
    if (labels.Contains("provider-sqlserver")) providers.Add("SQL Server");
    if (labels.Contains("provider-postgresql")) providers.Add("PostgreSQL");
    if (labels.Contains("provider-mysql")) providers.Add("MySQL");

    // Infer from area labels
    if (labels.Contains("area-sharding") || labels.Contains("area-cdc") ||
        labels.Contains("area-repository") || labels.Contains("area-bulk-operations"))
        if (providers.Count == 0) providers.Add("All 13 DB providers");

    return providers.Count > 0 ? string.Join(", ", providers) : "";
}

// Extract affected modules
static string GetAffectedModules(string labels)
{
    var modules = new List<string>();
    if (labels.Contains("area-core")) modules.Add("Encina (Core)");
    if (labels.Contains("area-messaging")) modules.Add("Encina.Messaging");
    if (labels.Contains("area-sharding")) modules.Add("Encina.Sharding");
    if (labels.Contains("area-cdc")) modules.Add("Encina.CDC");
    if (labels.Contains("area-repository")) modules.Add("Repository");
    if (labels.Contains("area-testing")) modules.Add("Testing");
    if (labels.Contains("area-caching")) modules.Add("Caching");
    if (labels.Contains("area-observability") || labels.Contains("area-otel")) modules.Add("OpenTelemetry");
    if (labels.Contains("area-validation")) modules.Add("Validation");
    if (labels.Contains("area-ddd")) modules.Add("DomainModeling");
    if (labels.Contains("area-scalability")) modules.Add("Scalability");
    if (labels.Contains("area-bulk-operations")) modules.Add("Bulk Operations");
    if (labels.Contains("area-aspnetcore")) modules.Add("AspNetCore");
    if (labels.Contains("area-security")) modules.Add("Security");
    return modules.Count > 0 ? string.Join(", ", modules) : "";
}

// Infer test types from labels and title
static string GetTestTypes(string title, string labels)
{
    var types = new List<string>();

    // From specific test labels
    if (labels.Contains("test-unit") || title.Contains("Unit Test")) types.Add("Unit");
    if (labels.Contains("test-integration") || title.Contains("Integration Test")) types.Add("Integration");
    if (labels.Contains("test-property") || title.Contains("Property")) types.Add("Property");
    if (labels.Contains("test-contract") || title.Contains("Contract")) types.Add("Contract");
    if (labels.Contains("test-guard") || title.Contains("Guard")) types.Add("Guard");
    if (labels.Contains("test-benchmark") || title.Contains("Benchmark")) types.Add("Benchmark");
    if (labels.Contains("test-load") || title.Contains("Load Test")) types.Add("Load");

    // Features typically include standard test suite
    if (types.Count == 0)
    {
        if (title.StartsWith("[FEATURE]"))
            types.AddRange(["Unit", "Guard", "Contract", "Property", "Integration"]);
        else if (title.StartsWith("[TEST]"))
            types.Add("Dedicated test issue");
        else if (title.StartsWith("[DEBT]") || title.StartsWith("[BUG]"))
            types.Add("Unit");
    }

    return types.Count > 0 ? string.Join(", ", types.Distinct()) : "";
}

using var workbook = new XLWorkbook();
var ws = workbook.Worksheets.Add("Issues");

// Headers
var headers = new[]
{
    "Number", "Type", "Title", "Milestone", "Opened", "Closed", "State",
    "Observability Phase?", "Testing Phase?", "Documentation Phase?",
    "Affected Providers", "Affected Modules", "Test Types", "Labels"
};

for (int i = 0; i < headers.Length; i++)
{
    var cell = ws.Cell(1, i + 1);
    cell.Value = headers[i];
    cell.Style.Font.Bold = true;
    cell.Style.Fill.BackgroundColor = XLColor.FromArgb(68, 114, 196);
    cell.Style.Font.FontColor = XLColor.White;
    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
}

// Freeze header row
ws.SheetView.FreezeRows(1);

int row = 2;
foreach (var line in lines.OrderBy(l =>
{
    var parts = l.Split('\t');
    return int.TryParse(parts[0], out var n) ? n : 0;
}))
{
    var parts = line.Split('\t');
    if (parts.Length < 7) continue;

    var number = int.TryParse(parts[0], out var n) ? n : 0;
    var title = parts[1];
    var state = parts[2];
    var milestone = parts[3];
    var opened = parts[4];
    var closed = parts[5];
    var labels = parts[6];

    var type = GetIssueType(title);
    type = InferTypeFromLabels(type, labels);

    ws.Cell(row, 1).Value = number;
    ws.Cell(row, 2).Value = type;
    ws.Cell(row, 3).Value = title;
    ws.Cell(row, 4).Value = milestone;
    ws.Cell(row, 5).Value = opened;
    ws.Cell(row, 6).Value = closed;
    ws.Cell(row, 7).Value = state == "closed" ? "Closed" : "Open";
    ws.Cell(row, 8).Value = HasObservability(title, labels);
    ws.Cell(row, 9).Value = HasTesting(title, labels);
    ws.Cell(row, 10).Value = HasDocumentation(title, labels);
    ws.Cell(row, 11).Value = GetAffectedProviders(labels);
    ws.Cell(row, 12).Value = GetAffectedModules(labels);
    ws.Cell(row, 13).Value = GetTestTypes(title, labels);
    ws.Cell(row, 14).Value = labels.Replace(";", ", ");

    // Color coding by type
    var typeColor = type switch
    {
        "Feature" => XLColor.FromArgb(226, 239, 218),
        "Bug" => XLColor.FromArgb(252, 228, 214),
        "Technical Debt" => XLColor.FromArgb(255, 242, 204),
        "Testing" => XLColor.FromArgb(221, 235, 247),
        "Decision" => XLColor.FromArgb(228, 223, 236),
        "Documentation" => XLColor.FromArgb(217, 225, 242),
        "Epic" => XLColor.FromArgb(248, 203, 173),
        _ => XLColor.White
    };

    for (int c = 1; c <= headers.Length; c++)
        ws.Cell(row, c).Style.Fill.BackgroundColor = typeColor;

    // Color coding for state
    if (state == "open")
        ws.Cell(row, 7).Style.Font.FontColor = XLColor.FromArgb(196, 89, 17);

    row++;
}

// Auto-fit columns
ws.Columns().AdjustToContents();
// Cap title column width
if (ws.Column(3).Width > 80)
    ws.Column(3).Width = 80;

// Add auto-filter
ws.RangeUsed()!.SetAutoFilter();

// Summary sheet
var summary = workbook.Worksheets.Add("Summary");

summary.Cell(1, 1).Value = "Encina Issues Summary";
summary.Cell(1, 1).Style.Font.Bold = true;
summary.Cell(1, 1).Style.Font.FontSize = 16;

summary.Cell(3, 1).Value = "Total Issues:";
summary.Cell(3, 1).Style.Font.Bold = true;
summary.Cell(3, 2).Value = lines.Length;

summary.Cell(4, 1).Value = "Generated:";
summary.Cell(4, 1).Style.Font.Bold = true;
summary.Cell(4, 2).Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm");

summary.Cell(6, 1).Value = "Issues by Milestone";
summary.Cell(6, 1).Style.Font.Bold = true;
summary.Cell(6, 1).Style.Font.FontSize = 14;

var milestoneGroups = lines
    .Select(l => l.Split('\t'))
    .Where(p => p.Length >= 7)
    .GroupBy(p => string.IsNullOrEmpty(p[3]) ? "(No milestone)" : p[3])
    .OrderByDescending(g => g.Count())
    .ToList();

int srow = 7;
summary.Cell(srow, 1).Value = "Milestone";
summary.Cell(srow, 2).Value = "Count";
summary.Cell(srow, 1).Style.Font.Bold = true;
summary.Cell(srow, 2).Style.Font.Bold = true;
srow++;

foreach (var g in milestoneGroups)
{
    summary.Cell(srow, 1).Value = g.Key;
    summary.Cell(srow, 2).Value = g.Count();
    srow++;
}

srow += 2;
summary.Cell(srow, 1).Value = "Issues by Type";
summary.Cell(srow, 1).Style.Font.Bold = true;
summary.Cell(srow, 1).Style.Font.FontSize = 14;
srow++;

summary.Cell(srow, 1).Value = "Type";
summary.Cell(srow, 2).Value = "Count";
summary.Cell(srow, 1).Style.Font.Bold = true;
summary.Cell(srow, 2).Style.Font.Bold = true;
srow++;

var typeGroups = lines
    .Select(l => l.Split('\t'))
    .Where(p => p.Length >= 7)
    .GroupBy(p =>
    {
        var t = GetIssueType(p[1]);
        return InferTypeFromLabels(t, p[6]);
    })
    .OrderByDescending(g => g.Count())
    .ToList();

foreach (var g in typeGroups)
{
    summary.Cell(srow, 1).Value = g.Key;
    summary.Cell(srow, 2).Value = g.Count();
    srow++;
}

summary.Columns().AdjustToContents();

workbook.SaveAs(outputPath);
Console.WriteLine($"Excel generated: {outputPath}");
Console.WriteLine($"Total issues: {lines.Length}");
Console.WriteLine($"Milestones: {milestoneGroups.Count}");
Console.WriteLine($"Types: {typeGroups.Count}");
