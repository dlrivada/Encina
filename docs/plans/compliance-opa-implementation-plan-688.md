# Implementation Plan: `Encina.Compliance.OPA` — Open Policy Agent as a Compliance Applicability Resolver

> **Issue**: [#688](https://github.com/dlrivada/Encina/issues/688)
> **Type**: Feature
> **Complexity**: Medium-High (8 phases, new package, no database, 8 pull requests, 72 file changes; see Estimated File Count)
> **Estimated Scope**: ~1,500-1,900 lines of production code + ~2,200-2,700 lines of tests, plus Rego examples and a Docker service
> **Milestone**: v0.17.0 — Compliance Lifecycle (last of the order #686, #687, #2700, #688)
> **Depends on**: #686 (the contract; its plan is merged on main); ordered after #2700

---

## Summary

A satellite package `Encina.Compliance.OPA` with `OPAComplianceResolver`, an `IComplianceApplicabilityResolver` that sends the typed `ComplianceContext` to an Open Policy Agent server (`POST /v1/data/{policyPath}`), maps the decision (a set of capability names, a set of regulation identifiers and, for a removal, its territorial evidence) to a `ComplianceProfile`, caches it for a bounded time and makes any failure end in the full profile. Organizations that keep their policies in Rego, outside the application, can reload and test them with `opa test` while the rest of Encina consumes the same profile as with any other resolver.

**Reading of #686 that shapes this plan.** The sketch of the issue (typed boolean decision keys, `enforcement_mode`, `FallbackProfile`, `ComplianceMergeStrategy`, `SubscriptionTier`) is replaced by the contract of the #686 plan: capability set plus regulation identifiers (Design Choice 3), no modes (9), union composition (11), no tier input (12), full profile when the answer is unknown or fails (4 and 6), removal only with territorial evidence and a registry-backed alternative that the service gate re-checks (5, 14, 15). The architectural review notes on the issue (Polly pipeline, health check, a defined fallback) are met by Design Choices 3 and 4: every failure ends in the full profile, so an outage can only widen the profile.

**Out of scope**: embedded WASM evaluation (Design Choice 1, option B), an Encina-specific policy language, a JSON/YAML rules engine (the DSL of #687 covers declarative rules), Casbin/Kyverno/Cedar adapters (they can follow the same resolver pattern later), `AddCcpa`/`AddLgpd`-style non-EU mappings (SPEC-002 INV-002), and writing policies: the package ships examples under `docs/examples/opa/`, not a policy library.

**Standards**: GDPR Art. 5(1)(c) (data minimisation: the input carries only typed context fields, never the subject id), Art. 5(2); SPEC-002 REQ-011, REQ-062, DEC-006, INV-005; OPA REST API (`/v1/data`, `/health`).

**Affected packages**: new `Encina.Compliance.OPA`; `Encina` only for the `EventIdRanges.cs` registration; `tests/Encina.TestInfrastructure` (an OPA fixture, or `Encina.Testing.Testcontainers` with Design Choice 13 B); `docker-compose.yml`; `Encina.Caching.*` only if Design Choice 2 is option B.

**Provider category**: none for the package (OPA is an external HTTP service, not a database, transport or lock provider). If Design Choice 2 is option B the cache goes through `ICacheProvider` and stays provider-agnostic across the 8 caching providers.

---

## Design Choices

Ten Dilemmas are open (1, 2, 3, 4, 6 for the shape of the custom mapper, 9 for the retry and breaker values, 10, 11, 12 and 13); the maintainer answers them when work on the issue starts. Choices 5, 7 and 8 are Settled or Trivial.

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
<summary><strong>3. Failure answer</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Any failure (timeout, open circuit, HTTP error, undefined result, unmappable decision, mapper failure) returns `Right(ComplianceProfile.Full(nameof(OPAComplianceResolver)))`, logged by error code** | Fails closed; the issue text says the failure "returns the full profile" | The #686 service sees a success: no `ResolutionFailed` log or count, its health check does not degrade, and `ResolvedBy = OPAComplianceResolver` reads like a decision of the policy |
| **B) Any failure returns `Left` with a coded `compliance.opa.*` error; the #686 service turns it into the full profile and logs and counts `ResolutionFailed` (#686 Design Choice 6); inside a composite, "any child `Left` makes the composite `Left`" (#686 Phase 2c)** | Fails closed with the same final answer; keeps Railway-oriented results; the failure is visible in the #686 failure metric and health check | Inside a composite the whole composite becomes `Left`, so the other children's removals are cancelled for that request (with A the union with `Full` cancels them too) |
| **C) A configurable `FallbackProfile` (deny-all, allow-cached, fail-open)** | Operators choose | Excluded: `Disabled`-like fallbacks lower a legal floor and stale answers hide a policy change (AGENTS.md section 3) |

No stale-cache answer and no configurable fallback exist in either A or B.

### Chosen Option: **B — Coded `Left`, the #686 service gives the full profile** (recommended, pending the maintainer)

### Rationale

- Both A and B fail closed and give the same final profile (#686 Design Choice 6; SPEC-002 INV-005). B reports the failure as a failure (ADR-001, ADR-006), so #686's single failure log, metric and health check see an OPA outage.
- A wins when OPA outages are expected and the maintainer prefers them not to raise the #686 failure counter and degrade its health check; the OPA metrics of Phase 4 then carry the outage alone.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Resilience engine: an own named client now, or <code>Encina.Http</code></strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A named `HttpClient` `Encina.Compliance.OPA` with the Microsoft.Extensions.Http.Resilience handler (total timeout, retry with backoff, circuit breaker, per-attempt timeout), always on; moved onto `Encina.Http` when #1233 ships** | #688 does not wait for #1233; the same engine ADR-055 prescribes for `Encina.Http` | A second outbound client to move later; follow-up issue to be opened (draft: artifacts/decisions/plan-batch/followups/688-opa-client-on-encina-http.md) |
| **B) Build on `Encina.Http` (#1233, open)** | One outbound HTTP stack from the start | #688 waits for #1233; `Encina.Http` partitions per client by tenant and denies a call without tenant context unless it opts out explicitly and the opt-out is logged (ADR-055), so the background resolution path of #686 needs that opt-out |

### Chosen Option: **A — Own named client, moved later** (recommended, pending the maintainer)

### Rationale

- OPA queries are idempotent reads, so retry is safe (ADR-029 classification); `EnableCircuitBreaker` does not exist, the pipeline is always on (#688 issue body, C3).
- B wins when #1233 is merged before #688 is picked up.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Decision shape, removals and composition</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The decision is a set of capability names, a set of regulation identifiers and optional `skips` with territorial evidence (kind, country); no `enforcement_mode`; union only through `CompositeComplianceResolver`; the service gate re-checks every removal** | One vocabulary with the other resolvers; a policy cannot lower a duty without evidence | A Rego author must emit evidence for every removal |
| **B) Typed booleans per module and a configurable merge strategy (the issue sketch)** | Familiar | Contradicts the capability set, the decided union and the binary applicability |

### Chosen Option: **A — Capability set with evidence for removals**

### Rationale

- Decided by the maintainer on 2026-10-30 in the #686 thread; an unknown capability name, a missing `result` (undefined policy) or malformed JSON is a failure (Design Choice 3).
- The evidence `Kind` and `Country` come from the decision and must equal the context field of that kind (`SubjectCountry` or `TenantCountry`), otherwise the decision is unmappable; `Source` is taken from the context (`SubjectCountrySource` or `TenantCountrySource`) and `OperatorCountry` from `ComplianceContext.OperatorCountry`, so a policy cannot invent the source of a country. The alternative of a removed capability is Design Choice 10.

**Class:** Settled
**Settled by:** #686 plan Design Choices 3, 9, 11, 14 and 15 (maintainer decisions of 2026-10-30); SPEC-002 DEC-006

</details>

<details>
<summary><strong>6. Privacy of the OPA input, secrets in options and the custom decision mapper</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Only typed context fields are sent (`operation`, `tenant {id, country, regulations}`, `operator_country`, `subject_country`, `region {country, in_eea}`, `module`, `data_categories`); the subject id and `Metadata` are never sent, logged or tagged; `OPAComplianceOptions.Headers` is `[JsonIgnore]` and `ToString()` hides values; the validator rejects an `Endpoint` with user information (`user:pass@`); the custom mapper is a DI service `IOPADecisionMapper` with `Either<EncinaError, ComplianceProfile> Map(JsonElement result, ComplianceContext context)`** | Data minimisation; credentials cannot print; the mapper reports failure as `Left` and can take dependencies | A policy cannot decide on the subject id or free metadata |
| **B) As A, but the custom mapper is `Func<JsonElement, Either<EncinaError, ComplianceProfile>>? CustomDecisionMapper` on the options** | One property, as in the issue sketch | A delegate on options cannot take dependencies; it is the only non-data member of the options |
| **C) Send the whole context including `Metadata`; the issue's `Func<JsonDocument, ComplianceProfile>` mapper** | Maximum policy power | Excluded: free-form data leaves the process (GDPR Art. 5(1)(c)), and a mapper that can report failure only by throwing breaks Railway-oriented results (AGENTS.md section 3) |

