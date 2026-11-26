# Implementation Plan: `Encina.Analyzers` — Roslyn analyzers and code fixes for Encina handlers, sagas and messaging

> **Issue**: [#438](https://github.com/dlrivada/Encina/issues/438)
> **Type**: Feature (new package, developer experience)
> **Complexity**: High by breadth (10 analyzers, 5 code fixes, 5 pull requests), low by runtime risk (compile-time only, no provider, no store)
> **Estimated Scope**: ~2,200 lines of production code + ~1,500 lines of tests + ~250 lines of documentation
> **Family**: dev-experience. **This plan owns the package `Encina.Analyzers`, its test harness, the id prefix and the shared symbol helper.** The plan of #343 (handler discovery and CQRS convention rules) adds its rules to this package and reads the pieces below by issue number.

---

## Summary

Add a dedicated, opt-in Roslyn analyzer package, `Encina.Analyzers`, so the common Encina mistakes fail or warn in the IDE and in CI instead of at runtime: ten rules (ENC001 to ENC010 of the issue) and code fixes for the five most useful, configurable through `.editorconfig`.

What exists today (read on `origin/main` at `326e65ebf`, after `git fetch`):

- One analyzer package exists, `src/Encina.Security.ABAC.Analyzers/` (three files: `DiagnosticDescriptors.cs` with ids `EEL001` to `EEL006`, `RequireConditionAnalyzer.cs`, `RequireConditionCodeFixProvider.cs`). Its project file is the template: `netstandard2.0`, `IsRoslynComponent`, `EnforceExtendedAnalyzerRules`, `IncludeBuildOutput=false`, `DevelopmentDependency=true`, the DLL packed under `analyzers/dotnet/cs`, and `Microsoft.CodeAnalysis.CSharp` and `.Workspaces` (both 5.0.0 in `Directory.Packages.props`, `PrivateAssets="all"`). It has no `PublicAPI.*.txt` and no test: no file under `tests/` references it (open issue #2842 asks for its test project; it also records that `Microsoft.CodeAnalysis.Testing` is not in `Directory.Packages.props`).
- Every project under `src/` ships as a package (`Directory.Build.props`, SPEC-000 REQ-001), so the new project is packed automatically; it is listed in `Encina.slnx` (line 48 holds the ABAC one) and in `.github/scripts/coverage-weights.json` (line 152 holds the ABAC one).
- The symbols the rules look for exist as metadata names: `IRequestHandler<,>` (`src/Encina/Abstractions/IRequestHandler.cs`), `ICommandHandler<,>`, `IQueryHandler<,>`, `ICommand<>`, `IQuery<>` (`CqrsContracts.cs:29-105`), `INotification` and `INotificationHandler<>`, `IPipelineBehavior<,>`, `IIdempotentRequest` (`src/Encina.Messaging/Inbox/InboxPipelineBehavior.cs:111`, an empty marker), `CacheAttribute` (`src/Encina.Caching/Attributes/CacheAttribute.cs:51`), `SagaDefinition<TData>` with a `Compensate` member (`src/Encina.Messaging/Sagas/LowCeremony/SagaDefinition.cs:197`) and `IValidationProvider` (`src/Encina/Validation/IValidationProvider.cs:15`). An analyzer cannot reference those assemblies (it targets `netstandard2.0` and must load in any IDE), so it finds them by metadata name through `Compilation.GetTypeByMetadataName`.
- Startup validation of the pipeline already exists at runtime: `PipelineOrderValidationHostedService` and `PipelineOrderResolver` (`src/Encina/Pipeline/Ordering/`) order behaviors by `PipelineStage`; there is no user-declared "run after" graph, so a literal cycle between behaviors cannot be declared today (see Dilemma 3 for ENC009).
- The handler return type is `Task<Either<EncinaError, TResponse>>` today, and [ADR-064](../architecture/adr/064-rop-result-union.md) replaces `Either<EncinaError, T>` by an Encina-owned result union during the 1.0 migration (#3662). Every rule and fix that names the carrier reads it from one place, `EncinaKnownTypes`, so the migration changes one file here.

What this plan delivers: the package (project, descriptors, `EncinaKnownTypes`), the ten analyzers, the five code fixes (add `CancellationToken`, implement `IIdempotentRequest`, generate a validator, add `[Cache]`, generate a handler skeleton), the in-process test harness, and the documentation page of every rule. Diagnostic ids use the prefix `ENC` (Dilemma 2).

- **Affected packages**: `Encina.Analyzers` (new), `Encina.UnitTests` and `Encina.GuardTests` (a `ProjectReference` and the Roslyn packages), `Encina.slnx`, `.github/scripts/coverage-weights.json`. The core `Encina` package is not changed.
- **Provider category**: none (compile-time tooling; no database, cache, transport, lock or validation provider code).
- **Estimated size**: 5 pull requests, see "Implementation Phases" and the file count table.
- **Prerequisites**: none blocking. #2842 (open, test setup for the ABAC analyzer package) wants the same harness; whichever lands first creates it (Phase 1 owns it if #2842 has not).

### Shared Pieces and Ownership (family dev-experience)

| Shared piece | Owner | Used by |
|--------------|-------|---------|
| Package `Encina.Analyzers`, `DiagnosticDescriptors` partial class, `EncinaKnownTypes`, option reader for `.editorconfig` keys | **This plan (#438)** | #343 (adds ENC011 to ENC018 and its own code fixes) |
| In-process analyzer test harness (`AnalyzerTestHost`) | **This plan** (or #2842, whichever lands first) | #343, #2842 |
| Rule overlap decisions between the two issues (CancellationToken, validator, behavior order, notification without handler) | **This plan** (the table in Dilemma 3) | #343 |
| Rules about convention-based handlers ([Handler], static handlers, naming) | #343 and #333 (sibling plans, by number) | none |

Order: Phase 1 first (it creates the package); #343 starts after Phase 1 merges.

---

## Design Choices

<details>
<summary><strong>1. Package placement and delivery — a separate opt-in package</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) New package `Encina.Analyzers`, referenced with `PrivateAssets="all"`** | Opt-in, no runtime cost, the same shape as `Encina.Security.ABAC.Analyzers`, versioned on its own | One more package to reference |
| **B) Bundle the analyzer DLL inside the `Encina` package** | Every user gets the rules with no extra reference | Violates pay-for-what-you-use: warnings appear in apps that did not ask for them; the core package would carry a `netstandard2.0` Roslyn payload |
| **C) Extend `Encina.Security.ABAC.Analyzers`** | No new project | Wrong name and scope: that package is about EEL expressions of ABAC |

### Chosen Option: **A — New package `Encina.Analyzers`**

### Rationale

- AGENTS.md section 1 makes every feature opt-in; the issue itself names the package.
- **Class:** Settled
- **Settled by:** AGENTS.md section 1 ("pay-for-what-you-use (every feature opt-in)"), the issue's "Package Structure", and the precedent `src/Encina.Security.ABAC.Analyzers/Encina.Security.ABAC.Analyzers.csproj`.

</details>

<details>
<summary><strong>2. Diagnostic id prefix — `ENC` (issue #438) or `ENCINA` (issue #343)</strong></summary>

The two issues of this package name their ids differently (`ENC001` here, `ENCINA001` in #343) and both claim `001`. One package needs one scheme. The existing ABAC package uses a three-letter prefix, `EEL`.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ENC` + 3 digits for every rule of the package (#438 keeps ENC001 to ENC010; #343's rules continue as ENC011 and up)** | Short in `.editorconfig` (`dotnet_diagnostic.ENC001.severity`), matches the three-letter `EEL` precedent, no id collision | #343's text must be renumbered |
| **B) `ENCINA` + 3 digits** | Unambiguous brand | Longer; the issue #438 table and its first PR would be renamed instead |
| **C) Two prefixes in one package** | Both issues keep their text | Two schemes for one package; both start at 001 so the same number means two rules |

### Chosen Option: **A — `ENC` for every rule** (recommended, pending the maintainer)

### Rationale

- A wins when one scheme is wanted and the shorter id matters in `.editorconfig` and suppressions; B wins if the maintainer prefers the full product name in every message. Ids are public once shipped and cannot be renumbered quietly, so the choice is made before Phase 1.
- **Class:** Dilemma

</details>

<details>
<summary><strong>3. Which of the ten rules ship, and how the narrow ones are defined</strong></summary>

Four rules of the issue have no decidable definition as written, and the two issues overlap. The plan fixes a definition for each so a worker can build and test it; whether all ten ship is the question.

| Rule | Definition used by this plan | Overlap with #343 |
|------|------------------------------|-------------------|
| ENC001 | A class implementing `IRequestHandler<,>` (or a derived handler interface) whose `Handle` method has no `CancellationToken` parameter. Warning. | #343 ENCINA008 (token not propagated) is a different rule: it becomes ENC011 there |
| ENC002 | A request type (`ICommand<>` or `IQuery<>`) with no class named `<Request>Validator` and none deriving from or implementing a validator of that request in the same compilation (`AbstractValidator<T>` by metadata name, or `IValidator<T>`). Warning, off when no validation package is referenced. | #343 ENCINA005 is the same rule; it is dropped there |
| ENC003 | A `SagaDefinition<TData>` fluent chain with a step that has no `.Compensate(...)` while a later step can fail. Error. | none |
| ENC004 | A `Publish` call whose notification type has no `INotificationHandler<>` implementation in the compilation. Warning. | none |
| ENC005 | `Task.Result`, `.Wait()`, `.GetAwaiter().GetResult()` inside a handler or behavior method. Error. | none |
| ENC006 | A query handler (`IQueryHandler<,>`) with no `[Cache]` whose body awaits a call on a type deriving `DbContext`, `IDbConnection` or `DbCommand`. Info. A heuristic: "slow" is not decidable at compile time. | none |
| ENC007 | `UseOutbox = true` assigned in an `AddEncinaMessaging`-style options lambda with no `MaxRetries` assignment in the same lambda. Warning. A heuristic on configuration code. | none |
| ENC008 | A request handled through the inbox (the project references `Encina.Messaging`) whose type implements neither `IIdempotentRequest` nor carries an idempotency key by design. Warning. | none |
| ENC009 | A pipeline behavior that calls `Send` with the same `TRequest` it handles (unbounded recursion). Error. A literal cycle between behaviors cannot be declared today (the stage resolver orders by `PipelineStage`). | #343 ENCINA007 (behavior order) is about stage order, which the runtime resolver already validates; it is dropped there |
| ENC010 | An `ICommandHandler<,>` whose `TResponse` is not `Unit`. The default severity is the question (see below). | none |

ENC010 collides with the library's own design: `ICommand<TResponse>` returning data is the normal Encina command (both issues' examples are `ICommandHandler<CreateOrderCommand, OrderDto>`), so a Warning would fire on most commands.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Ship all ten with the definitions above; ENC006 and ENC007 documented as heuristics; ENC010 default severity Suggestion (hidden in the build, visible in the IDE)** | Meets the issue's acceptance ("all 10 analyzers"); no noise on the common command shape | Two heuristic rules can miss or over-report; a Suggestion rule is easy to ignore |
| **B) Ship the eight decidable rules now; defer ENC006 and ENC007 to issues after real usage shows a better definition; ENC010 as in A** | No heuristic noise in a first release | Misses the acceptance criterion until the follow-ups land |
| **C) Ship all ten at the issue's severities (ENC010 Warning)** | Matches the issue text | ENC010 warns on the library's own command pattern; users disable it on day one |

