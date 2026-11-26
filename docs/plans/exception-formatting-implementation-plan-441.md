# Implementation Plan: `Encina.Diagnostics` — enhanced error and exception formatting

> **Issue**: [#441](https://github.com/dlrivada/Encina/issues/441)
> **Type**: Feature (new package, developer experience)
> **Complexity**: Medium (6 phases, no database provider, 6 pull requests of 7, 14, 7, 12, 11 and 5 files)
> **Estimated Scope**: ~3,200 lines over 6 pull requests (production code, tests and documentation, summed per pull request in "Estimated File Count")
> **Family**: dev-experience (siblings #438, #343, #333, #445). This plan owns the package `Encina.Diagnostics`, the EventId range `DiagnosticsFormatting` and the structured validation details of Phase 1. The dashboard plan (#445) may reuse the formatter for its error panel.

---

## Summary

Make `EncinaError` and exceptions easy to read in a terminal: a boxed pretty print with code, category and details, a demystified stack trace, validation failures grouped by property, plain-text fallback when color is unavailable, and an `ILogger` extension that logs an error safely.

What exists today (read on `origin/main` at `326e65ebf`, after `git fetch`):

- `EncinaError` is a `readonly record struct` with `Message`, `Exception` and internal metadata; code, category, details and metadata are read through extension methods `GetCode`, `GetCategory`, `GetCause`, `GetDetails`, `GetMetadata` (`src/Encina/Results/EncinaErrors.cs:195-276`); `EncinaErrors.Create(code, category, message, exception, details)` builds one with structured `details`.
- [AGENTS.md](../../AGENTS.md) section 3 forbids `EncinaError.Message` in logs, tags, health checks and plaintext storage, and requires exceptions passed to a logger to go through `ForLogging()`, which returns a `RedactedException` with the type and the full stack trace but never the message (`src/Encina/Diagnostics/RedactedException.cs`). The issue's sample output prints `Message`, `UserId` and `TenantId` and its Serilog and `ILogger` examples log the error: a formatter that does this by default would break the rule (Design Choice 2).
- The validation pipeline flattens failures into the message: `ValidationOrchestrator` calls `EncinaErrors.Create(ValidationFailed, ErrorCategory.Validation, result.ToErrorMessage(typeof(TRequest).Name))` with no `details` (`src/Encina/Validation/ValidationOrchestrator.cs:70-72`), and `ValidationError` is `record ValidationError(string? PropertyName, string ErrorMessage)` (`ValidationResult.cs`). Grouping by property from an `EncinaError` is therefore impossible without parsing text; Phase 1 puts the structured errors into `details` (Design Choice 3).
- The namespace `Encina.Diagnostics` already exists in core (`RedactedException`, `EventIdRanges`, `ActivityTagNames`); the new package's types live in `Encina.Diagnostics.Formatting` to avoid mixing.
- `Directory.Packages.props` pins `Spectre.Console` 0.57.2 (used by `Encina.Cli`) and has no `Ben.Demystifier`.
- The EventId block registry (`docs/engineering/eventid-block-registry.md` on `origin/main`) reserves `Testing and tooling` 70000-70049 for #4007; 70050 onward is free and no open plan pull request claims it (checked on `origin/main` and on the open `docs/plan-*` pull request heads).

What this plan delivers: structured validation details in core, the package with `ToPrettyString`, `ToGroupedString`, `ToPrettyStackTrace`, `ToStructured`, `LogEncinaError`, two themes (ANSI and plain) selected from `NO_COLOR` and redirect detection, and documentation. The issue's "Pipeline Error" box (request, handler, duration) is not delivered: `EncinaError` does not carry them, so it would need a pipeline behavior that records them; that belongs with the dashboard plan (#445), which has the per-request timing.

- **Affected packages**: `Encina` (core: structured validation details, one `EventIdRanges` field), `Encina.Diagnostics` (new), `Encina.UnitTests`, `Encina.GuardTests`, `Encina.PropertyTests`. `Encina.AspNetCore` and `Encina.OpenTelemetry` (ticked in the issue) are not changed: telemetry must not carry messages, so there is nothing to attach (matrix, row 2). The issue's other integration ticks (EF Core, Dapper, ADO.NET, Caching) need nothing: the formatter is a stateless extension over `EncinaError` and touches no store or cache.
- **Provider category**: none.
- **Prerequisites**: none blocking.

### Shared Pieces and Ownership (family dev-experience)

| Shared piece | Owner | Used by |
|--------------|-------|---------|
| `Encina.Diagnostics` package, `FormatterOptions`, themes, `ToStructured` | **This plan (#441)** | #445 (error panel), #438 (rule messages do not use it) |
| Structured validation errors in `details` (`EncinaErrorMetadataKeys` constant) | **This plan, Phase 1** | #445 (shows validation failures) |
| EventIds 70050-70059 (`DiagnosticsFormatting`) | **This plan** | none; #445 owns 70060-70089 in the same 50-id area, with its own registry row (no row of either plan names the other's range) |

---

## Design Choices

<details>
<summary><strong>1. Package placement — a new `Encina.Diagnostics` package</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) New package `Encina.Diagnostics`, no DI, extension methods** | Opt-in; core stays free of console code; named in the issue | Another package to reference; the name overlaps the core namespace `Encina.Diagnostics` (types live in `.Formatting`) |
| **B) Put the formatters in `Encina` core** | No new reference | Console and ANSI code in the dependency every package has; breaks pay-for-what-you-use |

### Chosen Option: **A — New package, no registration**

### Rationale

- The formatter is stateless; extension methods with an optional `FormatterOptions` need no `AddEncina*` method, so there is no registration to complete or validate.
- **Class:** Settled
- **Settled by:** AGENTS.md section 1 ("pay-for-what-you-use (every feature opt-in)") and the issue's "Package Structure".

</details>

<details>
<summary><strong>2. What the output shows by default</strong></summary>

The issue's sample prints the error message, a stack trace and a context block with `UserId` and `TenantId`. AGENTS.md section 3 keeps `EncinaError.Message` and raw identifiers out of logs; the pretty string is returned to the caller, who may print it to a console or hand it to a logger, so the default decides whether the safe path is the easy one.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Safe by default: code, category, exception type, validation property names, the framework correlation id (generated by Encina, not a user-supplied identifier, and documented as such in the options page) and stack frames; the message, validation messages and user and tenant ids appear only when `FormatterOptions.IncludeMessage` and `IncludeIdentifiers` are set** | Nothing sensitive reaches a log by accident; matches the rule | The default output is less informative than the issue's sample; a developer sets one option in development |
| **B) Developer-friendly default: include the message, never the ids** | Closer to the sample; the ids stay out | A message with personal data can be logged by a caller that logs the string |
| **C) As in the issue: message and ids included** | Matches the issue text | Violates the rule when the string is logged; tenant ids as raw values also break the ADR-054 D4 cardinality guidance |

