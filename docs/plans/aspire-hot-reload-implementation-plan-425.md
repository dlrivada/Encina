# Implementation Plan: Hot reload for Encina in development — cache invalidation on metadata updates and the AppHost switch

> **Issue**: [#425](https://github.com/dlrivada/Encina/issues/425)
> **Type**: Feature
> **Complexity**: Medium (8 phases, 5 pull requests of 8, 14, 5, 13 and 5 files)
> **Estimated Scope**: ~450 lines of production code + ~1,100 lines of tests
> **Family**: Aspire (aspire-2): #424 external services, #382 service discovery and configuration, #387 Dapr, #391 sidecar. Aspire-1 plans merged on `main` (the plans only: `src/Encina.Aspire.Hosting` and `src/Encina.Aspire.ServiceDefaults` do not exist yet, so pull request 4 starts after their code pull requests merge): #416 (`Encina.Aspire.Hosting`, `EncinaHostingOptions`, the `Encina__Aspire__*` contract), #417 (`Encina.Aspire.ServiceDefaults`, `AddEncinaDefaults`), #440 (naming rules and parity tests). The closed duplicate #442 carried into this issue "state preservation" and "dashboard integration". **This plan owns the `HotReload` folder of the core package, the `UseHotReload` declaration and the `AspireHotReload` EventId range.**

---

## Summary

The issue asks for hot reload of handlers, validators and pipeline behaviors, state preservation, dashboard visibility and development-only activation. State of `origin/main` (`e31de98cb`), verified by reading and grep, and the .NET hot reload contract:

| Item of the issue | Today | Owner of what remains |
|-------------------|-------|-----------------------|
| `[assembly: MetadataUpdateHandler]` and `ClearCache`/`UpdateApplication` | Nothing in `src/` (grep for `MetadataUpdateHandler` finds no match) | **This plan** |
| `EncinaHandlerCache.Clear()`, `EncinaHandlerRegistry`, `EncinaHotReloadNotifier` of the sketch | Do not exist. The real caches are private statics: `Encina.RequestHandlerCache` and `NotificationHandlerInvokerCache` (`src/Encina/Core/Encina.cs:43-44`), `StreamDispatcher.StreamHandlerCache` (`src/Encina/Core/StreamDispatcher.cs:18`), `RequestKindCache` (`src/Encina/Dispatchers/Encina.RequestDispatcher.cs:39`) | **This plan** clears them |
| Pipeline behavior attribute caches | `AuthorizationPipelineBehavior<TRequest,TResponse>` keeps four static caches (`CommandTypeCache`, `AllowAnonymousCache`, `AuthorizeAttributeCache`, `ResourceAuthorizeCache`, `src/Encina.AspNetCore/AuthorizationPipelineBehavior.cs:91-94`); statics of a generic class are per closed type, so they cannot be cleared from outside. `git grep` of `static readonly` `ConcurrentDictionary` or `ConditionalWeakTable` declarations in `src` finds 59, 50 of them outside the five handled here (`MediatorAssemblyScanner.Cache`, `src/Encina/Dispatchers/MediatorAssemblyScanner.cs:11`, is one: it is read at registration only, so a metadata update does not need it cleared; recorded in Design Choice 3) | **This plan** moves the authorization caches to a non-generic holder (Design Choices 4 and 9) |
| Validator reload | `AddEncinaFluentValidation` registers validators as singletons by default and already has an overload with a `ServiceLifetime` (`src/Encina.FluentValidation/ServiceCollectionExtensions.cs:93`) | Design Choice 5 |
| File watcher on `*.dll` | A loaded assembly cannot be replaced in the default load context | Design Choice 2 (not built) |
| State preservation (`Preserve(...)`) | Stores are DI services; a metadata update never recreates them | Design Choice 6 |
| Aspire Dashboard integration | The service defaults of #417 export logs; the AppHost cannot observe the application | Design Choice 7 |
| AppHost `UseHotReload` | Missing; `EncinaHostingOptions` of #416 has no such property | **This plan** |

Differences from the issue text: hot reload in .NET is delivered by the runtime and the tooling (`dotnet watch`, the IDE debugger; `Aspire.Hosting` 13.6.5 only hints `RunConfiguration.WatchEnabled`), which apply method-body and some type edits **in place**. The type identities do not change, so a handler whose body is edited needs no Encina work; what goes stale is what Encina cached from the old shape (dispatcher wrappers, attribute caches) and the services the container never knew (a new handler class). Everything below is therefore about invalidating caches and reporting, not about reloading assemblies.

**Delivered**: in core `Encina` (folder `HotReload/`): an assembly-level `MetadataUpdateHandler` that clears the four dispatcher caches and raises an internal notification; `EncinaHotReloadOptions` and `ConfigureEncinaHotReload(this IServiceCollection, Action<EncinaHotReloadOptions>? = null)`, which registers a development-only coordinator that logs each update and warns about handler types the container does not know; in `Encina.AspNetCore` the authorization caches in a non-generic holder cleared by its own handler; the declaration `UseHotReload` in `EncinaHostingOptions` (#416) written as `Encina__Aspire__HotReload`, read by `AddEncinaDefaults` (#417); log messages 855-860.

**Not in scope**: loading or unloading assemblies, a file watcher, a `Preserve` API, publishing state to the dashboard from the AppHost, changes to Messaging, caching or provider packages (the other static caches of `src/` are listed in the follow-up issue file of this run).

**Affected packages**: `Encina` (core), `Encina.AspNetCore`, `Encina.Aspire.Hosting`, `Encina.Aspire.ServiceDefaults`.

**Provider category**: none (a development-time feature; no store, transport or lock is added).

**Scope check (SPEC-000 section 6, DEC-009)**: hot reload runs locally and is listed as in scope by section 2 ("hot reload" is named among the widened items), so the issue is in 1.0.

---

## Design Choices

<details>
<summary><strong>1. Where the code lives (Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Core `Encina` owns `HotReload/` (the caches are `private static` or `internal`, and core already references `Microsoft.Extensions.Hosting.Abstractions`); `Encina.AspNetCore` has its own handler for its caches; the AppHost switch is in the Aspire packages** | Internal access without widening visibility; hot reload works with `dotnet watch` alone, no Aspire needed; no new package | Core gains a small folder and one more EventId range |
| **B) A new package `Encina.HotReload` reaching core internals through `InternalsVisibleTo`** | Core stays unchanged for users who never develop with hot reload | An `InternalsVisibleTo` to a runtime package for a dev feature; core caches must still gain clearing methods; one more package to ship |
| **C) Everything in `Encina.Aspire.ServiceDefaults`** | Matches the issue's "Encina.Aspire" label | Hot reload would need Aspire; `dotnet watch` users get nothing |

### Chosen Option: **A — core folder plus per-assembly handlers** (recommended, pending the maintainer)

### Rationale

A wins because the work is invalidating core caches and the feature is useful without Aspire; B wins if the maintainer wants core free of any development-time code; C wins only if hot reload is meant for Aspire solutions only. Under B or C the phases move files, not logic.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Mechanism: the runtime's metadata update contract, no file watcher</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `[assembly: MetadataUpdateHandler(typeof(...))]` with static `ClearCache(Type[]?)` and `UpdateApplication(Type[]?)`** | The runtime calls it after every applied update, with the updated types; no polling; works under `dotnet watch`, Visual Studio and the Aspire watch hint | Called only when a hot reload agent applies an update |
| **B) A `FileSystemWatcher` on `*.dll` that re-registers handlers (the issue sketch)** | Looks like a reload | A loaded assembly cannot be replaced in the default `AssemblyLoadContext`; a changed file on disk changes nothing in the running process |

