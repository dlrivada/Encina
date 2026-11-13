# Implementation Plan: Rule-Based Compliance Engine — Declarative Regulation DSL

> **Issue**: [#687](https://github.com/dlrivada/Encina/issues/687)
> **Type**: Feature
> **Complexity**: Medium (5 phases, no database, 5 pull requests, 49 file changes; see Estimated File Count)
> **Estimated Scope**: ~1,100-1,400 lines of production code + ~1,700-2,100 lines of tests
> **Milestone**: v0.17.0 — Compliance Lifecycle (order set on 2026-10-23: #686, #687, #2700, #688)
> **Depends on**: #686 (the contract; its plan is merged on main)

---

## Summary

A fluent DSL that configures which regulations and which compliance capabilities apply under which conditions, without writing a resolver class. The builder produces a `RuleBasedComplianceResolver` that implements `IComplianceApplicabilityResolver` from #686 and composes with the programmatic resolvers through `CompositeComplianceResolver`.

**Reading of #686 that shapes this plan.** The contract and its decisions are fixed by the #686 plan and are consumed here, not redefined: `ComplianceCapability` is a closed enum of 15 values, `ComplianceProfile` holds a capability set and a regulation set, composition is union only (Design Choice 11, decided 2026-10-30), a resolver never abstains and an unknown context gives the full profile (Design Choice 4, SPEC-002 INV-005), and the service gate refuses any removal that carries no territorial evidence (Design Choices 5, 14 and 15). Four points of the #687 issue text do not hold against that contract and are resolved in Design Choices 1 to 4 below:

1. Where the DSL lives (new package or inside `Encina.Compliance.Applicability`).
2. Whether the DSL compiles to XACML (the #686 plan left this to #687).
3. Whether a rule can express "this capability does not apply" (the issue says rules never lower a profile, but also promises a union that can narrow).
4. What a rule set answers when no rule matches. The issue says "full profile", but the union in `CompositeComplianceResolver` then lets a no-match DSL child cancel the territorial removal of every other child, and its own example (`VoluntaryGdpr`, a subject opt-in) has no effect. This is the finding of this plan.

Two more points touch the #686 options validator and health check and are Design Choices 9 and 11: what the DSL declares in `Reads`, and what its health probe evaluates.

**Out of scope** (all from the issue and the #686 plan): `ComplianceMode`, `WithMode`, `WithPriority`, `SetFallback`, `OnConflict`, `RegulationConflictStrategy` (removed by #686 Design Choices 9, 11 and 4); `SubscriptionTier` as an input (Design Choice 12); `AddCcpa()` and `AddLgpd()` templates (Encina claims nothing for non-EU law, SPEC-002 INV-002); loading rules from JSON or YAML (post-1.0, #688 covers external policies); run-time mutation of rules (rules are immutable after start-up, so no rule versioning and no audit of rule changes is needed).

**Standards**: GDPR Art. 2, 3 and 5(2); LOPDGDD art. 1-2; SPEC-002 REQ-011, REQ-019, REQ-062, DEC-006, INV-002, INV-005.

**Affected packages**: `Encina.Compliance.Applicability` (new in #686; or a new `Encina.Compliance.Rules`, Design Choice 1), `Encina` only for the `EventIdRanges.cs` registration. The #686 contract amendments that Design Choices 4 B and 9 B need are made in #686, not here (see Prerequisites).

**Provider category**: none. Rule evaluation is in memory and stateless; no database, caching, transport or lock code is written.

---

## Design Choices

Seven Dilemmas are open (1, 2, 3, 4, 6, 9 and 11); the maintainer answers them when work on the issue starts. Choices 5, 7, 8 and 10 are Settled or Trivial by earlier maintainer decisions or by rules.

<details>
<summary><strong>1. Where the DSL lives</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Inside `Encina.Compliance.Applicability`** (references only `Encina`) | One package and one registration call; the resolver and its rules ship together; the package gains no dependency; the templates reuse the package's internal EU/EEA list | Every application that resolves applicability also carries the builder types (a few small classes) |
| **B) New `Encina.Compliance.Rules` package referencing `Encina.Compliance.Applicability`** | Pay-for-what-you-use (AGENTS.md section 1): an application with only programmatic resolvers loads no DSL | One more package, README, manifest and architecture-test entry; two registration calls; the internal EU/EEA list of #686 needs `InternalsVisibleTo` or the single source of #3023 |

### Chosen Option: **A — Inside `Encina.Compliance.Applicability`** (recommended, pending the maintainer)

### Rationale

- The builder and the resolver are pure C# on core types; they add no dependency, so the pay-for-what-you-use cost of A is a few kilobytes, while B adds a package whose only reason is to hide them.
- B wins when most applications use programmatic resolvers and the maintainer wants the smallest possible dependency graph per package. The plan is written so that moving the folder `Rules/` to a new project is a mechanical change (no type depends on the host package internals except the EU/EEA list).

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
| **B) A `NotApplicableWhen(...)` rule that must carry evidence kind, country and source** and passes the same service-gate checks (no evidence or no registry-backed alternative is refused and falls back to full) | Territorial exemptions are declarative and audited like the built-in ones | A larger surface; a misconfigured rule is refused only at run time; Design Choice 9 must be re-opened, because the DSL could then remove |

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
| **B) No match is "no contribution".** The rule resolver returns an additive marker that `CompositeComplianceResolver` ignores; a standalone selection, or a composite where every child gives no contribution, turns it into the full profile | Keeps INV-005 at the level of the final answer; lets the DSL add capabilities (a subject opt-in restores GDPR where the region resolver removed it by territorial evidence) and compose with the removal resolvers | Needs an amendment of the #686 contract that the #686 plan does not contain today: its `ComplianceProfile` (Phase 1a) has no no-contribution member, its composite (Phase 2c) unions every child's answer, its contract suite (Phase 9a) asserts "empty context → full profile" and its health check (Phase 8) is `Unhealthy` when the configured resolver does not return the full profile for `ComplianceContext.Empty`. All four change; follow-up issue to be opened (draft: artifacts/decisions/plan-batch/followups/686-contract-amendments-for-687.md), merged before Phase 2 here |

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
<summary><strong>6. Failure of a predicate</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A predicate that throws (other than caller cancellation, Design Choice 10) fails the whole rule set: the resolver returns `Left` `compliance.applicability_failed`, which the #686 service turns into the full profile (#686 Design Choice 6) and logs and counts as `ResolutionFailed`** | Fails closed; the failure is visible in the #686 failure metric and health check; one rule never yields a partial answer | One flaky predicate widens the whole answer; inside a composite, the child `Left` makes the composite `Left` (#686 Phase 2c), so every territorial removal of the other children is cancelled for that request |
| **B) A failing predicate is treated as "does not match"** | Isolates a bad rule | Excluded: a failing gate that silently drops an obligation is the fail-open case AGENTS.md section 3 forbids |
| **C) A failing predicate is treated as "matched": that rule's capabilities are added and the other rules' answers are kept** | Fails closed per rule (the failed rule's capabilities are enforced); the other rules and, in a composite, the other children's removals keep their effect | The failure becomes a partial `Right`: the #686 service logs no `ResolutionFailed`, counts nothing and its health check does not degrade; only this plan's log 6603 and counter see it |

### Chosen Option: **A — Failure fails the rule set** (recommended, pending the maintainer)

### Rationale

- Railway-oriented results report a failure as `Left` (ADR-001, ADR-006; AGENTS.md section 3), and the #686 contract is `ValueTask<Either<EncinaError, ComplianceProfile>>` (#686 Design Choice 2), so A keeps the failure in the path that #686 already logs, counts and turns into the full profile.
- C wins when an application composes the DSL with removal resolvers and an occasionally failing asynchronous predicate must not cancel their territorial removals; the documentation then says that predicate failures are seen only in the rule metrics.

**Class:** Dilemma

</details>

<details>
<summary><strong>7. Templates and start-up validation</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Templates `AddGdpr()` and `AddLopdgdd()` only; duplicate rule names, an empty rule set, a rule without predicate or capabilities and an invalid regulation id fail at start-up through the options validator (`ValidateOnStart`)** | Encina claims only EU law; misconfiguration cannot reach run time | An application writes its own non-EU rules |
| **B) Also `AddCcpa()` and `AddLgpd()`** | Convenient | Encina would claim a mapping of non-EU law it does not maintain |

