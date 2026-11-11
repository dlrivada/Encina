# Implementation Plan: `Encina.Compliance.OPA` — Open Policy Agent as a Compliance Applicability Resolver

> **Issue**: [#688](https://github.com/dlrivada/Encina/issues/688)
> **Type**: Feature
> **Complexity**: Medium-High (7 phases, new package, no database, about 40 files in 4 pull requests)
> **Estimated Scope**: ~1,500-1,900 lines of production code + ~2,000-2,500 lines of tests, plus Rego examples and a Docker service
> **Milestone**: v0.17.0 — Compliance Lifecycle (last of the order #686, #687, #2700, #688)
> **Depends on**: #686 (the contract; `docs/plans/compliance-applicability-resolver-implementation-plan-686.md`); ordered after #2700

---

## Summary

A satellite package `Encina.Compliance.OPA` with `OpaComplianceResolver`, an `IComplianceApplicabilityResolver` that sends the typed `ComplianceContext` to an Open Policy Agent server (`POST /v1/data/{policyPath}`), maps the decision (a set of capability names, a set of regulation identifiers and, for a removal, its territorial evidence) to a `ComplianceProfile`, caches it for a bounded time and falls back to the full profile on any failure. Organizations that keep their policies in Rego, outside the application, can reload and test them with `opa test` while the rest of Encina consumes the same profile as with any other resolver.

**Reading of #686 that shapes this plan.** The sketch of the issue (typed boolean decision keys, `enforcement_mode`, `FallbackProfile`, `ComplianceMergeStrategy`, `SubscriptionTier`) is replaced by the contract of the #686 plan: capability set plus regulation identifiers (Design Choice 3), no modes (9), union composition (11), no tier input (12), full profile when the answer is unknown or fails (4 and 6), removal only with territorial evidence that the service gate re-checks (5, 14, 15). The architectural review notes on the issue (Polly pipeline, health check, a defined fallback) are met by Design Choices 3 and 4: the fallback is the full profile, so an outage can only widen the profile.

**Out of scope**: embedded WASM evaluation (Design Choice 1, option B), an Encina-specific policy language, a JSON/YAML rules engine (the DSL of #687 covers declarative rules), Casbin/Kyverno/Cedar adapters (they can follow the same resolver pattern later), `AddCcpa`/`AddLgpd`-style non-EU mappings (SPEC-002 INV-002), and writing policies: the package ships examples under `docs/examples/opa/`, not a policy library.

**Standards**: GDPR Art. 5(1)(c) (data minimisation: the input carries only typed context fields, never the subject id), Art. 5(2); SPEC-002 REQ-011, REQ-062, DEC-006, INV-005; OPA REST API (`/v1/data`, `/health`).

**Affected packages**: new `Encina.Compliance.OPA`; `Encina` only for the `EventIdRanges.cs` registration; `Encina.Testing.Testcontainers` (an OPA fixture and its default image); `docker-compose.yml`; `Encina.Caching.*` only if Design Choice 2 is option B.

**Provider category**: none for the package (OPA is an external HTTP service, not a database, transport or lock provider). If Design Choice 2 is option B the cache goes through `ICacheProvider` and stays provider-agnostic across the 8 caching providers.

---

## Design Choices

Two Dilemmas are open (1 and 2); the maintainer answers them when work on the issue starts. Choices 3 to 8 are Settled or Trivial.

<details>
<summary><strong>1. OPA deployment mode in 1.0</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) HTTP to an OPA server only** (sidecar, daemon or central) | Full Rego built-ins and `opa test` tooling; one code path; testable with the OPA container image | A network hop and an outage path (covered by the full-profile fallback) |
| **B) Also embed OPA in process as WASM** | No server, lower latency | .NET WASM interop is immature; fewer Rego built-ins; harder to debug; a second code path to test |

### Chosen Option: **A — HTTP only** (recommended, pending the maintainer)

### Rationale

- One code path testable against the real OPA image keeps the per-flag coverage and the integration tests honest; B doubles the surface for a deployment shape (no sidecar possible) that no application of the 1.0 scope has.
- B wins for single-process deployments that cannot run a sidecar and need the lowest latency.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Where the decision cache lives</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A private in-memory cache in the package** (TTL, keyed by the context fields sent to OPA) | No dependency; pay-for-what-you-use (AGENTS.md section 1); no serialization of `ComplianceProfile` | Each instance caches separately; only the TTL invalidates it after a policy change |
| **B) `ICacheProvider`** (any of the 8 caching providers) | Shared across instances; tag invalidation; consistent with the caching category | Needs a caching package and a serializer for the profile; the OPA server gives no invalidation signal, so tags have nothing to listen to |

