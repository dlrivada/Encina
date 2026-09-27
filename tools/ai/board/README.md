# Control board statistics (#1382)

Two scripts produce the data behind the maintainer's private control board (a claude.ai
artifact) "Week" tab: what was done since a given date, broken down by session and by day (time,
prompts, reply latency, tokens per model, agents, PRs and issues, the local model). Neither script
publishes anything; the orchestrator uploads the resulting files to the board.

## `session-stats.cs`

A C# file-based app that reads Claude Code transcripts under `~/.claude/projects/<project>/*.jsonl`
and, for each session, its subagent transcripts under `<session>/subagents/*.jsonl` (paired with
`.meta.json` files naming the agent type and model). For every session and every day since a given
timestamp it computes active time (events merged across a 5-minute gap), human prompts, reply
latency (split at 30 minutes into "review" and "away"), tokens per model (de-duplicated by
`requestId`), and agent spawns and token totals by agent type.

```powershell
dotnet run --file tools/ai/board/session-stats.cs -- <projectDir> <sinceIsoUtc> <outJson>
```

Writes a single JSON object (`{ generatedUtc, since, sessions: [...], days: [...] }`) to
`<outJson>`.

## `build-board-stats.ps1`

Runs `session-stats.cs`, collects GitHub activity (`gh pr list` / `gh issue list`, created or
closed since `-Since`) and every `artifacts/local-ai/ledger.csv` under the main checkout and its
`.claude/worktrees/*`, and merges them into the three files the board reads:

- `db-summary.json` — totals for the window (active/review/away minutes, prompts, spawns, PRs and
  issues, local-model calls) plus tokens by model and agents by total tokens.
- `db-days.json` — one row per day (tokens by model, PRs/issues opened or closed that day, local
  model calls and tokens/second).
- `db-sessions.json` — one row per session (id, title, assessment, timing, tokens by model, top
  agents by total tokens).

```powershell
pwsh -File tools/ai/board/build-board-stats.ps1 -Since 2026-09-21
```

Parameters:

| Parameter | Required | Default | Meaning |
| --- | --- | --- | --- |
| `-Since` | yes | — | ISO date (`yyyy-MM-dd`) the report starts from. |
| `-OutDir` | no | `artifacts/board` under the repository root | Where every output file (including the intermediate `session-stats.json`, `github.json`, `localai.json`) is written. |
| `-ProjectDir` | no | the Claude Code project folder derived from the main checkout's path (e.g. `D:\Proyectos\Encina` -> `~/.claude/projects/D--Proyectos-Encina`) | The transcript folder `session-stats.cs` reads. |
| `-SessionMeta` | no | none (sessions carry no title or assessment) | Path to a **private** JSON file of `{ "<8-char session id prefix>": { "title": "...", "assessment": "..." } }`. Holds the maintainer's own assessment of each session; never commit its content. |
| `-Plan` | no | none (`db-summary.json`'s `"plan"` field is `null`) | Path to a plan-usage JSON file, copied verbatim into `db-summary.json`'s `"plan"` field. |

`-SessionMeta` and `-Plan` are both optional and both private: keep the files themselves under
`artifacts/` (git-ignored), never under version control.
