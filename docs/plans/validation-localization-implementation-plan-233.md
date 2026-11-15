# Implementation Plan: Validation Error Localization (i18n)

> **Issue**: [#233](https://github.com/dlrivada/Encina/issues/233)
> **Type**: Feature
> **Related**: #230 (owns `IRequestValidator<T>`, the runner and EventIds 550-559; its orchestrator change is independent of this one), #227 (generated validators emit codes this plan localizes), #234 (aggregation, planned apart), #235 (schema builder, consumes the message keys and the `ValidationError` code defined here), #236 (two-phase validation, planned apart), #726 (central validation instrumentation), #3415 (a code-less validation `Left` never becomes a 400), #3426 (which error text a client may see), ADR-054, ADR-062 (merged: error category contract)
> **Complexity**: Medium (core mechanism plus one ASP.NET Core adapter; no persistence)
> **Estimated Scope**: ~800 lines of production code + ~1,900 lines of tests + ~300 lines of resources and documentation, in six pull requests of at most 15 files and 1,000 lines each

---

## Summary

### What exists today (read on 2026-11-15, main `db1e7f688`)

- A validation failure is a `ValidationError(string? PropertyName, string ErrorMessage)`, a two-field positional record (`src/Encina/Validation/ValidationResult.cs:97`), and the message is whatever string the provider library produced. `ValidationOrchestrator` joins those messages into the text of a code-less `Left` (`ValidationOrchestrator.cs:68-71`, via `ValidationResult.ToErrorMessage`, `ValidationResult.cs:77`).
- Nothing in `src` mentions localization: `git grep -n -E "Microsoft.Extensions.Localization|IStringLocalizer" -- src '*.csproj' '*.props'` finds nothing, `IRequestContext` carries `CorrelationId`, `CausationId`, `Identity`, `IdempotencyKey`, `TenantId`, `Timestamp` and `Metadata` but no culture, and there is no `AddEncinaValidation(options)` entry point (the issue's snippet is aspirational; each provider has its own `Add...` method).
- What already works with no Encina code: the issue's own "Usage" example (a FluentValidation validator that takes `IStringLocalizer<T>` and calls `WithMessage(localizer[...])`) is plain application code; FluentValidation translates its built-in messages by `CultureInfo.CurrentUICulture`; DataAnnotations attributes read `ErrorMessageResourceType`/`ErrorMessageResourceName` through the same culture. `Encina.AspNetCore` is a framework reference to `Microsoft.AspNetCore.App` (`Encina.AspNetCore.csproj`), which ships `Microsoft.Extensions.Localization` and the `UseRequestLocalization` middleware that sets the current culture per HTTP request.
- The three providers lose the machine-readable identity of a failure: `FluentValidationProvider` builds `new ValidationError(failure.PropertyName, failure.ErrorMessage)` (`FluentValidationProvider.cs:73-77`) although `ValidationFailure` carries `ErrorCode` and the placeholder values; `DataAnnotationsValidationProvider` and `MiniValidationProvider` build the error from a message string only.
- Not in `src`: any message key, any resource file for validation, any plural handling, any culture allow-list, any `IValidationMessageLocalizer`.

### What this plan delivers

An opt-in localization layer for validation errors, provider-neutral because it sits in the one place all three providers pass through, `ValidationOrchestrator`:

- `ValidationError` gains an optional `ErrorCode` (the lookup key) and optional named `Arguments` (the placeholder values); FluentValidation fills them from `ValidationFailure`.
- `IValidationMessageLocalizer` turns a coded error and a culture into a message: named placeholders (`{Min}`), plural variants chosen by a per-culture plural rule, culture-aware formatting of numbers and dates, and a fallback chain (exact culture, parent cultures, default culture, the provider's own message). The default implementation reads .NET resource files; an ASP.NET Core adapter reads `IStringLocalizer`.
- A culture allow-list (`SupportedCultures`) and a resolver (default: the current UI culture, which `UseRequestLocalization` already sets) decide the culture; the orchestrator runs the provider inside a scope that sets that culture, so the native localization of FluentValidation and DataAnnotations follows it too (DataAnnotations and MiniValidator errors carry no code, so Encina cannot re-key them; the culture scope is what localizes them).
- Default resource files (neutral, `es-ES`, `fr-FR`) for the common rule messages that provider-neutral producers (#227, #230, #235) use.

Nothing changes for an application that does not call `AddEncinaValidationLocalization`.

- Affected packages: `Encina` (core), `Encina.FluentValidation` (code and arguments), `Encina.AspNetCore` (`IStringLocalizer` adapter). `Encina.DataAnnotations` and `Encina.MiniValidator` are covered through the culture scope and their native resources, without code changes.
- Provider category: Validation (3) (`AGENTS.md` section 5): one equivalence unit test proves the same request in a non-default culture yields a localized message under each provider, each by its native mechanism (Design Choice 1).
- Shared pieces, owned by this plan: `ValidationError.ErrorCode` and `ValidationError.Arguments`, `IValidationMessageLocalizer`, the message key convention `validation.<rule>`, and the default resource set; #235 builds its messages on them.

---

## Design Choices

<details>
<summary><strong>1. Where localization happens</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Ambient culture only: the application sets `CurrentUICulture` and every library localizes with its own mechanism; Encina adds nothing** | No Encina API; works today for FluentValidation and DataAnnotations | Messages Encina or the new sibling features create (#227 generated validators, #230 `IRequestValidator<T>`, #235 schema) have no localization path; no fallback chain, no plurals, no allow-list |
| **B) Orchestrator-level localizer keyed by `ValidationError.ErrorCode`, plus a culture scope around the provider call** | One place for all providers and for every provider-neutral producer; native mechanisms still work because the scope sets the culture; opt-in | New public surface (`ErrorCode`, `Arguments`, the localizer); a lookup per coded error; DataAnnotations and MiniValidator errors have no code and rely on their native resources |
| **C) Per-provider code in each of the three packages** | Each provider can use its library's richest hook | Three implementations to keep equal (the exact drift `AGENTS.md` section 5 forbids); nothing for the neutral producers |

### Chosen Option: **B — Orchestrator-level localizer plus culture scope** (recommended, pending the maintainer)

### Rationale

- The orchestrator is the single choke point of the pipeline (`ValidationPipelineBehavior` calls it for every provider), so one implementation serves the three providers and every producer built on `ValidationResult`.
- A wins when an application's validators are all FluentValidation or DataAnnotations with their own resources and no Encina-created message exists; C wins only if the maintainer wants no `ValidationError` change.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. How the culture is chosen</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A pluggable `IValidationCultureResolver`; the default returns `CultureInfo.CurrentUICulture`, and an application replaces it where the culture does not travel in the thread (a message consumer that reads it from a header)** | No change to `IRequestContext` or any host; HTTP hosts already get the culture from `UseRequestLocalization`; the resolver is the extension point for everything else | A message consumer must register its own resolver |
| **B) A typed `Culture` property on `IRequestContext`** | One source for every host and consumer | A contract change on a core interface with many implementers and test doubles; every producer of a context must set it |
| **C) `Encina.AspNetCore` parses `Accept-Language` itself** | Works without `UseRequestLocalization` | Duplicates the framework's negotiation, supports only HTTP |

### Chosen Option: **A — Pluggable resolver, ambient default** (recommended, pending the maintainer)

### Rationale

- The result of any resolver is matched against `SupportedCultures` (exact, then each parent, then `DefaultCulture`); a name that is not in the allow-list is never turned into a `CultureInfo`, so a client-controlled header cannot create arbitrary culture objects or throw `CultureNotFoundException`.
- B wins when the maintainer wants the culture to be part of the request identity for audit and replay; C wins for a minimal API without `UseRequestLocalization`.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Parameterized messages and pluralization</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Named placeholders (`{Min}`) from `ErrorCode`/`Arguments`, and plural variants as separate keys (`validation.min_items.one`, `.other`) chosen by a small per-culture `IPluralRule` table (CLDR categories `zero`, `one`, `two`, `few`, `many`, `other`)** | Translators edit plain resource files; no template language to parse; rules are plain code with property tests | Encina ships and maintains plural rules for the cultures it supports (English, Spanish, French need only `one` and `other`; a culture without a rule uses `other`) |
| **B) An ICU MessageFormat subset inside one template (`{Count, plural, one {...} other {...}}`)** | One string per message; translation tools understand ICU | A parser to write, test and keep safe against hostile or broken templates |
| **C) A third-party formatter (SmartFormat, MessageFormat)** | Full plural, select and gender support | A new dependency for core; its template syntax becomes part of Encina's public contract |

