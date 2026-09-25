# AGENTS.md - Encina engineering rules

The operative rules for every contributor and every AI agent, whatever the tool. MUST and NEVER are binding. The reasoning, examples and history behind each rule are in the frozen [engineering handbook](docs/engineering/ENGINEERING-HANDBOOK.md) (the pre-2026-09-25 `CLAUDE.md`); "handbook: X" below names its section. Where this file and the handbook disagree, this file wins.

## 1. Project facts

- Encina is **pre-1.0**. There is no backward compatibility, no migration support and no existing user. Breaking changes are acceptable and encouraged when they improve the design. The library is renamed after 1.0.
- **.NET 10 only** (LTS, supported through November 2028); NEVER .NET 9 or older. Use the latest C# 14 features (extension members, the `field` keyword, user-defined compound assignment, null-conditional assignment, partial constructors and events, file-based apps). .NET 10 breaking changes are acceptable. Nullable reference types are enabled everywhere.
- Design principles: best solution first, never a compromise for compatibility; clean architecture (no legacy, deprecated or obsolete code); pay-for-what-you-use (every feature opt-in); provider-agnostic abstractions (EF Core, Dapper, ADO.NET, MongoDB).
- Before starting work, read the plan for your area in `docs/plans/` and continue from where it stopped. The active plans are listed in `CLAUDE.md` ("Active Plans"). The 1.0 scope is [SPEC-000](docs/specifications/SPEC-000-encina-1.0-baseline-and-release-scope.md).
- .NET 10 changes to know: `dotnet new sln` creates `.slnx`; `dotnet restore` audits transitive packages; OpenAPI 3.1 removed `OpenApiSchema.Nullable` (use `JsonSchemaType.Null`); `WebHostBuilder`/`IWebHost` are obsolete (use minimal hosting); `WithOpenApi` is deprecated; default container images use Ubuntu.

## 2. Scripting and tooling (MUST)

- MUST script only in PowerShell (`pwsh`) or C# 14 file-based apps (`dotnet run file.cs`, no `.csproj`). CLI tools (`dotnet`, `git`, `gh`, `docker`) are invoked directly.
- NEVER use `python`/`python3`; bash constructs (`for`/`do`/`done`, `if`/`then`/`fi`, pipes `|`, subshells `$(...)`); here-docs (`<< 'EOF'`); `sh -c`/`bash -c`; or Unix commands through Git Bash: `grep`, `find`, `cat`, `ls`, `sed`, `awk`, `head`, `tail`, `wc`, `sort`, `xargs`, `curl`, `tee`, `cut`, `paste`, `shuf`, `unzip`, `basename`, `xxd`, `dd`.
- Equivalents: `grep` → `Select-String`; `find` → `Get-ChildItem -Recurse`; `cat` → `Get-Content`; `head`/`tail` → `Select-Object -First/-Last`; `curl` → `Invoke-RestMethod`; `jq` → `ConvertFrom-Json`; `kill` → `Stop-Process`; `wc -l` → `(Get-Content f).Count`. Full table and the policy's rationale: handbook "Scripting & Tooling Policy".

## 3. Code rules

