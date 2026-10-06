# Implementation Plan: Release Readiness Gate — Final Pre-Release Checklist for the 1.0 Sequence

> **Issue**: [#104](https://github.com/dlrivada/Encina/issues/104)
> **Type**: Feature (release engineering, developer tooling)
> **Complexity**: Medium (7 phases, no `src/` code, one C# file-based tool, one registry, CI wiring, docs)
> **Estimated Scope**: ~1,100-1,400 lines of tool code (`.github/scripts/release-readiness.cs`) + ~600 lines of registry JSON + ~25 self-test fixtures + ~4 documentation pages changed

---

## Summary

Issue #104 asks for a "final pre-release checklist" covering code quality, documentation, security, legal, publishing and branding before `1.0.0`. Read against today's repository and [SPEC-000](../specifications/SPEC-000-encina-1.0-baseline-and-release-scope.md), the issue describes two different things:

1. **A checklist that can be verified.** SPEC-000 §4 already defines most of it as acceptance criteria (AC-001 – AC-029), and SPEC-002 adds the release-process criteria AC-025 – AC-030 (law-map re-verification, legal status, SBOM, SECURITY.md, advisories, Trusted Publishing). The comment on #104 makes SPEC-002 REQ-025 a step of this checklist ("re-verify the SPEC-002 §3 law map … before each minor tag of the 1.0 sequence and before `1.0.0-rc.1`").
2. **The act of cutting 1.0.** Sign-off, tag, publish to NuGet.org and the announcement.

This plan implements the first one as a **release readiness gate**:

- a registry of checklist items, each traced to a SPEC acceptance criterion or to the issue;
- a C# file-based tool that collects the facts from GitHub, the Pages dashboards, SonarCloud and nuget.org, evaluates every item for a release profile (`minor`, `rc`, `final`), and fails closed;
- a rendered checklist and sign-off file for the human items;
- the generated evidence report that SPEC-000 AC-022 requires.

The tool is wired into the release PR and into [`docs/releases/RELEASE-PROCESS.md`](../releases/RELEASE-PROCESS.md). How the second part (the 1.0 cut) is tracked is Design Choice 7.

**Provider category**: none. The feature is repository tooling and touches no `src/` package, so no provider matrix applies.

**Affected packages**: none ship. The files touched are under `.github/` and `docs/`, plus `.github/workflows/ci.yml`, which is the orchestrator's hot spot.

### What already exists (partial implementation)

Most items of the issue are owned by other issues or already exist. The gate checks them; it does not rebuild them.

| Issue item | State today (evidence) | Owner |
|---|---|---|
| All tests pass | `RELEASE-PROCESS.md:119-144` requires a green `CI Full` run, all jobs `success`, on the commit to tag; `ci-full.yml:531-580` `pack` runs only when `build` and the six `test-*` jobs succeed (#1745, closed) | done; the gate automates the check |
| Zero build warnings | `Directory.Build.props:24` `TreatWarningsAsErrors` true; CI build fails on any warning | done |
| Code coverage ≥ 85% | **Superseded.** AGENTS.md §9 forbids a project-wide percentage. SPEC-000 AC-007 asks that every 1.0 package meet its per-flag manifest targets, with `latest.json` from the release week. The per-flag gate is #1651 (open) | #1651; the gate reads the coverage `latest.json` |
| Mutation score ≥ 80% | **Superseded.** SPEC-000 REQ-008 and §6 rule out a mutation threshold. AC-008 asks for a completed 17-shard run within the release month | gate reads the mutations `latest.json` |
| No critical SonarCloud issues | SonarCloud kept (DEC-006). The release blocks at high or higher, and medium findings are listed (DEC-004, AC-009). `.github/scripts/sonar-issues.ps1` queries the API | gate |
| CodeQL passing | `.github/workflows/codeql.yml` job `Analyze`, a required check (REQ-020) | gate reads the open alerts at high or higher |
| No secrets in code (git-secrets) | Secret scanning and push protection are **enabled** (`GET /repos/dlrivada/Encina` → `security_and_analysis.secret_scanning`, `secret_scanning_push_protection`: `enabled`) | gate reads the open secret-scanning alerts |
| Dependabot configured | `.github/dependabot.yml` (NuGet split into `/src`, `/tests`, `/tools`, #1042); `dependabot_security_updates` enabled | done |
| No known CVEs | NuGet audit fails the build at moderate or higher (`Directory.Build.props:24-25`, DEC-004, REQ-005); VEX statements are #1253 | gate reads the open Dependabot alerts |
| SECURITY.md published | `SECURITY.md` exists (81 lines, #94 closed); the five SPEC-002 REQ-028 elements and the legal status (REQ-026) are #1229 (open) | #1229 |
| License correct (MIT) | `LICENSE` is MIT; `PackageLicenseExpression` is not set anywhere: `src/Encina/Encina.csproj` has no package metadata | #102 |
| Dependencies license-compatible, no GPL, third-party notices | **Nothing exists and no issue tracks it** | new issue (Prerequisites) |
| CHANGELOG up to date | `changelog.d/` fragments + `.github/scripts/changelog-fragments.cs --release`; `CHANGELOG.md:19` `## [0.13.0] - 2026-09-22`; tags `v0.10.0` – `v0.13.0` exist | done; gate checks AC-017 |
| All documentation links working | `.github/workflows/link-check.yml` (daily full scan, one `link-health` tracking issue) | gate |
| API docs generated | `docs.yml` builds the DocFX reference; #1032 (254 DocFX warnings) open | #1032 |
| README / examples | AC-015: #81, #82, #83, #87 (and #88, #89) | documentation milestone |
| Package names reserved, publish workflow | #100, #101, #1231 (Trusted Publishing + prefix), #1100 (CI release workflow, manual until 1.0 by the maintainer's decision of 2026-10-02) | those issues |
| Version 1.0.0 | `Directory.Build.props:4-5` `0.14.0` / `dev`; the runbook clears the suffix | gate |
| Release notes, GitHub Release | `RELEASE-PROCESS.md` steps 1 and 4; `docs/releases/v0.13.0/README.md` is the model | gate |
| Logo, icons, social preview, banner | only `docs/assets/Encina Framework Identidad Visual.png` exists | #103, #102 |
| SBOM | `.github/workflows/sbom.yml` covers `src/Encina` only (line 30) | #1228 |
| Signing, provenance | not implemented | #92, #93 |
| Law map re-verified before each minor tag | SPEC-002 §3 carries "Verified on **2026-09-23**" (`SPEC-002…md:77`); cadence at line 147; AC-025 at line 476 | this plan (automated item) |
| SDK pinned (`global.json`, SPEC-000 REQ-020/AC-020) | **No `global.json` in the repository**; `ci.yml:17` uses `DOTNET_SDK_VERSION: "10.0.x"`; no issue tracks it | new issue (Prerequisites) |
| Bug gate (SPEC-000 AC-011) | AC-011 relies on a `deferred-1.0` label that **does not exist** (labels have `post-1.0` and `p2-post-1.0` only) | Phase 4 (orchestrator creates the label) |
| Package manifest (AC-001/AC-002) | #1362 (open) | #1362 |

The repository has one stale checklist: `.opencode/skills/release-checklist/SKILL.md` still lists "Line Coverage ≥85%" (lines 30-35), against AGENTS.md §9. #1655 already tracks it. This plan makes that skill point at the gate instead of keeping a second checklist (Phase 7).

### Corrections to the issue body

The plan follows SPEC-000 where the 2025 issue text disagrees with it:

- the coverage and mutation thresholds above are replaced as shown in the table;
- `git-secrets` is replaced by GitHub secret scanning, which is already on;
- "Branch protection: require PR reviews, signed commits" in EPIC #893 conflicts with SPEC-000 REQ-020 ("no required approving review"). The gate checks REQ-020 exactly as AC-020 states it.

---

## Design Choices

<details>
<summary><strong>1. Deliverable shape — executable readiness gate plus rendered checklist</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Markdown checklist page only** (`docs/releases/RELEASE-CHECKLIST.md`, ticked by hand per release) | Smallest change; readable; matches the issue's wording | Every item is checked by hand ~5 times before 1.0 (one per minor tag plus rc and final); breaks SPEC-000 INV-001 ("nothing is claimed that CI cannot regenerate"); does not produce the AC-022 evidence report; the 0.13.0 cut showed manual checks drift (#1747) |
| **B) Executable gate (registry + tool) that also renders the checklist and the sign-off file** | Every machine-checkable item is checked the same way each time; fails closed; the same facts produce the AC-022 evidence; the human items stay explicit in a sign-off file | ~1,200 lines of tool code plus fixtures to maintain; needs API access (token scopes, rate limits) |
| **C) One GitHub issue per release, opened from an issue template with the checklist** | Visible on the board; comment history per item | Still manual; an issue template cannot verify anything; adds a ninth template to `.github/ISSUE_TEMPLATE/` that AGENTS.md §11 would have to list |

### Chosen Option: **B — Executable gate plus rendered checklist** (recommended, pending the maintainer)

### Rationale

I recommend B:

- About two thirds of the items are facts that an API can answer: CI run conclusions, alert counts, issue states, file contents, dashboard timestamps, branch protection. Checking them by hand five or more times before 1.0 is where drift comes from. #1747 was a runbook that let a release publish with failing tests.
- SPEC-000 INV-001 and AC-022 require evidence generated from CI artifacts. The gate gives that evidence as a by-product.
- The human items (adversarial review AC-023, maintainer sign-off, announcement, social preview) stay human. B makes them explicit, unchecked lines that block until answered.

</details>

<details>
<summary><strong>2. Source of truth for the checklist items — a JSON registry traced to SPEC acceptance criteria</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) JSON registry** `.github/release-checklist.json`; the Markdown checklist is rendered from it | One place to add or retire an item; each entry carries its source (`SPEC-000 AC-009`, `SPEC-002 AC-025`, `#104`), its profiles and its check; the tool validates the registry (`--check-registry`) like `dashboard-freshness.cs --check-registry` validates `tools/ai/sites.json` | JSON is less pleasant to read than Markdown; the rendered page must be regenerated when the registry changes |
| **B) Markdown checklist that the tool parses** (`- [ ] AC-009 … <!-- check: codeql-high -->`) | Human-first; one file | Parsing structure out of prose is fragile (the same class of problem as the heading parsing of the plan gate `tools/ai/plans/check-plan.ps1`, #1927); check parameters in HTML comments are hard to validate |
| **C) Items hard-coded in the tool** | No second file | Adding an item needs a code change; the traceability to SPEC criteria lives in code comments; the reader of a release PR cannot see the list without running the tool |

### Chosen Option: **A — JSON registry rendered to Markdown** (recommended, pending the maintainer)

### Rationale

I recommend A:

- SPEC-000 is the authority, and its criteria have stable identifiers. A registry keyed by those identifiers makes traceability mechanical: `--check-registry` can fail when a SPEC-000 AC or a SPEC-002 AC-025 – AC-030 has no entry, or is retired without a reason.
- The repository already uses this pattern twice: `.github/coverage-manifest/*.json` and `tools/ai/sites.json`.

</details>

<details>
<summary><strong>3. Release profiles and cadence — <code>minor</code>, <code>rc</code> and <code>final</code>, run before every tag of the 1.0 sequence</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One checklist, run once before `1.0.0`** | Matches the issue literally; least configuration | SPEC-002 REQ-025/AC-025 needs the law-map step before **each minor tag** and before `1.0.0-rc.1`; SPEC-000 DEC-005 makes every 1.0 block close its own minor version. A one-shot list misses all of those, and finds problems at the last moment |
| **B) Three profiles** (`minor` for each `vX.Y.0` of the sequence, `rc` for `1.0.0-rc.N`, `final` for `1.0.0`); each item states block / warn / skip per profile | Problems surface at each minor tag, not only at rc; the minor profile stays light (CI Full on the SHA, CHANGELOG section, release notes page, law-map date, milestone clean, version without suffix) while rc carries all of SPEC-000 §4 | The registry carries a severity per profile; three profiles to test |
| **C) Continuous readiness**: a scheduled workflow evaluates the `rc` profile nightly and publishes a "distance to 1.0" page | Always-current view of what blocks 1.0 | A dashboard is outside the 1.0 contract (SPEC-000 §6 lists dashboards as non-goals); nightly API load; a red nightly result nobody acts on becomes noise |

