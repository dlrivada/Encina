# Contributing to Encina: onboarding guide

> Everything a new contributor needs before the first pull request: how to prepare a machine, where things live, how the pieces relate, how a change travels from an idea to `main`, and the rules that will otherwise bite. This guide describes the repository as it is; the rules it cites live in [`CLAUDE.md`](../../CLAUDE.md), the specifications in [`docs/specifications/`](../specifications/README.md) and the working method in [`docs/engineering/HOW-ENCINA-IS-BUILT.md`](../engineering/HOW-ENCINA-IS-BUILT.md). Tracking: #1103 (EPIC #1102). Sections 2, 3 and 5 were first drafted by the project's local model from `CLAUDE.md` and then reviewed.

## 1. Prepare your machine

What you need, and the first commands that prove the setup works. Times are indicative for a first run on a normal laptop.

### Install

| Tool | Version | Why | Check |
|---|---|---|---|
| .NET SDK | 10.0.x (the only supported target framework; `TargetFramework` is `net10.0` in `Directory.Build.props`) | Build and tests | `dotnet --version` prints `10.0.…` |
| Docker Desktop (or Docker Engine + Compose v2) | current | Integration tests run against real databases and brokers through Testcontainers and the `docker-compose.yml` profiles | `docker compose version` |
| PowerShell 7 (`pwsh`) | 7.x | The only scripting shell the project uses; every helper script is PowerShell or a C# file-based app | `pwsh --version` |
| Git | current, with worktree support | Branch-per-task, worktree-per-agent workflow | `git --version` |
| GitHub CLI (`gh`) | current, authenticated | Issues, pull requests, checks | `gh auth status` |

The SDK version is not yet pinned by a `global.json`; that is a 1.0 requirement (SPEC-000 REQ-020) still open. Until it lands, install the latest 10.0 SDK.

### Clone to green (about 15 minutes the first time)

```bash
git clone https://github.com/dlrivada/Encina.git
cd Encina
dotnet restore Encina.slnx
dotnet build Encina.slnx --configuration Release --no-restore
dotnet format Encina.slnx --verify-no-changes
```

The build must end with `0 Warning(s)` and `0 Error(s)`: warnings are errors in this repository, including NuGet audit advisories (`Directory.Build.props`). `Directory.Build.rsp` limits MSBuild to one node; leave it, it prevents an intermittent CLR crash on this solution.

Run one fast test project to confirm the test stack:

```bash
dotnet test tests/Encina.GuardTests/Encina.GuardTests.csproj --configuration Release --no-build --results-directory artifacts/test-results
```

### Databases for integration tests (optional until you touch a provider)

```bash
docker compose --profile core up -d
```

Profiles: `core`, `databases`, `messaging`, `caching`, `cloud`, `observability`, `full` (`docs/infrastructure/docker-infrastructure.md`). Most integration test classes start their own containers through Testcontainers and only need Docker running; the compose profiles are for the suites that expect a shared service.

Then run the integration tests of one provider, for example ADO.NET:

```bash
dotnet test tests/Encina.IntegrationTests/Encina.IntegrationTests.csproj --configuration Release --filter "FullyQualifiedName~IntegrationTests.ADO" --results-directory artifacts/test-results
```

### Editors

Any editor works. The repository ships `.editorconfig`; `dotnet format` is the formatter of record and CI rejects unformatted code. Analyzer rules come from the packages referenced in `Directory.Build.props`, so IDE warnings match CI.

## 2. Repository map

A fresh clone of the Encina repository contains the solution file, root configuration files, source code, tests, documentation, and CI assets (source: repository listing). The structure is organized to keep source, tests, and documentation in separate top-level folders (source: repository listing).

### 2.1 The solution

The root contains `Encina.slnx`, which is the solution file for the project (source: repository listing). `Directory.Build.props`, `Directory.Packages.props`, and `Directory.Build.rsp` are MSBuild configuration files located at the root that apply settings to all projects in the solution (source: repository listing). The project uses .NET 10 exclusively (source: CLAUDE.md, Technology Stack); the SDK version is not yet pinned by a `global.json`, which SPEC-000 REQ-020 requires before 1.0. The solution builds without issues after the test consolidation, and solution filters (`.slnf`) are no longer needed (source: CLAUDE.md, Build Environment Known Issues).

### 2.2 `src/` by family

