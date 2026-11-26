# Implementation Plan: Publish `ProhibitedUseBlockedNotification` when the AI Act pipeline blocks a prohibited practice

> **Issue**: [#2616](https://github.com/dlrivada/Encina/issues/2616)
> **Type**: Feature
> **Complexity**: Low (5 phases, no provider code, 3 pull requests)
> **Estimated Scope**: ~150 lines of production code + ~600 lines of tests

---

## Summary

`AIActCompliancePipelineBehavior<TRequest, TResponse>` blocks a request that resolves to a registered AI system declaring a prohibited practice (Art. 5 of the EU AI Act), in Block and Warn mode alike (`AIActCompliancePipelineBehavior.cs`, `TryBlock`, step 5). It logs EventId 9505 and increments the `ProhibitedUseBlocked` counter, but `ProhibitedUseBlockedNotification` (`Notifications/ProhibitedUseBlockedNotification.cs`) is never constructed. Issue #1256 corrected the XML remark to say so. This plan makes the behavior publish one notification per declared practice through `IEncina.Publish`, so audit handlers and alerting can subscribe.

- **Standards**: Regulation (EU) 2024/1689 Art. 5 (prohibited practices), Art. 12 and Art. 26(6) (logs available for audit).
- **Affected package**: `Encina.Compliance.AIAct` only. **Provider category**: none (no database, cache, transport or lock code).
- **Today's code** (`origin/main` 19b761ffa): the behavior takes `IAIActComplianceValidator`, `IOptions<AIActOptions>` and a logger; `IEncina` and `TimeProvider` are not injected. `AddEncinaAIAct` already registers `TimeProvider` (`TryAddSingleton(TimeProvider.System)`, `ServiceCollectionExtensions.cs:80`); `IEncina` is registered by `AddEncina` (`TryAddScoped<IEncina, Encina>`, `src/Encina/Core/ServiceCollectionExtensions.cs:59`). `Encina` depends only on `IServiceScopeFactory` and optional services, so a behavior that injects `IEncina` creates no dependency cycle.
- **A gap the issue does not name**: `AIActComplianceResult` carries `IsProhibited` and the violation strings but not which practices were declared; `DefaultAIActClassifier` builds the strings from `AISystemRegistration.ProhibitedPractices` (`DefaultAIActClassifier.cs:73-90`). One notification per practice needs the practices, so Phase 1 adds them to the result (Design Choice 1).
- **Estimated scope**: 3 pull requests (Phase 6), 19 files in total.

---

## Design Choices

<details>
<summary><strong>1. Where the behavior gets the declared practices</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Add `IReadOnlyList<ProhibitedPractice> ProhibitedPractices { get; init; } = []` to `AIActComplianceResult`, filled by `DefaultAIActClassifier`** | One evaluation snapshot; no second lookup; `DefaultAIActComplianceValidator` keeps it through `compliance with { ... }`; additive (default `[]`, no constructor site breaks) | Public API grows by one property; a custom `IAIActClassifier` that does not fill it yields no notifications |
| **B) The behavior calls `IAISystemRegistry.GetSystemAsync` after the block** | `AIActComplianceResult` unchanged | A second lookup that can disagree with the evaluated one (reclassification between the two calls); one more dependency and a `Left` to handle on the blocked path |
| **C) Parse the `Violations` strings** | No API change | Brittle: the strings are human text; rejected |

### Chosen Option: **A (recommended, pending the maintainer)**

### Rationale

- A wins when custom classifiers or validators exist (they extend the record, not the behavior) and when the notification must describe exactly what was evaluated. B wins only if the result type must stay frozen, for example if the registry is replaced by the Marten-backed one of #847 and the result is meant to stay summary-only.
- Class: **Dilemma**.

</details>

<details>
<summary><strong>2. `IEncina` and `TimeProvider` on the behavior constructor: required or optional</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Required `IEncina encina`, `TimeProvider timeProvider`** | A behavior built without a mediator cannot silently lose audit events (the exact defect of #2616); `TimeProvider` is already registered by `AddEncinaAIAct`; null checks are guard-tested | Breaking constructor change: 9 `new AIActCompliancePipelineBehavior` sites in tests (Unit 3, Contract 2, GuardExtended 3, Guard 1) and one PublicAPI line change |
| **B) Optional `IEncina? encina = null` (precedent: `InMemoryAISystemRegistry`, `InMemoryAISystemRegistry.cs`)** | No existing test site changes; usable in a custom pipeline without a mediator | No publication without a visible signal: the same silent gap this issue closes |

