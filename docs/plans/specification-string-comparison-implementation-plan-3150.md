# Implementation Plan: String comparison semantics of specifications across providers (case and accent sensitivity, collation)

> **Issue**: [#3150](https://github.com/dlrivada/Encina/issues/3150) (child of EPIC [#3148](https://github.com/dlrivada/Encina/issues/3148), milestone v0.14.0 — Hardening)
> **Type**: Feature
> **Complexity**: High (10 phases, 10 database providers in 8 implementations, 11 pull requests, ~78 files)
> **Estimated Scope**: ~1,800-2,400 lines of production code + ~2,500-3,200 lines of tests
> **Related**: [#3149](https://github.com/dlrivada/Encina/issues/3149) (conformance suite, must exist first), [#3158](https://github.com/dlrivada/Encina/issues/3158) (follows this issue)

---

## Summary

Make the string operations of a `Specification<T>` mean the same thing on every `IFunctionalRepository` implementation. Today a specification passes `==`, `Contains`, `StartsWith` and `EndsWith` to the engine's own collation, so the same specification returns different rows per provider: SQL Server and MySQL (`utf8mb4_0900_ai_ci`) compare case- and accent-insensitively, PostgreSQL and MongoDB compare bytes. `ToLower()` and `ToUpper()` translate only on EF Core: the six SQL builders reject them (`GetColumnName` throws `NotSupportedException` for a method call, which the repository turns into a `Left`, for example `src/Encina.ADO.PostgreSQL/Repository/SpecificationSqlBuilder.cs:716-722`) and MongoDB rejects them in `ExtractMemberAndValue` (`src/Encina.MongoDB/Repository/SpecificationFilterBuilder.cs:558-572`). Switching the DI registration therefore changes which rows match, which `AGENTS.md` section 5 (provider coherence) forbids.

The contract was decided by the maintainer on 2026-11-04 (issue #3150, Dilemma D1, option D; Choice 1 and the `Maintainer Decisions` section below):

1. `==`, `Contains`, `StartsWith` and `EndsWith` are **ordinal** on all 10 providers. Encina guarantees it by requiring a binary collation on the string columns of SQL Server and MySQL (the ADR-046 pattern: no silent default) and by checking it, and the conformance suite #3149 proves it on real databases.
2. Case-insensitivity exists only through an **explicit typed operation** (for example `EqualsIgnoreCase`) that each provider translates (`ILIKE`/`LOWER`, `COLLATE`, regex `i`) or answers with `Left`.
3. Accent-insensitivity is **out of 1.0**: it answers `Left`, documented.
4. A **new ADR** records the decision, and `AGENTS.md` section 5 gets the parity rule.

### What is already in the code

| Item | Location | State |
|---|---|---|
| LIKE translation of `Contains`/`StartsWith`/`EndsWith`, `Equals` as `=` | `TranslateStringMethod` in the six `src/Encina.{ADO,Dapper}.{SqlServer,PostgreSQL,MySQL}/Repository/SpecificationSqlBuilder.cs` (PostgreSQL ADO: `:526-541`) | Done; engine collation decides the result |
| LIKE escaping with `ESCAPE '!'` (and the 8,000-byte SQL Server pattern limit) | `BuildLikeClause`, `EscapeLikeValue` (SQL Server ADO: `:663-698`) | Done; reused unchanged |
| `==` as `=` | `TranslateBinaryExpression` (PostgreSQL ADO: `:446-488`) | Done; engine collation decides the result |
| MongoDB `Contains`/`StartsWith`/`EndsWith` as anchored `BsonRegularExpression` over `Regex.Escape` | `SpecificationFilterBuilder.cs:473-486`, `:655-658`; `==` is `Filter.Eq` (binary) | Done; case-sensitive |
| MongoDB case-insensitive regex precedent | `src/Encina.MongoDB/Auditing/ReadAuditStoreMongoDB.cs:229` (`new BsonRegularExpression(..., "i")`) | Done; the pattern to reuse |
| EF Core passes `ToExpression()` to `IQueryable.Where` | `src/Encina.EntityFrameworkCore/Repository/SpecificationEvaluator.cs` | Done; the database provider decides the result |
| Binary collation as a decided pattern | ADR-046 decisions 1-2, `src/Encina.EntityFrameworkCore/DeadLetter/DeadLetterMessageConfiguration.cs:28-35` (`Latin1_General_100_BIN2`, `utf8mb4_bin`) | Done for the dead-letter store only |
| The same builders are reused by the tenant, soft-delete and sharded repositories | `Tenancy/TenantAwareSpecificationSqlBuilder.cs`, `SoftDelete/SoftDeleteSpecificationSqlBuilder.cs`, `Sharding/FunctionalShardedRepository*.cs`, `UnitOfWork/UnitOfWorkRepository*.cs` | Done; one change reaches all of them |
| A typed case-insensitive operation, a collation check, parity rule in `AGENTS.md`, the ADR | none | **This plan** |

### Deviations from the issue text

- The issue's task "Define how a Specification states a case-insensitive comparison (`StringComparison.OrdinalIgnoreCase` and `ToLower`/`ToUpper` on the member)" is replaced by the maintainer's decision: a typed operation, not `ToLower`. The `StringComparison` overloads that the six builders drop today are #3158, which follows this issue (Choice 9).
- The cross-cutting row "Validation: Integrate" is narrowed: there is no request input here. What integrates is the `Either` validation Encina already uses for configuration (a `Left` for a column that is not binary), see the matrix.
- The issue ticks no OpenTelemetry, logging or health-check work; the matrix below records why, and defers one health check to its own issue.

**Affected packages**: `Encina.DomainModeling` (new typed operations, new error code), `Encina.ADO.SqlServer`, `Encina.ADO.PostgreSQL`, `Encina.ADO.MySQL`, `Encina.Dapper.SqlServer`, `Encina.Dapper.PostgreSQL`, `Encina.Dapper.MySQL` (builder, collation check), `Encina.EntityFrameworkCore` (evaluator, collation check, convention helper), `Encina.MongoDB` (filter builder, collation check). No new package and no new project reference.

**Provider category**: Database (10): ADO.NET x3, Dapper x3, EF Core x3 (SqlServer, PostgreSQL, MySQL) and MongoDB. The EF Core code is one package for the three databases; its MySQL integration tests are skipped today (#2086, Pomelo 9.0.0 does not support EF Core 10), so the EF MySQL result is proven by translation tests and stays open until #2086 is solved.

### Provider matrix

| Provider | Ordinal contract enforced by | Explicit ignore-case translation | Notes |
|---|---|---|---|
| ADO-SqlServer | catalog check: `sys.columns.collation_name` ends in `BIN2` (or `_BIN`) | `col COLLATE Latin1_General_100_CI_AS` (accent-sensitive, case-insensitive) | LIKE pattern limit of 8,000 bytes stays |
| ADO-PostgreSQL | none needed: deterministic collations compare by value; a nondeterministic column collation is rejected | `ILIKE ... ESCAPE '!'` (equality is an `ILIKE` with an escaped value) | `ILIKE` folds by the database `LC_CTYPE`; proven in the conformance suite |
| ADO-MySQL | catalog check: `information_schema.COLUMNS.COLLATION_NAME` ends in `_bin` | `col COLLATE utf8mb4_0900_as_ci` | requires an `utf8mb4` column; a column in another character set returns `Left` |
| Dapper-SqlServer | same as ADO-SqlServer | same as ADO-SqlServer | |
| Dapper-PostgreSQL | same as ADO-PostgreSQL | same as ADO-PostgreSQL | |
| Dapper-MySQL | same as ADO-MySQL | same as ADO-MySQL | |
| EFCore-SqlServer | model check: `IProperty.GetCollation()` is binary for each mapped string property | `EF.Functions.Collate(col, "Latin1_General_100_CI_AS")` (Choice 6) | `Database.ProviderName` selects the rewrite |
| EFCore-PostgreSQL | none needed (as ADO-PostgreSQL) | `EF.Functions.ILike(col, pattern, "!")` | Npgsql is already referenced by the package |
| EFCore-MySQL | model check (as EFCore-SqlServer) | `EF.Functions.Collate(col, "utf8mb4_0900_as_ci")` | integration tests skipped (#2086) |
| MongoDB | collection check: `listCollections` reports no collation | `BsonRegularExpression("^...$", "i")` over `Regex.Escape` | MongoDB `==` stays `Filter.Eq` (bytes) |

---

## Design Choices

<details>
<summary><strong>1. Contract — which comparison semantics every provider guarantees</strong></summary>

**Class:** Dilemma (answered by the maintainer on 2026-11-04, see `Maintainer Decisions` entry 1)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Ordinal everywhere, enforced by the engine defaults only** | Matches C# `==`; same as ADR-046 precedent | Does not change SQL Server and MySQL, so it is not enforced there |
| **B) Case-insensitive everywhere** | Matches user-facing search expectations | Diverges from C# `==`; IDs and codes compare loosely; PostgreSQL and MongoDB need extra translation on every comparison |
| **C) Keep the engine default** | No cost | Results vary per provider; violates `AGENTS.md` section 5 |
| **D) Two-level contract: ordinal by contract with binary collation required; case-insensitivity only through an explicit typed operation or `Left`; accent-insensitivity out of 1.0** | Portable and predictable; the opt-in is visible in the specification; consistent with ADR-046 | Existing SQL Server/MySQL tables with a case-insensitive collation must be migrated or the repository answers `Left` |

### Chosen Option: **D — Two-level contract with a required binary collation** (maintainer decision, 2026-11-04)

### Rationale

Decided by the maintainer in the comment of 2026-11-04 on #3150. Options A to C were the issue's original choices; the maintainer chose a fourth. It is recorded here and in `Maintainer Decisions`, and it is not re-opened by this plan.

</details>

<details>
<summary><strong>2. The typed case-insensitive operation — shape and home</strong></summary>

**Class:** Settled
**Settled by:** #3150 maintainer decision of 2026-11-04 (clause 2: "an explicit, typed operation (e.g. `EqualsIgnoreCase`)"); `AGENTS.md` section 3, provider coherence (abstractions carry no provider-specific code)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Marker extension methods on `string` in `Encina.DomainModeling` with an in-memory implementation** | Works inside any `Expression<Func<T,bool>>`; `IsSatisfiedBy` still evaluates in memory; each provider recognises the method by declaring type | The builders must recognise a static method (today they only look at `DeclaringType == typeof(string)`) |
| **B) New `Specification<T>` helper base class with builder methods** | Typed API | Cannot be combined freely with `And`/`Or`/`Not` expressions; forces a base class |
| **C) A `StringComparison` argument (`string.Equals(a, b, OrdinalIgnoreCase)`)** | Familiar C# | Not an Encina contract: the SQL builders drop it today (#3158); it also admits culture-aware values that must be refused |