The `src/` directory holds the 105 projects of the solution plus the two unsupported SQLite packages, grouped into functional families (source: repository listing; CLAUDE.md, Multi-Provider Implementation Rule). The core family includes `Encina` and `Encina.Messaging`, which provide the base abstractions and shared messaging interfaces like `IOutboxStore` and `IInboxStore` (source: CLAUDE.md, Provider Coherence). Database providers are split into ADO.NET, Dapper, and EF Core families, each supporting SqlServer, PostgreSQL, and MySQL (source: CLAUDE.md, Multi-Provider Implementation Rule). The ADO family includes `Encina.ADO.SqlServer`, `Encina.ADO.PostgreSQL`, `Encina.ADO.MySQL`, and the unsupported `Encina.ADO.Sqlite` (source: repository listing). The Dapper family includes `Encina.Dapper.SqlServer`, `Encina.Dapper.PostgreSQL`, `Encina.Dapper.MySQL`, and the unsupported `Encina.Dapper.Sqlite` (source: repository listing). The EF Core family includes `Encina.EntityFrameworkCore` (source: repository listing). The MongoDB family includes `Encina.MongoDB` (source: repository listing).

Caching providers include `Encina.Caching`, `Encina.Caching.Memory`, `Encina.Caching.Hybrid`, `Encina.Caching.Redis`, `Encina.Caching.Valkey`, `Encina.Caching.Dragonfly`, `Encina.Caching.Garnet`, and `Encina.Caching.KeyDB` (source: repository listing). These implement `ICacheProvider` for different backends (source: CLAUDE.md, Caching Providers). Messaging transports include `Encina.RabbitMQ`, `Encina.AzureServiceBus`, `Encina.AmazonSQS`, `Encina.Kafka`, `Encina.NATS`, `Encina.Redis.PubSub`, `Encina.MQTT`, `Encina.InMemory`, `Encina.gRPC`, and `Encina.GraphQL` (source: repository listing). These implement `IMessageTransport` for various brokers and protocols (source: CLAUDE.md, Messaging Transport Providers). Distributed locks include `Encina.DistributedLock`, `Encina.DistributedLock.InMemory`, `Encina.DistributedLock.Redis`, and `Encina.DistributedLock.SqlServer` (source: repository listing). Validation providers include `Encina.FluentValidation`, `Encina.DataAnnotations`, and `Encina.MiniValidator` (source: repository listing). Scheduling includes `Encina.Hangfire` and `Encina.Quartz` (source: repository listing). Event sourcing includes `Encina.Marten`, `Encina.Marten.GDPR`, and `Encina.Audit.Marten` (source: repository listing). Cloud/serverless includes `Encina.AzureFunctions` and `Encina.AwsLambda` (source: repository listing). Resilience includes `Encina.Polly`, `Encina.Extensions.Resilience` (source: repository listing). Observability includes `Encina.OpenTelemetry` (source: repository listing). Security includes `Encina.Security`, `Encina.Security.ABAC`, `Encina.Security.ABAC.Analyzers`, `Encina.Security.AntiTampering`, `Encina.Security.Audit`, `Encina.Security.Encryption`, `Encina.Security.PII`, `Encina.Security.Sanitization`, `Encina.Security.Secrets` and its backend packages (`Encina.Security.Secrets.*`; the former `Encina.Secrets.*` family was removed by SPEC-000 DEC-001) (source: repository listing; CLAUDE.md, Active Plans). Compliance includes `Encina.Compliance.AIAct`, `Encina.Compliance.Anonymization`, `Encina.Compliance.Attestation`, `Encina.Compliance.BreachNotification`, `Encina.Compliance.Consent`, `Encina.Compliance.CrossBorderTransfer`, `Encina.Compliance.DataResidency`, `Encina.Compliance.DataSubjectRights`, `Encina.Compliance.DPIA`, `Encina.Compliance.GDPR`, `Encina.Compliance.LawfulBasis`, `Encina.Compliance.NIS2`, `Encina.Compliance.PrivacyByDesign`, `Encina.Compliance.ProcessorAgreements`, and `Encina.Compliance.Retention` (source: repository listing). Testing packages include `Encina.Testing`, `Encina.Testing.Architecture`, `Encina.Testing.Bogus`, `Encina.Testing.Fakes`, `Encina.Testing.FsCheck`, `Encina.Testing.Pact`, `Encina.Testing.Respawn`, `Encina.Testing.Shouldly`, `Encina.Testing.Testcontainers`, `Encina.Testing.TUnit`, `Encina.Testing.Verify`, and `Encina.Testing.WireMock` (source: repository listing). The remaining packages cover DDD building blocks, tenancy, change data capture, identifiers, message encryption and web or CLI integration: `Encina.Cli`, `Encina.Refit`, `Encina.SignalR`, `Encina.AspNetCore`, `Encina.Tenancy`, `Encina.Tenancy.AspNetCore`, `Encina.DomainModeling`, `Encina.Cdc`, `Encina.Cdc.Debezium`, `Encina.Cdc.MongoDb`, `Encina.Cdc.MySql`, `Encina.Cdc.PostgreSql`, `Encina.Cdc.SqlServer`, `Encina.GuardClauses`, `Encina.IdGeneration`, `Encina.Messaging.Encryption`, `Encina.Messaging.Encryption.AwsKms`, `Encina.Messaging.Encryption.AzureKeyVault`, `Encina.Messaging.Encryption.DataProtection`, and `Encina.Aspire.Testing` (source: repository listing).

