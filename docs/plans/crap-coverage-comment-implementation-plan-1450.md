# Implementation Plan: CRAP table in the in-repository per-flag coverage comment (the Codecov fallback)

> **Issue**: [#1450](https://github.com/dlrivada/Encina/issues/1450) - [FEATURE] Own per-flag coverage and CRAP summary comment on PRs as the Codecov fallback
> **Status**: Draft plan (written against `origin/main` 4b0a600c7, 2026-11-25)
> **Epic**: [#1453](https://github.com/dlrivada/Encina/issues/1453) (Fallback system for external review bots)
> **Scope note**: repository tooling and workflows only. No `src/` code, no .NET package, no EventIds.

---

## Summary

**Most of this issue is already on `main`.** #3289 (closed) and ADR-063 replaced Codecov with an in-repository comment: `.github/workflows/coverage-comment.yml` posts one sticky comment per pull request (hidden marker `<!-- encina-coverage-comment -->`, updated in place) with the "Per-file targets" table of the `src/` files the pull request touches, rendered by `.github/scripts/coverage-report.cs --files-from`. Codecov is gone, so the issue's trigger ("only when Codecov is detected as unavailable", the `steps.bot-status` output) no longer exists, and the maintainer comment of 2026-10-17 already settled "one sticky comment updated in place on every run" instead of that trigger.

**What is still missing, and what this plan implements: the CRAP table.** The comment shows per-flag attainment but not the CRAP score of the methods the pull request changes. `crap-gate` (job in `ci.yml`, `.github/scripts/crap-gate.cs`) computes it and only prints text to the job log. The plan adds a Markdown output to `crap-gate.cs` and a section "CRAP of the changed methods" to the same sticky comment, computed by base-repository code under the ADR-063 trust boundary.

**Estimated scope.** 2 modified scripts, 1 new fixture, 2 workflow edits (orchestrator), 1 doc, 1 fragment; about 350 changed lines, in two pull requests.

**Affected packages.** None. Paths: `.github/scripts/crap-gate.cs`, `.github/scripts/crap-gate-selftest.ps1`, `.github/scripts/testdata/crap-gate/`, `.github/workflows/ci.yml`, `.github/workflows/coverage-comment.yml`.

**Provider category.** None.

**Shared hot spots.** `.github/workflows/*` belongs to the orchestrator: this plan specifies the edits, the orchestrator applies them (marked below).

---

## Design Choices

<details>
<summary><strong>1. Scope: add the CRAP section to the existing comment, not a second implementation</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Extend the existing sticky comment with a CRAP section** | One comment, one marker, no duplicate of the per-flag table | Touches a `workflow_run` workflow |
| **B) Build the fallback workflow the issue sketches (Codecov-gated)** | Matches the old text | Codecov no longer exists; duplicates ADR-063 |
| **C) Close the issue as done** | No work | The CRAP half of the acceptance criteria is not met |

### Chosen Option: **A - Extend the existing comment**

### Rationale

- The per-flag half is shipped (ADR-063, #3289 closed). Acceptance criteria 2 and 3 of the issue (per-flag results, CRAP table in one readable block) are the remaining delta.

**Class:** Settled
**Settled by:** ADR-063 (Decision: one informational comment per pull request); maintainer comment on issue #1450 (2026-10-17: one sticky comment, updated in place)

</details>

<details>
<summary><strong>2. The comment stays informational and never gates</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Informational; `crap-gate` keeps blocking in `ci.yml`** | Keeps the trust boundary; no second gate | Reviewers read two places |
| **B) The comment workflow fails the PR** | One place | A `workflow_run` result is not a required check and its data is untrusted |

### Chosen Option: **A - Informational**

### Rationale

- ADR-063 Consequences: the comment is never a gate; `crap-gate` stays the blocking job.

**Class:** Settled
**Settled by:** ADR-063 section "Consequences" (first bullet)

</details>

