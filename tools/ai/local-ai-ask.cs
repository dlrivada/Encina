// local-ai-ask.cs — one bounded request to the maintainer's local llama-server with a usage ledger.
//
// Usage:
//   dotnet run tools/ai/local-ai-ask.cs -- --task <name> --brief <file> [--input <file>]... [--out <file>]
//                                              [--system <file>] [--max-tokens 8192] [--url http://127.0.0.1:8080]
//                                              [--health-attempts 6] [--health-wait-seconds 20]
//                                              [--engine local|standin|auto] [--state-file <file>]
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
// - Switch (#1593): the first thing it does is read the local-model switch, <main checkout>\artifacts\local-ai\state.json
//   (managed by tools/ai/local-ai-state.ps1; absent = on; --state-file overrides the path). With the switch OFF the
//   same prompt goes to the stand-in engine instead of llama-server, no health retries: the Claude Code CLI
//   (`claude -p --model haiku --effort low --setting-sources "" --tools "" --output-format json`, the prompt on stdin,
//   the body of .claude/agents/local-ai-standin.md as --system-prompt, working directory the repository root of
//   --out). The reply is written to --out exactly like a llama reply, and one row is appended to
//   <artifacts root>\local-ai\standin-ledger.csv: the ledger.csv columns plus costUsd (paid tokens, never mixed into
//   ledger.csv). --engine local|standin|auto (default auto = follow the switch) forces a route, for tests.
// - Exit code 0 on success, 1 on any failure (server down after every retry, HTTP error, empty reply, a stand-in CLI
//   error), 3 when the switch is OFF and the claude CLI is not available: spawn the local-ai-standin agent through the
//   Agent tool with the same --task/--brief/--input/--out and record it with local-ai-state.ps1 -RecordStandin.
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

// --- The local-model switch (#1593) ---------------------------------------------------------------------------
var engine = Get("--engine") ?? "auto";
if (engine is not ("local" or "standin" or "auto"))
{
    Console.Error.WriteLine($"--engine must be local, standin or auto (got '{engine}')");
    return 1;
}

string? switchOffReason = null;
if (engine == "standin")
{
    switchOffReason = "forced with --engine standin";
}
else if (engine == "auto")
{
    var stateFile = Get("--state-file") ?? ResolveDefaultStateFile();
    if (stateFile is not null && File.Exists(stateFile))
    {
        try
        {
            var state = JsonNode.Parse(File.ReadAllText(stateFile));
            if (string.Equals(state?["state"]?.ToString(), "off", StringComparison.OrdinalIgnoreCase))
                switchOffReason = state?["reason"]?.ToString() ?? "no reason recorded";
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"cannot read the local-model state file {stateFile}: {ex.Message}");
            return 1;
        }
    }
}

if (switchOffReason is not null)
    return await RunStandin(switchOffReason);

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

// <main checkout>\artifacts\local-ai\state.json: the parent of `git rev-parse --path-format=absolute --git-common-dir`
// run from this script's own folder, so every worktree resolves the same file (same rule as local-ai-state.ps1).
string? ResolveDefaultStateFile()
{
    var entry = AppContext.GetData("EntryPointFilePath") as string;
    var scriptDir = entry is null ? Directory.GetCurrentDirectory() : Path.GetDirectoryName(Path.GetFullPath(entry))!;
    try
    {
        var psi = new ProcessStartInfo("git") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        foreach (var a in new[] { "-C", scriptDir, "rev-parse", "--path-format=absolute", "--git-common-dir" }) psi.ArgumentList.Add(a);
        using var p = Process.Start(psi)!;
        var common = p.StandardOutput.ReadToEnd().Trim();
        p.WaitForExit();
        if (p.ExitCode != 0 || common.Length == 0) return null;
        var mainRoot = Path.GetDirectoryName(Path.GetFullPath(common));
        return mainRoot is null ? null : Path.Combine(mainRoot, "artifacts", "local-ai", "state.json");
    }
    catch (Exception)
    {
        return null;
    }
}

// The nearest ancestor of the --out file that holds a .git entry (file in a worktree, folder in the main checkout).
string RepositoryRootOf(string path)
{
    var dir = Path.GetDirectoryName(Path.GetFullPath(path));
    while (dir is not null)
    {
        if (Directory.Exists(Path.Combine(dir, ".git")) || File.Exists(Path.Combine(dir, ".git"))) return dir;
        dir = Path.GetDirectoryName(dir);
    }
    return Directory.GetCurrentDirectory();
}

