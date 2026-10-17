# ADR-050: A Module May Require Other Modules When It Needs Them for Its Guarantees

## Status

**Accepted** - 2026-10-17, issue [#2381](https://github.com/dlrivada/Encina/issues/2381). The general rule was decided by the maintainer on 2026-10-16 (decision 1, option a, in the comment thread of [#2239](https://github.com/dlrivada/Encina/issues/2239)). The enforcement mechanism (Option A) was decided by the maintainer on 2026-10-17; the other seven design choices were settled by elimination the same day and are recorded under "Sub-decisions" below.

## Context

`AGENTS.md` section 3 used to say that every messaging pattern is optional and disabled by default, and that no pattern may be made mandatory. Applied to every module without exception, that rule makes several guarantees false: an Outbox without a transaction, a row-claiming lock or a transport publisher does not deliver "at least once"; a compliance gate without an audit store cannot prove anything; an in-memory key or nonce store loses data on restart. The module dependency matrix study of 2026-10-16 (summarised in [#2239](https://github.com/dlrivada/Encina/issues/2239)) found these cases.

The pay-for-what-you-use principle (every feature opt-in) is still correct for everything a module does not need. The conflict is only about what a module needs for the guarantee it advertises.

## Decision

**A module MAY require other modules when it needs them for its guarantees.** Opt-in remains the rule for everything a module does not need.

Definitions:

| Term | Meaning |
| --- | --- |
| Required | The module's guarantee is false without the other module. Enabling the module without it is a startup error. |
| Conditional | Required only under a stated condition (multi-instance, multi-tenant, personal data, an environment other than Development). The condition is written in the table. |
| Forbidden | Enabling both modules together breaks a guarantee. The combination is a startup error. |

Rules:

1. The closed list of required, conditional and forbidden modules of each module is the table in this ADR. A requirement that is not in the table does not exist; adding one is an amendment to this ADR.
2. Each row is enforced by the earliest safe mechanism of the next section. Every failure names the module, the requirement and ADR-050, and says how to add the missing module. The startup validator is implemented by [#2382](https://github.com/dlrivada/Encina/issues/2382).
3. Enforcement is by `TryAdd` registration, startup validation or a registration error, never by a silent default: a module never starts in a state where its guarantee is false without saying so.

### Closed list per module

| Module | Requires | Forbids | Condition | Source |
| --- | --- | --- | --- | --- |
| Event sourcing (`Encina.Marten`) | Outbox (`OutboxStoreMarten`, written in the Marten session); Inbox for domain events; Recoverability and Dead Letter Queue; processor health and metrics; multi-instance coordination | `TransactionPipelineBehavior` (the Marten session is already transactional) | Always; multi-instance is always assumed (sub-decision 3) | [#2239](https://github.com/dlrivada/Encina/issues/2239), [#2383](https://github.com/dlrivada/Encina/issues/2383) |
| Dead Letter Queue | The operation-audit adapter | | Always | [#2013](https://github.com/dlrivada/Encina/issues/2013) |
| Operation audit | An `IPiiMasker` | | Unless the explicit, logged opt-out is set | [#1835](https://github.com/dlrivada/Encina/issues/1835) |
| Delayed retries (Recoverability) | An `IDelayedRetryStore` | | Always, because delayed retries are on by default | [#2248](https://github.com/dlrivada/Encina/issues/2248) |
| Crypto-shredding locators | Every registered locator is reachable through the resolved one | | Always | [#1773](https://github.com/dlrivada/Encina/issues/1773) |
Module families not listed have no recorded requirement yet. Each issue of the range [#2382](https://github.com/dlrivada/Encina/issues/2382) to [#2413](https://github.com/dlrivada/Encina/issues/2413) that settles a family adds its row by amendment, with its source issue; the list is closed in the sense that a row exists only through such an amendment.

### Enforcement mechanism per row (sub-decision 1, Option A)

Each row uses the earliest safe mechanism:

| Situation | Mechanism |
| --- | --- |
| The required dependency has a single possible implementation | Auto-registration with `TryAdd`; the application's own registration wins |
| The dependency has several providers, or the registered one is test-only outside Development | Startup validation: an `IHostedService` that throws in `StartAsync` |
| The combination is detectable statically (a forbidden pair, a missing option) | A registration error thrown by the `AddEncina*` method |

The validator is [#2382](https://github.com/dlrivada/Encina/issues/2382); the issue that implements each row picks the mechanism from this table.

### Sub-decisions

Sub-decision 1 is the table above. Sub-decisions 2 to 8 were settled by elimination on 2026-10-17: in each, one option is the only one that satisfies the existing rules, the law and comparable products, so no maintainer choice was needed.

| # | Question | Decision | Forcing reason |
| --- | --- | --- | --- |
| 2 | In-memory implementations outside Development | Startup error; the only escape is an explicit, logged opt-out | [SPEC-002](../../specifications/SPEC-002-eu-regulatory-readiness.md) DEC-006 (fail closed, the only opt-out is explicit and logged); GDPR art. 32 (in-memory key and nonce stores lose data on restart) |
| 3 | How multi-instance is declared | Multi-instance is assumed; an application declares single-instance explicitly | The safe default must be the default (GDPR art. 25 "by default", fail-closed rule); the reverse makes duplicate processing the silent default. For event sourcing it is always on ([#2239](https://github.com/dlrivada/Encina/issues/2239) decision 5) |
| 4 | Default `EnforcementMode` of the seven compliance modules | Enforce (block) by default; warn-only is the explicit opt-out | SPEC-002 DEC-006; implemented in [#2409](https://github.com/dlrivada/Encina/issues/2409) |
| 5 | When Audit is mandatory for DSR, NIS2, AI Act and ABAC decision audit | Mandatory whenever the module is enabled; the audit write is awaited and its failure fails the operation | GDPR art. 5(2) accountability, NIS2 art. 21, AI Act art. 12 record-keeping; `AGENTS.md` section 3 "errors are never swallowed" |
| 6 | How the Outbox reaches brokers | Follows its own feature issue, not decided here | [#2385](https://github.com/dlrivada/Encina/issues/2385) already covers it |
| 7 | Forbidden combinations: registration errors or warnings | Registration errors (startup error for Marten with `TransactionPipelineBehavior`, [#2239](https://github.com/dlrivada/Encina/issues/2239) decision 4) | Sub-decision 1: statically detectable combinations throw in `AddEncina*` |
| 8 | Build order | Not a design decision: [#2184](https://github.com/dlrivada/Encina/issues/2184) (stages, ADR-049) and [#1251](https://github.com/dlrivada/Encina/issues/1251) (row claiming, ADR-043) first, then the requirements validator [#2382](https://github.com/dlrivada/Encina/issues/2382) | Ordering follows priority and dependencies |

## Alternatives considered

- **Documentation only** (publish the matrix, rely on guides): rejected. The guarantees stay false by default, which contradicts the fail-closed rule of SPEC-002 DEC-006.
- **Startup validation only, no auto-registration**: rejected as the general rule. Where a dependency has exactly one implementation, `TryAdd` removes the failure at no cost and the application's own registration still wins. Startup validation remains the mechanism when a dependency has several providers.
- **Keep the blanket rule** ("NEVER make a pattern mandatory"): rejected. It forbids the modules from stating what they need to be correct.

## Consequences

- `AGENTS.md` section 3 was amended in the same change to state the rule and cite this ADR ("(why: #2239, #2381; ADR-050)").
- [ADR-019](019-compliance-event-sourcing-marten.md) and [ADR-028](028-domain-events-versus-integration-events.md) carry dated amendment notes for the event-sourcing route (publication by the behavior is superseded; domain events of event-sourced modules are also delivered with guarantees, [#2383](https://github.com/dlrivada/Encina/issues/2383)).
- Each module's requirement list is checkable by a DI test that builds the provider with `ValidateOnBuild` and `ValidateScopes`.
- An application that enabled a module without its requirements now fails at startup. Pre-1.0 policy accepts this.
- The table is closed: adding a requirement needs an amendment, which keeps the list reviewable.

## Related

- [ADR-018](018-cross-cutting-integration-principle.md) (cross-cutting integration check)
- [ADR-019](019-compliance-event-sourcing-marten.md), [ADR-028](028-domain-events-versus-integration-events.md), [ADR-029](029-recoverability-error-classification.md), [ADR-036](036-three-audit-stores.md), [ADR-046](046-persistent-dead-letter-queue.md)
- [SPEC-002](../../specifications/SPEC-002-eu-regulatory-readiness.md) DEC-006 (fail-closed gates)
- Reserved: ADR-049 (pipeline execution order, [#2184](https://github.com/dlrivada/Encina/issues/2184)), ADR-043 (row claiming, [#1251](https://github.com/dlrivada/Encina/issues/1251)), ADR-037 (transactional outbox, [#718](https://github.com/dlrivada/Encina/issues/718))
- Issues [#2381](https://github.com/dlrivada/Encina/issues/2381), [#2239](https://github.com/dlrivada/Encina/issues/2239), [#2382](https://github.com/dlrivada/Encina/issues/2382)

## Date

2026-10-17