### Chosen Option: **A — Ship all ten, heuristics documented, ENC010 as Suggestion** (recommended, pending the maintainer)

### Rationale

- A wins when meeting the acceptance criteria matters more than a first-release false-positive rate; B wins if the maintainer would rather ship fewer, sharper rules. Severity stays configurable through `.editorconfig` in every option.
- **Class:** Dilemma

</details>

<details>
<summary><strong>4. How the tests run — project reference from the consolidated test projects</strong></summary>

The analyzer targets `netstandard2.0`; the tests need a way to compile a source snippet and run an analyzer over it. #2842 (open) records that `Microsoft.CodeAnalysis.Testing` is not in `Directory.Packages.props` and lists both routes.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) In-process helper `AnalyzerTestHost`: build a `CSharpCompilation` from a snippet plus metadata references, run `CompilationWithAnalyzers`, apply a code fix through an `AdhocWorkspace` (`Microsoft.CodeAnalysis.CSharp.Workspaces` 5.0.0, already pinned); tests live in `Encina.UnitTests` and `Encina.GuardTests`** | No new package pin; the coverage flags come from the existing consolidated projects, as `coverage-report.cs` classifies by project name | The helper (about 120 lines) is ours to maintain; fix-all and batch behavior are not covered by a library |
| **B) Add `Microsoft.CodeAnalysis.CSharp.Analyzer.Testing` and `.CodeFix.Testing` to `Directory.Packages.props` and a new test project** | Maintained verifier with markup (`{|ENC001:...|}`) | A new pin (exact version, clean restore audit); a project name `coverage-report.cs` does not classify as a flag; transitive pins may be needed (ADR-060) |

