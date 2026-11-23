# Implementation Plan: Compliance Dry-Run Report (One Evaluation Contract, an Encina.Testing Helper and an Encina.Cli Command, Replacing Warn)

> **Issue**: [#3112](https://github.com/dlrivada/Encina/issues/3112)
> **Type**: Feature (compliance, tooling)
> **Related**: #3089 (EPIC: compliance controls are never optional) and its children #3091, #3092, #3093, #3094, #3095, #3096, #3098, #3099, #3100, #3101, #3102, #3103, #3104, #3105, #3106, #3107 (each removes a Warn mode; this plan is the replacement and never duplicates them), #3090 (the architecture test that keeps Warn out of every public API), #2983 (Encina.Cli command host and contract, ADR-058) and #2612 (Encina.Cli compliance commands), ADR-058 (CLI command architecture), ADR-054, ADR-062, ADR-036 (audit stores), SPEC-002 DEC-006
> **Complexity**: Medium (a small contract, one service, one helper, one command; the care is in not recreating a runtime mode and in the identity a dry-run runs under)
> **Estimated Scope**: ~650 lines of production code + ~900 lines of tests, in five pull requests of at most 15 files (the issue's own size line, 14 files and 900 lines, covers D1 alone)

---

## Summary

### What exists today (read on 2026-11-23, main `ce352001f`)

- Fifteen public enforcement-mode enums exist, one per package: `AIActEnforcementMode`, `AnonymizationEnforcementMode`, `BreachDetectionEnforcementMode`, `ConsentEnforcementMode`, `CrossBorderTransferEnforcementMode`, `DataResidencyEnforcementMode`, `DSREnforcementMode`, `DPIAEnforcementMode`, `GDPREnforcementMode`, `LawfulBasisEnforcementMode`, `NIS2EnforcementMode`, `PrivacyByDesignEnforcementMode`, `ProcessorAgreementEnforcementMode`, `RetentionEnforcementMode` and `ABACEnforcementMode`. `ConsentEnforcementMode` still has `Warn` ("non-compliant requests log a warning but are allowed to proceed") and `Disabled`. The children of #3089 remove those modes; this plan does not touch them.
- The policy decision is interleaved with the pipeline behavior. In `ConsentRequiredPipelineBehavior` (`src/Encina.Compliance.Consent/ConsentRequiredPipelineBehavior.cs`) subject extraction (`ExtractSubjectId`, line 194), the validator call (`IConsentValidator.ValidateAsync`, `src/Encina.Compliance.Consent/Abstractions/IConsentValidator.cs`) and the Block, Warn or skip branch live in one `Handle`. There is no way to ask "what would be denied" without running the request pipeline.
- The compliance packages all reference core `Encina`; none references another compliance package for shared contracts, and there is no `Encina.Compliance.Abstractions` package.
- `Encina.Testing` (`src/Encina.Testing/`) has `TestIdentity` (`User`, `Service`, `Anonymous`) and `TestRequestContext.For(...)`/`WithIdentity(...)` (`Identity/`), the only supported way to build an authenticated identity outside an Encina entry point; `RequestIdentity` says application code cannot mint one.
- `Encina.Cli` (`src/Encina.Cli/`) is a global tool of three internal command factories (`NewCommand`, `GenerateCommand`, `AddCommand`) built on `System.CommandLine` and `Spectre.Console`; it writes files and starts no application. ADR-058 D2 decides that commands touching data run inside the application's real container through a command host and a shared command contract, which is #2983 (open, `needs-decision`, no plan on main). #2612 plans the other compliance commands on the same host.
- The issue text is a template with sections "to be defined in the plan" (interfaces, DI, key types); its cross-cutting table marks multi-tenancy and audit trail not applicable, which the plan revisits below (a dry-run run for a tenant and a probe of consent states are both tenant data and personal data).

### What this plan delivers

- **Contract in core `Encina`**, folder `src/Encina/Compliance/DryRun/`, namespace `Encina.Compliance.DryRun`: `IComplianceDryRunEvaluator` (one per compliance module; `string Control { get; }` and `ValueTask<Either<EncinaError, IReadOnlyList<DryRunFinding>>> EvaluateAsync(object request, IRequestContext context, CancellationToken cancellationToken)`), `DryRunFinding` (`Control`, `DryRunOutcome`, `Code`, `Category`, `Detail`), `DryRunOutcome` (`WouldAllow`, `WouldDeny`, `NotApplicable`, `Indeterminate`; no zero value), `ComplianceDryRunReport` (the findings, `EvaluatedAtUtc`, `bool WouldDeny`), `ComplianceDryRunService` (runs every registered evaluator, never dispatches the request, never calls `IEncina`), `DryRunErrorCodes` and `AddEncinaComplianceDryRun()`.
- **Fail closed**: an evaluator that throws becomes an `Indeterminate` finding (code `encina.compliance.dry_run.evaluator_failed`, `Detail` = the exception type name) and `Indeterminate` counts as a denial in `WouldDeny`; no registered evaluator returns `Left` (`encina.compliance.dry_run.no_evaluators`, `FailedPrecondition`); multi-tenancy on with no tenant returns `Left` (`tenant_required`, `FailedPrecondition`).
- **`Encina.Testing` helper**: `ComplianceDryRunHelper.EvaluateAsync(IServiceProvider, request, RequestIdentity? identity, string? tenantId, CancellationToken)` and `ComplianceDryRunAssertions` (`ShouldBeAllowed`, `ShouldBeDeniedBy(control)`), both over the same service.
- **`Encina.Cli` command** `encina compliance dry-run --request <type> --input <json> [--tenant <id>] [--output table|json]`, contributed through the host of #2983 (so it runs in the application's container); exit code 0 when nothing would be denied, 2 when something would be, 1 on error.
- **Reference evaluator**: `ConsentDryRunEvaluator` in `Encina.Compliance.Consent`, built on policy code extracted from `ConsentRequiredPipelineBehavior` that the behavior and the evaluator both call. The other fourteen modules follow in per-module batches (issue file `artifacts/issues/compliance-dry-run-evaluators.md`).
- **Not delivered**: removal of any Warn mode (the children of #3089), the architecture test that Warn is absent from every public API (#3090), and the evaluators of the other fourteen modules.
- Affected packages: `Encina` (core), `Encina.Testing`, `Encina.Cli`, `Encina.Compliance.Consent` (reference evaluator). Provider category: none (the dry-run reads policy through each module's own abstractions).

---

## Design Choices

<details>
<summary><strong>1. How the dry-run reuses the policy code</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One `IComplianceDryRunEvaluator` per module, over policy code extracted from the behavior so both call the same method; the service runs the evaluators and never the pipeline** | Satisfies "runs the same policy code without the request pipeline" and "never changes the outcome of a request"; no runtime flag; each module keeps its own semantics | Each of fifteen modules needs a small extraction and an evaluator (batched follow-up) |
| **B) Run the real behaviors in a "recording" mode switched by a dry-run flag in the request context** | No extraction | Re-creates a runtime mode inside every behavior, which is exactly what #3089 removes; the first denial stops the chain, so later controls are never evaluated |
| **C) Static reading of the `[Require*]` attributes of a request type** | No evaluation code | Reports which controls apply, never what would be decided for a subject, tenant or identity |

### Chosen Option: **A - Evaluators over shared policy code** (recommended, pending the maintainer)

### Rationale

- A wins because the issue's own sentence asks for "the same policy code" outside the pipeline; B wins only if the maintainer accepts a permanent dry-run branch in every behavior, which #3089's principle argues against; C wins for a purely documentary inventory of controls, which the existing `docs` pages already provide. The extraction is also what lets a contract test assert that behavior and evaluator agree for the same inputs.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Where the contract lives</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Core `Encina`, folder `Compliance/DryRun/`** | Every compliance package already references `Encina`, so no csproj edit in fifteen modules; `Encina.Testing` and `Encina.Cli` reference core already | Core gains a compliance-flavored folder |
| **B) A new `Encina.Compliance.Abstractions` package** | Core stays free of compliance vocabulary | A new project, README and manifest for six small types; fifteen modules plus `Encina.Testing` and `Encina.Cli` gain a ProjectReference |

### Chosen Option: **A - Core folder** (recommended, pending the maintainer)

### Rationale

- A wins on cost: the contract is six types with no dependency beyond core. B wins if the maintainer wants a compliance-only dependency surface for applications that must not reference core compliance vocabulary, which no current package needs.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Which identity a dry-run evaluates under</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The context passed in: tests give a `TestIdentity`; the CLI uses the operator's own authenticated context (ADR-058 D14). Subject-bound policies (consent of the data subject named in the request) read the subject from the request as the real behavior does** | No new way to mint an identity; the dry-run answers "what would this request do for this caller" | The operator cannot ask "as user X" in production |
| **B) A simulation facility that lets an operator dry-run as another identity** | Answers "what would user X be denied" | A new identity-minting path: `RequestIdentity` says application code cannot mint an authenticated identity; a security-sensitive feature of its own |