### Chosen Option: **A — the metadata update handler**

### Rationale

The handler is the documented extension point of .NET hot reload (the issue's own references list `MetadataUpdateHandlerAttribute`); a watcher cannot work, so there is no valid second option. Under every option of the Dilemmas below this contract stays the trigger.

**Class:** Settled
**Settled by:** .NET `System.Reflection.Metadata.MetadataUpdateHandlerAttribute` documentation (the contract of `ClearCache` and `UpdateApplication`), referenced by issue #425

</details>

<details>
<summary><strong>3. A new handler class added during the session (Dilemma D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Detect and say so: `UpdateApplication` receives the updated types; for each type that implements a handler interface and has no registration in the container, log a warning "restart required for this handler" (EventId 857) and do nothing else** | Honest; no change to the dispatcher or the container; no lifetime questions | The developer restarts for a new handler class (body edits of existing handlers need nothing) |
| **B) A late-registration table the dispatcher consults when the container has no handler** | New handlers work without a restart | A second lookup path in the hot dispatch path (a miss path only, but still a change to `Encina.cs`); scoped lifetimes and open generics must be re-created by hand |

### Chosen Option: **A — warn, do not register** (recommended, pending the maintainer)

### Rationale

A wins while body edits dominate hot reload sessions, which is what the issue's "handler logic changes frequently" describes; B wins if teams add handler classes constantly and accept a dispatcher change plus its tests. The warning names the type only. `MediatorAssemblyScanner.Cache` (assembly scan results, read only while services are registered) is not cleared under A, because nothing rescans after the container is built; under B it would have to be, since a late-registration table needs a rescan of the updated assembly.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Cache clearing: always present, or only when opted in (Dilemma D4)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The handlers are always compiled in and always clear the caches when the runtime reports an update; the opt-in `ConfigureEncinaHotReload` adds only the coordinator (logs and warnings)** | A stale `[AllowAnonymous]` or `[Authorize]` cache after an edit can never keep an old decision (a fail-open risk otherwise); a handler that the runtime never calls costs nothing | Behavior without an opt-in, though only inside a hot reload session |
| **B) Handlers clear caches only after `ConfigureEncinaHotReload` ran in the application** | Strict pay-for-what-you-use | A developer who forgets the call gets stale authorization decisions after an attribute edit during development |

### Chosen Option: **A — always clear, opt in only for the coordinator** (recommended, pending the maintainer)

### Rationale

A wins because the only time the handler runs is a development hot reload and a stale authorization cache is a security defect, so correctness must not depend on a call being present; B wins if the maintainer reads "every feature opt-in" as covering even an inert handler. The caches are moved to a non-generic holder in `Encina.AspNetCore` because the statics of `AuthorizationPipelineBehavior<TRequest,TResponse>` exist once per closed generic and cannot be reached to clear them. The handlers are not gated by the environment: the runtime calls them only inside a hot reload session (a development agent), and a gate that could be wrong would reintroduce the stale-decision risk. Clearing is `ConcurrentDictionary.Clear()`, which does not throw for these dictionaries, so the handlers carry no `catch`: nothing is swallowed, and an unexpected exception reaches the runtime's own reporting in the developer's session instead of leaving a stale authorization entry silently (fail closed, AGENTS.md section 3).

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Validators (Dilemma D5)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No new API: the guide and the Aspire quickstart show `AddEncinaFluentValidation(assembly, ServiceLifetime.Transient)` in development, so each resolution runs the edited constructor** | Uses the existing overload; nothing to maintain | `ReloadValidatorsOnChange` of the issue sketch is not delivered; DataAnnotations and MiniValidator keep no instance state, so they need nothing |
| **B) `ReloadValidatorsOnChange` rewrites container descriptors whose service type is `FluentValidation.IValidator<>` from singleton to transient** | The sketch API exists | Core cannot reference FluentValidation, so the match is by type name; a fragile rule over a third-party type |
| **C) Put the option in `Encina.FluentValidation` as `AddEncinaFluentValidation(assembly, lifetimeInDevelopment)`** | Typed, no name matching | A second lifetime parameter next to the existing one |

