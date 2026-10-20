Remediation for #17:
- tests 1 (Major): draft 17-delta-2026-10-tests-1-github-coverage-manifest-encina-marten-json-entry-snapshots.md
- tests 2 (Major): draft 17-delta-2026-10-tests-2-github-coverage-manifest-encina-marten-json-entry-martenaggr.md
- tests 3 (Minor): duplicate of #1847 (manual override)
- docs 1 (Minor): draft 17-delta-2026-10-docs-1-docs-architecture-adr-027-marten-as-the-event.md
- docs 2 (Minor): draft 17-delta-2026-10-docs-2-docs-architecture-adr-027-marten-as-the-event.md
- docs 3 (Minor): draft 17-delta-2026-10-docs-3-docs-architecture-adr-027-marten-as-the-event.md
- docs 4 (Major): draft 17-delta-2026-10-docs-4-rule-a-point-5-adequate-docs-for-a.md

## Lessons for the pipeline
- The tests stage cites the commit `3aaa66b1` (2026-04-03, "remove integration target (0 files assigned)") for the package losing its `integration` target. This agent has no shell and cannot run `git log`, so tests 2 states the commit, date and message as the stage recorded them and states only the verified current state (`Encina.Marten.json:5-9` and `:100-108`) as fact.
- The Glob tool again returned nothing for paths that exist in the audit worktree (`docs/features/*.md`, `src/Encina.Marten/**/*.md` pattern checks); absent-path claims in the docs 4 draft (no `.md` under `src/Encina.Marten/`, no Marten or event-sourcing hit in `docs/tutorials/`, `docs/features/index.md` and `docs/guides/`) were checked with count-mode and files-with-matches Grep as a second check.
- The docs stage says docs 4 found no entry "in the tutorials or learning paths", but no learning-path page exists under `docs/` (the search for "learning path" matches only knowledge records), so the draft names only the tutorials index as the missing entry.
- Rule (b) delta briefs now propose values for the per-file `targets` and `justifications` fields (the task prompt states #1762 is closed); the drafts for tests 1, 2 and 3 ask for those fields, taking the figures verbatim from the tests stage (Release configuration).
- The docs stage finding 2 quotes the ADR text containing the emoji U+274C; the draft refers to it as `<U+274C>` so the issue body itself carries no emoji.
- tests 3: recorded as duplicate of #1847 by manual override
