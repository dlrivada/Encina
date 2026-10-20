# Delta scope of issue #11 (set rules-2026-10)

Reused from the original audit; do not re-derive it. Rules in this delta: see tools/ai/audit/pipeline-delta.json.

**Scope source:** the published record and the published audit result (docs/knowledge/audits/issue-11.md) only: the original audit has no published stage files.

## Knowledge record (docs/knowledge/issues/11.md)

```yaml
schema: 1
nav_exclude: true
issue: 11
title: "[DEBT] Fix FluentValidation PropertyTests - error details verification"
closed: 2025-12-23
state_reason: completed
outcome: delivered
type: debt
area: validation
review: verified
packages:
  - Encina.FluentValidation
prs:
linked_prs:
knowledge:
  - kind: decision
    statement: "Two FluentValidation property tests were fixed to treat EncinaError.Exception as Option<Exception>: the direct cast (ValidationException)error.Exception! became error.Exception.IsSome plus IfSome(ex => ex.ShouldBeOfType<ValidationException>()). The root cause named in the issue was the Mediator-to-Encina rename changing the error structure without the test assertions being updated."
    current: no
    sources:
      - "quote: \"Fixed in commit 3f20fec - error.Exception is Option<Exception>, fixed to use IsSome/IfSome pattern.\" (https://github.com/dlrivada/Encina/issues/11#issuecomment-3684610454, 2025-12-23)"
      - "paraphrase: the commit diff changes both hunks to the IsSome/IfSome pattern (https://github.com/dlrivada/Encina/commit/3f20fec8f4ebbe98317be9ec02e8b9755ed19dc2, 2025-12-23)"
    destinations:
      - kind: none
        status: done
  - kind: direction-change
    statement: "The invariant the fixed tests protected was invalidated by the Orchestrator refactor of #14: ValidationOrchestrator now builds the failure with EncinaError.New(errorMessage), a flat string, so Exception is always None for a validation failure, while the FluentValidation README still documents Exception = Some(ValidationException) and no test asserts on Exception for a validation failure."
    current: yes
    sources:
      - "paraphrase: commit fc36f4df 'refactor: apply Orchestrator pattern to validation packages (#14)' changed the production code the deleted tests protected (https://github.com/dlrivada/Encina/commit/fc36f4df, 2025-12-23)"
      - "paraphrase: src/Encina/Validation/ValidationOrchestrator.cs builds Left(EncinaError.New(errorMessage)) at line 71 (https://github.com/dlrivada/Encina/blob/main/src/Encina/Validation/ValidationOrchestrator.cs, re-read 2026-10-07)"
    destinations:
      - kind: backlog
        status: planned
        target: "#1330"
  - kind: gotcha
    statement: "The test file the fix touched was deleted five days later as an incidental side effect of an unrelated commit (d7b7b8ac, 462 lines, no replacement), and its successor ValidationInvariantProperties.cs never asserts on error.Exception, so the Option<Exception> invariant was dropped without anyone deciding it."
    current: yes
    sources:
      - "paraphrase: commit d7b7b8ac 'Add unit tests for domain modeling components' deleted tests/Encina.FluentValidation.PropertyTests/ValidationPipelineBehaviorPropertyTests.cs (https://github.com/dlrivada/Encina/commit/d7b7b8acaaad78b3d15b51f7cf4de5ea72b607ff, 2025-12-29)"
    destinations:
      - kind: backlog
        status: planned
        target: "#1330"
  - kind: rule
    statement: "When a core error type changes shape, every property, unit and contract test that asserts on it must be updated in the same change, not discovered later as a CI failure (tracked jointly with #7)."
    current: unknown
    sources:
      - "paraphrase: the root cause named in the issue and the closing comment, jointly tracked with #7 (https://github.com/dlrivada/Encina/issues/11, 2025-12-23)"
    destinations:
      - kind: none
        status: done
  - kind: gotcha
    statement: "Issue #11 has no closing PR: the closed event has no commit_id, and the one cross-referenced PR (#849, tamper-evident audit attestation) is an unrelated auto cross-reference, not the fix; the fix is the direct commit 3f20fec8."
    current: no
    sources:
      - "quote: closed event with commit_id null; cross-reference only from PR #849 (gh api repos/dlrivada/Encina/issues/11/timeline, https://github.com/dlrivada/Encina/issues/11, re-verified 2026-10-07)"
    destinations:
      - kind: none
        status: done
remediation:
  - 1330
audit:
  checklist: 1
  date: 2026-09-25
  verdict: findings-tracked
  record: "docs/knowledge/audits/issue-11.md"
```

## Where the knowledge lives (record)

- Regression test for the `error.Exception` invariant: missing, and moot until #1330 decides what shape the structured errors reach callers in.
- `src/Encina.FluentValidation/README.md` still documents the pre-#14 behavior; tracked by #1330 (open).
- The manifest `.github/coverage-manifest/Encina.FluentValidation.json` requires only `unit` and `guard` flags, so the missing property assertion is not a manifest violation.

## Audit result (docs/knowledge/audits/issue-11.md)

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