<details>
<summary><strong>3. Where the CRAP is computed: base-repository code with head sources as data</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Re-run `crap-gate.cs` of the base repository in `coverage-comment.yml`; the head's touched `.cs` files are read as git blobs (data) so `// crap-exempt:` comments resolve** | All rendering by trusted code; same trust boundary as the manifests step | A little extra work in the workflow |
| **B) The PR's own `crap-gate` job renders the Markdown and uploads it** | No re-run | The Markdown is produced by pull-request code (`crap-gate.cs` of the head), so a fork could post arbitrary text under the bot's name |
| **C) Skip the exemption lookup** | Simplest | The comment would list an exempt method as a violation while the gate passes |

### Chosen Option: **A - Recompute with base code**

### Rationale

- `crap-gate.cs` `TryGetExemption` reads the source file from disk (`File.ReadAllLines(m.File)`); in `workflow_run` the base checkout has no head sources, so they are written under a scratch folder from `git show <head>:<path>` and the script runs with that folder as working directory, never executing them.

**Class:** Settled
**Settled by:** ADR-063 Decision (artifacts are untrusted data; pull-request code is never checked out or run; manifests are read as blobs with `git show`)

</details>

<details>
<summary><strong>4. Where the `-U0` diff comes from</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The `crap-gate` job uploads the `diff.patch` it already builds (`git diff -U0 base...head`) as an artifact; the comment workflow downloads it as untrusted data** | Exactly the diff the gate used; no merge-base work | One more `ci.yml` step; no CRAP section when the job did not run |
| **B) The comment workflow rebuilds it (`git fetch` of base and head, merge-base, `git diff -U0`)** | Independent of the `ci.yml` artifact | Shallow-fetch and merge-base failure modes; the REST diff has 3 context lines and would over-attribute changed lines |

### Chosen Option: **A - Upload the gate's diff (recommended, pending the maintainer)**

### Rationale

- A wins for accuracy and size (the diff parser takes `-U0` hunks); B wins if the maintainer wants no `ci.yml` change. In both cases a PR without `src/` changes gets no CRAP section.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Which methods the table lists</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Every changed method, highest CRAP first, at most 30 rows, plus the count above the threshold** | Reviewers see the headroom of a method that is at 9.5 | Longer comment |
| **B) Only methods above the threshold (violations and exemptions)** | Short, matches the log | A clean PR shows nothing; no headroom |

### Chosen Option: **A - All changed methods, top 30 (recommended, pending the maintainer)**

### Rationale

- A wins when reviewers use the table to judge risk; B wins when comment length matters more. The issue asks for "the complexity table for the changed methods".

**Class:** Dilemma

</details>

<details>
<summary><strong>6. Untrusted names in the table</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Allow-list the characters of file, class and method names, render in code spans, cap the length, drop others** | No injected link, mention or table break | A legal but odd name is shown shortened |
| **B) Render names as they come** | Faithful | Names come from pull-request Cobertura; `@team` mentions, links and `|` break the table |

### Chosen Option: **A - Allow-list and code spans**

### Rationale

- The Cobertura and diff are data from the pull request (ADR-063); the comment is posted by `github-actions[bot]`.

**Class:** Settled
**Settled by:** ADR-063 Consequences (the data comes from the pull request and is only parsed); AGENTS.md section 3 (untrusted input never decides on its own)

</details>

---

## Implementation Phases

### Phase 1: `crap-gate.cs` Markdown output

<details>
<summary><strong>Tasks</strong></summary>

