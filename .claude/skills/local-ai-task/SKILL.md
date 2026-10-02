---
name: local-ai-task
description: Delegate a bounded, low-risk "read then produce an artifact" task to the maintainer's free local model (llama-server) through tools/ai/local-ai-ask.cs, with a tight brief, a ledger entry and a review of the output. Use for drafts, classifications, summaries and mechanical conversions that would otherwise spend paid tokens.
---

# Local AI task

Routing rules live in `docs/engineering/ai-task-routing.md`. This skill is the procedure for one delegated task.

## When it fits

- Good fits: classifying or labelling issues, summarising closed issues, drafting a script by copying and adapting an existing one, converting a table, first drafts of mechanical plan sections.
- Bad fits: design decisions, security-sensitive code, anything whose correctness cannot be checked by reading the output, and tasks that must edit many files and run builds. Agentic coding goes through opencode (`.opencode/agents/`), not this script.

## 1. Check the switch

The maintainer can order the local model off (hardware temperature, for example); the main session records that with `tools/ai/local-ai-state.ps1 -Off -Reason <text>` (only the main session runs `-Off`/`-On`). Read it first:

```powershell
pwsh -NoProfile -File tools/ai/local-ai-state.ps1 -Status
```

- `ON`: the server is llama-server at `http://127.0.0.1:8080`, started by the maintainer. Follow the procedure below as written. If it is down, say so and do the task another way; do not start it yourself.
- `OFF: <reason>`: the procedure below stays the same, because `tools/ai/local-ai-ask.cs` reads the switch first and drafts through the stand-in by itself (the Claude Code CLI on haiku at low effort, with the instructions of `.claude/agents/local-ai-standin.md`, no health retries). The draft costs paid tokens: the call is recorded in `artifacts/local-ai/standin-ledger.csv` (the `ledger.csv` columns plus `costUsd` and `totalTokens`), never in `ledger.csv`. The CLI runs with no tool and no MCP server (`--strict-mcp-config`); the script checks the init event and fails closed (exit 1) otherwise, kills a hung CLI after `--standin-timeout-seconds` (default 600), maps `--max-tokens` to the CLI and always uses the subscription login (API keys are removed from the child). Review it exactly as in section 4. Exit code 4 means the CLI failed on a login or usage-limit problem: fix that or wait, or spawn the agent as below; batch scripts stop on 3 or 4. Exit code 3 means the switch is off and the CLI is not available: spawn the `local-ai-standin` agent (foreground, through the Agent tool; the main session, `issue-worker` and `docs-writer` may) with the same task name, brief, inputs and out path, then record that spawn with `pwsh -NoProfile -File tools/ai/local-ai-state.ps1 -RecordStandin -Task <name> -OutFile <out> -Tokens <subagent_tokens> -Seconds <duration>`.

## 2. Write the brief

Save it under `artifacts/local-ai/briefs/<task>.md`. A brief that worked in earlier runs has:

1. One task, one output file. Split anything larger.
2. The exact output format: file type, sections, and a short example of the expected shape.
3. Enumerated points to implement, each testable by reading the output.
4. The inputs named, passed with `--input` rather than pasted into the brief.
5. The project constraints that apply, such as the C# or PowerShell only policy, English in code, and no AI attribution.
6. The words "Output only the file content. Stop when the file is written."

## 3. Run it

```powershell
dotnet run tools/ai/local-ai-ask.cs -- --task <name> --brief artifacts/local-ai/briefs/<name>.md --input <file> [--input <file>...] --out artifacts/local-ai/out/<name>.<ext>
```

The script disables thinking per request and appends a line to `artifacts/local-ai/ledger.csv` with the prompt tokens, completion tokens, seconds and throughput. It exits non-zero on any failure (1 for server or HTTP errors; an unhandled exit code for a missing or unreadable brief). It retries the health check for about two minutes (`--health-attempts`/`--health-wait-seconds`, default 6 tries 20s apart) before reporting the server down, so do not conclude "model down" from a single failure earlier than that. With the switch off it skips the health check altogether (section 1).

## 4. Review before use

The output is a draft. Read all of it and compile or run it. Typical defects in earlier runs were:

- quoted error strings that do not interpolate,
- duplicated variable names,
- a regex that misses a case the brief named.

Fix these in place; re-prompt only when the draft is structurally wrong.

## 5. Record

State in the PR or document that the artifact was drafted locally and reviewed, with the ledger's token counts. SPEC-001 §10 "Routing" is the reference wording.

When the artifact is a `gh issue create --body-file` body, the `check-issue-template` hook enforces this at the point of creation (#1410): keep the local model's output path as the file's first line, `<!-- local-draft: <path to the --out file> -->` (the ledger row `local-ai-ask.cs` just appended must be less than 24 hours old and name that same path as `outFile`), or pass the drafted file straight through as `--body-file` unchanged so it IS the ledger's `outFile`. When the task is a bad fit for the local model (see "When it fits" above) and you draft the body yourself, use a first line `<!-- local-draft: none, reason: <text> -->` instead — a non-empty reason, logged to `artifacts/local-ai/opt-outs.log`. A worker's own worktree ledger counts too: the hook also checks every `.claude/worktrees/*/artifacts/local-ai/ledger.csv`. Stand-in drafts count the same through `artifacts/local-ai/standin-ledger.csv` (main checkout and every worktree, 24 hours, same pointer rules); the opt-out marker stays for bad fits.
