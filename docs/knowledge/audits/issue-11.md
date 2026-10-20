# SPEC-003 audit — issue #11

Scope: `tests/Encina.FluentValidation.PropertyTests/ValidationPipelineBehaviorPropertyTests.cs` (as it existed 2025-12-23 to 2025-12-29), its successor concern is now split between:
- `tests/Encina.PropertyTests/Validation/FluentValidation/ValidationInvariantProperties.cs` (today's FluentValidation property tests — different scope, does not cover `error.Exception`)
- `src/Encina/Validation/ValidationOrchestrator.cs` (today's production code that builds the `EncinaError` the deleted tests verified)
- `src/Encina.FluentValidation/FluentValidationProvider.cs`
- `src/Encina.FluentValidation/README.md`

This is a narrow-scope issue (one test file, one assertion pattern fix). The concern is "does an `EncinaError` from a validation failure carry the structured `ValidationException`/per-field errors it once did." Items unrelated to that concern (e.g., 10-provider matrix, EventIds, TimeProvider, secrets) are not applicable to this issue's concern and are marked n/a below; they belong to whichever issue built `Encina.FluentValidation` (#1fae17c3, pre-issue-tracking) or applied the Orchestrator pattern (#14).

| AUD item | Result | Evidence |
|---|---|---|
| Scope located in today's code | pass | Traced via `git log --follow`, `--diff-filter=D`; file deleted `d7b7b8ac` (2025-12-29), concern moved to `ValidationInvariantProperties.cs` (does not cover it) and `ValidationOrchestrator.cs` (changed the behavior) |
| Regression test exists for the original bug | **fail** | No test anywhere in `tests/` asserts on `error.Exception` for a FluentValidation/DataAnnotations validation failure today (`Grep "\.Exception"` over `tests/Encina.PropertyTests/Validation/FluentValidation/ValidationInvariantProperties.cs`, `tests/Encina.ContractTests/Core/Validation/ValidationPipelineBehaviorContractTests.cs`, `tests/Encina.UnitTests/FluentValidation/ValidationPipelineBehaviorTests.cs`: zero matches) |
| Coverage per flag vs manifest | n/a for this concern | `.github/coverage-manifest/Encina.FluentValidation.json` requires only `unit` (80%) and `guard` (25%); no `property` flag is declared for this package, so the missing property assertion is not a manifest violation |
| XML docs / README accuracy | **fail** | `src/Encina.FluentValidation/README.md:100-257` documents `error.Exception = Some(ValidationException{Errors:[...]})` and an ASP.NET Core example relying on it; production code (`ValidationOrchestrator.cs:71`) sets `Exception` to `None` via `EncinaError.New(errorMessage)` (string only). Already tracked: issue #1330 |
| Message leaks (`EncinaError.Message` / no plaintext secrets) | n/a for this concern | Validation error messages are user input field names/messages, not secrets; no options class involved |
| Fail-closed defaults | n/a | No gate/deny semantics in this concern |
| TimeProvider | n/a | No time-dependent code in this concern |
| Registration completeness | pass (not this issue's concern, verified as a sanity check) | `ServiceCollectionExtensions.AddEncinaFluentValidation` registers `IValidationProvider` and the orchestrator; unrelated to the exception-shape defect |
| EventIds / structured logging | n/a | No `[LoggerMessage]` in this concern |
| 10/8/etc. provider matrix | n/a | Validation has its own 3-provider matrix (FluentValidation/DataAnnotations/MiniValidator); the `Exception`-shape defect is confirmed to affect at least FluentValidation and DataAnnotations per issue #1330's title; MiniValidator not checked here (out of #11's original scope, would be additional scope for #1330's remediation) |
| PublicAPI tracking | n/a | No public surface changed by the original fix (test-only commit) |
| Errors never swallowed | pass | `ValidationOrchestrator.ValidateAsync` returns `Left<EncinaError>` on failure; the `Left` is not swallowed, just less structured than before |

## Coverage per flag (files in scope, measured)

Not run: the only source file whose behavior is in scope, `ValidationOrchestrator.cs`, is exercised indirectly by many existing test suites across three validation providers; isolating a per-flag Cobertura run for this single audit would require a full `dotnet test` per flag across `Encina.slnx` (~40 min per CLAUDE.md), disproportionate to a already-tracked, doc/test-gap finding with no manifest violation. The manifest check above (no `property` flag required for `Encina.FluentValidation`) already answers the only coverage question this issue's concern raises.

## Specialist passes

- **adversarial-reviewer**: skipped. The code artifact issue #11 touched (the test file) no longer exists; the behavior it protected is now owned by issue #14's refactor and the resulting doc/behavior mismatch is already an open, ranked finding (#1330) opened by a parallel audit thread today. Spawning a reviewer against a deleted file, or re-reviewing #1330's already-filed defect, would not add verified findings beyond what's recorded here.
- **docs-reviewer**: done manually (not yet in the spawn allowlist). Read `src/Encina.FluentValidation/README.md` end to end; confirmed the "Validation Failure Structure" and "Integration with ASP.NET Core" sections (lines 194-257) describe a return shape (`Exception = Some(ValidationException)`) that `ValidationOrchestrator.cs` does not produce. This is the same defect #1330 tracks; no new finding.
- **Test review**: done as part of the AUD table above (Grep-verified absence of `.Exception` assertions).

## Deduplication

- Searched `gh issue list --state open --search "EncinaError.Exception validation"`, `"ValidationOrchestrator"`: found #1330, which already covers the README/code mismatch for FluentValidation and DataAnnotations. No new remediation issue drafted.