### Chosen Option: **A — Safe by default** (recommended, pending the maintainer)

### Rationale

- A wins when the formatted string can end up in a shared log (the common case in production code); B wins if the library is documented as a local-console tool whose output is never logged, since it removes the friction in development. `LogEncinaError` and `ToStructured` never include the message under any option.
- **Class:** Dilemma

</details>

<details>
<summary><strong>3. How grouped validation errors reach the formatter</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ValidationOrchestrator` also stores the structured errors in the error's `details` (new key `EncinaErrorMetadataKeys.ValidationErrors`: an array of property name and message pairs)** | Grouping is exact; other hosts (ProblemDetails, the dashboard) can use it | One core change (2 files); the details hold the same text as the message, so they carry the same sensitivity and are handled by the same rule |
| **B) Parse the flattened message (`ToErrorMessage`)** | No core change | Fragile text parsing; breaks when the format changes; reads `Message` |
| **C) Flat list only, no grouping** | Simplest | Drops a feature the issue asks for |

### Chosen Option: **A — Structured details from the orchestrator** (recommended, pending the maintainer)

### Rationale

- The `details` parameter exists for this (`EncinaErrors.Create(..., details)`), and AGENTS.md section 3 says validation "returns `Either` with detailed errors"; but B and C also satisfy that sentence, so no rule excludes them. A wins because grouping is exact and other hosts (ProblemDetails, the dashboard) can read the same structure; B wins only if the maintainer refuses any core change and accepts text parsing, which breaks when `ToErrorMessage` changes; C wins if grouping is dropped from the first release.
- **Class:** Dilemma

</details>

<details>
<summary><strong>4. Demystifying stack traces — own implementation or `Ben.Demystifier`</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Own `StackTraceFormatter` over `System.Diagnostics.StackTrace`: resolve async state machines through `StateMachineAttribute`, tidy generic and tuple names, drop framework frames by namespace prefix (configurable)** | No new dependency; behavior covered by our tests; about 250 lines | We maintain the edge cases (local functions, lambdas, iterators) |
| **B) Reference `Ben.Demystifier`** | Mature rewriting logic | A new package pin (exact version, clean restore audit; its release and .NET 10 support must be verified when pinned); its output cannot be redacted by our options without post-processing |
| **C) No demystification: plain frames with noise filtering only** | Smallest | Async frames stay unreadable, the first motivation of the issue |

### Chosen Option: **A — Own formatter** (recommended, pending the maintainer)

### Rationale

- A wins while few edge cases matter and the dependency count stays minimal; B wins if the maintainer prefers a library's long tail of edge cases over maintaining ours. The stack trace shown is that of `ForLogging()` when the options ask for safe output, so the frames are the same under A and B.
- **Class:** Dilemma

</details>

<details>
<summary><strong>5. Spectre.Console and the logging integration</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No Spectre dependency; `IFormatterTheme` lets a caller write a Spectre-based theme; `ToStructured()` returns an `IReadOnlyDictionary<string, object?>` that Serilog's `{@Error}` destructures; `LogEncinaError(this ILogger, ...)` logs code and category and passes an exception through `ForLogging()`** | No heavy dependency; no Serilog reference; follows the logging rule | Users who want Spectre write a short theme |
| **B) Reference Spectre.Console and Serilog for first-class adapters** | Out-of-the-box integration | Heavy, optional dependencies in a diagnostic package; Serilog adds a second logging abstraction |

### Chosen Option: **A — No Spectre or Serilog reference**

### Rationale

- The issue lists Spectre integration as optional, and AGENTS.md section 1 makes every feature opt-in; the structured dictionary and the theme interface are the extension points.
- **Class:** Trivial
- **Settled by:** AGENTS.md section 1 (pay-for-what-you-use), the issue's "Spectre.Console integration (optional)", and AGENTS.md section 3 (`ForLogging()`, no message in logs).

</details>

---

## Implementation Phases

### Phase 1: Structured validation details in core (PR-1)

<details>
<summary>Tasks</summary>

1. Edit `src/Encina/Errors/EncinaErrorMetadataKeys.cs`: add `ValidationErrors` (value `validation.errors`, XML docs: an `IReadOnlyList<ValidationError>`).
2. Edit `src/Encina/Validation/ValidationOrchestrator.cs` (line 70-72): pass `details: { [EncinaErrorMetadataKeys.ValidationErrors] = result.Errors }`; the message and the code stay as they are, so no caller breaks.
3. Tests: new `tests/Encina.UnitTests/Core/Validation/ValidationOrchestratorDetailsTests.cs` (details present, property names and messages preserved, code and category unchanged) and one assertion added to `tests/Encina.ContractTests/Core/Validation/ValidationPipelineBehaviorContractTests.cs`. The 12 test files that name `ValidationOrchestrator` (`git grep -c` on `origin/main`) are re-run; the only `.verified.*` snapshot that mentions `ValidationFailed` (`EncinaVerifyTests.PrepareEither_WithEncinaErrorLeft_ReturnsCorrectSnapshot.verified.txt`) is built from an error created directly, so it does not change. Consumers of `GetDetails()` read named keys (`ErrorCategoryHttp.cs:134,148`) or serialize all details for snapshots (`Encina.Testing.Verify/EncinaErrorConverter.cs:42`); no HTTP mapping serializes the whole dictionary.
4. `src/Encina/PublicAPI.Unshipped.txt` line (through `mechanical-fixer`); manifest `Encina.json` edit; changelog fragment `changelog.d/441-validation-error-details.changed.md` (through `mechanical-fixer`).

File count 7 (keys, orchestrator, the new unit test file, the contract test edit, `PublicAPI.Unshipped.txt`, `Encina.json`, the changelog fragment); about 250 lines. ADR-062 category: no new code; `ValidationFailed` stays `ErrorCategory.Validation`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: ValidationOrchestrator (src/Encina/Validation/ValidationOrchestrator.cs:70-72) returns EncinaErrors.Create(ValidationFailed, ErrorCategory.Validation, flattenedMessage) with no details, so the failures cannot be read back structurally.
TASK: Add EncinaErrorMetadataKeys.ValidationErrors and make the orchestrator store result.Errors in details under that key. Keep code, category and message unchanged. Add unit tests and the PublicAPI line.
KEY RULES: No behavior change for existing callers. Details hold the same text as the message and follow the same rule: never logged. At most 15 files.
REFERENCE FILES: src/Encina/Validation/ValidationOrchestrator.cs, src/Encina/Validation/ValidationResult.cs, src/Encina/Errors/EncinaErrorMetadataKeys.cs, src/Encina/Results/EncinaErrors.cs.
```

