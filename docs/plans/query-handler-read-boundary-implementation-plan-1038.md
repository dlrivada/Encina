# Implementation Plan: Repository vs Query-Handler Read Boundary (Architecture Rule and Guide)

> **Issue**: [#1038](https://github.com/dlrivada/Encina/issues/1038)
> **Type**: Feature (architecture testing, documentation)
> **Related**: ADR-001 (Railway Oriented Programming), the repository pattern rule of `AGENTS.md` section 3 (the repository is optional and never forced), `docs/features/read-write-separation.md`
> **Complexity**: Low (one opt-in rule with a call-site scanner, one attribute, one guide; no runtime behavior)
> **Estimated Scope**: ~2,300 lines in total (~420 production, ~1,150 tests, ~730 documentation and example), in three pull requests of at most 11 files and 1,100 lines each

---

## Summary

### What exists today (read on 2026-11-17, main `822c310c7`)

- The repository contracts live in `Encina.DomainModeling`: `IReadOnlyRepository<TEntity, TId>` declares `GetByIdAsync`, `GetAllAsync`, two `FindAsync` overloads, `FindOneAsync`, two `AnyAsync`, two `CountAsync` and two `GetPagedAsync` (`src/Encina.DomainModeling/Repository.cs:21-130`); `IRepository<TEntity, TId>` extends it with the write members (`:167`). The functional family has the same shape with `Either` results: `IFunctionalReadRepository<TEntity, TId>` (`ListAsync`, `FirstOrDefaultAsync`, `GetPagedAsync`, `FunctionalRepository.cs:57-236`). The issue's snippets name `IRepository<,>.FindAsync` and `IReadOnlyRepository<,>` equivalents; the methods are declared on the read-only interfaces.
- `IQueryHandler<TQuery, TResponse> : IRequestHandler<TQuery, TResponse>` with `TQuery : IQuery<TResponse>` is in `Encina` (`src/Encina/Abstractions/CqrsContracts.cs:97`). `git grep "IQueryHandler<"` finds no query handler class in `src`: only the contract, a code-generator template (`src/Encina.Cli/Services/CodeGenerator.cs`), the existing architecture rule that asks handlers to implement it (`EncinaArchitectureRules.cs:419`) and three test files. The self-application the issue asks for will therefore find no violation in the shipped assemblies.
- `Encina.Testing.Architecture` (`ArchUnitNET.xUnitV3`; project reference to `Encina` only) has `EncinaArchitectureRules` (static `IArchRule` factories), `EncinaArchitectureRulesBuilder` and `EventIdUniquenessRule`. The issue's `IEncinaArchitectureRule`, `ArchitectureViolations` and `EncinaArchitectureTestBase`-based wiring do not exist as written; the real precedent for a rule that scans assemblies and is not an `IArchRule` is `EventIdUniquenessRule` (a static class returning a list of violation strings), wired into the builder by a flag (`_enforceUniqueEventIds`, `EnforceUniqueEventIds()`, `EncinaArchitectureRulesBuilder.cs:34` and `:350`), and opt-in rule sets such as `ApplyAllSagaRules()` (`:263`).
- Call-site detection by IL is already proven in two tests: `MessageSerializerCallSitesArchitectureTests` and `IdentityMintingArchitectureTests` (`tests/Encina.UnitTests/Testing/Architecture/`) read each method body's IL, resolve the called methods and cover the handler's nested compiler-generated types (closures, async state machines) and recover the source method name from the generated names (`MessageSerializerCallSitesArchitectureTests.cs:109-215`). `ArchUnitNET`'s fluent API in this package checks type and dependency relations, not which methods a method body calls.
- There is no guide for the boundary: the documentation of the repository methods does not mention query handlers, `docs/features/cqrs-read-write-boundary.md` does not exist, and `Encina.DomainModeling` and `Encina.Testing.Architecture` have READMEs (`src/Encina.DomainModeling/README.md`, `src/Encina.Testing.Architecture/README.md`).
- `Encina.Testing.Architecture` grants `InternalsVisibleTo` to `Encina.UnitTests`, `Encina.IntegrationTests`, `Encina.ContractTests` and `Encina.PropertyTests` (its csproj), not to `Encina.GuardTests`; `Encina.DomainModeling` grants it to the same four and the provider packages, not to `Encina.GuardTests`. `tests/Encina.IntegrationTests` does not reference `Encina.Testing.Architecture` today.

### What this plan delivers

1. **`QueryHandlerReadBoundaryRule`** (`Encina.Testing.Architecture`): a static rule, shaped like `EventIdUniquenessRule`, that reads the IL of every query handler (and its nested compiler-generated types) in the given assemblies and reports each call to a multi-row materializing read of a repository (`GetAllAsync`, `FindAsync`, `GetPagedAsync`, the functional `ListAsync`, and the multi-row reads of the soft-delete and temporal repositories: `ListWithDeletedAsync`, `GetHistoryAsync`, `GetChangedBetweenAsync`, `ListAsOfAsync`); `GetByIdAsync`, `FindOneAsync`, `FirstOrDefaultAsync`, `AnyAsync` and `CountAsync` are allowed.
2. **`EncinaArchitectureRulesBuilder.EnforceQueryHandlerReadBoundary()`**: opt-in, not part of `ApplyAllStandardRules()`.
3. **`ExpectedQueryHandlerRepositoryUseAttribute(reason)`** for justified exceptions on a handler class or its `Handle` method.
4. **Self-application**: the rule runs over every shipped `Encina*` assembly in a unit test (a permanent gate in CI) and over a real test project in an integration test.
5. **The guide** `docs/features/cqrs-read-write-boundary.md` with a worked before and after example whose SQL comes from a test, the two package READMEs and a cross-reference from the XML documentation of the repository read members.

Nothing runs in an application unless it calls `EnforceQueryHandlerReadBoundary()`.

- Affected packages: `Encina.Testing.Architecture` (rule, builder), `Encina.DomainModeling` (attribute, XML documentation, README); no runtime behavior changes.
- Provider category: none (the rule and the guide are provider-independent; the guide's example shows the SQL of the SQL Server provider as an illustration of one EF Core provider).

---

## Design Choices

<details>
<summary><strong>1. How the rule finds the repository calls</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A reflection and IL scan of the handler types and their nested types, as the two existing call-site tests do, packaged as an internal scanner** | Proven in this repository, including async state machines and closures; no new dependency; the scanner can be reused by the two existing tests later | A small internal IL reader (operand sizes) to maintain |
| **B) ArchUnitNET method-call dependencies** | The package's usual tooling | The fluent API in this package is used for type and namespace dependencies; whether the dependency model attributes calls inside `MoveNext` of an async state machine to the source method is not shown anywhere in the repository |
| **C) A Roslyn analyzer** | Instant IDE feedback | The issue itself defers it ("a Roslyn analyzer can follow if adoption proves the rule's value") |

