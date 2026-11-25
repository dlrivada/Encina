# Implementation Plan: `dependency-audit` skill, scheduled vulnerable/outdated package audit (the Dependabot fallback)

> **Issue**: [#1451](https://github.com/dlrivada/Encina/issues/1451) - [FEATURE] dependency-audit skill: scheduled vulnerable/outdated package audit as the Dependabot fallback
> **Status**: Draft plan (written against `origin/main` 4b0a600c7, 2026-11-25)
> **Epic**: [#1453](https://github.com/dlrivada/Encina/issues/1453) (Fallback system for external review bots)
> **Scope note**: repository tooling and one new workflow. No `src/` code, no EventIds.

---

## Summary

**What it implements.** A C# 14 file-based app `tools/ai/dependency-audit.cs` that runs `dotnet list package --vulnerable --include-transitive` and `--outdated` on `Encina.slnx` with `--format json`, normalises the findings, and writes a JSON and a Markdown report; a scheduled workflow that runs it and publishes the report; and a skill `.claude/skills/dependency-audit/SKILL.md` that the orchestrator runs to turn new vulnerable-package findings into issue files through `open-issue`, without duplicates.

**What exists today.** `.github/dependabot.yml` (NuGet in `/src`, `/tests`, `/tools`, plus GitHub Actions, weekly on Monday, grouped, cooldown); `Directory.Build.props` makes `NU*` warnings errors, so a restore that hits a known advisory fails the build (AGENTS.md section 1: `dotnet restore` audits transitive packages); `Directory.Packages.props` centralises versions; `.github/workflows/sbom.yml` runs on tags only. Nothing scans the base branch on a schedule, and nothing records a vulnerable or outdated package when Dependabot stops opening pull requests.

**Boundary.** The audit detects and records; it never opens a dependency pull request and never edits `Directory.Packages.props`. Fixing a package stays a normal issue.

**Estimated scope.** 8 files, about 650 changed lines, one pull request.

**Affected packages.** None of the Encina packages (`tools/ai/`, `.claude/skills/`, `.github/workflows/`, `docs/engineering/`).

**Provider category.** None.

**Maintainer decision already recorded on the issue.** Comment of 2026-10-17: a scheduled `dependency-audit` file-based app (no `.csproj`, AGENTS.md section 2), run by a scheduled workflow, routing findings through `open-issue`; Dependabot-only, a third-party SaaS and a `ci.yml`-only audit are rejected.

---

## Design Choices

<details>
<summary><strong>1. Implementation form: C# file-based app, not a project</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `tools/ai/dependency-audit.cs` (file-based app)** | Complies with the scripting rule; same form as `local-ai-ask.cs` | None |
| **B) `tools/ai/DependencyAudit.csproj`** | Familiar | The issue sketch says so, but AGENTS.md forbids a `.csproj` for tools |

### Chosen Option: **A - File-based app**

### Rationale

- The maintainer comment on #1451 says "no `.csproj`, AGENTS.md section 2".

**Class:** Settled
**Settled by:** AGENTS.md section 2; maintainer comment on issue #1451 (2026-10-17)

</details>

<details>
<summary><strong>2. Read the SDK's JSON, not its text table</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `dotnet list package ... --format json`** | Stable structure, one record per project and framework; no column parsing | Needs the SDK 9+ option (the repo is .NET 10 only) |
| **B) Parse the text output** | Works on any SDK | Column layout and localisation make it fragile |

### Chosen Option: **A - JSON**

### Rationale

- The repository requires the .NET 10 SDK (AGENTS.md section 1), which has `--format json`.

**Class:** Trivial
**Settled by:** AGENTS.md section 1 (.NET 10 only); `dotnet list package --format` option of the .NET 10 SDK

</details>