### Chosen Option: **A — Private in-memory cache** (recommended, pending the maintainer)

### Rationale

- OPA does not announce policy changes, so the only invalidation either option has is the TTL; B adds a dependency and serialization for no extra freshness. A stale entry is never served during an outage (Design Choice 3).
- B wins for scaled-out deployments with a central OPA whose policies change often and where one shared entry saves many requests.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Failure answer and resilience</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Any failure (timeout, open circuit, HTTP error, undefined result, unmappable decision) returns the full profile, counted and logged by error code; no stale-cache answer; no configurable fallback; a named `HttpClient` with the Microsoft.Extensions.Http.Resilience handler (timeout, retry with backoff, circuit breaker), always on** | Fails closed: an outage can only widen the profile; one engine, as in the resilience family | An outage raises enforcement until OPA is back |
| **B) A configurable `FallbackProfile` (deny-all, allow-cached, fail-open)** | Operators choose | `Disabled`-like fallbacks lower a legal floor; stale answers hide a policy change |

### Chosen Option: **A — Full profile, resilience always on**

### Rationale

- The architectural review note asked for the fallback to be defined: it is the full profile (#686 Design Choice 6; SPEC-002 INV-005). `Encina.Http` (#1233) does not exist yet, so the package builds its own named client on the same engine ADR-055 prescribes for it, and moves to `Encina.Http` when that package ships. OPA queries are idempotent reads, so retry is safe (ADR-029 classification).
- In a `CompositeComplianceResolver` union the full profile of a failed OPA only widens the result, which is the intended fail-closed behavior.

**Class:** Settled
**Settled by:** AGENTS.md section 3 (gates fail closed; errors are not swallowed); #686 plan Design Choice 6; SPEC-002 INV-005; ADR-055 (resilience family; `Encina.Http` over Microsoft.Extensions.Http.Resilience); the architectural review note on #688

</details>

<details>
<summary><strong>4. Decision shape, removals and composition</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The decision is a set of capability names, a set of regulation identifiers and optional `skips` with territorial evidence (kind, country, source); no `enforcement_mode`; union only through `CompositeComplianceResolver`; the service gate re-checks every removal** | One vocabulary with the other resolvers; a policy cannot lower a duty without evidence | A Rego author must emit evidence for every removal |
| **B) Typed booleans per module and a configurable merge strategy (the issue sketch)** | Familiar | Contradicts the capability set, the decided union and the binary applicability |

### Chosen Option: **A — Capability set with evidence for removals**

### Rationale

- Decided by the maintainer on 2026-10-30 in the #686 thread; an unknown capability name, a missing `result` (undefined policy) or malformed JSON is a failure and gives the full profile.

**Class:** Settled
**Settled by:** #686 plan Design Choices 3, 9, 11, 14 and 15 (maintainer decisions of 2026-10-30); SPEC-002 DEC-006

</details>

<details>
<summary><strong>5. Privacy of the OPA input and secrets in options</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Only typed context fields are sent (`operation`, `tenant {id, country, regulations}`, `operator_country`, `subject_country`, `region {country, in_eea}`, `module`, `data_categories`); the subject id and `Metadata` are never sent, logged or tagged; `OpaComplianceOptions.Headers` is `[JsonIgnore]` and `ToString()` hides values** | Data minimisation; credentials cannot print | A policy cannot decide on the subject id or free metadata |
| **B) Send the whole context including `Metadata`** | Maximum policy power | Free-form data leaves the process; contradicts data minimisation |

### Chosen Option: **A — Typed fields only, secret-safe options**

### Rationale

- The span attribute `opa.endpoint` and `http.url` of the issue are dropped because an endpoint can contain credentials.

