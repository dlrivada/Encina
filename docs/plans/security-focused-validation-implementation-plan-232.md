# Implementation Plan: Security-Focused Validation (`Encina.Validation.Security`)

> **Issue**: [#232](https://github.com/dlrivada/Encina/issues/232)
> **Type**: Feature
> **Related**: #230 (owns the runner and the decisions on the provider lifetime and on the service provider seen by attributes; a prerequisite of S7 and S8), #227 and #228 (siblings of the family), #234 and #236 (planned apart), #726 (central validation instrumentation), #3415 (a code-less validation `Left` never becomes a 400), #3426 (which error text a client may see), ADR-036, ADR-054, ADR-062 (merged: error category contract)
> **Complexity**: Medium-high (three new packages, seven detectors and attributes, no persistence)
> **Estimated Scope**: ~2,000 lines of production code + ~4,300 lines of tests and corpora + ~1,200 lines of manifests, PublicAPI files and documentation (~7,600 changed lines in total), in twelve pull requests of at most 15 files and 1,000 lines each

---

## Summary

### What exists today (read on 2026-11-16, main `17761fc1f`)

- No security validation exists in the validation providers. `Encina.Security.Sanitization` sanitizes (changes) input: `[SanitizeSql]`, `[SanitizeHtml]`, `[StripHtml]`, an input and an output pipeline behavior (`InputSanitizationPipelineBehavior`, `OutputEncodingPipelineBehavior`), `SqlSanitizer`, `ShellSanitizer`, `XmlSanitizer`, `JsonSanitizer` (all `internal`), the `HtmlSanitizer` package, EventIds 1-99 (`Sanitization = (1, 99)`) and its own `SanitizationDiagnostics` (`src/Encina.Security.Sanitization/`). It rejects nothing and reports no CWE. The issue's alternatives reject "sanitization instead of validation" as lossy.
- The three validation providers accept DataAnnotations `ValidationAttribute` subclasses (DataAnnotations and MiniValidator, by reflection) and FluentValidation rules. `DataAnnotationsValidationProvider` builds `new ValidationContext(request, serviceProvider: null, items: null)` (`DataAnnotationsValidationProvider.cs:31`), so an attribute cannot read options from the container today; #230 (its register row D3) passes the service provider. `AddDataAnnotationsValidation` and `AddMiniValidation` register their provider and orchestrator as singletons (`TryAddSingleton`, `src/Encina.DataAnnotations/ServiceCollectionExtensions.cs:72`, `src/Encina.MiniValidator/ServiceCollectionExtensions.cs:65`), while `AddEncinaFluentValidation` registers them scoped; #230 keeps this lifetime as a Dilemma (its Design Choice 2). All three behaviors are registered with `TryAddEnumerable(... typeof(ValidationPipelineBehavior<,>))`, which appends. FluentValidation validators come from DI, and `FluentValidationProvider` copies `CorrelationId`, `UserId` and `TenantId` into `RootContextData` (`FluentValidationProvider.cs:52-63`); `MiniValidationProvider` ignores the context (`MiniValidationProvider.cs:31`) and MiniValidation 0.9.2 offers `TryValidate(target, IServiceProvider, out errors)`.
- The pipeline has an applied stage contract: `PipelineStageAttribute` and `PipelineOrderResolver` (`src/Encina/Core/Encina.cs:179` resolves the order per closed pipeline), with `PipelineStage.Audit = 300` outside `Validation = 900` and `Application = 1500` (the stage of `ValidationPipelineBehavior<,>`, which declares none). `IEncinaPipelineInspector` reports the resolved order.
- `Encina.Security.Audit` has `IOperationAuditStore.RecordAsync(OperationAuditEntry, ct)` returning `ValueTask<Either<EncinaError, Unit>>` (`IOperationAuditStore.cs:50`); `OperationAuditEntry` requires `Id`, `CorrelationId`, `Action`, `EntityType`, `Outcome`, `TimestampUtc` (`:147`), `StartedAtUtc` and `CompletedAtUtc`, and carries optional `UserId`, `TenantId`, `IpAddress`, `UserAgent`, `RequestPayload` and a `Metadata` dictionary; `AuditOutcome` has `Denied`. The package references `Encina`, `Encina.DomainModeling` and `Encina.Messaging` (`Encina.Security.Audit.csproj`), which a validation package should not pull in. `OperationAuditOptions.IncludeRequestPayload` (`:139`) stores the request payload when an application opts in.
- Not in `src`: `NoHtmlTags`, `NoScriptInjection`, `NoSqlInjection`, `SafeFileName`, `NoPathTraversal`, `NoLdapInjection`, `NoCommandInjection`, `AddEncinaSecurityValidation`, `SecurityValidationOptions`, any package named `Encina.Validation.Security`, any EventId range for validation security.

### What this plan delivers

An opt-in package `Encina.Validation.Security` with seven rules backed by one detection engine (input normalization, then bounded non-backtracking patterns, three strictness levels), exposed as DataAnnotations attributes (so they work under `Encina.DataAnnotations` and `Encina.MiniValidator`) and, in a satellite package `Encina.Validation.Security.FluentValidation`, as FluentValidation rule builder extensions with the same names the issue shows. A violation is a `ValidationError` that never contains the input. The attributes write each violation into a request-scoped collector; an audit behavior, declared in the `Audit` pipeline stage so that it runs outside validation in any registration order, counts and logs the violations (tenant tag when the tenant is known) and, with `AuditViolations = true`, awaits an operation audit entry through the `ISecurityViolationSink` port, whose adapter lives in a bridge package `Encina.Validation.Security.Audit`. The rules are heuristics and defense in depth, not a replacement for parameterized queries, output encoding or path canonicalization; the documentation says so.

- Affected packages: `Encina.Validation.Security` (new), `Encina.Validation.Security.FluentValidation` (new), `Encina.Validation.Security.Audit` (new, the bridge, subject to Design Choice 6), `Encina.FluentValidation` (one `RootContextData` entry), `Encina` (EventIds only).
- Provider category: Validation (3): attributes serve DataAnnotations and MiniValidator, the satellite serves FluentValidation, and a contract test pins that the same payload gives the same verdict through all three.
- Shared pieces owned here: `ISecurityViolationCollector`, `ISecurityViolationSink`, `ISecurityDetector` and the strictness levels. Sibling plans refer to nothing here.

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
<summary><strong>3. How a violation reaches the audit store, and where the audit behavior runs</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A request-scoped `ISecurityViolationCollector` the attributes write into, and an audit behavior in the `Audit` pipeline stage (outside validation, whatever the registration order) that drains it after the next step and awaits `ISecurityViolationSink.RecordAsync` for each violation** | Async, awaited, same request; no background loop; works for sync attributes; a failed validation is still audited because the behavior runs outside it | Depends on Design Choice 7 (the collector must be the one the attribute sees) |
| **B) A bounded channel and a hosted writer** | Rules need no scope | A background loop; a full channel or a crash loses records; the failure rules of AGENTS.md section 3 for background work apply |
| **C) Synchronous sink called from the attribute** | Simple | Sync-over-async on a database call is forbidden (AGENTS.md section 3) |