| Family | Count | Example Packages | Purpose |
|--------|-------|------------------|---------|
| Core | 2 | `Encina`, `Encina.Messaging` | Base abstractions and messaging interfaces |
| ADO | 4 | `Encina.ADO.SqlServer` | ADO.NET data access |
| Dapper | 4 | `Encina.Dapper.SqlServer` | Dapper data access |
| EF Core | 1 | `Encina.EntityFrameworkCore` | Entity Framework Core data access |
| MongoDB | 1 | `Encina.MongoDB` | MongoDB data access |
| Caching | 8 | `Encina.Caching.Redis` | Caching providers |
| Messaging | 10 | `Encina.RabbitMQ` | Message transport implementations |
| Distributed Locks | 4 | `Encina.DistributedLock.Redis` | Distributed locking |
| Validation | 3 | `Encina.FluentValidation` | Input validation |
| Scheduling | 2 | `Encina.Hangfire` | Background job scheduling |
| Event Sourcing | 3 | `Encina.Marten` | Event store and projections |
| Cloud | 2 | `Encina.AzureFunctions` | Serverless function hosting |
| Resilience | 2 | `Encina.Polly` | Retry and circuit breaker patterns |
| Observability | 1 | `Encina.OpenTelemetry` | Tracing and metrics |
| Security | 12 | `Encina.Security.ABAC` | Security and PII handling |
| Compliance | 15 | `Encina.Compliance.GDPR` | Regulatory compliance |
| Testing | 12 | `Encina.Testing` | Test helpers and fakes |
| Domain, tenancy, CDC, identifiers, encryption and tooling | 18 | `Encina.DomainModeling`, `Encina.Tenancy`, `Encina.Cdc`, `Encina.IdGeneration`, `Encina.Messaging.Encryption`, `Encina.Cli` | DDD building blocks, multi-tenancy, change data capture, id generation, message encryption, CLI and web integration |

### 2.3 `tests/` by test type

The `tests/` directory contains consolidated test projects for different test types (source: repository listing). `Encina.UnitTests` contains isolated unit tests with subfolders like `ADO/`, `Caching/`, and `Core/` (source: CLAUDE.md, Test Organization). `Encina.IntegrationTests` contains tests against real databases using Docker/Testcontainers (source: CLAUDE.md, Test Organization). `Encina.PropertyTests` contains property-based tests using FsCheck (source: CLAUDE.md, Test Organization). `Encina.ContractTests` verify public API contracts (source: CLAUDE.md, Test Organization). `Encina.GuardTests` verify null checks and parameter validation (source: CLAUDE.md, Test Organization). `Encina.LoadTests` and `Encina.NBomber` are for load and concurrency testing (source: repository listing). `Encina.BenchmarkTests` contains BenchmarkDotNet benchmarks with subfolders like `Encina.Benchmarks/` and `Encina.AspNetCore.Benchmarks/` (source: CLAUDE.md, Test Organization). `Encina.TestInfrastructure` holds shared test infrastructure, and `Encina.Testing.Examples` provides reference examples (source: repository listing).

### 2.4 `docs/`

The `docs/` directory holds architecture ADRs in `architecture/`, specifications in `specifications/`, engineering plans in `engineering/` and `plans/`, user guides in `guides/`, feature documentation in `features/`, coverage data in `coverage/`, mutation data in `mutations/`, release notes in `releases/`, and testing methodology in `testing/` (source: repository listing). The `docs/` folder also contains `benchmarks/`, `load-tests/`, `messaging/`, `security/`, `sharding/`, `spikes/`, and `reports/` (source: repository listing).

