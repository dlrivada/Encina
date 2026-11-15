# Implementation Plan: Caching Documentation — Strategy Overview, `CachingOptions` Reference and One Page per Provider

> **Issue**: [#84](https://github.com/dlrivada/Encina/issues/84)
> **Type**: Feature (documentation only, no production code)
> **Complexity**: Medium (4 phases, 3 pull requests, 19 files)
> **Estimated Scope**: ~9 pages under `docs/features/caching/` (about 1,200 lines of Markdown), 8 package READMEs (about 250 lines), 1 index entry, 1 changelog fragment

---

## Summary

Document Encina caching for a junior .NET developer who has never seen it. Today none of the 8 caching packages (`Encina.Caching` and the 7 providers `Encina.Caching.Memory`, `.Hybrid`, `.Redis`, `.Valkey`, `.KeyDB`, `.Dragonfly`, `.Garnet`) has a README, handler-level caching (`AddEncinaCaching`, `[Cache]`, the three pipeline behaviors) has no feature page, and there is no option table for `CachingOptions`. The only caching pages under `docs/` are `docs/features/query-caching.md` (EF Core second-level cache, a different feature), `docs/features/cdc-cache-invalidation.md` and the benchmark pages `docs/benchmarks/caching-*.md`.

The plan produces, under `docs/features/caching/`:

- `overview.md`: strategy guide, provider comparison, handler-level caching, a Mermaid flow diagram of a cached query (key generation, L1/L2 lookup, handler call, store, invalidation message over the pub/sub channel), invalidation patterns, a short section on the sharding caches (`src/Encina.Caching/Sharding/`) and the known limits tracked by open bugs.
- `options.md`: the `CachingOptions` table (17 public properties, `src/Encina.Caching/Configuration/CachingOptions.cs`) and the `CacheSerializerOptions` table (3 properties), each with type, default and meaning.
- One page per provider: `memory.md`, `hybrid.md`, `redis.md`, `valkey.md`, `keydb.md`, `dragonfly.md`, `garnet.md`. Memcached is named as planned only (`AGENTS.md` section 5, SPEC-000 REQ-027, #277).
- A README for each of the 8 caching packages, plus an entry in `docs/features/index.md` and the changelog fragment `changelog.d/84-caching-docs.added.md`.

Facts that shape the pages, read from `src/` on origin/main (d1793713d; re-verify the identifiers when PR 1 starts): `Encina.Caching.Valkey`, `.KeyDB`, `.Dragonfly` and `.Garnet` contain only a `ServiceCollectionExtensions.cs` whose four overloads delegate to `AddEncinaRedisCache`; the cache, lock and pub/sub implementations (`RedisCacheProvider`, `RedisDistributedLockProvider`, `RedisPubSubProvider`) live in `Encina.Caching.Redis`, and `Encina.Caching.Memory` holds `MemoryCacheProvider`, `MemoryDistributedLockProvider` and `MemoryPubSubProvider`. `Encina.Caching.Hybrid` holds `HybridCacheProvider` and two registrations (`AddEncinaHybridCache`, `AddEncinaHybridCacheWithOptions`). The default `KeyPrefix` is `"sm"` and the three sibling defaults start with `sm:`, an older product prefix; the pages document the value as it is and a debt issue is written for it (see Prerequisites & Dependencies).

**Which provider category applies**: Caching (8 providers, `AGENTS.md` section 5). The pages must state per provider what it supports (Get/Set/Remove, TTL, serialization abstraction, pub/sub backplane where applicable).
**Affected packages**: none changed; documented only.
**Size**: the work exceeds one worker pull request (19 files), so Phase 1 to Phase 3 each ship as their own pull request (5, 5 and 9 files; the changelog fragment ships with the last one).

---

## Design Choices

<details>
<summary><strong>1. Location and navigation — <code>docs/features/caching/</code> with <code>parent: "Features"</code></strong></summary>

**Class:** Trivial
**Settled by:** `.claude/skills/encina-docs/SKILL.md` section 1 and the sibling pages in `docs/features/` (`docs/features/abac/` uses `parent: "Features"` on a folder of pages).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `docs/features/caching/`, each page `parent: "Features"`** | Follows the sibling feature folders; the sub-pages stay discoverable from the Features index | A flat list of 9 children under Features |
| **B) `docs/caching/` (the original proposal)** | Short URL | The folder does not exist and has no sibling; breaks the navigation convention |
| **C) A parent page `Caching` with `has_children`** | Nested navigation | Needs `grand_parent` on every child; no other feature folder does it |