### Chosen Option: **A — Collector plus an awaiting behavior** (recommended, pending the maintainer)

### Rationale

- Only A awaits the audit write inside the request and can fail the start-up when the audit is enabled and no sink exists. B wins when the validation runs outside any request scope (a batch importer).
- Ordering (register row D4): the behavior carries `[PipelineStage(PipelineStage.Audit)]`, so `PipelineOrderResolver` places it outside `ValidationPipelineBehavior<,>` (`Application`) in both registration orders; `TryAddEnumerable` appending the validation behavior after it does not matter.
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

<details>
<summary><strong>6. Where the audit adapter lives</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Inside `Encina.Security.Audit`** (a project reference to `Encina.Validation.Security`) | No extra package | Every audit user pulls `Encina.Validation.Security` transitively, the pattern Design Choice 1 rejects for FluentValidation (AGENTS.md section 1, pay-for-what-you-use) |
| **B) A bridge package `Encina.Validation.Security.Audit`** referencing both packages | Pay-for-what-you-use; ADR-036 is still honored (the entry goes through `IOperationAuditStore`) | A third package, README and manifest |

### Chosen Option: **B — A bridge package** (recommended, pending the maintainer)

### Rationale

- B keeps both feature packages free of each other and matches Design Choice 1. A wins when the maintainer wants one fewer package and accepts the transitive reference. The file counts below are for B; with A, S11 becomes 8 files (the adapter, the `Encina.Security.Audit.csproj` reference, its registration, PublicAPI, three tests, the manifest).

**Class:** Dilemma

</details>

<details>
<summary><strong>7. How the collector reaches the attribute (the provider lifetime)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A scoped collector resolved through `ValidationContext.GetService`; blocked on #230 Design Choice 2 option A (scoped provider and orchestrator)** | One request, one collector; the attribute and the behavior see the same instance | With #230 option B (providers stay singleton, a separate scope per validation) the attribute resolves from the root provider: with `ValidateScopes` that throws, without it one collector is shared by all requests and violations are audited under another request's user |
| **B) The providers place the collector in `ValidationContext.Items` (and the Fluent `RootContextData`) per call** | Independent of the provider lifetime | Three provider edits that belong to #230's area; MiniValidator exposes no items hook |
| **C) No collector: the audit behavior derives the violations from the `ValidationError` entries of the validation `Left`** | Independent of lifetimes; no shared state | Couples the audit to the error text (a stable rule marker in the message) and to #3415's `Left` shape |

### Chosen Option: **A — Scoped collector; S7 and S8 wait for #230 option A** (recommended, pending the maintainer)

### Rationale

- A is the only option that needs no provider change beyond #230 row D3, and #230's option A (the lifetime `AddEncinaFluentValidation` already uses) is its recommendation. If the maintainer picks #230 option B, this plan switches to B or C before S7 starts. A DI test with `ValidateOnBuild` and `ValidateScopes` resolves the provider and the collector from a scope and shows both attributes and behavior see one instance; a second test resolves from the root and shows the gap is counted (register row C13).

**Class:** Dilemma

</details>

### Register of defaults and semantics