### Chosen Option: **A — Marker extension methods in `Encina.DomainModeling`**

### Rationale

The abstractions live in `Encina.DomainModeling`, which has no database reference (`AGENTS.md` section 3); the marker methods are plain C# so the specification still evaluates in memory (`Specification<T>.IsSatisfiedBy` compiles the expression). Each provider translates the call, or answers `Left` through its existing `NotSupportedException` path (`FunctionalRepositoryADO.cs:186`).

</details>

<details>
<summary><strong>3. Scope of the ignore-case family</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `EqualsIgnoreCase` only** | Smallest public surface; exactly the maintainer's example | A search screen that needs "contains, ignoring case" still has no portable way; users reach for `ToLower` and get `Left` on 7 of 8 implementations |
| **B) `EqualsIgnoreCase`, `ContainsIgnoreCase`, `StartsWithIgnoreCase`, `EndsWithIgnoreCase`** | Mirrors the four ordinal operations the contract names; one shared LIKE/regex path, so each extra operation is a few lines | Four public members to document and test per provider |

### Chosen Option: **B — The four-operation family** (recommended, pending the maintainer)

### Rationale

We recommend B. The six builders already build `Contains`/`StartsWith`/`EndsWith` through one `BuildLikeClause`, and MongoDB through one regex switch; the ignore-case variants differ only in the collation or flag, so the marginal cost is small and the ordinary "search by name" case is served. A wins in a scenario where the maintainer wants the 1.0 surface minimal and accepts that case-insensitive substring search is added later.

</details>

<details>
<summary><strong>4. How the binary-collation requirement is enforced on tables the application owns</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Declare the collation in the mapping (`IEntityMapping`) and also verify it** | Explicit, like `DeadLetterMessageConfiguration`'s required argument; the mapping documents the intent | Four mapping builders per package (base, soft-delete, temporal, tenancy) and three interfaces change in each of 6 packages; the declared value can disagree with the real column, so a catalog check is needed anyway |
| **B) Verify the real column collation on first use, fail closed with `Left` (ADO/Dapper: catalog query; EF Core: model metadata; MongoDB: `listCollections`)** | No new mapping API; one internal verifier per package; the check reads the truth, not a declaration; a mismatch cannot reach a query | The failure appears at first use of the repository, not at startup |
| **C) Verify at startup in a hosted service** | Fails the application before it takes traffic | Needs the database reachable at startup; a hosted service in a library breaks pay-for-what-you-use; every repository needs registering |
| **D) Declare only; the conformance suite checks** | Least code | The application can run on a case-insensitive column with no signal: no enforcement |

### Chosen Option: **B — Verify on first use, fail closed** (recommended, pending the maintainer)

### Rationale

The decision text asks for the binary collation to be required in the schema configuration, with no silent default (the ADR-046 declaration pattern). Option A is the declaration; B satisfies the enforcement half of that text (a wrong column is refused) but not the declaration half, so choosing B is a gap against the wording that the maintainer must accept. Either way EF Core and ADO/Dapper stay consistent: Phase 5 already adds an EF helper that takes the collation as a required argument, so with B that helper remains an optional convenience for the schema and the model check enforces. We recommend B. The maintainer's text asks for a required binary collation with "no silent default", and the dead-letter tables could require a constructor argument because Encina creates them (ADR-046 decision 2). The functional repositories map tables the application owns, so Encina cannot create the collation, only refuse to run on the wrong one. B does that with the fewest changes and cannot disagree with the database. A wins when the maintainer wants Encina to ship the DDL of application tables, and C when failing at startup matters more than library simplicity; a startup health signal is deferred in the matrix (function 4). The check applies to every mapped string column, including the `TenantId` column, where a case-insensitive collation would make tenants `A` and `a` collide (see the matrix, function 10).

</details>

<details>
<summary><strong>5. Translation of the explicit ignore-case operation per provider</strong></summary>

