# Implementation Plan: `issue-enricher` (related issues, duplicates and affected files for new issues; the CodeRabbit enrichment fallback)

> **Issue**: [#1449](https://github.com/dlrivada/Encina/issues/1449) - [FEATURE] issue-enricher agent: related issues, duplicates and affected files for new issues (CodeRabbit enrichment fallback)
> **Status**: Draft plan (written against `origin/main` 4b0a600c7, 2026-11-25)
> **Epic**: [#1453](https://github.com/dlrivada/Encina/issues/1453) (Fallback system for external review bots); piece 3, independent of piece 2 (#1448, see choice 6)
> **Scope note**: repository tooling only. No `src/` code, no EventIds.

---

## Summary

**What it implements.** A C# 14 file-based app `tools/ai/issues/issue-enricher.cs`. Given an issue number it (1) reads the issue and its comments over REST, (2) shortlists related and possible duplicate issues from a local index of all issues, (3) maps the issue to likely files and `Encina.*` packages from what the text names and the repository tree, (4) asks the local model (through `tools/ai/local-ai-ask.cs`) only to rank and phrase the shortlisted candidates, (5) validates every `#n` and path of the result against the index and the tree, and (6) renders one Markdown comment with the hidden marker `<!-- issue-enricher:v1 -->`, which the orchestrator posts or updates in place. A step in the `open-issue` and `worker-brief` skills runs it.

**What exists today.** CLAUDE.md "CodeRabbit" says issue enrichment is a CodeRabbit feature triggered when an issue is created or edited and asked for by hand (`@coderabbitai please analyze this issue and suggest related issues/PRs`); CodeRabbit has been unreliable since 2026-09-26. `tools/ai/local-ai-ask.cs` (exit 0, 1, 3 and 4, see its header) is the model gateway; `tools/ai/issues/` holds issue tooling (`check-feature-plans.ps1`, `check-project-membership.ps1`); `.github/scripts/check-pr-size.cs` is the pattern for a file-based app that runs `gh` through `ProcessStartInfo`, with a `--self-test`.

**Estimated scope.** 6 files, about 750 changed lines, in one pull request (see Estimated File Count).

**Affected packages.** None of the Encina packages. Paths: `tools/ai/issues/`, `.claude/skills/open-issue/`, `.claude/skills/worker-brief/`, `docs/engineering/`.

**Provider category.** None.

**Maintainer decision already recorded on the issue.** Comment of 2026-10-17: build the internal agent as a C# 14 file-based app drafting through `local-ai-ask.cs` (which falls back to the stand-in route when the local model is off); the body's class sketch must become a file-based app; rewrite the issue to the template before the brief.

---

## Design Choices

<details>
<summary><strong>1. Form and location</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) File-based app `tools/ai/issues/issue-enricher.cs`** | Complies with the scripting rule; the folder already holds issue tooling | None |
| **B) `tools/ai/agents/issue-enricher/issue-enricher.cs` (the issue's path)** | Matches the issue text | There is no `tools/ai/agents/` folder on `main`; "agent" in this repository means a `.claude/agents/*.md` definition |

### Chosen Option: **A - `tools/ai/issues/`**

### Rationale

- The maintainer comment fixes the form; the folder follows the existing tooling layout.

**Class:** Trivial
**Settled by:** AGENTS.md section 2; maintainer comment on issue #1449 (2026-10-17); layout of `tools/ai/issues/`

</details>

<details>
<summary><strong>2. Candidate retrieval for related and duplicate issues</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A local index `artifacts/issue-index/index.json` of every issue (number, state, title, labels, a body excerpt), refreshed with conditional REST reads (`since` / ETag), scored locally by title and body token overlap** | No search quota; works offline after the first run; deterministic; the model only ranks a short list | First run pages through every issue; one more artifact to refresh |
| **B) The REST search API (`search/issues`) per run** | No index | 30 requests per minute; purely lexical and title-weighted; no control over scoring |
| **C) Send titles of all issues to the model** | Semantic | Thousands of titles exceed the model's context and cost minutes per call |

### Chosen Option: **A - Local index (recommended, pending the maintainer)**