### Chosen Option: **A — Named placeholders and plural keys** (recommended, pending the maintainer)

### Rationale

- The BCL has no plural-category API, so some rule table is unavoidable; A keeps it the smallest and the resource files the plainest.
- B wins when translators work in ICU-based tools; C wins when many languages with complex plural rules (Arabic, Russian, Polish) must be supported on day one.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Where the `IStringLocalizer` integration lives</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) An adapter `StringLocalizerValidationMessageLocalizer` in `Encina.AspNetCore`, registered by `AddEncinaStringLocalizerValidation`** | Core stays free of a new dependency (pay-for-what-you-use); the package already references the shared framework that contains `Microsoft.Extensions.Localization` | A worker-service host without `Encina.AspNetCore` cannot use `IStringLocalizer` for Encina messages (it uses the resource-file localizer) |
| **B) A new package `Encina.Validation.Localization`** | Any host can use it; no web dependency | A new package (csproj, README, manifest, solution entry) for one small class |
| **C) Core references `Microsoft.Extensions.Localization.Abstractions`** | Works everywhere, no new package | Every Encina user pays for a package only some use |

### Chosen Option: **A — Adapter in `Encina.AspNetCore`** (recommended, pending the maintainer)

### Rationale

- Localization of user-facing text is chiefly a web concern, and the default resource-file localizer in core serves hosts without ASP.NET Core.
- B wins when worker-service or gRPC-only hosts need `IStringLocalizer` backed by custom stores (a database, a translation service); C wins when the maintainer prefers fewer packages over a lean core.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. How a coded error is represented</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Two optional trailing parameters on the existing record: `ValidationError(string? PropertyName, string ErrorMessage, string? ErrorCode = null, IReadOnlyDictionary<string, object?>? Arguments = null)`** | One type everywhere; every existing two-argument construction (14 sites in the validation providers, `ValidationResult.cs` and their tests, verified with `git grep -c "new ValidationError(" -- src tests`) compiles unchanged | Record equality treats `Arguments` by reference, so two errors with equal but distinct dictionaries differ; `Deconstruct` gains two outputs |
| **B) A derived `CodedValidationError : ValidationError`** | The base record stays frozen | `ValidationError` is `sealed`; unsealing is a larger change, and consumers must test for the subtype |
| **C) A parallel list of keys inside `ValidationResult`** | No change to the record | A second collection to keep aligned with `Errors` by index |

### Chosen Option: **A — Optional trailing parameters** (recommended, pending the maintainer)

