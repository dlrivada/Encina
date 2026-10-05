# Audit result — issue #7

**Checklist version:** 1 (SPEC-003 §5.2, amended §15.3)
**Date:** 2026-09-25
**Audit unit:** issue #7, "[DEBT] Fix 57 failing tests across multiple packages" (closed 2025-12-23, COMPLETED)

## Scope

Files touched by the closing commits (no PR; direct commits to `main`, pre-review-pipeline era):

| File as touched by #7 | State today | Evidence |
|---|---|---|
| `tests/Encina.Dapper.Sqlite.ContractTests/Scheduling/ScheduledMessageStoreDapperContractTests.cs` | **code-removed**: `Encina.Dapper.Sqlite` / `Encina.ADO.Sqlite` were removed from the supported provider matrix (ADR-024, SQLite removal pre-1.0); no trace under `src/` or `tests/` beyond build artifacts | `git log --follow` on the path returns nothing after the removal; `docs/architecture/adr/024-remove-sqlite-provider-pre-1.0.md` |
| `tests/Encina.DataAnnotations.PropertyTests/DataAnnotationsValidationBehaviorPropertyTests.cs` | merged into `tests/Encina.PropertyTests/ValidationProviderProperties.cs` by the test-consolidation effort | `Grep` for `NoValidationCommand`/`ContextAwareCommand` across `tests/`; only `ValidationProviderProperties.cs:98` has `NoValidationCommand`, `ContextAwareCommand` is gone |
| `tests/Encina.FluentValidation.PropertyTests/ValidationPipelineBehaviorPropertyTests.cs` | merged into the same `ValidationProviderProperties.cs`; the `Option<Exception>` assertion pattern is gone (the consolidated file tests through `ValidationResult.IsValid`/`IsInvalid`/`Errors` instead) | same file, read in full |
| `.github/workflows/sonarcloud.yml`, `ci.yml`, `dotnet-ci.yml` filter changes | superseded by the current matrix-sharded CI | current `.github/workflows/ci.yml` has no `FullyQualifiedName!~...` exclusion of ContractTests/PropertyTests |

Per SPEC-003 §15.4, a broad issue audits its concern across every package it touched in one pass. This issue's concern (test reliability for the DataAnnotations, FluentValidation and Dapper.Sqlite validation/scheduling test suites) is audited above; the Dapper.Sqlite/ADO.Sqlite provider matrix itself is out of scope (code-removed, ADR-024).

AUD-11 (README/docs accuracy) runs once per package (§15.3); this is the first audit to touch `Encina.DataAnnotations` and `Encina.FluentValidation`, so it runs here.

## Checklist

