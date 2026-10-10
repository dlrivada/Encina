## Scope

- #4 is a documentation-only umbrella; no `src/` code was touched and no PR or commit closed it. The only commit in its timeline, 69389b05 (2025-12-23, "restructure ROADMAP with 6-phase approach"), merely mapped #4 to Phase 5 in `ROADMAP.md`.
- Files the issue proposed: `docs/guides/migrating-from-mediatr.md` (does not exist today; `docs/guides` exists but has no MediatR page), provider comparison tables (none exist; `docs/benchmarks/*-comparison.md` and `docs/comparacion-nestjs.md` are unrelated), DocFX deployment to GitHub Pages (today `.github/workflows/docs.yml` is "the ONLY workflow that deploys GitHub Pages", #1381; tracked as #90), README links.
- Nearest existing content: the "Comparison with MediatR" section of `docs/introduction.md` and the planned `18-from-mediatr` page in `docs/roadmap-documentacion.md` (line 498).
- Nothing removed on purpose; no `src/` scope.

## Destinations

- Duplicate-split decision: planned at #85 (guide), #86 (comparison tables), #90 (Pages deploy). Missing as delivered content: no guide or comparison tables in `docs/`.
- No ADR, rule or hook applies (documentation only; the issue's own cross-cutting table marks all 12 functions N/A).

## Successor and duplicate issues

Verified with `gh issue view <n> --json state,title` on 2026-10-10:
- #85 "[FEATURE] Documentation: MediatR Migration Guide": OPEN.
- #86 "[FEATURE] Documentation: Package Comparison Tables": OPEN.
- #90 "[INFRA] Deploy documentation site to GitHub Pages": OPEN.
All open, so the work is pending, not implemented. The close comment says the scope only #4 had was copied to the target as a comment, but #85 and #86 have 0 comments (checked), so that copy is not visible.

## Lessons for the pipeline

- The close comment claims "scope copied to the target as a comment" but the target issues have no comments; verify such claims before recording them.
- The pre-draft for #4 was absent (total=0 links), so the record was built from the issue, its one comment and the timeline only.