**Class:** Settled
**Settled by:** #3150 maintainer decision of 2026-11-04 (clauses 2 and 3: `ILIKE`/`LOWER`, `COLLATE`, regex `i`; accent-insensitivity out); ADR-046 (named collations)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Per-engine native: `COLLATE ..._CI_AS` (SQL Server), `COLLATE utf8mb4_0900_as_ci` (MySQL), `ILIKE` (PostgreSQL), regex `i` (MongoDB)** | Case-insensitive but accent-sensitive, as the decision requires; one parameter | `COLLATE` on a column can defeat an index; needs an `utf8mb4` column on MySQL |
| **B) `LOWER(col) = LOWER(@p)` everywhere** | One shape on the SQL engines | Folding depends on the engine's locale; no index; MongoDB has no equivalent |
| **C) An accent-insensitive collation (`_AI_CI`)** | What most users expect from "ignore case" | Contradicts clause 3: accent-insensitivity is out of 1.0 |

### Chosen Option: **A — Per-engine native translation**

### Rationale

The decision names the mechanism per engine (`ILIKE`/`LOWER`, `COLLATE`, regex `i`) but not the collation names; the specific names `Latin1_General_100_CI_AS` (SQL Server) and `utf8mb4_0900_as_ci` (MySQL) are a Trivial choice of this plan: the case-insensitive, accent-sensitive members of the `Latin1_General_100` and `utf8mb4_0900` families, kept in one constant per package. They are not stated by the decision, and the worker confirms that both exist on the test containers before using them. Case folding is not identical on every engine: `ILIKE` folds by the database `LC_CTYPE`, the `_CI` collations by their Unicode tables and MongoDB regex `i` by PCRE rules, so non-ASCII letters can fold differently. The conformance suite (Phase 7) pins this limit with explicit non-ASCII cases and documents the parity as "ASCII identical; others per engine". `LOWER` is rejected for the same reason plus the lost index. The accent-sensitive collations keep clause 3 true. The index cost is documented in the guide (Phase 10) and measured by no benchmark, because the operation is opt-in and the ordinal path is unchanged.

</details>

<details>
<summary><strong>6. EF Core — how the typed operation is translated</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Rewrite the marker call in `SpecificationEvaluator` by `Database.ProviderName`: `EF.Functions.Collate` on SQL Server and MySQL, `EF.Functions.ILike` on PostgreSQL** | Same SQL as the ADO/Dapper builders; the PostgreSQL index story is the same | A visitor to write and test; EF must accept `Collate(...).Contains(...)` (to prove in the integration case) |
| **B) Rewrite to `col.ToLower()` compared with `value.ToLower()` for all three** | Provider-agnostic, tiny, EF translates it today | Folding by the engine locale; no index; differs from the SQL builders |
| **C) Register an EF `IMethodCallTranslatorPlugin` per database** | The idiomatic EF extension point | Three plugins, each registered through the application's `UseSqlServer`/`UseNpgsql` options: registration the library cannot guarantee |

### Chosen Option: **A — Provider-selected rewrite** (recommended, pending the maintainer)

### Rationale

We recommend A because the contract is "the same rows on every provider" and A produces the same collation as the ADO/Dapper builders. B wins if the maintainer prefers the smallest EF change and accepts a locale-dependent fold; it is also the fallback if the integration case shows EF cannot translate `Collate(...).Contains(...)`. The rewrite is a case-by-case on `Database.ProviderName`; an unknown provider returns `Left`.

</details>

<details>
<summary><strong>7. EF Core and `ToLower`/`ToUpper` inside specifications once the typed operation exists</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Leave EF permissive** | No change for existing specifications; EF's own behavior stays available | A specification using `ToLower()` works on EF Core and answers `Left` on 7 other implementations: the parity gap the EPIC exists to close |
| **B) The EF repository checks the specification expression before building the query and returns `Left(RepositoryErrors.InvalidOperation)` for `ToLower`, `ToUpper`, `ToLowerInvariant`, `ToUpperInvariant` on a string member, naming the typed operation** | Same behavior on all 10 providers; the error message teaches the replacement | A specification that worked on EF Core before now answers `Left` (pre-1.0, no compatibility rule) |
| **C) Teach the six SQL builders and MongoDB `ToLower`/`ToUpper`** | Existing EF specifications keep working everywhere | Implicit case-insensitivity through arbitrary functions, which the decision's clause 2 ("only through an explicit typed operation") excludes; folding rules differ per engine |

### Chosen Option: **B — EF rejects the implicit forms** (recommended, pending the maintainer)

### Rationale

We recommend B: it is the only option in which a specification means the same on every provider, and the pre-1.0 rule is to change completely. A wins for a team that runs only EF Core and wants zero change; the cost for that team is a few specifications to rewrite. The worker greps `src/` for specifications that use these methods before the change and migrates them in the same pull request.

</details>

<details>
<summary><strong>8. Delivery — pull requests</strong></summary>

**Class:** Settled
**Settled by:** `AGENTS.md` section 11 (a worker's issue is one pull request: one package or provider, at most 15 files and 1500 changed lines)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) 11 pull requests, one per package, ordered by dependency (Next Steps)** | Each within the size limit; each reviewable | Coordination of the order |
| **B) One pull request for the whole feature** | One review | About 75 files; exceeds the limit and the `pr-size` gate |

### Chosen Option: **A — One pull request per package**

### Rationale

The size rule is binding; the order in Next Steps keeps `main` green after each merge because the marker operations and error code land first and each provider adopts them independently.

</details>

<details>
<summary><strong>9. Culture-aware and accent-insensitive requests</strong></summary>

**Class:** Settled
**Settled by:** #3150 maintainer decision of 2026-11-04 (clause 3: accent-insensitivity out of 1.0, `Left`, documented); #3158 (the `StringComparison` overloads)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Return `Left` (`RepositoryErrors.InvalidOperation`) with a message naming the supported typed operation; this issue documents it, #3158 implements the `StringComparison` detection** | One boundary between two issues; no half-translation | Users needing accent-insensitive search have no portable way in 1.0 |
| **B) Translate `CompareOptions.IgnoreNonSpace` through `_AI` collations now** | Serves accent-insensitive search | Contradicts the decision; PostgreSQL and MongoDB have no portable equivalent |

### Chosen Option: **A — `Left`, documented, detection in #3158**

### Rationale

The decision is explicit and #3158 is the issue that stops the silent dropping of `StringComparison` arguments; this plan fixes the contract it implements against.

</details>

---

## Implementation Phases

### Phase 1: Core contract — typed operations, error code, ADR

<details>
<summary>Tasks</summary>