### Rationale

- A wins for repeatability and quota; B wins if the maintainer prefers no cache on disk. C is not viable at the current issue count.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. When the local model is unavailable</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Render the deterministic sections (shortlist by score, affected files and packages) and state "ranking not applied: local model unavailable"; exit code 3 or 4 of `local-ai-ask.cs` is passed on so the orchestrator can use the stand-in** | Context is never missing | A reader may over-trust an unranked list |
| **B) Post nothing and exit non-zero** | Never a half-checked comment | The issue stays without context while the model is off |

### Chosen Option: **A - Deterministic sections with an explicit label (recommended, pending the maintainer)**

### Rationale

- A wins when the model is off for days (it was switched off 2026-10-02 to 2026-10-17); B wins when a misleading list costs more than waiting. Either way the comment never claims a duplicate check that did not run.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Model output is untrusted: validate before rendering</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Accept only `#n` that exist in the index and paths that exist in `git ls-files`; drop everything else; the model never produces URLs; issue body text is passed as quoted data in the brief** | A prompt-injected issue body cannot add links, mentions or invented references | A valid but unlisted reference is dropped |
| **B) Render the model's text as is** | Richer prose | An issue body written by an external contributor can steer the model into posting arbitrary Markdown under the maintainer's account |

### Chosen Option: **A - Validate**

### Rationale

- The comment is posted with the maintainer's credentials; the issue body is third-party text.

**Class:** Settled
**Settled by:** AGENTS.md section 3 (untrusted input never decides on its own; gates fail closed); `docs/engineering/ai-task-routing.md` (local drafts are reviewed against the code before use)

</details>

<details>
<summary><strong>5. Idempotent posting</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One comment with the marker `<!-- issue-enricher:v1 -->`; found by REST, updated in place (`PATCH`); `--post` is the only option that writes, default prints the Markdown** | Re-running never duplicates; a dry run is the default | One extra REST read |
| **B) Always post a new comment** | Simple | Duplicates on every run, which the issue forbids |

### Chosen Option: **A - Marker, update in place**

### Rationale

- Acceptance criterion "Agent is idempotent (does not post duplicate comments)" of the issue; ADR-063 uses the same sticky-marker pattern for the coverage comment.

**Class:** Settled
**Settled by:** issue #1449 acceptance criteria; ADR-063 (hidden marker, updated in place)

</details>

<details>
<summary><strong>6. Trigger: the orchestrator's issue steps, not a bot-registry event</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A step in the `open-issue` skill (after creating the issue) and in `worker-brief` (when the issue has no enrichment comment)** | Runs where the local model is; every issue is covered | Orchestrator-run |
| **B) A GitHub Actions workflow on `issues: opened`** | Automatic | Hosted runners cannot reach the maintainer's local llama-server |
| **C) A `BOT-UNAVAILABLE` event of #1448** | Reuses the registry | #1448 detects absent PR reviews; there is no PR event for an issue |

### Chosen Option: **A - Skill steps**

### Rationale

- The model is local (`127.0.0.1:8080`), so only a local run can use it, and the issue says "every issue", not "only when CodeRabbit fails".

**Class:** Settled
**Settled by:** CLAUDE.md "Model routing" (the local model is reached through `tools/ai/local-ai-ask.cs` from the main session); issue #1449 Motivation (every issue has a context summary before development begins)

</details>

---

## Implementation Phases

### Phase 1: Core API (reading, index, shortlist, file mapping)

<details>
<summary><strong>Tasks</strong></summary>

1. **`tools/ai/issues/issue-enricher.cs`** (new). Usage: `dotnet run --file tools/ai/issues/issue-enricher.cs -- <issue-number> [--repo dlrivada/Encina] [--index artifacts/issue-index/index.json] [--refresh] [--post] [--no-model] [--out <file>] [--self-test]`. Calls `gh api` through `ProcessStartInfo` as `check-pr-size.cs` does (REST only, tokens as the environment provides them).
2. Index: `--refresh` pages `repos/<repo>/issues?state=all&per_page=100&since=<last>` (pull requests excluded); the file holds number, state, title, labels, first 400 characters of the body, `updated_at`.
3. Shortlist: lowercase tokens of the title and body (identifiers, `Encina.*` names, backticked terms) scored by weighted overlap with each indexed issue; the top 8 open or closed issues, never the issue itself. Issues the body already links (`#n`) are listed first as "linked".
4. Affected files and packages: backticked paths and `Encina.*` tokens of the issue text resolved against `git ls-files` and the `src/` folder names; at most 15 paths.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of #1449.