**Class:** Settled
**Settled by:** GDPR Art. 5(1)(c); SPEC-002 REQ-062; AGENTS.md section 3 (options with secrets, #851); #686 plan (`ComplianceContext.ToString()` omits the subject id)

</details>

<details>
<summary><strong>6. Registration and naming</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `AddEncinaComplianceOPA(Action<OpaComplianceOptions>)` registers options, validator, named client with resilience, cache, resolver and health check; the application selects the resolver with the existing `UseResolver<OpaComplianceResolver>()` or adds it to `CompositeResolverBuilder`** | Package-named registration; no change to the options class of #686 | Two calls for a standalone setup |
| **B) A `UseOpa(...)` extension on `ComplianceApplicabilityOptions`** | One call | Hides the registration of services behind an options extension; a second way to select a resolver |

### Chosen Option: **A — `AddEncinaComplianceOPA` plus `UseResolver<T>()`**

### Rationale

- Registration methods are named after their package, and the options validator of #686 already requires exactly one selected resolver. A DI test proves the provider builds with `ValidateOnBuild` and `ValidateScopes` in both orders.

**Class:** Trivial
**Settled by:** implementation-plan prompt rule 3 (satellite DI `AddEncina{Feature}`); #686 plan Design Choice 18; AGENTS.md section 3 (registration completeness)

</details>

<details>
<summary><strong>7. EventIds, telemetry names and audit</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A new range `ComplianceOPA = (6620, 6639)` from the family block, registered first; instruments named by ADR-054 on the `Encina.Compliance.OPA` source and meter; OPA writes no audit record, the service gate of #686 records skips and the policy path is part of the evidence** | One audit writer; ranges separate from #687 | None significant |
| **B) Reuse the #687 range** | One range | Two packages in one range breaks the one-assembly-per-range rule of the architecture test |

### Chosen Option: **A — Range 6620-6639, ADR-054 names, audit through #686**

### Rationale

- The issue named 9800-9999, which the orchestrator registry assigned to other families; the plan family block 6600-6649 replaces it.

**Class:** Trivial
**Settled by:** AGENTS.md section 7 and ADR-021; ADR-054 settled point 4; #686 plan Design Choice 15; ADR-036

</details>

<details>
<summary><strong>8. OPA container image and compose profile</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One pinned stable OPA release tag defined once in `ContainerDefaultImages` and once in `docker-compose.yml` (service `opa`, profiles `compliance` and `full`)** | Reproducible; one source per provider | Two places to bump (compose and Testcontainers), as for every other provider |
| **B) `latest`** | No maintenance | Non-reproducible CI |

### Chosen Option: **A — Pinned stable tag, profile `compliance`**

### Rationale

- Every container default is the vendor's supported line, or stable where the vendor has no LTS, and is defined once.

**Class:** Trivial
**Settled by:** `src/Encina.Testing.Testcontainers/ContainerDefaultImages.cs` (remarks on defaults); AGENTS.md section 9 (local services via `docker compose --profile`)

</details>

---

## Implementation Phases

Each phase is one pull request of at most 15 files and 1500 changed lines. Phase 0 is the gate: #686 Phases 1 to 2 are merged.

### Phase 1: Package Scaffold, Options, Validator and EventIds

> **Goal**: The package exists with options, validator and registered EventId range. **Size**: about 13 files, 600 lines.

<details>
<summary>Tasks</summary>

