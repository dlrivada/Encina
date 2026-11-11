# Implementation Plan: Rule-Based Compliance Engine — Declarative Regulation DSL

> **Issue**: [#687](https://github.com/dlrivada/Encina/issues/687)
> **Type**: Feature
> **Complexity**: Medium (6 phases, no database, about 30 files in 3 pull requests)
> **Estimated Scope**: ~1,100-1,400 lines of production code + ~1,500-1,900 lines of tests
> **Milestone**: v0.17.0 — Compliance Lifecycle (order set on 2026-10-23: #686, #687, #2700, #688)
> **Depends on**: #686 (the contract; its plan is `docs/plans/compliance-applicability-resolver-implementation-plan-686.md`)

---

## Summary

A fluent DSL that configures which regulations and which compliance capabilities apply under which conditions, without writing a resolver class. The builder produces a `RuleBasedComplianceResolver` that implements `IComplianceApplicabilityResolver` from #686 and composes with the programmatic resolvers through `CompositeComplianceResolver`.

**Reading of #686 that shapes this plan.** The contract and its decisions are fixed by the #686 plan and are consumed here, not redefined: `ComplianceCapability` is a closed enum of 15 values, `ComplianceProfile` holds a capability set and a regulation set, composition is union only (Design Choice 11, decided 2026-10-30), a resolver never abstains and an unknown context gives the full profile (Design Choice 4, SPEC-002 INV-005), and the service gate refuses any removal that carries no territorial evidence (Design Choices 5, 14 and 15). Four points of the #687 issue text do not hold against that contract and are resolved in Design Choices 1 to 4 below:

1. Where the DSL lives (new package or inside `Encina.Compliance.Applicability`).
2. Whether the DSL compiles to XACML (the #686 plan left this to #687).
3. Whether a rule can express "this capability does not apply" (the issue says rules never lower a profile, but also promises a union that can narrow).
4. What a rule set answers when no rule matches. The issue says "full profile", but the union in `CompositeComplianceResolver` then lets a no-match DSL child cancel the territorial removal of every other child, and its own example (`VoluntaryGdpr`, a subject opt-in) has no effect. This is the finding of this plan.

**Out of scope** (all from the issue and the #686 plan): `ComplianceMode`, `WithMode`, `WithPriority`, `SetFallback`, `OnConflict`, `RegulationConflictStrategy` (removed by #686 Design Choices 9, 11 and 4); `SubscriptionTier` as an input (Design Choice 12); `AddCcpa()` and `AddLgpd()` templates (Encina claims nothing for non-EU law, SPEC-002 INV-002); loading rules from JSON or YAML (post-1.0, #688 covers external policies); run-time mutation of rules (rules are immutable after start-up, so no rule versioning and no audit of rule changes is needed).

**Standards**: GDPR Art. 2, 3 and 5(2); LOPDGDD art. 1-2; SPEC-002 REQ-011, REQ-019, REQ-062, DEC-006, INV-002, INV-005.

**Affected packages**: `Encina.Compliance.Applicability` (new in #686; or a new `Encina.Compliance.Rules`, Design Choice 1), `Encina` only for the `EventIdRanges.cs` registration and, if Design Choice 4 is option B, one additive member of the #686 contract.

**Provider category**: none. Rule evaluation is in memory and stateless; no database, caching, transport or lock code is written.

---

## Design Choices

Four Dilemmas are open (1 to 4); the maintainer answers them when work on the issue starts. Choices 5 to 8 are Settled by earlier maintainer decisions or by rules.

<details>
<summary><strong>1. Where the DSL lives</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Inside `Encina.Compliance.Applicability`** (references only `Encina`) | One package and one registration call; the resolver and its rules ship together; the package gains no dependency | Every application that resolves applicability also carries the builder types (a few small classes) |
| **B) New `Encina.Compliance.Rules` package referencing `Encina.Compliance.Applicability`** | Pay-for-what-you-use (AGENTS.md section 1): an application with only programmatic resolvers loads no DSL | One more package, README, manifest and architecture-test entry; two registration calls |

### Chosen Option: **A — Inside `Encina.Compliance.Applicability`** (recommended, pending the maintainer)

### Rationale

- The builder and the resolver are pure C# on core types; they add no dependency, so the pay-for-what-you-use cost of A is a few kilobytes, while B adds a package whose only reason is to hide them.
- B wins when most applications use programmatic resolvers and the maintainer wants the smallest possible dependency graph per package. The plan is written so that moving the folder `Rules/` to a new project is a mechanical change (no type depends on the host package internals).

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Plain C# predicates or compilation to XACML (ABAC)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Plain C# predicates; no reference to `Encina.Security.ABAC`** | No Roslyn compilation; no `RequestType` requirement (`PolicyEvaluationContext.RequestType` is required, `src/Encina.Security.ABAC/Model/PolicyEvaluationContext.cs:94`); no dependency on a package that references Caching, Security and Security.Audit | An application that already writes XACML keeps two policy languages |
| **B) Compile the rules to XACML policies evaluated by `IPolicyDecisionPoint`** | One policy language and one decision audit (ADR-047) with ABAC | The dependency chain above; background contexts have no natural request type; the applicability package would no longer reference only `Encina` (#686 Design Choice 1) |

### Chosen Option: **A — Plain C# predicates** (recommended, pending the maintainer)

### Rationale

- #686 Design Choice 17 built the four resolvers as plain C# and left this decision to #687; A keeps the package graph of #686 intact and the evaluation cost to a delegate call.
- B wins for an application that already runs ABAC and wants one decision audit for both questions.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Can a rule express a territorial removal</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Add-only rules.** A rule only adds capabilities; removal on territorial evidence stays in the built-in resolvers and the service gate of #686 | A rule can never weaken a legal duty; the simplest invariant to prove with a property test | An application that needs a non-built-in territorial exemption writes a custom resolver |
| **B) A `NotApplicableWhen(...)` rule that must carry evidence kind, country and source** and passes the same service-gate checks (no evidence is refused and falls back to full) | Territorial exemptions are declarative and audited like the built-in ones | A larger surface; a misconfigured rule is refused only at run time |

### Chosen Option: **A — Add-only rules** (recommended, pending the maintainer)

### Rationale

- The principle of 2026-10-30 (the legal floor is kept; resolvers only add) is the simplest to keep with A, and the built-in territorial removal covers the Spain-first scope of SPEC-002.
- B wins when tenants in several non-EU territories need configured exemptions that no built-in resolver knows.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. What a rule set answers when no rule matches, and how it composes</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No match gives the full profile** (the issue text and SPEC-002 INV-005 read literally) | No change to the #686 contract | In `CompositeComplianceResolver` the union with a full-profile DSL child cancels every removal of the other children; `VoluntaryGdpr` (add GDPR for a subject opt-in) changes nothing because the answer is already full; the DSL is useful only as the sole resolver |
| **B) No match is "no contribution".** The rule resolver returns an additive marker that `CompositeComplianceResolver` ignores; a standalone selection, or a composite where every child gives no contribution, turns it into the full profile | Keeps INV-005 at the level of the final answer; lets the DSL add capabilities (a subject opt-in restores GDPR where the region resolver removed it by territorial evidence) and compose with the removal resolvers | Needs one additive member in the #686 contract (for example `ComplianceProfile.NoContribution`, handled in Phase 2c of that plan), agreed with #686 before Phase 2 of this plan |

### Chosen Option: **B — No match is no contribution** (recommended, pending the maintainer)

### Rationale

- The issue promises both "no match gives the full profile" and "composes with the programmatic resolvers through `CompositeComplianceResolver`"; with union semantics only B satisfies both. A is correct only if the DSL is never composed with a resolver that removes.
- A wins when #686 must ship before this decision and the maintainer prefers not to touch its contract; the documentation then states that the DSL is a standalone resolver.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Conflict resolution, modes, priority and subscription tier</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Union only; no modes, no priority, no tier input** | Keeps the legal floor; nothing to configure wrongly; matches #686 | An application cannot choose "highest priority wins" |
| **B) `RegulationConflictStrategy` (`HighestPriority`, `FirstMatch`) and `WithMode`, `WithPriority`** | Familiar to rule-engine users | Lets a priority hide a legal duty; contradicts the decided principle |

### Chosen Option: **A — Union only**

### Rationale

- Decided by the maintainer on 2026-10-30 in the #686 thread; the issue text already drops these members.

**Class:** Settled
**Settled by:** #686 plan Design Choices 9, 11 and 12 (maintainer decisions of 2026-10-30); SPEC-002 DEC-006

</details>

<details>
<summary><strong>6. Asynchronous predicates and failure behavior</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `AppliesWhen` (sync) and `AppliesWhenAsync(Func<ComplianceContext, CancellationToken, ValueTask<bool>>)`; a predicate that throws or returns a failure gives the full profile for the whole rule set, logged by error code** | Matches the asynchronous Railway-oriented contract; fails closed | A buggy predicate widens the profile instead of narrowing it |
| **B) A failing predicate is treated as "does not match"** | Isolates a bad rule | A failing gate that silently drops an obligation is the fail-open case AGENTS.md section 3 forbids |