</details>

### Phase 2: Package, options and themes (PR-2)

<details>
<summary>Tasks</summary>

1. `src/Encina.Diagnostics/Encina.Diagnostics.csproj` (project reference to `Encina`, no other dependency; `InternalsVisibleTo` for `Encina.UnitTests` and `Encina.GuardTests`), `PublicAPI.Shipped.txt` (empty) and `PublicAPI.Unshipped.txt`; add to `Encina.slnx` and `.github/scripts/coverage-weights.json`; new `.github/coverage-manifest/Encina.Diagnostics.json` (through `mechanical-fixer`).
2. `Themes/IFormatterTheme.cs` (`Styled(string text, TextStyle style)`, box characters), `Themes/AnsiTheme.cs`, `Themes/PlainTextTheme.cs` (ASCII box, no escape codes).
3. `FormatterOptions.cs` (`sealed record`: `Theme`, `IncludeMessage` false, `IncludeIdentifiers` false, `MaxFrames`, `HideFrameworkFrames`, `Environment` as a `Func<string, string?>` and `IsOutputRedirected` as a `Func<bool>` so tests do not touch the process environment); `ThemeSelector` inside it: `NO_COLOR` set and non-empty, or output redirected, selects the plain theme.
4. Tests in `tests/Encina.UnitTests/Diagnostics/Formatting/ThemeTests.cs` and `tests/Encina.GuardTests/Diagnostics/Formatting/FormattingGuardTests.cs`; add the project references and nothing else to `Encina.UnitTests.csproj` and `Encina.GuardTests.csproj`.

