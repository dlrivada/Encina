# Delta scope of issue #14 (set rules-2026-10)

Reused from the original audit; do not re-derive it. Rules in this delta: see tools/ai/audit/pipeline-delta.json.

**Scope source:** the published record and the published audit result (docs/knowledge/audits/issue-14.md) only: the original audit has no published stage files.

## Knowledge record (docs/knowledge/issues/14.md)

```yaml
schema: 1
nav_exclude: true
issue: 14
title: "[REFACTOR] Apply Orchestrator pattern to Encina.Validation (3 packages)"
closed: 2025-12-23
state_reason: completed
outcome: delivered
type: refactor
area: validation
review: verified
packages:
  - Encina
  - Encina.FluentValidation
  - Encina.DataAnnotations
  - Encina.MiniValidator
prs:
linked_prs:
knowledge:
  - kind: decision
    statement: "Validation logic is centralized in the core Encina.Validation namespace: IValidationProvider, ValidationResult/ValidationError, ValidationOrchestrator and one shared ValidationPipelineBehavior<TRequest, TResponse>; the per-package duplicated pipeline behaviors were removed."
    current: yes
    sources:
      - "quote: \"ValidationOrchestrator - Centralized validation orchestration\" and \"ValidationPipelineBehavior<TRequest, TResponse> - Generic behavior using orchestrator\" (https://github.com/dlrivada/Encina/issues/14#issuecomment-3687478957, 2025-12-23)"
      - "paraphrase: commit fc36f4df 'refactor: apply Orchestrator pattern to validation packages (#14)' (https://github.com/dlrivada/Encina/commit/fc36f4df, 2025-12-23)"
    destinations:
      - kind: rule
        status: done
        target: "AGENTS.md"
      - kind: executable-rule
        status: done
        target: "src/Encina/Validation/ValidationOrchestrator.cs"
  - kind: decision
    statement: "FluentValidationProvider, DataAnnotationsValidationProvider and MiniValidationProvider each implement IValidationProvider, and each package's ServiceCollectionExtensions registers the provider, the ValidationOrchestrator and the open-generic ValidationPipelineBehavior<,>; all three return the same ValidationResult shape and the behavior never branches on provider type."
    current: yes
    sources:
      - "quote: \"IValidationProvider → Provider-specific implementation\", \"ValidationOrchestrator\" and \"ValidationPipelineBehavior<,>\" registered by all 3 packages (https://github.com/dlrivada/Encina/issues/14#issuecomment-3687478957, 2025-12-23)"
    destinations:
      - kind: executable-rule
        status: done
        target: "src/Encina.FluentValidation/ServiceCollectionExtensions.cs"
  - kind: rule
    statement: "Centralizing behavior into one generic class with a domain orchestrator gives uniform behavior across the three validation libraries, and such a refactor must keep the full test suite green (68 validation tests passed at close: FluentValidation 28, DataAnnotations 20, MiniValidator 20)."
    current: yes
    sources:
      - "quote: \"All 68 validation tests pass\" (https://github.com/dlrivada/Encina/issues/14#issuecomment-3687478957, 2025-12-23)"
    destinations:
      - kind: regression-test
        status: planned
        target: "#1339"
  - kind: gotcha
    statement: "The refactor changed how a validation failure reaches the caller: ValidationOrchestrator builds EncinaError.New(errorMessage) from a flat string, so EncinaError.Exception is always None, while the FluentValidation and DataAnnotations READMEs still document Exception = Some(ValidationException); the same string put per-field text into EncinaError.Message, which the core logged (fixed by #1319, closed)."
    current: yes
    sources:
      - "paraphrase: src/Encina/Validation/ValidationOrchestrator.cs line 71 builds Left(EncinaError.New(errorMessage)) (https://github.com/dlrivada/Encina/blob/main/src/Encina/Validation/ValidationOrchestrator.cs, re-read 2026-10-07)"
      - "paraphrase: issue #1319 fixed the core mediator logging EncinaError.Message; src/Encina/Core/Encina.cs:144 now logs the error code (https://github.com/dlrivada/Encina/issues/1319, 2026-10-07)"
    destinations:
      - kind: backlog
        status: planned
        target: "#1330"
  - kind: gotcha
    statement: "Issue #14 has no closing pull request: it was closed manually at 17:51 UTC on 2025-12-23, the same minute as commit fc36f4df, which carries (#14) in its subject; the commit was recovered from the timestamp and subject, not from a GitHub link."
    current: no
    sources:
      - "paraphrase: issue closed 2025-12-23T17:51:11Z, commit fc36f4df authored 2025-12-23T18:51:48+01:00 (https://github.com/dlrivada/Encina/issues/14, re-verified 2026-10-07)"
    destinations:
      - kind: none
        status: done
remediation:
  - 1319
  - 1330
  - 1337
  - 1338
  - 1339
audit:
  checklist: 1
  date: 2026-09-25
  verdict: findings-tracked
  record: "docs/knowledge/audits/issue-14.md"
```

