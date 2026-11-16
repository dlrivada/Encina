# Implementation Plan: Validation Error Localization (i18n)

> **Issue**: [#233](https://github.com/dlrivada/Encina/issues/233)
> **Type**: Feature
> **Related**: #230 (owns `IRequestValidator<T>`, the runner and EventIds 550-559; its orchestrator change is independent of this one), #227 (generated validators emit codes this plan localizes), #234 (aggregation, planned apart), #235 (schema builder, consumes the `ErrorCode` contract, the message keys and the localizer defined here), #236 (two-phase validation, planned apart), #726 (central validation instrumentation), #3415 (a code-less validation `Left` never becomes a 400), #3426 (which error text a client may see), ADR-054, ADR-062 (merged: error category contract)
> **Complexity**: Medium (core mechanism plus one ASP.NET Core adapter; no persistence)
> **Estimated Scope**: ~5,100 lines in total (~900 production, ~3,700 tests, ~500 resources and documentation), in seven pull requests of at most 15 files and 1,500 lines each

---

## Summary

### What exists today (read on 2026-11-16, main `8e01577ef`)

- A validation failure is a `ValidationError(string? PropertyName, string ErrorMessage)`, a two-field positional record (`src/Encina/Validation/ValidationResult.cs:97`), and the message is whatever string the provider library produced. `ValidationOrchestrator` joins those messages into the text of a code-less `Left` (`ValidationOrchestrator.cs:70-71`, via `ValidationResult.ToErrorMessage`, `ValidationResult.cs:77`).
- Nothing in `src` mentions localization: `git grep -n -E "Microsoft.Extensions.Localization|IStringLocalizer" -- src '*.csproj' '*.props'` finds nothing, `IRequestContext` carries `CorrelationId`, `CausationId`, `Identity`, `IdempotencyKey`, `TenantId`, `Timestamp` and `Metadata` but no culture, and there is no `AddEncinaValidation(options)` entry point (the issue's snippet is aspirational; each provider has its own `Add...` method).
- What already works with no Encina code: the issue's own "Usage" example (a FluentValidation validator that takes `IStringLocalizer<T>` and calls `WithMessage(localizer[...])`) is plain application code; FluentValidation translates its built-in messages by `CultureInfo.CurrentUICulture`; DataAnnotations attributes read `ErrorMessageResourceType`/`ErrorMessageResourceName` through the same culture. `Encina.AspNetCore` is a framework reference to `Microsoft.AspNetCore.App` (`Encina.AspNetCore.csproj`), which ships `Microsoft.Extensions.Localization` and the `UseRequestLocalization` middleware that sets the current culture per HTTP request.
- The three providers lose the machine-readable identity of a failure: `FluentValidationProvider` builds `new ValidationError(failure.PropertyName, failure.ErrorMessage)` (`FluentValidationProvider.cs:76`) although `ValidationFailure` carries `ErrorCode` (the validator type name, for example `NotEmptyValidator`, or an application `WithErrorCode` value) and the placeholder values; `DataAnnotationsValidationProvider` (`:57`) and `MiniValidationProvider` (`:39`) build the error from a message string only.
- Not in `src`: any message key, any resource file for validation, any plural handling, any culture allow-list, any `IValidationMessageLocalizer`.

### What this plan delivers

An opt-in localization layer for validation errors, provider-neutral because it sits in the one place all three providers pass through, `ValidationOrchestrator`:

- `ValidationError` gains an optional `ErrorCode` (the lookup key) and optional named `Arguments` (the placeholder values). `FluentValidationProvider` fills them from `ValidationFailure`, translating the validator names to the message keys through a mapping table (below).
- `IValidationMessageLocalizer` turns a coded error and a culture into a message: named placeholders (`{MinLength}`), plural variants chosen by a per-culture plural rule, culture-aware formatting of numbers and dates, and a fallback chain (exact culture, parent cultures, default culture, the provider's own message). The default implementation reads .NET resource files; an ASP.NET Core adapter reads `IStringLocalizer`.
- A culture allow-list (`SupportedCultures`) and a resolver (default: the current UI culture, which `UseRequestLocalization` already sets) decide the culture; the orchestrator runs the provider inside a scope that sets that culture, so the native localization of FluentValidation and DataAnnotations follows it too (DataAnnotations and MiniValidator errors carry no code, so Encina cannot re-key them; the culture scope is what localizes them).
- Default resource files (neutral, `es-ES`, `fr-FR`) for the common rule messages that provider-neutral producers (#227, #230, #235) use.

Nothing changes for an application that does not call `AddEncinaValidationLocalization`.

- Affected packages: `Encina` (core), `Encina.FluentValidation` (code, mapping and arguments), `Encina.AspNetCore` (`IStringLocalizer` adapter). `Encina.DataAnnotations` and `Encina.MiniValidator` are covered through the culture scope and their native resources, without code changes.
- Provider category: Validation (3) (`AGENTS.md` section 5): one equivalence unit test proves the same request in a non-default culture yields a localized message under each provider, each by its native mechanism (Design Choice 1).
- Shared pieces, owned by this plan: `ValidationError.ErrorCode` and `ValidationError.Arguments`, the `ErrorCode` contract below, `ValidationMessageKeys`, `IValidationMessageLocalizer`, `ValidationCultureScope` and the default resource set; #235 and the other producers build on them.

### The `ErrorCode` contract (owned by this plan, register rows C12 and C13)

| Aspect | Contract |
|--------|----------|
| Purpose | `ErrorCode` is the in-process lookup key between a producer of a validation failure and the localizer. It is not a client-facing field: the orchestrator flattens the errors into the text of a code-less `Left` (`ValidationOrchestrator.cs:70-71`), so the code never reaches `EncinaError`; whether a client ever sees structured codes is decided by #3415 and #3426 |
| Format | Case-sensitive, compared with `StringComparison.Ordinal`; Encina-minted codes match `validation.<rule>` with `<rule>` in lower snake case (`validation.min_length`), one or more dot-separated segments of `[a-z][a-z0-9_]*`; stable once published (a rename is a breaking change of the key set) |
| Plural variants | Not part of the code: the localizer appends `.one`, `.other`, ... to the code to look up the plural resource (`validation.min_items` becomes `validation.min_items.one`); a plural base key has at least `.one` and `.other` |
| Who may mint | Encina packages mint only the constants of `ValidationMessageKeys` (#227, #230 and #235 use them); an application mints any code outside the `validation.` prefix and supplies its own `ResourceType`; a code with no resource leaves the provider message untouched |
| Placeholders | Named, `PascalCase`, taken from `Arguments`; for FluentValidation the names are FluentValidation's own (`MinLength`, `MaxLength`, `TotalLength`, `ComparisonValue`, `From`, `To`, `PropertyName`); `PropertyValue` is never copied (user input) |
| Absent | `null` for DataAnnotations and MiniValidator errors, which carry no code; their messages are localized by the culture scope and their native resources |

Mapping of FluentValidation built-in validators to the keys, implemented in S1 as the internal `FluentValidationErrorCodeMap` (validator names verified against `FluentValidation.dll` 12.1.1, which defines each of these validator types):

| FluentValidation `ErrorCode` | `ErrorCode` stored in `ValidationError` | Placeholders in `Arguments` |
|------------------------------|-----------------------------------------|-----------------------------|
| `NotEmptyValidator`, `NotNullValidator` | `validation.required` | `PropertyName` |
| `MinimumLengthValidator` | `validation.min_length` | `MinLength`, `TotalLength` |
| `MaximumLengthValidator` | `validation.max_length` | `MaxLength`, `TotalLength` |
| `LengthValidator`, `ExactLengthValidator` | `validation.length` | `MinLength`, `MaxLength`, `TotalLength` |
| `InclusiveBetweenValidator` | `validation.range` | `From`, `To` |
| `ExclusiveBetweenValidator` | `validation.range_exclusive` | `From`, `To` |
| `EmailValidator`, `AspNetCoreCompatibleEmailValidator` | `validation.email` | `PropertyName` |
| `RegularExpressionValidator` | `validation.pattern` | `PropertyName` |
| `EqualValidator` / `NotEqualValidator` | `validation.equal` / `validation.not_equal` | `ComparisonValue` |
| `GreaterThanValidator` / `GreaterThanOrEqualValidator` | `validation.greater_than` / `validation.greater_than_or_equal` | `ComparisonValue` |
| `LessThanValidator` / `LessThanOrEqualValidator` | `validation.less_than` / `validation.less_than_or_equal` | `ComparisonValue` |
| any other value (an application `WithErrorCode`, `PredicateValidator`, ...) | copied unchanged | all placeholders except `PropertyValue` |

An unmapped code is looked up in the application's `ResourceType` and, when absent, leaves the provider message. `ValidationMessageKeys` (S1) also lists `validation.min_items` and `validation.max_items` (plural bases): FluentValidation has no built-in collection-size validator, so these keys are minted by the neutral producers (#227, #230, #235).

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

- The result of any resolver is matched against `SupportedCultures` by the algorithm of register row C18 (exact, then same language, then `DefaultCulture`); a name that is not in the allow-list is never turned into a `CultureInfo`, so a client-controlled header cannot create arbitrary culture objects or throw `CultureNotFoundException`.
- B wins when the maintainer wants the culture to be part of the request identity for audit and replay; C wins for a minimal API without `UseRequestLocalization`.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Parameterized messages and pluralization</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Named placeholders (`{MinLength}`) from `ErrorCode`/`Arguments`, and plural variants as separate keys (`validation.min_items.one`, `.other`) chosen by a small per-culture rule table (CLDR categories `zero`, `one`, `two`, `few`, `many`, `other`)** | Translators edit plain resource files; no template language to parse; rules are plain code with property tests | Encina ships and maintains plural rules for the cultures it supports (English, Spanish, French need only `one` and `other`; a culture without a rule uses `other`) |
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
| **A) Two optional trailing parameters on the existing record: `ValidationError(string? PropertyName, string ErrorMessage, string? ErrorCode = null, IReadOnlyDictionary<string, object?>? Arguments = null)`** | One type everywhere; every existing two-argument construction (14 sites: 3 in `src` and 11 in tests, verified with `git grep -c "new ValidationError(" -- src tests`) compiles unchanged | Record equality treats `Arguments` by reference, so two errors with equal but distinct dictionaries differ; `Deconstruct` gains two outputs |
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
| **A) Resource lookup for the selected culture and its parents, then for `DefaultCulture` and its parents, then the message the provider produced; never an exception for a missing key, never an empty message** | A missing translation degrades to readable text; the same parent order .NET resource lookup uses | A silently missing translation is invisible unless logged (the debug log of Phase 4 names the key and culture) |
| **B) Throw when a supported culture lacks a key** | Missing translations are found early | A request fails because of a translation gap; a validation gate must not fail for presentation reasons |

### Chosen Option: **A — Resource fallback chain ending at the provider's message**

### Rationale

- The order is the documented .NET resource fallback process, and ending at the provider's own message guarantees a non-empty result. Which culture is selected before the lookup (so that `fr-CA` reaches the `fr-FR` resources) is a separate decision, row C18.

**Class:** Trivial
**Settled by:** Microsoft documentation, "Packaging and Deploying Resources in .NET Apps", section "The resource fallback process" (specific culture, then parent, then neutral) and the issue's own requirement "Fallback chain for missing translations"

</details>

### Register of defaults and semantics

| Id | Choice | Class | Basis |
|----|--------|-------|-------|
| C1 | A culture string is only ever turned into a `CultureInfo` after it matched `SupportedCultures`; an unsupported or malformed culture falls back to `DefaultCulture` and never throws. The alternative is to reject the request with an error (a `Left`) when the culture is unsupported | Dilemma (recommended, pending the maintainer) | OWASP ASVS 4.0 V5.1.3 requires allow-list validation of all input but does not say whether the answer to a failed match is a fallback or a rejection; the fallback wins for a public API where a missing translation must not block a request, the rejection wins for a deployment that must prove every response was produced in a supported language |
| C2 | Logs, tags and errors carry the error code, the culture name from the allow-list and the request type name, never `Arguments` values, the rendered message or `EncinaError.Message` | Settled | `AGENTS.md` section 3 (`EncinaError.Message` never reaches logs; #1168, #1173, #1259, #1274) |
| C3 | `ValidationLocalizationOptions` are validated at start-up: `DefaultCulture` non-empty and a member of `SupportedCultures`, `SupportedCultures` non-empty with no duplicates (ordinal, ignoring case), every name matching the shape `[A-Za-z]{2,3}(-[A-Za-z0-9]{2,8})*` of at most 35 characters, `ResourceType` (when set) has a resource set | Trivial | `AGENTS.md` section 3 (registration completeness) and the repository's options-validator idiom (`IValidateOptions<T>` with `ValidateOnStart`, for example `src/Encina.Compliance.Consent/ConsentOptionsValidator.cs`) |
| C4 | The localizer is registered as a singleton and depends on nothing scoped (resource managers and `IStringLocalizerFactory` are singletons); the orchestrator keeps its existing lifetime | Settled | `AGENTS.md` section 3 (registration completeness: no scoped service into a singleton); the orchestrator is a singleton in `AddDataAnnotationsValidation` (`ServiceCollectionExtensions.cs:73`) and `AddMiniValidation` and scoped in `AddEncinaFluentValidation` (`ServiceCollectionExtensions.cs:63`), so only a singleton localizer is safe for all three |
| C5 | The culture scope (`ValidationCultureScope`, public, so the adapter and applications reuse it) sets `CurrentUICulture` only, around the provider call and the localization, and restores it in a `finally`; `CurrentCulture` is left alone. The alternative sets `CurrentCulture` too. The scope is entered before the provider starts its work, so the culture flows with the `ExecutionContext` into the tasks `FluentValidationProvider` creates for `Task.WhenAll` (`FluentValidationProvider.cs:70`); the equivalence test asserts it | Dilemma (recommended, pending the maintainer) | Both values flow with the execution context ([.NET documentation, `CultureInfo.CurrentCulture`](https://learn.microsoft.com/en-us/dotnet/api/system.globalization.cultureinfo.currentculture)); UI only wins when validators parse or compare user input (`decimal.Parse`, date parsing inside a `Must`), which must not change with a request header; both win when native messages must also format numbers and dates in the user's language (Encina's own templates always pass the culture explicitly) |
| C6 | No new `EncinaError` code is created: a localization problem never turns a valid request invalid, a misconfiguration fails at start-up through options validation, so there is no ADR-062 `ErrorCategory` to assign | Settled | ADR-062 contract (a category is chosen where an error is created; none is created) |
| C7 | OpenTelemetry for localization (a culture attribute on validation failures) is not added here: #726 instruments `ValidationOrchestrator` centrally; when it adds a culture attribute it takes the value from the allow-list (bounded cardinality) and adds the tenant tag only when multi-tenancy is on (ADR-054 D4) | Settled | Issue #726 (open, proposed solution 4); ADR-054 D3, D4 |
| C8 | EventIds 600-602 are packed from the start of the block 600-649 assigned to this family; the range `ValidationLocalization = (600, 609)` is registered first, in the pull request that first logs (S4) | Trivial | `AGENTS.md` section 7; ADR-021; `artifacts/decisions/plan-batch/eventid-blocks.md` |
| C9 | The summary text of the orchestrator's `Left` ("Validation failed for X with N error(s): ...") stays English: it is the diagnostic `EncinaError.Message`, while the per-error messages inside it are the localized part | Dilemma (recommended, pending the maintainer) | public behavior; the alternative is localizing the prefix with a key, which matters when an application shows the whole message to users (#3426 decides what a client may see) |
| C10 | Default resource cultures are the neutral (English) set plus `es-ES` and `fr-FR`, the cultures of the issue's example; the Spanish and French texts are reviewed by a native speaker before merge | Dilemma (recommended, pending the maintainer) | scope; `es-ES` alone is the alternative when no French reviewer is available |
| C11 | The template engine never evaluates code: placeholders are looked up by name in the argument bag and formatted only through `IFormattable.ToString(null, culture)` with the explicit culture | Settled | OWASP ASVS 4.0 V5.2.4 (no dynamic code execution from user-controlled data) |
| C12 | Message key naming: `ErrorCode` is `validation.<rule>` in lower snake case, plural variants add `.one`, `.other`, ... (the contract table above). The alternative adopts FluentValidation's own names (`NotEmptyValidator`) as the keys | Dilemma (recommended, pending the maintainer) | public name; the Encina keys win because the three providers and the neutral producers (#227, #230, #235) share one provider-independent vocabulary and resource files are readable; FluentValidation names win when only FluentValidation is used, since they need no mapping table |
| C13 | Where the FluentValidation mapping lives: in `Encina.FluentValidation` (internal `FluentValidationErrorCodeMap`), so core never knows a FluentValidation name. The alternatives put the table in the core localizer, or copy the FluentValidation `ErrorCode` unchanged and ship resources keyed by it (C12 alternative) | Dilemma (recommended, pending the maintainer) | the provider package wins because the provider-specific part stays in the provider (`AGENTS.md` section 3, provider coherence); the core table wins when a future producer outside the provider also emits FluentValidation names |
| C14 | The argument named `Count` (any numeric value convertible to `decimal`) selects the plural variant; a code with no `.one`/`.other` resources ignores it. The alternative names the plural argument per key in a small table | Dilemma (recommended, pending the maintainer) | public convention; `Count` wins for resource files a translator edits without code; the table wins when one message has two numeric arguments that could both be plural |
| C15 | A custom `IValidationMessageLocalizer` or `IStringLocalizer` that throws (other than cancellation): the exception propagates, so the request fails visibly and an invalid request can never become valid. The alternative catches it, keeps the provider message for that error, and logs the exception type through `ForLogging()` (needs a fourth EventId, 603, inside the registered range 600-609, added only if this alternative is chosen) | Dilemma (recommended, pending the maintainer) | error semantics; propagation wins because a broken localizer is a configuration bug that must not stay hidden; the catch wins when the localizer calls a remote translation service that can fail transiently and a readable provider message is better than a failed request |
| C16 | Options defaults: `DefaultCulture` `"en-US"`; `SupportedCultures` is empty in the class and the registration fills it with `en-US`, `es-ES`, `fr-FR` only when the application left it empty (so an application list replaces the default instead of being appended to it, which would duplicate entries); `ResourceType` defaults to the shipped `ValidationMessages` when the application sets none | Dilemma (recommended, pending the maintainer) | public defaults; the shipped set wins for an application that wants the three cultures with no configuration; an empty default (localization does nothing until configured) wins for a deployment that wants every culture to be an explicit decision |
| C17 | A placeholder with no argument is left untouched in the rendered text (`{MinLength}` stays as written). The alternative replaces it with an empty string | Dilemma (recommended, pending the maintainer) | output behavior; untouched wins because a missing argument is visible to the developer and never produces a misleading sentence; empty wins when text shown to end users must never contain braces |
| C18 | Culture matching (the selection that precedes the resource lookup): the requested name is matched against `SupportedCultures` by exact name ignoring case, then by language (the part before the first `-`, so `fr-CA` and `fr` both match `fr-FR`, taking the first supported entry of that language in list order), then `DefaultCulture`; the match is done on strings, a `CultureInfo` is created only for the matched member. The alternative is exact name or exact parent only (`fr-CA` then falls to `DefaultCulture`) | Dilemma (recommended, pending the maintainer) | user-visible behavior; language matching wins when the application ships regional variants such as `es-ES` and a visitor from `es-MX` should read Spanish; exact matching wins when regional wording differs enough that a wrong region is worse than the default |
| C19 | Registration mechanism for the adapter: `AddEncinaStringLocalizerValidation` inspects the existing `IValidationMessageLocalizer` descriptor and replaces it only when it is Encina's own default (`ImplementationType == typeof(ResourceManagerValidationMessageLocalizer)`), keeps an application-registered localizer, and `AddEncinaValidationLocalization` uses `TryAddSingleton` so it never replaces an adapter registered first. The alternatives are a marker service or a single registration with a selector option | Dilemma (recommended, pending the maintainer) | DI behavior; descriptor inspection wins because it needs no extra public type; the selector option wins when a third localizer source (a database store) must join the same choice |

---

## Implementation Phases

### Phase 1: Coded `ValidationError`, message keys and FluentValidation mapping (pull request S1)

> **Goal**: Errors carry a key and named arguments; FluentValidation fills them through the mapping table. Nothing consumes them yet.

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `src/Encina/Validation/ValidationResult.cs`: `public sealed record ValidationError(string? PropertyName, string ErrorMessage, string? ErrorCode = null, IReadOnlyDictionary<string, object?>? Arguments = null)` with XML docs for both parameters stating the `ErrorCode` contract (Design Choice 5, register rows C12 and C13).
2. **Create** `src/Encina/Validation/ValidationMessageKeys.cs` (`public static class`, one `const string` per key of the contract: `Required`, `MinLength`, `MaxLength`, `Length`, `Range`, `RangeExclusive`, `Email`, `Pattern`, `Equal`, `NotEqual`, `GreaterThan`, `GreaterThanOrEqual`, `LessThan`, `LessThanOrEqual`, `MinItems`, `MaxItems`, each `validation.<rule>`; XML docs).
3. **Create** `src/Encina.FluentValidation/FluentValidationErrorCodeMap.cs` (`internal static`, a frozen dictionary of the mapping table above, `string? Map(string? errorCode)` returning the key, or the input unchanged when it is not mapped, or null for null), and **modify** `src/Encina.FluentValidation/FluentValidationProvider.cs` (`:76`): build the error with `Map(failure.ErrorCode)` and the placeholder values of `failure.FormattedMessagePlaceholderValues` except `PropertyValue` (user input; register row C2 keeps values out of logs).
4. **Modify** `src/Encina/PublicAPI.Unshipped.txt` (the constructor line, the two new members, the `ValidationMessageKeys` constants) and `.github/coverage-manifest/Encina.json` and `Encina.FluentValidation.json` through `mechanical-fixer`.
5. Tests: `tests/Encina.UnitTests/Core/ValidationResultTests.cs` (edit: coded and uncoded errors, `with` expression, equality with and without arguments, null `Arguments` as a valid case); new `tests/Encina.UnitTests/FluentValidation/FluentValidationErrorCodeTests.cs` (one real FluentValidation rule per row of the mapping table yields the expected key and placeholders, so a drifting validator name fails the test; `PropertyValue` is not copied; an unmapped application code is copied unchanged; a null code stays null); new `tests/Encina.ContractTests/Core/Validation/ValidationErrorContractTests.cs` (a real `ValidationError` and a real provider result honour the shape of the contract: a null or `validation.`-prefixed lower-snake code, every `ValidationMessageKeys` constant matches the format and is unique).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
Encina (.NET 10, C# 14, pre-1.0). Issue #233 plan docs/plans/validation-localization-implementation-plan-233.md,
phase 1 of 7. ValidationError is a positional record in src/Encina/Validation/ValidationResult.cs.

TASK:
1. Add optional trailing parameters ErrorCode (string?) and Arguments (IReadOnlyDictionary<string, object?>?) to
   ValidationError; document both with the ErrorCode contract of the plan.
2. Add ValidationMessageKeys (constants) and the internal FluentValidationErrorCodeMap; in FluentValidationProvider
   copy Map(failure.ErrorCode) and the placeholders except PropertyValue (it is user input).

KEY RULES:
- Existing two-argument constructions must compile unchanged; no behavior change for a consumer that ignores the new fields.
- PublicAPI.Unshipped.txt and the coverage manifests through mechanical-fixer.
- One unit test per mapping row with a real FluentValidation rule; contract test with real objects.

REFERENCE FILES:
- src/Encina/Validation/ValidationResult.cs
- src/Encina.FluentValidation/FluentValidationProvider.cs (:76)
- tests/Encina.UnitTests/Core/ValidationResultTests.cs
```

</details>

---

### Phase 2: Options, Message Template and Plural Rules (pull request S2)

> **Goal**: The pure building blocks, tested alone; nothing is registered or called yet. Independent of S1.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina/Validation/ValidationLocalizationOptions.cs` (`DefaultCulture` default `"en-US"`, `SupportedCultures` as `IList<string>` empty by default, `ResourceType` as `Type?`; XML docs and an example; register rows C3, C16) and `ValidationLocalizationOptionsValidator.cs : IValidateOptions<ValidationLocalizationOptions>` (`Validate(string? name, ValidationLocalizationOptions options)` throws `ArgumentNullException` for a null `options`).
2. **Create** `src/Encina/Validation/ValidationMessageTemplate.cs` (`internal static`): `string Render(string template, IReadOnlyDictionary<string, object?>? arguments, CultureInfo culture)` replaces `{Name}` placeholders from the argument bag using the given culture (`IFormattable.ToString(null, culture)` for numbers, dates), leaves a placeholder with no argument untouched (row C17), ignores `{{`/`}}` escapes, throws `ArgumentNullException` only for a null template or culture (programming errors) and never for a malformed template or any argument bag (register rows C2, C11). `Encina.GuardTests` is in the `InternalsVisibleTo` list of `src/Encina/Encina.csproj` (verified on origin/main), so the guard flag reaches it.
3. **Create** `src/Encina/Validation/PluralCategory.cs` (enum: `Zero`, `One`, `Two`, `Few`, `Many`, `Other`) and `src/Encina/Validation/PluralRules.cs` (`public static`): `PluralCategory Select(CultureInfo culture, decimal count)` (null culture throws `ArgumentNullException`) with a table for the neutral/English, Spanish and French rules and `Other` for any culture without a rule. A `crap-exempt` single-question switch is marked only if it stays one switch.
4. **Modify** `PublicAPI.Unshipped.txt` and `.github/coverage-manifest/Encina.json` through `mechanical-fixer`.
5. Tests (new): `ValidationMessageTemplateTests` (placeholders, culture formatting of `1234.5` in `en-US` and `es-ES`, a placeholder with no argument, escapes, malformed input, null arguments as a valid case), `PluralRulesTests` (English, Spanish, French boundaries, `0`, `1`, `2`, large counts, a culture without a rule), `ValidationLocalizationOptionsValidatorTests` (every rule of row C3), `ValidationMessageTemplateProperties` (FsCheck: the template never throws and its output contains no brace introduced by an argument value), `PluralRulesProperties` (FsCheck: `Select` is total and deterministic for any culture and count, English `One` only for exactly `1`, French `One` exactly for `0 <= count < 2`), `tests/Encina.GuardTests/Core/Validation/ValidationLocalizationBuildingBlocksGuardTests.cs` (null culture for `PluralRules.Select`, null template and null culture for the template, null `options` for the validator).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
Issue #233 plan docs/plans/validation-localization-implementation-plan-233.md, phase 2 of 7. Pure components in
src/Encina/Validation/; no DI yet.

TASK:
1. Options + IValidateOptions validator; internal ValidationMessageTemplate (named placeholders, culture formatting);
   PluralCategory and PluralRules with a rule table for en/es/fr and Other as the fallback.
2. Property tests: the template never throws; PluralRules.Select is total and deterministic. Guard tests for the
   null checks.

KEY RULES:
- Placeholders are looked up by name only; nothing is evaluated; an argument value can never inject a placeholder.
- XML docs on every public member; PublicAPI.Unshipped.txt and manifests through mechanical-fixer.

REFERENCE FILES:
- src/Encina.Compliance.Consent/ConsentOptionsValidator.cs (options validator idiom)
- src/Encina/Validation/ValidationResult.cs
```

</details>

---

### Phase 3: Default Resource Set (pull request S3)

> **Goal**: The shipped message set and its marker type, loadable through `ResourceManager`. Needs only the keys of S1.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** the resource files `src/Encina/Validation/Resources/ValidationMessages.resx` (neutral, English), `ValidationMessages.es-ES.resx` and `ValidationMessages.fr-FR.resx` (register row C10): one entry per `ValidationMessageKeys` constant, with `.one` and `.other` entries for the two plural bases; placeholders named as in the contract table. The SDK embeds `.resx` files by default (`src/Encina/Encina.csproj` has no `EnableDefaultEmbeddedResourceItems` override, verified on origin/main) and produces the `es-ES` and `fr-FR` satellite assemblies.
2. **Create** `src/Encina/Validation/Resources/ValidationMessages.cs` (`public static class ValidationMessages`, namespace `Encina.Validation.Resources`): the marker type of the resource set, so that `new ResourceManager(typeof(ValidationMessages))` and `IStringLocalizerFactory.Create(typeof(ValidationMessages))` both resolve the base name `Encina.Validation.Resources.ValidationMessages`. The default `ResourceType` of row C16 (set by the registration of S5) points at it.
3. **Modify** `src/Encina/PublicAPI.Unshipped.txt` and `.github/coverage-manifest/Encina.json` through `mechanical-fixer`.
4. Tests (new): `tests/Encina.UnitTests/Validation/ValidationMessagesResourceTests.cs`: loads the three sets through a real `ResourceManager`, asserts every `ValidationMessageKeys` constant exists in the neutral, `es-ES` and `fr-FR` sets, every plural base key has `.one` and `.other`, and placeholders match across the three cultures.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
Issue #233 plan docs/plans/validation-localization-implementation-plan-233.md, phase 3 of 7. Depends on S1 (the keys).

TASK:
1. Add the three resource sets (neutral, es-ES, fr-FR) under src/Encina/Validation/Resources and the marker class
   ValidationMessages in the namespace Encina.Validation.Resources.
2. A test that loads each set through ResourceManager and checks keys, plural variants and placeholders.

KEY RULES:
- The Spanish and French texts are reviewed by a native speaker; placeholders must be identical across cultures.
- PublicAPI.Unshipped.txt and the manifest through mechanical-fixer.

REFERENCE FILES:
- src/Encina/Validation/ValidationMessageKeys.cs (from phase 1)
- src/Encina/Encina.csproj
```

</details>

---

### Phase 4: Localizer, Culture Resolver and Logging (pull request S4)

> **Goal**: The localizer abstraction, its resource-file implementation, the culture resolver and the EventIds. Nothing is registered yet.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina/Validation/IValidationMessageLocalizer.cs` (`ValidationError Localize(ValidationError error, CultureInfo culture)`; returns the error unchanged when it has no code or no resource) and `ResourceManagerValidationMessageLocalizer.cs` (sealed; constructor `(IOptions<ValidationLocalizationOptions> options, ILogger<ResourceManagerValidationMessageLocalizer>? logger = null)`, a null logger is a valid case; builds one `ResourceManager` from `options.ResourceType`, a null `ResourceType` means the error is returned unchanged; applies the fallback chain of Design Choice 6, picks a plural key through `PluralRules` when `Arguments` holds `Count` (row C14), renders with `ValidationMessageTemplate`; logs 601 and 602).
2. **Create** `IValidationCultureResolver.cs` (`CultureInfo Resolve(IRequestContext context)`) and `AmbientValidationCultureResolver.cs` (sealed; constructor `(IOptions<ValidationLocalizationOptions> options, ILogger<AmbientValidationCultureResolver>? logger = null)`; takes `CultureInfo.CurrentUICulture.Name`, applies the matching of row C18 on strings, creates a `CultureInfo` only for the matched `SupportedCultures` member, logs 600 when it falls to `DefaultCulture`).
3. **Modify** `src/Encina/Diagnostics/EventIdRanges.cs`: register `ValidationLocalization = (600, 609)`; **create** `src/Encina/Validation/ValidationLocalizationLog.cs` (`[LoggerMessage]`, packed): 600 culture not in the allow-list, fell back to the default (debug: default culture name), 601 no resource for a key (debug: key, culture), 602 no plural rule for the culture, used `Other` (debug: culture). 603-609 stay free. XML docs name the range (`Event IDs: 600-602 (see EventIdRanges.ValidationLocalization)`).
4. **Modify** `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` (assembly `Encina` also maps to the new range), `PublicAPI.Unshipped.txt` and `.github/coverage-manifest/Encina.json` (through `mechanical-fixer`).
5. Tests (new): `ResourceManagerValidationMessageLocalizerTests` (exact, parent and default fallback, missing key returns the original error, plural key choice, uncoded error unchanged, null `ResourceType` unchanged), `AmbientValidationCultureResolverTests` (allow-list; `fr-CA` and `fr` select `fr-FR` under row C18; `de-DE` and hostile strings such as an empty string, a 5 kB string and `"xx-INVALID"` fall to the default and never throw), `ValidationLocalizationLogTests` (EventIds, and no argument values in the rendered messages), `ValidationLocalizationGuardTests` (null `error` and `culture` for `Localize`, null `context` for `Resolve`, null `options` for both constructors; a null logger is a valid case), `ValidationLocalizationContractTests` (in `tests/Encina.ContractTests/Core/Validation/`: the real localizer and the real resolver honour their interface contracts: never null, an uncoded error is returned unchanged, the resolved culture is always a member of `SupportedCultures` or `DefaultCulture`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT:
Issue #233 plan docs/plans/validation-localization-implementation-plan-233.md, phase 4 of 7. Depends on S1, S2 and S3.
ADR-021: register the EventId range in src/Encina/Diagnostics/EventIdRanges.cs before using it.

TASK:
1. IValidationMessageLocalizer + ResourceManager implementation with the fallback chain, plural keys and the template.
2. IValidationCultureResolver with an ambient default constrained by the SupportedCultures allow-list (row C18).
3. ValidationLocalizationLog 600-602 and the EventIdRanges entry.

KEY RULES:
- Never create a CultureInfo from a string that is not in the allow-list; never throw for a missing translation.
- Logs carry codes and culture names only. Both classes are singleton-safe (no scoped dependency).
- EncinaEventIdAllocationTests must map the Encina assembly to the new range.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
- src/Encina/Validation/ValidationOrchestrator.cs
```

</details>

---

### Phase 5: Registration, Culture Scope and Orchestrator Integration (pull request S5)

> **Goal**: `AddEncinaValidationLocalization` and the orchestrator localizing under the resolved culture for all three providers.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina/Validation/ValidationLocalizationServiceCollectionExtensions.cs` (`AddEncinaValidationLocalization(this IServiceCollection services, Action<ValidationLocalizationOptions>? configure = null)`: options with `ValidateOnStart` and the validator of S2; a post-configure applies the defaults of row C16 (the three cultures only when the list is empty, `ResourceType` defaulting to `ValidationMessages`); `TryAddSingleton` of `IValidationMessageLocalizer` (the resource-file implementation) and `IValidationCultureResolver`; no hosted service; the method needs no other registration, so a host without `AddLocalization()` works) and `src/Encina/Validation/ValidationCultureScope.cs` (`public sealed`, `IDisposable`: the constructor sets `CurrentUICulture` and `Dispose` restores the previous value; null culture throws; row C5).
2. **Modify** `src/Encina/Validation/ValidationOrchestrator.cs`: optional trailing constructor parameters `IValidationMessageLocalizer? localizer = null, IValidationCultureResolver? cultureResolver = null` (a null localizer keeps today's behavior; the 13 `new ValidationOrchestrator(...)` sites in `ValidationPipelineBehaviorContractTests.cs` and `ValidationPipelineBehaviorGuardTests.cs` compile unchanged, verified with `git grep -c "new ValidationOrchestrator(" -- tests`). When both are present: resolve the culture from the context, run the provider and the localization of each coded error of an invalid result inside a `ValidationCultureScope`, then build the message as today; a localizer exception propagates (row C15). The code-less `Left` is #3415's and is not changed; the independent catch added by #230 sits next to this code, and whichever pull request lands second rebases a few lines. The orchestrator is registered by type (`TryAddSingleton`/`TryAddScoped`), so the container fills the optional parameters when the services are registered, whatever the order of the `Add...` calls; all registrations must happen before the provider is built (the ordinary rule of the container), and the DI test proves both orders for each of the three providers.
3. **Modify** `PublicAPI.Unshipped.txt` and `.github/coverage-manifest/Encina.json` through `mechanical-fixer`.
4. Tests (new): `ValidationOrchestratorLocalizationTests` (unit: a coded error localized in `es-ES`, an unsupported culture, a missing key, a localizer that throws propagates and never yields `Right`, cancellation during localization, scope restored after an exception); `ValidationLocalizationEquivalenceTests` in `tests/Encina.UnitTests/Validation/` (the same request, an equivalent `[Required]`/`NotEmpty` rule per provider, `es-ES`: each provider returns Spanish text by its native mechanism, the culture reaches the FluentValidation tasks of `Task.WhenAll`, and no `PropertyValue` appears in the output; the three project references already exist in `Encina.UnitTests`); `ValidationLocalizationRegistrationTests` (DI: builds with `ValidateOnBuild` and `ValidateScopes`, resolves localizer, resolver and each of the three orchestrators with the localizer injected, `AddEncinaValidationLocalization` before and after each provider registration, an invalid option fails at start-up, an application list of cultures replaces the default list); `ValidationLocalizationRegistrationGuardTests` (in `Encina.GuardTests`: null `services` for the extension, null `culture` for the scope; a null `configure` is a valid case); `tests/Encina.IntegrationTests/Validation/{DataAnnotations,FluentValidation,MiniValidator}/LocalizedValidationPipelineTests.cs` (through `IEncina.Send` with the real pipeline behavior and the culture set per test; no containers); `ValidationPipelineBehaviorGuardTests.cs` (edit: the null-localizer valid case).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT:
Issue #233 plan docs/plans/validation-localization-implementation-plan-233.md, phase 5 of 7. Depends on S1-S4.
The orchestrator is the single point all three validation providers pass through.

TASK:
1. AddEncinaValidationLocalization (options with ValidateOnStart, defaults of row C16, TryAddSingleton) and the public
   ValidationCultureScope.
2. Add optional trailing localizer and culture resolver parameters to ValidationOrchestrator; run the provider and the
   localization inside the culture scope and localize coded errors.
3. Write the unit, equivalence, DI and three integration tests.

KEY RULES:
- A null localizer means exactly today's behavior; existing new ValidationOrchestrator(provider) sites compile unchanged.
- Do not change the code-less Left of an invalid result (that is #3415's); never log or tag a rendered message.
- DI test with ValidateOnBuild and ValidateScopes for the three providers, both registration orders.

REFERENCE FILES:
- src/Encina/Validation/ValidationOrchestrator.cs
- tests/Encina.ContractTests/Core/Validation/ValidationPipelineBehaviorContractTests.cs
- tests/Encina.GuardTests/Core/Validation/ValidationPipelineBehaviorGuardTests.cs
```

</details>

---

### Phase 6: The `IStringLocalizer` Adapter (pull request S6)

> **Goal**: The ASP.NET Core integration. Needs S4 and S5.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.AspNetCore/Validation/StringLocalizerValidationMessageLocalizer.cs` (sealed; constructor `(IStringLocalizerFactory factory, IOptions<ValidationLocalizationOptions> options, ILogger<StringLocalizerValidationMessageLocalizer>? logger = null)`; creates the localizer with `factory.Create(options.ResourceType)`; same plural and fallback rules; sets the culture through `ValidationCultureScope`, not through `WithCulture` (removed from `IStringLocalizer`)) and `.../ValidationLocalizationApplicationExtensions.cs` (`AddEncinaStringLocalizerValidation(this IServiceCollection services, Action<ValidationLocalizationOptions>? configure = null)`: calls `services.AddLocalization()` (which registers `IStringLocalizerFactory`) and `AddEncinaValidationLocalization(configure)` first, so options, resolver and the factory exist whatever the caller registered, then applies the mechanism of row C19: an existing `IValidationMessageLocalizer` descriptor is replaced only when it is Encina's own resource-file localizer, an application's own registration is kept, and when no descriptor exists the adapter is added). `AddEncinaValidationLocalization` called after it keeps the adapter because it uses `TryAddSingleton`.
2. **Modify** `src/Encina.AspNetCore/PublicAPI.Unshipped.txt` and `.github/coverage-manifest/Encina.AspNetCore.json` through `mechanical-fixer`.
3. Tests (new): `StringLocalizerValidationMessageLocalizerTests` (unit with an `IStringLocalizerFactory` fake and with the real factory over `ValidationMessages`), `StringLocalizerValidationMessageLocalizerGuardTests` (constructor and `Localize` nulls, the extension's null `services`; a null `configure` and a null logger are valid cases), `StringLocalizerRegistrationTests` (DI with `ValidateOnBuild` and `ValidateScopes`; the order matrix: default then adapter, adapter then default, application localizer then adapter, adapter then application localizer; the call with no prior `AddEncinaValidationLocalization` and no prior `AddLocalization`), `StringLocalizerValidationMessageLocalizerContractTests` (in `Encina.ContractTests`: the real adapter honours the `IValidationMessageLocalizer` contract of S4).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```text
CONTEXT:
Issue #233 plan docs/plans/validation-localization-implementation-plan-233.md, phase 6 of 7. Depends on S4 and S5.

TASK:
1. Add the IStringLocalizer adapter and AddEncinaStringLocalizerValidation to Encina.AspNetCore.
2. Unit, guard, DI-order and contract tests.

KEY RULES:
- The extension registers AddLocalization() and AddEncinaValidationLocalization itself; it replaces only Encina's own
  default localizer descriptor and keeps an application's registration, in any order (register row C19).
- Use ValidationCultureScope, never WithCulture.

REFERENCE FILES:
- src/Encina.AspNetCore/Encina.AspNetCore.csproj
- src/Encina/Validation/ResourceManagerValidationMessageLocalizer.cs (from phase 4)
- src/Encina/Validation/ValidationLocalizationServiceCollectionExtensions.cs (from phase 5)
```

</details>

---

### Phase 7: Documentation and Finalization (pull request S7)

> **Goal**: Usage, README updates, changelog and justification files.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `docs/features/validation-localization.md` through `docs-writer` (the layers, the `ErrorCode` contract and the key table, the culture scope, `UseRequestLocalization` with Encina, the three provider paths, plural keys, writing a new culture, the `IStringLocalizer` adapter, security notes).
2. **Modify** `src/Encina.FluentValidation/README.md`, `src/Encina.DataAnnotations/README.md`, `src/Encina.MiniValidator/README.md` and `src/Encina.AspNetCore/README.md` through `docs-writer`.
3. **Create** `changelog.d/233-validation-localization.added.md` through `mechanical-fixer`.
4. **Create** the justification files for Load and Benchmark tests (`AGENTS.md` section 9): `tests/Encina.LoadTests/Validation/ValidationLocalization.md` and `tests/Encina.BenchmarkTests/Validation/ValidationLocalization.md` (a per-error dictionary lookup with no I/O and no shared mutable state). A contract-test justification is never written: contract tests exist (S1, S4, S6).
5. Verification: `dotnet build --configuration Release` with zero warnings, `dotnet test` of the touched projects, per-flag coverage of the touched files against the table below.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```text
CONTEXT:
Issue #233 plan docs/plans/validation-localization-implementation-plan-233.md, phase 7 of 7.

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
| FluentValidation 12.1.1 | `ValidationFailure.ErrorCode` (validator type name or `WithErrorCode`), `FormattedMessagePlaceholderValues`; built-in translations follow the current UI culture | The mapping table and Phase 1; why the culture scope localizes Fluent natively |
| `System.ComponentModel.DataAnnotations` | `ErrorMessageResourceType`/`ErrorMessageResourceName` read through the current UI culture | Why DataAnnotations and MiniValidator need no code |
| OWASP ASVS 4.0 V5.1.3, V5.2.4 | Allow-list validation of all input; no dynamic evaluation of user-controlled data | Register rows C1, C11 and C18 |
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
| `Encina` core (`ValidationLocalization`, new, inside the block 600-649 assigned to the validation-2 family) | 600-609 | 600-602 used (three messages); 603-609 free (603 only if register row C15 chooses the alternative); sibling plans in the same block: #234 610-619, #235 620-629, #236 630-639; #702 needs none |

### Estimated File Count

Every count includes tests, coverage manifest edits, PublicAPI files, the README and the changelog fragment. No pull request uses a type, project or log class created by a later one; each enters its per-file manifest targets with its own tests, and the log class and EventIds appear in S4, the first pull request that logs.

| PR | Content | New src | Changed src | New tests | Edited tests | Manifest | Other | Files | Lines (est.) |
|----|---------|---------|-------------|-----------|--------------|----------|-------|-------|--------------|
| S1 | Coded `ValidationError`, keys, Fluent mapping | 2 (`ValidationMessageKeys.cs`, `FluentValidationErrorCodeMap.cs`) | 3 (`ValidationResult.cs`, `FluentValidationProvider.cs`, PublicAPI) | 2 (Fluent code unit, error contract) | 1 (`ValidationResultTests.cs`) | 2 (`Encina.json`, `Encina.FluentValidation.json`) | 0 | 10 | ~450 |
| S2 | Options, template, plural rules | 5 (options, options validator, template, `PluralCategory`, `PluralRules`) | 1 (PublicAPI) | 6 (template, plural, options validator unit; template and plural property; building-blocks guard) | 0 | 1 | 0 | 13 | ~950 |
| S3 | Default resource set | 4 (3 `.resx`, `ValidationMessages.cs`) | 1 (PublicAPI) | 1 (resource test) | 0 | 1 | 0 | 7 | ~350 |
| S4 | Localizer, resolver, logging | 5 (`IValidationMessageLocalizer`, `ResourceManagerValidationMessageLocalizer`, `IValidationCultureResolver`, `AmbientValidationCultureResolver`, `ValidationLocalizationLog`) | 2 (`EventIdRanges.cs`, PublicAPI) | 5 (localizer, resolver, log, guard, contract) | 1 (`EncinaEventIdAllocationTests.cs`) | 1 | 0 | 14 | ~1,100 |
| S5 | Registration, culture scope, orchestrator integration | 2 (extensions, `ValidationCultureScope`) | 2 (`ValidationOrchestrator.cs`, PublicAPI) | 7 (orchestrator unit, equivalence unit, DI, registration guard, 3 integration) | 1 (`ValidationPipelineBehaviorGuardTests.cs`) | 1 | 0 | 13 | ~1,200 |
| S6 | `IStringLocalizer` adapter | 2 (adapter, adapter extension) | 1 (`Encina.AspNetCore` PublicAPI) | 4 (adapter unit, adapter guard, adapter DI, adapter contract) | 0 | 1 (`Encina.AspNetCore.json`) | 0 | 8 | ~600 |
| S7 | Documentation | 0 | 4 (4 READMEs) | 0 | 0 | 0 | 4 (feature page, changelog fragment, 2 justification `.md`) | 8 | ~450 |
| **Total** | | **20** | **14** | **25** | **3** | **7** | **4** | **73** | **~5,100** |

The columns sum to the total (20 + 14 + 25 + 3 + 7 + 4 = 73) and the per-pull-request rows sum to 73 files (10 + 13 + 7 + 14 + 13 + 8 + 8) and to ~5,100 lines (450 + 950 + 350 + 1,100 + 1,200 + 600 + 450). The largest is S4 with 14 files and S5 with ~1,200 lines, inside the limits of 15 files and 1,500 lines. S1 comes first; S2 is independent of S1; S3 needs S1; S4 needs S1 to S3; S5 needs S4; S6 needs S4 and S5; S7 needs all. Verify caller counts before each pull request with `git grep`, and keep S4 at 15 files at most (a further file moves the log class and its test to a pull request of their own, together with the callers that log).

Per-file coverage targets (per flag, with a one-sentence justification), written into the package manifests by the pull request that adds the file; `Encina.GuardTests` is in the `InternalsVisibleTo` list of `src/Encina/Encina.csproj` (verified on origin/main), `Encina.FluentValidation` has no entry for it:

| File (PR) | Unit | Guard | Contract | Property | Justification |
|-----------|------|-------|----------|----------|---------------|
| `ValidationMessageKeys.cs` (S1) | 0 | 0 | 0 | 0 | Constants only, no executable code; the key format and uniqueness are asserted by the S1 contract test and the keys' presence by the S3 resource test |
| `ValidationResult.cs`, changed members (S1) | 95 | existing target kept | 80 | 0 | A coded and an uncoded error, `with` and equality are reachable by unit tests; the record has no argument check beyond what it had, and a null `Arguments` is a valid case tested in unit; the contract test instantiates real errors; a two-field data record has no logic for properties |
| `FluentValidationErrorCodeMap.cs` (S1) | 95 | 0 | 0 | 0 | One real rule per mapping row reaches every branch; the only parameter is a nullable code whose null case is a valid result, and the type is internal without `InternalsVisibleTo` for `Encina.GuardTests`; not a public contract |
| `FluentValidationProvider.cs`, changed members (S1) | 95 | existing target kept | 0 | 0 | A failing rule with arguments and one with an application code are reachable by unit tests; existing guards stay; the provider's contract is the unchanged `IValidationProvider` one |
| `ValidationMessageTemplate.cs` (S2) | 95 | 85 | 0 | 70 | Placeholders, formats, escapes and malformed input are reachable with plain strings; the guard tests reach the null template and culture checks (the type is visible to `Encina.GuardTests`); the property test drives arbitrary templates and bags; internal, so no public contract |
| `PluralRules.cs`, `PluralCategory.cs` (S2) | 95 | 90 | 0 | 60 | Every culture rule and boundary is reachable with `CultureInfo`; the guard tests reach the null culture check; the property test covers totality, determinism and the English and French boundaries; a static function has no interface to contract-test; the enum has no executable code |
| `ValidationLocalizationOptions.cs` (S2) | 90 | 0 | 0 | 0 | Properties with defaults, exercised by the validator and registration tests; no parameters, so guard tests cannot reach it |
| `ValidationLocalizationOptionsValidator.cs` (S2) | 95 | 90 | 0 | 0 | Pure rule checks on one options object; the guard tests reach the null `options` check; an `IValidateOptions` implementation has no contract of its own |
| `ValidationMessages.cs` (S3) | 0 | 0 | 0 | 0 | An empty marker class with no executable code; the three sets it names are verified by `ValidationMessagesResourceTests` |
| `ResourceManagerValidationMessageLocalizer.cs` (S4) | 90 | 85 | 70 | 0 | Fallback, plural and missing-key branches are reachable with in-test resource sets; the guard tests reach the null checks; the contract test instantiates the real localizer against the interface contract |
| `AmbientValidationCultureResolver.cs` (S4) | 90 | 85 | 70 | 0 | Allow-list, same-language and default branches are reachable with hostile strings; the guard tests reach the null checks; the contract test instantiates the real resolver |
| `ValidationLocalizationLog.cs` (S4) | 90 | 0 | 0 | 0 | Source-generated methods exercised by the logging test; their parameters are the logger and plain values, so guard tests cannot reach a check |
| `IValidationMessageLocalizer.cs`, `IValidationCultureResolver.cs` (S4) | 0 | 0 | 0 | 0 | Interface declarations with no executable code; their contract is asserted by the S4 contract test through the real implementations |
| `ValidationLocalizationServiceCollectionExtensions.cs` (S5) | 90 | 90 | 0 | 0 | Registration is fully exercised by the DI test; null checks are guard-tested; registration has no public behavior contract beyond the DI test |
| `ValidationCultureScope.cs` (S5) | 95 | 90 | 0 | 0 | Set, restore and restore-after-exception are unit-tested; the guard tests reach the null culture check |
| `ValidationOrchestrator.cs`, changed methods (S5) | 90 | 85 | existing target kept | 0 | The new branches (no localizer, supported culture, fallback, throwing localizer, scope restore) each have a unit test; the optional parameters are guard-tested; the existing contract test already covers the orchestrator's contract |
| `StringLocalizerValidationMessageLocalizer.cs`, `ValidationLocalizationApplicationExtensions.cs` (S6) | 90 | 85 | 70 | 0 | The adapter is exercised with a fake and a real factory; the guard tests reach the null checks; the registration-order matrix covers the extension; the contract test instantiates the real adapter |

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
1. ValidationError gains optional ErrorCode and Arguments; ValidationMessageKeys defines the keys; FluentValidation
   fills the fields through its mapping table.
2. Options, a named-placeholder template engine and plural rules; the default resource sets (neutral, es-ES, fr-FR).
3. An IValidationMessageLocalizer (resource files) and a culture resolver constrained by a SupportedCultures
   allow-list; EventIds 600-602 in a range registered first.
4. AddEncinaValidationLocalization and ValidationOrchestrator localize coded errors and run the provider inside a
   culture scope, so native localization of FluentValidation and DataAnnotations follows the same culture.
5. An IStringLocalizer adapter in Encina.AspNetCore.
Seven pull requests (S1 to S7), see the Estimated File Count table.

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
| 3 | Structured Logging | ✅ Phase 4 | `ValidationLocalizationLog` EventIds 600-602 in the new range 600-609, by code and culture only |
| 4 | Health Checks | ❌ N/A | In-process; a missing translation never degrades health |
| 5 | Validation | ✅ All phases | This is the validation feature; the three providers of `AGENTS.md` section 5 stay consistent through the equivalence test |
| 6 | Resilience | ❌ N/A | No external call; a custom `IStringLocalizer` that calls a service is the application's concern (register row C15) |
| 7 | Distributed Locks | ❌ N/A | No shared state to coordinate |
| 8 | Transactions | ❌ N/A | Validation runs before the handler |
| 9 | Idempotency | ❌ N/A | Localization is deterministic for a given culture and key |
| 10 | Multi-Tenancy | ⏭️ Follow-up | Tenant-specific locale preferences (the issue defers them) go to a follow-up issue to be opened (draft: `artifacts/decisions/plan-batch/followups/validation-tenant-module-scoped-resources.md`); no tenant id is stored or keyed here, so there is no tenant-qualified key to make injective |
| 11 | Module Isolation | ⏭️ Follow-up | Module-scoped resource sets: the same follow-up issue to be opened (same draft) |
| 12 | Audit Trail | ⏭️ Follow-up | Auditing of rejected requests with the locale: follow-up issue to be opened (draft: `artifacts/decisions/plan-batch/followups/validation-localization-audit-locale.md`); nothing here changes security state |

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
| Which error text a client may see | #3426 | Decides whether the English summary of register row C9 reaches clients, and whether `ErrorCode` is ever exposed |
| Central validation instrumentation | #726 | Owns the culture attribute |
| Message consumer of this plan | #235 | Uses the keys, `ErrorCode` and the localizer; planned after this plan |

---

## Next Steps

1. Link this plan from #233 and ask the maintainer to answer Design Choices 1 to 5 and register rows C1, C5, C9, C10 and C12 to C19 when work starts.
2. Open the two follow-up issues when the first pull request opens: tenant and module scoped locale and resources (draft: `artifacts/decisions/plan-batch/followups/validation-tenant-module-scoped-resources.md`) and locale in the audit of rejected requests (draft: `artifacts/decisions/plan-batch/followups/validation-localization-audit-locale.md`).
3. Implement S1 to S7 as the pull requests of the Estimated File Count split, `Fixes #233` on S7.
