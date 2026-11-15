# Implementation Plan: Health Checks for All Encina Components (umbrella)

> **Issue**: [#622](https://github.com/dlrivada/Encina/issues/622)
> **Type**: Feature (umbrella for the remaining gaps)
> **Complexity**: Medium (5 phases, 36 file changes, split into 3 pull requests of 12, 14 and 10 files)
> **Estimated Scope**: about 350 lines of production code, about 800 lines of tests
> **Milestone**: Encina 1.0 (observability, SPEC-000 DEC-009 widened scope)

---

## Summary

Most of the checks the issue originally listed exist (stores, 10 database providers, Kafka, RabbitMQ, AzureServiceBus, AmazonSQS, NATS, MQTT, gRPC, SignalR, Redis and QueryCache, the Redis and SqlServer lock checks, `SecretsHealthCheck`). This plan closes the gaps that are real today and gives every other gap an owner, so the umbrella can close.

Gaps re-checked against `main`:

| Gap | Finding | Owner |
|-----|---------|-------|
| Redis.PubSub transport | No check in `src/Encina.Redis.PubSub` | This plan, Phase 1 |
| InMemory transport | No check in `src/Encina.InMemory` | This plan, Phase 2 |
| GraphQL | No check; the package has no external dependency (Design Choice 3) | This plan, Phase 3 (conditional) |
| Memory and Hybrid caches | No check | #754 |
| InMemory lock | No check | #755 |
| Processor liveness | The backlog checks cannot see a dead processor | #2396 |
| Probe endpoints, handler and pipeline checks | Absent | #454 |
| RoutingSlip store | `IRoutingSlipStore` is an interface with no implementation; its provider stores are #590 | #590 ships the check with the store |
| Choreography state store | `IChoreographyStateStore` has no implementation and is documented but unbuilt (#1467) | Not buildable yet |
| Schema Registry, Feature Flags, Service Discovery | The packages do not exist | #309, #259, #2696; #451, #604; #607 ship their own check |
| Health telemetry | No per-check metric exists in `src/` | This plan, Phase 4 |

**Affected packages:**

- `Encina.Redis.PubSub`, `Encina.InMemory` (and `Encina.GraphQL` only if Design Choice 3 chooses a check): check, options property, registration, logging. These three reference only `Encina` today, so each would gain a `ProjectReference` to `Encina.Messaging` (home of `EncinaHealthCheck` and `ProviderHealthCheckOptions`), unless the maintainer prefers moving the base to core (Design Choice 12; `Encina.InMemory` describes itself as "zero-dependency" in its csproj).
- `Encina.OpenTelemetry` - the health-check metrics publisher (Phase 4).
- `Encina.AspNetCore` - no change; registration extensions stay where they are.

**Provider category**: Transports (AGENTS.md section 5) for Phases 1 to 3; observability for Phase 4. Of the 10 existing transports, 7 have a check today (RabbitMQ, AzureServiceBus, AmazonSQS, Kafka, NATS, MQTT, gRPC; `Encina.SignalR` also has one but is not in the category list); Redis.PubSub and InMemory get one here; GraphQL is Design Choice 3. The 6 planned transports (GoogleCloudPubSub, AmazonEventBridge, Pulsar, Redis.Streams, ActiveMQ, Dapr) ship their own check with their provider.

**Sibling plans (health-checks family)**: #754 owns every change to the shared `ProviderHealthCheckOptions` and the tag convention; the new checks set their own tags in their options (family answer to #754 Design Choice 3). #454 owns the probe endpoints and the tag constants for liveness, readiness and startup. #2396 owns `IProcessorHeartbeat`. This plan owns the health-check telemetry (Design Choice 4). Options wiring follows the same story as #754 and #755 (recommended, pending the maintainer): each check receives the `ProviderHealthCheckOptions` instance of its own registration, built by a factory, and the shared type is never registered as a service (Design Choice 8).

---

## Design Choices

<details>
<summary><strong>1. Where the checks live</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Each check stays in its own package** (current practice: dozens of checks across `src/`) | No new package; a package brings its check; no reference cycles | Discoverability depends on tags and documentation |
| **B) A new `Encina.HealthChecks` package** (the original text; #181 also asks for it) | One place and one entry point | It must reference every package or hold only the base, which already lives in `Encina.Messaging/Health`; moving 40 checks is a rewrite |

### Chosen Option: **A - each check stays in its own package** (recommended, pending the maintainer)

### Rationale

- The base (`IEncinaHealthCheck`, `EncinaHealthCheck`, `ProviderHealthCheckOptions`) and the host registration (`HealthCheckBuilderExtensions` in `Encina.AspNetCore`) already exist; a new package would add a third location.
- Option B wins when a single package must be the only dependency of an application that wants every check. This is the same package question as #454 Design Choice 1 and #181; answer them together.
- Option A needs no ADR; if the maintainer chooses Option B (a new package), its ADR number is assigned when the ADR pull request opens.
- Class: Dilemma

</details>

<details>
<summary><strong>2. Role of this issue now that most checks exist</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Ship the Phase 1 to 4 pull requests, then close it**; every other gap keeps its own issue (the table above) | No stale list; each gap has one owner | The gap table must be copied into the closing comment |
| **B) Keep it open as the gap list** | One place to see the gaps | It goes stale on the first merged sibling, and the checkboxes duplicate five other issues |

### Chosen Option: **A - close it after the pull requests of this plan** (recommended, pending the maintainer)

### Rationale

- Every gap in the table has an owner or a precondition; nothing is left unowned.
- Option B wins when gaps are many and unowned, which is no longer the case.
- Class: Dilemma

</details>

<details>
<summary><strong>3. Does GraphQL get a check</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A check that creates a scope and resolves `IGraphQLEncinaBridge`** | The package appears in the probe list like every other transport | There is no external dependency to check; DI validation tests already prove resolvability |
| **B) No check; document the reason in the package README and the guide** | Honest: AGENTS.md section 6 asks for a check where there is a checkable dependency, and `GraphQLEncinaBridge` has none (it forwards to `IEncina`) | The acceptance item "transport health checks (all transports)" needs a recorded exception |