### Chosen Option: **A — IL scan, internal scanner**

### Rationale

- The behavior that matters (a call inside an `async` `Handle`) is exactly what the existing tests already handle with this technique, so the rule inherits tested ground.

**Class:** Trivial
**Settled by:** `tests/Encina.UnitTests/Testing/Architecture/MessageSerializerCallSitesArchitectureTests.cs:109-215` and `IdentityMintingArchitectureTests.cs` (the repository's convention for call-site rules); the issue's Alternative 1 (architecture test first, analyzer later)

</details>

<details>
<summary><strong>2. Which repository reads are flagged</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The issue's literal list: `FindAsync`, `GetAllAsync`, `GetPagedAsync` on `IReadOnlyRepository<,>` and `IRepository<,>`** | Exactly what the issue names | The functional repositories (`IFunctionalReadRepository<,>`, `IFunctionalRepository<,>`) have the same problem under other names (`ListAsync`) and would pass |
| **B) Every multi-row materializing read of both repository families: `GetAllAsync`, both `FindAsync`, both `GetPagedAsync`, the functional `ListAsync` overloads, and the multi-row reads of `ISoftDeleteRepository` and `ITemporalRepository` (`ListWithDeletedAsync`, `GetHistoryAsync`, `GetChangedBetweenAsync`, `ListAsOfAsync`, `Repository.cs:286-475`); single-row reads (`GetByIdAsync`, `FindOneAsync`, `FirstOrDefaultAsync`) and the non-materializing `AnyAsync` and `CountAsync` are allowed** | Covers the whole pattern the issue describes (materializing entities with their change tracking to map them to DTOs) in the two repository families Encina ships; consistent with the issue's own allowance of `GetByIdAsync` | A slightly larger forbidden set to document |

### Chosen Option: **B — Multi-row materializing reads of both families** (recommended, pending the maintainer)

### Rationale

- The motivation of the issue is the materialization of aggregate lists in a query handler; the two families differ only in the return type, so flagging one and not the other would leave the same antipattern one rename away.
- A wins when the maintainer wants the rule to match the issue text exactly for the first release and to widen it after a period of adoption.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Where the suppression attribute lives</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) In `Encina.Testing.Architecture` (as the issue says)** | One package for the rule and its attribute | An application would reference a testing package from its production handlers to apply the attribute |
| **B) In `Encina.DomainModeling`, next to the repository contracts; the rule finds it by its full name, so the testing package needs no reference to `Encina.DomainModeling`** | Production code references only a package it already uses for `IRepository`; the attribute sits with the contract it qualifies | The attribute is a runtime type for a test-time rule; the issue names the other package |
| **C) No attribute: an allow-list passed to the rule in test code (`allow: [typeof(OrderListHandler)]`)** | No production type | The reason is far from the code it justifies; the exception list drifts from the handlers |