- NEVER add `[Obsolete]`, legacy code, compatibility aliases, migration helpers or compatibility layers. If something must change, change it completely. Every line serves a current purpose.
- Time comes from `TimeProvider`. Production code NEVER reads `DateTime.UtcNow` or `DateTimeOffset.UtcNow`; it takes `TimeProvider` by injection (an optional parameter defaulting to `TimeProvider.System` where a default is needed). Every store and repository supports it. (why: #543, #667)
- An options class with a password, connection string, token or key MUST mark that property `[JsonIgnore]` and override `ToString()` so logging, serialization and diagnostics cannot print it. (why: #851)
- Database calls MUST be asynchronous with a `CancellationToken` (`OpenAsync`, `BeginTransactionAsync`, `ExecuteNonQueryAsync` and siblings); NEVER the synchronous overloads such as `IDbConnection.Open()`. (why: Sonar S6966, thread-pool starvation; #794, #897)
- Registration completeness: an `AddEncina*` method that adds a service, orchestrator or hosted service MUST also register every option type and dependency it resolves, proven by a DI test that builds the provider with `ValidateOnBuild` and `ValidateScopes`. A database store MUST win over an in-memory default in any registration order without overriding the application's own registration. (why: #1260, #1273, #1285, #1289; #1269, #1295)
- Errors are NEVER swallowed in background infrastructure: a `Left` from `IEncina.Send`/`Publish` or from a store inside a processor, adapter, orchestrator or job fails that operation (retry, dead-letter or exception per its semantics), never reports success. (why: #1150-#1153, #1184)
- Compliance and security gates fail closed: when context is missing (no `HttpContext`, tenant or principal) or a lookup fails, the gate denies; the only opt-out is explicit and logged. (why: SPEC-002 DEC-006; #1143, #1145, #1148, #1155, #1161)
- `EncinaError.Message` NEVER reaches logs, activity tags, health-check results or plaintext storage; record only the error code or the exception type. (why: #1168, #1173, #1259, #1274)
- Railway Oriented Programming: operations return `Either<EncinaError, T>`; NEVER use exceptions for business logic; validation returns `Either` with detailed errors. (why: ADR-001, ADR-006)
- Every messaging pattern (Outbox: reliable at-least-once publishing; Inbox: idempotent exactly-once processing; Saga: orchestrated distributed transactions with compensation; Scheduling: delayed/recurring messages; Transactions: commit/rollback from the ROP result) is optional and disabled by default. NEVER make a pattern mandatory. Example: `config.UseOutbox = true;`.
- The repository pattern is optional and NEVER forced. Default to `DbContext` (already a unit of work) and `DbSet<T>`; consider a repository only for several DbContexts or databases, transactions with non-EF components, mocking, switching providers, or DDD aggregate repositories. Opt in per entity: `services.AddEncinaRepository<Order, OrderId>();`.
- Provider coherence: abstractions (`IOutboxStore`, `IInboxStore`, ...) and options live in `Encina.Messaging`; providers share the same interfaces and configuration and differ only in implementation, so switching provider means changing the DI registration. NEVER mix provider-specific code into abstractions.
- Validation uses the orchestrator pattern: `IValidationProvider`, `ValidationOrchestrator`, `ValidationPipelineBehavior<,>`, `ValidationResult` and `ValidationError` live in core `Encina.Validation`; each validation package implements `*ValidationProvider` and its registration (`AddEncinaFluentValidation(assembly)`, `AddDataAnnotationsValidation()`, `AddMiniValidation()`) registers provider, orchestrator and behavior.
- Scheduling: Encina scheduling carries domain messages (commands, queries, notifications); Hangfire/Quartz run infrastructure jobs. They coexist; adapters to use them as scheduling backends are future work.
- Event-sourced compliance modules have no InMemory stores: unit tests mock `IAggregateRepository` (NSubstitute) and integration tests run against Marten on PostgreSQL through Testcontainers. Marten projections take dependencies through `IDocumentOperations` and constructor injection, NEVER `IServiceProvider`; keep at least one test that registers each projection with a real store. (why: #777, #783, #784, #785, #949; ADR-019)
- Pattern choice: Outbox for publishing domain events, Inbox for external messages (webhooks, queue consumers), Saga for cross-service transactions, Scheduling for delayed domain operations, Transactions for automatic commit/rollback.

## 4. Naming

- Entities: `OutboxMessage`, `InboxMessage`, `SagaState`, `ScheduledMessage` (never `Message`, `Saga`, `ScheduledCommand`).
- Properties: `RequestType`/`NotificationType` (not `MessageType`); `ErrorMessage` (never `Error`, CA1716); UTC timestamps with the `AtUtc` suffix (`CreatedAtUtc`, `ProcessedAtUtc`, `ScheduledAtUtc`; sagas: `StartedAtUtc`, `LastUpdatedAtUtc`, `CompletedAtUtc`); `RetryCount`, `NextRetryAtUtc` (not `AttemptCount`); descriptive identifiers (`SagaId`, not `Id`, when implementing an interface).
- Stores: `{Pattern}Store{Provider}` (`OutboxStoreEF`, `OutboxStoreDapper`), never a bare `Store` or `Repository`.
- Feature-specific stores and helpers inside a provider package live in a folder named after the feature (`LawfulBasis/`, `Consent/`), not after the package defining the abstraction (`GDPR/`). (why: #413)

## 5. Providers

**Database: every provider-dependent feature MUST be implemented for all 10 providers.** It applies to every store (outbox, inbox, saga, scheduled message, ...), repositories, unit of work, bulk operations, anything with database-specific SQL or connection types, and registrations in `ServiceCollectionExtensions`.

| Family | Providers | SQL notes |
| --- | --- | --- |
| ADO.NET | SqlServer, PostgreSQL, MySQL | SQL Server: `@param`, `TOP (@n)`, `bit`, native DateTime/GUID |
| Dapper | SqlServer, PostgreSQL, MySQL | PostgreSQL: `@param`, `LIMIT @n`, `true/false`, case-sensitive identifiers |
| EF Core | SqlServer, PostgreSQL, MySQL | MySQL: `@param`, `LIMIT @n`, `0/1`, backtick identifiers |
| MongoDB | MongoDB | |

Oracle (ADR-009, code in `.backup/oracle/`) and SQLite (ADR-024, packages in `.backup/`, not built, tested or guaranteed) are out of the matrix. Message brokers, caching and event sourcing (Marten) are excluded from the database rule; they follow their own category below.

**Specialized categories: a feature that touches one MUST be consistent across every provider of that category.**

| Category | Providers | 1.0 scope | Every provider MUST support |
| --- | --- | --- | --- |
| Caching (8) | Memory (L1), Hybrid (L1+L2), Redis, Valkey, Dragonfly, Garnet, KeyDB, Memcached (planned) | all 8 (SPEC-000 REQ-027, #277) | Get/Set/Remove; TTL/expiration; serialization abstraction; pub/sub backplane where applicable. Applies to `ICacheProvider`, `IPubSubProvider`, stampede protection, eager refresh, fail-safe, tag invalidation, read/write-through |
| Transports (10 + 6 planned) | RabbitMQ, AzureServiceBus, AmazonSQS, Kafka, NATS, Redis.PubSub, MQTT, InMemory, gRPC, GraphQL; planned (v0.15.0): GoogleCloudPubSub, AmazonEventBridge, Pulsar, Redis.Streams, ActiveMQ, Dapr | the 10 existing | Send/Publish; subscription management; error handling and DLQ; metadata propagation. Applies to `IMessageTransport`, outbox publishing, inbox consumption, DLQ |
| Distributed locks | InMemory (testing), Redis (Redlock), SqlServer (`sp_getapplock`), PostgreSQL (`pg_advisory_lock`, #207), MySQL (`GET_LOCK`, #208); post-1.0: Azure Blob, DynamoDB, Consul, etcd, ZooKeeper | exactly these 5; a lock feature is complete for 1.0 when they are covered (SPEC-000 DEC-003) | TryAcquire with timeout; auto-release on timeout; `CancellationToken`. Applies to `IDistributedLockProvider`, leader election, resource coordination |
| Validation (3) | FluentValidation, DataAnnotations, MiniValidator | all 3 | integrate with `ValidationOrchestrator`; return `ValidationResult`; same `ValidationPipelineBehavior`. Applies to `IValidationProvider` and its registration |
| Cloud | AwsLambda, AzureFunctions; GoogleCloudFunctions (#205) | AWS + Azure; GCP is post-1.0 (SPEC-000 DEC-003): note the GCP gap in the issue | consider the AWS/Azure/GCP triangle for any cloud-specific feature |

Other categories: Scheduling (built-in `Encina.Messaging`, Hangfire, Quartz; applies to `IScheduledMessageStore` and scheduler adapters); Event sourcing (Marten primary, EventStoreDB future; applies to aggregate repositories, projections, snapshots, crypto-shredding); Resilience (Polly, Extensions.Resilience, Extensions.Http.Resilience); Observability (OpenTelemetry; planned exporters AzureMonitor, AwsXRay, Prometheus); Testing packages (`Encina.Testing`, `.Fakes`, `.Respawn`, `.WireMock`, `.Shouldly`, `.Verify`, `.Bogus`, `.FsCheck`, `.Architecture`, `.Testcontainers`, `.TUnit`, `.Pact`).

**Applicability matrix** (✅ required; ◐ where applicable; empty: not applicable):

| Feature | Database (10) | Caching (8) | Transport | Lock | Validation (3) |
| --- | :-: | :-: | :-: | :-: | :-: |
| Outbox / Inbox / Saga, Scheduled messages, Unit of Work, Audit trail | ✅ | | | | |
| Query caching | | ✅ | | | |
| Message publishing | | | ✅ | | |
| Resource locking | | | | ✅ | |
| Request validation | | | | | ✅ |
| Multi-tenancy | ✅ | ◐ | ◐ | | |

Rule of thumb: if a feature touches provider-specific code, implement it consistently across ALL providers of that category (new cache pattern → 8 caches; transport-agnostic feature → all transports; coordination → all lock providers; validation pattern → all 3; cloud feature → the triangle).

## 6. Cross-cutting integration check (MUST, ADR-018)

Every feature that creates entities, stores, pipeline behaviors, background services or external integrations MUST be evaluated against all 12 functions. For each one record, in the plan or the PR: **Integrate** (implement now), **Defer** (open an issue and reference it) or **Not applicable** (one sentence why).

| # | Function | Question | Integration point |
| --- | --- | --- | --- |
| 1 | Caching | Reads data that benefits from caching? | `ICacheProvider`, decorator, `[Cache]` |
| 2 | OpenTelemetry | Operations worth tracing/metering? | `ActivitySource`, `Meter`, semantic attributes |
| 3 | Structured logging | Needs operational visibility? | `Log.cs` with `[LoggerMessage]`, EventId range |
| 4 | Health checks | Has a checkable dependency? | `IEncinaHealthCheck` |
| 5 | Validation | Receives input? | `IValidationProvider`, pipeline behavior |
| 6 | Resilience | Calls external systems? | Polly retry, circuit breaker, timeout |
| 7 | Distributed locks | Concurrent access to shared state? | `IDistributedLockProvider` |
| 8 | Transactions | Needs atomic multi-operation guarantees? | `IUnitOfWork`, `TransactionPipelineBehavior` |
| 9 | Idempotency | Can receive duplicates? | `InboxPipelineBehavior`, deduplication key |
| 10 | Multi-tenancy | Stores/queries tenant data? | `TenantId`, `ITenantContext` |
| 11 | Module isolation | Needs module scoping? | `ModuleId`, `IModuleContext` |
| 12 | Audit trail | Compliance/security implications? | `IAuditStore`, audit events |

Common misses: a new store or entity misses OpenTelemetry, `TenantId`, `ModuleId`, health check; a background service misses locks, leader election, logging; an external integration misses resilience, health check, OpenTelemetry; a pipeline behavior misses validation, idempotency, audit; a messaging pattern misses transactions, locks, multi-tenancy.

## 7. Structured logging and EventIds (MUST, ADR-021)

- NEVER use a `[LoggerMessage]` EventId without first registering its range in `src/Encina/Diagnostics/EventIdRanges.cs` (`public static readonly (int Min, int Max) Name = (min, max);`, discovered by `EventIdRanges.GetAllRanges()`), and NEVER assign an EventId outside your package's range.
- Workflow: pick the next free range in the right area (typically 50 or 100 slots); register it; write `Diagnostics/*LogMessages.cs` with EventIds inside it; add the new field to `PublicAPI.Unshipped.txt`; add the assembly to the `AssemblyRanges` map of `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` (the test fails otherwise); run the architecture tests.
- Pack EventIds sequentially; NEVER sparse allocations (8400, 8410, 8420). Group them by functional area. Use the `[LoggerMessage]` source generator, not `LoggerMessage.Define`, for new code; existing `LoggerMessage.Define` calls need one literal `new EventId(<n>, ...)` each, which the test scans (#1125). Add XML docs naming the range (`/// Event IDs: 8120-8133 (see EventIdRanges.ComplianceGDPR)`).
- `EventIdUniquenessRule` (`Encina.Testing.Architecture`) asserts every `[LoggerMessage]` has an EventId, every EventId is in a range mapped to its assembly, and no ranges overlap.
- The registry is the source of truth for the range map. Free ranges as of 2026-09-25: 300-1099, 5400-6999, 7100-7999, 8950-8999, 9700-9999 (area map: handbook "Current Range Map").

## 8. Build, analysis, public API and documentation

- Zero warnings. Every CA warning is fixed or suppressed with a justification: CA1848 may be suppressed when the LoggerMessage optimisation is future work; CA2263 when dynamic serialization needs the non-generic overload; CA1716 is fixed by renaming (`Error` → `ErrorMessage`).
- Public API is tracked by `PublicAPI.Shipped.txt` and `PublicAPI.Unshipped.txt`: RS0016 → add the symbol to Unshipped; RS0017 → remove the stale line; RS0036/RS0037 → fix the nullable annotation. Format `Namespace.Type.Member(params) -> ReturnType`, with `string!` (non-null) and `string?` (nullable).
- XML comments on every public API, with examples when helpful. Each satellite package has its own README. Architectural decisions get an ADR in `docs/architecture/adr/`.
- The full `Encina.slnx` builds; no `.slnf` needed. Keep `-maxcpucount:1 -nodeReuse:false` in `Directory.Build.rsp` (parallel MSBuild triggers CLR error 0x80131506). Load tests are excluded from standard CI (a .NET 10 JIT bug); run them locally with `DOTNET_JitObjectStackAllocationConditionalEscape=0`. (why: #5, #496)
- CI enforces: all tests pass, zero warnings, formatting, public API compatibility.

## 9. Testing obligations

**Coverage is per flag (unit, guard, contract, property, integration).** Each flag MUST independently reach its own target from `.github/coverage-manifest/{Package}.json`; a line covered by one flag does not count for another, and each flag has its own coverable lines. There is no project-wide percentage; branch and method coverage are informational. `.github/scripts/coverage-report.cs` computes it; the [coverage dashboard](https://dlrivada.github.io/Encina/coverage/) is the result. Check the manifest before starting, run `dotnet test` locally, and do NOT push or merge until every applicable flag reaches its target (CI Full takes ~40 minutes; do not trigger it with incomplete work). Details: [coverage methodology](docs/testing/coverage-measurement-methodology.md).

- **Tests MUST execute real package code**: instantiate and call implementations, factories and validators. Reflection-only tests (`typeof(I).GetMethod(...)`) or type-only assertions cover zero lines; contract and property tests MUST instantiate real implementations.
- Documentation NEVER types coverage figures by hand; it cites `cov:<Package>/<path>.cs` with `<!-- covref-table: glob -->` / `<!-- covref: id:field -->` markers (SPEC-001). Mutation data is cited with `<!-- mutref-table: mut:... -->` / `<!-- mutref: id:field -->`; markers inside fenced code blocks are not expanded.
- Mutation score is per file with no project-wide target ([methodology](docs/testing/mutation-measurement-methodology.md), [guide](docs/en/guides/MUTATION_TESTING.md)). When adding or renaming a folder in `FOLDERS` of `.github/workflows/mutation-tests.yml`, update the parallel `FILTERS` array in the same step (empty entry = config default). The test filter goes in `stryker-config.json` (`test-case-filter`), because Stryker.NET 4.14 ignores it on the CLI. Why Stryker runs in `AllTests` mode with per-folder filters and a matrix: handbook "Mutation Testing System" (#1027, #1028).

Test projects (consolidated, one per type, under `tests/`):

- `Encina.UnitTests`: always required; one method in isolation, dependencies mocked, fast (<1 ms per test).
- `Encina.GuardTests`: every public method with parameters; null checks throw `ArgumentNullException` (GuardClauses.xUnit).
- `Encina.ContractTests`: public interfaces and abstract classes keep their contract.
- `Encina.PropertyTests`: FsCheck invariants for complex logic and cross-provider behavior.
- `Encina.IntegrationTests`: real databases and externals via Docker/Testcontainers, full workflows, `[Trait("Category", "Integration")]`.
- `Encina.LoadTests` (concurrent, performance-critical code) and `Encina.BenchmarkTests` (hot paths, BenchmarkDotNet).
- Shared infrastructure in `Encina.TestInfrastructure`; reference examples in `Encina.Testing.Examples`.
- Tests of a provider-dependent feature cover all 10 database providers.

**Required test types by feature category:**

| Test type | Database features | Non-database features |
| --- | --- | --- |
| Unit, Guard | required | required |
| Property | required | if the logic is complex |
| Contract | required | if public API |
| Integration | **required, real databases; NEVER a `.md` justification** | justify if skipped |
| Load | only concurrent features (implement for Unit of Work, multi-tenancy, read/write separation; justify for repository, specification, module isolation) | justify if skipped |
| Benchmark | only hot paths (implement for read/write separation; maybe specification; justify for repository, UoW, multi-tenancy, module isolation) | justify if skipped |

**Justification files**: when a test type is legitimately skipped, write `{TestProject}/{Provider or Feature}/{Feature}.md` with: title `# {Test Type} - {Provider} {Feature}`, `## Status: Not Implemented`, `## Justification` with numbered reasons, "Adequate Coverage from Other Test Types" (unit, guard, property, contract), "Recommended Alternative", `## Related Files`, `## Date:` and `## Issue:`. Allowed for benchmarks and load tests of thin or non-concurrent code; NEVER for unit, guard or contract tests, nor for integration tests of database features. A folder with neither `.cs` tests nor a `.md` justification means the coverage was not evaluated.

**Integration test fixtures** (shared containers, ~23 instead of ~71):

- Every database integration test class MUST use a shared `[Collection("<Family>-<Database>")]` fixture, plus `[Trait("Category", "Integration")]` and `[Trait("Database", "<Db>")]`. NEVER create a per-class fixture for database tests.
- Collections: `ADO-`, `Dapper-` × `SqlServer`/`PostgreSQL`/`MySQL` use `SqlServerFixture`, `PostgreSqlFixture`, `MySqlFixture`; `EFCore-SqlServer`/`-PostgreSQL`/`-MySQL` use `EFCoreSqlServerFixture`, `EFCorePostgreSqlFixture`, `EFCoreMySqlFixture` (defined in `Collections.cs` files).
- NEVER use `IClassFixture<T>` for database fixtures; NEVER `new SqlServerFixture()` or `_fixture = new()`; NEVER call the fixture's `DisposeAsync()` from a test (the collection owns the lifecycle).
- Inject the fixture through the constructor, call `_fixture.ClearAllDataAsync()` in `InitializeAsync()`, use `_fixture.CreateConnection()` for shared connections. Details: [integration tests](docs/testing/integration-tests.md#collection-fixture-strategy).
- Local services: `docker compose --profile core|databases|messaging|caching|cloud|observability|full up -d` ([Docker guide](docs/infrastructure/docker-infrastructure.md)); run the suite with `dotnet run --file .github/scripts/run-integration-tests.cs`.

**Test quality**:

- Descriptive names (no `Test1`), Arrange-Act-Assert, one behavior per test, independent (no shared state), deterministic, resources cleaned up.
- NEVER skip a test without justification, ignore a flaky test (fix or delete it), test implementation details, use `Thread.Sleep`, or hard-code paths, dates or GUIDs when avoidable.
- Build test data with builders.
- Assertions use Shouldly through `Encina.Testing.Shouldly`; NEVER FluentAssertions (commercial licence). Test projects reference the `Encina.Testing.*` wrappers (Shouldly, Bogus, FsCheck, Verify, WireMock, Testcontainers, Fakes), not the raw libraries. (why: #429, #495, #1023)

**Workflow**: unit tests first, then the feature, then the other types by risk; before committing run `dotnet test Encina.slnx --configuration Release` (optionally `--collect "XPlat Code Coverage"` and `dotnet run --file .github/scripts/run-stryker.cs`). Balance thoroughness with velocity: test critical paths, complex logic and public APIs.

**Outputs** go under `artifacts/`, never the repository root:

- `artifacts/test-results/` (`dotnet test --results-directory artifacts/test-results`), `artifacts/coverage/` (collectors `--output artifacts/coverage`), `artifacts/performance/`, `artifacts/load-metrics/`, `artifacts/mutation/`.
- `runsettings` files and scripts write to `artifacts/` subdirectories.
- NEVER create `TestResults/`, `test-results.log`, `coverage-*` or `BenchmarkDotNet.Artifacts/` at the root.

**BenchmarkDotNet** (why: #564):

- Use `BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config)`, NEVER `BenchmarkRunner.Run<T>()` (it ignores `--filter`).
- NEVER return `IQueryable<T>` from a benchmark; materialize with `.ToList()`.
- Verify the filter with `--list flat --filter "..."` before a full run; use `--job short` or `--job dry` for quick validation.
- Benchmark entities MUST match the operations (only `AddInclude` existing navigation properties); results go to `artifacts/performance/`.

## 10. Language and git

- The maintainer writes in Spanish; answer the maintainer in Spanish. Code, comments, documentation and commit messages are **English only**; translate any Spanish comment you touch.
- NEVER force-push `main`/`master`. Commit messages are clear, descriptive English.
- NEVER add AI attribution: no `Co-Authored-By: Claude...`, no "Generated with ..." lines, no reference to AI assistance in commits or PRs. Commits appear authored solely by the repository owner.
- GitHub Actions `permissions:` are declared per job, never at workflow level. (why: Sonar S8264, #896)

## 11. Issues, plans and changelog

- Every bug, feature and debt item is a GitHub issue (<https://github.com/dlrivada/Encina/issues>). NEVER leave an identified problem unresolved or unrecorded: fix it now, or, when fixing would derail the current work, open an issue and continue; list the issues you opened in your summary. NEVER track issues or known problems in `AGENTS.md`/`CLAUDE.md`.
- Use the matching template of `.github/ISSUE_TEMPLATE/`:

| Template | Prefix | Default label | Use for |
| --- | --- | --- | --- |
| `bug_report.md` | `[BUG]` | `bug` | wrong behavior in Encina code, including failing tests caused by a code bug |
| `feature_request.md` | `[FEATURE]` | `enhancement` | new features or enhancements |
| `technical_debt.md` | `[DEBT]` | `technical-debt` | code that works but is messy, duplicated, incomplete or slow |
| `test_implementation.md` | `[TEST]` | `area-testing` | missing tests, a coverage flag below target, load tests, benchmarks |
| `architecture_spike.md` | `[SPIKE]` | `investigation` | time-boxed investigations, evaluations, architecture decisions |
| `epic.md` | `[EPIC]` | `epic` | a multi-issue initiative; create the child issues too |
| `refactoring.md` | `[REFACTOR]` | `enhancement` | restructuring without behavior change |
| `infrastructure.md` | `[INFRA]` | `area-ci-cd` | CI/CD, Docker, build system, developer tooling |

- Write clear, technical titles, concrete acceptance criteria and the same terminology in issue and PR, so linked-issue validation can judge the PR.
- Prefixes are normalized: `[TECH-DEBT]` → `[DEBT]`; `[TESTING]` → `[TEST]`; `[ARCHITECTURE]`/`[DECISION]`/`[REVIEW]` → `[SPIKE]`; no `[Phase N]` prefixes (use milestones).
- An issue body uses the headers of its template **verbatim and in order**, every section filled and the applicable checkboxes ticked (`[x]`); read the template first. `technical_debt`: Type, Description, Location, Current Behavior, Expected Behavior, Root Cause, Proposed Fix, Priority, Effort Estimate, Related Issues. `bug_report`: Description, Steps to Reproduce, Expected Behavior, Actual Behavior, Environment, Code Sample, Stack Trace, Additional Context (plus Root Cause when known). House style: #1050 (`[DEBT]`), #949 (`[BUG]`).
- A `[FEATURE]` issue of any size gets an implementation plan at `docs/plans/{feature}-implementation-plan-{issue}.md`, generated with [the plan prompt](docs/engineering/prompts/implementation-plan-prompt.md) (style: `docs/plans/dsr-implementation-plan-404.md`) and linked from the issue before implementation starts.
- Workflow: issue → plan (features) → assign and move to In Progress → reference it in the commit or PR (`Fixes #123` or `Closes #123`) → it closes on merge.
- Changelog: NEVER edit `CHANGELOG.md` by hand. A user-visible change adds `changelog.d/<issue>-<slug>.<section>.md` (section: `added`, `changed`, `deprecated`, `removed`, `fixed`, `security`; see `changelog.d/README.md`). Releases fold fragments with `dotnet run .github/scripts/changelog-fragments.cs -- --release <version> <yyyy-MM-dd> [title]` (maintainer only).
- Other records: `ROADMAP.md` (roadmap), `docs/releases/vX.Y.Z/README.md` (update after a major implementation phase), `docs/architecture/adr/` (after an architectural decision), `docs/roadmap-documentacion.md` (documentation roadmap).

## 12. When doing X, read Y

| When | Read |
| --- | --- |
| Writing or reviewing a page under `docs/`, a README or CONTRIBUTING | `.claude/skills/encina-docs/SKILL.md`; `.opencode/agents/encina-docs.md` |
| Opening an issue | `.claude/skills/open-issue/SKILL.md` |
| Planning a feature | `.claude/skills/implementation-plan/SKILL.md`, the plan prompt |
| Checking provider coherence, cross-cutting functions, EventIds, the test workflow or a release | `.opencode/skills/{provider-coherence, cross-cutting-check, eventid-allocation, test-workflow, release-checklist}/SKILL.md` |
| Delegating to the local model | `.claude/skills/local-ai-task/SKILL.md`, `docs/engineering/ai-task-routing.md` |
| Scope of 1.0, regulatory readiness, citations, the closed-issue audit | SPEC-000, SPEC-002, SPEC-001, SPEC-003 in `docs/specifications/` |
| A design decision already taken | `docs/architecture/adr/index.md` (ROP 001/006, providers 009/024, cross-cutting 018, Marten 019/027, EventIds 021, events 028, recoverability 029) |
| Tests and measurement | `docs/testing/` (coverage, mutation, performance, integration tests, load baselines), `docs/en/guides/TESTING.md` |
| How the project is built and why | `docs/engineering/HOW-ENCINA-IS-BUILT.md`, `docs/engineering/AI-DEVELOPMENT-MODEL.md`, `docs/engineering/PROJECT-HISTORY.md` |
| The reasoning and examples behind a rule here | [engineering handbook](docs/engineering/ENGINEERING-HANDBOOK.md) (frozen snapshot) |
| Claude Code specifics (agents, hooks, orchestration, CodeRabbit) | `CLAUDE.md`, `.claude/agents/README.md` |
