- When an issue shipped no page and its audit already found a README defect, the delta docs stage still finds new rule (a) gaps by counting emojis per README (Unicode ranges) and by reading the sibling READMEs of the same category; a per-README count is quicker than reading for tone.
Applied: role:docs-reviewer Count emojis per README by Unicode range (state count and lines) and read the sibling READMEs of the same category; cite .claude/agents/docs-reviewer.md point 1 for the no-emoji rule.
- A feature that is documented only in package READMEs has no docs-site page and no tutorial entry; check `docs/tutorials/` and `docs/index.md` for the feature name before assuming the READMEs are enough.
Applied: role:docs-reviewer For feature adequacy, search docs/tutorials/ and docs/index.md for the feature name; READMEs alone are not adequate docs.
- When one package is not instrumented (here `Encina.FluentValidation`, #898), the per-file rule (b) targets for it cannot be checked against measured coverage at all; the stage should state "not measured" per file and the delta brief should list known uninstrumented assemblies so the target proposals are marked provisional up front.
Applied: role:test-auditor If a package is absent from Cobertura (#898: Encina.FluentValidation), say "not measured" per file and mark its target proposals provisional.
- A guard-flag proposal needs the guard-reachable line count (null checks only), not the package aggregate: for a 22-line provider only 2 lines are guard-reachable, so a package guard target of 25 can never be met by guard tests alone; the manifest's guard aggregate and the per-file targets should be reasoned together.
Applied: role:test-auditor Base guard targets on guard-reachable lines (argument checks), not on the package aggregate.
- Open PRs that carry per-file targets (here #1826 for `Encina.DataAnnotations`) should be compared with the measurement in the stage; the measurement showed their unit 90 and property 90 sit at today's value, not above it.
Applied: not applied: one-off; #1826 merges its example targets as they are and #1825 raises them; recorded in the resume point.
- docs 1: recorded as duplicate of #1330 by manual override
Applied: not applied: record of the -DuplicateOf override.
- The docs stage counted emojis per README (26, 13, 22) but cited lines as if they were the count; the drafts state both the emoji count and the lines that carry them (21, 13 and 16 lines), and the rule source for "no emojis" is `.claude/agents/docs-reviewer.md` point 1, which a docs finding should name.
Applied: not applied: folded into lesson 1's role note.
- docs 6 is already tracked by open #1827 and is expected to be re-prepared as a duplicate; its draft was written as the manifest stood. The page also lists `Encina.GuardClauses.LoadTests` (line 47), which is not in `Encina.slnx` either; the docs stage named only the FluentValidation entry.
Applied: not applied: superseded by the docs 6 duplicate override (#1827); #1827 covers line 47 too.
- The Glob tool returned no files for existing paths in the audit worktree again (`tests/**/FluentValidation/*.cs`, `tests/Encina.*LoadTests`); the absence claims were checked with Grep and `Encina.slnx` instead.
Applied: not applied: already a remediation-drafter role lesson.
- The tests stage proposes per-file targets for a manifest that has no per-file target field yet (it arrives with open PR #1826); the drafts write each proposal as target plus one-sentence justification, and the FluentValidation targets are marked provisional until #898 is fixed.
Applied: not applied: already a test-auditor role lesson (per-file targets until #1762/#1826).
- docs 6: recorded as duplicate of #1827 by manual override
Applied: not applied: record of the -DuplicateOf override.
- After a regeneration with manual `-DuplicateOf` overrides (one per run), verify the final state by counting the surviving drafts by command (here 8) and grepping the stage artifacts and drafts for the old path and sentences; the narrow re-check took five calls.
Applied: role:audit-verifier After -DuplicateOf regenerations, count the surviving drafts by command and grep for the old sentences.
- The remediation stage's lesson lines are carried over from earlier runs and can describe a state that the final run changed (docs 6 "expected to be re-prepared"); the stage could drop lesson lines that its own later override has made obsolete.
Applied: role:remediation-drafter On a re-run, drop your own earlier lesson lines that the final state makes obsolete.
