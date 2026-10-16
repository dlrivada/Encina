# ADR-050: A Module May Require Other Modules When It Needs Them for Its Guarantees

## Status

**Accepted** - 2026-10-16, issue [#2381](https://github.com/dlrivada/Encina/issues/2381). The general rule was decided by the maintainer on 2026-10-16 (decision 1, option a, in the comment thread of [#2239](https://github.com/dlrivada/Encina/issues/2239)). The sub-decisions listed in #2381 other than the ones above are not decided here (the registration mechanism per row, in-memory implementations outside Development, how multi-instance is declared, default `EnforcementMode`, audit as a precondition of DSR, NIS2, AI Act and ABAC, forbidden combinations as registration errors or warnings, how the Outbox reaches brokers, build order); each is recorded as an amendment when the maintainer picks.

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
2. Requirements are enforced by startup validation: the application fails fast, and the message names the missing module and how to add it. Forbidden combinations fail at startup too (for example `TransactionPipelineBehavior` together with Marten). The validator is implemented by [#2382](https://github.com/dlrivada/Encina/issues/2382).
3. Enforcement is by `TryAdd` registration or startup validation, never by a silent default: a module never starts in a state where its guarantee is false without saying so.

### Closed list per module

| Module | Requires | Forbids | Condition | Source |
| --- | --- | --- | --- | --- |
| Event sourcing (`Encina.Marten`) | Outbox (`OutboxStoreMarten`, written in the Marten session); Inbox for domain events; Recoverability and Dead Letter Queue; processor health and metrics; multi-instance coordination | `TransactionPipelineBehavior` (the Marten session is already transactional) | Always; how multi-instance is declared is an open sub-decision | [#2239](https://github.com/dlrivada/Encina/issues/2239), [#2383](https://github.com/dlrivada/Encina/issues/2383) |
| Dead Letter Queue | The operation-audit adapter | | Always | [#2013](https://github.com/dlrivada/Encina/issues/2013) |
| Operation audit | An `IPiiMasker` | | Unless the explicit, logged opt-out is set | [#1835](https://github.com/dlrivada/Encina/issues/1835) |
| Delayed retries (Recoverability) | An `IDelayedRetryStore` | | Always, because delayed retries are on by default | [#2248](https://github.com/dlrivada/Encina/issues/2248) |
| Crypto-shredding locators | Every registered locator is reachable through the resolved one | | Always | [#1773](https://github.com/dlrivada/Encina/issues/1773) |
| All other module families | Pending | Pending | Pending | The module dependency matrix study and its issues [#2382](https://github.com/dlrivada/Encina/issues/2382) to [#2413](https://github.com/dlrivada/Encina/issues/2413) |

Entries for the remaining module families are not invented here. Each issue of the range above that settles a family adds its row by amendment, with its source issue.

### Validator pattern

The mechanism per row is not decided here. The recommended option of #2381 (Option A), pending the maintainer's pick, is: an `IHostedService` that throws in `StartAsync` where a dependency has several providers; `TryAdd` auto-registration where it has exactly one, so the application's own registration wins; a throw in `AddEncina*` for statically detectable forbidden combinations. Whether a forbidden combination is a registration error or a warning is likewise open; the maintainer's instruction is that it fails at startup. The issue that implements each row ([#2382](https://github.com/dlrivada/Encina/issues/2382) for the validator) settles the mechanism.

## Alternatives considered

- **Documentation only** (publish the matrix, rely on guides): rejected. The guarantees stay false by default, which contradicts the fail-closed rule of SPEC-002 DEC-006.
- **Startup validation only, no auto-registration**: not excluded for any row; it is the mechanism when a dependency has several providers.
- **Keep the blanket rule** ("NEVER make a pattern mandatory"): rejected. It forbids the modules from stating what they need to be correct.

## Consequences

- `AGENTS.md` section 3 must be amended to state the rule and link this ADR (the edit is made by the orchestrator in the same PR).
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

2026-10-16
