// session-stats.cs — Summarises Claude Code transcripts of one project since a date, for the
// maintainer's control board "Week" tab (#1382).
//
// Reads every top-level `~/.claude/projects/<project>/<session>.jsonl` transcript and, when
// present, its subagent transcripts under `<session>/subagents/*.jsonl` (each paired with a
// `.meta.json` naming the agent type and model). Per session and per day it computes: active time
// (events merged across a 5-minute gap), human prompts (bot- and system-generated user turns are
// excluded by a prefix list), reply latency (the gap between Claude's last assistant message and
// the next human prompt, split at 30 minutes into "review" and "away"), tokens per model
// (de-duplicated by requestId, since a model can stream more than one line for the same request),
// and per-agent-type spawn counts and token totals.
//
// Usage:
//   dotnet run --file tools/ai/board/session-stats.cs -- <projectDir> <sinceIsoUtc> <outJson>
//
// Output: a single JSON object ({ generatedUtc, since, sessions: [...], days: [...] }) written to
// <outJson>. A session with no event on or after <sinceIsoUtc> is omitted.
#pragma warning disable CA1305, CA1310, CA1852

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using static H;

if (args.Length != 3)
{
    Console.Error.WriteLine("usage: dotnet run --file tools/ai/board/session-stats.cs -- <projectDir> <sinceIsoUtc> <outJson>");
    return 2;
}

var projectDir = args[0];
var since = DateTimeOffset.Parse(args[1], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
var outPath = args[2];
var activityGap = TimeSpan.FromMinutes(5);
var awayGap = TimeSpan.FromMinutes(30);

string[] botPrefixes = ["<task-notification", "Another Claude session", "<local-command", "<command-", "Caveat:", "<system-reminder", "[Request interrupted", "This session is being continued"];

var sessions = new JsonArray();
var dayAgg = new SortedDictionary<string, DayAgg>(StringComparer.Ordinal);

foreach (var main in Directory.GetFiles(projectDir, "*.jsonl"))
{
    var id = Path.GetFileNameWithoutExtension(main);
    var sessionDir = Path.Combine(projectDir, id);
    var s = new SessAgg { Id = id };
    ReadTranscript(main, s, isMain: true, agentType: null, agentModel: null, since);
    if (Directory.Exists(sessionDir))
    {
        foreach (var sub in Directory.GetFiles(sessionDir, "*.jsonl", SearchOption.AllDirectories))
        {
            string? type = null, model = null;
            var meta = Path.ChangeExtension(sub, null) + ".meta.json";
            if (File.Exists(meta))
            {
                var m = JsonNode.Parse(File.ReadAllText(meta));
                type = m?["agentType"]?.GetValue<string>();
                model = m?["model"]?.GetValue<string>();
            }
            ReadTranscript(sub, s, isMain: false, agentType: type ?? "unknown", agentModel: model, since);
        }
        var titleFile = Path.Combine(sessionDir, "custom-title.json");
        if (File.Exists(titleFile))
        {
            try { s.Title = JsonNode.Parse(File.ReadAllText(titleFile))?["title"]?.GetValue<string>(); }
            catch (JsonException) { }
        }
    }
    if (s.AllTimes.Count == 0) continue;
    sessions.Add(s.ToJson(activityGap, awayGap, dayAgg));
}

var days = new JsonArray();
foreach (var (day, d) in dayAgg) days.Add(d.ToJson(day, activityGap));
var root = new JsonObject
{
    ["generatedUtc"] = Iso(DateTimeOffset.UtcNow),
    ["since"] = Iso(since),
    ["sessions"] = sessions,
    ["days"] = days,
};
File.WriteAllText(outPath, root.ToJsonString(new JsonSerializerOptions { WriteIndented = false }));
Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"sessions={sessions.Count} days={days.Count} -> {outPath}"));
return 0;