CONTEXT:
- C# 14 file-based app, no .csproj (AGENTS.md section 2). Pattern: .github/scripts/check-pr-size.cs (gh through ProcessStartInfo, fail closed, --self-test).

TASK:
Create tools/ai/issues/issue-enricher.cs with reading, the index, the shortlist and the file mapping as the plan describes. No model call yet.

KEY RULES:
- REST only; a gh failure exits non-zero, never an empty "clean" result.
- Never log or echo an issue body beyond what the comment renders.

REFERENCE FILES:
- .github/scripts/check-pr-size.cs, tools/ai/issues/check-feature-plans.ps1, docs/engineering/github-api-quota.md
```

</details>

### Phase 2: Model ranking, validation and rendering

<details>
<summary><strong>Tasks</strong></summary>

1. Write the brief and the shortlist to `artifacts/local-ai/` and call `tools/ai/local-ai-ask.cs --task issue-enrich` with `--out` under `artifacts/local-ai/out/` (the ledger row is written by that tool). Exit 1 or 3 or 4 follows Design Choice 3.
2. Validation (Design Choice 4): keep only `#n` present in the index and paths present in `git ls-files`; the model returns a small JSON (ranked numbers with a one-line reason, a duplicate verdict `none | possible #n`), parsed strictly; a parse failure counts as "model unavailable".
3. Render sections: Related issues, Duplicate check (`possible duplicate of #n` only with a score above the threshold AND the model's agreement), Affected files and packages, and a footer naming the index date and whether ranking ran. Marker on the first line.
4. `--post`: find the marker comment by REST (`issues/<n>/comments`), `PATCH` it or `POST` a new one.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of #1449.

CONTEXT:
- Phase 1 is merged. local-ai-ask.cs exit codes: 0 ok, 1 failure, 3 switch off and CLI unavailable (spawn local-ai-standin), 4 login or usage-limit problem.

TASK:
Add the model call, strict validation, rendering and --post as the plan Phase 2 describes.

KEY RULES:
- The model output never reaches the comment unvalidated.
- --post is the only option that writes to GitHub.
- A duplicate verdict needs both the score and the model.

REFERENCE FILES:
- tools/ai/local-ai-ask.cs, .claude/skills/local-ai-task/SKILL.md, .github/workflows/coverage-comment.yml (marker pattern)
```

</details>

### Phase 3: Skill integration, testing and documentation

<details>
<summary><strong>Tasks</strong></summary>

1. **`.claude/skills/open-issue/SKILL.md`** (modify): after the issue exists, run the enricher and post (`--post`); on exit 3 spawn `local-ai-standin` as the skill for the local model already says.
2. **`.claude/skills/worker-brief/SKILL.md`** (modify): when the issue has no comment with the marker, run it before writing the brief and read the result.
3. `--self-test` (fixtures embedded in the file): tokenisation, scoring order, exclusion of the issue itself, linked issues first, path resolution against a fake tree, validation drops an unknown `#n`, an unknown path and a URL, marker parsing, strict JSON failure counts as model unavailable, rendering is byte-stable. Numeric target: at least 14 assertions. CI wiring: one step `dotnet run --file tools/ai/issues/issue-enricher.cs -- --self-test` in `ci.yml` (a shared hot spot: **the orchestrator applies it**).
4. **`docs/engineering/issue-enricher.md`** (new, by `docs-writer`): usage, comment format, the index, exit codes. No changelog fragment (developer tooling; the issue ticks one, so `mechanical-fixer` adds `changelog.d/1449-issue-enricher.added.md` if the maintainer treats it as user-visible).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of #1449.

CONTEXT:
- Phases 1-2 are merged. Skills are instructions for the orchestrator session.

TASK:
Edit the two skills, add the self-test and the documentation page as the plan Phase 3 describes. Do NOT edit .github/workflows/*; report the exact ci.yml step.

KEY RULES:
- The skills run the enricher; the app posts only with --post.
- Documentation follows .claude/skills/encina-docs/SKILL.md; English only.

REFERENCE FILES:
- .claude/skills/open-issue/SKILL.md, .claude/skills/worker-brief/SKILL.md, tools/ai/issues/issue-enricher.cs
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Requirement | Key Details |
|----------|-------------|-------------|
| AGENTS.md section 2 | File-based apps for tooling | No `.csproj` |
| AGENTS.md section 3 | Untrusted input never decides alone | Choice 4 |
| ADR-063 | Sticky marker comment pattern | Choice 5 |
| ADR-054 D4 / ADR-062 | Telemetry tags and error categories | Not applicable: no telemetry, no `EncinaError` |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage |
|-----------|----------|-------|
| Model gateway | `tools/ai/local-ai-ask.cs` | Ranking and phrasing |
| File-based app pattern | `.github/scripts/check-pr-size.cs` | `gh` process, `--self-test` |
| Issue tooling folder | `tools/ai/issues/` | Location |
| Skills | `.claude/skills/open-issue/SKILL.md`, `.claude/skills/worker-brief/SKILL.md` | Triggers |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| none | none | File-based app: no `[LoggerMessage]`; no block claimed |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| New | 2 | `issue-enricher.cs`, `docs/engineering/issue-enricher.md` |
| Modified | 2 | the two skills |
| Workflow | 1 | the `ci.yml` self-test step, applied by the orchestrator |
| Fragment | 1 | optional changelog fragment |
| Coverage flags | 0 | Manifests cover `src/` only; numeric target of the self-test: at least 14 assertions |

One pull request: 6 files in total, under the limits.

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT:
- Encina issue #1449: an issue enricher as the CodeRabbit enrichment fallback. A C# 14 file-based app, tooling only.

IMPLEMENTATION OVERVIEW:
Phase 1 reading, local index, shortlist, file mapping; Phase 2 model ranking via tools/ai/local-ai-ask.cs, strict validation, marker comment, --post; Phase 3 skill steps, self-test, docs.

KEY PATTERNS:
- Model output is untrusted: only existing #n and existing paths survive.
- One comment with a marker, updated in place; default is a dry run.
- Local index (recommended, pending the maintainer); deterministic fallback when the model is off (recommended, pending the maintainer).

REFERENCE FILES:
- tools/ai/local-ai-ask.cs, .github/scripts/check-pr-size.cs, .claude/skills/open-issue/SKILL.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ | Phase 1: the local issue index is the cache, refreshed with `since` |
| 2 | OpenTelemetry | ❌ | Not applicable: file-based app, no instrumentation |
| 3 | Structured Logging | ❌ | Not applicable: console output of a tool |
| 4 | Health Checks | ❌ | Not applicable: not a service |
| 5 | Validation | ✅ | Phase 2: strict validation of the model output; Phase 1: argument checks |
| 6 | Resilience | ✅ | Phase 1 and 2: REST failure is a non-zero exit, a model failure follows choice 3 |
| 7 | Distributed Locks | ❌ | Not applicable: single orchestrator run |
| 8 | Transactions | ❌ | Not applicable: one read-only analysis and one comment write |
| 9 | Idempotency | ✅ | Phase 2: marker comment updated in place |
| 10 | Multi-Tenancy | ❌ | Not applicable: single repository |
| 11 | Module Isolation | ❌ | Not applicable |
| 12 | Audit Trail | ❌ | Not applicable: the GitHub comment history is the record |

---

## Next Steps

1. Review and approve this plan; two Dilemmas (index, model-off behavior) are answered when work on #1449 starts.
2. Rewrite the issue body to the `feature_request` template before the brief (the maintainer comment asks for it); the orchestrator applies the `ci.yml` self-test step.
