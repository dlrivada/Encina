## Scope
- None. The issue delivered no code: closed 20 minutes 4 seconds after creation (created 2025-12-24T11:32:28Z, closed 11:52:32Z) with "Reverted - issue created in error". Its only timeline commit, `2b50a1ec` (docs restructure), touched `.claude/CLAUDE.md`, `ROADMAP.md` and `docs/history/2025-12.md`, not source.
- Verified absent today: there is no Roslyn source-generator project: `src/Encina.SourceGenerators` does not exist, no directory under `src/` or `tests/` is named `*SourceGenerator*`, and no `.cs`/`.csproj` file in `src/` or `tests/` contains `IIncrementalGenerator`, `ISourceGenerator`, `[Generator]`, `Encina.SourceGenerators` or `EncinaHandler`. (Directories named `Generators` exist, e.g. `src/Encina.IdGeneration/Generators`, but they are ID generators, unrelated.)
- The code stage has no `src/` surface for this issue. The affected-package text in the issue (`Encina` dispatch, `Encina.SourceGenerators`) was intent only. Nothing was removed on purpose.

## Destinations
- Decision "build a Roslyn source generator for zero-reflection dispatch, switch dispatch and NativeAOT": destination is backlog issue #50 (and #51 for switch dispatch). Present: #50 and #51 are OPEN (checked 2026-10-03). The work itself is missing, not implemented.
- No ADR, rule, test or doc exists for it (nothing was decided beyond the proposal; "Alternatives Considered: Not evaluated yet").

## Successor and duplicate issues
- #27 is a duplicate of #50 (same scope re-created 1 hour 36 minutes after the revert; compared bodies: Roslyn generator, switch-based dispatch, compile-time registration, NativeAOT). Found by searching `gh issue list --state all --search "Source Generators"`.
- #50 "[FEATURE] Source Generators - zero-reflection, NativeAOT ready": state OPEN, created 2025-12-24T13:28:08Z, no state reason (`gh issue view 50`). Pending work, not implemented.
- #51 "[FEATURE] Switch-based dispatch - no dictionary lookup": state OPEN, depends on #50. Pending.
- Related but not duplicates: #49 (delegate cache optimization, CLOSED COMPLETED), #227, #333, #343 (all OPEN).
- The pre-draft chose `rejected-unexplained`; corrected to `duplicate` with `duplicate_of: 50`.

## Lessons for the pipeline
- Pre-draft marked `rejected-unexplained` for a "created in error" issue again (same as #21, #26); the same-title search in all states found the re-creation (#50) at once. The pre-draft script could run `gh issue list --state all --search "<title keywords>"` and hand the candidates to the archivist.
- The worktree has no `artifacts/knowledge/predraft`; the pre-draft was read from the main checkout (read-only), as earlier lessons say, and the file listing was identical to the issue text.