| ID | Outcome | Evidence |
|---|---|---|
| AUD-01 | **pass** | The issue's own decisions (public-type requirement for `CustomValidationAttribute`; `Option<Exception>` access pattern) still hold as general .NET/LanguageExt facts; the SQLite-specific fix is code-removed (ADR-024), recorded above. |
| AUD-02 | n/a | The issue created no entity, store, pipeline behavior, background service or external integration; it only adjusted test code. |
| AUD-03 | n/a | The issue touched no source file tracked by a coverage manifest. `.github/coverage-manifest/Encina.DataAnnotations.json` and `Encina.FluentValidation.json` do not even declare a `property` flag target (only `unit` 80 / `guard` 25), so the property-test file this issue touched was never a manifest obligation. |
| AUD-04 | **pass** | `tests/Encina.PropertyTests/ValidationProviderProperties.cs` (the current descendant) instantiates real `DataAnnotationsValidationProvider`/`FluentValidationProvider`/`MiniValidationProvider` via DI and calls `ValidateAsync`; no reflection-only or assert-only-on-type pattern. Verified by `adversarial-reviewer` and confirmed running `dotnet test tests/Encina.PropertyTests --filter FullyQualifiedName~ValidationProviderProperties`: 17/17 passed. |
| AUD-05 | **partial** | Property tests exist and pass (17/17). However, two scenarios the original #7 fix exercised have no test anywhere today: (1) `CustomValidationAttribute` + `ValidationContext`, and (2) an assertion on `EncinaError.Exception` for a validation failure. Tracked as minor findings below (not a missing test *type*, a missing *scenario* within an existing type). |
| AUD-06 | n/a | Issue type is `debt`, not `bug` (§15.3 amendment: AUD-06 is not applicable when the issue's evidence includes no closed bug). |
| AUD-07 | n/a | Provider-agnostic: the issue fixed already-implemented validation providers' tests; it added no new provider implementation. |
| AUD-08 | n/a | No logging touched. |
| AUD-09 | n/a | No public API surface touched (test-only files; test projects carry no `PublicAPI.*.txt`). |
| AUD-10 | n/a | Same reasoning as AUD-09. |
| AUD-11 | **finding (major)** — done by the coordinating worker directly (docs-reviewer not yet spawnable by issue-worker; noted per the audit brief) | `src/Encina.DataAnnotations/README.md` lines 228-250 ("Validation Failure Structure") and lines 88-118 (Quick Start `error.Exception.IfSome(ex => ...)`); `src/Encina.FluentValidation/README.md` lines 194-212 and 91-115 (same pattern) document `EncinaError.Exception = Some(ValidationException {...structured per-field errors...})` for an ordinary validation failure. `src/Encina/Validation/ValidationOrchestrator.cs:71` (`return Left<EncinaError, Unit>(EncinaError.New(errorMessage));`) shows `Exception` is never set on this path — it stays `None`; only the cancellation branch (lines 76-79) sets `Exception`. The documented code path never executes. Confirmed by `adversarial-reviewer` and by reading `ValidationOrchestrator.cs` directly; `git log` on the file (`fc36f4df`, issue #14) shows this has been the behaviour since the orchestrator's creation — the READMEs were never updated to match. |
| AUD-12 | n/a | Not a security/compliance/audit/personal-data unit. |
| AUD-13 | n/a | `ValidationOrchestrator.ValidateAsync` performs no logging, activity tagging or metric tagging; nothing in this issue's scope emits `EncinaError.Message` to a sink. |
| AUD-14 | **pass** | No `DateTime.UtcNow`/`DateTimeOffset.UtcNow` in `ValidationOrchestrator.cs` or in `ValidationProviderProperties.cs` (the original SQLite test that used `DateTime.UtcNow` is code-removed). |
| AUD-15 | n/a | No options classes touched. |
| AUD-16 | n/a | No database calls in scope (Dapper.Sqlite part is code-removed). |
| AUD-17 | n/a | No `AddEncina*` extension touched by this issue. |
| AUD-18 | **pass** | No `[Obsolete]` member found in `ValidationProviderProperties.cs` or `ValidationOrchestrator.cs`; confirmed by `adversarial-reviewer`. |

## Coverage per flag (files in scope)

No source file touched by this issue is tracked by a coverage manifest (AUD-03 n/a above); `Encina.DataAnnotations.json` and `Encina.FluentValidation.json` declare only `unit`/`guard` targets, no `property` target, so the property-test file itself carries no coverage obligation. For completeness, the test run was executed with coverage collection:

```
dotnet test tests/Encina.PropertyTests/Encina.PropertyTests.csproj --filter "FullyQualifiedName~ValidationProviderProperties" --collect "XPlat Code Coverage" --results-directory artifacts/audit/coverage/property -c Release
```

Result: 17/17 passed, `artifacts/audit/coverage/property/*/coverage.cobertura.xml` produced. No `covref` citation is made because no manifest entry applies to the files in scope.

## Specialist passes

- **`adversarial-reviewer`** (foreground): reviewed `tests/Encina.PropertyTests/ValidationProviderProperties.cs` and the DataAnnotations/FluentValidation production files it exercises, against issue #7's goal and today's `CLAUDE.md` standards. Found no blocker; two minor findings (coverage gaps, folded into AUD-05 above) plus, independently, confirmed the `EncinaError.Exception`/README mismatch that became the AUD-11 major finding. Full report in the session transcript; 119,900 subagent tokens.
- **`docs-reviewer`**: not spawned. Per the audit brief, `docs-reviewer` cannot yet be spawned by `issue-worker` (spawn allowlist gap); the coordinating worker performed the AUD-11 check directly instead, reading both READMEs and `ValidationOrchestrator.cs` line by line.
- **Test review**: performed by the coordinating worker (§15.4): ran the current test file with coverage, confirmed no reflection-only tests, and searched the whole `tests/` tree for `CustomValidationAttribute` and for assertions on `EncinaError.Exception` in a validation context, finding none in either case.

## Remediation

One new remediation issue drafted (no open-issue duplicate found; searched `EncinaError.Exception validation README`, `ValidationException README`, `CustomValidationAttribute`):

- `artifacts/knowledge/remediation/7-validation-readme-and-test-gaps.md` — `[BUG]`, groups the AUD-11 major finding and the two AUD-05 minor coverage-gap findings for `Encina.DataAnnotations` + `Encina.FluentValidation`, milestone `v0.14.0 — Hardening`.