### Chosen Option: **A (recommended, pending the maintainer)**

### Rationale

- In production the behavior only runs inside `Encina.Send`, so `IEncina` is always available; A wins there. B wins for hand-built pipelines in tests of other packages. Pre-1.0 favours A (AGENTS.md section 1: breaking changes are acceptable).
- Parameter order (CS1737 is not an issue: all are required): `validator, options, encina, timeProvider, logger`.
- Class: **Dilemma**.

</details>

<details>
<summary><strong>3. EventId of the failed-publish log message</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One id inside `ComplianceAIActGPAI` 5950-5969, which the #845 plan reserves (5950-5957 used by #845): 5958** | No new range, no registry row, no `EventIdRanges` edit; the assembly already maps to this range in the #845 plan | PR 2 must land after the #845 PR that registers the range; the range name is misleading for core logs (the #845 plan proposes a rename) |
| **B) A new 50-slot in the Security and compliance base 50000, assigned by the orchestrator** | Independent of #845 | A whole slot for one id; new `EventIdRanges` field, registry row, PublicAPI line and `AssemblyRanges` entry (4 more files) |

`ComplianceAIAct` 9500-9599 is fully allocated (9500-9512 core, 9513-9529 #847, 9530-9547 #842, 9548-9561 #838, 9562-9575 #840, 9576-9587 #841, 9588-9599 #843), so no id is free there.

### Chosen Option: **A, id 5958 (recommended, pending the maintainer; the orchestrator confirms the claim)**

### Rationale

- A wins when #845 merges first, which its own plan sequences; B wins if this fix must ship before #845. The new message uses the `[LoggerMessage]` source generator (AGENTS.md section 7), which needs `AIActLogMessages` to become `partial`; `CryptoShreddingLogMessages.cs` already mixes `LoggerMessage.Define` and `[LoggerMessage]`.
- Class: **Dilemma**.

</details>

<details>
<summary><strong>4. `HumanOversightRequiredNotification`: publish it, correct the docs, or delete it</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Correct the XML doc of `RequireHumanOversightAttribute` (line ~41) and of the record to say nothing publishes it yet** | Smallest change; keeps the extension point; the #845 plan adds `TenantId` to this record | A declared type stays unused |
| **B) Publish it in `LogObligations` for every successful request that needs oversight** | The type serves its purpose | Fires on every call of a high-risk system, after which the request runs anyway, so a handler cannot hold it "for human review before execution" as the record's remark says; needs a design of the review flow |
| **C) Delete the record** | No dead public type (AGENTS.md section 3) | Breaks the #845 plan; removes a declared extension point |

### Chosen Option: **A (recommended, pending the maintainer)**

### Rationale

- B wins once a human-review hold exists in the pipeline; C wins if the maintainer prefers no unused public types. This choice is isolated in PR 3 so it can change without touching PRs 1 and 2.
- Class: **Dilemma**.

</details>

### Settled and Trivial choices