### Chosen Option: **B - no check for GraphQL** (recommended, pending the maintainer)

### Rationale

- `src/Encina.GraphQL` registers only `IGraphQLEncinaBridge` (scoped) and the host's GraphQL server owns the endpoint; the readiness of the handlers behind it is #454's handler check.
- Option A wins when operators want every transport to appear in the probe output regardless of depth.
- Class: Dilemma

</details>

<details>
<summary><strong>4. Where health telemetry is emitted</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One `IHealthCheckPublisher` in `Encina.OpenTelemetry` emitting `encina.health.check_status` per check** (tag `check.name`; 0 unhealthy, 1 degraded, 2 healthy) | One mechanism for every check; reuses the shared `Encina` meter (`metrics.AddMeter("Encina")`); #454 derives per-probe views from the check tags | The publisher runs on the framework's publisher period, not on every probe request |
| **B) Each check or endpoint records its own metric** | Per-request freshness | One instrumentation per check, and the probe endpoint of #454 would duplicate it |
| **C) Rely on the framework's own health-check metrics** | Nothing to build | Not verified for .NET 10; to be checked at the start of Phase 4, and it replaces Option A if the framework already emits a per-check status |

### Chosen Option: **A - one publisher and one gauge** (recommended, pending the maintainer)

### Rationale

- Today `src/` has no `encina.health.*` metric; `OutboxProcessorMetrics` (`src/Encina.Messaging/Diagnostics`) shows the precedent of an observable gauge on the shared `Encina` meter.
- The issue text of #454 proposes `encina.health.probe_status` and a probe span; this plan makes the per-check gauge the one metric, and #454 records the difference.
- Class: Dilemma

</details>

<details>
<summary><strong>5. Shape of the InMemory transport check</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Healthy normally; Degraded when a bounded channel is nearly full** (a ratio option on `EncinaInMemoryOptions`, default 0.9 of `BoundedCapacity`) | Same warning/critical idea as `OutboxHealthCheck` thresholds; uses `IInMemoryMessageBus.PendingCount` | One new option |
| **B) Always Healthy when the bus resolves** | No option | Proves nothing; a full `Wait` channel blocks publishers silently |

### Chosen Option: **A - Degraded near capacity** (recommended, pending the maintainer)

### Rationale

- With `InMemoryFullMode.Wait` a full channel stalls every publisher, which is the failure an operator needs to see; an unbounded channel (`UseUnboundedChannel`) is always Healthy.
- The default ratio 0.9 and the choice of a ratio instead of absolute counts are unregistered values: `OutboxHealthCheck` classifies by counts, so it does not justify either; the maintainer picks the default (0.9 is the recommendation) and the type.
- Option B also satisfies AGENTS.md section 6 (a bus that resolves is a "check"), so the rule does not decide.
- `DegradedFillRatio` is validated between 0 and 1 by a new `EncinaInMemoryOptionsValidator` (`IValidateOptions`, mirroring `src/Encina.NATS/EncinaNATSOptionsValidator.cs`) applied eagerly in `AddEncinaInMemory`; `EncinaInMemoryOptions` has no validation hook today.
- Class: Dilemma

</details>

<details>
<summary><strong>6. Checks for stores and external services that do not exist</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Each store or service ships its check with its own issue** (#590 for routing slip stores; #309, #259, #2696 for schema registry; #451 and #604 for feature flags; #607 for service discovery) | The check is written against code that exists and can be integration tested | The umbrella cannot tick those items |
| **B) Write checks now against the interfaces** | Items ticked | A check against `IRoutingSlipStore` or `IChoreographyStateStore` has no implementation to run against |

### Chosen Option: **A - the check ships with the store or service** (recommended, pending the maintainer)

### Rationale

- `IRoutingSlipStore` and `IChoreographyStateStore` have no implementer in `src/`; the Choreography feature is documented but unbuilt (#1467), so a check written now has nothing to run against.
- Option B also satisfies AGENTS.md section 6 (it asks that a check be evaluated, not when it is written), so the rule does not decide.
- Class: Dilemma

</details>

<details>
<summary><strong>7. Check implementation for Redis.PubSub</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A check in `Encina.Redis.PubSub` resolving `IConnectionMultiplexer`, like `NATSHealthCheck`** | Each transport owns its check; no cross-package dependency | The ping loop resembles `RedisHealthCheck` |
| **B) Reuse `Encina.Caching.Redis.RedisHealthCheck`** | No duplicate logic | A transport package would reference the cache package |