### Chosen Option: **A — In-process helper in the consolidated test projects**

### Rationale

- AGENTS.md section 9 fixes the test projects and the flag names `coverage-report.cs` classifies; a new test project would have no flag. A also avoids a new package pin.
- **Class:** Settled
- **Settled by:** AGENTS.md section 9 ("Test projects (consolidated, one per type, under `tests/`)", "Coverage is per flag"), and #2842's own "or an in-process `CSharpCompilation` plus `CompilationWithAnalyzers` helper if the project prefers no new package".

</details>

<details>
<summary><strong>5. Which packages' symbols a rule may depend on</strong></summary>

ENC004 to ENC008 need types from `Encina.Messaging`, `Encina.Caching` and validation packages that the analyzed project may not reference.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Look every type up by metadata name; when the type is missing, the rule does nothing (`CompilationStartAction` returns without registering)** | Works with any subset of Encina packages; no assembly reference from the analyzer | A renamed type silently disables its rule unless a test pins the names |
| **B) Reference the Encina assemblies from the analyzer** | Compile-time-checked names | Impossible for a `netstandard2.0` analyzer that must load in every IDE; couples versions |

### Chosen Option: **A — Metadata names, no-op when absent**

### Rationale

- A guard test in the unit suite resolves each name in `EncinaKnownTypes` against the real assemblies (`typeof(IRequestHandler<,>).FullName` and siblings), so a rename fails a test.
- **Class:** Trivial
- **Settled by:** the precedent `RequireConditionAnalyzer.cs` (metadata-name lookup on `SyntaxKind.Attribute`) and the `netstandard2.0` target of `Encina.Security.ABAC.Analyzers.csproj`.

