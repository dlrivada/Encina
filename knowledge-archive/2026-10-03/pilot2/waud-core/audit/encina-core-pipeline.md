# Audit result: encina-core-pipeline

- **Checklist version**: SPEC-003 §5.2, v1 (2026-09-24 draft)
- **Date**: 2026-09-24
- **Package**: Encina
- **Audit unit**: `src/Encina/Pipeline/**`, `src/Encina/Core/**`, `src/Encina/Dispatchers/**` (25 files)
- **Manifest**: `.github/coverage-manifest/Encina.json` (package-level targets: unit 70, guard 20, contract 15; no property/integration target declared for this package)

## Files in scope

Core: `AmbientRequestContext.cs`, `Encina.cs`, `Encina.Stream.cs`, `EncinaConfiguration.cs`, `RequestContext.cs`, `RequestContextAccessor.cs`, `RequestContextDispatchExtensions.cs`, `ServiceCollectionExtensions.cs`, `StreamDispatcher.cs`.
Dispatchers: `Encina.NotificationDispatcher.cs`, `Encina.RequestDispatcher.cs`, `MediatorAssemblyScanner.cs`, `Strategies/INotificationDispatchStrategy.cs`, `Strategies/ParallelDispatchStrategy.cs`, `Strategies/ParallelWhenAllDispatchStrategy.cs`, `Strategies/SequentialDispatchStrategy.cs`.
Pipeline: `Behaviors/CommandActivityPipelineBehavior.cs`, `Behaviors/CommandMetricsPipelineBehavior.cs`, `Behaviors/QueryActivityPipelineBehavior.cs`, `Behaviors/QueryMetricsPipelineBehavior.cs`, `EncinaBehaviorGuards.cs`, `EncinaNotificationGuards.cs`, `EncinaRequestGuards.cs`, `PipelineBuilder.cs`, `StreamPipelineBuilder.cs`.

## Checklist results

| ID | Outcome | Evidence |
|---|---|---|
| AUD-01 | pass | Decisions recorded for this unit: #1147 (pipeline behaviors must see a populated `IRequestContext`) and #1163 (`IRequestContextAccessor` must resolve without the core `AddEncina()`). Both still hold: `Dispatchers/Encina.RequestDispatcher.cs:46` and `Encina.NotificationDispatcher.cs:43` enter the ambient context via `AmbientRequestContext.Enter` before resolving handlers; `Core/ServiceCollectionExtensions.cs:58` registers `IRequestContextAccessor` unconditionally inside `AddEncina`. |
| AUD-02 | conforms-with-na | OpenTelemetry: integrated (`EncinaDiagnostics.SendStarted/SendCompleted`, `StartStreamActivity`, per-behavior `ActivitySource.StartActivity` in `CommandActivityPipelineBehavior.cs`/`QueryActivityPipelineBehavior.cs`). Structured logging: integrated (`Encina.cs Log`, `Encina.Stream.cs Log`, both partial `[LoggerMessage]` classes). Multi-tenancy: integrated (`IRequestContext.TenantId`, propagated by `RequestContext.ForNestedDispatch`, `Core/RequestContext.cs:94-108`). Module isolation: integrated (`IModuleHandlerRegistry` resolved and defaulted to `NullModuleHandlerRegistry.Instance` in `ServiceCollectionExtensions.cs:64`). Idempotency: integrated at the context level (`IRequestContext.IdempotencyKey` stripped for nested dispatches, `RequestContext.cs:104`); enforcement itself lives in `Encina.Messaging` (out of unit). Caching/Resilience/Distributed Locks/Transactions/Audit Trail: not applicable — the mediator dispatches in-process and does not itself call an external system, cache, lock or transactional resource; those are separate opt-in pipeline behaviors in other packages. Health checks: not applicable — the mediator has no external dependency of its own to probe. |
| AUD-03 | partial (findings F-06, F-07) | Coverage measured by running the unit's own test classes against the package's Cobertura output (see "Coverage" below): unit 90.8%, guard 48.2%, contract 85.9% for the four files where the manifest declares `contract` — all above the manifest's 70/20/15 targets. Two manifest entries misdeclare their file: `Core/EncinaConfiguration.cs`'s `reason` says "EF entity type configuration" (a copy of the reason used for unrelated EF entity configuration classes elsewhere in the manifest) and its `defaultTests` omits `guard`, although `tests/Encina.GuardTests/Core/EncinaConfigurationGuardTests.cs` exists, passes, and exercises 71.1% of the file's lines (F-06, F-07). |
| AUD-04 | pass | The contract tests read (`ActivityPipelineBehaviorContractTests.cs`, `MetricsPipelineBehaviorContractTests.cs`) instantiate the real behaviors (`new CommandActivityPipelineBehavior<TestCommand,string>(detector)`) and drive them through `Handle`, not reflection over the interface. |
| AUD-05 | pass | Unit, guard and contract folders exist for every file the manifest requires them for (`tests/Encina.UnitTests/{Core,Dispatchers,Pipeline}`, `tests/Encina.GuardTests/Core/{Pipeline,Dispatchers}`, `tests/Encina.ContractTests/Core/Pipeline`). No file in this unit declares `property` or `integration` in the manifest, so no justification file is owed for those flags here. |
| AUD-06 | n/a | This is a unit-level audit, not a per-record audit; AUD-06 runs per closed bug record (SPEC-003 §5.1). Not evaluated here. |
| AUD-07 | n/a | The core mediator is provider-agnostic; it does not implement a provider-dependent feature (no database, cache, transport, lock, validation or cloud provider code in this unit). |
| AUD-08 | pass | Every `[LoggerMessage]` EventId in scope (100-120, `Core/Encina.Stream.cs` and `Core/Encina.cs`) falls inside `EventIdRanges`' documented Core range 1-199 ("Encina core: mediator, streaming, sharding (100-199)"). No `LoggerMessage.Define` calls found in the unit (`Grep "LoggerMessage\(EventId"` and `"new EventId("` both empty outside the two `[LoggerMessage]` classes). |
| AUD-09 | pass | `dotnet build src/Encina/Encina.csproj -c Release` → "Compilación correcta. 0 Advertencia(s) 0 Errores" with `TreatWarningsAsErrors=true` (`Directory.Build.props:24`), so RS0016/RS0017 report nothing. |
| AUD-10 | pass | Same Release build (0 warnings) — CS1591-class doc-comment warnings would fail the build with `TreatWarningsAsErrors=true`; none were raised. |
| AUD-11 | n/a (out of unit) | `Encina`'s package README and Diátaxis pages are shared with the whole package, not scoped to this folder triple; a README/docs review is better run once for the whole `Encina` package audit, not duplicated per top-level folder. Flagged as a gap to close when the `Encina` package's docs are audited. |
| AUD-12 | pass | `RequestDispatcher.ExecuteAsync` and `NotificationDispatcher.ExecuteAsync` return `Left`/deny on every validation failure (null request, missing handler, wrong handler type) and let any exception that is not `OperationCanceledException` propagate (fail-fast/fail-closed) instead of swallowing it and reporting success — `Dispatchers/Encina.RequestDispatcher.cs:79-99`, `Pipeline/PipelineBuilder.cs:141-164` ("Pure ROP: Any other exception ... will propagate to let the application crash (fail-fast)"). |
| AUD-13 | **finding (blocker, F-01, F-02)** | `EncinaError.Message`/`Exception.Message` reach OpenTelemetry activity tags and structured logs in four places. See Findings. |
| AUD-14 | finding (major, F-03) | `Core/RequestContext.cs:125` (`Create(string correlationId)`) and `:151` (`CreateForTest`) read `DateTimeOffset.UtcNow` directly; the parameterless `Create()` in the same file correctly uses `TimeProvider.System.GetUtcNow()` (line 66), so the file is internally inconsistent with the rule it otherwise follows. |
| AUD-15 | n/a | No options class in this unit holds a password, connection string, token or key (`Options/NotificationDispatchOptions.cs` only has a dispatch strategy and a parallelism degree). |
| AUD-16 | n/a | This unit makes no database calls. |
| AUD-17 | finding (major, F-04) | `Core/ServiceCollectionExtensions.AddEncina` registers `IEncina`, `IRequestContextAccessor`, `IEncinaMetrics`, `IFunctionalFailureDetector` and `IModuleHandlerRegistry`, but no test in `tests/Encina.UnitTests/Core/*` (`EncinaTests.cs`, `RequestContextAccessorRegistrationTests.cs`, `NestedDispatchContextTests.cs`, `AmbientRequestContextTests.cs`) builds the provider with `ValidateOnBuild`/`ValidateScopes` both `true` — every `BuildServiceProvider()` call found (7 call sites in `RequestContextAccessorRegistrationTests.cs`) uses the default (both `false`). |
| AUD-18 | finding (major, F-05) | `Core/ServiceCollectionExtensions.cs:13-23` declares `AddApplicationMessaging` (two overloads) with the XML summary "Legacy alias for `AddEncina(...)`" — a compatibility alias without `[Obsolete]`, contradicting "No Legacy Code" / "No Migration Paths". |