### Chosen Option: **A - an own check in the transport package**

### Rationale

- `Encina.Redis.PubSub` references only `Encina` and registers its own `IConnectionMultiplexer`; every other transport (NATS, Kafka, MQTT) owns its check the same way (convention of `src/Encina.NATS/Health/NATSHealthCheck.cs`).
- Option B (reuse `RedisHealthCheck`) is not excluded by AGENTS.md section 5 either; the NATS convention is the only reason, which is why this is Trivial with a convention citation and not a settled rule.
- Class: Trivial

</details>

<details>
<summary><strong>8. Registration and options wiring (per-registration options)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Register `IEncinaHealthCheck` with a factory that captures the `ProviderHealthCheckOptions` of this registration** (`sp => new RedisPubSubHealthCheck(sp, options.ProviderHealthCheck)`); the shared type is NOT registered as a service | Each check keeps its own `Name`, `Tags`, `Timeout` and `Enabled`; a DI test with two transports proves it | Differs from NATS today |
| **B) Copy NATS: `services.AddSingleton(options.ProviderHealthCheck)` plus `AddSingleton<IEncinaHealthCheck, ...>`** (`src/Encina.NATS/ServiceCollectionExtensions.cs` lines 60-65) | Identical to the existing transports | Cross-wires options: with two transports (or the Redis cache check) registered, the constructor `(IServiceProvider, ProviderHealthCheckOptions?)` resolves the last registered instance, so the name and tags of one check apply to another |
| **C) Keyed or named options per check** | Container-visible | More surface; two patterns in the family |

### Chosen Option: **A** (recommended, pending the maintainer)

### Rationale

- Same story as #754 and #755. Moving NATS and the other existing transports is out of scope: follow-up issue to be opened (draft: artifacts/decisions/plan-batch/followups/transport-healthcheck-options-wiring.md).
- Proven by a DI test (`ValidateOnBuild`, `ValidateScopes`) that registers Redis.PubSub with a second check built from another options instance and asserts each reports its own name and tags (PR 1), and the same with InMemory (PR 2).
- Class: Dilemma

</details>

<details>
<summary><strong>9. Degraded threshold and Timeout use of the ping check</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Degraded when a ping exceeds `Timeout / 2`** (the rule of `src/Encina.Caching.Redis/Health/RedisHealthCheck.cs` lines 60-63; the timeout source comes from `TimeProvider.CreateCancellationTokenSource(options.Timeout)`) | Same rule as the Redis cache check; the threshold needs no option | An unregistered ratio, copied from one check |
| **B) A `DegradedPingRatio` option** | Tunable | One more option per transport |

### Chosen Option: **A** (recommended, pending the maintainer)

### Rationale

- The constant is a user-visible default (it decides Degraded versus Healthy), so it is a Dilemma even though one precedent exists.
- Class: Dilemma

</details>

<details>
<summary><strong>10. Names, tags, readiness impact and status mapping</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Names `encina-redis-pubsub` and `encina-inmemory-transport`; default tags include `ready`; Unhealthy and Degraded both come from the checks, and the host decides what Degraded means for readiness (#454)** | A failing transport removes the instance from readiness, like the other transport checks; names follow `encina-<provider>` | A Degraded InMemory bus (near capacity) can affect readiness depending on the #454 endpoint mapping; `FailureStatus` of the options is ignored |
| **B) Default tags without `ready` (liveness and diagnostics only)** | No readiness side effect | Differs from every other transport check; a dead transport stays "ready" |

### Chosen Option: **A** (recommended, pending the maintainer)

### Rationale

- Whether Degraded counts as failing for readiness is an endpoint decision owned by #454; this plan only chooses the tags. Both names and the readiness consequence are public and user-visible.
- Class: Dilemma

</details>

<details>
<summary><strong>11. Health gauge encoding and publisher opt-in</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Gauge value 0 unhealthy, 1 degraded, 2 healthy; `AddEncinaHealthCheckMetrics()` opt-in, off by default** | Opt-in per AGENTS.md section 1; one value per check; simple alert rules (`< 2`) | Higher means healthier, the reverse of "severity" encodings |
| **B) Value 0 healthy, 1 degraded, 2 unhealthy** | Severity-ordered | Differs from Option A's "higher is better" |

### Chosen Option: **A** (recommended, pending the maintainer)

### Rationale

- The encoding is a public metric contract and the default (off) follows the opt-in principle; both are recorded here because they were previously inside Design Choice 4.
- Class: Dilemma

</details>

