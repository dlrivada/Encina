---
name: local-ai-task
description: Delegate a bounded, low-risk "read then produce an artifact" task to the maintainer's free local model (llama-server) through tools/ai/local-ai-ask.cs, with a tight brief, a ledger entry and a review of the output. Use for drafts, classifications, summaries and mechanical conversions that would otherwise spend paid tokens.
---

# Local AI task

Routing rules live in `docs/engineering/ai-task-routing.md`. This skill is the procedure for one delegated task.

## When it fits

- Good fits: classifying or labelling issues, summarising closed issues, drafting a script by copying and adapting an existing one, converting a table, first drafts of mechanical plan sections.
- Bad fits: design decisions, security-sensitive code, anything whose correctness cannot be checked by reading the output, and tasks that must edit many files and run builds. Agentic coding goes through opencode (`.opencode/agents/`), not this script.

## 1. Check the server

The server is llama-server at `http://127.0.0.1:8080` and is started by the maintainer.

```powershell
Invoke-RestMethod http://127.0.0.1:8080/health
```

If it is down, say so and do the task another way; do not start it yourself.

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

The script disables thinking per request and appends a line to `artifacts/local-ai/ledger.csv` with the prompt tokens, completion tokens, seconds and throughput. It exits 1 on any failure.

## 4. Review before use

The output is a draft. Read all of it and compile or run it. Typical defects in earlier runs were:

- quoted error strings that do not interpolate,
- duplicated variable names,
- a regex that misses a case the brief named.

Fix these in place; re-prompt only when the draft is structurally wrong.

## 5. Record

State in the PR or document that the artifact was drafted locally and reviewed, with the ledger's token counts. SPEC-001 §10 "Routing" is the reference wording.
