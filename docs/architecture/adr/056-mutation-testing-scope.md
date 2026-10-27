# ADR-056: Mutation Testing Scope: A Risk-Based List That Only Grows, Toward All Packages

## Status

**Accepted** - 2026-10-27, issue [#2828](https://github.com/dlrivada/Encina/issues/2828). The maintainer decided on 2026-10-27, in the comment thread of the issue: option A now, option C as the final goal.

## Context

Only `src/Encina/Encina.csproj` is mutated. `.github/stryker-config.json` sets `"project": "Encina.csproj"`, and every shard of `.github/workflows/mutation-tests.yml` mutates a slice of that project (the comment above the `select-matrix` job says so). The mutation dashboard on 2026-10-25 showed a single package in scope.

No document said which packages must be mutated. [AGENTS.md](../../../AGENTS.md) section 9 ("Mutation score is per file with no project-wide target") and [SPEC-000](../../specifications/SPEC-000-encina-1.0-baseline-and-release-scope.md) REQ-008 cover reproducibility and the absence of a score threshold, not scope. The [mutation measurement methodology](../../testing/mutation-measurement-methodology.md) described the core-only run without justifying it, and no ADR decided it.

The packages outside the core include the code where a surviving mutant matters most: the fail-closed gates of AGENTS.md section 3 (authorization, encryption, anti-tampering, PII, sanitization, the compliance modules, tenancy), the outbox, inbox and saga stores, caching stampede protection and tag invalidation, distributed locks and resilience.

Two technical limits shape the decision. The upstream Microsoft Testing Platform runner of Stryker under-reports kills (see the methodology page, section "Concurrency 1"), and one `Encina.UnitTests` project runs for every mutant, so widening the scope without a criterion would multiply cost and noise.

## Decision

**Option A now, option C as the final goal.** The mutation scope is risk-based and written down.

### The criterion

A package enters the mutation scope if it contains any of:

1. a fail-closed gate (AGENTS.md section 3, compliance and security gates);
2. security or personal-data handling;
3. a state machine or concurrency;
4. high cyclomatic complexity (the same measure as the CRAP gate of AGENTS.md section 9).

No numeric threshold is defined. The judgement is made per package and recorded with its reason.

### Who applies it, and where the list lives

The criterion is applied per package by the test-obligations-assessor agent planned in [#2911](https://github.com/dlrivada/Encina/issues/2911). This ADR does not list packages: the list is the output of that assessment and lives where #2911 decides.

### Ratchet

The list only grows. Removing a package from it needs a reviewed pull request that states why.

### Review trigger

Just before 1.0, after all pre-1.0 development, this decision is re-opened to get as close to option C as is realistic, with all CI time available. A reminder issue in milestone v1.0.0-rc.1 carries the trigger.

## Alternatives considered

- **B, core only, explicitly decided**: rejected. It costs nothing and wins if the upstream runner bug stays open until 1.0 and extra scope only adds noise, but the security and compliance code would stay without mutation evidence.
- **C, all packages now**: deferred as the final goal, not rejected. It is a uniform rule with no judgement, but it needs per-package test runs instead of one `Encina.UnitTests` project and its cost grows with the number of packages. It becomes realistic when CI time and a per-package test split are available.

Option A was chosen because it spends mutation time where a surviving mutant is a real defect, its list is auditable, and it is the one option that still works with a limited budget and the upstream runner bug open.

## Consequences

- The criterion needs judgement and upkeep, and packages near the line are debatable. The assessment records the reason per package so the choice can be audited.
- The scope grows by pull request, so the mutation workload grows in steps that CI can absorb. The ratchet prevents a quiet shrink.
- Extending Stryker beyond `src/Encina` is separate infrastructure work, not part of this decision.
- The methodology page states the scope and links this ADR, and AGENTS.md section 9 carries one sentence with the rule.
- The reminder at the 1.0 milestone makes the final review a tracked item, not a memory.

### What each point implies for the code

| Implication | Owner |
| --- | --- |
| Extending Stryker beyond `src/Encina`: per-package projects or shards | [#2829](https://github.com/dlrivada/Encina/issues/2829) |
| Applying the criterion to each package and producing the list | [#2911](https://github.com/dlrivada/Encina/issues/2911) |
| Mutation measurement integrity work this scope builds on | [#2845](https://github.com/dlrivada/Encina/issues/2845) |
| The 1.0 review of the scope | A reminder issue in milestone v1.0.0-rc.1 |

## Related

- [Mutation measurement methodology](../../testing/mutation-measurement-methodology.md), section "Scope"
- [SPEC-000](../../specifications/SPEC-000-encina-1.0-baseline-and-release-scope.md) (REQ-008)
- [AGENTS.md](../../../AGENTS.md) section 9
- Issues: [#2828](https://github.com/dlrivada/Encina/issues/2828) (the spike), [#2829](https://github.com/dlrivada/Encina/issues/2829), [#2845](https://github.com/dlrivada/Encina/issues/2845), [#2911](https://github.com/dlrivada/Encina/issues/2911), [#1441](https://github.com/dlrivada/Encina/issues/1441) (Stryker 5 with the MTP runner)

## Date

2026-10-27