### 2.5 `.github/`

The `.github/` directory contains `workflows/`, `scripts/`, `coverage-manifest/`, and `ISSUE_TEMPLATE/` (source: repository listing). Workflows include `ci-full.yml`, `ci.yml`, `benchmarks.yml`, `load-tests.yml`, `mutation-tests.yml`, `publish-coverage.yml`, `publish-mutations.yml`, and `release-on-milestone.yml` (source: repository listing). The `scripts/` folder contains C# file-based tools like `coverage-report.cs`, `run-integration-tests.cs`, `run-stryker.cs`, `mutation-history.cs`, `mut-docs-render.cs`, and `perf-docs-render.cs` (source: repository listing). `coverage-report.cs` calculates per-flag coverage obligations (source: CLAUDE.md, Per-Flag Coverage System). `run-integration-tests.cs` executes integration tests (source: CLAUDE.md, Docker Integration Testing). `run-stryker.cs` runs mutation testing (source: CLAUDE.md, Mutation Testing System). `mutation-history.cs` merges mutation results (source: CLAUDE.md, Mutation Testing System). `mut-docs-render.cs` expands mutation reference markers in docs (source: CLAUDE.md, Mutation Testing System). `perf-docs-render.cs` expands performance reference markers (source: CLAUDE.md, Mutation Testing System).

### 2.6 Where outputs go

All test outputs must go to the `artifacts/` directory, never to the repository root (source: CLAUDE.md, Test Output Conventions). Test results go to `artifacts/test-results/`, code coverage to `artifacts/coverage/`, benchmark results to `artifacts/performance/`, load test metrics to `artifacts/load-metrics/`, and mutation reports to `artifacts/mutation/` (source: CLAUDE.md, Test Output Conventions). Root-level outputs like `TestResults/`, `test-results.log`, `coverage-*`, and `BenchmarkDotNet.Artifacts/` are forbidden (source: CLAUDE.md, Test Output Conventions). Scripts should write to `artifacts/` subdirectories (source: CLAUDE.md, Test Output Conventions).

## 3. How the pieces relate

### 3.1 You touch a provider-dependent feature (a store, repository, unit of work, SQL)

When your change implements a store, repository, unit of work, or any feature that interacts with database-specific SQL, the Multi-Provider Implementation Rule applies. This rule is mandatory for all 10 database providers. These providers consist of three implementations for ADO.NET (SqlServer, PostgreSQL, MySQL), three for Dapper (SqlServer, PostgreSQL, MySQL), three for EF Core (SqlServer, PostgreSQL, MySQL), and one for MongoDB. Oracle and SQLite are excluded from this count because they were removed from the pre-1.0 scope or lack production features, respectively (source: CLAUDE.md, Multi-Provider Implementation Rule (MANDATORY)).

Beyond database providers, Encina uses specialized provider categories for specific feature areas. You must determine which category applies to your feature using the Provider Applicability Matrix.

| Category | Count | Examples |
| :--- | :--- | :--- |
| Caching | 8 | Memory, Hybrid, Redis, Valkey, Dragonfly, Garnet, KeyDB, Memcached |
| Messaging Transports | 10+ | RabbitMQ, Kafka, NATS, MQTT, AzureServiceBus, AmazonSQS, InMemory, gRPC, GraphQL |
| Distributed Locks | 5 (for 1.0) | InMemory, Redis, SqlServer, PostgreSQL, MySQL |
| Validation | 3 | FluentValidation, DataAnnotations, MiniValidator |
| Scheduling | 2+ | Encina.Messaging, Hangfire, Quartz |
| Event Sourcing | 1 | Marten |
| Cloud/Serverless | 2 (for 1.0) | AzureFunctions, AwsLambda |
| Resilience | 3 | Polly, Extensions.Resilience, Extensions.Http.Resilience |
| Observability | 1+ | OpenTelemetry |
| Testing | 12 | Testing, Fakes, Respawn, WireMock, Shouldly, Verify, Bogus, FsCheck, Architecture, Testcontainers, TUnit, Pact |