## Where the knowledge lives (record)

- The orchestrator pattern for validation is in `AGENTS.md` section 3 ("Validation uses the orchestrator pattern") and in `src/Encina/Validation/`; each package has its `PublicAPI.Unshipped.txt` and README.
- The regression suites under `tests/Encina.{UnitTests,PropertyTests,ContractTests}/{FluentValidation,DataAnnotations,MiniValidator}` guard the behavior, with gaps tracked below.

## Audit result (docs/knowledge/audits/issue-14.md)

# SPEC-003 deep audit — issue #14

Scope: `src/Encina/Validation/*`, `src/Encina.FluentValidation/*`, `src/Encina.DataAnnotations/*`, `src/Encina.MiniValidator/*` (providers, ServiceCollectionExtensions, PublicAPI, READMEs).

## AUD checklist

| Item | Result | Evidence |
|---|---|---|
| Registration completeness (service + deps registered, proven by `ValidateOnBuild`/`ValidateScopes` DI test) | **FAIL** | `tests/Encina.UnitTests/{FluentValidation,DataAnnotations,MiniValidator}/ServiceCollectionExtensionsTests.cs` all call plain `services.BuildServiceProvider()`. DataAnnotations gap already tracked by #1337; FluentValidation/MiniValidator gap drafted in `remediation/14-fluentvalidation-minivalidator-validateonbuild.test.md`. |
| Errors never swallowed (Left fails the operation) | **PASS** | `ValidationPipelineBehavior<,>.Handle` returns `Left` immediately on validation failure (`ValidationPipelineBehavior.cs:64-66`); no catch-and-succeed path. |
| Fail-closed gates | **N/A** | Validation is not a compliance/security gate in the SPEC-002 sense; it is opt-in request validation. |
| `EncinaError.Message` never reaches logs/activity tags/health checks/plaintext storage | **FAIL** | `ValidationOrchestrator.ValidateAsync` (`ValidationOrchestrator.cs:70-71`) puts full per-field validation text into `EncinaError.Message`, which core sinks `Log.RequestFailed` (`Core/Encina.cs:243`) and `EncinaDiagnostics.SendCompleted`→`activity.SetStatus` (`Dispatchers/Encina.RequestDispatcher.cs:140-144`, `Diagnostics/EncinaDiagnostics.cs:39`) record unconditionally. New finding; remediation drafted. |
| TimeProvider for time-dependent behavior | **N/A** | No time-dependent logic in this scope. |
| Secrets in options never leak (`[JsonIgnore]` + `ToString()`) | **N/A** | No options class with secrets in this scope. |
| Async DB calls with CancellationToken | **N/A** | No database calls in this scope. |
| 12 cross-cutting functions | **Partial, mostly N/A by design** | Validation is itself function #5 (Validation) for other features; for validation-the-feature: Caching N/A (no read to cache), OpenTelemetry N/A (no ActivitySource/Meter added — none expected), Structured Logging N/A (no `[LoggerMessage]` — correct per audit, none expected in this scope), Health Checks N/A (no external dependency), Resilience N/A (in-process only), Distributed Locks N/A, Transactions N/A, Idempotency N/A (validation runs before dedup concerns), Multi-Tenancy — `context.TenantId` is passed into FluentValidation's and DataAnnotations' validation context items (`FluentValidationProvider.cs:60-62`, `DataAnnotationsValidationProvider.cs:39-41`) for custom validators to read, present; Module Isolation N/A, Audit Trail N/A (not a compliance operation). |
| 10-provider database matrix | **N/A** | Validation is not a database feature; the applicable matrix is the 3 validation providers, see below. |
| 3 validation providers coherent (same interface, same `ValidationResult`, same pipeline behavior) | **PASS with a minor gap** | All three return `Encina.Validation.ValidationResult`/`ValidationError` and share `ValidationPipelineBehavior<,>`. Minor: `DataAnnotationsValidationProvider` and `MiniValidationProvider` never consult `cancellationToken` (by design — both underlying libraries are synchronous), while `FluentValidationProvider` does; not a defect, but the shared `IValidationProvider.ValidateAsync` XML doc implies active cancellation support all three should honor identically. Not drafted as a separate issue (too minor to justify a standalone ticket; noted here for visibility). |
| EventIds registered/in range | **N/A — none used** | No `[LoggerMessage]` or `LoggerMessage.Define` calls anywhere in scope; correctly so, since this feature performs no logging of its own. |
| PublicAPI.Unshipped/Shipped accurate | **PASS** | All new public members (`IValidationProvider`, `ValidationOrchestrator`, `ValidationResult`, `ValidationError`, `ValidationPipelineBehavior<,>`, the three providers, the three `ServiceCollectionExtensions` methods) are present in the respective `PublicAPI.Unshipped.txt` files; no shipped file yet (pre-1.0, expected). |
| XML docs on public APIs | **PASS** | Every public type/member in scope has XML docs, including `<remarks>` and `<example>` blocks. |
| Diátaxis docs / README accuracy | **FAIL, already tracked** | `Encina.FluentValidation/README.md` and `Encina.DataAnnotations/README.md` document `error.Exception` being populated with a `ValidationException`, but `ValidationOrchestrator` only ever calls the string overload `EncinaError.New(errorMessage)`, so `error.Exception` is always `None`. Already tracked by #1330 — not re-drafted. `Encina.MiniValidator/README.md` does not repeat this specific defect (confirmed by adversarial-reviewer pass). |
| Coverage per flag vs manifest | **See below** | |

