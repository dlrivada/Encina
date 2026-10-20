## Pages reviewed

Issue #27 delivered nothing (archivist: duplicate of open #50, companion #51; code and tests stages: no source generator exists). There is no page that describes #27 as delivered, so this stage searched every `*.md` (outside `node_modules`, `.git`, `bin`, `obj`) for the proposed-but-absent identifiers and for claims of source-generated dispatch or NativeAOT readiness. Patterns: `Encina.SourceGenerators`, `EncinaHandler`, `zero-reflection`, `reflection-free`, `NativeAOT`, `Native AOT`, `AOT`, `source generator`, `#50`, `#51`. `artifacts/` was searched with `Select-String` (the hits there are this audit's own stage files and `artifacts\knowledge\issues\27.md`, which describe the generator as absent).

Pages that mention the generator or its identifiers and were opened:

- `ROADMAP.md` (lines 318-324, 330, 608)
- `docs/INVENTORY.md` (lines 4355-4356, 4421-4442)
- `docs/architecture/adr/005-reject-source-generators.md` (lines 10, 285-293), `docs/architecture/adr/003-caching-strategy.md` (lines 370-374), `docs/architecture/adr/index.md` (line 17)
- `docs/engineering/ENCINA-1.0-RECONCILIATION.md` (§5.2, line 153)
- `docs/releases/v0.11.0/CHANGELOG-DETAILS.md` (lines 1855, 2955) and `CHANGELOG.md` (lines 7312, 8412): historical, "Depends on #50" / "aligns with #50", accurate as history

`Encina.SourceGenerators` appears in `.md` files only in `artifacts\knowledge\issues\27.md`, `artifacts\knowledge\stages\archivist.md` and `docs/architecture/adr/003-caching-strategy.md:374` (as a future optional package). `EncinaHandler` appears only in this audit's artifacts. No page outside `docs/releases/`, `CHANGELOG.md` and `docs/INVENTORY.md` carries "NativeAOT ready" for dispatch. The other NativeAOT hits are `Encina.Testing.TUnit` (TUnit, a real `<IsAotCompatible>` at `src/Encina.Testing.TUnit/Encina.Testing.TUnit.csproj:21`) and `[LoggerMessage]`/"zero reflection after first resolution" statements about attribute caching in compliance pages, which are unrelated to #27.

## Findings

1. **Major** — `ROADMAP.md:318-324`, heading "Legacy Phase 2 Details (Completed Items)" (line 318), lead-in "Key areas already completed:" (line 320). Line 324 reads "**Performance** — ✅ Delegate cache optimization [#49], Source generators for NativeAOT [#50], Switch-based dispatch [#51]". The ✅ marks only #49; #50 and #51 carry no pending marker and sit inside a section that declares its items completed, so a reader takes source generators and switch dispatch as delivered. Both issues are OPEN and no generator exists (code stage; archivist: #50 created 2025-12-24, state OPEN). `ROADMAP.md` mentions #50/#51 only at line 324 (checked with `Select-String` for `#50`, `#51` and the issue links); the planned wording is in `docs/INVENTORY.md:4419-4442` ("Features planificadas"), so the contradiction is between the two pages. Check that fails: accuracy against today's code (a delivered-looking claim for absent work).
2. **Major** — `docs/architecture/adr/005-reject-source-generators.md:10` ("**Status:** Rejected", "We REJECT the use of Source Generators for handler dispatch" at line 75), indexed as "Reject Source Generators" at `docs/architecture/adr/index.md:17`. Open issues #50/#51, `ROADMAP.md:324` and `docs/INVENTORY.md:4421-4442` plan exactly that generator (compile-time handler discovery, switch dispatch, NativeAOT). No ADR records the decision to build it: `Select-String` for `ADR-005|005-reject` in `ROADMAP.md` and `docs/INVENTORY.md` finds nothing, and no ADR in `docs/architecture/adr/` supersedes 005 (only ADR-003:374 mentions an optional future package, and ADR-005:293 says "Deferred until Native AOT is a concrete requirement", which also disagrees with its own Rejected status at line 10). Check that fails: decisions (a design choice with an accepted ADR saying the opposite and no superseding ADR). Remedy is a new superseding ADR when #50 is taken up, never an edit of the accepted one.

## Informational (not findings)

- `docs/INVENTORY.md` is written largely in Spanish (for example line 4423 "Generadores de código en tiempo de compilación para zero-reflection dispatch", line 4421 section on #50). Under the docs rules any Spanish is a blocker, but the page is a package inventory not created or edited by #27, so it belongs to a page-level docs audit, not to this issue. Its #50/#51 text (lines 4355-4356, 4421-4442) describes the work correctly as planned ("Features planificadas") and depends-on #50 for #51.
- `docs/engineering/ENCINA-1.0-RECONCILIATION.md` §5.2 (line 153) only classifies v0.20 source-generator work as non-blocking for 1.0; it does not claim delivery.
- `docs/releases/v0.11.0/CHANGELOG-DETAILS.md:1855`, `:2955`, `CHANGELOG.md:7312`, `:8412` reference #50 as a dependency or alignment target; they are dated release records and accurate.
- `tests/Encina.BenchmarkTests/Encina.Benchmarks/Messaging/Scheduling/ScheduledMessageProcessorBenchmarks.md:29` speaks of a "future source-generator-based dispatcher (AOT)"; accurate (planned).
- Tooling not run: `lychee` and `markdownlint-cli2` were not run because no page was changed or authored by #27; the pages above were reviewed only for the delivered-versus-planned claim.
- Figures: no hand-typed coverage or performance figure about the generator exists. ADR-005 carries benchmark literals (13.07 ns, 0.92 ns) as its own historical measurements; not examined further as they are unrelated to a delivery claim.

## Lessons for the pipeline

- A closed-in-error issue whose feature was later rejected by an ADR needs a decision check as well as an absence check: the ADR rejecting the feature (here ADR-005) and the open backlog issue (#50) contradict each other, and only the docs stage reading the ADR index sees it.
- A roadmap sentence that mixes ✅-marked and unmarked items under a "completed" heading reads as delivered for the unmarked ones; search roadmap sections by heading, not only by identifier.
- Read every cited line number back with `Select-String` (line numbers from a zero-based array index are off by one) and check "elsewhere the page says X" claims with a search before writing them (audit #27 verifier FAIL: heading at 318 not 317, lead-in at 320 not 319, and ROADMAP.md mentions #50/#51 only at line 324).