A feature is considered "complete" in each category when it is implemented consistently across all providers in that specific category. For database features, this means all 10 providers. For caching features, all 8 providers. For cloud features in 1.0, this means AWS Lambda and Azure Functions are covered, with the GCP gap noted in the issue. For lock features in 1.0, this means the five existing providers are covered. You must check the Provider Applicability Matrix to see if a feature requires Database, Caching, Transport, Lock, or Validation providers. For example, a new Outbox Store requires the 10 Database providers but not Caching or Transport. A new Query Caching feature requires the 8 Caching providers but not Database (source: CLAUDE.md, Specialized Provider Categories (Beyond the 10 Database Providers); Provider Applicability Matrix).

### 3.2 You create a new entity, store, pipeline behavior, background service or external integration

The Cross-Cutting Integration Rule is mandatory for every new feature. You must evaluate your new feature against all 12 transversal functions. For each function, you must choose one of three outcomes: Integrate, Defer, or Not Applicable.

| # | Function | Key Question |
| :--- | :--- | :--- |
| 1 | Caching | Does this feature read data that benefits from caching? |
| 2 | OpenTelemetry | Does this feature perform operations worth tracing/metering? |
| 3 | Structured Logging | Does this feature need operational visibility? |
| 4 | Health Checks | Does this feature have a health-checkable dependency? |
| 5 | Validation | Does this feature receive input that needs validation? |
| 6 | Resilience | Does this feature call external systems that can fail? |
| 7 | Distributed Locks | Does this feature have concurrent access to shared state? |
| 8 | Transactions | Does this feature need atomic multi-operation guarantees? |
| 9 | Idempotency | Can this feature receive duplicate requests? |
| 10 | Multi-Tenancy | Does this feature store/query data that belongs to a tenant? |
| 11 | Module Isolation | In modular monolith, does this feature need module scoping? |
| 12 | Audit Trail | Does this feature perform operations with compliance/security implications? |

The three allowed outcomes are:
1. **Integrate**: Implement the integration in the current feature.
2. **Defer**: Create a GitHub Issue for future integration and reference it in the plan or PR.
3. **Not Applicable**: Document why in the plan or PR description in one sentence.

You cannot skip this evaluation. Missing integrations create invisible gaps that compound over time. Common misses include OpenTelemetry and TenantId for new stores, and Resilience and Health Checks for new external integrations (source: CLAUDE.md, Cross-Cutting Integration Rule (MANDATORY)).

### 3.3 You add logging

Every `[LoggerMessage]` EventId must be registered in the central registry before use. The registry is located at `src/Encina/Diagnostics/EventIdRanges.cs`. You must check this file for the next free range in the appropriate area. Each area has a defined range, such as Messaging (2000-2499) or Compliance (8100-8949). You register a new `public static readonly (int Min, int Max)` field with an appropriate size, typically 50 or 100 slots. Then you create your `Diagnostics/*LogMessages.cs` file with EventIds within that registered range. You must update `PublicAPI.Unshipped.txt` for the new public field. Finally, you run architecture tests to verify no collisions or range violations (source: CLAUDE.md, Structured Logging & EventId Allocation (MANDATORY)).

The architecture test `EventIdUniquenessRule` in `Encina.Testing.Architecture` enforces this. It provides methods to assert that EventIds are globally unique, within registered ranges, and that no two registered ranges overlap. You must never use an EventId without registering its range first, and you must never assign EventIds outside your package's registered range. Avoid sparse allocations; pack EventIds sequentially to avoid overflowing the range (source: CLAUDE.md, Structured Logging & EventId Allocation (MANDATORY)).

### 3.4 You add or change public API

When you add or change public API, the `Microsoft.CodeAnalysis.PublicApiAnalyzers` package tracks these changes via `PublicAPI.Shipped.txt` and `PublicAPI.Unshipped.txt`. If you add a new public symbol, you will get an RS0016 error because the symbol is not in the declared API. You must add the entry to `PublicAPI.Unshipped.txt`. The format is `Namespace.Type.Member(params) -> ReturnType`. If you remove a public symbol that was in the declared API but is no longer found, you will get an RS0017 error. You must remove the entry from the txt file. Nullable annotations use `string!` for non-null and `string?` for nullable. You must also provide XML documentation on all public APIs, including code examples when helpful (source: CLAUDE.md, PublicAPI Analyzers (RS0016/RS0017); Documentation).

### 3.5 You add tests

