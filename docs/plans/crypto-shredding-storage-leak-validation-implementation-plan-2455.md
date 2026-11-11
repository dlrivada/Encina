# Implementation Plan: Crypto-shredding startup validation of Marten storage leaks (duplicated fields, identity members, flat-table projections)

> **Issue**: [#2455](https://github.com/dlrivada/Encina/issues/2455)
> **Type**: Feature (hardening, milestone v0.14.0)
> **Complexity**: Medium (10 phases, 1 package, no database-provider matrix)
> **Estimated Scope**: ~700-900 lines of production code + ~1,200-1,600 lines of tests, delivered as three pull requests

---

## Summary

`Encina.Marten.GDPR` encrypts `[CryptoShredded]` strings inside the JSON that Marten writes. Spike #1770 showed that some Marten features copy the decrypted in-memory value into PostgreSQL columns without going through the serializer: duplicated fields (`[DuplicateField]`, `Schema.For<T>().Duplicate(...)`, the duplicated-field unique index), a document identity member (the `id` column and the identity map) and `FlatTableProjection` column maps. Each copy survives key deletion, `MartenEventPersonalDataLocator` reads events only, and an Art. 17 erasure therefore reports success while plaintext remains. Today the only protection is documentation (`docs/features/crypto-shredding.md` line 808, ADR-034 line 47 "documented, not enforced").

This plan extends the existing startup validation (`CryptoShreddingStartupValidationHostedService`, the same pattern as ADR-034) so the host fails to start in those three cases, warns for computed, GIN and full-text indexes over a crypto member (a lookup by plaintext never matches, but nothing leaks), and offers a narrow, audited opt-out. The maintainer decisions of 2026-10-26/27 (C4 of the #1770 decision register) and 2026-10-30 (factual non-applicability) fix the policy; this plan implements them and leaves the technical choices they did not cover as Dilemmas.

- **Standards**: GDPR Art. 5(1)(f), 5(2), 17, 25, 32 (plus Art. 2 and 4(1) for the opt-out); SPEC-002 DEC-006 (fail closed); ADR-034, ADR-021, ADR-018.
- **Estimated scope**: ~10 production files touched or created, ~8 test files, ~5 documentation and metadata files. AGENTS.md section 11 limits one pull request to 15 files and 1500 lines, so the orchestrator decomposes the work into three sub-issues before any brief (see Prerequisites & Dependencies).
- **Affected packages**: `Encina.Marten.GDPR` (startup validation, classifier, options, logging). `Encina.Marten` is read, not changed.
- **Provider category**: none of the matrix categories. Marten on PostgreSQL is the only event-sourcing provider (AGENTS.md section 5, event sourcing), so there is no provider matrix; the check is written once against Marten's public store options.
- **Not in scope** (decided in #1770): the Marten `Patch` path, raw `JsonDocument` upcasters, the `MartenReadModelRepository` expression guard, message-serializer and logger guards (issues #2456, #2457, #2462). No analyzer for application code (C1b: Encina documents the risk and does not audit user code).

---

## Design Choices

<details>
<summary><strong>1. Where the checks live — extend the startup validator with an internal inspector</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Extend `CryptoShreddingStartupValidationHostedService`, with the Marten-store inspection in a new internal inspector class** | Same pattern as ADR-034; one `CryptoShreddingConfigurationException` lists every issue; no new registration; keeps the hosted-service method small (CRAP gate) | One more internal class |
| **B) A second hosted service just for storage leaks** | Isolated | Two exceptions at startup, two orderings to reason about, new DI registration to keep complete (AGENTS.md section 3) |
| **C) A Marten `IConfigureOptions<StoreOptions>` that throws while the store is built** | Earliest possible failure | Runs before the application finishes configuring Marten, so schema registrations added later are missed; throws inside DI composition, not a startup validation |

### Chosen Option: **A — Extend the validator, inspection in an internal inspector**

### Rationale

- The issue states it ("Extend `CryptoShreddingStartupValidationHostedService`, same pattern as ADR-034") and ADR-034 already reports all issues once, on their declaring type, in one exception (event 8470).
- `StartingAsync` runs before any `StartAsync`, so a failure stops the host before Marten's async daemon processes an event.
- **Class:** Settled
- **Settled by:** issue #2455 "Proposed Solution"; ADR-034; AGENTS.md section 3 (registration completeness and fail-closed gates)

</details>

<details>
<summary><strong>2. Failure policy — fail start for duplicated fields, identity members and flat-table projections; warn for indexes</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Fail start for duplicated fields, identity and flat-table; warn for computed, GIN and full-text indexes** | Closes the three plaintext paths before data is written; indexes leak nothing (they index the token) | The flat-table check is coarse and can reject a legitimate projection (handled by the opt-out, choice 4) |
| **B) Fail start for duplicated fields and identity only** | No false positive from the unproven flat-table check | Leaves the High-severity flat-table path (row 7 of the spike) open |
| **C) Warnings only** | No startup breakage | Not fail closed |
| **D) Documentation only (today)** | No code | Excluded by rule (INV-005 of the spike register) |

### Chosen Option: **A — Fail start for the three paths, warn for indexes**

### Rationale

- The maintainer decided it for C4 on 2026-10-26/27: flat-table = fail start on the coarse over-approximation; indexes over crypto members (row 4) = startup warning. The earlier agent-written "settled by elimination" comment was withdrawn on 2026-10-25, and this decision replaces it.
- GDPR Art. 25 favours a fail-fast technical guard over documentation, and AGENTS.md section 3 requires compliance gates to fail closed.
- **Class:** Settled
- **Settled by:** maintainer decision C4 of the #1770 register (https://github.com/dlrivada/Encina/issues/1770#issuecomment-6436978234, 2026-10-26/27); AGENTS.md section 3; GDPR Art. 25

</details>

<details>
<summary><strong>3. What counts as a crypto member — direct <code>[CryptoShredded]</code> plus types that reach an owner</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Only members that carry `[CryptoShredded]`** | Simplest | Misses a strongly typed id or value object that wraps a crypto string |
| **B) Direct `[CryptoShredded]` members plus types that reach an owner (nested value objects)** | Reuses `CryptoShreddedPropertyClassifier.ReachesCryptoOwner`; already used by the existing scan (`IsCryptoRelevant`) | Needs the interpretation below to avoid rejecting unrelated duplicated fields |
| **C) B plus an assembly-wide scan for types of other assemblies** | No multi-assembly false negatives | Cost and noise with no observed false negative |

### Chosen Option: **B — Direct members plus types that reach an owner**

### Rationale