### Chosen Option: **A - The context passed in** (recommended, pending the maintainer)

### Rationale

- A wins now: it needs no new trust rule and matches the helper's test identities. B wins if operators need to rehearse enforcement for a population before switching an application on; it needs its own design (who may simulate, audit, tenant checks) and is not part of the issue.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Whether a CLI dry-run is audited</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The command writes one `IOperationAuditStore` entry per run (operator, tenant, request type name, finding codes, never values or messages) through the host of #2983, and goes to the read audit when `IReadAuditStore` is enabled and an evaluator reads personal data** | A dry-run exposes whether a data subject has consent, a read of personal data; ADR-058 D19 leaves "whether dry-runs are audited" to the plans; an audit failure fails the command (D17) | One more dependency on the audit stores for the CLI path |
| **B) No audit, as the issue's table states** | Simplest; the dry-run changes nothing | An operator could probe consent or residency states without a trace |

### Chosen Option: **A - Audited through the host** (recommended, pending the maintainer)

### Rationale

- A wins for any deployment that treats consent state as personal data (SPEC-002); B wins for a development tool run only against test data, which the Testing helper already covers without audit. The Testing helper never audits (no store, no operator).

**Class:** Dilemma

</details>

### Register of defaults and semantics

| Id | Choice | Class | Basis |
|----|--------|-------|-------|
| C1 | Fail closed: an evaluator exception is an `Indeterminate` finding that counts as a denial; zero evaluators is `Left` `encina.compliance.dry_run.no_evaluators` (`ErrorCategory.FailedPrecondition`), never "everything allowed"; an evaluator `Left` is returned unchanged | Settled | `AGENTS.md` section 3 (gates fail closed; errors never swallowed); ADR-062 |
| C2 | Multi-tenancy on with no tenant in the context returns `Left` `encina.compliance.dry_run.tenant_required` (`FailedPrecondition`); the reserved `default` tenant segment is never invented; no tenant-qualified id is built here | Settled | ADR-058 D13; ADR-054 D4 |
| C3 | The CLI command is contributed to the in-app host of #2983, not built into the standalone scaffolding tool: the dry-run needs the application's real policies, stores and tenant wiring | Settled | ADR-058 D2 and D7 (a) |
| C4 | The report carries codes, categories, outcomes and at most an exception type name: never `EncinaError.Message`, a subject id, a user id or a claim value; the CLI table and JSON print only those fields | Settled | `AGENTS.md` section 3 (`EncinaError.Message` never reaches logs or output stores); ADR-054 settled point 2 |
| C5 | The service never calls `IEncina.Send` and evaluators call read-only policy members only; a contract test per evaluator runs the behavior and the evaluator on the same inputs and asserts equal decisions and no write to the module's stores | Trivial | Issue acceptance "the dry-run report does not change any request outcome" |
| C6 | New error codes: `encina.compliance.dry_run.no_evaluators` and `tenant_required` (`FailedPrecondition`), `invalid_input` (`Validation`, a null request) | Settled | ADR-062 |
| C7 | `EvaluatedAtUtc` comes from an injected `TimeProvider` (optional parameter, default `TimeProvider.System`); no `DateTime.UtcNow` | Settled | `AGENTS.md` section 3 |
| C8 | No logging and no EventId: the result is returned data, not an operational event; the CLI prints it | Settled | `AGENTS.md` section 7 (a range is claimed only for `[LoggerMessage]` code) |
| C9 | CLI exit codes: 0 nothing would be denied, 2 at least one denial or `Indeterminate`, 1 usage or execution error | Trivial | Common CLI convention for check commands (`dotnet format --verify-no-changes`, `git diff --exit-code`) |
| C10 | "Warn absent from every public API" (acceptance 1) is enforced by the architecture test of #3090 and by each child of #3089; this plan adds no reflection-only test for it | Settled | #3090; `AGENTS.md` section 9 (reflection-only tests cover zero lines) |

