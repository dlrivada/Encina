# Implementation Plan: Source-Generated Validation (`Encina.Validation.Generators`)

> **Issue**: [#227](https://github.com/dlrivada/Encina/issues/227)
> **Type**: Feature
> **Related**: #230 (owns `IRequestValidator<T>`, `RequestValidatorRunner` and EventIds 550-559; a blocking prerequisite of S1 and S5), #228 (value objects; its boilerplate generator reuses this project), #231 (OpenAPI validators; reuses the rule model and emitters), #234 and #236 (planned apart), #702 (validator resolution cache), #726 (central validation instrumentation), ADR-054, ADR-062 (merged: error category contract)
> **Complexity**: Very high (a new Roslyn component, a runtime contract in core and a change in the three validation providers)
> **Estimated Scope**: ~1,900 lines of production code (generator, emitters, runtime) + ~2,700 lines of tests (about 600 of them snapshots) + ~450 lines of documentation (~5,050 lines in total, as the Estimated File Count table), in seven pull requests of at most 15 files and 1,500 lines each
> **Note on #230**: every statement about `IRequestValidator<T>`, the runner, its constructors and EventIds 550-559 follows the plan of #230 (a plan pull request, not code on `main`); it is re-verified against `main` when S1 starts

---

## Summary

### What exists today (read on 2026-11-15, main `db1e7f688`)

- No source generator exists in `src`. The only Roslyn component is `Encina.Security.ABAC.Analyzers` (netstandard2.0, `IsRoslynComponent`, `EnforceExtendedAnalyzerRules`, `IncludeBuildOutput=false`, DLL packed under `analyzers/dotnet/cs`, `Microsoft.CodeAnalysis.CSharp` with `PrivateAssets=all`; `src/Encina.Security.ABAC.Analyzers/Encina.Security.ABAC.Analyzers.csproj`). It has no test project reference under `tests/`. `Microsoft.CodeAnalysis.CSharp` 5.0.0 is a central package version; `Microsoft.CodeAnalysis.Testing` is not.
- The three validation providers read DataAnnotations attributes by reflection at run time: `DataAnnotationsValidationProvider` (`Validator.TryValidateObject`, `DataAnnotationsValidationProvider.cs:45`) and `MiniValidationProvider` (`MiniValidator.TryValidate`, `MiniValidationProvider.cs:31`); `FluentValidationProvider` resolves hand-written `IValidator<T>`. There is no compile-time path.
- `ValidationResult` and `ValidationError(PropertyName, ErrorMessage)` are the provider-neutral result (`src/Encina/Validation/ValidationResult.cs`); field errors travel inside a `ValidationResult`, not as separate `EncinaError` codes. The orchestrator builds the one `Left` (`ValidationOrchestrator.cs:68-72`; the code-less `Left` is #3415's).
- Not in `src`: `[GenerateValidator]`, `NotEqual`, `GreaterThan`, any generated validator, any generator test harness. The issue's sample generates a FluentValidation `AbstractValidator<T>`; FluentValidation is one of the three providers of `AGENTS.md` section 5, the other two have no generated counterpart.

### What this plan delivers

A new package `Encina.Validation.Generators` (an incremental source generator, analyzer-packaged, netstandard2.0) that turns a type marked `[GenerateValidator]` and its DataAnnotations and Encina attributes into a reflection-free, AOT-compatible `partial` validator class implementing `IGeneratedRequestValidator<T>` (a core marker over the `IRequestValidator<T>` contract of #230). The generated classes are registered by a generated per-assembly extension and run by the shared runner under all three providers; a provider skips its own reflective attribute pass for a type that has a registered generated validator and falls back to reflection (with a warning) when the attribute is present but no validator is registered, so a forgotten registration never leaves a request unvalidated. Nothing changes for a type without `[GenerateValidator]`.

- Affected packages: `Encina` (core: attributes, marker, runner method, log class), `Encina.Validation.Generators` (new), `Encina.DataAnnotations`, `Encina.MiniValidator`, `Encina.FluentValidation`.
- Provider category: Validation (3): the same generated validator runs under each provider and an equivalence contract test compares the generated result with the reflective result.
- Shared pieces: owned by #230: `IRequestValidator<T>`, `RequestValidatorRunner`, `AddEncinaRequestValidators`. Owned by this plan: `IGeneratedRequestValidator<T>`, `RequestValidatorRunner.HasGeneratedValidator<T>()`, the three attributes and the rule model reused by #228 and #231.
- Behavior change: none for existing code; a type with `[GenerateValidator]` and a registered generated validator is no longer validated by reflection.

---

## Design Choices

<details>
<summary><strong>1. What does the generator emit</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A FluentValidation `AbstractValidator<T>` (the issue's sample)** | Matches the issue text; users extend it with the Fluent API | Only FluentValidation users benefit; DataAnnotations and MiniValidator keep reflection; the generator package would depend on FluentValidation |
| **B) A provider-neutral `IGeneratedRequestValidator<T>` (over `IRequestValidator<T>` of #230) run by the shared runner under every provider** | The three providers behave the same (`AGENTS.md` section 5); reflection-free for all; no FluentValidation dependency | A new public marker in core; depends on #230 |
| **C) Both, selected by `[GenerateValidator(Target = ...)]`** | Maximum choice | Two emitters, two test matrices; the provider rule still needs B |

### Chosen Option: **B — Provider-neutral generated validator** (recommended, pending the maintainer)

### Rationale

- The issue's goal is "no reflection" and "integration with validation pipeline"; B reaches every provider, A one of three. The sample's `partial` class is kept: the generated class is `public sealed partial` so the application can add members.
- A wins when the application uses FluentValidation only and wants to extend the generated class with the Fluent DSL; C wins when both audiences are in scope.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Where the attributes live</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Public types in core `Encina.Validation`: `GenerateValidatorAttribute`, `NotEqualAttribute`, `GreaterThanAttribute` (the last two are `ValidationAttribute` subclasses)** | The cross-field attributes also work under the reflective DataAnnotations and MiniValidator providers (same result with or without the generator); no duplicate types per assembly | Three public types in core |
| **B) Emitted by the generator as internal types in each consumer assembly (post-initialization output)** | No runtime dependency for the attributes; core untouched | The cross-field attributes would mean nothing at run time without the generator; one copy per assembly (CS0436 warnings with `InternalsVisibleTo`) |

### Chosen Option: **A — Public attributes in core** (recommended, pending the maintainer)

### Rationale

- The cited reasoning (the attributes must work under the reflective providers) is also met by a third option, the attributes in a separate small package referenced by both core consumers and the generator; the contract-in-core choice is therefore a decision, not a rule.
- With A a type validates identically whether or not the generator ran, which is what the equivalence contract test asserts. B wins when the package must be removable without any source change.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. What a provider does with a type that has a generated validator</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Skip its reflective attribute pass when `HasGeneratedValidator<T>()` is true; when the type carries `[GenerateValidator]` but no generated validator is registered, log EventId 560 and keep the reflective pass** | No duplicate errors; no reflection on the hot path; a missing registration fails safe (still validated) | Providers gain a check per call (cached per type) |
| **B) Run both and de-duplicate by `(PropertyName, ErrorMessage)`** | Simple | Reflection stays; a changed message duplicates; wasted work |
| **C) A fourth `GeneratedValidationProvider`** | No change in the three | The orchestrator takes one provider, so it cannot be combined with the others; breaks the three-provider consistency |