File count 14 (csproj, 2 PublicAPI, slnx, weights, manifest, 3 theme files, options, 2 tests, 2 test csproj edits); about 600 lines.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: Phase 1 merged. New package src/Encina.Diagnostics (namespace Encina.Diagnostics.Formatting) references only Encina.
TASK: Create the package, FormatterOptions, IFormatterTheme, AnsiTheme, PlainTextTheme and theme selection from NO_COLOR and redirect detection.
KEY RULES: NO_COLOR and redirected output are read through injectable functions (no process-global reads in tests). Never throw from a public method. No DateTime.UtcNow. At most 15 files.
REFERENCE FILES: src/Encina.Security.ABAC.Analyzers/Encina.Security.ABAC.Analyzers.csproj (csproj conventions), src/Encina.SignalR/Encina.SignalR.csproj (project reference and InternalsVisibleTo pattern), src/Encina/PublicAPI.Unshipped.txt (format).
```

</details>

### Phase 3: Pretty printer and `ToPrettyString` (PR-3)

<details>
<summary>Tasks</summary>

1. `PrettyPrinter.cs` (`internal`: builds the box from a list of lines) and `EncinaErrorFormattingExtensions.cs` (`ToPrettyString(this EncinaError, FormatterOptions? = null)`; shows code, category, exception type and correlation id; the message and identifiers only with the options of Design Choice 2; any internal failure returns a fallback line with the code only and never throws).
2. Tests `PrettyPrinterTests.cs` and `EncinaErrorFormattingExtensionsTests.cs`; guard tests added to `FormattingGuardTests.cs`; PublicAPI and manifest edits (through `mechanical-fixer`).

File count 7; about 450 lines.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: Package, options and themes exist.
TASK: Add the box printer and EncinaError.ToPrettyString. Safe by default (Design Choice 2): no message and no user or tenant ids unless options ask; the correlation id is shown.
KEY RULES: Never throw; return a code-only fallback. ADR-062: no new error code. Keep each method at complexity 8 or less. A nullable options parameter that accepts null gets a test of the valid result in the guard project, not an exception test. At most 15 files.
REFERENCE FILES: src/Encina/Results/EncinaErrors.cs, src/Encina/Diagnostics/RedactedException.cs, src/Encina.Diagnostics/FormatterOptions.cs.
```