| Id | Choice | Class | Settled by |
|----|--------|-------|-----------|
| S1 | The behavior publishes in Block and Warn mode alike, because the block itself happens in both | Settled | `AIActCompliancePipelineBehavior.cs`, `TryBlock` step 5 comment and `IsProhibited` branch; issue #2616 Proposed Solution |
| S2 | A failed `Publish` (`Left`) never turns the block into success; the block error is still returned and the failure is logged by error code only (never `EncinaError.Message`) | Settled | Issue #2616 Proposed Solution; AGENTS.md section 3, the `EncinaError.Message` rule |
| S3 | One attempt per declared practice, in the declared order, each awaited inline; a failure of one does not skip the rest; a system classified `Prohibited` with no declared practice (`DefaultAIActClassifier.cs:87`) yields no notification, only the existing EventId 9505 log | Settled | Issue #2616 ("one `ProhibitedUseBlockedNotification` per declared practice"); fire-and-forget would lose the `Left`, forbidden by AGENTS.md section 3 |
| S4 | `OccurredAtUtc` is `TimeProvider.GetUtcNow()`; `RequestTypeName` is `Type.FullName ?? Name`, as the record's own doc states ("Fully qualified type name"), while logs and metric tags keep the short name; `SystemId` is `compliance.SystemId` | Settled | AGENTS.md section 3 (`TimeProvider`); `ProhibitedUseBlockedNotification.cs` param docs |
| S5 | Publish with the explicit-context overload `Publish(notification, context, cancellationToken)` using the `IRequestContext` that `Handle` receives (correlation flows); `Handle` gains `ArgumentNullException.ThrowIfNull(context)` | Trivial | `IEncina.Publish` overloads, `src/Encina/Core/Encina.cs:73-78` |
| S6 | `Publish` already converts handler exceptions and cancellation into `Left` (`Encina.NotificationDispatcher.cs`), so the behavior adds no `try/catch` | Settled | `src/Encina/Dispatchers/Encina.NotificationDispatcher.cs` catch blocks |
| S7 | ADR-062 `ErrorCategory`: no new error code; the block keeps `aiact.prohibited_use` (`AIActErrors.ProhibitedUseCode`) and a publish failure is logged by the code of the `Left` it received | Trivial | `AIActErrors.cs`; ADR-062 category rules (no new code is created) |
| S8 | Tenant: the notification carries no tenant and no telemetry is added, so ADR-054 decision 4 is not engaged; `TenantId` on the record is owned by plan #845 | Settled | ADR-054 D4; plan #845 Phase 4 |

---

## Implementation Phases

### Phase 1: Result model and classifier (PR 1)

<details>
<summary>Tasks</summary>