### Chosen Option: **B — Attribute in `Encina.DomainModeling`** (recommended, pending the maintainer)

### Rationale

- Production assemblies must not reference a testing package; `Encina.DomainModeling` already owns the repository contracts the rule is about.
- A wins when the maintainer accepts applications referencing `Encina.Testing.Architecture` in production, and C wins when exceptions should live only in the architecture test project.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. How the rule is named and wired</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The issue's names: the rule type `QueryHandlersDoNotUseFindAsyncRule` and the builder method `AddQueryHandlerDiscipline()`, opt-in, not part of `ApplyAllStandardRules()`** | Matches the issue's acceptance criterion ("`QueryHandlersDoNotUseFindAsyncRule` published") and its text | The type name says `FindAsync` while the recommended set of Design Choice 2 also flags `GetAllAsync`, `GetPagedAsync`, `ListAsync` and the history reads; `AddQueryHandlerDiscipline` does not follow the builder's `Enforce*` and `ApplyAll*` names |
| **B) `QueryHandlerReadBoundaryRule` and `EnforceQueryHandlerReadBoundary()` (a flag, like `EnforceUniqueEventIds()`), opt-in, not part of `ApplyAllStandardRules()`** | Names describe what is checked (the boundary, not one method); follows the builder's naming and flag precedent (`EncinaArchitectureRulesBuilder.cs:34`, `:350`) | Differs from the issue's literal names |
| **C) Add the rule to `ApplyAllStandardRules()`** | Everyone gets it | Breaks consumers that deliberately use repositories in query handlers; the issue requires the rule to be opt-in |

### Chosen Option: **B — Descriptive names, opt-in `Enforce*` method with a flag** (recommended, pending the maintainer)

### Rationale

- Every option except C is opt-in as the issue requires; the names are the open point: B keeps the type name true if Design Choice 2 widens the set and follows the builder's existing conventions.
- A wins if the issue's acceptance criterion on the names is binding (for example because documentation already links to them) or if Design Choice 2 keeps the literal `FindAsync` set.

**Class:** Dilemma

</details>

### Register of defaults and semantics

| Id | Choice | Class | Basis |
|----|--------|-------|-------|
| C1 | A query handler is a non-abstract class that implements `IQueryHandler<,>` or `IRequestHandler<TRequest, TResponse>` where `TRequest` implements `IQuery<>`; a handler dispatched without `IQueryHandler` is still a query handler | Trivial | `CqrsContracts.cs:97` (`IQueryHandler` extends `IRequestHandler`; `IQuery<TResponse> : IRequest<TResponse>`) |
| C2 | The scan covers every declared method and constructor of the handler type and, recursively, its nested types (closures, display classes and async state machines); a violation is reported against the source type and source method, recovered from the generated names | Trivial | `MessageSerializerCallSitesArchitectureTests.cs:109-140` (the same mapping) |
| C3 | A call is flagged when its declaring type is, or implements, a repository read interface identified by full name (`Encina.DomainModeling.IReadOnlyRepository`2`, `IRepository`2`, `IFunctionalReadRepository`2`, `IFunctionalRepository`2`) and its method name is in the forbidden set of Design Choice 2; no reference to `Encina.DomainModeling` is added to `Encina.Testing.Architecture`; `ISoftDeleteRepository` and `ITemporalRepository` extend these interfaces, so their calls are matched through "implements" | Dilemma (follow-on of Design Choice 2 B, recommended, pending the maintainer) | `Encina.Testing.Architecture.csproj` (references `Encina` only); applies to whichever forbidden set the maintainer picks |
| C4 | Known limit, documented: the scan sees direct calls in the handler type and its nested types; a call made by a helper class or an injected service the handler delegates to is not followed, and extension methods that take a repository are not flagged. It guards against the direct regression, not against every route | Trivial | `NoPostProcessorWritesToOutboxTests.cs` remarks (the same stated limit for the existing guard) |
| C5 | A violation is a string with the namespace-qualified handler type, the source method and the called interface method, never a value or a message | Trivial | `EventIdUniquenessRule` returns plain strings of the same shape (`EventIdUniquenessRule.cs:55`) |
| C6 | The attribute has `AttributeUsage(Class \| Method)`, a required non-blank `reason` (`ArgumentException` otherwise), and the rule honors it on the handler class or on the source method of the call; a suppressed use is not reported | Dilemma (follow-on of Design Choice 3 B, recommended, pending the maintainer) | The issue's text ("justified exceptions on individual call sites"; an attribute cannot target a call site, so the source method is the finest target); applies only if the attribute exists as Design Choice 3 recommends (an allow-list option would replace it) |
| C7 | The rule takes `IEnumerable<Assembly>` and throws `ArgumentNullException` for a null sequence or element; an assembly that cannot load some types is scanned for the types that load (the loadable-types helper of the existing tests) | Trivial | `EventIdUniquenessRule.AssertEveryLoggerMessageHasEventId(IReadOnlyList<Assembly>)` (`:55`) takes the assemblies to scan and returns violation strings; `MessageSerializerCallSitesArchitectureTests.cs:217` (loadable types) |
| C8 | No telemetry, logging, EventIds, error codes or tenant data: the rule runs in a test process and returns strings; ADR-062 categories, ADR-054 D4 tags and the reserved `default` tenant segment do not arise | Trivial | `AGENTS.md` section 6 (the issue lists every function as N/A) |
| C9 | Self-application: the rule runs over every shipped `Encina*` assembly in the unit test project (a permanent gate in CI, the pattern of `NoPostProcessorWritesToOutboxTests`) and over the `Encina.IntegrationTests` assembly in the integration test; any violation found is fixed in its package or recorded with the attribute and a reason; none is expected, because the shipped assemblies contain no query handler class | Dilemma (follow-on of Design Choices 2 and 3, recommended, pending the maintainer) | The issue's self-application task; `git grep "IQueryHandler<"` on `822c310c7` |
| C10 | No ADR is created: the issue makes it optional "only if the rule semantics warrant one"; the semantics here are a guideline with an opt-in check, recorded by the guide | Trivial | Issue #1038 ("ADR in `docs/architecture/adr/` — only if the rule semantics warrant one") |