### Chosen Option: **A — Typed fields only, secret-safe options, `IOPADecisionMapper`** (the input and secrets part is Settled; the mapper shape, A or B, is recommended, pending the maintainer)

### Rationale

- The span attribute `opa.endpoint` and `http.url` of the issue are dropped; with user information rejected, credentials can only travel in `Headers`, which never prints.
- A mapper `Left` or exception is a failure under Design Choice 3; the resolver applies the evidence and alternative rules of Design Choices 5 and 10 to any mapped profile, default or custom.
- B wins for an application that only needs a small inline mapping and no dependency.

**Class:** Dilemma
**Settled by (input and secrets part):** GDPR Art. 5(1)(c); SPEC-002 REQ-062; AGENTS.md section 3 (options with secrets, #851); #686 plan (`ComplianceContext.ToString()` omits the subject id)

</details>

<details>
<summary><strong>7. Registration and naming</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `AddEncinaComplianceOPA(Action<OPAComplianceOptions>)` registers options, validator, named client with resilience, cache, mapper, resolver and health check; the application selects the resolver with the existing `UseResolver<OPAComplianceResolver>()` or adds it to `CompositeResolverBuilder`; acronym upper case in every type name (`OPAComplianceResolver`, `OPAComplianceOptions`, `OPAHealthCheck`, `OPADiagnostics`)** | Package-named registration; one casing for package, method and types; no change to the options class of #686 | Two calls for a standalone setup |
| **B) A `UseOpa(...)` extension on `ComplianceApplicabilityOptions`, `Opa*` type names** | One call | Hides the registration of services behind an options extension; a second way to select a resolver; a casing the repository does not use |

### Chosen Option: **A — `AddEncinaComplianceOPA` plus `UseResolver<T>()`, `OPA*` names**

### Rationale

- Registration methods are named after their package, and the options validator of #686 already requires exactly one selected resolver. A DI test proves the provider builds with `ValidateOnBuild` and `ValidateScopes` in both orders.
- The repository writes acronyms upper case in type names: `src/Encina.Compliance.DPIA/Health/DPIAHealthCheck.cs`, `src/Encina.Security.ABAC/DecisionAudit/AuditStoreABACDecisionRecorder.cs`; no `src/` file name starts with `Dpia`, `Abac` or `Gdpr`.

**Class:** Trivial
**Settled by:** implementation-plan prompt rule 3 (satellite DI `AddEncina{Feature}`); #686 plan Design Choice 18; AGENTS.md section 3 (registration completeness); repository casing precedent above

</details>

<details>
<summary><strong>8. EventIds, telemetry names, error codes and audit</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A new range `ComplianceOPA = (6620, 6639)` from the family block, registered in the first pull request that logs (Phase 2b); instruments named by ADR-054 on the `Encina.Compliance.OPA` source and meter (Phase 4); error codes with an `ErrorCategory` (ADR-062); OPA writes no audit record, the service gate of #686 records skips and the policy path is part of `ResolvedBy`** | One audit writer; ranges separate from #687 | None significant |
| **B) Reuse the #687 range** | One range | Two packages in one range breaks the one-assembly-per-range rule of the architecture test |

### Chosen Option: **A — Range 6620-6639, ADR-054 names, ADR-062 categories, audit through #686**

### Rationale