### Chosen Option: **A — Skip with a fail-safe fallback** (recommended, pending the maintainer)

### Rationale

- Fail closed (`AGENTS.md` section 3): the only unsafe state is "attribute present, no validator, reflection skipped"; A never enters it. B wins when an application has hand-written reflective rules that the generator does not know about and wants nothing skipped.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. How generated validators are registered</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A generated extension per assembly, `Add<SanitizedAssemblyName>GeneratedValidators(this IServiceCollection)`, calling `AddEncinaRequestValidators()` and one `TryAddEnumerable` per validator** | Explicit, reflection-free, AOT-safe; a unique name per assembly avoids CS0121 ambiguity | The application calls one method per assembly with generated validators |
| **B) A `[ModuleInitializer]` per assembly feeding a static catalog read by a core `AddEncinaGeneratedValidators()`** | One call, no per-assembly name | Runs when the assembly loads, so an assembly not yet loaded is missed; static mutable state |

### Chosen Option: **A — Generated extension per assembly** (recommended, pending the maintainer)

### Rationale

- The missed-assembly failure of B is silent, which design choice 3 only partly mitigates (the fallback covers types with the attribute, not the performance loss). B wins when the application has many small assemblies and wants no registration code.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Package name and shape</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Encina.Validation.Generators`, one analyzer-packaged assembly** | The name in the issue title; the same packaging as `Encina.Security.ABAC.Analyzers` | None |
| **B) `Encina.Validation.SourceGenerator` (the name in the issue's Affected Packages)** | Singular | Contradicts the title |

### Chosen Option: **A — `Encina.Validation.Generators`** (recommended, pending the maintainer)

### Rationale

- The issue itself names both (title `Encina.Validation.Generators`, Affected Packages `Encina.Validation.SourceGenerator`). A is the name the sibling plans #228 and #231 use for "the generator project" and the plural matches the one package that also hosts their emitters; B wins when the maintainer prefers the singular form of the Affected Packages list.

**Class:** Dilemma

</details>

<details>
<summary><strong>6. Generator pipeline and tests</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IIncrementalGenerator` with `ForAttributeWithMetadataName`, equatable value models, no `Compilation` captured in a model; tests drive `CSharpGeneratorDriver` and assert incrementality with `TrackIncrementalGeneratorSteps`, plus Verify snapshots** | The acceptance criterion "incremental generation"; no new test package | Hand-written harness |
| **B) The `Microsoft.CodeAnalysis.Testing` packages** | Ready harness | A new central package for one project |

### Chosen Option: **A — Incremental generator with the driver harness** (recommended, pending the maintainer)

### Rationale

- The incremental model is the acceptance criterion ("Incremental generation") and the Roslyn guidance the issue links; `Microsoft.CodeAnalysis.CSharp` is already a central version and `Encina.Testing.Verify` wraps snapshots. B also satisfies the criterion, so the new central test package is a decision: B wins when the maintainer accepts a new test dependency for a ready harness. The harness is shared by several test projects, so it lives in `tests/Encina.TestInfrastructure` (S2), which `Encina.UnitTests`, `Encina.PropertyTests` and `Encina.ContractTests` already reference.

**Class:** Dilemma

</details>

### Register of defaults and semantics