- The maintainer chose (b) for C4 and kept (c) only for the case where multi-assembly false negatives appear.
- Interpretation applied by this plan (verified by the Phase 1 prototype): a **duplicated field** is rejected when any member of its chain is a `[CryptoShredded]` property (the issue's wording); an **identity member** is rejected when it is `[CryptoShredded]` or its type reaches an owner; a **flat-table projection** is rejected when a handled event type is an owner or reaches one. Duplicating an unrelated scalar of a document that merely owns crypto fields is allowed, because the column holds no crypto value.
- **Class:** Settled
- **Settled by:** maintainer decision C4 of the #1770 register (https://github.com/dlrivada/Encina/issues/1770#issuecomment-6436978234)

</details>

<details>
<summary><strong>4. Opt-out — a per-member allow-list with a justification, never <code>ValidateOnStartup = false</code></strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Reuse `ValidateOnStartup = false`** | No new API | Turns off every check (infrastructure, contract modifier), not one member |
| **B) Per-type or per-member allow-list with a mandatory written justification, an audited startup record and rejection rules** | Narrow; matches GDPR Art. 5(2) accountability; fits factual (material-scope) non-applicability | New public options type |
| **C) Wildcards or namespace-wide exclusions** | Short configuration | Hides new leaks silently |

### Chosen Option: **B — Per-member allow-list**

### Rationale

- Maintainer decisions: C4 (c) of 2026-10-26/27 (per-type/per-member allow-list, never `ValidateOnStartup = false`) and C4 (b) of 2026-10-30. Requirements: per member, never wildcards; a mandatory written justification; an audited record at startup that appears in the compliance evidence; Encina rejects the opt-out when the member is marked `[PersonalData]`, `[PII]` or `[CryptoShredded]`, or is a free-text field.
- Consequence: a duplicated field over a `[CryptoShredded]` member can never be opted out (the member is itself marked). The opt-out serves flat-table projections and identity types that reach an owner without exposing a marked member.
- **Class:** Settled
- **Settled by:** maintainer decisions C4 (2026-10-26/27) and C4 = (b) (2026-10-30) recorded on #2455; GDPR Art. 2, 4(1) and 5(2)

</details>

<details>
<summary><strong>5. Reporting — reuse the problem flags, the issue record and the single exception</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Add flags to `CryptoShreddedPropertyProblems` (bits 27-29), describe them in `CryptoShreddedPropertyIssue.DescribeFlag`, report through `CryptoShreddingConfigurationException.MisconfiguredProperties`** | One reporting path, one log event (8470), the existing leak test (type and member names only) applies | The `int` enum has bit 30 left after three flags |
| **B) A new exception and issue type for storage leaks** | Unlimited reasons | A second failure shape for the host and for tests; duplicates the formatting |

### Chosen Option: **A — Add flags; widen the enum to `long` only if a fourth reason is ever needed**

### Rationale

- The enum, the record and the exception are the established vehicle ("Every issue is reported once, on its declaring type" in the validator remarks); pre-1.0 permits changing the underlying type later (AGENTS.md section 1).
- New flags: `DuplicatedFieldCarriesCryptoShredded`, `IdentityMemberCarriesCryptoShredded`, `FlatTableProjectionOverCryptoGraph`. The describing sentences name the fix and never a value (AGENTS.md section 3, `EncinaError.Message` rule).
- **Class:** Trivial
- **Settled by:** ADR-034 (single reporting path); AGENTS.md section 3 (fail closed, no values in messages); `CryptoShreddedPropertyIssue` remarks

</details>

<details>
<summary><strong>6. How the flat-table check reads a projection (needs the Phase 1 prototype)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Public API where Marten exposes it; when it does not expose the handled event types, treat every registered `FlatTableProjection` as reaching the crypto graph unless opted out** | Stable across Marten upgrades; fails closed | Rejects projections over unrelated events until the opt-out is configured |
| **B) Reflection over Marten internals (the member-to-column maps are internal) to read the exact event types and members** | Precise: no false positive | Breaks silently on a Marten upgrade; a reflection test must pin it |
| **C) Postpone the flat-table check and ship it as a warning** | No false positive risk | Row 7 stays unguarded; contradicts the maintainer's decision C4 (a) |

### Chosen Option: **A — Public API first, fail closed otherwise** (recommended, pending the maintainer)

### Rationale

- The maintainer fixed the outcome (fail start on the coarse over-approximation with a logged per-projection opt-out) and left the mechanism to this issue's prototype. Option A keeps the check on public API and fails closed when the API is not enough, which matches AGENTS.md section 3; option B wins when the number of legitimate flat-table projections in an application is large and each opt-out would be a burden, because it removes the false positives at the price of tracking Marten internals.
- **Class:** Dilemma

</details>

<details>
<summary><strong>7. Opt-out granularity for a flat-table projection</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One entry per projection type and per mapped event member, as the 2026-10-30 decision says ("per member, never wildcards")** | Consistent with the rule; the audited record names every excepted column source | If the member-to-column maps stay internal, Encina cannot verify the listed members exist, so the list is a declaration |
| **B) One entry per projection type with a mandatory justification and a mandatory non-empty list of declared members** | Simple to configure; the declaration is explicit | Weaker than A where Encina can enumerate the members |
| **C) One entry per projection type, justification only** | Least effort | A wildcard in practice; breaks the 2026-10-30 rule |

### Chosen Option: **A — Per projection and per member** (recommended, pending the maintainer)

### Rationale

- A follows the maintainer's rule literally. Option B wins in the scenario where the Phase 1 prototype proves that the members cannot be read at all and the entries would be unverifiable text.
- The rejection rules (marked `[PersonalData]`, `[PII]`, `[CryptoShredded]` or free text) apply to the declared member whenever it can be resolved by reflection on the event type.
- **Class:** Dilemma

</details>

<details>
<summary><strong>8. Where the audited opt-out record goes</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A structured startup log event per active opt-out (justification hashed or omitted; codes and names only) and the same list in the health-check data** | No new package dependency; visible to operators and to evidence tooling that reads logs | Logs are not an audit store; retention depends on the application |
| **B) Write to `IOperationAuditStore` (`Encina.Security.Audit`) when it is registered** | A real audit record in compliance evidence | `Encina.Marten.GDPR` does not reference `Encina.Security.Audit` today (new project reference, optional resolution at runtime) |
| **C) A public read-only `CryptoShreddingOptOutRegister` service the application or evidence tooling reads** | Explicit API, testable | New public surface; someone must persist it |

### Chosen Option: **A — Log event plus health-check data** (recommended, pending the maintainer)

### Rationale

- The maintainer requires "an audited record at startup that appears in the compliance evidence" but did not say which store. A is the cheapest that is verifiable today and adds no dependency; B wins when the application already runs `Encina.Security.Audit` and the auditor needs the opt-out in the audit trail itself (the cross-cutting matrix then marks it as integrated by configuration).
- **Class:** Dilemma