<details>
<summary><strong>3. Who opens the issues: the workflow or the orchestrator through `open-issue`</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The workflow only publishes the report (artifact and job summary, `contents: read`); the orchestrator runs the skill, which drafts issue files with `open-issue` and opens them** | Issues are born complete (template headers, milestone, priority label, project 1); no write token in the schedule | A human or session must run the skill; a detection can wait for the next session |
| **B) The workflow opens issues itself with `issues: write`** | Fully automatic | The default token cannot add the issue to project 1 and cannot read the template rules; every issue starts as `needs-triage` (issue-hygiene, #1987); a workflow with a write token runs on content of a package feed |

### Chosen Option: **A - Report from the workflow, issues from the orchestrator (recommended, pending the maintainer)**

### Rationale

- A wins while "an issue is complete or it does not exist" (#1926) holds; B wins when the maintainer wants a vulnerability to open an issue with no session running. A High or Critical finding turns the workflow red in both options, so the maintainer is notified.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Routing of outdated packages</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Report only (weekly summary table in the artifact and job summary), no issue** | Dependabot already groups updates; no noise | An outdated package is not tracked as work |
| **B) One issue per outdated package** | Each is tracked | Hundreds of packages, mostly minor; floods the board |
| **C) One rolling summary issue updated in place** | Visible, one item | A long-lived issue that never closes |

### Chosen Option: **A - Report only (recommended, pending the maintainer)**

### Rationale

- A wins for signal: the issue title says "vulnerable/outdated" but the acceptance concern is a blind spot for security. C wins if the maintainer wants outdated drift on the board. Vulnerable packages always become issues (choice 3).

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Issue type and prefix of a finding</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `[DEBT]` with `technical_debt.md`, labels `technical-debt`, `dependencies`, priority from severity (Critical/High: `p0-mandatory`, Moderate: `p1-recommended`, Low: `p2-post-1.0`)** | An existing template and prefix; priority set at creation | Severity-to-priority mapping is a default the maintainer can change in the skill |
| **B) A `[DEPENDENCY]` prefix as the issue says** | Matches the text | AGENTS.md section 11 has a closed prefix table; `check-issue-template` rejects it |

### Chosen Option: **A - `[DEBT]`**

### Rationale

- The prefix table of AGENTS.md section 11 is closed and `[DEPENDENCY]` is not in it; a vulnerable dependency is code that works but is unsafe, which is the `technical_debt` use.

**Class:** Settled
**Settled by:** AGENTS.md section 11 (template and prefix table); `.github/workflows/issue-hygiene.yml` (the three priority labels)

</details>

<details>
<summary><strong>6. Deduplication key and untrusted fields</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A marker `<!-- dependency-audit: <packageId>@<resolvedVersion> <advisoryId> -->` in the issue body; the skill searches open issues for it; package id and advisory URL are checked against allow-lists (`^[A-Za-z0-9._-]+$`, `^https://github\.com/advisories/GHSA-[a-z0-9-]+$`) before they enter a body** | Re-running creates nothing new; a hostile feed value cannot inject Markdown or links | A new advisory on the same package is a new issue (correct) |
| **B) Match on the title only** | No marker | Titles are edited; the match is fuzzy |

### Chosen Option: **A - Marker and allow-lists**

### Rationale

- The issue asks that duplicates are not created (cross-cutting row 9); feed data is untrusted input.

**Class:** Settled
**Settled by:** issue #1451 (Cross-Cutting row 9, Idempotency: the router checks for an existing issue); AGENTS.md section 3 (untrusted input never decides on its own)

</details>

---

## Implementation Phases

### Phase 1: Core API (the audit app)

<details>
<summary><strong>Tasks</strong></summary>

1. **`tools/ai/dependency-audit.cs`** (new): usage `dotnet run --file tools/ai/dependency-audit.cs -- [--solution Encina.slnx] [--vulnerable] [--outdated] [--out artifacts/dependency-audit] [--fail-on high] [--no-restore] [--self-test]`. It runs the two `dotnet list package` commands, reads their JSON, and keeps, per package id and resolved version, the projects that reference it, direct or transitive, severity, advisory URL; writes `dependency-audit.json` and `dependency-audit.md`. Exit codes: 0 (nothing at or above `--fail-on`), 1 (a vulnerable package at or above it, default `high`), 2 (a tool failure: the SDK command failed or the JSON was unreadable; never reported as "clean").
2. A tool failure is not a clean result: a non-zero exit from `dotnet list package`, an empty output for a solution that has projects, or a JSON problem exits 2.
3. `--self-test` fixtures under **`tools/ai/dependency-audit/fixtures/`** (new): `vulnerable.json`, `outdated.json`, `hostile.json`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of #1451.

