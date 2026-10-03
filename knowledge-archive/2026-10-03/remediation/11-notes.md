# Remediation notes for issue #11's audit

No new remediation issue drafted. The one real, verified defect surfaced by this audit — `EncinaError.Exception` no longer carries a structured `ValidationException` for FluentValidation/DataAnnotations failures, contradicting `src/Encina.FluentValidation/README.md` — is already tracked as:

- **#1330** — "[BUG] Encina.DataAnnotations and Encina.FluentValidation READMEs document EncinaError.Exception behaviour the code does not implement" (milestone v0.14.0 — Hardening, open as of 2026-09-25).

Found by the SPEC-003 audit of #11. No action beyond linking; #1330 already covers the fix (either restore structured exception/metadata in `ValidationOrchestrator`, or rewrite the READMEs — the fix owner should decide which side is correct) and its own remediation scope should extend to adding the missing regression test (`error.Exception` invariant, or its replacement contract) once the design decision in #1330 is made — note this dependency in #1330's discussion if not already there.

One informational-only observation, not worth its own issue: commit `d7b7b8ac` ("Add unit tests for domain modeling components", 2025-12-29) deleted an entire unrelated 462-line test file (`ValidationPipelineBehaviorPropertyTests.cs`) as a side effect. This is a single pre-1.0 historical commit with no recurring pattern found elsewhere in this audit; recorded here for completeness only.