</details>

<details>
<summary><strong>9. How Encina recognises a free-text field in an opt-out</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Structural: a `string` member without a declared maximum length (`[MaxLength]`, `[StringLength]`, or a closed value set) is free text** | No word list; testable; the user fixes it by declaring a bound | A bounded string can still hold personal data (the justification and the other rejection rules still apply) |
| **B) Name convention: `Notes`, `Comments`, `Description`, `Remarks`, ... (case-insensitive)** | Matches how people name free text | Easy to evade by renaming; word list in two languages |
| **C) Never allow a `string` member in an opt-out** | Strictest | Rejects legitimate technical strings such as a status code |

### Chosen Option: **A — Structural rule** (recommended, pending the maintainer)

### Rationale

- The 2026-10-30 decision requires rejecting free text "because free text can always contain personal data" and gives the notes/comments string as the example, without a detection rule. A is deterministic and does not depend on language; B wins if the maintainer wants the rule to be visible to developers as a naming convention.
- **Class:** Dilemma

</details>

---

## Implementation Phases

Delivery split (AGENTS.md section 11): **PR 1** = Phases 2, 3 (core flags, opt-out model, classifier rules); **PR 2** = Phases 4, 6, 8 (inspector, validator wiring, log messages) with their tests; **PR 3** = Phase 5 (flat-table) with its tests and the prototype outcome. Phases 7, 9 and 10 are spread over the three; the documentation lands with PR 2 and is extended by PR 3. The orchestrator turns this into sub-issues before any brief.

### Phase 1: Prototype and Marten API Verification

> **Goal**: Prove, before any production code, what Marten 9.38 exposes through public API, and what the flat-table projection exposes. Output: a written note in the PR description and the unit tests that pin it.

<details>
<summary><strong>Tasks</strong></summary>

1. **Store enumeration prototype** (a throwaway test under `tests/Encina.UnitTests/Marten/GDPR/Nested/`, kept as a real test if green):
   - Build real `StoreOptions` with `Schema.For<T>().Duplicate(x => x.Member)`, a `[DuplicateField]` property, a document with a `[CryptoShredded]` identity member and a computed index.
   - Assert that `StoreOptions.AllKnownDocumentTypes()` returns the mapping and that `IDocumentType.DuplicatedFields`, `DuplicatedField.Members`, `IdMember` and `Indexes` expose what the inspector needs. No database is needed for the mapping inspection.
2. **Flat-table prototype**:
   - Register a `FlatTableProjection` in `StoreOptions.Projections` (or the events options) and find what is public: the registered projection instances, the table name, the handled event types, the mapped members.
   - Record the result: if the handled event types are public, choice 6 option A applies fully; otherwise the fallback (every registered `FlatTableProjection` is suspect unless opted out) applies.
3. **Decide nothing**: write the finding in the PR description. Choices 6 and 7 are the maintainer's.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT
Issue #2455 of the Encina repository. Package src/Encina.Marten.GDPR validates crypto-shredding at startup in CryptoShreddingStartupValidationHostedService. Marten is 9.38.0 (Directory.Packages.props). Spike #1770 claims that Marten's public store options enumerate duplicated fields, the identity member and indexes, and that FlatTableProjection maps are internal. This phase verifies those claims; it writes no production code.

TASK
1. In tests/Encina.UnitTests/Marten/GDPR/Nested/ add MartenStorageApiPrototypeTests.cs. Use new StoreOptions() (see StartupValidationHost.cs for how real StoreOptions are built in this folder).
2. Assert, with real StoreOptions and no database: AllKnownDocumentTypes() lists a document registered with Schema.For<T>().Duplicate(...); IDocumentType.DuplicatedFields[...].Members names the chain; IdMember is the identity property; Indexes lists a computed index.
3. Register a FlatTableProjection and assert what can be read about it through public API only (registered instances, handled event types, mapped members). Record each result as a comment in the test.
4. Report in your final message which API the later phases may use, and whether the handled event types of a FlatTableProjection are public.

KEY RULES
- Tests only; no change under src/. Assertions use Shouldly through Encina.Testing.Shouldly; never FluentAssertions.
- Never touch the main checkout; absolute paths in the worktree only.
- Do not decide the open design choices 6 and 7 of the plan; report the evidence.

REFERENCE FILES
- src/Encina.Marten.GDPR/CryptoShreddingStartupValidationHostedService.cs
- tests/Encina.UnitTests/Marten/GDPR/Nested/StartupValidationHost.cs
- docs/plans/crypto-shredding-storage-leak-validation-implementation-plan-2455.md (design choices 6 and 7)
```

</details>

### Phase 2: Core Models — Problem Flags and Opt-out Model

> **Goal**: Add the vocabulary the later phases report with and the opt-out type the options carry.

<details>
<summary><strong>Tasks</strong></summary>

1. **`CryptoShreddedPropertyProblems.cs`** (modify): add three flags after `SubjectIdPropertyIsCryptoShredded = 1 << 26`:
   - `DuplicatedFieldCarriesCryptoShredded = 1 << 27`
   - `IdentityMemberCarriesCryptoShredded = 1 << 28`
   - `FlatTableProjectionOverCryptoGraph = 1 << 29`
2. **`CryptoShreddedPropertyIssue.cs`** (modify): add the three cases to `DescribeFlag` (a single-question switch, already marked `crap-exempt`). Sentences name the fix: do not duplicate the member; do not use a crypto-shredded value as the document id; map the projection from non-crypto events or configure the opt-out. No value, no subject id.
3. **`CryptoShreddingStorageOptOut.cs`** (new, `src/Encina.Marten.GDPR/`): `public sealed record CryptoShreddingStorageOptOut(Type OwnerType, string MemberName, string Justification)`. XML docs state the rules of choice 4. A `ToString()` override prints type and member names only (AGENTS.md section 3 pattern for options).
4. **`CryptoShreddingOptions.cs`** (modify): `public List<CryptoShreddingStorageOptOut> StorageOptOuts { get; } = [];` documented as the only supported opt-out for the storage checks.
5. **`CryptoShreddingOptionsValidator.cs`** (modify): reject an entry with a blank justification, a blank member, a wildcard (`*`, `?`), a member that does not exist on `OwnerType`, and a member marked `[PersonalData]`, `[PII]` or `[CryptoShredded]` or recognised as free text (choice 9). The failure uses the existing options-validation path.
6. **`PublicAPI.Unshipped.txt`** (modify): add the new enum members, the record and the options property.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT
Issue #2455, package src/Encina.Marten.GDPR (net10.0, C# 14, nullable). The startup validator reports misconfigurations as CryptoShreddedPropertyIssue records carrying CryptoShreddedPropertyProblems flags, thrown in one CryptoShreddingConfigurationException. We add three problem flags and a per-member opt-out type.

TASK
1. Add the three flags to CryptoShreddedPropertyProblems (bits 27, 28, 29) and their sentences to CryptoShreddedPropertyIssue.DescribeFlag.
2. Create CryptoShreddingStorageOptOut (sealed record: Type OwnerType, string MemberName, string Justification) with XML docs and a ToString that prints names only.
3. Add List<CryptoShreddingStorageOptOut> StorageOptOuts to CryptoShreddingOptions.
4. Extend CryptoShreddingOptionsValidator with the opt-out rules (blank justification, blank member, wildcard, unknown member, member marked [PersonalData]/[PII]/[CryptoShredded] or free text per the plan's choice 9 as resolved by the maintainer).
5. Update PublicAPI.Unshipped.txt (delegate this to mechanical-fixer with exact lines).

KEY RULES
- Messages and logs carry type and member names only; never a value, a subject id or EncinaError.Message (AGENTS.md section 3).
- Wildcards are rejected; the opt-out never replaces ValidateOnStartup.
- No [Obsolete], no compatibility layer; XML docs on every public member; zero warnings.
- Keep every method at CRAP <= 10 (split rules into small functions as CryptoShreddedPropertyClassifier does).

REFERENCE FILES
- src/Encina.Marten.GDPR/CryptoShreddedPropertyProblems.cs
- src/Encina.Marten.GDPR/CryptoShreddedPropertyIssue.cs
- src/Encina.Marten.GDPR/CryptoShreddingOptions.cs
- src/Encina.Marten.GDPR/CryptoShreddingOptionsValidator.cs
- src/Encina.Security.PII/Attributes/PIIAttribute.cs
- src/Encina.Compliance.DataSubjectRights/Attributes/PersonalDataAttribute.cs
```

