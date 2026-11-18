# Implementation Plan: `TenantId` and `ModuleId` on `IChoreographyState` and `IRoutingSlipState`

> **Issue**: [#762](https://github.com/dlrivada/Encina/issues/762)
> **Type**: Feature (interface-only; SPEC-002 REQ-061 applies to every capability SPEC-002 adds or changes)
> **Complexity**: Low (3 phases, no provider code, 1 pull request of 4 files)
> **Estimated Scope**: ~40 lines of production code (XML documentation included) + 8 PublicAPI lines + 1 changelog fragment
> **Related plans** (cited by issue number, not by link): #590 (persistent routing slip stores: its P1 already lists `TenantId` and `ModuleId` on `IRoutingSlipState`), #592 (persistent choreography stores, open, no plan yet), #738 (the saga row, same pattern), #747 (module on the messaging rows and the `MessagingModuleId` normalizer), #1257 (the shared tenant-scope primitive and the default-tenant rule)

---

## Summary

Add the two nullable members `string? TenantId { get; set; }` and `string? ModuleId { get; set; }` to the persisted-state interfaces of the two messaging patterns that have no implementation yet: `IChoreographyState` (`src/Encina.Messaging/Choreography/IChoreographyState.cs`) and `IRoutingSlipState` (`src/Encina.Messaging/RoutingSlip/IRoutingSlipState.cs`). The motivation of the issue holds: an interface member added before any implementer exists costs nothing, and one added after the 10 providers exist is a breaking change across all of them. SPEC-002 DEC-009 (b) makes multi-tenancy required in 1.0 for every capability SPEC-002 adds or changes.

**State of `origin/main` (`f484cdf4f`), verified by reading and grep (corrects the issue text).**

- **No implementer exists, in `src` or in `tests`.** A grep of class and record declarations for `IChoreographyState` and `IRoutingSlipState` finds none; the only users are the store interfaces (`IChoreographyStateStore`, `IChoreographyEventBus.GetSagaStateAsync`, `IRoutingSlipStore`). Mocks (NSubstitute) of the two interfaces would not break. Nothing builds on `IChoreographyStateStore` either: no class implements it, and the choreography feature has no registration (open debt #1467, #1472).
- `IChoreographyState` has nine members (`CorrelationId`, `SagaType`, `StateData`, `Status`, `StartedAtUtc`, `LastUpdatedAtUtc`, `CompletedAtUtc`, `ErrorMessage`, `EventCount`) and is tracked in `src/Encina.Messaging/PublicAPI.Unshipped.txt` (lines for `Encina.Messaging.Choreography.IChoreographyState.*`). `IRoutingSlipState` has `RoutingSlipId`, `SlipType`, `Data`, `Itinerary`, `ActivityLog`, `Status`, `CurrentStep`, `CreatedAtUtc`, `LastUpdatedAtUtc`, `CompletedAtUtc`, `TimeoutAtUtc` and `ErrorMessage`; a grep finds no `IRoutingSlipState` line in any `PublicAPI*.txt` on `main` although RS0016 is an error (`.editorconfig` line 145), so the first build of the pull request decides exactly which lines are needed (Phase 1 task 3).
- The routing slip side is already claimed: the merged plan of #590 lists "`IRoutingSlipState` gains `Version` and `TenantId`/`ModuleId`" in its P1 (typed columns like the dead letter row, Design Choice 3 of that plan). This plan therefore has to say which plan owns those two members (Design Choice 1).
- The tenant precedent: `IDeadLetterMessage.TenantId` is `string? TenantId { get; set; }` (`src/Encina.Messaging/DeadLetter/IDeadLetterMessage.cs` line 86) with a `VARCHAR(128) NULL` column; the saga row follows it in #738 and the module is `string?` in #747.
- The contract tests asked by the issue cannot execute real package code: no implementation exists (Design Choice 4).

**Not in scope**: the stores, factories, orchestrators and columns of the two patterns (#590 for routing slips, #592 and #593 for choreography); filling the members from the request context; the `MessagingModuleId` normalizer (#747).

**Affected packages**: `Encina.Messaging` (interface definitions only).

**Provider category**: none (interface definitions; the provider matrix applies to the stores of #590 and #592).

---

## Design Choices

<details>
<summary><strong>1. Which plan owns the two members of `IRoutingSlipState` (Dilemma D1)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) This plan (#762) owns `TenantId` and `ModuleId` on BOTH interfaces; the P1 of #590 keeps `Version` and its other changes and skips the two members it finds on `main`** | The issue exists for exactly this member change; a 4-file pull request lands in minutes and unblocks #590 and #592; the shape is decided once for both patterns | The merged #590 plan names the two members in its P1 text; its worker must read `main` first (the plan says to re-verify) |
| **B) #590 owns the routing slip members (its P1) and this plan adds only `IChoreographyState`** | No overlap with a merged plan | The issue's acceptance criteria name both interfaces and would stay half done until a 15-file pull request lands; the two patterns get their shape in two places |
| **C) Both plans add them** | None | A duplicate edit and a merge conflict |

### Chosen Option: **A — #762 owns the members of both interfaces** (recommended, pending the maintainer)

### Rationale

- A wins when the interface change should land early and alone (no implementer, no risk), which is the issue's own reason; B wins if the maintainer wants the routing slip interface to change only together with its first store. Under A the P1 task of #590 reduces to `Version` and the `Either` store contract; the plan text of #590 does not need a rewrite because its first task already says to re-verify the state of `main`.

</details>

<details>
<summary><strong>2. The shape of the members</strong></summary>

**Class:** Trivial
**Settled by:** the precedent `IDeadLetterMessage.TenantId` (`string? TenantId { get; set; }`, `src/Encina.Messaging/DeadLetter/IDeadLetterMessage.cs` line 86), the saga row of #738 and the module column of #747 (`string?`), and the acceptance criterion "Properties are nullable (optional)" of #762.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `string? TenantId { get; set; }` and `string? ModuleId { get; set; }`, XML-documented: the tenant is the `TenantScope.TenantId` of #1257 (null when tenancy is off under a null default tenant, never an empty string); the module is the normalized module name of #747; null means "not scoped"** | The same shape on every messaging row; the existing members of both interfaces are `{ get; set; }` too | None |
| **B) A typed `TenantId` value type** | Compile-time distinction from other strings | No such type exists (`ITenantContext` and a `TenantId` type do not exist in `src`, #346 and #385 plans); a parallel mechanism |

### Chosen Option: **A — nullable strings with documented semantics**

### Rationale

- The documentation states the semantics once, so the two stores that implement them (#590, #592) behave the same: stores persist the value as written, orchestrators fill it from the resolved scope, readers filter strictly (a null tenant never means "all tenants").

</details>

<details>
<summary><strong>3. Add the members now or with the first implementer</strong></summary>

**Class:** Trivial
**Settled by:** the issue's own reasoning and `AGENTS.md` section 1 (pre-1.0, best solution first, no compatibility layers); the grep of the Summary shows zero implementers.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Now: an interface-only pull request with zero implementers** | Nothing can break; the implementers of #590 and #592 are written against the final shape | A member with no consumer for a while (it is documented as a contract) |
| **B) With #590 and #592** | Each member lands with its consumer | Two pull requests of 15 files carry an interface edit each; the shape is decided under review of a large change |

### Chosen Option: **A — now**

### Rationale

- Cost of A is zero and the cost of changing later grows with every provider; B only moves the same edit into larger pull requests. The members are not dead code: they are the contract the stores of #590 and #592 implement.

</details>

<details>
<summary><strong>4. How the interface contract is tested</strong></summary>

**Class:** Settled
**Settled by:** `AGENTS.md` section 9 ("Tests MUST execute real package code: instantiate and call implementations... reflection-only tests (`typeof(I).GetMethod(...)`) or type-only assertions cover zero lines; contract and property tests MUST instantiate real implementations").

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No test in this pull request: the contract is asserted by the contract suites of the first implementers (the abstract `RoutingSlipStoreContractTests` of #590 P1; the choreography store contract suite of the #592 plan), which instantiate real stores and check that the tenant and the module round trip and filter strictly** | Complies with the rule; the tests exist where code exists | The acceptance bullet "Contract tests updated to verify new interface members" is met by those pull requests, not this one |
| **B) A reflection test that the members exist** | Satisfies the bullet literally | Covers zero lines and asserts nothing about behavior: the pattern section 9 forbids |
| **C) A test-project class implementing the interface and asserting that its own properties hold values** | Executes something | Tests the test double, not package code |

### Chosen Option: **A — the implementers' contract suites carry the contract**

### Rationale

- Only A satisfies section 9. #590's P1 already ships the abstract `RoutingSlipStoreContractTests`; the plan of #592 (open, none on `main` yet) must include the same tenant and module rows, which is recorded in this plan's Prerequisites and in the pull request description. The per-file targets of the two interface files are 0 with that reason (Phase 2).

</details>

<details>
<summary><strong>5. Where the values are normalized and validated</strong></summary>

**Class:** Trivial
**Settled by:** `AGENTS.md` section 3 (provider coherence: abstractions carry no provider logic) and Design Choice 3 of the merged plan of #747 (`MessagingModuleId.Normalize`: trimmed, lower-invariant, at most 128 characters, blank rejected).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The interface holds no validation; the orchestrators and stores (#590, #592) normalize the module with #747's helper and resolve the tenant with #1257's resolver before they set the members** | An interface cannot hold logic; one rule per concern in one place | A value set by hand on a state object is not validated until it is stored |
| **B) A setter that validates** | Early failure | Interfaces cannot declare setter bodies; a default interface implementation would be a compatibility layer (`AGENTS.md` section 1) |

