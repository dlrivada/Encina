Remediation for #28:
- code 1 (Major): duplicate of #1721 (manual override)
- tests 1 (Minor): draft 28-tests-1-tests-encina-propertytests-configurationproperties-cs-301-30.md
- tests 2 (Minor): draft 28-tests-2-duplicated-theory-rows-that-xunit-silently-discards-31.md
- docs 1 (Minor): draft 28-docs-1-docs-plans-testing-dogfooding-plan-md-132-134.md

## Lessons for the pipeline
- The dedup pass marked code 1 (CI path filters skip test jobs) as a duplicate of #1464, whose title is "Run the unit test job with outbound network blocked so a unit test that opens a real connection fails at PR time"; the two titles share no symbol or file, so the verifier should re-check that match before the issue is closed.
- code 1: recorded as duplicate of #1721 by manual override