The coverage system uses a per-flag obligations model. Each test type (unit, guard, contract, property, integration) is a separate flag. Coverage is measured independently per flag. A line covered by unit tests does not count toward the guard or contract target. Each flag must independently reach its own target percentage defined in the manifest `.github/coverage-manifest/{Package}.json`. A critical rule is that tests must execute real package code. Reflection-based tests that only load metadata cover zero lines. Contract and property tests must instantiate real implementations from the package to generate coverage (source: CLAUDE.md, Per-Flag Coverage System (Obligations Model) — CRITICAL).

Required test types depend on the feature category. For database features, UnitTests, GuardTests, PropertyTests, ContractTests, and IntegrationTests are required. For non-DB features, IntegrationTests are only required if justified, otherwise you must create a justification document. Justification documents are `.md` files located in `{TestProject}/{Provider/Feature}/{Feature}.md`. You never create justification files for IntegrationTests on database features. Integration tests for database features must use Docker/Testcontainers and use shared xUnit `[Collection]` fixtures to keep Docker containers low. You never create per-class fixtures for database tests. Test results go to `artifacts/test-results/` and coverage to `artifacts/coverage/` (source: CLAUDE.md, Test Type Guidelines by Feature Category; Collection Fixtures (Container Reduction Strategy); Test Output Conventions).

### 3.6 You add a benchmark or a load test

Load tests are only meaningful for features with concurrent behavior, such as Unit of Work, Multi-Tenancy, or Read/Write Separation. Benchmarks are only meaningful for hot paths where microseconds matter, such as Read/Write Separation. If you skip a benchmark or load test, you must create a justification document.

For benchmarks, you must follow BenchmarkDotNet rules. You must use `BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config)`, not `BenchmarkRunner.Run<T>()`. This allows filtering benchmarks at runtime. You must materialize `IQueryable<T>` results using `.ToList()` to avoid validation errors. You should verify filtering before full execution using `--list flat` and `--filter`. Benchmark results go to `artifacts/performance/` (source: CLAUDE.md, LoadTests Guidelines; BenchmarkTests Guidelines; BenchmarkDotNet Guidelines).

### 3.7 Naming and messaging conventions that every change must respect

Entity names follow specific conventions. Outbox entities are `OutboxMessage`, Inbox entities are `InboxMessage`, Saga states are `SagaState`, and scheduled messages are `ScheduledMessage`. Property names must use `ErrorMessage` instead of `Error` to avoid CA1716 keyword conflicts. Timestamps always use the `AtUtc` suffix, such as `CreatedAtUtc` or `ProcessedAtUtc`. Store implementations follow the pattern `{Pattern}Store{Provider}`, such as `OutboxStoreEF` or `InboxStoreDapper`. You never just name a class `Store` or `Repository`. These conventions ensure consistency across the codebase and avoid common analyzer errors (source: CLAUDE.md, Naming Conventions).

## 4. From an idea to `main`

The path every change follows, and what each step must produce.