CONTEXT:
- C# 14 file-based app, no .csproj (AGENTS.md section 2). Model the layout on tools/ai/local-ai-ask.cs and the --self-test pattern of .github/scripts/check-pr-size.cs.

TASK:
Create tools/ai/dependency-audit.cs and its fixtures as described in the plan Phase 1.

KEY RULES:
- Exit 2 on any tool failure; never a clean report from a failed command.
- Package ids and advisory URLs pass the allow-lists of Design Choice 6 before they reach any output.
- Production-grade XML or header comments; zero warnings.

REFERENCE FILES:
- tools/ai/local-ai-ask.cs, .github/scripts/check-pr-size.cs, Directory.Packages.props
```

</details>

### Phase 2: Scheduled workflow (the orchestrator applies it)

<details>
<summary><strong>Tasks</strong></summary>

1. **`.github/workflows/dependency-audit.yml`** (new, **applied by the orchestrator**: `.github/workflows/*` is a shared hot spot): `schedule` cron `23 4 * * *` (vulnerable) and `23 5 * * 1` (adds outdated, after Dependabot's Monday run), plus `workflow_dispatch`; one job, `permissions: contents: read` declared on the job (AGENTS.md section 10), `timeout-minutes: 30`; steps: checkout, setup-dotnet (10.0.x), `dotnet restore Encina.slnx`, `dotnet run --file tools/ai/dependency-audit.cs -- --no-restore ...`, upload the report artifact (retention 30 days), write the Markdown to the job summary. The job fails on exit 1 or 2.
2. A self-test step in `.github/workflows/ci.yml` (orchestrator): `dotnet run --file tools/ai/dependency-audit.cs -- --self-test`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of #1451. The ORCHESTRATOR applies .github/workflows edits: output the exact YAML and do not edit the workflow files.

CONTEXT:
- Existing workflows pin actions to the same majors (checkout v7, setup-dotnet v6); permissions are per job.

TASK:
Write dependency-audit.yml and the ci.yml self-test step per the plan Phase 2.

KEY RULES:
- No secrets, no write permission, no pull_request trigger.
- Event values reach scripts through env only.

REFERENCE FILES:
- .github/workflows/sbom.yml, .github/workflows/coverage-comment.yml, .github/workflows/ci.yml
```

</details>

### Phase 3: The skill, tests and documentation

<details>
<summary><strong>Tasks</strong></summary>

1. **`.claude/skills/dependency-audit/SKILL.md`** (new): when to run (after the scheduled run is red, weekly, or before a release); fetch the latest report artifact with `gh run download`, or run the app locally; for each vulnerable package with no open issue carrying its marker (REST search), draft an issue file per `open-issue` (`[DEBT]`, labels and priority of Design Choice 5, the marker, advisory link, projects that reference it, and the fix target version); the orchestrator opens it; a closed issue with the same marker and a still-vulnerable package is a regression and is opened again.
2. **`docs/engineering/dependency-audit.md`** (new, by `docs-writer`): schedule, exit codes, report fields, how it differs from Dependabot (detects, does not fix).
3. No changelog fragment: developer tooling, not user-visible (the issue does not tick one).
4. Verification: `dotnet run --file tools/ai/dependency-audit.cs -- --self-test` exits 0; a local run against a temporary project referencing a package with a known advisory reports it and exits 1.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of #1451.

CONTEXT:
- Phases 1-2 are merged; the report JSON is the skill's input.

TASK:
Write the SKILL.md and the documentation page as described in the plan Phase 3.

KEY RULES:
- The skill never opens an issue itself; it drafts files and the orchestrator opens them.
- The documentation page follows .claude/skills/encina-docs/SKILL.md.

REFERENCE FILES:
- .claude/skills/open-issue/SKILL.md, .claude/skills/site-health/SKILL.md, .github/workflows/issue-hygiene.yml
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Requirement | Key Details |
|----------|-------------|-------------|
| AGENTS.md section 2 | File-based apps for tooling | No `.csproj` |
| AGENTS.md section 11 | Issue templates and prefixes | `[DEBT]` for a finding |
| AGENTS.md section 10 | `permissions:` per job | Read-only job |
| ADR-054 D4 / ADR-062 | Telemetry tags and error categories | Not applicable: no telemetry, no `EncinaError` |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage |
|-----------|----------|-------|
| Dependabot | `.github/dependabot.yml` | Unchanged; the audit is independent of it |
| Central versions | `Directory.Packages.props`, `Directory.Build.props` (`NU*` as errors) | The audit reads resolved versions |
| Issue routing | `.claude/skills/open-issue/SKILL.md` | The skill's output format |
| Self-test pattern | `.github/scripts/check-pr-size.cs --self-test` | `--self-test` |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| none | none | File-based app: no `[LoggerMessage]`; no block claimed |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| New | 6 | `dependency-audit.cs`, three fixtures, `SKILL.md`, docs page |
| Workflows | 2 | `dependency-audit.yml` (new) and the `ci.yml` step, applied by the orchestrator |
| Coverage flags | 0 | Manifests cover `src/`; numeric target of the self-test: at least 12 assertions (parse vulnerable, parse outdated, transitive flag, severity order, `--fail-on` exit codes 0 and 1, tool failure exit 2, empty output exit 2, hostile package id dropped, hostile URL dropped, marker format, dedupe, report stable across runs) |

One pull request: 8 files in total, under the limits.

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT:
- Encina issue #1451: scheduled dependency audit as the Dependabot fallback. Tooling only.

IMPLEMENTATION OVERVIEW:
Phase 1 tools/ai/dependency-audit.cs with JSON parsing, reports, exit codes, --self-test and fixtures; Phase 2 dependency-audit.yml and a ci.yml self-test step (orchestrator applies); Phase 3 SKILL.md and docs page.

KEY PATTERNS:
- Detect and record; never fix or open a dependency PR.
- A tool failure is exit 2, never a clean report.
- Issues come from the orchestrator through open-issue (recommended, pending the maintainer); outdated packages are report-only (recommended, pending the maintainer).

REFERENCE FILES:
- tools/ai/local-ai-ask.cs, .github/dependabot.yml, Directory.Packages.props, .claude/skills/open-issue/SKILL.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Not applicable: tooling, no runtime reads |
| 2 | OpenTelemetry | ❌ | Not applicable: file-based app, no instrumentation |
| 3 | Structured Logging | ❌ | Not applicable: workflow log and job summary |
| 4 | Health Checks | ❌ | Not applicable: not a service |
| 5 | Validation | ✅ | Phase 1: allow-lists for package ids and advisory URLs |
| 6 | Resilience | ⏭️ | Retry of a transient feed failure is deferred: the next scheduled run is the retry; a failed run exits 2 and is visible |
| 7 | Distributed Locks | ❌ | Not applicable: a read-only report; runs are independent |
| 8 | Transactions | ❌ | Not applicable |
| 9 | Idempotency | ✅ | Phase 1 and 3: stable report; the marker prevents duplicate issues |
| 10 | Multi-Tenancy | ❌ | Not applicable: single repository |
| 11 | Module Isolation | ❌ | Not applicable |
| 12 | Audit Trail | ✅ | Phase 3: each finding becomes an issue with its marker; the artifact is kept 30 days |

---

## Next Steps

1. Review and approve this plan; two Dilemmas (who opens issues, outdated routing) are answered when work on #1451 starts.
2. The orchestrator applies `dependency-audit.yml` and the `ci.yml` step.