### Chosen Option: **A — use the existing lifetime overload** (recommended, pending the maintainer)

### Rationale

A wins because FluentValidation builds its rules in the validator constructor and a singleton instance never re-runs it; the existing overload already lets a development build choose transient. B wins if the maintainer wants the issue's API exactly; C if typed support is wanted without name matching. The omitted `ReloadValidatorsOnChange` is this Dilemma.

**Class:** Dilemma

</details>

<details>
<summary><strong>6. State preservation (Dilemma D6)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No `Preserve` API: a metadata update never recreates a service, so stores, processors and running sagas are untouched; the plan adds a test that the coordinator and the handlers never dispose or replace a resolved service, and the guide states the guarantee** | The acceptance item is met by construction and proven by a test; no dead options | The sketch's `Preserve(options => ...)` does not exist |
| **B) `Preserve(...)` options that do nothing but document intent** | Matches the sketch | Options with no behavior are dead code (AGENTS.md section 3) |
| **C) Real preservation: snapshot and restore store state across a reload** | Would matter if reload recreated services | Reload does not recreate services; a snapshot mechanism would add risk for no case |

### Chosen Option: **A — preserved by construction, proven by a test** (recommended, pending the maintainer)

### Rationale

A wins because the runtime keeps the process, its container and its hosted services alive; B and C only make sense if some future reload mode restarts them. Running sagas continue with the original handler logic only for the code already executing; the next step of a saga runs the edited body, which the guide states.

**Class:** Dilemma

</details>

<details>
<summary><strong>7. Dashboard integration (Dilemma D7)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Structured logs: each update logs its affected type count and duration (EventIds 855-856), the OTLP log exporter of the Aspire service defaults shows them in the dashboard's structured logs** | No endpoint, no AppHost state, works with the existing export of #417 | Not a dedicated panel |
| **B) A dashboard resource state or command published by the AppHost** | A status next to the resource | The AppHost does not observe the application's process; needs a channel from the application back to the AppHost |
| **C) A new `Meter` counting updates** | Chartable | Requires `AddMeter` in `Encina.OpenTelemetry` for one dev counter; the issue itself marks OpenTelemetry not applicable |

### Chosen Option: **A — structured logs through the existing export** (recommended, pending the maintainer)

### Rationale

A wins because the information is an event with a duration, which a log carries well, and nothing new is exported; B wins if the maintainer wants a visible resource status; C if charts of update counts matter. Logs carry counts, durations and type names, never request data and no tenant or user ids.

**Class:** Dilemma

</details>

<details>
<summary><strong>8. Activation: development only, declared by the AppHost, default off</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `EncinaHostingOptions.UseHotReload` (default false) is written as `Encina__Aspire__HotReload=true` only when `builder.ExecutionContext.IsRunMode`; the application enables the coordinator only when the declaration is true and `IHostEnvironment.IsDevelopment()`; an explicit `ConfigureEncinaHotReload` call also needs `IsDevelopment()`, else it registers nothing** | A publish never carries the variable; production cannot turn the coordinator on by configuration; fails closed when the environment is unknown | Two conditions to explain |
| **B) Enabled by the variable alone** | One condition | A copied variable enables it in production |
| **C) Enabled by default in Development without any call** | Zero setup | Default-on breaks pay-for-what-you-use for the coordinator |

### Chosen Option: **A — run mode and Development, default off**

### Rationale

The issue states "development only"; AGENTS.md section 3 requires gates to fail closed when context is missing, so an unknown environment disables the coordinator, and section 1 makes every feature opt-in. This choice covers only the **coordinator and the AppHost declaration** (logging, warnings, the `OnReload` callback): they are off unless declared or enabled and the host is Development. The cache-clearing handlers are a separate matter, decided in Design Choice 4 (always present, not environment gated). The statement holds under every option of Design Choices 3 to 7 because those change what the coordinator reports, not when it is enabled.

**Class:** Settled
**Settled by:** AGENTS.md section 1 (pay-for-what-you-use, every feature opt-in) and section 3 (gates fail closed); issue #425 acceptance criterion "Development-only activation"

</details>

<details>
<summary><strong>9. Pipeline behaviors beyond authorization (Dilemma D9)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Only the authorization behavior's caches (a security gate) and the dispatcher caches are cleared here; the other static caches are a follow-up issue (59 declarations in `src`, 50 outside the five handled, per `git grep`)** | A bounded plan and the one security-relevant cache covered | The issue's "reload custom pipeline behaviors" criterion is met for the behaviors that cache security decisions only |
| **B) Clear every static cache of every package in this plan** | Meets the criterion fully | About 50 sites over many packages, far over one worker pull request; each package needs its own handler |
| **C) A registry that each cache registers into** | One clear call | Changes every cache site and gives core a registry other packages must call |

### Chosen Option: **A — authorization and dispatcher caches now, the rest by issue** (recommended, pending the maintainer)

### Rationale

A wins when the first delivery must be correct for decisions that gate access and small enough to review; B wins if the maintainer wants full coverage in one program of work; C if one registry is preferred to many handlers. Behavior ordering is preserved without Encina code: a metadata update does not rebuild the pipeline, which the container composed at start.

**Class:** Dilemma

</details>

---

## Implementation Phases

### Phase 1: Core cache invalidation (pull request 1)

> **Goal**: A metadata update clears the four dispatcher caches; nothing else changes.

<details>
<summary>Tasks</summary>

