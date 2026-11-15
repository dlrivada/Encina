# Implementation Plan: Security-Focused Validation (`Encina.Validation.Security`)

> **Issue**: [#232](https://github.com/dlrivada/Encina/issues/232)
> **Type**: Feature
> **Related**: #230 (owns the runner and the decision that lets attributes see the service provider; a prerequisite of S4 and S5), #227 and #228 (siblings of the family), #234 and #236 (planned apart), #726 (central validation instrumentation), #3415 (a code-less validation `Left` never becomes a 400), #3426 (which error text a client may see), ADR-036, ADR-054, ADR-062 (merged: error category contract)
> **Complexity**: Medium-high (two new packages, one adapter in `Encina.Security.Audit`, seven detectors and attributes, no persistence)
> **Estimated Scope**: ~1,700 lines of production code + ~3,300 lines of tests (~600 of them payload corpora) + ~400 lines of documentation, in nine pull requests of at most 15 files and 1,000 lines each

---

## Summary

### What exists today (read on 2026-11-15, main `db1e7f688`)

- No security validation exists in the validation providers. `Encina.Security.Sanitization` sanitizes (changes) input: `[SanitizeSql]`, `[SanitizeHtml]`, `[StripHtml]`, an input and an output pipeline behavior (`InputSanitizationPipelineBehavior`, `OutputEncodingPipelineBehavior`), `SqlSanitizer`, `ShellSanitizer`, `XmlSanitizer`, `JsonSanitizer` (all `internal`), the `HtmlSanitizer` package, EventIds 1-99 (`Sanitization = (1, 99)`) and its own `SanitizationDiagnostics` (`src/Encina.Security.Sanitization/`). It rejects nothing and reports no CWE. The issue's alternatives reject "sanitization instead of validation" as lossy.
- The three validation providers accept DataAnnotations `ValidationAttribute` subclasses (DataAnnotations and MiniValidator, by reflection) and FluentValidation rules: `DataAnnotationsValidationProvider` builds `new ValidationContext(request, serviceProvider: null, items: null)` (`DataAnnotationsValidationProvider.cs:31`), so an attribute cannot read options from the container today; the plan of #230 adds the service provider (its register row D3). FluentValidation validators come from DI, and `FluentValidationProvider` copies `CorrelationId`, `UserId` and `TenantId` into `RootContextData` (`FluentValidationProvider.cs:52-63`); `MiniValidationProvider` ignores the context (`MiniValidationProvider.cs:31`) and MiniValidation 0.9.2 offers `TryValidate(target, IServiceProvider, out errors)`.
- `Encina.Security.Audit` has `IOperationAuditStore.RecordAsync(OperationAuditEntry, ct)` returning `ValueTask<Either<EncinaError, Unit>>` (`Abstractions/IOperationAuditStore.cs:50`); the package references `Encina`, `Encina.DomainModeling` and `Encina.Messaging` (`Encina.Security.Audit.csproj`), which a validation package should not pull in.
- Not in `src`: `NoHtmlTags`, `NoScriptInjection`, `NoSqlInjection`, `SafeFileName`, `NoPathTraversal`, `NoLdapInjection`, `NoCommandInjection`, `AddEncinaSecurityValidation`, `SecurityValidationOptions`, any package named `Encina.Validation.Security`, any EventId range for validation security.

### What this plan delivers

An opt-in package `Encina.Validation.Security` with seven rules backed by one detection engine (input normalization, then bounded non-backtracking patterns, three strictness levels), exposed as DataAnnotations attributes (so they work under `Encina.DataAnnotations` and `Encina.MiniValidator`) and, in a satellite package `Encina.Validation.Security.FluentValidation`, as FluentValidation rule builder extensions with the same names the issue shows. A violation is a `ValidationError` that never contains the input, a counter and an activity event (tenant tag when the tenant is known), a warning log by rule and CWE id, and an operation audit entry through a port whose adapter lives in `Encina.Security.Audit`. The rules are heuristics and defense in depth, not a replacement for parameterized queries, output encoding or path canonicalization; the documentation says so.

- Affected packages: `Encina.Validation.Security` (new), `Encina.Validation.Security.FluentValidation` (new), `Encina.Security.Audit` (adapter), `Encina.FluentValidation` (one `RootContextData` entry), `Encina` (EventIds only).
- Provider category: Validation (3): attributes serve DataAnnotations and MiniValidator, the satellite serves FluentValidation, and a contract test pins that the same payload gives the same verdict through all three.
- Shared pieces owned here: `ISecurityViolationCollector`, `ISecurityViolationSink` and the strictness levels. Sibling plans refer to nothing here.

---

## Design Choices

<details>
<summary><strong>1. Package layout and the FluentValidation dependency</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One package `Encina.Validation.Security` that references FluentValidation** (the issue's single package) | One package to install | A DataAnnotations or MiniValidator application pulls FluentValidation transitively |
| **B) `Encina.Validation.Security` (engine, options, attributes, DI) plus `Encina.Validation.Security.FluentValidation` (rule builder extensions)** | Pay-for-what-you-use; the same layout as one satellite per provider | Two packages, two READMEs, two manifests |

### Chosen Option: **B — Core package plus a FluentValidation satellite** (recommended, pending the maintainer)

### Rationale

- AGENTS.md section 1 says every feature is opt-in and pay-for-what-you-use; B keeps the dependency out of two of the three providers' users. A wins when the maintainer prefers a single package name for the whole feature, as the issue states.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Detection strategy</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Heuristic detectors per rule: input normalization (Unicode NFKC, bounded percent-decoding and HTML-entity decoding), then non-backtracking patterns, three strictness levels** | One CWE-tagged verdict per rule; no extra dependency; bounded cost | Heuristics have false negatives and positives; the strictness levels trade them |
| **B) Allow-list only (reject anything outside a character class)** | Strongest guarantee for ids and file names | Unusable for free text (names, search terms), which is what the issue's examples validate |
| **C) Reuse `Encina.Security.Sanitization`: a value changed by the sanitizer is a violation** | No second pattern list | The sanitizers are `internal`, change meaning (they repair), carry the `HtmlSanitizer` dependency and report no rule or CWE |