| Id | Choice | Class | Basis |
|----|--------|-------|-------|
| C1 | A violation denies the request; no mode reports success on a detected pattern | Settled | AGENTS.md section 3 (security gates fail closed) |
| C2 | Neither an error, a log, a tag nor an audit entry contains the input; they carry the rule id, the CWE id, the property name and the request type | Settled | AGENTS.md section 3 (`EncinaError.Message` and plaintext rules); SPEC-002 DEC-006 |
| C3 | A null or empty value is valid (absence is `[Required]`'s concern, as the built-in DataAnnotations attributes treat null); a value longer than `MaxInputLength` is a violation, never scanned (bounded cost, fail closed) | Settled | the DataAnnotations convention for null (`System.ComponentModel.DataAnnotations.ValidationAttribute`); AGENTS.md section 3 for the length bound |
| C4 | Patterns use `RegexOptions.NonBacktracking` with `CultureInvariant`, so detection time is linear; NonBacktracking supports no backreferences or lookarounds, so the SQL tautologies (`'1'='1'`) are detected as operand-comparison-operand shapes, not by comparing the two operands. Option B: hand-written span scanners with no regex. Option C: backtracking patterns with a match timeout mapped to a violation | Dilemma (recommended, pending the maintainer): NonBacktracking | the three options all keep the gate from being a denial-of-service vector (AGENTS.md section 3); NonBacktracking wins on code size, scanners win when the maintainer wants no regex engine at all |
| C5 | The audit entry is written through `IOperationAuditStore` (ADR-036) by an adapter; where the adapter lives is Design Choice 6 | Dilemma (recommended, pending the maintainer): bridge package | Design Choice 6 |
| C6 | Telemetry: counter `encina.validation.security.violations` with tags `rule` and `strictness`, plus an event on `Activity.Current`, both emitted by the audit behavior, which holds `IRequestContext` and so adds the tenant tag when `TenantId` is known (ADR-054 D4), for all three providers (no provider hook is needed, so MiniValidator's missing items hook does not matter); no `ActivitySource` of its own. The plan builds no tenant-qualified id, so the reserved `default` segment does not apply | Dilemma (recommended, pending the maintainer) | ADR-054 D3 asks one source per package; the alternative is a source with a span per call |
| C7 | No new `EncinaError` code and no new `ErrorCategory`: a violation is a `ValidationError` entry of the existing validation `Left`; the `Left` itself is #3415's | Settled | ADR-062 (categories apply to `EncinaErrors.Create` calls; none is added) |
| C8 | EventIds 590-592 packed from the start of the family block: 590 violation detected (warning), 591 violation audit failed (error), 592 input over the length limit (warning); the log class and the range are created in S5, the first pull request that logs | Trivial | AGENTS.md section 7; ADR-021; `artifacts/decisions/plan-batch/eventid-blocks.md` |
| C9 | A normalizer or detector exception (for example `ArgumentException` from `Normalize` on a lone surrogate) is a violation `MalformedInput` (CWE-20), never a pass and never an unhandled exception | Settled | AGENTS.md section 3 (gates fail closed; errors never swallowed): a pass or a throw would not comply |
| C10 | When a third decoding pass would still change the value after two percent passes and two entity passes (triple encoding), the value is a violation `MalformedInput` at every strictness level | Settled | AGENTS.md section 3 (fail closed when the check cannot complete) |
| C11 | `MaxInputLength` is checked twice: on the raw length before any work, and on the normalized length after NFKC (which expands up to 18 times, U+FDFA); either over the limit is `InputTooLong` | Settled | AGENTS.md section 3 (bounded cost, fail closed): checking only the raw or only the normalized length leaves an unbounded step |
| C12 | Which form each rule inspects: the five detectors scan the normalized value; `SafeFileName` is an allow-list and judges the raw value (a fullwidth name normalizes to ASCII but is stored raw). Option B: every detector scans both forms (two bounded passes) | Dilemma (recommended, pending the maintainer): normalized for detectors, raw for `SafeFileName` | both options reject the same corpus; B wins when the maintainer prefers fewer bypasses over one pass of cost |
| C13 | A violation found while no collector can be resolved (a singleton provider, no service provider) still denies the request (C1); the audit gap is counted (`encina.validation.security.audit_gaps`, a static counter that needs no container) and noted as an event on `Activity.Current`, never silently skipped. Option B: the attribute throws | Dilemma (recommended, pending the maintainer): count the gap | throwing breaks ROP (ADR-001) and still hides the cause from the audit; B wins when the maintainer wants a hard failure for a mis-wired provider |
| C14 | The audit entry carries `Id`, `CorrelationId`, `UserId` (pseudonymous personal data; ADR-036 entries already carry `UserId`), `TenantId`, action, `EntityType` = the request type name, outcome `Denied`, `TimestampUtc`, `StartedAtUtc` and `CompletedAtUtc` from an injected `TimeProvider` (AGENTS.md section 3), and `Metadata` with rule id, CWE id and property name; `RequestPayload`, `RequestPayloadHash`, `IpAddress`, `UserAgent` and `ErrorMessage` stay null | Settled | ADR-036 (`OperationAuditEntry.cs:77`, `:147`, `:185`, `:194`, `:223`); AGENTS.md section 3 (`TimeProvider`; `EncinaError.Message` never stored) |
| C15 | The audit action name is `validation.security_violation` | Dilemma (recommended, pending the maintainer) | public name stored in audit data; alternative `security.validation.violation` follows the dotted-noun style of other audit actions |
| C16 | An application that sets `OperationAuditOptions.IncludeRequestPayload` stores the hostile payload of a denied request through the existing operation audit behavior; this plan documents the interaction in the feature page and does not change that behavior. Option B: the operation audit behavior omits the payload when the request failed a security rule | Dilemma (recommended, pending the maintainer): document only | the option belongs to a package this plan does not change; B wins when the maintainer wants payload capture off for rejected requests |
| C17 | Nested `Send` calls share the scope and the collector, so the audit behavior drains the collector after its next step returns; an outer audit behavior then sees none and no violation is audited twice | Dilemma (recommended, pending the maintainer) | semantics of the shared scope; the alternative is one collector per behavior invocation (a stack), which costs an allocation per request |
| C18 | The public key `RootContextData["Encina.ServiceProvider"]` carries the service provider into Fluent rules (set by `FluentValidationProvider`, read by the satellite). Option B: a typed accessor on `FluentValidationProvider` instead of a string key | Dilemma (recommended, pending the maintainer): the string key | the existing context entries are string keys (`FluentValidationProvider.cs:52-63`) |
| C19 | The violation message text is generic and fixed: "The value of '{PropertyName}' is not allowed." | Trivial | wording only; it carries no input (C2) |
| C20 | EventId 590 is a warning per violation, so the log volume follows the attacker; the counter is the volume signal and the documentation says so. Option B: log once per request with the violation count | Dilemma (recommended, pending the maintainer): one warning per violation | per-violation rows give the rule and property; B wins when log cost under attack matters more than detail |
| D1 | Strictness levels `Lenient`, `Standard`, `Strict`, default `Standard`, an undefined enum value treated as `Strict`, `MaxInputLength` 4096 characters, `AuditViolations` default `false` | Dilemma (recommended, pending the maintainer) | defaults; the issue's `StrictMode` is the `Strict` level |
| D2 | `AllowSafeHtml` with `AllowedTags` permits the listed tags with no attributes; any attribute, event handler, `javascript:` or `data:` URI in an allowed tag is still a violation | Dilemma (recommended, pending the maintainer) | semantics of the issue's `AllowedTags` example |
| D3 | `SafeFileName` is an allow-list (letters, digits, `.`, `-`, `_`, one extension, no reserved Windows device names, at most 255 characters); `NoPathTraversal` is a detector (`..`, encoded and doubly encoded separators, absolute roots, UNC and null bytes) | Dilemma (recommended, pending the maintainer) | rule scope |
| D4 | The audit behavior is always registered by `AddEncinaSecurityValidation` (it counts and logs), declares `[PipelineStage(PipelineStage.Audit)]` and calls the sink only with `AuditViolations = true`. Option B: the audit is done inside `ValidationPipelineBehavior` through an optional sink (a core change). Option C: a post-configure step that reorders the descriptors | Dilemma (recommended, pending the maintainer): the stage attribute | `PipelineOrderResolver` orders by stage in any registration order (Design Choice 3); B and C touch the core or depend on registration order |
| D5 | Payload corpora under test data are curated from the public OWASP cheat sheets linked in the issue and from CWE examples; every payload is paired with a benign near-miss that must pass (false-positive guard). The corpora must contain the bypass classes: overlong UTF-8 (`%c0%ae`), IIS `%u` encoding, entities without a semicolon or with leading zeros, mixed-order encodings (an entity inside percent encoding), triple encoding, `\` and mixed separators, case variants, null bytes, and homoglyphs that NFKC does not fold | Dilemma (recommended, pending the maintainer) | test scope |

---

## Implementation Phases

Twelve phases, one pull request each; the log class, the EventIds and the collector are created in the first pull request that uses them, and no pull request uses a type of a later one.

### Phase 1 (S1): Package Skeleton, Vocabulary and Options

> **Goal**: The new package with its public vocabulary and validated options.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.Validation.Security/Encina.Validation.Security.csproj` (references `Encina`; `Microsoft.Extensions.Options`, `Microsoft.Extensions.DependencyInjection.Abstractions`, `Microsoft.Extensions.Logging.Abstractions`; `PublicApiAnalyzers`), `PublicAPI.Shipped.txt`, `PublicAPI.Unshipped.txt`; add it to `Encina.slnx`.
2. **Create** `SecurityStrictness.cs` (`Lenient`, `Standard`, `Strict`), `SecurityRuleId.cs` (`Xss`, `SqlInjection`, `PathTraversal`, `LdapInjection`, `CommandInjection`, `UnsafeFileName`, `InputTooLong`, `MalformedInput`, each mapped to a CWE id: 79, 89, 22, 90, 78, 22, 20, 20), `SecurityViolation.cs` (record: rule, CWE, property name, request type; no value) and `SecurityValidationOptions.cs` (`Strictness`, `MaxInputLength`, `AuditViolations`, `XssProtection.AllowSafeHtml` and `AllowedTags`, `SqlInjection.StrictMode`) with `SecurityValidationOptionsValidator.cs : IValidateOptions<...>` (length above 0, tags are plain element names).
3. **Modify** `tests/Encina.UnitTests/Encina.UnitTests.csproj` and `tests/Encina.GuardTests/Encina.GuardTests.csproj` (one `ProjectReference` each); **create** `.github/coverage-manifest/Encina.Validation.Security.json` (through `mechanical-fixer`).
4. Tests (S1): options and validator unit tests, `SecurityValidationGuardTests.cs` (the guard file extended by later pull requests).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
Encina (.NET 10, C# 14, pre-1.0, no backward compatibility). Issue #232 adds a rejecting security validation package.
Read docs/plans/security-focused-validation-implementation-plan-232.md, Design Choices 1, 2, 4 and register rows C1-C4, C9-C11, D1.

TASK:
1. Create the Encina.Validation.Security project, add it to Encina.slnx.
2. Create the vocabulary (strictness, rule ids with CWE, SecurityViolation without a value) and the options with their validator.

KEY RULES:
- A violation never carries the input; options validated at start-up; XML docs on every public member; no [Obsolete].
- PublicAPI, slnx and manifest edits through mechanical-fixer where they are not source code.

REFERENCE FILES:
- src/Encina.Security.Sanitization/ (package layout, options), src/Encina.Security.ABAC/Encina.Security.ABAC.csproj
```

</details>

---

### Phase 2 (S2): Normalizer and XSS Detector

> **Goal**: The shared normalization, `ISecurityDetector` and the first detector.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `ISecurityDetector.cs` (`bool TryDetect(ReadOnlySpan<char> input, SecurityStrictness strictness, out SecurityRuleId rule)`), `SecurityInputNormalizer.cs` (internal; the length checks of C11, NFKC, percent-decoding for at most two passes, HTML-entity decoding for at most two passes; a lone surrogate, any exception or a third pass that would still change the value returns `MalformedInput`, rows C9 and C10) and `Detectors/XssDetector.cs` (tags, script contexts, event handlers, `javascript:` and `data:` URIs; honors `AllowSafeHtml` and `AllowedTags`, row D2).
2. **Modify** `PublicAPI.Unshipped.txt`, `tests/Encina.UnitTests/Encina.UnitTests.csproj` (a `Content` glob copying `Security\Validation\TestData\*.txt`), `tests/Encina.PropertyTests/Encina.PropertyTests.csproj` (one `ProjectReference`) and the manifest (through `mechanical-fixer`).
3. Tests (S2): normalizer unit (lone surrogate, triple encoding, expansion, both length checks) and property (never throws, output length bounded), XSS unit tests over `TestData/xss-payloads.txt` and `xss-benign.txt` (row D5), `SecurityDetectorsProperties.cs` (property: a detector never throws and is deterministic for any string, extended by S3 and S4), the guard file extended.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
Issue #232, plan docs/plans/security-focused-validation-implementation-plan-232.md, phase 2 of 12. Depends on S1.

TASK:
1. Write the bounded normalizer, ISecurityDetector and the XSS detector (NonBacktracking patterns).
2. Write the corpora and the unit, property and guard tests.

KEY RULES:
- Linear-time matching only; nothing logs or returns the input; every payload has a benign near-miss that must pass;
  fail closed on a normalizer exception, residual encoding and over-length (rows C9-C11).

REFERENCE FILES:
- src/Encina.Validation.Security/ (phase 1), the OWASP XSS and Input Validation cheat sheets linked in issue #232
```

</details>

---

### Phase 3 (S3): Path Traversal, SQL Injection and the Collector

> **Goal**: Two detectors and the request-scoped collector.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `Detectors/PathTraversalDetector.cs` (row D3) and `Detectors/SqlInjectionDetector.cs` (tautologies as operand-comparison-operand shapes, stacked statements, comment terminators, `UNION SELECT`, time-delay functions; `SqlInjection.StrictMode` widens the keyword set), `ISecurityViolationCollector.cs` and `SecurityViolationCollector.cs` (scoped, thread-safe list of `SecurityViolation` with a drain method, row C17).
2. **Modify** `PublicAPI.Unshipped.txt` and the manifest (through `mechanical-fixer`).
3. Tests (S3): path and SQL unit tests over `path-*` and `sql-*` payload and benign files, a collector unit test, the property and guard files extended.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
Issue #232, plan docs/plans/security-focused-validation-implementation-plan-232.md, phase 3 of 12. Depends on S2.

TASK:
1. Write the path traversal and SQL injection detectors on the shared normalizer; write the collector.
2. Write the corpora (payload and benign near-miss) and the unit, property and guard tests.

KEY RULES:
- Heuristics: document false-positive and false-negative behavior per strictness level in the XML docs.
- Nothing logs or returns the input; the collector is request-scoped state and is never a singleton.

REFERENCE FILES:
- src/Encina.Validation.Security/Detectors/ (phase 2), the OWASP cheat sheets linked in issue #232
```

</details>

---

### Phase 4 (S4): LDAP, Command Injection and File Name Detectors

> **Goal**: The remaining detectors.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `Detectors/LdapInjectionDetector.cs` (filter metacharacters `* ( ) \ NUL` in unescaped positions, `)(` filter breaks), `Detectors/CommandInjectionDetector.cs` (shell metacharacters, command substitution, redirection, newline separators, Windows `&`, `|`, `%`) and `Detectors/SafeFileNameDetector.cs` (the allow-list of row D3, judging the raw value, row C12).
2. **Modify** `PublicAPI.Unshipped.txt` and the manifest (through `mechanical-fixer`).
3. Tests (S4): LDAP and command unit tests over `ldap-*` and `command-*` payload and benign files, a file-name unit test with inline data, the property and guard files extended.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT:
Issue #232, plan docs/plans/security-focused-validation-implementation-plan-232.md, phase 4 of 12. Depends on S2.

TASK:
1. Write the LDAP, command injection and safe file name detectors.
2. Write the corpora and the unit, property and guard tests.

KEY RULES:
- SafeFileName judges the raw value; the others the normalized one. Nothing logs or returns the input.

REFERENCE FILES:
- src/Encina.Validation.Security/Detectors/ (phases 2 and 3)
```

</details>

---

### Phase 5 (S5): Audit Behavior, Sink Port, Logging and EventIds

> **Goal**: The audit behavior, the sink port and the first pull request that logs (creates the log class and registers the range).

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `src/Encina/Diagnostics/EventIdRanges.cs`: register `ValidationSecurity = (590, 599)`; **create** `ISecurityViolationSink.cs` (`ValueTask<Either<EncinaError, Unit>> RecordAsync(SecurityViolation violation, IRequestContext context, CancellationToken ct)`), `Diagnostics/SecurityValidationDiagnostics.cs` (meter `Encina.Validation.Security`, the violations counter of row C6, the static `audit_gaps` counter of row C13, an event on `Activity.Current`), `Diagnostics/SecurityValidationLog.cs` (`[LoggerMessage]`: 590 violation detected with rule id, CWE, property name and request type; 591 audit failed with the error code; 592 input over the limit) and `SecurityViolationAuditPipelineBehavior.cs` (`[PipelineStage(PipelineStage.Audit)]`; calls the next step, drains the collector, counts and logs each violation with the tenant tag when known, and with `AuditViolations = true` awaits the sink; a `Left` from the sink is logged by code and never turns the denied request into a success).
2. **Modify** the `PublicAPI.Unshipped.txt` of `Encina` and of the package, `EncinaEventIdAllocationTests.cs` and both manifests (`Encina.Validation.Security.json`, `Encina.json`; through `mechanical-fixer`).
3. Tests (S5): behavior unit (success, sink `Left`, no violation, gap), `MeterListener` test (tags, tenant tag only when known), a log test (ids, ranges, the input absent), the guard file extended.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT:
Issue #232, plan docs/plans/security-focused-validation-implementation-plan-232.md, phase 5 of 12. ADR-021, ADR-036, ADR-054.
AGENTS.md section 3: errors never swallowed, TimeProvider, never a scoped service in a singleton.

TASK:
1. Register ValidationSecurity (590, 599) in EventIdRanges.cs first; write the log class, the diagnostics, the sink port and the audit behavior.

KEY RULES:
- Pack ids sequentially; tenant tag only when the tenant is known; the input never appears in a tag, event or log.
- The audit write is awaited inside the request; codes and rule ids only.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs, src/Encina.Security.Sanitization/Diagnostics/SanitizationDiagnostics.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs, src/Encina/Pipeline/Ordering/PipelineStageAttribute.cs
```

</details>

---

### Phase 6 (S6): Registration

> **Goal**: `AddEncinaSecurityValidation` and its start-up checks.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `SecurityValidationServiceCollectionExtensions.cs` (`AddEncinaSecurityValidation(Action<SecurityValidationOptions>? configure = null)`: options with `ValidateOnStart`, detectors as singletons, the collector scoped, the audit behavior registered, `TryAddSingleton(TimeProvider.System)`) and `SecurityValidationSinkCheck.cs` (internal `IValidateOptions`: with `AuditViolations = true` and no `ISecurityViolationSink` registered, start-up fails).
2. **Modify** `PublicAPI.Unshipped.txt` and the manifest (through `mechanical-fixer`).
3. Tests (S6): a DI test with `ValidateOnBuild` and `ValidateScopes` in both registration orders against each validation provider's registration, with and without a sink, asserting through `IEncinaPipelineInspector` that the audit behavior is outside `ValidationPipelineBehavior<,>` in both orders (row D4); the guard file extended.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```text
CONTEXT:
Issue #232, plan docs/plans/security-focused-validation-implementation-plan-232.md, phase 6 of 12. Depends on S1-S5.
AGENTS.md section 3: registration completeness, never a scoped service in a singleton.

TASK:
1. Write AddEncinaSecurityValidation and the sink check.
2. Prove registration completeness and the pipeline order in both registration orders.

KEY RULES:
- A missing sink with AuditViolations = true fails start-up.

REFERENCE FILES:
- src/Encina/Validation/ValidationPipelineBehavior.cs, src/Encina.Security.Sanitization/ServiceCollectionExtensions.cs
```

</details>

---

### Phase 7 (S7): DataAnnotations Attributes, Part 1

> **Goal**: The attribute base and four attributes (needs #230 option A, Design Choice 7).

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `Attributes/SecurityValidationAttribute.cs` (abstract; reads `IOptions<SecurityValidationOptions>` and the collector through `ValidationContext.GetService`, falls back to `Standard`; optional strictness override; writes the `SecurityViolation` into the collector, or counts the gap (row C13); never puts the value in `ErrorMessage`) and `NoHtmlTagsAttribute`, `NoScriptInjectionAttribute`, `NoSqlInjectionAttribute`, `NoPathTraversalAttribute`.
2. **Modify** `PublicAPI.Unshipped.txt`, `tests/Encina.ContractTests/Encina.ContractTests.csproj` (references to `Encina.DataAnnotations`, `Encina.MiniValidator` and `Encina.Validation.Security`) and the manifest (through `mechanical-fixer`).
3. Tests (S7): per attribute through `Validator.TryValidateObject` and `MiniValidator.TryValidate` (two unit files), the DI test of Design Choice 7 (one collector instance; root resolution counts the gap), a guard file extension, and `SecurityValidationContractTests.cs` (all attributes derive from the base, share the message shape and never echo the value; every `ISecurityDetector` returns false for null-like input and never throws; the collector and the sink port keep their contract through a fake).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```text
CONTEXT:
Issue #232, plan docs/plans/security-focused-validation-implementation-plan-232.md, phase 7 of 12. Depends on S1-S6 and on the
plan of issue #230 (scoped providers that pass their service provider to the validation context).

TASK:
1. Write SecurityValidationAttribute and four attributes; options and collector from the container with a Standard fallback.
2. Test each through the real DataAnnotations validator and the real MiniValidator.

KEY RULES:
- Fail closed: a missing service provider gives Standard, never Lenient; the value never appears in a message.

REFERENCE FILES:
- src/Encina.DataAnnotations/DataAnnotationsValidationProvider.cs, src/Encina.MiniValidator/MiniValidationProvider.cs
```

</details>

---

### Phase 8 (S8): DataAnnotations Attributes, Part 2

> **Goal**: `SafeFileNameAttribute`, `NoLdapInjectionAttribute`, `NoCommandInjectionAttribute`.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** the three attributes; **modify** `PublicAPI.Unshipped.txt` and the manifest (through `mechanical-fixer`).
2. Tests (S8): the two attribute unit files and the contract file extended; the guard file extended.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```text
CONTEXT:
Issue #232, plan docs/plans/security-focused-validation-implementation-plan-232.md, phase 8 of 12. Depends on S7.

TASK:
1. Add the three remaining attributes on the shared base and extend the tests.

KEY RULES:
- Same shape as S7; SafeFileName judges the raw value.

REFERENCE FILES:
- src/Encina.Validation.Security/Attributes/ (phase 7)
```

</details>

---

### Phase 9 (S9): Service Provider in the Fluent Context

> **Goal**: Expose the service provider to Fluent rules.

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `src/Encina.FluentValidation/FluentValidationProvider.cs`: add `RootContextData["Encina.ServiceProvider"] = _serviceProvider` beside the existing context entries (`:52-63`, row C18); its `PublicAPI` is unchanged.
2. **Modify** `tests/Encina.UnitTests/FluentValidation/FluentValidationProviderTests.cs` (one added test) and `.github/coverage-manifest/Encina.FluentValidation.json` (through `mechanical-fixer`; the existing targets do not go down).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 9</strong></summary>

```text
CONTEXT:
Issue #232, plan docs/plans/security-focused-validation-implementation-plan-232.md, phase 9 of 12.

TASK:
1. Add the RootContextData entry and its test.

KEY RULES:
- No signature change; keep the manifest targets.

REFERENCE FILES:
- src/Encina.FluentValidation/FluentValidationProvider.cs
```

</details>

---

### Phase 10 (S10): FluentValidation Satellite

> **Goal**: The rule builder extensions of the issue's example.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.Validation.Security.FluentValidation/Encina.Validation.Security.FluentValidation.csproj` (references `Encina.Validation.Security` and FluentValidation), `SecurityRuleBuilderExtensions.cs` (`NoHtmlTags`, `NoScriptInjection`, `NoSqlInjection`, `SafeFileName`, `NoPathTraversal`, `NoLdapInjection`, `NoCommandInjection` on `IRuleBuilder<T, string>`), `SecurityPropertyValidator.cs` (a `PropertyValidator<T, string>` over the detectors; options and collector from `RootContextData`), the two `PublicAPI` files, a slnx entry and a manifest.
2. **Modify** the unit, guard and contract test projects (one `ProjectReference` each; the contract project also gets `Encina.FluentValidation`).
3. Tests (S10): extension unit tests over a real `AbstractValidator`, a guard test, and `tests/Encina.ContractTests/Validation/SecurityValidationEquivalenceContractTests.cs` (the same payload corpus gives the same verdict through the attribute under DataAnnotations and MiniValidator and through the Fluent extension).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 10</strong></summary>

```text
CONTEXT:
Issue #232, plan docs/plans/security-focused-validation-implementation-plan-232.md, phase 10 of 12. Depends on S1-S9.
AGENTS.md section 5: the three validation providers stay consistent.

TASK:
1. Create the satellite package with the seven rule builder extensions.
2. Write the extension tests and the cross-provider equivalence contract test.

KEY RULES:
- The satellite only adapts: the detectors, options and collector come from Encina.Validation.Security.
- No value in any message.

REFERENCE FILES:
- src/Encina.FluentValidation/FluentValidationProvider.cs, src/Encina.FluentValidation/ServiceCollectionExtensions.cs
```

</details>

---

### Phase 11 (S11): Audit Bridge Package

> **Goal**: The `IOperationAuditStore` adapter of the port (Design Choice 6, option B).

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.Validation.Security.Audit/Encina.Validation.Security.Audit.csproj` (references `Encina.Validation.Security` and `Encina.Security.Audit`), `OperationAuditSecurityViolationSink.cs` (`ISecurityViolationSink` over `IOperationAuditStore.RecordAsync` and an injected `TimeProvider`; the entry of row C14), `SecurityValidationAuditServiceCollectionExtensions.cs` (`AddEncinaSecurityValidationAudit()` with `TryAddScoped<ISecurityViolationSink, ...>()` so an application's own sink wins in any registration order), the two `PublicAPI` files, a slnx entry and a manifest.
2. **Modify** the unit and guard test projects (one `ProjectReference` each).
3. Tests (S11): unit (entry content: user, tenant, `TimestampUtc` from a fake clock, the payload fields null, the input absent), DI (with `AddEncinaSecurityValidation` and `AddEncinaAudit` in both orders, `ValidateOnBuild` and `ValidateScopes`), guard.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 11</strong></summary>

```text
CONTEXT:
Issue #232, plan docs/plans/security-focused-validation-implementation-plan-232.md, phase 11 of 12. Depends on S5 and S6.
ADR-036: operation audit goes through IOperationAuditStore.

TASK:
1. Create the bridge package with OperationAuditSecurityViolationSink and its registration (TryAdd).
2. Test the entry content, the registration orders and the null checks.

KEY RULES:
- The entry carries codes and ids only (row C14); a Left from the store is returned to the behavior, which logs it.

REFERENCE FILES:
- src/Encina.Security.Audit/IOperationAuditStore.cs, src/Encina.Security.Audit/OperationAuditEntry.cs
```

</details>

---

### Phase 12 (S12): Benchmark, Documentation and Finalization

> **Goal**: The hot-path evidence, the usage guide and the changelog.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `tests/Encina.BenchmarkTests/Encina.Benchmarks/SecurityValidationBenchmarks.cs` (detectors on benign and hostile inputs; `BenchmarkSwitcher` per `AGENTS.md` section 9) and **modify** `tests/Encina.BenchmarkTests/Encina.Benchmarks/Encina.Benchmarks.csproj` (one `ProjectReference`); **create** a load-test justification `.md` (stateless and bounded) and an integration-test justification `.md` (no external dependency).
2. **Create** through `docs-writer`: the READMEs of the three packages, `docs/features/security-focused-validation.md` (rules, strictness, limits of heuristics, the relation to `Encina.Security.Sanitization`, the `IncludeRequestPayload` interaction of row C16, the log volume of row C20), an ADR for the package split and the audit route (number assigned when the ADR pull request opens) and its index line; **modify** `docs/INVENTORY.md`.
3. **Create** `changelog.d/232-security-focused-validation.added.md` (through `mechanical-fixer`).
4. Build verification: `dotnet build --configuration Release` with zero warnings; per-flag coverage of the touched files against the manifest targets below.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 12</strong></summary>

```text
CONTEXT:
Issue #232, plan docs/plans/security-focused-validation-implementation-plan-232.md, phase 12 of 12.

TASK:
1. Write the benchmark and the justifications; document the packages, the rules and their limits.
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
| ADR-036 | Operation audit through `IOperationAuditStore` | Phase 11 |
| ADR-054 D3, D4 | One meter per package; tenant tag only when known | Register row C6 |
| ADR-062 | Error category contract | Register row C7: no new code |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `Encina.Security.Sanitization` | `src/Encina.Security.Sanitization/` | Complements; the package layout and diagnostics pattern are the model |
| Validation providers and `ValidationPipelineBehavior` | `src/Encina.DataAnnotations/`, `src/Encina.MiniValidator/`, `src/Encina.FluentValidation/`, `src/Encina/Validation/` | The three integration points |
| `PipelineStageAttribute`, `PipelineOrderResolver`, `IEncinaPipelineInspector` | `src/Encina/Pipeline/Ordering/` | Places the audit behavior outside validation in any registration order |
| `IOperationAuditStore`, `AddEncinaAudit` | `src/Encina.Security.Audit/` | Audit adapter (bridge) |
| The provider lifetime and the service provider in the validation context | issue #230 | Attributes read options and the collector |
| `EventIdRanges` and `EncinaEventIdAllocationTests` | `src/Encina/Diagnostics/`, `tests/Encina.UnitTests/Testing/Architecture/` | EventId registration |
| `ValidationBenchmarks` | `tests/Encina.BenchmarkTests/Encina.Benchmarks/ValidationBenchmarks.cs` | Benchmark style |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Validation.Security` (`ValidationSecurity`, new, inside the block 550-599 assigned to the validation-1 family) | 590-599 | 590-592 used (three messages); 593-599 free; #230 owns 550-559, #227 560-569, #231 580-589; registered and first used in S5 |

### Estimated File Count

Every count includes tests, corpora, coverage manifest edits, PublicAPI files, test project edits (`ProjectReference` and `Content`), the slnx, READMEs and the changelog fragment; the guard file `SecurityValidationGuardTests.cs` is created in S1 and counted as edited afterwards. No pull request uses a type created by a later one. Checked on `origin/main`: `Encina.UnitTests` and `Encina.GuardTests` already reference `Encina.DataAnnotations`, `Encina.MiniValidator`, `Encina.FluentValidation` and `Encina.Security.Audit`; `Encina.PropertyTests` references the three providers but not the new package; `Encina.ContractTests` references only `Encina.Security.Audit`; `Encina.UnitTests` copies only `Security\ABAC\EEL\TestData\*.json`.

| PR | Content | New src | Changed src | New tests | Edited tests | Manifest | Other | Files | Lines (est.) |
|----|---------|---------|-------------|-----------|--------------|----------|-------|-------|--------------|
| S1 | Skeleton, vocabulary, options | 8 (csproj, 2 PublicAPI, strictness, rule id, violation, options, options validator) | 0 | 2 (options and validator unit, guard) | 2 (unit and guard test csproj) | 1 | 1 (`Encina.slnx`) | 14 | ~650 |
| S2 | Normalizer, `ISecurityDetector`, XSS | 3 | 1 (PublicAPI) | 6 (normalizer unit, normalizer property, XSS unit, detector property, 2 corpus files) | 3 (guard, unit csproj `Content`, property csproj) | 1 | 0 | 14 | ~800 |
| S3 | Path, SQL, collector | 4 (2 detectors, collector interface and class) | 1 (PublicAPI) | 7 (path unit, SQL unit, collector unit, 4 corpus files) | 2 (guard, detector property) | 1 | 0 | 15 | ~900 |
| S4 | LDAP, command, file name | 3 | 1 (PublicAPI) | 7 (LDAP, command, file name unit, 4 corpus files) | 2 (guard, detector property) | 1 | 0 | 14 | ~850 |
| S5 | Audit behavior, sink port, logging, EventIds | 4 (sink port, diagnostics, log class, behavior) | 3 (`EventIdRanges.cs`, PublicAPI of `Encina`, PublicAPI of the package) | 3 (behavior unit, metrics, log) | 2 (guard, `EncinaEventIdAllocationTests`) | 2 (package manifest, `Encina.json`) | 0 | 14 | ~800 |
| S6 | Registration | 2 (extensions, sink check) | 1 (PublicAPI) | 1 (DI) | 1 (guard) | 1 | 0 | 6 | ~350 |
| S7 | Attributes, part 1 | 5 (base, 4 attributes) | 1 (PublicAPI) | 4 (DataAnnotations unit, MiniValidator unit, DI scope test, contract) | 1 (guard) + 1 (`Encina.ContractTests.csproj`) | 1 | 0 | 13 | ~750 |
| S8 | Attributes, part 2 | 3 | 1 (PublicAPI) | 0 | 4 (both unit files, contract, guard) | 1 | 0 | 9 | ~400 |
| S9 | Service provider in the Fluent context | 0 | 1 (`FluentValidationProvider.cs`) | 0 | 1 (`FluentValidationProviderTests.cs`) | 1 (`Encina.FluentValidation.json`) | 0 | 3 | ~120 |
| S10 | FluentValidation satellite | 5 (csproj, extensions, property validator, 2 PublicAPI) | 0 | 3 (extensions unit, guard, equivalence contract) | 3 (unit, guard, contract test csproj) | 1 | 1 (`Encina.slnx`) | 13 | ~800 |
| S11 | Audit bridge package | 5 (csproj, sink, extensions, 2 PublicAPI) | 0 | 3 (unit, DI, guard) | 2 (unit and guard test csproj) | 1 | 1 (`Encina.slnx`) | 12 | ~550 |
| S12 | Benchmark, docs, ADR, changelog | 0 | 0 | 3 (benchmark, load `.md`, integration `.md`) | 1 (`Encina.Benchmarks.csproj`) | 0 | 8 (3 package READMEs, feature page, ADR, ADR index, `docs/INVENTORY.md`, changelog fragment) | 12 | ~650 |
| **Total** | | **42** | **10** | **39** | **25** | **12** | **11** | **139** | **~7,620** |

The rows sum to the total (14 + 14 + 15 + 14 + 14 + 6 + 13 + 9 + 3 + 13 + 12 + 12 = 139) and so do the columns (42 + 10 + 39 + 25 + 12 + 11 = 139). Every statement of the total in this plan is 139 files and about 7,600 changed lines. The corpus files count as test files. The largest pull request is 15 files (S3); none exceeds 1,000 lines. S7 and S8 wait for #230 option A (Design Choice 7); S1 to S6 and S9 do not. With Design Choice 6 option A, S11 becomes 8 files.

Per-file coverage targets (per flag, with a one-sentence justification), written into the package manifests by the pull request that adds the file; a 0 carries its reason:

| File | Unit | Guard | Property | Contract | Justification |
|------|------|-------|----------|----------|---------------|
| `SecurityStrictness.cs`, `ISecurityDetector.cs`, `ISecurityViolationCollector.cs`, `ISecurityViolationSink.cs` | 0 | 0 | 0 | 0 | No executable code (an enum and interfaces); the contract test pins the behavior of their implementations |
| `SecurityRuleId.cs`, `SecurityViolation.cs` | 90 | 0 | 0 | 0 | The CWE map and record equality are unit cases; neither has a parameter that is validated |
| `SecurityValidationOptions.cs`, `...OptionsValidator.cs` | 95 | 50 | 0 | 0 | Plain data and rules; the guard test reaches the one null check of `Validate` |
| `Detectors/*.cs` | 90 | 0 | 50 | 30 | Payload and benign corpora drive every pattern and strictness branch; `SecurityDetectorsProperties.cs` (S2, extended by S3 and S4) asserts no throw and determinism over arbitrary strings; the contract test runs every detector; guard is 0 because the span and enum parameters have no executable check |
| `SecurityInputNormalizer.cs` | 95 | 0 | 80 | 0 | Internal pure function; the unit test covers the lone surrogate, triple encoding and expansion, the property test the bounds |
| `SecurityViolationCollector.cs` | 90 | 70 | 0 | 40 | Add, drain and thread-safety are unit cases; the guard test reaches the null check of `Add`; the contract test uses the interface |
| `SecurityViolationAuditPipelineBehavior.cs` | 90 | 80 | 0 | 0 | The sink success, the sink `Left`, no violation and the gap are unit cases; the guard tests cover the constructor and `Handle` arguments |
| `Diagnostics/*.cs` | 90 | 0 | 0 | 0 | `MeterListener` and log capture tests cover them; internal static types have no guard surface |
| `SecurityValidationServiceCollectionExtensions.cs`, `SecurityValidationSinkCheck.cs` | 90 | 80 | 0 | 0 | Both registration orders and the sink check are tested; the null check of `services` is guard-tested |
| `Attributes/*.cs` | 90 | 60 | 0 | 70 | Each attribute is run through the real DataAnnotations and MiniValidator; the guard tests reach the null `ValidationContext`; the contract test pins the shared shape |
| `SecurityRuleBuilderExtensions.cs`, `SecurityPropertyValidator.cs` | 90 | 70 | 0 | 60 | A real `AbstractValidator` is validated per rule; the equivalence contract compares the three providers |
| `OperationAuditSecurityViolationSink.cs`, `SecurityValidationAuditServiceCollectionExtensions.cs` | 90 | 80 | 0 | 0 | Entry content and the store `Left` are unit cases; constructor and method arguments are guard-tested |
| `FluentValidationProvider.cs` (S9), `EventIdRanges.cs` (S5) | existing | existing | existing | existing | The existing manifest targets stay (they never go down); the added statement is covered by the added test, and `EventIdRanges.cs` holds constants checked by `EncinaEventIdAllocationTests` |

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
- The maintainer's answers to Design Choices 1-4, 6 and 7 of docs/plans/security-focused-validation-implementation-plan-232.md
  decide the package layout, the detection strategy, the audit route, how attributes get their options, where the
  audit adapter lives and how the collector reaches the attribute.

IMPLEMENTATION OVERVIEW:
1. Encina.Validation.Security: vocabulary, options, bounded normalizer, six detectors on NonBacktracking patterns.
2. The request-scoped collector, the sink port, the audit behavior in the Audit stage, EventIds 590-592 (registered in S5).
3. AddEncinaSecurityValidation, DataAnnotations attributes and the FluentValidation satellite package.
4. The Encina.Validation.Security.Audit bridge; benchmark, docs, ADR and changelog.
Twelve pull requests (S1 to S12), see the Estimated File Count table.

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
| 2 | OpenTelemetry | ✅ Phase 5 | Counter `encina.validation.security.violations` and an event on `Activity.Current`; tenant tag only when known (ADR-054 D4, register row C6); the central validation duration is #726 |
| 3 | Structured Logging | ✅ Phase 5 | `SecurityValidationLog` EventIds 590-592 in the new range 590-599; rule id and CWE only, never the input |
| 4 | Health Checks | ❌ N/A | Stateless detectors with no dependency |
| 5 | Validation | ✅ All phases | This is validation; the three providers of `AGENTS.md` section 5 are covered (attributes for two, the satellite for one) and pinned by a contract test |
| 6 | Resilience | ❌ N/A | In-process, bounded linear-time matching; no external call |
| 7 | Distributed Locks | ❌ N/A | No shared state |
| 8 | Transactions | ❌ N/A | Validation precedes the handler; the audit write is its own store call, awaited in the request |
| 9 | Idempotency | ❌ N/A | Stateless and repeatable |
| 10 | Multi-Tenancy | ⏭️ | The tenant id is tagged and audited when known (Phases 5 and 11); tenant-specific strictness is a follow-up shared with the family: follow-up issue to be opened (draft: `artifacts/issues/validation-tenant-module-scoped-selection.md`) |
| 11 | Module Isolation | ⏭️ | Module-scoped rules: the same follow-up issue |
| 12 | Audit Trail | ✅ Phases 5 and 11 | Violations are audited through `IOperationAuditStore` (ADR-036) by the bridge adapter; opt-in by `AuditViolations`, a missing sink fails start-up |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| `IOperationAuditStore` and `AddEncinaAudit` | ✅ On `main` | Phase 11 |
| Scoped providers that pass their service provider to the validation context | Plan #230 (Design Choice 2 option A and row D3) | Blocking for S7 and S8 of this plan (Design Choices 4 and 7 option A) |
| ADR-062 error category contract | ✅ Merged | Register row C7 |
| `PipelineStageAttribute` and `PipelineOrderResolver` | ✅ On `main` | Register row D4 |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| Code-less validation `Left` never becomes a 400 | #3415 | The `Left` that carries the violations is #3415's |
| Which error text a client may see | #3426 | The violation message is generic by design |
| Central validation instrumentation | #726 | Covers duration; this plan adds the security counter only |

---

## Next Steps

1. Link this plan from #232 and ask the maintainer to answer Design Choices 1 to 4, 6 and 7 and the Dilemma rows of the register when work starts.
2. Open the follow-up issue when the first pull request opens: tenant and module scoped selection and strictness (draft: `artifacts/issues/validation-tenant-module-scoped-selection.md`).
3. Implement S1 to S12 as the pull requests of the Estimated File Count split, `Fixes #232` on S12.