</details>

### Phase 4: Validation grouping and stack traces (PR-4)

<details>
<summary>Tasks</summary>

1. `ValidationErrorFormatter.cs` and `ToGroupedString(this EncinaError, FormatterOptions? = null)`: reads `EncinaErrorMetadataKeys.ValidationErrors` from `GetDetails`; groups by `PropertyName` (null name under the request group); prints property names, and the messages only with `IncludeMessage`.
2. `StackTraceFormatter.cs` and `ExceptionFormattingExtensions.ToPrettyStackTrace(this Exception, FormatterOptions? = null)`: state-machine and generic-name rewriting, framework-frame filtering by prefix list, a depth limit for inner exceptions (the cycle guard of `RedactedException` is reused by formatting `exception.ForLogging()` unless `IncludeMessage`).
3. Tests: `ValidationErrorFormatterTests.cs`, `StackTraceFormatterTests.cs` (async method, generic method, local function, iterator, cyclic inner exceptions); guard tests added; PublicAPI and manifest edits.
4. The property flag: the targets below set property 70 for `ValidationErrorFormatter.cs` and 50 for `StackTraceFormatter.cs`, so this pull request carries the tests that reach them: edit `tests/Encina.PropertyTests/Encina.PropertyTests.csproj` (a `ProjectReference` to `Encina.Diagnostics`; Phase 2 adds references to the unit and guard projects only) and add `tests/Encina.PropertyTests/Diagnostics/Formatting/ValidationErrorFormatterPropertyTests.cs` (FsCheck: grouping keeps every property name and the total count) and `StackTraceFormatterPropertyTests.cs` (random exception chains never throw).