<details>
<summary><strong>12. How the transports reach the health-check base</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ProjectReference` from `Encina.Redis.PubSub`, `Encina.InMemory` (and `Encina.GraphQL` if chosen) to `Encina.Messaging`** | Smallest change; `Encina.Messaging` references only `Encina` (no cycle, verified) | `Encina.InMemory` describes itself as "zero-dependency" and gains a dependency only for a base class, against pay-for-what-you-use |
| **B) Move `IEncinaHealthCheck`, `EncinaHealthCheck` and `ProviderHealthCheckOptions` to core `Encina`** | No new dependency for any transport | A move of widely used public types across assemblies (all `using` consumers rebuild); a wider change than this issue |

### Chosen Option: **A** (recommended, pending the maintainer)

### Rationale

- Option B wins when the dependency footprint of `Encina.InMemory` matters more than the size of the change; it is a packaging decision for the maintainer, not an implementation detail.
- Class: Dilemma

</details>

---

## Implementation Phases

### Phase 1: Redis.PubSub transport check (pull request 1)

<details>
<summary><strong>Tasks</strong></summary>

PR 1 holds exactly these 12 files (neither `Encina.Redis.PubSub` nor `Encina.InMemory` has a `PublicAPI.*.txt`; both csproj files lack the PublicApi analyzer, so no PublicAPI line is needed; the EventIds 4406-4407 and their log methods ship here, in the first PR that logs):

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Redis.PubSub/Encina.Redis.PubSub.csproj` | modify (`ProjectReference` to `Encina.Messaging`, Design Choice 12) |
| 2 | `src/Encina.Redis.PubSub/Health/RedisPubSubHealthCheck.cs` | new |
| 3 | `src/Encina.Redis.PubSub/EncinaRedisPubSubOptions.cs` | modify |
| 4 | `src/Encina.Redis.PubSub/ServiceCollectionExtensions.cs` | modify |
| 5 | `src/Encina.Redis.PubSub/Log.cs` | modify (4406-4407) |
| 6 | `src/Encina.Redis.PubSub/README.md` | new (none exists today; `docs-writer`) |
| 7 | `tests/Encina.UnitTests/Messaging/RedisPubSub/RedisPubSubHealthCheckTests.cs` | new (check, registration and multi-transport DI tests) |
| 8 | `tests/Encina.GuardTests/Messaging/RedisPubSub/RedisPubSubHealthCheckGuardTests.cs` | new |
| 9 | `tests/Encina.ContractTests/Messaging/TransportHealthCheckContractTests.cs` | new (Redis.PubSub; PR 2 extends it with InMemory) |
| 10 | `tests/Encina.IntegrationTests/Messaging/RedisPubSub/RedisPubSubHealthCheckIntegrationTests.cs` | new (shared Redis collection fixture) |
| 11 | `.github/coverage-manifest/Encina.Redis.PubSub.json` | modify (`mechanical-fixer`) |
| 12 | `changelog.d/622-redis-pubsub-health-check.added.md` | new (`mechanical-fixer`) |

Tasks:

1. **Modify** the csproj (file 1).
2. **Create** `RedisPubSubHealthCheck`: `public sealed class RedisPubSubHealthCheck : EncinaHealthCheck`, `DefaultName = "encina-redis-pubsub"` (Design Choice 10), constructor `(IServiceProvider serviceProvider, ProviderHealthCheckOptions? options, TimeProvider? timeProvider = null, ILogger<RedisPubSubHealthCheck>? logger = null)`; resolves `IConnectionMultiplexer`, Unhealthy when `!IsConnected`, pings every endpoint with a token linked to `timeProvider.CreateCancellationTokenSource(options.Timeout)` (a system timer would not follow a `FakeTimeProvider`), Degraded when a ping exceeds `Timeout / 2` (Design Choice 9); result text carries the exception type only; the timeout path logs 4407 and a non-cancellation exception is logged by 4406 through `ForLogging()` and rethrown so `EncinaHealthCheck` returns the type name.
3. **Modify** `EncinaRedisPubSubOptions.cs`: `public ProviderHealthCheckOptions ProviderHealthCheck { get; } = new() { Tags = ["encina", "messaging", "redis-pubsub", "ready"] };`.
4. **Modify** `ServiceCollectionExtensions.cs`: when `options.ProviderHealthCheck.Enabled`, register `IEncinaHealthCheck` with a factory capturing `options.ProviderHealthCheck` (Design Choice 8); never `AddSingleton(options.ProviderHealthCheck)`. The method copies each option property into `services.Configure`; the check uses the local `options` instance, so the copy list needs no new line.
5. **Tests and manifest in this PR**: healthy, disconnected, slow ping, exception, timeout on a `FakeTimeProvider`, no message leakage; the DI tests; guard tests for the constructor; the contract test (name, tags including `encina`, no throw); the integration test against the shared Redis collection fixture; manifest targets per touched file with one-sentence justifications: `Health/RedisPubSubHealthCheck.cs` (unit 90, guard 15, contract 40, integration 60), `EncinaRedisPubSubOptions.cs` and `ServiceCollectionExtensions.cs` (unit 85 each; guard keeps the existing target), `Log.cs` (unit 70 for the two new bodies; guard 0 and contract 0 because generated logger bodies run only when the check logs). The measured value replaces each estimate before the PR.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
Encina is a .NET 10 / C# 14 library. Issue #622 closes the transport health-check gaps. Encina.Redis.PubSub references
only the Encina project and registers its own IConnectionMultiplexer in AddEncinaRedisPubSub.

TASK:
Add the ProjectReference to Encina.Messaging, create RedisPubSubHealthCheck, add EncinaRedisPubSubOptions.ProviderHealthCheck
with transport tags, register the check when enabled, add PublicAPI lines, build with zero warnings.