### Chosen Option: **A — `docs/features/caching/`, flat under Features**

### Rationale

- The repository convention is feature pages under `docs/features/`, and `docs/features/index.md` is the navigation source (there is no `docs/toc.yml`).
- Pre-1.0 and 9 small pages do not justify a three-level navigation tree.

</details>

<details>
<summary><strong>2. Pages for the four Redis-compatible providers — one short page each, or one shared page</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One short page per provider (`valkey.md`, `keydb.md`, `dragonfly.md`, `garnet.md`)** | Each provider is findable by name in the navigation and in search; matches the 8-provider matrix of `AGENTS.md` section 5 and the issue's "document each provider"; each page can hold its own default port and server notes | Four pages repeat the same registration overloads (all delegate to `AddEncinaRedisCache`); a change to the Redis registration touches five pages |
| **B) One page `redis-compatible.md` covering Valkey, KeyDB, Dragonfly and Garnet next to a full `redis.md`** | No repetition; one place to update when the delegation changes | A reader looking for "Garnet" finds a section, not a page; the navigation hides four providers the matrix lists as first-class |

### Chosen Option: **A — one short page per provider** (recommended, pending the maintainer)

### Rationale

- A: wins for a new user who searches the docs for the server they already run, and for the provider-coherence rule that every provider is documented the same way. B wins when the maintenance cost of repeated text matters more, for example if the wrappers gain real differences and the pages drift. The recommendation keeps each wrapper page short (registration, default port, a link to `redis.md` for every shared behavior) so that the repetition stays small.

</details>

<details>
<summary><strong>3. Package READMEs — written in this feature, in their own pull request</strong></summary>

**Class:** Settled
**Settled by:** `AGENTS.md` section 8 ("Each satellite package has its own README") and `.claude/skills/encina-docs/SKILL.md` section 3 rule 8 (a package README is reference plus one short how-to).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Write the 8 READMEs here as Phase 3, linking the new pages** | One coherent documentation set; the issue's Documentation checklist asks for it | A third pull request |
| **B) Leave the READMEs to a sibling issue** | Smaller scope | The 8 packages stay without README; the rule has no exception; the pages would link to nothing from the package |

### Chosen Option: **A — Phase 3 writes the 8 READMEs**

### Rationale

- The rule requires a README for each satellite package, and the packages are the subject of this feature. Phase 3 is its own pull request of 8 files, which keeps every pull request within the limits of `AGENTS.md` section 11.

</details>

<details>
<summary><strong>4. Pull request split — three pull requests</strong></summary>

**Class:** Settled
**Settled by:** `AGENTS.md` section 11 (a worker's issue is one pull request of at most 15 files and 1500 changed lines).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Three pull requests: overview, options and the two own-implementation providers (5 files); the Redis family (5 files); the READMEs and the changelog fragment (9 files)** | Every pull request within the limits; the first one already gives the reader the strategy and the option table | The Redis-family pages land after the overview that links to them, so the overview links them only in PR 2 |
| **B) One pull request with all 19 files** | One review | Over the 15-file limit; the `pr-size` job fails it |

### Chosen Option: **A — three pull requests**

### Rationale

- The `pr-size` job blocks a pull request over 15 files. The overview in PR 1 names the Redis-family providers in a table without links; PR 2 adds the links in the same files it creates plus one edit to `overview.md` (the file counts stay within the limit).

</details>

<details>
<summary><strong>5. Performance and coverage figures — cited, never typed</strong></summary>

**Class:** Settled
**Settled by:** `AGENTS.md` section 9 ("Documentation NEVER types coverage figures by hand") and SPEC-001.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Link `docs/benchmarks/caching-*-benchmark-results.md` and `caching-performance.md`; use a docref marker where a number is needed; drop the original star ratings** | Figures stay correct when benchmarks are re-run | The comparison table has no ranking column |
| **B) Type the numbers and star ratings from the original issue** | Quick to read | Goes stale; breaks the no-hand-typed-figures rule |