1. **Modify `src/Encina.Compliance.AIAct/Model/AIActComplianceResult.cs`**: add `public IReadOnlyList<ProhibitedPractice> ProhibitedPractices { get; init; } = [];` with XML doc (empty when none or when a custom classifier does not fill it).
2. **Modify `DefaultAIActClassifier.cs`** (`EvaluateComplianceAsync`, lines 73-100): set `ProhibitedPractices = reg.ProhibitedPractices`.
3. `DefaultAIActComplianceValidator` needs no change (`compliance with { ... }` keeps the property; `UnregisteredSystemResult` and `NoSystemResult` keep the default `[]`).
4. **PublicAPI.Unshipped.txt**: `AIActComplianceResult.ProhibitedPractices.get -> System.Collections.Generic.IReadOnlyList<Encina.Compliance.AIAct.Model.ProhibitedPractice>!` and `.init -> void` (2 lines, by `mechanical-fixer`).
5. **Tests**: `DefaultAIActClassifierTests` (practices copied; empty for a system with none), `DefaultAIActComplianceValidatorTests` (practices survive the `with`), `Model/AIActComplianceResultTests` (default is empty).

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: Encina, .NET 10, ROP. Package src/Encina.Compliance.AIAct. Issue #2616.
TASK: add AIActComplianceResult.ProhibitedPractices (IReadOnlyList<ProhibitedPractice>, init, default []) and fill it in DefaultAIActClassifier.EvaluateComplianceAsync from reg.ProhibitedPractices. No other production change.
KEY RULES: XML docs; additive only; no [Obsolete]; Shouldly assertions through Encina.Testing.Shouldly; AAA tests.
REFERENCE FILES: Model/AIActComplianceResult.cs, DefaultAIActClassifier.cs, DefaultAIActComplianceValidator.cs, tests/Encina.UnitTests/Compliance/AIAct/DefaultAIActClassifierTests.cs.
```

</details>

### Phase 2: Behavior publishes the notification (PR 2)

<details>
<summary>Tasks</summary>

1. **Modify `AIActCompliancePipelineBehavior.cs`**:
   - constructor `(IAIActComplianceValidator validator, IOptions<AIActOptions> options, IEncina encina, TimeProvider timeProvider, ILogger<...> logger)` with `ArgumentNullException.ThrowIfNull` on each;
   - `Handle`: add `ArgumentNullException.ThrowIfNull(context)`; carry `context` into `CheckContext` (new field `IRequestContext Context`);
   - the prohibited branch moves out of the synchronous `TryBlock`: in `RunCheckAsync`, when `validationResult` is `Right` and `IsProhibited`, `return await BlockProhibitedAsync(compliance, check, cancellationToken)`; `BlockProhibitedAsync` keeps the existing log, counter and `RecordFailed`, then `await PublishBlockedAsync(...)`, then returns the existing `Left`;
   - `PublishBlockedAsync(compliance, check, ct)`: for each `practice` in `compliance.ProhibitedPractices`, build `new ProhibitedUseBlockedNotification(check.RequestType.FullName ?? check.RequestTypeName, compliance.SystemId, practice, _timeProvider.GetUtcNow())`, `await _encina.Publish(notification, check.Context, ct)`, and on `Left` call `_logger.ProhibitedUseNotificationFailed(check.RequestTypeName, compliance.SystemId, practice.GetArticlePoint(), error.GetCode().IfNone("encina.unknown"))` (the existing idiom of `HandleValidatorError`); continue with the next practice. Complexity of each method stays under CRAP 10 (loop plus one branch, both covered by unit tests).
   - update the class remark "EventIds 9500-9510" to name the new id.
2. **Modify `Diagnostics/AIActLogMessages.cs`**: make the class `partial`; add `[LoggerMessage(EventId = 5958, Level = LogLevel.Warning, Message = "...Notification not published. RequestType={RequestType}, SystemId={SystemId}, ArticlePoint={ArticlePoint}, ErrorCode={ErrorCode}")] internal static partial void ProhibitedUseNotificationFailed(this ILogger logger, string requestType, string systemId, string articlePoint, string errorCode);` with the XML line naming the range (Design Choice 3).
3. **Modify `Notifications/ProhibitedUseBlockedNotification.cs`**: replace the remark "Nothing in the package publishes this notification yet" with the real behavior (published once per declared practice, in Block and Warn mode).
4. **PublicAPI.Unshipped.txt**: replace the behavior constructor line with the new five-parameter signature (by `mechanical-fixer`).
5. **Tests** (below), **coverage manifest**, **changelog fragment** `changelog.d/2616-aiact-prohibited-use-notification.added.md`, **docs** `docs/features/aiact-compliance.md` (a "Notifications" section: who publishes `AISystemReclassifiedNotification` and `ProhibitedUseBlockedNotification`, with a handler example; by `docs-writer`).
6. **DI registration**: nothing to add; `AddEncinaAIAct` already registers `TimeProvider`, and `IEncina` comes from `AddEncina`. The DI test (Phase 4) proves it.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: Encina, .NET 10. src/Encina.Compliance.AIAct/AIActCompliancePipelineBehavior.cs. Issue #2616. Phase 1 (AIActComplianceResult.ProhibitedPractices) is merged.
TASK: inject IEncina and TimeProvider; on the prohibited block publish one ProhibitedUseBlockedNotification per compliance.ProhibitedPractices entry with IEncina.Publish(notification, context, ct); a Left is logged by error code only and never changes the returned block error; keep Block and Warn behaviour identical otherwise.
KEY RULES: never log EncinaError.Message; TimeProvider for OccurredAtUtc; no try/catch (Publish returns Left); [LoggerMessage] source generator with the claimed EventId; CRAP <= 10 per changed method; update every `new AIActCompliancePipelineBehavior` site in tests (grep Unit, Contract, GuardTests).
REFERENCE FILES: the behavior, InMemoryAISystemRegistry.cs (Publish call), AIActLogMessages.cs, tests/Encina.UnitTests/Compliance/AIAct/AIActCompliancePipelineBehaviorTests.cs.
```

</details>

### Phase 3: Documentation fixes for `HumanOversightRequiredNotification` (PR 3, Design Choice 4 option A)

<details>
<summary>Tasks</summary>