1. `src/Encina/Core/Encina.cs`, `src/Encina/Core/StreamDispatcher.cs`, `src/Encina/Dispatchers/Encina.RequestDispatcher.cs` (edited): an `internal static void ClearHotReloadCaches()` next to each cache (`RequestHandlerCache` and `NotificationHandlerInvokerCache` are in the partial class `Encina`; `RequestKindCache` is in its dispatcher partial; `StreamHandlerCache` in `StreamDispatcher`); nothing else changes.
2. `src/Encina/HotReload/EncinaHotReloadHandler.cs` (internal static): `[assembly: MetadataUpdateHandler(typeof(EncinaHotReloadHandler))]`, `ClearCache(Type[]? updatedTypes)` records `Stopwatch.GetTimestamp()` in a static field and calls the four clearers (each is `ConcurrentDictionary.Clear()`, which does not throw, so there is no `catch` and nothing is swallowed), and `UpdateApplication(Type[]? updatedTypes)` raises the notifier with the recorded start timestamp. The runtime calls `ClearCache` and then `UpdateApplication` as separate callbacks, so the duration of event 855 runs from the timestamp taken in `ClearCache` to the end of the coordinator's handling. The methods take no token.
3. `src/Encina/HotReload/HotReloadNotifier.cs` (internal static): `event Action<HotReloadUpdate>? Updated` (a readonly struct carrying the updated types and the start timestamp); `Raise(Type[]? types, long startTimestamp)` invokes the event. The only subscriber is the coordinator, which catches and logs its own failures (event 860), so the notifier needs no `catch` and no static logger sink exists. The event is process-global: unit tests that subscribe do so through a coordinator instance that unsubscribes on dispose, and the tests of one class never run in parallel with another that raises it (a collection attribute); this limitation is stated in the pull request.
4. `src/Encina/HotReload/CoreHotReloadCaches.cs` (internal static): `Clear()` calling the three clearers, so the handler has one call.
5. Tests: unit tests call `ClearCache` and `UpdateApplication` directly (the runtime contract is two static methods, so a test can run them without a hot reload agent): after one dispatch fills each cache, `ClearCache` empties it and a second dispatch works; a throwing subscriber does not stop the others; `Encina.json` manifest entries through `mechanical-fixer`. No public API, so no PublicAPI lines and no guard test.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT:
- Issue #425; plan docs/plans/aspire-hot-reload-implementation-plan-425.md. D1, D3, D4, D5, D6, D7 and D9 are recommended, pending the maintainer: follow the Maintainer Decisions section if it exists.
- The caches are private statics in src/Encina/Core/Encina.cs (:43-44), src/Encina/Core/StreamDispatcher.cs (:18) and src/Encina/Dispatchers/Encina.RequestDispatcher.cs (:39).

TASK:
Add the four internal clearers, the assembly MetadataUpdateHandler, the notifier and their unit tests.

KEY RULES:
- ClearCache and UpdateApplication are called by the runtime inside the developer's session; they contain no catch (the clears do not throw) and log nothing.
- All new types are internal; no public API change.
- Tests call the two static methods directly; never rely on a real hot reload agent.

REFERENCE FILES:
- src/Encina/Core/Encina.cs
- src/Encina/Dispatchers/Encina.RequestDispatcher.cs
- src/Encina/Core/StreamDispatcher.cs
```

</details>

---

### Phase 2: Options, registration and the coordinator (pull request 2)

> **Goal**: `ConfigureEncinaHotReload` registers a development-only coordinator that logs updates and warns about unregistered handler types.

<details>
<summary>Tasks</summary>

1. `src/Encina/HotReload/EncinaHotReloadOptions.cs` (public sealed): `Enabled` (bool, default `false`: the call alone does not enable it), `WarnOnUnregisteredHandlers` (bool, default `true`), `OnReload` (`Action<EncinaHotReloadEvent>?`), `WatchAssemblies(params Assembly[])` (restricts the warning to those assemblies; null elements rejected). `EncinaHotReloadEvent` (public sealed record): `UpdatedTypeCount`, `DurationMilliseconds`.
2. `src/Encina/HotReload/ServiceCollectionExtensions.cs` (the folder's extension class, `EncinaHotReloadServiceCollectionExtensions`): `ConfigureEncinaHotReload(this IServiceCollection services, Action<EncinaHotReloadOptions>? configure = null)`: guards; registers the options with `AddOptions` and a `TryAddEnumerable` `IHostedService` coordinator; the coordinator does nothing unless `Enabled` is true and `IHostEnvironment.IsDevelopment()` (an unresolved environment counts as not Development: fail closed).
3. `src/Encina/HotReload/HotReloadCoordinator.cs` (internal sealed, `IHostedService`): subscribes to `HotReloadNotifier.Updated` on `StartAsync`, unsubscribes on `StopAsync` and `Dispose`; on an update computes the duration with `Stopwatch.GetElapsedTime(update.StartTimestamp)` (a monotonic interval, not a date, so `TimeProvider` is not involved; the timestamp was taken in `ClearCache`), logs 855 (updated type count, duration), invokes `OnReload` inside `try`/`catch` (a failing callback logs 860 and never escapes), and, when `WarnOnUnregisteredHandlers` is true, resolves `IServiceProviderIsService` and logs 857 for each updated type that implements `IRequestHandler<,>`, `INotificationHandler<>` or the stream handler interface and is not registered.
4. `src/Encina/HotReload/Diagnostics/HotReloadLog.cs` (`[LoggerMessage]`): 855 update applied (Information: count, milliseconds), 856 caches cleared (Debug), 857 new handler type needs a restart (Warning: the type name), 858 coordinator started (Information), 859 coordinator disabled because the environment is not Development (Debug), 860 hot reload step failed (Error: exception through `ForLogging()`). `EventIdRanges.AspireHotReload = (855, 864)` registered first, the `AssemblyRanges` entry of `EncinaEventIdAllocationTests`, the `Encina` PublicAPI lines. `docs/engineering/eventid-block-registry.md` is edited in the same pull request, as `EventIdRegistryAgreementTests` requires: the "Registered ranges" table (header `Name | Block | Area | Zone`) gains `| AspireHotReload | 855-864 | Observability and platform | historical |` (Zone is `historical` for every range below 10000; `reserved-pre-zone` is only a Status of the "Blocks" table), and in the "Blocks" table (header `Block | Area | Owner | Status`) the reserved row 850-899 is replaced by `855-864` with Status `registered` and the remaining ids kept `reserved-pre-zone`. The five aspire-2 plans edit that one shared row: the first to land splits it into one `reserved-pre-zone` row per plan (#424 850-854, #425 855-864, #382 865-877, #387 878-889, #391 890-899, owners `aspire-2: #<n>`); every later pull request changes only its own row, so the edits are disjoint lines and a conflict is resolved by keeping both; recommended merge order #424, #425, #382, #387, #391. Ids 855-860 are packed; 861-864 spare. Every log call is made by the coordinator (856 when the notification arrives, 860 for any failure of its own steps), so the Phase 1 handler and notifier stay internal and log-free, and the log file's methods all have an emitter in the pull request that declares them.
5. No scoped service is injected into the coordinator (a singleton): it takes `IOptions<EncinaHotReloadOptions>`, `IHostEnvironment`, `IServiceProviderIsService`, `ILogger<>` and `TimeProvider` (registered with `TryAddSingleton(TimeProvider.System)`).
6. Tests: unit (disabled outside Development, disabled when `Enabled` is false, enabled path logs and calls `OnReload`, a throwing callback is contained, the warning for an unregistered handler and not for a registered one, unsubscribe on stop); guard (null arguments, null assembly element); a DI test building the provider with `ValidateOnBuild` and `ValidateScopes`; PublicAPI and manifest through `mechanical-fixer`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT:
- Issue #425, Phase 2 (pull request 2). Phase 1 is merged. The Dilemmas of the plan are recommended, pending the maintainer.

