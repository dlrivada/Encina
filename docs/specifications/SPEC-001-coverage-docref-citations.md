# SPEC-001 — DocRef citations for coverage

| | |
|---|---|
| **Status** | 🟢 APPROVED — maintainer approved the design on 2026-09-22 |
| **Author** | Specifier (Claude) |
| **Date** | 2026-09-22 |
| **Tracking** | #1092 (milestone v0.22.0, `ai:local-candidate`) |
| **Evidence** | `.github/scripts/mut-docs-render.cs`, `perf-docs-render.cs`, `mutation-history.cs`, `coverage-report.cs`, `.github/workflows/publish-coverage.yml`, `publish-mutations.yml`, `ci-full.yml`, `docs/coverage/app.js`, `docs/mutations/app.js` (read 2026-09-22) |
| **Supersedes** | — |

> First feature run end to end through the SDD flow (`docs/engineering/AI-DEVELOPMENT-MODEL.md` §5–§13). The design lives in #1092; this document states what must be true and how it is verified. Design choices that turn out to need a real trade-off go to an ADR.

## 1. Problem

Documentation quotes coverage figures by hand, and they drift (#1090). Benchmarks and mutation testing already have a citation system (DocRef IDs, marker blocks expanded by a renderer, an index published with the dashboard, a reverse `cited-by` index surfaced on the dashboard). Coverage has none: `coverage-report.cs` computes per-file, per-flag results but publishes only per-package data, and no renderer exists.

## 2. Requirements

- **REQ-001 (parity)** Coverage citations use the same grammar and behaviour as mutation citations: an ID prefix (`cov:<Package>/<relative-path>.cs`), a block marker with a glob and an inline marker with `id:field`, expansion that never touches content outside markers or inside fenced code blocks, a `⚠` fallback for unmatched globs and missing IDs/fields, and a `cited-by.json` in the same format (DocRef → `file:line`).
- **REQ-002 (index)** Every CI Full run emits `docref-index.json` for coverage with one entry per source file that has at least one applicable flag in its manifest, carrying `package, path, coverage, obligations, metObligations, flags, noData, perFlag{flag: total, covered, coverage, target, noData}, lastRun, dashboardUrl`.
- **REQ-003 (stable IDs)** The set of IDs is derived from the coverage manifests, not from which files happened to produce Cobertura data in the run. `noData` is defined per flag: `perFlag[flag].noData` is `true` when the run produced no Cobertura report for that flag (then `total`, `covered` and `coverage` are `null` and the flag is excluded from `obligations`, `metObligations` and the file-level `coverage`); the file-level `noData` is `true` only when every applicable flag has `noData: true`. An ID disappears only when the file is removed or its manifest entry has no applicable flags.
- **REQ-004 (publishing)** `publish-coverage` publishes the index and `cited-by.json` to Pages, renders the markers in `docs/` and `src/`, and persists index, `cited-by.json` and rendered documents back to the repository on a best-effort basis, with the same "empty candidate never overwrites" guard the mutation publish uses.
- **REQ-005 (dangling gate)** A pull request that cites an ID or field that will not exist after merge fails a required check. Because IDs derive from the manifests (REQ-003), the check does not need coverage data: it runs in `ci.yml` and validates every `cov:` citation in the PR's own tree against (a) the PR's `.github/coverage-manifest/*.json` plus the existence of the cited file under `src/`, for IDs, and (b) the fixed field schema of REQ-002, for fields. Citations of files added in the PR therefore pass, and citations of files removed in the PR fail. The index published on Pages is used only for rendering values, never for the gate.
- **REQ-006 (dashboard)** The published coverage dashboard (`docs/coverage/index.html` + `app.js`) shows a "Cited In" column from `cited-by.json` and exposes `#pkg-<Package>` anchors that `dashboardUrl` points to.
- **REQ-007 (first use)** `docs/testing/coverage-measurement-methodology.md` cites at least one table and one inline value from the live index, and the methodology documents the citation convention as the mutation methodology does.

## 3. Acceptance criteria

| AC | Requirement | Criterion |
|---|---|---|
| AC-001 | REQ-001 | `.github/scripts/cov-docs-render.cs` exists; on a fixture doc with one `covref-table`, one `covref` inline, one prose mention and one fenced example, it expands the two markers, leaves the fence untouched and writes a `cited-by.json` listing the three citations with `file:line`. |
| AC-002 | REQ-002 | `dotnet run .github/scripts/coverage-report.cs -- --output <dir>` writes `<dir>/docref-index.json` whose entry count equals the number of manifest files with ≥1 applicable flag, and every entry has the fields of REQ-002. |
| AC-003 | REQ-003 | Removing the Cobertura XML of one test type (say `guard`) from the input and re-running produces the same ID set; every entry whose manifest lists `guard` has `perFlag.guard.noData: true` with null totals, its other flags unchanged, and the file-level `noData` is `true` only for entries whose sole applicable flag was `guard`. |
| AC-004 | REQ-004 | After a `publish-coverage` run: (1) `_site/coverage/data/docref-index.json` and `_site/coverage/data/cited-by.json` are served from Pages; (2) the persist-back commit (or, if branch protection rejects it, the logged warning as for mutations) contains `docs/coverage/data/docref-index.json`, `docs/coverage/data/cited-by.json`, and the rendered `.md` files under `docs/` and `.cs` files under `src/` whose marker blocks changed; (3) a run whose candidate index is empty leaves the previously persisted copies untouched. |
| AC-005 | REQ-005 | In a test PR: citing `cov:Encina/DoesNotExist.cs` fails the `ci.yml` check; citing a file the same PR adds to `src/` and to its manifest passes; citing a file the same PR deletes fails; citing an existing ID with a field outside the REQ-002 schema fails; the same PR with only valid citations passes. The check runs with Pages unreachable. |
| AC-006 | REQ-006 | The coverage dashboard renders a "Cited In" column with the citations of the methodology doc, and `https://dlrivada.github.io/Encina/coverage/#pkg-Encina` scrolls to the `Encina` row. |
| AC-007 | REQ-007 | `coverage-measurement-methodology.md` contains an expanded `covref-table` and one expanded `covref` inline after the first publish, plus a "DocRef convention" section mirroring the mutation methodology's. |

## 4. Constraints

- Scripts are C# 14 file-based apps under `.github/scripts/` (`CLAUDE.md` scripting policy); workflows are GitHub Actions YAML.
- Marker names must not collide with `docref-*` or `mutref-*` (renderers scan the same `docs/` and `src/` trees).
- The per-flag obligations model stays authoritative (`AI-DEVELOPMENT-MODEL.md` §3); the index reports what `coverage-report.cs` computes, it does not recompute.
- No change to the coverage manifests' format.

## 5. Non-goals

- Line-level or member-level citations (they would go stale on every commit; the unit of citation is the file with a per-flag breakdown).
- Fingerprints or carry-forward between runs (every CI Full regenerates the index; the benchmark fingerprint mechanism does not apply).
- Retro-fitting citations into every package README in this feature (first use is the methodology doc; READMEs follow as documentation work under v0.22.0).

## 6. Invariants

- **INV-001** Hand-written content outside marker blocks is never modified by the renderer.
- **INV-002** The renderer never fails a Pages deploy: unmatched globs and missing IDs render as warnings; only the explicit `--check-dangling` mode exits non-zero.
- **INV-003** The published index is always the fresh CI artifact; the repository copy may lag.

## 7. Verification

| Requirement | Method |
|---|---|
| REQ-001, REQ-003 | Unit-style fixture run of the two scripts in a temp directory (documented in the PR); compare output against the mutation renderer on an equivalent fixture. |
| REQ-002 | Run `coverage-report.cs` on the `coverage-raw-*` artifact of a green CI Full and count entries against `.github/coverage-manifest/*.json`. |
| REQ-004, REQ-006, REQ-007 | One `publish-coverage` run after merge; inspect Pages. |
| REQ-005 | Throwaway PR with a deliberately dangling citation. |

## 8. Routing (per `docs/engineering/ai-task-routing.md`)

- **Local AI (🟢):** `cov-docs-render.cs` (copy of `mut-docs-render.cs` with renamed markers and the `--check-dangling` mode), the `docref-index.json` emission in `coverage-report.cs`, the `publish-coverage.yml` steps mirroring `publish-mutations.yml`. Brief: one script per task, enumerated points, output paths named, "stop when the file is written".
- **Claude / maintainer (🔴):** the `ci.yml` gate design (fetching the Pages index), the `app.js` dashboard change, verification of AC-001 … AC-007, and the final gate before merge.

## 9. Decisions taken while specifying

| Decision | Choice | Alternatives rejected |
|---|---|---|
| Citation granularity | File with per-flag dictionary | Line-level (stale on every commit); package-level (too coarse to be useful in package docs) |
| ID source | Manifest (with `noData`) | Cobertura output (IDs vanish on partial runs) |
| Where the dangling gate runs | `ci.yml`, validating IDs against the PR's own manifests and `src/` tree and fields against the REQ-002 schema | CI Full only (not PR-triggered, would not protect PRs); `ci.yml` against the Pages index (rejects citations of files the PR adds, accepts citations of files it removes, and depends on Pages availability) |
| Dashboard file to change | `docs/coverage/app.js` | HTML generated by `coverage-report.cs` (not what Pages serves) |