### Chosen Option: **A — validate at the writers**

### Rationale

- The members document the limits (128 characters for both, the same as the dead letter and saga columns) so the stores' columns and validation agree.

</details>

---

## Implementation Phases

### Phase 1: Core Contract

> **Goal**: The two members on both interfaces, documented, and the tracked public API (PR 762-A, 3 files).

<details>
<summary>Tasks</summary>

1. `src/Encina.Messaging/Choreography/IChoreographyState.cs`: add `string? TenantId { get; set; }` and `string? ModuleId { get; set; }` after `EventCount`, each with `<summary>` and `<remarks>` (Design Choice 2: null means not scoped, never an empty string; limits of 128 characters; the tenant is the resolved `TenantScope.TenantId`; the module is the normalized name; readers filter strictly).
2. `src/Encina.Messaging/RoutingSlip/IRoutingSlipState.cs`: the same two members after `ErrorMessage`, same documentation.
3. `src/Encina.Messaging/PublicAPI.Unshipped.txt`: four lines per tracked interface (`...TenantId.get -> string?`, `...TenantId.set -> void`, `...ModuleId.get -> string?`, `...ModuleId.set -> void`) for `Encina.Messaging.Choreography.IChoreographyState`; for `IRoutingSlipState` build once and add exactly the lines RS0016 reports (a grep on `main` finds no line for the type, Summary), in the same pull request. Zero warnings.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```
CONTEXT:
- Issue #762; plan docs/plans/choreography-routing-slip-tenant-implementation-plan-762.md. D1 is recommended, pending the maintainer: follow the Maintainer Decisions section if it exists.
- Encina is pre-1.0: no [Obsolete], no default interface members, no compatibility layers. Neither interface has an implementer in src or tests (re-grep before editing).

TASK:
Add string? TenantId and string? ModuleId (get and set) to IChoreographyState and IRoutingSlipState with XML documentation, and update PublicAPI.Unshipped.txt.

KEY RULES:
- Nullable strings; null means not scoped, never an empty string; at most 128 characters; no validation logic in the interface.
- Build with RS0016 as an error and add exactly the reported PublicAPI lines.
- Do not add stores, factories, columns or tests that reflect over the interface.

REFERENCE FILES:
- src/Encina.Messaging/Choreography/IChoreographyState.cs
- src/Encina.Messaging/RoutingSlip/IRoutingSlipState.cs
- src/Encina.Messaging/DeadLetter/IDeadLetterMessage.cs (TenantId, line 86)
- src/Encina.Messaging/PublicAPI.Unshipped.txt
```

