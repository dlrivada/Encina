// local-ai-ask.cs — one bounded request to the maintainer's local llama-server with a usage ledger.
//
// Usage:
//   dotnet run tools/ai/local-ai-ask.cs -- --task <name> --brief <file> [--input <file>]... [--out <file>]
//                                              [--system <file>] [--max-tokens 8192] [--url http://127.0.0.1:8080]
//                                              [--health-attempts 6] [--health-wait-seconds 20]
//
// - The brief is the user message; each --input file is appended as a fenced block (path + content).
// - Thinking is disabled per request (chat_template_kwargs.enable_thinking=false), as the maintainer's trials require.
// - The reply is written to --out (default artifacts/local-ai/out/<task>.md) and a CSV line is appended to
//   artifacts/local-ai/ledger.csv: timestampUtc, task, promptTokens, completionTokens, seconds, tokensPerSecond, outFile.
// - The health check is retried up to --health-attempts times (default 6), --health-wait-seconds apart
//   (default 20s, so ~2 minutes total): the pre-draft queue and a worker can share one llama-server slot, and
//   a busy server must not be reported as "down" from a single failed probe (#1345). Each attempt uses a 15s
//   timeout; a timeout, exception or non-"ok" status prints "llama-server busy or unreachable ... retrying in
//   <w> s" to stderr and waits before the next attempt. Only the last attempt's failure is fatal.
// - Exit code 0 on success, 1 on any failure (server down after every retry, HTTP error, empty reply).
//
// This is for "read -> produce an artifact" work (classification, summaries, drafts). Agentic coding tasks that must
// edit files and run builds still go through opencode; their token usage is read from the llama-server log instead.

using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

var args2 = Environment.GetCommandLineArgs().Skip(1).ToArray();
string? Get(string name)
{
    for (var i = 0; i < args2.Length - 1; i++)
        if (args2[i] == name) return args2[i + 1];
    return null;
}
IEnumerable<string> GetAll(string name)
{
    for (var i = 0; i < args2.Length - 1; i++)
        if (args2[i] == name) yield return args2[i + 1];
}

var task = Get("--task") ?? "adhoc";
var briefPath = Get("--brief") ?? throw new ArgumentException("--brief <file> is required");
var url = (Get("--url") ?? "http://127.0.0.1:8080").TrimEnd('/');
var maxTokens = int.Parse(Get("--max-tokens") ?? "8192", CultureInfo.InvariantCulture);
var outPath = Get("--out") ?? Path.Combine("artifacts", "local-ai", "out", task + ".md");
var systemPath = Get("--system");
var healthAttempts = int.Parse(Get("--health-attempts") ?? "6", CultureInfo.InvariantCulture);
var healthWaitSeconds = int.Parse(Get("--health-wait-seconds") ?? "20", CultureInfo.InvariantCulture);
var ledgerPath = Get("--ledger") ?? Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(outPath))!, "..", "ledger.csv"));

var sb = new StringBuilder(File.ReadAllText(briefPath));
foreach (var input in GetAll("--input"))
{
    sb.AppendLine().AppendLine().AppendLine(CultureInfo.InvariantCulture, $"### File: {input}").AppendLine("```");
    sb.AppendLine(File.ReadAllText(input));
    sb.AppendLine("```");
}

var systemPrompt = systemPath is null
    ? "You are a precise assistant working on the Encina .NET repository. Follow every numbered point of the brief without omitting any. Write only the requested deliverable, in English, no preamble."
    : File.ReadAllText(systemPath);

using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(30) };

var healthy = false;
for (var attempt = 1; attempt <= healthAttempts; attempt++)
{
    string? reason = null;
    try
    {
        using var healthCts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var health = JsonNode.Parse(await http.GetStringAsync($"{url}/health", healthCts.Token));
        if (health?["status"]?.ToString() == "ok") { healthy = true; break; }
        reason = $"status={health}";
    }
    catch (OperationCanceledException)
    {
        reason = "timed out after 15s";
    }
    catch (Exception ex)
    {
        reason = ex.Message;
    }

    if (attempt < healthAttempts)
    {
        Console.Error.WriteLine($"llama-server busy or unreachable at {url} (attempt {attempt}/{healthAttempts}): {reason}; retrying in {healthWaitSeconds} s");
        await Task.Delay(TimeSpan.FromSeconds(healthWaitSeconds));
    }
    else
    {
        Console.Error.WriteLine($"llama-server unreachable at {url}: {reason}");
    }
}

if (!healthy)
{
    return 1;
}

var body = new JsonObject
{
    ["model"] = "qwen3.8-27b",
    ["messages"] = new JsonArray(
        new JsonObject { ["role"] = "system", ["content"] = systemPrompt },
        new JsonObject { ["role"] = "user", ["content"] = sb.ToString() }),
    ["max_tokens"] = maxTokens,
    ["stream"] = false,
    ["chat_template_kwargs"] = new JsonObject { ["enable_thinking"] = false }
};

var sw = Stopwatch.StartNew();
using var request = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json");
using var response = await http.PostAsync($"{url}/v1/chat/completions", request);
sw.Stop();

var json = await response.Content.ReadAsStringAsync();
if (!response.IsSuccessStatusCode)
{
    Console.Error.WriteLine($"HTTP {(int)response.StatusCode}: {json}");
    return 1;
}

var node = JsonNode.Parse(json)!;
var content = node["choices"]?[0]?["message"]?["content"]?.ToString() ?? string.Empty;
var promptTokens = node["usage"]?["prompt_tokens"]?.GetValue<int>() ?? 0;
var completionTokens = node["usage"]?["completion_tokens"]?.GetValue<int>() ?? 0;

if (string.IsNullOrWhiteSpace(content))
{
    Console.Error.WriteLine("Empty reply from the model.");
    return 1;
}

Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath))!);
File.WriteAllText(outPath, content);

var seconds = sw.Elapsed.TotalSeconds;
var tps = seconds > 0 ? completionTokens / seconds : 0;
Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(ledgerPath))!);
if (!File.Exists(ledgerPath))
    File.WriteAllText(ledgerPath, "timestampUtc,task,promptTokens,completionTokens,seconds,tokensPerSecond,outFile\n");
File.AppendAllText(ledgerPath, string.Create(CultureInfo.InvariantCulture,
    $"{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ},{task},{promptTokens},{completionTokens},{seconds:F1},{tps:F1},{outPath}\n"));

Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
    $"task={task} prompt={promptTokens} completion={completionTokens} seconds={seconds:F1} tok/s={tps:F1} out={outPath}"));
return 0;
