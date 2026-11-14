# Implementation Plan: Examples `03-entity-framework`, `04-dapper-integration`, `05-redis-caching` — database and cache integration

> **Issue**: [#89](https://github.com/dlrivada/Encina/issues/89) (family: #87, #88, #188, #344)
> **Type**: Feature (documentation and examples; no change under `src/`)
> **Research**: `src/Encina.EntityFrameworkCore/ServiceCollectionExtensions.cs`, `src/Encina.Dapper.SqlServer/ServiceCollectionExtensions.cs`, `src/Encina.Caching/ServiceCollectionExtensions.cs`, `src/Encina.Caching.Redis/ServiceCollectionExtensions.cs`, `src/Encina.Caching/Attributes/`, `docker-compose.yml`, `docs/infrastructure/docker-infrastructure.md`
> **ADR**: none (ADR number assigned when the ADR pull request opens, if one is needed)
> **Related**: #87 (owns the `examples/` scaffold and Dilemma D1), #88 (CQRS example), #188 (dashboard guide), #344 (adoption guide)
> **Complexity**: Medium (three small applications, each with a Docker Compose file); too large for one pull request, so the plan proposes a split
> **Estimated Scope**: about 30 files, about 1,300 lines in three pull requests of 10 to 12 files and about 430 lines each

---

## Summary

This plan adds three runnable examples that show Encina with a real dependency: Entity Framework Core with the outbox and a transaction on SQL Server, Dapper on SQL Server, and query caching on Redis. Each example has its own `docker-compose.yml` with only the service it needs, so it can be copied out of the repository. The folder `examples/`, its `Directory.Build.props` and the `Encina.slnx` entry belong to the #87 plan; Dilemma D1 (where the examples live) is written once there and summarised in Design Choice 1.

**What each example shows, with the real API checked in `src/`:**

| Example | Shows | Registration and API |
|---------|-------|----------------------|
| `03-entity-framework` | EF Core, outbox, transaction, `DbContext` configuration | `AddEncinaEntityFrameworkCore<TDbContext>(Action<MessagingConfiguration>)` (`src/Encina.EntityFrameworkCore/ServiceCollectionExtensions.cs:136`) with `config.UseOutbox = true` and `config.UseTransactions = true` (`MessagingConfiguration.UseOutbox`, `UseTransactions`); the `DbContext` applies `OutboxMessageConfiguration` in `OnModelCreating` as the package README shows. The outbox is fed by `TransactionalOutboxPipelineBehavior<,>` (`src/Encina.Messaging/Outbox/TransactionalOutboxPipelineBehavior.cs:30`), which writes the notifications of a request that implements `IHasNotifications`; `IEncina.Publish` dispatches in process and never touches the outbox, and the transaction behavior only commits, so the handler calls `SaveChangesAsync` itself |
| `04-dapper-integration` | Dapper on SQL Server, direct SQL beside Encina messaging, connection management | `AddEncinaDapper(...)` in `Encina.Dapper.SqlServer` (three overloads; one takes a connection factory, the README shows `new SqlConnection(connectionString)`) with the outbox switch per Design Choice 7 (Dilemma) |
| `05-redis-caching` | Redis cache setup, query caching, invalidation, TTL | `AddEncinaCaching(...)` **and** `AddEncinaRedisCache(connectionString)` (`src/Encina.Caching.Redis/ServiceCollectionExtensions.cs`), `[Cache(DurationSeconds = ...)]` and `[InvalidatesCache]` (`src/Encina.Caching/Attributes/`) |

**A correction to the issue's table.** The issue lists only `AddEncinaRedisCache(...)` for example 05. `AddEncinaRedisCache` registers the Redis `ICacheProvider` and its options (`AddEncinaRedisCacheCore`, `ServiceCollectionExtensions.cs:124-154`) but not the pipeline behaviors; `AddEncinaCaching` registers `QueryCachingPipelineBehavior<,>` and `CacheInvalidationPipelineBehavior<,>` (`src/Encina.Caching/ServiceCollectionExtensions.cs:80-85`; the method is at :44). `[Cache]` has no effect without it, so the example calls both. The worker confirms the call order and options when writing.

**Provider coverage is stated, not implied.** The examples demonstrate Dapper-SqlServer, one EF Core provider (Design Choice 2) and Redis. They are not provider features, so AGENTS.md section 5 does not require the other 8 database providers or the other 7 caches; each README names the sibling packages (ADO.NET, the other Dapper and EF Core providers, MongoDB; Valkey, Dragonfly, Garnet, KeyDB, Memory, Hybrid) and says they are not demonstrated.

**Size and split.** Three examples of about 9 files each exceed one worker pull request only when taken together (about 27 files). AGENTS.md section 11 gives one pull request per issue (15 files, 1500 lines), so before any worker brief the orchestrator splits #89 into three sub-issues, one per example (Next Steps). The three can run in parallel after the #87 scaffold merges, but they are not file-independent: each adds an `Encina.slnx` entry and a row in `examples/README.md`, so parallel pull requests conflict on both. Either the #87 scaffold pull request pre-creates the three rows and the three solution entries (recommended: the later pull requests then only add their folder), or the three land in order 03, 04, 05 each rebased on the previous; both files are counted in every pull request below.

**Provider category**: Database (one provider per example, as a demonstration) and Caching (Redis); no new provider-dependent library code.

**Affected packages**: `Encina.EntityFrameworkCore`, `Encina.Dapper.SqlServer`, `Encina.Caching`, `Encina.Caching.Redis`, `Encina.Messaging` (all consumed, not changed); the three new example projects.

**Size**: 1 plan file; the implementation is three pull requests of 10 to 12 files and about 430 lines each (see Estimated File Count).

---

## Design Choices

<details>
<summary><strong>1. Where the examples live (D1, shared; full text in the #87 plan)</strong></summary>

**Class:** Dilemma

The question and the options are written once in the #87 plan, Design Choice 1. For these three examples the trade-off has one more factor: they need Docker, so CI can build them but not run them.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **(a) `examples/` with `ProjectReference`, in `Encina.slnx` and CI** | Compiles against current code; an API change that breaks an example breaks its pull request | CI only builds these three (no Docker run); widens the solution build |
| **(b) `examples/` outside the solution, on a local package feed** | Closest to user code | CI never notices an API change |
| **(c) Snippets under `docs/examples/`** | No build cost | Nothing proves the code compiles or runs against a database |

### Chosen Option: **(a) — `examples/` with `ProjectReference`, inside `Encina.slnx` and CI** (recommended, pending the maintainer)

### Rationale

Same as #87: only (a) makes a broken example fail a pull request. Running against the database stays a manual, documented step (Phase 5), because the repository's integration jobs own Docker-dependent tests.

</details>

<details>
<summary><strong>2. Which EF Core provider example 03 demonstrates</strong></summary>

**Class:** Dilemma

The issue's provider table marks EFCore-SqlServer and leaves the choice to this plan. The EF Core package has SqlServer, PostgreSQL and MySQL providers (AGENTS.md section 5); the example can show one.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **(a) SQL Server** | Example 04 also uses SQL Server, so a reader compares EF Core and Dapper against the same database and one image; `Microsoft.EntityFrameworkCore.SqlServer` is already in `Directory.Packages.props` | The SQL Server image is large and slower to start; the licence prompt (`ACCEPT_EULA`) appears in the compose file |
| **(b) PostgreSQL** | Small, fast image; no licence variable; the root `docker-compose.yml` already puts it in the `core` profile | A different database from example 04, so the comparison is weaker; the EF Core PostgreSQL package needs a `PackageVersion` entry check |

### Chosen Option: **(a) — SQL Server** (recommended, pending the maintainer)

### Rationale

We recommend (a) because the three examples then use two services, not three, and 03 and 04 can be read side by side. (b) wins if the maintainer wants the lightest first experience for readers on laptops without Docker memory to spare. The choice changes one package reference and the compose file, not the shape of the example.

</details>

<details>
<summary><strong>3. Three focused examples, not one</strong></summary>

**Class:** Settled
**Settled by:** AGENTS.md section 11 (a worker's issue is one pull request of at most 15 files and 1500 lines), which excludes B (about 30 files in one pull request); option C (documentation only) is excluded by the issue, which asks for runnable examples; #89 (original reasoning: one mega-example is too complex to learn a feature from)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — Three examples, three pull requests** | Each is learnable alone and fits the size limit | Three folders, three compose files |
| **B — One combined example** | One project | About 27 files in one pull request; the size gate fails it; a reader cannot isolate one feature |
| **C — Documentation only** | No code | No runnable proof |

### Chosen Option: **A — Three examples**

### Rationale

Only A satisfies the size rule and the issue's own reasoning. An external samples repository (the original alternative) was rejected by the issue because it drifts from the library.

</details>

<details>
<summary><strong>4. Redis as the cache, and the registration pair</strong></summary>

**Class:** Settled
**Settled by:** #89 (the issue asks for Redis, which excludes B and C) and AGENTS.md section 5 (Caching category: Redis is one of the 8 providers behind `ICacheProvider`)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — Redis through `AddEncinaRedisCache` plus `AddEncinaCaching` for the behaviors** | What the issue asks for; both calls verified in `src/` | Two calls to explain |
| **B — Memory cache (`Encina.Caching.Memory`)** | No Docker | Not the Redis integration the issue names |
| **C — Valkey, Dragonfly, Garnet or KeyDB** | Redis-compatible, have their own packages | Not what the issue asks for; the README points to them instead |

### Chosen Option: **A — Redis with both registrations**

### Rationale

The README explains why two calls are needed (provider versus behaviors) and links the other caching packages as not demonstrated.

</details>

<details>
<summary><strong>5. One Docker Compose file per example, with the repository's image tags</strong></summary>

**Class:** Trivial
**Settled by:** #89 (the issue asks for a compose file per example, which excludes B) and `docs/infrastructure/docker-infrastructure.md`, whose service table (lines 49 and 69) is the one place that names the repository's SQL Server and Redis images (`mcr.microsoft.com/mssql/server:2022-latest`, `redis:7-alpine`)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — A small `docker-compose.yml` inside each example, with the image of the Docker guide's table** | The example can be copied out; one service each | The tag is repeated; it is a moving tag (`2022-latest`) that already appears in `ci.yml`, `ci-full.yml`, `load-tests.yml` and two templates, so no single source exists in the repository today |
| **B — Use the root file with `--profile`** | One source of tags | The example no longer stands alone |

### Chosen Option: **A — A compose file per example**

### Rationale

The issue says so, and the repository root keeps its profiles for development of Encina itself. The compose files take the images from the Docker guide's table, and each README links that guide as the place where the maintained value lives, instead of claiming "the same as the root file". If the maintainer later pins the tags repository-wide (LTS default images rule), the three compose files follow that one source.

</details>

<details>
<summary><strong>6. Performance comparison notes carry no figures</strong></summary>

**Class:** Settled
**Settled by:** `.claude/skills/encina-docs/SKILL.md` house rule 2 (no hand-typed figures; a performance number is a citation), SPEC-001 (evidence-cited figures)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — Qualitative notes in the Dapper README, with a link to the performance dashboard if it covers the case** | Complies; cannot go stale | Less concrete |
| **B — Typed numbers from a local run** | Concrete | Violates the rule; a laptop run is not evidence |

### Chosen Option: **A — Qualitative notes**

### Rationale

Only A complies. If the performance dashboard does not cover EF Core versus Dapper, the README says so and links no figure.

</details>

<details>
<summary><strong>7. Whether example 04 enables the outbox (D3)</strong></summary>

**Class:** Dilemma

`UseOutbox = true` registers the outbox store and processor, but rows are written only for a request that implements `IHasNotifications` (`TransactionalOutboxPipelineBehavior.cs:30`). Example 04 as originally written has no such request, so the switch would be on and do nothing.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **(a) Drop `UseOutbox` from 04; the README says the outbox is shown in example 03 only** | Example 04 stays about Dapper and direct SQL; no unused switch (pay-for-what-you-use) | The Dapper outbox store is not demonstrated |
| **(b) Give 04 one `IHasNotifications` command and a notification handler, with the `OutboxProcessor` note** | Shows the Dapper outbox store | Duplicates the pattern of 03; adds files to a 10-file pull request |

### Chosen Option: **(a) — Drop `UseOutbox` from 04** (recommended, pending the maintainer)

### Rationale

We recommend (a) because the subject of 04 is the Dapper data path, and a switch that writes nothing misleads a reader who copies it. (b) wins if the maintainer wants each data-access example to show its own outbox store.

</details>

---

## Implementation Phases

Phases 2, 3 and 4 are independent and become three pull requests after the split. Phase 1 is the common part each of them checks first.

### Phase 1: Common conventions (checked by every example)

> **Goal**: Each example is self-contained and follows the repository rules. Depends on the #87 scaffold.

<details>
<summary>Tasks</summary>

1. Each project sits under `examples/<name>/` and inherits `examples/Directory.Build.props` (`IsPackable` false, zero warnings from the root file).
2. Each connection string comes from configuration (`ConnectionStrings:Default`), set in `appsettings.json` for the compose service; the README states the compose credentials are for local development only. No connection string or password is logged.
3. Database calls are asynchronous with a `CancellationToken` (AGENTS.md section 3).
4. Time comes from `TimeProvider` where a timestamp is needed (AGENTS.md section 3).
4a. Error codes use the explicit category overload `EncinaErrors.Create(code, ErrorCategory.X, message)` (ADR-062): `orders.rejected` (03) is `Validation`, `orders.not_found` (04) is `NotFound`, `products.not_found` (05) is `NotFound`.
5. Each `docker-compose.yml` starts only its service, with a health check, and the README says how to stop and remove it (`docker compose down -v`).

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: examples/ exists (#87 plan). Each of the three examples is a small console or Web project with its own docker-compose.yml.
TASK: Apply the common conventions of Phase 1 to the example you are building: Directory.Build.props inheritance, connection string from configuration, async calls with CancellationToken, TimeProvider, compose file with only the needed service and a health check.
KEY RULES: never log a connection string or EncinaError.Message; no synchronous database calls (IDbConnection.Open); English only; zero warnings.
REFERENCE FILES: docker-compose.yml, docs/infrastructure/docker-infrastructure.md, AGENTS.md section 3, examples/Directory.Build.props.
```

</details>

### Phase 2: Example `03-entity-framework` (pull request A)

> **Goal**: A command writes an order and an outbox message in one transaction; the processor publishes it.

<details>
<summary>Tasks</summary>

1. `examples/03-entity-framework/03-entity-framework.csproj`: references `Encina`, `Encina.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore.SqlServer` (version from `Directory.Packages.props`).
2. `Data/AppDbContext.cs`: `DbSet<Order>` and `DbSet<OutboxMessage>`; `OnModelCreating` applies `OutboxMessageConfiguration`. `Orders/Order.cs`: the entity.
3. `Orders/CreateOrder.cs`: `CreateOrder : ICommand<Guid>, IHasNotifications` (the interface the outbox behavior reads); `GetNotifications()` returns an `OrderCreated` notification (`Orders/OrderCreated.cs`). `Orders/CreateOrderHandler.cs`: adds the order to the `DbContext` and calls `await dbContext.SaveChangesAsync(cancellationToken)` itself, because the transaction behavior only commits (`lease.CommitAsync`, `TransactionPipelineBehavior.cs:184`) and nothing else saves. With `UseTransactions` the pipeline commits on `Right` and rolls back on `Left`; `TransactionalOutboxPipelineBehavior<,>` writes the notification to the outbox in the same transaction (package README, "joins the request's transaction").
4. `Orders/FailingCreateOrder.cs` (command and handler in one file): the handler stages an order change, calls `SaveChangesAsync` and then returns a `Left` (`ErrorCategory.Validation`, code `orders.rejected`); the pipeline rolls back, so the row and the outbox message are absent, which the program proves by counting rows.
5. `Orders/OrderCreatedHandler.cs`: a notification handler; the `OutboxProcessor` hosted service (registered when `UseOutbox` is set) reads the stored message and delivers it to this handler, so the console shows the delivery after a short poll. The README states that delivery is asynchronous and at-least-once.
6. `Program.cs`: `AddEncinaEntityFrameworkCore<AppDbContext>(config => { config.UseTransactions = true; config.UseOutbox = true; })`, `EnsureCreatedAsync` for the demo schema, one successful command, one failing command, and the row counts.
7. `docker-compose.yml` (SQL Server, per Design Choice 2), `appsettings.json` and `README.md` (by `docs-writer`). Error categories (ADR-062): `orders.rejected` is `Validation`; the example uses `EncinaErrors.Create(code, ErrorCategory.X, message)`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: Encina.EntityFrameworkCore registers stores and behaviors with AddEncinaEntityFrameworkCore<TDbContext>(Action<MessagingConfiguration>); MessagingConfiguration has UseOutbox and UseTransactions. Read src/Encina.EntityFrameworkCore/README.md for the DbContext configuration.
TASK: Build examples/03-entity-framework as in the plan's Phase 2 tasks 1-7: a command that implements IHasNotifications and whose handler calls SaveChangesAsync, commit and rollback shown through the Either result, the outbox message delivered by the OutboxProcessor to a notification handler.
KEY RULES: verify every type in src/ before use; the outbox is fed only by requests implementing IHasNotifications (IEncina.Publish does not write it) and the transaction behavior never calls SaveChangesAsync; the outbox is opt-in (config.UseOutbox = true); use the explicit-category EncinaErrors.Create overload; business failure is a Left, not an exception; no synchronous database calls; connection string from configuration, never logged; zero warnings; English only.
REFERENCE FILES: src/Encina.EntityFrameworkCore/ServiceCollectionExtensions.cs, src/Encina.EntityFrameworkCore/README.md, src/Encina.Messaging/MessagingConfiguration.cs, docker-compose.yml.
```

</details>

### Phase 3: Example `04-dapper-integration` (pull request B)

> **Goal**: Direct SQL with Dapper beside Encina messaging on SQL Server.

<details>
<summary>Tasks</summary>

1. `examples/04-dapper-integration/04-dapper-integration.csproj`: references `Encina`, `Encina.Dapper.SqlServer`, `Dapper`, `Microsoft.Data.SqlClient` (versions from `Directory.Packages.props`).
2. `Orders/CreateOrderHandler.cs` and a query handler using `connection.ExecuteAsync` and `QueryAsync` with parameters; connections come from the registered factory and are opened with `OpenAsync(cancellationToken)`.
3. `Program.cs`: `AddEncinaDapper(...)` with the connection factory overload (exact overload confirmed in `src/Encina.Dapper.SqlServer/ServiceCollectionExtensions.cs`) and the outbox switch per Design Choice 7 (Dilemma), schema script executed at start. `UseOutbox = true` writes nothing unless a request implements `IHasNotifications`, so it is set only under Design Choice 7 option (b). Error categories (ADR-062): the query's `orders.not_found` is `NotFound`, created with the explicit-category overload.
4. `docker-compose.yml` (SQL Server) and `README.md` with the qualitative "when Dapper, when EF Core" notes (Design Choice 6; link to example 03).

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: Encina.Dapper.SqlServer registers stores through AddEncinaDapper (three overloads). The package README shows the connection factory form.
TASK: Build examples/04-dapper-integration as in Phase 3 tasks 1-4.
KEY RULES: parameterized SQL only; OpenAsync with the CancellationToken, never Open(); no figures in the README; verify overloads in src/; connection string never logged; zero warnings; English only.
REFERENCE FILES: src/Encina.Dapper.SqlServer/ServiceCollectionExtensions.cs, src/Encina.Dapper.SqlServer/README.md, Directory.Packages.props, docker-compose.yml.
```

</details>

### Phase 4: Example `05-redis-caching` (pull request C)

> **Goal**: A query is cached with a TTL and a command invalidates it.

<details>
<summary>Tasks</summary>

1. `examples/05-redis-caching/05-redis-caching.csproj`: references `Encina`, `Encina.Caching`, `Encina.Caching.Redis`.
2. `Products/GetProduct.cs` decorated with `[Cache(DurationSeconds = 60)]` (`CacheAttribute`, default 300; `VaryByTenant` defaults to true, explained in the README); `Products/UpdateProduct.cs` decorated with `[InvalidatesCache]` (`KeyPattern`, `BroadcastInvalidation`).
3. Handlers (a not-found returns `EncinaErrors.Create("products.not_found", ErrorCategory.NotFound, ...)`) over an in-memory product list with a counter, so the output shows the handler ran once for two identical queries, then again after the update.
4. `Program.cs`: `AddEncinaCaching(...)` and `AddEncinaRedisCache("localhost:6379")` (confirm options in `src/Encina.Caching/ServiceCollectionExtensions.cs:44`).
5. `docker-compose.yml` (Redis) and `README.md` including the list of the other caching packages that implement the same `ICacheProvider`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT: AddEncinaCaching registers QueryCachingPipelineBehavior and CacheInvalidationPipelineBehavior; AddEncinaRedisCache registers the Redis ICacheProvider. [Cache] and [InvalidatesCache] are in src/Encina.Caching/Attributes/.
TASK: Build examples/05-redis-caching as in Phase 4 tasks 1-5, printing a counter that proves a cache hit and an invalidation.
KEY RULES: call both registrations; the attribute goes on the request type (query or command), not the handler; verify names in src/; no connection string in logs; English only; zero warnings.
REFERENCE FILES: src/Encina.Caching/Attributes/CacheAttribute.cs, src/Encina.Caching/Attributes/InvalidatesCacheAttribute.cs, src/Encina.Caching/ServiceCollectionExtensions.cs, src/Encina.Caching.Redis/ServiceCollectionExtensions.cs.
```

</details>

### Phase 5: Verification and documentation

> **Goal**: Each example is proven once against its compose service and documented.

<details>
<summary>Tasks</summary>

1. For each example: `docker compose up -d`, wait for the health check, `dotnet run --project examples/<name>`, paste the real output into its README, `docker compose down -v`.
2. Zero-warnings build of the project and of `Encina.slnx`; `dotnet format Encina.slnx --verify-no-changes`.
3. A row for each example in `examples/README.md` (created by the #87 plan), stating that it needs Docker.
4. No test project is added: no file under `src/` changes, so no per-flag coverage target applies, and CI builds but does not run these examples (Docker); the integration jobs keep owning Docker-dependent tests.
5. No changelog fragment, ADR, ROADMAP or `PublicAPI` change.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT: The example(s) of your pull request are written (Phases 2-4).
TASK: Run each example against its compose service, paste the real output into the README (by docs-writer, naming the worktree path), run the zero-warnings build and the format check, and add the row to examples/README.md.
KEY RULES: if Docker is unavailable, say so in the report and do not invent output; spawn ci-diagnoser for a failure that is not obvious; no hand-typed figures; English only.
REFERENCE FILES: .claude/skills/encina-docs/SKILL.md, examples/README.md, each example's docker-compose.yml.
```

</details>

---

## Research

### Relevant Standards and Prior Art

| Source | Relevance to this plan |
|--------|------------------------|
| AGENTS.md section 3 (outbox opt-in, async database calls, `TimeProvider`, secrets) | Rules every example follows |
| AGENTS.md section 5 (Database and Caching provider categories) | Which providers are demonstrated and which are named as not demonstrated |
| Transactional outbox pattern (microservices.io) | What example 03 demonstrates |
| `.claude/skills/encina-docs/SKILL.md` | READMEs and no hand-typed figures |
| Comparable samples (Wolverine and MassTransit sample repositories) | One focused sample per infrastructure dependency, each with its own compose file |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `AddEncinaEntityFrameworkCore<TDbContext>` | `src/Encina.EntityFrameworkCore/ServiceCollectionExtensions.cs` | Example 03 registration |
| `OutboxMessageConfiguration`, `UnitOfWorkEF`, `OutboxProcessor` | `src/Encina.EntityFrameworkCore/` | Outbox and transaction in example 03 |
| `AddEncinaDapper` | `src/Encina.Dapper.SqlServer/ServiceCollectionExtensions.cs` | Example 04 registration |
| `AddEncinaCaching`, `[Cache]`, `[InvalidatesCache]` | `src/Encina.Caching/` | Example 05 behaviors and attributes |
| `AddEncinaRedisCache` | `src/Encina.Caching.Redis/ServiceCollectionExtensions.cs` | Example 05 provider |
| `MessagingConfiguration.UseOutbox`, `UseTransactions` | `src/Encina.Messaging/MessagingConfiguration.cs` | Opt-in switches |
| Root compose file and Docker guide | `docker-compose.yml`, `docs/infrastructure/docker-infrastructure.md` | Image tags and conventions |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| None | None | No library code and no `[LoggerMessage]` in `src/`; no range is registered in `EventIdRanges.cs` |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Example 03 | 12 | csproj, `Program.cs`, `AppDbContext.cs`, `Order.cs`, `OrderCreated.cs`, `CreateOrder.cs` (command and handler), `FailingCreateOrder.cs` (command and handler), `OrderCreatedHandler.cs`, compose file, README, `appsettings.json`, plus the two shared files below |
| Example 04 | 10 | csproj, `Program.cs`, handlers (2), schema script, compose file, README, `appsettings.json`, plus the two shared files below |
| Example 05 | 10 | csproj, `Program.cs`, query, command, handlers (2), compose file, README, plus the two shared files below |
| Shared, per pull request | 2 | an `Encina.slnx` entry and a row in `examples/README.md` (counted in each total above as 2 of the files; pre-created by the #87 scaffold pull request if the maintainer's order allows) |
| **Total** | **about 30** | Three pull requests of 10 to 12 files each (shared files included), about 430 lines each; none exceeds 15 files or 1500 lines; no test files (none is added) |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
PROJECT CONTEXT: Encina is a pre-1.0 .NET 10 / C# 14 mediator library with Railway Oriented Programming (Either<EncinaError, T>), opt-in messaging patterns and provider coherence (AGENTS.md). Issue #89 adds three examples under examples/ (plan docs/plans/example-database-cache-implementation-plan-89.md), each in its own pull request after the sub-issue split. D1 (location) is recommended as examples/ with ProjectReference, D2 (EF provider) as SQL Server; both are pending the maintainer.

IMPLEMENTATION OVERVIEW:
1. Conventions: inherit examples/Directory.Build.props; connection string from configuration and never logged; async database calls with CancellationToken; one compose file per example with a health check.
2. 03-entity-framework: AddEncinaEntityFrameworkCore<AppDbContext>(config => UseTransactions and UseOutbox), a command implementing IHasNotifications whose handler calls SaveChangesAsync (the transaction behavior only commits), commit and rollback shown through the Either result, outbox message delivered by the OutboxProcessor to a notification handler (IEncina.Publish does not write the outbox).
3. 04-dapper-integration: AddEncinaDapper with a connection factory (UseOutbox only if D3 option (b) is chosen), parameterized SQL with OpenAsync, qualitative notes without figures.
4. 05-redis-caching: AddEncinaCaching plus AddEncinaRedisCache, [Cache(DurationSeconds)] on a query, [InvalidatesCache] on a command, counter proving hit and invalidation.
5. Verification: run each against its compose service, paste real output into the README, zero-warnings build, format check.

KEY PATTERNS: verify every identifier in src/ before use; the caching example needs both registrations; never IDbConnection.Open(); outbox only when config.UseOutbox is set; no hand-typed performance figures; README names the providers not demonstrated; English only; no AI attribution; no private client application names.

REFERENCE FILES: src/Encina.EntityFrameworkCore/ServiceCollectionExtensions.cs, src/Encina.EntityFrameworkCore/README.md, src/Encina.Dapper.SqlServer/ServiceCollectionExtensions.cs, src/Encina.Dapper.SqlServer/README.md, src/Encina.Caching/ServiceCollectionExtensions.cs, src/Encina.Caching/Attributes/CacheAttribute.cs, src/Encina.Caching.Redis/ServiceCollectionExtensions.cs, docker-compose.yml, docs/infrastructure/docker-infrastructure.md.
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ | Example 05 (`[Cache]`, `[InvalidatesCache]`, Redis) |
| 2 | OpenTelemetry | ❌ | Example code only; the dashboard guide and its sample are #188 |
| 3 | Structured Logging | ❌ | No library code, no EventIds; the examples print to the console |
| 4 | Health Checks | ❌ | The compose services have container health checks; an `IEncinaHealthCheck` in the app is not part of these examples (health checks are in `docs/guides/health-checks.md`) |
| 5 | Validation | ❌ | Shown in #88 |
| 6 | Resilience | ❌ | Not shown; a connection-retry demo would belong to a Polly example, not to this family |
| 7 | Distributed Locks | ❌ | Single-instance demos |
| 8 | Transactions | ✅ | Example 03 (`UseTransactions`, commit and rollback from the `Either` result) |
| 9 | Idempotency | ❌ | The inbox is not part of these examples |
| 10 | Multi-Tenancy | ❌ | No tenant data (`VaryByTenant` is explained in the example 05 README only) |
| 11 | Module Isolation | ❌ | Single module |
| 12 | Audit Trail | ❌ | Demo data with no compliance implication |

No function is deferred, so no follow-up issue is needed for the matrix.

---

## Prerequisites & Dependencies

| Item | State | Relation |
|------|-------|----------|
| #87 plan: `examples/` scaffold and D1 | open (plan in this family) | The three pull requests start after the scaffold merges |
| Split of #89 into three sub-issues (one per example) | to do (orchestrator, before any brief) | Required by AGENTS.md section 11 |
| `Encina.slnx` and `examples/README.md` shared by #87, #88, #89 (three PRs) and #188 | each pull request edits both | Land in order after #87, or let #87 pre-create the rows and entries; parallel pull requests conflict |
| Docker available to the worker | environment | Needed for the Phase 5 run; without it the report says so |
| `Directory.Packages.props` entries | present | `Microsoft.EntityFrameworkCore.SqlServer`, `Dapper`, `Microsoft.Data.SqlClient`, `StackExchange.Redis` are already listed; if D2 picks PostgreSQL the worker adds the provider entry |

**Risks**: the compose image tags drift from the root file (a comment names the source); the examples are only built in CI, so a runtime regression is found at the next manual run (accepted; the integration jobs cover the same registrations in their own tests); a developer's port 1433 or 6379 may be in use (the README names the port variable).

---

## Next Steps

1. Merge this plan.
2. The orchestrator splits #89 into three sub-issues (03, 04, 05), each with its file list and acceptance, before any brief.
3. When each sub-issue is picked up, re-check D1 (recorded in the #87 plan) and ask D2 (EF provider for 03); record the answers in `## Maintainer Decisions`.
4. One worker brief per example; delegate READMEs to `docs-writer` and review to `adversarial-reviewer`.
5. Close #89 when the three pull requests have merged.
