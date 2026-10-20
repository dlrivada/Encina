# Delta scope of issue #27 (set rules-2026-10)

Reused from the original audit; do not re-derive it. Rules in this delta: see tools/ai/audit/pipeline-delta.json.

**Scope source:** the published record and the published stage files of the original audit (docs/knowledge/audits/27/stages/).

## Knowledge record (docs/knowledge/issues/27.md)

```yaml
schema: 1
nav_exclude: true
issue: 27
title: "[FEATURE] Source Generators for zero-reflection dispatch"
closed: 2025-12-24
state_reason: completed
outcome: duplicate
duplicate_of: 50
type: feature
area: core
review: verified
packages:
prs:
linked_prs:
knowledge:
  - kind: pending-work
    statement: "Compile-time handler registration, switch-based dispatch and NativeAOT compatibility through a Roslyn source generator remain unimplemented; the work is tracked by the open issue #50 (generator) and #51 (switch dispatch), created the same day after #27 was reverted."
    current: yes
    sources:
      - "quote: \"Reverted - issue created in error\" (issue #27 comment, 2025-12-24T11:52:32Z)"
      - "paraphrase: #50 was created 2025-12-24T13:28:08Z with the same scope (Roslyn generator, zero-reflection switch dispatch, NativeAOT) and is OPEN today (gh issue view 50, 2026-10-03)"
    destinations:
      - kind: backlog
        status: planned
        target: "#50"
audit:
  checklist: 1
  date: 2026-10-03
  verdict: code-removed
  record: "docs/knowledge/audits/issue-27.md"
remediation:
```

## From the original archivist.md (docs/knowledge/audits/27/stages/archivist.md)

- None. The issue delivered no code: closed 20 minutes 4 seconds after creation (created 2025-12-24T11:32:28Z, closed 11:52:32Z) with "Reverted - issue created in error". Its only timeline commit, `2b50a1ec` (docs restructure), touched `.claude/CLAUDE.md`, `ROADMAP.md` and `docs/history/2025-12.md`, not source.
- Verified absent today: there is no Roslyn source-generator project: `src/Encina.SourceGenerators` does not exist, no directory under `src/` or `tests/` is named `*SourceGenerator*`, and no `.cs`/`.csproj` file in `src/` or `tests/` contains `IIncrementalGenerator`, `ISourceGenerator`, `[Generator]`, `Encina.SourceGenerators` or `EncinaHandler`. (Directories named `Generators` exist, e.g. `src/Encina.IdGeneration/Generators`, but they are ID generators, unrelated.)
- The code stage has no `src/` surface for this issue. The affected-package text in the issue (`Encina` dispatch, `Encina.SourceGenerators`) was intent only. Nothing was removed on purpose.

## From the original code.md (docs/knowledge/audits/27/stages/code.md)

- `artifacts\knowledge\stages\archivist.md` and `artifacts\knowledge\issues\27.md` read. The archivist's scope is empty: the issue delivered no code. No scope correction.
- Empty diff confirmed. Issue #27 was closed in error 20 minutes after creation. Its only timeline commit, `2b50a1ec`, touched `.claude/CLAUDE.md`, `ROADMAP.md` and `docs/history/2025-12.md` (`git show --stat 2b50a1ec`), no `src/` or `tests/` file.
- Proposed types confirmed absent in the current worktree: no `.cs` or `.csproj` under `src/` or `tests/` matches `EncinaHandler`, `IncrementalGenerator` (so no `IIncrementalGenerator`), `ISourceGenerator` or `[Generator]` (Select-String over `src` and `tests`, `*.cs` and `*.csproj`), and no directory named `*SourceGenerator*` exists, so no `Encina.SourceGenerators` (or `Encina.SourceGenerator`) project exists.
- `Microsoft.CodeAnalysis.CSharp` does match, in five `src/` files, none of them a source generator: `src/Encina.Security.ABAC/EEL/EELCompiler.cs:7` (`using Microsoft.CodeAnalysis.CSharp.Scripting;`, a scripting reference), `src/Encina.Security.ABAC/Encina.Security.ABAC.csproj:19` (the `...CSharp.Scripting` package), `src/Encina.Security.ABAC.Analyzers/Encina.Security.ABAC.Analyzers.csproj:23-24` (`...CSharp` and `...CSharp.Workspaces` packages, Roslyn analyzers), `src/Encina.Security.ABAC.Analyzers/RequireConditionAnalyzer.cs:7-8` and `src/Encina.Security.ABAC.Analyzers/RequireConditionCodeFixProvider.cs:10-11` (an analyzer and its code fix). They are unrelated to #27's proposed `[EncinaHandler]` dispatch generator.
- The successor work (#50 generator, #51 switch dispatch, both OPEN) is audited when it is delivered and closed; not walked here.