</details>

---

## Implementation Phases

### Phase 1: Package, symbols and test harness (PR-1, ENC001 and its fix)

<details>
<summary>Tasks</summary>

1. New `src/Encina.Analyzers/Encina.Analyzers.csproj`: copy of the ABAC analyzer project file (`netstandard2.0`, `IsRoslynComponent`, `EnforceExtendedAnalyzerRules`, `DevelopmentDependency`, pack under `analyzers/dotnet/cs`), new `ProjectGuid`, `PackageId` `Encina.Analyzers`.
2. `src/Encina.Analyzers/DiagnosticDescriptors.cs` (`public static partial class`, one `DiagnosticDescriptor` per rule, `HelpLinkUri` to the rule page, `customTags: WellKnownDiagnosticTags.Telemetry` omitted): ENC001 first.
3. `src/Encina.Analyzers/EncinaKnownTypes.cs` (`internal sealed class`): resolves `IRequestHandler<,>`, `ICommandHandler<,>`, `IQueryHandler<,>`, `ICommand<>`, `IQuery<>`, `INotification`, `INotificationHandler<>`, `IPipelineBehavior<,>`, `IIdempotentRequest`, `CacheAttribute`, `SagaDefinition<>` and the result carrier by metadata name; `TryCreate(Compilation, out EncinaKnownTypes)`.
4. `src/Encina.Analyzers/HandlerCancellationTokenAnalyzer.cs` (`[DiagnosticAnalyzer(LanguageNames.CSharp)]`, `Initialize(AnalysisContext)` with `ConfigureGeneratedCodeAnalysis(None)` and `EnableConcurrentExecution()`, `ArgumentNullException.ThrowIfNull(context)`): `RegisterCompilationStartAction` then `RegisterSymbolAction` on `SymbolKind.Method` for `Handle`.
5. `src/Encina.Analyzers/AddCancellationTokenCodeFix.cs` (`CodeFixProvider`, `FixableDiagnosticIds` = ENC001): adds `CancellationToken cancellationToken` as the last parameter and updates the single call chain it can see.
6. `src/Encina.Analyzers/README.md` (package readme, rule table).
7. `tests/Encina.UnitTests/Analyzers/AnalyzerTestHost.cs` (snippet to compilation to diagnostics, and code-fix application), `tests/Encina.UnitTests/Analyzers/HandlerCancellationTokenAnalyzerTests.cs` (positive, negative, the fix), `tests/Encina.GuardTests/Analyzers/EncinaAnalyzersGuardTests.cs` (`Initialize(null)`, `EncinaKnownTypes.TryCreate(null, ...)`, the name-pinning test of Design Choice 5).
8. Edit `tests/Encina.UnitTests/Encina.UnitTests.csproj` and `tests/Encina.GuardTests/Encina.GuardTests.csproj`: `ProjectReference` to the analyzer project and the two Roslyn `PackageReference`s (versions come from `Directory.Packages.props`, no new pin).
9. Edit `Encina.slnx` and `.github/scripts/coverage-weights.json` (one entry each); new `.github/coverage-manifest/Encina.Analyzers.json` (through `mechanical-fixer`); changelog fragment `changelog.d/438-roslyn-analyzers.added.md` (through `mechanical-fixer`).

