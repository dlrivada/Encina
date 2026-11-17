# Implementation Plan: `Encina.Aspire.Hosting` — `EncinaResource`, `AddEncina` and `WithEncina` for the Aspire AppHost

> **Issue**: [#416](https://github.com/dlrivada/Encina/issues/416)
> **Type**: Feature
> **Complexity**: Medium (6 phases, 3 pull requests of 14, 8 and 5 files)
> **Estimated Scope**: ~450 lines of production code + ~900 lines of tests
> **Family**: Aspire (aspire-1): #440 umbrella and naming owner, #417 service defaults, #419 dashboard commands, #421 deployment; modules in the AppHost are #423; the family continues in #424, #425, #382, #387 and #391. **This plan owns the hosting package, `EncinaResource`, the AppHost options and the environment-variable contract that #417 reads.**

---

## Summary

Create `Encina.Aspire.Hosting`, the AppHost-side package of the Aspire integration. State of `origin/main` (`ec129cae5`), verified by reading and grep:

| Item of the issue | Today | Owner of what remains |
|-------------------|-------|-----------------------|
| `Encina.Aspire.Hosting` package | Missing; `src/Encina.Aspire.Testing` is the only Aspire package and it is a testing helper | **This plan** |
| `Aspire.Hosting` package reference | Not pinned: `Directory.Packages.props` pins only `Aspire.Hosting.Testing` and `Aspire.Hosting.PostgreSQL`, both 13.6.5 (the first release patched against GHSA-f7vh-h5g3-7jrh, #3491, closed) | **This plan** pins `Aspire.Hosting` 13.6.5 |
| `AddEncina()` on the builder, `EncinaResource` | Missing | **This plan** |
| `WithEncina()` on a resource, configuration propagation | Missing | **This plan** |
| Health check registration | `AddEncinaOutbox`, `AddEncinaInbox`, `AddEncinaSaga`, `AddEncinaScheduling` exist in `src/Encina.AspNetCore/Health/HealthCheckBuilderExtensions.cs`; probes are #454 | #417 (registers them in the application); **this plan** adds the HTTP health check to the Aspire resource |
| Custom dashboard commands | Missing | #419 |
| Documentation with examples | Missing | **This plan** (package README) and #440 (quickstart) |

Differences from the issue text, found while reading the code and the Aspire package:

- **Aspire 13, not 9.4.** The repository pins 13.6.5 (`Directory.Packages.props`); the `WithCommand(name, displayName, executeCommand, updateState, iconName)` shape of the sibling issue #419 is replaced by the `CommandOptions` overload in `Aspire.Hosting` 13.6.5 (read in `Aspire.Hosting.xml`).
- **The AppHost cannot use `MessagingConfiguration`.** `config.UseOutbox = true` (`src/Encina.Messaging/MessagingConfiguration.cs:87`) configures the application in code; an AppHost references the application only through generated `Projects.*` metadata. The package therefore has its own `EncinaHostingOptions` with the same member names, and a contract test keeps the names equal (Design Choice 4). The package takes no reference to `Encina`, the same rule as #423 for module names.
- **Environment variables declare, they do not enable.** `MessagingConfiguration` has no `IConfiguration` binding; a pattern is enabled by code in the application. The variables set by `WithEncina` tell the application's service defaults (#417) which patterns the AppHost expects, so they can register the matching health checks and fail the start when a declared pattern is absent (Design Choice 3).
- **`WithReference(encina)` is not possible** for a model-only resource: the `WithReference` overloads of `Aspire.Hosting` 13.6.5 take a connection string resource, a service-discovery resource, an `EndpointReference`, a URI or an external service (Design Choice 2).

**Delivered**: package `Encina.Aspire.Hosting`; `EncinaResource` (`Resource`, model-only); `EncinaHostingOptions` (`UseOutbox`, `UseInbox`, `UseSagas`, `UseScheduling`, `UseDeadLetterQueue`, `HealthCheckPath`); `AddEncina(this IDistributedApplicationBuilder, string name, Action<EncinaHostingOptions>? configure = null)`; `WithEncina<T>(this IResourceBuilder<T>, IResourceBuilder<EncinaResource>)` for `T : IResourceWithEnvironment, IResourceWithEndpoints` (a project, a container or an executable, the same constraint #423 uses): it sets the `Encina__Aspire__*` environment variables, adds the resource relationship and adds an HTTP health check at `HealthCheckPath` (default `/health`, null for a worker without an HTTP endpoint).

**Not in scope**: commands (#419), service defaults and telemetry (#417), publishers (#421), modules (#423), Azure Container Apps (#2698, outside 1.0 by SPEC-000 section 6), a `WithReference`-style connection string (Design Choice 2).

**Shared pieces and owners** (so the sibling family aspire-2 reuses them):

| Piece | Owner | Reused by |
|-------|-------|-----------|
| `Encina.Aspire.Hosting` project, `Aspire.Hosting` pin | #416 | #419, #421, #423, #424, #387, #391 |
| `EncinaResource`, `AddEncina`, `WithEncina` | #416 | all |
| Environment-variable contract (`Encina__Aspire__Name`, `Encina__Aspire__Patterns__{Outbox,Inbox,Sagas,Scheduling,DeadLetterQueue}`), `EncinaAspireEnvironment` constants | #416 (writer); #417 (reader); #440 (the parity contract test) | #425, #382 |
| Naming rule (`AddEncina*` on the builder, `WithEncina*` on a resource) | #440 | all |

**Affected packages**: `Encina.Aspire.Hosting` (new). `Encina` and `Encina.Messaging` are test references only.

**Provider category**: none (AppHost modeling; no store, transport or lock is added).

---

## Design Choices

<details>
<summary><strong>1. Entry points: one resource, then attach it (Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `AddEncina(name, configure)` creates the shared model resource; `WithEncina(encina)` attaches it to any number of resources** | One way to do it; the resource is one row in the dashboard with its dependents; the settings are written once | Two calls for the simplest case |
| **B) `WithEncina(configure)` only, on each resource (the shape of the #416 snippet), no resource** | One call; no model resource | Settings repeated for each service; no resource to show in the dashboard; #440's `EncinaResource` first-class requirement is not met |
| **C) A and B together, plus a fluent `WithOutbox()` set on the resource builder (the #440 snippet)** | Every snippet of the issues works | Three ways to express one thing |

### Chosen Option: **A — `AddEncina` then `WithEncina(encina)`** (recommended, pending the maintainer)

### Rationale

A wins when several services share the same patterns and the dashboard should show the dependency (#440 acceptance: `EncinaResource` as a first-class resource). B wins for a single-service solution that wants one line. C is rejected: one way to do a thing keeps the API small, and AGENTS.md section 3 forbids compatibility aliases. The pattern flags are written like the application's own code, `configure: o => o.UseOutbox = true`, not as a second fluent vocabulary.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Shape of `EncinaResource` (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `EncinaResource : Resource`, model-only: it carries the options and nothing is started; applications get the declaration through `WithEncina(encina)`** | Honest: Encina is a library inside the application, not a process; no invented connection string | `WithReference(encina)` as written in #440 does not compile |
| **B) `EncinaResource : Resource, IResourceWithConnectionString`, the connection string being the one of the state store the patterns use** | `WithReference(encina)` works and carries the store connection | Encina has several stores (one per pattern, one per provider) and a tenant database per tenant; one connection string is a wrong simplification; the store resource already supports `WithReference(postgres)` |
| **C) `EncinaResource` runs a real process (a processor host)** | Dashboard state is real | Encina ships no standalone processor executable: processors are hosted services of the application (`OutboxProcessorBase : BackgroundService`, `src/Encina.Messaging/Outbox/OutboxProcessorBase.cs`) |

### Chosen Option: **A — model-only resource** (recommended, pending the maintainer)

### Rationale

A wins because the store connection strings are already delivered by `WithReference(<store>)` on the project, the way every Aspire integration does; B wins only if the maintainer wants a single reference to stand for patterns plus one store, accepting that a multi-store application cannot express itself. C is rejected by the code: there is no processor executable to run. The resource shows in the dashboard through relationships (`WithRelationship(resource, "Encina")`, present in `Aspire.Hosting` 13.6.5) and a published state through `ResourceNotificationService.PublishUpdateAsync`, whose use is verified at Phase 1.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. What the environment variables mean (Dilemma D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A declaration: `Encina__Aspire__Patterns__Outbox=true` tells the application's service defaults (#417) which patterns to expect; the application still enables each pattern in code (`config.UseOutbox = true`) and the host fails to start when a declared pattern has no store** | No change in core or in any provider; pay-for-what-you-use stays in the application; a mismatch is a visible start failure (gates fail closed, AGENTS.md section 3) | The pattern is declared in two places (AppHost and code), kept honest by the start check |
| **B) An instruction: core binds `Encina:Messaging:*` from `IConfiguration` into `MessagingConfiguration`, so the AppHost enables the pattern** | One place to declare | A change to core and to the registration of every provider (10 database providers) for a developer-experience feature; patterns would turn on from a variable, against opt-in in code |
| **C) Nothing is propagated; only the health check and the relationship are modeled** | Smallest | The issue's "configuration propagation" is dropped, and #417 has nothing to decide its health checks from |

### Chosen Option: **A — a declaration with a start check** (recommended, pending the maintainer)

### Rationale

A wins when the AppHost should document the architecture and catch drift between it and the code. B wins if the maintainer wants the AppHost to be the single source of truth and accepts a core change (it would be its own plan). C is the smallest delivery and loses the typed configuration the issue's alternatives section asks for.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Options mirror `MessagingConfiguration` by name, with a parity test (Dilemma D4)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `EncinaHostingOptions` declares its own five booleans named as the members of `MessagingConfiguration` (`UseOutbox` at `:87`, `UseInbox` `:97`, `UseSagas` `:106`, `UseScheduling` `:131`, `UseDeadLetterQueue` `:176`); a unit test in a project that references both asserts the names are equal** | The AppHost does not load the application runtime; the names cannot drift unnoticed | Five properties are written twice |
| **B) The package references `Encina.Messaging` and reuses `MessagingConfiguration`** | No duplication | An AppHost would load the messaging assembly and its dependencies (stores, `LanguageExt`) only to read five booleans; `MessagingConfiguration` has dozens of provider-specific members unrelated to the AppHost |

### Chosen Option: **A — own options, parity test** (recommended, pending the maintainer)

### Rationale

A wins because the AppHost stays a small, fast process that does not load application code (the same reason #423 passes module names, not types). B wins if `MessagingConfiguration` is split into a small flags class that both sides can reference, which is a separate refactoring.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. The Aspire version of the package</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Aspire.Hosting` 13.6.5, the version the other Aspire packages of the repository use** | The first release patched against GHSA-f7vh-h5g3-7jrh; one version across `Aspire.Hosting.Testing`, `Aspire.Hosting.PostgreSQL` and the new package | None found |
| **B) The 9.4 of the issue text, or 13.5.4** | None | 13.5.4 and older are the vulnerable releases; restore fails on them (#3491) |

### Chosen Option: **A — 13.6.5**

### Rationale

`Directory.Packages.props` carries the comment that 13.6.5 is the first release patched against GHSA-f7vh-h5g3-7jrh (#3491). The newest release on NuGet at the time of writing is also 13.6.5.

**Class:** Settled
**Settled by:** `Directory.Packages.props` (the Aspire comment citing GHSA-f7vh-h5g3-7jrh and #3491); issue #3491 (closed).

</details>

<details>
<summary><strong>6. Default path of the HTTP health check (Dilemma D6)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `/health`, the endpoint of the Aspire service-defaults template, overridable with `HealthCheckPath`; null adds no check (a worker without an endpoint)** | Works with an unmodified template project; the same path #417 keeps | The probe endpoints of #454 (`/health/live`, `/health/ready`, `/health/startup`) are not the default |
| **B) `/health/ready`, the readiness probe of #454** | Matches the Kubernetes probe family | Only exists when the application calls `MapEncinaHealthChecks` (#454, open); a template project would show the resource unhealthy |

### Chosen Option: **A — `/health`** (recommended, pending the maintainer)

### Rationale

A wins today because the template maps `/health` and `/alive` (the issue #417 lists the same two) and the dashboard turns a 404 into an unhealthy resource, which would look like an Encina fault. B wins once #454 is the standard of the whole family; changing the default is one constant.

**Class:** Dilemma

</details>

---

## Implementation Phases

### Phase 1: Package, options and `EncinaResource` (pull request 1)

> **Goal**: The package builds, pins `Aspire.Hosting`, and `AddEncina` adds a validated model resource.

<details>
<summary>Tasks</summary>

1. `src/Encina.Aspire.Hosting/Encina.Aspire.Hosting.csproj` (`PackageId` `Encina.Aspire.Hosting`, `GenerateDocumentationFile`, `PackageReference Aspire.Hosting`, no `ProjectReference` to any Encina package, `InternalsVisibleTo` for `Encina.UnitTests`, `Encina.GuardTests`, `Encina.ContractTests`, the PublicAPI analyzer and the two PublicAPI files), the pin `<PackageVersion Include="Aspire.Hosting" Version="13.6.5" />` next to the other Aspire pins in `Directory.Packages.props`, and the project entry in `Encina.slnx`.
2. `EncinaResource.cs` (public sealed, `Resource`): constructor `EncinaResource(string name, EncinaHostingOptions options)` with `ArgumentException.ThrowIfNullOrWhiteSpace(name)` and `ArgumentNullException.ThrowIfNull(options)`; property `Options`.
3. `EncinaHostingOptions.cs` (public sealed): `UseOutbox`, `UseInbox`, `UseSagas`, `UseScheduling`, `UseDeadLetterQueue` (all `false`) and `HealthCheckPath` (`string?`, default `"/health"`); an internal `Validate()` that rejects a non-null `HealthCheckPath` that is blank, does not start with `/` or contains whitespace, with an `InvalidOperationException` that names the property (no secret is ever held by this type, so no `ToString` override is needed).
4. `EncinaAspireEnvironment.cs` (internal static): the constants `Prefix = "Encina__Aspire__"`, `Name = "Encina__Aspire__Name"` and `PatternKey(string pattern)` returning `Encina__Aspire__Patterns__{pattern}`; the five pattern names as constants.
5. `EncinaDistributedApplicationBuilderExtensions.AddEncina(this IDistributedApplicationBuilder builder, string name, Action<EncinaHostingOptions>? configure = null)`: guards, runs `configure` on a new options instance, validates, adds the resource with `builder.AddResource(...)` and returns `IResourceBuilder<EncinaResource>`; a second resource with the same name is Aspire's own duplicate-name error.
6. Tests: unit `EncinaAspireHostingAddEncinaTests` over `DistributedApplication.CreateBuilder()` inspecting the model (no start, no container runtime): the resource exists, options are applied, an invalid path throws naming `HealthCheckPath`; guard `EncinaAspireHostingGuardTests`; the project references of `Encina.UnitTests.csproj` and `Encina.GuardTests.csproj` to the new project (two csproj edits); `PublicAPI.Unshipped.txt` and `.github/coverage-manifest/Encina.Aspire.Hosting.json` through `mechanical-fixer`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT:
- Issue #416; plan docs/plans/aspire-hosting-implementation-plan-416.md. D1 to D4 and D6 are recommended, pending the maintainer: follow the Maintainer Decisions section if it exists. Aspire.Hosting is pinned at 13.6.5 (D5).
- Encina.Aspire.Testing (src/Encina.Aspire.Testing) is the only Aspire package today; it shows the csproj conventions of the repository.

TASK:
Create the package, EncinaHostingOptions, EncinaResource, EncinaAspireEnvironment and AddEncina with their tests.

KEY RULES:
- No ProjectReference to any Encina package; names only (the AppHost does not load application code).
- Fail closed on input: blank name, null options and an invalid HealthCheckPath throw at AppHost build time, never at run time.
- Never put a connection string or secret into an environment value of this feature.
- XML docs with examples on every public member; PublicAPI.Unshipped.txt; zero warnings.
- Before using an Aspire member, confirm it in Aspire.Hosting.xml of the pinned version (AddResource, Resource, IResourceBuilder).

REFERENCE FILES:
- src/Encina.Aspire.Testing/Encina.Aspire.Testing.csproj
- src/Encina.Messaging/MessagingConfiguration.cs
- Directory.Packages.props
- tests/Encina.UnitTests/Aspire
```

</details>

---

### Phase 2: `WithEncina` (pull request 2)

> **Goal**: A project, container or executable resource gets the declaration, the relationship and the health check.

<details>
<summary>Tasks</summary>

1. `EncinaResourceBuilderExtensions.WithEncina<T>(this IResourceBuilder<T> builder, IResourceBuilder<EncinaResource> encina)` with `where T : IResourceWithEnvironment, IResourceWithEndpoints`: guards; sets the environment `Encina__Aspire__Name` (the resource name) and the five `Encina__Aspire__Patterns__*` values, explicit `true` or `false` (so a `false` overrides a value from a configuration file); `WithRelationship(encina.Resource, "Encina")`; when `HealthCheckPath` is not null, `WithHttpHealthCheck(() => builder.GetEndpoint("http"), path)` using the overload that takes a `Func<EndpointReference>` so the endpoint is resolved when the resource starts, not when the extension is called. A second `WithEncina` on the same resource throws `InvalidOperationException` (the precedent is `WithEncinaTestSupport`, which throws on a second call, `src/Encina.Aspire.Testing/DistributedApplicationTestingBuilderExtensions.cs`).
2. Tests: unit (environment values for each pattern combination, relationship present, health check annotation present with the default path and absent when the path is null, a container and an executable resource accepted, second call throws), guard, and contract `EncinaHostingOptionsParityTests` in `tests/Encina.ContractTests/Aspire/` (the five names of `EncinaHostingOptions` equal the five `Use*` members of `MessagingConfiguration`; `Encina.ContractTests.csproj` gains a reference to the new project, one csproj edit).
3. `PublicAPI.Unshipped.txt` and the manifest entries through `mechanical-fixer`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT:
- Issue #416, Phase 1 is merged: the package, EncinaResource, EncinaHostingOptions and AddEncina exist. D1 (attach with WithEncina(encina)), D3 (declaration, not instruction) and D6 (default /health) apply.

TASK:
Add WithEncina with its tests and the parity contract test.

KEY RULES:
- The environment keys come only from EncinaAspireEnvironment; #417 reads the same keys (its plan names the reader).
- The health check endpoint is resolved lazily through the Func<EndpointReference> overload; a worker with HealthCheckPath = null gets no check.
- Verify WithRelationship, WithHttpHealthCheck and the generic constraint against Aspire.Hosting.xml 13.6.5 before writing the code.
- Guard tests for null builder and null encina; a nullable argument gets a valid-result test.

REFERENCE FILES:
- src/Encina.Aspire.Hosting (Phase 1)
- src/Encina.Aspire.Testing/DistributedApplicationTestingBuilderExtensions.cs
- tests/Encina.UnitTests/Aspire
```

</details>

---

### Phase 3: Cross-Cutting Integration

<details>
<summary>Tasks</summary>

1. Health checks (function 4): Phase 2 (the HTTP check on the resource); the application side is #417.
2. Validation (function 5): names, options and the health path are validated when the AppHost is built.
3. Deferred, with an owner: telemetry and logging (#417 for the application, #419 for commands), modules in the AppHost (#423), deployment (#421).
4. Failure codes: this package creates no `EncinaError` (an AppHost has no Encina runtime), so ADR-062 has nothing to categorize; every failure is an exception at AppHost build time that carries the property name, never a value.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT:
- Issue #416; the cross-cutting matrix is the contract.

TASK:
Verify each included row exists in code and tests.

KEY RULES:
- No telemetry and no tenant data are emitted by this package; the tenant-tag rule of ADR-054 D4 and the reserved default tenant segment do not apply.
- Fail closed: an invalid declaration stops the AppHost build; nothing is ignored silently.

REFERENCE FILES:
- AGENTS.md sections 3 and 6
```

</details>

---

### Phase 4: Observability

<details>
<summary>Tasks</summary>

1. No `ActivitySource`, `Meter` or `[LoggerMessage]` class: an AppHost has no application logger at build time, and the package starts no work. The EventId ranges of the family are reserved by #417 (`AspireServiceDefaults = (800, 819)`) and #419 (`AspireHosting = (820, 839)`); this plan uses none.
2. The resource appears in the dashboard through its relationship; its published state is verified in Phase 1.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT:
- Issue #416. EventIds 800-849 are the block of the Aspire family; this package takes none.

TASK:
Confirm the package declares no log class and no instrument.

KEY RULES:
- Do not add a [LoggerMessage] without a registered range (ADR-021).

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

---

### Phase 5: Testing

<details>
<summary>Tasks</summary>

1. **Unit, Guard, Contract** as in Phases 1 and 2. **Property**: justification file `tests/Encina.PropertyTests/Aspire/Hosting.md` (the logic is a fixed mapping of five booleans). **Integration**: justification `tests/Encina.IntegrationTests/Aspire/Hosting.md` (the package is model-only; starting an AppHost needs the Aspire orchestrator and a container runtime that the integration collections do not provide, and the model is asserted without starting). **Load**: `tests/Encina.LoadTests/Aspire/Hosting.md`. **Benchmark**: `tests/Encina.BenchmarkTests/Aspire/Hosting.md`.
2. **Per-file targets** (measured at work start with `dotnet test tests\Encina.<Flag>Tests --collect "XPlat Code Coverage"` and `.github/scripts/coverage-report.cs`; a target is the measured value or higher; new files have no baseline):

   | File | Unit | Guard | Contract | Property | Integration | Justification |
   |------|:---:|:---:|:---:|:---:|:---:|---------------|
   | `EncinaResource.cs` | 100 | 80 | 0 | 0 | 0 | Constructor guards are reached by guard tests; the unit tests build real resources; no property or integration flag applies |
   | `EncinaHostingOptions.cs` | 100 | 0 | 90 | 0 | 0 | Auto-properties and `Validate()` run in unit tests; the contract test pins the five names; no parameterized public member for guard |
   | `EncinaAspireEnvironment.cs` | 100 | 0 | 0 | 0 | 0 | Constants and one key builder, run through every `WithEncina` test |
   | `EncinaDistributedApplicationBuilderExtensions.cs` | 95 | 90 | 0 | 0 | 0 | Run over the real `DistributedApplication.CreateBuilder()` model with no container runtime |
   | `EncinaResourceBuilderExtensions.cs` | 95 | 90 | 0 | 0 | 0 | Each environment value, the health check and the second-call guard are asserted on the real model |

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT:
- Issue #416. Per-flag coverage per AGENTS.md section 9; targets live in .github/coverage-manifest/Encina.Aspire.Hosting.json.

TASK:
Write the tests, the manifest entries and the four justification files.

KEY RULES:
- Tests execute the real extension methods over the real Aspire model; they never swallow an exception.
- A justification file is allowed for property, integration, load and benchmark only; never for unit, guard or contract.
- Shouldly through Encina.Testing.Shouldly; targets never go down; CRAP <= 10 on every changed method.

REFERENCE FILES:
- tests/Encina.UnitTests/Aspire
- tests/Encina.GuardTests/Aspire
- .github/coverage-manifest/Encina.Aspire.Testing.json
```

</details>

---

### Phase 6: Documentation & Finalization (pull request 3)

<details>
<summary>Tasks</summary>

1. XML docs with examples on every public API; `PublicAPI.Unshipped.txt`.
2. `src/Encina.Aspire.Hosting/README.md` (through `docs-writer`: install, `AddEncina` and `WithEncina` with a postgres and a project, the environment contract table, the limits: model-only resource, declaration not instruction, no `WithReference(encina)`); the feature quickstart is #440. `docs/INVENTORY.md` line for the package.
3. `changelog.d/416-aspire-hosting.added.md` (through `mechanical-fixer`).
4. `dotnet build --configuration Release` with 0 warnings; `dotnet test` with every flag at its target.

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT:
- Issue #416 is implemented and tested.

TASK:
Write the documentation set of the Phase 6 tasks through the specialists that own them.

KEY RULES:
- README and INVENTORY through docs-writer; the fragment and PublicAPI lines through mechanical-fixer; never edit CHANGELOG.md; the reference application is named only as "the reference application".

REFERENCE FILES:
- changelog.d/README.md
- .claude/skills/encina-docs/SKILL.md
- src/Encina.Aspire.Testing/README.md
```

</details>

---

### Proposed split into pull requests (limit 15 files and 1500 lines each)

| PR | Content | Files | Approx. lines |
|----|---------|------:|--------------:|
| 1 | csproj, `Directory.Packages.props` edit, `Encina.slnx` edit, `EncinaResource`, `EncinaHostingOptions`, `EncinaAspireEnvironment`, `EncinaDistributedApplicationBuilderExtensions`, `PublicAPI.Shipped.txt`, `PublicAPI.Unshipped.txt`, manifest, unit and guard test files, two test csproj edits (Unit, Guard) | 14 | 800 |
| 2 | `EncinaResourceBuilderExtensions`, `PublicAPI.Unshipped.txt` edit, manifest edit, unit, guard and contract test files, `Encina.ContractTests.csproj` edit | 7 | 600 |
| 3 | README, `docs/INVENTORY.md` line, changelog fragment, four justification files (property, integration, load, benchmark) | 7 | 280 |

PR 2 needs PR 1; PR 3 is last. No interface changes, so no implementer is touched. A pull request that measures over 15 files or 1,500 lines when it is briefed is split again.

---

## Research

### Relevant Standards & Specifications

| Reference | What it defines | Use in this feature |
|-----------|-----------------|---------------------|
| `AGENTS.md` sections 1, 3 and 6 | Opt-in, fail-closed, 12 functions | D3, validation |
| SPEC-000 section 6 and DEC-009 | Aspire integration that runs locally is in 1.0; cloud services without a free local emulator are out | Scope (Azure Container Apps is #2698) |
| ADR-021 | EventId ranges registered first | Reserved block 800-849, none used here |
| ADR-054 | Observability conventions; tenant tag; exporters outside the library | Not applicable here, stated |
| ADR-062 | Error category contract for `EncinaError` | No `EncinaError` is created, stated |
| `Aspire.Hosting` 13.6.5 (`Aspire.Hosting.xml`) | `AddResource`, `WithEnvironment`, `WithRelationship`, `WithHttpHealthCheck(Func<EndpointReference>, ...)`, `WithCommand` with `CommandOptions` | The AppHost API |
| GHSA-f7vh-h5g3-7jrh and #3491 | `Aspire.Hosting` below 13.6.5 is vulnerable | D5 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `MessagingConfiguration` (`UseOutbox` and siblings) | `src/Encina.Messaging/MessagingConfiguration.cs:87-176` | Names mirrored by `EncinaHostingOptions` |
| `AddEncinaOutbox`, `AddEncinaInbox`, `AddEncinaSaga`, `AddEncinaScheduling` | `src/Encina.AspNetCore/Health/HealthCheckBuilderExtensions.cs` | What the declared patterns turn into in the application (#417) |
| `WithEncinaTestSupport` (second-call guard) | `src/Encina.Aspire.Testing/DistributedApplicationTestingBuilderExtensions.cs` | Convention for the second-call `InvalidOperationException` |
| `Encina.Aspire.Testing` and its manifest | `src/Encina.Aspire.Testing/`, `.github/coverage-manifest/Encina.Aspire.Testing.json` | csproj and manifest conventions |
| Module declaration in the AppHost | #423 | Extends this package (`WithEncinaModules`) |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Aspire.Hosting` | none | An AppHost has no application logger at build time. The family block 800-849 (`artifacts/decisions/plan-batch/eventid-blocks.md`): `AspireServiceDefaults = (800, 819)` by #417, `AspireHosting = (820, 839)` by #419, 840-849 free |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production code | 6 | csproj, resource, options, environment constants, two extension classes |
| Public API and manifests | 3 | Shipped, Unshipped, manifest |
| Tests and project files | 10 | Unit and guard test files of pull requests 1 and 2 (4), contract (1), 3 test csproj edits, props edit, slnx edit |
| Justification files | 4 | Property, integration, load, benchmark |
| Documentation | 3 | README, inventory line, changelog fragment |
| **Total** | **26 distinct files; 28 file touches across 3 pull requests** | `PublicAPI.Unshipped.txt` and the manifest are edited in pull requests 1 and 2; the sum of the rows of the split table (14 + 7 + 7) is the authority |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
PROJECT CONTEXT:
- Encina is a pre-1.0 .NET 10 / C# 14 library; Either<EncinaError, T>; no [Obsolete], no compatibility layers.
- Issue #416: the AppHost package Encina.Aspire.Hosting. Plan: docs/plans/aspire-hosting-implementation-plan-416.md; D1-D4 and D6 are recommended, pending the maintainer.

IMPLEMENTATION OVERVIEW (3 pull requests, each builds alone):
1. Package, Aspire.Hosting 13.6.5 pin, EncinaHostingOptions, EncinaResource, AddEncina (Phase 1).
2. WithEncina: environment declaration, relationship, health check, parity contract test (Phase 2).
3. README, inventory, changelog fragment, justification files (Phase 6).

KEY PATTERNS:
- The package references no Encina package; EncinaHostingOptions mirrors the five Use* names of MessagingConfiguration and a contract test keeps them equal.
- The environment variables Encina__Aspire__Name and Encina__Aspire__Patterns__{Outbox,Inbox,Sagas,Scheduling,DeadLetterQueue} are a declaration read by #417, never an instruction.
- WithEncina is generic over IResourceWithEnvironment and IResourceWithEndpoints; the health check endpoint is resolved lazily; a second call throws.
- Fail at AppHost build time with the property name only; no secret or connection string in any value.

REFERENCE FILES:
- src/Encina.Messaging/MessagingConfiguration.cs
- src/Encina.Aspire.Testing/DistributedApplicationTestingBuilderExtensions.cs
- Directory.Packages.props
- .github/coverage-manifest/Encina.Aspire.Testing.json
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Not applicable: hosting configuration, nothing is read repeatedly |
| 2 | OpenTelemetry | ⏭️ | Deferred as in the issue: the application's telemetry is #417 (open, planned); Aspire already injects the OTLP environment into every project |
| 3 | Structured Logging | ❌ | Not applicable: an AppHost has no application logger at build time; failures are exceptions that carry the property name. Command logging is #419 |
| 4 | Health Checks | ✅ | Phase 2: one HTTP health check on the resource at `HealthCheckPath`; the application side is #417 and probes are #454 |
| 5 | Validation | ✅ | Phase 1: name, options and health path are validated when the AppHost is built |
| 6 | Resilience | ❌ | Not applicable: the package makes no external call |
| 7 | Distributed Locks | ❌ | Not applicable: hosting declaration only |
| 8 | Transactions | ❌ | Not applicable: no operation to make atomic |
| 9 | Idempotency | ✅ | A second `WithEncina` on one resource is rejected, so the declaration is applied once; the environment values are idempotent by construction |
| 10 | Multi-Tenancy | ❌ | Not applicable: no tenant data exists in an AppHost model; the reserved `default` tenant segment and tenant-qualified ids do not apply |
| 11 | Module Isolation | ⏭️ | Deferred as in the issue: #423 adds the module declaration to this package |
| 12 | Audit Trail | ❌ | Not applicable: developer infrastructure with no business operation |

---

## Prerequisites & Dependencies

- None to start: the package depends only on the pinned `Aspire.Hosting`. #423, #419 and #421 add their extensions to this package after pull request 2; #417 reads the environment contract of Phase 2 (the contract is fixed here so both can be built in parallel).
- #440 owns the naming rule and the quickstart; #607 (`Encina.ServiceDiscovery.Aspire`) is independent.

---

## Next Steps

1. The maintainer answers D1, D2, D3, D4 and D6 when work on #416 starts; the answers go into `## Maintainer Decisions` and the markers are removed.
2. Pull request 1, then 2, then 3; #417 can start once the environment keys of pull request 2 are agreed.
