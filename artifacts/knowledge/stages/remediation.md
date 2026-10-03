Remediation for #29:
- code 1 (Major): draft 29-code-1-github-workflows-ci-full-yml-531-541-and.md
- code 2 (Minor): duplicate of #1728 (manual override)
- tests 1 (Major): draft 29-tests-1-tests-encina-contracttests-29-of-the-134-test.md
- tests 2 (Minor): draft 29-tests-2-tests-encina-contracttests-database-pagination-cursorpaginat.md
- docs 1 (Blocker): draft 29-docs-1-hand-typed-contract-test-counts-on-six-feature.md
- docs 2 (Major): draft 29-docs-2-docs-releases-release-process-md-17-23-checklist.md
- docs 3 (Minor): draft 29-docs-3-docs-releases-release-process-md-142-by-its.md

## Lessons for the pipeline
- code 2: recorded as duplicate of #1728 by manual override
- code stage: finding code 1 says test-contract has "the identical wiring" at ci-full.yml:197-199 and :535; in the code only `needs: build` (:199) and the listing in pack's `needs:` (:535) are shared, which is what the draft says. The run-history claims (jobs of runs 35652357800 and 35736044176) could not be re-verified by the drafter, which has no shell; the drafts repeat them as the stages' evidence.
- code stage: finding code 1 lists the failed jobs of run 35652357800 by name only (four `test-ef-providers` shards, no counts for the others); the draft uses the per-job counts from the corrected docs finding (18 failed jobs).