File count of PR-1: 14 (csproj, descriptors, known types, analyzer, fix, README, three test files, two test csproj, slnx, weights, manifest) plus the fragment = 15 files; about 650 lines.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: Encina is a pre-1.0 .NET 10 library. src/Encina.Security.ABAC.Analyzers is the template for a Roslyn analyzer package (netstandard2.0). Rules are in AGENTS.md; no backward compatibility, zero warnings, XML docs on every public type.
TASK: Create src/Encina.Analyzers with the descriptor class, EncinaKnownTypes, the ENC001 analyzer and its code fix, the in-process test host and tests (unit and guard), and register the project in Encina.slnx and coverage-weights.json. ENC001: handler Handle method without a CancellationToken parameter, Warning.
KEY RULES: Look types up by metadata name and do nothing when absent. Never read DateTime.UtcNow. Tests execute the real analyzer through CompilationWithAnalyzers (no reflection-only tests). Guard tests cover Initialize(null) and TryCreate(null). Keep every method at cyclomatic complexity 8 or less (CRAP gate). PR at most 15 files and 1500 lines.
REFERENCE FILES: src/Encina.Security.ABAC.Analyzers/*, src/Encina/Abstractions/IRequestHandler.cs, src/Encina/Abstractions/CqrsContracts.cs, Directory.Packages.props, tests/Encina.UnitTests/Encina.UnitTests.csproj.
```

</details>

### Phase 2: Synchronous and recursion rules (PR-2: ENC004, ENC005, ENC009, ENC010)

<details>
<summary>Tasks</summary>

1. `NotificationWithoutHandlerAnalyzer.cs` (ENC004): `CompilationStartAction` collects every `INotificationHandler<T>` implementer, an end action reports `Publish` invocations whose argument type has none.
2. `SyncOverAsyncAnalyzer.cs` (ENC005): `Task.Result`, `.Wait()`, `.GetAwaiter().GetResult()` inside methods of handler and behavior types.
3. `RecursiveBehaviorAnalyzer.cs` (ENC009) and `CommandReturnsDataAnalyzer.cs` (ENC010, default severity per Design Choice 3).
4. Four descriptors added to `DiagnosticDescriptors.cs`; four unit test files (one per analyzer) in `tests/Encina.UnitTests/Analyzers/`; manifest targets.

File count: 4 analyzers, 1 descriptors edit, 4 tests, 1 manifest edit = 10 files; about 700 lines.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: Phase 1 created src/Encina.Analyzers with EncinaKnownTypes and the AnalyzerTestHost.
TASK: Add ENC004, ENC005, ENC009 and ENC010 as defined in the plan (Design Choice 3), each with positive, negative and no-package-referenced tests.
KEY RULES: The rule does nothing when its type is absent from the compilation. Severity is configurable through .editorconfig (dotnet_diagnostic.ENCxxx.severity); never hard-code behavior that ignores it. Methods at complexity 8 or less. At most 15 files.
REFERENCE FILES: src/Encina.Analyzers/*, src/Encina/Abstractions/INotification.cs, src/Encina/Abstractions/INotificationHandler.cs, src/Encina/Abstractions/IEncina.cs, src/Encina/Pipeline/PipelineBuilder.cs.
```

</details>

### Phase 3: Rules with code fixes (PR-3: ENC002, ENC006, ENC008 and the generate-handler fix)

<details>
<summary>Tasks</summary>

1. `MissingValidatorAnalyzer.cs` (ENC002) and `GenerateValidatorCodeFix.cs` (creates `<Request>Validator`; the base class depends on which validation package the project references: FluentValidation `AbstractValidator<T>`, otherwise a DataAnnotations-style class is not generated and the fix is offered only for FluentValidation).
2. `MissingCacheAttributeAnalyzer.cs` (ENC006) and `AddCacheAttributeCodeFix.cs` (adds `[Cache]` with the defaults of `CacheAttribute`).
3. `NonIdempotentInboxRequestAnalyzer.cs` (ENC008) and `ImplementIdempotentRequestCodeFix.cs`.
4. `GenerateHandlerCodeFix.cs`: a `CodeRefactoringProvider` on a command or query type that adds a handler skeleton in the same file (a refactoring, not a diagnostic fix, as the issue lists it separately).
5. Three descriptors, four test files, manifest targets.

File count: 3 analyzers, 4 fixes and refactorings, 1 descriptors edit, 4 tests, 1 manifest = 13 files; about 1,100 lines.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: src/Encina.Analyzers exists with ENC001, ENC004, ENC005, ENC009, ENC010.
TASK: Add ENC002, ENC006 and ENC008 with their code fixes and the generate-handler refactoring. Fixes must produce code that compiles; tests apply each fix and compile the result.
KEY RULES: The handler skeleton and validator return types come from EncinaKnownTypes (the result carrier changes under ADR-064). Fixes never touch files outside the document. At most 15 files and 1500 lines.
REFERENCE FILES: src/Encina.Analyzers/*, src/Encina.Caching/Attributes/CacheAttribute.cs, src/Encina.Messaging/Inbox/InboxPipelineBehavior.cs, src/Encina/Validation/IValidationProvider.cs.
```

</details>

### Phase 4: Saga and outbox configuration rules (PR-4: ENC003, ENC007)

<details>
<summary>Tasks</summary>

1. `SagaCompensationAnalyzer.cs` (ENC003) walking the `SagaDefinition<TData>` fluent chain; `OutboxRetryConfigurationAnalyzer.cs` (ENC007) on the options lambda.
2. Two descriptors, two test files, manifest targets. About 8 files and 600 lines.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT: src/Encina.Analyzers has eight rules. ENC003 and ENC007 inspect fluent and configuration code.
TASK: Add ENC003 (step without Compensate in a saga chain, Error) and ENC007 (UseOutbox assigned with no MaxRetries in the same lambda, Warning).
KEY RULES: Both must tolerate chains built across statements by doing nothing (no false positive). Tests include the negative case for every tolerated shape.
REFERENCE FILES: src/Encina.Messaging/Sagas/LowCeremony/SagaDefinition.cs, src/Encina.Messaging/Outbox/OutboxOptions.cs, src/Encina.Analyzers/*.
```

</details>

### Phase 5: Documentation and finalization (PR-5)

<details>
<summary>Tasks</summary>

1. Through `docs-writer`: `docs/features/analyzers.md` (one section per rule: what it flags, bad and good example, the `.editorconfig` key, how to suppress), update `docs/INVENTORY.md` and `ROADMAP.md`, the package README (already in PR-1, completed here).
2. Verification: `dotnet build --configuration Release` (0 warnings), `dotnet test tests/Encina.UnitTests --filter "FullyQualifiedName~Analyzers"` and the guard project, the per-flag coverage run of AGENTS.md section 9.

About 4 files and 350 lines of documentation.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT: The ten rules and five fixes of src/Encina.Analyzers are merged.
TASK: Write docs/features/analyzers.md and update INVENTORY.md and ROADMAP.md through docs-writer; run the verification commands and report the measured per-flag coverage of every new file against the manifest.
KEY RULES: Never type coverage figures by hand (covref markers). English only. Follow .claude/skills/encina-docs/SKILL.md.
REFERENCE FILES: docs/features/abac, docs/INVENTORY.md, .github/coverage-manifest/Encina.Analyzers.json.
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard / Specification | Relevance | Where it applies in the feature |
|--------------------------|-----------|---------------------------------|
| Roslyn `DiagnosticAnalyzer` and `CodeFixProvider` APIs (Microsoft.CodeAnalysis 5.0.0) | The only supported way to ship compile-time rules for every IDE | All analyzers and fixes |
| Analyzer release tracking (`AnalyzerReleases.Shipped.md` / `Unshipped.md`, RS2000 family) | Rule ids are public once shipped | Decision at Phase 1 whether the project turns the tracking on (the ABAC project does not) |
| `.editorconfig` severity keys (`dotnet_diagnostic.<id>.severity`) | Configurable severity is an acceptance criterion | Every descriptor is enabled by default with a configurable severity |
| ADR-064 (result union) | The handler return type changes during the 1.0 migration | `EncinaKnownTypes` is the single place that names the carrier |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| ABAC analyzer package | `src/Encina.Security.ABAC.Analyzers/` | Project file template, descriptor and code fix style |
| Handler and request contracts | `src/Encina/Abstractions/IRequestHandler.cs`, `CqrsContracts.cs`, `INotification.cs` | Metadata names the rules match |
| `IIdempotentRequest`, `CacheAttribute`, `SagaDefinition<TData>` | `src/Encina.Messaging/Inbox/InboxPipelineBehavior.cs`, `src/Encina.Caching/Attributes/CacheAttribute.cs`, `src/Encina.Messaging/Sagas/LowCeremony/SagaDefinition.cs` | ENC008, ENC006, ENC003 |
| Pipeline stage validation | `src/Encina/Pipeline/Ordering/PipelineOrderValidationHostedService.cs` | Why ENC009 is about recursion, not stage order |
| Central package versions | `Directory.Packages.props` (`Microsoft.CodeAnalysis.CSharp` 5.0.0, `.Workspaces` 5.0.0) | No new pin |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Analyzers` | none | Compile-time diagnostics are Roslyn diagnostics, not `[LoggerMessage]` log events; no `EventIdRanges` field, no registry row, no `AssemblyRanges` entry |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production (`src/Encina.Analyzers`) | 22 | csproj, descriptors, known types, 10 analyzers, 5 fixes or refactorings, README, plus helper files |
| Tests | 17 | harness, 10 analyzer test files, fix tests inside them, guard test file, 2 csproj edits |
| Manifest, weights, slnx | 3 | `Encina.Analyzers.json`, `coverage-weights.json`, `Encina.slnx` |
| Documentation and fragment | 5 | `docs/features/analyzers.md`, INVENTORY, ROADMAP, README, `changelog.d/438-roslyn-analyzers.added.md` |
| **Total** | **~47 over 5 pull requests** | PR-1 15, PR-2 10, PR-3 13, PR-4 8, PR-5 5 files; about 650 + 700 + 1,100 + 600 + 350 = 3,400 lines in total (each pull request is under 1,500) |

Coverage targets per file (unit and guard are the flags that apply; no contract, property or integration flag, see below): every analyzer and fix file unit 85 and guard 25. Justification: the analyzer logic is pure and fully testable through `CompilationWithAnalyzers`; the guard flag reaches only `Initialize` and the null checks, as the ABAC package's guard target of 20 does. `DiagnosticDescriptors.cs` and `EncinaKnownTypes.cs`: unit 90, guard 25 (static data and one factory with a null check). Contract, property and integration tests are justified as not applicable: no public interface is added, there is no complex numeric logic, and no database or transport is involved. Load and benchmark: not applicable (compile-time, per-compilation cost is measured once by running the analyzers over `Encina.slnx` in Phase 5).

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina is a pre-1.0 .NET 10 / C# 14 library (AGENTS.md rules apply: zero warnings, XML docs, tests execute real code, per-flag coverage targets from .github/coverage-manifest). Issue #438 adds the opt-in package Encina.Analyzers (netstandard2.0 Roslyn analyzers and code fixes). Issue #343 later adds ENC011 and up to the same package.

IMPLEMENTATION OVERVIEW: Phase 1 package, EncinaKnownTypes, test host, ENC001 and its fix. Phase 2 ENC004, ENC005, ENC009, ENC010. Phase 3 ENC002, ENC006, ENC008, generate-handler refactoring. Phase 4 ENC003, ENC007. Phase 5 docs through docs-writer. Five pull requests, each at most 15 files and 1500 lines, each building and passing alone.

KEY PATTERNS: Metadata-name lookup with no-op when absent; CompilationStartAction; severity through .editorconfig; the result carrier named only in EncinaKnownTypes (ADR-064); tests through AnalyzerTestHost in Encina.UnitTests and Encina.GuardTests; cyclomatic complexity 8 or less per method.

REFERENCE FILES: src/Encina.Security.ABAC.Analyzers/*, src/Encina/Abstractions/IRequestHandler.cs, src/Encina/Abstractions/CqrsContracts.cs, src/Encina.Messaging/Sagas/LowCeremony/SagaDefinition.cs, docs/plans/dsr-implementation-plan-404.md (plan style).
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Compile-time tool; it only suggests `[Cache]` (ENC006) |
| 2 | OpenTelemetry | ❌ | No runtime component to trace; ADR-054 tenant tags do not apply (no telemetry is emitted) |
| 3 | Structured Logging | ❌ | Roslyn diagnostics are the output; no `[LoggerMessage]`, so no EventId range |
| 4 | Health Checks | ❌ | Not a service |
| 5 | Validation | ✅ | ENC002 checks a validator exists for each request, a compile-time validation of the validation setup |
| 6 | Resilience | ❌ | No external calls; ENC007 only checks outbox retry configuration in the analyzed code |
| 7 | Distributed Locks | ❌ | Single-process compilation |
| 8 | Transactions | ❌ | No data operations |
| 9 | Idempotency | ✅ | ENC008 and its fix implement `IIdempotentRequest` |
| 10 | Multi-Tenancy | ❌ | Tooling holds no tenant data; the telemetry tenant rule (ADR-054 D4) does not apply |
| 11 | Module Isolation | ⏭️ | A cross-module access rule belongs to #359 (open), which owns the module data model it would check |
| 12 | Audit Trail | ❌ | Development-time diagnostics, nothing to audit |

---

## Next Steps

1. The maintainer answers Dilemmas 2 and 3 when work starts; Phase 1 does not start before Dilemma 2 (ids are public).
2. Open the sub-issues for PR-1 to PR-5 (each within 15 files and 1500 lines) and brief Phase 1.
3. Coordinate with #2842 (the ABAC package needs the same harness) and #343 (starts after Phase 1).
4. Existing finding to track separately: `Encina.Security.ABAC.Analyzers` has coverage targets and no tests (#2842, open).