### Chosen Option: **A — Asynchronous predicates, failure gives the full profile**

### Rationale

- The contract is `ValueTask<Either<EncinaError, ComplianceProfile>>` (#686 Design Choice 2) and errors are never swallowed in infrastructure.

**Class:** Settled
**Settled by:** AGENTS.md section 3 (Railway Oriented Programming; gates fail closed); #686 plan Design Choices 2 and 6; ADR-001, ADR-006

</details>

<details>
<summary><strong>7. Templates and start-up validation</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Templates `AddGdpr()` and `AddLopdgdd()` only; duplicate rule names, an empty rule set and an invalid regulation id fail at start-up through the options validator (`ValidateOnStart`)** | Encina claims only EU law; misconfiguration cannot reach run time | An application writes its own non-EU rules |
| **B) Also `AddCcpa()` and `AddLgpd()`** | Convenient | Encina would claim a mapping of non-EU law it does not maintain |

### Chosen Option: **A — GDPR and LOPDGDD templates, start-up validation**

### Rationale

- Non-EU regulation ids are identifiers; Encina claims nothing for them. `AddGdpr()` uses `ComplianceRegulationCapabilities.For(ComplianceRegulation.Gdpr)` from #686.

**Class:** Settled
**Settled by:** SPEC-002 INV-002; #686 plan Design Choice 14; AGENTS.md section 3 (registration completeness, `ValidateOnBuild`)