- The issue named 9800-9999, which the orchestrator registry assigned to other families; the plan family block 6600-6649 replaces it (6600-6619 for #687, 6620-6639 here).
- Error codes, through `EncinaErrors.Create(code, category, message)` (ADR-062, merged), by ADR-062 R7 for outbound calls: `compliance.opa.request_failed` (HTTP error) `UpstreamFailure`; `compliance.opa.unavailable` (network error or open circuit) `Unavailable`; `compliance.opa.timeout` `Timeout`; `compliance.opa.undefined_result` and `compliance.opa.unmappable_decision` `UpstreamFailure`. Messages are fixed strings without identifiers.

**Class:** Trivial
**Settled by:** AGENTS.md section 7 and ADR-021; ADR-054 settled point 4; ADR-062 R7; #686 plan Design Choice 15; ADR-036

</details>

<details>
<summary><strong>9. Option and resilience defaults</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
All options take the defaults of the #688 issue body: `PolicyPath = "encina/compliance/applicability"`, `CacheDuration = 5 min`, `EnableCaching = true`, `RequestTimeout = 2 s` (the total timeout of one resolution, retries included), `CircuitBreakerThreshold = 5`, `CircuitBreakerDuration = 30 s` (the break duration). The issue leaves open the retry values and how the threshold maps onto the ratio-based breaker of Microsoft.Extensions.Http.Resilience; those are the options below. Retries apply to transport errors, timeouts of one attempt and 5xx (OPA queries are idempotent reads).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) 2 retries (3 attempts), exponential backoff from 100 ms with jitter (ADR-055: Polly's own `UseJitter`), per-attempt timeout 500 ms. Nominal budget: 0.5 + 0.1 + 0.5 + 0.2 + 0.5 = 1.8 s, inside the 2 s total. Breaker: `FailureRatio = 1.0`, `MinimumThroughput = CircuitBreakerThreshold` (5), `SamplingDuration = 30 s` (the repository's standard sampling, `StandardResilienceOptions.cs:43`), `BreakDuration = CircuitBreakerDuration` (30 s): the circuit opens after 5 calls in 30 s all fail** | Every retry can run inside the issue's 2 s budget; the breaker reads the issue's two options literally | A slow OPA (over 500 ms per query) fails every attempt; jitter can stretch a delay past its nominal value, and then the total timeout cuts the last attempt short (the outcome is still the full profile) |
| **B) 1 retry (2 attempts), 100 ms delay with jitter, per-attempt timeout 900 ms (0.9 + 0.1 + 0.9 = 1.9 s); same breaker as A** | More time per attempt for a slow OPA | One retry only covers a single transient fault |
| **C) The repository's standard values (`StandardResilienceOptions.cs:26-50`: 3 retries from 1 s, attempt 10 s, total 30 s, ratio 0.1 over a minimum throughput of 10)** | No new numbers | Ignores the issue's 2 s budget for a call on the request path; with a 2 s total, retries 2 and 3 can never run |

### Chosen Option: **A — 2 retries from 100 ms, 500 ms per attempt, breaker on 5 failures in 30 s** (recommended, pending the maintainer)

### Rationale

- A and B both fit the 2 s budget and follow ADR-055 (Polly's jitter, no Encina-owned formula); A tolerates two transient faults, B a slower server. C does not fit the issue's budget.
- The Phase 3 retry-count test uses a fake handler that fails immediately, so all three attempts run in a few milliseconds of `FakeTimeProvider` time (0.1 + 0.2 s of delays), well inside the total timeout; a second test proves that a hanging handler is cut at 500 ms per attempt and at 2 s in total.
- B wins when the OPA server is remote and its latency often exceeds 500 ms; C wins only if the maintainer raises `RequestTimeout` to the standard 30 s.

**Class:** Dilemma

</details>

<details>
<summary><strong>10. Where the alternative of an OPA removal comes from</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The resolver looks the alternative up in `ComplianceCoveringRegulationRegistry.TryGetCover(evidenceCountry, capability, out cover)` and fills `ComplianceProfile.Alternatives`; a removed capability the registry does not cover is kept (applies), logged 6624** | The same rule as the built-in resolvers (#686 Phases 2b and 2c); the policy cannot name an alternative; the service gate accepts the skip | A Rego author cannot see from the policy alone which removals take effect |
| **B) The decision carries an alternative per skip, and the resolver checks it against the registry (a mismatch is an unmappable decision)** | The policy states its intent | The registry value is duplicated in Rego; any drift makes every request fail (Design Choice 3) |

### Chosen Option: **A — Registry lookup in the resolver** (recommended, pending the maintainer)

### Rationale

- #686 Design Choice 15 makes the registry, filled by `MapCountry`, the only source of an alternative and refuses a skip without one; A meets that without a second copy.
- B wins when policy authors and application owners are different teams and the maintainer wants a mismatch to surface as a failure.

**Class:** Dilemma

</details>

<details>
<summary><strong>11. Decision cache bound and health-check depth</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Cache cap of 10,000 entries (new entries are not cached while full, expired entries are removed on access); health check `GET /health` only** | Bounded memory; the probe does not depend on a policy input | A missing or renamed policy path is found only by the first request (which then fails closed) |
| **B) Same cap; health check also queries the policy path with an empty input and requires a `result`** | Detects a missing policy at probe time | A policy whose rules depend on input may be undefined for the empty input, so a correct policy reports unhealthy |
| **C) Cap as an option (`MaxCacheEntries`), `/health?bundles` when the deployment uses bundles** | Configurable | More surface; bundle status depends on how OPA is deployed |

### Chosen Option: **A — 10,000 entries, `/health` only** (recommended, pending the maintainer)

### Rationale

- A probe that sends no context cannot tell a missing policy from an input-dependent one; a missing policy already fails closed on the first request and is logged (6622).
- B wins when every policy defines a `default` result for an empty input; C wins for deployments with very many distinct contexts or bundle-managed policies.

**Class:** Dilemma

</details>

<details>
<summary><strong>12. OPA container image and compose profile</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Service `opa` in the `full` profile only; one pinned stable OPA release tag; a developer starts it alone with `docker compose up -d opa`** | No new profile; AGENTS.md section 9's profile list stays as it is | No one-word profile for compliance work |
| **B) A new `compliance` profile plus `full`** | `docker compose --profile compliance up -d` | A seventh profile: the list in AGENTS.md section 9 and `docs/infrastructure/docker-infrastructure.md` change |
| **C) `latest`** | No maintenance | Excluded: non-reproducible CI |