| Id | Choice | Class | Basis |
|----|--------|-------|-------|
| C1 | The generator is `netstandard2.0`, `EnforceExtendedAnalyzerRules`, no reflection in generated code (`[GeneratedRegex]` for `RegularExpression`), so the output is AOT-compatible | Settled | The issue label `aot-compatible` and the benefit "no reflection"; precedent csproj of `Encina.Security.ABAC.Analyzers`; the generated-code rule does not depend on C5, which only chooses how unknown attributes are called |
| C2 | A shape the generator cannot support (nested request type, generic request type, an attribute argument that is not a constant, a name collision with an existing `<Type>Validator`) is a compile error `ENCVAL001` to `ENCVAL004`, never a silent skip. Option B: a warning diagnostic, and the type stays validated reflectively through the provider fallback of Design Choice 3 | Dilemma (recommended, pending the maintainer): error | an error cannot be missed and keeps the "zero warnings" rule of `AGENTS.md` section 8 trivially; a warning wins when the maintainer prefers a build that never fails because of a new generator |
| C3 | Field errors are `ValidationError(PropertyName, ErrorMessage)` as the reflective providers produce them; messages never include the value being validated | Trivial | the DataAnnotations default messages carry the member name and the rule bounds, never the value, and the generated text equals them (equivalence tests, S3); logs and tags follow `AGENTS.md` section 3 |
| C4 | No new `EncinaError` code and no new `ErrorCategory`: a failed field is a `ValidationResult` entry; the runner failure codes belong to #230 | Settled | ADR-062 (categories apply to `EncinaErrors.Create` calls; none is added here) |
| C5 | Unknown `ValidationAttribute` subclasses are called as instances (`new X(args).GetValidationResult(value, context)`) in generated code, so no attribute is dropped | Dilemma (recommended, pending the maintainer) | scope of "support custom validation attributes"; the alternative is a diagnostic for every attribute outside the inlined set |
| C6 | Inlined set: `Required`, `StringLength`, `MinLength`, `MaxLength`, `Range`, `RegularExpression`, `EmailAddress`, plus `NotEqual` and `GreaterThan`; everything else goes through C5 | Dilemma (recommended, pending the maintainer) | public behavior |
| C7 | Generated class name is `<Type>Validator` in the request's namespace, `public sealed partial` (or `internal` when the request is internal) | Dilemma (recommended, pending the maintainer) | public name |
| C8 | The generated validator does not echo values; `NotEqual` and `GreaterThan` compare in generated code with the property's own type (the `double` attribute argument is converted to the property type in the emitted literal) | Dilemma (recommended, pending the maintainer) | semantics of the two issue attributes |
| C9 | EventId 560 (one message) in the new range `ValidationGenerated (560, 569)`, inside the block 550-599 assigned to the family | Trivial | Settled by: AGENTS.md section 7; ADR-021; `artifacts/decisions/plan-batch/eventid-blocks.md` |
| C10 | A generated `[GeneratedRegex]` for `RegularExpression` carries a match timeout equal to the attribute's `MatchTimeoutInMilliseconds` (DataAnnotations default 2,000 ms), never an infinite one, and the pattern and options come only from the attribute's constant arguments; this keeps the ReDoS exposure and the behaviour equal to the reflective provider. Option B: no timeout (the source generator default) | Dilemma (recommended, pending the maintainer): attribute timeout | request input reaches the pattern, so a timeout bounds the cost; no timeout wins only for patterns proven linear |
| C11 | Generated validators are registered `Scoped`, like `AddRequestValidator` of #230 (a hand-written validator can take scoped services and the runner is scoped). Option B: `Singleton` for the stateless generated classes | Dilemma (recommended, pending the maintainer): Scoped | Scoped keeps one lifetime per runner dependency set; Singleton wins when allocation per request matters and the validators use no service |
| C12 | The generated extension is `Microsoft.Extensions.DependencyInjection.<SanitizedAssemblyName>GeneratedValidatorsExtensions.Add<SanitizedAssemblyName>GeneratedValidators`; sanitizing replaces every character that is not a letter, digit or `_` with `_` and prefixes `_` when the name starts with a digit; two assemblies that sanitize to one name get distinct static classes in distinct assemblies, so only a consumer referencing both sees CS0121 and calls them by class name. Option B: a fixed name `AddGeneratedValidators` with the assembly name as a parameter | Dilemma (recommended, pending the maintainer): sanitized per assembly | public name; B wins when applications have many generated assemblies and want one call shape |
| C13 | Generated and hand-written validators of one request type run together in the runner's order (registration order, per #230 register row D2); a generated validator is registered by `TryAddEnumerable`, so a second registration of it is ignored. Option B: generated validators always run first | Dilemma (recommended, pending the maintainer): registration order | depends on #230 D2; B wins when a stable result order across assemblies matters |
| C14 | Run-time semantics of the two issue attributes: `GreaterThan` with a null value is valid (as `Range`; `Required` decides null); `NotEqual` with both values null is invalid (they are equal); a property type that is not `IComparable` for `GreaterThan` or a missing sibling for `NotEqual` is `ENCVAL004` at compile time in the generated path and an invalid result naming the property (fail closed, never an exception, never valid) in the reflective path; a numeric comparison uses the property's own type with `decimal` and `double` conversion exact for the constant. Option B: a missing sibling or non-comparable type throws `InvalidOperationException` as other `ValidationAttribute` misuse does | Dilemma (recommended, pending the maintainer): invalid result | fail-closed gate semantics (`AGENTS.md` section 3) with an `Either`-style result; B wins when the maintainer prefers developer errors to surface as exceptions |
| C15 | The generator csproj hides RS1038 and NU5128 with `NoWarn` exactly as `src/Encina.Security.ABAC.Analyzers` does, and does not ship `AnalyzerReleases.*.md` for the four `ENCVAL` diagnostics. Option B: ship `AnalyzerReleases.Shipped.md` and `AnalyzerReleases.Unshipped.md` (two more files; S2 would exceed 15 files, so the harness edit would move to S3) | Dilemma (recommended, pending the maintainer): NoWarn like the precedent | zero warnings hold either way; B wins when the maintainer wants release tracking of the diagnostics from the first version |
| C16 | The constructors of the runner (an optional `ILogger`) and the providers (an optional runner) are changed by #230 (its plan, phases 2, 4 and 5), with their callers counted there; this plan adds no constructor parameter and its S5 and S6 consume the runner as #230 leaves it. Option B: this plan makes the constructor changes itself (the 19, 25 and 6 `new` sites of the three providers would be edited here) | Dilemma (recommended, pending the maintainer): #230 owns them | #230's own constructor choice (optional or required runner) is itself pending; B wins when #230 is not implemented before this plan |