1. **Start from an issue.** Every piece of work has a GitHub issue with a typed prefix and the matching template: `[BUG]`, `[FEATURE]`, `[DEBT]`, `[TEST]`, `[SPIKE]`, `[INFRA]`, `[REFACTOR]`, `[EPIC]` (`CLAUDE.md`, "Issue Tracking"). If you find a problem while working on something else, open an issue for it and keep going; never leave a finding unrecorded. Issues carry a backlog priority label (`p0-mandatory`, `p1-recommended`, `p2-post-1.0`, `p3-obsolete`) and a milestone: the six `v0.14.0` to `v0.19.0` blocks are the 1.0 contract, the `Post-1.0: …` milestones are not (SPEC-000 DEC-002, DEC-005).
2. **Specify before you build a feature.** A feature gets a `SPEC-NNN` under `docs/specifications/` with requirements, acceptance criteria and invariants before implementation; a design choice with a real trade-off gets an ADR under `docs/architecture/adr/` (`docs/engineering/AI-DEVELOPMENT-MODEL.md` §5 to §7). Bug fixes and mechanical debt do not need a specification.
3. **Branch per task.** `type/short-description` (`fix/…`, `feat/…`, `docs/…`, `ci/…`, `chore/…`, `test/…`); one branch per issue; never commit to `main`. If you run several tasks at once, use `git worktree` so that no two tasks share a checkout.
4. **Evaluate the twelve cross-cutting functions** for any new entity, store, behavior, background service or integration, and record the outcome per function in the pull request description: integrated, deferred with an issue, or not applicable with a reason (`CLAUDE.md`, "Cross-Cutting Integration Rule").
5. **Verify locally before you push.** Build in Release, run the affected test projects, run `dotnet format --verify-no-changes`. CI is confirmation, not discovery; a push that fails CI costs everybody a cycle.
6. **Open the pull request with the contract.** Title in Conventional Commits form (`type(scope): summary`; the `semantic` check rejects other types), body with `Fixes #N` or `Refs #N`, what was verified and how, and the cross-cutting outcomes. Commits and pull requests carry no AI attribution lines. A user-visible change includes a changelog fragment under `changelog.d/<issue>-<slug>.<section>.md` (see `changelog.d/README.md`) in this same PR, instead of a hand edit to `CHANGELOG.md`'s `[Unreleased]` section: every PR editing that section in place used to conflict with every other PR doing the same, and CI now rejects a PR whose diff touches `[Unreleased]` directly.
7. **Ask for the bot review once.** Comment `@coderabbitai review` when the PR is ready; the free plan allows one review per hour and every push cancels the one in progress, so batch your corrections into one push and re-request once. Answer every thread with the commit that fixes it or the evidence that refutes it. Codecov reports patch coverage (target 60%), SonarCloud and CodeQL run on their own.
8. **What must be green.** Branch protection on `main` requires `ci-result` (the summary of `build` and every test shard of `ci.yml`) and CodeQL `Analyze`; conversations must be resolved; history is linear; squash merge is the only merge mode. There is no required approval: the maintainer is one person and cannot approve their own pull requests, so review comes from the bots and from the maintainer reading the PR.
9. **Merge on green without waiting.** Arm auto-merge (`gh pr merge <n> --auto --squash`) when the PR is ready; it merges by itself when the required checks pass. Start the next task instead of watching the checks.
10. **After merge.** The issue closes through `Fixes #N`; the tag and the release are cut by the maintainer per milestone block (SPEC-000 DEC-005), folding the accumulated fragments with `changelog-fragments.cs --release`.