### Chosen Option: **B — Three profiles, run before every tag** (recommended, pending the maintainer)

### Rationale

I recommend B:

- It satisfies SPEC-002 AC-025 by construction: the law-map date item blocks in the `minor` and `rc` profiles.
- It applies DEC-005's sequence of minor releases as rehearsals of the 1.0 cut. The first real run is the `v0.14.0` tag, which exercises the tool long before `rc`.
- Running C on demand stays possible later: `--profile rc` on `workflow_dispatch` costs nothing extra.

</details>

<details>
<summary><strong>4. Enforcement point — a release-only job inside <code>ci.yml</code> that feeds <code>ci-result</code></strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Advisory**: the maintainer runs the tool locally as a step of `RELEASE-PROCESS.md` | No CI change; works today under the manual-release decision of #1100 | Nothing stops a release whose gate failed; the same gap #1747 closed for CI Full |
| **B) Job `release-readiness` in `ci.yml`**, run only when the PR head branch starts with `release/` (the runbook already requires that prefix, `RELEASE-PROCESS.md:51`), listed in `ci-result`'s `needs`; plus a pre-tag re-check (`--only ci-full-on-sha`) in step 2 of the runbook | Blocking without changing the required checks: SPEC-000 REQ-020/AC-020 fixes them to **exactly** `build`, `ci-result` and `Analyze`, and `ci-result` already aggregates jobs; the release PR cannot merge with a failing item | Touches `ci.yml`, a shared hot spot (orchestrator-owned); `ci-result` must treat the job's `skipped` state on non-release PRs as success |
| **C) A separate workflow `release-readiness.yml` added as a required check** | Isolated file | Breaks REQ-020's "exactly the required checks" rule and AC-020's evidence; a required check that skips on most PRs needs extra handling in branch protection |

### Chosen Option: **B — Release-only job in `ci.yml` feeding `ci-result`, plus the pre-tag re-check** (recommended, pending the maintainer)

### Rationale

I recommend B:

- It blocks, and it does so inside the protection model that SPEC-000 fixed.
- The two-point design is needed. The release PR's merge commit, which is the commit that gets tagged, does not exist while the PR is open, so "CI Full green on the tagged SHA" can only be checked before the tag. `RELEASE-PROCESS.md` step 2 already does that check by hand, and the tool takes it over.
- When #1100 automates releases, the same two calls move into that workflow unchanged.

</details>

<details>
<summary><strong>5. Data flow and evidence — collect a facts snapshot, evaluate it offline, render the evidence report from the same snapshot</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Two stages**: `--collect` writes `facts.json` (every API answer, with its source URL and retrieval time); `--evaluate` and `--evidence` read only that file | Evaluation is deterministic and testable offline with fixture facts; the evidence report (`docs/releases/v<version>/evidence.md`, SPEC-000 AC-022) and the verdict cannot disagree; facts that the workflow token cannot read (for example secret-scanning alerts) can come from the maintainer's run committed in the release PR, with a maximum age | Two commands instead of one; a committed facts file must be guarded against staleness (the tool rejects facts older than a set age or collected for another version) |
| **B) Live evaluation** (each check calls the API as it runs); the evidence report is a separate tool | Simpler first version | Self-tests need network mocks; verdict and evidence can differ; AC-022 still needs a second tool that repeats the queries |
| **C) No evidence report**: the gate prints PASS/FAIL and links to the dashboards | Smallest | SPEC-000 AC-022 stays unimplemented, and no issue owns it today |

### Chosen Option: **A — Facts snapshot, offline evaluation, evidence from the same snapshot** (recommended, pending the maintainer)

### Rationale

I recommend A:

