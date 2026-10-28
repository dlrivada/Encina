# ADR-058: Encina.Cli Command Architecture: An In-App Command Host and One Command Contract (Accepted)

## Status

**Accepted** - 2026-10-28, issue [#2605](https://github.com/dlrivada/Encina/issues/2605) (parent EPIC [#2614](https://github.com/dlrivada/Encina/issues/2614)). The maintainer decided the four open gates, D2, D7, D14 and D21, on 2026-10-28; the points that follow from a rule were screened on 2026-10-27 and are listed under "Settled points".

The names of types in this ADR (`IEncinaCommand`, `AddEncinaCommand<T>()`, `IOutboxAdministration`, `ISagaAdministration`) are **illustrative**: none exists in `src/` today. The implementation plans fix the final names.

## Context

Today `src/Encina.Cli` only scaffolds. Its `Program.cs` registers `NewCommand`, `GenerateCommand` and `AddCommand` and works on files, without ever starting the user's application. The maintainer decision U1 (2026-10-21, recorded on [#2518](https://github.com/dlrivada/Encina/issues/2518#issuecomment-6313546373) and [#2520](https://github.com/dlrivada/Encina/issues/2520#issuecomment-6313546896)) ships the tool in 1.0 with the scope of [#47](https://github.com/dlrivada/Encina/issues/47) (`new`, `generate`, `add`), with no `dotnet new` templates and no `remove` or `list` commands. EPIC #2614 then took the tool beyond scaffolding: introspection, schema, messaging operations, event sourcing and compliance commands, in six children (#2607 to #2612).

Every command beyond scaffolding needs the user's real configuration and dependency-injection container, so the command architecture has to be decided before any of those features is planned ([AGENTS.md](../../../AGENTS.md) section 11: a feature gets a plan first, and the plan needs this decision). Three constraints frame it:

- Commands are written once over the store abstractions, so they work on all 10 database providers (AGENTS.md section 5).
- A module contributes commands only when it is registered (pay-for-what-you-use, AGENTS.md section 1).
- Mutating commands carry the safety rules of AGENTS.md section 3: dry-run by default with explicit confirmation, fail closed when context such as the tenant is missing, and every execution audited ([ADR-018](018-cross-cutting-integration-principle.md) function 12).

The store abstractions do not have the operations an operator needs. `IOutboxStore` (`src/Encina.Messaging/Outbox/IOutboxStore.cs`) and `ISagaStore` (`src/Encina.Messaging/Sagas/ISagaStore.cs`) have no list, purge or inspect operation; their members serve the runtime processors (`AddAsync`, `GetPendingMessagesAsync`, `GetStuckSagasAsync`, and so on).

## Decision

### Settled points

These points follow from a rule and were not open choices. The maintainer can veto any of them.

| Id | Point | Rule |
| --- | --- | --- |
| D6 | Processors and other hosted services do not run in command mode. The ADR-050 requirement validators are hosted services (an `IHostedService` that throws in `StartAsync`, [ADR-050](050-modules-may-require-modules.md) line 104), and they still run, so a command never starts on an invalid composition. How the suppression works is left to the plans. | AGENTS.md section 3 (fail closed); ADR-050 |
| D8 | Only registered modules contribute commands. | AGENTS.md sections 1 and 3 (every feature opt-in); ADR-050 |
| D10 | Mutating commands are dry-run by default and execute only on explicit confirmation. | EPIC #2614 and the context of #2605 (the consequence of failing closed is in AGENTS.md section 3, but the dry-run default itself is recorded only in the EPIC and the issue) |
| D13 | Commands fail closed when context such as the tenant is missing; the only opt-out is explicit and logged. | AGENTS.md section 3; ADR-050 (Multi-tenancy row, `RequireTenant`) |
| D15, D16 | Every execution of a mutating command is recorded in the operation audit (`IOperationAuditStore`, [ADR-036](036-three-audit-stores.md) decision 2); the entry holds the error code or exception type, never `EncinaError.Message`. | ADR-018 function 12; ADR-036; AGENTS.md section 3 |
| D17 | The audit is written ahead and awaited; an audit failure fails the command. | ADR-050 sub-decision 5 (line 120); AGENTS.md section 3 |
| D18 | When no persistent audit store is registered outside Development, startup validation fails; the only exit is an explicit, logged opt-out. | ADR-050 sub-decision 2 (line 117) and the Audit stores row (line 69); AGENTS.md section 3 |
| D19 (part) | Commands that read personal data go to the read audit (`IReadAuditStore`) when it is enabled. Whether dry-runs are audited is left to the plans. | ADR-036; ADR-018 function 12 |
| D20 | Commands depend on abstractions and carry no provider-specific code, so switching provider changes only the DI registration. | AGENTS.md sections 3 and 5 |
| D22 | Read-only commands need no confirmation and no dry-run; they still obey the fail-closed rule where they touch tenant data. | Issue [#2608](https://github.com/dlrivada/Encina/issues/2608) ("need no confirmation"); AGENTS.md section 3 |
| D26 | `describe` and `doctor` judge only Encina's own registrations, contracts and configuration, never the application's own code. | Maintainer principle C1b, [#1770 comment](https://github.com/dlrivada/Encina/issues/1770#issuecomment-6436978234) |
| D27 | Test obligations: unit and guard tests; integration tests on real databases for every provider-dependent command; contract tests for the public module-author contract; tests execute real code. | AGENTS.md section 9 |
| D29 | The module-author contract and its registration extension are public API (XML docs, PublicAPI lines, contract tests). The existing scaffolding types stay `internal`. | AGENTS.md sections 8 and 9; [#2539](https://github.com/dlrivada/Encina/issues/2539) |
| D30 | The new package registers its EventId range before use, and its `ActivitySource` and `Meter` follow the `Encina.*` PascalCase names, one per package. The placeholders `encina.cli.command` and `encina.cli.command_count` in the child issues must follow this. | AGENTS.md section 7; [ADR-054](054-observability-conventions-and-trace-propagation.md) line 43 |
| D32 | All six EPIC children stay in 1.0. This supersedes the EPIC wording "1.0 fit decided by SPEC-002" for #2612. | [SPEC-000](../../specifications/SPEC-000-encina-1.0-baseline-and-release-scope.md) REQ-030 and section 6 |
| D35 | The exclusions of U1 hold: no `dotnet new` templates, no `remove` or `list` commands in 1.0. | U1 comments on #2518 and #2520 |

### D2 (D + E): a hybrid command host and an administration surface, both before 1.0

**D, the hybrid.** Commands that touch data run inside the application's real container. The application adds one opt-in line in `Program.cs` (the JasperFx command-host model), and the tool launches the startup project as a child process in command mode. A global `dotnet tool` covers scaffolding that does not need the application. Both share **one command contract**, and the global tool forwards in-app commands to the application.

**E, the administration surface.** The application can expose an authenticated, authorized remote administration surface, in the style of ServiceControl and ServicePulse, built on the same command contract, so operators can administer production without shell access. This is a real gap that `Encina.Cli` alone does not cover. It enlarges the attack surface, so it is designed with great care now, before 1.0, where the best solution can still be chosen without consequences. This ADR fixes only that the surface shares the command contract and the identity model of D14. Its shape (package, SPIKE, EPIC or milestone), threat model, authentication and authorization, and audit are investigated in [#2972](https://github.com/dlrivada/Encina/issues/2972).

D2 absorbs two items of the register: D3 (how the hosting line gets into `Program.cs`) and D5 (the process model). The line is one opt-in line and the process is a child process; how the line is placed in a project is left to the plan of the host child.

### D7 (a): modules contribute commands through DI

Each module contributes its commands through an interface registered in DI by its own `AddEncina*` method, for example `IEncinaCommand` registered with `AddEncinaCommand<T>()` (illustrative names). Only registered modules contribute. The command-line host and the administration surface (#2972) read the same catalogue from the container. The registration is proven by the DI test that builds the provider with `ValidateOnBuild` and `ValidateScopes` (AGENTS.md section 3, registration completeness). A source-generated registry may be added later on top without changing the contract.

### D14 (d): the operator logs in against the application's own identity provider

The operator authenticates interactively against the application's own identity provider before operating.

- The OAuth 2.0 Device Authorization Grant (RFC 8628) is preferred, so the password and the multi-factor step happen in the browser and never pass through the terminal. A password prompt (resource owner password) is a last resort, only when the application's provider offers nothing else.
- The session lasts as long as the token and asks to log in again when it expires.
- Unattended automation uses explicit client credentials with its own service identity, audited as such.
- The tenant is always an explicit `--tenant`, and the authenticated identity must have access to it.
- The operating-system user is recorded only as context in the audit, never as the identity.
- Encina does not become an identity provider (C1b): it uses the OIDC provider the application already configures. With none configured, the command is denied (AGENTS.md section 3).
- The same identity model serves the console, the administration surface (#2972) and the web.

This replaces the earlier recommendation (c), the operating-system user as actor.

### D21 (b): separate administration interfaces

Separate administration interfaces (for example `IOutboxAdministration` and `ISagaAdministration`, illustrative names) hold the operator operations: list, inspect, purge, replay. They are implemented on all 10 database providers (AGENTS.md section 5) and registered only when command hosting or the administration surface is enabled. The runtime store interfaces stay minimal, and purge and replay never reach business code.

### Left to the implementation plans

The plans of the EPIC children decide the following inside this architecture:

| Id | Item |
| --- | --- |
| D4 | Locating the startup project (the convention of `--project` and `--startup-project`; defect [#2519](https://github.com/dlrivada/Encina/issues/2519)) |
| D9 | Package name and shape |
| D11 | How dry-run is expressed in the command contract |
| D12 | The confirmation mechanism (prompt, `--yes`, typed token); that confirmation is needed is settled by D10 |
| D23 | How an error is rendered to the operator (code only, per AGENTS.md section 3) |
| D24 | Command naming and collisions |
| D25 | Output format and exit codes |
| D28 | Tests of the tool-to-application handshake |
| D36 | Whether a prototype is needed |

## Alternatives considered

Each option comes from the dilemma screen of the issue; the maintainer chose the ones above.

- **D2, option A alone** (the application exposes a command host): the model chosen for in-app commands, but alone it leaves scaffolding tied to a project that does not exist yet; D keeps scaffolding in the standalone tool.
- **D2, option B** (the tool builds the startup project and loads its service provider without code changes, as `dotnet ef` does): not chosen. The issue records its weaknesses: assembly loading and dependency resolution are fragile across .NET 10 projects, hosted services and configuration sources may still run, and it is weak for commands contributed by modules. The issue asserts this fragility; it was not measured (no prototype).
- **D2, option C** (two separate surfaces with no shared command model): not chosen. Two surfaces to learn and document, and modules would contribute commands twice. E keeps a remote surface, but on the shared contract.
- **D7, scanning the assemblies of registered modules** and **D7, a source-generated registry**: not chosen for 1.0. Scanning is reflection at startup and cannot be proven by the registration test as directly; a generated registry adds a generator and an analyzer package (ADR-005 rejects generators for handler dispatch only, so it is not excluded, and it can be added later without changing the contract). Unrestricted assembly scanning is excluded by D8.
- **D14, a tenant and principal supplied by option, by the application's configuration, or by a required identity option or configured accessor**: not chosen; (d) gives an authenticated identity instead of a supplied one.
- **D14, the operating-system user as actor plus an explicit `--tenant`** (the earlier recommendation, (c)): replaced by (d). The register notes it is the weakest evidence for a compliance audit.
- **D21, growing the existing store interfaces on all 10 providers**: not chosen. It widens every store interface and its 10 implementations and the in-memory store, for operations only operators use.
- **D21, limiting the 1.0 commands to what the existing abstractions support** (deferring the rest): not chosen; the stores have no list or purge, and only a requeue of exhausted outbox messages (`RequeueExhaustedAsync`), so operator commands would be left out.
- **D21, per-provider DDL and SQL held inside the Cli**: not chosen. It puts provider-specific code outside the provider packages and does not scale to 10 providers without duplication.

## Consequences

- The command architecture is decided before any of the six EPIC children is planned, so their plans can proceed.
- One contract serves the global tool, the in-app host and the administration surface; a module writes a command once.
- Commands see exactly what production sees: the real configuration, container, providers and tenant wiring.
- Pay-for-what-you-use holds: a command exists only when its module is registered, and the administration interfaces are registered only when hosting or the administration surface is enabled.
- The application owner adds one line in `Program.cs`, and the tool must locate, launch and relay to the startup project.
- The administration surface enlarges the attack surface and gets its own SPIKE before it is built.
- Every provider package that supports an administered pattern implements the administration interface on its provider, on top of its store.
- The final names, the confirmation mechanism and the output contract wait for the plans.

### Follow-up work

| Item | State |
| --- | --- |
| D33: a [FEATURE] child of #2614 for the command host, the command contract and the module registration. No existing child builds them; the six children presuppose them. | Issue to be opened (no number yet) |
| D34: milestone alignment of #2612 (v0.17.0) with #2608 and #2609 (v0.18.0), which need the same host | To do by the orchestrator now that D2 is decided |
| The remote administration surface (shape, threat model, authentication and authorization, audit) | SPIKE [#2972](https://github.com/dlrivada/Encina/issues/2972) |
| Stale text in the children: "1.0 fit decided by SPEC-002" in #2612; the metric placeholders (see D30) | To correct in the children |

## Cross-cutting functions

The 12 functions of [ADR-018](018-cross-cutting-integration-principle.md) and AGENTS.md section 6, for the architecture as a whole. Each EPIC child records its own evaluation in its plan.

| # | Function | Position |
| --- | --- | --- |
| 1 | Caching | Not applicable: the host and the contract hold no read path that benefits from caching. |
| 2 | OpenTelemetry | Integrate: one `ActivitySource` and `Meter` named `Encina.*` for the new package (D30, ADR-054). |
| 3 | Structured logging | Integrate: `[LoggerMessage]` with an EventId range registered first (D30, AGENTS.md section 7). |
| 4 | Health checks | Not applicable: the host has no runtime dependency to check; the `doctor` command belongs to #2608. |
| 5 | Validation | Defer: validation of command input is decided in the plan of the D33 child (issue to be opened). |
| 6 | Resilience | Defer: calls to the application's identity provider and the retry of remote calls are decided in the plan of the D33 child (issue to be opened). |
| 7 | Distributed locks | Defer: the plans of the operations children ([#2609](https://github.com/dlrivada/Encina/issues/2609), [#2610](https://github.com/dlrivada/Encina/issues/2610)) decide whether purge and replay need a lock against a running processor elsewhere. |
| 8 | Transactions | Defer: atomicity of purge and replay belongs to the administration interface implementations in the plans of #2609 and #2610. |
| 9 | Idempotency | Defer: replay semantics are decided in the plans of #2609 and #2610. |
| 10 | Multi-tenancy | Integrate: an explicit `--tenant`, fail closed when it is missing or the identity has no access to it (D13, D14). |
| 11 | Module isolation | Not applicable: the catalogue follows which modules are registered (D8), not `ModuleId` scoping of data. |
| 12 | Audit trail | Integrate: every mutating execution in `IOperationAuditStore`, written ahead and awaited; reads of personal data in `IReadAuditStore` (D15 to D19). |

## Related

- [Command contract for module authors](../../engineering/cli-command-contract-spike-2605.md): the explanation of how a module contributes commands under this ADR
- [ADR-005](005-reject-source-generators.md), [ADR-018](018-cross-cutting-integration-principle.md), [ADR-036](036-three-audit-stores.md), [ADR-050](050-modules-may-require-modules.md), [ADR-054](054-observability-conventions-and-trace-propagation.md)
- [SPEC-000](../../specifications/SPEC-000-encina-1.0-baseline-and-release-scope.md): REQ-030, section 6
- Issues: [#2605](https://github.com/dlrivada/Encina/issues/2605) (this SPIKE), [#2614](https://github.com/dlrivada/Encina/issues/2614) (EPIC), [#2972](https://github.com/dlrivada/Encina/issues/2972) (administration surface), [#47](https://github.com/dlrivada/Encina/issues/47), [#2517](https://github.com/dlrivada/Encina/issues/2517), [#2526](https://github.com/dlrivada/Encina/issues/2526), [#2539](https://github.com/dlrivada/Encina/issues/2539)
- The D-numbers are the labels of the decision register of #2605; the maintainer's decisions are in the comments of 2026-10-28 on [#2605](https://github.com/dlrivada/Encina/issues/2605)

## Date

2026-10-28