---

## Implementation Phases

### Delivery order and size

Five pull requests, each building and passing alone with its own tests. D1 to D3 start on `main` today; D3 merges after the Consent child #3101 (both edit `ConsentRequiredPipelineBehavior.cs`); D4 starts when the host contract of #2983 is decided and merged.

| PR | Phase | Content | Files |
|----|-------|---------|-------|
| D1 | 1 | Core contract, service, registration | 14 |
| D2 | 2 | `Encina.Testing` helper and assertions | 6 |
| D3 | 3 | Reference evaluator in `Encina.Compliance.Consent` | 10 |
| D4 | 4 | `Encina.Cli` command (after #2983) | 8 |
| D5 | 5 | Guide, ADR, INVENTORY, ROADMAP | 4 |

### Phase 1: Contract, Service and Registration (pull request D1)

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** in `src/Encina/Compliance/DryRun/`: `DryRunOutcome.cs`, `DryRunFinding.cs` (`sealed record`, validates `Control` and `Code` non-blank in a factory `DryRunFinding.Create`), `IComplianceDryRunEvaluator.cs`, `ComplianceDryRunReport.cs` (`IReadOnlyList<DryRunFinding> Findings`, `DateTimeOffset EvaluatedAtUtc`, `bool WouldDeny`), `DryRunErrorCodes.cs`, `ComplianceDryRunService.cs` (constructor `(IEnumerable<IComplianceDryRunEvaluator> evaluators, TimeProvider? timeProvider = null)`; `ValueTask<Either<EncinaError, ComplianceDryRunReport>> RunAsync(object request, IRequestContext context, CancellationToken cancellationToken = default)`: validates input, applies C1 and C2, runs evaluators sequentially in registration order so the report order is stable, converts a throwing evaluator to `Indeterminate`, never calls `IEncina`) and `DryRunServiceCollectionExtensions.cs` (`AddEncinaComplianceDryRun(this IServiceCollection)` registering the service as scoped with `TryAdd*`; evaluators are registered by each module, never by this method).
2. **Modify** `src/Encina/PublicAPI.Unshipped.txt` and `.github/coverage-manifest/Encina.json` (per-file targets of the table in Research), and add `changelog.d/3112-compliance-dry-run.added.md`, all three through `mechanical-fixer`.
3. Tests: new `tests/Encina.UnitTests/Core/Compliance/DryRun/ComplianceDryRunServiceTests.cs` (allow, deny, `NotApplicable`, throwing evaluator is `Indeterminate` and `WouldDeny`, no evaluators, tenant required with `MultiTenancyMarker`, evaluator `Left` propagates, order, `FakeTimeProvider`), `ComplianceDryRunReportTests.cs`, `ComplianceDryRunRegistrationTests.cs` (provider built with `ValidateOnBuild` and `ValidateScopes`; evaluators from two fake modules are all resolved), new `tests/Encina.GuardTests/Core/Compliance/DryRun/ComplianceDryRunServiceGuardTests.cs` (null evaluators, null request, null context; null `timeProvider` is valid and uses the system clock).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
Encina (.NET 10, C# 14, pre-1.0). Issue #3112, plan docs/plans/compliance-dry-run-implementation-plan-3112.md, phase 1.
The dry-run replaces the Warn modes removed by the children of #3089; it must never become a runtime mode.

TASK:
1. Create the contract types, ComplianceDryRunService and AddEncinaComplianceDryRun in src/Encina/Compliance/DryRun/.
2. PublicAPI, manifest and changelog through mechanical-fixer. Unit, DI and guard tests.

KEY RULES:
- Operations return Either<EncinaError, T>; fail closed (throwing evaluator is Indeterminate and counts as a denial; zero evaluators is Left).
- Never call IEncina.Send; never put EncinaError.Message, a subject id or a user id in a finding.
- Time from an injected TimeProvider; guard tests for every public constructor and method parameter.

REFERENCE FILES:
- src/Encina/Identity/RequestIdentity.cs, src/Encina/Abstractions/MultiTenancyMarker.cs, src/Encina/Errors/ErrorCategory.cs
- src/Encina.Compliance.Consent/ConsentRequiredPipelineBehavior.cs
```

</details>

---

### Phase 2: `Encina.Testing` Helper (pull request D2)

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.Testing/Compliance/ComplianceDryRunHelper.cs` (`public static class`; `EvaluateAsync(IServiceProvider provider, object request, RequestIdentity? identity = null, string? tenantId = null, CancellationToken cancellationToken = default)` builds the context with `TestRequestContext.For(...)` and `TestIdentity`, resolves `ComplianceDryRunService` from a created scope, and returns its `Either`) and `ComplianceDryRunAssertions.cs` (`ShouldBeAllowed(this ComplianceDryRunReport)` and `ShouldBeDeniedBy(this ComplianceDryRunReport, string control)` throwing the same assertion exception as the other `Encina.Testing` assertions).
2. **Modify** `.github/coverage-manifest/Encina.Testing.json` through `mechanical-fixer` (`Encina.Testing` has no PublicAPI files, so none is touched).
3. Tests: new `tests/Encina.UnitTests/Testing/Compliance/ComplianceDryRunHelperTests.cs` and `ComplianceDryRunAssertionsTests.cs` (an anonymous, a `TestIdentity.User` and a tenant run against fake evaluators; a missing service returns the registration error), new `tests/Encina.GuardTests/Testing/ComplianceDryRunHelperGuardTests.cs` (null provider, null request; null identity, tenant and token are valid).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
Issue #3112 plan docs/plans/compliance-dry-run-implementation-plan-3112.md, phase 2. D1 merged.

TASK:
1. ComplianceDryRunHelper and ComplianceDryRunAssertions in src/Encina.Testing/Compliance/ over ComplianceDryRunService.
2. Manifest through mechanical-fixer; unit and guard tests.

KEY RULES:
- Identities come from TestIdentity only; the helper never audits and never dispatches the request.
- Shouldly-style assertions through the existing Encina.Testing assertion exceptions; null optional parameters get valid-result tests.

REFERENCE FILES:
- src/Encina.Testing/Identity/TestIdentity.cs, TestRequestContext.cs, src/Encina.Testing/Assertions/EitherAssertions.cs
```

</details>

---

### Phase 3: Reference Evaluator for Consent (pull request D3)

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** in `src/Encina.Compliance.Consent/`: `ConsentRequirement.cs` (`internal static`; the policy code extracted from `ConsentRequiredPipelineBehavior`: attribute lookup per request type with the same cache, subject-id extraction and the call to `IConsentValidator.ValidateAsync`, returning a small decision record) and `ConsentDryRunEvaluator.cs` (`public sealed`, `IComplianceDryRunEvaluator`, `Control` = `"consent"`; maps the decision to `DryRunFinding` with the existing `ConsentErrors.MissingConsentCode`; a request type with no `[RequireConsent]` is `NotApplicable`).
2. **Modify** `ConsentRequiredPipelineBehavior.cs` (delegates to `ConsentRequirement`; no behavior change: the existing behavior tests pass unchanged), `ServiceCollectionExtensions.cs` (register the evaluator with `TryAddEnumerable`), `PublicAPI.Unshipped.txt` and `.github/coverage-manifest/Encina.Compliance.Consent.json`.
3. Tests: new `tests/Encina.UnitTests/Compliance/Consent/ConsentDryRunEvaluatorTests.cs`, new `tests/Encina.GuardTests/Compliance/Consent/ConsentDryRunEvaluatorGuardTests.cs`, new `tests/Encina.ContractTests/Compliance/ConsentDryRunContractTests.cs` (the behavior and the evaluator decide equally for the same requests, subjects and validator answers, and the evaluator writes nothing to the consent store: the in-memory store is snapshotted before and after).
4. Merge order: after #3101 (the Consent child removes `Warn` in the same file).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
Issue #3112 plan docs/plans/compliance-dry-run-implementation-plan-3112.md, phase 3. D1 merged; #3101 merged (Warn removed from Consent).

TASK:
1. Extract ConsentRequirement from ConsentRequiredPipelineBehavior and add ConsentDryRunEvaluator over it; register the evaluator.
2. PublicAPI and manifest through mechanical-fixer; unit, guard and contract tests.

KEY RULES:
- The behavior's observable behavior must not change (existing tests stay green); behavior and evaluator call the same method.
- The evaluator is read-only: it never writes to a consent store and never emits the subject id.

REFERENCE FILES:
- src/Encina.Compliance.Consent/ConsentRequiredPipelineBehavior.cs, Abstractions/IConsentValidator.cs, ServiceCollectionExtensions.cs
```

</details>

---

### Phase 4: `Encina.Cli` Command (pull request D4, after #2983)

> **Not startable until the host and command contract of #2983 are decided and merged** (it carries `needs-decision`). The tasks below describe the command's behavior; the contract type names are #2983's.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** in `src/Encina.Cli/`: the `compliance dry-run` command written against the in-app command contract of #2983 and contributed through DI by the compliance module registration (ADR-058 D7 (a): present only when `AddEncinaComplianceDryRun()` is called), and `Services/DryRunReportRenderer.cs` (table through `Spectre.Console` and JSON through `System.Text.Json`; only the report fields of C4).
2. The command resolves the request type by full name from the application's loaded request types (the host's assembly set), deserializes `--input` with `System.Text.Json` into it, requires `--tenant` when `MultiTenancyMarker` is registered (C2), runs `ComplianceDryRunService` under the operator's authenticated context (Design Choice 3), writes the audit entry of Design Choice 4 through the host and returns the exit code of C9.
3. **Modify** `.github/coverage-manifest/Encina.Cli.json` through `mechanical-fixer`; `Encina.Cli` has no PublicAPI files; add `InternalsVisibleTo` for `Encina.GuardTests` in `Encina.Cli.csproj` if the renderer stays internal.
4. Tests: new `tests/Encina.UnitTests/Cli/Commands/ComplianceDryRunCommandTests.cs` (exit codes, missing tenant, unknown request type, audit failure fails the command), `tests/Encina.UnitTests/Cli/Services/DryRunReportRendererTests.cs` (no message, subject or user value in any output), `tests/Encina.GuardTests/CLI/DryRunReportRendererGuardTests.cs`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT:
Issue #3112 plan docs/plans/compliance-dry-run-implementation-plan-3112.md, phase 4. D1 merged; the host contract of #2983 merged.

TASK:
1. The compliance dry-run command on the in-app host, the report renderer, exit codes 0, 2 and 1.
2. Audit entry through the host; manifest through mechanical-fixer; unit and guard tests.

KEY RULES:
- Runs in the application's container under the operator's identity; fail closed without --tenant when multi-tenancy is on.
- Output carries codes, categories and outcomes only: never EncinaError.Message, subject ids or claim values.

REFERENCE FILES:
- docs/architecture/adr/058-encina-cli-command-architecture.md, src/Encina.Cli/Commands/GenerateCommand.cs
```

</details>

---

### Phase 5: Documentation and Finalization (pull request D5)

<details>
<summary><strong>Tasks</strong></summary>

1. Through `docs-writer`: `docs/features/compliance-dry-run.md` (guide: the contract, writing an evaluator over shared policy code, the helper in tests, the CLI command, exit codes, why there is no Warn), the ADR (the mechanism of Design Choice 1; the number is assigned when the ADR pull request opens), `docs/INVENTORY.md` and `ROADMAP.md` lines. Documentation never types coverage figures by hand.
2. Build with zero warnings; every flag reaches the targets below.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT:
Issue #3112 plan docs/plans/compliance-dry-run-implementation-plan-3112.md, phase 5. D1 to D4 merged.

TASK:
1. Ask docs-writer for the guide, the ADR and the INVENTORY and ROADMAP lines.
2. Report measured per-file coverage against the plan's targets.

KEY RULES:
- Public text never names a private client application; no hand-typed coverage figures (covref markers).

REFERENCE FILES:
- .claude/skills/encina-docs/SKILL.md, docs/features/
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Use in this feature |
|--------|-----------|---------------------|
| #3089 and SPEC-002 DEC-006 | A compliance control is never optional; only territorial non-applicability disables one | The reason Warn goes and the dry-run stays outside the request path |
| ADR-058 | D2, D7, D13, D14, D15 to D19 | CLI in the application's container, tenant fail closed, operator identity, audit |
| ADR-062 | `ErrorCategory` contract | Categories of the three new codes |
| ADR-054 | Settled point 2, D4 | No message or subject value in output; tenant handling |
| `AGENTS.md` sections 3 and 9 | Fail closed; tests execute real code | Indeterminate semantics; contract test |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `ConsentRequiredPipelineBehavior`, `IConsentValidator` | `src/Encina.Compliance.Consent/` | Reference evaluator source of the shared policy code |
| `TestIdentity`, `TestRequestContext` | `src/Encina.Testing/Identity/` | Identities of the helper |
| `MultiTenancyMarker`, `IRequestContext` | `src/Encina/Abstractions/` | Tenant rule and evaluation context |
| `GenerateCommand` and the three command factories | `src/Encina.Cli/Commands/` | Command and option style of the CLI |
| `IOperationAuditStore`, `IReadAuditStore` | ADR-036 | Audit of the CLI run, through #2983's host |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina`, `Encina.Testing`, `Encina.Cli`, `Encina.Compliance.Consent` | none | No new `[LoggerMessage]` code (register row C8); no block of `docs/engineering/eventid-block-registry.md` is claimed |

### Estimated File Count

| PR | Content | New src | Changed src | New tests | Edited tests | Manifest | Other | Files | Lines (est.) |
|----|---------|---------|-------------|-----------|--------------|----------|-------|-------|--------------|
| D1 | Phase 1 | 7 | 1 (`PublicAPI.Unshipped.txt`) | 4 | 0 | 1 | 1 (changelog) | 14 | ~900 |
| D2 | Phase 2 | 2 | 0 | 3 | 0 | 1 | 0 | 6 | ~330 |
| D3 | Phase 3 | 2 | 3 (behavior, `ServiceCollectionExtensions.cs`, `PublicAPI.Unshipped.txt`) | 3 | 0 | 1 | 1 (changelog) | 10 | ~600 |
| D4 | Phase 4 | 2 | 1 (`Encina.Cli.csproj`) | 3 | 0 | 1 | 1 (changelog) | 8 | ~520 |
| D5 | Phase 5 | 0 | 0 | 0 | 0 | 0 | 4 (guide, ADR, `INVENTORY.md`, `ROADMAP.md`) | 4 | ~300 |
| **Total** | | **13** | **5** | **13** | **0** | **4** | **7** | **42** | **~2,650** |

Columns sum: 13 + 5 + 13 + 0 + 4 + 7 = 42 = 14 + 6 + 10 + 8 + 4. D1 is the issue's own "14 files, 900 lines". The D4 estimate is the least firm (it depends on the contract of #2983) and is recounted against that contract when D4 is briefed. Interfaces: `IComplianceDryRunEvaluator` is new; its first implementer is `ConsentDryRunEvaluator` (D3) and the test fakes of D1 to D2 are private test classes, so no existing implementer or `new X(` site changes. `ConsentRequiredPipelineBehavior` keeps its constructor: a grep of `src` and `tests` on `origin/main` finds 12 `new ConsentRequiredPipelineBehavior<` sites, all in existing tests (the guard tests `ConsentRequiredPipelineBehaviorGuardTests`, `ConsentPiiLeakTests` and `ConsentRequiredPipelineBehaviorTests`), and none changes.

Per-file coverage targets (the manifests have no per-file entries for the new files; package targets today: `Encina` unit 70, guard 20, contract 15, property 80, integration 85; `Encina.Testing` unit 60, guard 15; `Encina.Cli` unit 60, guard 15, integration 75; the plan adds per-file entries above those values):

| File | Unit | Guard | Contract | Property | Integration | Justification |
|------|------|-------|----------|----------|-------------|---------------|
| `Encina/Compliance/DryRun/ComplianceDryRunService.cs` | 95 | 70 | 0 | 0 | 0 | Every outcome, the throwing evaluator, no evaluators and the tenant rule are unit tested; guard covers constructor and `RunAsync` arguments |
| `Encina/Compliance/DryRun/DryRunFinding.cs`, `ComplianceDryRunReport.cs` | 95 | 50 | 0 | 0 | 0 | Factory validation and `WouldDeny` are table-driven |
| `Encina/Compliance/DryRun/DryRunServiceCollectionExtensions.cs` | 90 | 50 | 0 | 0 | 0 | Registration driven by the DI test and the null check by guard |
| `Encina/Compliance/DryRun/DryRunErrorCodes.cs`, `DryRunOutcome.cs`, `IComplianceDryRunEvaluator.cs` | 0 | 0 | 0 | 0 | 0 | Constants, an enum and an interface: no executable code |
| `Encina.Testing/Compliance/ComplianceDryRunHelper.cs` | 90 | 60 | 0 | 0 | 0 | Anonymous, user and tenant runs against fakes; guard covers null provider and request |
| `Encina.Testing/Compliance/ComplianceDryRunAssertions.cs` | 90 | 40 | 0 | 0 | 0 | Pass and fail paths of both assertions |
| `Encina.Compliance.Consent/ConsentRequirement.cs` | 90 | 0 | 40 | 0 | 0 | Extracted policy code, internal; the contract test drives it through behavior and evaluator |
| `Encina.Compliance.Consent/ConsentDryRunEvaluator.cs` | 90 | 60 | 60 | 0 | 0 | Applicable, not applicable, allow, deny and validator `Left`; the contract test compares it with the behavior |
| `Encina.Cli/Services/DryRunReportRenderer.cs` | 90 | 50 | 0 | 0 | 0 | Table and JSON output for each outcome, and the absence of message, subject and user values |
| `Encina.Cli/Commands/ComplianceDryRunCommand.cs` | 70 | 0 | 0 | 0 | 0 | Exit codes and option handling by unit tests; the command class is internal and its host wiring is covered by #2983's tests; guard targets stay with the renderer |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #3112 - the compliance dry-run report that replaces every Warn enforcement mode - in five small pull
requests (D1 to D5). D3 merges after #3101 (Consent child); D4 starts only after the host and command contract of #2983 merged.

PROJECT CONTEXT:
- Encina .NET 10 / C# 14, pre-1.0, no [Obsolete], no compatibility layers. Edit with Edit/Write only; PublicAPI, manifests and
  changelog through mechanical-fixer; the guide and the ADR through docs-writer.
- The dry-run never dispatches the request and never becomes a runtime mode; it reuses each module's policy code through evaluators.

IMPLEMENTATION OVERVIEW:
1. D1: IComplianceDryRunEvaluator, DryRunFinding, DryRunOutcome, ComplianceDryRunReport, ComplianceDryRunService in core.
2. D2: Encina.Testing ComplianceDryRunHelper and assertions.
3. D3: ConsentRequirement extraction and ConsentDryRunEvaluator (reference), contract test behavior-versus-evaluator.
4. D4: the CLI command on the in-app host, report renderer, audit through the host.
5. D5: documentation and the ADR.

KEY PATTERNS:
- Fail closed: Indeterminate counts as a denial; no evaluators is Left; missing tenant with multi-tenancy on is Left.
- Output and report carry codes, categories, outcomes and exception type names only: never EncinaError.Message or subject ids.
- Guard tests for every public constructor and method parameter; a null optional parameter gets a valid-result test.

REFERENCE FILES:
- src/Encina.Compliance.Consent/ConsentRequiredPipelineBehavior.cs, src/Encina.Testing/Identity/TestIdentity.cs
- docs/architecture/adr/058-encina-cli-command-architecture.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | A dry-run is a one-shot evaluation and must read current state, so nothing is cached |
| 2 | OpenTelemetry | ❌ N/A | The operator invokes it explicitly and the result is returned data; the Consent behavior's own spans are unchanged |
| 3 | Structured Logging | ❌ N/A | The report is the output; no `[LoggerMessage]` and no EventId (register row C8) |
| 4 | Health Checks | ❌ N/A | An evaluation is not a checkable dependency |
| 5 | Validation | ✅ Phase 1 | The service validates its input (null request, missing tenant) and returns `Left`; the CLI validates `--input` against the request type |
| 6 | Resilience | ❌ N/A | Local evaluation through the module's own stores; no new external call |
| 7 | Distributed Locks | ❌ N/A | Read-only; no shared state is written |
| 8 | Transactions | ❌ N/A | Read-only; evaluators never open a unit of work |
| 9 | Idempotency | ❌ N/A | Re-running a dry-run changes nothing |
| 10 | Multi-Tenancy | ✅ Phases 1, 4 | `tenant_required` fails closed with `MultiTenancyMarker`; the CLI needs an explicit `--tenant` (ADR-058 D13); the reserved `default` segment is never invented |
| 11 | Module Isolation | ❌ N/A | Evaluators belong to compliance packages, not to application modules; no module data is read |
| 12 | Audit Trail | ✅ Phase 4 | The CLI run is audited through the host (Design Choice 4); the Testing helper is not audited; the issue's N/A is revisited because a dry-run reads consent and similar personal data |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| #3101 (Consent child of #3089) | Open | D3 merges after it (same file) |
| #2983 (host and command contract) | Open, `needs-decision`, no plan on main | D4 only; D1, D2, D3 and D5 do not wait |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| The other Warn-removal children | #3091, #3092, #3093, #3094, #3095, #3096, #3098, #3099, #3100, #3102, #3103, #3104, #3105, #3106, #3107 | Each module's evaluator follows its child; batches in the issue file `artifacts/issues/compliance-dry-run-evaluators.md` |
| Architecture test that Warn is absent | #3090 | Delivers acceptance criterion 1 |
| Other compliance commands | #2612 | Shares the in-app host |

---

## Next Steps

1. Link this plan from #3112 and ask the maintainer to answer Design Choices 1 to 4 when work starts.
2. Implement D1, then D2 and D3 (independent after D1), then D4 after #2983, then D5.
3. Maintainer action: amend the cross-cutting rows 10 and 12 of #3112 (multi-tenancy and audit trail marked not applicable) to the outcome of this plan, and decide whether #2983's contract names a dry-run flag that overlaps ADR-058 D11 (this plan never comments on an issue).