1. `Attributes/RequireHumanOversightAttribute.cs` (line ~41) and `Notifications/HumanOversightRequiredNotification.cs` remark: state that nothing publishes the notification yet; no behavior change.
2. `docs/INVENTORY.md` line 6234: `ProhibitedUseBlockedNotification` is now published by the pipeline behavior; `HumanOversightRequiredNotification` stays "declared, not yet published" (by `docs-writer`).
3. Changelog fragment `changelog.d/2616-aiact-oversight-notification-docs.fixed.md`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: Encina.Compliance.AIAct. Issue #2616, option A of Design Choice 4 (maintainer decision pending: do this PR only if option A is chosen).
TASK: correct the XML docs of RequireHumanOversightAttribute and HumanOversightRequiredNotification so they no longer claim the pipeline publishes the notification; update INVENTORY.md. No code change.
KEY RULES: English XML docs; documentation specialist edits INVENTORY.md; changelog fragment by mechanical-fixer.
REFERENCE FILES: Attributes/RequireHumanOversightAttribute.cs, Notifications/HumanOversightRequiredNotification.cs, docs/INVENTORY.md.
```

</details>

### Phase 4: Testing

<details>
<summary>Tasks</summary>

| Flag | File (all exist on origin/main unless marked new) | Cases |
|------|------|-------|
| Unit | `tests/Encina.UnitTests/Compliance/AIAct/AIActCompliancePipelineBehaviorTests.cs` | Block and Warn mode: one `Publish` per practice with correct fields and `OccurredAtUtc` from a `FakeTimeProvider`; two practices give two notifications in order; no publish when not prohibited, Disabled, no attribute, validator error; a failing `Publish` for the first practice still attempts the second, still returns the block `Left` and logs 5958 with the error code only (asserted through a capturing logger); the context passed is the one given to `Handle` |
| Unit | `DefaultAIActClassifierTests.cs`, `DefaultAIActComplianceValidatorTests.cs`, `Model/AIActComplianceResultTests.cs` | practices filled, kept through the validator, default empty (PR 1) |
| Unit (DI) | `ServiceCollectionExtensionsTests.cs` | `AddEncina` + `AddEncinaAIAct`, provider built with `ValidateOnBuild` and `ValidateScopes`, `IPipelineBehavior<,>` closed type resolved from a scope with its `IEncina` and `TimeProvider` |
| Guard | `AIActGuardTests.cs`, `AIActGuardExtendedTests.cs` | constructor null for `encina` and `timeProvider` at every site; `Handle` with null `context` throws `ArgumentNullException`; the 4 existing constructor sites updated |
| Contract | `AIActCompliancePipelineBehaviorContractTests.cs` | real `Encina` from `AddEncina`, real registry, classifier and validator, a test `INotificationHandler<ProhibitedUseBlockedNotification>` that records what it receives: a prohibited request produces the notifications and the block; the 2 existing constructor sites updated |
| Property, Integration, Load, Benchmark | none | no persistence, no concurrency, no hot path; the package's `tests/Encina.LoadTests/Compliance/AIAct/AIAct.md` justification stands |

**Per-file coverage targets** (numeric, `.github/coverage-manifest/Encina.Compliance.AIAct.json`; targets never go down):

| File | Unit | Guard | Contract | Justification |
|------|-----:|------:|---------:|---------------|
| `AIActCompliancePipelineBehavior.cs` | 98 (was 97) | 12 (was 8) | 95 | The new loop, the `Left` branch and the zero-practice path are each driven by a unit test; guard tests add the two constructor checks and the context check; the contract test drives the real publish |
| `Diagnostics/AIActLogMessages.cs` | 98 | 0 | n/a | The failing-publish unit test reaches the new delegate; log delegates have no guard-testable parameters |
| `Model/AIActComplianceResult.cs` | 90 (new) | 95 | n/a | The init line is executed by the classifier and validator tests; guard keeps its target through record construction |
| `DefaultAIActClassifier.cs` | 98 | 90 | n/a | One more assignment on a path already covered |
| `Notifications/ProhibitedUseBlockedNotification.cs` | 90 (new) | 90 (new) | n/a | The record is constructed by the behavior tests and by the existing guard test (`AIActGuardExtendedTests.cs:376`) |

The PR reports the measured per-flag figures with the commands of AGENTS.md section 9, and the CRAP table for the changed methods.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT: Encina.Compliance.AIAct tests, issue #2616; the behavior from Phase 2 exists.
TASK: write the unit, DI, guard and contract tests listed in the Phase 4 table; update every `new AIActCompliancePipelineBehavior` site (Unit 3, Contract 2, GuardExtended 3, Guard 1 on origin/main).
KEY RULES: tests execute real code; Shouldly via Encina.Testing.Shouldly; FakeTimeProvider; no Thread.Sleep; AAA; assert the logged error code, never a message.
REFERENCE FILES: the five test files named in the table and AIActCompliancePipelineBehaviorContractTests.cs for the real-registry setup.
```