</details>

### Phase 3: Classifier Rules for Types Not Known at Startup

> **Goal**: Reject at first use what the startup scan of known document types cannot see (a document type registered later, or a type of another assembly), by adding reflection rules next to the existing ones.

<details>
<summary><strong>Tasks</strong></summary>

1. **`Metadata/CryptoShreddedPropertyClassifier.cs`** (modify): add two member rules to the `MemberRules` array, each a small private static function returning `CryptoShreddedPropertyProblems`:
   - `RuleDuplicateField`: the attributed property also carries Marten's `[DuplicateField]` attribute (`Marten.Schema.DuplicateFieldAttribute`) -> `DuplicatedFieldCarriesCryptoShredded`.
   - `RuleIdentityMember`: the attributed property is the document identity by Marten convention (`Id`/`id`, or an `[Identity]`-attributed member) -> `IdentityMemberCarriesCryptoShredded`. The convention check is the part the prototype of Phase 1 must confirm.
2. The contract modifier already uses the shape's issues, so the rejection happens on first use of an unseen type through `CryptoShreddingConfigurationException` (the existing path).
3. No new public API.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT
Issue #2455. CryptoShreddedPropertyClassifier (src/Encina.Marten.GDPR/Metadata) applies reflection rules per member; each rule returns CryptoShreddedPropertyProblems and the results are combined. The Phase 2 flags exist.