### Chosen Option: **A — Normalizing heuristic detectors** (recommended, pending the maintainer)

### Rationale

- The rules must say which attack was found (audit, counter, CWE); only A produces that. B wins for identifiers and file names, which is why `SafeFileName` is an allow-list inside option A; C wins when the maintainer wants the two packages to share one rule set.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. How a violation reaches the audit store</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A scoped `ISecurityViolationCollector` the rules write into (reachable through the service provider the providers expose), and a pipeline behavior that awaits `ISecurityViolationSink.RecordAsync` for each collected violation after validation** | Async, awaited, same request; no background loop; works for sync attributes | The behavior must run outside the validation behavior; a registration order risk |
| **B) A bounded channel and a hosted writer** | Rules need no scope | A background loop; a full channel or a crash loses records; the failure rules of AGENTS.md section 3 for background work apply |
| **C) Synchronous sink called from the attribute** | Simple | Sync-over-async on a database call is forbidden (AGENTS.md section 3) |

### Chosen Option: **A — Collector plus an awaiting behavior** (recommended, pending the maintainer)

### Rationale

- Only A awaits the audit write inside the request and can fail the registration when the audit is enabled and no sink exists. `AddEncinaSecurityValidation` inserts the audit behavior before an already registered `ValidationPipelineBehavior<,>` descriptor and a DI test proves both registration orders. B wins when the validation runs outside any request scope (a batch importer).
- Fail closed: a missing sink with `AuditViolations = true` fails start-up validation; an audit write that fails is logged by code and does not turn the denied request into a success.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. How an attribute gets its options</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) From `IOptions<SecurityValidationOptions>` through `ValidationContext.GetService`, with a per-attribute override (`[NoSqlInjection(SecurityStrictness.Strict)]`) and the `Standard` defaults when no service provider is present** | Configured once in `AddEncinaSecurityValidation`; attribute override for one field | Needs the providers to pass the service provider (#230 register row D3); the `Standard` fallback applies when it is absent |
| **B) Constructor parameters only** | No dependency on #230 | Every attribute repeats its level; the issue's `AllowedTags` list cannot be configured once |

### Chosen Option: **A — Options from the container with an attribute override** (recommended, pending the maintainer)

### Rationale

- The issue's configuration example sets `AllowedTags` and `StrictMode` once. The fallback never weakens the check: a missing service provider gives `Standard`, not `Lenient`. B wins if #230's register row D3 is not accepted.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Validation instead of sanitization</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A new validation package that rejects (the issue's proposal)** | A verdict and an audit trail; no data is altered | Does not repair input |
| **B) Add a reject mode to `Encina.Security.Sanitization`** | One package | Mixes two verbs in one pipeline; the issue rejects this alternative |

### Chosen Option: **A — A separate rejecting package**

### Rationale

- The issue states the alternative and rejects it ("sanitization can be lossy and may not catch all attack vectors"); the two packages complement each other: a request may be validated and, in the same pipeline, sanitized.

**Class:** Trivial
**Settled by:** issue #232, "Alternatives Considered" 1, and the package name `Encina.Validation.Security` in its Proposed Solution

</details>

### Register of defaults and semantics