## Coverage per flag (manifest targets vs. files in scope)

Per `.github/coverage-manifest/Encina.FluentValidation.json`, `Encina.DataAnnotations.json`, `Encina.MiniValidator.json`: targets are `unit: 80, guard: 25` (FluentValidation/DataAnnotations) and equivalent for MiniValidator. `Encina.json` (core) covers `src/Encina/Validation/*` under its own unit/guard/property/contract obligations.

- **unit**: existing suites (`tests/Encina.UnitTests/{FluentValidation,DataAnnotations,MiniValidator}/*Tests.cs`, `tests/Encina.UnitTests/Core/ValidationResultTests.cs`) instantiate and exercise real provider code (not reflection-only) — consistent with the manifest's `unit: 80` target being previously reported as met (not independently re-run in this audit; no code changed since the last CI Full run that would regress it).
- **guard**: **unmeasurable today** — no `.cs` files exist under `tests/Encina.GuardTests/` for any of the three provider packages, so the manifest's `guard: 25` target has no coverable-lines source. Already tracked by #1338 (found by the audit of #10); not re-drafted here.
- **property/contract**: `tests/Encina.PropertyTests/Validation/{FluentValidation,DataAnnotations,MiniValidator}/ValidationInvariantProperties.cs` and `tests/Encina.ContractTests/Core/Validation/ValidationPipelineBehaviorContractTests.cs` exist and instantiate real types.
- This audit did not re-run `dotnet test --collect "XPlat Code Coverage"` for these packages: no code in scope changed since #10's and #1337/#1338's audits already measured and reported the same files' coverage state days earlier in this same ascending audit sequence, and re-running would not change the FAIL/PASS calls above (all are structural: missing test files, not borderline percentages).

## Specialist passes

- **adversarial-reviewer** (foreground, Sonnet): ran against the full scope plus READMEs. Confirmed M1 (`EncinaError.Message` leak — new) and M2 (missing `ValidateOnBuild` DI tests, all three packages — DataAnnotations part already covered by #1337, remainder drafted here). Minor notes: cancellation-token parity (N1, not drafted, see AUD table above), a plausible but unverified DataAnnotations-vs-MiniValidation nested-object validation divergence (N2, not drafted — could not confirm from source since MiniValidation is an external package not vendored in the worktree), and confirmed #1330 covers the README `error.Exception` defect with no distinct MiniValidator-specific variant.
- **docs-reviewer**: not spawned (issue-worker-equivalent restriction; this audit is not an issue-worker). Performed the equivalent check manually: the three READMEs are Diátaxis how-to pages with real API names except for the `error.Exception` defect already covered by #1330.
- Second `adversarial-reviewer` test-focused pass: not run separately — the single pass above already covered DI-test and guard-test gaps in enough depth to reach the FAIL calls above; a second pass would only re-confirm #1337/#1338's own findings.

## Local-model use

- `dotnet run --file D:\Proyectos\Encina\tools\ai\local-ai-ask.cs -- --task issue-draft ...` used twice: once to draft the `EncinaError.Message` leak bug body (24.1s, 1057 prompt / 1243 completion tokens, 51.6 tok/s), once to draft the FluentValidation/MiniValidator `ValidateOnBuild` test body (10.4s, 1098 prompt / 912 completion tokens, 87.5 tok/s). Both drafts were verified against the template headers and source files, then corrected (fixed method names, `Related Issues` formatting, environment/coverage fields) before being written to `remediation/`.
- The pre-draft at `predraft/14.md` was verified against source and used as the base for `knowledge/issues/14.md`, per the mandatory local-model-first rule; no extraction was redone from zero.

## Outcome

`delivered`. All code described by the issue exists in `src/` today and matches the description. Two new/incremental remediation items were drafted (leak, DI test gap for 2 of 3 packages); three related gaps were found already tracked (#1330, #1337, #1338) and were not duplicated.