### Chosen Option: **A — cite the benchmark pages**

### Rationale

- The comparison table keeps only facts read from source (type L1, L2 or L1+L2, persistence, cluster support, pub/sub backplane). A performance section links the benchmark pages, which exist for Memory, Hybrid and Redis; the pages for the four wrappers say the Redis figures apply because the implementation is shared.

</details>

---

## Implementation Phases

### Phase 1: Overview, Options Reference, Memory and Hybrid Pages (pull request 1)

<details>
<summary>Tasks</summary>

1. `docs/features/caching/overview.md` (Explanation plus how-to): strategy guide (when to cache, L1 versus L2, invalidation), provider comparison table (type, persistence, cluster, pub/sub, lock provider, taken from each provider's source), handler-level caching (`AddEncinaCaching` at `src/Encina.Caching/ServiceCollectionExtensions.cs:45` registers `ICacheKeyGenerator`, `QueryCachingPipelineBehavior<,>`, `CacheInvalidationPipelineBehavior<,>` and `DistributedIdempotencyPipelineBehavior<,>`; `[Cache]` with `DurationSeconds`, `KeyTemplate`, `VaryByUser`, `VaryByTenant`, `Priority`, `SlidingExpiration`, `MaxAbsoluteExpirationSeconds`; `[InvalidatesCache]`; `AddCacheKeyGenerator<T>`; `AddCacheConfiguration<TRequest>`), the Mermaid sequence diagram, invalidation patterns, the sharding caches, Memcached as planned, known limits linked to #2436, #1954, #2452 and #3438, and links to `docs/features/query-caching.md` and `docs/features/cdc-cache-invalidation.md`. The overview also has a "Performance" section (which provider fits which load, linking the `docs/benchmarks/caching-*.md` pages, no hand-typed figures) and an "Idempotency" subsection: `EnableDistributedIdempotency` defaults to `true`, and #3438 (keyed by tenant and key only, so two users or payloads sharing a key can receive each other's cached result) and #2452 (key shared across tenants when no tenant is resolved) are open; the subsection states the risk and the mitigations the code allows (set the option to `false`, or give every request an unambiguous idempotency key). The `[Cache]` section carries the same kind of warning for #2436 and #1954 (`VaryByUser` invalidation).
2. `docs/features/caching/options.md` (Reference): the `EnableDistributedIdempotency` row carries the same warning (risk, #3438, #2452, mitigation) next to the default, not only a link to the overview; `CachingOptions` (5 `Enable*` switches, `DefaultDuration`, `DefaultPriority`, `KeyPrefix`, `InvalidationChannel`, `IdempotencyKeyPrefix`, `IdempotencyTtl`, `LockKeyPrefix`, `DefaultLockExpiry`, `DefaultLockWait`, `DefaultLockRetry`, `ThrowOnCacheErrors`, `SerializerOptions`) and `CacheSerializerOptions` (`SerializerType`, `EnableCompression`, `CompressionThreshold`), each with type, default and meaning, copied from the XML documentation.
3. `docs/features/caching/memory.md` (`AddEncinaMemoryCache`; `MemoryCacheProvider`, `MemoryDistributedLockProvider`, `MemoryPubSubProvider`; single-process scope; a "Performance" line linking the benchmark page).
4. `docs/features/caching/hybrid.md` (`AddEncinaHybridCache`, `AddEncinaHybridCacheWithOptions`; `HybridCacheProvider`, L1 plus L2 behavior; a "Performance" line linking the benchmark page).
5. `docs/features/index.md`: add the caching entries to the Messaging & Caching section.
6. No changelog fragment in this pull request: the single fragment `changelog.d/84-caching-docs.added.md` (written by `mechanical-fixer`) ships with pull request 3, the last one, so it never announces pages that are not merged yet.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT:
Encina is a pre-1.0 .NET 10 library. Issue #84 asks for caching documentation. Nothing under docs/ describes handler-level caching, and the 8 caching packages have no README. You write the first pull request of three: the overview, the options reference, the memory and hybrid provider pages, and the index entry. Documentation only; write no production code.

TASK:
1. Read .claude/skills/encina-docs/SKILL.md and its diataxis.md. Write the compass verdict (quadrant, reader, what the reader can do afterwards) for each page before writing it.
2. Write docs/features/caching/overview.md, options.md, memory.md and hybrid.md with just-the-docs front matter (layout: default, parent: "Features").
3. Read every identifier from src/Encina.Caching, src/Encina.Caching.Memory and src/Encina.Caching.Hybrid before using it. Copy defaults from CachingOptions.cs and the attribute classes.
4. Add the Mermaid sequence diagram of a cached query and the provider comparison table (facts from source only).
5. Add the caching entries to docs/features/index.md. No changelog fragment in this pull request (it ships with pull request 3).
6. Run the verification of the skill (lychee offline, markdownlint, cov-docs-render --check-dangling); when a tool is missing, check every relative link with Test-Path and say so.

KEY RULES:
- English only; junior-readable; one Diataxis quadrant per section, other quadrants linked.
- No hand-typed performance or coverage figures: link docs/benchmarks/caching-*.md or use a docref marker.
- Every code sample names real API (Grep it in src/) and compiles; log or show the error code, never EncinaError.Message.
- State pre-1.0; no migration notes, no Obsolete members.
- Name the known limits (#2436, #1954, #2452, #3438) and link them; do not hide them.
- Memcached is documented only as planned (AGENTS.md section 5, #277).
- At most 15 files and 1500 changed lines in this pull request.

REFERENCE FILES:
- src/Encina.Caching/ServiceCollectionExtensions.cs, Configuration/CachingOptions.cs, Abstractions/*.cs, Attributes/, Behaviors/
- src/Encina.Caching.Memory/, src/Encina.Caching.Hybrid/
- docs/features/query-caching.md, docs/features/cdc-cache-invalidation.md, docs/benchmarks/caching-performance.md
- docs/features/index.md, docs/features/abac/ (front matter precedent)
```

</details>

### Phase 2: Redis Family Pages (pull request 2)

<details>
<summary>Tasks</summary>

1. `docs/features/caching/redis.md`: `AddEncinaRedisCache` (4 overloads: connection string, with options, `IConnectionMultiplexer`, multiplexer with options), `RedisCacheProvider`, `RedisDistributedLockProvider`, `RedisPubSubProvider`, `RedisHealthCheck`, a "Performance" line linking `docs/benchmarks/caching-redis-benchmark-results.md`; this is the page the four wrappers link for shared behavior.
2. `docs/features/caching/valkey.md`, `keydb.md`, `dragonfly.md`, `garnet.md`: short pages, each with its `AddEncina<Name>Cache` overloads (four each, delegating to `AddEncinaRedisCache`), the server's default port and one note on protocol compatibility, each external fact with a link to the server's own documentation and no version numbers on the pages (container versions follow the vendor-LTS rule, a hand-typed version goes stale), and a link to `redis.md` (the wrapper pages do not repeat the performance line).
3. Edit `docs/features/caching/overview.md`: link the five new pages from the comparison table.
4. Run the provider samples against the local containers: `docker compose --profile caching up -d`. The `caching` profile of `docker-compose.yml` starts redis, garnet, valkey, dragonfly and keydb, so every sample of this pull request runs against a real server.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT:
Pull request 2 of 3 for issue #84. Pull request 1 created docs/features/caching/overview.md and options.md. The four Redis-compatible packages (Valkey, KeyDB, Dragonfly, Garnet) only delegate to AddEncinaRedisCache, so their pages are short and link to redis.md. Documentation only.

TASK:
1. Write redis.md (full) and valkey.md, keydb.md, dragonfly.md, garnet.md (short) under docs/features/caching/, front matter parent: "Features".
2. Verify every overload signature in src/Encina.Caching.Redis/ServiceCollectionExtensions.cs and the four wrapper ServiceCollectionExtensions.cs files before writing a sample.
3. Edit overview.md to link the five pages from the comparison table.
4. Run the samples against docker compose --profile caching (it starts redis, garnet, valkey, dragonfly and keydb).
5. Run the verification of the skill and report any tool you could not run.

KEY RULES:
- Same rules as Phase 1: English, one quadrant per section, real API only, no hand-typed figures, no EncinaError.Message in samples.
- Do not copy the Redis text into the wrapper pages: link redis.md for shared behavior.
- Valkey, KeyDB, Dragonfly and Garnet performance: say the Redis benchmark pages apply because the implementation is shared; link them.
- At most 15 files and 1500 changed lines.

REFERENCE FILES:
- src/Encina.Caching.Redis/ (ServiceCollectionExtensions.cs, RedisCacheProvider.cs, RedisDistributedLockProvider.cs, RedisPubSubProvider.cs, RedisHealthCheck.cs)
- src/Encina.Caching.Valkey/, .KeyDB/, .Dragonfly/, .Garnet/ ServiceCollectionExtensions.cs
- docs/benchmarks/caching-redis-benchmark-results.md
- docker-compose.yml (caching profile)
```

</details>

### Phase 3: Package READMEs (pull request 3)

<details>
<summary>Tasks</summary>

1. Create `src/Encina.Caching/README.md`, `src/Encina.Caching.Memory/README.md`, `.Hybrid/README.md`, `.Redis/README.md`, `.Valkey/README.md`, `.KeyDB/README.md`, `.Dragonfly/README.md`, `.Garnet/README.md`: install command, the registration, one short how-to, the options and a link to the matching page under `docs/features/caching/` (reference plus one short how-to; philosophy stays on the site).
2. Check each `.csproj` for `PackageReadmeFile`: on current main neither `Directory.Build.props` nor the caching `.csproj` files set it, so a README is not packed and is invisible on NuGet. The packing change is the follow-up debt issue named in Prerequisites & Dependencies (opened with this plan), not part of this documentation PR; this pull request reports the result per package.
3. `changelog.d/84-caching-docs.added.md` (written by `mechanical-fixer`): the one fragment of the whole feature, shipped here because pull requests 1 and 2 are merged by then.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT:
Pull request 3 of 3 for issue #84. The pages under docs/features/caching/ exist. The 8 caching packages have no README, and AGENTS.md section 8 requires one per satellite package.

TASK:
1. Write the 8 package READMEs listed in the plan, each linking its page under docs/features/caching/ with an absolute documentation URL pattern used by the other package READMEs (read src/Encina.Polly/README.md for the house layout).
2. Verify every sample against the package's ServiceCollectionExtensions.cs.
3. Check whether each package's .csproj packs its README and report the result.

KEY RULES:
- English only; reference plus one short how-to; no philosophy.
- No hand-typed figures; no EncinaError.Message in samples.
- At most 15 files and 1500 changed lines.

REFERENCE FILES:
- src/Encina.Polly/README.md (layout), src/Encina.OpenTelemetry/README.md
- docs/features/caching/*.md (from pull requests 1 and 2)
- the eight src/Encina.Caching*/ServiceCollectionExtensions.cs files
```

</details>

### Phase 4: Verification and Finalization (applies to each pull request)

<details>
<summary>Tasks</summary>

1. Compile every code sample (a scratch project that references the packages, kept out of the pull request) and run the Redis-family samples against the local containers.
2. Run `lychee --offline --config .github/lychee.toml 'docs/**/*.md' 'README.md'`, `npx --yes markdownlint-cli2 "docs/**/*.md" "#docs/INVENTORY.md"` and the `cov-docs-render.cs --check-dangling` command of the encina-docs skill, section 5; report any tool that is not installed.
3. `docs-reviewer` reviews each pull request's pages; fix every blocker and major finding.
4. Check `docs/INVENTORY.md` for a caching entry and update it only if it lists feature pages.
5. Build verification is not needed (no `src/` change); the `docs.yml` workflow builds the site on the pull request.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT:
Final verification of one of the three #84 documentation pull requests. No production code changes.

TASK:
1. Build a scratch project outside the repository that compiles every C# sample of the pages in this pull request.
2. Run lychee offline, markdownlint-cli2 and cov-docs-render --check-dangling as in .claude/skills/encina-docs/SKILL.md section 5; when a tool is missing, check every relative link with Test-Path and say so.
3. Run docs-reviewer on the pages and fix blockers and majors.
4. Confirm the file and line counts are within 15 files and 1500 lines.

KEY RULES:
- Never skip a verification step silently.
- Do not edit CHANGELOG.md; the fragment lives in changelog.d/.
- No AI attribution in commits.

REFERENCE FILES:
- .claude/skills/encina-docs/SKILL.md (sections 5 and 6)
- .github/lychee.toml, .markdownlint.json
- changelog.d/README.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard / Specification | Relevance |
|--------------------------|-----------|
| `AGENTS.md` section 5, Caching row | The 8-provider category; every provider MUST support Get/Set/Remove, TTL, serialization abstraction and pub/sub where applicable; the pages state this per provider |
| `AGENTS.md` section 8 | A README for each satellite package; XML comments are the source of the option tables |
| `AGENTS.md` section 9 and SPEC-001 | No hand-typed figures; citations by marker |
| SPEC-000 REQ-027 (#277) | All 8 caching providers are in 1.0 scope; Memcached is planned |
| `.claude/skills/encina-docs/SKILL.md` | One Diataxis quadrant per page, front matter, verification commands |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `AddEncinaCaching`, `AddCacheKeyGenerator<T>`, `AddCacheConfiguration<TRequest>` | `src/Encina.Caching/ServiceCollectionExtensions.cs` | Registration of handler-level caching; described in `overview.md` |
| `CachingOptions`, `CacheSerializerOptions` | `src/Encina.Caching/Configuration/CachingOptions.cs` | Source of the 17 plus 3 option rows in `options.md` |
| `ICacheProvider`, `IPubSubProvider`, `ICacheKeyGenerator` | `src/Encina.Caching/Abstractions/` | The contract every provider page states it meets |
| `QueryCachingPipelineBehavior<,>`, `CacheInvalidationPipelineBehavior<,>`, `DistributedIdempotencyPipelineBehavior<,>`, `QueryCacheHealthCheck` | `src/Encina.Caching/Behaviors/`, `src/Encina.Caching/Health/` | Explained in the flow diagram and the overview |
| Sharding caches (`CachedShardTopologyProvider` and the cache options) | `src/Encina.Caching/Sharding/` | Short section of the overview |
| `MemoryCacheProvider`, `MemoryDistributedLockProvider`, `MemoryPubSubProvider` | `src/Encina.Caching.Memory/` | `memory.md` |
| `HybridCacheProvider` | `src/Encina.Caching.Hybrid/` | `hybrid.md` |
| `RedisCacheProvider`, `RedisDistributedLockProvider`, `RedisPubSubProvider`, `RedisHealthCheck` | `src/Encina.Caching.Redis/` | `redis.md`; shared by the four wrappers |
| Benchmark pages | `docs/benchmarks/caching-*.md` | Performance citations |
| Caching container profile | `docker-compose.yml` (`--profile caching`) | Running the Redis-family samples |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| Encina.Caching | 3500-3549 (`EventIdRanges.Caching`) | Existing; documented only, no new EventId |
| Encina.Caching.Memory | 3550-3599 (`EventIdRanges.CachingMemory`) | Existing; documented only |
| Encina.Caching.Redis | 3600-3649 (`EventIdRanges.CachingRedis`) | Existing; documented only |
| Encina.Caching.Hybrid | 3650-3699 (`EventIdRanges.CachingHybrid`) | Existing; documented only |

No new range is needed: this plan adds no logging.

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Overview and options pages | 2 | `overview.md`, `options.md` |
| Provider pages | 7 | memory, hybrid, redis, valkey, keydb, dragonfly, garnet |
| Navigation entry (`docs/features/index.md`) | 1 | Messaging & Caching section |
| Package READMEs | 8 | Core package plus the 7 providers |
| Changelog fragment | 1 | `changelog.d/84-caching-docs.added.md` |

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT:
Encina is a pre-1.0 .NET 10 library (C# 14, nullable enabled) with Railway Oriented Programming (Either<EncinaError, T>). Issue #84 asks for caching documentation. The 8 caching packages (Encina.Caching and Memory, Hybrid, Redis, Valkey, KeyDB, Dragonfly, Garnet) have no README and handler-level caching has no page. Valkey, KeyDB, Dragonfly and Garnet delegate to AddEncinaRedisCache. Documentation only; write no production code. Follow AGENTS.md and .claude/skills/encina-docs/SKILL.md.

IMPLEMENTATION OVERVIEW:
Three pull requests, each at most 15 files and 1500 lines. PR 1: docs/features/caching/overview.md, options.md, memory.md, hybrid.md, and the docs/features/index.md entry (no fragment). PR 2: redis.md, valkey.md, keydb.md, dragonfly.md, garnet.md and links from overview.md. PR 3: the 8 package READMEs and the changelog fragment changelog.d/84-caching-docs.added.md (ask mechanical-fixer). Each PR is verified with the commands of the encina-docs skill, section 5, and reviewed by docs-reviewer.

KEY PATTERNS:
- just-the-docs front matter (layout: default, parent: "Features"); lower-case kebab-case file names.
- Option tables are copied from the XML documentation of CachingOptions.cs; defaults are read, not remembered.
- Figures are cited (docs/benchmarks/caching-*.md or docref markers), never typed.
- Samples use real API and log the error code, never EncinaError.Message.
- Known limits (#2436, #1954, #2452, #3438) are stated and linked.
- Memcached is documented only as planned (#277).

REFERENCE FILES:
- src/Encina.Caching/ServiceCollectionExtensions.cs and Configuration/CachingOptions.cs
- src/Encina.Caching.Redis/, src/Encina.Caching.Memory/, src/Encina.Caching.Hybrid/
- src/Encina.Caching.Valkey/, .KeyDB/, .Dragonfly/, .Garnet/ (ServiceCollectionExtensions.cs)
- docs/features/query-caching.md, docs/features/cdc-cache-invalidation.md, docs/benchmarks/caching-performance.md
- src/Encina.Polly/README.md (README layout)
```

</details>

---

## Cross-Cutting Integration Matrix

> Per `AGENTS.md` section 6 (ADR-018): documentation only, so no runtime function is added. A function the pages describe is marked ✅ as page content.

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ | This is the subject of the pages. |
| 2 | OpenTelemetry | ❌ | No runtime code; the instrumented cache provider (`InstrumentedCacheProvider` in `Encina.OpenTelemetry`) is mentioned with a link to the observability page of #906. |
| 3 | Structured Logging | ❌ | No runtime code. |
| 4 | Health Checks | ✅ | `QueryCacheHealthCheck` and `RedisHealthCheck` are described as page content; no code change. |
| 5 | Validation | ❌ | No runtime code. |
| 6 | Resilience | ✅ | `ThrowOnCacheErrors` and the fail-safe behavior are documented in the option table as page content. |
| 7 | Distributed Locks | ✅ | The lock options of `CachingOptions` and the lock providers of Memory and Redis are documented as page content. |
| 8 | Transactions | ❌ | No runtime code. |
| 9 | Idempotency | ✅ | `DistributedIdempotencyPipelineBehavior` and its options are documented as page content. |
| 10 | Multi-Tenancy | ✅ | `VaryByTenant` (default true) is documented as page content; the idempotency bugs #2452 and #3438 (tenant-sharing and cross-user key reuse of `DistributedIdempotencyPipelineBehavior`, not the cache key) are warned about at the `EnableDistributedIdempotency` option and in the overview. |
| 11 | Module Isolation | ❌ | No runtime code; per-module cache keys are #746. |
| 12 | Audit Trail | ❌ | No runtime code. |

---

## Prerequisites & Dependencies

- No code prerequisite. The pages describe the packages as they are on origin/main; when the open bugs #2436, #1954, #2452 or #3438 are fixed, the "Known limits" lines are removed in the fixing pull request's documentation phase.
- Tools: `lychee` and `markdownlint-cli2` may be missing on the machine; the verification phase says so and checks links with `Test-Path`.
- Follow-up debt issue (opened with this plan, recorded per AGENTS.md section 11): none of the eight caching `.csproj` files nor `Directory.Build.props` sets `PackageReadmeFile`, so the READMEs written in pull request 3 are not packed and do not show on NuGet; the fix is a build change in those projects, outside this documentation plan.
- Follow-up written as an issue file: the default key prefix `"sm"` (and `sm:cache:invalidate`, `sm:idempotency`, `sm:lock`) in `CachingOptions` is an older product prefix; the pages document the current value.

---

## Next Steps

1. Maintainer answers Design Choice 2 when work on the issue starts.
2. Brief PR 1 (Phase 1) to a `docs-writer`; PR 2 and PR 3 follow on their own branches.
3. Link this plan from #84 and open the follow-up debt issue for the key prefix.