| Id | Choice | Class | Basis |
|----|--------|-------|-------|
| C1 | A violation denies the request; no mode reports success on a detected pattern | Settled | AGENTS.md section 3 (security gates fail closed) |
| C2 | Neither an error, a log, a tag nor an audit entry contains the input; they carry the rule id, the CWE id, the property name and the request type | Settled | AGENTS.md section 3 (`EncinaError.Message` and plaintext rules); SPEC-002 DEC-006 |
| C3 | A null or empty value is valid (absence is `[Required]`'s concern); a value longer than `MaxInputLength` is a violation, never scanned (bounded cost, fail closed) | Settled | AGENTS.md section 3 (fail closed when a lookup cannot complete) |
| C4 | Patterns use `RegexOptions.NonBacktracking` with `CultureInvariant`, so detection time is linear in the input length | Settled | AGENTS.md section 3 (a gate must not be a denial-of-service vector); the .NET `NonBacktracking` engine |
| C5 | The audit entry is written through `IOperationAuditStore` (ADR-036) by the adapter in `Encina.Security.Audit`; the validation package never references the audit package | Settled | ADR-036; `Encina.Security.Audit.csproj` references `Encina.Messaging` and `Encina.DomainModeling` |
| C6 | Telemetry: counter `encina.validation.security.violations` with tags `rule` and `strictness`, plus an event on `Activity.Current`; the tenant tag only when the tenant is known (ADR-054 D4); no `ActivitySource` of its own (one span per property would dwarf the check). MiniValidator exposes no hook for context items, so its violations carry no tenant tag | Dilemma (recommended, pending the maintainer) | ADR-054 D3 asks one source per package; the alternative is a source with a span per call |
| C7 | No new `EncinaError` code and no new `ErrorCategory`: a violation is a `ValidationError` entry of the existing validation `Left`; the `Left` itself is #3415's | Settled | ADR-062 (categories apply to `EncinaErrors.Create` calls; none is added) |
| C8 | EventIds 590-592 packed from the start of the family block: 590 violation detected (warning), 591 violation audit failed (error), 592 input over the length limit (warning) | Trivial | AGENTS.md section 7; ADR-021; `artifacts/decisions/plan-batch/eventid-blocks.md` |
| D1 | Strictness levels `Lenient`, `Standard`, `Strict`, default `Standard`, `MaxInputLength` 4096 characters, `AuditViolations` default `false` | Dilemma (recommended, pending the maintainer) | defaults; the issue's `StrictMode` is the `Strict` level |
| D2 | `AllowSafeHtml` with `AllowedTags` permits the listed tags with no attributes; any attribute, event handler, `javascript:` or `data:` URI in an allowed tag is still a violation | Dilemma (recommended, pending the maintainer) | semantics of the issue's `AllowedTags` example |
| D3 | `SafeFileName` is an allow-list (letters, digits, `.`, `-`, `_`, one extension, no reserved Windows device names, at most 255 characters); `NoPathTraversal` is a detector (`..`, encoded and doubly encoded separators, absolute roots, UNC and null bytes) | Dilemma (recommended, pending the maintainer) | rule scope |
| D4 | The audit behavior is registered by `AddEncinaSecurityValidation` and inserted before the validation behavior; with `AuditViolations = false` it is not registered | Dilemma (recommended, pending the maintainer) | follows Design Choice 3 |
| D5 | Payload corpora under test data are curated from the public OWASP cheat sheets linked in the issue and from CWE examples; every payload is paired with a benign near-miss that must pass (false-positive guard) | Dilemma (recommended, pending the maintainer) | test scope |

---

## Implementation Phases

### Phase 1: Package Skeleton, Contracts and Options

> **Goal**: The new package with its public vocabulary and validated options (pull request S1).

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.Validation.Security/Encina.Validation.Security.csproj` (references `Encina`; `Microsoft.Extensions.Options`, `Microsoft.Extensions.DependencyInjection.Abstractions`, `Microsoft.Extensions.Logging.Abstractions`; `PublicApiAnalyzers`), `PublicAPI.Shipped.txt`, `PublicAPI.Unshipped.txt`; add it to `Encina.slnx`.
2. **Create** `SecurityStrictness.cs` (`Lenient`, `Standard`, `Strict`), `SecurityRuleId.cs` (`Xss`, `SqlInjection`, `PathTraversal`, `LdapInjection`, `CommandInjection`, `UnsafeFileName`, `InputTooLong`, each mapped to a CWE id: 79, 89, 22, 90, 78, 22, 20), `SecurityViolation.cs` (record: rule, CWE, property name, request type; no value), `ISecurityDetector.cs` (`bool TryDetect(ReadOnlySpan<char> input, SecurityStrictness strictness, out SecurityRuleId rule)`), `SecurityValidationOptions.cs` (`Strictness`, `MaxInputLength`, `AuditViolations`, `XssProtection.AllowSafeHtml` and `AllowedTags`, `SqlInjection.StrictMode`) and `SecurityValidationOptionsValidator.cs : IValidateOptions<...>` (length above 0, tags are plain element names).
3. **Modify** `tests/Encina.UnitTests/Encina.UnitTests.csproj` and `tests/Encina.GuardTests/Encina.GuardTests.csproj` (one `ProjectReference` each); **create** `.github/coverage-manifest/Encina.Validation.Security.json` (through `mechanical-fixer`).
4. Tests (S1): options and validator unit tests, a guard test of the public constructors and methods.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
Encina (.NET 10, C# 14, pre-1.0, no backward compatibility). Issue #232 adds a rejecting security validation package.
Read docs/plans/security-focused-validation-implementation-plan-232.md, Design Choices 1, 2, 4 and register rows C1-C4, D1.

TASK:
1. Create the Encina.Validation.Security project, add it to Encina.slnx.
2. Create the vocabulary (strictness, rule ids with CWE, SecurityViolation without a value), ISecurityDetector and the
   options with their validator.

KEY RULES:
- A violation never carries the input; options validated at start-up; XML docs on every public member; no [Obsolete].
- PublicAPI, slnx and manifest edits through mechanical-fixer where they are not source code.

REFERENCE FILES:
- src/Encina.Security.Sanitization/ (package layout, options), src/Encina.Security.ABAC/Encina.Security.ABAC.csproj
```

</details>

---

### Phase 2: Normalizer, XSS and Path Traversal Detectors

> **Goal**: The shared normalization and the first two detectors (pull request S2).

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `SecurityInputNormalizer.cs` (internal; NFKC, percent-decoding for at most two passes, HTML-entity decoding for at most two passes, all bounded by `MaxInputLength`), `Detectors/XssDetector.cs` (tags, script contexts, event handlers, `javascript:` and `data:` URIs; honors `AllowSafeHtml` and `AllowedTags`, register row D2) and `Detectors/PathTraversalDetector.cs` (register row D3).
2. **Modify** `PublicAPI.Unshipped.txt` and the manifest (through `mechanical-fixer`).
3. Tests (S2): normalizer unit and property (idempotent past the bound, never throws, output length bounded), XSS and path detector unit tests over corpora (`TestData/xss-payloads.txt`, `xss-benign.txt`, `path-payloads.txt`, `path-benign.txt`, register row D5) and a guard test.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
Issue #232, plan docs/plans/security-focused-validation-implementation-plan-232.md, phase 2 of 9. Depends on S1.

TASK:
1. Write the bounded normalizer and the XSS and path traversal detectors (NonBacktracking patterns, linear time).
2. Write the corpora and the unit, property and guard tests.

KEY RULES:
- Linear-time matching only; nothing logs or returns the input; every payload has a benign near-miss that must pass.

REFERENCE FILES:
- src/Encina.Validation.Security/ (phase 1), the OWASP XSS and Input Validation cheat sheets linked in issue #232
```

</details>

---

### Phase 3: SQL, LDAP and Command Injection Detectors

> **Goal**: The other three detectors (pull request S3).

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `Detectors/SqlInjectionDetector.cs` (tautologies, stacked statements, comment terminators, `UNION SELECT`, time-delay functions; `SqlInjection.StrictMode` widens the keyword set), `Detectors/LdapInjectionDetector.cs` (filter metacharacters `* ( ) \ NUL` in unescaped positions, `)(` filter breaks) and `Detectors/CommandInjectionDetector.cs` (shell metacharacters, command substitution, redirection, newline separators, Windows `&`, `|`, `%`).
2. **Modify** `PublicAPI.Unshipped.txt` and the manifest (through `mechanical-fixer`).
3. Tests (S3): per-detector unit tests over corpora (`sql-*`, `ldap-*`, `command-*` payload and benign files) and a guard test.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
Issue #232, plan docs/plans/security-focused-validation-implementation-plan-232.md, phase 3 of 9. Depends on S2.

TASK:
1. Write the SQL, LDAP and command injection detectors on the shared normalizer with NonBacktracking patterns.
2. Write the corpora (payload and benign near-miss) and the unit and guard tests.

KEY RULES:
- Heuristics: document false-positive and false-negative behavior per strictness level in the XML docs.
- Nothing logs or returns the input.

REFERENCE FILES:
- src/Encina.Validation.Security/Detectors/ (phase 2), the OWASP cheat sheets linked in issue #232
```

</details>

---

### Phase 4: DataAnnotations Attributes

> **Goal**: The rules as `ValidationAttribute` subclasses for DataAnnotations and MiniValidator (pull request S4; needs #230 S4).

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `Attributes/SecurityValidationAttribute.cs` (abstract; reads `IOptions<SecurityValidationOptions>` through `ValidationContext.GetService`, falls back to `Standard`; optional strictness override; writes the `SecurityViolation` into the collector when one is available; never puts the value in `ErrorMessage`) and seven attributes: `NoHtmlTagsAttribute`, `NoScriptInjectionAttribute`, `NoSqlInjectionAttribute`, `SafeFileNameAttribute`, `NoPathTraversalAttribute`, `NoLdapInjectionAttribute`, `NoCommandInjectionAttribute`.
2. **Modify** `PublicAPI.Unshipped.txt` and the manifest (through `mechanical-fixer`).
3. Tests (S4): per attribute through `Validator.TryValidateObject` (DataAnnotations) and through `MiniValidator.TryValidate` (two unit test files); a guard test; a contract test that all attributes derive from the base, return the same message shape and never echo the value.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT:
Issue #232, plan docs/plans/security-focused-validation-implementation-plan-232.md, phase 4 of 9. Depends on S1-S3 and
the plan of issue #230 (the providers pass their service provider to the validation context).

TASK:
1. Write SecurityValidationAttribute and the seven attributes; options from the container with a Standard fallback.
2. Test each through the real DataAnnotations validator and the real MiniValidator.

KEY RULES:
- Fail closed: a missing service provider gives Standard, never Lenient; the value never appears in a message.
- XML docs with examples; PublicAPI and manifest through mechanical-fixer.

REFERENCE FILES:
- src/Encina.DataAnnotations/DataAnnotationsValidationProvider.cs, src/Encina.MiniValidator/MiniValidationProvider.cs
- docs/plans/async-cross-field-validation-implementation-plan-230.md (register row D3)
```

</details>

---

### Phase 5: Registration, Collector, Sink Port and Audit Behavior

> **Goal**: `AddEncinaSecurityValidation`, the collector and the awaiting audit behavior (pull request S5).

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `ISecurityViolationCollector.cs` and `SecurityViolationCollector.cs` (scoped, thread-safe list of `SecurityViolation`), `ISecurityViolationSink.cs` (`ValueTask<Either<EncinaError, Unit>> RecordAsync(SecurityViolation violation, IRequestContext context, CancellationToken ct)`), `SecurityViolationAuditPipelineBehavior.cs` (calls the next step, then awaits the sink for each collected violation; a `Left` from the sink is logged by code and never turns the denied request into a success) and `SecurityValidationServiceCollectionExtensions.cs` (`AddEncinaSecurityValidation(Action<SecurityValidationOptions>? configure = null)`: options with `ValidateOnStart`, detectors as singletons, the collector scoped, and with `AuditViolations = true` the behavior inserted before an existing `ValidationPipelineBehavior<,>` descriptor and a start-up check that a sink is registered).
2. **Modify** `PublicAPI.Unshipped.txt` and the manifest (through `mechanical-fixer`).
3. Tests (S5): a DI test with `ValidateOnBuild` and `ValidateScopes` in both registration orders and with and without a sink, a behavior unit test, a collector unit test and guard tests for the extension and the behavior.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT:
Issue #232, plan docs/plans/security-focused-validation-implementation-plan-232.md, phase 5 of 9. Depends on S1-S4.
AGENTS.md section 3: registration completeness, never a scoped service in a singleton, errors never swallowed.

TASK:
1. Write the collector, the sink port, the audit behavior and AddEncinaSecurityValidation.
2. Prove registration completeness in both registration orders with a ValidateOnBuild and ValidateScopes test.

KEY RULES:
- The audit write is awaited inside the request; a missing sink with AuditViolations = true fails start-up.
- Codes and rule ids only in logs and entries; never the input.

REFERENCE FILES:
- src/Encina/Validation/ValidationPipelineBehavior.cs, src/Encina.Security.Sanitization/ServiceCollectionExtensions.cs
```

</details>

---

### Phase 6: FluentValidation Satellite

> **Goal**: The rule builder extensions of the issue's example (pull request S6).

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.Validation.Security.FluentValidation/Encina.Validation.Security.FluentValidation.csproj` (references `Encina.Validation.Security` and FluentValidation), `SecurityRuleBuilderExtensions.cs` (`NoHtmlTags`, `NoScriptInjection`, `NoSqlInjection`, `SafeFileName`, `NoPathTraversal`, `NoLdapInjection`, `NoCommandInjection` on `IRuleBuilder<T, string>`) and `SecurityPropertyValidator.cs` (a `PropertyValidator<T, string>` over the detectors; options and collector from the service provider placed in `RootContextData`), the two `PublicAPI` files, a slnx entry and a manifest.
2. **Modify** `src/Encina.FluentValidation/FluentValidationProvider.cs`: add `RootContextData["Encina.ServiceProvider"] = _serviceProvider` beside the existing context entries (`:52-63`), its `PublicAPI` is unchanged (no signature change) and its manifest; **modify** `tests/Encina.UnitTests/FluentValidation/FluentValidationProviderTests.cs` (one added test); **modify** the unit and guard test projects (one `ProjectReference` each).
3. Tests (S6): extension unit tests over a real `AbstractValidator`, a guard test, and the cross-provider contract test `tests/Encina.ContractTests/Validation/SecurityValidationEquivalenceContractTests.cs` (the same payload corpus gives the same verdict through the attribute under DataAnnotations and MiniValidator and through the Fluent extension).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```text
CONTEXT:
Issue #232, plan docs/plans/security-focused-validation-implementation-plan-232.md, phase 6 of 9. Depends on S1-S5.
AGENTS.md section 5: the three validation providers stay consistent.

TASK:
1. Create the satellite package with the seven rule builder extensions.
2. Expose the service provider to Fluent rules through RootContextData in FluentValidationProvider.
3. Write the extension tests and the cross-provider equivalence contract test.

KEY RULES:
- The satellite only adapts: the detectors, options and collector come from Encina.Validation.Security.
- No value in any message.

REFERENCE FILES:
- src/Encina.FluentValidation/FluentValidationProvider.cs, src/Encina.FluentValidation/ServiceCollectionExtensions.cs
```

</details>

---

### Phase 7: Observability and Logging

> **Goal**: Counter, activity event, log messages and the registered EventId range (pull request S7).

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `src/Encina/Diagnostics/EventIdRanges.cs`: register `ValidationSecurity = (590, 599)`; **create** in `src/Encina.Validation.Security/Diagnostics/` `SecurityValidationDiagnostics.cs` (meter `Encina.Validation.Security`, counter of register row C6, an event on `Activity.Current`) and `SecurityValidationLog.cs` (`[LoggerMessage]`: 590 violation detected with rule id, CWE, property name and request type; 591 audit failed with the error code; 592 input over the limit).
2. **Modify** the attribute base, the behavior and the satellite property validator to call them; **modify** `EncinaEventIdAllocationTests.cs`, the `PublicAPI.Unshipped.txt` of `Encina` and both manifests (through `mechanical-fixer`).
3. Tests (S7): a `MeterListener` test (tags, tenant tag only when known), a log test (ids, ranges, the input absent), a start-up test that the EventIds are inside the range.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```text
CONTEXT:
Issue #232, plan docs/plans/security-focused-validation-implementation-plan-232.md, phase 7 of 9. ADR-021 and ADR-054.

TASK:
1. Register ValidationSecurity (590, 599) in EventIdRanges.cs first; write the log class (590-592) and the diagnostics.
2. Call them from the attribute base, the audit behavior and the satellite validator.

KEY RULES:
- Pack ids sequentially; tenant tag only when the tenant is known; the input never appears in a tag, event or log.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs, src/Encina.Security.Sanitization/Diagnostics/SanitizationDiagnostics.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

---

### Phase 8: Audit Adapter in `Encina.Security.Audit`

> **Goal**: The `IOperationAuditStore` adapter of the port (pull request S8).

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `src/Encina.Security.Audit/Encina.Security.Audit.csproj`: one `ProjectReference` to `Encina.Validation.Security` (the validation package references only `Encina`, so no cycle).
2. **Create** `src/Encina.Security.Audit/OperationAuditSecurityViolationSink.cs` (`ISecurityViolationSink` over `IOperationAuditStore.RecordAsync`; entry carries action `validation.security_violation`, the rule id, the CWE id, the request type and the correlation, user and tenant ids; never the input) and register it from `AddEncinaAudit` with `TryAddScoped<ISecurityViolationSink, OperationAuditSecurityViolationSink>()` so an application's own sink wins in any registration order.
3. **Modify** the `PublicAPI.Unshipped.txt` and the manifest of `Encina.Security.Audit` (through `mechanical-fixer`).
4. Tests (S8): unit (entry content, the input absent), DI (with `AddEncinaSecurityValidation` and `AddEncinaAudit` in both orders, `ValidateOnBuild`), guard.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```text
CONTEXT:
Issue #232, plan docs/plans/security-focused-validation-implementation-plan-232.md, phase 8 of 9. Depends on S5.
ADR-036: operation audit goes through IOperationAuditStore.

TASK:
1. Add the project reference and OperationAuditSecurityViolationSink to Encina.Security.Audit; register it with TryAdd
   so the application's own sink wins in any registration order.
2. Test the entry content, the registration orders and the null checks.

KEY RULES:
- The audit entry carries codes and ids only; a Left from the store is returned to the behavior, which logs it.

REFERENCE FILES:
- src/Encina.Security.Audit/Abstractions/IOperationAuditStore.cs, src/Encina.Security.Audit/ServiceCollectionExtensions.cs
```

</details>

---

### Phase 9: Benchmark, Documentation and Finalization

> **Goal**: The hot-path evidence, the usage guide and the changelog (pull request S9).

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `tests/Encina.BenchmarkTests/Encina.Benchmarks/SecurityValidationBenchmarks.cs` (detectors on benign and hostile inputs; `BenchmarkSwitcher` per `AGENTS.md` section 9) and a load-test justification `.md` (stateless and bounded).
2. **Create** through `docs-writer`: the READMEs of both packages, `docs/features/security-focused-validation.md` (rules, strictness, limits of heuristics, the relation to `Encina.Security.Sanitization`), an ADR for the package split and the audit route (number assigned when the ADR pull request opens) and its index line; **modify** `docs/INVENTORY.md`.
3. **Create** `changelog.d/232-security-focused-validation.added.md` (through `mechanical-fixer`).
4. Build verification: `dotnet build --configuration Release` with zero warnings; per-flag coverage of the touched files against the manifest targets below.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 9</strong></summary>

```text
CONTEXT:
Issue #232, plan docs/plans/security-focused-validation-implementation-plan-232.md, phase 9 of 9.

TASK:
1. Write the benchmark and the load-test justification; document both packages, the rules and their limits.
2. Add the ADR, the changelog fragment and run the build and the per-flag coverage measurement.

KEY RULES:
- Never type coverage or benchmark figures by hand; cite cov: markers (SPEC-001). Pages through docs-writer;
  fragments through mechanical-fixer. State that the rules are defense in depth, not a replacement for parameterized
  queries, encoding or path canonicalization.

REFERENCE FILES:
- .claude/skills/encina-docs/SKILL.md, tests/Encina.BenchmarkTests/Encina.Benchmarks/ValidationBenchmarks.cs
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Use in this feature |
|--------|-----------|---------------------|
| OWASP Input Validation Cheat Sheet (linked in the issue) | Allow-list and syntactic and semantic validation at the boundary | Design Choice 2, register row D3 |
| OWASP Cross Site Scripting Prevention Cheat Sheet (linked in the issue) | Contexts of script injection; encoding is the primary defense | `XssDetector`, documentation of limits |
| OWASP SQL Injection Prevention Cheat Sheet | Parameterized queries are the primary defense | `SqlInjectionDetector` is defense in depth |
| OWASP cheat sheets for path traversal, LDAP injection and OS command injection | Canonicalization, filter escaping, avoiding the shell | Detectors and corpora |
| CWE-20, 22, 78, 79, 89, 90 | Input validation, path traversal, OS command, XSS, SQL and LDAP injection | `SecurityRuleId` to CWE mapping |
| ADR-036 | Operation audit through `IOperationAuditStore` | Phase 8 |
| ADR-054 D3, D4 | One meter per package; tenant tag only when known | Register row C6 |
| ADR-062 | Error category contract | Register row C7: no new code |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `Encina.Security.Sanitization` | `src/Encina.Security.Sanitization/` | Complements; the package layout and diagnostics pattern are the model |
| Validation providers and `ValidationPipelineBehavior` | `src/Encina.DataAnnotations/`, `src/Encina.MiniValidator/`, `src/Encina.FluentValidation/`, `src/Encina/Validation/` | The three integration points |
| `IOperationAuditStore`, `AddEncinaAudit` | `src/Encina.Security.Audit/` | Audit adapter |
| `RequestValidatorRunner` and the service provider in the validation context | issue #230 | Attributes read options |
| `EventIdRanges` and `EncinaEventIdAllocationTests` | `src/Encina/Diagnostics/`, `tests/Encina.UnitTests/Testing/Architecture/` | EventId registration |
| `ValidationBenchmarks` | `tests/Encina.BenchmarkTests/Encina.Benchmarks/ValidationBenchmarks.cs` | Benchmark style |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Validation.Security` (`ValidationSecurity`, new, inside the block 550-599 assigned to the validation-1 family) | 590-599 | 590-592 used (three messages); 593-599 free; #230 owns 550-559, #227 560-569, #231 580-589 |

### Estimated File Count

Every count includes tests, corpora, coverage manifest edits, PublicAPI files, test project references, READMEs and the changelog fragment. No pull request uses a type created by a later one.

| PR | Content | New src | Changed src | New tests | Edited tests | Manifest | Other | Files | Lines (est.) |
|----|---------|---------|-------------|-----------|--------------|----------|-------|-------|--------------|
| S1 | Package skeleton, vocabulary, options | 9 (csproj, `SecurityStrictness`, `SecurityRuleId`, `SecurityViolation`, `ISecurityDetector`, options, options validator, PublicAPI Shipped and Unshipped) | 0 | 2 (options and validator unit, guard) | 2 (unit and guard test csproj) | 1 | 1 (`Encina.slnx`) | 15 | ~700 |
| S2 | Normalizer, XSS, path traversal | 3 | 1 (PublicAPI) | 9 (normalizer unit, normalizer property, XSS unit, path unit, guard, 4 corpus files) | 0 | 1 | 0 | 14 | ~800 |
| S3 | SQL, LDAP, command detectors | 3 | 1 (PublicAPI) | 10 (3 detector unit, guard, 6 corpus files) | 0 | 1 | 0 | 15 | ~850 |
| S4 | Attributes | 8 | 1 (PublicAPI) | 4 (DataAnnotations unit, MiniValidator unit, guard, contract) | 0 | 1 | 0 | 14 | ~700 |
| S5 | Registration, collector, sink port, audit behavior | 5 | 1 (PublicAPI) | 5 (DI, behavior, collector, 2 guard) | 0 | 1 | 0 | 12 | ~850 |
| S6 | FluentValidation satellite | 5 (csproj, extensions, property validator, PublicAPI Shipped and Unshipped) | 1 (`FluentValidationProvider.cs`) | 3 (extensions unit, guard, equivalence contract) | 3 (`FluentValidationProviderTests.cs`, 2 test csproj) | 2 (satellite, `Encina.FluentValidation.json`) | 1 (`Encina.slnx`) | 15 | ~800 |
| S7 | Observability, logging, EventIds | 2 (`SecurityValidationDiagnostics`, `SecurityValidationLog`) | 5 (`EventIdRanges.cs`, attribute base, behavior, satellite validator, PublicAPI of `Encina`) | 2 (metrics, log) | 1 (`EncinaEventIdAllocationTests`) | 3 (both validation security manifests, `Encina.json`) | 0 | 13 | ~650 |
| S8 | Audit adapter | 1 (`OperationAuditSecurityViolationSink`) | 3 (`Encina.Security.Audit.csproj`, registration, PublicAPI) | 3 (unit, DI, guard) | 0 | 1 | 0 | 8 | ~450 |
| S9 | Benchmark, docs, ADR, changelog | 0 | 0 | 2 (benchmark, load `.md`) | 0 | 0 | 7 (2 package READMEs, feature page, ADR, ADR index, `docs/INVENTORY.md`, changelog fragment) | 9 | ~600 |
| **Total** | | **36** | **13** | **40** | **6** | **11** | **9** | **115** | **~6,400** |

The columns sum to the total (36 + 13 + 40 + 6 + 11 + 9 = 115) and every statement of the total in this plan is 115 files; the rows sum to the same (15 + 14 + 15 + 14 + 12 + 15 + 13 + 8 + 9). The corpus files count as test files. The largest pull requests are 15 files (S1, S3, S6); none exceeds 1,000 lines. S4 needs #230 S4; S1 to S3 do not.

Per-file coverage targets (per flag, with a one-sentence justification), written into the package manifests by the pull request that adds the file:

| File | Unit | Guard | Property | Contract | Justification |
|------|------|-------|----------|----------|---------------|
| `Detectors/*.cs` | 90 | 85 | 50 | 0 | Payload and benign corpora drive every pattern and strictness branch; the guard tests reach the null and bound checks; the property tests assert no throw and linear bounds |
| `SecurityInputNormalizer.cs` | 95 | 0 | 80 | 0 | Internal pure function with no argument validation beyond the length bound; the property test covers idempotence and bounds |
| `Attributes/*.cs` | 90 | 85 | 0 | 80 | Each attribute is run through the real DataAnnotations and MiniValidator; the contract test pins the shared shape |
| `SecurityValidationOptions.cs`, `...OptionsValidator.cs` | 95 | 0 | 0 | 0 | Plain data and rules with no parameters worth guarding |
| `SecurityValidationServiceCollectionExtensions.cs` | 90 | 90 | 0 | 0 | Both registration orders and the sink check are tested; the null checks are guard-tested |
| `SecurityViolationAuditPipelineBehavior.cs`, `SecurityViolationCollector.cs` | 90 | 85 | 0 | 0 | The sink success, the sink `Left` and no violation are unit cases; the guard tests cover the arguments |
| `SecurityRuleBuilderExtensions.cs`, `SecurityPropertyValidator.cs` | 90 | 85 | 0 | 80 | A real `AbstractValidator` is validated per rule; the equivalence contract compares the three providers |
| `Diagnostics/*.cs` | 90 | 0 | 0 | 0 | `MeterListener` and log capture tests cover them; internal static types have no guard surface |
| `OperationAuditSecurityViolationSink.cs` | 90 | 85 | 0 | 0 | Entry content and the store `Left` are unit cases; constructor and method arguments are guard-tested |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #232 - OWASP-oriented security validation for the three Encina validation providers.

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library; pre-1.0, no backward compatibility, best solution always.
- Validation follows the orchestrator pattern with three providers (AGENTS.md sections 3 and 5); security gates fail
  closed; neither errors, logs, tags nor audit entries ever contain the validated input.
- The maintainer's answers to Design Choices 1-4 of docs/plans/security-focused-validation-implementation-plan-232.md
  decide the package layout, the detection strategy, the audit route and how attributes get their options.

IMPLEMENTATION OVERVIEW:
1. Encina.Validation.Security: vocabulary, options, bounded normalizer, five detectors on NonBacktracking patterns.
2. DataAnnotations attributes (DataAnnotations and MiniValidator) and the FluentValidation satellite package.
3. AddEncinaSecurityValidation, the scoped collector, the sink port and the awaiting audit behavior.
4. Counter and activity event, EventIds 590-592 in ValidationSecurity (590-599), registered first.
5. The Encina.Security.Audit adapter; benchmark, docs, ADR and changelog.
Nine pull requests (S1 to S9), see the Estimated File Count table.

KEY PATTERNS:
- Corpora pair every payload with a benign near-miss; the three providers must agree (contract test).
- DI test with ValidateOnBuild and ValidateScopes in both registration orders; zero warnings.

REFERENCE FILES:
- src/Encina.Security.Sanitization/, src/Encina.Security.Audit/, src/Encina.FluentValidation/
- docs/plans/security-focused-validation-implementation-plan-232.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | Validation is per request and stateless |
| 2 | OpenTelemetry | ✅ Phase 7 | Counter `encina.validation.security.violations` and an event on `Activity.Current`; tenant tag only when known (ADR-054 D4, register row C6); the central validation duration is #726 |
| 3 | Structured Logging | ✅ Phase 7 | `SecurityValidationLog` EventIds 590-592 in the new range 590-599; rule id and CWE only, never the input |
| 4 | Health Checks | ❌ N/A | Stateless detectors with no dependency |
| 5 | Validation | ✅ All phases | This is validation; the three providers of `AGENTS.md` section 5 are covered (attributes for two, the satellite for one) and pinned by a contract test |
| 6 | Resilience | ❌ N/A | In-process, bounded linear-time matching; no external call |
| 7 | Distributed Locks | ❌ N/A | No shared state |
| 8 | Transactions | ❌ N/A | Validation precedes the handler; the audit write is its own store call, awaited in the request |
| 9 | Idempotency | ❌ N/A | Stateless and repeatable |
| 10 | Multi-Tenancy | ⏭️ | The tenant id is tagged and audited when known (Phase 7 and 8); tenant-specific strictness is a follow-up shared with the family (draft: `artifacts/issues/validation-tenant-module-scoped-selection.md`) |
| 11 | Module Isolation | ⏭️ | Module-scoped rules: the same follow-up issue |
| 12 | Audit Trail | ✅ Phases 5 and 8 | Violations are audited through `IOperationAuditStore` (ADR-036) by the adapter; opt-in by `AuditViolations`, a missing sink fails start-up |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| `IOperationAuditStore` and `AddEncinaAudit` | ✅ On `main` | Phase 8 |
| Providers pass their service provider to the validation context | Plan #230, S4 | Blocking for S4 of this plan (Design Choice 4 option A) |
| ADR-062 error category contract | ✅ Merged | Register row C7 |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| Code-less validation `Left` never becomes a 400 | #3415 | The `Left` that carries the violations is #3415's |
| Which error text a client may see | #3426 | The violation message is generic by design |
| Central validation instrumentation | #726 | Covers duration; this plan adds the security counter only |

---

## Next Steps

1. Link this plan from #232 and ask the maintainer to answer Design Choices 1 to 4 and the Dilemma rows C6 and D1 to D5 of the register when work starts.
2. Open the follow-up issue when the first pull request opens: tenant and module scoped selection and strictness (draft: `artifacts/issues/validation-tenant-module-scoped-selection.md`).
3. Implement S1 to S9 as the pull requests of the Estimated File Count split, `Fixes #232` on S9.
