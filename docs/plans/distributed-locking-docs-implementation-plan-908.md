# Implementation Plan: Distributed Locking Documentation — feature page and entry points

> **Issue**: [#908](https://github.com/dlrivada/Encina/issues/908)
> **Type**: Feature (documentation)
> **Complexity**: Low (3 phases, 4 files, no production code, no provider code)
> **Estimated Scope**: about 450-600 changed lines of Markdown, no tests

---

## Summary

Create `docs/features/distributed-locking.md`, the first page about Encina's distributed locking: the `IDistributedLockProvider` abstraction, the three providers that exist (InMemory, Redis, SQL Server), the recommended usage patterns, configuration, failure modes and the providers still to come. Add the entry points the issue names: a short Distributed Locking section in `README.md` (it has none today: a search for "Lock" in the README finds nothing), one entry in `docs/features/index.md` and a link from `src/Encina.DistributedLock/README.md`.

What the code says today, which the page must follow (read from `src/` on 2026-11-11):

- `IDistributedLockProvider` has `Task<IAsyncDisposable?> TryAcquireAsync(...)`, `Task<IAsyncDisposable> AcquireAsync(...)`, `Task<bool> IsLockedAsync(string resource, CancellationToken)` and `Task<bool> ExtendAsync(string resource, TimeSpan extension, CancellationToken)`; the handle type is `ILockHandle` (the issue's earlier text named a type that does not exist).
- `DistributedLockOptions` has `KeyPrefix` (empty), `DefaultExpiry` (30 s), `DefaultWait` (10 s), `DefaultRetry` (100 ms) and `ProviderHealthCheck`. `InMemoryLockOptions.WarnOnUse` (true), `RedisLockOptions.Database` and `SqlServerLockOptions.ConnectionString` extend it.
- Registration: `AddEncinaDistributedLock` (two overloads), the InMemory registration, `AddEncinaDistributedLockRedis` (four overloads: connection string or `IConnectionMultiplexer`, each with or without `Action<RedisLockOptions>`) and the SQL Server registration, in each package's `ServiceCollectionExtensions.cs`.
- The Redis provider is a single-instance `SET NX` acquisition with Lua scripts; its own remarks say Redlock across instances is "to consider" (`RedisDistributedLockProvider.cs` line 11). The AGENTS.md section 5 table names the Redis lock "Redis (Redlock)". The page describes what the code does and does not claim Redlock; the wording of AGENTS.md is reported to the orchestrator, not edited here.
- The SQL Server provider uses `sp_getapplock` and throws `LockAcquisitionException` (lines 119 and 124 of `SqlServerDistributedLockProvider.cs`). [ADR-052](../architecture/adr/052-distributed-lock-failure-contract.md) fixes the target contract (providers return `Either`, contention is not an error); its implementation, [#2667](https://github.com/dlrivada/Encina/issues/2667), is open.
- Only Redis and SQL Server have a health check (`RedisDistributedLockHealthCheck`, `SqlServerDistributedLockHealthCheck`); the InMemory provider has none.
- 1.0 scope is exactly seven providers (AGENTS.md section 5, SPEC-000 DEC-009): InMemory, Redis, SQL Server and the open [#207](https://github.com/dlrivada/Encina/issues/207) (PostgreSQL), [#208](https://github.com/dlrivada/Encina/issues/208) (MySQL), [#209](https://github.com/dlrivada/Encina/issues/209) (Azure Blob) and [#210](https://github.com/dlrivada/Encina/issues/210) (DynamoDB). Consul, etcd and ZooKeeper are post-1.0.
- No production code under `src/` consumes `IDistributedLockProvider` except the cache packages' own lock providers (ADR-052 context), so the leader-election and duplicate-processing patterns are recommended usage, and the page says so.

**Provider category**: distributed locks (AGENTS.md section 5). The page covers all three existing providers and lists the four planned ones with their issues.

**Affected packages**: none changed. Documented: `Encina.DistributedLock`, `Encina.DistributedLock.InMemory`, `Encina.DistributedLock.Redis`, `Encina.DistributedLock.SqlServer`.

**Size for the worker brief**: 4 files, about 500 lines. Within the 15-file and 1,500-line limit of one pull request.

---

## Design Choices

No open design choices. Every choice is settled by a cited source; the `Verified` column stays empty until the adversarial review sets it.

| Id | Choice | Class | Settled by | Verified |
|----|--------|-------|------------|----------|
| 1 | One page, `docs/features/distributed-locking.md`, Explanation plus option and API tables as the other feature pages; no `docs/toc.yml` entry (the file does not exist, navigation is front matter) | Settled | `.claude/skills/encina-docs/SKILL.md` sections 1 and 2; issue comment (CodeRabbit plan, design choice 1) | |
| 2 | Document the current API (`Task<IAsyncDisposable?>` and `LockAcquisitionException`) and link ADR-052 for the target `Either` contract; re-check the page when #2667 merges | Settled | ADR-052; issue #908 acceptance criterion "re-checked against the API when #2667 changes the return types"; SKILL.md section 3 rule 3 | |
| 3 | Leader election and duplicate-processing patterns are presented as recommended usage, not built-in behaviour | Settled | ADR-052 context table (no `src/` consumer of `IDistributedLockProvider`); SKILL.md section 3 rule 7 | |
| 4 | Redis is described as single-instance `SET NX` with Lua scripts, not as Redlock | Settled | `src/Encina.DistributedLock.Redis/RedisDistributedLockProvider.cs` line 11 and the Lua scripts in the same file; SKILL.md section 3 rule 3 | |
| 5 | Planned providers are listed with their issues (#207 to #210) and Consul, etcd and ZooKeeper as post-1.0, with no implementation detail the code does not hold | Settled | AGENTS.md section 5 (distributed locks row); SPEC-000 DEC-009 | |
| 6 | Mermaid for the class diagram and the leader-election flow | Trivial | SKILL.md section 2 (Mermaid is available) | |

---

## Implementation Phases

### Phase 1: The page — `docs/features/distributed-locking.md`

<details>
<summary>Tasks</summary>

1. Create `docs/features/distributed-locking.md`, front matter `title: "Distributed locking"`, `layout: default`, `parent: "Features"`. Opening sentence: who it is for (a developer coordinating work across several instances of an application) and what they get.
2. Overview and problem: why a lock, and when optimistic concurrency (`docs/features/optimistic-concurrency.md`) is the better tool; link it.
3. Abstraction: a table for `IDistributedLockProvider` (the four methods, parameters and return types copied from `src/Encina.DistributedLock/IDistributedLockProvider.cs`), a table for `ILockHandle` (`Resource`, `LockId`, `AcquiredAtUtc`, `ExpiresAtUtc`, `IsReleased`, `ExtendAsync`), `LockAcquisitionException`, and a Mermaid class diagram.
4. Providers: one subsection each for InMemory (`ConcurrentDictionary`, testing, `WarnOnUse`), Redis (`SET NX` with expiry, Lua owner-checked release and extend, `Database`, the four registration overloads, no Redlock) and SQL Server (`sp_getapplock`, `ConnectionString`, timeout delegated to SQL Server). A provider matrix: persistence, cross-process, health check, production use.
5. Patterns (flagged as recommended usage): leader election for a `BackgroundService` with `await using`, preventing duplicate Outbox or Inbox processing, and coordination in sharded environments with key naming and granularity trade-offs. Every sample compiles in a scratch project.
6. Configuration: `DistributedLockOptions` table, `expiry` versus `wait` versus `retry`, cancellation tokens; the registration methods.
7. Failure modes: lock expiry during long work (`ExtendAsync`), network partition, provider unavailability, as a "what happens when" table per provider, linked to ADR-052; no claim the code does not back.
8. Health checks: the two that exist, `ProviderHealthCheck.Enabled`, and that InMemory has none; link the health checks reference of #907 once it exists (do not link a page that is not merged).
9. Planned providers table (PostgreSQL #207, MySQL #208, Azure Blob #209, DynamoDB #210; post-1.0: Consul, etcd, ZooKeeper) and a "Performance" line that cites `docs/benchmarks/distributed-lock-performance.md`, never a typed figure.
10. Link ADR-052 and the spike report `docs/engineering/rop-exceptions-locks-aggregates-spike-1799-1798.md` from the failure-modes section.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT
Encina is a pre-1.0 .NET 10 library. Worktree: <worktree> (absolute paths only). You write one Explanation page (with API and option tables) of the Diataxis model for a junior .NET developer. Plan: docs/plans/distributed-locking-docs-implementation-plan-908.md.

TASK
Create <worktree>\docs\features\distributed-locking.md as listed in Phase 1 of the plan. Read first: src/Encina.DistributedLock/*.cs, src/Encina.DistributedLock.InMemory/*.cs, src/Encina.DistributedLock.Redis/*.cs and Health/*.cs, src/Encina.DistributedLock.SqlServer/*.cs and Health/*.cs, docs/architecture/adr/052-distributed-lock-failure-contract.md, docs/benchmarks/distributed-lock-performance.md.

KEY RULES
- Copy every signature, option name and default from src/; grep each identifier before writing it. The handle type is ILockHandle.
- Redis is single-instance SET NX with Lua scripts; do not write Redlock.
- Patterns are recommended usage: no src/ consumer of IDistributedLockProvider exists yet; say so.
- Document the current API; link ADR-052 for the target Either contract; do not describe the target API as available.
- No hand-typed performance figures: cite docs/benchmarks/distributed-lock-performance.md.
- Compile every C# sample in a scratch project outside the repository.
- English only, no emojis, Mermaid for diagrams, markdownlint and link check clean; commit locally, never push.

REFERENCE FILES
.claude/skills/encina-docs/SKILL.md, .claude/skills/encina-docs/diataxis.md, docs/features/optimistic-concurrency.md, docs/features/id-generation.md (layout reference), AGENTS.md section 5
```

</details>

### Phase 2: Entry points

<details>
<summary>Tasks</summary>

1. `README.md`: add a short `### Distributed Locking` section with a registration snippet copied from `src/Encina.DistributedLock.Redis/ServiceCollectionExtensions.cs` and a link to the page, in the style of the neighbouring feature sections (find the right parent heading first; the README has no lock section today).
2. `docs/features/index.md`: add `- [Distributed Locking](distributed-locking.md)` under the heading that fits (the "Other" group or "Messaging & Caching"; pick the one whose neighbours share the topic).
3. `src/Encina.DistributedLock/README.md`: add one link to the page (the file exists).
4. No `docs/toc.yml`; no changelog fragment unless the maintainer says documentation is user-visible.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT
Encina is a pre-1.0 .NET 10 library; the site is Jekyll with just-the-docs. Worktree: <worktree> (absolute paths only). Phase 1 created docs/features/distributed-locking.md.

TASK
Add the entry points of Phase 2 in docs/plans/distributed-locking-docs-implementation-plan-908.md: a short Distributed Locking section in README.md, one line in docs/features/index.md, one link in src/Encina.DistributedLock/README.md.

KEY RULES
- The README snippet uses a real registration method copied from src/ (AddEncinaDistributedLockRedis has four overloads; pick one and match its parameters).
- Do not copy page content; link.
- Relative links resolve (Test-Path from the file's folder).
- English only, no emojis; commit locally, never push.

REFERENCE FILES
README.md, docs/features/index.md, src/Encina.DistributedLock/README.md, src/Encina.DistributedLock.Redis/ServiceCollectionExtensions.cs
```

</details>

### Phase 3: Verification and finalization

<details>
<summary>Tasks</summary>

1. Run the checks of the encina-docs skill section 5: `lychee --offline --config .github/lychee.toml 'docs/**/*.md' 'README.md'`, `npx --yes markdownlint-cli2 "docs/**/*.md" "#docs/INVENTORY.md"` and the `cov-docs-render.cs --check-dangling` command; when a tool is missing, say so and check relative links with `Test-Path`.
2. Compile every C# sample of the page in a scratch project against the repository packages; paste the result in the PR.
3. Run `docs-reviewer` on the page.
4. `docs/INVENTORY.md`: add the page if it lists feature pages.
5. After #2667 merges, re-check the page (a separate small change; this plan does not block on it).
6. No `src/`, PublicAPI, EventId or coverage-manifest change.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT
Encina is a pre-1.0 .NET 10 library. Worktree: <worktree> (absolute paths only). Phases 1 and 2 are committed.

TASK
Run the verification of Phase 3 in docs/plans/distributed-locking-docs-implementation-plan-908.md, fix every finding, and report the exact output of each command.

KEY RULES
- Do not skip a tool silently: report a missing tool and check relative links with Test-Path.
- Compile each C# sample in a scratch project outside the repository tree and paste the result.
- No changes under src/ other than the README link; no PublicAPI, EventId or coverage-manifest edits.
- English only, no emojis, no AI attribution; never push.

REFERENCE FILES
.claude/skills/encina-docs/SKILL.md sections 5 and 6, .github/lychee.toml, .markdownlint.json, .github/scripts/cov-docs-render.cs
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard or specification | Relevance | Where it applies |
|---------------------------|-----------|------------------|
| [ADR-052](../architecture/adr/052-distributed-lock-failure-contract.md) | Failure contract: providers return `Either`, contention is not an error; implementation #2667 open | Failure modes section, API note |
| SPEC-000 DEC-009 and AGENTS.md section 5 | The 1.0 lock set is exactly seven providers | Provider matrix, planned providers |
| [ADR-018](../architecture/adr/018-cross-cutting-integration-principle.md) | Distributed locks are function 7 of the 12 cross-cutting functions | Patterns section |
| Redis `SET key value NX PX` and Lua scripting; SQL Server `sp_getapplock` | The mechanisms the providers use | Provider subsections |
| Diátaxis | One quadrant per page | `.claude/skills/encina-docs/diataxis.md` |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| IDistributedLockProvider | src/Encina.DistributedLock/IDistributedLockProvider.cs | the abstraction: TryAcquireAsync, AcquireAsync, IsLockedAsync, ExtendAsync |
| ILockHandle | src/Encina.DistributedLock/ILockHandle.cs | handle type documented in the API section |
| DistributedLockOptions | src/Encina.DistributedLock/DistributedLockOptions.cs | KeyPrefix, DefaultExpiry, DefaultWait, DefaultRetry, ProviderHealthCheck |
| LockAcquisitionException | src/Encina.DistributedLock/LockAcquisitionException.cs | the exception thrown today by the SqlServer provider |
| Registration | src/Encina.DistributedLock/ServiceCollectionExtensions.cs | AddEncinaDistributedLock overloads |
| InMemory provider | src/Encina.DistributedLock.InMemory/InMemoryDistributedLockProvider.cs | testing provider, InMemoryLockOptions.WarnOnUse |
| Redis provider | src/Encina.DistributedLock.Redis/RedisDistributedLockProvider.cs | single-instance SET NX with Lua scripts, RedisLockOptions.Database |
| SqlServer provider | src/Encina.DistributedLock.SqlServer/SqlServerDistributedLockProvider.cs | sp_getapplock, SqlServerLockOptions.ConnectionString |
| Lock health checks | src/Encina.DistributedLock.Redis/Health/RedisDistributedLockHealthCheck.cs | health checks of the Redis and SqlServer providers |
| ADR-052 | docs/architecture/adr/052-distributed-lock-failure-contract.md | failure contract the page links |
| Package README | src/Encina.DistributedLock/README.md | receives a link to the page |
| Benchmarks | docs/benchmarks/distributed-lock-performance.md | cited for any performance statement |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| None | None | Documentation only: no `[LoggerMessage]`, so nothing to register in `src/Encina/Diagnostics/EventIdRanges.cs` |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Feature page (docs/features/distributed-locking.md) | 1 | new |
| README.md | 1 | one link in a new short Distributed Locking section or an existing one |
| docs/features/index.md | 1 | one entry |
| src/Encina.DistributedLock/README.md | 1 | one link |
| Changelog fragment | 0 | not applicable |

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt — all phases</summary>

```text
PROJECT CONTEXT
Encina is a pre-1.0 .NET 10 / C# 14 library (no backward compatibility, no [Obsolete]). You are a docs-writer in the git worktree <worktree> (absolute paths only; never touch the main checkout). Rules: AGENTS.md and .claude/skills/encina-docs/SKILL.md. Issue: https://github.com/dlrivada/Encina/issues/908. No production code changes.

IMPLEMENTATION OVERVIEW
1. Create docs/features/distributed-locking.md: overview, abstraction tables, three providers, patterns (recommended usage), configuration, failure modes (ADR-052), health checks, planned providers (#207 to #210).
2. Add a Distributed Locking section to README.md, an entry to docs/features/index.md and a link in src/Encina.DistributedLock/README.md.
3. Verify (lychee, markdownlint-cli2, cov-docs-render check, compile every C# sample in a scratch project), run docs-reviewer, commit locally.

KEY PATTERNS
- TryAcquireAsync returns Task<IAsyncDisposable?>; AcquireAsync returns Task<IAsyncDisposable>; the handle type is ILockHandle; LockAcquisitionException is thrown by the SQL Server provider today.
- Redis is single-instance SET NX with Lua scripts, not Redlock.
- DistributedLockOptions defaults: DefaultExpiry 30 s, DefaultWait 10 s, DefaultRetry 100 ms.
- One Diataxis quadrant, links not restatements, no typed performance figures, every identifier verified in src/, English only, no emojis.

REFERENCE FILES
docs/plans/distributed-locking-docs-implementation-plan-908.md, .claude/skills/encina-docs/SKILL.md, src/Encina.DistributedLock*/, docs/architecture/adr/052-distributed-lock-failure-contract.md, docs/benchmarks/distributed-lock-performance.md, README.md, docs/features/index.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Documentation only, no runtime code. |
| 2 | OpenTelemetry | ❌ | Documentation only, no runtime code, no metrics or spans. |
| 3 | Structured Logging | ❌ | Documentation only; no `[LoggerMessage]`, so no EventId range is reserved. |
| 4 | Health Checks | ❌ | The page documents the two existing lock health checks; no runtime change. The missing InMemory check is a code matter, not this issue's. |
| 5 | Validation | ❌ | Documentation only; the page receives no input. |
| 6 | Resilience | ❌ | Documentation only; timeouts and failure modes are documented, not changed. |
| 7 | Distributed Locks | ❌ | This feature documents function 7; no lock code is changed. |
| 8 | Transactions | ❌ | Documentation only, no state change. |
| 9 | Idempotency | ❌ | Documentation only; the duplicate-processing pattern is described as recommended usage. |
| 10 | Multi-Tenancy | ❌ | Documentation only; `KeyPrefix` is documented as an option, no tenancy change. |
| 11 | Module Isolation | ❌ | Documentation only, no runtime code. |
| 12 | Audit Trail | ❌ | Documentation only, no runtime code. |

---

## Prerequisites & Dependencies

- [#2667](https://github.com/dlrivada/Encina/issues/2667) (open, `technical-debt`, p0): changes the return types. Not a prerequisite; the page documents the current API and is re-checked when it merges (Design Choice 2).
- Open provider issues #207 to #210 are linked, not required.
- For a reviewer: AGENTS.md section 5 calls the Redis lock "Redlock" while the provider is single-instance; the page follows the code. The wording of AGENTS.md is reported to the orchestrator.

---

## Next Steps

1. The orchestrator links this plan from #908. There are no Dilemmas, so nothing needs the maintainer before work starts.
2. Brief a `docs-writer` for the page and the three link edits (4 files, about 500 lines).
3. After #2667 merges, open a small docs change to re-check the page.