- AC-022 has no owning issue. The gate already needs the same data, so producing the evidence from one snapshot closes AC-022 at almost no extra cost.
- The offline evaluator is what makes `--self-test` possible without network access, the same way `coverage-report.cs --self-test` and `crap-gate-selftest.ps1` run in CI today (`ci.yml:556`, `ci.yml:598-631`).

</details>

<details>
<summary><strong>6. Scope boundary with the sibling release issues — the gate checks, the owners implement, unowned gaps get issues</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Gate only.** An item owned by another issue (#92, #93, #100 – #103, #1100, #1228 – #1231, #1253, #1362, #1651) is checked through its artifact or its issue state; each gap with no owner gets its own issue | #104 stays a focused, reviewable tool; ownership is clear; matches EPIC #893, which lists #104 as depending on "all others" | The gate will report red items for months, until the owners land |
| **B) #104 also implements every missing item** (licence scan and notices, `global.json`, branding, the bug-gate label …) | One issue closes many gaps | A PR that mixes a tool, CI, licensing, SDK pinning and branding; duplicates work owned by other issues; violates one-issue-per-worker (CLAUDE.md "Orchestration") |
| **C) Gate only, and the unowned gaps stay manual items** | No new issues | Hides real gaps behind a tick box; against AGENTS.md §11 ("never leave an identified problem unrecorded") |

### Chosen Option: **A — Gate only; unowned gaps become issues** (recommended, pending the maintainer)

### Rationale

I recommend A:

- Three gaps have no owner today: dependency-licence compatibility and third-party notices, `global.json`, and workflow-level `permissions:` in six workflows. Their issue files are drafted with this plan (Prerequisites & Dependencies).
- The `deferred-1.0` label that AC-011 needs is a one-line repository change. The orchestrator makes it in Phase 4, so it needs no issue.

</details>

<details>
<summary><strong>7. Lifecycle of #104 — the tool closes #104; cutting 1.0 is tracked by its own issue</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) #104 closes when the gate merges and has run once on a real minor tag** | A clean "done" for the tooling; the 1.0 checklist run is evidenced in the `v1.0.0` evidence report anyway | The issue's own acceptance criteria ("v1.0.0 published", "announcement posted") would then be closed without being met; EPIC #893 lists "#104 fully checked off" |
| **B) #104 stays open until `1.0.0` is published and announced** | Matches the issue text literally | The tool's PR cannot say `Fixes #104`; an open issue whose deliverable merged months earlier hides the real state on the board |
| **C) Split.** #104 delivers the gate (`Fixes #104` on its PR, after the first minor-tag run); a new `[INFRA]` issue in the same milestone, "Cut `1.0.0-rc.1` and `1.0.0` through the readiness gate", owns sign-off, publish and announcement, and EPIC #893's criterion is reworded to point at it | Each issue has a deliverable that can be verified; the 1.0 cut gets its own acceptance criteria (evidence report, adversarial review, NuGet.org listing, announcement) | One more issue; EPIC #893 must be edited |

### Chosen Option: **C — Split: #104 is the gate, a new issue cuts 1.0** (recommended, pending the maintainer)

### Rationale

I recommend C:

- Under C, both the tool and the 1.0 cut have acceptance criteria that can be checked.
- The issue's own "Final Tasks" (sign-off, tag, publish, announce) are events of the release day, not engineering work, and they depend on every other release issue.
- The draft of the new issue is included (Prerequisites & Dependencies). It is opened only if the maintainer picks C.

</details>