</details>

---

### Phase 2: Testing and Verification

> **Goal**: Prove the change builds and breaks nothing; state why no new test exists.

<details>
<summary>Tasks</summary>

1. `dotnet build Encina.slnx --configuration Release` with 0 warnings proves that no implementer or double of either interface exists (a missing implementation of a new member would be CS0535).
2. No new test (Design Choice 4): unit, guard, contract and property tests of these members belong to the first implementers (#590 P1 `RoutingSlipStoreContractTests`; the choreography contract suite of #592). Guard tests: none, the pull request adds no method and no constructor.
3. Per-file targets in `.github/coverage-manifest/Encina.Messaging.json`: the entries `Choreography/IChoreographyState.cs` and `RoutingSlip/IRoutingSlipState.cs` already declare `defaultTests: []` with the reason "Interfaces have no implementation to test"; they stay unchanged, and the targets of both files are 0 for every flag with that one-sentence reason (interface without executable code). The measured coverage of the touched files is reported as "no coverable lines".

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```
CONTEXT:
- Issue #762, Phase 1 done. AGENTS.md section 9: tests execute real package code; no implementation exists.

TASK:
Run the full build and the Encina.Messaging tests; confirm the manifest entries stay valid with dotnet run --file .github/scripts/coverage-report.cs -- --check-justifications.

KEY RULES:
- Do not add reflection-only tests or test-project implementers.
- State in the PR description which pull requests carry the contract tests.

REFERENCE FILES:
- .github/coverage-manifest/Encina.Messaging.json
- docs/plans/routing-slip-stores-implementation-plan-590.md (P1, read on main)
```

</details>

---

### Phase 3: Documentation & Finalization

<details>
<summary>Tasks</summary>

1. XML docs of Phase 1 (`<summary>`, `<remarks>`).
2. `changelog.d/762-choreography-routing-slip-tenant.added.md`: one bullet naming the two interface members (written by `mechanical-fixer`).
3. No guide page: the interfaces are not user-facing yet; the messaging guide is updated by the plans that implement the stores (#590, #592). No ADR: no architectural decision beyond Design Choice 1.
4. `dotnet build --configuration Release` with 0 warnings; `dotnet test tests\Encina.UnitTests` passes.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```
CONTEXT:
- Issue #762 is implemented; the pull request has 4 files (two interfaces, PublicAPI.Unshipped.txt, the fragment).

TASK:
Write the changelog fragment through mechanical-fixer and confirm the file count and the build.

KEY RULES:
- Fragments live in changelog.d/<issue>-<slug>.<section>.md with the section added; never edit CHANGELOG.md.
- Public text never names a private client application.

REFERENCE FILES:
- changelog.d/README.md
```

</details>

---

### Proposed split into pull requests (limit 15 files and 1500 lines each)

| PR | Content | Approx. files | Approx. lines |
|----|---------|---------------|---------------|
| 762-A | `IChoreographyState.cs`, `IRoutingSlipState.cs`, `PublicAPI.Unshipped.txt`, the changelog fragment | 4 | 70 |

One pull request; it builds and passes alone because no implementer exists (re-grep when it is briefed: a class that implements either interface by then must get the two members in the same pull request, which would raise the file count).

---

## Research

### Relevant Standards & Specifications

| Reference | What it defines | Use in this feature |
|-----------|-----------------|---------------------|
| SPEC-002 DEC-009 (b), REQ-061 | Multi-tenancy is required for every capability SPEC-002 adds or changes | Why the members exist before 1.0 |
| `AGENTS.md` sections 1, 3, 9 | Pre-1.0 (no compatibility layers), provider coherence, tests execute real code | Design Choices 3, 4, 5 |
| #738, #747 plans | Tenant on the saga row, module on the messaging rows | Shape and limits |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `IDeadLetterMessage.TenantId` | `src/Encina.Messaging/DeadLetter/IDeadLetterMessage.cs` | Shape precedent |
| `IChoreographyStateStore`, `IRoutingSlipStore` | `src/Encina.Messaging/Choreography/`, `src/Encina.Messaging/RoutingSlip/` | The consumers of the interfaces (no implementer yet) |
| `ITenantScopeResolver` | #1257 PR A1 (core) | Source of the value, used by the future orchestrators |
| `MessagingModuleId.Normalize` | #747 PR 1 | Module normalization at the writers |
| Coverage manifest entries of both interfaces | `.github/coverage-manifest/Encina.Messaging.json` | `defaultTests: []` with reason |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Messaging` | none | No log message is added: the change is interface-only; the stores of #590 and #592 allocate their own ids |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Production | 2 | The two interfaces |
| Public API | 1 | `PublicAPI.Unshipped.txt` |
| Tests | 0 | Design Choice 4 |
| Documentation | 1 | Changelog fragment |
| **Total** | **4 in 1 pull request** | |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT:
- Encina is a pre-1.0 .NET 10 / C# 14 library; no [Obsolete], no default interface members, no compatibility layers.
- Issue #762: TenantId and ModuleId on IChoreographyState and IRoutingSlipState (SPEC-002 DEC-009 (b)). Plan: docs/plans/choreography-routing-slip-tenant-implementation-plan-762.md; D1 is recommended, pending the maintainer.

IMPLEMENTATION OVERVIEW (one pull request, 4 files):
1. Add the two nullable members to both interfaces with XML documentation (Phase 1).
2. Update PublicAPI.Unshipped.txt from the RS0016 build output (Phase 1).
3. Verify the build and the manifest entries; no new test (Phase 2).
4. The changelog fragment (Phase 3).

KEY PATTERNS:
- string? members with { get; set; } like IDeadLetterMessage.TenantId; null means not scoped; no logic in the interface.
- Re-grep for implementers of both interfaces before editing; any implementer gets the members in the same pull request.
- Contract tests belong to the first implementers (#590 P1, #592), never to a reflection test here.

REFERENCE FILES:
- src/Encina.Messaging/Choreography/IChoreographyState.cs
- src/Encina.Messaging/RoutingSlip/IRoutingSlipState.cs
- src/Encina.Messaging/DeadLetter/IDeadLetterMessage.cs
- .github/coverage-manifest/Encina.Messaging.json
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Not applicable: interface members only; the cache of routing slip lookups is #701 |
| 2 | OpenTelemetry | ❌ | Not applicable: no operation is added; the tenant tag of the stores is ADR-054 D4 and belongs to #590 and #592 |
| 3 | Structured Logging | ❌ | Not applicable: no log message is added |
| 4 | Health Checks | ❌ | Not applicable: no dependency is added |
| 5 | Validation | ⏭️ | Deferred: the writers validate and normalize (Design Choice 5); done by #590, #592 and #747's helper |
| 6 | Resilience | ❌ | Not applicable: no external call |
| 7 | Distributed Locks | ❌ | Not applicable: no concurrent access is added |
| 8 | Transactions | ❌ | Not applicable: the interfaces persist nothing by themselves |
| 9 | Idempotency | ❌ | Not applicable: no deduplication key is touched |
| 10 | Multi-Tenancy | ✅ | The members themselves: the contract of the tenant on both patterns |
| 11 | Module Isolation | ✅ | The members themselves: the contract of the module on both patterns |
| 12 | Audit Trail | ⏭️ | Deferred: audit of routing slip and choreography transitions belongs to the stores' plans; saga transition audit is #750 (open, plan on main) |

---

## Prerequisites & Dependencies

- None blocking: no implementer exists. Confirm by grep when the pull request is briefed.
- The plan of #592 (open, none on `main`) must carry the tenant and module rows of the choreography contract suite (Design Choice 4); #590's P1 must skip the two members this pull request adds (Design Choice 1).

---

## Next Steps

1. The maintainer answers D1 when work on #762 starts; the answer goes into `## Maintainer Decisions` and the marker is removed.
2. Brief pull request 762-A; open the pull request, then tell the #590 and #592 workers that the members are on `main`.