1. `src/Encina.Compliance.OPA/Encina.Compliance.OPA.csproj` (`net10.0`, nullable, XML docs, PublicAPI analyzers; references `Encina` and `Encina.Compliance.Applicability`; packages `Microsoft.Extensions.Http.Resilience`, `Microsoft.Extensions.Options`, `Microsoft.Extensions.Diagnostics.HealthChecks`); add to `Encina.slnx`; `PublicAPI.Shipped.txt` (empty) and `PublicAPI.Unshipped.txt`; `README.md`.
2. `OpaComplianceOptions.cs` (public sealed): `required string Endpoint`, `PolicyPath` (default `encina/compliance/applicability`), `CacheDuration` (5 min), `EnableCaching`, `RequestTimeout` (2 s), `CircuitBreakerThreshold` (5), `CircuitBreakerDuration` (30 s), `Dictionary<string,string> Headers` with `[JsonIgnore]`, `ToString()` that hides values, `Func<JsonDocument, ComplianceProfile>? CustomDecisionMapper`. No `FallbackProfile`, no `EnableCircuitBreaker`.
3. `OpaComplianceOptionsValidator.cs` (`IValidateOptions`, `ValidateOnStart`): absolute `http`/`https` endpoint URI, non-blank policy path, positive durations and thresholds.
4. `src/Encina/Diagnostics/EventIdRanges.cs`: register `ComplianceOPA = (6620, 6639)`; field in `src/Encina/PublicAPI.Unshipped.txt`; add `["Encina.Compliance.OPA"] = [nameof(EventIdRanges.ComplianceOPA)]` to `AssemblyRanges` in `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`.
5. `Diagnostics/OpaLogMessages.cs` (`[LoggerMessage]`, doc `Event IDs: 6620-6629 (see EventIdRanges.ComplianceOPA)`): 6620 `RequestFailed` (Warning), 6621 `CircuitOpened` (Warning), 6622 `CircuitClosed` (Information), 6623 `FallbackUsed` (Warning), 6624 `DecisionMapped` (Debug), 6625 `DecisionUnmappable` (Warning), 6626 `RemovalWithoutEvidence` (Warning), 6627 `StartupValidationFailed` (Error), 6628 `HealthProbeFailed` (Warning), 6629 `CacheEntryExpired` (Debug); 6630-6639 free.
6. Tests: validator, options `ToString()` hides header values and is `[JsonIgnore]`d, guard tests; `.github/coverage-manifest/Encina.Compliance.OPA.json` (new) with per-file, per-flag targets and one-sentence justifications.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT:
- Issue #688; plan docs/plans/compliance-opa-implementation-plan-688.md; the contract is #686 (merged). Maintainer Decisions override the "recommended" choices.
- ADR-021: register the EventId range before any [LoggerMessage].

TASK:
Create the package scaffold, OpaComplianceOptions, its validator, the EventId registration and log messages,
the manifest and the tests. At most 15 files and 1500 lines.

KEY RULES:
- Headers is [JsonIgnore] and ToString() hides values (AGENTS.md section 3).
- No FallbackProfile and no EnableCircuitBreaker option exist.
- Zero warnings; XML docs on every public member; RS0016 must not fire.

REFERENCE FILES:
- src/Encina.Compliance.Consent/ConsentOptionsValidator.cs
- src/Encina/Diagnostics/EventIdRanges.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

---

### Phase 2: Input and Decision Mappers, OPA Client and Resolver

> **Goal**: A context goes to OPA and a profile comes back. **Size**: about 12 files, 900 lines.

<details>
<summary>Tasks</summary>

1. `Mapping/OpaInputMapper.cs` (internal): `ComplianceContext` to the JSON of Design Choice 5 (snake_case names); never the subject id or `Metadata`.
2. `Mapping/OpaDecisionMapper.cs` (internal): JSON `{ "result": { "capabilities": [...], "regulations": [...], "skips": [...] } }` to `ComplianceProfile` through `ComplianceRegulation.Parse` and the capability name table; a removal builds `ComplianceSkipEvidence` (kind, country, source, operator country); an unknown capability, a missing `result` or malformed JSON is a failure. `CustomDecisionMapper` replaces it when set.
3. `OpaClient.cs` (internal): `POST {endpoint}/v1/data/{policyPath}` through the named `HttpClient` `Encina.Compliance.OPA`, headers from options, `CancellationToken` honoured; returns `Either<EncinaError, JsonDocument>`; errors by code (`compliance.opa.request_failed`, `compliance.opa.undefined_result`).
4. `OpaDecisionCache.cs` (internal, Design Choice 2 = A): `ConcurrentDictionary` plus expiry from `TimeProvider`, key from the typed fields sent to OPA; a stale entry is never served; size bound with a documented cap.
5. `OpaComplianceResolver.cs` (public sealed, `IComplianceApplicabilityResolver`, `IComplianceContextReader` with `Reads = Operator | Tenant | Subject`): cache, client, mapper, cache store; any failure returns `ComplianceProfile.Full(nameof(OpaComplianceResolver))`, counted and logged; never throws; a removal without evidence returns the full profile (the gate would refuse it anyway).
6. `OpaErrors.cs` (`EncinaErrors.Create`, fixed messages, no identifiers); tests: mappers (every JSON shape, property test that the mapping is total), cache expiry with `FakeTimeProvider`, resolver with a fake `HttpMessageHandler` at the boundary, guard tests, contract suite of #686; manifest targets.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT:
- Phase 1 is merged. Plan Design Choices 3, 4 and 5; the profile, evidence and error types are from #686.

