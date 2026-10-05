# Control board tooling (#1382, #1732)

This directory holds the tooling behind the maintainer's private control board (a claude.ai
artifact). Two scripts produce the data of the "Week" tab (#1382): what was done since a given
date, broken down by session and by day (time, prompts, reply latency, tokens per model, agents,
PRs and issues, the local model). The board reconciler (#1732, last section) keeps the board's work,
flow and audit collections current. None of the scripts publishes anything; the orchestrator
uploads the resulting files to the board.

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

## Board reconciler (#1732)

`reconcile-board.ps1` keeps the board's db collections `work/<id>`, `flow/<issue>`, `audits/<n>` and
`meta/board` current from GitHub (open PRs, PRs merged in the last 14 days with `Fixes #n`, closed
issues), the git worktrees (commits ahead of `origin/main`) and, in the **main** checkout,
`artifacts/knowledge/current-audit.json` and `progress.csv`. It is deterministic (no model), only
reads and emits `ArtifactData` batch writes (it never applies them), never deletes a document and
never touches `dash/*`, `stats/*`, `gates/*` or `prio/*`.

```powershell
pwsh -NoProfile -File tools/ai/board/reconcile-board.ps1 -CurrentDir <tmp> -Versions <tmp>/versions.json -Out <tmp>/batch.json
```

It needs PowerShell 7.5+ (`ConvertFrom-Json -DateKind`).

| Parameter | Required | Default | Meaning |
| --- | --- | --- | --- |
| `-CurrentDir` | yes | — | The folder an `ArtifactData list ... out_dir` call produced: `work/*.json`, `flow/*.json`, `audits/*.json`, `meta/board.json`. Each file holds a document's data only; the file name is the `doc_id`. |
| `-Versions` | yes | — | The versions sidecar (see below). |
| `-Out` | yes | — | The batch file to write. |
| `-Repo` | no | `dlrivada/Encina` | The GitHub repository queried through `gh`. |
| `-MainRoot` | no | derived from `git rev-parse --git-common-dir` | The main checkout, where the audit progress files are read. |
| `-NowUtc` | no | the current UTC time | The reference time (tests pass a fixed value). |
| `-CreateOp` | no | `set` | The batch operation name for a new document. |
| `-UpdateOp` | no | `update` | The batch operation name for an existing document. |
| `-MergedDays` | no | `14` | How far back merged PRs and closed issues are read. |
| `-MaxWrites` | no | `50` | Writes per batch file. |

The output is a JSON array of `{ op, collection, doc_id, if_version?, data }`, only for documents
whose data differ. At most 50 writes go in one file: when there are more, `-Out` becomes
`<name>-001.json`, `<name>-002.json` and so on. Stdout prints `WRITES <n> SKIPPED <m>` followed by the
file paths, one per line. ArtifactData batch ops are `set` (replace/create), `update` (merges fields
into an existing document) and `delete` (never used); because `update` merges, the reconciler sends
only the changed top-level fields for existing documents and the full document (`set`) for new ones.

### The versions sidecar

`ArtifactData list` with `out_dir` writes only each document's data; the document's version appears
only in the tool result text (for example `1698 ... version 8`). The session that runs the
reconciler writes `<dir>/versions.json` from those results before running it:

```json
{ "work/1698": 8, "flow/1698": 3, "meta/board": 2, "audits/29": 5 }
```

Every write to an existing document is pinned with `if_version`. An existing document missing from
the sidecar is skipped with a `WARN` on stderr and never written unpinned (a write without a version
could overwrite a concurrent manual edit); the run still exits 0 so the other writes can be applied.
New documents carry no version.

### Rules

| Situation | Result |
| --- | --- |
| Card or flow front with a merged PR | Card `merged` (flow `merged` + stage `done`), with the merge time. |
| Open PR | Card `pr-open`; flow `in-progress` + stage `review`. |
| Draft open PR | A running card stays `running` and flow is left alone; only the PR number is recorded. |
| a `queued` or `running` card with a worktree ahead of main and no PR | Card `running`. |
| `running` worker card with no worktree and no PR | Card `stopped`, with a note. Only when its startedUtc is more than 2 hours old; a freshly spawned worker may not have a worktree yet. |
| PR closed without merging | Card `stopped`, with a note. |
| Card that groups several issues | Finished only by a PR that closes all of them. |
| PR older than the window | Resolved with `gh pr view`. |
| Flow front whose issue closed with no PR | Flow `closed`, stage `close-out`. |
| Open, non-bot PR that closes an issue and has no card | New card (id = the issue number). |
| `progress.csv` row `done` | Audit `closed` / `done`. |
| Audit missing from the board | Created. |
| Audit in `current-audit.json` | Audit `open`. |
| `meta/board` status | Regenerated: open audit, open PRs, PRs merged in the last 48 hours (capped at 12), card counts. Hand-written text after ` Notes: ` is preserved. On the first run an existing hand-written status that is not a generated one moves, whole, behind ` Notes: ` automatically. |

The reconciler is idempotent: a second run on a reconciled board writes nothing.

### Scheduled task

A Claude session that the orchestrator creates after merge runs this every 30 minutes:

1. `ArtifactData list` for `work`, `flow`, `audits` and `meta` with `out_dir` set to `<tmp>/work`, `<tmp>/flow`, `<tmp>/audits` and `<tmp>/meta`.
2. Write `<tmp>/versions.json` from the versions in those list results.
3. Run `pwsh -NoProfile -File tools/ai/board/reconcile-board.ps1 -CurrentDir <tmp> -Versions <tmp>/versions.json -Out <tmp>/batch.json`.
4. Apply each printed batch file with `ArtifactData batch`. The batch call takes inline writes, so the session reads the file and passes its array as the `writes` argument, one call per file.
5. Report the `WRITES`/`SKIPPED` line. If `SKIPPED` is above 0, list again and retry once.

### Event reminder hook

`.claude/hooks/board-event-reminder.ps1` is a `PostToolUse` hook (matcher `Bash|PowerShell|Agent|Task`
in `.claude/settings.json`). It fires after `gh pr create`, `gh pr merge`, `audit-done.ps1`,
`audit-commit-stage.ps1` and after an `issue-worker` or `docs-writer` spawn, and adds the context
`Board: update work/flow/audits for <event> now (or let the 30-minute reconciler do it)`. It never
blocks. Its tests are in `.claude/hooks/tests/Test-Hooks.ps1`.

### Self-test

```powershell
pwsh -NoProfile -File tools/ai/board/reconcile-board-selftest.ps1
```

Runs the reconciler against fixtures with stubbed `gh` and `git`; exits 1 on any failure.