## Coverage per flag (measured, scoped to this unit's own test classes)

Collected with `dotnet test ... --collect:"XPlat Code Coverage"`, one run per flag, filtered to the test classes that target these 25 files, then aggregated per the obligations model (sum of covered lines / sum of coverable lines across the unit's files) from the resulting Cobertura XML. This is a **floor**: it excludes any incidental coverage this unit's code gets from tests elsewhere in the suite (e.g., `AspNetCore`, `Compliance.*`, satellite `ServiceCollectionExtensions` tests that also call `Send`/`Publish`).

| Flag | Manifest target | Measured (this unit) | Verdict |
|---|---|---|---|
| unit | 70% | 1132/1247 = 90.8% | pass |
| guard | 20% | 601/1247 = 48.2% | pass |
| contract | 15% (package-level; only 4 files in this unit declare `contract`) | 189/220 = 85.9% (the four `*PipelineBehavior.cs` files only) | pass |
| property | not declared for this package | n/a | n/a — no file in this unit lists `property` in the manifest |
| integration | not declared for this package | n/a | n/a — no file in this unit lists `integration` in the manifest |

Per-file detail (unit / guard / contract-where-applicable) is in `artifacts/audit/coverage/*/*.cobertura.xml`, parsed with `artifacts/audit/parse-cov.cs`.

## Closed issues consulted

`gh issue list --repo dlrivada/Encina --state closed --search "<term> in:title,body"` for: `Pipeline`, `dispatcher`, `mediator`, `RequestContext`, `AddApplicationMessaging OR EncinaConfiguration`. Issues actually about this unit's files: #1147 (empty request context in behaviors), #1163 (unregistered `IRequestContext` at ~28 call sites), #33 (`Encina.Publish` guard consistency), #27 (source generators for zero-reflection dispatch, not shipped — dispatch is still reflection/expression-tree based, see `Dispatchers/Encina.NotificationDispatcher.cs`), #499 (Dogfood Phase 1: core package tests). Issues #1168, #1173, #1259, #1274 were read for the `EncinaError.Message` leakage precedent that AUD-13's findings repeat (that rule was written for `Encina.Messaging`'s outbox/dead-letter path; the core mediator was never re-audited against it).
