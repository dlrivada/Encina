---
nav_exclude: true
---

# Native AOT across Encina: measured blockers and options (spike #3170)

This page is for the maintainer, who has to answer decision D1 (does Native AOT enter 1.0, and at what depth) and for the authors of the plans that follow. It records what stops an Encina application from publishing with `PublishAot`, measured on a scratch build, and what each option would cost. **Decided by the maintainer on 2026-11-05: Native AOT is not in 1.0** ([decision comment](https://github.com/dlrivada/Encina/issues/3170#issuecomment-6653651407)); it is studied again after 1.0, and whether it is feasible at all is an open question. The measurements below are the basis of that decision; the recommendation in section 9 is kept as it was written. Nothing in `src/` was changed.

**Status:** measured on 2026-11-04 against `origin/main` at `ed8143c78`, with .NET SDK 10.0.403 and ILCompiler 10.0.14 on win-x64. **Issue:** [#3170](https://github.com/dlrivada/Encina/issues/3170). **Trigger:** [#3065](https://github.com/dlrivada/Encina/issues/3065) (`Encina.AwsLambda` cold start), where the maintainer decided on 2026-11-04 to ship source-generation overloads now and to make no Native AOT claim until the core is AOT-capable.

Pre-1.0, backward compatibility and breaking-change cost are not criteria ([AGENTS.md](../../AGENTS.md) section 1).

## 1. Background

Native AOT compiles an application to a native executable at publish time. The compiler analyses the program statically, so code that depends on `MakeGenericType`, `Activator.CreateInstance`, `Assembly.GetTypes`, `dynamic` or compiled expression trees can be trimmed away or can lack precompiled native code for a generic instantiation that exists only at run time.

Encina's core dispatches handlers by reflection. [ADR-005](../architecture/adr/005-reject-source-generators.md) rejected source generators and, in its "Native AOT Consideration" section (lines 285-293), deferred the question: "Deferred until Native AOT is a concrete requirement." [SPEC-000](../specifications/SPEC-000-encina-1.0-baseline-and-release-scope.md) section 6 (line 179) repeats it: "revisit only for Native AOT". Before this spike, only `src/Encina.Testing.TUnit/Encina.Testing.TUnit.csproj:21` set `IsAotCompatible`.

## 2. How it was measured

Two measurements, both in a scratch folder (`artifacts/spike-3170/`, git-ignored); no repository file was edited.

1. **Analyzer run over all of `src/`.** 107 projects were rebuilt in Release with `IsAotCompatible`, `EnableTrimAnalyzer`, `EnableAotAnalyzer` and `EnableSingleFileAnalyzer` set, and 0 build errors.
2. **`PublishAot` of a scratch sample application** with four variants, published on win-x64 in Release: `core` (`AddEncina(assembly)` assembly scan), `manual` (`AddEncina()` plus explicit `AddTransient<IRequestHandler<..>>`), `lambda` (`manual` plus `AddEncinaAwsLambda`) and `ef` (`manual` plus `AddEncinaEntityFrameworkCore<AppDb>` over a SQLite `DbContext`).

### 2.1 Why the first build showed no IL warnings

The analyzers ran, but their diagnostics were hidden. `.editorconfig:142` sets `dotnet_analyzer_diagnostic.severity = suggestion` for every analyzer (comment at lines 139-141: "All analyzers default to suggestion for incremental adoption"). The ILLink Roslyn analyzer (`ILLink.RoslynAnalyzer.dll` from `microsoft.net.illink.tasks/10.0.14`) is passed to the compiler and the three `Enable*Analyzer` properties reach it, but IL2xxx and IL3xxx are demoted to `suggestion`, which the command line does not print. `Directory.Build.props` has no trim, AOT or IL setting except `WarningsAsErrors` with `IL*` at line 25, which only matters once a diagnostic is a warning.

To expose them without touching the repository, a scratch global analyzer config re-enabled the IDs (`IL2001` to `IL2122`, `IL3000` to `IL3003`, `IL3050` to `IL3058`, all at severity `warning`, `global_level = 1000`) and a scratch MSBuild target added it as an `EditorConfigFiles` item before `CoreCompile`. Command, run from the worktree root:

```text
dotnet msbuild artifacts\spike-3170\all-src.proj -restore -p:Configuration=Release -p:IsAotCompatible=true
  -p:EnableTrimAnalyzer=true -p:EnableAotAnalyzer=true -p:EnableSingleFileAnalyzer=true
  -p:TreatWarningsAsErrors=false "-p:WarningsAsErrors=" -p:GenerateDocumentationFile=false
  -p:CustomAfterMicrosoftCommonTargets=...\aot-inject.targets
```

`all-src.proj` rebuilds every `src/**/*.csproj`. With the same injection, a build of `src/Encina` alone prints 35 warnings, which matches the core row of section 3.

The repository's own `WarningsAsErrors` includes `IL*`, so once someone raises these IDs to warning the build breaks unless they are handled in the 72 packages with findings. `Encina.Testing.TUnit`, which already sets `IsAotCompatible`, has 0 findings today. See section 8 for the separate finding.

Limits of the analyzer numbers: the Roslyn analyzer does not follow data flow across assemblies, so it reports fewer problems in core than the AOT compiler (ILC) does, and it cannot see third-party code. It also misses value-type instantiation hazards (section 4). Counts are deduplicated by file, line, column and ID, so a project that compiles shared linked files is not counted twice.

## 3. Analyzer results

107 projects rebuilt, 969 unique diagnostics in 72 packages; 35 packages have 0. The per-diagnostic data is in `aot-warnings.csv` and the per-package data in `per-package-categories.csv` (both in the scratch folder, not committed).

By diagnostic ID:

| ID | Count |
| --- | ---: |
| IL3050 | 307 |
| IL2026 | 288 |
| IL2091 | 89 |
| IL2070 | 80 |
| IL2090 | 66 |
| IL2075 | 36 |
| IL2087 | 34 |
| IL2072 | 29 |
| IL2060 | 14 |
| IL2057 | 12 |
| IL2067 | 7 |
| IL2080 | 3 |
| IL2111 | 2 |
| IL2055 | 1 |
| IL2093 | 1 |

By cause (classified from the message text):

| Cause | Count |
| --- | ---: |
| System.Text.Json without source generation | 383 |
| Other DAM or reflection on `Type` | 241 |
| Framework APIs (Minimal API `MapGet`/`MapPost`, `ConfigurationBinder`, EF) | 105 |
| `dynamic` / RuntimeBinder (Dapper packages 28 each, plus SignalR and gRPC) | 94 |
| Assembly scanning | 67 |
| `MakeGenericType` / `MakeGenericMethod` | 49 |
| Expression trees | 28 |

Per package, the 31 largest. "Other IL2xxx" is every IL2xxx except IL2026 (mostly DAM annotation gaps); the cause columns overlap the ID columns.

| Package | Total | IL2026 | IL3050 | Other IL2xxx | JSON | dynamic | expr | MakeGeneric | asm-scan |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Encina.EntityFrameworkCore | 120 | 12 | 13 | 95 | 10 | 0 | 12 | 1 | 6 |
| Encina.Testing | 77 | 31 | 31 | 15 | 60 | 0 | 0 | 0 | 1 |
| Encina.DomainModeling | 49 | 12 | 5 | 32 | 10 | 0 | 0 | 0 | 13 |
| Encina.Dapper.SqlServer | 44 | 17 | 19 | 8 | 6 | 28 | 2 | 0 | 1 |
| Encina.Dapper.MySQL | 42 | 17 | 19 | 6 | 6 | 28 | 2 | 0 | 0 |
| Encina.Messaging | 41 | 5 | 11 | 25 | 10 | 0 | 0 | 10 | 5 |
| Encina.Dapper.PostgreSQL | 41 | 17 | 19 | 5 | 6 | 28 | 2 | 0 | 0 |
| **Encina (core)** | 35 | 4 | 12 | 19 | 2 | 0 | 0 | 11 | 3 |
| Encina.Testing.Pact | 28 | 10 | 12 | 6 | 18 | 0 | 0 | 6 | 4 |
| Encina.Marten.GDPR | 27 | 6 | 2 | 19 | 4 | 0 | 0 | 0 | 2 |
| Encina.ADO.SqlServer | 23 | 3 | 5 | 15 | 6 | 0 | 2 | 0 | 1 |
| Encina.Marten | 22 | 1 | 6 | 15 | 0 | 0 | 0 | 10 | 2 |
| Encina.ADO.PostgreSQL | 22 | 3 | 5 | 14 | 6 | 0 | 2 | 0 | 1 |
| Encina.Security.ABAC | 21 | 8 | 8 | 5 | 15 | 0 | 0 | 0 | 5 |
| Encina.ADO.MySQL | 20 | 3 | 5 | 12 | 6 | 0 | 2 | 0 | 0 |
| Encina.Security.Audit | 19 | 8 | 8 | 3 | 16 | 0 | 0 | 0 | 0 |
| Encina.MongoDB | 19 | 4 | 8 | 7 | 8 | 0 | 4 | 0 | 0 |
| Encina.AspNetCore | 18 | 8 | 7 | 3 | 0 | 0 | 0 | 0 | 2 |
| Encina.Compliance.GDPR | 17 | 8 | 8 | 1 | 15 | 0 | 0 | 0 | 2 |
| Encina.gRPC | 16 | 5 | 7 | 4 | 4 | 6 | 0 | 4 | 1 |
| Encina.AwsLambda | 16 | 8 | 8 | 0 | 16 | 0 | 0 | 0 | 0 |
| Encina.Testing.WireMock | 16 | 8 | 8 | 0 | 16 | 0 | 0 | 0 | 0 |
| Encina.Cdc | 16 | 3 | 9 | 4 | 6 | 0 | 0 | 7 | 1 |
| Encina.SignalR | 15 | 7 | 5 | 3 | 6 | 4 | 0 | 0 | 3 |
| Encina.Caching.Redis | 14 | 7 | 7 | 0 | 14 | 0 | 0 | 0 | 0 |
| Encina.Compliance.Anonymization | 13 | 1 | 0 | 12 | 0 | 0 | 0 | 0 | 1 |
| Encina.Security.PII | 13 | 6 | 6 | 1 | 12 | 0 | 0 | 0 | 0 |
| Encina.Compliance.DataSubjectRights | 11 | 3 | 2 | 6 | 3 | 0 | 0 | 0 | 2 |
| Encina.Security.Secrets | 9 | 3 | 3 | 3 | 4 | 0 | 0 | 0 | 0 |
| Encina.NATS | 8 | 4 | 4 | 0 | 8 | 0 | 0 | 0 | 0 |
| Encina.AmazonSQS | 8 | 4 | 4 | 0 | 8 | 0 | 0 | 0 | 0 |

The remaining 41 packages with findings have 1 to 6 each: Encina.MQTT, Security.Sanitization, Testing.Fakes, AzureFunctions, Audit.Marten, AzureServiceBus and Compliance.Attestation have 6 each; the other 34 have 1 to 4.

The 35 packages with 0 findings are: Aspire.Testing, AspNetCore.Blazor, Caching.Dragonfly, Caching.Garnet, Caching.Hybrid, Caching.KeyDB, Caching.Valkey, Cdc.MongoDb, Cli, Compliance.NIS2, Compliance.ProcessorAgreements, DistributedLock (and its InMemory, Redis and SqlServer packages), Extensions.Resilience, GraphQL, GuardClauses, Hangfire, IdGeneration, Messaging.Encryption.AwsKms, Messaging.Encryption.AzureKeyVault, Messaging.Encryption.DataProtection, MiniValidator, OpenTelemetry, Polly, Quartz, Refit, Security.ABAC.Analyzers, Security.AntiTampering, Testing.FsCheck, Testing.Respawn, Testing.Shouldly, Testing.TUnit and Testing.Verify. Some of these are thin wrappers or have no reflection; a 0 means the Roslyn analyzer found nothing, not that ILC will be silent.

System.Text.Json findings appear in 49 packages.

## 4. PublishAot results

| Variant | Registration | Publish | ILC warnings (unique) | Binary | Runtime result |
| --- | --- | --- | --- | --- | --- |
| core | `AddEncina(assembly)` scan | OK, 60 s | 29 (20 LanguageExt, 9 Encina) | 4.6 MB | Runs, but `Send` returns "No registered IRequestHandler was found": the scan finds no handler (trimmed type, `Assembly.GetTypes`). |
| manual | `AddEncina()` plus explicit `AddTransient<IRequestHandler<..>>` | OK, about 45 to 100 s | 34 (20 LanguageExt, 14 Encina) | 4.9 MB | `Send` of a class request with a class response (`Ping -> string`) works (`OK: pong hello`); see the failures below. |
| lambda | `manual` plus `AddEncinaAwsLambda` | OK, 48 s | 29 | 4.6 MB | Works. The sample did not root the AwsLambda handlers, so that package's 16 analyzer warnings were not exercised. |
| ef | `manual` plus `AddEncinaEntityFrameworkCore<AppDb>` and a SQLite `DbContext` | OK, 109 s | 575 (530 EF Core, 20 LanguageExt, 9 Encina, 16 other) | 20.8 MB | `Send` works; EF fails at run time: `InvalidOperationException: Model building is not supported when publishing with NativeAOT. Use a compiled model.` |

ILC warning counts for the code Encina core owns in the `manual` variant (LanguageExt included): IL3050 10, IL2070 8, IL2026 5, IL2072 5, IL2075 3, IL2055 2 (the 34 unique warnings of the variant also include 1 IL2067).

### 4.1 Run-time failures in the `manual` variant

Only reference-type request and response pairs dispatch. Three failures:

1. `Send(new PingS(3))` with `readonly record struct PingS : IRequest<int>` throws ``NotSupportedException: 'Encina.Encina+RequestHandlerWrapper`2[PingS,Int32]' is missing native code or metadata``. This is the IL3050 at `src/Encina/Core/Encina.cs:154` coming true: `MakeGenericType` over value types is not precompiled.
2. `Stream(new Count(3))` (`IStreamRequest<int>`) throws the same error for `StreamRequestHandlerWrapper<Count,Int32>` (`src/Encina/Core/StreamDispatcher.cs:69`). Any stream of a value-type item breaks.
3. `Publish(new Note("n"))` returns `ERR Handler NoteHandler does not expose a compatible Handle method`: the notification path calls `GetMethods` on the handler (`src/Encina/Dispatchers/Encina.NotificationDispatcher.cs:347-348`) and the trimmer removed the method.

### 4.2 Toolchain note

Visual Studio 18 Community with the C++ tools was installed and ILCompiler 10.0.14 was already in the NuGet cache. The default publish failed at link time: ILCompiler's `build/findvcvarsall.bat` (lines 9-17) expands `%ProgramFiles(x86)%` inside a `FOR /F (...)` block, the `)` closes the block, and the garbage output is used as the linker path (MSB3073, `vswhere.exe` not recognized). The workaround was to run from `vcvarsall.bat amd64` and pass `-p:IlcUseEnvironmentalTools=true`. This affects any developer or CI machine with Visual Studio in `Program Files (x86)\...\Installer` and is independent of Encina.

### 4.3 Dependencies

- **LanguageExt.Core** is a hard dependency of core and of every `Either` signature. It contributes 20 ILC warnings of its own (`ClassInstances`, `IL.Compare` and `ClassFunctions.MakeFunc1`: `Assembly.GetTypes`, `MakeGenericType`, `Expression.PropertyOrField`), reachable even though the sample only uses `Right` and `Match`. They cannot be fixed inside Encina. Whether they cause run-time failures was not tested beyond `Right` and `Match`, which work.
- **EF Core**: Microsoft states that "NativeAOT and query precompilation are highly experimental ... not yet suited for production use", requires a compiled model and precompiled queries (interceptors), does not support dynamic queries, and says "Publishing currently reports a number of trimming and NativeAOT warnings" ([NativeAOT and precompiled queries](https://learn.microsoft.com/en-us/ef/core/performance/nativeaot-and-precompiled-queries), page dated 2026-06-24). This agrees with the 575 warnings and the run-time failure above.
- **Not verified** (no source consulted, treat as unknown): Marten, Dapper, the MongoDB driver, Npgsql, MySqlConnector and the broker clients. Their local analyzer evidence is in section 3: the three Dapper packages use `dynamic` (28 findings each) and Marten has 10 `MakeGenericType` sites.

## 5. Ranked blockers

| Rank | Blocker | What the measurement shows |
| --- | --- | --- |
| 1 | Core dispatch: `MakeGenericType` plus `Activator.CreateInstance` for requests and streams | Blocks every app; a hard run-time failure for any value-type request, response or stream item, not a warning. Fixable by a source-generated dispatcher; no external dependency. |
| 2 | Handler discovery and registration by assembly scan (`GetTypes`, `GetInterfaces`, unannotated `Type` registration) | In AOT it silently registers nothing (variant `core`). Fixable by the generator emitting registrations, or by documenting explicit registration. |
| 3 | Notification dispatch: `MakeGenericType(INotificationHandler<>)`, `GetMethods`, compiled expression trees | Run-time failure shown in 4.1. Fixable by the generator. |
| 4 | Pipeline behaviors and ordering (`PipelineOrderValidationHostedService`, `PipelineRegistrationSnapshot`) | The hosted service is a startup validator with six `MakeGenericType` calls; the registration snapshot closes open generics. Fixable, but the generator design must cover it. Pipeline behaviors were not exercised at run time (section 7). |
| 5 | System.Text.Json without source generation | 383 of 969 findings, in 49 packages (messaging stores, transports, security, compliance, AWS and Azure adapters). Mechanical per package (`JsonSerializerContext`), but the payload types must be known at compile time or the user must supply a `JsonTypeInfoResolver`: a design question for every message payload, not only a code edit. |
| 6 | Third-party dependencies | LanguageExt.Core (20 ILC warnings, not ours to fix, risk unknown); EF Core (experimental, compiled model plus precompiled queries, 530 ILC warnings, run-time failure); the Dapper packages (`dynamic` binder, 28 each, three packages); Marten and the broker clients (status unverified). A claim for the 10 database providers is unreachable today for EF Core and likely for Dapper; the ADO.NET providers have 20 to 23 findings each, mostly DAM-annotated generics (`TEntity` reflection in bulk operations), which could be annotated or rewritten. |
| 7 | Everything else | DAM annotation gaps (`GetProperties` on `TEntity`, `AddTransient(Type)`), Minimal API `MapGet`/`MapPost` in `Encina.AspNetCore` (the Request Delegate Generator is needed), expression trees in the Dapper, ADO.NET and MongoDB mappers. |

## 6. What a source-generated dispatcher must replace

Reflection sites in `src/Encina` (line numbers checked on 2026-11-04):

| Concern | Site | Reflection |
| --- | --- | --- |
| Request wrapper creation | `Core/Encina.cs:152-156` (`CreateRequestHandlerWrapper`), cache at `Core/Encina.cs:43`, used at `Dispatchers/Encina.RequestDispatcher.cs:69` | `MakeGenericType` plus `Activator.CreateInstance` per (request, response) |
| Stream wrapper | `Core/StreamDispatcher.cs:67-71` (`CreateStreamHandlerWrapper`), cache at `:18`, used at `:33` | Same pattern; a value-type item fails |
| Notification handler resolution | `Dispatchers/Encina.NotificationDispatcher.cs:63-64` | `typeof(INotificationHandler<>).MakeGenericType(notificationType)` plus `GetServices(Type)` |
| Notification invoker | `Dispatchers/Encina.NotificationDispatcher.cs:337` (`GetInterfaces`), `:348` (`GetMethods`), `lambda.Compile()` at `:331`, cache at `Core/Encina.cs:44` | Expression tree compiled per (handler, notification) |
| Handler and behavior discovery | `Dispatchers/MediatorAssemblyScanner.cs:19` (`ScanAssembly`), `:31-33` (`ProcessType`, `GetInterfaces`), `:144` (`GetTypes`) | Assembly scan at startup |
| Registration | `Core/ServiceCollectionExtensions.cs:136-153` (`ServiceDescriptor.Describe` with runtime `Type` values), `:254` (`AddScoped(serviceType, implementationType)`); `Core/EncinaConfiguration.cs:484,490,531` | Registration by unannotated `Type`, `GetInterfaces` |
| Pipeline ordering | `Pipeline/Ordering/PipelineRegistrationSnapshot.cs:117` (closes an open generic); `Pipeline/Ordering/PipelineOrderValidationHostedService.cs:68-80` (six `MakeGenericType`) | Startup validation by reflection |
| Module registry | `Modules/ModuleHandlerRegistry.cs:77` (`GetTypes`), `:87` (`GetInterfaces`) | Assembly scan |
| Sharding (not dispatch, but in core) | `Sharding/ShardKeyExtractor.cs:84`, `Sharding/CompoundShardKeyExtractor.cs:198` (`GetProperties`), `Sharding/ShardingServiceCollectionExtensions.cs:436` (`GetTypes`), `Sharding/ReferenceTables/ReferenceTableReplicator.cs:305` and `Sharding/ReferenceTables/PollingRefreshDetector.cs:145` (`GetMethod`) | Attribute and property reflection over entity types |
| Diagnostics DI | `Sharding/Diagnostics/ShardingDiagnosticsServiceCollectionExtensions.cs:163`, `Sharding/Shadow/ShadowShardingServiceCollectionExtensions.cs:117` | `ActivatorUtilities.CreateInstance` |

The wrapper bodies and pipeline builders (`Core/Encina.cs:165-185`, `Pipeline/PipelineBuilder.cs:103-117`, `Pipeline/StreamPipelineBuilder.cs:69`) are generic and AOT-safe once the wrapper type is statically instantiated.

A generator therefore has to emit, per assembly:

- A static table from (`TRequest`, `TResponse`) to a concrete `RequestHandlerWrapper<,>` instance, with no `MakeGenericType`. It also covers value types, because the closed generic is named in generated code. The same for streams.
- A notification publisher per notification type: handlers resolved through the closed `IEnumerable<INotificationHandler<T>>`, invoked by a direct call instead of `Compile`.
- The `AddTransient<TService, TImpl>()` registrations, the behavior and processor registrations, and the ordering metadata for `PipelineRegistrationSnapshot` and the validation hosted service.
- Diagnostics for handlers that are generic, internal or in other assemblies.

Polymorphic publish (`notification.GetType()` at run time, `Dispatchers/Encina.NotificationDispatcher.cs:52`) needs an explicit rule, for example a generated table keyed by run-time type with a base-type and interface fallback computed at compile time.

## 7. Not measured

- Cold start on AWS Lambda and Azure Functions. The issue lists it as a deliverable; no cold-start number exists yet, so D1 rests on blockers and cost only.
- AWS and Azure adapters under ILC with their real handlers (the sample did not root them), and brokers, caches, Marten, MongoDB, Dapper and the ADO.NET providers at publish time: only analyzer counts exist for them.
- Behavior of LanguageExt beyond `Right` and `Match`.
- Pipeline behaviors at run time under AOT (the sample registered none).
- Linux and ARM64 targets; only win-x64 was published.
- The generated dispatcher itself: no prototype was built.

## 8. Separate finding: analyzers are demoted to suggestion

`.editorconfig:142` sets `dotnet_analyzer_diagnostic.severity = suggestion` for every analyzer, so the AOT and trim diagnostics (and any other analyzer without its own severity rule) stay invisible in normal builds until someone raises them. This is a repository-wide configuration matter, outside this spike; it was reported to the orchestrator and is not decided here.

## 9. Options for D1

| | A: no Native AOT in 1.0 | B: AOT-capable core only | C: full AOT across 1.0 packages |
| --- | --- | --- | --- |
| Scope | Keep reflection dispatch ([ADR-005](../architecture/adr/005-reject-source-generators.md) unchanged); document that `PublishAot` and trimming are unsupported; re-open after 1.0 | Optional source generator for wrappers and registration; `Encina` core and abstractions marked `IsAotCompatible`; provider packages stay outside the claim until each is proven | Option B plus annotation or rewrite of every shipped package |
| Cost | Low (documentation, ADR-005 note) | Medium-High (generator, core annotations, tests, new ADR) | High (cross-package, likely several milestones) |
| What the measurement says | Leaves blockers 1 to 3 in place | Addresses blockers 1 to 4 (hard failures and discovery); the 20 LanguageExt warnings remain but did not stop class-type dispatch | Meets EF Core's experimental status (530 ILC warnings, run-time failure) and Dapper's `dynamic` (28 findings in each of three packages); a claim for all 10 database providers is not reachable today |
| Wins when | The 1.0 audience is server hosting where startup time is not a hard requirement | Serverless cold start is a stated 1.0 value and a core-only claim is enough | AOT is a headline 1.0 value and long-term investment in dependency rewrites is accepted |
| Costs of choosing it | AOT-only deployments are excluded | Reverses part of ADR-005; the generator is a new maintenance surface; every dependent package still needs its own status | Largest cost; the claim may be unreachable for ORMs, Marten and broker clients |

Recommendation (not a decision): Option B fits the measurements best. It addresses the hard run-time failures (blockers 1 to 3) and the discovery gap, none of which has an external dependency, and it keeps the generator opt-in, so pay-for-what-you-use holds. What the measurements cannot settle is whether the 20 LanguageExt warnings, the unproven pipeline behaviors and the missing cold-start number are acceptable for a core-only "AOT-capable" claim; Option A remains the safe choice if the maintainer prefers to decide after those are known.

**Decision (maintainer, 2026-11-05): D1 = no Native AOT in 1.0.** The measurements show that supporting it would need deep changes to the architecture, the strategies and the supported capabilities (assembly-scanning discovery, value-type responses and `Publish` fail under `PublishAot`; EF Core needs a compiled model; LanguageExt warnings Encina cannot fix). It is studied again after 1.0; feasibility itself is open.

### 9.1 Follow-ups

- Record the decision in an ADR that amends [ADR-005](../architecture/adr/005-reject-source-generators.md) (Native AOT not in 1.0, revisit after 1.0), as the issue's ADR deliverable.
- Document in the user documentation that `PublishAot` and trimming are not supported in 1.0.
- After 1.0: prototype a source-generated dispatcher and rerun the `manual` variant with value-type requests, streams and notifications; measure cold start on Lambda and Azure Functions; assess EF Core compiled-model integration.

## Related

- [#3170](https://github.com/dlrivada/Encina/issues/3170): SPIKE, Native AOT across Encina
- [#3065](https://github.com/dlrivada/Encina/issues/3065): `Encina.AwsLambda` cold start, the trigger
- [#727](https://github.com/dlrivada/Encina/issues/727): OpenTelemetry instrumentation for Azure Functions and AWS Lambda
- [#60](https://github.com/dlrivada/Encina/issues/60): AWS Lambda integration
- [ADR-005](../architecture/adr/005-reject-source-generators.md): reject source generators
- [SPEC-000](../specifications/SPEC-000-encina-1.0-baseline-and-release-scope.md): 1.0 baseline and release scope