1. **`.github/scripts/crap-gate.cs`** (modify): new option `--markdown <path>` writing a section "### CRAP of the changed methods" (threshold, count of changed methods analysed, count above threshold, table `Method | Complexity | Coverage | CRAP | Status` with `ok`, `VIOLATION` or `EXEMPT: <reason>`), top 30 by CRAP; names pass the allow-list `[A-Za-z0-9_.<>,`+~/\[\]-]` (anything else becomes `?`) and are cut at 120 characters; no-diff or no-method input writes "No changed method has coverage data in this run." The existing console output and exit codes do not change. A new `--source-root <dir>` is NOT added: callers use the working directory, as today.
2. **`.github/scripts/crap-gate-selftest.ps1`** (modify) and **`.github/scripts/testdata/crap-gate/`** (one new expected file `sample.markdown.md`): assert the Markdown for the violating, exempt-only and empty cases and that a hostile method name (`@org/team|](http://x)`) is neutralised.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of #1450 (CRAP Markdown from crap-gate.cs).

CONTEXT:
- .github/scripts/crap-gate.cs is a C# 14 file-based app (no .csproj); its self-test is .github/scripts/crap-gate-selftest.ps1 with fixtures in .github/scripts/testdata/crap-gate/.

TASK:
Add --markdown <path> as described in the plan Phase 1 and extend the self-test.

KEY RULES:
- Existing stdout and exit codes stay byte-identical.
- Names are untrusted: allow-list, code spans, length cap.
- Edit with Edit/Write only.

REFERENCE FILES:
- .github/scripts/crap-gate.cs, .github/scripts/crap-gate-selftest.ps1, docs/engineering/crap-gate-design.md
```

</details>

### Phase 2: Workflows (the orchestrator applies them)

<details>
<summary><strong>Tasks</strong></summary>

1. **`.github/workflows/ci.yml`** (modify, orchestrator): in the `crap-gate` job, a step `actions/upload-artifact` of `artifacts/crap-gate/diff.patch` as `crap-gate-diff` (retention 7 days), `if: always()` after the gate step, only when the file exists.
2. **`.github/workflows/coverage-comment.yml`** (modify, orchestrator), job `report`, after "Render the comment": download `crap-gate-diff` (untrusted data, `continue-on-error`); write the touched `.cs` blobs from `git show $HEAD_SHA:<path>` into `crap-src/` (path regex `^src/[A-Za-z0-9._/-]+\.cs$`); run `dotnet run --file .github/scripts/crap-gate.cs -- --report --markdown crap.md --diff crap-gate-diff/diff.patch <cobertura files>` with `crap-src` as working directory; append `crap.md` to `pr-coverage-comment.md` before the marker and footer are added. A missing artifact, or a failing script, leaves the per-flag comment unchanged and adds the line "CRAP table not available for this run." The `comment` job and the 60000-character cap stay as they are.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of #1450. The ORCHESTRATOR applies .github/workflows edits; produce the exact YAML for it and do not edit the workflow files yourself.

CONTEXT:
- coverage-comment.yml runs from the default branch (workflow_run); pull-request code is never checked out or run; every event value reaches scripts through env.

TASK:
Write the two workflow fragments of the plan Phase 2 and the manual verification steps (a throw-away PR touching one src file).

KEY RULES:
- Permissions stay per job; the comment job keeps the only write permission.
- The artifact is untrusted data; the path allow-list is mandatory.

REFERENCE FILES:
- .github/workflows/coverage-comment.yml, .github/workflows/ci.yml (crap-gate job), docs/architecture/adr/063-in-repo-coverage-comment-replaces-codecov.md
```

</details>

### Phase 3: Testing, documentation and finalization

<details>
<summary><strong>Tasks</strong></summary>

1. Run `./.github/scripts/crap-gate-selftest.ps1` (already a CI step of the `crap-gate` job) and the live check on a throw-away PR after the workflows merge (a `workflow_run` workflow only runs from `main`).
2. **`docs/testing/coverage-measurement-methodology.md`** (modify, by `docs-writer`): the CRAP section says the comment shows the table; no hand-typed figures.
3. **`changelog.d/1450-crap-table-in-coverage-comment.added.md`** (by `mechanical-fixer`): one bullet; optional if tooling is not user-visible.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of #1450 (docs and verification).

CONTEXT:
- Phases 1-2 are merged.

TASK:
Update the CRAP paragraph of docs/testing/coverage-measurement-methodology.md and add the changelog fragment.

KEY RULES:
- Documentation never types coverage figures by hand (SPEC-001).
- English only.

REFERENCE FILES:
- docs/testing/coverage-measurement-methodology.md, docs/engineering/crap-gate-design.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Requirement | Key Details |
|----------|-------------|-------------|
| ADR-063 | In-repository coverage comment replaces Codecov | Trust boundary, sticky comment, informational |
| AGENTS.md section 9 | CRAP <= 10 on changed methods | `crap-gate` stays the blocking gate |
| AGENTS.md section 10 | `permissions:` per job | Unchanged |
| ADR-054 D4 / ADR-062 | Telemetry tags and error categories | Not applicable: no telemetry, no `EncinaError` |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage |
|-----------|----------|-------|
| In-repo comment | `.github/workflows/coverage-comment.yml` | Gets the new section |
| Renderer | `.github/scripts/coverage-report.cs` (`RenderPullRequestComment`) | Per-flag part, unchanged |
| Gate | `.github/scripts/crap-gate.cs`, `crap-gate-selftest.ps1`, `testdata/crap-gate/` | New `--markdown` |
| Diff | `ci.yml` job `crap-gate` (`artifacts/crap-gate/diff.patch`) | Uploaded as an artifact |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| none | none | Scripts and workflows: no `[LoggerMessage]`; no block claimed |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| PR 1 | 3 | `crap-gate.cs`, `crap-gate-selftest.ps1`, `sample.markdown.md` fixture |
| PR 2 | 4 | `ci.yml`, `coverage-comment.yml` (both applied by the orchestrator), methodology doc, fragment |
| Coverage flags | 0 | Manifests cover `src/` only; numeric target of the self-test: at least 6 new assertions (violating, exempt-only, empty, hostile name, cap of 30, unchanged stdout) |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT:
- Encina issue #1450. The in-repo per-flag comment exists (ADR-063, #3289). Add a CRAP section computed by the base-repository crap-gate.cs from the PR's diff artifact, Cobertura artifacts and head source blobs.

IMPLEMENTATION OVERVIEW:
Phase 1 --markdown in crap-gate.cs + self-test; Phase 2 workflow fragments (orchestrator applies); Phase 3 docs, fragment, live check.

KEY PATTERNS:
- Untrusted data: allow-list names, never run PR code, informational only.
- Diff source and row scope are Dilemmas (recommended: upload the gate's diff; all changed methods, top 30), pending the maintainer.

REFERENCE FILES:
- .github/workflows/coverage-comment.yml, .github/scripts/crap-gate.cs, docs/architecture/adr/063-in-repo-coverage-comment-replaces-codecov.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Not applicable: CI tooling |
| 2 | OpenTelemetry | ❌ | Not applicable: no .NET runtime component |
| 3 | Structured Logging | ❌ | Not applicable: workflow log lines only |
| 4 | Health Checks | ❌ | Not applicable: not a service |
| 5 | Validation | ✅ | Phase 2: path allow-list and numeric PR number checks; Phase 1: name allow-list |
| 6 | Resilience | ✅ | Phase 2: a missing artifact or failing script degrades to the per-flag comment |
| 7 | Distributed Locks | ❌ | Not applicable: the workflow's `concurrency` group already serialises a branch |
| 8 | Transactions | ❌ | Not applicable |
| 9 | Idempotency | ✅ | Phase 2: the sticky marker updates one comment; a re-run yields the same body |
| 10 | Multi-Tenancy | ❌ | Not applicable: single repository |
| 11 | Module Isolation | ❌ | Not applicable |
| 12 | Audit Trail | ❌ | Not applicable: the comment is informational; the gates are the record |

---

## Next Steps

1. Review and approve this plan; two Dilemmas (diff source, row scope) are answered when work on #1450 starts.
2. Update the body of #1450 to the reduced scope (the Codecov trigger and `steps.bot-status` no longer exist), by the orchestrator.