1. `src/Encina.DomainModeling/StringSpecificationExtensions.cs` (new): `public static class StringSpecificationExtensions` in namespace `Encina.DomainModeling` with `EqualsIgnoreCase(this string? value, string? other)` and, per Choice 3, `ContainsIgnoreCase`, `StartsWithIgnoreCase`, `EndsWithIgnoreCase`. Each is implemented in memory with `StringComparison.OrdinalIgnoreCase` so `IsSatisfiedBy` works. XML documentation with an example; the remarks state the translation per provider and the index cost.
2. `src/Encina.DomainModeling/RepositoryErrors.cs`: a new code `Repository.CollationNotOrdinal` (private const, public factory `CollationNotOrdinal<TEntity>(string columnName, string? actualCollation)`), built like the neighboring factories; the message names the column and the required family, never a value from a row.
3. `src/Encina.DomainModeling/PublicAPI.Unshipped.txt`: the new public members (`mechanical-fixer`).
4. `docs/architecture/adr/<next free number>-specification-string-comparison.md` (new; ADR number assigned when the ADR pull request opens) and its row in `docs/architecture/adr/index.md`; `AGENTS.md` section 5 gains the parity rule: string comparison in a specification is ordinal on all 10 providers, case-insensitivity only through the typed operations (`docs-writer`).
5. `.github/coverage-manifest/Encina.DomainModeling.json`: entries with per-flag targets and one-sentence justifications for the two new/changed files (`mechanical-fixer`).
6. Tests: unit and guard tests for the four methods (null, empty, culture independence: `"I"` and `"i"` under `tr-TR`), a contract test that the in-memory result equals the expression result.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: Encina is pre-1.0 (.NET 10, C# 14). Issue #3150 decides: string ==, Contains, StartsWith, EndsWith in a Specification are ordinal on all providers; case-insensitivity only through explicit typed operations. Plan: docs/plans/specification-string-comparison-implementation-plan-3150.md.
TASK: Add StringSpecificationExtensions to src/Encina.DomainModeling (EqualsIgnoreCase and the family chosen in Design Choice 3), add RepositoryErrors.CollationNotOrdinal, add the ADR and the AGENTS.md section 5 rule, update PublicAPI.Unshipped.txt, the coverage manifest and tests.
KEY RULES: no [Obsolete] or compatibility layer; ROP (the error is an EncinaError, never an exception); the in-memory implementation uses StringComparison.OrdinalIgnoreCase; XML docs on every public member; tests instantiate the real methods (reflection-only tests cover nothing); culture test under tr-TR; zero warnings; no emojis.
REFERENCE FILES: src/Encina.DomainModeling/Specification.cs, src/Encina.DomainModeling/RepositoryErrors.cs, docs/architecture/adr/046-persistent-dead-letter-queue.md, docs/architecture/adr/index.md, AGENTS.md section 5.
```

</details>

### Phase 2: SQL Server — ADO.NET and Dapper (2 pull requests)

<details>
<summary>Tasks</summary>

1. `src/Encina.{ADO,Dapper}.SqlServer/Repository/SpecificationSqlBuilder.cs`: in `TranslateMethodCallExpression`, recognise `DeclaringType == typeof(StringSpecificationExtensions)` before the `typeof(string)` branch (the extension call has no `Object`; the member is `Arguments[0]`). Equality: `[col] COLLATE Latin1_General_100_CI_AS = @p`; the family: `[col] COLLATE Latin1_General_100_CI_AS LIKE @p ESCAPE '!'` through the existing `BuildLikeClause`/`EscapeLikeValue` (the 8,000-byte limit still applies). The single-question `switch` keeps its `crap-exempt` marker; the new branch is a separate small method.
2. `.../Repository/StringColumnCollationVerifier.cs` (new, `internal sealed`, per package): given the `IDbConnection`, `IEntityMapping.TableName` and the string-typed entries of `ColumnMappings`, reads `sys.columns.collation_name` through `OpenAsync`/`ExecuteReaderAsync` with a `CancellationToken`, caches the verdict per mapping, and returns `Either<EncinaError, Unit>` (`Left` = `RepositoryErrors.CollationNotOrdinal`). Binary means the name ends in `_BIN2` or `_BIN`.
3. `FunctionalRepositoryADO.cs` / `FunctionalRepositoryDapper.cs`, `Sharding/FunctionalShardedRepository*.cs` (the check runs per shard connection on first use) and `UnitOfWork/UnitOfWorkRepository*.cs`: call the verifier before the first string-bearing operation.
4. Tests: unit (builder output for each operation, the `Left` paths), guard (new public/internal entry points), the integration cases the conformance suite #3149 gains (SQL Server collection `ADO-SqlServer` / `Dapper-SqlServer`, `ClearAllDataAsync` in `InitializeAsync`): ordinal `==`/`Contains` on `'Abc'` vs `'abc'`, the typed operation, a column created with `SQL_Latin1_General_CP1_CI_AS` answers `Left`.
5. `.github/coverage-manifest/Encina.{ADO,Dapper}.SqlServer.json` entries (`mechanical-fixer`).

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: Encina issue #3150, plan docs/plans/specification-string-comparison-implementation-plan-3150.md. Phase 1 (typed operations, error code) is merged. SQL Server must treat ==, Contains, StartsWith, EndsWith as ordinal (binary collation required) and translate the typed ignore-case operations with COLLATE Latin1_General_100_CI_AS.
TASK: In ONE package (Encina.ADO.SqlServer, then Encina.Dapper.SqlServer in its own pull request) teach SpecificationSqlBuilder the typed operations, add the internal collation verifier, call it from the repository, sharded repository and unit-of-work repository, add tests and the manifest entries.
KEY RULES: every database call is async with a CancellationToken (OpenAsync, ExecuteReaderAsync), never the synchronous overloads; a verifier failure is a Left (ROP), never swallowed and never an exception; the error message never carries row data; do not touch TenantAware or SoftDelete builders except through tests (they delegate); keep CRAP <= 10 on changed methods (measure it); integration tests use the shared [Collection("ADO-SqlServer")] or [Collection("Dapper-SqlServer")] fixture; at most 15 files in the pull request.
REFERENCE FILES: src/Encina.ADO.SqlServer/Repository/SpecificationSqlBuilder.cs, src/Encina.ADO.SqlServer/Repository/FunctionalRepositoryADO.cs, src/Encina.ADO.SqlServer/Sharding/FunctionalShardedRepositoryADO.cs, src/Encina.EntityFrameworkCore/DeadLetter/DeadLetterMessageConfiguration.cs, tests/Encina.UnitTests/ADO/SqlServer/Repository/QuerySpecificationSqlBuilderTests.cs.
```

</details>

### Phase 3: PostgreSQL — ADO.NET and Dapper (2 pull requests)

<details>
<summary>Tasks</summary>

1. `src/Encina.{ADO,Dapper}.PostgreSQL/Repository/SpecificationSqlBuilder.cs`: recognise the marker class; every operation is `"col" ILIKE @p ESCAPE '!'` (equality uses the escaped value without wildcards), reusing `BuildLikeClause`/`EscapeLikeValue`.
2. `StringColumnCollationVerifier.cs` (new, internal): PostgreSQL needs no binary collation (deterministic collations compare by value); the verifier rejects a column whose collation is nondeterministic (`pg_collation.collisdeterministic = false`) and caches the verdict.
3. Repository, sharded repository and unit-of-work call sites as in Phase 2.
4. Tests: unit, guard, and the conformance cases in `ADO-PostgreSQL` / `Dapper-PostgreSQL` (ordinal `==`; `ILIKE` with `%` and `_` in the value; a nondeterministic-collation column answers `Left`).
5. Manifest entries (`mechanical-fixer`).

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: Encina issue #3150, plan docs/plans/specification-string-comparison-implementation-plan-3150.md. PostgreSQL is already case-sensitive by default; the work is the typed ignore-case operations (ILIKE) and the guard against a nondeterministic column collation.
TASK: In ONE package (Encina.ADO.PostgreSQL, then Encina.Dapper.PostgreSQL) add the marker-method translation, the internal verifier, the call sites, tests and manifest entries.
KEY RULES: async database calls with CancellationToken; Left on failure (ROP); ILIKE values go through EscapeLikeValue with ESCAPE '!'; identifiers stay double-quoted; PostgreSQL identifiers are case-sensitive; integration tests use [Collection("ADO-PostgreSQL")] / [Collection("Dapper-PostgreSQL")]; CRAP <= 10; at most 15 files.
REFERENCE FILES: src/Encina.ADO.PostgreSQL/Repository/SpecificationSqlBuilder.cs, src/Encina.Dapper.PostgreSQL/Repository/SpecificationSqlBuilder.cs, src/Encina.ADO.PostgreSQL/Repository/FunctionalRepositoryADO.cs.
```

</details>

### Phase 4: MySQL — ADO.NET and Dapper (2 pull requests)

<details>
<summary>Tasks</summary>

1. `src/Encina.{ADO,Dapper}.MySQL/Repository/SpecificationSqlBuilder.cs`: recognise the marker class; `` `col` COLLATE utf8mb4_0900_as_ci = @p `` for equality and `` `col` COLLATE utf8mb4_0900_as_ci LIKE @p ESCAPE '!' `` for the family.
2. `StringColumnCollationVerifier.cs` (new, internal): reads `information_schema.COLUMNS` (`COLLATION_NAME`, `CHARACTER_SET_NAME`); binary means the collation ends in `_bin`; a typed ignore-case call on a column whose character set is not `utf8mb4` returns `Left` (the `COLLATE` would be invalid for it).
3. Call sites as in Phase 2.
4. Tests: unit, guard, conformance cases in `ADO-MySQL` / `Dapper-MySQL` (a `utf8mb4_0900_ai_ci` column answers `Left`; trailing-space behavior is documented in the ADR because MySQL `PAD SPACE` collations ignore it, ADR-046 decision 6 already rejects trailing white space for the dead-letter keys).
5. Manifest entries (`mechanical-fixer`).

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT: Encina issue #3150, plan docs/plans/specification-string-comparison-implementation-plan-3150.md. MySQL defaults to utf8mb4_0900_ai_ci (case- and accent-insensitive); the contract needs utf8mb4_bin on string columns and utf8mb4_0900_as_ci for the explicit ignore-case operations.
TASK: In ONE package (Encina.ADO.MySQL, then Encina.Dapper.MySQL) add the marker-method translation, the internal verifier, the call sites, tests and manifest entries.
KEY RULES: async database calls with CancellationToken; Left on failure; backtick identifiers; values through EscapeLikeValue with ESCAPE '!'; integration tests use [Collection("ADO-MySQL")] / [Collection("Dapper-MySQL")]; CRAP <= 10; at most 15 files.
REFERENCE FILES: src/Encina.ADO.MySQL/Repository/SpecificationSqlBuilder.cs, src/Encina.Dapper.MySQL/Repository/SpecificationSqlBuilder.cs, src/Encina.ADO.MySQL/DeadLetter/DeadLetterStoreADO.cs, docs/architecture/adr/046-persistent-dead-letter-queue.md.
```

</details>

### Phase 5: EF Core — SqlServer, PostgreSQL, MySQL (1 pull request)

<details>
<summary>Tasks</summary>

1. `src/Encina.EntityFrameworkCore/Repository/SpecificationEvaluator.cs`: before `Where`, run an `ExpressionVisitor` that (Choice 6) rewrites the marker calls by `Database.ProviderName` and (Choice 7) finds `ToLower`/`ToUpper`/`ToLowerInvariant`/`ToUpperInvariant` on a string member. The rejection is not an exception: `FunctionalRepositoryEF` runs the visitor's check on the specification expression before it builds the query and returns `Left(RepositoryErrors.InvalidOperation(...))` directly with a message naming the typed operation (its generic `catch (Exception)` would turn an exception into `Repository.PersistenceError` and lose the code of Choice 9). `SpecificationEvaluator.GetQuery` is public and static, so the provider name travels as a new parameter from `FunctionalRepositoryEF` and the changed signature goes into `PublicAPI.Unshipped.txt` (counted in the Phase 5 files).
2. `src/Encina.EntityFrameworkCore/Repository/OrdinalStringCollation.cs` (new): a public `UseOrdinalStringCollation(this ModelConfigurationBuilder, string? collation)` convention helper (`Properties<string>().UseCollation(...)`) with the `SqlServerBinaryCollation` and `MySqlBinaryCollation` constants of `DeadLetterMessageConfiguration` reused (the helper takes them as the argument; no default), and an internal model check that reads `IProperty.GetCollation()` of each mapped string property for the entity type and returns `Left` (`Repository.CollationNotOrdinal`) on SQL Server and MySQL when it is not binary. PostgreSQL needs none.
3. `FunctionalRepositoryEF.cs`: call the model check once per entity type.
4. `PublicAPI.Unshipped.txt` (`mechanical-fixer`), manifest entries (`mechanical-fixer`).
5. Tests: unit (visitor output per provider name, the rejected forms), guard, contract (the same specifications give the same rows as the other implementations), integration cases for EFCore-SqlServer and EFCore-PostgreSQL; EFCore-MySQL translation tests only until #2086.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT: Encina issue #3150, plan docs/plans/specification-string-comparison-implementation-plan-3150.md. The EF Core package serves SqlServer, PostgreSQL and MySQL; Microsoft.EntityFrameworkCore.Relational (EF.Functions.Collate) and Npgsql.EntityFrameworkCore.PostgreSQL (EF.Functions.ILike) are already referenced.
TASK: Add the expression visitor to SpecificationEvaluator, the OrdinalStringCollation helper and model check, wire the check into FunctionalRepositoryEF, add tests, PublicAPI lines and manifest entries. The maintainer's answers to Design Choices 6 and 7 are in the plan's Maintainer Decisions section; follow them, not the recommendation text.
KEY RULES: no [Obsolete]; Left, never a swallowed exception; an unknown Database.ProviderName returns Left; prove that EF translates Collate(...).Contains(...) in an integration case before relying on it; EFCore-MySQL integration tests stay skipped (#2086), do not unskip; CRAP <= 10; at most 15 files.
REFERENCE FILES: src/Encina.EntityFrameworkCore/Repository/SpecificationEvaluator.cs, src/Encina.EntityFrameworkCore/Repository/FunctionalRepositoryEF.cs, src/Encina.EntityFrameworkCore/DeadLetter/DeadLetterMessageConfiguration.cs, src/Encina.EntityFrameworkCore/Extensions/KeysetComparison.cs.
```

</details>

### Phase 6: MongoDB (1 pull request)

<details>
<summary>Tasks</summary>

1. `src/Encina.MongoDB/Repository/SpecificationFilterBuilder.cs`: in `TranslateMethodCallExpression` handle the marker class: `Filter.Regex(field, new BsonRegularExpression("^" + EscapeRegex(value) + "$", "i"))` for equality, and the anchored forms for the family (reuse `EscapeRegex`, as `ReadAuditStoreMongoDB.cs:229` does for `Purpose`). `ToLower`/`ToUpper` keep returning `Left` through the existing `NotSupportedException`.
2. `src/Encina.MongoDB/Repository/CollectionCollationVerifier.cs` (new, internal): `listCollections` for the entity's collection; a collation other than simple returns `Left` (`Repository.CollationNotOrdinal`); verdict cached.
3. `FunctionalRepositoryMongoDB.cs`: call the verifier on first use.
4. Tests: unit (filter documents), guard, conformance cases in the MongoDB collection fixture (including a collection created with a case-insensitive collation answering `Left`), manifest entries (`mechanical-fixer`).

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT: Encina issue #3150, plan docs/plans/specification-string-comparison-implementation-plan-3150.md. MongoDB compares bytes by default; the typed ignore-case operation maps to an anchored regex with the "i" option.
TASK: Extend SpecificationFilterBuilder for the typed operations, add the internal collection collation verifier, call it from FunctionalRepositoryMongoDB, add tests and manifest entries.
KEY RULES: all values through Regex.Escape (EscapeRegex); the driver calls are async with a CancellationToken; Left on failure; do not change the ordinal Eq/Regex translations; the tenant-aware and soft-delete Mongo builders delegate and need tests only; CRAP <= 10; at most 15 files.
REFERENCE FILES: src/Encina.MongoDB/Repository/SpecificationFilterBuilder.cs, src/Encina.MongoDB/Repository/FunctionalRepositoryMongoDB.cs, src/Encina.MongoDB/Auditing/ReadAuditStoreMongoDB.cs, src/Encina.MongoDB/DeadLetter/DeadLetterStoreMongoDB.cs.
```

</details>

### Phase 7: Cross-cutting integration — tenant, soft delete, sharding, conformance suite

<details>
<summary>Tasks</summary>

1. Prove that `TenantAwareSpecificationSqlBuilder`, `SoftDeleteSpecificationSqlBuilder` (6 packages) and the Mongo tenant-aware and soft-delete builders produce the same string translation as the base builders (tests only; they delegate).
2. Tenant column: add a conformance case where tenants `A` and `a` exist and a tenant-aware query for `A` returns only its rows, on every provider (a case-insensitive `TenantId` column would leak data across tenants; the verifier covers every mapped string column).
3. Sharded repositories: a test that each shard connection is verified (Phases 2-4).
4. Add the string-comparison cases to the conformance suite #3149 (`Encina.IntegrationTests`, shared `[Collection]` fixtures): ordinal `==`/`Contains`/`StartsWith`/`EndsWith`, the typed family, `%`/`_` escaping, `Left` for a non-binary column and for a culture-aware `StringComparison` (the last asserted as `Left` once #3158 lands).

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```text
CONTEXT: Encina issue #3150, plan docs/plans/specification-string-comparison-implementation-plan-3150.md. Phases 1-6 are merged. The conformance suite of #3149 is the single place that proves the 8 implementations return the same rows.
TASK: Add the string-comparison cases to the conformance suite, the tenant 'A' versus 'a' case, and the delegation tests for the tenant-aware, soft-delete and sharded builders.
KEY RULES: integration tests only through shared [Collection("<Family>-<Database>")] fixtures with ClearAllDataAsync in InitializeAsync, never IClassFixture or a per-class container; every case runs on all 10 providers (EFCore-MySQL stays skipped per #2086); tests execute real implementations; a security-relevant case (tenants) must fail if the verifier is removed.
REFERENCE FILES: the conformance suite delivered by #3149, src/Encina.ADO.SqlServer/Tenancy/TenantAwareSpecificationSqlBuilder.cs, src/Encina.ADO.SqlServer/SoftDelete/SoftDeleteSpecificationSqlBuilder.cs, docs/testing/integration-tests.md.
```

</details>

### Phase 8: Observability

<details>
<summary>Tasks</summary>

1. No new `ActivitySource`, `Meter` or `[LoggerMessage]`: the change is a translation rule and a configuration check; its failure is a `Left` with the error code `Repository.CollationNotOrdinal`, surfaced by the existing repository spans (`SpecificationActivitySource`) as an error status by code. No EventId range is needed (`DomainModelingSpecification` 1400-1499 and `DomainModelingRepository` 1100-1199 are registered already and unchanged).
2. Verify that no log, tag or health result carries `EncinaError.Message` (`AGENTS.md` section 3): the error factory puts the column name and collation name in the message, and the repositories already log only codes.

</details>

<details>
<summary>Prompt for AI Agents — Phase 8</summary>

```text
CONTEXT: Encina issue #3150, plan docs/plans/specification-string-comparison-implementation-plan-3150.md. This feature adds no telemetry.
TASK: Confirm in each changed repository that the Left produced by the collation verifier is reported through the existing span/error handling by error code only, and add one unit test per package that the code Repository.CollationNotOrdinal reaches the caller unchanged.
KEY RULES: EncinaError.Message never reaches logs, tags or health results; do not add an EventId or register a range; do not add a Meter or ActivitySource.
REFERENCE FILES: src/Encina.DomainModeling/Diagnostics/SpecificationActivitySource.cs, src/Encina.DomainModeling/Diagnostics/SpecificationLog.cs, src/Encina/Diagnostics/EventIdRanges.cs.
```

</details>

### Phase 9: Testing

<details>
<summary>Tasks</summary>

1. Unit tests (per builder, evaluator, verifier): output of each operation; the `Left` paths; the culture test under `tr-TR`.
2. Guard tests for every new public and changed method with parameters (`ArgumentNullException`).
3. Contract test: the 8 implementations (and the in-memory evaluation) return the same rows for one set of specifications; instantiates the real builders.
4. Property test (FsCheck, the logic is a string-matching rule): for random strings, the ordinal operations never match strings that differ only in case.
5. Integration tests: Phase 7 (conformance suite), no `.md` justification for database features.
6. Load and benchmark: justification files `tests/Encina.LoadTests/Repository/SpecificationStringComparison.md` and `tests/Encina.BenchmarkTests/Repository/SpecificationStringComparison.md` (not concurrent code; the opt-in translation is a string build per query, not a hot path).
7. Per-flag coverage: each changed `src/` file has unit, guard, contract, property and integration targets in its manifest with justifications; measure with `coverage-report.cs` and report the measured values against the targets; CRAP <= 10 on every changed method. The targets (written by `mechanical-fixer`; the same shape per package, and a manifest entry that does not exist yet is created with these values; none goes down):

| File (per package) | unit | guard | contract | property | integration | Justification |
|---|:-:|:-:|:-:|:-:|:-:|---|
| `Encina.DomainModeling/StringSpecificationExtensions.cs` (new) | 95 | 90 | 80 | 80 | 0 | Marker methods throw in memory by design; every operation is called, and integration is covered in the provider packages |
| `Encina.DomainModeling/RepositoryErrors.cs` | existing | existing | 80 | 0 | 0 | One new factory built and checked like its neighbours |
| `StringColumnCollationVerifier.cs` (new, each ADO/Dapper package) | 90 | 80 | 0 | 0 | 80 | Each catalog outcome (binary, not binary, missing column) is a unit case on a faked connection and a real-database case in the conformance suite |
| `SpecificationSqlBuilder.cs` (each ADO/Dapper package) | existing, not lower | existing | 70 | 70 | 70 | The four operations add one translation each, covered by the shared conformance cases |
| `Encina.EntityFrameworkCore/Repository/SpecificationEvaluator.cs` | 90 | 80 | 70 | 0 | 70 | The visitor is tested per provider name; MySQL by translation only (#2086) |
| `Encina.EntityFrameworkCore/Repository/OrdinalStringCollation.cs` (new) | 90 | 90 | 0 | 0 | 70 | A helper and a model check with one `Left` per failing property |
| `Encina.MongoDB/Repository/CollectionCollationVerifier.cs` (new) | 90 | 80 | 0 | 0 | 80 | `listCollections` outcomes are unit cases and one container case |
| `Encina.MongoDB/Repository/SpecificationFilterBuilder.cs` | existing, not lower | existing | 70 | 70 | 70 | One regex branch per operation, covered by the conformance cases |

</details>

<details>
<summary>Prompt for AI Agents — Phase 9</summary>

```text
CONTEXT: Encina issue #3150, plan docs/plans/specification-string-comparison-implementation-plan-3150.md. Test projects are consolidated under tests/ (Encina.UnitTests, GuardTests, ContractTests, PropertyTests, IntegrationTests).
TASK: Add the tests listed in Phase 9 of the plan next to the existing builder tests, measure per-flag coverage of every changed file against .github/coverage-manifest/<Package>.json and report it.
KEY RULES: Shouldly through Encina.Testing.Shouldly, never FluentAssertions; tests execute real package code; no Thread.Sleep; descriptive names, Arrange-Act-Assert; integration tests only with shared Collection fixtures; a justification .md is allowed only for load and benchmark.
REFERENCE FILES: tests/Encina.UnitTests/ADO/PostgreSQL/Repository/SpecificationSqlBuilderTests.cs, tests/Encina.GuardTests/ADO/PostgreSQL/Repository/SpecificationSqlBuilderGuardTests.cs, tests/Encina.UnitTests/MongoDB/Repository/SpecificationFilterBuilderTests.cs, docs/en/guides/TESTING.md.
```

</details>

### Phase 10: Documentation & Finalization

<details>
<summary>Tasks</summary>

1. XML documentation on every new public member (`<summary>`, `<remarks>`, `<param>`, `<returns>`, `<example>`).
2. `changelog.d/3150-specification-string-comparison.changed.md` (`mechanical-fixer`): string operations of specifications are ordinal on all providers; new `EqualsIgnoreCase` family; repositories answer `Left` on a non-binary column.
3. `docs/features/specification-string-comparison.md` (`docs-writer`): the contract in plain terms, the collation to use per engine (`Latin1_General_100_BIN2`, `utf8mb4_bin`, EF `UseOrdinalStringCollation`), the typed operations and their index cost, what returns `Left` (culture-aware and accent-insensitive requests).
4. The ADR (Phase 1) and the `AGENTS.md` section 5 rule; `docs/INVENTORY.md` only if it lists the new files; `ROADMAP.md` not affected.
5. `PublicAPI.Unshipped.txt` complete for DomainModeling and EF Core.
6. Build and test: `dotnet build --configuration Release` with 0 errors and 0 warnings; `dotnet test` passes; every coverage flag reaches its manifest target.

</details>

<details>
<summary>Prompt for AI Agents — Phase 10</summary>

```text
CONTEXT: Encina issue #3150, plan docs/plans/specification-string-comparison-implementation-plan-3150.md. Phases 1-9 are merged.
TASK: Finish the XML documentation, the changelog fragment, the feature page and the final verification.
KEY RULES: documentation pages go to docs-writer, the changelog fragment and PublicAPI lines to mechanical-fixer; English only; no hand-typed coverage figures (cite covref markers); no emojis; link the ADR and the issue.
REFERENCE FILES: .claude/skills/encina-docs/SKILL.md, changelog.d/README.md, docs/features/, docs/architecture/adr/046-persistent-dead-letter-queue.md.
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard / reference | Where it applies | How this feature uses it |
|---|---|---|
| `AGENTS.md` section 5 (provider coherence, 10 database providers) | All implementations | Switching the DI registration must not change which rows match |
| ADR-046 (persistent dead-letter queue), decisions 1-2 | SQL Server, MySQL | Binary collations `Latin1_General_100_BIN2` and `utf8mb4_bin`; no silent default |
| ADR-001, ADR-006 (Railway Oriented Programming) | Repository errors | A non-binary column and an unsupported form are `Left`, never an exception for business logic |
| #3150 maintainer decision, 2026-11-04 (D1 = D) | The whole feature | Ordinal contract, typed ignore-case operation, accent-insensitivity out of 1.0 |
| SQL `LIKE ... ESCAPE`, `COLLATE` (SQL Server, MySQL), `ILIKE` (PostgreSQL), MongoDB `$regex` with option `i` | Builders | The translation of the typed operations |
| EF Core `EF.Functions.Collate`, `EF.Functions.ILike`, `UseCollation`, `IProperty.GetCollation()` | EF Core package | Translation and model check |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|---|---|---|
| `SpecificationSqlBuilder<TEntity>` (6) | `src/Encina.{ADO,Dapper}.{SqlServer,PostgreSQL,MySQL}/Repository/SpecificationSqlBuilder.cs` | Extended: marker-method branch; `BuildLikeClause`/`EscapeLikeValue` reused |
| Tenant-aware and soft-delete builders | `src/Encina.{ADO,Dapper}.*/Tenancy`, `/SoftDelete` | Delegate to the base builder; tests only |
| `FunctionalRepositoryADO`/`FunctionalRepositoryDapper`, sharded and unit-of-work repositories | `.../Repository`, `.../Sharding`, `.../UnitOfWork` | Call the collation verifier; `catch (NotSupportedException)` already yields `Left` |
| `SpecificationFilterBuilder<TEntity>` | `src/Encina.MongoDB/Repository/SpecificationFilterBuilder.cs` | Extended with the regex `i` form |
| `ReadAuditStoreMongoDB` regex `i` | `src/Encina.MongoDB/Auditing/ReadAuditStoreMongoDB.cs:229` | Precedent for the Mongo form |
| `SpecificationEvaluator` | `src/Encina.EntityFrameworkCore/Repository/SpecificationEvaluator.cs` | Gains the visitor |
| `DeadLetterMessageConfiguration` | `src/Encina.EntityFrameworkCore/DeadLetter/DeadLetterMessageConfiguration.cs` | Collation constants and the `UseCollation` precedent |
| `Specification<T>`, `QuerySpecification<T>` | `src/Encina.DomainModeling` | Host of the marker operations' consumers; in-memory `IsSatisfiedBy` |
| `RepositoryErrors` | `src/Encina.DomainModeling/RepositoryErrors.cs` | New code `Repository.CollationNotOrdinal` |
| `IEntityMapping`, `EntityMappingBuilder` | `src/Encina.{ADO,Dapper}.*/Repository` | Provide `TableName` and `ColumnMappings` to the verifier (unchanged, Choice 4 B) |

### Event ID Allocation

| Package | Range | Notes |
|---|---|---|
| `Encina.DomainModeling` (existing) | `DomainModelingRepository` 1100-1199, `DomainModelingSpecification` 1400-1499 | Registered in `src/Encina/Diagnostics/EventIdRanges.cs`; this feature adds no `[LoggerMessage]` and needs no new range |

### Estimated File Count

| Category | Files | Notes |
|---|---:|---|
| `Encina.DomainModeling` production | 2 | 1 new (`StringSpecificationExtensions.cs`), 1 modified (`RepositoryErrors.cs`) |
| ADO/Dapper production (6 packages) | 30 | per package: 1 new verifier + builder + repository + sharded repository + unit-of-work repository |
| `Encina.EntityFrameworkCore` production | 3 | 1 new (`OrdinalStringCollation.cs`), `SpecificationEvaluator.cs`, `FunctionalRepositoryEF.cs` |
| `Encina.MongoDB` production | 3 | 1 new verifier, builder, repository |
| `PublicAPI.Unshipped.txt` | 2 | DomainModeling, EF Core |
| Tests | ~24 | unit and guard per package (about 16), contract 1, property 1, conformance cases (about 4), 2 justification `.md` |
| Docs and records | 5 | ADR, ADR index, `AGENTS.md`, feature page, changelog fragment |
| Coverage manifests | 9 | 6 ADO/Dapper, EF Core, MongoDB, DomainModeling |
| **Total** | **~78** | Spread over 11 pull requests (Next Steps) |

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt — all phases</summary>

```text
PROJECT CONTEXT: Encina (.NET 10, C# 14, nullable enabled, pre-1.0: no backward compatibility, no [Obsolete]). Issue #3150: string comparison in a Specification must mean the same on all 10 database providers (ADO x3, Dapper x3, EF Core x3, MongoDB). Maintainer decision (2026-11-04): ==, Contains, StartsWith, EndsWith are ordinal; binary collation is required on SQL Server and MySQL string columns; case-insensitivity only through explicit typed operations (EqualsIgnoreCase and the family chosen in Design Choice 3) that each provider translates or answers with Left; accent-insensitivity is out of 1.0 (Left). Plan: docs/plans/specification-string-comparison-implementation-plan-3150.md. The answers to the open Design Choices are in its Maintainer Decisions section.

IMPLEMENTATION OVERVIEW: Phase 1 core (StringSpecificationExtensions, RepositoryErrors.CollationNotOrdinal, ADR, AGENTS.md section 5). Phases 2-4 one package per pull request for SQL Server, PostgreSQL, MySQL (ADO then Dapper): SpecificationSqlBuilder marker branch, internal StringColumnCollationVerifier, call sites in the repository, sharded and unit-of-work repositories. Phase 5 EF Core (visitor, OrdinalStringCollation helper, model check). Phase 6 MongoDB (regex i, collection collation check). Phase 7 conformance cases, tenant A/a case. Phases 8-10 observability confirmation (none new), tests, documentation.

KEY PATTERNS: ROP (Either<EncinaError, T>, Left on every failure, nothing swallowed); async database calls with CancellationToken; LIKE values escaped with EscapeLikeValue and ESCAPE '!'; Regex.Escape on MongoDB; the verifier caches its verdict per mapping and is internal; no logging of EncinaError.Message; integration tests through shared [Collection("<Family>-<Database>")] fixtures; per-flag coverage targets in .github/coverage-manifest/<Package>.json for every changed src file; CRAP <= 10 on changed methods; at most 15 files and 1500 lines per pull request.

REFERENCE FILES: src/Encina.ADO.PostgreSQL/Repository/SpecificationSqlBuilder.cs, src/Encina.ADO.SqlServer/Repository/SpecificationSqlBuilder.cs, src/Encina.MongoDB/Repository/SpecificationFilterBuilder.cs, src/Encina.EntityFrameworkCore/Repository/SpecificationEvaluator.cs, src/Encina.EntityFrameworkCore/DeadLetter/DeadLetterMessageConfiguration.cs, src/Encina.DomainModeling/RepositoryErrors.cs, docs/architecture/adr/046-persistent-dead-letter-queue.md, AGENTS.md sections 3, 5, 6, 9.
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | The change is a translation rule; the collation verdict is cached per mapping inside the verifier, not through `ICacheProvider` |
| 2 | OpenTelemetry | ❌ | No new operation to trace; a collation `Left` is reported by the existing repository spans by error code (Phase 8) |
| 3 | Structured Logging | ❌ | No new `[LoggerMessage]`; no EventId range is needed (Research, Event ID Allocation) |
| 4 | Health Checks | ⏭️ | A readiness check that reports a non-binary column before the first request is useful but is a separate feature; the follow-up issue file is to be drafted by the orchestrator when this plan is accepted |
| 5 | Validation | ✅ | Narrowed to configuration validation in the `Either` style: the verifier returns `Left` for a column that is not binary (Phases 2-6). There is no request input, so no `IValidationProvider` work |
| 6 | Resilience | ❌ | The verifier reads the catalog through the repository's own connection; no new external system |
| 7 | Distributed Locks | ❌ | No shared mutable state; the verdict cache is idempotent |
| 8 | Transactions | ❌ | The catalog read is read-only and uses the repository connection; it takes no part in `IUnitOfWork` semantics |
| 9 | Idempotency | ❌ | No message is received |
| 10 | Multi-Tenancy | ✅ | A case-insensitive `TenantId` column would match tenants `A` and `a` together. The verifier covers every mapped string column, and Phase 7 adds a conformance case for it; the tenant-aware builders delegate to the base builders |
| 11 | Module Isolation | ❌ | A `ModuleId` string column is covered by the same verifier; no module-specific work |
| 12 | Audit Trail | ❌ | No state change; the audit stores (`IOperationAuditStore`) record no comparison |

---

## Prerequisites & Dependencies

| Item | State | Effect on this plan |
|---|---|---|
| #3149 conformance suite | open | Phase 7 and the integration cases of Phases 2-6 need its fixtures; no provider PR merges without its cases |
| #3158 `StringComparison` silently dropped by the SQL builders | open, follows this issue | Detects culture-aware and `OrdinalIgnoreCase` `StringComparison` arguments; may map `OrdinalIgnoreCase` onto the typed operations' translation (Choice 9) |
| #2086 EF Core MySQL integration tests skipped | open | EFCore-MySQL is proven by translation tests only until Pomelo supports EF Core 10 |
| #1276 composable row filters | open, related | Tenant and soft-delete filters will become composable; the verifier must keep covering their string columns |
| ADR number | ADR number assigned when the ADR pull request opens | The number is taken from the batch's ADR table at that time and added to `docs/architecture/adr/index.md` in the same PR |

---

## Next Steps

1. Maintainer answers the open Dilemmas (Design Choices 3, 4, 6, 7) when work on the issue starts; the answers are recorded in `Maintainer Decisions` and the "(recommended, pending the maintainer)" markers are replaced.
2. Pull requests, in order (each at most 15 files and 1500 lines, one package each):
   1. Phase 1: `Encina.DomainModeling`, ADR, `AGENTS.md`.
   2. Phase 2: `Encina.ADO.SqlServer`; 3. `Encina.Dapper.SqlServer`.
   4. Phase 3: `Encina.ADO.PostgreSQL`; 5. `Encina.Dapper.PostgreSQL`.
   6. Phase 4: `Encina.ADO.MySQL`; 7. `Encina.Dapper.MySQL`.
   8. Phase 5: `Encina.EntityFrameworkCore`.
   9. Phase 6: `Encina.MongoDB`.
   10. Phase 7: conformance cases (depends on #3149).
   11. Phase 10: feature page and changelog fragment (the ADR travels with PR 1).
3. Open the deferred health-check issue (function 4) and link it here.
4. Run `check-plan.ps1` without `-Draft` after the answers are recorded.

---

## Maintainer Decisions

1. (2026-11-04) **Design Choice 1, issue Dilemma D1 — option D.** The maintainer decided, in a comment on #3150: `==`, `Contains`, `StartsWith` and `EndsWith` are ordinal by contract on all 10 providers; Encina guarantees it by requiring a binary collation on columns with ordinal semantics in SQL Server and MySQL (the ADR-046 pattern: required in the schema configuration, no silent default), checked by the conformance suite #3149 against real databases; case-insensitivity only through an explicit, typed operation (for example `EqualsIgnoreCase`) that each provider translates (`ILIKE`/`LOWER`, `COLLATE`, regex `i`) or answers with `Left`; accent-insensitivity is out of 1.0 (`Left`, documented); a new ADR records the decision.
