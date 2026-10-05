Remediation for #29:
- code 1 (Major): draft 29-code-1-github-workflows-ci-full-yml-531-541-and.md
- code 2 (Minor): duplicate of #1728 (manual override)
- tests 1 (Major): draft 29-tests-1-tests-encina-contracttests-29-of-the-134-test.md
- tests 2 (Minor): draft 29-tests-2-tests-encina-contracttests-and-tests-encina-propertytests-da.md
- docs 1 (Blocker): draft 29-docs-1-hand-typed-contract-test-counts-on-six-feature.md
- docs 2 (Major): draft 29-docs-2-docs-releases-release-process-md-17-23-checklist.md
- docs 3 (Minor): draft 29-docs-3-docs-releases-release-process-md-142-by-its.md

## Lessons for the pipeline
- code 2: recorded as duplicate of #1728 by manual override
- code 1: in `ci-full.yml` the `pack` job's `needs:` (`:532-538`) does not list `build`, yet its condition reads `needs.build.result` (`:541`); the finding did not examine what that context evaluates to, so the code 1 draft only states the listing and asks the fixer to decide how `build` is referenced.
- tests 1: 23 of the 29 files are a text-heuristic list that was never read; the draft carries that caveat and asks the fixer to read them first. The heuristic misses target-typed `new(...)` and helper factories.
- docs 2 and docs 3 rest on the facts of code 1 (the `pack` condition and run 35652357800); the run ids and job counts come from the findings and could not be re-checked without `gh`.