File count 12 (the 9 above plus the PropertyTests csproj edit and two property test files); about 1,150 lines.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT: ToPrettyString and the themes exist. ValidationOrchestrator now stores structured errors in details.
TASK: Add ToGroupedString and ToPrettyStackTrace as described; demystify async state machines and generics with our own formatter.
KEY RULES: Without IncludeMessage, show property names and frames only. Never throw. Keep each method at complexity 8 or less (CRAP gate); split frame rewriting into small static methods. At most 15 files.
REFERENCE FILES: src/Encina/Validation/ValidationResult.cs, src/Encina/Diagnostics/RedactedException.cs, tests of RedactedException for the cyclic-chain case.
```

</details>

### Phase 5: Structured logging, EventIds (PR-5)

<details>
<summary>Tasks</summary>

1. `ErrorLoggingExtensions.cs`: `ToStructured(this EncinaError)` (dictionary: `code`, `category`, `exception.type`, `exception.stacktrace` from `ForLogging()`, and validation property names; never the message) and `LogEncinaError(this ILogger, EncinaError, LogLevel level = Error)`.
2. `DiagnosticsLog.cs` (`[LoggerMessage]`, `internal static partial class`): 70050 error logged (level parameter, code and category), 70051 formatting fell back to the code-only line (Warning; exception through `ForLogging()`).
3. Register the range: `EventIdRanges.DiagnosticsFormatting = (70050, 70059)` in `src/Encina/Diagnostics/EventIdRanges.cs`, its line in `src/Encina/PublicAPI.Unshipped.txt` (`mechanical-fixer`), the `AssemblyRanges` entry for the assembly `Encina.Diagnostics` in `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`, and in `docs/engineering/eventid-block-registry.md` (through `docs-writer`) one "Registered ranges" row (`DiagnosticsFormatting | 70050-70059 | Testing and tooling | zone`) and one Blocks row whose bounds equal the registered range exactly: `| 70050-70059 | Testing and tooling | #441 dev-experience, DiagnosticsFormatting (2026-11-26) | registered |`. `EventIdRegistryAgreementTests` requires that every Blocks row marked `registered` matches a range of `EventIdRanges.cs` with identical bounds (`Check_BlockRowMarkedRegisteredWithoutMatchingRange`), so a row spanning 70050-70099 and shared with #445 would turn it red; this plan therefore registers only 70050-70059, and #445 adds its own row for 70060-70089 in its own pull request. Run the architecture tests (`EventIdRegistryAgreementTests`).
4. Tests: `ErrorLoggingExtensionsTests.cs` (a fake logger asserts no message text, `RedactedException` as the logged exception, EventId), guard tests; manifest edit; changelog fragment `changelog.d/441-diagnostics-formatting.added.md`. The new types of the package are added to `src/Encina.Diagnostics/PublicAPI.Unshipped.txt` (`mechanical-fixer`), counted below.

