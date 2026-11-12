# Implementation Plan: Event metadata headers never carry `[CryptoShredded]` personal data

> **Issue**: [#2457](https://github.com/dlrivada/Encina/issues/2457)
> **Type**: Feature
> **Complexity**: Medium (5 phases, 1 provider: Marten, 3 pull requests, about 33 files in total)
> **Estimated Scope**: ~500 lines of production code + ~1,000 lines of tests (each pull request under 1,500 changed lines)

---

## Summary

Marten stores event headers as a plain string-keyed dictionary on the event row. The crypto-shredding serializer encrypts `[CryptoShredded]` properties of event bodies only, so a header value copied from an event's plaintext (typically by an `IEventMetadataEnricher`, which receives the event, `EventMetadataEnrichmentService.cs`) is stored unencrypted and survives erasure. This plan adds a fail-closed guard: `Encina.Marten` exposes a hook that `Encina.Marten.GDPR` registers; for every header an enricher returns, the guard compares the value (normalized: trim, case-insensitive equality) with the `[CryptoShredded]` plaintext of the events being appended and rejects the append, reporting only the header key and an error code. It also makes the built-in `UserId` header an explicit opt-in capture and documents that headers are never crypto-shredded.

- **Standards**: GDPR Art. 17 (erasure) and Art. 5(1)(f) (integrity and confidentiality); AGENTS.md section 3 (no `EncinaError.Message` in logs, no swallowed errors).
- **Maintainer decisions already recorded** (#1770 comment C5, 2026-10-26/27): matching is normalized equality with documented misses; scope is enricher values only; `UserId` is not retained by default. They appear as Settled choices below.
- **Affected packages**: `Encina.Marten` (hook, `EnrichSession` result, `CaptureUserId` default, repositories), `Encina.Marten.GDPR` (guard, registration, error code, logs, counter).
- **Provider category**: event sourcing, Marten on PostgreSQL only; the database, caching, transport and lock matrices do not apply.
- **Prerequisites**: none open. Related: the Roslyn analyzer issue #2461 (build-time advisory, separate) and #2423/#2433 (other crypto-shredding items).
- **Scope and split**: counted with tests, manifests, PublicAPI files and changelog fragments the work is about 33 files, over the 15-file limit of one worker PR, so it ships as three pull requests that each build and pass their own tests (the table under Estimated File Count lists the files of each):
  - **PR 1, hook in `Encina.Marten`** (13 files): `IEventMetadataHeaderGuard`, the `Either`-returning staged `EnrichSession`, the repository constructors, the `CaptureUserId` default; tested with a test-double guard.
  - **PR 2, guard in `Encina.Marten.GDPR`** (10 files): `CryptoShreddedHeaderGuard`, the error factory and its category, the registration and DI test; unit, guard and contract tests with the real guard. Needs PR 1 merged.
  - **PR 3, observability, property and integration tests, documentation** (10 files): log messages, counter, the property test, the PostgreSQL integration tests, the `.md` justifications and the README of the GDPR package. Needs PR 2 merged.

---

## Design Choices

<details>
<summary><strong>1. Where the guard runs: a hook in Encina.Marten implemented by Encina.Marten.GDPR</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IEventMetadataHeaderGuard` in `Encina.Marten`, implemented and registered by `Encina.Marten.GDPR`** | Marten core never references GDPR code; pay-for-what-you-use; fail closed at the one place headers are set | New public interface |
| **B) Marten event masking for headers** (`AddMaskingRuleForProtectedInformation`) | Built-in | After the fact, plaintext exists meanwhile; header behaviour unverified |
| **C) Documentation plus the Roslyn analyzer only** | No runtime cost | Advisory only; excluded by the INV-005 rule recorded on the issue |

### Chosen Option: **A — hook in `Encina.Marten`, guard in `Encina.Marten.GDPR`**

### Rationale

- `Encina.Marten.GDPR` already references `Encina.Marten` (`.csproj`), never the reverse, and registers its services in `AddEncinaMartenGdpr` (`ServiceCollectionExtensions.cs`).
- The guard is registered with `TryAddEnumerable`, so applications can add their own guards beside it.

- **Class:** Settled
- **Settled by:** AGENTS.md section 3 (compliance and security gates fail closed, which excludes the after-the-fact option B and the advisory-only option C) and the module dependency direction (`Encina.Marten.GDPR` references `Encina.Marten`, so the hook can only live in `Encina.Marten`). Decision C5 of #1770 decides matching, scope and `UserId` (choices 2, 3 and 5), not the placement of the hook.

</details>

<details>
<summary><strong>2. Matching rule: normalized equality against crypto-shredded plaintext</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Normalized equality (trim, ordinal ignore-case) of the header value's string form** | Decided; cheap; no false positives on short fragments | Misses hashes, substrings, concatenations (documented) |
| **B) Substring / token containment** | Catches concatenations | False positives on short values (an initial, a postcode); cost grows with the event |
| **C) Taint tracking of values returned by getters** | Precise | Not feasible on `IDictionary<string, object>` returned by arbitrary code |

### Chosen Option: **A — normalized equality**

### Rationale

- Only string-like header values are compared (`string`, or any value whose `ToString()` is non-empty after trimming); a blank value never matches.
- The documentation lists the misses as a residual limit, not a fork.

- **Class:** Settled
- **Settled by:** maintainer decision C5 (matching = (b) normalized equality) at https://github.com/dlrivada/Encina/issues/1770#issuecomment-6436978234

</details>

<details>
<summary><strong>3. Scope of the guard: enricher values only</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Values returned by `IEventMetadataEnricher`** | Decided; the only dynamic, event-derived source | `CustomHeaders` and built-in headers unguarded |
| **B) Every header source** (also `CustomHeaders`, built-ins) | Complete | `CustomHeaders` are static configuration, built-ins are identifiers; matching them against event plaintext is noise |

### Chosen Option: **A — enricher values only**

### Rationale

- `EventMetadataEnrichmentService.ApplyEnricherHeaders` is the single place that sets event-derived headers.
- The existing `catch (Exception)` there logs and continues (error 2658); a guard rejection must not be inside that `try`, or it would be swallowed.

- **Class:** Settled
- **Settled by:** maintainer decision C5 (scope = (e) enricher values only) at https://github.com/dlrivada/Encina/issues/1770#issuecomment-6436978234 and AGENTS.md section 3 (errors never swallowed in background infrastructure)

</details>

<details>
<summary><strong>4. How a rejection reaches the caller: <code>EnrichSession</code> returns <code>Either</code></strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `EnrichSession` returns `Either<EncinaError, Unit>`; `SaveAsync`/`CreateAsync` return the `Left` before `SaveChangesAsync`** | ROP; nothing stored; no exception for business logic | Four call sites change (`MartenAggregateRepository.cs:194,293`, `SnapshotAwareAggregateRepository.cs:167,263`) |
| **B) Guard throws a typed exception** | Smaller diff | Violates ROP (ADR-001/006) |

### Chosen Option: **A — `Either` result, append refused before commit**

### Rationale

- `SetHeader` is session-wide and the session is scoped and reused (by the unit of work and by the next `SaveChangesAsync` of another aggregate). Failing before the commit is therefore not enough: a header set before the rejection would stay on the session and be stamped on unrelated events. So `EnrichSession` is staged: (1) it collects every enricher header into a local dictionary (an enricher that throws contributes nothing, and is logged as today), (2) it runs every guard over the whole batch, (3) only when all guards pass does it mutate the session (correlation id, causation id, built-in headers, `CustomHeaders`, then the collected enricher headers, in the same precedence as today). A `Left` leaves the session exactly as it was.
- The error carries the header key and the code `crypto.header_contains_personal_data`; never the value.
- Error categories (ADR-062), created with `EncinaErrors.Create(code, category, ...)`: `crypto.header_contains_personal_data` is `Validation` (R3: a rule about an input value); the reused `crypto.serializer_unsupported` is `Internal` (R4: a serialization/crypto configuration failure). Tests assert both with `GetCategory`.

- **Class:** Settled
- **Settled by:** AGENTS.md section 3 (Railway Oriented Programming; errors never swallowed) and ADR-001

</details>

<details>
<summary><strong>5. Built-in <code>UserId</code> header: default off, or default on with a startup warning</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `EventMetadataOptions.CaptureUserId` defaults to `false` (explicit opt-in)** | No retained direct identifier by default; simple | Applications that relied on the default lose the header silently (pre-1.0: acceptable) |
| **B) Keep `true`, log a startup warning when crypto-shredding is registered** | Behaviour unchanged | A retained direct identifier remains the default; warning is easy to ignore |

### Chosen Option: **A — default `false` (recommended, pending the maintainer)**

### Rationale

- The maintainer decided "explicit opt-in capture (or startup warning), not retained by default". Option A satisfies "not retained by default"; option B keeps retaining it. The choice between them is the part left open.
- Option A wins in a deployment that never reads the header; option B wins in one whose queries (`MartenEventMetadataQuery.cs:378-386`) already depend on `UserId` being present everywhere.
- Either way the documentation states that `UserId` is a retained direct identifier that cannot be crypto-shredded.

- **Class:** Dilemma

</details>

---

## Implementation Phases

### Phase 1: Hook and fail-closed enrichment in `Encina.Marten`

<details>
<summary>Tasks</summary>

1. New public `IEventMetadataHeaderGuard` (`src/Encina.Marten/IEventMetadataHeaderGuard.cs`): `Either<EncinaError, Unit> Validate(IDocumentSession session, IReadOnlyCollection<object> events, string headerKey, object headerValue)`.
2. `EventMetadataEnrichmentService`: take `IEnumerable<IEventMetadataHeaderGuard>` in its constructor (a trailing optional parameter, so its seven existing direct constructions in `EventMetadataEnrichmentServiceTests.cs` (lines 387, 454-530) and the three in `EventMetadataEnrichmentServiceGuardTests.cs` (lines 11, 20, 29) keep compiling). `EnrichSession` returns `Either<EncinaError, Unit>` and is staged as in Design Choice 4: collect the enricher headers into a local dictionary (the enricher `try/catch` stays around the enricher call only), run every guard for each collected header, and call `session.SetHeader`, `CorrelationId` and `CausationId` only after every guard passed. The guard call sits outside the `try`.
3. `MartenAggregateRepository` (constructor line 53, call sites 194 and 293) and `SnapshotAwareAggregateRepository` (constructor line 43, call sites 167 and 263): the public constructors gain a trailing optional `IEnumerable<IEventMetadataHeaderGuard>? headerGuards = null` after `projectionDispatcher`, passed to the service built at lines 75 and 71; the `Left` of `EnrichSession` is returned before `SaveChangesAsync`. DI resolves the extra parameter by constructor injection with no change to the three registrations in `ServiceCollectionExtensions.cs` (lines 52, 143, 372); the existing positional constructions in the tests (about 60 across the Marten unit, guard, contract and integration tests and the compliance aggregate integration tests) keep compiling because the parameter is optional last. A DI test resolves an `IAggregateRepository<T>` with a guard registered and asserts the guard is invoked.
4. `EventMetadataOptions.CaptureUserId` default (per choice 5) and XML docs.
5. `PublicAPI.Unshipped.txt` lines (via `mechanical-fixer`).

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: Encina.Marten (src/Encina.Marten). EventMetadataEnrichmentService.EnrichSession is void and swallows enricher exceptions. Issue #2457.
TASK: Add IEventMetadataHeaderGuard, make EnrichSession return Either<EncinaError, Unit> and staged (collect enricher headers, guard them all, then mutate the session), add the optional trailing headerGuards constructor parameter to both repositories, and propagate a Left from the four repository call sites before the session is saved. Apply the CaptureUserId default chosen in Design Choice 5. Tests use a test-double guard; add the unit test "a rejection leaves no header, correlation id or causation id set on the session" and a unit test "a second save after a rejected one carries no header of the rejected batch".
KEY RULES: ROP, no exceptions for business outcomes; a guard failure is never inside the enricher try/catch; no session mutation before every guard passed; never log the header value or EncinaError.Message; async where the repositories are async; XML docs; PublicAPI.Unshipped.txt; zero warnings.
REFERENCE FILES: src/Encina.Marten/EventMetadataEnrichmentService.cs, src/Encina.Marten/IEventMetadataEnricher.cs, src/Encina.Marten/MartenAggregateRepository.cs, src/Encina.Marten/Snapshots/SnapshotAwareAggregateRepository.cs, src/Encina.Marten/EventMetadataOptions.cs
```

</details>

### Phase 2: Guard implementation and registration in `Encina.Marten.GDPR`

<details>
<summary>Tasks</summary>

1. New `src/Encina.Marten.GDPR/Metadata/CryptoShreddedHeaderGuard.cs` (`internal sealed`, implements `IEventMetadataHeaderGuard`): gets the `CryptoShredderSerializer` from `session.DocumentStore.Options.Serializer()` (as `MartenEventPersonalDataLocator.LocateAsync` does), walks each event with `CryptoShreddedGraphWalker.Walk(event, serializer.WalkOptions!, serializer.Registry)`, reads `occurrence.Field.Getter(occurrence.Owner)`, normalizes and compares.
2. `CryptoShreddingErrors.HeaderContainsPersonalData(string headerKey)` and `HeaderContainsPersonalDataCode = "crypto.header_contains_personal_data"`, created with `ErrorCategory.Validation`; the reused `SerializerUnsupported` error is `Internal` (ADR-062 R3 and R4), with a `GetCategory` unit test for each.
3. `AddEncinaMartenGdpr`: `services.TryAddEnumerable(ServiceDescriptor.Singleton<IEventMetadataHeaderGuard, CryptoShreddedHeaderGuard>())`. Serializer unsupported (store without `CryptoShredderSerializer`) returns the existing `SerializerUnsupported` error (fail closed).
4. DI test with `ValidateOnBuild` and `ValidateScopes` (AGENTS.md section 3, registration completeness).

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: Phase 1 added IEventMetadataHeaderGuard to Encina.Marten. Encina.Marten.GDPR already walks [CryptoShredded] fields in MartenEventPersonalDataLocator and CryptoShreddedGraphWalker.
TASK: Implement CryptoShreddedHeaderGuard, the error factory and code, and the TryAddEnumerable registration in AddEncinaMartenGdpr.
KEY RULES: fail closed when the serializer is not CryptoShredderSerializer; compare only normalized string forms (trim, OrdinalIgnoreCase), blank never matches; the error and every log carry the header key and the error code only, never the value or the subject id; ForLogging() for exceptions.
REFERENCE FILES: src/Encina.Marten.GDPR/Locator/MartenEventPersonalDataLocator.cs, src/Encina.Marten.GDPR/Locator/CryptoShreddedGraphWalker.cs, src/Encina.Marten.GDPR/CryptoShreddingErrors.cs, src/Encina.Marten.GDPR/ServiceCollectionExtensions.cs
```

</details>

### Phase 3: Observability

<details>
<summary>Tasks</summary>

1. EventIds 8489-8491 in `Diagnostics/CryptoShreddingLog.cs` (the class that holds the `[LoggerMessage]` source-generated methods, lines 76-86 are 8482-8484; `CryptoShreddingLogMessages.cs` holds the older `LoggerMessage.Define` ids up to 8469), inside the registered `MartenGDPRCryptoShredding` range (8450-8499). 8450-8484 are used on main, and 8485-8488 are taken by the merged #2455 plan (`crypto-shredding-storage-leak-validation-implementation-plan-2455.md`), so this plan uses 8489 header rejected (key, code), 8490 guard serializer unsupported, and 8491 `UserId` capture warning (only if choice 5 option B is chosen). The batch registry records the claim 8489-8491 for #2457; re-read `eventid-blocks.md` before writing and follow it if #2455 changes.
2. `Counter<long> HeaderRejectedTotal` (`crypto.header.rejected.total`) on the existing `CryptoShreddingDiagnostics.Meter`, with ONE fixed low-cardinality tag `reason` (`personal_data` or `serializer_unsupported`). The header key is chosen by arbitrary enricher code, so it is never a metric tag (ADR-054 D6 forbids data-carrying keys); it appears only in the log message. Activity failure through `CryptoShreddingDiagnostics.RecordFailed` with the error code. Tenant tag (ADR-054 D4): the counter and the failure activity carry `encina.tenant_id` (from the session's tenant) only when multi-tenancy is on, detected with `MultiTenancyMarker`, and none when it is off; a unit test asserts both.
3. No new range: the registry and `EncinaEventIdAllocationTests` already cover the assembly.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: Encina.Marten.GDPR owns EventIds 8450-8499 (EventIdRanges.MartenGDPRCryptoShredding); 8450-8484 are used and 8485-8488 are taken by plan #2455 (re-read the batch registry eventid-blocks.md first).
TASK: Add the log messages 8489-8491 in CryptoShreddingLog.cs and the rejection counter; call them from CryptoShreddedHeaderGuard.
KEY RULES: ADR-021 sequential ids; the counter has one fixed tag (reason), never the header key or value; the key may appear in the log only; EncinaError.Message never logged.
REFERENCE FILES: src/Encina.Marten.GDPR/Diagnostics/CryptoShreddingLog.cs, src/Encina.Marten.GDPR/Diagnostics/CryptoShreddingDiagnostics.cs, src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

### Phase 4: Testing

<details>
<summary>Tasks</summary>

1. Unit (`Encina.UnitTests`): guard accepts clean headers, rejects exact and case/whitespace-different plaintext, ignores blank values, nested `[CryptoShredded]` fields, unsupported serializer; `EnrichSession` returns `Left` and a rejection leaves no header, correlation id or causation id set on the session (including headers an earlier enricher returned for the same batch); enricher exceptions are still logged and tolerated; `GetCategory` of both errors.
2. Guard tests: new public members (`IEventMetadataHeaderGuard` consumers, constructors) throw `ArgumentNullException`.
3. Contract tests: `IEventMetadataHeaderGuard` contract with the real `CryptoShreddedHeaderGuard` and a test double.
4. Property tests (FsCheck): any non-blank `[CryptoShredded]` string, with arbitrary case and padding, is rejected; arbitrary strings that differ after normalization are accepted.
5. Integration (Marten on PostgreSQL via Testcontainers, `MartenCollection`): an enricher returning the subject's email makes `SaveAsync` return `Left` and store nothing; a clean enricher stores its header; after a rejected save, a second aggregate saved through the same session carries none of the rejected batch's headers.
6. Load and benchmark: `.md` justifications (not concurrent infrastructure; per-append cost is one graph walk).
7. Per-file coverage targets (numbers are the floor; the worker measures the baseline with the AGENTS.md section 9 commands and writes them, with their sentences, to `Encina.Marten.json` and `Encina.Marten.GDPR.json` via `mechanical-fixer`; the current manifests carry only `defaultTests` for these files):

| File (PR) | Unit | Guard | Contract | Property | Integration | One-sentence justification |
|-----------|------|-------|----------|----------|-------------|----------------------------|
| `Encina.Marten/EventMetadataEnrichmentService.cs` (1) | 90 | 40 | 0 | 0 | 70 | Staging, rejection and enricher-failure branches are unit-tested with a test-double guard; guard tests cover argument checks. |
| `Encina.Marten/MartenAggregateRepository.cs`, `SnapshotAwareAggregateRepository.cs` (1) | 85 | 50 | 40 | 0 | 75 | The new `Left` propagation is unit-tested on both save paths; integration reaches them on a real store. |
| `Encina.Marten/EventMetadataOptions.cs` (1) | 100 | 0 | 0 | 0 | 0 | A property default asserted by a unit test. |
| `Encina.Marten.GDPR/Metadata/CryptoShreddedHeaderGuard.cs` (2) | 95 | 70 | 90 | 90 | 85 | Match, miss, blank, nested field and unsupported-serializer branches are unit-tested; the contract and property tests drive the real guard; PostgreSQL tests cover the end-to-end rejection. |
| `Encina.Marten.GDPR/CryptoShreddingErrors.cs` (2) | 90 | 0 | 0 | 0 | 0 | The new factory and code constant are asserted, with their categories. |
| `Encina.Marten.GDPR/ServiceCollectionExtensions.cs` (2) | 95 | 65 | 0 | 0 | 0 | The `TryAddEnumerable` registration is covered by the DI test with `ValidateOnBuild` and `ValidateScopes`. |
| `Encina.Marten.GDPR/Diagnostics/CryptoShreddingLog.cs`, `CryptoShreddingDiagnostics.cs` (3) | 80 | 0 | 0 | 0 | 60 | Counter and log calls run when the guard rejects; the unit tests assert the counter tag. |

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT: Per-flag coverage (AGENTS.md section 9). Manifests: .github/coverage-manifest/Encina.Marten.json and Encina.Marten.GDPR.json (touched: EventMetadataEnrichmentService.cs, repositories, EventMetadataOptions.cs, ServiceCollectionExtensions.cs, CryptoShreddingErrors.cs, new CryptoShreddedHeaderGuard.cs).
TASK: Write the tests of Phase 4 with Encina.Testing.Shouldly; add per-file targets with one-sentence justifications to both manifests via mechanical-fixer.
KEY RULES: real implementations instantiated (no reflection-only tests); integration through the shared Marten collection fixture; compliance modules use Marten on PostgreSQL, not in-memory stores; CRAP <= 10 on changed methods.
REFERENCE FILES: tests/Encina.UnitTests/Marten/, tests/Encina.IntegrationTests/Infrastructure/Marten/, .github/coverage-manifest/Encina.Marten.GDPR.json
```

</details>

### Phase 5: Documentation and finalization

<details>
<summary>Tasks</summary>

1. `src/Encina.Marten.GDPR/README.md` and `src/Encina.Marten/README.md`: headers are never crypto-shredded, `UserId` is a retained direct identifier, the guard and its documented misses (via `docs-writer`).
2. `changelog.d/2457-event-header-crypto-guard.added.md` and, for the `CaptureUserId` default, `changelog.d/2457-capture-userid-opt-in.changed.md` (via `mechanical-fixer`).
3. ADR: none new; the decision record is the #1770 spike decision register. `PublicAPI.Unshipped.txt` complete.
4. `dotnet build --configuration Release` with 0 warnings; every flag reaches its manifest target.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT: Phases 1-4 are implemented.
TASK: Document the guard and the retained UserId header, add the changelog fragments, verify the build.
KEY RULES: never edit CHANGELOG.md; docs by docs-writer; no typed coverage figures in docs.
REFERENCE FILES: src/Encina.Marten.GDPR/README.md, src/Encina.Marten/README.md, changelog.d/README.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Relevance |
|--------|-----------|
| GDPR Art. 17 | Erasure must reach every stored copy of the personal data |
| GDPR Art. 5(1)(f) | Integrity and confidentiality of stored personal data |
| Spike #1770 (row 10) and decision C5 | Header leak path and the decided matching, scope and `UserId` rules |
| AGENTS.md section 3 | `EncinaError.Message` never logged; errors never swallowed |
| ADR-019 | Marten for event-sourced compliance modules |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `EventMetadataEnrichmentService` | `src/Encina.Marten/EventMetadataEnrichmentService.cs` | Where headers are set; gains the guard call and an `Either` result |
| `IEventMetadataEnricher` | `src/Encina.Marten/IEventMetadataEnricher.cs` | Source of the guarded values |
| `CryptoShreddedGraphWalker` | `src/Encina.Marten.GDPR/Locator/CryptoShreddedGraphWalker.cs` | Finds `[CryptoShredded]` fields and owners in an event |
| `CryptoShredderSerializer` (`WalkOptions`, `Registry`) | `src/Encina.Marten.GDPR/Serialization/CryptoShredderSerializer.cs` | Supplies the walk options |
| `CryptoShreddingErrors` | `src/Encina.Marten.GDPR/CryptoShreddingErrors.cs` | New error code and factory |
| `CryptoShreddingDiagnostics` | `src/Encina.Marten.GDPR/Diagnostics/CryptoShreddingDiagnostics.cs` | Meter and activity for the rejection counter |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| Encina.Marten.GDPR | 8489-8491 | Inside the registered `MartenGDPRCryptoShredding` range 8450-8499 (8450-8484 used on main, 8485-8488 taken by #2455); no new range. The batch registry records the claim 8489-8491 for #2457 under "Ids inside existing package ranges" |

### Estimated File Count

| PR | Category | Files | Notes |
|----|----------|-------|-------|
| 1 | Production | 5 | `IEventMetadataHeaderGuard.cs` (new), `EventMetadataEnrichmentService.cs`, `MartenAggregateRepository.cs`, `SnapshotAwareAggregateRepository.cs`, `EventMetadataOptions.cs` |
| 1 | Tests | 4 | `EventMetadataEnrichmentServiceTests.cs`, `EventMetadataEnrichmentServiceGuardTests.cs`, repository DI/unit test (modified), contract test with a test double (new) |
| 1 | Metadata and docs | 4 | `PublicAPI.Unshipped.txt` (Marten), `Encina.Marten.json` manifest, fragment `2457-capture-userid-opt-in.changed.md`, `src/Encina.Marten/README.md` |
| 1 | **Total** | **13** | Builds and passes alone: no implementer of the hook exists yet |
| 2 | Production | 3 | `Metadata/CryptoShreddedHeaderGuard.cs` (new), `CryptoShreddingErrors.cs`, `ServiceCollectionExtensions.cs` |
| 2 | Tests | 4 | Unit guard tests (new), GDPR guard test (new), contract test with the real guard (new), DI test (existing registration test, modified) |
| 2 | Metadata | 3 | `PublicAPI.Unshipped.txt` (GDPR), `Encina.Marten.GDPR.json` manifest, fragment `2457-event-header-crypto-guard.added.md` |
| 2 | **Total** | **10** | Needs PR 1 |
| 3 | Production | 3 | `CryptoShreddingLog.cs`, `CryptoShreddingDiagnostics.cs`, `CryptoShreddedHeaderGuard.cs` (log and counter calls) |
| 3 | Tests | 5 | Property test (new), PostgreSQL integration test (new), observability unit test (new), load and benchmark `.md` justifications (2) |
| 3 | Metadata and docs | 2 | `src/Encina.Marten.GDPR/README.md`, manifest update |
| 3 | **Total** | **10** | Needs PR 2 |
| | **Grand total** | **33** | Each PR is under 15 files and about 500 lines of changes |

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina (pre-1.0, .NET 10, C# 14). Issue #2457: event metadata headers must never carry [CryptoShredded] personal data. Maintainer decisions C5 of #1770 apply: normalized equality, enricher values only, UserId not retained by default. AGENTS.md rules apply (ROP, fail closed, no EncinaError.Message in logs, per-flag coverage, CRAP <= 10).

IMPLEMENTATION OVERVIEW: (1) IEventMetadataHeaderGuard in Encina.Marten and an Either-returning EnrichSession; (2) CryptoShreddedHeaderGuard registered by AddEncinaMartenGdpr; (3) EventIds 8489-8491 (8485-8488 belong to #2455) and a rejection counter with one fixed tag; (4) unit, guard, contract, property and PostgreSQL integration tests; (5) docs, changelog, PublicAPI, manifests.

KEY PATTERNS: Either<EncinaError, T>; TryAddEnumerable; the guard call outside the enricher try/catch; DI test with ValidateOnBuild and ValidateScopes; shared Marten collection fixture.

REFERENCE FILES: src/Encina.Marten/EventMetadataEnrichmentService.cs, src/Encina.Marten.GDPR/Locator/MartenEventPersonalDataLocator.cs, src/Encina.Marten.GDPR/ServiceCollectionExtensions.cs, src/Encina.Marten.GDPR/Diagnostics/CryptoShreddingDiagnostics.cs, .github/coverage-manifest/Encina.Marten.GDPR.json
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | No reads of cacheable data |
| 2 | OpenTelemetry | ✅ | Rejection counter and failed activity on the existing GDPR meter (Phase 3) |
| 3 | Structured Logging | ✅ | EventIds 8489-8491; header key and error code only |
| 4 | Health Checks | ❌ | No new checkable dependency |
| 5 | Validation | ✅ | The guard validates header values at the append boundary |
| 6 | Resilience | ❌ | No external call |
| 7 | Distributed Locks | ❌ | No shared mutable state |
| 8 | Transactions | ✅ | The append is refused before `SaveChangesAsync` and the session is left untouched (staged `EnrichSession`, Design Choice 4), so nothing is stored and nothing leaks to the next save on the same scoped session |
| 9 | Idempotency | ❌ | A rejected append can be retried with corrected headers |
| 10 | Multi-Tenancy | ❌ | The `TenantId` header is an identifier and stays unchanged; the guard reads only the events being appended through the caller's tenant-scoped session, and the rejection counter and activity carry `encina.tenant_id` only when tenancy is on (ADR-054 D4); the guard never denies or allows by tenant, so there is no tenant-required branch |
| 11 | Module Isolation | ❌ | Not module-scoped |
| 12 | Audit Trail | ❌ | A rejection is an application error, not an audited operation |

---

## Next Steps

1. Commit this plan, open its PR and link it from #2457.
2. When the issue is picked up, re-check the choices against today's code and ask the maintainer about Design Choice 5 (the only open one).
3. Brief three workers in sequence, one per pull request of the split (13, 10 and 10 files); PR 2 starts after PR 1 merges and PR 3 after PR 2.

---

## Maintainer Decisions

1. (2026-10-27) Matching rule: normalized equality (trim, case) with the misses documented (decision C5 on #1770, https://github.com/dlrivada/Encina/issues/1770#issuecomment-6436978234). Recorded as Settled in Design Choice 2.
2. (2026-10-27) Scope: enricher values only (decision C5 on #1770). Recorded as Settled in Design Choice 3.

The built-in `UserId` header (Design Choice 5) was decided only as "explicit opt-in capture or startup warning, not retained by default"; the maintainer has not yet chosen between the two, so it has no entry here.
