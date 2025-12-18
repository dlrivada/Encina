using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Monitors GitHub Actions workflows and reports failures automatically.
/// Usage: dotnet run --file scripts/monitor-workflows.cs -- [--token GITHUB_TOKEN] [--interval 60]
/// </summary>

var token = GetArgument(args, "--token") ?? Environment.GetEnvironmentVariable("GITHUB_TOKEN");
if (string.IsNullOrEmpty(token))
{
    Console.Error.WriteLine("ERROR: GITHUB_TOKEN required. Provide via --token argument or GITHUB_TOKEN environment variable.");
    Console.Error.WriteLine("Get token from: https://github.com/settings/tokens");
    return 1;
}

var intervalSeconds = int.TryParse(GetArgument(args, "--interval"), out var interval) ? interval : 60;
var repoPath = GetArgument(args, "--repo") ?? "dlrivada/SimpleMediator";

Console.WriteLine($"Monitoring GitHub Actions for {repoPath} every {intervalSeconds} seconds...");
Console.WriteLine("Press Ctrl+C to stop.");
Console.WriteLine();

using var http = new HttpClient();
http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("SimpleMediator-Monitor", "1.0"));

var knownRuns = new HashSet<long>();

while (true)
{
    try
    {
        var runs = await GetWorkflowRuns(http, repoPath);

        foreach (var run in runs)
        {
            // Skip if we've already reported this run
            if (knownRuns.Contains(run.Id))
                continue;

            knownRuns.Add(run.Id);

            // Only report failures
            if (run.Conclusion != "failure")
                continue;

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ WORKFLOW FAILED: {run.Name}");
            Console.ResetColor();
            Console.WriteLine($"   Run ID: {run.Id}");
            Console.WriteLine($"   Branch: {run.HeadBranch}");
            Console.WriteLine($"   Commit: {run.HeadSha[..7]}");
            Console.WriteLine($"   Started: {run.CreatedAt}");
            Console.WriteLine($"   URL: {run.HtmlUrl}");
            Console.WriteLine();

            // Get detailed logs
            var logs = await GetWorkflowLogs(http, repoPath, run.Id);
            if (!string.IsNullOrEmpty(logs))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("📋 FAILURE LOGS:");
                Console.ResetColor();
                Console.WriteLine(logs);
                Console.WriteLine();
            }

            Console.WriteLine("─────────────────────────────────────────────────────────────");
            Console.WriteLine();
        }

        await Task.Delay(TimeSpan.FromSeconds(intervalSeconds));
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"ERROR: {ex.Message}");
        Console.ResetColor();
        await Task.Delay(TimeSpan.FromSeconds(intervalSeconds));
    }
}

static async Task<List<WorkflowRun>> GetWorkflowRuns(HttpClient http, string repo)
{
    var url = $"https://api.github.com/repos/{repo}/actions/runs?per_page=10";
    var response = await http.GetStringAsync(url);
    var json = JsonDocument.Parse(response);

    var runs = new List<WorkflowRun>();
    foreach (var runElement in json.RootElement.GetProperty("workflow_runs").EnumerateArray())
    {
        runs.Add(new WorkflowRun
        {
            Id = runElement.GetProperty("id").GetInt64(),
            Name = runElement.GetProperty("name").GetString() ?? "Unknown",
            Status = runElement.GetProperty("status").GetString() ?? "unknown",
            Conclusion = runElement.GetProperty("conclusion").GetString(),
            HeadBranch = runElement.GetProperty("head_branch").GetString() ?? "unknown",
            HeadSha = runElement.GetProperty("head_sha").GetString() ?? "unknown",
            CreatedAt = runElement.GetProperty("created_at").GetDateTime(),
            HtmlUrl = runElement.GetProperty("html_url").GetString() ?? ""
        });
    }

    return runs;
}

static async Task<string?> GetWorkflowLogs(HttpClient http, string repo, long runId)
{
    try
    {
        var url = $"https://api.github.com/repos/{repo}/actions/runs/{runId}/jobs";
        var response = await http.GetStringAsync(url);
        var json = JsonDocument.Parse(response);

        var failedSteps = new List<string>();

        foreach (var job in json.RootElement.GetProperty("jobs").EnumerateArray())
        {
            var jobName = job.GetProperty("name").GetString() ?? "Unknown Job";
            var jobConclusion = job.GetProperty("conclusion").GetString();

            if (jobConclusion != "failure")
                continue;

            if (job.TryGetProperty("steps", out var steps))
            {
                foreach (var step in steps.EnumerateArray())
                {
                    var stepConclusion = step.GetProperty("conclusion").GetString();
                    if (stepConclusion == "failure")
                    {
                        var stepName = step.GetProperty("name").GetString() ?? "Unknown Step";
                        failedSteps.Add($"Job: {jobName} → Step: {stepName}");
                    }
                }
            }
        }

        return failedSteps.Count > 0
            ? string.Join("\n", failedSteps)
            : "No detailed logs available (check GitHub Actions web UI)";
    }
    catch
    {
        return null;
    }
}

static string? GetArgument(string[] args, string name)
{
    for (int i = 0; i < args.Length - 1; i++)
    {
        if (args[i] == name)
            return args[i + 1];
    }
    return null;
}

record WorkflowRun
{
    public long Id { get; init; }
    public string Name { get; init; } = "";
    public string Status { get; init; } = "";
    public string? Conclusion { get; init; }
    public string HeadBranch { get; init; } = "";
    public string HeadSha { get; init; } = "";
    public DateTime CreatedAt { get; init; }
    public string HtmlUrl { get; init; } = "";
}

return 0;