File count 11: `ErrorLoggingExtensions.cs`, `DiagnosticsLog.cs`, `EventIdRanges.cs`, core `PublicAPI.Unshipped.txt`, `src/Encina.Diagnostics/PublicAPI.Unshipped.txt`, the `AssemblyRanges` test edit, the registry page, `ErrorLoggingExtensionsTests.cs`, the guard test edit, the manifest, the fragment; about 450 lines.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT: ToPrettyString, ToGroupedString and ToPrettyStackTrace exist in Encina.Diagnostics.
TASK: Add ToStructured and LogEncinaError, DiagnosticsLog with EventIds 70050-70051, and register the range 70050-70059 as AGENTS.md section 7 requires (EventIdRanges field, PublicAPI line, AssemblyRanges entry, one registered-ranges row and one Blocks row with the identical bounds 70050-70059 and status registered; never a row that also names the range of another plan).
KEY RULES: EncinaError.Message never reaches the logger; exceptions go through ForLogging(). Pack EventIds without gaps. The range is in the 10000+ zone (Testing and tooling, after the #4007 extent 70000-70049). At most 15 files.
REFERENCE FILES: src/Encina/Diagnostics/EventIdRanges.cs, docs/engineering/eventid-block-registry.md, tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs, src/Encina/Diagnostics/RequestIdentityLog.cs.
```

</details>

### Phase 6: Documentation and finalization (PR-6)

<details>
<summary>Tasks</summary>

1. Through `docs-writer`: `src/Encina.Diagnostics/README.md`, `docs/features/error-formatting.md` (options, the safe default, examples), `docs/INVENTORY.md`, `ROADMAP.md`.
2. Verification: `dotnet build --configuration Release` (0 warnings), `dotnet test` of the unit and guard projects, the architecture tests, the per-flag measurement of AGENTS.md section 9.

About 5 files and 250 lines.

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT: Encina.Diagnostics is complete and merged.
TASK: Document it through docs-writer and run the verification commands; report the measured per-flag coverage of every new file against .github/coverage-manifest/Encina.Diagnostics.json.
KEY RULES: No hand-typed coverage figures (covref markers). English only. Follow .claude/skills/encina-docs/SKILL.md.
REFERENCE FILES: docs/features/, docs/INVENTORY.md, src/Encina.SignalR/README.md.
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard / Specification | Relevance | Where it applies in the feature |
|--------------------------|-----------|---------------------------------|
| ECMA-48 / ANSI SGR escape sequences | Defines the color and attribute sequences | `AnsiTheme` |
| NO_COLOR convention (no-color.org) | Color is disabled when `NO_COLOR` is set and non-empty | Theme selection |
| Unicode box-drawing block (U+2500 to U+257F) | Box characters, with an ASCII fallback | `PlainTextTheme` |
| OpenTelemetry semantic conventions for exceptions (`exception.type`, `exception.stacktrace`) | Telemetry carries type and trace, not the message | `ToStructured` keys |
| .NET stack frame model (`System.Diagnostics.StackTrace`, `StateMachineAttribute`) | What the demystifier rewrites | `StackTraceFormatter` |
| `Console.IsOutputRedirected` | Switch to plain text when piped | Theme selection |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `EncinaError` accessors | `src/Encina/Results/EncinaErrors.cs` | Code, category, details |
| `ForLogging()` / `RedactedException` | `src/Encina/Diagnostics/RedactedException.cs` | Safe stack traces and logging |
| `ValidationOrchestrator`, `ValidationError` | `src/Encina/Validation/` | Source of grouped failures (Phase 1) |
| `EventIdRanges` and the block registry | `src/Encina/Diagnostics/EventIdRanges.cs`, `docs/engineering/eventid-block-registry.md` | New range 70050-70059 |
| `Spectre.Console` pin | `Directory.Packages.props` (0.57.2, used by `Encina.Cli`) | Not referenced; a theme may use it |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Diagnostics` (`EventIdRanges.DiagnosticsFormatting`, new) | 70050-70059 | Ids used: 70050 error logged, 70051 formatting fallback. Inside the Testing and tooling area, after the #4007 extent 70000-70049. The registry Blocks row is `70050-70059 | Testing and tooling | #441 ... | registered`, with bounds identical to the registered range; #445 owns 70060-70089 with its own row (two plans in one 50-id area need two rows, never one row `70050-70099 registered`). Needs the `EventIdRanges` field, its PublicAPI line, an `AssemblyRanges` entry and the two registry rows (Phase 5) |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production (new package) | 14 | csproj, 2 PublicAPI, 3 theme files, options, printer, 3 extension classes, 2 formatters, log class |
| Production (core edits) | 4 | metadata keys, orchestrator, `EventIdRanges.cs`, `PublicAPI.Unshipped.txt` |
| Tests | 15 | 7 unit files (one new in core), guard tests, contract and allocation test edits, 2 property test files, 3 test csproj edits (unit, guard, property) |
| Manifest, weights, slnx, registry, fragments | 6 | `Encina.Diagnostics.json`, `Encina.json`, weights, slnx, registry, 2 fragments |
| Documentation | 4 | README, feature page, INVENTORY, ROADMAP |
| **Total** | **~43 file touches over 6 pull requests** | PR-1 7, PR-2 14, PR-3 7, PR-4 12, PR-5 11, PR-6 5 files (shared files such as the manifest and the `PublicAPI.Unshipped.txt` files are touched in several); about 250 + 600 + 450 + 1,150 + 450 + 250 = 3,150 lines in total (each pull request is under 1,500) |

Coverage targets per file (flags unit, guard; property for the formatters): `PrettyPrinter.cs` unit 90, guard 20; `AnsiTheme.cs` and `PlainTextTheme.cs` unit 95; `FormatterOptions.cs` unit 90; `EncinaErrorFormattingExtensions.cs` unit 90, guard 80 (`ToPrettyString` null options is valid; the guard test asserts the valid result, not an exception); `ValidationErrorFormatter.cs` unit 90, property 70 (FsCheck: grouping preserves every property name and count); `StackTraceFormatter.cs` unit 85, property 50 (random exception chains never throw); `ErrorLoggingExtensions.cs` unit 90, guard 85 (null logger throws `ArgumentNullException`); `DiagnosticsLog.cs` unit 80. Justification: the code is pure string building, fully reachable by unit tests; guard targets reflect that most public methods take a nullable options parameter that must not throw. Contract and integration: not applicable (no public interface other than `IFormatterTheme`, which has two implementations covered by unit tests; no infrastructure). Benchmarks and load: not applicable (a developer tool, not a hot path).

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina is a pre-1.0 .NET 10 / C# 14 library (AGENTS.md rules: zero warnings, XML docs, real-code tests, per-flag coverage, EncinaError.Message never in logs, exceptions through ForLogging()). Issue #441 adds the package Encina.Diagnostics: pretty printing of EncinaError, grouped validation errors, demystified stack traces, ANSI and plain themes, safe logging helpers.

IMPLEMENTATION OVERVIEW: Phase 1 structured validation details in core. Phase 2 package, options, themes. Phase 3 printer and ToPrettyString. Phase 4 ToGroupedString and ToPrettyStackTrace. Phase 5 ToStructured, LogEncinaError, EventIds 70050-70059 and their registration. Phase 6 documentation through docs-writer. Six pull requests, each at most 15 files and 1500 lines.

KEY PATTERNS: Safe by default (no message, no user or tenant ids); formatters never throw; NO_COLOR and redirect detection through injectable functions; extension methods with optional FormatterOptions, no DI registration; own stack trace formatter; methods at complexity 8 or less.

REFERENCE FILES: src/Encina/Results/EncinaErrors.cs, src/Encina/Diagnostics/RedactedException.cs, src/Encina/Validation/ValidationOrchestrator.cs, src/Encina/Diagnostics/EventIdRanges.cs, docs/engineering/eventid-block-registry.md, docs/plans/dsr-implementation-plan-404.md (plan style).
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Formatting is stateless, not data caching |
| 2 | OpenTelemetry | ❌ | The issue defers "attach formatted errors to spans"; AGENTS.md forbids the message in activity tags, and the exception type and trace already travel as `exception.type` and `exception.stacktrace` through `ForLogging()`; the dashboard plan (#445) holds the span view |
| 3 | Structured Logging | ✅ | Phase 5: `ToStructured`, `LogEncinaError`, `DiagnosticsLog` EventIds 70050-70051 in the new range, error code and exception type only |
| 4 | Health Checks | ❌ | A formatting utility, not a service |
| 5 | Validation | ✅ | Phase 1 stores structured validation errors in `details`; Phase 4 groups them |
| 6 | Resilience | ❌ | No external calls |
| 7 | Distributed Locks | ❌ | Stateless |
| 8 | Transactions | ❌ | Error presentation only |
| 9 | Idempotency | ❌ | Stateless |
| 10 | Multi-Tenancy | ✅ | The tenant id is not printed by default (Design Choice 2); `IncludeIdentifiers` is an explicit opt-in and the logging helpers never include identifiers |
| 11 | Module Isolation | ❌ | A cross-cutting formatting utility with no module scope |
| 12 | Audit Trail | ❌ | A presentation concern; the formatted text is never stored |

---

## Next Steps

1. The maintainer answers Dilemmas 2, 3 and 4 when work starts; Phase 1 (structured validation details) starts after Dilemma 3, and it changes no behavior for existing callers.
2. Open the sub-issues for PR-1 to PR-6 and brief Phase 1.
3. Tell the #445 plan owner when the package and the `ValidationErrors` key are merged, so the dashboard error panel can reuse them.
