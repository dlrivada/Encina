# Implementation Plan: Crypto-shredding of string collections and a Marten.Newtonsoft adapter

> **Issue**: [#1771](https://github.com/dlrivada/Encina/issues/1771)
> **Type**: Feature
> **Complexity**: High (5 phases, 3 pull requests of 15, 15 and 7 files)
> **Estimated Scope**: about 900 lines of production code, about 1400 lines of tests
> **Milestone**: v0.17.0 - Compliance Lifecycle (the decision comment of 2026-10-17 schedules the work by priority after the 1.0 line)

---

## Summary

`Encina.Marten.GDPR` encrypts a `[CryptoShredded]` string property at any depth through a System.Text.Json contract modifier (#1698): `CryptoShreddingContractModifier` wraps `JsonPropertyInfo.Get` to return a v2 token (`cs2:{version}:{base64url}` or the tombstone `cs2:erased`, `CryptoShreddingToken`), and `JsonTypeInfo.OnDeserialized` decrypts through `CryptoShreddingEngine.OnOwnerDeserialized`. Two limits remain, both recorded as follow-ups of #1698:

- A `[CryptoShredded]` string collection is rejected at startup: `RuleString` in `CryptoShreddedPropertyClassifier` (`src/Encina.Marten.GDPR/Metadata/CryptoShreddedPropertyClassifier.cs`) returns `CryptoShreddedPropertyProblems.NotString` for any property whose type is not `string`.
- Any serializer other than Marten's `SystemTextJsonSerializer` is rejected: `CryptoShredderSerializerFactory.Apply` throws `CryptoShreddingConfigurationException(SerializerNotSupported)`. No project references a Marten Newtonsoft package (`Newtonsoft.Json` 13.0.4 is in `Directory.Packages.props` only for `Encina.Hangfire`).

Both are fail closed today and stay so until the feature exists. Re-checked against `origin/main` (db1e7f688): the engine is serializer-neutral where it matters (the token, the cipher with associated data `encina:cs2:{subjectId}:{version}`, the per-call frame, the key access and the forgotten-subject handling do not depend on System.Text.Json), but the classifier rules and `CryptoShredderSerializer` (staging buffers, resolver installation, canary) are System.Text.Json specific.

The plan has two parts. Part A (pull request 1) adds the four collection shapes of the issue to the System.Text.Json path: each element is its own v2 token, bound to the declaring object's subject like a scalar. Part B (pull requests 2 and 3) adds a separate Newtonsoft serializer wrapper over the same engine, with its own contract resolver and rules; its first task is a spike that verifies the Marten package and type names, because nothing in the repository references them.

**Affected packages:** `Encina.Marten.GDPR` only (`Directory.Packages.props` gains one `PackageVersion` in Part B).

**Provider category**: none for the feature itself. Event sourcing (Marten on PostgreSQL) is its own category; integration tests run against Marten through the shared collection fixture, as AGENTS.md section 3 requires for event-sourced compliance modules.

**Sibling plans (same package)**: #2455 and #2457 (merged plans) claim EventIds 8485-8491 of the crypto-shredding range; this plan starts at 8492. #1699 (key provider races) is a prerequisite of #1698's design and applies to this one unchanged.

---

## Design Choices

<details>
<summary><strong>1. Supported collection shapes</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Exactly the shapes of the issue: `string[]`, `List<string>`, `IReadOnlyList<string>`, and the values of `Dictionary<string, string>`; every other collection type is rejected at startup with a new `UnsupportedStringCollection` problem** | Each shape has one known runtime type under System.Text.Json (`string[]`, `List<string>`, `List<string>` for `IReadOnlyList<string>`, `Dictionary<string,string>`), so reads rebuild exactly the type the property declares | `HashSet<string>`, `ImmutableArray<string>`, `IEnumerable<string>`, `IReadOnlyDictionary<string,string>` stay unsupported |
| **B) Any `IEnumerable<string>`** | Wider | A set loses its equality semantics when each element is a random-nonce token; a lazy `IEnumerable` cannot be rebuilt; the runtime type is unknown |

### Chosen Option: **A - the four shapes of the issue**

### Rationale

- **Class:** Settled
- **Settled by:** the issue body of #1771 (the four shapes and "decrypt by rebuilding the collection for the concrete collection type") and its decision comment of 2026-10-17 ("implement both as proposed"); the current rejection stays as the fail-closed behavior for everything else. Option B is not what the issue proposes.

</details>

<details>
<summary><strong>2. How the collection is decrypted on read</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Rebuild a new collection of the declared concrete type from the decrypted elements and set it through the compiled setter, after `OnDeserialized`** | The proposal of the issue; works for `init`-only setters through the existing compiled setter of `CryptoShreddedField`; the existing `NoSetter` rule keeps applying | One allocation per collection read |
| **B) Mutate the deserialized collection in place** | No allocation | Depends on the runtime type being mutable (not guaranteed for future shapes); no use of the setter rule |