---

## Implementation Phases

### Phase 1: Core Attributes, Marker and Runner Method

> **Goal**: The runtime contract in core, usable and tested without the generator (pull request S1; needs #230 S1 on `main`).

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** in `src/Encina/Validation/`: `GenerateValidatorAttribute.cs` (`AttributeUsage` class and struct, not a `ValidationAttribute`), `NotEqualAttribute.cs` (`ValidationAttribute`, constructor `(string otherPropertyName)`, reads the other property from `ValidationContext.ObjectInstance`), `GreaterThanAttribute.cs` (`ValidationAttribute`, constructor `(double minimum)`, exclusive bound, supports numeric property types and `IComparable`), `IGeneratedRequestValidator.cs` (`public interface IGeneratedRequestValidator<in TRequest> : IRequestValidator<TRequest>`, a marker).
2. **Modify** `RequestValidatorRunner.cs` (created by #230): `bool HasGeneratedValidator<TRequest>()` (resolves `IRequestValidator<TRequest>` services and tests `is IGeneratedRequestValidator<TRequest>`; the per-type decision that the attribute is present is cached in a static generic holder).
3. **Modify** `src/Encina/PublicAPI.Unshipped.txt` and `.github/coverage-manifest/Encina.json` (through `mechanical-fixer`).
4. Tests of this phase ship in S1: unit tests of both attributes (equal, not equal, null, missing property, numeric types, `decimal` and `int` bounds), a runner unit test of `HasGeneratedValidator`, and a guard test of the attribute constructors.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
Encina (.NET 10, C# 14, pre-1.0). Issue #227, plan docs/plans/validation-source-generators-implementation-plan-227.md.
Prerequisite: the IRequestValidator<T> contract and RequestValidatorRunner of issue #230 are on main.

TASK:
1. Add GenerateValidatorAttribute, NotEqualAttribute, GreaterThanAttribute and IGeneratedRequestValidator<T> to
   src/Encina/Validation/.
2. Add RequestValidatorRunner.HasGeneratedValidator<TRequest>().
3. Unit and guard tests; PublicAPI and manifest through mechanical-fixer.

KEY RULES:
- NotEqual and GreaterThan are ValidationAttribute subclasses so the reflective providers honor them too.
- XML docs on every public member; no values in messages; no [Obsolete].

REFERENCE FILES:
- src/Encina/Validation/IValidationProvider.cs, ValidationResult.cs
- docs/plans/async-cross-field-validation-implementation-plan-230.md (shared contract)
```

</details>

---

### Phase 2: Generator Project, Parsing Model and Diagnostics

> **Goal**: A buildable incremental generator that parses the annotated types into an equatable model and reports the compile errors, emitting nothing else yet (pull request S2).

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.Validation.Generators/Encina.Validation.Generators.csproj` (copy of the packaging of `Encina.Security.ABAC.Analyzers.csproj`: `netstandard2.0`, `LangVersion 14.0`, `IsRoslynComponent`, `EnforceExtendedAnalyzerRules`, `IncludeBuildOutput=false`, `DevelopmentDependency`, `PackagePath analyzers/dotnet/cs`, `Microsoft.CodeAnalysis.CSharp` with `PrivateAssets=all`) and add it to `Encina.slnx`.
2. **Create** `ValidatorGenerator.cs` (`[Generator] public sealed class ValidatorGenerator : IIncrementalGenerator`, `ForAttributeWithMetadataName("Encina.Validation.GenerateValidatorAttribute")`), `Models/ValidatedTypeModel.cs`, `Models/PropertyRuleModel.cs`, `Models/EquatableArray.cs` (value equality so the pipeline caches), `Parsing/AttributeParser.cs` and `DiagnosticDescriptors.cs` (`ENCVAL001` unsupported type shape, `ENCVAL002` name collision, `ENCVAL003` non-constant attribute argument, `ENCVAL004` cross-field target property not found).
3. **Modify** `tests/Encina.TestInfrastructure/Encina.TestInfrastructure.csproj` (a `ProjectReference` to the generator, not as an `Analyzer`, and a `PackageReference` to the central `Microsoft.CodeAnalysis.CSharp`; `Encina.UnitTests`, `Encina.PropertyTests` and `Encina.ContractTests` already reference this project, so no other test csproj changes) and **create** `.github/coverage-manifest/Encina.Validation.Generators.json` (through `mechanical-fixer`). The csproj hides RS1038 and NU5128 as the precedent does (register row C15).
4. Tests (S2): `GeneratorTestHost` in `tests/Encina.TestInfrastructure/Validation/` (compile a source string, run the driver, return diagnostics, generated trees and an in-memory loaded assembly; used by the S3 property test, the S5 DI test and the S6 tests), parser unit tests, diagnostics unit tests, and an incrementality test (an unrelated edit does not re-run the model step).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
Issue #227, plan docs/plans/validation-source-generators-implementation-plan-227.md, phase 2 of 7.

TASK:
1. Create the Encina.Validation.Generators project exactly like src/Encina.Security.ABAC.Analyzers (analyzer packaging).
2. Implement the incremental pipeline, the equatable models, the attribute parser and the four ENCVAL diagnostics.
3. Add the test harness over CSharpGeneratorDriver and the incrementality test.

KEY RULES:
- No Compilation or ISymbol inside a model (breaks incremental caching); netstandard2.0 APIs only.
- A shape the generator cannot support is a compile error (register row C2), never a silent skip.
- Zero warnings; manifest and slnx edits through mechanical-fixer.

REFERENCE FILES:
- src/Encina.Security.ABAC.Analyzers/Encina.Security.ABAC.Analyzers.csproj, DiagnosticDescriptors.cs
- Directory.Packages.props (Microsoft.CodeAnalysis.CSharp 5.0.0)
```

</details>

---

### Phase 3: Rule Emission for the Standard Attributes

> **Goal**: Generated validators for `Required`, `StringLength`, `MinLength`, `MaxLength`, `Range`, `RegularExpression` and `EmailAddress` (pull request S3).

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `Emit/ValidatorEmitter.cs` (class shell: `public sealed partial class <Type>Validator : IGeneratedRequestValidator<<Type>>`, `ValueTask<ValidationResult> ValidateAsync(request, context, ct)` building a `List<ValidationError>` and returning `ValidationResult.Failure` or `Success`) and `Emit/StandardRuleEmitter.cs` (one statement per rule; the message text equals the DataAnnotations default message so the reflective and generated results agree; `RegularExpression` emits a `[GeneratedRegex]` partial method).
2. Wire the emitter into `ValidatorGenerator.cs`.
3. Tests (S3): three Verify snapshots (Required and lengths, Range, Regex and email), a snapshot test class, and an equivalence property test: for random instances of an annotated type the generated validator and `Validator.TryValidateObject` agree on validity and on the set of `(PropertyName, ErrorMessage)` pairs (the type is compiled and emitted in memory by the harness).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
Issue #227, plan docs/plans/validation-source-generators-implementation-plan-227.md, phase 3 of 7. Depends on S1, S2.

TASK:
1. Emit the validator class and one rule statement per standard attribute; RegularExpression uses [GeneratedRegex].
2. Keep the DataAnnotations default messages so generated and reflective results are equal.
3. Add the snapshots and the FsCheck equivalence property.

KEY RULES:
- No reflection and no value in any message; nullable annotations respected (a non-nullable reference type with no
  [Required] is not null-checked beyond the compiler's contract).
- Tests execute the emitted code, not only the text.

REFERENCE FILES:
- src/Encina.Validation.Generators/ (phase 2)
- tests/Encina.PropertyTests/Validation/DataAnnotations/ValidationInvariantProperties.cs
```

</details>

---

### Phase 4: Cross-Field Rules and Custom Attributes

> **Goal**: `NotEqual`, `GreaterThan` and the instance fallback for every other `ValidationAttribute` (pull request S4).

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `Emit/CrossFieldRuleEmitter.cs` (`NotEqual` compares with the named sibling property using its own type; `GreaterThan` converts the `double` argument to the property type; `ENCVAL004` when the sibling does not exist) and `Emit/InstanceAttributeEmitter.cs` (register row C5: constructs the attribute from its constant constructor and named arguments and calls `GetValidationResult` with a `ValidationContext` carrying the request, the member name and the service provider).
2. Tests (S4): two snapshots, unit tests for each diagnostic and an equivalence property over `NotEqual`, `GreaterThan` and a custom attribute against the reflective result.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT:
Issue #227, plan docs/plans/validation-source-generators-implementation-plan-227.md, phase 4 of 7. Depends on S3.

TASK:
1. Emit NotEqual and GreaterThan with the property's own type; report ENCVAL004 for a missing sibling.
2. Emit unknown ValidationAttribute subclasses as instance calls built from their constant arguments.
3. Snapshots, diagnostics tests and the equivalence property.

KEY RULES:
- A non-constant attribute argument is ENCVAL003, never a silent drop.
- Messages never include the validated value.

REFERENCE FILES:
- src/Encina.Validation.Generators/Emit/
- src/Encina/Validation/NotEqualAttribute.cs, GreaterThanAttribute.cs (phase 1)
```

</details>

---

### Phase 5: Registration, Fallback Warning and EventIds

> **Goal**: The generated registration extension, the missing-registration warning and the registered EventId range (pull request S5).

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `Emit/RegistrationEmitter.cs`: per assembly `Microsoft.Extensions.DependencyInjection.<SanitizedAssemblyName>GeneratedValidatorsExtensions.Add<SanitizedAssemblyName>GeneratedValidators(this IServiceCollection)` calling `AddEncinaRequestValidators()` and one `TryAddEnumerable(ServiceDescriptor.Scoped<IRequestValidator<T>, <Type>Validator>())` per generated class.
2. **Modify** `src/Encina/Diagnostics/EventIdRanges.cs`: register `ValidationGenerated = (560, 569)`; **create** `src/Encina/Validation/GeneratedValidatorLog.cs` (EventId 560, warning: a type carries `[GenerateValidator]` but no generated validator is registered; request type name only); the runner logs it once per type from `HasGeneratedValidator<T>()`; **modify** `EncinaEventIdAllocationTests.cs`, `PublicAPI.Unshipped.txt` and both manifests (through `mechanical-fixer`).
3. Tests (S5): a DI test that compiles a sample assembly in memory, loads it, calls the generated extension and builds the provider with `ValidateOnBuild` and `ValidateScopes`; a log test (EventId, once per type, no values); a runner test of the attribute-without-registration case.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT:
Issue #227, plan docs/plans/validation-source-generators-implementation-plan-227.md, phase 5 of 7. Depends on S1-S4.
ADR-021: register the EventId range in src/Encina/Diagnostics/EventIdRanges.cs first.

TASK:
1. Emit the per-assembly registration extension (unique name per assembly) with TryAddEnumerable and scoped lifetime.
2. Register ValidationGenerated (560, 569); write GeneratedValidatorLog.cs with 560; log once per type from the runner.
3. Prove registration completeness with a ValidateOnBuild and ValidateScopes DI test over an in-memory compiled assembly.

KEY RULES:
- Pack ids sequentially; request type name only in the message; no scoped service into a singleton.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

---

### Phase 6: The Three Providers Skip Reflection

> **Goal**: Design Choice 3 option A in DataAnnotations, MiniValidator and FluentValidation (pull request S6).

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `DataAnnotationsValidationProvider.cs` and `MiniValidationProvider.cs` (the runner exists from #230): when `runner.HasGeneratedValidator<TRequest>()` is true the reflective attribute pass is skipped and only the runner runs; otherwise unchanged. `FluentValidationProvider.cs` has no reflective attribute pass to skip and already calls the runner (#230), so it is not edited; its behavior with a generated validator is covered by the equivalence test.
2. **Modify** the two manifests `Encina.DataAnnotations.json` and `Encina.MiniValidator.json` (through `mechanical-fixer`).
3. Tests (S6): two unit tests, one per provider (`DataAnnotationsValidationProviderGeneratedSkipTests.cs`, `MiniValidationProviderGeneratedSkipTests.cs` in `tests/Encina.UnitTests`: a type with a registered generated validator skips the reflective pass, the same type without a registration keeps it, a plain type is unchanged); `tests/Encina.ContractTests/Core/Validation/GeneratedValidatorEquivalenceContractTests.cs` (the same annotated type validated reflectively and through the generated validator under each of the three providers gives the same `ValidationResult`); edit the three `AsyncValidationPipelineTests.cs` integration files of #230 to add a generated-validator case.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```text
CONTEXT:
Issue #227, plan docs/plans/validation-source-generators-implementation-plan-227.md, phase 6 of 7. Depends on S1-S5 and #230.

TASK:
1. In the DataAnnotations and MiniValidator providers skip the reflective pass when HasGeneratedValidator is true.
2. Add the equivalence contract test and extend the three integration tests of #230.

KEY RULES:
- The fallback is the reflective pass; a type without the generator behaves exactly as before.
- Tests run the real providers through the real pipeline.

REFERENCE FILES:
- src/Encina.DataAnnotations/DataAnnotationsValidationProvider.cs, src/Encina.MiniValidator/MiniValidationProvider.cs
- tests/Encina.IntegrationTests/Validation/*/AsyncValidationPipelineTests.cs (from #230)
```

</details>

---

### Phase 7: Benchmark, Documentation and Finalization

> **Goal**: The performance evidence, the usage guide, the ADR and the changelog (pull request S7).

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `tests/Encina.BenchmarkTests/Encina.Benchmarks/GeneratedValidationBenchmarks.cs` (reflective DataAnnotations against the generated validator, `BenchmarkSwitcher` per `AGENTS.md` section 9) and a load-test justification `.md` (the generated code is stateless and allocation-bounded).
2. **Create** through `docs-writer`: `src/Encina.Validation.Generators/README.md`, `docs/features/source-generated-validation.md`; **modify** `docs/INVENTORY.md` (new package); an ADR that records the maintainer's answers to Design Choices 1 to 4 (`docs/architecture/adr/`; ADR number assigned when the ADR pull request opens) and its index line.
3. **Create** `changelog.d/227-validation-source-generators.added.md` (through `mechanical-fixer`).
4. Build verification: `dotnet build --configuration Release` with zero warnings; per-flag coverage of the touched files against the manifest targets below.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```text
CONTEXT:
Issue #227, plan docs/plans/validation-source-generators-implementation-plan-227.md, phase 7 of 7.

TASK:
1. Write the benchmark (generated against reflective) and the load-test justification.
2. Document the package, the attributes, the ENCVAL diagnostics and the registration call; add the ADR and changelog.

KEY RULES:
- Never type coverage or benchmark figures by hand; cite cov: markers (SPEC-001). Pages through docs-writer,
  fragments through mechanical-fixer. Sibling plans are referenced by issue number only.

REFERENCE FILES:
- .claude/skills/encina-docs/SKILL.md
- tests/Encina.BenchmarkTests/Encina.Benchmarks/ValidationBenchmarks.cs
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Use in this feature |
|--------|-----------|---------------------|
| Roslyn incremental generators (the document linked in the issue) | `IIncrementalGenerator`, `ForAttributeWithMetadataName`, equatable pipeline values | Design Choice 6 |
| `System.ComponentModel.DataAnnotations` | `ValidationAttribute`, `Validator.TryValidateObject`, default error messages | Input attributes and the equivalence oracle |
| .NET regular expression source generation | `[GeneratedRegex]` | Reflection-free `RegularExpression` rules |
| ADR-021 | EventId ranges registered before use | Register row C9 |
| ADR-054 D3, D4 | One meter per package; tenant tag when multi-tenancy is on | No instrument here (the central instrumentation is #726) |
| ADR-062 | Error category contract | Register row C4: no new `EncinaError` code |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| Analyzer packaging | `src/Encina.Security.ABAC.Analyzers/Encina.Security.ABAC.Analyzers.csproj` | Template for the new csproj |
| `ValidationResult`, `ValidationError` | `src/Encina/Validation/ValidationResult.cs` | Output of generated validators |
| `IRequestValidator<T>`, runner, options | `src/Encina/Validation/` (issue #230) | The runtime contract |
| Reflective providers | `src/Encina.DataAnnotations/`, `src/Encina.MiniValidator/`, `src/Encina.FluentValidation/` | Skip or call the runner; the equivalence oracle |
| `Encina.Testing.Verify`, FsCheck wrappers | `src/Encina.Testing.Verify/`, `src/Encina.Testing.FsCheck/` | Snapshots and properties |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina` core (`ValidationGenerated`, new, inside the block 550-599 assigned to the validation-1 family) | 560-569 | 560 used (one message); 561-569 free; #230 owns 550-559 |

### Estimated File Count

Every count includes tests, snapshots, coverage manifest edits, PublicAPI files, the README and the changelog fragment. No pull request uses a type created by a later one.

| PR | Content | New src | Changed src | New tests | Edited tests | Manifest | Other | Files | Lines (est.) |
|----|---------|---------|-------------|-----------|--------------|----------|-------|-------|--------------|
| S1 | Attributes, marker, runner method (needs #230 S1) | 4 | 2 (runner, PublicAPI) | 4 (2 attribute unit, runner unit, guard) | 0 | 1 | 0 | 11 | ~700 |
| S2 | Generator project, parser, diagnostics, shared harness | 7 (csproj, generator, 2 models, `EquatableArray`, parser, descriptors) | 0 | 4 (parser, diagnostics, incrementality, `GeneratorTestHost` in `Encina.TestInfrastructure`) | 1 (`Encina.TestInfrastructure.csproj`) | 1 (new) | 1 (`Encina.slnx`) | 14 | ~900 |
| S3 | Standard rule emission | 2 | 1 (`ValidatorGenerator.cs`) | 5 (snapshot class, equivalence property, 3 snapshots) | 0 | 1 | 0 | 9 | ~800 |
| S4 | Cross-field and instance attributes | 2 | 1 (`ValidatorGenerator.cs`) | 4 (2 snapshots, diagnostics unit, equivalence property) | 0 | 1 | 0 | 8 | ~650 |
| S5 | Registration, warning, EventIds | 2 (`RegistrationEmitter`, `GeneratedValidatorLog`) | 4 (`EventIdRanges.cs`, runner, PublicAPI, `ValidatorGenerator.cs`) | 3 (DI test, log test, runner test) | 1 (`EncinaEventIdAllocationTests`) | 2 | 0 | 12 | ~850 |
| S6 | Providers skip reflection | 0 | 2 (DataAnnotations and MiniValidator providers) | 3 (equivalence contract, 2 provider unit tests of the skip branch) | 3 (integration tests of #230) | 2 | 0 | 10 | ~650 |
| S7 | Benchmark, docs, ADR, changelog | 0 | 0 | 2 (benchmark, load `.md` justification) | 0 | 0 | 6 (package README, feature page, `docs/INVENTORY.md`, ADR, ADR index, changelog fragment) | 8 | ~500 |
| **Total** | | **17** | **10** | **25** | **5** | **8** | **7** | **72** | **~5,050** |

The columns sum to the total (17 + 10 + 25 + 5 + 8 + 7 = 72) and every statement of the total in this plan is 72 files and about 5,050 lines (700 + 900 + 800 + 650 + 850 + 650 + 500). The largest pull request is S2 at 14 files; none exceeds 1,500 lines. S1 waits for #230 S1; S2 does not.

Per-file coverage targets (per flag, with a one-sentence justification), written into the package manifests by the pull request that adds the file:

| File | Unit | Guard | Property | Justification |
|------|------|-------|----------|---------------|
| `NotEqualAttribute.cs`, `GreaterThanAttribute.cs` (S1: Unit and Guard) | 95 | 90 | 0 in S1 | Small attributes with every branch (types, null, missing property, register row C14) unit-tested and constructor arguments guard-tested in S1 |
| `NotEqualAttribute.cs`, `GreaterThanAttribute.cs` (S4: Property) | 95 | 90 | 50 | The Property target is entered by S4, the pull request whose equivalence property exercises both attributes against the reflective oracle |
| `GenerateValidatorAttribute.cs`, `IGeneratedRequestValidator.cs` | 100 | 0 | 0 | A marker attribute and a marker interface with no executable code to guard |
| `ValidatorGenerator.cs`, `Parsing/AttributeParser.cs` | 85 | 0 | 0 | Driven through `CSharpGeneratorDriver` with every shape and diagnostic; the entry points take Roslyn structs or internal types the guard project cannot reference |
| `Emit/*.cs` | 90 | 0 | 60 | Snapshot and unit tests cover each emitter branch; the equivalence property compares the generated code against `Validator.TryValidateObject`; internal types have no guard tests |
| `DiagnosticDescriptors.cs`, `Models/*.cs` | 80 | 0 | 0 | Static descriptors and value models, covered by the diagnostics and incrementality tests |
| `GeneratedValidatorLog.cs` | 90 | 0 | 0 | One source-generated log method, covered by the log test; no parameters to guard |
| `RequestValidatorRunner.cs` (changed method, S1) | 90 | 85 | 0 | `HasGeneratedValidator` has three paths (generated, attribute without registration, plain), each with a unit test |
| `EventIdRanges.cs` (S5) | 100 | 0 | 0 | One new static field read by the existing allocation architecture test; no parameters, so guard tests reach nothing |
| `DataAnnotationsValidationProvider.cs`, `MiniValidationProvider.cs` (S6) | 90 | 0 in S6 | 0 | The changed skip branch is covered by the two S6 provider unit tests; Contract 40 and Integration 50 are entered by S6 too (the equivalence contract test and the extended `AsyncValidationPipelineTests` run the provider's main path); the Guard and Property targets are entered by #230's pull requests, which own the constructors and the merge |
| `Encina.Validation.Generators.csproj`, `Encina.TestInfrastructure.csproj`, `Encina.slnx` | n/a | n/a | n/a | Project files with no executable code; the harness `GeneratorTestHost.cs` is test infrastructure and has no manifest entry |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #227 - a source generator for provider-neutral validators.

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library; pre-1.0, no backward compatibility, best solution always.
- Validation follows the orchestrator pattern with three providers (AGENTS.md sections 3 and 5); issue #230 owns the
  IRequestValidator<T> contract and the shared runner this generator builds on.
- The maintainer's answers to Design Choices 1-4 of docs/plans/validation-source-generators-implementation-plan-227.md
  decide the emission target, the attribute home, the provider behavior and the registration style.

IMPLEMENTATION OVERVIEW:
1. Core: GenerateValidator, NotEqual, GreaterThan, IGeneratedRequestValidator<T>, runner.HasGeneratedValidator.
2. Encina.Validation.Generators: incremental generator, equatable models, ENCVAL diagnostics.
3. Emitters: standard attributes, cross-field, instance fallback; per-assembly registration extension.
4. EventId 560 in ValidationGenerated (560-569), registered first; fail-safe fallback when a registration is missing.
5. The three providers skip reflection; equivalence contract test; benchmark, docs, ADR, changelog.
Seven pull requests (S1 to S7), see the Estimated File Count table.

KEY PATTERNS:
- Generated code is reflection-free and AOT-compatible; no value ever appears in a message.
- Generated and reflective results must be equal (property and contract tests).
- DI test with ValidateOnBuild and ValidateScopes; zero warnings; PublicAPI and manifests through mechanical-fixer.

REFERENCE FILES:
- src/Encina.Security.ABAC.Analyzers/ (packaging), src/Encina/Validation/
- docs/plans/validation-source-generators-implementation-plan-227.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | Validation is per request; the validator resolution cache of Fluent is #702 |
| 2 | OpenTelemetry | ⏭️ #726 | Source generation happens at compile time and cannot be traced; the run-time duration of validation is instrumented centrally by #726 (verified open); this plan adds no instrument and therefore no tenant tag (ADR-054 D4) |
| 3 | Structured Logging | ✅ Phase 5 | `GeneratedValidatorLog` EventId 560 in `ValidationGenerated (560, 569)`; the issue's "log generated validator registration" is a compile-time fact, so the logged event is the failure mode (attribute without registration) |
| 4 | Health Checks | ❌ N/A | In-process generated code with no dependency |
| 5 | Validation | ✅ All phases | This is the validation feature; the three providers stay consistent (Phase 6) |
| 6 | Resilience | ❌ N/A | No external call |
| 7 | Distributed Locks | ❌ N/A | No shared state |
| 8 | Transactions | ❌ N/A | Validation runs before the handler |
| 9 | Idempotency | ❌ N/A | Stateless and repeatable |
| 10 | Multi-Tenancy | ⏭️ | Tenant-scoped validator selection: a follow-up shared with #230 (draft: `artifacts/issues/validation-tenant-module-scoped-selection.md`); `TenantId` already reaches generated validators through `IRequestContext` |
| 11 | Module Isolation | ⏭️ | Module-scoped generation: the same follow-up issue |
| 12 | Audit Trail | ⏭️ | Auditing of rejected requests: a follow-up (draft: `artifacts/issues/validation-failure-audit.md`) |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| `IRequestValidator<T>`, `RequestValidatorRunner`, `AddEncinaRequestValidators`, EventIds 550-559 | Plan #230, S1 and S2 | Blocking for S1 and S5 of this plan |
| ADR-062 error category contract | ✅ Merged | No new code here (register row C4) |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| Central validation instrumentation | #726 | Covers the runtime duration |
| Domain value object boilerplate generator | #228 | Reuses the project created in S2 |
| OpenAPI validators from a specification | #231 | Reuses the rule model and emitters |

---

## Next Steps

1. Link this plan from #227 and ask the maintainer to answer Design Choices 1 to 6 and the Dilemma rows C2, C5 to C8 and C10 to C16 of the register when work starts. At pick-up the maintainer also re-confirms the issue task "Generate FluentValidation validators", which Design Choice 1 option B replaces with the provider-neutral validator.
2. Open the follow-up issues when the first pull request opens (shared with #230; both drafts exist in the planning worktree's `artifacts/issues/`): tenant and module scoped selection (draft: `artifacts/issues/validation-tenant-module-scoped-selection.md`) and auditing of validation failures (draft: `artifacts/issues/validation-failure-audit.md`).
3. Implement S1 to S7 as the pull requests of the Estimated File Count split, `Fixes #227` on S7.