TASK:
Write the options, ConfigureEncinaHotReload, the coordinator, HotReloadLog, the EventId range and their tests.

KEY RULES:
- Register EventIdRanges.AspireHotReload first; ids 855-860 packed inside 855-864; update EncinaEventIdAllocationTests.
- The coordinator is a singleton and injects no scoped service; use IServiceProviderIsService for the registration check.
- Disabled unless Enabled and IsDevelopment(); a missing environment disables it.
- Log counts, milliseconds and type names only; exceptions through ForLogging(); never EncinaError.Message; no tenant or user id.
- A callback exception never escapes the coordinator.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
- src/Encina.Messaging/Outbox/OutboxProcessorBase.cs (hosted service conventions)
```

</details>

---

### Phase 3: Authorization caches (pull request 3)

> **Goal**: A metadata update cannot leave a stale authorization decision.

<details>
<summary>Tasks</summary>

1. `src/Encina.AspNetCore/AuthorizationAttributeCaches.cs` (internal static): the four dictionaries moved from `AuthorizationPipelineBehavior<TRequest,TResponse>` (`CommandTypeCache`, `AllowAnonymousCache`, `AuthorizeAttributeCache`, `ResourceAuthorizeCache`) and `Clear()`.
2. `AuthorizationPipelineBehavior.cs` (edited): the behavior reads the caches through the holder; its logic is unchanged.
3. `src/Encina.AspNetCore/HotReload/AspNetCoreHotReloadHandler.cs` (internal static): `[assembly: MetadataUpdateHandler(...)]`, `ClearCache` calls `AuthorizationAttributeCaches.Clear()` (four `ConcurrentDictionary.Clear()` calls, which do not throw), with no `catch`: nothing is swallowed, and an unexpected exception reaches the runtime instead of leaving a stale authorization entry unnoticed (Design Choice 4).
4. Tests: unit (the caches fill, `ClearCache` empties them, an attribute added to a request type is seen after the clear: the test uses two request types to simulate the before and after, since a real edit is not possible in a test); the existing authorization behavior tests must pass unchanged; the AspNetCore manifest entry through `mechanical-fixer`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT:
- Issue #425, Phase 3 (pull request 3). Statics of a generic class are per closed generic, so the four caches move to a non-generic holder.

TASK:
Move the caches, add the AspNetCore handler and the tests.

KEY RULES:
- No behavior change in AuthorizationPipelineBehavior: run its existing tests.
- No catch in the handler: ConcurrentDictionary.Clear does not throw, and an exception must never be swallowed in a security-relevant clear (fail closed).
- Fail closed: an empty cache means the attributes are read again, never that a request is allowed.

REFERENCE FILES:
- src/Encina.AspNetCore/AuthorizationPipelineBehavior.cs (:91-94 and :387)
- tests/Encina.UnitTests (AuthorizationPipelineBehavior tests)
```

</details>

---

### Phase 4: The AppHost switch and the application reader (pull request 4)

> **Goal**: `UseHotReload` in the AppHost enables the coordinator of a Development application.

<details>
<summary>Tasks</summary>

