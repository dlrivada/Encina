// Run: dotnet run --file scripts/check-sonarcloud-coverage.cs
// Checks SonarCloud coverage metrics for the Encina project via API

using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

const string projectKey = "dlrivada_Encina";
const string baseUrl = "https://sonarcloud.io/api";

using var client = new HttpClient();
client.DefaultRequestHeaders.Add("Accept", "application/json");

Console.WriteLine("=== SonarCloud Coverage Check ===");
Console.WriteLine($"Project: {projectKey}");
Console.WriteLine();

// Fetch coverage metrics
var metricsUrl = $"{baseUrl}/measures/component?component={projectKey}&metricKeys=coverage,line_coverage,branch_coverage,lines_to_cover,uncovered_lines,conditions_to_cover,uncovered_conditions";

Console.WriteLine($"Fetching metrics from: {metricsUrl}");
Console.WriteLine();

try
{
    var response = await client.GetAsync(metricsUrl);
    var json = await response.Content.ReadAsStringAsync();

    if (!response.IsSuccessStatusCode)
    {
        Console.Error.WriteLine($"API request failed: {response.StatusCode}");
        Console.Error.WriteLine(json);
        return 1;
    }

    var result = JsonSerializer.Deserialize(json, SonarJsonContext.Default.MeasuresResponse);

    if (result?.Component?.Measures is null || result.Component.Measures.Count == 0)
    {
        Console.WriteLine("No coverage data available yet.");
        Console.WriteLine();
        Console.WriteLine("Possible reasons:");
        Console.WriteLine("  1. First analysis hasn't completed");
        Console.WriteLine("  2. Coverage report not uploaded properly");
        Console.WriteLine("  3. Scanner couldn't find coverage files");
        Console.WriteLine();

        // Try to get project analysis info
        await CheckAnalysisStatus(client, projectKey);
        return 0;
    }

    Console.WriteLine("Coverage Metrics:");
    Console.WriteLine(new string('-', 50));

    foreach (var measure in result.Component.Measures)
    {
        var displayName = measure.Metric switch
        {
            "coverage" => "Overall Coverage",
            "line_coverage" => "Line Coverage",
            "branch_coverage" => "Branch Coverage",
            "lines_to_cover" => "Lines to Cover",
            "uncovered_lines" => "Uncovered Lines",
            "conditions_to_cover" => "Conditions to Cover",
            "uncovered_conditions" => "Uncovered Conditions",
            _ => measure.Metric
        };

        var unit = measure.Metric.Contains("coverage") ? "%" : "";
        Console.WriteLine($"  {displayName,-25}: {measure.Value}{unit}");
    }

    Console.WriteLine();

    // Check if coverage is adequate
    var coverageMeasure = result.Component.Measures.FirstOrDefault(m => m.Metric == "coverage");
    if (coverageMeasure is not null && double.TryParse(coverageMeasure.Value, out var coverageValue))
    {
        if (coverageValue >= 80)
        {
            Console.WriteLine($"Coverage {coverageValue}% meets quality gate threshold (80%)");
        }
        else if (coverageValue >= 50)
        {
            Console.WriteLine($"Coverage {coverageValue}% is acceptable but below target (80%)");
        }
        else if (coverageValue > 0)
        {
            Console.WriteLine($"Coverage {coverageValue}% needs improvement - target is 80%");
        }
    }

    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 1;
}

static async Task CheckAnalysisStatus(HttpClient client, string projectKey)
{
    var analysesUrl = $"https://sonarcloud.io/api/project_analyses/search?project={projectKey}&ps=5";

    Console.WriteLine("Recent Analyses:");
    Console.WriteLine(new string('-', 50));

    try
    {
        var response = await client.GetAsync(analysesUrl);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine("  Unable to fetch analysis history");
            return;
        }

        var result = JsonSerializer.Deserialize(json, SonarJsonContext.Default.AnalysesResponse);

        if (result?.Analyses is null || result.Analyses.Count == 0)
        {
            Console.WriteLine("  No analyses found");
            return;
        }

        foreach (var analysis in result.Analyses.Take(5))
        {
            Console.WriteLine($"  [{analysis.Date ?? "unknown"}] {analysis.ProjectVersion ?? "no version"}");
            if (analysis.Events is { Count: > 0 })
            {
                foreach (var evt in analysis.Events)
                {
                    Console.WriteLine($"    - {evt.Category}: {evt.Name}");
                }
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  Error fetching analyses: {ex.Message}");
    }
}

// JSON models
sealed class MeasuresResponse
{
    [JsonPropertyName("component")]
    public ComponentData? Component { get; set; }
}

sealed class ComponentData
{
    [JsonPropertyName("key")]
    public string? Key { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("measures")]
    public List<Measure>? Measures { get; set; }
}

sealed class Measure
{
    [JsonPropertyName("metric")]
    public string Metric { get; set; } = "";

    [JsonPropertyName("value")]
    public string Value { get; set; } = "";
}

sealed class AnalysesResponse
{
    [JsonPropertyName("analyses")]
    public List<Analysis>? Analyses { get; set; }
}

sealed class Analysis
{
    [JsonPropertyName("key")]
    public string? Key { get; set; }

    [JsonPropertyName("date")]
    public string? Date { get; set; }

    [JsonPropertyName("projectVersion")]
    public string? ProjectVersion { get; set; }

    [JsonPropertyName("events")]
    public List<AnalysisEvent>? Events { get; set; }
}

sealed class AnalysisEvent
{
    [JsonPropertyName("category")]
    public string? Category { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

[JsonSerializable(typeof(MeasuresResponse))]
[JsonSerializable(typeof(AnalysesResponse))]
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
internal sealed partial class SonarJsonContext : JsonSerializerContext;