### Chosen Option: **A — `full` only, pinned stable tag** (recommended, pending the maintainer)

### Rationale

- Every container default is the vendor's supported line, or stable where the vendor has no LTS, defined once in compose and once in the fixture (the `KafkaFixture` precedent, `.WithImage(...)` in the fixture).
- B wins when more compliance services (for example a second policy engine) join OPA.

**Class:** Dilemma

</details>

<details>
<summary><strong>13. Where the OPA test fixture lives</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `tests/Encina.TestInfrastructure/Fixtures/OPAFixture.cs` (flat, like `Fixtures/KafkaFixture.cs`) with its `[CollectionDefinition("OPA")]`** | The precedent for non-database services (`KafkaFixture`, `AzuriteFixture`, `LocalStackFixture`, `MqttFixture` live there); no public API, PublicAPI file or coverage manifest | Applications cannot reuse the fixture to test their own Rego with Encina |
| **B) `src/Encina.Testing.Testcontainers/OPAContainerFixture.cs` with `ContainerDefaultImages.Opa` and `EncinaContainers.Opa()`** | Reusable by applications | Public surface kept to 1.0; Phase 5 grows by the PublicAPI file, `ContainerDefaultImages.cs`, `EncinaContainers.cs`, a fixture test file and the `Encina.Testing.Testcontainers.json` manifest edit (5 files) |

### Chosen Option: **A — `tests/Encina.TestInfrastructure`** (recommended, pending the maintainer)

### Rationale

- The public Testcontainers package holds the database and Redis fixtures; the other services live in the test infrastructure.
- B wins when the maintainer wants applications to test their Rego policies against Encina with a ready fixture.

**Class:** Dilemma

</details>

---

## Implementation Phases

