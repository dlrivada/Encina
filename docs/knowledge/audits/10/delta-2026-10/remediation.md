Remediation for #10:
- tests 1 (Minor): duplicate of #1850 (manual override)
- tests 2 (Minor): duplicate of #1850 (manual override)
- tests 3 (Minor): draft 10-delta-2026-10-tests-3-github-coverage-manifest-encina-dataannotations-json-package.md
- tests 4 (Minor): draft 10-delta-2026-10-tests-4-github-coverage-manifest-encina-dataannotations-json-dataann.md
- docs 1 (Major): draft 10-delta-2026-10-docs-1-src-encina-dataannotations-readme-md-214-226-and.md
- docs 2 (Major): duplicate of #1330 (manual override)
- docs 3 (Major): duplicate of #1850 (manual override)
- docs 4 (Major): duplicate of #1850 (manual override)
- docs 5 (Major): duplicate of #1850 (manual override)

## Lessons for the pipeline
- docs 2: recorded as duplicate of #1330 by manual override
- docs 3: recorded as duplicate of #1850 by manual override
- docs 4: recorded as duplicate of #1850 by manual override
- docs 5: recorded as duplicate of #1850 by manual override
- tests 1: recorded as duplicate of #1850 by manual override
- tests 2: recorded as duplicate of #1850 by manual override
- The manifest's `routes.docs` carries the label `technical-debt` only, while the role definition says a docs-stage draft uses `area-documentation`; the docs 1 draft follows the manifest. (Align the route's labels with the routing section of the drafter definition.)
- The tests stage lists the 27 guard-coverable lines and the three reachable lines (provider `:27`, `:28`, registration `:69`) without naming them; the drafts verified the three `ThrowIfNull` lines in `src/` but the 27-line total is the stage's measured figure (22 + 5), not re-measured here. (State the line numbers behind a coverable-lines figure.)
- Docs 1 says the README line is repeated in the FluentValidation README only if it carries the same sample and reports 0 hits; the draft states the verified result (no "Combining" or `AddDataAnnotationsValidation` there). (Phrase a scope check as its result, not as a conditional.)