---

## Implementation Phases

### Phase 1: Rule, Scanner, Attribute and Builder Method (pull request S1)

> **Goal**: The rule works on handlers with direct, async and closure calls, honors the attribute, and is available from the builder.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.DomainModeling/ExpectedQueryHandlerRepositoryUseAttribute.cs`: `[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = false)] public sealed class ExpectedQueryHandlerRepositoryUseAttribute : Attribute` with `public ExpectedQueryHandlerRepositoryUseAttribute(string reason)` (`ArgumentException.ThrowIfNullOrWhiteSpace`) and `public string Reason { get; }`; XML docs with an example (register row C6).
2. **Create** `src/Encina.Testing.Architecture/CallSiteScanner.cs`: `internal static class CallSiteScanner` with `IEnumerable<MethodBase> CalledMethods(MethodBase method)`, `IEnumerable<Type> WithNestedTypes(Type type)` and the source-name recovery, extracted in the same shape as `MessageSerializerCallSitesArchitectureTests.cs:109-215` (the two existing tests keep their own copies; moving them onto the scanner is a follow-up, see Next Steps).
3. **Create** `src/Encina.Testing.Architecture/QueryHandlerReadBoundaryRule.cs`: `public static class QueryHandlerReadBoundaryRule` with `public static IReadOnlyList<string> AssertQueryHandlersDoNotMaterializeAggregates(IEnumerable<Assembly> assemblies)` (register rows C1 to C7); the forbidden method names and the four interface full names are `static readonly` sets (Design Choice 2, row C3); the attribute is found by full name `Encina.DomainModeling.ExpectedQueryHandlerRepositoryUseAttribute` (Design Choice 3).
4. **Modify** `src/Encina.Testing.Architecture/EncinaArchitectureRulesBuilder.cs`: `public EncinaArchitectureRulesBuilder EnforceQueryHandlerReadBoundary()` sets a flag; `ExecuteVerification` adds each violation as `new ArchitectureRuleViolation("QueryHandlerReadBoundary", message)` (the shape of `:396-410`); it is not added by `ApplyAllStandardRules()` (Design Choice 4).
5. **Modify** `src/Encina.Testing.Architecture/PublicAPI.Unshipped.txt`, `src/Encina.DomainModeling/PublicAPI.Unshipped.txt`, `.github/coverage-manifest/Encina.Testing.Architecture.json` and `.github/coverage-manifest/Encina.DomainModeling.json` through `mechanical-fixer`.
6. Tests (new): `tests/Encina.UnitTests/Testing/Architecture/QueryHandlerReadBoundaryRuleTests.cs` (fixture handlers, each a real class: a violating `async` handler that calls `FindAsync`, `GetAllAsync` and `GetPagedAsync` on the read-only and full repository interfaces, the functional `ListAsync`, `ListWithDeletedAsync` and `GetHistoryAsync` through the soft-delete and temporal interfaces, and a lambda inside `Handle`, each reported with its source method; a conforming handler that projects from a queryable, one that calls `GetByIdAsync`, `FindOneAsync`, `AnyAsync` and `CountAsync`; a handler dispatched through `IRequestHandler` with an `IQuery<>` request; an abstract handler and a command handler are ignored; the attribute on the class and on the method suppresses; a handler calling a concrete repository class that implements the interface is flagged) and `tests/Encina.GuardTests/Testing/Architecture/QueryHandlerReadBoundaryGuardTests.cs` (null assemblies, a null element; a null or blank attribute reason). (edited) `tests/Encina.UnitTests/Testing/Architecture/EncinaArchitectureRulesBuilderTests.cs` (the builder method adds the check, `ApplyAllStandardRules()` does not, `Verify()` throws with the violation and `VerifyWithResult()` reports it).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
Encina (.NET 10, C# 14, pre-1.0). Issue #1038 plan docs/plans/query-handler-read-boundary-implementation-plan-1038.md,
phase 1 of 3. Encina.Testing.Architecture references only Encina (not Encina.DomainModeling) and has
InternalsVisibleTo for the unit, integration, contract and property test projects, not for the guard tests. EventIdUniquenessRule is the model of a static rule class returning violation strings, wired into
EncinaArchitectureRulesBuilder by a flag.

TASK:
1. Create ExpectedQueryHandlerRepositoryUseAttribute in Encina.DomainModeling (class or method, required reason).
2. Create the internal CallSiteScanner (IL call sites of a method, nested types, source-name recovery) and the public
   QueryHandlerReadBoundaryRule.AssertQueryHandlersDoNotMaterializeAggregates(IEnumerable<Assembly>).
3. Add EncinaArchitectureRulesBuilder.EnforceQueryHandlerReadBoundary() (opt-in flag; not in ApplyAllStandardRules).

KEY RULES:
- Identify repository interfaces by full name; do not add a project reference to Encina.DomainModeling.
- Flag GetAllAsync, FindAsync, GetPagedAsync, the functional ListAsync, ListWithDeletedAsync, GetHistoryAsync,
  GetChangedBetweenAsync and ListAsOfAsync; allow GetByIdAsync, FindOneAsync,
  FirstOrDefaultAsync, AnyAsync, CountAsync. Cover async state machines and lambdas through nested types.
- Violation strings name types and methods only. Tests run the real rule on real fixture classes.
- PublicAPI.Unshipped.txt and the manifests through mechanical-fixer.

REFERENCE FILES:
- src/Encina.Testing.Architecture/EventIdUniquenessRule.cs, EncinaArchitectureRulesBuilder.cs (:34, :263, :350, :380-411)
- tests/Encina.UnitTests/Testing/Architecture/MessageSerializerCallSitesArchitectureTests.cs (:109-215)
- src/Encina.DomainModeling/Repository.cs, FunctionalRepository.cs
```