### Chosen Option: **A — GDPR and LOPDGDD templates, start-up validation**

### Rationale

- Non-EU regulation ids are identifiers; Encina claims nothing for them. `AddGdpr()` uses `ComplianceRegulationCapabilities.For(ComplianceRegulation.Gdpr)` from #686.
- The new rule-set error codes (`compliance.rules.duplicate_rule_name`, `compliance.rules.empty_rule_set`, `compliance.rules.rule_without_predicate`, `compliance.rules.rule_without_capability`) are created with the category overload `EncinaErrors.Create(code, ErrorCategory.Internal, message)` of ADR-062 (merged): they are server-side configuration errors (ADR-062 R1). An invalid regulation id reuses the #686 code `compliance.invalid_regulation` returned by `ComplianceRegulation.Parse`.
- Start-up validation failures are reported by `OptionsValidationException`; no log event is added for them.

**Class:** Settled
**Settled by:** SPEC-002 INV-002; #686 plan Design Choice 14; AGENTS.md section 3 (registration completeness, `ValidateOnBuild`); ADR-062 R1 (configuration errors are `Internal`)

</details>

<details>
<summary><strong>8. EventIds, telemetry names and decision audit</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A new range `ComplianceRules = (6600, 6619)` from the family block, registered in the first pull request that logs (Phase 2); instruments named by ADR-054 on the `Encina.Compliance.Applicability` meter; the rule engine never writes its own audit record, the matched rule names travel in `ResolvedBy`** | One audit writer; the free compliance range 8950-8999 stays with #686 | The assembly maps to two ranges in `EncinaEventIdAllocationTests` |
| **B) Extend `ComplianceApplicability` 8950-8999** | One range per assembly | The tail 8960-8999 is the last free slot of the compliance block (#686 plan, Event ID Allocation) |

### Chosen Option: **A — Range 6600-6619, ADR-054 names, audit through #686**

### Rationale

- The issue named 9800-9999, which the orchestrator registry has since assigned to other families; the plan family block 6600-6649 replaces it (6600-6619 for #687, 6620-6639 for #688).

**Class:** Trivial
**Settled by:** AGENTS.md section 7 and ADR-021 (register first, packed ids); ADR-054 settled point 4 (instrument names); #686 plan Design Choice 15 (decision audit); ADR-036

</details>

<details>
<summary><strong>9. What the rule resolver declares in <code>Reads</code></strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Reads = Operator \| Tenant \| Subject`** (the predicates are opaque, so the DSL claims everything) | No change to #686; the #686 options validator accepts the DSL standalone and in any composite | The #686 validator rejects a built-in selection whose combined `Reads` lacks any of the three flags, which keeps a standalone tenant-aware resolver out (#686 Phase 2a task 3, Design Choice 14). With A, `UseCompositeResolver(tenant-aware + rules)` passes that check although nothing in it reads the subject country; only the service gate still checks it (weaker defence in depth) |
| **B) `Reads = None`, with a #686 validator amendment: the `Reads` check applies only to resolvers that can remove a capability; an add-only resolver (the DSL under Design Choice 3 A) neither satisfies nor fails it** | Keeps the #686 rejection of a tenant-aware resolver without a subject reader; the DSL claims only what it does | Needs the #686 amendment of the follow-up draft of Design Choice 4 (as written today, the #686 validator would reject a standalone `UseRules` with `Reads = None`) |
| **C) The rule resolver does not implement `IComplianceContextReader`, so the validator treats it like a custom `UseResolver<T>`** | No flag to choose | The #686 plan does not say how a composite with a child that does not implement the interface is checked, so the result depends on an undefined case |

### Chosen Option: **B — `Reads = None` with the add-only exemption** (recommended, pending the maintainer)

### Rationale

- The DSL is add-only (Design Choice 3 A), so it never decides a removal and claims no input for one; B keeps the #686 validator's start-up rejection meaningful. The #686 validator does not require `OperatorCountry`: an unset operator country is allowed and means the full profile (#686 Maintainer Decisions, Design Choice 14 correction), so the DSL adds no operator-country requirement.
- A wins when #686 must not change before #687 ships; the documentation then says that the service gate is the only subject-country check for a composite with the DSL.
- With Design Choice 3 B (removal rules) this choice is re-opened: a removal rule must then declare what it reads.

**Class:** Dilemma

</details>

<details>
<summary><strong>10. Cancellation of asynchronous predicates</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) An `OperationCanceledException` raised for the caller's `CancellationToken` propagates from `ResolveAsync`: it is not turned into `Left` and not into the full profile; an `OperationCanceledException` the caller did not request (a predicate's own timeout) is a predicate failure under Design Choice 6** | Cancellation is not an error; the same rule as the rest of the codebase | None for safety: a cancelled request does not run its handler |
| **B) Every `OperationCanceledException` becomes `Left`** | One path for every exception | Reports a caller's cancellation as a resolution failure in the #686 metric |

### Chosen Option: **A — Caller cancellation propagates**

### Rationale

- The codebase rule: `EitherHelpers.TryAsync` re-throws `OperationCanceledException` and converts every other exception (`src/Encina/Results/EitherHelpers.cs:38`); ADR-052 and AGENTS.md section 3 state that cancellation is not an error and propagates.

**Class:** Settled
**Settled by:** AGENTS.md section 3 (cancellation is not an error and propagates); ADR-052; `src/Encina/Results/EitherHelpers.cs:38`

</details>

<details>
<summary><strong>11. What the rule-set health check evaluates</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No predicate is run by the probe: the check is `Healthy` once the rule set is built (start-up validation guarantees its shape) and `Degraded` when `RuleEvaluationFailed` passed `FailureDegradedThreshold` (#686 option) in the window; the data lists the failing rule names** | No I/O per probe; a broken predicate is attributed to its rule | A predicate that never ran is not checked until real traffic reaches it |
| **B) Dry run of the synchronous predicates only, against `ComplianceContext.Empty`** | Catches a throwing synchronous predicate before traffic; no I/O | Asynchronous predicates stay unchecked; an empty context may not exercise the predicate's failing branch |
| **C) Dry run of every predicate, against `ComplianceContext.Empty`, on every probe** | Checks everything | Runs the user's asynchronous I/O (the issue's `subjectPrefsStore` example) at probe frequency |
| **D) No own health check: the #686 check already degrades on repeated resolution failures** | Nothing to build | No per-rule attribution |

### Chosen Option: **A — Failure-count check without a dry run** (recommended, pending the maintainer)

### Rationale

- "Unhealthy when no rule is configured" cannot happen, because an empty rule set is a start-up error (Design Choice 7), so it is not a condition of the check.
- The #686 health check (Phase 8) resolves `ComplianceContext.Empty` through the configured resolver, which runs every DSL predicate when the DSL is configured; the follow-up draft of Design Choice 4 records this, so #686 can decide whether its probe runs user predicates.
- B wins for rule sets of synchronous predicates only; C wins when predicates are cheap and early detection matters more than probe cost; D wins when per-rule attribution is not needed.

**Class:** Dilemma

</details>

---

## Implementation Phases

Each phase is one pull request of at most 15 files and 1500 changed lines; every count below includes source, tests, guard tests, PublicAPI files, manifest edits and the EventId registration. Phase 0 is the gate: #686 Phases 1a-2d are merged first, and with Design Choice 4 B or 9 B the #686 amendment of the follow-up draft is merged before Phase 2 here. Each phase enters the per-file, per-flag targets of the files it adds or touches, with a one-sentence justification, in the same pull request as the tests that meet them (AGENTS.md section 9, #1762).

### Phase 1: Rule Model, Builders and Validation

> **Goal**: The DSL builds an immutable rule set that validates itself. **Size**: 13 files, about 750 lines. No logging in this phase.

<details>
<summary>Tasks</summary>

Folder `src/Encina.Compliance.Applicability/Rules/` (or the new project, Design Choice 1), namespace `Encina.Compliance.Applicability.Rules`.

1. `RegulationRule.cs` (internal sealed record): `string Name`, `ComplianceRegulation? Regulation`, `Func<ComplianceContext, CancellationToken, ValueTask<bool>> Predicate`, `IReadOnlySet<ComplianceCapability> Capabilities`.
2. `RegulationRuleBuilder.cs` (public sealed): `AppliesWhen(Func<ComplianceContext, bool>)`, `AppliesWhenAsync(Func<ComplianceContext, CancellationToken, ValueTask<bool>>)`, `Enforces(params ComplianceCapability[])`; a rule without predicate or without capabilities is a validation error; both are `ArgumentNullException`-guarded.
3. `ComplianceRegulationBuilder.cs` (public sealed): `AddRegulation(string regulationId, Action<RegulationRuleBuilder>)` (parses through `ComplianceRegulation.Parse`) and `AddRule(string ruleName, Action<RegulationRuleBuilder>)`; collects rules in registration order.
4. `RuleSetValidator.cs` (internal): duplicate names, empty set, invalid regulation ids, a rule without predicate or capability; returns `Either<EncinaError, ...>` and is used by the options validator in Phase 2.
5. `ComplianceRulesErrors.cs` (public static): the four codes of Design Choice 7, each through `EncinaErrors.Create(code, ErrorCategory.Internal, message)`; messages are fixed strings without identifiers.
6. Templates `ComplianceRegulationBuilderTemplates.cs` (extension members): `AddGdpr()` (EU/EEA tenant, operator or subject country, or `RegionInEea`; capabilities from `ComplianceRegulationCapabilities.For(Gdpr)`) and `AddLopdgdd()` (country `ES` in tenant, operator or subject country).
7. `src/Encina.Compliance.Applicability/PublicAPI.Unshipped.txt`.
8. Tests: `tests/Encina.UnitTests/Compliance/Applicability/Rules/` (`RegulationRuleBuilderTests.cs`, `ComplianceRegulationBuilderTests.cs`, `RuleSetValidatorTests.cs`, `ComplianceRegulationBuilderTemplatesTests.cs`), `tests/Encina.GuardTests/Compliance/Applicability/Rules/RulesBuilderGuardTests.cs`.
9. `.github/coverage-manifest/Encina.Compliance.Applicability.json`: unit and guard targets for the six source files.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT:
- Issue #687; the #687 plan; the contract is #686 (Phases 1a-2d of the #686 plan merged).
- Maintainer Decisions of the plan override the "recommended" choices.

TASK:
Create the rule model, the two builders, the rule-set validator, the error codes and the GDPR/LOPDGDD templates in
the Rules folder, with PublicAPI lines, unit and guard tests and manifest targets. 13 files; at most 15 files and
1500 lines.

KEY RULES:
- Rules only add capabilities; no mode, priority, conflict strategy or subscription tier exists.
- Non-EU regulations get no template; ids parse through ComplianceRegulation.Parse.
- New error codes use EncinaErrors.Create(code, ErrorCategory.Internal, message) (ADR-062).
- No logging in this phase (the EventId range is registered in Phase 2).
- XML docs on every public member; RS0016 must not fire; no subject id in any message.

REFERENCE FILES:
- src/Encina/Compliance/ComplianceRegulationCapabilities.cs (from #686)
- src/Encina.Compliance.Applicability/Resolvers/CompositeResolverBuilder.cs (builder precedent from #686)
- src/Encina.Compliance.Consent/ConsentOptionsValidator.cs
- src/Encina/Results/EncinaErrors.cs (category overload)
```

</details>

---

### Phase 2: RuleBasedComplianceResolver, Registration and Logging

> **Goal**: The rule set resolves a context, logs by error code and registers with one call. **Size**: 13 files, about 1,100 lines.

<details>
<summary>Tasks</summary>

1. `src/Encina/Diagnostics/EventIdRanges.cs`: register `ComplianceRules = (6600, 6619)` before any `[LoggerMessage]`; the field in `src/Encina/PublicAPI.Unshipped.txt`; add `nameof(EventIdRanges.ComplianceRules)` to the `Encina.Compliance.Applicability` entry of `AssemblyRanges` in `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`. `EventIdRanges.cs` already has its target in `.github/coverage-manifest/Encina.json`, so that manifest is not edited.
2. `Diagnostics/ComplianceRulesLogMessages.cs` (`[LoggerMessage]`, doc `Event IDs: 6600-6604 (see EventIdRanges.ComplianceRules)`): 6600 `RuleMatched` (Debug, rule name), 6601 `MultipleRulesMatched` (Information, count), 6602 `NoRuleMatched` (Debug), 6603 `RuleEvaluationFailed` (Warning, rule name and error code or exception type only). 6604 is added in Phase 3; 6605-6619 stay free.
3. `Rules/RuleBasedComplianceResolver.cs` (public sealed, `IComplianceApplicabilityResolver`; `IComplianceContextReader` per Design Choice 9): evaluates rules in registration order with their asynchronous predicates; the answer is the union of the capabilities and regulations of the matched rules, and `ResolvedBy` names the matched rule names (no subject id); a predicate failure follows Design Choice 6 (A: `Left` with `compliance.applicability_failed`, logged 6603); caller cancellation propagates (Design Choice 10). Behavior with no match follows Design Choice 4 (A: full profile; B: the no-contribution marker of the #686 amendment).
4. `ComplianceApplicabilityOptions.cs` (edit): `UseRules(Action<ComplianceRegulationBuilder>)` selecting the resolver; `ComplianceApplicabilityOptionsValidator.cs` (edit): runs `RuleSetValidator` through `IValidateOptions` with `ValidateOnStart` and applies the `Reads` handling of Design Choice 9.
5. `ServiceCollectionExtensions.cs` (edit): registration inside `AddEncinaComplianceApplicability` (no new top-level method); the rule set is a singleton.
6. `src/Encina.Compliance.Applicability/PublicAPI.Unshipped.txt`.
7. Tests: `tests/Encina.UnitTests/Compliance/Applicability/Rules/RuleBasedComplianceResolverTests.cs` (union of matched rules, rule order, no match per Design Choice 4, failing predicate per Design Choice 6, caller cancellation propagates `OperationCanceledException`, a predicate's own timeout is a failure, `ResolvedBy` lists the matched rule names, log events by code only); `RulesRegistrationTests.cs` (DI test with `ValidateOnBuild` and `ValidateScopes` for three shapes: rules alone, rules inside `UseCompositeResolver`, rules plus a custom resolver rejected; validator cases); `tests/Encina.GuardTests/Compliance/Applicability/Rules/RuleBasedComplianceResolverGuardTests.cs`.
8. `.github/coverage-manifest/Encina.Compliance.Applicability.json`: unit and guard targets for the resolver, the log messages and the three edited files.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT:
- Phase 1 is merged. Issue #687; plan Design Choices 3, 4, 6, 9 and 10 (Maintainer Decisions override the recommendations).
- Contract: IComplianceApplicabilityResolver, ComplianceProfile.Union, ComplianceContextReads (#686).
- With Design Choice 4 B or 9 B, the #686 contract amendment is merged before this phase.

TASK:
Register ComplianceRules = (6600, 6619), add the log messages 6600-6603, implement RuleBasedComplianceResolver, the
UseRules option selection, the validator wiring, the registration and the tests listed. 13 files; at most 15 files
and 1500 lines.

KEY RULES:
- Register the range in EventIdRanges.cs before any [LoggerMessage]; ids packed from 6600.
- Union only; a rule never removes a capability; unknown context gives the full profile at the level of the final answer.
- A failing predicate follows Design Choice 6; caller cancellation propagates (Design Choice 10).
- Error code or exception type only in logs; no subject id, no predicate text.
- Time, if needed, from TimeProvider; no static state.

REFERENCE FILES:
- src/Encina.Compliance.Applicability/Resolvers/CompositeComplianceResolver.cs (#686)
- src/Encina/Compliance/ComplianceProfile.cs (#686)
- src/Encina/Diagnostics/EventIdRanges.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
- src/Encina/Results/EitherHelpers.cs (cancellation rule)
```

</details>

---

### Phase 3: Observability, Health Check and Tenant Tags

> **Goal**: Rule evaluation is traceable, counted, tagged by tenant and checked. **Size**: 10 files, about 650 lines. Gate: #686 Phase 8 (its diagnostics class) is merged.

<details>
<summary>Tasks</summary>

1. `Diagnostics/ComplianceApplicabilityDiagnostics.cs` (#686 Phase 8, edit): instruments on the existing `Encina.Compliance.Applicability` source and meter (ADR-054): `encina.compliance.rule.evaluations` (Counter, tag `rule_name`), `encina.compliance.rule.matches` (Counter, tag `rule_name`), `encina.compliance.rule.evaluation.duration` (Histogram, unit `s`); span `encina.compliance.rule_engine.evaluate` with `rules.count` and `matches.count`; the tenant id as a tag and span attribute only when multi-tenancy is on (ADR-054 D4); no subject id. The issue's `conflict_resolution_count` is dropped with the conflict strategies.
2. `Rules/RuleBasedComplianceResolver.cs` (edit): the span, the counters and the tenant tag.
3. `Diagnostics/ComplianceRulesLogMessages.cs` (edit): 6604 `HealthProbeFailed` (Warning).
4. `Health/RuleSetHealthCheck.cs`: per Design Choice 11 (A: `Degraded` when `RuleEvaluationFailed` passed `FailureDegradedThreshold` in the window, data lists the failing rule names; no predicate dry run).
5. `ServiceCollectionExtensions.cs` (edit): the health check is registered when `AddHealthCheck` is true.
6. `src/Encina.Compliance.Applicability/PublicAPI.Unshipped.txt`.
7. Tests: `tests/Encina.UnitTests/Compliance/Applicability/Rules/RuleBasedComplianceResolverDiagnosticsTests.cs` (in-memory exporter test for the span and counters; tenant tag present only with tenancy; no subject id); `RuleSetHealthCheckTests.cs`; `tests/Encina.GuardTests/Compliance/Applicability/Rules/RuleSetHealthCheckGuardTests.cs`; EventId architecture tests (run, not new).
8. `.github/coverage-manifest/Encina.Compliance.Applicability.json`: targets for the health check and the edited files.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT:
- Phases 1-2 merged; #686 Phase 8 merged. ADR-021 (EventIds) and ADR-054 (instrument names) apply; the range is
  6600-6619 and 6600-6603 are used.

TASK:
Add the rule instruments and span to the Applicability diagnostics, the tenant tag, log message 6604, the health check
of Design Choice 11 and their tests. 10 files; at most 15 files and 1500 lines.

KEY RULES:
- Ids packed: the next id is 6604.
- Error code or exception type only in logs; no subject id, no rule predicate text.
- Tenant tag only when multi-tenancy is on; instruments cost nothing without a listener.
- The health check never runs user predicates (Design Choice 11 A, unless the maintainer chose otherwise).

REFERENCE FILES:
- src/Encina.Compliance.Applicability/Diagnostics/ComplianceApplicabilityDiagnostics.cs (#686)
- src/Encina.Compliance.Applicability/Diagnostics/ComplianceApplicabilityLogMessages.cs (#686)
- src/Encina.Security.BreakTheGlass/Health/BreakTheGlassHealthCheck.cs
```

</details>

---

### Phase 4: Contract, Property, Integration, Load and Benchmark Tests

> **Goal**: The test types no earlier phase covers, and the targets they raise. **Size**: 6 files, about 700 lines. Gate: #686 Phases 3a (service) and 9a (resolver contract suite) are merged.

<details>
<summary>Tasks</summary>

1. Contract: `tests/Encina.ContractTests/Compliance/Applicability/RuleBasedComplianceResolverContractTests.cs` derives the abstract resolver contract suite of #686 Phase 9a and runs it against `RuleBasedComplianceResolver` (with Design Choice 4 B, the amended assertion for the no-contribution marker).
2. Property (FsCheck through `Encina.Testing.FsCheck`): `tests/Encina.PropertyTests/Compliance/Applicability/RuleBasedComplianceResolverPropertyTests.cs`: the answer is monotonic in the matched rules and independent of rule order; any rule set with no matching rule gives the Design Choice 4 answer; adding a rule never removes a capability.
3. Integration (no Docker): `tests/Encina.IntegrationTests/Compliance/Applicability/RuleBasedComplianceIntegrationTests.cs`, `[Trait("Category", "Integration")]`: `AddEncinaComplianceApplicability` with `UseRules` through a pipeline with the #686 service; with Design Choice 4 B, `VoluntaryGdpr` restores GDPR in a composite with the region resolver; the skip record written by the #686 gate (InMemory operation-audit store) contains no subject id.
4. Load: `.md` justification (in-memory, stateless) at `tests/Encina.LoadTests/Compliance/Applicability/RuleBasedComplianceResolver.md`.
5. Benchmark: `tests/Encina.BenchmarkTests/Compliance/RuleBasedComplianceResolverBenchmarks.cs`, evaluation of N rules by M capabilities (BenchmarkSwitcher, results under `artifacts/performance/`).
6. `.github/coverage-manifest/Encina.Compliance.Applicability.json`: contract, property and integration targets for the `Rules/` files, with justifications; CRAP of every method of the feature at most 10, measured locally.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT:
- Phases 1-3 merged; #686 Phases 3a and 9a merged. Coverage is per flag (AGENTS.md section 9); tests execute real code.

TASK:
Write the contract, property and integration tests, the benchmark and the load justification; raise the contract,
property and integration targets of the Rules files; verify per-flag coverage and CRAP. 6 files.

KEY RULES:
- Shouldly through Encina.Testing.Shouldly; FsCheck through Encina.Testing.FsCheck; no reflection-only tests; no Thread.Sleep.
- The contract test derives the #686 suite; it is never replaced by a .md justification.
- Measure with coverage-report.cs and run the crap gate locally before the PR.

REFERENCE FILES:
- tests/Encina.ContractTests/Compliance/Applicability/ (resolver contract suite, #686 Phase 9a)
- tests/Encina.PropertyTests/Compliance/Applicability/ (#686 Phase 9a)
- .github/coverage-manifest/Encina.Compliance.Applicability.json
```

</details>

---

### Phase 5: Documentation and Finalization

> **Goal**: The feature is documented with multi-criteria examples. **Size**: 7 files, about 600 lines.

<details>
<summary>Tasks</summary>

1. XML docs are written with the code in Phases 1-3 (each public member, with an `<example>` of each criterion type: region, tenant, subject opt-in, data category, operation, module, composite); this phase checks them.
2. `docs/features/compliance-rule-engine.md` (a comparison with the programmatic resolvers; the migration section of the issue becomes a comparison because pre-1.0 has no migration support) and the package README section `src/Encina.Compliance.Applicability/README.md`; written by the docs-writer agent.
3. `changelog.d/687-rule-based-compliance-engine.added.md`; `docs/INVENTORY.md` and `ROADMAP.md`.
4. ADR for the rule-based compliance engine (ADR number assigned when the ADR pull request opens) and its row in `docs/architecture/adr/index.md`: records Design Choices 1 to 4, 6, 9 and 11.
5. Build `dotnet build --configuration Release` with 0 warnings; `dotnet test` green; per-flag coverage at the targets. The last pull request carries `Fixes #687`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT:
- Phases 1-4 merged. Documentation goes through docs-writer; the changelog fragment through mechanical-fixer.

TASK:
Check the XML docs, write the feature page, the README section, the ADR draft with its index row and the changelog
fragment; update INVENTORY and ROADMAP. 7 files.

KEY RULES:
- Never type coverage figures by hand (covref markers); English only; no private client names (write "the reference application").

REFERENCE FILES:
- docs/features/ (an existing compliance feature page)
- changelog.d/README.md
- docs/architecture/adr/index.md
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
| Resolver package (to be added by #686) | `src/Encina.Compliance.Applicability/` | Host of the DSL, `CompositeComplianceResolver`, options validator, diagnostics |
| Error factory with category (ADR-062, merged) | `src/Encina/Results/EncinaErrors.cs`, `src/Encina/Errors/ErrorCategory.cs` | Rule-set error codes |
| Cancellation rule | `src/Encina/Results/EitherHelpers.cs:38` | Design Choice 10 |
| Event ID registry | `src/Encina/Diagnostics/EventIdRanges.cs` | New field `ComplianceRules` |
| EventId architecture test | `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` | `AssemblyRanges` entry |
| Operation audit | `src/Encina.Security.Audit/Abstractions/IOperationAuditStore.cs` | Skip records written by the #686 gate |
| Observability conventions | `docs/architecture/adr/054-observability-conventions-and-trace-propagation.md` | Instrument names |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Compliance.Applicability` (rules) | 6600-6619 (`ComplianceRules`) | New, from the family block 6600-6649 of the plan batch; registered in Phase 2; 6600-6604 planned, packed (6600-6603 in Phase 2, 6604 in Phase 3); 6605-6619 free |
| `Encina.Compliance.OPA` (#688) | 6620-6639 | Owned by the #688 plan |
| `Encina.Compliance.DPIA` screening (#1243) | existing 8897-8899 | Owned by the #1243 plan |

### Estimated File Count

| Pull request | Files | Content |
|--------------|-------|---------|
| Phase 1 | 13 | 6 source, package PublicAPI, 4 unit test files, 1 guard test file, 1 manifest |
| Phase 2 | 13 | resolver, log messages, `EventIdRanges.cs`, options and validator and registration edits (3), core and package PublicAPI (2), architecture-test map, 2 unit test files, 1 guard test file, 1 manifest |
| Phase 3 | 10 | diagnostics, resolver, log messages and registration edits (4), health check, package PublicAPI, 2 unit test files, 1 guard test file, 1 manifest |
| Phase 4 | 6 | contract, property and integration tests, load `.md`, benchmark, 1 manifest |
| Phase 5 | 7 | feature page, README, changelog fragment, ADR, ADR index, `INVENTORY.md`, `ROADMAP.md` |
| **Total** | **49** | 49 file changes in 5 pull requests (a file edited in several pull requests counts in each); each pull request at most 15 files and 1500 lines |

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
1. Rule model, RegulationRuleBuilder, ComplianceRegulationBuilder, validator, error codes, GDPR/LOPDGDD templates.
2. EventIds 6600-6619 registered, log messages 6600-6603, RuleBasedComplianceResolver (union of matched rules,
   asynchronous predicates, failure per Design Choice 6, cancellation propagates), UseRules option, registration.
3. Instruments and span on Encina.Compliance.Applicability, tenant tag, log 6604, health check per Design Choice 11.
4. Contract (the #686 suite), property, integration, load justification, benchmark.
5. Documentation, ADR, changelog.

KEY PATTERNS:
- Union only; rules add, never remove; no modes, priority or tier input.
- Unknown or failing means the full profile at the final answer (INV-005); the service gate of #686 refuses removals without territorial evidence.
- Startup validation through IValidateOptions with ValidateOnStart; DI test with ValidateOnBuild and ValidateScopes.
- New error codes with an ErrorCategory (ADR-062); errors by code or exception type in logs; no subject id; TimeProvider for time.
- Each pull request enters the coverage targets of its own files with the tests that meet them.

REFERENCE FILES:
- the #686 plan (merged on main)
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
| 3 | Structured Logging | ✅ | Phase 2: `[LoggerMessage]` 6600-6603 in `ComplianceRules`, registered in the same pull request; Phase 3: 6604 |
| 4 | Health Checks | ✅ | Phase 3: rule-set health check per Design Choice 11 |
| 5 | Validation | ✅ | Phases 1-2: builder and start-up validation (duplicate names, empty set, invalid ids) |
| 6 | Resilience | ❌ | In-process evaluation; an asynchronous predicate owns its I/O and a failure follows Design Choice 6 |
| 7 | Distributed Locks | ❌ | Stateless evaluation |
| 8 | Transactions | ❌ | No state change |
| 9 | Idempotency | ❌ | Deterministic evaluation with no message entry point |
| 10 | Multi-Tenancy | ✅ | Phase 3: tenant fields feed predicates; tenant is a tag only when tenancy is on |
| 11 | Module Isolation | ❌ | Predicates read `ComplianceContext.ModuleName`, so a rule can be scoped to a module without a separate module-scoped rule set |
| 12 | Audit Trail | ✅ | The matched rule names travel in `ResolvedBy` (Phase 2); skip records are written by the #686 gate; the engine writes no record itself |

---

## Prerequisites & Dependencies

### Required Prerequisites

- #686 Phases 1a-2d merged (contract, package, composite resolver); Phase 3 here also needs #686 Phase 8, Phase 4 needs #686 Phases 3a and 9a.
- With Design Choice 4 B or 9 B: an amendment of the #686 contract, which the #686 plan does not contain today (no no-contribution member, the composite unions every child, the contract suite and the health check expect the full profile for an empty context, and the validator applies the `Reads` check to every built-in resolver). Follow-up issue to be opened (draft: artifacts/decisions/plan-batch/followups/686-contract-amendments-for-687.md); it is merged before Phase 2 here.

### Recommended (Not Blocking)

- #3021 (profile cache) and #3022 (registration consistency), the follow-ups of #686.
- #2700 (data subject request rules) consumes this DSL; it is ordered after #687 in v0.17.0.

---

## Next Steps

1. Link this plan from #687 and from #2700 and #688.
2. When #687 is picked up, re-check each Design Choice against the code of #686 as merged; present the seven Dilemmas to the maintainer and record the answers in `## Maintainer Decisions`; open the #686 amendment issue from its draft if Design Choice 4 B or 9 B is chosen.
3. Ship Phases 1 to 5 as five pull requests, one per phase, each referencing #687; the last one carries `Fixes #687`.