### Chosen Option: **A - rebuild and set**

### Rationale

- **Class:** Settled
- **Settled by:** the issue body of #1771 and its decision comment of 2026-10-17 ("as proposed"), which says rebuild; the classifier, the setter rule and the locator paths stay uniform for scalar and collection fields.

</details>

<details>
<summary><strong>3. What a forgotten subject reads as</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Each element reads as the placeholder (`[REDACTED]`), the element count is kept; a re-saved snapshot writes the tombstone per element** | The scalar semantics applied per element (`ReadTombstone`, `WriteForgotten`); projections that index the list keep working | The number of elements stays visible after the erasure |
| **B) The collection reads as empty** | The count is not retained | Differs from the scalar rule; a dictionary loses its keys, which hold structure the application may rely on; a re-saved empty list cannot be told from an original empty list |
| **C) The collection reads as one placeholder element** | Count hidden, shape kept | Invented data; a dictionary has no single-entry equivalent |

### Chosen Option: **A - placeholder per element** (recommended, pending the maintainer)

### Rationale

- The element count of a tag list is rarely personal data, and option A reuses the scalar code paths unchanged, so the forgotten-subject tests of #1698 carry over.
- Option B wins when the count itself is sensitive (for example a list of diagnoses), which the application can express by modelling that data as a scalar.
- Class: Dilemma

</details>

<details>
<summary><strong>4. Subject, token and wire format of a collection element</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The subject is the sibling of the declaring object (as for a scalar); every element is its own v2 token with a fresh nonce; nothing changes in the token, the cipher or its associated data** | One key lookup per owner, however long the collection; stored data stays readable by the scalar path rules; keys of a dictionary stay plaintext (stated in the attribute documentation) | An element can be swapped for another element of the same subject (the associated data binds subject and key version, not position; the same holds today for two scalar fields of one owner) |
| **B) Bind the element index into the associated data** | Detects reordering and truncation | A new token version for collections, no benefit for the erasure guarantee, and an insert in the middle of a stored list breaks every later element |

### Chosen Option: **A**

### Rationale

- **Class:** Trivial
- **Settled by:** #1698 (plan Design Choices 2 and 4: subject resolution by the sibling on the declaring object, v2 token with associated data `encina:cs2:{subjectId}:{version}` and a fresh nonce per value) and ADR-034. The plan changes neither.

</details>

<details>
<summary><strong>5. Architecture of the Newtonsoft path</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A separate `CryptoShredderNewtonsoftSerializer` (public sealed, an `ISerializer` over Marten's Newtonsoft serializer) with its own `CryptoShreddingContractResolver`, `IValueProvider` wrapper and Newtonsoft-specific contract rules; it shares the engine, the token, the cipher and the classifier shapes** | No change to the working System.Text.Json serializer; the two serializers can evolve and be tested separately; the issue's design (`IContractResolver`, `IValueProvider.GetValue(target)`, `OnDeserializedCallbacks`) maps one to one | Two public serializer types; the shape rules that depend on serializer attributes (`[JsonIgnore]`, converters, constructors) are written twice |
| **B) Generalize `CryptoShredderSerializer` over a serializer-binding abstraction with System.Text.Json and Newtonsoft implementations** | One public type | A large refactor of 397 lines of staging, installation and canary code that works today; every existing test moves |
| **C) Do not support Newtonsoft** (keep `SerializerNotSupported`) | No code | The current behavior |

### Chosen Option: **A - a separate Newtonsoft serializer** (recommended, pending the maintainer)

### Rationale

- Option B wins if the maintainer expects a third serializer later and wants one extension point; Option C wins if no user needs Newtonsoft before 1.0 (the issue is `p1-recommended`).
- The first task of Part B is a spike that records the exact package id, the `ISerializer` implementation type and its customization hook in Marten 9.38, and stops with a report if the type cannot be wrapped (for example if the serializer is sealed with no settings access).
- Class: Dilemma

</details>

<details>
<summary><strong>6. Failure and error model</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Reuse `CryptoShreddingConfigurationException` with the existing and one new `[Flags]` problem (`UnsupportedStringCollection`), and the existing encryption and decryption exceptions; no new `EncinaError` code** | Same error model as #1698 (Design Choice 7); a serializer cannot return `Either`, so exceptions are the contract; no ADR-062 category is needed because no `EncinaErrors.Create` call is added | The startup check and the exception list the problem by name only |
| **B) New error codes through `EncinaErrors.Create`** | Consistent with the ROP rule | The `ISerializer` contract throws; the codes would have no `Either` to travel in |

### Chosen Option: **A**