TASK:
Implement the mappers, OpaClient, OpaDecisionCache, OpaComplianceResolver and OpaErrors with their tests.
At most 15 files and 1500 lines.

KEY RULES:
- Any failure returns the full profile and never throws; no stale-cache answer.
- The subject id and Metadata never reach the input JSON, a log, a tag or an exception message.
- Time from TimeProvider; the test double sits at the HTTP boundary only.

REFERENCE FILES:
- src/Encina/Compliance/ComplianceProfile.cs and ComplianceSkipEvidence.cs (#686)
- src/Encina.Security.Secrets/Resilience/SecretsResiliencePipelineFactory.cs
- src/Encina.Compliance.Applicability/Resolvers/RegionBasedComplianceResolver.cs (#686)
```

</details>

---

### Phase 3: Resilience, Registration and Health Check

> **Goal**: The package registers with one call and is resilient and checkable. **Size**: about 9 files, 600 lines.

<details>
<summary>Tasks</summary>

1. `ServiceCollectionExtensions.cs`: `AddEncinaComplianceOPA(this IServiceCollection, Action<OpaComplianceOptions>)`: options and validator (`ValidateOnStart`), `TryAddSingleton(TimeProvider.System)`, the named client `Encina.Compliance.OPA` with `AddResilienceHandler` (total timeout from `RequestTimeout`, retry with exponential backoff and jitter for transport errors and 5xx, circuit breaker from the two options, per-attempt timeout), `OpaDecisionCache`, `OpaComplianceResolver` as singleton (`TryAdd`), the health check.
2. Circuit state is per container (singleton), never static; circuit open and close events are logged (6621, 6622) and counted.
3. `Health/OpaHealthCheck.cs`: `GET /health` reachable and the policy path defined (`/v1/data/{policyPath}` returns a `result`); `DefaultName` const, static `Tags`, scoped resolution through `CreateScope()` where needed.
4. Tests: DI test with `ValidateOnBuild` and `ValidateScopes` for standalone (`UseResolver<OpaComplianceResolver>()`) and composite use, in both registration orders; resilience behavior against a fake handler (retry count, circuit opens at the threshold, timeout) using `FakeTimeProvider`; health check healthy/unhealthy.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT:
- Phases 1-2 merged. ADR-055: resilience through Microsoft.Extensions.Http.Resilience on the named client; Encina.Http (#1233) does not exist yet.

TASK:
Implement AddEncinaComplianceOPA, the resilience handler, circuit events and OpaHealthCheck with tests.

KEY RULES:
- Registration completeness: options, validator, client, cache, resolver and health check, proven by a DI test with ValidateOnBuild and ValidateScopes.
- No static circuit state; retry only the idempotent query; timeouts from TimeProvider-driven tests, no Thread.Sleep.

REFERENCE FILES:
- src/Encina.Refit/ServiceCollectionExtensions.cs
- src/Encina.Compliance.DPIA/Health/DPIAHealthCheck.cs
- docs/architecture/adr/055-resilience-family.md
```

</details>

---

### Phase 4: Observability

> **Goal**: Requests, cache, circuit and fallback are visible without personal data. **Size**: about 6 files, 400 lines.

<details>
<summary>Tasks</summary>

1. `Diagnostics/OpaDiagnostics.cs`: `ActivitySource` and `Meter` both named `Encina.Compliance.OPA` (ADR-054); instruments: `encina.compliance.opa.requests` (Counter, tags `status`, `policy_path`), `encina.compliance.opa.request.duration` (Histogram, unit `s`), `encina.compliance.opa.cache.hits`, `encina.compliance.opa.cache.misses`, `encina.compliance.opa.circuit.opened`, `encina.compliance.opa.fallbacks` (Counters), `encina.compliance.opa.decision.capabilities` (Counter, tag `capability`).
2. Spans `encina.compliance.opa.evaluate` (`opa.policy_path`, `opa.cached`) and `encina.compliance.opa.http_request` (`http.request.method`, `http.response.status_code`); no endpoint, no URL, no subject id; tenant tag only when multi-tenancy is on; zero cost without listeners.
3. Tests: in-memory exporter test for spans and counters, a test that no tag or log carries the subject id, endpoint or header values.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT:
- Phases 1-3 merged. ADR-054 fixes names and tenant tagging.

TASK:
Add OpaDiagnostics (source, meter, instruments, spans) and wire it into the resolver, client and circuit events.

KEY RULES:
- Lowercase dotted names, no _total/_count suffix, durations in seconds.
- Never tag the endpoint, URL, header values or subject id.

REFERENCE FILES:
- src/Encina.Compliance.DPIA/Diagnostics/DPIADiagnostics.cs
- docs/architecture/adr/054-observability-conventions-and-trace-propagation.md
```

</details>

---

### Phase 5: Examples, Docker Service and OPA Test Fixture

> **Goal**: A real OPA can run locally and in the integration tests. **Size**: about 12 files, 600 lines.

<details>
<summary>Tasks</summary>

1. `docker-compose.yml`: service `opa` (pinned stable image, port 8181, the example policy directory mounted read-only, `run --server`), profiles `compliance` and `full`; `docs/infrastructure/docker-infrastructure.md` lists it (docs-writer).
2. `src/Encina.Testing.Testcontainers/OpaContainerFixture.cs` (generic `ContainerBuilder`, wait for `/health`, loads the example policies) and `ContainerDefaultImages.Opa`; `EncinaContainers.Opa()`.
3. `docs/examples/opa/`: `gdpr_applicability.rego`, `health_data.rego`, `territorial_removal.rego` (each `package encina.compliance.applicability`, `import rego.v1`, a removal always carries its evidence, no `default` that lowers the profile) and the matching `_test.rego` files (`opa test` is run by the integration job).
4. Request to the orchestrator: add the OPA service and the `opa test` step to the integration job in `.github/workflows/*` (hot spot, not edited by the worker).

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT:
- Phases 1-4 merged. Local services are started with docker compose profiles; Testcontainers fixtures live in Encina.Testing.Testcontainers.

TASK:
Add the OPA compose service, the OpaContainerFixture with its default image, and the example Rego policies with opa test files.

KEY RULES:
- One pinned stable image tag defined once per place; no latest.
- Example policies follow the decision shape of the plan; CCPA/LGPD appear only as application-written identifiers.
- Do not edit .github/workflows/*.

REFERENCE FILES:
- docker-compose.yml
- src/Encina.Testing.Testcontainers/RedisContainerFixture.cs
- src/Encina.Testing.Testcontainers/ContainerDefaultImages.cs
```

</details>

---

### Phase 6: Cross-Cutting Integration and Testing

> **Goal**: Every flag reaches its target against a real OPA. **Size**: about 12 files, 1000 lines.

<details>
<summary>Tasks</summary>

1. Integration (`tests/Encina.IntegrationTests/Compliance/OPA/`, `[Collection("OPA")]` over a shared `OpaContainerFixture`, `[Trait("Category", "Integration")]`): example policies against the real server, the outage fallback (container stopped), cache behavior, a removal with and without evidence, the pipeline with the #686 gate.
2. Contract: the resolver contract suite of #686 against `OpaComplianceResolver`. Property (FsCheck): the mapping is total and the input never contains the subject id.
3. Load (`tests/Encina.LoadTests/`): concurrent requests, cache effectiveness and circuit behavior; run locally with `DOTNET_JitObjectStackAllocationConditionalEscape=0`. Benchmark: cache hit and miss paths and mapper cost (`BenchmarkSwitcher`, results to `artifacts/performance/`).
4. Multi-tenancy: tenant id, country and regulations are in the input and in the cache key; a two-tenant test shows tenant A never reads tenant B's cached profile. Module isolation: `module` in the input lets a policy scope by module.
5. Per-flag coverage at the targets of `.github/coverage-manifest/Encina.Compliance.OPA.json`; CRAP of every new method at most 10.

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT:
- Phases 1-5 merged. Tests execute real code; integration tests use shared collection fixtures.

TASK:
Write the integration, contract, property, load and benchmark tests and verify per-flag coverage and CRAP.

KEY RULES:
- [Collection("OPA")] shared fixture; no per-class container; Shouldly through Encina.Testing.Shouldly.
- No Thread.Sleep; the outage test stops the container through the fixture.

REFERENCE FILES:
- tests/Encina.IntegrationTests/ (an existing Redis or database collection definition)
- tests/Encina.ContractTests/Core/Compliance/ (#686)
```

</details>

---

### Phase 7: Documentation and Finalization

> **Goal**: The feature, the Rego guide and the package are documented. **Size**: about 8 files.

<details>
<summary>Tasks</summary>

1. XML docs with examples on all public APIs; `src/Encina.Compliance.OPA/README.md`.
2. `docs/features/compliance-opa-integration.md` and `docs/guides/opa-rego-compliance-policies.md` (standalone, composite and hybrid examples; the Rego writing guide), by the docs-writer agent.
3. `changelog.d/688-compliance-opa.added.md`; `docs/INVENTORY.md`, `ROADMAP.md`.
4. ADR for the OPA compliance integration (number assigned when the ADR pull request opens): records Design Choices 1 to 4.
5. `dotnet build --configuration Release` with 0 warnings; `dotnet test` green.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```text
CONTEXT:
- Phases 1-6 merged. Documentation goes through docs-writer; changelog fragment and PublicAPI lines through mechanical-fixer.

TASK:
Finish XML docs, README, the feature page, the Rego guide, the ADR draft and the changelog fragment.

KEY RULES:
- English only; no private client names (write "the reference application"); coverage figures only through covref markers.

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
| GDPR | Art. 5(1)(c), 5(2) | Data minimisation of the OPA input; accountability through skip records |
| SPEC-002 | REQ-011, REQ-062, DEC-006, INV-005 | Conditional enforcement; instrumentation without personal data; fail closed; unknown means full |
| Open Policy Agent | REST API `/v1/data/{path}`, `/health`; `opa test` | Query, health probe and policy tests (CNCF graduated project) |
| ADR-055, ADR-054 | Resilience family; observability conventions | Resilience engine, instrument and meter names |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| Contract (to be added by #686) | `src/Encina/Compliance/` | Resolver interface, `ComplianceContext`, `ComplianceProfile`, evidence |
| Resolver package (to be added by #686) | `src/Encina.Compliance.Applicability/` | `UseResolver<T>()`, `CompositeResolverBuilder`, options validator |
| Container fixtures | `src/Encina.Testing.Testcontainers/` | Pattern for `OpaContainerFixture` and `ContainerDefaultImages` |
| Resilience precedent | `src/Encina.Security.Secrets/Resilience/SecretsResiliencePipelineFactory.cs` | Timeout, retry, circuit breaker layering |
| Health check precedent | `src/Encina.Compliance.DPIA/Health/DPIAHealthCheck.cs` | `DefaultName`, tags, scoped resolution |
| Local services | `docker-compose.yml` | New service `opa` |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Compliance.OPA` | 6620-6639 (`ComplianceOPA`) | New, from the family block 6600-6649 of the plan batch; 6620-6629 planned, packed; 6630-6639 free |
| `Encina.Compliance.Applicability` (rules, #687) | 6600-6619 | Owned by the #687 plan |
| `Encina.Compliance.DPIA` screening (#1243) | existing 8897-8899 | Owned by the #1243 plan |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Source (package, mappers, client, cache, resolver, diagnostics, health) | 16 | `src/Encina.Compliance.OPA/` |
| Testing infrastructure and compose (`OpaContainerFixture`, compose service) | 3 | `Encina.Testing.Testcontainers`, `docker-compose.yml` |
| Tests (unit, guard, contract, property, integration, load, benchmark) | 16 | Under `tests/Encina.*Tests/Compliance/OPA/` |
| Rego examples and `_test.rego` | 6 | `docs/examples/opa/` |
| Manifest, PublicAPI, EventId registry, architecture-test map | 5 | Edits and one new manifest |
| Documentation (feature page, Rego guide, README, ADR, changelog) | 6 | Docs through docs-writer |
| **Total** | **~52** | In 7 pull requests of at most 15 files |

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library, pre-1.0, Railway Oriented Programming, no compatibility layers. Rules: AGENTS.md.
- Issue #688 adds Encina.Compliance.OPA on the #686 contract. The Maintainer Decisions of the plan override its recommendations.

IMPLEMENTATION OVERVIEW:
1. Package scaffold, options with secret-safe headers, validator, EventIds 6620-6639 (register first).
2. Input/decision mappers, OPA client over a named HttpClient, decision cache, OpaComplianceResolver (any failure gives the full profile).
3. AddEncinaComplianceOPA with the resilience handler and OpaHealthCheck.
4. Observability (ADR-054 names), OPA compose service and fixture, example Rego with opa test.
5. Integration, contract, property, load and benchmark tests; documentation.

KEY PATTERNS:
- Fail closed: timeout, open circuit, HTTP error, undefined result or unmappable decision returns ComplianceProfile.Full; never a stale or narrower answer.
- The subject id and Metadata never leave the process; the endpoint and header values never reach logs or tags.
- A removal needs territorial evidence; the service gate of #686 re-checks it.
- DI test with ValidateOnBuild and ValidateScopes; TimeProvider for time; shared [Collection] fixtures for integration tests.

REFERENCE FILES:
- docs/plans/compliance-applicability-resolver-implementation-plan-686.md
- src/Encina/Diagnostics/EventIdRanges.cs
- src/Encina.Testing.Testcontainers/RedisContainerFixture.cs
- docker-compose.yml
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ | Phase 2: decision cache with TTL; where it lives is Design Choice 2 (Dilemma, private in-memory recommended) |
| 2 | OpenTelemetry | ✅ | Phase 4: seven instruments and two spans, ADR-054 names, no endpoint or subject id |
| 3 | Structured Logging | ✅ | Phase 1: `[LoggerMessage]` 6620-6629 in `ComplianceOPA`, registered first |
| 4 | Health Checks | ✅ | Phase 3: `OpaHealthCheck` (`/health` reachable, policy path defined) |
| 5 | Validation | ✅ | Phase 1: options validator with `ValidateOnStart` |
| 6 | Resilience | ✅ | Phase 3: timeout, retry, circuit breaker on the named client (Design Choice 3) |
| 7 | Distributed Locks | ❌ | Read-only policy evaluation |
| 8 | Transactions | ❌ | No state change |
| 9 | Idempotency | ❌ | Evaluation is a pure query |
| 10 | Multi-Tenancy | ✅ | Phase 6: tenant id, country and regulations in the input and the cache key; tenant tag only when tenancy is on |
| 11 | Module Isolation | ✅ | Phase 6: `module` in the input so a policy can scope by module |
| 12 | Audit Trail | ✅ | Through the skip records of #686; the policy path is part of the evidence; the package writes none itself |

---

## Prerequisites & Dependencies

### Required Prerequisites

- #686 Phases 1 to 2 merged (contract, `Encina.Compliance.Applicability`, composite resolver, options validator).
- The orchestrator adds the OPA service and `opa test` step to the integration workflow (Phase 5 request).

### Recommended (Not Blocking)

- #2700 (ordered before #688 in v0.17.0) and #687 (the DSL; same profile output, composes through the composite).
- #1233 (`Encina.Http`): when it ships, the named client moves onto it (follow-up, no issue yet).

---

## Next Steps

1. Link this plan from #688 and from #686 and #687.
2. When #688 is picked up, re-check each Design Choice against the code of #686 as merged; present the two Dilemmas to the maintainer and record the answers in `## Maintainer Decisions`.
3. Ship Phases 1-2, 3-4, 5-6 and 7 as separate pull requests referencing #688; the last one carries `Fixes #688`.
