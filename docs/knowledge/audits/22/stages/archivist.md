## Scope
- Issue #22 was closed on 2025-12-24 (about 21 minutes after creation: created 2025-12-24T11:31:36Z, closed 11:52:23Z) by its author with the single comment "Reverted - issue created in error". It has no linked PR and no closing commit. Its only timeline commit reference is 2b50a1ec (documentation restructure), which created it together with #21 and #23-#28. That commit touched only `.claude/CLAUDE.md`, `ROADMAP.md` and `docs/history/2025-12.md`.
- The issue itself changed no code, so there is nothing to scope for it directly.
- The feature it described (event upcasting and schema versioning for Marten) exists today in `src/Encina.Marten/Versioning/` (IEventUpcaster, EventUpcasterBase, LambdaEventUpcaster, EventUpcasterRegistry, EventVersioningOptions, ConfigureMartenEventVersioning, EventVersioningErrorCodes, VersioningLog). It was delivered under #37 by commit 957093c2 (`git log --follow` on IEventUpcaster.cs shows that commit only). Tests exist in tests/Encina.UnitTests, GuardTests and IntegrationTests under `Marten/Versioning`. If the auditor wants the code reviewed, it belongs to #37's audit, not this one.
- `Encina.EventStoreDB` (named in the issue as "Other") does not exist in `src/`; it was never created (EventStoreDB is listed as "future" in AGENTS.md).

## Destinations
- The decision "this issue was created in error" has no destination and needs none: the issue holds no technical decision. The record sets `current: no` with destination `none`.
- Present today: the feature's code under `src/Encina.Marten/Versioning/` (verified by listing the directory).
- Not checked: the pre-draft's open question "does implementation code exist despite the error" is answered above (yes, under #37). The pre-draft's "rule" ("verify issue creation accuracy") is generic and I did not record it as a rule.

## Successor and duplicate issues
- #37 "[FEATURE] Event Versioning - upcasting and schema evolution": same title, created 2025-12-24 13:23Z (#22 was created 11:31Z), CLOSED/COMPLETED on 2025-12-26, closed by commit 957093c2 (`Fixes #37`). Verified with `gh issue view 37` and the timeline. The record uses `outcome: duplicate`, `duplicate_of: 37`. The "duplicate" label is an inference from the identical title and timing; the author's comment says only "created in error". GitHub's own state_reason on #22 is COMPLETED, although no work was done under it.
- #21 (Projections) and #25 (Snapshotting), cross-referenced from #22's body, are both CLOSED; they are unrelated to this duplicate.

## Lessons for the pipeline
- The `docs/knowledge/audits/issue-22.md` path in `audit.record` does not exist yet and is expected to be created at audit close; the checker accepts it.
- A reverted-in-error issue may have a same-titled successor created hours later; search `gh issue list --state all --search "<title>"` to find it before choosing the outcome. GitHub shows state_reason COMPLETED for it, so state_reason alone is not evidence of delivery.
- The checker requires block-style empty lists (`prs:` with nothing after), not `[]`, and rejects `current: yes` without a non-`none` destination, so a historical-only entry must use `current: no`.
- `block-main-checkout-writes.ps1` blocks PowerShell `-replace`/`Set-Content` on `.md` files even inside the worktree artifacts; use the Edit tool.
