# Implementation Plan: Health Checks for All Encina Components (umbrella)

> **Issue**: [#622](https://github.com/dlrivada/Encina/issues/622)
> **Type**: Feature (umbrella for the remaining gaps)
> **Complexity**: Medium (5 phases, about 30 files, split into 3 pull requests)
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

- `Encina.Redis.PubSub`, `Encina.InMemory` (and `Encina.GraphQL` only if Design Choice 3 chooses a check): check, options property, registration, logging. These three reference only `Encina` today, so each gains a `ProjectReference` to `Encina.Messaging` (home of `EncinaHealthCheck` and `ProviderHealthCheckOptions`).
- `Encina.OpenTelemetry` - the health-check metrics publisher (Phase 4).
- `Encina.AspNetCore` - no change; registration extensions stay where they are.

**Provider category**: Transports (AGENTS.md section 5) for Phases 1 to 3; observability for Phase 4. Of the 10 existing transports, 7 have a check today (RabbitMQ, AzureServiceBus, AmazonSQS, Kafka, NATS, MQTT, gRPC; `Encina.SignalR` also has one but is not in the category list); Redis.PubSub and InMemory get one here; GraphQL is Design Choice 3. The 6 planned transports (GoogleCloudPubSub, AmazonEventBridge, Pulsar, Redis.Streams, ActiveMQ, Dapr) ship their own check with their provider.

**Sibling plans (health-checks family)**: #754 owns every change to the shared `ProviderHealthCheckOptions` and the tag convention; the new checks set their own tags in their options (family answer to #754 Design Choice 3). #454 owns the probe endpoints and the tag constants for liveness, readiness and startup. #2396 owns `IProcessorHeartbeat`. This plan owns the health-check telemetry (Design Choice 4).

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

### Chosen Option: **A - Degraded near capacity**

### Rationale

- With `InMemoryFullMode.Wait` a full channel stalls every publisher, which is the failure an operator needs to see; an unbounded channel (`UseUnboundedChannel`) is always Healthy.
- Class: Trivial
- Settled by: AGENTS.md section 6 (function 4, a checkable state exposes a health check) and `src/Encina.Messaging/Health/OutboxHealthCheck.cs` (warning and critical thresholds).

</details>

<details>
<summary><strong>6. Checks for stores and external services that do not exist</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Each store or service ships its check with its own issue** (#590 for routing slip stores; #309, #259, #2696 for schema registry; #451 and #604 for feature flags; #607 for service discovery) | The check is written against code that exists and can be integration tested | The umbrella cannot tick those items |
| **B) Write checks now against the interfaces** | Items ticked | A check against `IRoutingSlipStore` or `IChoreographyStateStore` has no implementation to run against |

### Chosen Option: **A - the check ships with the store or service**

### Rationale

- `IRoutingSlipStore` and `IChoreographyStateStore` have no implementer in `src/`; the Choreography feature is documented but unbuilt (#1467).
- Class: Settled
- Settled by: AGENTS.md section 6 (every feature that creates stores or external integrations is evaluated for a health check in its own plan) and #590, #1467.

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

- `Encina.Redis.PubSub` references only `Encina` and registers its own `IConnectionMultiplexer`; every other transport (NATS, Kafka, MQTT) owns its check the same way.
- Class: Trivial
- Settled by: AGENTS.md section 5 (provider coherence: providers differ only in implementation) and `src/Encina.NATS/Health/NATSHealthCheck.cs`.

</details>

---

## Implementation Phases

### Phase 1: Redis.PubSub transport check (pull request 1)

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `src/Encina.Redis.PubSub/Encina.Redis.PubSub.csproj`: add `ProjectReference` to `..\Encina.Messaging\Encina.Messaging.csproj`.
2. **Create** `src/Encina.Redis.PubSub/Health/RedisPubSubHealthCheck.cs`: `public sealed class RedisPubSubHealthCheck : EncinaHealthCheck`, `DefaultName = "encina-redis-pubsub"`, constructor `(IServiceProvider serviceProvider, ProviderHealthCheckOptions? options)` like `NATSHealthCheck`; resolves `IConnectionMultiplexer`, Unhealthy when `!IsConnected`, pings every endpoint with the timeout-linked token, Degraded when a ping exceeds `Timeout / 2`; result text carries the exception type only.
3. **Modify** `EncinaRedisPubSubOptions.cs`: `public ProviderHealthCheckOptions ProviderHealthCheck { get; } = new() { Tags = ["encina", "messaging", "redis-pubsub", "ready"] };`.
4. **Modify** `ServiceCollectionExtensions.cs`: register the options instance and `IEncinaHealthCheck` when `options.ProviderHealthCheck.Enabled`. Note that the method copies each option property into `services.Configure`; the check uses the local `options` instance, so the copy list needs no new line.
5. `PublicAPI.Unshipped.txt` (`mechanical-fixer`).

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
- Registration is gated by ProviderHealthCheck.Enabled; prove it with a ValidateOnBuild/ValidateScopes DI test.
- XML docs with an example; sealed; nullable enabled.

REFERENCE FILES:
- src/Encina.NATS/Health/NATSHealthCheck.cs
- src/Encina.NATS/ServiceCollectionExtensions.cs (lines 65-68)
- src/Encina.Caching.Redis/Health/RedisHealthCheck.cs
- src/Encina.Redis.PubSub/ServiceCollectionExtensions.cs
- src/Encina.Redis.PubSub/EncinaRedisPubSubOptions.cs
```

</details>

### Phase 2: InMemory transport check (pull request 2)

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `src/Encina.InMemory/Encina.InMemory.csproj`: `ProjectReference` to `Encina.Messaging`.
2. **Create** `src/Encina.InMemory/Health/InMemoryTransportHealthCheck.cs` (`DefaultName = "encina-inmemory-transport"`): resolves `IInMemoryMessageBus`; Healthy when unbounded or `PendingCount` below the ratio, Degraded at or above `DegradedFillRatio * BoundedCapacity` (Design Choice 5); data entries `pending_messages`, `subscribers`.
3. **Modify** `EncinaInMemoryOptions.cs`: `ProviderHealthCheck` (tags `encina`, `messaging`, `inmemory`, `ready`) and `DegradedFillRatio` (default 0.9, validated between 0 and 1).
4. **Modify** `ServiceCollectionExtensions.cs` (`AddEncinaInMemory`): register when enabled.

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
2. If Option A: `ProjectReference` to `Encina.Messaging`, `src/Encina.GraphQL/Health/GraphQLHealthCheck.cs` creating a scope and resolving `IGraphQLEncinaBridge`, `EncinaGraphQLOptions.ProviderHealthCheck`, registration in `AddEncinaGraphQL` (pull request 3).

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

1. Verify whether .NET 10 emits a per-check status metric itself (Design Choice 4, Option C); if it does, stop and report.
2. **Create** `src/Encina.OpenTelemetry/Health/HealthCheckMetricsPublisher.cs`: `internal sealed class HealthCheckMetricsPublisher : IHealthCheckPublisher`, an observable gauge `encina.health.check_status` on the shared `Encina` meter, tag `check.name`, value 0, 1 or 2, updated from each `HealthReport`.
3. **Modify** `src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs`: `AddEncinaHealthCheckMetrics()` registering the publisher (opt-in, off by default).
4. Logging: in `Encina.Redis.PubSub` EventIds 4406-4407 (range 4400-4449, 4400-4405 used) and in `Encina.InMemory` 4460-4461 (range 4450-4499, 4450-4459 used) for failed and degraded checks; in `Encina.OpenTelemetry` 7010 (range 7000-7099, 7000-7009 used) for a publisher failure. No new range.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
Encina.OpenTelemetry already calls metrics.AddMeter("Encina"); OutboxProcessorMetrics (Encina.Messaging) shows an
observable gauge on that meter.

TASK:
Implement the publisher and its registration, and add the log messages with the EventIds above.

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

1. Unit tests per check: Redis.PubSub with a mocked multiplexer (healthy, disconnected, slow ping, exception, timeout); InMemory with the real bus (below, at and above the ratio, unbounded); publisher with a fake `HealthReport`. Tests also prove no message leakage.
2. Guard tests for the new constructors; contract tests that the new checks honor `IEncinaHealthCheck` (name, tags, never throws).
3. Integration: Redis.PubSub against a Redis Testcontainers fixture (`[Collection]` shared fixture, `[Trait("Category", "Integration")]`); InMemory and the publisher are in-process and justified in their manifests.
4. Property, load, benchmark: not required (deterministic, low frequency).
5. `mechanical-fixer`: manifests for `Encina.Redis.PubSub.json`, `Encina.InMemory.json`, `Encina.OpenTelemetry.json` (per-file targets and one-sentence justifications), PublicAPI lines, changelog fragment `changelog.d/622-transport-health-checks.added.md`.
6. `docs-writer`: `docs/guides/health-checks.md` (transport table, telemetry section), package READMEs.
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
| `NATSHealthCheck` and its registration | `src/Encina.NATS/Health/`, `ServiceCollectionExtensions.cs` lines 65-68 | Transport reference pattern |
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
| Production code | 5 | 8 | Redis.PubSub and InMemory checks, publisher, two log classes; options, registrations, csproj files, OpenTelemetry registration |
| Tests | 7 | 0 | Unit (3), guard, contract, one Redis integration, DI registration |
| Documentation | 0 | 4 | Guide, three package READMEs |
| Configuration | 1 | 6 | Changelog fragment; three manifests, three `PublicAPI.Unshipped.txt` |
| **Total** | **13** | **18** | 31 files in 3 pull requests: PR 1 Redis.PubSub (about 11), PR 2 InMemory (about 10), PR 3 telemetry publisher and guide (about 10); each under 15 files |

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
- Options property `{ get; } = new() { Tags = [...] }`; registration gated by Enabled at registration time.
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
| 3 | Structured Logging | ✅ Include | Phase 4: EventIds 4406-4407, 4460-4461, 7010 inside existing ranges |
| 4 | Health Checks | ✅ Include | This is the feature |
| 5 | Validation | ✅ Include | `DegradedFillRatio` is validated between 0 and 1 at options validation |
| 6 | Resilience | ✅ Include | Probes are bounded by `ProviderHealthCheckOptions.Timeout`; endpoint-level policy is #454 |
| 7 | Distributed Locks | ❌ N/A | Each instance checks itself |
| 8 | Transactions | ❌ N/A | Read-only probes |
| 9 | Idempotency | ❌ N/A | Probes are repeatable |
| 10 | Multi-Tenancy | ⏭️ Defer | Per-tenant database or cache health needs a tenant-aware check design; no issue exists, so an issue file ships with this plan (shared with #454) |
| 11 | Module Isolation | ⏭️ Defer | `IModuleHealthCheck` and `AddEncinaModuleHealthChecks` already exist; nothing new for transports |
| 12 | Audit Trail | ❌ N/A | Infrastructure monitoring, no compliance event |

**Deferred items:**

- Multi-tenancy: per-tenant health is not requested by any open issue; the orchestrator opens the issue file written with this plan (also referenced by #454).
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
| Maintainer answers to Design Choices 1 to 4 | Pending | Asked when work starts |

No blocking prerequisites.

---

## Next Steps

1. The orchestrator asks the maintainer Design Choices 1 to 4 when work starts, records them under `## Maintainer Decisions`, then briefs three workers (Redis.PubSub, InMemory, telemetry).
2. Comment on #622 with the gap table; close it after the last pull request if Design Choice 2 is Option A.
3. Open the deferred per-tenant health issue from the issue file written with this plan.
