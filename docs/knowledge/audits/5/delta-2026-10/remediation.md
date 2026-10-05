Remediation for #5:
- tests 1 (Minor): draft 5-delta-2026-10-tests-1-src-encina-core-streamdispatcher-cs-declared-type-streamdisp.md
- tests 2 (Minor): duplicate of #1821 (manual override)
- docs 1 (Minor): draft 5-delta-2026-10-docs-1-docs-testing-load-tests-known-issues-md-heading.md
- docs 2 (Major): draft 5-delta-2026-10-docs-2-docs-testing-load-tests-known-issues-md-header.md
- docs 3 (Minor): draft 5-delta-2026-10-docs-3-docs-testing-load-tests-known-issues-md-whole.md
- docs 4 (Minor): draft 5-delta-2026-10-docs-4-docs-testing-load-tests-known-issues-md-whole.md

## Lessons for the pipeline
- tests 2: recorded as duplicate of #1821 by manual override
- The Glob tool returned nothing for existing worktree paths (docs/testing/*.md); Grep found them. Absent-path claims need a Grep as a second check.
- A sibling-page convention is evidence about drift, not about the rule: docs finding 3 (missing just-the-docs front matter) stays a finding although the other docs/testing pages lack it too, because `encina-docs` SKILL section 2 requires front matter on every docs/ page. Check the convention source before dropping a finding.
- The drafts of the earlier remediation run were missing from the remediation folder when this stage re-ran, so all five were rewritten from the inputs.