</details>

<details>
<summary><strong>8. EventIds, telemetry names and decision audit</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A new range `ComplianceRules = (6600, 6619)` from the family block, registered first; instruments named by ADR-054 on the `Encina.Compliance.Applicability` meter; the rule never writes its own audit record, the matched rule name travels in the skip evidence of #686** | One audit writer; the free compliance block 8950-8999 stays reserved for #686 | The assembly maps to two ranges in `EncinaEventIdAllocationTests` |
| **B) Extend `ComplianceApplicability` 8950-8999** | One range per assembly | The tail 8960-8999 is the last free slot of the compliance block (#686 plan, Event ID Allocation) |

### Chosen Option: **A — Range 6600-6619, ADR-054 names, audit through #686**

### Rationale

- The issue named 9800-9999, which the orchestrator registry has since assigned to other families; the plan family block 6600-6649 replaces it.

**Class:** Trivial
**Settled by:** AGENTS.md section 7 and ADR-021 (register first, packed ids); ADR-054 settled point 4 (instrument names); #686 plan Design Choice 15 (decision audit); ADR-036

</details>

---

## Implementation Phases

Each phase is one pull request of at most 15 files and 1500 changed lines. Phase 0 is the gate: #686 Phases 1 and 2 (contract, package, composite) are merged first.

### Phase 1: Rule Model, Builders and Validation

> **Goal**: The DSL builds an immutable rule set that validates itself. **Size**: about 10 files, 600 lines.

<details>
<summary>Tasks</summary>

Folder `src/Encina.Compliance.Applicability/Rules/` (or the new project, Design Choice 1), namespace `Encina.Compliance.Applicability.Rules`.

1. `RegulationRule.cs` (internal sealed record): `string Name`, `ComplianceRegulation? Regulation`, `Func<ComplianceContext, CancellationToken, ValueTask<bool>> Predicate`, `IReadOnlySet<ComplianceCapability> Capabilities`.
2. `RegulationRuleBuilder.cs` (public sealed): `AppliesWhen(Func<ComplianceContext, bool>)`, `AppliesWhenAsync(Func<ComplianceContext, CancellationToken, ValueTask<bool>>)`, `Enforces(params ComplianceCapability[])`; a rule without predicate or without capabilities is a validation error; both are `ArgumentNullException`-guarded.
3. `ComplianceRegulationBuilder.cs` (public sealed): `AddRegulation(string regulationId, Action<RegulationRuleBuilder>)` (parses through `ComplianceRegulation.Parse`) and `AddRule(string ruleName, Action<RegulationRuleBuilder>)`; collects rules in registration order.
4. `RuleSetValidator.cs` (internal): duplicate names, empty set, invalid regulation ids, a rule that enforces no capability; returns `Either<EncinaError, ...>` and is used by the options validator (`ValidateOnStart`).
5. Templates `ComplianceRegulationBuilderTemplates.cs` (extension members): `AddGdpr()` (EU/EEA tenant, operator or subject country, or `RegionInEea`; capabilities from `ComplianceRegulationCapabilities.For(Gdpr)`) and `AddLopdgdd()` (country `ES` in tenant, operator or subject country).
6. `PublicAPI.Unshipped.txt` of the package; tests `tests/Encina.UnitTests/Compliance/Applicability/Rules/` (builder, validator, templates), `tests/Encina.GuardTests/Compliance/Applicability/Rules/`; manifest entries in `.github/coverage-manifest/Encina.Compliance.Applicability.json`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT:
- Issue #687; plan docs/plans/compliance-rule-engine-implementation-plan-687.md; the contract is #686 (merged in Phases 1-2 of its plan).
- Maintainer Decisions of the plan override the "recommended" choices.

TASK:
Create the rule model, the two builders, the rule-set validator and the GDPR/LOPDGDD templates in the Rules folder,
with PublicAPI lines, unit and guard tests and manifest targets. At most 15 files and 1500 lines.

KEY RULES:
- Rules only add capabilities; no mode, priority, conflict strategy or subscription tier exists.
- Non-EU regulations get no template; ids parse through ComplianceRegulation.Parse.
- XML docs on every public member; RS0016 must not fire; no subject id in any message.

REFERENCE FILES:
- src/Encina/Compliance/ComplianceRegulationCapabilities.cs (from #686)
- src/Encina.Compliance.Applicability/Resolvers/CompositeResolverBuilder.cs (builder precedent from #686)
- src/Encina.Compliance.Consent/ConsentOptionsValidator.cs
```

</details>

---

### Phase 2: RuleBasedComplianceResolver and Registration

> **Goal**: The rule set resolves a context and registers with one call. **Size**: about 10 files, 700 lines.

<details>
<summary>Tasks</summary>

1. `Rules/RuleBasedComplianceResolver.cs` (public sealed, `IComplianceApplicabilityResolver`, `IComplianceContextReader`): evaluates rules in registration order with their asynchronous predicates; the answer is the union of the capabilities and regulations of the matched rules, `ResolvedBy` names the matched rule names (no subject id); a predicate that throws, or a `Left`, gives `Left` with `compliance.applicability_failed` (the service gate of #686 turns a `Left` into the full profile) and is logged by error code. Behavior with no match follows Design Choice 4 (A: full profile; B: the no-contribution marker).
2. `Reads`: the DSL predicates are opaque, so `Reads` is `Operator | Tenant | Subject`; the options validator of #686 therefore requires the operator country to be configured when the DSL is the only resolver (consistent with its fail-closed rule).
3. `ComplianceApplicabilityOptions.UseRules(Action<ComplianceRegulationBuilder>)` (extension on the options of #686, same file family) selecting the resolver; the validator of Phase 1 runs through `IValidateOptions` with `ValidateOnStart`.
4. Registration inside `AddEncinaComplianceApplicability` (no new top-level method): the rule set is a singleton; a DI test builds the provider with `ValidateOnBuild` and `ValidateScopes` for the three shapes (rules alone, rules inside `UseCompositeResolver`, rules plus a custom resolver rejected).
5. Tests: union is monotonic and independent of rule order (FsCheck), no match, failing predicate, async predicate with cancellation, `VoluntaryGdpr` restores GDPR in a composite with the region resolver (only with Design Choice 4 = B), contract suite of #686 Phase 9a run against the resolver, guard tests.
6. `PublicAPI.Unshipped.txt` and manifest targets.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT:
- Phase 1 is merged. Issue #687; plan Design Choices 3, 4 and 6 (Maintainer Decisions override the recommendations).
- Contract: IComplianceApplicabilityResolver, ComplianceProfile.Union, ComplianceContextReads (#686).

TASK:
Implement RuleBasedComplianceResolver, the UseRules option selection, the registration and the tests listed.
At most 15 files and 1500 lines.

KEY RULES:
- Union only; a rule never removes a capability; unknown context gives the full profile at the level of the final answer.
- A failing predicate fails the whole rule set (never "no match").
- Time, if needed, from TimeProvider; no static state.

REFERENCE FILES:
- src/Encina.Compliance.Applicability/Resolvers/CompositeComplianceResolver.cs (#686)
- src/Encina/Compliance/ComplianceProfile.cs (#686)
- tests/Encina.ContractTests/Core/Compliance/ (resolver contract suite, #686 Phase 9a)
```

</details>

---

### Phase 3: Observability and Health Check

> **Goal**: Rule evaluation is traceable, counted and checked at start-up. **Size**: about 8 files, 450 lines.

<details>
<summary>Tasks</summary>

1. `src/Encina/Diagnostics/EventIdRanges.cs`: register `ComplianceRules = (6600, 6619)`; the field in `src/Encina/PublicAPI.Unshipped.txt`; add `nameof(EventIdRanges.ComplianceRules)` to the `Encina.Compliance.Applicability` entry of `AssemblyRanges` in `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`.
2. `Diagnostics/ComplianceRulesLogMessages.cs` (`[LoggerMessage]`, doc `Event IDs: 6600-6607 (see EventIdRanges.ComplianceRules)`): 6600 `RuleMatched` (Debug), 6601 `MultipleRulesMatched` (Information), 6602 `NoRuleMatched` (Debug), 6603 `RuleEvaluationFailed` (Warning, error code only), 6604 `DuplicateRuleName`, 6605 `EmptyRuleSet`, 6606 `InvalidRegulationId`, 6607 `HealthProbeFailed`. 6608-6619 stay free.
3. Instruments on the existing `Encina.Compliance.Applicability` source and meter (ADR-054): `encina.compliance.rule.evaluations` (Counter, tag `rule_name`), `encina.compliance.rule.matches` (Counter, tag `rule_name`), `encina.compliance.rule.evaluation.duration` (Histogram, unit `s`); span `encina.compliance.rule_engine.evaluate` with `rules.count` and `matches.count`; tenant tag only when multi-tenancy is on; no subject id. The issue's `conflict_resolution_count` is dropped with the conflict strategies.
4. `Health/RuleSetHealthCheck.cs`: unhealthy when no rule is configured or when a dry run with an empty context does not give the full profile (or, with Design Choice 4 = B, the no-contribution marker).
5. Tests: in-memory exporter test for the span and counters, log tests, health check test, EventId architecture tests (run, not new).

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT:
- Phases 1-2 merged. ADR-021 (EventIds) and ADR-054 (instrument names) apply; the range is 6600-6619 from the plan family block.

TASK:
Register the EventId range first, then add the log messages, instruments, span and health check and their tests.

KEY RULES:
- Register ComplianceRules in EventIdRanges.cs before any [LoggerMessage]; ids packed from 6600.
- Error code or exception type only in logs; no subject id, no rule predicate text.
- Instruments cost nothing without a listener.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
- src/Encina.Compliance.Applicability/Diagnostics/ComplianceApplicabilityLogMessages.cs (#686)
```

</details>

---

### Phase 4: Cross-Cutting Integration — Evidence, Tenancy and Module Scope

> **Goal**: The matched rule is part of the audit evidence and the tenant is a tag. **Size**: about 5 files, 250 lines.

<details>
<summary>Tasks</summary>

1. `ResolvedBy` and the skip evidence of #686 carry the matched rule names, so the skip record the service gate writes (`IOperationAuditStore`, Design Choice 15 of #686) names the rule; no new audit writer.
2. Tenant id as a metric tag and span attribute only when multi-tenancy is on (ADR-054 D4); module scope: predicates read `ComplianceContext.ModuleName`; module-scoped rule sets stay deferred (no issue needed unless asked).
3. Tests: skip record contains the rule name and no subject id; two-tenant test: tenant A's rules never evaluate tenant B's context fields beyond those the context carries.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT:
- Phases 1-3 merged. The skip record and gate are #686 (Phases 3a and 7).

TASK:
Carry the matched rule names in ResolvedBy and the skip evidence; add tenant tags; write the tests listed.

KEY RULES:
- The rule engine never writes an audit record; the service gate does.
- No subject id, no Metadata in any tag, log or evidence.

REFERENCE FILES:
- src/Encina/Compliance/ComplianceSkipRecord.cs (#686)
- src/Encina.Security.Audit/Abstractions/IOperationAuditStore.cs
```

</details>

---

### Phase 5: Testing

> **Goal**: Every flag reaches its target. **Size**: about 8 files, 600 lines.

<details>
<summary>Tasks</summary>

1. Unit and guard tests as in Phases 1-3 (all public builder methods, null guards).
2. Contract: the resolver contract suite of #686 runs against `RuleBasedComplianceResolver`.
3. Property (FsCheck): union is monotonic and order-independent; any rule set with no matching rule gives the Design Choice 4 answer; adding a rule never removes a capability.
4. Integration: the DI test with `ValidateOnBuild` and `ValidateScopes`, and a pipeline test with declarative rules (no Docker).
5. Load: `.md` justification (in-memory, stateless) at `tests/Encina.LoadTests/Compliance/Applicability/RuleBasedComplianceResolver.md`. Benchmark: `tests/Encina.BenchmarkTests/` evaluation of N rules by M capabilities (BenchmarkSwitcher, `artifacts/performance/`).
6. Per-flag targets in `.github/coverage-manifest/Encina.Compliance.Applicability.json` for each new file with a one-sentence justification; CRAP of every new method at most 10.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT:
- Phases 1-4 merged. Coverage is per flag (AGENTS.md section 9); tests execute real code.

TASK:
Write the contract, property, integration, benchmark and load-justification files and verify per-flag coverage and CRAP.

KEY RULES:
- Shouldly through Encina.Testing.Shouldly; no reflection-only tests; no Thread.Sleep.
- Measure with coverage-report.cs and run the crap gate locally before the PR.

REFERENCE FILES:
- tests/Encina.PropertyTests/Core/Compliance/ (#686)
- .github/coverage-manifest/Encina.Compliance.Applicability.json
```

</details>

---

### Phase 6: Documentation and Finalization

> **Goal**: The feature is documented with multi-criteria examples. **Size**: about 6 files.

<details>
<summary>Tasks</summary>

1. XML docs on all public members (with `<example>` of each criterion type: region, tenant, subject opt-in, data category, operation, module, composite).
2. `docs/features/compliance-rule-engine.md` (a comparison with the programmatic resolvers; the migration section of the issue becomes a comparison because pre-1.0 has no migration support) and the package README section; written by the docs-writer agent.
3. `changelog.d/687-rule-based-compliance-engine.added.md`; `docs/INVENTORY.md`, `ROADMAP.md` when affected.
4. ADR for the rule-based compliance engine (number assigned when the ADR pull request opens): records Design Choices 1 to 4.
5. Build `dotnet build --configuration Release` with 0 warnings; `dotnet test` green; per-flag coverage at the targets.

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT:
- Phases 1-5 merged. Documentation goes through docs-writer; the changelog fragment and PublicAPI lines through mechanical-fixer.

TASK:
Finish XML docs, the feature page, the README, the ADR draft and the changelog fragment.

KEY RULES:
- Never type coverage figures by hand (covref markers); English only; no private client names (write "the reference application").

REFERENCE FILES:
- docs/features/ (an existing compliance feature page)
- changelog.d/README.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | What it asks of this feature |
|--------|-----------|------------------------------|
| GDPR | Art. 2, 3, 5(2) | Territorial scope and accountability: a rule never lowers a duty and the matched rule is recorded |
| LOPDGDD (Ley Orgánica 3/2018) | Art. 1-2 | `AddLopdgdd()` adds the national identifier when a country is ES |
| SPEC-002 | REQ-011, REQ-019, REQ-062, DEC-006, INV-002, INV-005 | Conditional enforcement; skips explicit and logged; instrumentation without personal data; fail closed; non-EU law claims nothing; unknown means full |
| Open Policy Agent, AWS Config Rules, Azure Policy | Declarative policy models | Industry comparison only; OPA integration is #688 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| Contract (to be added by #686) | `src/Encina/Compliance/` | `IComplianceApplicabilityResolver`, `ComplianceContext`, `ComplianceProfile`, `ComplianceCapability`, `ComplianceRegulationCapabilities` |
| Resolver package (to be added by #686) | `src/Encina.Compliance.Applicability/` | Host of the DSL, `CompositeComplianceResolver`, options validator |
| Event ID registry | `src/Encina/Diagnostics/EventIdRanges.cs` | New field `ComplianceRules` |
| EventId architecture test | `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` | `AssemblyRanges` entry |
| Operation audit | `src/Encina.Security.Audit/Abstractions/IOperationAuditStore.cs` | Skip records written by the #686 gate |
| Observability conventions | `docs/architecture/adr/054-observability-conventions-and-trace-propagation.md` | Instrument names |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Compliance.Applicability` (rules) | 6600-6619 (`ComplianceRules`) | New, from the family block 6600-6649 of the plan batch; 6600-6607 planned, packed; 6608-6619 free |
| `Encina.Compliance.OPA` (#688) | 6620-6639 | Owned by the #688 plan |
| `Encina.Compliance.DPIA` screening (#1243) | existing 8897-8899 | Owned by the #1243 plan |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Source (`Rules/`, diagnostics, health) | 12 | Model, builders, validator, templates, resolver, log messages, health check |
| Tests (unit, guard, contract, property, integration, benchmark) | 12 | Under `tests/Encina.*Tests/Compliance/Applicability/` |
| Manifest, PublicAPI, EventId registry, architecture-test map | 4 | Edits |
| Documentation (feature page, README, ADR, changelog, load justification) | 5 | Docs through docs-writer |
| **Total** | **~33** | In 3 to 4 pull requests of at most 15 files |

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library, pre-1.0, Railway Oriented Programming, no compatibility layers. Rules: AGENTS.md.
- Issue #687 builds on the #686 contract (IComplianceApplicabilityResolver, ComplianceProfile, ComplianceContext,
  ComplianceCapability). The Maintainer Decisions of the plan override its recommendations.

IMPLEMENTATION OVERVIEW:
1. Rule model, RegulationRuleBuilder, ComplianceRegulationBuilder, validator, GDPR/LOPDGDD templates.
2. RuleBasedComplianceResolver (union of matched rules, asynchronous predicates, failure fails the rule set), UseRules option, registration.
3. EventIds 6600-6619 (register first), instruments on Encina.Compliance.Applicability, health check.
4. Evidence and tenant tags; tests of every flag; documentation.

KEY PATTERNS:
- Union only; rules add, never remove; no modes, priority or tier input.
- Unknown or failing means the full profile at the final answer (INV-005); the service gate of #686 refuses removals without territorial evidence.
- Startup validation through IValidateOptions with ValidateOnStart; DI test with ValidateOnBuild and ValidateScopes.
- Errors by code or exception type in logs; no subject id; TimeProvider for time.

REFERENCE FILES:
- docs/plans/compliance-applicability-resolver-implementation-plan-686.md
- src/Encina/Diagnostics/EventIdRanges.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
- .github/coverage-manifest/Encina.Compliance.Applicability.json
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ⏭️ | A cross-request profile cache is the open follow-up #3021 of #686; rule evaluation is CPU-bound and in memory |
| 2 | OpenTelemetry | ✅ | Phase 3: span and counters on the Applicability source and meter, ADR-054 names, rule name tag, no subject id |
| 3 | Structured Logging | ✅ | Phase 3: `[LoggerMessage]` 6600-6607 in `ComplianceRules`, registered first |
| 4 | Health Checks | ✅ | Phase 3: empty rule set and dry run |
| 5 | Validation | ✅ | Phase 1: builder and start-up validation (duplicate names, empty set, invalid ids) |
| 6 | Resilience | ❌ | In-process evaluation; an asynchronous predicate owns its I/O and a failure gives the full profile |
| 7 | Distributed Locks | ❌ | Stateless evaluation |
| 8 | Transactions | ❌ | No state change |
| 9 | Idempotency | ❌ | Deterministic evaluation with no message entry point |
| 10 | Multi-Tenancy | ✅ | Phase 4: tenant fields feed predicates; tenant is a tag only when tenancy is on |
| 11 | Module Isolation | ⏭️ | Predicates can read `ComplianceContext.ModuleName`; module-scoped rule sets are deferred to a follow-up only if needed (no issue yet) |
| 12 | Audit Trail | ✅ | Phase 4: the matched rule name in the skip record of #686; the engine writes no record itself |

---

## Prerequisites & Dependencies

### Required Prerequisites

- #686 Phases 1 to 2 merged (contract, package, composite resolver). Design Choice 4 = B also needs the additive no-contribution member agreed in the #686 contract before Phase 2 here.

### Recommended (Not Blocking)

- #3021 (profile cache) and #3022 (registration consistency), the follow-ups of #686.
- #2700 (data subject request rules) consumes this DSL; it is ordered after #687 in v0.17.0.

---

## Next Steps

1. Link this plan from #687 and from #2700 and #688.
2. When #687 is picked up, re-check each Design Choice against the code of #686 as merged; present the four Dilemmas to the maintainer and record the answers in `## Maintainer Decisions`.
3. Ship Phases 1-2, then 3-4, then 5-6, each as its own pull request referencing #687; the last one carries `Fixes #687`.
