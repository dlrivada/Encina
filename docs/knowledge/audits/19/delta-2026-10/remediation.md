Remediation for #19:
- tests 1 (Minor): duplicate of #1668 (manual override)
- docs 1 (Major): draft 19-delta-2026-10-docs-1-docs-testing-coverage-measurement-methodology-md-section-dat.md
- docs 2 (Minor): draft 19-delta-2026-10-docs-2-docs-testing-coverage-measurement-methodology-md-236-238.md
- docs 3 (Major): duplicate of #1441
- docs 4 (Minor): draft 19-delta-2026-10-docs-4-docs-testing-mutation-measurement-methodology-md-1-is.md
- docs 5 (Minor): draft 19-delta-2026-10-docs-5-placement-and-neighbours-rule-a-point-4-docs.md

## Lessons for the pipeline
- tests 1: recorded as duplicate of #1668 by manual override
- (tests 1) Disposition after the verifier's FAIL: the fixture-run self-test of the `--check-*` modes of `coverage-report.cs` is a manual duplicate of #1668, and the orchestrator posted its assertions on that issue (issuecomment-6017488167), so no draft is written for it. The earlier suggestion that the harness could ship before the per-flag target check still stands as a note for the implementer of #1668.
- (docs 3) Disposition after the verifier's FAIL: docs 3 stays an automatic duplicate of #1441 (the manifest decides). The missing job-graph diagram of the mutation methodology page is tracked by #1900, opened by the orchestrator, with a pointer on PR #1713, so the diagram is drawn once together with the page rewrite. No draft is written.
- (docs 2) The renderer `.github/scripts/cov-docs-render.cs` itself writes the warning-sign pictograph (U+26A0) at `:437`, `:475` and `:498` (console lines `:142` and `:207`), checked in the audit worktree; editing the methodology page does not remove it from published citation output. The draft keeps that as a separate decision and says no item covers it. The draft names the pictograph and the coloured circles by code point so the issue body carries no emoji.
- (docs 2, docs 5) The Related Issues lines of these drafts are written as "#N: title. Why it is related", each on its own line with a title from the manifest, and carry no prose that needs a bare issue number to make sense (for example no "part of #N" or "the replaced #19 threshold"); the docs 5 draft says "the single project-wide coverage threshold" instead. Issues that only the finding named without a title in the manifest (the emoji items for other pages) were left out.
- (all drafts) The Glob tool returned nothing for `docs/testing/*` and `artifacts/knowledge/remediation/19-delta*`, and brace globs in Grep returned no match for files that exist, so every absent-path claim was checked a second way: `docs/testing/index.md` by reading it ("File does not exist"), front matter in `docs/testing/` by a Grep for `title:`, `layout:`, `parent:`, `nav_order:` and `has_children:` (0 files, with `docs/index.md` as a positive control, 3 hits), and Mermaid by a count-mode Grep (2 blocks, only in `aspire-migration-guide.md`, which the docs 1 draft cites as precedent).
