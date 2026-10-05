Remediation for #7:
- tests 1 (Major): draft 7-delta-2026-10-tests-1-src-encina-dataannotations-dataannotationsvalidationprovider.md
- tests 2 (Major): draft 7-delta-2026-10-tests-2-src-encina-dataannotations-servicecollectionextensions-cs-st.md
- tests 3 (Major): draft 7-delta-2026-10-tests-3-src-encina-fluentvalidation-fluentvalidationprovider-cs-flue.md
- tests 4 (Minor): draft 7-delta-2026-10-tests-4-src-encina-validation-validationorchestrator-cs-validationor.md
- docs 1 (Major): duplicate of #1330 (manual override)
- docs 2 (Major): draft 7-delta-2026-10-docs-2-point-1-visual-and-scannable-no-emojis-the.md
- docs 3 (Minor): draft 7-delta-2026-10-docs-3-point-1-the-how-it-works-sections-src.md
- docs 4 (Major): draft 7-delta-2026-10-docs-4-point-3-figures-cited-never-hand-typed-src.md
- docs 5 (Major): draft 7-delta-2026-10-docs-5-point-5-adequate-docs-for-a-feature-in.md
- docs 6 (Minor): duplicate of #1827 (manual override)

## Lessons for the pipeline
- docs 1: recorded as duplicate of #1330 by manual override
- The docs stage counted emojis per README (26, 13, 22) but cited lines as if they were the count; the drafts state both the emoji count and the lines that carry them (21, 13 and 16 lines), and the rule source for "no emojis" is `.claude/agents/docs-reviewer.md` point 1, which a docs finding should name.
- docs 6 is already tracked by open #1827 and is expected to be re-prepared as a duplicate; its draft was written as the manifest stood. The page also lists `Encina.GuardClauses.LoadTests` (line 47), which is not in `Encina.slnx` either; the docs stage named only the FluentValidation entry.
- The Glob tool returned no files for existing paths in the audit worktree again (`tests/**/FluentValidation/*.cs`, `tests/Encina.*LoadTests`); the absence claims were checked with Grep and `Encina.slnx` instead.
- The tests stage proposes per-file targets for a manifest that has no per-file target field yet (it arrives with open PR #1826); the drafts write each proposal as target plus one-sentence justification, and the FluentValidation targets are marked provisional until #898 is fixed.
- docs 6: recorded as duplicate of #1827 by manual override