What CI runs where (after #1110): on a pull request, the fast tier (`build`, unit/guard/contract/property/integration shards, CodeQL in source mode, internal link check); on push to `main`, the same plus benchmarks and the external link scan; weekly, the full coverage suite (`ci-full.yml`) and mutation testing. A docs-only PR skips build and tests and is green in minutes.

## 5. Rules that will bite you

### Scripting and tooling
- Only use PowerShell or C# scripts for any automation or scripting task. Python and direct bash or shell scripting are strictly prohibited in all development contexts. (source: CLAUDE.md, Scripting & Tooling Policy (MANDATORY))
- Do not use bash constructs such as loops, pipes, subshells, or here-docs for file creation or logic. Use PowerShell Set-Content or C# File.WriteAllText for writing files and Get-ChildItem for searching instead of unix commands. (source: CLAUDE.md, Prohibited)
- The project mandates PowerShell and C# 14 to ensure consistency, Windows-native support, type safety, and alignment with the .NET ecosystem. This policy exists to prevent the maintenance burden of mixing multiple scripting languages. (source: CLAUDE.md, Why This Policy Exists)

### Build and code quality
- Address all CA warnings by fixing them or providing a justified suppression, as the build requires zero warnings. This rule ensures that code quality standards are strictly enforced during every build. (source: CLAUDE.md, Code Analysis)
- Never mark code as Obsolete for backward compatibility because the project is pre-1.0 and does not maintain legacy features. (source: CLAUDE.md, Code Quality Standards)
- Do not implement migration helpers or compatibility layers since breaking changes are acceptable and encouraged if they improve the design. This approach keeps the codebase clean of legacy code and deprecated features. (source: CLAUDE.md, Code Quality Standards)
- Enable nullable reference types everywhere in the codebase to ensure type safety and prevent null reference errors. (source: CLAUDE.md, Technology Stack)
- Use only .NET 10 as the mandatory target framework because it is the exclusive stable release supported by the project. (source: CLAUDE.md, Technology Stack)
- Use the latest C# 14 features without hesitation, including extension members and the field keyword, to leverage modern language capabilities. (source: CLAUDE.md, .NET 10 / C# 14 Reference (Released November 2025))

### Language and attribution
- Write all code, comments, and documentation in English only, as this is the mandatory language for the repository. (source: CLAUDE.md, Spanish/English)
- Ensure all commit messages are written in clear, descriptive English to maintain consistency across the project history. (source: CLAUDE.md, Git Workflow)
- Do not include any AI signatures, co-author tags, or references to Claude in commit messages because all commits must appear as authored solely by the repository owner. (source: CLAUDE.md, Git Workflow)
- Translate any Spanish comments to English when encountered during editing to maintain the English-only standard for code. (source: CLAUDE.md, Spanish/English)

### Tests
- Follow the Test Quality Standards by writing tests with clear names, AAA pattern, single responsibility, independence, and determinism. This ensures that tests remain maintainable and reliable over time. (source: CLAUDE.md, Test Quality Standards)
- Use shared xUnit Collection fixtures for database integration tests to keep Docker container usage low and avoid per-class fixtures. This rule prevents excessive resource consumption during test execution. (source: CLAUDE.md, Collection Fixtures (Container Reduction Strategy))
- Direct all test outputs to the artifacts directory and never write to the repository root to keep the working tree clean. (source: CLAUDE.md, Test Output Conventions)
- Create justification documents for legitimately skipped test types like Benchmarks or LoadTests, but never use them to skip IntegrationTests for database features. This practice ensures that missing coverage is intentional and documented. (source: CLAUDE.md, Test Justification Documents (.md))
- Avoid reflection-only tests that do not instantiate real implementations because they generate zero coverage for the target package. Contract and property tests must execute real package code to count toward coverage obligations. (source: CLAUDE.md, Per-Flag Coverage System (Obligations Model) — CRITICAL)

### Issues and process
- Create a GitHub Issue with the correct prefix for every problem identified, as no issue should be left untracked. This ensures that technical debt and bugs are recorded for future resolution. (source: CLAUDE.md, When to Create Issues)
- Normalize issue prefixes by using specific tags like DEBT instead of TECH-DEBT and SPIKE instead of ARCHITECTURE. This standardization helps maintain a consistent and searchable issue tracker. (source: CLAUDE.md, Prefix normalization rules)
- Create issues immediately for bugs found during development and use the DEBT prefix for technical debt that might derail current work. This separates immediate fixes from deferred improvements. (source: CLAUDE.md, When to Create Issues)
- Utilize CodeRabbit for manual review and plan generation by referencing issues in pull requests to enable linked issue validation. This integration helps ensure that pull requests meet the specific requirements of the linked issues. (source: CLAUDE.md, CodeRabbit Features)

### The "Common Errors to Avoid" list, condensed
- Avoid adding Obsolete attributes, migration helpers, or using .NET 9 or older versions to maintain a modern, clean codebase. (source: CLAUDE.md, Common Errors to Avoid)
- Do not name properties Error without the Message suffix to avoid CA1716 keyword conflicts and ensure clear error handling. (source: CLAUDE.md, Common Errors to Avoid)
- Keep all patterns opt-in rather than mandatory to adhere to the Pay-for-What-You-Use principle. (source: CLAUDE.md, Common Errors to Avoid)
- Implement features for all applicable providers in their respective categories to ensure coherence and avoid invisible gaps. (source: CLAUDE.md, Common Errors to Avoid)
- Do not skip test types without justification or leave any coverage flag below its manifest target. (source: CLAUDE.md, Common Errors to Avoid)
- Use BenchmarkSwitcher instead of BenchmarkRunner and materialize IQueryable results in benchmarks to avoid validation errors. (source: CLAUDE.md, Common Errors to Avoid)
- Register LoggerMessage EventId ranges in EventIdRanges.cs before use and pack them sequentially to prevent range overflow. (source: CLAUDE.md, Common Errors to Avoid)

## 6. Where to ask

- **Bugs, features, debt, tests, investigations:** a GitHub issue with the right template ([`.github/ISSUE_TEMPLATE/`](../../.github/ISSUE_TEMPLATE)).
- **Security vulnerabilities:** never in a public issue; follow [`SECURITY.md`](../../SECURITY.md).
- **"Is this the right design?"** Open a `[SPIKE]` issue with a time box; the human decision gate is the maintainer (`docs/engineering/AI-DEVELOPMENT-MODEL.md` §15).
- **Why is something the way it is?** Check [`docs/engineering/PROJECT-HISTORY.md`](../engineering/PROJECT-HISTORY.md) (decisions and rejected alternatives with their issue citations) and the ADRs before asking; most "why" questions already have a cited answer.
- **Conduct:** [`CODE_OF_CONDUCT.md`](../../CODE_OF_CONDUCT.md).
