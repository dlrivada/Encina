---
nav_exclude: true
---

# CRAP gate — CI wiring design note

**Status:** design note. `crap-gate.cs` exists; its behavior (diff parsing, the exemption comment, `--report`/`--enforce` exit codes) was verified manually against the fixtures under `.github/scripts/testdata/crap-gate/` during development. Sections 1–3 and 5 describe the state before wiring and are otherwise unchanged; the gate is now wired into `.github/workflows/ci.yml` as the `crap-gate` job, enforcing from day one (see §4), and `.github/scripts/crap-gate-selftest.ps1` now runs the fixtures in that job, so the "no automated test" gap noted in [§5](#5-open-questions--follow-ups) is closed.
**Issue:** #1346
**Audience:** whoever wires `crap-gate.cs` into CI next — this note exists so that decision does not have to re-derive what `ci.yml` already does.

## 1. What the gate does today

`.github/scripts/crap-gate.cs` takes a diff (the output of `git diff -U0 <base>...HEAD`) and one or more Cobertura files, and reports every method the diff touches whose CRAP score exceeds a threshold (default 10). `--report` always exits `0`; `--enforce` exits `1` when a non-exempt violation exists. The formula, the combined-coverage rationale, the exemption comment convention and the JSON/markdown reporting in `coverage-report.cs` are documented in [`docs/testing/coverage-measurement-methodology.md`](../testing/coverage-measurement-methodology.md#crap-change-risk-anti-patterns) — this note does not repeat them. What matters here is only the gate's inputs: it needs (a) a diff and (b) Cobertura XML with per-method `<lines>` and a `complexity` attribute, covering the files the diff touches.

## 2. Where the Cobertura input comes from

Three options were evaluated against what `ci.yml` (the required, per-PR workflow — distinct from `ci-full.yml`, which is the weekly/manual/release workflow that runs `coverage-report.cs` and updates the dashboard) actually does today.

### (a) Reuse the coverage artifacts `ci.yml` already produces on every PR

`ci.yml` already collects Cobertura coverage on every pull request, per flag, as a normal part of its required jobs — this is not something that would need to be added:

- `test-unit` (8 parallel shards, `--collect "XPlat Code Coverage"`, uploaded as `test-results-UnitTests-<Shard>`)
- `test-integration` (per-provider shards, `test-results-IntegrationTests-<Shard>`)
- `test-contract` (`test-results-ContractTests`)
- `test-property` (`test-results-PropertyTests`)
- `test-guard` (`test-results-GuardTests`)
- `test-ef-providers` (per-database matrix, `test-results-EFCore-<Database>`)

Each of these jobs also uploads to Codecov (`codecov/codecov-action@v7`, `fail_ci_if_error: false`), but that upload is for Codecov's own dashboard, not something `crap-gate.cs` can read back from — the artifact the job attaches to the workflow run (`actions/upload-artifact@v7`) is the reusable input, and it contains the same `coverage.cobertura.xml` files under `artifacts/test-results/<name>/`.

Critically, `test-unit`, `test-integration`, `test-guard` and `test-contract` are all gated on the *same* `changes` job output (each of their `paths-filter` entries lists `src/**` alongside its own test folder — see the `changes` job in `ci.yml`), so **any PR that touches `src/` runs all of them**, not just the ones for the area it touched. `test-ef-providers` is the one exception, gated only on `src/Encina.EntityFrameworkCore/**` plus its own integration folder — a PR outside EF Core will not have EF-flavoured coverage data, which only matters for methods in that package.

The existing required job `coverage-citations` shows the pattern for a lightweight job that runs on every PR, docs-only included, without needing build output or coverage data — a `crap-gate` job would instead need `needs: [test-unit, test-integration, test-contract, test-property, test-guard, test-ef-providers]` and `if: always()`, download every `test-results-*` artifact from the *same* run (`actions/download-artifact@v8` with a glob pattern and `merge-multiple: false`, one directory per artifact), and produce the diff itself with `git diff -U0 ${{ github.event.pull_request.base.sha }}...${{ github.event.pull_request.head.sha }}` (the `changelog-fragments` job already does an equivalent base/head diff with `fetch-depth: 0`, so the pattern exists in this workflow already).

**Tradeoff:** no new test execution — the only added CI cost is `crap-gate.cs` parsing the downloaded Cobertura files and the diff, which has not been timed against a real run (see §5). The gap: a PR that changes only docs, `.github/`, or an area none of the `paths-filter` entries cover skips every test job, so there is no coverage data for the gate to read — but there is also no changed method to gate in that case, so `--report`/`--enforce` degrade to "nothing to check", which is the correct behavior, not a failure mode to work around.

### (b) A dedicated, affected-files-only coverage run just for the gate

A new CI step running `dotnet test --filter <touched-namespaces>` with coverage collection, purely to feed `crap-gate.cs`, computing "affected" from the same diff the gate already parses (e.g., mapping changed `src/<Package>/...` paths to the corresponding `tests/Encina.*Tests/<Namespace>` filter, mirroring the shard filters already hand-written in `ci.yml`'s `test-unit` matrix).

**Tradeoff:** this duplicates test execution that (a) already gets for free from the required jobs — CI Full already runs ~40 minutes for the full per-flag suite (see `CLAUDE.md`, Testing Standards); adding a second, narrower `dotnet test` invocation to `ci.yml` is strictly additive CI time for data (a) already has, unless (a) turns out to have a real coverage gap (see the `test-ef-providers` exception above) that only affects the EF Core package specifically. It also needs its own "what changed" → "what to filter" mapping, which is new logic to maintain, whereas (a) reuses filters the jobs already declare. (b) is only worth its cost as a *narrow* supplement to (a), not a replacement for it.

### (c) Last published coverage from GitHub Pages, plus fresh data for touched files

Fetch the last `docref-index.json` / Cobertura-derived data Publish Coverage put on `gh-pages` for everything the diff does not touch, and combine it with fresh data (from (a) or (b)) for what it does touch.

**Tradeoff:** this cannot work alone for the gate's actual purpose. The gate exists to catch complex, undertested code *in the diff* — by definition, a method a PR adds or substantially rewrites has no prior entry on Pages (or an entry describing code that no longer exists at those line numbers). (c) could theoretically reduce the Cobertura payload needed for methods the diff touches only incidentally (e.g., a line-range shift from an unrelated edit earlier in the same file) by falling back to the last known score for lines the diff didn't change — but `crap-gate.cs`'s method-matching is already keyed by method identity (file + class + name + signature), not by the surrounding file's line numbers, so an untouched method never needs re-scoring in the first place. (c) adds staleness risk (Pages coverage is only as fresh as the last successful `main` run) for no benefit the gate does not already get from (a).

## 3. Recommendation

**(a), unmodified**, wired as a new job in `ci.yml` that runs after the six flag jobs. The data already exists on every PR that touches `src/` — which is the only case the gate needs to run for — at zero additional test-execution cost. (b) is worth revisiting only if the EF Core gap in (a) (`test-ef-providers` not gated on general `src/**`) turns out to hide gate-relevant methods in practice; that is a narrow, provider-specific follow-up, not a reason to build a general affected-files runner up front. (c) does not solve a problem the gate has.

## 4. Rollout plan (decided)

Blocking from day one (maintainer decision, 2026-09-25). There is no report-only period: the `crap-gate` job in `ci.yml` runs `--enforce --threshold 10` from the first PR that merges it, and it is in `ci-result`'s `needs:` list alongside `coverage-citations` and `changelog-fragments`, so a non-exempt violation blocks the merge immediately, the same way those two checks already do. This supersedes an earlier draft of this section, written before `ci.yml` was wired, which proposed a week of `--report`-only output before switching to `--enforce`; the maintainer rejected the phased rollout and chose to ship the gate enforcing from the start, matching the precedent `coverage-citations` and `changelog-fragments` already set (both shipped directly as enforcing checks, with no report-only period).

The job runs only for a pull request where `needs.changes.outputs.src == 'true'`, after the six flag jobs (`test-unit`, `test-integration`, `test-contract`, `test-property`, `test-guard`, `test-ef-providers`) whose Cobertura artifacts it downloads, and it runs `.github/scripts/crap-gate-selftest.ps1` against the script's own fixtures first (closing #1354), so a broken `crap-gate.cs` fails loudly instead of silently passing every PR — see `.github/workflows/ci.yml`, the `crap-gate` job.

## 5. Open questions / follow-ups

- **The `test-ef-providers` gap** (§2a): a PR that changes `src/Encina.EntityFrameworkCore/**` in a way that also touches shared `src/Encina.Messaging/**` code gets EF-flavoured coverage; a PR that changes only, say, `src/Encina.Dapper/**` does not get EF Core's coverage, which is correct (nothing in Dapper needs EF's data) but worth confirming this reasoning holds once the gate has run against a few real Dapper/ADO-only PRs.
- **Artifact size at `download-artifact` time**: `test-unit` alone uploads 8 shard artifacts; combined with integration, guard, contract, property and EF Core matrices, a `crap-gate` job downloads roughly 15–20 artifacts per run. This is expected to be fast (Cobertura XML is small relative to the `build-output` artifact the same workflow already downloads in every test job), but has not been measured against a real `crap-gate` job — worth a first-run timing check once wired in.
- **Where the `--threshold` value lives**: this note assumes the CI-wired job keeps the script's own default (10). If the maintainer wants a different threshold per rollout phase (e.g., a looser threshold during report-only, tightening at the same time as the `--enforce` switch), that is an additional decision this note does not make.
- **No automated test for `crap-gate.cs`**: the fixtures under `.github/scripts/testdata/crap-gate/` are hand-authored sample data with hand-computed expected values in a comment, and the script's behavior (diff parsing, the exemption comment, `--report`/`--enforce` exit codes) was only checked manually against them during development. There is no test project or CI step that runs `crap-gate.cs` and asserts an outcome. Adding one (e.g., a small script or test that invokes `crap-gate.cs` against the fixtures and asserts the documented exit codes) is a follow-up worth doing before or alongside wiring the gate into `ci.yml`.
- **Whether the gate job blocks the same `ci-result` required check** that `coverage-citations` and `changelog-fragments` feed today, or is a separate, optionally-required check — this determines whether the report-only period is even observable as "non-blocking" from a contributor's point of view (a job that is not in `ci-result`'s `needs:` list never blocks regardless of its own exit code, which may be a simpler way to implement "report-only" than teaching the script to always exit 0 during that phase).