<details>
<summary><strong>8. Implementation language — a C# 14 file-based app next to the other release scripts</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) C# file-based app** `.github/scripts/release-readiness.cs` | Same family as `changelog-fragments.cs` and `dashboard-freshness.cs`, which the runbook already calls; typed JSON models (`System.Text.Json`); `--self-test` convention already wired in CI; runs wherever `setup-dotnet` runs | GitHub calls need either `HttpClient` + token handling or `gh` as a child process; slower first start (file-based app compilation) |
| **B) PowerShell 7 script** `tools/release/release-readiness.ps1` | `gh api` and `ConvertFrom-Json` are very short; same style as the plan gate `check-plan.ps1` (#1927) and its `-SelfTest` fixtures | Loosely typed models for a ~70-item registry and a large facts file; AGENTS.md §2 allows both, but the release tooling the runbook uses today is C# |
| **C) A GitHub Action from the marketplace** | No code to maintain | No action covers SPEC-000-specific criteria; adds a third-party dependency to the release path, which SLSA work (#92) would then have to pin and review |

### Chosen Option: **A — C# file-based app** (recommended, pending the maintainer)

### Rationale

I recommend A:

- The gate is the largest script of the release toolchain, and its value is in typed, deterministic evaluation. C# models plus the existing `--self-test` convention fit that best.
- GitHub calls go through `gh api` as a child process. That keeps authentication identical to the runbook's commands (keyring locally, `GH_TOKEN` in Actions) and uses REST only, never GraphQL (the GraphQL rate limit was a known problem in this repository; the plan gate of #1927 reads issues through REST for the same reason).

</details>

---

## Implementation Phases

### Phase 1: Core API Design — registry schema, facts schema and command-line contract

> **Goal**: Fix the three contracts (registry, facts, CLI) before any check is written.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `.github/release-checklist.json` (schema `1`) with one entry per item:
   - `id` (stable, kebab-case, e.g. `ci-full-on-sha`, `codeql-high-alerts`, `law-map-date`)
   - `title` (one plain-English sentence)
   - `source`: an array of references (`"SPEC-000 AC-009"`, `"SPEC-002 AC-025"`, `"#104"`)
   - `owner`: the issue numbers that implement the item, if any (e.g. `[92]`)
   - `kind`: one of `auto`, `issue-state`, `manual`
   - `check`: the evaluator name plus its parameters (only for `auto` and `issue-state`)
   - `profiles`: `{ "minor": "block|warn|skip", "rc": "…", "final": "…" }`
   - `retired`: optional `{ "reason": "…", "ref": "…" }` (an item is never deleted silently)
2. **Populate the registry** with every row of the Summary table and every SPEC-000 AC-001 – AC-029 and SPEC-002 AC-025 – AC-030 (about 70 entries).
   - Several ACs split into more than one item (AC-020 → `branch-protection-exact`, `global-json-present`).
   - The superseded issue items (coverage ≥ 85 %, mutation ≥ 80 %, git-secrets) are recorded with `retired.reason` that names AGENTS.md §9, SPEC-000 REQ-008 and the secret-scanning setting.
3. **Define the facts schema** (documented in the tool header and in the ADR):
   - `{ schema, version, profile, sha, collectedAtUtc, collector, facts: { <factKey>: { value, source, retrievedAtUtc, error? } } }`
   - A fact with `error` is evaluated as FAIL for a `block` item. This is the fail-closed rule (AGENTS.md §3).
4. **Fix the CLI contract** of `.github/scripts/release-readiness.cs`:
   - `--check-registry [--registry <path>]`: validates the schema and that every SPEC-000 AC and SPEC-002 AC-025 – AC-030 has an entry or a `retired` record.
   - `--collect --version <v> --profile <p> [--sha <sha>] --out <facts.json>`
   - `--evaluate --facts <facts.json> [--signoff <checklist.md>] [--only <id,…>] [--max-facts-age-hours 24] [--now <utc>]`
   - `--evidence --facts <facts.json> [--signoff <checklist.md>] --out <evidence.md>`
   - `--render-checklist --profile <p> --version <v> --out <checklist.md>`
   - `--self-test`
   - Exit codes: `0` pass, `1` at least one blocking item failed or is unanswered, `2` usage or registry error.
5. **Define the sign-off file format**: `docs/releases/v<version>/checklist.md`, rendered by `--render-checklist`. It has one line per `manual` item:
   - format: `- [ ] <id> — <title> — evidence: <link>`
   - the tool parses ticked lines;
   - a ticked line without an evidence link counts as unanswered.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
You are implementing Phase 1 of the release readiness gate for Encina issue #104 (plan: docs/plans/pre-release-checklist-implementation-plan-104.md).

CONTEXT:
- Encina is a .NET 10 / C# 14 library, pre-1.0. Releases are cut by hand with docs/releases/RELEASE-PROCESS.md until #1100 automates them.
- SPEC-000 (docs/specifications/SPEC-000-encina-1.0-baseline-and-release-scope.md) section 4 lists acceptance criteria AC-001..AC-029; SPEC-002 (docs/specifications/SPEC-002-eu-regulatory-readiness.md) lines 476-481 list AC-025..AC-030, the release-process criteria. The comment on #104 makes SPEC-002 REQ-025 (law-map re-verification before each minor tag and before 1.0.0-rc.1) a checklist step.
- The plan's Summary table maps every item of issue #104 to its state and owner issue.

TASK:
1. Create .github/release-checklist.json (schema 1) with one entry per checklist item: id, title, source[], owner[], kind (auto | issue-state | manual), check {name, params}, profiles {minor, rc, final: block | warn | skip}, optional retired {reason, ref}.
2. Populate it from the Summary table and from every SPEC-000 AC and SPEC-002 AC-025..AC-030. Record the superseded issue items (coverage >= 85%, mutation >= 80%, git-secrets) as retired with their reason.
3. Write the header comment of .github/scripts/release-readiness.cs: the CLI contract (--check-registry, --collect, --evaluate, --evidence, --render-checklist, --self-test), exit codes 0/1/2, the facts schema, the sign-off file format. Implement only argument parsing, the registry model and --check-registry in this phase.

KEY RULES:
- Scripting only in C# 14 file-based apps or PowerShell (AGENTS.md section 2); no python, no bash constructs.
- Fail closed: an unreadable fact fails a blocking item (AGENTS.md section 3, compliance and security gates).
- Never type a coverage or mutation figure into the registry; thresholds come from the manifests and SPEC-000 (AGENTS.md section 9, SPEC-001).
- Item ids are stable; an item is retired with a reason, never deleted.
- English only in code, comments and JSON.

REFERENCE FILES:
- .github/scripts/dashboard-freshness.cs (registry validation with --check-registry, exit codes, header style)
- tools/ai/sites.json (registry shape precedent)
- .github/scripts/changelog-fragments.cs (release tooling family, header usage block)
- docs/releases/RELEASE-PROCESS.md (the steps the gate plugs into)
- docs/specifications/SPEC-000-encina-1.0-baseline-and-release-scope.md section 4
```

</details>

---

### Phase 2: Implementation — collectors and evaluators

> **Goal**: Collect every fact through REST and evaluate every `auto` and `issue-state` item offline.

<details>
<summary><strong>Tasks</strong></summary>

1. **Collector infrastructure**:
   - `gh api` as a child process (REST only, `--paginate` where needed) and `HttpClient` for public endpoints (Pages, SonarCloud, nuget.org).
   - Every call is wrapped in a retry: 3 attempts with exponential backoff on 5xx, timeouts and secondary rate-limit 403s.
   - A failed fact is recorded with `error` and never omitted.
2. **Evaluators**: one small static function each, with cyclomatic complexity kept low. The function names follow the registry `check.name`.
   - `ci-run`: latest run of a workflow on a SHA; `event` in an allowed set; every job `success`. Replaces the manual commands at `RELEASE-PROCESS.md:121-142`.
   - `workflow-latest`: last run of `docs.yml`, `link-check.yml` (scheduled full scan) and `codeql.yml` on `main` succeeded within N days.
   - `code-scanning-alerts`: `GET /repos/{o}/{r}/code-scanning/alerts?state=open`, count with severity at or above a threshold (DEC-004: high).
   - `secret-scanning-alerts`, `dependabot-alerts`: open counts at or above a severity.
   - `sonar-issues`: SonarCloud `api/issues/search` for project `dlrivada_Encina` (key from `.github/scripts/sonar-issues.ps1:15`), count at or above HIGH/BLOCKER. Medium findings are listed for the evidence (AC-009).
   - `issue-state`: listed issues closed, optionally with a label (e.g. AC-026 "#804–#808 labelled post-1.0 and left open").
   - `bug-gate`: AC-011 exactly as written. Take the union of the open `bug`-labelled issues and the open `[BUG]`-titled issues. Every issue in it must carry `deferred-1.0` and a reason comment, and none may carry `security`.
   - `skip-scan`: every `Skip =` in `tests/**/*.cs` references an issue (`#\d+`) on the same line or the line above (AC-006). Today there are 21 occurrences to classify.
   - `file-exists`, `file-regex`: e.g. `global.json`; `LICENSE` starts with `MIT License`; `Directory.Build.props` `VersionPrefix` equals the version and `VersionSuffix` is empty.
   - `changelog-section`: `## [<version>] - <yyyy-MM-dd>` exists and is the top released section; for `minor`, every earlier minor of the sequence has a dated section (AC-017).
   - `public-api-unshipped-empty`: every `src/**/PublicAPI.Unshipped.txt` is empty (AC-013; `final` profile; 88 non-empty files today).
   - `pages-freshness`: reads `tools/ai/sites.json` entries with `dataPath` and checks the age against the profile's window (AC-007: release week; AC-008: release month). This reuses the registry, not the code, of `dashboard-freshness.cs`.
   - `coverage-flags-green`: from the coverage `latest.json`, every 1.0 package meets every applicable flag target (AC-007). If the published JSON does not expose per-package status, the item is `error` and the gap is reported, never assumed green.
   - `branch-protection`: `GET /repos/{o}/{r}/branches/main/protection` matches AC-020 field by field, with exactly the contexts `build`, `ci-result`, `Analyze`.
   - `spec-verified-date`: parse "Verified on **yyyy-MM-dd**" in SPEC-002 §3 and require it to be at most 30 days before `--now` (SPEC-002 AC-025).
   - `milestone-clean`: the release milestone has no open issue outside an allow-list given by the maintainer in the sign-off file.
   - `nuget-listing`: the package version is listed on nuget.org (AC-019, `final` and post-rc) and the ID prefix is reserved (`#1231`).
   - `release-asset`: the GitHub Release of the tag has one SBOM per package (SPEC-002 AC-027) and, once #92/#93 land, the attestation (`gh attestation verify`) and the signature.
3. **Manual items** are read from the sign-off file. Unticked, or ticked without an evidence link, means unanswered, and an unanswered `block` item fails.
4. **Evaluation output**: one line per item, `PASS|FAIL|WARN|SKIP|PENDING <id> <title> (<source>) — <reason>`, sorted by registry order, then a summary line. When `GITHUB_STEP_SUMMARY` is set, a Markdown table is written there too.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
You are implementing Phase 2 of the release readiness gate for Encina issue #104 (plan: docs/plans/pre-release-checklist-implementation-plan-104.md). Phase 1 created .github/release-checklist.json and the CLI skeleton of .github/scripts/release-readiness.cs.

CONTEXT:
- --collect gathers facts into facts.json; --evaluate reads only facts.json and the optional sign-off file. Evaluation never touches the network.
- GitHub data comes from REST through `gh api` (never GraphQL); public data (Pages latest.json, SonarCloud, nuget.org) through HttpClient.
- The registry's check.name selects one evaluator; params come from the registry.

TASK:
1. Implement the collectors with retry (3 attempts, exponential backoff on 5xx, timeout and secondary-rate-limit 403). A failed call writes the fact with an error field; it is never dropped.
2. Implement the evaluators listed in the plan's Phase 2 tasks (ci-run, workflow-latest, code-scanning-alerts, secret-scanning-alerts, dependabot-alerts, sonar-issues, issue-state, bug-gate, skip-scan, file-exists, file-regex, changelog-section, public-api-unshipped-empty, pages-freshness, coverage-flags-green, branch-protection, spec-verified-date, milestone-clean, nuget-listing, release-asset) and the manual-item reader for the sign-off file.
3. Implement --evaluate output: one line per item (PASS|FAIL|WARN|SKIP|PENDING), summary line, Markdown table to GITHUB_STEP_SUMMARY when set; exit 1 when any block item is FAIL or PENDING.
4. Verify which facts the Actions GITHUB_TOKEN can read (secret-scanning and Dependabot alerts may need the maintainer's token). For each such fact, document in the header that it comes from the committed facts file, and enforce --max-facts-age-hours and a version match on it.

KEY RULES:
- Fail closed: a fact with an error, a missing fact or an unparseable answer fails a block item; never default to PASS.
- AC-011 and AC-020 are checked exactly as SPEC-000 words them (union of bug label and [BUG] title; exact required contexts build, ci-result, Analyze).
- Keep every evaluator a small pure function over facts so the CRAP-style complexity stays low and each one has a self-test fixture.
- Time comes from --now when given (tests), otherwise the system clock once at start; never read the clock inside an evaluator.
- No hand-typed coverage or mutation thresholds: per-flag targets come from the published coverage data and the manifests.

REFERENCE FILES:
- docs/releases/RELEASE-PROCESS.md lines 119-182 (the CI Full and pack checks being automated)
- .github/scripts/dashboard-freshness.cs (HttpClient use, timestamp parsing, summary output)
- .github/scripts/sonar-issues.ps1 (SonarCloud API, project key)
- tools/ai/sites.json (dashboard dataPath and timestampField)
- docs/specifications/SPEC-002-eu-regulatory-readiness.md line 77 (the verification date format) and lines 147, 476
- tools/ai/plans/check-plan.ps1 from #1927, once merged (REST-only gh usage and error handling in Get-IssueTitle)
```

</details>

---

### Phase 3: CLI / Template Integration — rendered checklist, sign-off file and evidence report

> **Goal**: Produce the human-facing outputs: the checklist for the release PR, the sign-off file and the AC-022 evidence report.

<details>
<summary><strong>Tasks</strong></summary>

1. **`--render-checklist`**: writes `docs/releases/v<version>/checklist.md` for the profile.
   - Front matter matches `docs/releases/v0.13.0/README.md` (just-the-docs, `nav_exclude`).
   - Sections: automatic items (rendered as "checked by the gate", with their ids) and manual items (unticked lines with an `evidence:` placeholder).
   - The milestone allow-list block for `milestone-clean`.
2. **`--evidence`**: writes `docs/releases/v<version>/evidence.md` (SPEC-000 AC-022). For each SPEC-000 requirement group of §8 (Inventory, Build/quality gates, Defects/security, API/docs, Release, Evidence, Regulatory), the report gives:
   - the items, their verdict and the fact source URL;
   - the collection time and the SHA;
   - the medium Sonar/CodeQL findings with the disposition column taken from the sign-off file (DEC-004);
   - the AC-020 branch-protection JSON;
   - links to the coverage, mutation and benchmark dashboards. No figure is typed: the report quotes the facts verbatim and links the source.
3. **Release PR body helper**: `--render-checklist --format pr-body` prints a short Markdown block (verdict summary + link to `checklist.md` and `evidence.md`), which the release PR description includes.
4. **Facts file location**: `docs/releases/v<version>/facts.json` is committed in the release PR. It is the reproducible input of `evidence.md`, so anyone can re-run `--evidence` and get the same file.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
You are implementing Phase 3 of the release readiness gate for Encina issue #104 (plan: docs/plans/pre-release-checklist-implementation-plan-104.md). Phases 1-2 implemented the registry, collectors and evaluators in .github/scripts/release-readiness.cs.

CONTEXT:
- SPEC-000 AC-022 requires docs/releases/v1.0.0/evidence.md generated by a script from CI artifacts; no other issue implements it.
- Release notes pages live in docs/releases/v<version>/README.md; docs/releases/v0.13.0/README.md is the model.
- Manual items (adversarial review AC-023, maintainer sign-off, announcement, social preview, medium-finding dispositions, milestone allow-list) are answered in docs/releases/v<version>/checklist.md.

TASK:
1. Implement --render-checklist (file and --format pr-body) as described in the plan's Phase 3 tasks.
2. Implement --evidence: group items by the SPEC-000 section 8 requirement groups; for each item print verdict, fact source URL, retrieval time; include the AC-020 protection JSON and the medium findings with their dispositions; header with version, SHA, profile and collection time.
3. Make --evidence byte-for-byte deterministic for the same facts and sign-off file (stable ordering, invariant culture, no clock reads).

KEY RULES:
- Never type a figure that the facts do not contain (AGENTS.md section 9 and SPEC-000 INV-001); quote facts and link sources.
- Documentation output follows the encina-docs house style: just-the-docs front matter, plain English, no emojis.
- A ticked manual line without an evidence link is unanswered.

REFERENCE FILES:
- docs/releases/v0.13.0/README.md (front matter and tone)
- docs/specifications/SPEC-000-encina-1.0-baseline-and-release-scope.md section 8 (requirement groups) and AC-022, AC-023
- .claude/skills/encina-docs/SKILL.md (house style for generated pages)
```

</details>

---

### Phase 4: Configuration & CI Wiring — profiles, the release-only job and the runbook steps

> **Goal**: Make the gate block release PRs without changing the required checks of SPEC-000 REQ-020.

<details>
<summary><strong>Tasks</strong></summary>

1. **`ci.yml` job `release-readiness`** (orchestrator-owned edit, hot spot):
   - `if: startsWith(github.head_ref, 'release/')`, `runs-on: ubuntu-latest`, `timeout-minutes: 15`.
   - Job-level `permissions:` only (AGENTS.md §10): `contents: read`, `issues: read`, `pull-requests: read`, `security-events: read`, `actions: read`.
   - Steps:
     1. checkout and `setup-dotnet` with `${{ env.DOTNET_SDK_VERSION }}`;
     2. `--check-registry`;
     3. `--collect` for what the token can read, merged with the committed `docs/releases/v<version>/facts.json` for the facts it cannot read;
     4. `--evaluate --signoff docs/releases/v<version>/checklist.md`.
   - The version comes from the branch name `release/v<version>`. The profile comes from the version: `-rc.N` → `rc`, `1.0.0` → `final`, anything else → `minor`.
2. **`ci-result`**: add `release-readiness` to `needs`, and treat `skipped` as success (it skips on every non-release PR).
3. **Self-test in CI**: add `dotnet run --file .github/scripts/release-readiness.cs -- --self-test` and `--check-registry` to the `coverage-citations` job, where the other script self-tests run (`ci.yml:580-625`). Running on every PR keeps the registry valid between releases.
4. **`workflow_dispatch` dry run**: a manual trigger input `release_profile` on `ci.yml`, or a tiny `release-readiness-dry-run.yml` with job-level permissions. Either runs `--collect` + `--evaluate` against `main` for a chosen profile, never blocks anything, and lets the maintainer see the distance to rc on demand.
5. **Label**: the orchestrator creates the `deferred-1.0` label that SPEC-000 AC-011 refers to:
   `gh label create deferred-1.0 --repo dlrivada/Encina --description "Open bug consciously deferred past 1.0 (SPEC-000 AC-011); needs a reason comment" --color BFD4F2`.
6. **Runbook**: `RELEASE-PROCESS.md` gains:
   - in "Before you start", the local `--collect` / `--render-checklist` commands;
   - in step 1, committing `checklist.md`, `facts.json` and `evidence.md` in the release PR;
   - in step 2, `--evaluate --only ci-full-on-sha` on the merge SHA, which replaces the hand-run `gh run view … --jq` check at lines 138-142.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
You are implementing Phase 4 of the release readiness gate for Encina issue #104 (plan: docs/plans/pre-release-checklist-implementation-plan-104.md). The tool .github/scripts/release-readiness.cs and the registry .github/release-checklist.json exist.

CONTEXT:
- SPEC-000 REQ-020/AC-020: main's required checks are exactly build, ci-result and CodeQL Analyze. A new required check is not allowed; ci-result aggregates jobs.
- The runbook requires release branches named release/v<version> (docs/releases/RELEASE-PROCESS.md line 51).
- .github/workflows/ci.yml is a shared hot spot: in the orchestrated workflow only the orchestrator edits it. If you are a worker, prepare the exact YAML as a patch file under artifacts/ and report it instead of editing ci.yml.

TASK:
1. Add job release-readiness to ci.yml (if: startsWith(github.head_ref, 'release/')), job-level permissions only, steps: checkout, setup-dotnet, --check-registry, --collect merged with the committed facts.json, --evaluate with the sign-off file. Derive version and profile from the branch name.
2. Add release-readiness to ci-result's needs and treat skipped as success.
3. Add --self-test and --check-registry steps to the coverage-citations job.
4. Add a non-blocking workflow_dispatch dry run (input: profile).
5. Update docs/releases/RELEASE-PROCESS.md: before-you-start collection, committing checklist.md/facts.json/evidence.md in step 1, and --evaluate --only ci-full-on-sha in step 2 replacing the manual jq check.

KEY RULES:
- GitHub Actions permissions are declared per job, never at workflow level (AGENTS.md section 10).
- Do not change branch protection or the list of required checks.
- Never skip hooks; a blocked step is reported with the hook's message.
- Commit messages in English, conventional format, no AI attribution.

REFERENCE FILES:
- .github/workflows/ci.yml (jobs coverage-citations at line 580, ci-result at line 716)
- docs/releases/RELEASE-PROCESS.md
- docs/specifications/SPEC-000-encina-1.0-baseline-and-release-scope.md REQ-020, AC-020
```

</details>

---

### Phase 5: Cross-Cutting Integration — validation, resilience, idempotency and the audit record

> **Goal**: Apply the four transversal functions marked ✅ in the matrix to the tool.

<details>
<summary><strong>Tasks</strong></summary>

1. **Validation (✅)**:
   - `--check-registry` validates the schema, unique ids, known evaluator names, `params` per evaluator, profile values, and SPEC coverage. For SPEC coverage, the AC identifiers are read from the SPEC-000 §4 and SPEC-002 AC tables at run time, never hard-coded.
   - `--collect` validates `--version` against SemVer 2.0.0 and `--sha` against 40 hex characters.
   - `--evaluate` validates the facts schema version and rejects facts collected for another version or older than `--max-facts-age-hours`.
2. **Resilience (✅)**: centralize the retry policy of Phase 2 in one helper, honour `Retry-After` and `x-ratelimit-reset`, and record the attempt count in the fact. After the last attempt the fact carries `error`, and the item fails closed.
3. **Idempotency (✅)**: the same facts plus the same sign-off file give byte-identical `--evaluate` output and `evidence.md`. A self-test asserts it by running twice and comparing hashes.
4. **Audit trail (✅)**: `facts.json`, `checklist.md` and `evidence.md` are committed together in the release PR, and the PR merge records who answered each manual item. The evidence report states the collector identity (`gh api user --jq .login` locally, `github-actions` in CI) and the collection time.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
You are implementing Phase 5 of the release readiness gate for Encina issue #104 (plan: docs/plans/pre-release-checklist-implementation-plan-104.md).

CONTEXT:
- The plan's Cross-Cutting Integration Matrix marks Validation, Resilience, Idempotency and Audit Trail as included; the other eight functions are not applicable to a repository tool.
- The tool is .github/scripts/release-readiness.cs with the registry .github/release-checklist.json.

TASK:
1. Validation: complete --check-registry (schema, unique ids, evaluator names and params, profiles, SPEC coverage read from the SPEC-000 section 4 and SPEC-002 AC tables at run time); validate --version (SemVer 2.0.0) and --sha; reject facts for another version or older than --max-facts-age-hours.
2. Resilience: one retry helper (3 attempts, exponential backoff, Retry-After and x-ratelimit-reset honoured), attempt count recorded in each fact, fail closed after the last attempt.
3. Idempotency: make --evaluate and --evidence deterministic and add a self-test that runs each twice on the same fixture and compares SHA-256 hashes.
4. Audit trail: record the collector identity and collection time in facts.json and print them in evidence.md.

KEY RULES:
- Fail closed everywhere (AGENTS.md section 3).
- No clock reads inside evaluators; --now drives tests.
- Keep methods small; a single switch that maps evaluator names may carry the crap-exempt marker described in AGENTS.md section 9 only if it answers one question.

REFERENCE FILES:
- .github/scripts/dashboard-freshness.cs (--now handling, registry validation)
- docs/specifications/SPEC-000-encina-1.0-baseline-and-release-scope.md sections 4 and 8
- docs/specifications/SPEC-002-eu-regulatory-readiness.md lines 470-481 (AC table format)
```

</details>

---

### Phase 6: Testing — self-test fixtures for every evaluator and every profile

> **Goal**: Prove every evaluator and the fail-closed rule offline, in CI, on every PR.

<details>
<summary><strong>Tasks</strong></summary>

1. **Fixtures** under `.github/scripts/fixtures/release-readiness/` (the `fixtures/` folder already holds `crap-gate` and `link-health`):
   - `registry-valid.json`, `registry-duplicate-id.json`, `registry-unknown-evaluator.json`, `registry-missing-ac.json`;
   - a minimal SPEC-000/SPEC-002 AC excerpt for the coverage check;
   - for each evaluator, one passing and one failing facts file;
   - `facts-error.json`: a fact with `error` on a block item, which must FAIL;
   - `facts-stale.json` (older than the limit) and `facts-wrong-version.json`;
   - sign-off fixtures: ticked with evidence, ticked without evidence, and unticked.
2. **Self-test cases** (`--self-test`, the same convention as `coverage-report.cs --self-test` and `generate-coverage-manifest.cs --self-test`). Each case asserts the verdict per item and the exit code:
   - `minor` passes with a clean fixture and fails on a law-map date 31 days old;
   - `rc` fails on one open high CodeQL alert;
   - `final` fails on one non-empty Unshipped file;
   - AC-011 fails on a `[BUG]`-titled issue without the label, and on a `security`-labelled issue even when it carries `deferred-1.0`;
   - AC-020 fails on an extra required context;
   - determinism: identical hashes across two runs;
   - `--render-checklist` output matches a golden file.
3. **Coverage obligations**: no file under `src/` changes, so no coverage manifest applies (AGENTS.md §9 obligations are per `src/` file). The tool's assurance is the self-test, which runs in the `coverage-citations` job (Phase 4).
4. **Load and benchmark tests**: not applicable. There is no `src/` package and no hot path, so the test-type justification files of AGENTS.md §9 (which live under the `tests/` projects for `src/` features) do not apply.
5. **First live run**: run `--collect` and `--evaluate --profile minor` against the `v0.14.0` release PR (or a dry run on `main` if that release has already been cut). Record the run in the PR and fix false positives before closing #104.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```text
You are implementing Phase 6 of the release readiness gate for Encina issue #104 (plan: docs/plans/pre-release-checklist-implementation-plan-104.md).

CONTEXT:
- Scripts under .github/scripts prove themselves with a --self-test mode run in CI (ci.yml lines 596-625: generate-coverage-manifest.cs and coverage-report.cs). Fixtures live under .github/scripts/fixtures/<tool>/.
- --evaluate and --evidence are offline, so every case is a facts fixture plus an optional sign-off fixture.

TASK:
1. Create .github/scripts/fixtures/release-readiness/ with registry fixtures (valid, duplicate id, unknown evaluator, missing AC), an AC excerpt, one pass and one fail facts fixture per evaluator, error/stale/wrong-version facts, and three sign-off fixtures.
2. Implement --self-test cases listed in the plan's Phase 6 tasks, including the AC-011 and AC-020 exact-wording cases, the 31-day law-map case and the determinism hash case; print one "SELFTEST ok|FAIL <case>" line each and exit 1 on any failure.
3. Run --self-test and --check-registry locally and paste the output in your report.

KEY RULES:
- Tests are deterministic: --now fixed, no network, no Thread.Sleep.
- Every evaluator has at least one passing and one failing case; the fail-closed rule has its own case.
- No file under src/ changes, so no coverage manifest entry is needed; do not add one.

REFERENCE FILES:
- .github/scripts/coverage-report.cs (--self-test structure)
- .github/scripts/crap-gate-selftest.ps1 and .github/scripts/fixtures/crap-gate/ (fixture layout)
- tools/ai/plans/check-plan.ps1 from #1927, once merged (Invoke-SelfTest case table with expected failures)
```

</details>

---

### Phase 7: Documentation & Finalization

> **Goal**: Document the gate, record the decision, retire the stale checklist and close #104.

<details>
<summary><strong>Tasks</strong></summary>

1. **ADR** `docs/architecture/adr/037-release-readiness-gate.md` (037 is the next free number in `docs/architecture/adr/index.md` today; reserve it in the plan PR). It records:
   - the registry, the profiles and the fail-closed facts snapshot;
   - the enforcement through `ci-result` that keeps REQ-020's required checks unchanged;
   - the maintainer's answers to the Design Choices.
2. **`docs/releases/RELEASE-PROCESS.md`**: the Phase 4 changes, plus a "Release readiness gate" section explaining the three profiles and the sign-off file. Written by `docs-writer` with the encina-docs skill.
3. **`docs/releases/README`-level how-to** (new page `docs/releases/release-readiness.md`, how-to quadrant): how to add an item to the registry, how to retire one, and how to answer a manual item.
4. **`.opencode/skills/release-checklist/SKILL.md`**: replace the duplicated checklist (lines 10-70) with "run the gate" instructions. Coordinate with #1655, which already tracks the 85 % lines there; if #1655 lands first, only the pointer to the gate is added.
5. **`.github/scripts/README-workflows.md`**: add `release-readiness.cs` to the script list.
6. **Changelog fragment** `changelog.d/104-release-readiness-gate.added.md`: the gate and the evidence report (no edit of `CHANGELOG.md`).
7. **`ROADMAP.md`**: mark #104's line in the Release Engineering milestone if it is listed. **`docs/INVENTORY.md`**: no change (no package or module added).
8. **PublicAPI files, XML docs**: not applicable (no `src/` code).
9. **Verification**:
   - `dotnet run --file .github/scripts/release-readiness.cs -- --self-test` and `-- --check-registry` exit 0;
   - `dotnet build Encina.slnx --configuration Release` is unchanged at 0 warnings (no `src/` change);
   - markdownlint and the link check pass on the changed pages.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```text
You are implementing Phase 7 of the release readiness gate for Encina issue #104 (plan: docs/plans/pre-release-checklist-implementation-plan-104.md). The tool, registry, CI wiring and self-tests are done.

CONTEXT:
- Documentation pages under docs/ are written by the docs-writer agent with the encina-docs skill (Diataxis: one quadrant per page).
- ADR numbers: take the next free number in docs/architecture/adr/index.md (037 when this plan was written) and reserve it in the reserved-numbers table if not yet done.
- #1655 tracks the 85% coverage lines in agent and skill instructions, including .opencode/skills/release-checklist/SKILL.md.

TASK:
1. Write ADR 037 "Release readiness gate" with the alternatives of the plan's Design Choices and the maintainer's decisions.
2. Update docs/releases/RELEASE-PROCESS.md (profiles, sign-off file, gate commands in steps 1 and 2) and add docs/releases/release-readiness.md (how-to: add, retire and answer items).
3. Replace the checklist in .opencode/skills/release-checklist/SKILL.md with instructions to run the gate.
4. Add release-readiness.cs to .github/scripts/README-workflows.md.
5. Add changelog.d/104-release-readiness-gate.added.md.
6. Run --self-test, --check-registry, markdownlint and the link check; report the output.

KEY RULES:
- Never edit the [Unreleased] section of CHANGELOG.md by hand; use a changelog.d fragment.
- No hand-typed coverage or mutation figures in any page (SPEC-001).
- English only; no emojis; link SPEC-000, SPEC-002 and the ADR instead of restating them.
- The PR body says "Fixes #104" only if the maintainer chose the split of Design Choice 7; otherwise "Refs #104".

REFERENCE FILES:
- docs/architecture/adr/index.md and docs/architecture/adr/036-three-audit-stores.md (ADR format)
- docs/releases/RELEASE-PROCESS.md
- .claude/skills/encina-docs/SKILL.md
- changelog.d/README.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard / specification | Relevance to the gate |
|---|---|
| SPEC-000 §3 REQ-001 – REQ-029, §4 AC-001 – AC-029, §8 verification | The authoritative list of what must hold for `1.0.0-rc.1` and `1.0.0`; each AC is one or more registry items |
| SPEC-000 DEC-004 | Thresholds: NuGet audit at moderate or higher, static analysis at high or higher, medium findings dispositioned |
| SPEC-000 DEC-005 | One minor version per 1.0 block before `1.0.0-rc.1`, which is why the `minor` profile exists |
| SPEC-002 REQ-025 – REQ-030, AC-025 – AC-030 | Law-map date within 30 days of each minor tag and the rc; legal status; per-package SBOM; SECURITY.md; GHSA process; Trusted Publishing and Scorecard |
| Semantic Versioning 2.0.0 | Version validation, pre-release ordering (`1.0.0-rc.1` < `1.0.0`) |
| Keep a Changelog 1.1.0 | Section format checked by `changelog-section` (already produced by `changelog-fragments.cs`) |
| NuGet.org publishing guidance and ID-prefix reservation | `nuget-listing` item (AC-019, #100, #1231) |
| SLSA v1.0 Build L2, GitHub artifact attestations, Sigstore | `release-asset` attestation and signature checks once #92 and #93 land |
| CycloneDX 1.6 / SPDX 2.3 | SBOM presence per package (SPEC-002 AC-027, #1228) |
| GitHub REST: code scanning, secret scanning, Dependabot alerts, branch protection, issues, Actions runs | The fact sources of the `auto` items |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|---|---|---|
| Manual release runbook | `docs/releases/RELEASE-PROCESS.md` | The gate plugs into "Before you start", step 1 and step 2 |
| Changelog tooling | `.github/scripts/changelog-fragments.cs` | Produces the section that `changelog-section` checks |
| Dashboard registry | `tools/ai/sites.json` | `pages-freshness` reads `dataPath` and `timestampField` from it |
| Freshness checker | `.github/scripts/dashboard-freshness.cs` | Pattern for `--check-registry`, `--now`, HttpClient use |
| SonarCloud query | `.github/scripts/sonar-issues.ps1` | Project key and API shape for `sonar-issues` |
| Script self-tests in CI | `ci.yml:596-625` (`coverage-citations` job) | Where `--self-test` and `--check-registry` run on every PR |
| Required-check aggregator | `ci.yml:716` (`ci-result`) | Makes the release-only job blocking without a new required check |
| CI Full pack gate | `ci-full.yml:531-580` (#1745) | The publish path whose SHA the pre-tag check verifies |
| Plan gate (structure precedent; #1927, not yet on `main`) | `tools/ai/plans/check-plan.ps1` | REST-only `gh` usage, fixture-based self-test with expected failures |
| Secret scanning + push protection | repository settings (enabled) | Replaces the issue's `git-secrets` item |
| Dependabot | `.github/dependabot.yml` | `dependabot-alerts` item |
| SBOM workflow | `.github/workflows/sbom.yml` | Today's single SBOM; the per-package release asset is #1228 |

### Event ID Allocation

| Package | Range | Notes |
|---|---|---|
| None | — | The gate is a repository tool under `.github/scripts/`; it adds no `[LoggerMessage]` and no `src/` code, so `src/Encina/Diagnostics/EventIdRanges.cs` is unchanged |

### Estimated File Count

| Category | Files | Notes |
|---|---|---|
| Tool | 1 | `.github/scripts/release-readiness.cs` (~1,100-1,400 lines) |
| Registry | 1 | `.github/release-checklist.json` (~70 items) |
| Self-test fixtures | ~25 | `.github/scripts/fixtures/release-readiness/` |
| CI | 1-2 | `ci.yml` (job + self-test step), optional dry-run workflow |
| Documentation | 5 | ADR 037, `RELEASE-PROCESS.md`, `release-readiness.md`, `README-workflows.md`, `.opencode/skills/release-checklist/SKILL.md` |
| Changelog fragment | 1 | `changelog.d/104-release-readiness-gate.added.md` |
| **Total** | **~34-35** | No `src/` file |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing the release readiness gate for Encina issue #104 ("Final pre-release checklist"). Plan: docs/plans/pre-release-checklist-implementation-plan-104.md.

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library, pre-1.0, released by hand (docs/releases/RELEASE-PROCESS.md) until #1100 automates releases.
- SPEC-000 defines 1.0 through acceptance criteria AC-001..AC-029; SPEC-002 adds AC-025..AC-030 (law-map re-verification before each minor tag and the rc, legal status, SBOM per package, SECURITY.md, advisories, Trusted Publishing).
- Main's required checks are exactly build, ci-result and CodeQL Analyze (SPEC-000 REQ-020); ci-result aggregates jobs.
- Scripts are C# 14 file-based apps or PowerShell only; no python, no bash constructs (AGENTS.md section 2).
- Gates fail closed (AGENTS.md section 3). No hand-typed coverage or mutation figures (AGENTS.md section 9, SPEC-001).

IMPLEMENTATION OVERVIEW:
Phase 1: registry .github/release-checklist.json (id, title, source, owner, kind, check, profiles, retired), facts schema, CLI contract of .github/scripts/release-readiness.cs, --check-registry.
Phase 2: collectors (gh api REST + HttpClient, retry) and evaluators (ci-run, workflow-latest, code/secret/dependabot alerts, sonar, issue-state, bug-gate AC-011, skip-scan AC-006, file checks, changelog-section AC-017, public-api-unshipped-empty AC-013, pages-freshness AC-007/008, coverage-flags-green, branch-protection AC-020, spec-verified-date SPEC-002 AC-025, milestone-clean, nuget-listing, release-asset); manual items from the sign-off file.
Phase 3: --render-checklist (checklist.md, PR body), --evidence (docs/releases/v<version>/evidence.md, SPEC-000 AC-022), committed facts.json.
Phase 4: ci.yml job release-readiness on release/* heads feeding ci-result; self-test step in coverage-citations; workflow_dispatch dry run; deferred-1.0 label; runbook steps.
Phase 5: validation, resilience (retry), idempotency (deterministic output), audit trail (collector identity).
Phase 6: fixtures and --self-test cases for every evaluator, each profile, fail-closed and determinism; first live run on the v0.14.0 release.
Phase 7: ADR 037, RELEASE-PROCESS.md, how-to page, .opencode release-checklist skill pointer, README-workflows.md, changelog fragment.

KEY PATTERNS:
- Two stages: --collect writes facts.json; --evaluate/--evidence read only facts and the sign-off file (offline, deterministic, --now for time).
- Three profiles: minor (each vX.Y.0 of the 1.0 sequence), rc (1.0.0-rc.N), final (1.0.0); each item block | warn | skip per profile.
- An error fact or an unanswered manual block item fails; exit 0 pass, 1 fail, 2 usage/registry error.
- Items owned by other issues are checked through artifacts or issue state, never implemented here.
- GitHub Actions permissions per job; ci.yml is edited by the orchestrator.

REFERENCE FILES:
- docs/specifications/SPEC-000-encina-1.0-baseline-and-release-scope.md (sections 3, 4, 8, 9)
- docs/specifications/SPEC-002-eu-regulatory-readiness.md (line 77, line 147, lines 310-327, lines 476-481)
- docs/releases/RELEASE-PROCESS.md
- .github/scripts/dashboard-freshness.cs, .github/scripts/changelog-fragments.cs, .github/scripts/coverage-report.cs
- tools/ai/sites.json; tools/ai/plans/check-plan.ps1 (#1927, once merged)
- .github/workflows/ci.yml (coverage-citations, ci-result), .github/workflows/ci-full.yml (pack)
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | A one-shot command run a few times per release; every fact is read once per run and snapshotted in `facts.json`, which is the only reuse needed |
| 2 | OpenTelemetry | ❌ N/A | Repository tooling, not library code; nothing runs in an application process, and the CI job log plus `evidence.md` are the trace |
| 3 | Structured Logging | ❌ N/A | No `ILogger` or EventIds outside `src/`; the tool prints one stable, parseable line per item (`PASS <id> …`) and a step summary table |
| 4 | Health Checks | ❌ N/A | No runtime dependency to probe; site availability is the job of the site-health skill and `dashboard-freshness.cs`, which the gate reads |
| 5 | Validation | ✅ Include | Registry validation (`--check-registry`, SPEC coverage), argument validation (SemVer, SHA), facts version and age validation (Phase 5) |
| 6 | Resilience | ✅ Include | Retry with backoff and rate-limit awareness on every GitHub, Pages, SonarCloud and nuget.org call; fail closed after the last attempt (Phases 2 and 5) |
| 7 | Distributed Locks | ❌ N/A | A single process run per release PR or dispatch; no shared mutable state |
| 8 | Transactions | ❌ N/A | Read-only against every remote system; it writes only local files that the release PR commits |
| 9 | Idempotency | ✅ Include | The same facts and sign-off file give byte-identical verdicts and evidence; asserted by a self-test (Phase 5) |
| 10 | Multi-Tenancy | ❌ N/A | No tenant data; one repository, one release |
| 11 | Module Isolation | ❌ N/A | No modules; repository-level tool |
| 12 | Audit Trail | ✅ Include | `facts.json`, `checklist.md` and `evidence.md` committed together in the release PR, with collector identity and time, are the release's audit record (SPEC-000 AC-022, AC-023) |

---

## Prerequisites & Dependencies

### Issues the gate depends on (checked, not implemented here)

| Issue | State | What the gate checks |
|---|---|---|
| #1362 package manifest (AC-001/AC-002) | open | Manifest exists and its drift check is green |
| #1651 per-flag coverage gate | open | Published coverage data exposes per-package, per-flag status for `coverage-flags-green` |
| #92, #93 provenance and signing | open | `release-asset` attestation and signature |
| #100, #101, #1231 NuGet reservation, publish, Trusted Publishing | open | `nuget-listing`, prefix |
| #102, #103 package metadata, branding | open | Licence expression, icon, readme metadata; branding assets |
| #1100 CI release workflow | open | Hosts the two gate calls when releases are automated |
| #1228 per-package SBOM | open | SBOM per package on the Release |
| #1229, #1230 SECURITY.md content, GHSA process | open | SPEC-002 AC-026, AC-028, AC-029 |
| #95, #96, #97 contributing, conduct, templates (AC-021) | open | issue state |
| #1655 stale 85 % targets in agent and skill instructions | open | Coordinate the `.opencode` skill edit in Phase 7 |

### New issues drafted with this plan

Files under `artifacts/issues/` of the plan worktree:

- `plan-104-global-json.md`: `[INFRA]` Pin the .NET SDK with `global.json` (SPEC-000 REQ-020/AC-020; no issue tracks it, and #98 is closed).
- `plan-104-dependency-licences.md`: `[INFRA]` Dependency licence compatibility check and third-party notices for the published packages (issue #104 "Legal Checklist"; no owner).
- `plan-104-workflow-permissions.md`: `[DEBT]` Six workflows declare `permissions:` at workflow level (AGENTS.md §10). `sbom.yml` is #1228 and `release-on-milestone.yml` is #1100.
- `plan-104-cut-1-0-release.md`: `[INFRA]` Cut `1.0.0-rc.1` and `1.0.0` through the readiness gate. Opened only if the maintainer picks option C of Design Choice 7.

### Ordering

The gate itself has no prerequisite: it can land now and report red items until their owners land. Phases 1-6 should merge before the `v0.14.0` tag, so that its first live run is the `minor` profile on that release.

---

## Next Steps

1. The maintainer answers Design Choices 1-8; the orchestrator records the answers in a "Maintainer Decisions" section and reserves ADR 037 in `docs/architecture/adr/index.md` in the plan PR.
2. The orchestrator opens the drafted issues (the fourth only if Design Choice 7 is C) and links this plan from #104.
3. One `issue-worker` implements Phases 1-3, 5 and 6 in its own worktree. The orchestrator applies the Phase 4 `ci.yml` patch and creates the `deferred-1.0` label. `docs-writer` writes the Phase 7 pages.
4. First live run on the `v0.14.0` release PR; false positives fixed in the same PR.
5. The PR closes #104 (`Fixes #104` under option C of Design Choice 7, `Refs #104` otherwise).