1. `EncinaHostingOptions.cs` (#416, edited): `UseHotReload` (bool, default false). `EncinaAspireEnvironment.cs` (#416, edited): the key `Encina__Aspire__HotReload`. `WithEncina` (edited) writes `true` only when `builder.ExecutionContext.IsRunMode` and `UseHotReload` is set; in publish mode the variable is never written.
2. `Encina.Aspire.ServiceDefaults` (#417): `EncinaAspireConfiguration` (edited) reads `Encina:Aspire:HotReload` (an invalid value is an `Either` `Left` with `encina.aspire.defaults.invalid_declaration` of the reader's existing category; the value is never echoed); `AddEncinaDefaults` (edited) calls `ConfigureEncinaHotReload(o => o.Enabled = true)` when the declaration is true; the coordinator itself still requires `IsDevelopment()`.
3. `EnvironmentContractTests` (#440, edited): the writer-reader parity case for the new key, and for the publish-mode absence.
4. Tests: unit for the writer (run and publish mode) and the reader; guard test edit; a DI test of `AddEncinaDefaults` with the declaration true (the coordinator hosted service and options are registered; the provider builds with `ValidateOnBuild` and `ValidateScopes`) and with it false (nothing registered); PublicAPI of Hosting (the new property) and manifests through `mechanical-fixer`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT:
- Issue #425, Phase 4 (pull request 4). The plans of #416, #417 and #440 are merged and their code pull requests are merged before this one starts (`src/Encina.Aspire.Hosting` and `src/Encina.Aspire.ServiceDefaults` do not exist on `main` today); pull request 2 is merged.

TASK:
Add UseHotReload, its environment key, the reader branch in the service defaults and the parity test.

KEY RULES:
- The variable is written in run mode only; publish never carries it.
- Default false everywhere; the coordinator also requires IsDevelopment().
- Create any new error with EncinaErrors.Create(code, category: ErrorCategory.<X>, message: <constant text>); no configuration value in a message.

REFERENCE FILES:
- src/Encina.Aspire.Hosting (EncinaHostingOptions, EncinaResourceBuilderExtensions)
- src/Encina.Aspire.ServiceDefaults (EncinaAspireConfiguration, EncinaDefaultsExtensions)
- tests/Encina.ContractTests/Aspire/EnvironmentContractTests.cs
```

</details>

---

### Phase 5: Cross-Cutting Integration

> **Goal**: Every function of the matrix is evaluated and the integrated ones are done.

<details>
<summary>Tasks</summary>

1. Structured logging (Phase 2) and options validation (Phase 2) are the integrated functions; the rest are justified in the matrix.
2. Fail closed: the coordinator is off when the environment is unresolved; the authorization caches clear toward re-reading, never toward allowing.
3. No tenant, module or user identifier appears in any log or tag (ADR-054 D4: tenant tags only where multi-tenancy is on, and this feature emits no per-request telemetry).
4. The guide sentence "Saga continuity" and the restart-required cases are documented in Phase 8.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT:
- Issue #425; the code of Phases 1-4 exists.

TASK:
Verify the cross-cutting matrix against the code and fix any gap; add no feature.

KEY RULES:
- A gap in a gate fails closed; a gap in logging is fixed in HotReloadLog.
- No new public API in this phase.

REFERENCE FILES:
- AGENTS.md section 6
- src/Encina/HotReload
```

</details>

---

### Phase 6: Observability

> **Goal**: Hot reload events are structured logs; no tracing or metrics are added (Design Choice 7).

<details>
<summary>Tasks</summary>

1. The log class and the range of Phase 2 are the observability deliverable; confirm the allocation architecture test passes (`EncinaEventIdAllocationTests`) and `EventIdUniquenessRule` is clean.
2. No `ActivitySource` and no `Meter`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT:
- Issue #425, Phase 6. Phase 2 created HotReloadLog and the range 855-864.

TASK:
Run the architecture tests and confirm ids 855-860 are packed and mapped to the Encina assembly.

KEY RULES:
- No id outside 855-864; no sparse ids.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

---

### Phase 7: Testing

> **Goal**: Every flag reaches its target in the pull request that sets it.

<details>
<summary>Tasks</summary>

1. **Unit**, **Guard**, **DI** as in Phases 1 to 4. **Contract**: the environment parity case (Phase 4). **Property**, **Integration**, **Load**, **Benchmark**: not applicable (no complex logic; a real hot reload agent cannot run in CI; a development-time path), stated in the pull request.
2. **Per-file targets** (new files have no baseline; each target is reached by the tests of the pull request named):

   | File (pull request) | Unit | Guard | Contract | Property | Integration | Justification |
   |------|-----:|------:|---------:|---------:|------------:|---------------|
   | `EncinaHotReloadHandler.cs` (1) | 95 | 0 | 0 | 0 | 0 | Two static methods run directly; internal, no guarded parameter |
   | `HotReloadNotifier.cs` (1) | 100 | 0 | 0 | 0 | 0 | One event and a raise loop, every branch tested |
   | `CoreHotReloadCaches.cs` (1) | 100 | 0 | 0 | 0 | 0 | One method over the three clearers |
   | `EncinaHotReloadOptions.cs` (2) | 95 | 90 | 0 | 0 | 0 | Defaults and `WatchAssemblies` null rejection |
   | `HotReload/ServiceCollectionExtensions.cs` (2) | 95 | 90 | 0 | 0 | 0 | Registration, guards and the DI test |
   | `HotReloadCoordinator.cs` (2) | 92 | 0 | 0 | 0 | 0 | Every branch with fake environment and `FakeTimeProvider`; internal constructor, no guard flag |
   | `HotReloadLog.cs` (2) | 90 | 0 | 0 | 0 | 0 | Generated methods called by the coordinator tests |
   | `AuthorizationAttributeCaches.cs` (3) | 100 | 0 | 0 | 0 | 0 | Four dictionaries and `Clear` |
   | `AspNetCoreHotReloadHandler.cs` (3) | 95 | 0 | 0 | 0 | 0 | One static method run directly |
   | `EncinaHostingOptions.cs` (4, edit) | 100 | 0 | 90 | 0 | 0 | The new property read by the writer tests and the parity case; no manifest target is lowered |
   | `EncinaAspireConfiguration.cs` (4, edit) | 100 | 0 | 90 | 0 | 0 | The new branch is read by unit tests and the parity case; existing targets are kept or raised |
   | `Encina.cs` (1, edit) | 98 (current, kept) | 44 (current, kept) | 71 (current, kept) | 0 | 0 | Gains one internal `ClearHotReloadCaches()` called by the handler's unit tests; no target is lowered |
   | `StreamDispatcher.cs` (1, edit) | 100 (current, kept) | 0 | 76 (current, kept) | 0 | 0 | Same, one internal method |
   | `Encina.RequestDispatcher.cs` (1, edit) | 90 (new: the manifest entry has no target today) | 0 | 0 | 0 | 0 | Gains one internal `ClearHotReloadCaches()` run by the handler's unit tests; the existing dispatch paths are exercised by the dispatcher tests |
   | `EncinaHotReloadHandler.cs` (1, CRAP note) | 95 | 0 | 0 | 0 | 0 | CRAP of the three touched dispatcher files is unchanged because each only gains one small method; the handler calls the four clearers |
   | `AuthorizationPipelineBehavior.cs` (3, edit) | 95 (current, kept) | 0 | 0 | 0 | 0 | The caches move to the holder; the existing behavior tests run unchanged; the touched members only read the holder |
   | `EncinaAspireEnvironment.cs`, `EncinaResourceBuilderExtensions.cs` (4, edits) | 100 | existing kept | 90 | 0 | 0 | The new key and the run-mode-only write are covered by the writer tests and the parity case |
   | `EncinaDefaultsExtensions.cs` (4, edit) | 95 | existing kept | 0 | 0 | 0 | The new branch is covered by the DI test with the declaration true and false |
   | `EventIdRanges.cs` (2, edit) | 95 (current, kept) | 0 | 0 | 0 | 0 | One new field read by the allocation architecture test |

3. Measure per flag from the repository root and run `crap-gate` and `coverage-report.cs --check-justifications`; every touched method CRAP <= 10 (the coordinator's handler is split into small methods).

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```text
CONTEXT:
- Issue #425, Phase 7. Each pull request carries the tests that reach its own targets.

TASK:
Measure per-flag coverage, close the gaps and run crap-gate and the justification check.

KEY RULES:
- Tests execute real code; GuardTests live in tests/Encina.GuardTests; never lower a manifest target.
- Never swallow an exception thrown by the code under test.

REFERENCE FILES:
- .github/coverage-manifest/Encina.json
- docs/testing/coverage-measurement-methodology.md
```

</details>

---

### Phase 8: Documentation & Finalization (pull request 5)

> **Goal**: A developer can turn hot reload on and knows its limits.

<details>
<summary>Tasks</summary>

1. XML docs with `<example>` on `ConfigureEncinaHotReload`, the options and `UseHotReload`.
2. A guide page and README sections (through `docs-writer`): what works (body edits), what needs a restart (new handler class, interface changes, source generator changes), validators in development with the transient lifetime, saga continuity, the production statement.
3. `changelog.d/425-aspire-hot-reload.added.md` (through `mechanical-fixer`); `docs/INVENTORY.md` line.
4. The follow-up issue file for the other static caches of `src/` (82 sites outside core and AspNetCore) is opened by the orchestrator with this plan.

</details>

<details>
<summary>Prompt for AI Agents — Phase 8</summary>

```text
CONTEXT:
- Issue #425, Phase 8 (pull request 5). Code merged.

TASK:
Write the guide, README sections, changelog fragment and inventory line.

KEY RULES:
- No hand-typed coverage figures; English only; no private application names.

REFERENCE FILES:
- .claude/skills/encina-docs/SKILL.md
- changelog.d/README.md
```

</details>

---

### Proposed split into pull requests (limit 15 files and 1500 lines each)

| PR | Content | Files | Approx. lines |
|----|---------|------:|--------------:|
| 1 | Three clearer edits (`Encina.cs`, `StreamDispatcher.cs`, `Encina.RequestDispatcher.cs`), `EncinaHotReloadHandler`, `HotReloadNotifier`, `CoreHotReloadCaches`, `Encina.json` manifest, unit test file | 8 | 450 |
| 2 | Options, event record, extension class, coordinator, `HotReloadLog`, `EventIdRanges.cs` edit, `Encina` `PublicAPI.Unshipped.txt`, `Encina.json` edit, allocation test map edit, `docs/engineering/eventid-block-registry.md` edit, unit tests (2), guard test, DI test | 14 | 1,050 |
| 3 | `AuthorizationAttributeCaches`, `AuthorizationPipelineBehavior.cs` edit, `AspNetCoreHotReloadHandler`, AspNetCore manifest, unit test | 5 | 300 |
| 4 | `EncinaHostingOptions`, `EncinaAspireEnvironment`, `EncinaResourceBuilderExtensions`, `EncinaAspireConfiguration`, `EncinaDefaultsExtensions` edits, Hosting `PublicAPI.Unshipped.txt`, two manifests, two unit test files, a DI test file, guard test edit, `EnvironmentContractTests.cs` edit | 13 | 800 |
| 5 | Guide page, two README sections, inventory line, changelog fragment | 5 | 260 |

PR 1 needs nothing (the handler and notifier log nothing; the coordinator of PR 2 does); PR 2 needs PR 1; PR 3 is independent; PR 4 needs the code of #416, #417 and #440 (their plans are merged, their packages are not on `main` yet) and PR 2; PR 5 is last. No interface is changed, so no implementer or test double is touched. A pull request that measures over 15 files or 1,500 lines when it is briefed is split again.

---

## Research

### Relevant Standards & Specifications

| Reference | What it defines | Use in this feature |
|-----------|-----------------|---------------------|
| .NET `MetadataUpdateHandlerAttribute` | `ClearCache` and `UpdateApplication` contract | Phase 1, Design Choice 2 |
| `AGENTS.md` sections 1, 3 and 6 | Opt-in, fail closed, errors by code, 12 functions | Design Choice 8, Phase 5 |
| ADR-021 | EventId ranges registered first | `AspireHotReload` 855-864 |
| ADR-054 (D4) | Tenant tags only where multi-tenancy is on; no baggage | No tenant data in logs |
| ADR-062 | Error category contract | Any new code of Phase 4 |
| SPEC-000 section 2 and DEC-009 | Hot reload is in 1.0 | Scope |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `RequestHandlerCache`, `NotificationHandlerInvokerCache` | `src/Encina/Core/Encina.cs` | Cleared |
| `StreamHandlerCache` | `src/Encina/Core/StreamDispatcher.cs` | Cleared |
| `RequestKindCache` | `src/Encina/Dispatchers/Encina.RequestDispatcher.cs` | Cleared |
| Authorization attribute caches | `src/Encina.AspNetCore/AuthorizationPipelineBehavior.cs` | Moved to a holder and cleared |
| `AddEncinaFluentValidation(assembly, lifetime)` | `src/Encina.FluentValidation/ServiceCollectionExtensions.cs` | Validators in development |
| `EncinaHostingOptions`, `WithEncina` | #416 | The `UseHotReload` declaration |
| `AddEncinaDefaults`, `EncinaAspireConfiguration` | #417 | The reader and the call |
| `EnvironmentContractTests` | #440 | Parity case |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina` (`AspireHotReload`, new) | 855-864, ids 855-860 used | From the assigned block 850-899 (aspire-2); #424 uses 850-854, #382 865-877, #387 878-889, #391 890-899; registered in `EventIdRanges.cs` and the allocation test map before use |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production code (new) | 9 | Handler, notifier, core caches, options, extension class, coordinator, log, authorization holder, AspNetCore handler |
| Production code (edited) | 9 | Three dispatcher files, `AuthorizationPipelineBehavior.cs`, `EventIdRanges.cs`, four Aspire files of #416 and #417 |
| Public API and manifests | 5 | `Encina` and Hosting PublicAPI, `Encina.json`, AspNetCore and the two Aspire manifests |
| Tests | 9 | Unit (5), guard (2), DI (1), contract edit (1), allocation map edit |
| Documentation | 5 | Guide, two README sections, inventory line, changelog fragment |
| **Total** | **by the split table: 8 + 14 + 5 + 13 + 5 = 45 file touches over 5 pull requests** | The split table is the authority |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
PROJECT CONTEXT:
- Encina is a pre-1.0 .NET 10 / C# 14 library; Either<EncinaError, T>; no [Obsolete], no compatibility layers.
- Issue #425: hot reload support. Plan: docs/plans/aspire-hot-reload-implementation-plan-425.md; the Dilemmas D1, D3, D4, D5, D6, D7 and D9 are recommended, pending the maintainer.

IMPLEMENTATION OVERVIEW (5 pull requests, each builds alone):
1. Core: four cache clearers, the assembly MetadataUpdateHandler, the notifier (Phase 1).
2. Options, ConfigureEncinaHotReload, the coordinator, logs 855-860 (Phase 2).
3. Authorization caches in a non-generic holder and their handler (Phase 3).
4. UseHotReload in the AppHost and the reader in the service defaults (Phase 4).
5. Documentation (Phase 8).

KEY PATTERNS:
- A metadata update keeps type identities: clear what Encina cached, warn about handler types the container does not know, never reload assemblies.
- Handlers never throw. The coordinator is a development-only singleton, off unless Enabled and IsDevelopment().
- Authorization caches clear toward re-reading attributes, never toward allowing.
- EventIds 855-860 inside AspireHotReload (855-864), registered first.

REFERENCE FILES:
- src/Encina/Core/Encina.cs, src/Encina/Core/StreamDispatcher.cs, src/Encina/Dispatchers/Encina.RequestDispatcher.cs
- src/Encina.AspNetCore/AuthorizationPipelineBehavior.cs
- src/Encina/Diagnostics/EventIdRanges.cs
- docs/plans/aspire-hosting-implementation-plan-416.md, docs/plans/aspire-service-defaults-implementation-plan-417.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Not applicable: the feature clears dispatcher and attribute caches; it adds no cache |
| 2 | OpenTelemetry | ❌ | Not applicable, as in the issue: no spans or meters (Design Choice 7 option C rejected); logs are exported by the service defaults of #417 |
| 3 | Structured Logging | ✅ | Phase 2: `HotReloadLog`, EventIds 855-860 |
| 4 | Health Checks | ❌ | Not applicable: a development-time feature with no checkable dependency |
| 5 | Validation | ✅ | Options validated; the Aspire declaration validated by the reader |
| 6 | Resilience | ❌ | Not applicable: no external call |
| 7 | Distributed Locks | ❌ | Not applicable: per-process state |
| 8 | Transactions | ❌ | Not applicable: no store is touched |
| 9 | Idempotency | ❌ | Not applicable: clearing a cache twice is harmless by nature |
| 10 | Multi-Tenancy | ❌ | Not applicable: no tenant data; no tenant-qualified key or tag is created |
| 11 | Module Isolation | ❌ | Not applicable: the handler clears process-wide caches that no module owns |
| 12 | Audit Trail | ❌ | Not applicable: a development-only event, not a state change of user data |

---

## Prerequisites & Dependencies

- The plans of #416, #417 and #440 are merged on `main`, their code is not; pull requests 1 to 3 need none of it, pull request 4 needs all three packages.
- `Microsoft.Extensions.Hosting.Abstractions` is already referenced by core; no package is added.
- The follow-up issue file for the remaining static caches (Messaging, Security, Compliance, SignalR and others) is opened by the orchestrator with this plan.

---

## Next Steps

1. The maintainer answers D1, D3, D4, D5, D6, D7 and D9 when work on #425 starts; the answers go into `## Maintainer Decisions` and the markers are removed.
2. Pull requests 1 to 3 in any order after 1 (2 needs 1); pull request 4; pull request 5 last.