### Rationale

- **Class:** Settled
- **Settled by:** AGENTS.md section 3 (ROP applies to operations that return `Either`; Marten's `ISerializer` is an exception-based interface owned by the library) and the #1698 plan, Design Choice 7. The messages carry type and property names only, never values.

</details>

---

## Implementation Phases

### Phase 1: String collections on the System.Text.Json path (pull request 1)

<details>
<summary><strong>Tasks</strong></summary>

PR 1 holds exactly these 15 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Marten.GDPR/CryptoShreddedPropertyProblems.cs` | modify (`UnsupportedStringCollection = 1 << 27`) |
| 2 | `src/Encina.Marten.GDPR/Metadata/CryptoShreddedPropertyClassifier.cs` | modify (`RuleString` becomes a shape rule: string, the four collections, or `NotString` / `UnsupportedStringCollection`) |
| 3 | `src/Encina.Marten.GDPR/Metadata/CryptoShreddedField.cs` | modify (nested `CryptoShreddedFieldKind { Scalar, Array, List, ReadOnlyList, Dictionary }` and `Kind`) |
| 4 | `src/Encina.Marten.GDPR/Serialization/CryptoShreddingContractModifier.cs` | modify (`WrapGetter` returns a collection of the declared type holding tokens) |
| 5 | `src/Encina.Marten.GDPR/Serialization/CryptoShreddingEngine.cs` | modify (`EncryptCollectionForWrite`) |
| 6 | `src/Encina.Marten.GDPR/Serialization/CryptoShreddingEngine.Read.cs` | modify (decrypt and rebuild per kind) |
| 7 | `src/Encina.Marten.GDPR/Locator/CryptoShreddedGraphWalker.cs` | modify (paths `Tags[]` and `Notes{}` for collection fields) |
| 8 | `src/Encina.Marten.GDPR/Diagnostics/CryptoShreddingLogMessages.cs` | modify (ids 8492-8493, Phase 4) |
| 9 | `src/Encina.Marten.GDPR/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 10 | `tests/Encina.UnitTests/Marten/GDPR/CryptoShreddingCollectionTests.cs` | new |
| 11 | `tests/Encina.UnitTests/Marten/GDPR/Nested/CryptoShreddedPropertyClassifierTests.cs` | modify (the string-collection cases move from `NotString` to supported or `UnsupportedStringCollection`) |
| 12 | `tests/Encina.PropertyTests/Marten/GDPR/CryptoShreddingCollectionPropertyTests.cs` | new |
| 13 | `tests/Encina.IntegrationTests/Infrastructure/Marten/GDPR/CryptoShreddingCollectionIntegrationTests.cs` | new (shared Marten collection fixture) |
| 14 | `.github/coverage-manifest/Encina.Marten.GDPR.json` | modify (`mechanical-fixer`) |
| 15 | `changelog.d/1771-crypto-shredding-string-collections.added.md` | new (`mechanical-fixer`) |

Tasks:

1. **Classify**: `RuleString` accepts `string`, `string[]`, `List<string>`, `IReadOnlyList<string>` and `Dictionary<string, string>` (values) and returns `UnsupportedStringCollection` for any other `IEnumerable<string>` shape and `NotString` for everything else, so a message tells "not a string" from "a collection we do not support". All other member rules (`RuleReadable`, `RuleSetter`, `RuleSerialized`, `RuleConverter`, `RuleSubjectId`, ...) apply unchanged.
2. **Write**: `WrapGetter` keeps the original getter and returns a collection of the same declared type: `string[]` for arrays and `IReadOnlyList<string>`, a new `List<string>` for lists, a new `Dictionary<string,string>` (same keys) for dictionaries; a null collection stays null, a null element stays null (the scalar rule, no key lookup), every other element goes through the scalar `Encrypt` with the owner's subject. The original collection of the owner is never mutated.
3. **Read**: `OnOwnerDeserialized` enqueues the owner as today; `DecryptPending` decrypts each element through the scalar `ReadTombstone` and `ReadToken` paths and sets the rebuilt collection with the compiled setter (Design Choice 2); one element failing fails the owner closed with the scalar exception.
4. **Erasure paths**: the walker yields one occurrence per collection field with the path of the property followed by `[]` (sequence) or `{}` (dictionary values), matching the grammar documented at `CryptoShreddedOccurrence`.
5. **Tests**: round trip of the four shapes with two subjects (`InMemorySubjectKeyProvider`); null collection, empty collection, null element, one element failing integrity fails the owner; forgotten subject reads placeholders with the count kept, and a re-saved snapshot writes the tombstone per element; a nested owner inside a list that itself holds a collection; `HashSet<string>`, `ImmutableArray<string>` and `IEnumerable<string>` are rejected with `UnsupportedStringCollection` through `CryptoShreddingConfigurationException`; a property test over generated lists, arrays and dictionaries (round trip equals the input; the stored JSON contains no plaintext element) with FsCheck through `Encina.Testing.FsCheck`; an integration test through Marten on PostgreSQL appends an event with a tag list and reads it back, then forgets the subject and reads placeholders; the existing guard tests are unchanged because the only new public symbol is an enum member.
6. **Manifest**: targets in Phase 4.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
Encina.Marten.GDPR encrypts [CryptoShredded] string properties through a System.Text.Json contract modifier (#1698). Issue #1771
adds string collections. Each element is its own v2 token bound to the declaring object's subject; the token, cipher and
associated data do not change.

TASK:
1. Make RuleString shape-aware (string[], List<string>, IReadOnlyList<string>, Dictionary<string,string> values; other
   IEnumerable<string> -> UnsupportedStringCollection; anything else -> NotString).
2. Wrap the getter to return a same-typed collection of tokens; decrypt and rebuild through the compiled setter on read.
3. Extend the graph walker paths (Tags[], Notes{}) and the log messages 8492-8493.
4. Write the unit, property and Marten integration tests of the plan; ask mechanical-fixer for PublicAPI, the manifest and the fragment.

KEY RULES:
- Fail closed: any element that cannot be decrypted fails the owner; nothing is returned in plaintext by a fallback.
- Never mutate the owner's own collection on write; never log or put in an exception message a value, key or element.
- Dictionary keys stay plaintext; say so in the attribute documentation.
- Integration tests use the shared Marten collection fixture; property tests use Encina.Testing.FsCheck.
- Zero warnings; CRAP of every added method at most 10.

REFERENCE FILES:
- src/Encina.Marten.GDPR/Metadata/CryptoShreddedPropertyClassifier.cs
- src/Encina.Marten.GDPR/Serialization/CryptoShreddingContractModifier.cs
- src/Encina.Marten.GDPR/Serialization/CryptoShreddingEngine.cs
- src/Encina.Marten.GDPR/Serialization/CryptoShreddingEngine.Read.cs
- src/Encina.Marten.GDPR/Locator/CryptoShreddedGraphWalker.cs
- docs/plans/crypto-shredding-nested-implementation-plan-1698.md
```

</details>

### Phase 2: Newtonsoft serializer wrapper (pull request 2)

<details>
<summary><strong>Tasks</strong></summary>

PR 2 holds exactly these 15 files; the spike of task 1 may change the list and the plan is then amended before the PR is briefed:

| # | File | Change |
|---|------|--------|
| 1 | `Directory.Packages.props` | modify (`PackageVersion` of the Marten Newtonsoft package, version matching Marten 9.38) |
| 2 | `src/Encina.Marten.GDPR/Encina.Marten.GDPR.csproj` | modify (`PackageReference`) |
| 3 | `src/Encina.Marten.GDPR/Serialization/Newtonsoft/CryptoShreddingContractResolver.cs` | new |
| 4 | `src/Encina.Marten.GDPR/Serialization/Newtonsoft/CryptoShreddingValueProvider.cs` | new |
| 5 | `src/Encina.Marten.GDPR/Serialization/Newtonsoft/CryptoShredderNewtonsoftSerializer.cs` | new |
| 6 | `src/Encina.Marten.GDPR/Metadata/Newtonsoft/NewtonsoftCryptoContractRules.cs` | new |
| 7 | `src/Encina.Marten.GDPR/Serialization/CryptoShredderSerializerFactory.cs` | modify (`Apply` dispatches by serializer type; other types still throw `SerializerNotSupported`) |
| 8 | `src/Encina.Marten.GDPR/Diagnostics/CryptoShreddingLogMessages.cs` | modify (ids 8494-8495) |
| 9 | `src/Encina.Marten.GDPR/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 10 | `tests/Encina.UnitTests/Marten/GDPR/CryptoShreddingNewtonsoftResolverTests.cs` | new |
| 11 | `tests/Encina.UnitTests/Marten/GDPR/CryptoShredderNewtonsoftSerializerTests.cs` | new |
| 12 | `tests/Encina.GuardTests/Marten/GDPR/CryptoShredderNewtonsoftSerializerGuardTests.cs` | new |
| 13 | `tests/Encina.IntegrationTests/Infrastructure/Marten/GDPR/CryptoShreddingNewtonsoftIntegrationTests.cs` | new (shared Marten collection fixture) |
| 14 | `.github/coverage-manifest/Encina.Marten.GDPR.json` | modify (`mechanical-fixer`) |
| 15 | `changelog.d/1771-crypto-shredding-newtonsoft.added.md` | new (`mechanical-fixer`) |

Tasks:

1. **Spike first** (no code merged): in a scratch project record the package id and the `ISerializer` type that Marten 9.38 offers for Newtonsoft (the issue names `Marten.Newtonsoft`), its constructor and the hook that exposes `JsonSerializerSettings`; confirm that a custom `IContractResolver` and `OnDeserializedCallbacks` can be installed after construction. If the type is sealed or exposes no settings, stop and report to the orchestrator (Design Choice 5 reverts to a Dilemma between A and C).
2. **Create** `CryptoShreddingContractResolver : DefaultContractResolver` (or a decorator over the resolver Marten installed, whichever the spike shows is non-destructive): `CreateProperty` wraps `JsonProperty.ValueProvider` of every field the shared classifier marked with `CryptoShreddingValueProvider` (its `GetValue(target)` returns the encrypted value, scalar or collection, exactly like the System.Text.Json getter; `SetValue` delegates), and `CreateObjectContract` adds an `OnDeserializedCallbacks` entry that calls `CryptoShreddingEngine.OnOwnerDeserialized` with the same plan type.
3. **Create** `NewtonsoftCryptoContractRules`: the Newtonsoft counterparts of the serializer-specific rules (`[JsonIgnore]`, `[JsonProperty]` names, `[JsonConverter]` on the property, constructor binding through `[JsonConstructor]`, `IJsonOnDeserialized` equivalents `[OnDeserialized]`); every rule that does not depend on the serializer (value type, interface, subject-id sibling, setter) is reused from the shared classifier. A shape the rules cannot prove is rejected at startup with `CryptoShreddingConfigurationException`, never encrypted partially.
4. **Create** `CryptoShredderNewtonsoftSerializer : ISerializer` wrapping Marten's Newtonsoft serializer: every write and read method opens and closes the engine's per-call frame exactly as `CryptoShredderSerializer.Write` and `Read` do; the staging buffer logic of the System.Text.Json wrapper is not copied (Newtonsoft writes the value providers while serializing into the target, so a failure mid-write can leave a partial buffer; the wrapper therefore serializes to a `StringWriter` or pooled buffer first and only then writes to the caller's target).
5. **Factory**: `CryptoShredderSerializerFactory.Apply` dispatches: System.Text.Json wraps as today, the Newtonsoft serializer wraps with the new class, anything else keeps throwing `SerializerNotSupported`.
6. **Tests**: the scalar and collection round trips of Phase 1 over Newtonsoft with the same expectations; a token written by the System.Text.Json serializer is read by the Newtonsoft one and the reverse (the wire format is identical, so a store can change serializer); the classification of a `[JsonIgnore]` or converter-carrying crypto property is rejected; the factory still rejects an unknown serializer; guard tests for the new public constructor and `Apply`; integration against Marten on PostgreSQL with the Newtonsoft serializer and a forgotten subject.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
Phase 1 is merged: collections work on the System.Text.Json path. No project references a Marten Newtonsoft package.

TASK:
Run the spike first and stop with a report if Marten 9.38's Newtonsoft serializer cannot be wrapped. Otherwise add the contract
resolver, the value provider, the Newtonsoft-specific rules, CryptoShredderNewtonsoftSerializer and the factory dispatch, with
the tests of the plan.

KEY RULES:
- Share the engine, token, cipher and classifier shapes; never fork the cryptography.
- A shape the Newtonsoft rules cannot prove is rejected at startup; a partial encryption is never an outcome.
- Serialize to a private buffer first so a failure mid-write leaves nothing in the caller's target.
- Same fail-closed read semantics as the System.Text.Json path; no value, key or element in logs or exception messages.
- Integration tests use the shared Marten collection fixture.

REFERENCE FILES:
- src/Encina.Marten.GDPR/Serialization/CryptoShredderSerializer.cs
- src/Encina.Marten.GDPR/Serialization/CryptoShredderSerializerFactory.cs
- src/Encina.Marten.GDPR/Serialization/CryptoShreddingContractModifier.cs
- src/Encina.Marten.GDPR/Metadata/CryptoShreddedContractRules.cs
- docs/plans/crypto-shredding-nested-implementation-plan-1698.md
```

</details>

### Phase 3: Registration, startup validation and documentation (pull request 3)

<details>
<summary><strong>Tasks</strong></summary>

PR 3 holds exactly these 7 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Marten.GDPR/ServiceCollectionExtensions.cs` | modify (`AddEncinaMartenGdpr` applies the factory for either serializer; a DI test proves it) |
| 2 | `src/Encina.Marten.GDPR/CryptoShreddingStartupValidationHostedService.cs` | modify (the installed-resolver check covers the Newtonsoft serializer) |
| 3 | `tests/Encina.UnitTests/Marten/GDPR/CryptoShreddingNewtonsoftRegistrationTests.cs` | new (`ValidateOnBuild` and `ValidateScopes`; both serializers) |
| 4 | `src/Encina.Marten.GDPR/README.md` | modify (`docs-writer`) |
| 5 | `docs/features/crypto-shredding.md` | modify (`docs-writer`) |
| 6 | `docs/INVENTORY.md` | modify (`docs-writer`) |
| 7 | `.github/coverage-manifest/Encina.Marten.GDPR.json` | modify (`mechanical-fixer`) |

Tasks: `AddEncinaMartenGdpr` already calls the factory through an `IConfigureOptions`; with the factory dispatching, the change is the unsupported-serializer message and the startup validation of the installed Newtonsoft resolver (the canary encrypt of `CryptoShreddingCanary` runs through the Newtonsoft contract). The documentation states the supported shapes, that dictionary keys are plaintext, the forgotten-subject element semantics and the serializer choice.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
Phases 1 and 2 are merged. The factory dispatches for both serializers.

TASK:
Prove registration with a DI test (ValidateOnBuild, ValidateScopes) for both serializers, extend the startup validation to the
Newtonsoft resolver, and update the README, the feature page and INVENTORY.md through docs-writer.

KEY RULES:
- The startup check fails the host when the resolver is not intact; no silent plaintext fallback.
- Documentation never types coverage figures; state the plaintext dictionary keys and the element-count behavior for forgotten subjects.

REFERENCE FILES:
- src/Encina.Marten.GDPR/ServiceCollectionExtensions.cs
- src/Encina.Marten.GDPR/CryptoShreddingStartupValidationHostedService.cs
- .claude/skills/encina-docs/SKILL.md
```

</details>

### Phase 4: Observability, logging and coverage targets

<details>
<summary><strong>Tasks</strong></summary>

1. **EventIds** (existing range `MartenGDPRCryptoShredding` 8450-8499; 8467-8484 are used, 8485-8491 are claimed by #2455 and #2457): PR 1 takes 8492 `CollectionContractBuilt` (Debug: owner type name, field name and kind) and 8493 `CollectionRebuiltOnRead` (Debug: counts only); PR 2 takes 8494 `NewtonsoftContractBuilt` (Debug) and 8495 `NewtonsoftSerializerWrapped` (Information). 8496-8499 stay free. No new range, so `EncinaEventIdAllocationTests` is unchanged. Logs carry type and property names and counts, never an element, a key or an exception message.
2. **Telemetry**: the existing crypto-shredding counters and activities (`CryptoShreddingDiagnostics`) are reused; the encryption and decryption counters count elements, and the activity tags are unchanged. The key scope is the subject, not the tenant, so no tenant tag is added (ADR-054 D4 applies where multi-tenancy is enabled; the tenancy of subject keys is the existing behavior and is not changed here).
3. **Error categories (ADR-062)**: no `EncinaErrors.Create` or `FromException` call is added (Design Choice 6).
4. **Coverage targets (per flag, new or touched files)**, entered by `mechanical-fixer` in the PR that adds the tests:

| File | Unit | Guard | Contract | Property | Integration | Justification |
|------|:----:|:-----:|:--------:|:--------:|:-----------:|---------------|
| `Metadata/CryptoShreddedPropertyClassifier.cs` | 90 | 0 | 0 | 0 | 0 | Internal static rules driven fully by unit tests; no public parameters and no guard flag reaches an internal type without InternalsVisibleTo for the guard project (verify before entering the 0) |
| `Serialization/CryptoShreddingContractModifier.cs` | 90 | 0 | 0 | 0 | 40 | Wrapper installation covered by unit tests; the Marten integration reaches the real contract build |
| `Serialization/CryptoShreddingEngine.Read.cs` | keeps 95 | 0 | keeps 41 | keeps 56 | keeps 67 | Existing targets are not lowered (target-ratchet); the collection branch is covered by the new unit, property and integration tests |
| `Serialization/CryptoShreddingEngine.cs` | 90 | 0 | 0 | 40 | 40 | Encrypt collection paths covered by unit tests and the collection property test |
| `Locator/CryptoShreddedGraphWalker.cs` | 90 | 0 | 0 | 0 | 0 | Path cases for collections covered by unit tests |
| `Serialization/Newtonsoft/*.cs` (4 files) | 90 | 25 | 0 | 0 | 50 | Unit tests drive the resolver and serializer with a fake key provider; guard reaches the public constructor of the serializer; the Marten integration test reaches the real store |
| `Metadata/Newtonsoft/NewtonsoftCryptoContractRules.cs` | 90 | 0 | 0 | 0 | 0 | Pure rules over reflection, driven by unit tests with one case per rule |
| `Serialization/CryptoShredderSerializerFactory.cs` | 90 | 90 | 0 | 0 | 0 | Three branches (System.Text.Json, Newtonsoft, unsupported) all reachable by unit and guard tests |

5. **Contract and load**: no contract test (no new interface or abstract class); load and benchmark tests are not written (the cost is one AES-GCM operation per element, already measured by `Encina.Marten.GDPR.Benchmarks`; the manifest `reason` points there).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
Code and tests of phases 1 to 3 exist. Logs, targets and coverage remain per pull request.

TASK:
Add the four log messages to the PR that uses each, measure per-flag coverage of every touched file, and record targets with
one-sentence justifications through mechanical-fixer; run --check-justifications.

KEY RULES:
- EventIds only 8492-8495, packed; no range registration is needed.
- Never lower an existing target (target-ratchet); never put an element, key or exception message in a log.

REFERENCE FILES:
- src/Encina.Marten.GDPR/Diagnostics/CryptoShreddingLogMessages.cs
- .github/coverage-manifest/Encina.Marten.GDPR.json
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

### Phase 5: Documentation and finalization

<details>
<summary><strong>Tasks</strong></summary>

1. XML documentation with examples on every new public type and on `CryptoShreddedAttribute` (the supported shapes and the plaintext dictionary keys).
2. The two changelog fragments named in the phase tables (PR 3 adds none: its user-visible change is documentation of the two earlier ones); the ADR-034 addendum if the maintainer chooses Option A of Design Choice 5 and the Newtonsoft path changes the documented scope of the serializer (an addendum to the existing ADR; no new ADR number).
3. Verification per pull request: `dotnet build --configuration Release` with zero warnings; `dotnet test` of the unit, guard and property projects; the integration test with Docker; per-flag coverage with `coverage-report.cs` and `--check-justifications`; the CRAP table of changed methods.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT:
All code of phases 1 to 4 is merged or ready. Finalization remains.

TASK:
Confirm XML documentation, the changelog fragments and a clean Release build; ask docs-writer for any page not covered by Phase 3.

KEY RULES:
- Fragment sections: added; never edit CHANGELOG.md.
- No hand-typed coverage figures in documentation.

REFERENCE FILES:
- changelog.d/README.md
- docs/architecture/adr/index.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Relevance |
|----------|-----------|
| GDPR Art. 17 | Crypto-shredding as the erasure of event-sourced data; every personal element must be unreadable after the subject key is deleted |
| ADR-034 | Crypto-shredding design and the scope of the serializer |
| ADR-019 | Marten for event-sourced compliance modules; integration tests on PostgreSQL |
| AGENTS.md section 3 | Event-sourced compliance modules: integration tests through Marten on PostgreSQL with Testcontainers; fail closed |
| AGENTS.md section 9 | Per-flag targets, FsCheck through `Encina.Testing.FsCheck` |
| #1698 plan | Token, associated data, subject resolution, fail-closed read semantics |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in This Feature |
|-----------|----------|----------------------|
| `CryptoShreddedPropertyClassifier` and `CryptoShreddedContractRules` | `src/Encina.Marten.GDPR/Metadata` | Shape rules to extend |
| `CryptoShreddingContractModifier` | `src/Encina.Marten.GDPR/Serialization` | Getter wrapper and `OnDeserialized` chain |
| `CryptoShreddingEngine` (write and read) | `src/Encina.Marten.GDPR/Serialization` | Encrypt, decrypt, forgotten-subject handling |
| `CryptoShreddingFieldCipher`, `CryptoShreddingToken` | `src/Encina.Marten.GDPR/Serialization` | Unchanged cryptography and wire format |
| `CryptoShreddedGraphWalker` | `src/Encina.Marten.GDPR/Locator` | Erasure paths |
| `CryptoShredderSerializerFactory` | `src/Encina.Marten.GDPR/Serialization` | Dispatch by serializer |
| `InMemorySubjectKeyProvider` | `src/Encina.Marten.GDPR/KeyStore` | Test key provider |
| `MartenCollection`, `MartenFixture` | `tests/Encina.IntegrationTests/Infrastructure/Marten` | Shared integration fixture |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Marten.GDPR` | 8450-8499 (existing) | 8467-8484 used; 8485-8491 claimed by #2455 and #2457; this plan takes 8492-8495; 8496-8499 free |

No new range is registered.

### Estimated File Count

| Category | New Files | Modified Files | Notes |
|----------|-----------|----------------|-------|
| Production code | 5 | 14 | New: resolver, value provider, serializer wrapper, Newtonsoft rules (Part B), and the csproj/props edits counted as modified; modified: problems enum, classifier, field, modifier, engine (2 files), walker, log messages, factory, DI, startup service, PublicAPI (twice) |
| Tests | 7 | 2 | New: collection unit, property, integration; Newtonsoft resolver, serializer, guard, integration, registration; modified: classifier tests |
| Documentation | 0 | 3 | README, feature page, `INVENTORY.md` |
| Configuration | 2 | 5 | Two changelog fragments in PR 1 and PR 2 (a third is not needed in PR 3); manifest (three times), `Directory.Packages.props`, the csproj |
| **Total** | **14** | **24** | 3 pull requests: 15, 15 and 7 files; each at most 15 files and about 1100 lines |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Complete Implementation Prompt - All Phases</strong></summary>

```
PROJECT CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0, no backward compatibility. Issue #1771 adds [CryptoShredded] string collections and a
Marten Newtonsoft adapter to Encina.Marten.GDPR. Both are fail closed today (NotString, SerializerNotSupported).

IMPLEMENTATION OVERVIEW:
1. Part A: classify string[], List<string>, IReadOnlyList<string> and Dictionary<string,string> values; wrap the getter to return a
   same-typed collection of v2 tokens (one per element, the owner's subject); decrypt and rebuild through the compiled setter.
2. Part B: spike first; then CryptoShredderNewtonsoftSerializer with a contract resolver, a value provider and Newtonsoft rules over
   the same engine; the factory dispatches by serializer type.
3. Registration, startup validation, documentation. EventIds 8492-8495 in the existing range.
Three pull requests in phase order.

KEY PATTERNS:
- Fail closed everywhere: an unsupported shape is rejected at startup; an element that cannot be decrypted fails the owner.
- The token, cipher and associated data never change; a store can switch serializers without migrating data.
- Never log or put in a message a value, a key or an element; dictionary keys are plaintext by contract.
- Apply Design Choices 3 and 5 as the Maintainer Decisions section says.

REFERENCE FILES:
- src/Encina.Marten.GDPR/Metadata/CryptoShreddedPropertyClassifier.cs
- src/Encina.Marten.GDPR/Serialization/CryptoShreddingContractModifier.cs
- src/Encina.Marten.GDPR/Serialization/CryptoShredderSerializer.cs
- src/Encina.Marten.GDPR/Serialization/CryptoShredderSerializerFactory.cs
- .github/coverage-manifest/Encina.Marten.GDPR.json
- docs/plans/crypto-shredding-nested-implementation-plan-1698.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | Event-sourcing serializer feature; per-call key caches already exist inside the frame. |
| 2 | OpenTelemetry | ✅ Include | The existing crypto-shredding counters and activities are reused; no tenant tag because keys are subject-scoped. |
| 3 | Structured Logging | ✅ Include | EventIds 8492-8495 inside the existing `MartenGDPRCryptoShredding` range, names and counts only. |
| 4 | Health Checks | ✅ Include | The startup validation rejects unsupported shapes and an unwrapped serializer; the existing health check is unchanged. |
| 5 | Validation | ❌ N/A | Attribute-level feature; shapes are validated by the classifier, not a validation provider. |
| 6 | Resilience | ❌ N/A | No external call beyond the existing key provider. |
| 7 | Distributed Locks | ❌ N/A | The serializer holds no shared state beyond per-call frames. |
| 8 | Transactions | ❌ N/A | Serializer-level feature. |
| 9 | Idempotency | ❌ N/A | Encryption of a value is not repeated by a retry that matters: each attempt writes a fresh token. |
| 10 | Multi-Tenancy | ❌ N/A | The subject key scopes the encryption, not the tenant; tenant-aware keys are the existing behavior and are not changed here. |
| 11 | Module Isolation | ❌ N/A | Not module scoped. |
| 12 | Audit Trail | ❌ N/A | Crypto-shredding records its own events (`SubjectForgottenEvent`). |

**Deferred items:** none.

---

## Prerequisites & Dependencies

| Prerequisite | Status | Notes |
|--------------|--------|-------|
| #1698 nested crypto-shredding (token, engine, contract modifier) | ✅ Merged | The base of both parts |
| #1699 key provider races | ⏭️ Open bug | Applies to collections unchanged: a lost key makes a stream unreadable; listed as a prerequisite of #1698's design |
| #2455, #2457 (merged plans) | ⏭️ Implementation pending | Claim EventIds 8485-8491; this plan starts at 8492 |
| Marten 9.38 Newtonsoft serializer package | ❓ Unverified | The spike of Phase 2 task 1 verifies it before any code |
| Maintainer answers to Design Choices 3 and 5 | Pending | Asked when work starts |

No blocking prerequisites for Part A. Part B is blocked until the spike confirms the package.

---

## Next Steps

1. The orchestrator asks the maintainer the Dilemmas (Design Choices 3 and 5) when work starts, records the answers under `## Maintainer Decisions`, then briefs one worker per pull request in phase order (Part B only after the spike).
2. After merge, comment on #1771 with the result and on #1698's follow-up list that items 1 and 2 are done.