// The stand-in engine (#1593): same prompt as the llama request, answered by the Claude Code CLI on haiku.
async Task<int> RunStandin(string reason)
{
    var repoRoot = RepositoryRootOf(outPath);
    var scriptDir = Path.GetDirectoryName(Path.GetFullPath(AppContext.GetData("EntryPointFilePath") as string ?? "tools/ai/local-ai-ask.cs"))!;
    var agentFile = Path.GetFullPath(Path.Combine(scriptDir, "..", "..", ".claude", "agents", "local-ai-standin.md"));
    if (!File.Exists(agentFile))
    {
        Console.Error.WriteLine($"local model switched off ({reason}) but the stand-in instructions are missing: {agentFile}");
        return 1;
    }

    // The agent file's body (after the frontmatter) is the stand-in's system prompt: one source of truth.
    var agentText = File.ReadAllText(agentFile).Replace("\r\n", "\n");
    var bodyStart = agentText.StartsWith("---\n", StringComparison.Ordinal) ? agentText.IndexOf("\n---\n", 4, StringComparison.Ordinal) : -1;
    var standinPrompt = (bodyStart >= 0 ? agentText[(bodyStart + 5)..] : agentText).Trim();
    // A caller's own --system file (the batch scripts' JSON-only rules) comes after the stand-in's instructions.
    if (systemPath is not null)
        standinPrompt += "\n\n" + File.ReadAllText(systemPath).Trim();

    var psi = new ProcessStartInfo("claude")
    {
        RedirectStandardInput = true,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        WorkingDirectory = repoRoot,
        StandardInputEncoding = new UTF8Encoding(false),
        StandardOutputEncoding = Encoding.UTF8,
    };
    foreach (var a in new[] { "-p", "--model", "haiku", "--effort", "low", "--setting-sources", "", "--tools", "", "--system-prompt", standinPrompt, "--output-format", "json", "--no-session-persistence" })
        psi.ArgumentList.Add(a);
    // --setting-sources "" does not stop the repository CLAUDE.md/AGENTS.md from loading (about 2k extra tokens).
    psi.Environment["CLAUDE_CODE_DISABLE_CLAUDE_MDS"] = "1";
    psi.StandardErrorEncoding = Encoding.UTF8;

    Process proc;
    try
    {
        proc = Process.Start(psi)!;
    }
    catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or FileNotFoundException)
    {
        Console.Error.WriteLine($"local model switched off ({reason}) and the claude CLI is not available ({ex.Message}): spawn the local-ai-standin agent through the Agent tool with --task/--brief/--input/--out unchanged, then record it with tools/ai/local-ai-state.ps1 -RecordStandin; see the local-ai-task skill");
        return 3;
    }

    using (proc)
    {
        var sw2 = Stopwatch.StartNew();
        var stdoutTask = proc.StandardOutput.ReadToEndAsync();
        var stderrTask = proc.StandardError.ReadToEndAsync();
        try
        {
            await proc.StandardInput.WriteAsync(sb.ToString());
            proc.StandardInput.Close();
        }
        catch (IOException)
        {
            // The CLI exited before reading the prompt (for example an auth failure): its output below says why.
        }
        await proc.WaitForExitAsync();
        var stdout = await stdoutTask;
        var stderr = await stderrTask;
        sw2.Stop();

        JsonNode? reply;
        try { reply = JsonNode.Parse(stdout); }
        catch (JsonException)
        {
            Console.Error.WriteLine($"stand-in CLI returned no JSON (exit {proc.ExitCode}): {stderr} {stdout}");
            return 1;
        }

        var isError = reply?["is_error"]?.GetValue<bool>() ?? true;
        var result = reply?["result"]?.ToString() ?? string.Empty;
        if (isError || proc.ExitCode != 0)
        {
            Console.Error.WriteLine($"stand-in CLI error (exit {proc.ExitCode}): {result} {stderr}".TrimEnd());
            return 1;
        }

        if (string.IsNullOrWhiteSpace(result))
        {
            Console.Error.WriteLine("Empty reply from the stand-in.");
            return 1;
        }

        long Tok(string name) => reply?["usage"]?[name]?.GetValue<long>() ?? 0;
        var inTokens = Tok("input_tokens") + Tok("cache_creation_input_tokens") + Tok("cache_read_input_tokens");
        var outTokens = Tok("output_tokens");
        var secs = (reply?["duration_ms"]?.GetValue<double>() ?? sw2.Elapsed.TotalMilliseconds) / 1000.0;
        var tokPerSec = secs > 0 ? outTokens / secs : 0;
        var cost = reply?["total_cost_usd"]?.GetValue<double>() ?? 0;

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath))!);
        File.WriteAllText(outPath, result);

        // Next to the ledger.csv this call would have used: --ledger's folder when given, else <out>\..\.
        var standinLedger = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(ledgerPath))!, "standin-ledger.csv");
        Directory.CreateDirectory(Path.GetDirectoryName(standinLedger)!);
        if (!File.Exists(standinLedger))
            File.WriteAllText(standinLedger, "timestampUtc,task,promptTokens,completionTokens,seconds,tokensPerSecond,outFile,costUsd\n");
        File.AppendAllText(standinLedger, string.Create(CultureInfo.InvariantCulture,
            $"{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ},{task},{inTokens},{outTokens},{secs:F1},{tokPerSec:F1},{outPath},{cost:F4}\n"));

        Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
            $"stand-in ({reason}) task={task} prompt={inTokens} completion={outTokens} seconds={secs:F1} costUsd={cost:F4} out={outPath}"));
        return 0;
    }
}
