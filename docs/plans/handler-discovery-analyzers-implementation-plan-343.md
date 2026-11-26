# Implementation Plan: Handler discovery and CQRS convention analyzers (rules ENC011 to ENC015 of `Encina.Analyzers`)

> **Issue**: [#343](https://github.com/dlrivada/Encina/issues/343)
> **Type**: Feature (developer experience; rules added to the package of #438)
> **Complexity**: Medium (3 phases and a documentation phase, 4 pull requests, no provider)
> **Estimated Scope**: ~700 lines of production code + ~800 lines of tests + ~120 lines of documentation
> **Family**: dev-experience. The package `Encina.Analyzers`, its test harness, the id prefix and `EncinaKnownTypes` belong to the plan of #438; this plan adds rules to it and starts after #438's Phase 1 merges. The convention-based handlers that ENC014 checks belong to the plan of #333.

---

## Summary

Add the CQRS-convention rules of the issue to `Encina.Analyzers`: discovery mistakes (handlers that `AddEncina` never finds), handler naming, queries that change state, a response type that does not match the request, and handlers with too many dependencies, each with a severity and threshold configurable through `.editorconfig`.

What exists today (read on `origin/main` at `326e65ebf`, after `git fetch`):

- Of the issue's eight rules, three are already covered or contradicted by the code, so this plan drops them (Design Choice 1):
  - **ENCINA005 (missing validator)** is the same rule as ENC002 of #438.
  - **ENCINA007 (behavior order)**: `PipelineStage` fixes the order independent of call order (`Validation = 900`, `Transaction = 1300`, `src/Encina/Pipeline/Ordering/PipelineStage.cs`), and `PipelineOrderValidationHostedService` validates the registered steps at startup. The issue's example, `AddBehavior<TransactionBehavior>()` before `AddBehavior<ValidationBehavior>()`, is not a mistake today.
  - **ENCINA008 (CancellationToken not propagated)** is the .NET analyzer CA2016 ("Forward the CancellationToken parameter to methods that take one"), which ships with the .NET SDK analyzers that Encina builds already run (`EnableNETAnalyzers` in `Directory.Build.props`) and that user projects get from their SDK; a missing token parameter cannot occur on an interface handler (the compiler requires it), and ENC001 of #438 (redefined there as a token parameter never used) covers the unused case.
- The compiler already rejects the issue's example of ENCINA004 (`Handle` returning `Either<EncinaError, string>` for `ICommandHandler<CreateOrderCommand, OrderDto>`): the interface method fixes the return type (`src/Encina/Abstractions/IRequestHandler.cs:57`), so the error is CS0535. The rule only has a purpose for convention-based handlers (#333), whose `Handle` is found by name and not by interface.
- Discovery is by assembly scan: `AddEncina(services, configure, params Assembly[] assemblies)` scans the assemblies given and, when none is given, scans only `typeof(ServiceCollectionExtensions).Assembly`, the core assembly (`src/Encina/Core/ServiceCollectionExtensions.cs`, `AddEncina`), so `services.AddEncina()` with no assembly registers no application handler at all. `EncinaAssemblyScanner` (`src/Encina/Dispatchers/MediatorAssemblyScanner.cs`) registers every non-abstract class implementing `IRequestHandler<,>`, `INotificationHandler<>` or `IStreamRequestHandler<,>`, nested and internal included.
- Handler interfaces are `ICommandHandler<,>`, `ICommandHandler<>`, `IQueryHandler<,>`, `IQueryHandler<>` (`src/Encina/Abstractions/CqrsContracts.cs:79-105`); `ICommand<>` and `IQuery<>` are at `:29` and `:60`.
- There is no analyzer test in the repository today (#2842, open); #438 creates the in-process harness `AnalyzerTestHost` that this plan uses.

What this plan delivers: ENC011 to ENC015 as defined below, two code fixes (register the handler's assembly, rename the handler), an `.editorconfig` reader for `encina_max_handler_dependencies`, and a documentation section per rule.

| Rule | Detects | Default severity | Fix |
|------|---------|------------------|-----|
| ENC011 | `AddEncina` called with no assembly argument and none of the three registration calls in its configure lambda (`RegisterServicesFromAssembly`, `RegisterServicesFromAssemblies`, `RegisterServicesFromAssemblyContaining<T>`, `src/Encina/Core/EncinaConfiguration.cs:91,101,122`), in a compilation that declares at least one handler | Warning | add `typeof(<handler>).Assembly` as an argument |
| ENC012 | Handler class name that does not match its request name (Design Choice 2) | Suggestion | rename the class (Roslyn `Renamer`) |
| ENC013 | A query handler that calls a state-changing member (Design Choice 3) | Suggestion (a heuristic Warning would break `TreatWarningsAsErrors` builds on its false positives; raise it after measuring over `Encina.slnx`) | none |
| ENC014 | A convention-based handler (#333) whose `Handle` return type does not carry the response its request declares | Error | none |
| ENC015 | A handler whose constructor has more parameters than `encina_max_handler_dependencies` (default 6) | Suggestion | none |

- **Affected packages**: `Encina.Analyzers` (new in #438), `Encina.UnitTests`, `Encina.GuardTests`. The core package is not changed.
- **Provider category**: none.
- **Prerequisites**: #438 PR-1 (package, harness, `EncinaKnownTypes`, the `Tooling` coverage category and the manifest this plan edits) merges before this plan's PR-1; #333 merges before PR-3 (ENC014 needs its `[Handler]` convention), and #333's pending Dilemma 1 can remove PR-3 (see Phase 3).

### Shared Pieces and Ownership (family dev-experience)

| Shared piece | Owner | Used by |
|--------------|-------|---------|
| Package, harness, `EncinaKnownTypes`, id prefix | #438 (sibling plan, by number) | this plan |
| ENC011 to ENC015, `AnalyzerOptionsReader` for `encina_*` keys | **This plan (#343)** | none |
| `[Handler]` attribute and the convention (what ENC014 inspects) | #333 (sibling plan, by number) | this plan |

---

## Design Choices

<details>
<summary><strong>1. Which of the issue's eight rules to build — drop the three that the code already covers</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Build five rules; drop validator (same as ENC002), behavior order (runtime-validated by stage) and CancellationToken propagation (CA2016)** | No duplicate diagnostics for one mistake; no false positive on the documented stage ordering | The issue's list shrinks from eight to five; the acceptance "at least 5 analyzers" is met exactly |
| **B) Build all eight as written** | Matches the issue text | ENCINA007 flags correct code; ENCINA008 duplicates CA2016 and ENCINA005 duplicates ENC002; ENCINA004 can never fire on interface handlers |

### Chosen Option: **A — Five rules, three dropped with their reasons**

### Rationale

- Each dropped rule has a verifiable reason in the code or in the #438 plan, so the choice is not a preference.
- **Class:** Settled
- **Settled by:** `src/Encina/Pipeline/Ordering/PipelineStage.cs` (stage values) and `PipelineOrderValidationHostedService.cs`; `src/Encina/Abstractions/IRequestHandler.cs:57` (return type fixed by the interface); the CA2016 rule of the .NET SDK analyzers (`EnableNETAnalyzers`, `Directory.Build.props`; its default severity is the user's `.editorconfig` choice); issue #343's acceptance criterion "At least 5 analyzers implemented".

</details>

<details>
<summary><strong>2. Which names ENC012 accepts</strong></summary>

The issue's example says the handler of `CreateOrderCommand` should be `CreateOrderCommandHandler`; its sibling issue #333 and the repository's own documentation name it `CreateOrderHandler` (`ICommandHandler<CreateOrderCommand, OrderDto>`). A strict rule would flag the second form that users already write.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Accept `<Request>Handler` and `<Request without Command/Query suffix>Handler`** | Both common spellings pass; the rule catches real mismatches such as `OrderCreator` | Two accepted names; a class named for another request with the same prefix passes |
| **B) Accept only `<Request>Handler`** | One strict rule, as the issue's example | Flags `CreateOrderHandler`, the form used across the docs and #333 |
| **C) Accept any name ending in `Handler`** | Almost no noise | Does not check the request at all |

### Chosen Option: **A — Both spellings** (recommended, pending the maintainer)

### Rationale

- A wins when users keep the short spelling; B wins when the team wants one name per request for searchability. Severity is Suggestion either way, so the cost of the wrong choice is low.
- **Class:** Dilemma

</details>

<details>
<summary><strong>3. How ENC013 decides that a query "modifies state"</strong></summary>

State change is not decidable in general. The issue's example calls `_repository.UpdateLastAccessed(...)` from a query handler.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Name and type heuristic: a call on a member whose name starts with `Add`, `Insert`, `Update`, `Delete`, `Remove`, `Save` or `Send`, or any `SaveChanges*`/`ExecuteSql*` call on a `DbContext`; configurable prefix list through `.editorconfig`** | Catches the issue's example and the common EF mistake; cheap; configurable | False positives (`AddRange` on a local list) and misses (`Process`) |
| **B) Flow analysis through the callees to find writes to fields or I/O** | More precise | Whole-program analysis a per-compilation analyzer cannot do across assemblies; expensive in the IDE |
| **C) Do not ship ENC013** | No heuristic noise | Drops the CQRS separation rule the issue names |

### Chosen Option: **A — Name and type heuristic, excluding calls on locals and `System.Collections` types, default severity Suggestion** (recommended, pending the maintainer)

### Rationale

- A wins when an imperfect diagnostic that is easy to configure beats no rule; C wins when the team judges a heuristic rule more costly than the mistake it prevents. Under A the default is Suggestion, not Warning: a Warning on a heuristic fails builds that treat warnings as errors on every false positive, so the severity is raised to Warning only after the rule has been measured over `Encina.slnx` and the false-positive rate is known.
- **Class:** Dilemma

</details>

<details>
<summary><strong>4. The issue's acceptance "Integration with Encina source generators (#50)"</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Do not build it; #50 was closed as obsolete, and ADR-005 rejects source generators for handler dispatch** | Follows the recorded decision | The acceptance line is not met as written |
| **B) Re-open the question inside this plan** | Would honour the line | Reopening needs a new ADR (the #50 closing comment says so); out of this plan's remit |

### Chosen Option: **A — Not built**

### Rationale

- The analyzers inspect handlers the same way whether discovery is by scan or by a generator, so nothing is lost; ENC014 is written against the convention of #333 whichever mechanism that plan settles.
- **Class:** Settled
- **Settled by:** `docs/architecture/adr/005-reject-source-generators.md` (Decision: "We REJECT the use of Source Generators for handler dispatch") and the closing comment of issue #50 (not planned, obsolete).

</details>

---

## Implementation Phases

### Phase 1: Discovery and naming rules (PR-1: ENC011, ENC012)

<details>
<summary>Tasks</summary>

1. `src/Encina.Analyzers/HandlerNotDiscoveredAnalyzer.cs` (ENC011): `RegisterCompilationStartAction`; collect handler types of the compilation through `EncinaKnownTypes`; an `InvocationOperation` action on `AddEncina` (extension of `IServiceCollection`) checks the arguments and the configure lambda for the three accepted shapes: `RegisterServicesFromAssembly`, `RegisterServicesFromAssemblies` and `RegisterServicesFromAssemblyContaining<T>`. A negative test per shape (and one for an assembly passed as an argument) proves none is flagged; without the third shape the documented `RegisterServicesFromAssemblyContaining<T>()` call would be a false positive.
2. `src/Encina.Analyzers/RegisterHandlerAssemblyCodeFix.cs`: appends `typeof(<handler>).Assembly`.
3. `src/Encina.Analyzers/HandlerNamingAnalyzer.cs` (ENC012) and `RenameHandlerCodeFix.cs` (uses `Renamer.RenameSymbolAsync`).
4. Edit `DiagnosticDescriptors.cs` (two descriptors); tests `HandlerNotDiscoveredAnalyzerTests.cs`, `HandlerNamingAnalyzerTests.cs` (positive, negative, fix output compiles); guard tests added to `EncinaAnalyzersGuardTests.cs`; manifest targets; changelog fragment `changelog.d/343-handler-convention-analyzers.added.md` (through `mechanical-fixer`).

File count 10 (4 production, 1 descriptors edit, 2 tests, 1 guard test edit, 1 manifest, 1 fragment); about 450 lines.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: Encina.Analyzers exists (issue #438) with EncinaKnownTypes and AnalyzerTestHost. Diagnostic ids use the ENC prefix (ENC011 onward belong to this plan).
TASK: Add ENC011 (AddEncina without an assembly in a compilation that has handlers; Warning; fix adds typeof(handler).Assembly) and ENC012 (handler name does not match its request name; Suggestion; rename fix). Accept both <Request>Handler and <Request minus Command/Query suffix>Handler.
KEY RULES: A rule does nothing when Encina types are absent. Never flag a handler in a test project's generated code. Severity configurable through .editorconfig. Methods at complexity 8 or less. At most 15 files.
REFERENCE FILES: src/Encina/Core/ServiceCollectionExtensions.cs, src/Encina/Dispatchers/MediatorAssemblyScanner.cs, src/Encina/Abstractions/CqrsContracts.cs, src/Encina.Analyzers/*.
```

</details>

### Phase 2: Query state and dependency rules (PR-2: ENC013, ENC015)

<details>
<summary>Tasks</summary>

1. `src/Encina.Analyzers/AnalyzerOptionsReader.cs` (`internal static`): reads `encina_max_handler_dependencies` and `encina_query_mutation_prefixes` from `AnalyzerConfigOptions` per syntax tree; invalid values fall back to the defaults (6 and the prefix list).
2. `QueryModifiesStateAnalyzer.cs` (ENC013, heuristic of Design Choice 3) and `TooManyDependenciesAnalyzer.cs` (ENC015, counts the parameters of the single public constructor, or of the primary constructor).
3. Two descriptors, three test files (including the option reader), manifest targets.

File count 9; about 500 lines.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: ENC011 and ENC012 are merged.
TASK: Add ENC013 and ENC015 and the AnalyzerOptionsReader. ENC013 flags a call from an IQueryHandler to a member whose name starts with a configured prefix or to SaveChanges/ExecuteSql on a DbContext, excluding calls on locals and System.Collections types. ENC015 flags constructors above encina_max_handler_dependencies (default 6).
KEY RULES: Invalid .editorconfig values fall back to defaults and never throw. ENC013 defaults to Suggestion. Document the heuristic limits in the descriptor description.
REFERENCE FILES: src/Encina.Analyzers/*, src/Encina/Abstractions/CqrsContracts.cs.
```

</details>

### Phase 3: Response type rule for convention handlers (PR-3: ENC014)

<details>
<summary>Tasks</summary>

1. `ConventionHandlerResponseAnalyzer.cs` (ENC014): for a type that the convention of #333 marks as a handler (`[Handler]` or a class named `*Handler` with a `Handle` method), compare the `Handle` return type (through `EncinaKnownTypes`, which names the carrier) with the response declared by the request type (`IRequest<TResponse>`). Starts after #333 defines the attribute and the convention. If #333's pending Dilemma 1 keeps interface-only handlers, this rule is not built and PR-3 is dropped: the plan then has 3 pull requests and four analyzers (ENC011, ENC012, ENC013, ENC015), while the issue's acceptance reads "At least 5 analyzers implemented", so the acceptance is then met only if the maintainer accepts four (or adds a fifth rule); that is the maintainer's word, not this plan's.
2. One descriptor, one test file, manifest targets. About 4 files and 200 lines.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: Convention-based handlers (issue #333) are merged: the [Handler] attribute and the Handle method convention exist.
TASK: Add ENC014 (Error): the Handle return type does not carry the response the request declares through IRequest<TResponse>.
KEY RULES: Name the result carrier only through EncinaKnownTypes (ADR-064 changes it). Do nothing when the convention types are absent.
REFERENCE FILES: the #333 implementation, src/Encina/Abstractions/IRequestHandler.cs, src/Encina.Analyzers/EncinaKnownTypes.cs.
```

</details>

### Phase 4: Documentation and finalization (PR-4)

<details>
<summary>Tasks</summary>

1. Through `docs-writer`: add the five rules to `docs/features/analyzers.md` (created by #438's Phase 5) with the `.editorconfig` keys and the dropped-rule table of Design Choice 1; `docs/INVENTORY.md` if it lists rules.
2. Verification: `dotnet build --configuration Release` (0 warnings), `dotnet test tests/Encina.UnitTests --filter "FullyQualifiedName~Analyzers"`, the guard project, and the per-flag measurement of AGENTS.md section 9.

About 2 files and 120 lines.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT: ENC011 to ENC015 are merged in Encina.Analyzers.
TASK: Document the five rules through docs-writer and run the verification commands; report the measured per-flag coverage of every new file against .github/coverage-manifest/Encina.Analyzers.json.
KEY RULES: No hand-typed coverage figures (covref markers). English only.
REFERENCE FILES: docs/features/analyzers.md, .claude/skills/encina-docs/SKILL.md.
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard / Specification | Relevance | Where it applies in the feature |
|--------------------------|-----------|---------------------------------|
| CA2016 (Forward the CancellationToken) | Built-in rule that makes ENCINA008 redundant | Design Choice 1 |
| `AnalyzerConfigOptions` (`.editorconfig` keys for analyzers) | Per-rule thresholds and prefix lists | `AnalyzerOptionsReader` |
| ADR-005 (reject source generators) | Rules out the #50 integration line | Design Choice 4 |
| ADR-064 (result union) | The handler return type changes in the 1.0 migration | ENC014 reads the carrier from `EncinaKnownTypes` |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `EncinaAssemblyScanner`, `AddEncina` | `src/Encina/Dispatchers/MediatorAssemblyScanner.cs`, `src/Encina/Core/ServiceCollectionExtensions.cs` | Defines what discovery finds (ENC011) |
| CQRS contracts | `src/Encina/Abstractions/CqrsContracts.cs` | Handler and request symbols |
| `PipelineStage`, `PipelineOrderValidationHostedService` | `src/Encina/Pipeline/Ordering/` | Why ENCINA007 is dropped |
| Analyzer package and harness | `src/Encina.Analyzers/` (#438) | Hosts the rules |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Analyzers` | none | Roslyn diagnostics, no `[LoggerMessage]`; no `EventIdRanges` field |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production | 9 | 5 analyzers, 2 fixes, options reader, descriptors edit |
| Tests | 7 | one file per rule plus the option reader, guard test edit |
| Manifest and fragment | 2 | `Encina.Analyzers.json` edit, changelog fragment |
| Documentation | 2 | analyzers page, INVENTORY |
| **Total** | **~20 over 4 pull requests** | PR-1 10, PR-2 9, PR-3 4, PR-4 2 files; about 450 + 500 + 200 + 120 = 1,270 lines in total |

Coverage targets per file: every analyzer, fix and the options reader unit 85, guard 25. Justification: pure analysis code exercised through `CompilationWithAnalyzers`; the guard flag reaches `Initialize` and the null checks only. Contract, property, integration, load and benchmark: not applicable (no public interface, no complex numeric logic, no infrastructure, compile-time only).

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina is a pre-1.0 .NET 10 library (AGENTS.md rules: zero warnings, XML docs, real-code tests, per-flag coverage). Issue #343 adds five CQRS convention rules, ENC011 to ENC015, to the package Encina.Analyzers created by issue #438.

IMPLEMENTATION OVERVIEW: Phase 1 ENC011 and ENC012 with fixes. Phase 2 ENC013, ENC015 and the options reader. Phase 3 ENC014 after #333. Phase 4 documentation through docs-writer. Four pull requests, each at most 15 files.

KEY PATTERNS: Metadata-name lookup, no-op when absent; severity and thresholds through .editorconfig with safe fallbacks; result carrier named only in EncinaKnownTypes; tests through AnalyzerTestHost; complexity 8 or less per method.

REFERENCE FILES: src/Encina.Analyzers/*, src/Encina/Core/ServiceCollectionExtensions.cs, src/Encina/Dispatchers/MediatorAssemblyScanner.cs, src/Encina/Abstractions/CqrsContracts.cs.
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Compile-time tool |
| 2 | OpenTelemetry | ❌ | The issue defers "warn on missing OpenTelemetry in handlers"; there is no decidable definition of "missing", so no issue is opened unless the maintainer asks; no telemetry is emitted, so ADR-054 tenant tags do not apply |
| 3 | Structured Logging | ❌ | Same reason as OpenTelemetry for the issue's deferred "warn on missing logging"; Roslyn diagnostics are not log events, so no EventId range |
| 4 | Health Checks | ❌ | Build-time tool |
| 5 | Validation | ❌ | The validator rule is ENC002 of #438; this plan adds no validation rule |
| 6 | Resilience | ❌ | No external calls |
| 7 | Distributed Locks | ❌ | Compile-time analysis |
| 8 | Transactions | ❌ | Compile-time analysis |
| 9 | Idempotency | ❌ | The idempotency rule is ENC008 of #438 |
| 10 | Multi-Tenancy | ❌ | Tooling holds no tenant data |
| 11 | Module Isolation | ⏭️ | A rule for cross-module access follows #359 (open), which owns the module data model it would check |
| 12 | Audit Trail | ❌ | Development-time diagnostics |

---

## Next Steps

1. The maintainer answers Dilemmas 2 and 3 when work starts (the id prefix Dilemma of #438 comes first).
2. Wait for #438 PR-1 (package and harness) before briefing Phase 1; wait for #333 (and its Dilemma 1) before Phase 3.
3. Open the sub-issues for PR-1 to PR-4 and brief Phase 1.