Each phase is one pull request of at most 15 files and 1500 changed lines; every count includes source, tests, guard tests, PublicAPI files, manifest edits and the EventId registration. Each phase enters the per-file, per-flag targets of the files it adds or touches, with a one-sentence justification, in the same pull request as the tests that meet them (AGENTS.md section 9, #1762). Gates: #686 Phases 1a-2d are merged before Phase 1; #686 Phases 3a and 9a before Phase 6.

### Phase 1: Package Scaffold, Options and Validator

> **Goal**: The package exists with options and validator. **Size**: 11 files, about 550 lines. No logging in this phase.

<details>
<summary>Tasks</summary>

1. `src/Encina.Compliance.OPA/Encina.Compliance.OPA.csproj` (`net10.0`, nullable, XML docs, PublicAPI analyzers; references `Encina` and `Encina.Compliance.Applicability`; packages `Microsoft.Extensions.Http.Resilience`, `Microsoft.Extensions.Options`, `Microsoft.Extensions.Diagnostics.HealthChecks`); add to `Encina.slnx`; `PublicAPI.Shipped.txt` (empty) and `PublicAPI.Unshipped.txt`; `README.md` (short; completed in Phase 7).
2. `OPAComplianceOptions.cs` (public sealed): `required string Endpoint`, `PolicyPath`, `CacheDuration`, `EnableCaching`, `RequestTimeout`, `CircuitBreakerThreshold`, `CircuitBreakerDuration` with the defaults of Design Choice 9; `Dictionary<string,string> Headers` with `[JsonIgnore]`; `ToString()` that hides header values. No `FallbackProfile`, no `EnableCircuitBreaker`, no mapper delegate (Design Choice 6).
3. `OPAComplianceOptionsValidator.cs` (`IValidateOptions`, `ValidateOnStart`): absolute `http`/`https` endpoint URI without user information, non-blank policy path, positive durations and thresholds.
4. Tests: `tests/Encina.UnitTests/Compliance/OPA/OPAComplianceOptionsValidatorTests.cs`, `OPAComplianceOptionsTests.cs` (`ToString()` hides header values; `Headers` is `[JsonIgnore]`d); `tests/Encina.GuardTests/Compliance/OPA/OPAComplianceOptionsGuardTests.cs`.
5. `.github/coverage-manifest/Encina.Compliance.OPA.json` (new): unit and guard targets with justifications for the two source files.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT:
- Issue #688; the #688 plan; the contract is #686 (Phases 1a-2d of the #686 plan merged). Maintainer Decisions override the "recommended" choices.

TASK:
Create the package scaffold, OPAComplianceOptions, its validator, the manifest and the tests. 11 files; at most 15
files and 1500 lines.

KEY RULES:
- Headers is [JsonIgnore] and ToString() hides values (AGENTS.md section 3); the validator rejects user:pass@ in Endpoint.
- No FallbackProfile and no EnableCircuitBreaker option exist; defaults per Design Choice 9.
- No logging in this phase (the EventId range is registered in Phase 2b).
- Zero warnings; XML docs on every public member; RS0016 must not fire.

REFERENCE FILES:
- src/Encina.Compliance.Consent/ConsentOptionsValidator.cs
- src/Encina.Extensions.Resilience/Configuration/StandardResilienceOptions.cs
```

</details>

---

### Phase 2a: Input and Decision Mappers, Errors and Decision Cache

> **Goal**: A context becomes OPA input and a decision becomes a profile, without I/O. **Size**: 11 files, about 900 lines. No logging in this phase.

<details>
<summary>Tasks</summary>

1. `Mapping/OPAInputMapper.cs` (internal): `ComplianceContext` to the JSON of Design Choice 6 (snake_case names, `module` included so a policy can scope by module); never the subject id or `Metadata`.
2. `Mapping/IOPADecisionMapper.cs` (public, Design Choice 6) and `Mapping/OPADecisionMapper.cs` (internal default): JSON `{ "result": { "capabilities": [...], "regulations": [...], "skips": [...] } }` to `ComplianceProfile` through `ComplianceRegulation.Parse` and the capability name table; skips build `ComplianceSkipEvidence` per Design Choice 5; an unknown capability, a missing `result`, malformed JSON, or evidence that does not match the context is `Left` `compliance.opa.unmappable_decision` or `compliance.opa.undefined_result`.
3. `OPAErrors.cs` (public static): the codes of Design Choice 8 with their `ErrorCategory`.
4. `OPADecisionCache.cs` (internal, Design Choice 2 = A): `ConcurrentDictionary` plus expiry from `TimeProvider`, key from the typed fields sent to OPA (tenant id, country and regulations included); a stale entry is never served; the cap of Design Choice 11.
5. `src/Encina.Compliance.OPA/PublicAPI.Unshipped.txt`.
6. Tests: `tests/Encina.UnitTests/Compliance/OPA/OPAInputMapperTests.cs` (no subject id or `Metadata`; `module` present), `OPADecisionMapperTests.cs` (every JSON shape; evidence must match the context), `OPADecisionCacheTests.cs` (expiry with `FakeTimeProvider`, cap, a two-tenant test: tenant A never reads tenant B's entry); `tests/Encina.GuardTests/Compliance/OPA/OPAMappingGuardTests.cs`.
7. `.github/coverage-manifest/Encina.Compliance.OPA.json`: unit and guard targets for the five source files.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2a</summary>

```text
CONTEXT:
- Phase 1 is merged. Plan Design Choices 2, 5, 6, 8 and 11; the profile, evidence and error types are from #686.

TASK:
Implement the input mapper, IOPADecisionMapper and its default, OPAErrors and OPADecisionCache with their tests and
manifest targets. 11 files; at most 15 files and 1500 lines.

KEY RULES:
- The subject id and Metadata never reach the input JSON or an error message.
- Mapper failures are Left with a compliance.opa.* code created with its ErrorCategory (ADR-062).
- Time from TimeProvider; no stale-cache answer.

REFERENCE FILES:
- src/Encina/Compliance/ComplianceProfile.cs and ComplianceSkipEvidence.cs (#686)
- src/Encina/Results/EncinaErrors.cs (category overload)
```

</details>

---

### Phase 2b: OPA Client, Resolver and Logging

> **Goal**: A context goes to OPA and a profile comes back; failures are logged by code. **Size**: 11 files, about 900 lines.

<details>
<summary>Tasks</summary>

1. `src/Encina/Diagnostics/EventIdRanges.cs`: register `ComplianceOPA = (6620, 6639)` before any `[LoggerMessage]`; field in `src/Encina/PublicAPI.Unshipped.txt`; add `["Encina.Compliance.OPA"] = [nameof(EventIdRanges.ComplianceOPA)]` to `AssemblyRanges` in `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`. `EventIdRanges.cs` already has its target in `.github/coverage-manifest/Encina.json`.
2. `Diagnostics/OPALogMessages.cs` (`[LoggerMessage]`, doc `Event IDs: 6620-6627 (see EventIdRanges.ComplianceOPA)`): 6620 `RequestFailed` (Warning, error code), 6621 `DecisionMapped` (Debug), 6622 `DecisionUnmappable` (Warning, error code), 6623 `RemovalWithoutEvidence` (Warning), 6624 `RemovalNotCovered` (Debug, capability kept because the registry has no cover). 6625-6627 are added in Phase 3; 6628-6639 stay free.
3. `OPAClient.cs` (internal): `POST {endpoint}/v1/data/{policyPath}` through the named `HttpClient` `Encina.Compliance.OPA`, headers from options, `CancellationToken` honoured (caller cancellation propagates); returns `Either<EncinaError, JsonElement>`.
4. `OPAComplianceResolver.cs` (public sealed, `IComplianceApplicabilityResolver`, `IComplianceContextReader` with `Reads = Operator | Tenant | Subject`, the three countries it sends): cache, client, mapper (`IOPADecisionMapper`), alternatives from `ComplianceCoveringRegulationRegistry` (Design Choice 10), cache store; any failure per Design Choice 3 (B: coded `Left`); never throws for a business outcome; `ResolvedBy` names the resolver and the policy path.
5. `src/Encina.Compliance.OPA/PublicAPI.Unshipped.txt`.
6. Tests: `tests/Encina.UnitTests/Compliance/OPA/OPAClientTests.cs` (fake `HttpMessageHandler` at the boundary: status codes, malformed body, cancellation); `OPAComplianceResolverTests.cs` (cache hit and miss, every failure path, **an accepted removal: a skip with matching evidence and a registry cover gives a profile whose `Evidence` and `Alternatives` pass the #686 gate rules**, an uncovered removal keeps the capability, logs by code only); `tests/Encina.GuardTests/Compliance/OPA/OPAComplianceResolverGuardTests.cs`.
7. `.github/coverage-manifest/Encina.Compliance.OPA.json`: unit and guard targets for the client, the resolver and the log messages.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2b</summary>

```text
CONTEXT:
- Phases 1 and 2a are merged. Plan Design Choices 3, 5, 8 and 10.

TASK:
Register ComplianceOPA = (6620, 6639), add log messages 6620-6624, implement OPAClient and OPAComplianceResolver with
their tests and manifest targets. 11 files; at most 15 files and 1500 lines.

KEY RULES:
- Register the range in EventIdRanges.cs before any [LoggerMessage]; ids packed from 6620.
- Any failure follows Design Choice 3; no stale-cache answer; caller cancellation propagates.
- Alternatives only from ComplianceCoveringRegulationRegistry; an uncovered removal keeps the capability.
- The subject id and Metadata never reach a log, a tag or an exception message. No metrics yet (Phase 4).

REFERENCE FILES:
- src/Encina.Compliance.Applicability/Resolvers/RegionBasedComplianceResolver.cs (#686, registry-backed removal)
- src/Encina/Compliance/ComplianceCoveringRegulationRegistry.cs (#686)
- src/Encina/Diagnostics/EventIdRanges.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

---

### Phase 3: Resilience, Registration and Health Check

> **Goal**: The package registers with one call and is resilient and checkable. **Size**: 9 files, about 700 lines.

<details>
<summary>Tasks</summary>

1. `ServiceCollectionExtensions.cs`: `AddEncinaComplianceOPA(this IServiceCollection, Action<OPAComplianceOptions>)`: options and validator (`ValidateOnStart`), `TryAddSingleton(TimeProvider.System)`, the named client `Encina.Compliance.OPA` with `AddResilienceHandler` (Design Choices 4 and 9: total timeout, retry, circuit breaker, per-attempt timeout), `OPADecisionCache`, `TryAddSingleton<IOPADecisionMapper, OPADecisionMapper>()`, `OPAComplianceResolver` as singleton (`TryAdd`), the health check.
2. Circuit state is per container (singleton), never static; circuit open and close events are logged (6625 `CircuitOpened` Warning, 6626 `CircuitClosed` Information, added to `Diagnostics/OPALogMessages.cs`); they are counted from Phase 4.
3. `Health/OPAHealthCheck.cs`: per Design Choice 11 (A: `GET /health` reachable); `DefaultName` const, static `Tags`; 6627 `HealthProbeFailed` (Warning).
4. `src/Encina.Compliance.OPA/PublicAPI.Unshipped.txt`.
5. Tests: `tests/Encina.UnitTests/Compliance/OPA/OPARegistrationTests.cs` (DI test with `ValidateOnBuild` and `ValidateScopes` for standalone `UseResolver<OPAComplianceResolver>()` and composite use, in both registration orders; an application's own `IOPADecisionMapper` is not overridden), `OPAResilienceTests.cs` (fake handler: retry count, circuit opens at the threshold, timeout, with `FakeTimeProvider`), `OPAHealthCheckTests.cs`; `tests/Encina.GuardTests/Compliance/OPA/OPARegistrationGuardTests.cs`.
6. `.github/coverage-manifest/Encina.Compliance.OPA.json`: targets for the registration, the health check and the edited log messages.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT:
- Phases 1-2b merged. ADR-055: resilience through Microsoft.Extensions.Http.Resilience on the named client (Design Choice 4).

TASK:
Implement AddEncinaComplianceOPA, the resilience handler, the circuit log events 6625-6626, OPAHealthCheck with 6627
and their tests and manifest targets. 9 files; at most 15 files and 1500 lines.

KEY RULES:
- Registration completeness: options, validator, client, cache, mapper, resolver and health check, proven by a DI test with ValidateOnBuild and ValidateScopes.
- No static circuit state; retry only the idempotent query; timeouts from FakeTimeProvider-driven tests, no Thread.Sleep.
- Phases 2-3 only log; counters arrive in Phase 4.

REFERENCE FILES:
- src/Encina.Refit/ServiceCollectionExtensions.cs (named client with a resilience handler)
- src/Encina.Compliance.DPIA/Health/DPIAHealthCheck.cs
- docs/architecture/adr/055-resilience-family.md
```

</details>

---

### Phase 4: Observability

> **Goal**: Requests, cache, circuit and failures are visible without personal data. **Size**: 6 files, about 450 lines.

<details>
<summary>Tasks</summary>

1. `Diagnostics/OPADiagnostics.cs`: `ActivitySource` and `Meter` both named `Encina.Compliance.OPA` (ADR-054); instruments: `encina.compliance.opa.requests` (Counter, tags `status`, `policy_path`), `encina.compliance.opa.request.duration` (Histogram, unit `s`), `encina.compliance.opa.cache.hits`, `encina.compliance.opa.cache.misses`, `encina.compliance.opa.circuit.opened`, `encina.compliance.opa.failures` (Counters, the last with tag `error_code`), `encina.compliance.opa.decision.capabilities` (Counter, tag `capability`).
2. Spans `encina.compliance.opa.evaluate` (`opa.policy_path`, `opa.cached`) and `encina.compliance.opa.http_request` (`http.request.method`, `http.response.status_code`); no endpoint, no URL, no subject id; tenant tag only when multi-tenancy is on; zero cost without listeners.
3. Edits: `OPAComplianceResolver.cs`, `OPAClient.cs` and `ServiceCollectionExtensions.cs` (circuit counter) record the instruments.
4. Tests: `tests/Encina.UnitTests/Compliance/OPA/OPADiagnosticsTests.cs` (in-memory exporter for spans and counters; no tag or log carries the subject id, endpoint or header values).
5. `.github/coverage-manifest/Encina.Compliance.OPA.json`: targets for the diagnostics and the edited files.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT:
- Phases 1-3 merged. ADR-054 fixes names and tenant tagging.

TASK:
Add OPADiagnostics (source, meter, instruments, spans) and wire it into the resolver, client and circuit events, with
tests and manifest targets. 6 files.

KEY RULES:
- Lowercase dotted names, no _total/_count suffix, durations in seconds.
- Never tag the endpoint, URL, header values or subject id.

REFERENCE FILES:
- src/Encina.Compliance.DPIA/Diagnostics/DPIADiagnostics.cs
- docs/architecture/adr/054-observability-conventions-and-trace-propagation.md
```

</details>

---

### Phase 5: Docker Service, OPA Fixture and Example Policies

> **Goal**: A real OPA can run locally and in the integration tests. **Size**: 9 files, about 600 lines (14 with Design Choice 13 B).

<details>
<summary>Tasks</summary>

1. `docker-compose.yml`: service `opa` (pinned stable image, port 8181, the example policy directory mounted read-only, `run --server`), in the profiles of Design Choice 12; `docs/infrastructure/docker-infrastructure.md` lists it (docs-writer).
2. `tests/Encina.TestInfrastructure/Fixtures/OPAFixture.cs` (Design Choice 13 A; generic `ContainerBuilder`, the pinned image, wait for `/health`, loads the example policies; `[CollectionDefinition("OPA")]`). With 13 B: `src/Encina.Testing.Testcontainers/OPAContainerFixture.cs`, `ContainerDefaultImages.cs` and `EncinaContainers.cs` edits, its `PublicAPI.Unshipped.txt`, a fixture unit test file and the `.github/coverage-manifest/Encina.Testing.Testcontainers.json` entries (14 files).
3. `docs/examples/opa/`: `gdpr_applicability.rego`, `health_data.rego`, `territorial_removal.rego` (each `package encina.compliance.applicability`, `import rego.v1`, a removal always carries its evidence, no `default` that lowers the profile) and the matching `_test.rego` files (`opa test` is run by the integration job).
4. Request to the orchestrator: add the OPA service and the `opa test` step to the integration job in `.github/workflows/*` (hot spot, not edited by the worker).

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT:
- Phases 1-4 merged. Local services are started with docker compose; non-database fixtures live in tests/Encina.TestInfrastructure (Design Choice 13).

TASK:
Add the OPA compose service, the OPA fixture with its collection, and the example Rego policies with opa test files.
9 files (14 with Design Choice 13 B).

KEY RULES:
- One pinned stable image tag in compose and one in the fixture; no latest.
- Example policies follow the decision shape of the plan; CCPA/LGPD appear only as application-written identifiers.
- Do not edit .github/workflows/*.

REFERENCE FILES:
- docker-compose.yml
- tests/Encina.TestInfrastructure (KafkaFixture, a non-database fixture with its collection)
```

</details>

---

### Phase 6: Integration, Contract, Property, Load and Benchmark Tests

> **Goal**: The test types no earlier phase covers, against a real OPA. **Size**: 7 files, about 1,100 lines. Gate: #686 Phases 3a (service) and 9a (resolver contract suite) are merged.

<details>
<summary>Tasks</summary>

1. Integration (`tests/Encina.IntegrationTests/Compliance/OPA/`, `[Collection("OPA")]` over the shared `OPAFixture`, `[Trait("Category", "Integration")]`): `OPAComplianceResolverIntegrationTests.cs` (example policies against the real server, cache behavior, a removal with and without evidence; the outage case uses a second resolver pointed at a closed local port, so the shared container keeps running and the collection owns its lifecycle); `OPAPipelineIntegrationTests.cs` (the pipeline with the #686 service and gate: an accepted removal with a `MapCountry` declaration and the operation-audit recorder on the InMemory store writes a skip record with evidence and alternative and no subject id; without the declaration nothing is skipped).
2. Contract: `tests/Encina.ContractTests/Compliance/Applicability/OPAComplianceResolverContractTests.cs` derives the #686 Phase 9a suite and runs it against `OPAComplianceResolver` with a fake handler.
3. Property (FsCheck through `Encina.Testing.FsCheck`): `tests/Encina.PropertyTests/Compliance/OPA/OPAMappingPropertyTests.cs`: the decision mapping is total (every JSON gives `Right` or a coded `Left`, never an exception) and the input never contains the subject id.
4. Load: `tests/Encina.LoadTests/Compliance/OPA/OPAComplianceResolverLoadTests.cs` (concurrent requests, cache effectiveness and circuit behavior; run locally with `DOTNET_JitObjectStackAllocationConditionalEscape=0`). Benchmark: `tests/Encina.BenchmarkTests/Compliance/OPAComplianceResolverBenchmarks.cs` (cache hit and miss paths and mapper cost; `BenchmarkSwitcher`, results to `artifacts/performance/`).
5. `.github/coverage-manifest/Encina.Compliance.OPA.json`: contract, property and integration targets with justifications; CRAP of every method of the package at most 10, measured locally.

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT:
- Phases 1-5 merged; #686 Phases 3a and 9a merged. Tests execute real code; integration tests use shared collection fixtures.

TASK:
Write the integration, contract, property, load and benchmark tests, raise the contract, property and integration
targets, and verify per-flag coverage and CRAP. 7 files.

KEY RULES:
- [Collection("OPA")] shared fixture; no per-class container; never stop the shared container (the outage case uses a closed port).
- Shouldly through Encina.Testing.Shouldly; no Thread.Sleep; the contract test is never replaced by a .md justification.

REFERENCE FILES:
- tests/Encina.ContractTests/Compliance/Applicability/ (resolver contract suite, #686 Phase 9a)
- tests/Encina.IntegrationTests/ (an existing non-database collection, for example Kafka)
```

</details>

---

### Phase 7: Documentation and Finalization

> **Goal**: The feature, the Rego guide and the package are documented. **Size**: 8 files, about 900 lines.

<details>
<summary>Tasks</summary>

1. XML docs are written with the code in Phases 1-4; this phase checks them. `src/Encina.Compliance.OPA/README.md` completed.
2. `docs/features/compliance-opa-integration.md` and `docs/guides/opa-rego-compliance-policies.md` (standalone, composite and hybrid examples; the Rego writing guide; how a removal needs a `MapCountry` cover), by the docs-writer agent.
3. `changelog.d/688-compliance-opa.added.md`; `docs/INVENTORY.md`, `ROADMAP.md`.
4. ADR for the OPA compliance integration (ADR number assigned when the ADR pull request opens) and its row in `docs/architecture/adr/index.md`: records Design Choices 1 to 4, 6 and 9 to 13.
5. `dotnet build --configuration Release` with 0 warnings; `dotnet test` green. The last pull request carries `Fixes #688`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```text
CONTEXT:
- Phases 1-6 merged. Documentation goes through docs-writer; the changelog fragment through mechanical-fixer.

TASK:
Check the XML docs, complete the README, write the feature page, the Rego guide, the ADR draft with its index row and
the changelog fragment; update INVENTORY and ROADMAP. 8 files.

KEY RULES:
- English only; no private client names (write "the reference application"); coverage figures only through covref markers.

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
| GDPR | Art. 5(1)(c), 5(2) | Data minimisation of the OPA input; accountability through skip records |
| SPEC-002 | REQ-011, REQ-062, DEC-006, INV-005 | Conditional enforcement; instrumentation without personal data; fail closed; unknown means full |
| Open Policy Agent | REST API `/v1/data/{path}`, `/health`; `opa test` | Query, health probe and policy tests (CNCF graduated project) |
| ADR-055, ADR-054, ADR-062 | Resilience family; observability conventions; error categories | Resilience engine, instrument and meter names, error categories |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| Contract (to be added by #686) | `src/Encina/Compliance/` | Resolver interface, `ComplianceContext`, `ComplianceProfile`, evidence, `ComplianceCoveringRegulationRegistry` |
| Resolver package (to be added by #686) | `src/Encina.Compliance.Applicability/` | `UseResolver<T>()`, `CompositeResolverBuilder`, options validator |
| Standard resilience values | `src/Encina.Extensions.Resilience/Configuration/StandardResilienceOptions.cs` | Sampling duration and the rejected option C of Design Choice 9 |
| Named client with resilience | `src/Encina.Refit/ServiceCollectionExtensions.cs` | Registration precedent |
| Non-database test fixtures | `tests/Encina.TestInfrastructure` (`KafkaFixture`) | Pattern for `OPAFixture` |
| Health check precedent | `src/Encina.Compliance.DPIA/Health/DPIAHealthCheck.cs` | `DefaultName`, tags |
| Error factory with category (ADR-062, merged) | `src/Encina/Results/EncinaErrors.cs` | `compliance.opa.*` codes |
| Local services | `docker-compose.yml` | New service `opa` |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Compliance.OPA` | 6620-6639 (`ComplianceOPA`) | New, from the family block 6600-6649 of the plan batch; registered in Phase 2b; 6620-6627 planned, packed (6620-6624 in Phase 2b, 6625-6627 in Phase 3); 6628-6639 free |
| `Encina.Compliance.Applicability` (rules, #687) | 6600-6619 | Owned by the #687 plan |
| `Encina.Compliance.DPIA` screening (#1243) | existing 8897-8899 | Owned by the #1243 plan |

### Estimated File Count

| Pull request | Files | Content |
|--------------|-------|---------|
| Phase 1 | 11 | csproj, `Encina.slnx`, 2 PublicAPI files, README, options, validator, 2 unit test files, 1 guard test file, 1 manifest |
| Phase 2a | 11 | input mapper, mapper interface, default mapper, errors, cache, package PublicAPI, 3 unit test files, 1 guard test file, 1 manifest |
| Phase 2b | 11 | client, resolver, log messages, `EventIdRanges.cs`, core and package PublicAPI (2), architecture-test map, 2 unit test files, 1 guard test file, 1 manifest |
| Phase 3 | 9 | registration, health check, log messages edit, package PublicAPI, 3 unit test files, 1 guard test file, 1 manifest |
| Phase 4 | 6 | diagnostics, resolver, client and registration edits (3), 1 unit test file, 1 manifest |
| Phase 5 | 9 | `docker-compose.yml`, docker docs page, fixture, 3 Rego policies, 3 `_test.rego` files (14 with Design Choice 13 B) |
| Phase 6 | 7 | 2 integration test files, contract, property, load and benchmark tests, 1 manifest |
| Phase 7 | 8 | README, feature page, Rego guide, changelog fragment, ADR, ADR index, `INVENTORY.md`, `ROADMAP.md` |
| **Total** | **72** | 72 file changes in 8 pull requests (a file edited in several pull requests counts in each; 77 with Design Choice 13 B); each pull request at most 15 files and 1500 lines |

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library, pre-1.0, Railway Oriented Programming, no compatibility layers. Rules: AGENTS.md.
- Issue #688 adds Encina.Compliance.OPA on the #686 contract. The Maintainer Decisions of the plan override its recommendations.

IMPLEMENTATION OVERVIEW:
1. Package scaffold, options with secret-safe headers, validator.
2a. Input mapper, IOPADecisionMapper and default, OPAErrors (ADR-062 categories), decision cache.
2b. EventIds 6620-6639 registered, OPA client over a named HttpClient, OPAComplianceResolver (failure per Design Choice 3, alternatives from the registry).
3. AddEncinaComplianceOPA with the resilience handler and OPAHealthCheck.
4. Observability (ADR-054 names).
5. OPA compose service, fixture, example Rego with opa test.
6. Integration, contract, property, load and benchmark tests.
7. Documentation.

KEY PATTERNS:
- Fail closed: timeout, open circuit, HTTP error, undefined result or unmappable decision ends in ComplianceProfile.Full; never a stale or narrower answer.
- The subject id and Metadata never leave the process; the endpoint and header values never reach logs or tags.
- A removal needs evidence matching the context and a ComplianceCoveringRegulationRegistry cover; the service gate of #686 re-checks it.
- DI test with ValidateOnBuild and ValidateScopes; TimeProvider for time; shared [Collection] fixtures for integration tests.
- Each pull request enters the coverage targets of its own files with the tests that meet them.

REFERENCE FILES:
- the #686 plan (merged on main)
- src/Encina/Diagnostics/EventIdRanges.cs
- tests/Encina.TestInfrastructure (KafkaFixture)
- docker-compose.yml
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ | Phase 2a: decision cache with TTL and cap; where it lives is Design Choice 2 (Dilemma, private in-memory recommended) |
| 2 | OpenTelemetry | ✅ | Phase 4: seven instruments and two spans, ADR-054 names, no endpoint or subject id |
| 3 | Structured Logging | ✅ | Phase 2b: `[LoggerMessage]` 6620-6624 in `ComplianceOPA`, registered in the same pull request; Phase 3: 6625-6627 |
| 4 | Health Checks | ✅ | Phase 3: `OPAHealthCheck` per Design Choice 11 |
| 5 | Validation | ✅ | Phase 1: options validator with `ValidateOnStart` |
| 6 | Resilience | ✅ | Phase 3: timeout, retry, circuit breaker on the named client (Design Choices 4 and 9) |
| 7 | Distributed Locks | ❌ | Read-only policy evaluation |
| 8 | Transactions | ❌ | No state change |
| 9 | Idempotency | ❌ | Evaluation is a pure query |
| 10 | Multi-Tenancy | ✅ | Phase 2a: tenant id, country and regulations in the input and the cache key, two-tenant cache test; Phase 4: tenant tag only when tenancy is on |
| 11 | Module Isolation | ✅ | Phase 2a: `module` in the input so a policy can scope by module |
| 12 | Audit Trail | ✅ | Through the skip records of #686 (Phase 6 integration test); the policy path is part of `ResolvedBy`; the package writes none itself |

---

## Prerequisites & Dependencies

### Required Prerequisites

- #686 Phases 1a-2d merged (contract, `Encina.Compliance.Applicability`, composite resolver, options validator, covering-regulation registry) before Phase 1; #686 Phases 3a (service and gate) and 9a (resolver contract suite) before Phase 6.
- The orchestrator adds the OPA service and `opa test` step to the integration workflow (Phase 5 request).

### Recommended (Not Blocking)

- #2700 (ordered before #688 in v0.17.0) and #687 (the DSL; same profile output, composes through the composite).
- #1233 (`Encina.Http`, open): with Design Choice 4 A, the named client moves onto it when it ships; follow-up issue to be opened (draft: artifacts/decisions/plan-batch/followups/688-opa-client-on-encina-http.md).

---

## Next Steps

1. Link this plan from #688 and from #686 and #687.
2. When #688 is picked up, re-check each Design Choice against the code of #686 as merged; present the Dilemmas to the maintainer and record the answers in `## Maintainer Decisions`.
3. Ship Phases 1, 2a, 2b, 3, 4, 5, 6 and 7 as eight pull requests, one per phase, each referencing #688; the last one carries `Fixes #688`.