void ReadTranscript(string path, SessAgg s, bool isMain, string? agentType, string? agentModel, DateTimeOffset since)
{
    SubAgg? sub = null;
    if (!isMain)
    {
        sub = new SubAgg { Type = agentType!, Model = agentModel };
        s.Subs.Add(sub);
    }
    DateTimeOffset? prevTs = null;
    string? prevReq = null;
    foreach (var line in File.ReadLines(path))
    {
        if (line.Length == 0) continue;
        JsonNode? j;
        try { j = JsonNode.Parse(line); }
        catch (JsonException) { continue; }
        var tsStr = j?["timestamp"]?.GetValue<string>();
        if (tsStr is null) continue;
        var ts = DateTimeOffset.Parse(tsStr, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
        if (ts < since) { prevTs = ts; continue; }
        var type = j!["type"]?.GetValue<string>();
        s.AllTimes.Add(ts);
        if (isMain) s.MainTimes.Add(ts);
        if (type == "assistant")
        {
            if (isMain) s.MainAssistantTimes.Add(ts);
            var msg = j["message"];
            var model = msg?["model"]?.GetValue<string>() ?? "unknown";
            if (model == "<synthetic>") { prevTs = ts; continue; }
            var req = j["requestId"]?.GetValue<string>() ?? msg?["id"]?.GetValue<string>() ?? Guid.NewGuid().ToString();
            var u = msg?["usage"];
            var call = s.Calls.TryGetValue(req, out var c) ? c : s.Calls[req] = new Call { Model = model, IsMain = isMain, Sub = sub, Start = prevReq == req ? null : prevTs };
            call.End = ts;
            call.In = Math.Max(call.In, u?["input_tokens"]?.GetValue<long>() ?? 0);
            call.CacheW = Math.Max(call.CacheW, u?["cache_creation_input_tokens"]?.GetValue<long>() ?? 0);
            call.CacheR = Math.Max(call.CacheR, u?["cache_read_input_tokens"]?.GetValue<long>() ?? 0);
            call.Out = Math.Max(call.Out, u?["output_tokens"]?.GetValue<long>() ?? 0);
            if (msg?["content"] is JsonArray arr)
                foreach (var b in arr)
                    if (b?["type"]?.GetValue<string>() == "tool_use") call.Tools++;
            prevReq = req;
        }
        else if (type == "user" && isMain && j["isSidechain"]?.GetValue<bool>() != true && j["isMeta"]?.GetValue<bool>() != true)
        {
            var content = j["message"]?["content"];
            string? text = content switch
            {
                JsonValue v when v.TryGetValue<string>(out var str) => str,
                JsonArray a when a.Count > 0 && a[0]?["type"]?.GetValue<string>() == "text" => a[0]!["text"]?.GetValue<string>(),
                _ => null,
            };
            if (text is not null && !botPrefixes.Any(p => text.TrimStart().StartsWith(p, StringComparison.Ordinal)))
            {
                s.Prompts.Add(ts);
            }
            prevReq = null;
        }
        else prevReq = null;
        prevTs = ts;
    }
}

static class H
{
    public static long Sum(IEnumerable<long> x) => x.Aggregate(0L, (a, b) => a + b);

    public static List<(DateTimeOffset Start, DateTimeOffset End)> Blocks(IEnumerable<DateTimeOffset> times, TimeSpan gap)
    {
        var list = times.OrderBy(t => t).ToList();
        var blocks = new List<(DateTimeOffset, DateTimeOffset)>();
        if (list.Count == 0) return blocks;
        var start = list[0];
        var end = list[0];
        foreach (var t in list.Skip(1))
        {
            if (t - end > gap) { blocks.Add((start, end)); start = t; }
            end = t;
        }
        blocks.Add((start, end));
        return blocks;
    }

    public static string D(DateTimeOffset t) => t.ToUniversalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    public static string Iso(DateTimeOffset t) => t.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
    public static double R(double v, int d = 1) => Math.Round(v, d);
}

sealed class Call
{
    public string Model = "";
    public bool IsMain;
    public SubAgg? Sub;
    public DateTimeOffset? Start;
    public DateTimeOffset End;
    public long In, CacheW, CacheR, Out;
    public int Tools;
}

sealed class SubAgg
{
    public string Type = "";
    public string? Model;
}

sealed class DayAgg
{
    public List<DateTimeOffset> Times = [];
    public int Prompts;
    public Dictionary<string, long[]> Models = [];
    public int Spawns;
    public double ReviewMin, AwayMin;

    public JsonObject ToJson(string day, TimeSpan gap)
    {
        var blocks = Blocks(Times, gap);
        var models = new JsonObject();
        foreach (var (m, v) in Models) models[m] = new JsonObject { ["calls"] = v[4], ["in"] = v[0], ["cacheW"] = v[1], ["cacheR"] = v[2], ["out"] = v[3] };
        return new JsonObject
        {
            ["day"] = day,
            ["activeMin"] = R(blocks.Sum(b => (b.End - b.Start).TotalMinutes)),
            ["firstUtc"] = Times.Count > 0 ? Iso(Times.Min()) : null,
            ["lastUtc"] = Times.Count > 0 ? Iso(Times.Max()) : null,
            ["prompts"] = Prompts,
            ["spawns"] = Spawns,
            ["reviewMin"] = R(ReviewMin),
            ["awayMin"] = R(AwayMin),
            ["models"] = models,
        };
    }
}

sealed class SessAgg
{
    public string Id = "";
    public string? Title;
    public List<DateTimeOffset> AllTimes = [];
    public List<DateTimeOffset> MainTimes = [];
    public List<DateTimeOffset> MainAssistantTimes = [];
    public List<DateTimeOffset> Prompts = [];
    public Dictionary<string, Call> Calls = [];
    public List<SubAgg> Subs = [];

    public JsonObject ToJson(TimeSpan activityGap, TimeSpan awayGap, SortedDictionary<string, DayAgg> days)
    {
        var start = AllTimes.Min();
        var end = AllTimes.Max();
        var blocks = Blocks(AllTimes, activityGap);
        var activeMin = blocks.Sum(b => (b.End - b.Start).TotalMinutes);

        // Maintainer latency: the gap between Claude's last assistant message on the main thread
        // before a human prompt and that prompt, split at awayGap into "review" (still at the
        // keyboard) and "away".
        var mains = MainAssistantTimes.OrderBy(t => t).ToList();
        double reviewMin = 0, awayMin = 0;
        var awayCount = 0;
        var gaps = new List<double>();
        foreach (var p in Prompts.OrderBy(t => t))
        {
            var idx = mains.BinarySearch(p);
            if (idx < 0) idx = ~idx;
            if (idx == 0) continue;
            var gap = p - mains[idx - 1];
            gaps.Add(gap.TotalMinutes);
            if (gap <= awayGap) { reviewMin += gap.TotalMinutes; Day(days, p).ReviewMin += gap.TotalMinutes; }
            else { awayMin += gap.TotalMinutes; awayCount++; Day(days, p).AwayMin += gap.TotalMinutes; }
        }
        gaps.Sort();

        var models = new JsonObject();
        foreach (var g in Calls.Values.GroupBy(c => c.Model))
        {
            var rates = g.Where(c => c.IsMain && c.Start is not null && c.Out >= 200)
                .Select(c => c.Out / Math.Max(1, (c.End - c.Start!.Value).TotalSeconds)).OrderBy(x => x).ToList();
            models[g.Key] = new JsonObject
            {
                ["calls"] = g.Count(),
                ["in"] = Sum(g.Select(c => c.In)),
                ["cacheW"] = Sum(g.Select(c => c.CacheW)),
                ["cacheR"] = Sum(g.Select(c => c.CacheR)),
                ["out"] = Sum(g.Select(c => c.Out)),
                ["mainOut"] = Sum(g.Where(c => c.IsMain).Select(c => c.Out)),
                ["medianOutTokPerSec"] = rates.Count > 0 ? R(rates[rates.Count / 2]) : null,
            };
        }

        var agents = new JsonObject();
        foreach (var g in Subs.GroupBy(x => x.Type))
        {
            var calls = Calls.Values.Where(c => c.Sub is not null && c.Sub.Type == g.Key).ToList();
            agents[g.Key] = new JsonObject
            {
                ["spawns"] = g.Count(),
                ["models"] = string.Join(",", g.Select(x => x.Model ?? "inherit").Distinct()),
                ["out"] = Sum(calls.Select(c => c.Out)),
                ["total"] = Sum(calls.Select(c => c.In + c.CacheW + c.CacheR + c.Out)),
                ["tools"] = calls.Sum(c => c.Tools),
            };
        }

        foreach (var t in AllTimes) { var d = Day(days, t); d.Times.Add(t); }
        foreach (var p in Prompts) Day(days, p).Prompts++;
        foreach (var c in Calls.Values)
        {
            var d = Day(days, c.End);
            if (!d.Models.TryGetValue(c.Model, out var v)) d.Models[c.Model] = v = new long[5];
            v[0] += c.In; v[1] += c.CacheW; v[2] += c.CacheR; v[3] += c.Out; v[4]++;
        }
        foreach (var sub in Subs)
        {
            var first = Calls.Values.Where(c => c.Sub == sub).Select(c => c.End).DefaultIfEmpty(start).Min();
            Day(days, first).Spawns++;
        }

        return new JsonObject
        {
            ["id"] = Id,
            ["title"] = Title,
            ["startUtc"] = Iso(start),
            ["endUtc"] = Iso(end),
            ["spanMin"] = R((end - start).TotalMinutes),
            ["activeMin"] = R(activeMin),
            ["blocks"] = blocks.Count,
            ["prompts"] = Prompts.Count,
            ["reviewMin"] = R(reviewMin),
            ["awayMin"] = R(awayMin),
            ["awayCount"] = awayCount,
            ["medianReplyMin"] = gaps.Count > 0 ? R(gaps[gaps.Count / 2]) : null,
            ["calls"] = Calls.Count,
            ["tools"] = Calls.Values.Sum(c => c.Tools),
            ["spawns"] = Subs.Count,
            ["models"] = models,
            ["agents"] = agents,
        };
    }

    static DayAgg Day(SortedDictionary<string, DayAgg> days, DateTimeOffset t)
    {
        var k = D(t);
        if (!days.TryGetValue(k, out var d)) days[k] = d = new DayAgg();
        return d;
    }
}