### Rationale

- The pre-1.0 rule is the best design, not the compatible one (`AGENTS.md` section 1); A is also source-compatible with every existing call, which keeps the first pull request small.
- B wins when the maintainer wants `ValidationError` equality to stay purely structural.

**Class:** Dilemma

</details>

<details>
<summary><strong>6. Fallback chain for a missing translation</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Exact culture, then each parent culture, then `DefaultCulture`, then the message the provider produced; never an exception, never an empty message** | A missing translation degrades to readable text; the same order .NET resource lookup uses | A silently missing translation is invisible unless logged (the debug log of Phase 3 names the key and culture) |
| **B) Throw when a supported culture lacks a key** | Missing translations are found early | A request fails because of a translation gap; a validation gate must not fail for presentation reasons |

### Chosen Option: **A — Resource fallback chain ending at the provider's message**

### Rationale

- The order is the documented .NET resource fallback process, and ending at the provider's own message guarantees a non-empty result.

**Class:** Trivial
**Settled by:** Microsoft documentation, "Packaging and Deploying Resources in .NET Apps", section "The resource fallback process" (specific culture, then parent, then neutral) and the issue's own requirement "Fallback chain for missing translations"

</details>

### Register of defaults and semantics

| Id | Choice | Class | Basis |
|----|--------|-------|-------|
| C1 | A culture string is only ever turned into a `CultureInfo` after it matched `SupportedCultures`; an unknown or malformed culture falls back to `DefaultCulture` and never throws | Settled | OWASP ASVS 4.0 V5.1.3 (positive validation with an allow list for all input, including headers) |
| C2 | Logs, tags and errors carry the error code, the culture name from the allow-list and the request type name, never `Arguments` values, the rendered message or `EncinaError.Message` | Settled | `AGENTS.md` section 3 (`EncinaError.Message` never reaches logs; #1168, #1173, #1259, #1274) |
| C3 | `LocalizationOptions` are validated at start-up: `DefaultCulture` non-empty and a member of `SupportedCultures`, no duplicates, `ResourceType` (when set) has a resource set | Trivial | `AGENTS.md` section 3 (registration completeness) and the repository's options-validator idiom (`IValidateOptions<T>` with `ValidateOnStart`, for example `src/Encina.Compliance.Consent/ConsentOptionsValidator.cs`) |
| C4 | The localizer is registered as a singleton and depends on nothing scoped (resource managers and `IStringLocalizerFactory` are singletons); the orchestrator keeps its existing lifetime | Settled | `AGENTS.md` section 3 (registration completeness: no scoped service into a singleton); the orchestrator is a singleton in `AddDataAnnotationsValidation` and `AddMiniValidation` and scoped in `AddEncinaFluentValidation` |
| C5 | The culture scope sets `CurrentCulture` and `CurrentUICulture` for the provider call and restores both in a `finally`, because the values flow with the `ExecutionContext` | Settled | [.NET documentation, `CultureInfo.CurrentCulture`](https://learn.microsoft.com/en-us/dotnet/api/system.globalization.cultureinfo.currentculture) (stored per logical execution context and flows across `await`) |
| C6 | No new `EncinaError` code is created: a localization problem never fails a request, a misconfiguration fails at start-up through options validation, so there is no ADR-062 `ErrorCategory` to assign | Settled | ADR-062 contract (a category is chosen where an error is created; none is created) |
| C7 | OpenTelemetry for localization (a culture attribute on validation failures) is not added here: #726 instruments `ValidationOrchestrator` centrally; when it adds a culture attribute it takes the value from the allow-list (bounded cardinality) and adds the tenant tag only when multi-tenancy is on (ADR-054 D4) | Settled | Issue #726 (open, proposed solution 4); ADR-054 D3, D4 |
| C8 | EventIds 600-602 are packed from the start of the block 600-649 assigned to this family; the range `ValidationLocalization = (600, 609)` is registered first | Trivial | `AGENTS.md` section 7; ADR-021; `artifacts/decisions/plan-batch/eventid-blocks.md` |
| C9 | The summary text of the orchestrator's `Left` ("Validation failed for X with N error(s): ...") stays English: it is the diagnostic `EncinaError.Message`, while the per-error messages inside it are the localized part | Dilemma (recommended, pending the maintainer) | public behavior; the alternative is localizing the prefix with a key, which matters when an application shows the whole message to users (#3426 decides what a client may see) |
| C10 | Default resource cultures are the neutral (English) set plus `es-ES` and `fr-FR`, the cultures of the issue's example; the Spanish and French texts are reviewed by a native speaker before merge | Dilemma (recommended, pending the maintainer) | scope; `es-ES` alone is the alternative when no French reviewer is available |
| C11 | The coded-error argument bag and the message template engine never evaluate code: placeholders are looked up by name and formatted with `string.Format`-style culture formatting only | Settled | OWASP ASVS 4.0 V5.2.4 (no dynamic code execution from user-controlled data) |

---

## Implementation Phases

### Phase 1: Coded `ValidationError` and FluentValidation mapping (pull request S1)

> **Goal**: Errors carry a key and named arguments; FluentValidation fills them. Nothing consumes them yet.

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `src/Encina/Validation/ValidationResult.cs`: `public sealed record ValidationError(string? PropertyName, string ErrorMessage, string? ErrorCode = null, IReadOnlyDictionary<string, object?>? Arguments = null)` with XML docs for both parameters (Design Choice 5).
2. **Modify** `src/Encina.FluentValidation/FluentValidationProvider.cs`: build the error with `failure.ErrorCode` and the placeholder values of `failure.FormattedMessagePlaceholderValues` (excluding `PropertyValue`, which can hold user input; register row C2 keeps values out of logs, and the localizer receives only the numeric and comparison placeholders).
3. **Modify** `src/Encina/PublicAPI.Unshipped.txt` (the constructor line and the two new members) and `.github/coverage-manifest/Encina.json` and `Encina.FluentValidation.json` through `mechanical-fixer`.
4. Tests: `ValidationResultTests.cs` (edit: coded and uncoded errors, `with` expression, equality with and without arguments); new `FluentValidationErrorCodeTests.cs` (unit: a failing rule yields the FluentValidation `ErrorCode` and its arguments, `PropertyValue` is not copied).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
Encina (.NET 10, C# 14, pre-1.0). Issue #233 plan docs/plans/validation-localization-implementation-plan-233.md,
phase 1 of 6. ValidationError is a positional record in src/Encina/Validation/ValidationResult.cs.

TASK:
1. Add optional trailing parameters ErrorCode (string?) and Arguments (IReadOnlyDictionary<string, object?>?) to
   ValidationError; document both.
2. In FluentValidationProvider copy failure.ErrorCode and the numeric/comparison placeholders into the error;
   never copy PropertyValue (it is user input).

KEY RULES:
- Existing two-argument constructions must compile unchanged; no behavior change for a consumer that ignores the new fields.
- PublicAPI.Unshipped.txt and the coverage manifests through mechanical-fixer.

REFERENCE FILES:
- src/Encina/Validation/ValidationResult.cs
- src/Encina.FluentValidation/FluentValidationProvider.cs (:73-77)
- tests/Encina.UnitTests/Core/ValidationResultTests.cs
```

</details>

---

### Phase 2: Options, Message Template and Plural Rules (pull request S2)

> **Goal**: The pure building blocks, tested alone; nothing is registered or called yet.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina/Validation/ValidationLocalizationOptions.cs` (`DefaultCulture` default `"en-US"`, `SupportedCultures` as `IList<string>`, `ResourceType` as `Type?`; XML docs and an example) and `ValidationLocalizationOptionsValidator.cs : IValidateOptions<ValidationLocalizationOptions>` (register row C3).
2. **Create** `src/Encina/Validation/ValidationMessageTemplate.cs` (`internal static`): replaces `{Name}` placeholders from the argument bag using the given `CultureInfo` (`IFormattable.ToString(null, culture)` for numbers, dates), leaves an unknown placeholder untouched, ignores `{{`/`}}` escapes, never throws on a malformed template (register rows C2, C11).
3. **Create** `src/Encina/Validation/PluralRules.cs` (`public static`): `PluralCategory Select(CultureInfo culture, decimal count)` with a table for the neutral/English, Spanish and French rules and `Other` for any culture without a rule; `PluralCategory` enum (`Zero`, `One`, `Two`, `Few`, `Many`, `Other`). A `crap-exempt` single-question switch is marked only if it stays one switch.
4. **Modify** `PublicAPI.Unshipped.txt` and `.github/coverage-manifest/Encina.json` through `mechanical-fixer`.
5. Tests (new): `ValidationMessageTemplateTests` (placeholders, culture formatting of `1234.5` in `en-US` and `es-ES`, unknown placeholder, escapes, malformed input), `PluralRulesTests` (English, Spanish, French boundaries, `0`, `1`, `2`, large counts), `ValidationLocalizationOptionsValidatorTests` (every rule of row C3), `ValidationMessageTemplateProperties` (FsCheck: the template never throws and its output contains no brace introduced by an argument value).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
Issue #233 plan docs/plans/validation-localization-implementation-plan-233.md, phase 2 of 6. Pure components in
src/Encina/Validation/; no DI yet.

TASK:
1. Options + IValidateOptions validator; internal ValidationMessageTemplate (named placeholders, culture formatting);
   PluralRules with a rule table for en/es/fr and Other as the fallback.
2. Property test: the template engine never throws for any template and any argument bag.

KEY RULES:
- Placeholders are looked up by name only; nothing is evaluated; an argument value can never inject a placeholder.
- XML docs on every public member; PublicAPI.Unshipped.txt and manifests through mechanical-fixer.

REFERENCE FILES:
- src/Encina.Compliance.Consent/ConsentOptionsValidator.cs (options validator idiom)
- src/Encina/Validation/ValidationResult.cs
```

</details>

---

### Phase 3: Localizer, Culture Resolution, Registration and Logging (pull request S3)

> **Goal**: The localizer abstraction, its resource-file implementation, the culture resolver, the registration and the EventIds.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina/Validation/IValidationMessageLocalizer.cs` (`ValidationError Localize(ValidationError error, CultureInfo culture)`; returns the error unchanged when it has no code or no resource), `ResourceManagerValidationMessageLocalizer.cs` (sealed; reads the `ResourceType`'s `ResourceManager`, applies the fallback chain of Design Choice 6, picks a plural key through `PluralRules` when `Arguments` holds `Count`, renders with `ValidationMessageTemplate`; optional trailing `ILogger<ResourceManagerValidationMessageLocalizer>? logger = null` for ids 601 and 602, a null logger is a valid case), `IValidationCultureResolver.cs` and `AmbientValidationCultureResolver.cs` (optional trailing logger for id 600; `CultureInfo Resolve(IRequestContext context)`; returns the member of `SupportedCultures` per register row C1, else `DefaultCulture`), `ValidationLocalizationServiceCollectionExtensions.cs` (`AddEncinaValidationLocalization(this IServiceCollection, Action<ValidationLocalizationOptions>? configure = null)`: options with `ValidateOnStart`, `TryAddSingleton` of localizer and resolver; no hosted service).
2. **Modify** `src/Encina/Diagnostics/EventIdRanges.cs`: register `ValidationLocalization = (600, 609)`; **create** `src/Encina/Validation/ValidationLocalizationLog.cs` (`[LoggerMessage]`, packed): 600 culture not in the allow-list, fell back to the default (debug: default culture name), 601 no resource for a key (debug: key, culture), 602 no plural rule for the culture, used `Other` (debug: culture). 603-609 stay free. XML docs name the range (`Event IDs: 600-602 (see EventIdRanges.ValidationLocalization)`).
3. **Modify** `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` (assembly `Encina` also maps to the new range), `PublicAPI.Unshipped.txt` and `.github/coverage-manifest/Encina.json` (through `mechanical-fixer`).
4. Tests (new): `ResourceManagerValidationMessageLocalizerTests` (exact, parent and default fallback, missing key returns the original error, plural key choice, uncoded error unchanged), `AmbientValidationCultureResolverTests` (allow-list, `fr-CA` falls to `fr-FR`, hostile strings such as an empty string, a 5 kB string and `"xx-INVALID"` fall to the default), `ValidationLocalizationLogTests` (EventIds and no argument values in the rendered messages), `ValidationLocalizationGuardTests` (null checks for the two public method parameters and the extension; a null `configure` is a valid case), `ValidationLocalizationRegistrationTests` (DI: builds with `ValidateOnBuild` and `ValidateScopes`, resolves localizer and resolver, an invalid option fails at start-up).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
Issue #233 plan docs/plans/validation-localization-implementation-plan-233.md, phase 3 of 6. Depends on S1 and S2.
ADR-021: register the EventId range in src/Encina/Diagnostics/EventIdRanges.cs before using it.

TASK:
1. IValidationMessageLocalizer + ResourceManager implementation with the fallback chain, plural keys and the template.
2. IValidationCultureResolver with an ambient default constrained by the SupportedCultures allow-list.
3. AddEncinaValidationLocalization (options with ValidateOnStart, TryAddSingleton) and ValidationLocalizationLog 600-602.

KEY RULES:
- Never create a CultureInfo from a string that is not in the allow-list; never throw for a missing translation.
- Logs carry codes and culture names only. The localizer is a singleton with no scoped dependency.
- EncinaEventIdAllocationTests must map the Encina assembly to the new range.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
- src/Encina/Validation/ValidationOrchestrator.cs
```

</details>

---

### Phase 4: Orchestrator Integration and Provider Equivalence (pull request S4)

> **Goal**: The orchestrator localizes under the resolved culture for all three providers.

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `src/Encina/Validation/ValidationOrchestrator.cs`: optional trailing constructor parameters `IValidationMessageLocalizer? localizer = null, IValidationCultureResolver? cultureResolver = null` (a null localizer keeps today's behavior; the 13 `new ValidationOrchestrator(...)` sites in `ValidationPipelineBehaviorContractTests.cs` and `ValidationPipelineBehaviorGuardTests.cs` compile unchanged, verified with `git grep -c "new ValidationOrchestrator(" -- tests`). When both are present: resolve the culture from the context, run the provider inside an internal `ValidationCultureScope` (register row C5), localize each coded error of an invalid result, then build the message as today. The code-less `Left` is #3415's and is not changed; the independent catch added by #230 sits next to this code, and whichever pull request lands second rebases a few lines.
2. **Modify** `PublicAPI.Unshipped.txt` and `.github/coverage-manifest/Encina.json` through `mechanical-fixer`.
3. Tests (new): `ValidationOrchestratorLocalizationTests` (unit: a coded error localized in `es-ES`, an unsupported culture, a missing key, cancellation during localization, scope restored after an exception); `ValidationLocalizationEquivalenceTests` in `tests/Encina.UnitTests/Validation/` (the same request, an equivalent `[Required]`/`NotEmpty` rule per provider, `es-ES`: each provider returns Spanish text by its native mechanism; the three project references already exist in `Encina.UnitTests`); `tests/Encina.IntegrationTests/Validation/{DataAnnotations,FluentValidation,MiniValidator}/LocalizedValidationPipelineTests.cs` (through `IEncina.Send` with the real pipeline behavior and the culture set per test; no containers); the orchestrator guard tests gain the null-localizer valid case (edit of `ValidationPipelineBehaviorGuardTests.cs`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT:
Issue #233 plan docs/plans/validation-localization-implementation-plan-233.md, phase 4 of 6. Depends on S1-S3.
The orchestrator is the single point all three validation providers pass through.

TASK:
1. Add optional trailing localizer and culture resolver parameters to ValidationOrchestrator; run the provider inside
   a culture scope (set CurrentCulture and CurrentUICulture, restore in finally) and localize coded errors.
2. Write the equivalence unit test and the three integration tests through IEncina.Send.

KEY RULES:
- A null localizer means exactly today's behavior; existing new ValidationOrchestrator(provider) sites compile unchanged.
- Do not change the code-less Left of an invalid result (that is #3415's); never log or tag a rendered message.

REFERENCE FILES:
- src/Encina/Validation/ValidationOrchestrator.cs
- tests/Encina.ContractTests/Core/Validation/ValidationPipelineBehaviorContractTests.cs
- tests/Encina.GuardTests/Core/Validation/ValidationPipelineBehaviorGuardTests.cs
```

</details>

---

### Phase 5: Default Resources and the `IStringLocalizer` Adapter (pull request S5)

> **Goal**: The shipped message set and the ASP.NET Core integration.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina/Validation/ValidationMessageKeys.cs` (constants `validation.required`, `validation.min_length`, `validation.max_length`, `validation.range`, `validation.email`, `validation.pattern`, `validation.equal`, `validation.not_equal`, `validation.greater_than`, `validation.less_than`, `validation.min_items.one`/`.other`, `validation.max_items.one`/`.other`) and the resource files `src/Encina/Validation/Resources/ValidationMessages.resx` (neutral), `ValidationMessages.es-ES.resx`, `ValidationMessages.fr-FR.resx` (register row C10); the default `ResourceType` of `AddEncinaValidationLocalization` points at them when the application sets none.
2. **Create** `src/Encina.AspNetCore/Validation/StringLocalizerValidationMessageLocalizer.cs` (sealed; takes `IStringLocalizerFactory`; same fallback and plural rules; the culture is set through the scope, not through `WithCulture`) and `.../ValidationLocalizationApplicationExtensions.cs` (`AddEncinaStringLocalizerValidation(this IServiceCollection)`: `TryAddSingleton` of the adapter, replacing the resource-file localizer when called first or last, in any order).
3. **Modify** `src/Encina/PublicAPI.Unshipped.txt`, `src/Encina.AspNetCore/PublicAPI.Unshipped.txt` and both coverage manifests (`Encina.json`, `Encina.AspNetCore.json`) through `mechanical-fixer`.
4. Tests (new): `ValidationMessagesResourceTests` (every key exists in the neutral, `es-ES` and `fr-FR` sets and every plural base key has `.one` and `.other`; keys and placeholders match across the three), `StringLocalizerValidationMessageLocalizerTests` (unit with an `IStringLocalizerFactory` fake), `StringLocalizerValidationMessageLocalizerGuardTests`, `StringLocalizerRegistrationTests` (DI with `ValidateOnBuild` and `ValidateScopes`, and the registration-order test: the adapter wins in either order without overriding the application's own localizer).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT:
Issue #233 plan docs/plans/validation-localization-implementation-plan-233.md, phase 5 of 6. Depends on S3 and S4.

TASK:
1. Add the message key constants and the three resource sets (neutral, es-ES, fr-FR) under src/Encina/Validation/Resources.
2. Add the IStringLocalizer adapter and its registration to Encina.AspNetCore.

KEY RULES:
- The Spanish and French texts are reviewed by a native speaker; placeholders must be identical across cultures.
- The adapter and the resource-file localizer both register with TryAdd; the AddEncinaStringLocalizerValidation
  registration wins in any order without overriding an application-supplied localizer.

REFERENCE FILES:
- src/Encina.AspNetCore/Encina.AspNetCore.csproj
- src/Encina/Validation/ResourceManagerValidationMessageLocalizer.cs (from phase 3)
```

</details>

---

### Phase 6: Documentation and Finalization (pull request S6)

> **Goal**: Usage, README updates, changelog and justification files.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `docs/features/validation-localization.md` through `docs-writer` (the layers, the culture scope, `UseRequestLocalization` with Encina, the three provider paths, plural keys, writing a new culture, the `IStringLocalizer` adapter, security notes).
2. **Modify** `src/Encina.FluentValidation/README.md`, `src/Encina.DataAnnotations/README.md`, `src/Encina.MiniValidator/README.md` and `src/Encina.AspNetCore/README.md` through `docs-writer`.
3. **Create** `changelog.d/233-validation-localization.added.md` through `mechanical-fixer`.
4. **Create** the justification files for Load and Benchmark tests (`AGENTS.md` section 9): a per-error dictionary lookup with no I/O and no shared mutable state.
5. Verification: `dotnet build --configuration Release` with zero warnings, `dotnet test` of the touched projects, per-flag coverage of the touched files against the table below.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```text
CONTEXT:
Issue #233 plan docs/plans/validation-localization-implementation-plan-233.md, phase 6 of 6.

TASK:
1. Document the feature and update the four READMEs; add the changelog fragment and the two justification files.
2. Build and measure per-flag coverage against the manifest targets.

KEY RULES:
- Pages go through docs-writer; fragments through mechanical-fixer. Never type coverage figures by hand: cite cov: markers.
- Link ADRs by number; link sibling plans by issue number only.

REFERENCE FILES:
- .claude/skills/encina-docs/SKILL.md
- changelog.d/README.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Use in this feature |
|--------|-----------|---------------------|
| .NET resource fallback process | Specific culture, then parent, then neutral | Fallback chain (Design Choice 6) |
| `CultureInfo.CurrentCulture` / `CurrentUICulture` | Stored per logical execution context, flows across `await` | The culture scope (register row C5) |
| CLDR plural rules (Unicode TR35, "Language Plural Rules") | Plural categories `zero`, `one`, `two`, `few`, `many`, `other` per language | The `PluralRules` table (Design Choice 3) |
| FluentValidation 12.1.1 | `ValidationFailure.ErrorCode`, `FormattedMessagePlaceholderValues`; built-in translations follow the current UI culture | Mapping in Phase 1; why the culture scope localizes Fluent natively |
| `System.ComponentModel.DataAnnotations` | `ErrorMessageResourceType`/`ErrorMessageResourceName` read through the current UI culture | Why DataAnnotations and MiniValidator need no code |
| OWASP ASVS 4.0 V5.1.3, V5.2.4 | Allow-list validation of all input; no dynamic evaluation of user-controlled data | Register rows C1 and C11 |
| ADR-054 D3, D4 | One meter/source per package; tenant tag only when multi-tenancy is on | Why telemetry stays with #726 (row C7) |
| ADR-062 | Error category contract | No error code is created (row C6) |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `ValidationOrchestrator`, `ValidationResult`, `ValidationError` | `src/Encina/Validation/` | Integration point; the record gains two optional fields |
| `FluentValidationProvider` | `src/Encina.FluentValidation/FluentValidationProvider.cs` | Source of codes and arguments |
| Options validator idiom | `src/Encina.Compliance.Consent/ConsentOptionsValidator.cs` | Model for `ValidationLocalizationOptionsValidator` |
| `EventIdRanges`, `EncinaEventIdAllocationTests` | `src/Encina/Diagnostics/EventIdRanges.cs`, `tests/Encina.UnitTests/Testing/Architecture/` | Range registration |
| `Microsoft.AspNetCore.App` framework reference | `src/Encina.AspNetCore/Encina.AspNetCore.csproj` | `IStringLocalizerFactory` for the adapter |
| Provider unit, guard and integration folders | `tests/Encina.UnitTests/{FluentValidation,DataAnnotations,MiniValidator}`, `tests/Encina.IntegrationTests/Validation/` | Where the new tests sit (integration folders hold only `GlobalSuppressions.cs` today) |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina` core (`ValidationLocalization`, new, inside the block 600-649 assigned to the validation-2 family) | 600-609 | 600-602 used (three messages); 603-609 free; sibling plans in the same block: #234 610-619, #235 620-629, #236 630-639; #702 needs none |

### Estimated File Count

Every count includes tests, coverage manifest edits, PublicAPI files, the README and the changelog fragment. No pull request uses a type created by a later one; each enters its per-file manifest targets with its own tests.

| PR | Content | New src | Changed src | New tests | Edited tests | Manifest | Other | Files | Lines (est.) |
|----|---------|---------|-------------|-----------|--------------|----------|-------|-------|--------------|
| S1 | Coded `ValidationError`, Fluent mapping | 0 | 3 (`ValidationResult.cs`, `FluentValidationProvider.cs`, PublicAPI) | 1 (Fluent code unit) | 1 (`ValidationResultTests.cs`) | 2 (`Encina.json`, `Encina.FluentValidation.json`) | 0 | 7 | ~300 |
| S2 | Options, template, plural rules | 4 (options, options validator, template, `PluralRules`) | 1 (PublicAPI) | 4 (template, plural, options validator, template property) | 0 | 1 | 0 | 10 | ~800 |
| S3 | Localizer, resolver, registration, logging | 6 (`IValidationMessageLocalizer`, `ResourceManagerValidationMessageLocalizer`, `IValidationCultureResolver`, `AmbientValidationCultureResolver`, extensions, `ValidationLocalizationLog`) | 2 (`EventIdRanges.cs`, PublicAPI) | 5 (localizer, resolver, log, guard, DI) | 1 (`EncinaEventIdAllocationTests.cs`) | 1 | 0 | 15 | ~1,000 |
| S4 | Orchestrator integration, equivalence | 0 | 2 (`ValidationOrchestrator.cs`, PublicAPI) | 5 (orchestrator unit, equivalence unit, 3 integration) | 1 (`ValidationPipelineBehaviorGuardTests.cs`) | 1 | 0 | 9 | ~800 |
| S5 | Resources and `IStringLocalizer` adapter | 5 (`ValidationMessageKeys`, 3 `.resx`, adapter) + 1 (adapter extension) | 2 (2 PublicAPI) | 4 (resource, adapter unit, adapter guard, adapter DI) | 0 | 2 | 0 | 14 | ~900 |
| S6 | Documentation | 0 | 4 (4 READMEs) | 0 | 0 | 0 | 4 (feature page, changelog fragment, 2 justification `.md`) | 8 | ~450 |
| **Total** | | **16** | **14** | **19** | **3** | **7** | **4** | **63** | **~4,250** |

The columns sum to the total (16 + 14 + 19 + 3 + 7 + 4 = 63) and the per-pull-request rows sum to 63 files (7 + 10 + 15 + 9 + 14 + 8). The largest is S3 with 15 files, at the limit, and none exceeds 1,000 lines. S1 comes first; S2 is independent of S1; S3 needs S1 and S2; S4 needs S1 to S3; S5 needs S3 and S4; S6 needs all. Verify caller counts before each pull request with `git grep`, and keep S3 at 15 files (move the logging to a separate pull request if a file is added).

Per-file coverage targets (per flag, with a one-sentence justification), written into the package manifests by the pull request that adds the file:

| File | Unit | Guard | Property | Justification |
|------|------|-------|----------|---------------|
| `ValidationMessageTemplate.cs` | 95 | 0 | 70 | Internal static type without argument validation beyond tolerance of any input, so guard tests cannot reach it (internal, no `InternalsVisibleTo` for `Encina.GuardTests`); the property test drives arbitrary templates and bags |
| `PluralRules.cs` | 95 | 90 | 60 | Every culture rule and boundary is reachable with `CultureInfo`; the guard tests reach the null culture check; the property test covers monotonic categories |
| `ValidationLocalizationOptionsValidator.cs` | 95 | 0 | 0 | Pure rule checks on one options object; no further parameters for guard tests |
| `ResourceManagerValidationMessageLocalizer.cs`, `AmbientValidationCultureResolver.cs` | 90 | 85 | 0 | Fallback, plural and allow-list branches are reachable with in-test resource sets and hostile strings; the guard tests reach the null checks |
| `ValidationLocalizationServiceCollectionExtensions.cs` | 90 | 90 | 0 | Registration is fully exercised by the DI test; null checks are guard-tested |
| `ValidationLocalizationLog.cs` | 90 | 0 | 0 | Source-generated methods exercised by the logging test; no parameters to guard |
| `ValidationOrchestrator.cs` (changed methods) | 90 | 85 | 0 | The new branches (no localizer, supported culture, fallback, scope restore) each have a unit test; the optional parameters are guard-tested |
| `ValidationResult.cs`, `FluentValidationProvider.cs` (changed members) | 95 | 85 | 0 | A coded and an uncoded error, and a failing rule with arguments, are reachable by unit tests; existing guards stay |
| `StringLocalizerValidationMessageLocalizer.cs` and its extension | 90 | 85 | 0 | The adapter is exercised with a fake factory; the guard tests reach the null checks; registration is covered by the DI test |
| `ValidationMessageKeys.cs` | 0 | 0 | 0 | Constants only, no executable code; correctness of the keys is asserted by `ValidationMessagesResourceTests` |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #233 - localization of validation errors for the three Encina validation providers.

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library; pre-1.0, no backward compatibility, best solution always.
- Validation follows the orchestrator pattern (AGENTS.md sections 3 and 5): IValidationProvider, ValidationOrchestrator,
  ValidationPipelineBehavior live in core; FluentValidation, DataAnnotations and MiniValidator stay consistent.
- No new EncinaError code is created; logs carry codes and culture names only.

IMPLEMENTATION OVERVIEW:
1. ValidationError gains optional ErrorCode and Arguments; FluentValidation fills them.
2. Options, a named-placeholder template engine and plural rules; an IValidationMessageLocalizer (resource files) and
   a culture resolver constrained by a SupportedCultures allow-list; EventIds 600-602 in a range registered first.
3. ValidationOrchestrator localizes coded errors and runs the provider inside a culture scope, so native localization
   of FluentValidation and DataAnnotations follows the same culture.
4. Default resources (neutral, es-ES, fr-FR) and an IStringLocalizer adapter in Encina.AspNetCore.
Six pull requests (S1 to S6), see the Estimated File Count table.

KEY PATTERNS:
- Optional trailing constructor parameters keep existing tests compiling; DI tests with ValidateOnBuild and ValidateScopes.
- Never create a CultureInfo from a string outside the allow-list; never throw for a missing translation.
- PublicAPI.Unshipped.txt, manifests and changelog through mechanical-fixer; documentation through docs-writer.

REFERENCE FILES:
- src/Encina/Validation/*.cs
- src/Encina.FluentValidation/FluentValidationProvider.cs
- docs/plans/validation-localization-implementation-plan-233.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | The issue's note holds: messages are resource-based and cheap; `ResourceManager` caches resource sets itself, so Encina adds no cache layer |
| 2 | OpenTelemetry | ⏭️ #726 | #726 instruments `ValidationOrchestrator` centrally; a culture attribute is added there from the allow-list (bounded cardinality) with the tenant tag only when multi-tenancy is on (ADR-054 D4); this plan adds no instrument |
| 3 | Structured Logging | ✅ Phase 3 | `ValidationLocalizationLog` EventIds 600-602 in the new range 600-609, by code and culture only |
| 4 | Health Checks | ❌ N/A | In-process; a missing translation never degrades health |
| 5 | Validation | ✅ All phases | This is the validation feature; the three providers of `AGENTS.md` section 5 stay consistent through the equivalence test |
| 6 | Resilience | ❌ N/A | No external call; a custom `IStringLocalizer` that calls a service is the application's concern |
| 7 | Distributed Locks | ❌ N/A | No shared state to coordinate |
| 8 | Transactions | ❌ N/A | Validation runs before the handler |
| 9 | Idempotency | ❌ N/A | Localization is deterministic for a given culture and key |
| 10 | Multi-Tenancy | ⏭️ Follow-up | Tenant-specific locale preferences (the issue defers them): draft `artifacts/issues/validation-tenant-module-scoped-resources.md`; no tenant id is stored or keyed here, so there is no tenant-qualified key to make injective |
| 11 | Module Isolation | ⏭️ Follow-up | Module-scoped resource sets: the same draft |
| 12 | Audit Trail | ⏭️ Follow-up | Auditing of rejected requests with the locale is a follow-up of the family (accompanies the plan of #230); nothing here changes security state |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| Validation orchestrator pattern (`IValidationProvider`, `ValidationOrchestrator`) | ✅ On `main` | Integration point exists |
| ADR-062 error category contract | ✅ Merged | Not exercised (no new error code) |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| `IRequestValidator<T>` and the runner | #230 | Producers of coded errors; the orchestrator edits of #230 and this plan touch different lines |
| Code-less validation `Left` never becomes a 400 | #3415 | This plan keeps the existing invalid-result `Left` as is |
| Which error text a client may see | #3426 | Decides whether the English summary of register row C9 reaches clients |
| Central validation instrumentation | #726 | Owns the culture attribute |
| Message consumer of this plan | #235 | Uses the keys, `ErrorCode` and the localizer; planned after this plan |

---

## Next Steps

1. Link this plan from #233 and ask the maintainer to answer Design Choices 1 to 5 and register rows C9 and C10 when work starts.
2. Open the follow-up issue when the first pull request opens: tenant and module scoped locale and resources (draft: `artifacts/issues/validation-tenant-module-scoped-resources.md`).
3. Implement S1 to S6 as the pull requests of the Estimated File Count split, `Fixes #233` on S6.
