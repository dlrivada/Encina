- When an issue's only destination is an ADR and the ADR lacks front matter, check the sibling ADR on the same topic (here ADR-019) to show the expected block; the contrast makes the finding checkable.
Applied: role:docs-reviewer show the expected front matter from the sibling ADR on the same topic to make a placement finding checkable
- For a deprecation SPIKE, rule (a) point 5 is about the surviving provider, not the removed one: check for a concept page, a package README and a tutorial entry for the kept package.
Applied: role:docs-reviewer for a deprecation spike, rule (a) point 5 concerns the surviving provider: check its concept page, package README and tutorial entry
- Rule (b) delta briefs written before #1762 closed still say the manifest has no justification field; it now has per-file `targets` and `justifications`, so the brief and the role definition should say findings propose values for those fields, and the old lessons about "package aggregates only" apply to files that have no per-file entry yet.
Applied: role:test-auditor #1762 is closed: rule (b) findings propose values for the per-file targets and justifications fields
- A package manifest can silently lose a flag for the whole package (`3aaa66b1` removed `integration` from `Encina.Marten` because "0 files assigned"); in rule (b) check the package `targets` keys against the flags that apply to the scoped files, not only the file entries.
Applied: role:test-auditor check the package targets keys against the flags that apply to the scoped files; a flag can disappear for the whole package
- A test class that sets a default option to the opposite value in its constructor (`AsyncSnapshotCreation = false` at `SnapshotAwareAggregateRepositoryTests.cs:37` against the default `true`) leaves the default path with zero unit coverage; compare each uncovered branch with the option default before calling the gap an edge case.
Applied: role:test-auditor compare each uncovered branch with the option default; a test that flips the default leaves the default path untested
- The tests stage cites the commit `3aaa66b1` (2026-04-03, "remove integration target (0 files assigned)") for the package losing its `integration` target. This agent has no shell and cannot run `git log`, so tests 2 states the commit, date and message as the stage recorded them and states only the verified current state (`Encina.Marten.json:5-9` and `:100-108`) as fact.
Applied: role:remediation-drafter when a fact needs git history you cannot read, state it as the stage recorded it and state only verified current state as fact
- The Glob tool again returned nothing for paths that exist in the audit worktree (`docs/features/*.md`, `src/Encina.Marten/**/*.md` pattern checks); absent-path claims in the docs 4 draft (no `.md` under `src/Encina.Marten/`, no Marten or event-sourcing hit in `docs/tutorials/`, `docs/features/index.md` and `docs/guides/`) were checked with count-mode and files-with-matches Grep as a second check.
Applied: role:remediation-drafter confirm absent-path claims with count-mode Grep; Glob misses existing paths
- The docs stage says docs 4 found no entry "in the tutorials or learning paths", but no learning-path page exists under `docs/` (the search for "learning path" matches only knowledge records), so the draft names only the tutorials index as the missing entry.
Applied: role:remediation-drafter name only pages that exist; do not invent a learning-path target when none exists
- Rule (b) delta briefs now propose values for the per-file `targets` and `justifications` fields (the task prompt states #1762 is closed); the drafts for tests 1, 2 and 3 ask for those fields, taking the figures verbatim from the tests stage (Release configuration).
Applied: not applied: historical remark, accepted by the verifier (pass 2); the final disposition of tests 3 is recorded in the same file
- The docs stage finding 2 quotes the ADR text containing the emoji U+274C; the draft refers to it as `<U+274C>` so the issue body itself carries no emoji.
Applied: role:remediation-drafter refer to an emoji by its code point so the issue body carries none
- tests 3: recorded as duplicate of #1847 by manual override
Applied: applied: amendment comment on #1847 (issuecomment-6014235601, 2026-10-06)
- After a `-DuplicateOf` regeneration, a lesson written before the override can keep naming the converted draft ("tests 1, 2 and 3"); the verifier accepts it when the same file states the final disposition and no draft body repeats it, rather than forcing a second regeneration.
Applied: role:audit-verifier after a -DuplicateOf regeneration, accept an older lesson naming the converted draft when the file records the final disposition
- The orchestrator posted the #1847 amendment before this pass; reading the comment (`--json comments`) and comparing its table with the measured figures closes the duplicate correction without reproducing the measurement, as long as `git diff --stat` shows only stage artifacts changed.
Applied: role:audit-verifier close a duplicate correction by comparing the posted comment with the measured figures when only stage artifacts changed
- A pass-2 check by `git diff <verified base> HEAD` plus draft write-times versus the previous verification commit time shows the surviving drafts are untouched without hashing against old copies.
Applied: role:audit-verifier prove surviving drafts unchanged with git diff --stat and draft write-times versus the previous verification commit