</details>

### Phase 5: Observability

<details>
<summary>Tasks</summary>

1. One log message, EventId 5958 (Design Choice 3), packed after #845's 5950-5957, no gap; no new meter instrument and no new activity tag (the existing `ProhibitedUseBlocked` counter and `ComplianceCheck` activity already cover the block). Nothing is named with a `_total` suffix because nothing is added (ADR-054 decision 4).
2. Log content: request type short name, AI system id, Art. 5(1) point, error code. Never `EncinaError.Message`, never a tenant or user id.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT: Encina.Compliance.AIAct, issue #2616; EventId 5958 is claimed inside ComplianceAIActGPAI (confirm with the orchestrator).
TASK: add the single [LoggerMessage] ProhibitedUseNotificationFailed; no new meter instrument.
KEY RULES: ADR-021 ids packed, ADR-054 D4 (no `_total` suffix, nothing added); never log EncinaError.Message.
REFERENCE FILES: Diagnostics/AIActLogMessages.cs, src/Encina.Marten.GDPR/Diagnostics/CryptoShreddingLogMessages.cs.
```

</details>

### Pull Request Split

| PR | Files | Count | Lines (est.) | Builds and passes alone because |
|----|-------|------:|-------------:|-----------------|
| 1: result model | `Model/AIActComplianceResult.cs`, `DefaultAIActClassifier.cs`, `PublicAPI.Unshipped.txt`, coverage manifest, `DefaultAIActClassifierTests.cs`, `DefaultAIActComplianceValidatorTests.cs`, `Model/AIActComplianceResultTests.cs` | 7 | ~120 | the property is additive with default `[]`; its own tests |
| 2: publication (after PR 1 and, for the EventId, after the #845 PR that registers `ComplianceAIActGPAI`) | behavior, `AIActLogMessages.cs`, `ProhibitedUseBlockedNotification.cs`, `PublicAPI.Unshipped.txt`, manifest, `changelog.d/2616-aiact-prohibited-use-notification.added.md`, `docs/features/aiact-compliance.md`, unit, contract, guard (2 files), DI test | 13 | ~650 | the constructor change and all 9 test sites land together |
| 3: oversight docs (only if Design Choice 4 option A) | `RequireHumanOversightAttribute.cs`, `HumanOversightRequiredNotification.cs`, `docs/INVENTORY.md`, `changelog.d/2616-aiact-oversight-notification-docs.fixed.md` | 4 | ~30 | documentation only |

Sibling coordination: #845 also edits the behavior constructor (adds the effective-options resolver) and the notification records (`TenantId`); whichever merges second rebases and re-counts the constructor sites. #847 replaces the registry and must fill `ProhibitedPractices` in its result mapping. The two ids in the two plans (9513-9529 reserved by #847, 5958 here) do not overlap.

---

## Research

### EU AI Act references

| Reference | Subject | Use here |
|-----------|---------|----------|
| Art. 5(1) | Prohibited practices | what the notification reports (`GetArticlePoint`) |
| Art. 12, Art. 26(6) | Record-keeping and log retention | the notification is the hook for audit handlers |
| Art. 99(3) | Fines for prohibited practices | motivates a visible event |

### Existing Encina infrastructure to leverage

| Component | Location | Usage |
|-----------|----------|-------|
| `IEncina.Publish` | `src/Encina/Core/Encina.cs:73-78` | dispatch of the notification, returns `Either` |
| `InMemoryAISystemRegistry` publishing | `InMemoryAISystemRegistry.cs` | precedent for `TimeProvider.GetUtcNow()` plus `Publish` |
| `AIActErrors.ProhibitedUseCode` | `AIActErrors.cs` | unchanged block error |
| `ProhibitedPracticeExtensions.GetArticlePoint` | `Model/ProhibitedPracticeExtensions.cs` | the article point in the log |
| `CryptoShreddingLogMessages` | `src/Encina.Marten.GDPR/Diagnostics/` | precedent mixing `Define` and `[LoggerMessage]` in one class |

### Event ID allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Compliance.AIAct` | 5958 (inside `ComplianceAIActGPAI` 5950-5969, reserved by #845) | one id; `ComplianceAIAct` 9500-9599 is fully allocated; pending the orchestrator's confirmation |

### Estimated File Count

The cross-cutting matrix is the final section. Estimated files:

| Category | Files | Notes |
|----------|------:|-------|
| Production source | 6 | behavior, log messages, result model, classifier, notification doc, attribute and record docs |
| PublicAPI and coverage manifest | 2 | |
| Unit and DI tests | 4 | behavior, classifier, validator, result, DI (counted with unit) |
| Guard tests | 2 | |
| Contract tests | 1 | |
| Documentation and changelog | 4 | feature page, INVENTORY, two fragments |

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina, .NET 10 / C# 14, pre-1.0, ROP (Either<EncinaError, T>), no [Obsolete]. Package src/Encina.Compliance.AIAct. Issue #2616.
IMPLEMENTATION OVERVIEW: (1) AIActComplianceResult.ProhibitedPractices filled by DefaultAIActClassifier; (2) AIActCompliancePipelineBehavior takes IEncina and TimeProvider and publishes one ProhibitedUseBlockedNotification per practice when it blocks a prohibited use, in Block and Warn mode; a failed Publish is logged by error code (EventId 5958) and never changes the block; (3) XML doc corrections for HumanOversightRequiredNotification (only if the maintainer picks option A).
KEY PATTERNS: TimeProvider for time; never EncinaError.Message in logs; [LoggerMessage] source generator; PublicAPI.Unshipped.txt, coverage manifest, changelog fragment and docs go to their specialists; every PR <= 15 files.
REFERENCE FILES: AIActCompliancePipelineBehavior.cs, DefaultAIActClassifier.cs, InMemoryAISystemRegistry.cs, AIActLogMessages.cs, the AIAct test files named in Phase 4.
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | a block notification reads no cacheable data |
| 2 | OpenTelemetry | ❌ | no new operation; the existing `ProhibitedUseBlocked` counter and compliance-check activity cover the block; nothing new to tag (ADR-054 D4) |
| 3 | Structured Logging | ✅ | one `[LoggerMessage]` for a failed publish, error code only (Phase 5) |
| 4 | Health Checks | ❌ | no new checkable dependency; `AIActHealthCheck` unchanged |
| 5 | Validation | ❌ | no input is received |
| 6 | Resilience | ❌ | `Publish` is in-process, no external call; a failed publish is logged, not retried |
| 7 | Distributed Locks | ❌ | no shared mutable state |
| 8 | Transactions | ❌ | not part of a unit of work; the behavior saves nothing |
| 9 | Idempotency | ❌ | every blocked attempt is a distinct event |
| 10 | Multi-Tenancy | ❌ | the notification carries no tenant here; plan #845 adds `TenantId` and owns it |
| 11 | Module Isolation | ❌ | no module-scoped state |
| 12 | Audit Trail | ✅ | the published notification is the hook for audit handlers; no store is added |

---

## Next Steps

1. The maintainer answers the four Dilemmas when work on #2616 starts.
2. The orchestrator confirms the EventId claim (5958) or assigns a 50000-zone slot.
3. Open PR 1, then PR 2 (after the #845 range PR), then PR 3 if option A of Design Choice 4 stands.

---

## Maintainer Decisions

1. **Where the behavior gets the declared practices** — Chosen Option: A, a `ProhibitedPractices` property on `AIActComplianceResult` (recommended, pending the maintainer). Dated entry: 2026-11-26, not yet answered.
2. **`IEncina` and `TimeProvider` on the constructor** — Chosen Option: A, required (recommended, pending the maintainer). Dated entry: 2026-11-26, not yet answered.
3. **EventId of the failed-publish log** — Chosen Option: A, 5958 inside the #845 range (recommended, pending the maintainer). Dated entry: 2026-11-26, not yet answered.
4. **`HumanOversightRequiredNotification`** — Chosen Option: A, correct the docs (recommended, pending the maintainer). Dated entry: 2026-11-26, not yet answered.