</details>

---

### Phase 2: Self-Application (pull request S2)

> **Goal**: The rule is a permanent CI gate over Encina's own assemblies and is proven on a real test project.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `tests/Encina.UnitTests/Testing/Architecture/QueryHandlerReadBoundarySelfApplicationTests.cs`: loads every shipped `Encina*` assembly from the test output directory (the loader of `NoPostProcessorWritesToOutboxTests`), runs the rule and asserts no violation; a second test asserts the scan saw a minimum number of assemblies (so an empty scan cannot pass).
2. **Modify** `tests/Encina.IntegrationTests/Encina.IntegrationTests.csproj`: add `<ProjectReference Include="..\..\src\Encina.Testing.Architecture\Encina.Testing.Architecture.csproj" />` (the project does not reference it today; the file count of S2 includes it).
3. **Create** `tests/Encina.IntegrationTests/Testing/Architecture/QueryHandlerReadBoundaryIntegrationTests.cs`: applies the rule to the `Encina.IntegrationTests` assembly (a real test project, per the issue) and to the shipped assemblies; any violation found is fixed or marked with the attribute and its reason in the same pull request, or recorded as a `[DEBT]` follow-up when the handler belongs to another package (none is expected; the measured baseline is zero query handler classes in `src`).
4. **Modify** `.github/coverage-manifest/Encina.Testing.Architecture.json` through `mechanical-fixer`: the integration targets of `QueryHandlerReadBoundaryRule.cs` and `CallSiteScanner.cs`, added here because the integration scan that reaches them is in this PR (S1 sets their unit and guard targets only; no existing target is lowered). No PublicAPI change: tests only.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
Issue #1038 plan docs/plans/query-handler-read-boundary-implementation-plan-1038.md, phase 2 of 3. Depends on S1.

TASK:
1. Add the self-application unit test over the shipped Encina* assemblies (permanent CI gate).
2. Add the project reference to Encina.Testing.Architecture in Encina.IntegrationTests.csproj and the integration test
   that applies the rule to the Encina.IntegrationTests assembly.

KEY RULES:
- A scan that loads no assembly must fail, not pass.
- A violation found is fixed, suppressed with the attribute and a reason, or recorded as a [DEBT] issue; never ignored.
- Integration tests of this feature need no container: they scan assemblies.