KEY RULES:
- Derive from EncinaHealthCheck; failure text is the exception type name only; honor ProviderHealthCheckOptions.Timeout.
- Do not change ProviderHealthCheckOptions (owned by #754).
- Registration is gated by ProviderHealthCheck.Enabled, uses a factory that captures this registration's options (never AddSingleton(ProviderHealthCheckOptions)); prove it with a ValidateOnBuild/ValidateScopes DI test with two checks.
- The timeout source is TimeProvider.CreateCancellationTokenSource(options.Timeout).
- XML docs with an example; sealed; nullable enabled.

REFERENCE FILES:
- src/Encina.NATS/Health/NATSHealthCheck.cs
- src/Encina.NATS/ServiceCollectionExtensions.cs (lines 60-65; the pattern NOT to copy)
- src/Encina.Caching.Redis/Health/RedisHealthCheck.cs
- src/Encina.Redis.PubSub/ServiceCollectionExtensions.cs
- src/Encina.Redis.PubSub/EncinaRedisPubSubOptions.cs
```

</details>

### Phase 2: InMemory transport check (pull request 2)

<details>
<summary><strong>Tasks</strong></summary>

PR 2 holds exactly these 14 files (PR 1 is merged first; the EventIds 4460-4461 ship here):

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.InMemory/Encina.InMemory.csproj` | modify (`ProjectReference` to `Encina.Messaging`, Design Choice 12) |
| 2 | `src/Encina.InMemory/Health/InMemoryTransportHealthCheck.cs` | new |
| 3 | `src/Encina.InMemory/EncinaInMemoryOptions.cs` | modify |
| 4 | `src/Encina.InMemory/EncinaInMemoryOptionsValidator.cs` | new |
| 5 | `src/Encina.InMemory/ServiceCollectionExtensions.cs` | modify |
| 6 | `src/Encina.InMemory/Log.cs` | modify (4460-4461) |
| 7 | `src/Encina.InMemory/README.md` | new (`docs-writer`) |
| 8 | `tests/Encina.UnitTests/Messaging/InMemory/InMemoryTransportHealthCheckTests.cs` | new (check, validator and DI tests) |
| 9 | `tests/Encina.GuardTests/Messaging/InMemory/InMemoryTransportHealthCheckGuardTests.cs` | new |
| 10 | `tests/Encina.GuardTests/Encina.GuardTests.csproj` | modify (no `Encina.InMemory` reference today) |
| 11 | `tests/Encina.ContractTests/Messaging/TransportHealthCheckContractTests.cs` | modify (adds InMemory) |
| 12 | `tests/Encina.ContractTests/Encina.ContractTests.csproj` | modify (no `Encina.InMemory` reference today) |
| 13 | `.github/coverage-manifest/Encina.InMemory.json` | modify (`mechanical-fixer`) |
| 14 | `changelog.d/622-inmemory-transport-health-check.added.md` | new (`mechanical-fixer`) |

Tasks:

1. **Modify** the csproj (file 1).
2. **Create** `InMemoryTransportHealthCheck` (`DefaultName = "encina-inmemory-transport"`, constructor `(IServiceProvider serviceProvider, ProviderHealthCheckOptions? options, ILogger<InMemoryTransportHealthCheck>? logger = null)`; no time is needed, so no `TimeProvider`): resolves `IInMemoryMessageBus`; Healthy when unbounded or `PendingCount` below the ratio, Degraded at or above `DegradedFillRatio * BoundedCapacity` (Design Choice 5); data entries `pending_messages`, `subscribers`; a non-cancellation exception is logged by 4460 through `ForLogging()` and rethrown, and a Degraded result logs 4461.
3. **Modify** `EncinaInMemoryOptions.cs`: `ProviderHealthCheck` (tags `encina`, `messaging`, `inmemory`, `ready`) and `DegradedFillRatio` (default 0.9, Design Choice 5); **create** `EncinaInMemoryOptionsValidator` rejecting a ratio outside the 0 to 1 interval, applied eagerly in `AddEncinaInMemory`.
4. **Modify** `ServiceCollectionExtensions.cs` (`AddEncinaInMemory`): register through the factory of Design Choice 8 when enabled.
5. **Tests and manifest in this PR**: below, at and above the ratio, unbounded; validator; guard tests; the contract extension; DI tests including a second check with different options; manifest targets per touched file with one-sentence justifications (`Health/InMemoryTransportHealthCheck.cs` unit 90, guard 15, contract 40; `EncinaInMemoryOptionsValidator.cs` unit 95, guard 30 for the null-options guard of `Validate`; `Log.cs` unit 70, guard 0, contract 0; the two touched files keep their targets and add unit 85).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
Phase 1 is merged. Encina.InMemory exposes IInMemoryMessageBus with PendingCount and SubscriberCount, and
EncinaInMemoryOptions.BoundedCapacity / UseUnboundedChannel / FullMode.

TASK:
Create InMemoryTransportHealthCheck, the two options properties, the registration and the ProjectReference.

KEY RULES:
- No exception or error message in results; type name only.
- Unbounded channel is always Healthy; bounded channel is Degraded at the ratio.
- Time is not needed; if added, use TimeProvider.
- CRAP of every added method 10 or less.

REFERENCE FILES:
- src/Encina.InMemory/InMemoryMessageBus.cs
- src/Encina.InMemory/EncinaInMemoryOptions.cs
- src/Encina.Messaging/Health/OutboxHealthCheck.cs (Classify)
- src/Encina.NATS/Health/NATSHealthCheck.cs
```

</details>

### Phase 3: GraphQL decision (conditional)

<details>
<summary><strong>Tasks</strong></summary>

1. If Design Choice 3 is Option B: no code; `docs-writer` records the exception in the `Encina.GraphQL` README and the transport table of `docs/guides/health-checks.md`.
2. If Option A: a fourth pull request, with its own EventIds taken from the GraphQL package range (checked when it is briefed), the same factory registration and tests as PR 1: `ProjectReference` to `Encina.Messaging`, `src/Encina.GraphQL/Health/GraphQLHealthCheck.cs` creating a scope and resolving `IGraphQLEncinaBridge`, `EncinaGraphQLOptions.ProviderHealthCheck`, registration in `AddEncinaGraphQL`, log class, tests, manifest, changelog.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
Encina.GraphQL registers IGraphQLEncinaBridge (scoped) and has no external dependency.

TASK:
Apply the maintainer's answer to Design Choice 3: either document the exception, or implement GraphQLHealthCheck.

KEY RULES:
- Scoped services are resolved through IServiceProvider.CreateScope(), never from the root provider.
- Results carry the exception type name only.

REFERENCE FILES:
- src/Encina.GraphQL/ServiceCollectionExtensions.cs
- src/Encina.GraphQL/IGraphQLMediatorBridge.cs
- src/Encina.Messaging/Health/EncinaHealthCheck.cs
```

</details>

### Phase 4: Observability and structured logging (pull request 3)

<details>
<summary><strong>Tasks</strong></summary>

PR 3 holds exactly these 10 files (the Redis.PubSub and InMemory EventIds 4406-4407 and 4460-4461 already shipped with PR 1 and PR 2; this PR adds only 7010):

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.OpenTelemetry/Health/HealthCheckMetricsPublisher.cs` | new |
| 2 | `src/Encina.OpenTelemetry/Health/HealthCheckMetricsLog.cs` | new (7010) |
| 3 | `src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs` | modify |
| 4 | `src/Encina.OpenTelemetry/PublicAPI/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 5 | `src/Encina.OpenTelemetry/README.md` | modify (`docs-writer`) |
| 6 | `tests/Encina.UnitTests/OpenTelemetry/Health/HealthCheckMetricsPublisherTests.cs` | new |
| 7 | `tests/Encina.GuardTests/OpenTelemetry/HealthCheckMetricsPublisherGuardTests.cs` | new |
| 8 | `.github/coverage-manifest/Encina.OpenTelemetry.json` | modify (`mechanical-fixer`) |
| 9 | `docs/guides/health-checks.md` | modify (transport table, telemetry section; `docs-writer`) |
| 10 | `changelog.d/622-health-check-metrics.added.md` | new (`mechanical-fixer`) |

Tasks:

1. Verify whether .NET 10 emits a per-check status metric itself (Design Choice 4, Option C); if it does, stop and report.
2. **Create** `HealthCheckMetricsPublisher : IHealthCheckPublisher` (internal sealed): an observable gauge `encina.health.check_status` on the shared `Encina` meter, tag `check.name`, value 0, 1 or 2 (Design Choice 11), updated from each `HealthReport`; the publisher never throws.
3. **Modify** `ServiceCollectionExtensions.cs`: `AddEncinaHealthCheckMetrics()` registering the publisher (opt-in, off by default).
4. **Create** the log class with EventId 7010 (range 7000-7099, 7000-7009 used) for a publisher failure, logged through `ForLogging()`. No new range.
5. **Tests and manifest in this PR**: publisher with a fake `HealthReport` (all three statuses, a failing report path), guard tests for the registration extension and publisher; manifest targets per touched file with one-sentence justifications (`HealthCheckMetricsPublisher.cs` unit 90, guard 15; `HealthCheckMetricsLog.cs` unit 70, guard 0; `ServiceCollectionExtensions.cs` keeps its targets).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
Encina.OpenTelemetry already calls metrics.AddMeter("Encina"); OutboxProcessorMetrics (Encina.Messaging) shows an
observable gauge on that meter.

TASK:
Implement the publisher and its registration, and add the log message with EventId 7010 in its own log class.

KEY RULES:
- The publisher never throws (a publisher exception would escape into the host's timer): catch, log by exception type, continue.
- Use the source generator for logging; EventIds packed sequentially inside the existing ranges.
- Check names are tags, never user data; no error message in tags.

REFERENCE FILES:
- src/Encina.Messaging/Diagnostics/OutboxProcessorMetrics.cs
- src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs
- src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

### Phase 5: Testing, documentation and closure

<details>
<summary><strong>Tasks</strong></summary>

The tests, manifests, PublicAPI lines, changelog fragments and documents belong to the pull request of each file table above; this phase states what they contain.

1. Unit tests per check: Redis.PubSub with a mocked multiplexer (healthy, disconnected, slow ping, exception, timeout); InMemory with the real bus (below, at and above the ratio, unbounded); publisher with a fake `HealthReport`. Tests also prove no message leakage.
2. Guard tests for the new constructors and extension methods; contract tests that the new checks honor `IEncinaHealthCheck` (name, tags, never throws), created in PR 1 and extended in PR 2; no contract-test `.md` justification is written.
3. Integration: Redis.PubSub against a Redis Testcontainers fixture (`[Collection]` shared fixture, `[Trait("Category", "Integration")]`) in PR 1; InMemory and the publisher are in-process and justified in their manifests `reason`.
4. Property, load, benchmark: not required (deterministic, low frequency).
5. `mechanical-fixer`: the manifests, the one PublicAPI file (OpenTelemetry) and the three changelog fragments named in the file tables.
6. `docs-writer`: the READMEs of Redis.PubSub and InMemory (new, PR 1 and PR 2), the OpenTelemetry README and `docs/guides/health-checks.md` (PR 3).
7. Closure: comment on #622 with the gap table and the owner of each row, then close it when the maintainer answers Design Choice 2 with Option A.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT:
Checks, options, registrations and the publisher exist. Tests, manifests and documentation remain.

TASK:
Write the tests above, measure per-flag coverage of every touched file with the coverage-report script, record targets
with one-sentence justifications, and complete the documentation through the specialists.

KEY RULES:
- Tests execute real code; Shouldly through Encina.Testing.Shouldly; FakeTimeProvider; AAA.
- Integration tests use the shared Redis collection fixture, never a per-class container.
- Documentation never types coverage figures; changelog under changelog.d/ only.

REFERENCE FILES:
- tests/Encina.UnitTests/NATS/Health/NATSHealthCheckTests.cs
- .github/coverage-manifest/Encina.Redis.PubSub.json
- docs/guides/health-checks.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Relevance |
|----------|-----------|
| AGENTS.md section 5 (transports) | Providers of a category are consistent; 10 existing and 6 planned transports |
| AGENTS.md section 6, function 4 | A checkable dependency exposes a health check; a transport without one records why |
| AGENTS.md section 3 | No error or exception message in results; registration completeness |
| ADR-018 | Cross-cutting integration function evaluation |
| Kubernetes probes | Tags select checks per probe (#454) |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in This Feature |
|-----------|----------|----------------------|
| `EncinaHealthCheck`, `ProviderHealthCheckOptions`, `HealthCheckResult` | `src/Encina.Messaging/Health/` | Base type, options, result |
| `NATSHealthCheck` and its registration | `src/Encina.NATS/Health/`, `ServiceCollectionExtensions.cs` lines 60-65 | Transport reference for the check shape; its type-keyed options registration is not copied (Design Choice 8) |
| `RedisHealthCheck` | `src/Encina.Caching.Redis/Health/RedisHealthCheck.cs` | Ping loop, Degraded on slow ping |
| `OutboxHealthCheck` and options | `src/Encina.Messaging/Health/OutboxHealthCheck.cs` | Warning and critical classification |
| `IInMemoryMessageBus` | `src/Encina.InMemory/` | `PendingCount`, `SubscriberCount` |
| `OutboxProcessorMetrics` | `src/Encina.Messaging/Diagnostics/OutboxProcessorMetrics.cs` | Observable gauge on the shared `Encina` meter |
| `AddEncinaHealthChecks` and builder extensions | `src/Encina.AspNetCore/Health/HealthCheckBuilderExtensions.cs` | Host registration (unchanged) |
| `IRoutingSlipStore`, `IChoreographyStateStore` | `src/Encina.Messaging/RoutingSlip`, `Choreography` | Interfaces without implementation (Design Choice 6) |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Redis.PubSub` | 4400-4449 (existing `RedisPubSub`) | 4400-4405 used; this plan takes 4406-4407 |
| `Encina.InMemory` | 4450-4499 (existing `InMemory`) | 4450-4459 used; this plan takes 4460-4461 |
| `Encina.OpenTelemetry` | 7000-7099 (existing) | 7000-7009 used; this plan takes 7010 |

No new range is registered, so the 7350-7399 block assigned to this family is not used by this plan (#2396 takes 7350-7369).

### Estimated File Count

| Category | New Files | Modified Files | Notes |
|----------|-----------|----------------|-------|
| Production code | 5 | 9 | New: Redis.PubSub check, InMemory check, validator, publisher, OpenTelemetry log class; modified: two csproj files, two options files, two `ServiceCollectionExtensions.cs`, two `Log.cs`, OpenTelemetry `ServiceCollectionExtensions.cs` |
| Tests | 8 | 3 | New: unit (3), guard (3), contract (1), Redis integration (1); modified: the contract file, `Encina.ContractTests.csproj`, `Encina.GuardTests.csproj` (PR 2) |
| Documentation | 2 | 2 | New READMEs of Redis.PubSub and InMemory; OpenTelemetry README, `docs/guides/health-checks.md` |
| Configuration | 3 | 4 | Three changelog fragments; three manifests, the OpenTelemetry `PublicAPI.Unshipped.txt` (the other two packages have no PublicAPI files) |
| **Total** | **18** | **18** | 36 file changes in 3 pull requests: PR 1 = 12, PR 2 = 14, PR 3 = 10; each at most 15 files |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Complete Implementation Prompt - All Phases</strong></summary>

```
PROJECT CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0, no backward compatibility. Issue #622 is the umbrella of the health-check
gaps. Most checks exist; this plan adds the Redis.PubSub and InMemory transport checks, decides GraphQL, and adds a
per-check health metric. Memory/Hybrid caches are #754, the InMemory lock is #755, probes are #454, liveness is #2396.

IMPLEMENTATION OVERVIEW:
1. RedisPubSubHealthCheck + options + registration + ProjectReference to Encina.Messaging.
2. InMemoryTransportHealthCheck + ratio option + registration + ProjectReference.
3. GraphQL: document the exception or implement a scoped bridge check, per the maintainer's answer.
4. HealthCheckMetricsPublisher (IHealthCheckPublisher) emitting encina.health.check_status on the shared Encina meter.
5. EventIds inside existing ranges only (4406-4407, 4460-4461, 7010).
6. Unit, guard, contract, Redis integration tests; manifests, guide, READMEs, changelog through the specialists.
Three pull requests: Redis.PubSub, InMemory, telemetry.

KEY PATTERNS:
- Derive from EncinaHealthCheck; results carry the exception type only; timeout from ProviderHealthCheckOptions.Timeout.
- Options property `{ get; } = new() { Tags = [...] }`; registration gated by Enabled at registration time, with a factory capturing the registration's options (never AddSingleton(ProviderHealthCheckOptions)).
- The timeout source is TimeProvider.CreateCancellationTokenSource(options.Timeout).
- A publisher or timer callback never throws.
- Do not modify ProviderHealthCheckOptions (owned by #754).

REFERENCE FILES:
- src/Encina.NATS/Health/NATSHealthCheck.cs
- src/Encina.Caching.Redis/Health/RedisHealthCheck.cs
- src/Encina.Messaging/Health/EncinaHealthCheck.cs
- src/Encina.Messaging/Diagnostics/OutboxProcessorMetrics.cs
- src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs
- .github/coverage-manifest/Encina.Redis.PubSub.json
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | Health checks must reflect real-time status; caching would mislead |
| 2 | OpenTelemetry | ✅ Include | Phase 4: `encina.health.check_status` gauge through one publisher (Design Choice 4) |
| 3 | Structured Logging | ✅ Include | EventIds 4406-4407 (PR 1), 4460-4461 (PR 2), 7010 (PR 3) inside existing ranges |
| 4 | Health Checks | ✅ Include | This is the feature |
| 5 | Validation | ✅ Include | `DegradedFillRatio` is validated between 0 and 1 by the new `EncinaInMemoryOptionsValidator`, applied eagerly in `AddEncinaInMemory` (Design Choice 5) |
| 6 | Resilience | ✅ Include | Probes are bounded by `ProviderHealthCheckOptions.Timeout`; endpoint-level policy is #454 |
| 7 | Distributed Locks | ❌ N/A | Each instance checks itself |
| 8 | Transactions | ❌ N/A | Read-only probes |
| 9 | Idempotency | ❌ N/A | Probes are repeatable |
| 10 | Multi-Tenancy | ⏭️ Defer | Per-tenant database or cache health needs a tenant-aware check design; no issue exists: follow-up issue to be opened (draft: artifacts/decisions/plan-batch/followups/per-tenant-health-checks.md; shared with #454) |
| 11 | Module Isolation | ⏭️ Defer | `IModuleHealthCheck` and `AddEncinaModuleHealthChecks` already exist; nothing new for transports |
| 12 | Audit Trail | ❌ N/A | Infrastructure monitoring, no compliance event |

**Deferred items:**

- Multi-tenancy: per-tenant health is not requested by any open issue; follow-up issue to be opened from the draft artifacts/decisions/plan-batch/followups/per-tenant-health-checks.md (also referenced by #454).
- Module isolation: covered by the existing module health checks.

---

## Prerequisites & Dependencies

| Prerequisite | Status | Notes |
|--------------|--------|-------|
| Base types in `Encina.Messaging/Health` | ✅ Available | |
| Existing EventId ranges 4400-4449, 4450-4499, 7000-7099 | ✅ Available | Free ids after the used ones |
| .NET 10 built-in health-check metrics | Unverified | Checked at the start of Phase 4 (Design Choice 4, Option C) |
| #181 (a duplicate of the package question) | Open | Answer together with Design Choice 1 |
| #2138 (SchedulingHealthCheck false readiness failure) | Open | Independent bug; relevant to #454 readiness wiring |
| Maintainer answers to the Dilemmas (Design Choices 1 to 6 and 8 to 12) | Pending | Asked when work starts |

No blocking prerequisites.

---

## Next Steps

1. The orchestrator asks the maintainer the Dilemmas (Design Choices 1 to 6 and 8 to 12) when work starts, records them under `## Maintainer Decisions`, then briefs three workers (Redis.PubSub, InMemory, telemetry).
2. Comment on #622 with the gap table; close it after the last pull request if Design Choice 2 is Option A.
3. Follow-up issues to be opened from the drafts in artifacts/decisions/plan-batch/followups/: `per-tenant-health-checks.md` (deferred multi-tenancy) and `transport-healthcheck-options-wiring.md` (move NATS and the other existing transports to per-registration options).
