using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary>
/// Checks the latest GitHub Actions workflow status and shows failure details.
/// Usage: dotnet run --file scripts/check-latest-workflow.cs -- [--token GITHUB_TOKEN] [--workflow workflow-name]
/// </summary>

var token = GetArgument(args, "--token") ?? Environment.GetEnvironmentVariable("GITHUB_TOKEN");
if (string.IsNullOrEmpty(token))
{
    Console.Error.WriteLine("ERROR: GITHUB_TOKEN required.");
    Console.Error.WriteLine("Usage: dotnet run --file scripts/check-latest-workflow.cs -- --token YOUR_TOKEN");
    Console.Error.WriteLine("Or set GITHUB_TOKEN environment variable.");
    return 1;
}

var repoPath = GetArgument(args, "--repo") ?? "dlrivada/SimpleMediator";
var workflowFilter = GetArgument(args, "--workflow");

using var http = new HttpClient();
http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("SimpleMediator-Check", "1.0"));

try
{
    Console.WriteLine($"Checking latest workflows for {repoPath}...\n");

    var url = $"https://api.github.com/repos/{repoPath}/actions/runs?per_page=10";
    var response = await http.GetStringAsync(url);
    var json = JsonDocument.Parse(response);

    var found = false;
    foreach (var runElement in json.RootElement.GetProperty("workflow_runs").EnumerateArray())
    {
        var name = runElement.GetProperty("name").GetString() ?? "Unknown";

        // Filter by workflow name if specified
        if (workflowFilter != null && !name.Contains(workflowFilter, StringComparison.OrdinalIgnoreCase))
            continue;

        found = true;
        var id = runElement.GetProperty("id").GetInt64();
        var status = runElement.GetProperty("status").GetString() ?? "unknown";
        var conclusion = runElement.GetProperty("conclusion").GetString();
        var branch = runElement.GetProperty("head_branch").GetString() ?? "unknown";
        var sha = runElement.GetProperty("head_sha").GetString() ?? "unknown";
        var createdAt = runElement.GetProperty("created_at").GetDateTime();
        var htmlUrl = runElement.GetProperty("html_url").GetString() ?? "";

        // Color-coded status
        if (conclusion == "success")
            Console.ForegroundColor = ConsoleColor.Green;
        else if (conclusion == "failure")
            Console.ForegroundColor = ConsoleColor.Red;
        else
            Console.ForegroundColor = ConsoleColor.Yellow;

        var statusIcon = conclusion switch
        {
            "success" => "✅",
            "failure" => "❌",
            "cancelled" => "🚫",
            _ => "⏳"
        };

        Console.WriteLine($"{statusIcon} {name}");
        Console.ResetColor();
        Console.WriteLine($"   Status: {status} → {conclusion ?? "in_progress"}");
        Console.WriteLine($"   Branch: {branch}");
        Console.WriteLine($"   Commit: {sha[..7]}");
        Console.WriteLine($"   Started: {createdAt:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"   URL: {htmlUrl}");

        // If failed, get detailed logs
        if (conclusion == "failure")
        {
            Console.WriteLine("\n   📋 Failure Details:");
            var jobsUrl = $"https://api.github.com/repos/{repoPath}/actions/runs/{id}/jobs";
            var jobsResponse = await http.GetStringAsync(jobsUrl);
            var jobsJson = JsonDocument.Parse(jobsResponse);

            foreach (var job in jobsJson.RootElement.GetProperty("jobs").EnumerateArray())
            {
                var jobName = job.GetProperty("name").GetString() ?? "Unknown";
                var jobConclusion = job.GetProperty("conclusion").GetString();

                if (jobConclusion == "failure" && job.TryGetProperty("steps", out var steps))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"\n   ❌ Job: {jobName}");
                    Console.ResetColor();

                    foreach (var step in steps.EnumerateArray())
                    {
                        var stepName = step.GetProperty("name").GetString() ?? "Unknown";
                        var stepConclusion = step.GetProperty("conclusion").GetString();

                        if (stepConclusion == "failure")
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine($"      → Failed Step: {stepName}");
                            Console.ResetColor();
                        }
                    }
                }
            }
        }

        Console.WriteLine("\n─────────────────────────────────────────────────────────────\n");

        // Only show first (latest) workflow if no filter specified
        if (workflowFilter == null)
            break;
    }

    if (!found)
    {
        Console.WriteLine($"No workflows found matching '{workflowFilter}'");
        return 1;
    }

    return 0;
}
catch (HttpRequestException ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"ERROR: {ex.Message}");
    Console.ResetColor();
    Console.WriteLine("\nTip: Make sure GITHUB_TOKEN has 'repo' or 'actions:read' scope.");
    return 1;
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"ERROR: {ex.Message}");
    Console.ResetColor();
    return 1;
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