REFERENCE FILES:
- tests/Encina.UnitTests/Testing/Architecture/NoPostProcessorWritesToOutboxTests.cs
- tests/Encina.IntegrationTests/Encina.IntegrationTests.csproj
```

</details>

---

### Phase 3: Guide, Example and Finalization (pull request S3)

> **Goal**: The boundary is documented with a worked example whose SQL comes from a test, and the issue is ready to close.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `docs/features/cqrs-read-write-boundary.md` through `docs-writer`: the two read patterns and the rule of thumb of the issue, a before and after of one business question (a repository `FindAsync(specification)` mapped to a DTO versus a direct LINQ projection) with the SQL taken from the test of task 4, the rule, the builder method, the attribute and its placement, the known limit (register row C4), when a repository read is correct; add its entry to `docs/features/index.md`.
2. **Modify** `src/Encina.DomainModeling/README.md` (a section on the read and write boundary) and `src/Encina.Testing.Architecture/README.md` (the new rule in "Available Rules" and "Builder API") through `docs-writer`.
3. **Modify** the XML documentation (no code change) of `GetAllAsync`, both `FindAsync`, both `GetPagedAsync` in `src/Encina.DomainModeling/Repository.cs` and of the functional `ListAsync` and `GetPagedAsync` in `src/Encina.DomainModeling/FunctionalRepository.cs`: a `<remarks>` pointing to the guide and to the rule. These are two source files whose behavior does not change; their per-file targets stay as they are.
4. **Create** `tests/Encina.UnitTests/EntityFrameworkCore/ReadWriteBoundaryQueryShapeTests.cs`: builds a `DbContext` with the SQL Server provider and no connection (the SQL of `ToQueryString()` needs none) and asserts that the direct projection selects only the DTO columns and that the entity read selects every mapped column; the guide quotes this output. Counted here; it needs no csproj edit (`Encina.UnitTests` references EF Core and the SQL Server provider).
5. **Create** `changelog.d/1038-query-handler-read-boundary.added.md` through `mechanical-fixer`, and the justification files for Load and Benchmark tests (`AGENTS.md` section 9): `tests/Encina.LoadTests/Testing/QueryHandlerReadBoundary.md` and `tests/Encina.BenchmarkTests/Testing/QueryHandlerReadBoundary.md` (a test-time scan of assemblies; no concurrency and no hot path).
6. Verification: `dotnet build --configuration Release` with zero warnings, `dotnet test` of the touched projects, per-flag coverage of the touched files against the table below.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
Issue #1038 plan docs/plans/query-handler-read-boundary-implementation-plan-1038.md, phase 3 of 3. Depends on S1 and S2.

TASK:
1. Write docs/features/cqrs-read-write-boundary.md, update the two READMEs and the features index (docs-writer).
2. Add a remarks cross-reference to the XML docs of the repository read members (Repository.cs, FunctionalRepository.cs).
3. Add ReadWriteBoundaryQueryShapeTests (the SQL quoted in the guide), the changelog fragment (mechanical-fixer) and the
   two justification files for Load and Benchmark tests.

KEY RULES:
- Pages go through docs-writer; fragments through mechanical-fixer. Quote SQL only from the test output; never type
  coverage figures by hand.
- Name no private client application: write "the reference application" if one is needed.
- Link sibling plans by issue number only. No ADR is created by this plan.

REFERENCE FILES:
- .claude/skills/encina-docs/SKILL.md
- docs/features/read-write-separation.md
- src/Encina.DomainModeling/Repository.cs, FunctionalRepository.cs
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Use in this feature |
|--------|-----------|---------------------|
| CQRS (Young, Fowler) | Commands change state through aggregates; queries read through projections | The rule of thumb of the guide |
| Domain-driven design | Repositories load and save aggregates | Why a repository suits command handlers |
| `AGENTS.md` section 3 | The repository pattern is optional and never forced; default to `DbContext` and `DbSet<T>` | The rule is opt-in and the guide shows the `DbContext` projection |
| `AGENTS.md` section 9 | Tests execute real package code; documentation never types coverage figures by hand | Fixture handlers are real classes; the SQL comes from a test |
| Zoran Horvat, "EF Core Is DDD (Here Is Why)" (video and reference repository named in the issue) | Writes through aggregates, reads project directly to DTOs | The inspiration cited in the guide |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `EventIdUniquenessRule`, `EncinaArchitectureRulesBuilder` | `src/Encina.Testing.Architecture/` | Shape of the rule and the flag wiring |
| IL call-site scanning | `tests/Encina.UnitTests/Testing/Architecture/MessageSerializerCallSitesArchitectureTests.cs`, `IdentityMintingArchitectureTests.cs` | The technique, extracted into the internal scanner |
| `NoPostProcessorWritesToOutboxTests` | `tests/Encina.UnitTests/Testing/Architecture/` | Assembly loader and the self-application pattern |
| `IReadOnlyRepository`, `IRepository`, `IFunctionalReadRepository`, `IFunctionalRepository` | `src/Encina.DomainModeling/Repository.cs`, `FunctionalRepository.cs` | The contracts the rule recognizes by name |
| `IQueryHandler`, `IQuery` | `src/Encina/Abstractions/CqrsContracts.cs` | Query handler recognition |
| `docs/features/read-write-separation.md` | `docs/features/` | A sibling page the guide links to |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| none | none | No logging, no `EventId` and no new range: the rule returns strings in a test process. The block 650-699 assigned to this family is used by #729 (650-659), #730 (660-669) and #732 (670-679); nothing is taken from it here |

### Estimated File Count

Every count includes tests, coverage manifest edits, PublicAPI files, READMEs and the changelog fragment. No pull request uses a type created by a later one; each enters its per-file manifest targets with its own tests.

| PR | Content | New src | Changed src | New tests | Edited tests | Manifest | Other | Files | Lines (est.) |
|----|---------|---------|-------------|-----------|--------------|----------|-------|-------|--------------|
| S1 | Rule, scanner, attribute, builder method | 3 (`QueryHandlerReadBoundaryRule`, `CallSiteScanner`, `ExpectedQueryHandlerRepositoryUseAttribute`) | 3 (`EncinaArchitectureRulesBuilder`, the two PublicAPI files) | 2 (rule unit, guard) | 1 (`EncinaArchitectureRulesBuilderTests`) | 2 (`Encina.Testing.Architecture.json`, `Encina.DomainModeling.json`) | 0 | 11 | ~1,000 |
| S2 | Self-application | 0 | 0 | 2 (unit self-application, integration) | 0 | 1 (`Encina.Testing.Architecture.json`) | 1 (`Encina.IntegrationTests.csproj`) | 4 | ~280 |
| S3 | Guide, example, finalization | 0 | 2 (`Repository.cs`, `FunctionalRepository.cs`, XML docs only) | 1 (`ReadWriteBoundaryQueryShapeTests`) | 0 | 1 (`Encina.DomainModeling.json`) | 7 (guide, `docs/features/index.md`, two READMEs, changelog fragment, 2 justification `.md`) | 11 | ~1,060 |
| **Total** | | **3** | **5** | **5** | **1** | **4** | **8** | **26** | **~2,330** |

The columns sum to the total (3 + 5 + 5 + 1 + 4 + 8 = 26) and the pull-request rows sum to 26 files (11 + 4 + 11). S3's line estimate includes about 650 lines of guide and README text; the code part stays under 400 lines. Caller counts verified with `git grep` on `822c310c7`: `IQueryHandler<` appears in six files of `src` and `tests` and no query handler class exists in `src`; `EncinaArchitectureRulesBuilder` gains one method and no constructor parameter, so no caller changes. S2's csproj edit is the only project-level change.

Per-file coverage targets (per flag, with a one-sentence justification), written into the package manifests by the pull request whose tests reach them (unit and guard targets in S1, integration targets in S2). Manifest targets are whole-file and these files hold defaults only today, so each PR first measures the file's per-flag baseline and sets a whole-file target not below it; no existing target is lowered:

| File | Unit | Guard | Property | Integration | Justification |
|------|------|-------|----------|-------------|---------------|
| `Encina.Testing.Architecture/QueryHandlerReadBoundaryRule.cs` | 95 | 90 | 0 | 80 (added by S2) | Every path (violation, allowed call, attribute on class and method, ignored types) runs on real fixture handlers; the guard tests reach the null checks; the integration test runs the full scan over real assemblies (the attribute and fixture paths are unit-only); the logic is deterministic scanning, so no property adds coverage |
| `Encina.Testing.Architecture/CallSiteScanner.cs` | 90 | 0 | 0 | 80 (added by S2) | Internal type with no `InternalsVisibleTo` for the guard project, reached only through the public rule, so guard tests cannot reach it; the IL operand-size switch carries a `crap-exempt` marker; the unit tests exercise it through handlers with every call shape, and the integration scan runs it over real assemblies |
| `Encina.Testing.Architecture/EncinaArchitectureRulesBuilder.cs` (changed members) | 90 | 0 | 0 | 0 | The new method and the verification branch run in the builder tests; the method takes no parameter |
| `Encina.DomainModeling/ExpectedQueryHandlerRepositoryUseAttribute.cs` | 100 | 100 | 0 | 0 | A constructor with one guarded parameter and a property, fully reached by unit and guard tests |
| `Encina.DomainModeling/Repository.cs`, `FunctionalRepository.cs` (XML docs only) | measured baseline | measured baseline | measured baseline | measured baseline | The two manifest entries hold defaults only, so S3 (which touches the files) measures each file's per-flag coverage first and records those figures as the targets (a floor that pins today's coverage), with the reason "documentation comments only; the target pins the measured coverage"; the manifest edit is counted in S3 |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #1038 - the repository versus query-handler read boundary in Encina.

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library; pre-1.0, no backward compatibility, best solution always.
- Encina.Testing.Architecture references only Encina (no project reference to Encina.DomainModeling) and has
  InternalsVisibleTo for the unit, integration, contract and property test projects, not the guard tests. EventIdUniquenessRule is the model of a static assembly rule returning violation strings, wired
  into EncinaArchitectureRulesBuilder by a flag (EnforceUniqueEventIds).
- IL call-site scanning is already used by MessageSerializerCallSitesArchitectureTests and IdentityMintingArchitectureTests.
- The repository is optional (AGENTS.md section 3); the rule is opt-in.

IMPLEMENTATION OVERVIEW:
1. ExpectedQueryHandlerRepositoryUseAttribute (Encina.DomainModeling), internal CallSiteScanner, public
   QueryHandlerReadBoundaryRule and EncinaArchitectureRulesBuilder.EnforceQueryHandlerReadBoundary() (S1).
2. Self-application: a unit test over the shipped Encina* assemblies and an integration test over a real test project (S2).
3. docs/features/cqrs-read-write-boundary.md with SQL from ReadWriteBoundaryQueryShapeTests, two READMEs, XML doc
   cross-references, changelog fragment and justification files (S3).
Three pull requests, see the Estimated File Count table.

KEY PATTERNS:
- Flag GetAllAsync, FindAsync, GetPagedAsync, the functional ListAsync, ListWithDeletedAsync, GetHistoryAsync,
  GetChangedBetweenAsync and ListAsOfAsync inside query handlers; allow GetByIdAsync,
  FindOneAsync, FirstOrDefaultAsync, AnyAsync and CountAsync; cover async state machines and lambdas via nested types.
- Repository interfaces are matched by full name; violations are strings naming types and methods only.
- Tests run the real rule on real fixture classes; PublicAPI.Unshipped.txt, manifests and changelog through
  mechanical-fixer; documentation through docs-writer.

REFERENCE FILES:
- src/Encina.Testing.Architecture/EventIdUniquenessRule.cs, EncinaArchitectureRulesBuilder.cs
- tests/Encina.UnitTests/Testing/Architecture/MessageSerializerCallSitesArchitectureTests.cs
- src/Encina.DomainModeling/Repository.cs, FunctionalRepository.cs, src/Encina/Abstractions/CqrsContracts.cs
- docs/plans/dsr-implementation-plan-404.md (style)
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | A test-time rule and documentation; no runtime read path is added |
| 2 | OpenTelemetry | ❌ N/A | Nothing runs at application runtime |
| 3 | Structured Logging | ❌ N/A | The rule returns violation strings; no `[LoggerMessage]` and no EventId range |
| 4 | Health Checks | ❌ N/A | No dependency to check |
| 5 | Validation | ❌ N/A | The rule validates architecture, not request input; it uses none of the three validation providers |
| 6 | Resilience | ❌ N/A | No external call |
| 7 | Distributed Locks | ❌ N/A | No shared state |
| 8 | Transactions | ❌ N/A | The guide explains why reads that return DTOs need no unit of work; the feature opens none |
| 9 | Idempotency | ❌ N/A | The scan is repeatable and has no side effects |
| 10 | Multi-Tenancy | ❌ N/A | No tenant data is stored or composed; the reserved `default` tenant segment does not arise |
| 11 | Module Isolation | ❌ N/A | The rule applies per assembly given by the caller; no module scoping is needed |
| 12 | Audit Trail | ❌ N/A | No compliance or security implication |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| `IRepository`, `IReadOnlyRepository` and the functional repositories in `Encina.DomainModeling` | ✅ On `main` | The contracts the rule recognizes |
| `IQueryHandler`, `IQuery` in `Encina` | ✅ On `main` | Query handler recognition |
| `Encina.Testing.Architecture` builder and the precedent rule | ✅ On `main` | Shape and wiring |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| Moving the two existing call-site tests onto `CallSiteScanner` | Follow-up draft `artifacts/issues/architecture-call-site-scanner-reuse.md` | Removes the duplicated IL reader from `MessageSerializerCallSitesArchitectureTests` and `IdentityMintingArchitectureTests` |

---

## Next Steps

1. Link this plan from #1038 and ask the maintainer to answer Design Choices 2 and 3 when work starts.
2. Open the follow-up issue when S1 opens: reuse `CallSiteScanner` in the two existing call-site tests (draft: `artifacts/issues/architecture-call-site-scanner-reuse.md`).
3. Implement S1 to S3 as the pull requests of the Estimated File Count split, `Fixes #1038` on S3.
