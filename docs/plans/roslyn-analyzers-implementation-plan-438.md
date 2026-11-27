# Implementation Plan: `Encina.Analyzers` — Roslyn analyzers and code fixes for Encina handlers, sagas and messaging

> **Issue**: [#438](https://github.com/dlrivada/Encina/issues/438)
> **Type**: Feature (new package, developer experience)
> **Complexity**: High by breadth (10 analyzers, 5 code fixes, 5 pull requests), low by runtime risk (compile-time only, no provider, no store)
> **Estimated Scope**: ~3,300 lines over 5 pull requests (production code, tests and documentation, summed per pull request in "Estimated File Count")
> **Family**: dev-experience. **This plan owns the package `Encina.Analyzers`, its test harness, the id prefix and the shared symbol helper.** The plan of #343 (handler discovery and CQRS convention rules) adds its rules to this package and reads the pieces below by issue number.

---

## Summary

Add a dedicated, opt-in Roslyn analyzer package, `Encina.Analyzers`, so the common Encina mistakes fail or warn in the IDE and in CI instead of at runtime: ten rules (ENC001 to ENC010 of the issue) and code fixes for the most useful, configurable through `.editorconfig`. The issue's ENC001 ("handler `Handle` without a `CancellationToken`") cannot fire on an interface handler, because `IRequestHandler<,>.Handle` already requires the token (`src/Encina/Abstractions/IRequestHandler.cs:57`; a class without it does not compile, CS0535). Design Choice 6 redefines the rule and decides what happens to its fix.

What exists today (read on `origin/main` at `b5114e7e5`, after `git fetch`):

- One analyzer package exists, `src/Encina.Security.ABAC.Analyzers/` (three files: `DiagnosticDescriptors.cs` with ids `EEL001` to `EEL006`, `RequireConditionAnalyzer.cs`, `RequireConditionCodeFixProvider.cs`). Its project file is the template: `netstandard2.0`, `IsRoslynComponent`, `EnforceExtendedAnalyzerRules`, `IncludeBuildOutput=false`, `DevelopmentDependency=true`, the DLL packed under `analyzers/dotnet/cs`, and `Microsoft.CodeAnalysis.CSharp` and `.Workspaces` (both 5.0.0 in `Directory.Packages.props`, `PrivateAssets="all"`). It has no `PublicAPI.*.txt` and no test: no file under `tests/` references it (open issue #2842 asks for its test project; it also records that `Microsoft.CodeAnalysis.Testing` is not in `Directory.Packages.props`).
- Every project under `src/` ships as a package (`Directory.Build.props`, SPEC-000 REQ-001), so the new project is packed automatically; it is listed in `Encina.slnx` (line 48 holds the ABAC one) and in `.github/scripts/coverage-weights.json` (line 152 holds the ABAC one).
- The symbols the rules look for exist as metadata names: `IRequestHandler<,>` (`src/Encina/Abstractions/IRequestHandler.cs`), `ICommandHandler<,>`, `IQueryHandler<,>`, `ICommand<>`, `IQuery<>` (`CqrsContracts.cs:29-105`), `INotification` and `INotificationHandler<>`, `IPipelineBehavior<,>`, `IIdempotentRequest` (`src/Encina.Messaging/Inbox/InboxPipelineBehavior.cs:111`, an empty marker), `CacheAttribute` (`src/Encina.Caching/Attributes/CacheAttribute.cs:51`), `SagaDefinition<TData>` with a `Compensate` member (`src/Encina.Messaging/Sagas/LowCeremony/SagaDefinition.cs:197`) and `IValidationProvider` (`src/Encina/Validation/IValidationProvider.cs:15`). An analyzer cannot reference those assemblies (it targets `netstandard2.0` and must load in any IDE), so it finds them by metadata name through `Compilation.GetTypeByMetadataName`.
- Startup validation of the pipeline already exists at runtime: `PipelineOrderValidationHostedService` and `PipelineOrderResolver` (`src/Encina/Pipeline/Ordering/`) order behaviors by `PipelineStage`; there is no user-declared "run after" graph, so a literal cycle between behaviors cannot be declared today (see Dilemma 3 for ENC009).
- The handler return type is `Task<Either<EncinaError, TResponse>>` today, and [ADR-064](../architecture/adr/064-rop-result-union.md) replaces `Either<EncinaError, T>` by an Encina-owned result union during the 1.0 migration (#3662). Every rule and fix that names the carrier reads it from one place, `EncinaKnownTypes`, so the migration changes one file here.

What this plan delivers: the package (project, descriptors, `EncinaKnownTypes`), the ten analyzers, four code fixes (implement `IIdempotentRequest`, generate a validator, add `[Cache]`, generate a handler skeleton; the issue's fifth fix, "add `CancellationToken`", has no diagnostic to attach to, Design Choice 6), the in-process test harness, and the documentation page of every rule. Diagnostic ids use the prefix `ENC` (Dilemma 2).

- **Affected packages**: `Encina.Analyzers` (new), `Encina.UnitTests` and `Encina.GuardTests` (a `ProjectReference` and the Roslyn packages), `Encina.slnx`, `.github/scripts/coverage-weights.json`. The core `Encina` package is not changed.
- **Release tracking**: the project does not use `AnalyzerReleases.Shipped.md` / `AnalyzerReleases.Unshipped.md` (Research table). No such file exists anywhere in the repository, so no file is added or dropped in any pull request.
- **Provider category**: none (compile-time tooling; no database, cache, transport, lock or validation provider code).
- **Estimated size**: 5 pull requests, see "Implementation Phases" and the file count table.
- **Prerequisites**: no code prerequisite. #2842 (open, test setup for the ABAC analyzer package) wants the same harness; whichever lands first creates it (Phase 1 owns it if #2842 has not). Decision gate: the maintainer answers Dilemma 2 (id prefix) before Phase 1 starts, because ids are public once shipped; Dilemmas 3, 4 and 6 are answered before the phase that builds the affected rule.

### Shared Pieces and Ownership (family dev-experience)

| Shared piece | Owner | Used by |
|--------------|-------|---------|
| Package `Encina.Analyzers`, `DiagnosticDescriptors` partial class, `EncinaKnownTypes`, option reader for `.editorconfig` keys | **This plan (#438)** | #343 (adds ENC011 to ENC015 and its own code fixes; its ENC011 and ENC014 start only after PR-1 of this plan) |
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
| ENC001 | Redefined by Design Choice 6 (the issue's definition cannot fire): recommended, the `CancellationToken` parameter of a handler's `Handle` is never referenced in the method body. Warning. | #343 dropped its ENCINA008 (token not propagated) as a duplicate of the SDK analyzer CA2016, see its plan; ENC001 flags a token never used at all, which CA2016 does not |
| ENC002 | A request type (`ICommand<>` or `IQuery<>`) with no class named `<Request>Validator` and none deriving from or implementing a validator of that request in the same compilation (`AbstractValidator<T>` by metadata name, or `IValidator<T>`). Warning, off when no validation package is referenced. | #343 ENCINA005 is the same rule; it is dropped there |
| ENC003 | A `SagaDefinition<TData>` fluent chain with a step that has no `.Compensate(...)` while a later step can fail. `SagaStepBuilder` allows steps without compensation on purpose, so a step is exempt when it is read-only: its `Execute` lambda assigns no saga data member and calls no `Send`, `Publish` or `await` on an external service (a syntactic test; anything not provably read-only is reported). Severity Warning, not Error, because the exemption is a heuristic; the diagnostic message names the step. | none |
| ENC004 | A `Publish` call whose notification type is declared in the compilation and has no `INotificationHandler<>` implementation in it. Tolerance: when the notification type is declared outside the compilation (a referenced assembly), or when any `INotificationHandler<>` implementer in the compilation is open generic or abstract, the rule does nothing, because handlers may live in another assembly. A library that declares and publishes a notification whose handler lives in the consuming application is the same case: the rule is reported at Info (not Warning) when the project is a library (`OutputKind.DynamicallyLinkedLibrary`), and the diagnostic can be suppressed with the standard `#pragma` or `.editorconfig` severity setting. Warning in an executable project. | none |
| ENC005 | `Task.Result`, `.Wait()`, `.GetAwaiter().GetResult()` inside a handler or behavior method. Error. | none |
| ENC006 | A query handler (`IQueryHandler<,>`) whose request type (the first type argument, which `QueryCachingPipelineBehavior` reads) has no `[Cache]` (`CacheAttribute` is `AttributeTargets.Class` and is read from the request, so an attribute on the handler enables nothing) and whose handler body awaits a call on a type deriving `DbContext`, `IDbConnection` or `DbCommand`. The report is on the request type. The fix adds `[Cache]` to the request declaration and is offered only when that declaration is in the same document as the diagnostic location; when the request is declared in another document the diagnostic is reported without a fix, because fixes never touch other files. Info. A heuristic: "slow" is not decidable at compile time. | none |
| ENC007 | `UseOutbox = true` assigned in an `AddEncinaMessaging`-style options lambda with no `MaxRetries` assignment in the same lambda. Warning. A heuristic on configuration code. | none |
| ENC008 | Exact test: the project references `Encina.Messaging` (`IIdempotentRequest` resolves), and a request type (`ICommand<>` or `IQuery<>` implementer) declared in the compilation does not implement `IIdempotentRequest` but declares a public property named `IdempotencyKey`, `MessageId` or `RequestId`, which shows the author intended deduplication. The inbox behavior only deduplicates requests that implement the marker (`src/Encina.Messaging/Inbox/InboxPipelineBehavior.cs:50`), and it reads the key from `IRequestContext.IdempotencyKey`, not from the request property (lines 57 and 73), so the fix is partial: implementing the marker does not propagate the key, and the diagnostic message and fix title say that the caller must still set `IdempotencyKey` on the context (manual configuration; without it the inbox returns an error for a nested dispatch). A request with no such property is never flagged (most commands are not idempotent by design). A heuristic. Warning. | none |
| ENC009 | A pipeline behavior that calls `Send` with the same `TRequest` it handles and on a path with no guard (no enclosing `if`, `switch` or early `return` that tests request state or a flag), which proves nothing terminates the recursion. A behavior that marks the request and re-sends it only while the mark is absent is a valid terminating pattern and is not reported. Severity Warning, because termination is not decidable in general. A literal cycle between behaviors cannot be declared today (the stage resolver orders by `PipelineStage`). | #343 ENCINA007 (behavior order) is about stage order, which the runtime resolver already validates; it is dropped there |
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

### Chosen Option: **A — In-process helper in the consolidated test projects** (recommended, pending the maintainer)

### Rationale

- AGENTS.md section 9 fixes the test projects and the flag names `coverage-report.cs` classifies; a new test project would have no flag, and A also avoids a new package pin. B is still valid if the maintainer wants the maintained verifier with markup and accepts a new pin, with its tests placed in the consolidated projects as well; #2842 lists both routes, so no rule excludes either.
- **Class:** Dilemma

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
- **Settled by:** the precedent `RequireConditionAnalyzer.cs` (metadata-name lookup on `SyntaxKind.Attribute`) and the `netstandard2.0` target of `Encina.Security.ABAC.Analyzers.csproj` show B cannot work for a package that must load in every IDE.

</details>

<details>
<summary><strong>6. ENC001: the issue's rule cannot fire, what replaces it</strong></summary>

`IRequestHandler<TRequest, TResponse>.Handle(TRequest, CancellationToken)` (`src/Encina/Abstractions/IRequestHandler.cs:57`) requires the token, so a class implementing the interface without it does not compile (CS0535). The only places a "`Handle` without a token" rule could fire are a convention handler (#333, not built, and its finder treats a missing token as an unsupported shape) or an unrelated `Handle` overload, both of which are noise or dependent. The #343 plan drops its own token rule for the same family of reason (CA2016 covers propagation).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Redefine ENC001: the `CancellationToken` parameter of a handler's `Handle` is never referenced in the body (neither passed on nor observed). Warning. No code fix (the fix "pass the token to calls" is CA2016's)** | Can fire today on code that compiles; independent of #333; a real defect (cancellation silently ignored) | The issue's "add CancellationToken" fix disappears: four code fixes instead of five, and the acceptance "fixes for the top 5" needs the maintainer's word |
| **B) ENC001 flags a class that looks like a convention handler (public `Handle(request, ...)` method on a type that implements nothing) without a `CancellationToken`, because #333's discovery would skip it; its fix adds the parameter** | Keeps the issue's fix and meaning | Depends on #333 and on its Dilemma 1; useless if #333 is not built or keeps interface-only handlers |
| **C) Drop ENC001 and keep ids ENC002 to ENC010** | No vacuous rule | A gap in the id sequence and nine rules against the issue's ten |

### Chosen Option: **A — Redefine as "token parameter never used"** (recommended, pending the maintainer)

### Rationale

- A wins when a rule that fires today matters more than the issue's literal fix list; B wins if #333 is accepted and the maintainer wants the fix; C wins if the maintainer prefers fewer rules. Phase 1 builds A; the choice is made before Phase 1 starts because the first pull request and the acceptance mapping depend on it.
- **Class:** Dilemma

</details>

---

## Implementation Phases

### Phase 1: Package, symbols and test harness (PR-1, ENC001 as redefined by Design Choice 6)

<details>
<summary>Tasks</summary>

1. New `src/Encina.Analyzers/Encina.Analyzers.csproj`: copy of the ABAC analyzer project file (`netstandard2.0`, `IsRoslynComponent`, `EnforceExtendedAnalyzerRules`, `DevelopmentDependency`, pack under `analyzers/dotnet/cs`), new `ProjectGuid`, `PackageId` `Encina.Analyzers`, and `InternalsVisibleTo` items for `Encina.UnitTests` and `Encina.GuardTests` (`EncinaKnownTypes` is `internal`, and its guard test calls it from `Encina.GuardTests`; the ABAC csproj has none, so this is new; it is an item in the same csproj, no extra file). Release tracking stays off, as in the ABAC project, which builds with zero warnings and no `AnalyzerReleases.*.md`.
2. `src/Encina.Analyzers/DiagnosticDescriptors.cs` (`public static partial class`, one `DiagnosticDescriptor` per rule, `HelpLinkUri` to the rule page, `customTags: WellKnownDiagnosticTags.Telemetry` omitted): ENC001 first.
3. `src/Encina.Analyzers/EncinaKnownTypes.cs` (`internal sealed class`): resolves `IRequestHandler<,>`, `ICommandHandler<,>`, `IQueryHandler<,>`, `ICommand<>`, `IQuery<>`, `INotification`, `INotificationHandler<>`, `IPipelineBehavior<,>`, `IIdempotentRequest`, `CacheAttribute`, `SagaDefinition<>` and the result carrier by metadata name; `TryCreate(Compilation, out EncinaKnownTypes)`.
4. `src/Encina.Analyzers/HandlerCancellationTokenAnalyzer.cs` (`[DiagnosticAnalyzer(LanguageNames.CSharp)]`, `Initialize(AnalysisContext)` with `ConfigureGeneratedCodeAnalysis(None)` and `EnableConcurrentExecution()`, `if (context is null) throw new ArgumentNullException(nameof(context));`, because `ThrowIfNull` is not in `netstandard2.0`): `RegisterCompilationStartAction` then `RegisterSymbolAction` on `SymbolKind.Method` for `Handle` of a type implementing a handler interface; ENC001 reports when the `CancellationToken` parameter is never referenced in the body (Design Choice 6, option A). No code fix: the issue's "add `CancellationToken`" fix has nothing to attach to.
5. `src/Encina.Analyzers/README.md` (package readme, rule table).
6. `tests/Encina.UnitTests/Analyzers/AnalyzerTestHost.cs` (snippet to compilation to diagnostics, and code-fix application, which PR-3 first uses), `tests/Encina.UnitTests/Analyzers/HandlerCancellationTokenAnalyzerTests.cs` (positive, negative, token passed on, token observed), `tests/Encina.GuardTests/Analyzers/EncinaAnalyzersGuardTests.cs` (`Initialize(null)`, `EncinaKnownTypes.TryCreate(null, ...)`, the name-pinning test of Design Choice 5).
7. Edit `tests/Encina.UnitTests/Encina.UnitTests.csproj` and `tests/Encina.GuardTests/Encina.GuardTests.csproj`: `ProjectReference` to the analyzer project and the two Roslyn `PackageReference`s (versions come from `Directory.Packages.props`, no new pin).
8. Edit `Encina.slnx` (one entry) and `.github/scripts/coverage-weights.json`: add `"Encina.Analyzers"` to the `patterns` of the existing `Tooling` category (unit and guard apply, target 60.0). The ABAC analyzer sits in the `Excluded` category (`applicableTests: []`, "not measured", `coverage-weights.json` lines 146-153), so copying its entry would leave the new package unmeasured; the `Tooling` category (`Encina.Cli`) is the one with unit and guard. New `.github/coverage-manifest/Encina.Analyzers.json` (through `mechanical-fixer`) carries the per-file targets; changelog fragment `changelog.d/438-roslyn-analyzers.added.md` (through `mechanical-fixer`).

File count of PR-1: csproj, descriptors, known types, analyzer, README (5), three test files (8), two test csproj (10), slnx, weights, manifest (13), fragment (14) = 14 files; about 600 lines. The `InternalsVisibleTo` items live in the csproj already counted. PR-3 adds the first code fixes and uses the host's fix application.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: Encina is a pre-1.0 .NET 10 library. src/Encina.Security.ABAC.Analyzers is the template for a Roslyn analyzer package (netstandard2.0). Rules are in AGENTS.md; no backward compatibility, zero warnings, XML docs on every public type.
TASK: Create src/Encina.Analyzers with the descriptor class, EncinaKnownTypes, the ENC001 analyzer, the in-process test host and tests (unit and guard), InternalsVisibleTo for Encina.UnitTests and Encina.GuardTests in the csproj, and register the project in Encina.slnx and in the Tooling category of coverage-weights.json (never Excluded). ENC001 (Design Choice 6, option A): the CancellationToken parameter of a handler's Handle is never referenced in the body, Warning, no code fix. Use the maintainer's answer to Dilemma 6 if it differs.
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
2. `MissingCacheAttributeAnalyzer.cs` (ENC006) and `AddCacheAttributeCodeFix.cs` (adds `[Cache]` with the defaults of `CacheAttribute` to the request declaration, only when it is in the same document).
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
TASK: Add ENC003 (step without Compensate in a saga chain, Warning, read-only steps exempt) and ENC007 (UseOutbox assigned with no MaxRetries in the same lambda, Warning).
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
| Analyzer release tracking (`AnalyzerReleases.Shipped.md` / `Unshipped.md`, RS2000 family) | Rule ids are public once shipped | Trivial: tracking stays off. The ABAC project has six rules, no `AnalyzerReleases.*.md` and suppresses only RS1038 and NU5128 (`Encina.Security.ABAC.Analyzers.csproj`), so RS2008 stays at zero warnings the same way; no `AnalyzerReleases.*` file exists anywhere in the repository. Revisit when the package ships a 1.0 id set |
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
| Production (`src/Encina.Analyzers`) | 20 | csproj, descriptors, known types, 10 analyzers, 4 fixes or refactorings, README, plus helper files |
| Tests | 17 | harness, 10 analyzer test files, fix tests inside them, guard test file, 2 csproj edits |
| Manifest, weights, slnx | 3 | `Encina.Analyzers.json`, `coverage-weights.json` (the `Tooling` category), `Encina.slnx` |
| Documentation and fragment | 5 | `docs/features/analyzers.md`, INVENTORY, ROADMAP, README, `changelog.d/438-roslyn-analyzers.added.md` |
| **Total** | **~45 distinct files, 49 file touches over 5 pull requests** | PR-1 14, PR-2 10, PR-3 13, PR-4 8, PR-5 4 files (`analyzers.md`, INVENTORY, ROADMAP and the README completed; the fragment is in PR-1); a file edited in several pull requests (descriptors, manifest, README) counts in each; about 600 + 700 + 1,100 + 600 + 350 = 3,350 lines in total (each pull request is under 1,500) |

How the package enters measurement: PR-1 adds `Encina.Analyzers` to the `Tooling` category of `.github/scripts/coverage-weights.json` (applicable tests unit and guard) and creates its manifest; the ABAC analyzer package stays in `Excluded` (not measured), which is why its coverage targets have no effect today (#2842). Coverage targets per file (unit and guard are the flags that apply; no contract, property or integration flag, see below): every analyzer and fix file unit 85 and guard 25. Justification: the analyzer logic is pure and fully testable through `CompilationWithAnalyzers`; the guard flag reaches only `Initialize` and the null checks, as the ABAC package's guard target of 20 does. `DiagnosticDescriptors.cs` and `EncinaKnownTypes.cs`: unit 90, guard 25 (static data and one factory with a null check). Contract, property and integration tests are justified as not applicable: no public interface is added, there is no complex numeric logic, and no database or transport is involved. Load and benchmark: not applicable (compile-time, per-compilation cost is measured once by running the analyzers over `Encina.slnx` in Phase 5).

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina is a pre-1.0 .NET 10 / C# 14 library (AGENTS.md rules apply: zero warnings, XML docs, tests execute real code, per-flag coverage targets from .github/coverage-manifest). Issue #438 adds the opt-in package Encina.Analyzers (netstandard2.0 Roslyn analyzers and code fixes). Issue #343 later adds ENC011 and up to the same package.

IMPLEMENTATION OVERVIEW: Phase 1 package, EncinaKnownTypes, test host, ENC001 (Design Choice 6). Phase 2 ENC004, ENC005, ENC009, ENC010. Phase 3 ENC002, ENC006, ENC008, generate-handler refactoring. Phase 4 ENC003, ENC007. Phase 5 docs through docs-writer. Five pull requests, each at most 15 files and 1500 lines, each building and passing alone.

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

1. The maintainer answers Dilemmas 2, 3, 4 and 6 when work starts; Phase 1 does not start before Dilemmas 2 and 6 (ids are public, and ENC001's definition fixes PR-1).
2. Open the sub-issues for PR-1 to PR-5 (each within 15 files and 1500 lines) and brief Phase 1.
3. Coordinate with #2842 (the ABAC package needs the same harness) and #343 (starts after Phase 1).
4. Existing finding to track separately: `Encina.Security.ABAC.Analyzers` has coverage targets and no tests (#2842, open).