TASK
1. Add RuleDuplicateField and RuleIdentityMember to MemberRules.
2. RuleDuplicateField returns DuplicatedFieldCarriesCryptoShredded when the property has Marten's [DuplicateField].
3. RuleIdentityMember returns IdentityMemberCarriesCryptoShredded when Marten would use the property as the identity (verify the exact convention in Phase 1's notes).
4. Extend tests/Encina.UnitTests/Marten/GDPR/Nested/CryptoShreddedPropertyClassifierTests.cs with a case per rule and one case where an unrelated duplicated field of a document that owns crypto fields is allowed.

KEY RULES
- Every rule is a small function; keep CRAP <= 10 for each changed method.
- Classifier results are cached per type (Shapes); use ResetForTests in tests only.
- No new public API; messages carry names only.

REFERENCE FILES
- src/Encina.Marten.GDPR/Metadata/CryptoShreddedPropertyClassifier.cs
- src/Encina.Marten.GDPR/Metadata/CryptoShreddedContractRules.cs
- tests/Encina.UnitTests/Marten/GDPR/Nested/CryptoShreddedPropertyClassifierTests.cs
```

</details>

### Phase 4: Storage Inspector — Duplicated Fields, Identity Members and Indexes

> **Goal**: Inspect every document type Marten knows at startup and report the three plaintext paths plus the index warnings.

<details>
<summary><strong>Tasks</strong></summary>

1. **`Metadata/MartenStorageLeakInspector.cs`** (new, `internal static class` or `internal sealed class`, namespace `Encina.Marten.GDPR`):
   - `IReadOnlyList<CryptoShreddedPropertyIssue> Inspect(IReadOnlyStoreOptions options, IReadOnlyCollection<CryptoShreddingStorageOptOut> optOuts)` walks `options.AllKnownDocumentTypes()`.
   - For each `IDocumentType`: for every entry of `DuplicatedFields`, resolve each member of the chain (`DuplicatedField.Members`) to a `PropertyInfo` and report `DuplicatedFieldCarriesCryptoShredded` when any is `[CryptoShredded]`; for `IdMember`, report `IdentityMemberCarriesCryptoShredded` when the member is `[CryptoShredded]` or its type satisfies `CryptoShreddedPropertyClassifier.ReachesCryptoOwner`; for `Indexes`, collect computed, GIN and full-text indexes whose expressions touch a crypto member for the warning (not an issue).
   - Returns the issues plus the warning list (a small result record, internal).
   - Skips an entry listed in the opt-out set (choice 4); opt-outs on a marked member never reach here because the options validator rejected them.
2. Split into small methods (`InspectDuplicatedFields`, `InspectIdentity`, `FindIndexWarnings`) so each stays under CRAP 10.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT
Issue #2455. Phase 1 established which Marten 9.38 members are public: IReadOnlyStoreOptions.AllKnownDocumentTypes(), IDocumentType.DuplicatedFields, DuplicatedField.Members, IdMember, Indexes. Phase 2 added the problem flags and CryptoShreddingStorageOptOut.

TASK
1. Create src/Encina.Marten.GDPR/Metadata/MartenStorageLeakInspector.cs as described in the plan (Phase 4 tasks).
2. Report issues as CryptoShreddedPropertyIssue(declaringTypeName, memberName, flags) using CryptoShreddedPropertyClassifier.TypeName for names.
3. Return index warnings separately (type name and index kind only).
4. Unit-test it with real StoreOptions: duplicated field over a crypto member (rejected), duplicated field over an unrelated member (allowed), crypto identity member (rejected), identity value object that reaches an owner (rejected), computed index (warning), opted-out entry (skipped).

KEY RULES
- Names only in issues and warnings: never a value (AGENTS.md section 3).
- The inspector throws nothing; the hosted service decides (fail closed lives in the caller).
- Keep every method CRAP <= 10; unit coverage target 95% (see the plan's testing phase).
- Do not reflect into Marten internals here (choice 6 concerns only the flat-table phase).

REFERENCE FILES
- src/Encina.Marten.GDPR/Metadata/CryptoShreddedPropertyClassifier.cs
- src/Encina.Marten.GDPR/CryptoShreddedPropertyIssue.cs
- tests/Encina.UnitTests/Marten/GDPR/Nested/StartupValidationHost.cs
```

</details>

### Phase 5: Flat-Table Projection Inspector

> **Goal**: Reject a `FlatTableProjection` whose handled event types reach a crypto owner, according to the maintainer's resolution of choices 6 and 7.

<details>
<summary><strong>Tasks</strong></summary>

1. **`Metadata/FlatTableProjectionInspector.cs`** (new, internal): enumerate the registered `FlatTableProjection` instances of the store options (the access path is the outcome of Phase 1); for each, determine the handled event types by the approach chosen in choice 6; report `FlatTableProjectionOverCryptoGraph` on the projection type name when an event type is an owner (`CryptoShreddedPropertyClassifier.GetShape(type).IsOwner`) or reaches one (`ReachesCryptoOwner`).
2. Apply the opt-out entries per projection (and per member when choice 7 resolves to A or B).
3. Unit tests: a projection over a crypto event (rejected), over a non-crypto event (allowed), with an opt-out (allowed and audited), and the fallback behaviour if the handled event types are not public.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT
Issue #2455. Row 7 of spike #1770: FlatTableProjection column maps read the decrypted in-memory event and write plaintext columns. The member-to-column maps are internal in Marten 9.38; Phase 1 recorded what is public. The maintainer decided: fail start on the coarse over-approximation with a logged per-projection opt-out (decision C4 (a)). The mechanism (public API first / reflection) and the opt-out granularity are Dilemmas 6 and 7 of the plan, answered by the maintainer before this phase starts.

TASK
1. Create src/Encina.Marten.GDPR/Metadata/FlatTableProjectionInspector.cs implementing the maintainer's answer to choice 6.
2. Report FlatTableProjectionOverCryptoGraph on the projection type when any handled event type is an owner or reaches one.
3. Honour CryptoShreddingOptions.StorageOptOuts according to the answer to choice 7.
4. If reflection on Marten internals is the chosen mechanism, add a test that fails loudly when a Marten upgrade changes the shape.

KEY RULES
- Fail closed: an event type that cannot be inspected counts as reaching the graph unless opted out.
- Names only in issues; never a value.
- Keep every method CRAP <= 10.
- Do not decide choices 6 or 7 yourself: if the plan's Maintainer Decisions section lacks the answer, stop and report.

REFERENCE FILES
- src/Encina.Marten.GDPR/Metadata/MartenStorageLeakInspector.cs (Phase 4)
- src/Encina.Marten.GDPR/Metadata/CryptoShreddedPropertyClassifier.cs
- the Phase 1 test note in the PR description
```

</details>

### Phase 6: Startup Integration, Configuration and DI

> **Goal**: Call the inspectors from the validator and keep registration complete.

<details>
<summary><strong>Tasks</strong></summary>

1. **`CryptoShreddingStartupValidationHostedService.cs`** (modify): in `StartingAsync`, after `ValidateTypes`, call a new private `CheckStorage(store, scan)`; it runs the Phase 4 and 5 inspectors, adds their issues to the `ScanState`, logs the index warnings (event 8485) and the active opt-outs (event 8486), and lets the existing `ThrowIfIssues(scan)` raise one `CryptoShreddingConfigurationException` listing everything. The existing early-out for `ValidateOnStartup = false` remains the only global switch.
2. **`ServiceCollectionExtensions.cs`**: no change expected (the options and the hosted service are already registered); a DI test (builder with `ValidateOnBuild` and `ValidateScopes`) proves it for the new options property.
3. Update the class remarks of the hosted service and the `ValidateOnStartup` XML docs to describe the new checks.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
CONTEXT
Issue #2455. CryptoShreddingStartupValidationHostedService.StartingAsync runs RequireWrappedSerializer, VerifyContractModifierInstalled, CheckInfrastructure and ValidateTypes, then logs completion. The inspectors of Phases 4 and 5 exist.

TASK
1. Add CheckStorage(IDocumentStore store, ScanState scan) and call it between ValidateTypes' scan and ThrowIfIssues (restructure so the new issues join the same ScanState and one exception lists all).
2. Log index warnings (8485) and active opt-outs (8486) through CryptoShreddingLog; log rejected opt-outs (8487) before the exception.
3. Update XML docs of the class and of CryptoShreddingOptions.ValidateOnStartup.
4. Extend CryptoShreddingStartupValidationHostedServiceTests with StartupValidationHost: a duplicated crypto field stops the host; an unrelated duplicated field does not; an opt-out is logged.
5. Add a DI test that builds the provider with ValidateOnBuild and ValidateScopes.

KEY RULES
- The host fails closed; ValidateOnStartup = false is not an opt-out for individual members.
- No EncinaError.Message or exception message in logs (use codes, type names; ForLogging() for exceptions).
- Methods keep CRAP <= 10: extract helpers rather than growing StartingAsync.

REFERENCE FILES
- src/Encina.Marten.GDPR/CryptoShreddingStartupValidationHostedService.cs
- src/Encina.Marten.GDPR/Diagnostics/CryptoShreddingLog.cs
- tests/Encina.UnitTests/Marten/GDPR/Nested/CryptoShreddingStartupValidationHostedServiceTests.cs
```

</details>

### Phase 7: Cross-Cutting Integration

> **Goal**: Apply the matrix of this plan: structured logging (Phase 8), validation (Phases 2-6) and the audit record of opt-outs.

<details>
<summary><strong>Tasks</strong></summary>

1. **Audit record of opt-outs** (mechanism per choice 8): log event 8486 per active opt-out (owner type name, member name, justification length only, never the free text if the maintainer prefers; the justification itself is operator-authored configuration, not personal data, but is kept out of logs by default) and the list in `CryptoShreddingHealthCheck` data. If the maintainer chooses option B, an optional `IOperationAuditStore` write is added and the project reference is reviewed.
2. **Matrix items marked N/A** (see the matrix below): no work, one sentence each.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
CONTEXT
Issue #2455. The maintainer requires an audited record at startup of every active storage opt-out that appears in the compliance evidence (decision of 2026-10-30). The mechanism is Dilemma 8 of the plan, answered by the maintainer before this phase starts.

TASK
1. Implement the maintainer's answer to choice 8.
2. If it is the log event plus health-check data: include owner type name, member name and opt-out count in CryptoShreddingHealthCheck data; never a value.
3. Test the record: opt-out configured -> event emitted and health data lists it.

KEY RULES
- Names and counts only; never EncinaError.Message.
- Health check pattern: DefaultName const, Tags static array, scoped resolution via IServiceProvider.CreateScope().
- If the answer needs a new project reference, stop and report; do not add it without the maintainer.

REFERENCE FILES
- src/Encina.Marten.GDPR/Health/CryptoShreddingHealthCheck.cs
- src/Encina.Marten.GDPR/Diagnostics/CryptoShreddingLog.cs
- src/Encina.Security.Audit/Abstractions/IOperationAuditStore.cs
```

</details>

### Phase 8: Observability

> **Goal**: Structured logging for the new checks; no new metric or span (one-time startup validation).

<details>
<summary><strong>Tasks</strong></summary>

1. **`Diagnostics/CryptoShreddingLog.cs`** (modify): add four `[LoggerMessage]` methods in the already registered range `EventIdRanges.MartenGDPRCryptoShredding` (8450-8499; used 8450-8484):
   - 8485 `IndexOverCryptoMember` (Warning): `DocumentType`, `IndexKind`.
   - 8486 `StorageOptOutActive` (Warning): `OwnerType`, `MemberName`.
   - 8487 `StorageOptOutRejected` (Error): `OwnerType`, `MemberName`, `Reason` (a problem name).
   - 8488 `StorageInspectionCompleted` (Debug): `DocumentTypeCount`, `ProjectionCount`.
2. **No `EventIdRanges.cs` change** (the range exists) and no change to `EncinaEventIdAllocationTests` `AssemblyRanges` (the assembly is already mapped). Add the new event ids to the XML doc of the class (`Event IDs: 8470-8488`).
3. **`PublicAPI.Unshipped.txt`**: nothing for these (internal extension methods).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```
CONTEXT
Issue #2455. src/Encina.Marten.GDPR/Diagnostics/CryptoShreddingLog.cs uses the [LoggerMessage] source generator; its XML doc names the range. EventIdRanges.MartenGDPRCryptoShredding = (8450, 8499) already exists; 8450-8484 are used.

TASK
1. Add the four log methods with EventIds 8485, 8486, 8487, 8488 exactly as listed in the plan (packed sequentially, no gaps).
2. Update the class XML doc to "Event IDs: 8470-8488 (see EventIdRanges.MartenGDPRCryptoShredding)".
3. Run the architecture test for event ids: tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs.

KEY RULES
- ADR-021: EventIds only inside the registered range; never sparse; never a value, subject id or EncinaError.Message in a template.
- CA1848 is satisfied by the source generator.

REFERENCE FILES
- src/Encina/Diagnostics/EventIdRanges.cs
- src/Encina.Marten.GDPR/Diagnostics/CryptoShreddingLog.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

### Phase 9: Testing

> **Goal**: Per-flag coverage of the touched files reaches its targets; tests execute real package code.

<details>
<summary><strong>Tasks</strong></summary>

1. **UnitTests** (required): `MartenStorageLeakInspectorTests` (new), `CryptoShreddedPropertyClassifierTests` (extend), `CryptoShreddingOptionsValidatorTests` (extend), `CryptoShreddingStartupValidationHostedServiceTests` (extend), `FlatTableProjectionInspectorTests` (new, PR 3), `CryptoShreddingStorageOptOutTests` (new: `ToString` shows no justification text if the maintainer chooses so, rules). Real validator and real Marten `StoreOptions` instantiated; `CryptoShreddedPropertyIssue.Describe` messages carry names only.
2. **GuardTests** (required): `CryptoShreddingStorageOptOut` and the new options members (`ArgumentNullException`), in `tests/Encina.GuardTests/Marten/GDPR/`.
3. **ContractTests** (required, the public API grows): the opt-out contract (blank justification and wildcard rejected, member must exist) in `tests/Encina.ContractTests/Marten/GDPR/`.
4. **PropertyTests**: not required; the rules are enumerations over mappings, not complex logic. Property targets in the manifest apply to other files.
5. **IntegrationTests** (required, real PostgreSQL): `CryptoShreddingStorageLeakIntegrationTests` in `tests/Encina.IntegrationTests/Infrastructure/Marten/GDPR/` with `[Collection(MartenCollection.Name)]`, `[Trait("Category", "Integration")]`, `[Trait("Database", "PostgreSQL")]`: a real `AddMarten` store with a duplicated crypto field, a crypto identity member and a flat-table projection over crypto events; each stops the host; a compliant store starts.
6. **Load and Benchmark**: not required (startup only); no `.md` justification file because no folder is created for them by this feature.
7. **Per-file coverage targets** (`.github/coverage-manifest/Encina.Marten.GDPR.json`, written by `mechanical-fixer`; targets never go down):

| File | unit | guard | contract | Justification |
|------|:----:|:-----:|:--------:|---------------|
| `Metadata/MartenStorageLeakInspector.cs` (new) | 95 | 0 | 0 | One unit test per rule on real StoreOptions; guard 0 because it is internal with no public parameters |
| `Metadata/FlatTableProjectionInspector.cs` (new) | 90 | 0 | 0 | The uninspectable-event branch depends on Marten internals; covered by the fail-closed test |
| `CryptoShreddingStorageOptOut.cs` (new) | 100 | 100 | 90 | A record with a `ToString` and rules; guards and contract tests assert each rule |
| `CryptoShreddingStartupValidationHostedService.cs` | >= 94 (existing) | 0 | 0 | The existing target must not go down; new branches are unit tested |
| `Metadata/CryptoShreddedPropertyClassifier.cs` | 90 | 0 | 0 | Two new rule functions, each tested in both directions |
| `CryptoShreddingOptionsValidator.cs` | >= existing | >= existing | 0 | Opt-out rules extend an already tested validator |

   Measure with the commands of AGENTS.md section 9 (each flag's test project, then `coverage-report.cs` and `--check-justifications`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 9</strong></summary>

```
CONTEXT
Issue #2455. Test projects are consolidated under tests/ (Encina.UnitTests, GuardTests, ContractTests, IntegrationTests). Assertions use Shouldly through Encina.Testing.Shouldly. Integration tests use the shared Marten collection fixture.

TASK
1. Write the unit, guard, contract and integration tests listed in the plan's Phase 9 tasks, executing real package code (StoreOptions, the real validator, the real hosted service through StartupValidationHost).
2. Integration tests: [Collection(MartenCollection.Name)], [Trait("Category","Integration")], [Trait("Database","PostgreSQL")]; inject the fixture; never create a per-class fixture.
3. Measure per-flag coverage of the touched files and report it against the plan's per-file targets.

KEY RULES
- No reflection-only tests; no Thread.Sleep; no FluentAssertions.
- A test never swallows an exception thrown by the code it drives.
- Targets never go down; new/changed files get manifest entries via mechanical-fixer.

REFERENCE FILES
- tests/Encina.UnitTests/Marten/GDPR/Nested/StartupValidationHost.cs
- tests/Encina.UnitTests/Marten/GDPR/Nested/CryptoShreddingStartupValidationHostedServiceTests.cs
- tests/Encina.IntegrationTests/Infrastructure/Marten/GDPR/CryptoShreddingNestedIntegrationTests.cs
- .github/coverage-manifest/Encina.Marten.GDPR.json
```

</details>

### Phase 10: Documentation and Finalization

> **Goal**: Documentation states what Encina now enforces and what stays the user's responsibility.

<details>
<summary><strong>Tasks</strong></summary>

1. **XML docs** on every new public member (`CryptoShreddingStorageOptOut`, `StorageOptOuts`, the three enum members).
2. **`docs/features/crypto-shredding.md`** (via `docs-writer`): replace the line 808 limitation text for duplicated fields, identity and flat-table with the enforced behaviour; keep the paragraph for `Patch` (still documented). Add the opt-out configuration example and the rule list. Figures come from covref markers, never typed by hand.
3. **ADR**: the maintainer decision C9a asks for one new ADR amending ADR-034 (the guards of C2d and C4-C8, the reversed stance of ADR-034 line 47 for Encina-owned paths, the C1b scope principle) plus a one-line pointer in ADR-034. Coordinate with the sibling issues #2456, #2457 and #2462: the first PR that lands creates the ADR (next free number at that time; 061 and 062 are reserved), the others append their section.
4. **`src/Encina.Marten.GDPR/README.md`**: update the limits section.
5. **`changelog.d/2455-crypto-shredding-storage-leak-validation.added.md`** (via `mechanical-fixer`): one bullet naming the new startup rejections and the opt-out. Run `dotnet run --file .github/scripts/changelog-fragments.cs -- --check`.
6. **`PublicAPI.Unshipped.txt`** and the **coverage manifest** entries (via `mechanical-fixer`, exact lines decided by the worker).
7. **`docs/INVENTORY.md`** only if the file list is tracked there; **ROADMAP.md** no change (hardening within v0.14.0).
8. **Verification**: `dotnet build --configuration Release` with 0 warnings; the unit, guard, contract and integration test projects green; per-flag coverage reported; `check-plan` without `-Draft` passes once the maintainer has answered choices 6-9.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 10</strong></summary>

```
CONTEXT
Issue #2455, package Encina.Marten.GDPR. The behavior is implemented in Phases 2-8. Documentation must say what is enforced, what the opt-out requires, and what remains the user's responsibility (Patch, raw JsonDocument upcasters, application code).

TASK
1. Finish XML docs of the new public members.
2. Hand the page updates to docs-writer: docs/features/crypto-shredding.md (limitation at line 808), the package README, the ADR section per the plan.
3. Hand the changelog fragment, the PublicAPI.Unshipped.txt lines and the coverage-manifest entries to mechanical-fixer with exact lines.
4. Run build, the four test projects and the changelog check; report per-flag coverage of the touched files against the plan's targets.

KEY RULES
- Documentation never types coverage figures by hand (covref markers).
- English only; no emojis; no private application names (write "the reference application" where needed).
- Never edit CHANGELOG.md; the fragment goes in changelog.d/.

REFERENCE FILES
- docs/features/crypto-shredding.md
- docs/architecture/adr/034-crypto-shredding-through-the-stj-contract.md
- changelog.d/README.md
- .claude/skills/encina-docs/SKILL.md
```

</details>

---

## Research

### Relevant Standards and Specifications

| Reference | Requirement | Relevance to this feature |
|-----------|-------------|---------------------------|
| GDPR Art. 5(1)(f) | Integrity and confidentiality | A plaintext copy of a crypto-shredded value in a column breaks the confidentiality guarantee of the encryption |
| GDPR Art. 5(2) | Accountability | The opt-out is justified in writing and audited, so the controller can demonstrate why a member is excepted |
| GDPR Art. 17 | Erasure | Deleting the key must erase every copy; the column copies survive it and the erasure would report success |
| GDPR Art. 25 | Data protection by design and by default | A fail-fast startup guard beats documentation |
| GDPR Art. 32 | Security of processing | Appropriate technical measures; the guard closes structural bypasses of encryption |
| GDPR Art. 2 and 4(1) | Material scope | Factual non-applicability is the only reason an opt-out is admitted (decision of 2026-10-30) |
| SPEC-002 DEC-006 | Compliance gates fail closed | Missing or uninspectable context rejects; the opt-out is explicit and logged |
| ADR-034 | Crypto-shredding runs through the STJ contract | The pattern extended; line 47 "documented, not enforced" is reversed for Encina-owned paths by the new ADR |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|----------------------|
| `CryptoShreddingStartupValidationHostedService` | `src/Encina.Marten.GDPR/CryptoShreddingStartupValidationHostedService.cs` | Host of the new checks (`StartingAsync`, `ScanState`, `ThrowIfIssues`) |
| `CryptoShreddedPropertyClassifier` | `src/Encina.Marten.GDPR/Metadata/CryptoShreddedPropertyClassifier.cs` | `GetShape`, `ReachesCryptoOwner`, `MemberRules` for the two new reflection rules |
| `CryptoShreddedPropertyProblems` and `CryptoShreddedPropertyIssue` | `src/Encina.Marten.GDPR/` | Flags and issue record for the new reasons; `DescribeFlag` for the sentences |
| `CryptoShreddingConfigurationException` | `src/Encina.Marten.GDPR/CryptoShreddingConfigurationException.cs` | The single exception that lists every issue |
| `CryptoShreddingOptions` and `CryptoShreddingOptionsValidator` | `src/Encina.Marten.GDPR/` | Carry and validate the opt-out entries |
| `CryptoShreddingLog` | `src/Encina.Marten.GDPR/Diagnostics/CryptoShreddingLog.cs` | `[LoggerMessage]` home for events 8485-8488 |
| `CryptoShreddingHealthCheck` | `src/Encina.Marten.GDPR/Health/CryptoShreddingHealthCheck.cs` | Carries the list of active opt-outs (choice 8 option A) |
| Marten `IReadOnlyStoreOptions.AllKnownDocumentTypes`, `IDocumentType.DuplicatedFields`, `IdMember`, `Indexes` | Marten 9.38.0 (`Directory.Packages.props`) | The public enumeration the inspector reads; confirmed by the Phase 1 prototype |
| `StartupValidationHost` | `tests/Encina.UnitTests/Marten/GDPR/Nested/StartupValidationHost.cs` | Builds a provider with real `StoreOptions` for the unit tests |
| `MartenCollection` fixture | `tests/Encina.IntegrationTests/Infrastructure/Marten/Fixtures/` | Shared PostgreSQL container for the integration tests |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Marten.GDPR` crypto-shredding (`EventIdRanges.MartenGDPRCryptoShredding`) | 8450-8499 | Existing registered range; 8450-8484 used |
| **This feature** | **8485-8488** | **8485 index warning, 8486 opt-out active, 8487 opt-out rejected, 8488 inspection completed; 8489-8499 stay free. No new range to register** |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Production, modified | 7 | Hosted service, classifier, problems enum, issue record, options, options validator, `CryptoShreddingLog.cs` (and the health check if choice 8 is A) |
| Production, new | 3 | `MartenStorageLeakInspector`, `FlatTableProjectionInspector`, `CryptoShreddingStorageOptOut` |
| Unit tests | 5 | 2 new, 3 extended |
| Guard and contract tests | 2 | One each |
| Integration tests | 1 | PostgreSQL through the Marten collection |
| Documentation | 3 | Feature page, package README, ADR |
| Metadata | 3 | `PublicAPI.Unshipped.txt`, coverage manifest, changelog fragment |
| **Total** | **~24** | Over the 15-file limit: three pull requests (see the delivery split) |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
You are implementing issue #2455 for Encina: the crypto-shredding startup validation rejects Marten duplicated fields, identity members and flat-table projections over [CryptoShredded] data.

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library, pre-1.0: no backward compatibility, best solution always.
- Package: src/Encina.Marten.GDPR (existing). Marten 9.38.0. Marten on PostgreSQL is the only event-sourcing provider; no database-provider matrix.
- Startup validation is CryptoShreddingStartupValidationHostedService (IHostedLifecycleService.StartingAsync); issues are CryptoShreddedPropertyIssue records with CryptoShreddedPropertyProblems flags, thrown together in one CryptoShreddingConfigurationException.
- Maintainer decisions bind: fail start for duplicated fields, identity, flat-table; warn for indexes; opt-out only as a per-member allow-list with a written justification and an audited startup record, never ValidateOnStartup = false; the opt-out is rejected for members marked [PersonalData], [PII] or [CryptoShredded] and for free text.
- Open choices 6-9 of the plan are the maintainer's; do not decide them.

IMPLEMENTATION OVERVIEW:
Phase 1: prototype Marten's public API (AllKnownDocumentTypes, DuplicatedFields, IdMember, Indexes; FlatTableProjection)
Phase 2: three problem flags, CryptoShreddingStorageOptOut, options property and validator rules
Phase 3: two classifier rules for types not known at startup
Phase 4: MartenStorageLeakInspector (duplicated fields, identity, index warnings)
Phase 5: FlatTableProjectionInspector (after choices 6 and 7)
Phase 6: wire into the hosted service; DI test with ValidateOnBuild and ValidateScopes
Phase 7: audited opt-out record (after choice 8)
Phase 8: log events 8485-8488 in the existing range 8450-8499
Phase 9: unit, guard, contract, integration tests with per-file targets
Phase 10: docs (docs-writer), changelog fragment, PublicAPI and manifest entries (mechanical-fixer)

KEY PATTERNS:
- Messages and logs carry type and member names only; never a value, a subject id or EncinaError.Message (AGENTS.md section 3).
- Methods keep CRAP <= 10; split rules into small functions.
- Fail closed: what cannot be inspected counts as a leak unless opted out.
- Integration tests use [Collection(MartenCollection.Name)] shared fixtures.
- Assertions use Shouldly through Encina.Testing.Shouldly.
- Delivery in three pull requests of at most 15 files and 1500 lines each.

REFERENCE FILES:
- src/Encina.Marten.GDPR/CryptoShreddingStartupValidationHostedService.cs
- src/Encina.Marten.GDPR/Metadata/CryptoShreddedPropertyClassifier.cs
- src/Encina.Marten.GDPR/CryptoShreddedPropertyProblems.cs
- src/Encina.Marten.GDPR/Diagnostics/CryptoShreddingLog.cs
- tests/Encina.UnitTests/Marten/GDPR/Nested/StartupValidationHost.cs
- docs/architecture/adr/034-crypto-shredding-through-the-stj-contract.md
- https://github.com/dlrivada/Encina/issues/1770#issuecomment-6436978234
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | One-time startup validation; no read path to cache |
| 2 | OpenTelemetry | ❌ | No operation worth a span or metric; the outcome is a startup exception and log events |
| 3 | Structured Logging | ✅ | Four `[LoggerMessage]` methods, EventIds 8485-8488 inside the registered range 8450-8499 (Phase 8) |
| 4 | Health Checks | ✅ | `CryptoShreddingHealthCheck` lists the active opt-outs if choice 8 is option A; the host otherwise refuses to start |
| 5 | Validation | ✅ | It is itself a configuration validation, plus the options validator for opt-out entries (Phases 2, 6) |
| 6 | Resilience | ❌ | No external call |
| 7 | Distributed Locks | ❌ | No shared state |
| 8 | Transactions | ❌ | No writes |
| 9 | Idempotency | ❌ | No messages; the check is repeatable |
| 10 | Multi-Tenancy | ❌ | Configuration level, not tenant data |
| 11 | Module Isolation | ❌ | Not module scoped |
| 12 | Audit Trail | ✅ | The maintainer's decision of 2026-10-30 requires an audited record of each active opt-out; mechanism per choice 8 (Phase 7) |

---

## Prerequisites & Dependencies

- **Decomposition** (AGENTS.md section 11): about 25 files exceed the 15-file limit of one pull request; the orchestrator opens three sub-issues (core flags and opt-out; inspector, wiring and logging; flat-table) before any worker brief.
- **Prototype first**: Phase 1 decides what is public in Marten 9.38; choices 6 and 7 depend on it.
- **Sibling issues** of the #1770 register: #2456 (message serializer), #2457 (enricher values), #2462 (logger). They share the new ADR of decision C9a; whichever lands first creates it.
- **Open crypto-shredding items** #2423 and #2433 touch the same package but not these files.
- No new NuGet package and no new project reference, unless choice 8 resolves to option B.

---

## Next Steps

1. Review and approve this plan; publish its link as a comment on issue #2455.
2. When work on the issue starts, the orchestrator re-checks every choice against today's code and asks the maintainer to answer Dilemmas 6, 7, 8 and 9, then records the answers in a `## Maintainer Decisions` section.
3. Decompose into three sub-issues and write the briefs; run Phase 1 first.
4. Each phase is a self-contained commit; the final commit of each pull request references the issue.
