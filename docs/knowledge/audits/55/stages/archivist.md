## Scope

Closing commit 7e50983ed (timeline `closed` event commit_id; the issue was closed manually 2025-12-27T11:31:05Z, created 2025-12-24T13:29:41Z; no linked PR). Surviving today:

- `src/Encina.DistributedLock/` (IDistributedLockProvider.cs, ILockHandle.cs, DistributedLockOptions.cs, LockAcquisitionException.cs, ServiceCollectionExtensions.cs, README.md)
- `src/Encina.DistributedLock.InMemory/InMemoryDistributedLockProvider.cs`
- `src/Encina.DistributedLock.Redis/RedisDistributedLockProvider.cs`, `Health/RedisDistributedLockHealthCheck.cs`, `ServiceCollectionExtensions.cs`
- `src/Encina.DistributedLock.SqlServer/SqlServerDistributedLockProvider.cs`, `Health/SqlServerDistributedLockHealthCheck.cs`, `ServiceCollectionExtensions.cs`
- `src/Encina.Caching/Abstractions/IDistributedLockProvider.cs`: reduced in the same commit to reference the new abstraction; the file no longer exists today (Test-Path false), the interface lives only in `src/Encina.DistributedLock/`.
- Coverage manifests: `.github/coverage-manifest/Encina.DistributedLock{,.InMemory,.Redis,.SqlServer}.json`

Tests were consolidated; the per-feature test projects of the commit (`tests/Encina.DistributedLock.*Tests`) no longer exist. Today:
- `tests/Encina.UnitTests/DistributedLock/` (InMemory, Redis/, SqlServer/ incl. health checks, options)
- `tests/Encina.GuardTests/Infrastructure/DistributedLock/`
- `tests/Encina.IntegrationTests/Infrastructure/DistributedLock/`
- `tests/Encina.BenchmarkTests/Encina.DistributedLock.Benchmarks/`
- Contract, property and load tests of the commit: not found under a DistributedLock name in `tests/`; the test-auditor should search by class name (`IDistributedLockProviderContractTests`, `DistributedLockInvariantsTests`, `DistributedLockLoadTests`).

Nothing was removed on purpose. Packages in the issue's "Affected Packages" that do not exist: `Encina.DistributedLock.Redis` and `.SqlServer` exist; the issue omitted `.InMemory`, which shipped.

Divergences for the code stage: (1) interface is `IDistributedLockProvider` with `Task<IAsyncDisposable?>`, not `IDistributedLock`/`ValueTask`; (2) Redis uses `StringSetAsync(When.NotExists)` + Lua release, not Redlock; (3) auto-release by TTL (Redis) and session/transaction scope (SqlServer).

## Destinations

- API decision (IDistributedLockProvider): present, `src/Encina.DistributedLock/README.md` exists; AGENTS.md section 5 names `IDistributedLockProvider`.
- Providers and the 1.0 lock set: present in AGENTS.md section 5 ("Distributed locks" row, `sp_getapplock` found there).
- Redlock: missing; no code, no tracker found by name (#207/#208/#209-#213 are other providers, not Redlock).
- Documentation: `docs/` page still pending as #908 (OPEN).
- The pre-draft's "Candidate destinations" (adr, spec-invariant, coverage-manifest 85%) were dropped: no ADR names this decision (grep `DistributedLock`/`sp_getapplock` over docs/architecture/adr hits only 018, 021, 022, 024, 025, 050, none deciding the lock design), 85% coverage is superseded by per-flag manifests.
- `docs/history/2025-12.md`, touched by the commit, does not exist in the worktree.

## Successor and duplicate issues

No successor or duplicate. Related follow-ups, state verified 2026-10-22: #908 docs OPEN, #222 observability OPEN, #724 OTel OPEN, #755 remaining health checks OPEN, #207 PostgreSQL OPEN, #208 MySQL OPEN, #741 tenant-scoped locks OPEN, #885 epic OPEN, #214 Oracle CLOSED.

## Lessons for the pipeline

- The pre-draft listed code touched as "unknown" and `closed_at: 12/27/2025 11:31:05`, `linked_prs: []`, and packages straight from the issue; resolved with the timeline `closed` commit and `git show --stat`.
- The pre-draft's "Rules and lessons" and "spec-invariant"/"adr" destinations had no quotable source and were dropped.
- The issue title names an interface (`IDistributedLock`) that never shipped; compare title symbols with src/ before choosing `delivered` (outcome set to `partial` because of the unbuilt Redlock and renamed API).
- The pre-draft was read from the main checkout (the worktree has none, `artifacts/` is gitignored).
