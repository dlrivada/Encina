# Implementation Plan: Crypto-shredding of string collections and a Marten.Newtonsoft adapter

> **Issue**: [#1771](https://github.com/dlrivada/Encina/issues/1771)
> **Type**: Feature
> **Complexity**: High (9 phases, 7 pull requests of 15, 6, 9, 6, 10, 9 and 4 files)
> **Estimated Scope**: about 1100 lines of production code, about 2000 lines of tests
> **Milestone**: v0.17.0 - Compliance Lifecycle (the decision comment of 2026-10-17 schedules the work by priority after the 1.0 line)

---

## Summary

`Encina.Marten.GDPR` encrypts a `[CryptoShredded]` string property at any depth through a System.Text.Json contract modifier (#1698): `CryptoShreddingContractModifier` wraps `JsonPropertyInfo.Get` to return a v2 token (`cs2:{version}:{base64url}` or the tombstone `cs2:erased`, `CryptoShreddingToken`), and `JsonTypeInfo.OnDeserialized` decrypts through `CryptoShreddingEngine.OnOwnerDeserialized`. Two limits remain, both recorded as follow-ups of #1698:

- A `[CryptoShredded]` string collection is rejected at startup: `RuleString` in `CryptoShreddedPropertyClassifier` (`src/Encina.Marten.GDPR/Metadata/CryptoShreddedPropertyClassifier.cs:271`) returns `CryptoShreddedPropertyProblems.NotString` for any property whose type is not `string`, and `CryptoShreddedPropertyIssue.DescribeFlag` (`CryptoShreddedPropertyIssue.cs:38`) explains it as "string collections are not supported".
- Any serializer other than Marten's `SystemTextJsonSerializer` is rejected: `CryptoShredderSerializerFactory.Apply` throws `CryptoShreddingConfigurationException(SerializerNotSupported)`. No project references a Marten Newtonsoft package (`Newtonsoft.Json` 13.0.4 is in `Directory.Packages.props` only for `Encina.Hangfire`; `Marten` is 9.38.0).

Both are fail closed today and stay so until the feature exists. Re-checked against `origin/main` (0b941beeb): the engine is serializer-neutral where it matters (the token, the cipher with associated data `encina:cs2:{subjectId}:{version}`, the per-call frame, the key access and the forgotten-subject handling do not depend on System.Text.Json), but the classifier rules, `CryptoShredderSerializer` (staging buffers, resolver installation, canary, 397 lines) and three consumers of it are System.Text.Json specific: `CryptoShreddingStartupValidationHostedService.RequireWrappedSerializer`, `CryptoShreddingHealthCheck.Check` and `MartenEventPersonalDataLocator.LocateAllDataAsync` each test `is CryptoShredderSerializer`, so a Newtonsoft store would be rejected by all three until they learn the new serializer (Design Choice 11).

The plan has two parts. Part A (pull requests 1 and 2) adds the four collection shapes of the issue to the System.Text.Json path: each element is its own v2 token, bound to the declaring object's subject like a scalar. Part B (pull requests 3 to 7) adds a separate Newtonsoft serializer wrapper over the same engine, its consumers and its documentation; its first task is a spike that verifies the Marten package and type names, because nothing in the repository references them. The Newtonsoft path becomes reachable (factory dispatch) only in pull request 6, after the serializer and every consumer of it exist, so no merged state accepts a Newtonsoft store that the startup validation, the health check or the locator would then reject.

**Affected packages:** `Encina.Marten.GDPR` only (`Directory.Packages.props` gains one `PackageVersion` in Part B).

**Provider category**: none for the feature itself. Event sourcing (Marten on PostgreSQL) is its own category; integration tests run against Marten through the shared collection fixture, as AGENTS.md section 3 requires for event-sourced compliance modules.

**Sibling plans (same package)**: #2455 and #2457 claim EventIds 8485-8491 of the crypto-shredding range (`artifacts/decisions/plan-batch/eventid-blocks.md`); this plan starts at 8492. #1699 (key provider races) is closed and merged; its behavior is the baseline that collections inherit unchanged. ADR-062 (error category contract) is merged (PR #3599); this plan adds no error code (Design Choice 12).

---

## Design Choices

<details>
<summary><strong>1. Supported collection shapes</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Exactly the shapes of the issue: `string[]`, `List<string>`, `IReadOnlyList<string>`, and the values of `Dictionary<string, string>`; every other collection type is rejected at startup with a new `UnsupportedStringCollection` problem** | Each shape has one known runtime type under System.Text.Json (`string[]`, `List<string>`, `List<string>` for `IReadOnlyList<string>`, `Dictionary<string,string>`), so reads rebuild exactly the type the property declares | `HashSet<string>`, `ImmutableArray<string>`, `IEnumerable<string>`, `IReadOnlyDictionary<string,string>` stay unsupported |
| **B) Any `IEnumerable<string>`** | Wider | A set loses its equality semantics when each element is a random-nonce token; a lazy `IEnumerable` cannot be rebuilt; the runtime type is unknown, which contradicts the issue's "for the concrete collection type" |

### Chosen Option: **A - the four shapes of the issue**

### Rationale

- **Class:** Settled
- **Settled by:** the issue body of #1771 (the four shapes and "decrypt by rebuilding the collection for the concrete collection type") and its decision comment of 2026-10-17 ("implement both as proposed"); option B cannot rebuild a concrete type it does not know, and the current rejection stays as the fail-closed behavior for everything else (AGENTS.md section 3).

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
- **Settled by:** the issue body of #1771 ("decrypt by rebuilding the collection") and its decision comment of 2026-10-17 ("as proposed"); the classifier, the setter rule and the locator paths stay uniform for scalar and collection fields.

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
| **A) The subject is the sibling of the declaring object (as for a scalar); every element is its own v2 token with a fresh nonce; nothing changes in the token, the cipher or its associated data. Consequence: a token written by one serializer is readable by the other, and a store can change serializer without migrating data** | One key lookup per owner, however long the collection; stored data stays readable by the scalar path rules | The associated data does not bind the position of an element (see Design Choice 5) |
| **B) A new token version for collection elements** | Room for a position binding | A second wire format to maintain; the Newtonsoft and System.Text.Json paths could not share stored tokens of scalars and collections uniformly |

### Chosen Option: **A**

### Rationale

- **Class:** Trivial
- **Settled by:** #1698 (plan Design Choices 2 and 4: subject resolution by the sibling on the declaring object, v2 token with associated data `encina:cs2:{subjectId}:{version}` and a fresh nonce per value) and ADR-034. The plan changes neither the token nor the cipher, and therefore adds no new rule of its own.

</details>

<details>
<summary><strong>5. Binding the position of an element into the associated data</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No position binding: the associated data stays `encina:cs2:{subjectId}:{version}` for every element** | Same format as scalars (Design Choice 4); an element of one owner can be moved in code without re-keying; no new token format | Someone with write access to the event store can reorder or swap the elements of a stored list, or move a dictionary value to another key, and the owner still reads successfully (the same holds today for two scalar fields of one owner) |
| **B) Bind the list index, and for a dictionary the plaintext key, into the associated data** | Detects reordering and swapping of list elements and a value moved to another dictionary key (it does not detect truncation: the count would have to be bound as well) | A new token format for collection elements and a migration-free but incompatible read rule; a stored token cannot be moved to another position; the key of a dictionary is plaintext (Design Choice 6) so the binding protects only the pairing |

### Chosen Option: **A - no position binding** (recommended, pending the maintainer)

### Rationale

- Option A keeps one wire format and the erasure guarantee (the key deletion makes every element unreadable) is identical in both options; the integrity attack needs write access to the store, which also allows replacing a whole event.
- Option B wins in a named scenario: an ordered list whose order carries meaning that an attacker could change undetected (for example a sequence of dated diagnoses, or a dictionary of per-field values where swapping two values changes their meaning).
- Class: Dilemma

</details>

<details>
<summary><strong>6. Dictionary keys stay plaintext</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Only the values of `Dictionary<string, string>` are encrypted; the keys stay plaintext, are not erased by the subject's key deletion, and are stated as such in the attribute documentation, the feature page and the README** | The shape the issue names ("`Dictionary<string,string>` values"); keys remain usable as structure by projections and queries | A key that holds personal data (a name used as a key, for example) survives the erasure and is exported by the locator only as part of the collection; nothing at runtime detects it |
| **B) Encrypt the keys as well** | No personal data left in a key | Not what the issue proposes; a dictionary cannot be looked up by key without decrypting every key; random-nonce tokens make equal keys collide differently on every write |

### Chosen Option: **A**

### Rationale

- **Class:** Settled
- **Settled by:** the issue body of #1771 ("`Dictionary<string,string>` values"). The existing `DictionaryKeyCarriesCryptoShredded` rule only rejects a crypto-shredded type used as a key; it does not inspect key contents, so no runtime protection of keys exists or is added. The documentation says: "Dictionary keys are stored in plaintext and survive an erasure; never put personal data in a key."

</details>

<details>
<summary><strong>7. Null collections and null elements</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A null collection stays null and a null element stays null with no key lookup, exactly the scalar rule** | One rule for scalars and elements; a null needs no key | The slot of a null element stays visible after an erasure (the same as a null scalar field) |
| **B) Encrypt a null element as a token of an empty marker** | Hides which slots are null | Invented data; differs from the scalar rule |

### Chosen Option: **A**

### Rationale

- **Class:** Trivial
- **Settled by:** `CryptoShreddingEngine.EncryptForWrite` (`CryptoShreddingEngine.cs:75-77`: a `null` value returns `null` with no key lookup); the plan applies that rule per element.

</details>

<details>
<summary><strong>8. Locator and data-subject-request export shape of a collection field</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One `PersonalDataLocation` per collection field, as for a scalar: `FieldName` is the field path followed by `[]` (list or array) or `{}` (dictionary values), and `CurrentValue` is the collection object read through the original getter (plaintext elements)** | The locator code of `LocateFieldsInEvent` and `BuildLocation` stays as it is; the issue's path `Tags[]` is met; the export of the whole field is one unit | A consumer that wants per-element records splits the collection itself |
| **B) One location per element** | Per-element records | A location carries a path without an index (`CryptoShreddedOccurrence` documents "a path never contains an index or a key"), so the locations of one field would be indistinguishable |

### Chosen Option: **A**

### Rationale

- **Class:** Trivial
- **Settled by:** `MartenEventPersonalDataLocator.cs:113-152` (`LocateFieldsInEvent` yields one location per occurrence with `CurrentValue = value`, line 149) and the path grammar documented on `CryptoShreddedOccurrence` (`CryptoShreddedGraphWalker.cs`); the plan changes only the suffix of the field path (pull request 2) and tests the export shape.

</details>

<details>
<summary><strong>9. Architecture of the Newtonsoft path</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A separate `CryptoShredderNewtonsoftSerializer` (public sealed, an `ISerializer` over Marten's Newtonsoft serializer) with its own `CryptoShreddingContractResolver`, `IValueProvider` wrapper and Newtonsoft-specific contract rules; it shares the engine, the token, the cipher and the classifier shapes** | No change to the working System.Text.Json serializer; the two serializers can evolve and be tested separately; the issue's design (`IContractResolver`, `IValueProvider.GetValue(target)`, `OnDeserializedCallbacks`) maps one to one | Two public serializer types; the shape rules that depend on serializer attributes (`[JsonIgnore]`, converters, constructors) are written twice |
| **B) Generalize `CryptoShredderSerializer` over a serializer-binding abstraction with System.Text.Json and Newtonsoft implementations** | One public type | A large refactor of the 397 lines of staging, installation and canary code that works today; every existing test moves |
| **C) Do not support Newtonsoft** (keep `SerializerNotSupported`) | No code | The current behavior |

### Chosen Option: **A - a separate Newtonsoft serializer** (recommended, pending the maintainer)

### Rationale

- Option B wins if the maintainer expects a third serializer later and wants one extension point; Option C wins if no user needs Newtonsoft before 1.0 (the issue is `p1-recommended`).
- The first task of Part B is a spike that records the exact package id, the `ISerializer` implementation type and its customization hook in Marten 9.38, and stops with a report if the type cannot be wrapped (for example if the serializer is sealed with no settings access).
- Class: Dilemma

</details>

<details>
<summary><strong>10. Write staging of the Newtonsoft serializer</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The Newtonsoft wrapper serializes to a private buffer first and copies to the caller's target only on success** | A failure while a value provider encrypts leaves the caller's target untouched, as `WriteStaged` guarantees for System.Text.Json | One extra copy per write |
| **B) Serialize straight into the caller's target** | No copy | A failed encryption mid-write leaves partial JSON in the caller's buffer or parameter |

### Chosen Option: **A**

### Rationale

- **Class:** Trivial
- **Settled by:** `CryptoShredderSerializer.WriteStaged` (`CryptoShredderSerializer.cs:253`: "A failed write must leave the caller's buffer untouched, so the output is staged and copied on success"); the Newtonsoft wrapper follows the same convention with its own buffer instead of copying the System.Text.Json staging code (which is tied to `IBufferWriter<byte>` and `NeedsStaging`).

</details>

<details>
<summary><strong>11. How the startup validation, the health check and the locator see the Newtonsoft serializer</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) An internal serializer-neutral interface (`ICryptoShreddingInstallation`: verify the installation, expose the type-plan registry, walk the crypto fields of an event body) implemented by both serializers; the three consumers use it; a Newtonsoft graph walker (`CryptoShreddedNewtonsoftGraphWalker`) walks over Newtonsoft contracts** | A Newtonsoft store passes the startup validation, reports health and takes part in data subject requests (access and erasure routing) exactly like a System.Text.Json store | A second walker, and `CryptoShredderSerializer` changes to implement the interface |
| **B) Newtonsoft stores are supported for reads and writes only; the locator keeps returning `CryptoShreddingErrors.SerializerUnsupported` and the startup validation and health check accept the Newtonsoft serializer without the type scan** | Smaller change | A Newtonsoft store cannot answer a data subject access request through the Marten locator (fail closed, never silent), and the startup scan of misconfigured types does not run for it |

### Chosen Option: **A - full parity through an internal interface** (recommended, pending the maintainer)

### Rationale

- Option A keeps one behavior for every store the library accepts, and the locator is part of the erasure and access guarantee the package exists for.
- Option B wins if the maintainer wants Newtonsoft supported only as a read/write adapter first and accepts the gap in the DSR locator for a later issue; pull requests 3, 4 and 6 are valid under both options, pull request 5 changes (no walker, no interface, smaller).
- Class: Dilemma

</details>

<details>
<summary><strong>12. Failure and error model</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Reuse `CryptoShreddingConfigurationException` with the existing and one new `[Flags]` problem (`UnsupportedStringCollection`), and the existing encryption and decryption exceptions; no new `EncinaError` code** | Same error model as #1698 (Design Choice 7); a serializer cannot return `Either`, so exceptions are the contract; no ADR-062 category is needed because no `EncinaErrors.Create` call is added | The startup check and the exception list the problem by name only |
| **B) New error codes through `EncinaErrors.Create`** | Consistent with the ROP rule | The `ISerializer` contract throws; the codes would have no `Either` to travel in |

### Chosen Option: **A**

### Rationale

- **Class:** Settled
- **Settled by:** AGENTS.md section 3 (ROP applies to operations that return `Either`; Marten's `ISerializer` is an exception-based interface owned by the library) and the #1698 plan, Design Choice 7. The messages carry type and property names only, never values. The locator already returns `Left` through the existing `CryptoShreddingErrors` codes and keeps doing so.

</details>

---

## Implementation Phases

### Phase 1: String collections on the System.Text.Json path, write, read and classification (pull request 1)

<details>
<summary><strong>Tasks</strong></summary>

PR 1 holds exactly these 15 files (about 950 lines). The classifier admits a collection only in the same pull request that makes the engine encrypt and decrypt it, so no merged state accepts a shape that is written in plaintext.

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Marten.GDPR/CryptoShreddedPropertyProblems.cs` | modify (`UnsupportedStringCollection = 1 << 27`; the last member today is `SubjectIdPropertyIsCryptoShredded = 1 << 26`) |
| 2 | `src/Encina.Marten.GDPR/CryptoShreddedPropertyIssue.cs` | modify (`DescribeFlag`: the `NotString` sentence no longer says "string collections are not supported"; a new sentence for `UnsupportedStringCollection`, otherwise the `_ => flag.ToString()` arm answers) |
| 3 | `src/Encina.Marten.GDPR/Metadata/CryptoShreddedPropertyClassifier.cs` | modify (`RuleString` becomes a shape rule: string, the four collections, or `NotString` / `UnsupportedStringCollection`) |
| 4 | `src/Encina.Marten.GDPR/Metadata/CryptoShreddedField.cs` | modify (nested `CryptoShreddedFieldKind { Scalar, Array, List, ReadOnlyList, Dictionary }` and `Kind`, derived from `Property.PropertyType` inside the existing constructor, whose signature does not change) |
| 5 | `src/Encina.Marten.GDPR/Serialization/CryptoShreddingContractModifier.cs` | modify (`WrapGetter` returns a collection of the declared type holding tokens) |
| 6 | `src/Encina.Marten.GDPR/Serialization/CryptoShreddingEngine.cs` | modify (`EncryptCollectionForWrite`) |
| 7 | `src/Encina.Marten.GDPR/Serialization/CryptoShreddingEngine.Read.cs` | modify (`DecryptField` decrypts and rebuilds per kind) |
| 8 | `src/Encina.Marten.GDPR/Attributes/CryptoShreddedAttribute.cs` | modify (XML documentation with an example: the supported shapes and the plaintext dictionary keys) |
| 9 | `src/Encina.Marten.GDPR/Diagnostics/CryptoShreddingLog.cs` | modify (`[LoggerMessage]` 8492 and 8493, Phase 8; the newest messages 8470-8484 live in this file, not in `CryptoShreddingLogMessages.cs`) |
| 10 | `src/Encina.Marten.GDPR/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`: the one new enum member) |
| 11 | `tests/Encina.UnitTests/Marten/GDPR/CryptoShreddingCollectionTests.cs` | new |
| 12 | `tests/Encina.UnitTests/Marten/GDPR/Nested/CryptoShreddedPropertyClassifierTests.cs` | modify (the string-collection case `StringListOwner.Emails` at line 24 moves from `NotString` to supported; new `UnsupportedStringCollection` cases; `DescribeFlag` covers every flag) |
| 13 | `docs/features/crypto-shredding.md` | modify (`docs-writer`: lines 281, 339 and 813 say collections are rejected; add the shapes, the `UnsupportedStringCollection` row and the plaintext keys) |
| 14 | `.github/coverage-manifest/Encina.Marten.GDPR.json` | modify (`mechanical-fixer`) |
| 15 | `changelog.d/1771-crypto-shredding-string-collections.added.md` | new (`mechanical-fixer`) |

`CryptoShredderSerializerFailClosedTests.cs:88` (the only test caller of the `CryptoShreddedField` constructor, besides the classifier at `CryptoShreddedPropertyClassifier.cs:146`) does not change because `Kind` is derived inside the constructor. Verified with `git grep -n "new CryptoShreddedField("`.

Tasks:

1. **Classify**: `RuleString` accepts `string`, `string[]`, `List<string>`, `IReadOnlyList<string>` and `Dictionary<string, string>` (values) and returns `UnsupportedStringCollection` for any other `IEnumerable<string>` shape and `NotString` for everything else, so a message tells "not a string" from "a collection we do not support". All other member rules (`RuleReadable`, `RuleSetter`, `RuleSerialized`, `RuleConverter`, `RuleSubjectId`, ...) apply unchanged.
2. **Write**: `WrapGetter` keeps the original getter and returns a collection of the same declared type: `string[]` for arrays and `IReadOnlyList<string>`, a new `List<string>` for lists, a new `Dictionary<string,string>` (same keys) for dictionaries; a null collection stays null, a null element stays null (Design Choice 7), every other element goes through the scalar `Encrypt` with the owner's subject. The original collection of the owner is never mutated.
3. **Read**: `OnOwnerDeserialized` enqueues the owner as today; `DecryptField` (it reads `field.Getter(owner) is not string stored` today) handles the collection kinds element by element with the scalar `ReadTombstone` and `ReadToken` paths and sets the rebuilt collection (a `string[]`, a `List<string>` for both list kinds, a `Dictionary<string,string>`) with the compiled setter (Design Choice 2); the `ReferenceEquals(field.Getter(owner), ...)` check after the set applies to the rebuilt collection; one element failing fails the owner closed with the scalar exception.
4. **Tests** (`CryptoShreddingCollectionTests.cs` carries its own owner shapes; the classifier test file keeps its existing shapes): round trip of the four shapes with two subjects (`InMemorySubjectKeyProvider`); null collection, empty collection, null element, one element failing integrity fails the owner; forgotten subject reads placeholders with the count kept (Design Choice 3), and a re-saved snapshot writes the tombstone per element; a nested owner inside a list that itself holds a collection; `HashSet<string>`, `ImmutableArray<string>` and `IEnumerable<string>` are rejected with `UnsupportedStringCollection` through `CryptoShreddingConfigurationException`; every `CryptoShreddedPropertyProblems` flag has a `DescribeFlag` sentence that is not the `flag.ToString()` fallback; the existing guard tests are unchanged because the only new public symbol is an enum member.
5. **Between PR 1 and PR 2** the locator reports a collection field under the bare field path (`Tags`); PR 2 adds the suffix. Both pull requests are meant to merge back to back.

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
   IEnumerable<string> -> UnsupportedStringCollection; anything else -> NotString); update CryptoShreddedPropertyIssue.DescribeFlag.
2. Derive CryptoShreddedField.Kind inside the existing constructor (its signature does not change).
3. Wrap the getter to return a same-typed collection of tokens; decrypt and rebuild through the compiled setter on read.
4. Add log messages 8492-8493 to Diagnostics/CryptoShreddingLog.cs; write the unit tests; ask mechanical-fixer for PublicAPI, the
   manifest and the fragment, and docs-writer for docs/features/crypto-shredding.md.

KEY RULES:
- Fail closed: any element that cannot be decrypted fails the owner; nothing is returned in plaintext by a fallback.
- Never mutate the owner's own collection on write; never log or put in an exception message a value, key or element.
- Dictionary keys stay plaintext; say so in the attribute documentation.
- Zero warnings; CRAP of every added method at most 10.

REFERENCE FILES:
- src/Encina.Marten.GDPR/Metadata/CryptoShreddedPropertyClassifier.cs
- src/Encina.Marten.GDPR/CryptoShreddedPropertyIssue.cs
- src/Encina.Marten.GDPR/Serialization/CryptoShreddingContractModifier.cs
- src/Encina.Marten.GDPR/Serialization/CryptoShreddingEngine.cs
- src/Encina.Marten.GDPR/Serialization/CryptoShreddingEngine.Read.cs
- docs/plans/crypto-shredding-nested-implementation-plan-1698.md
```

</details>

### Phase 2: Collections, locator paths and the remaining test types (pull request 2)

<details>
<summary><strong>Tasks</strong></summary>

PR 2 holds exactly these 6 files (about 600 lines):

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Marten.GDPR/Locator/CryptoShreddedGraphWalker.cs` | modify (the path of a collection field gets the suffix `[]` for arrays and lists, `{}` for dictionary values) |
| 2 | `tests/Encina.UnitTests/Marten/GDPR/CryptoShreddingCollectionLocatorTests.cs` | new (walker paths `Tags[]` and `Notes{}`; `LocateFieldsInEvent` yields one location per field with the collection as `CurrentValue`; the field of another subject is skipped) |
| 3 | `tests/Encina.ContractTests/Marten/GDPR/CryptoShredderSerializerContractTests.cs` | modify (collection round trip through every write and read member of the existing contract class) |
| 4 | `tests/Encina.PropertyTests/Marten/GDPR/CryptoShreddingCollectionPropertyTests.cs` | new (FsCheck through `Encina.Testing.FsCheck`: generated lists, arrays and dictionaries round trip to the input; the stored JSON contains no plaintext element) |
| 5 | `tests/Encina.IntegrationTests/Infrastructure/Marten/GDPR/CryptoShreddingCollectionIntegrationTests.cs` | new (shared Marten collection fixture: append an event with a tag list and a note dictionary on PostgreSQL, read it back, forget the subject, read placeholders) |
| 6 | `.github/coverage-manifest/Encina.Marten.GDPR.json` | modify (`mechanical-fixer`) |

Tasks:

1. **Paths**: the walker yields the path of the property followed by `[]` or `{}` for a collection field, matching the grammar documented at `CryptoShreddedOccurrence`; the walker's other paths do not change.
2. **DSR export** (Design Choice 8): one `PersonalDataLocation` per collection field; the unit test asserts `FieldName`, `CurrentValue` (the original collection, plaintext) and the subject filter.
3. **Property and integration tests** run the shapes of PR 1 end to end; the integration test uses `MartenCollection` and `MartenFixture`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
PR 1 is merged: collections are classified, encrypted and decrypted on the System.Text.Json path.

TASK:
Add the suffix to the walker path of a collection field, then the locator, contract, property and Marten integration tests of
the plan; ask mechanical-fixer for the manifest.

KEY RULES:
- Integration tests use the shared Marten collection fixture; property tests use Encina.Testing.FsCheck.
- Raise per-flag targets only upward (target-ratchet); one-sentence justification per file.

REFERENCE FILES:
- src/Encina.Marten.GDPR/Locator/CryptoShreddedGraphWalker.cs
- src/Encina.Marten.GDPR/Locator/MartenEventPersonalDataLocator.cs
- tests/Encina.ContractTests/Marten/GDPR/CryptoShredderSerializerContractTests.cs
- tests/Encina.IntegrationTests/Infrastructure/Marten/GDPR/CryptoShredderSerializerIntegrationTests.cs
```

</details>

### Phase 3: Spike and the Newtonsoft contract pieces (pull request 3)

<details>
<summary><strong>Tasks</strong></summary>

PR 3 holds exactly these 9 files (about 650 lines); the spike of task 1 may change the list and the plan is then amended before the PR is briefed. No type of PR 3 is public, so it adds no PublicAPI line, no guard test and no changelog fragment.

| # | File | Change |
|---|------|--------|
| 1 | `Directory.Packages.props` | modify (`PackageVersion` of the Marten Newtonsoft package, version matching Marten 9.38.0) |
| 2 | `src/Encina.Marten.GDPR/Encina.Marten.GDPR.csproj` | modify (`PackageReference`) |
| 3 | `src/Encina.Marten.GDPR/Serialization/Newtonsoft/CryptoShreddingContractResolver.cs` | new (internal) |
| 4 | `src/Encina.Marten.GDPR/Serialization/Newtonsoft/CryptoShreddingValueProvider.cs` | new (internal) |
| 5 | `src/Encina.Marten.GDPR/Metadata/Newtonsoft/NewtonsoftCryptoContractRules.cs` | new (internal) |
| 6 | `src/Encina.Marten.GDPR/Diagnostics/CryptoShreddingLog.cs` | modify (8494 `NewtonsoftContractBuilt`, logged by the resolver) |
| 7 | `tests/Encina.UnitTests/Marten/GDPR/CryptoShreddingNewtonsoftResolverTests.cs` | new |
| 8 | `tests/Encina.UnitTests/Marten/GDPR/CryptoShreddingNewtonsoftRulesTests.cs` | new (one case per rule) |
| 9 | `.github/coverage-manifest/Encina.Marten.GDPR.json` | modify (`mechanical-fixer`) |

Tasks:

1. **Spike first** (no code merged): in a scratch project record the package id and the `ISerializer` type that Marten 9.38.0 offers for Newtonsoft (the issue names `Marten.Newtonsoft`), its constructor and the hook that exposes `JsonSerializerSettings`; confirm that a custom `IContractResolver` and `OnDeserializedCallbacks` can be installed after construction. If the type is sealed or exposes no settings, stop and report to the orchestrator (Design Choice 9 reverts to a Dilemma between A and C).
2. **Create** `CryptoShreddingContractResolver : DefaultContractResolver` (or a decorator over the resolver Marten installed, whichever the spike shows is non-destructive): `CreateProperty` wraps `JsonProperty.ValueProvider` of every field the shared classifier marked with `CryptoShreddingValueProvider` (its `GetValue(target)` returns the encrypted value, scalar or collection, exactly like the System.Text.Json getter; `SetValue` delegates), and `CreateObjectContract` adds an `OnDeserializedCallbacks` entry that calls `CryptoShreddingEngine.OnOwnerDeserialized` with the same plan type.
3. **Create** `NewtonsoftCryptoContractRules`: the Newtonsoft counterparts of the serializer-specific rules (`[JsonIgnore]`, `[JsonProperty]` names, `[JsonConverter]` on the property, constructor binding through `[JsonConstructor]`, `[OnDeserialized]` callbacks on the owner); every rule that does not depend on the serializer (value type, interface, subject-id sibling, setter, the collection shapes of PR 1) is reused from the shared classifier. A shape the rules cannot prove is rejected at startup with `CryptoShreddingConfigurationException`, never encrypted partially.
4. **Tests**: scalar and collection round trips through a resolver over `JsonSerializerSettings` with a fake key provider; a `[JsonIgnore]` or converter-carrying crypto property is rejected; the log 8494 is asserted.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
PRs 1 and 2 are merged: collections work on the System.Text.Json path. No project references a Marten Newtonsoft package.

TASK:
Run the spike first and stop with a report if Marten 9.38.0's Newtonsoft serializer cannot be wrapped. Otherwise add the package
reference, the contract resolver, the value provider and the Newtonsoft-specific rules with their unit tests.

KEY RULES:
- Share the engine, token, cipher and classifier shapes; never fork the cryptography.
- A shape the Newtonsoft rules cannot prove is rejected at startup; a partial encryption is never an outcome.
- No value, key or element in logs or exception messages.

REFERENCE FILES:
- src/Encina.Marten.GDPR/Serialization/CryptoShreddingContractModifier.cs
- src/Encina.Marten.GDPR/Metadata/CryptoShreddedContractRules.cs
- src/Encina.Marten.GDPR/Serialization/CryptoShreddingEngine.Read.cs
- docs/plans/crypto-shredding-nested-implementation-plan-1698.md
```

</details>

### Phase 4: The Newtonsoft serializer wrapper (pull request 4)

<details>
<summary><strong>Tasks</strong></summary>

PR 4 holds exactly these 6 files (about 600 lines):

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Marten.GDPR/Serialization/Newtonsoft/CryptoShredderNewtonsoftSerializer.cs` | new (public sealed, `ISerializer` over Marten's Newtonsoft serializer) |
| 2 | `src/Encina.Marten.GDPR/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 3 | `tests/Encina.UnitTests/Marten/GDPR/CryptoShredderNewtonsoftSerializerTests.cs` | new |
| 4 | `tests/Encina.GuardTests/Marten/GDPR/CryptoShredderNewtonsoftSerializerGuardTests.cs` | new (null inner, scope factory and logger; blank placeholder; null arguments of the write members) |
| 5 | `tests/Encina.ContractTests/Marten/GDPR/CryptoShredderNewtonsoftSerializerContractTests.cs` | new (mirrors `CryptoShredderSerializerContractTests`: documents without personal data are byte-identical to Marten's Newtonsoft serializer, the Weasel settings delegate, nested personal data round-trips through every read member) |
| 6 | `.github/coverage-manifest/Encina.Marten.GDPR.json` | modify (`mechanical-fixer`) |

Tasks:

1. **Create** `CryptoShredderNewtonsoftSerializer : ISerializer`, constructed like `CryptoShredderSerializer` (inner serializer, `IServiceScopeFactory`, `ILogger<CryptoShredderSerializer>`, placeholder) so `CryptoShredderSerializerFactory.Apply` keeps its signature: every write and read member opens and closes the engine's per-call frame exactly as `CryptoShredderSerializer.Write` and `Read` do, and installs the resolver of PR 3 on the inner serializer's settings.
2. **Staging** (Design Choice 10): writes go to a private buffer first and are copied to the caller's target only on success; the System.Text.Json `WriteStaged` code is not copied.
3. **Tests**: the scalar and collection round trips of Phases 1 and 2 over Newtonsoft with the same expectations; a token written by the System.Text.Json serializer is read by the Newtonsoft one and the reverse (Design Choice 4); a failed encryption leaves the target untouched.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
PR 3 is merged: the resolver, the value provider and the Newtonsoft rules exist as internal types.

TASK:
Add the public CryptoShredderNewtonsoftSerializer with its unit, guard and contract tests; ask mechanical-fixer for the PublicAPI
lines and the manifest.

KEY RULES:
- Same fail-closed read semantics as the System.Text.Json path; no value, key or element in logs or exception messages.
- Serialize to a private buffer first so a failure mid-write leaves nothing in the caller's target.
- Guard tests for every public constructor and method with parameters (AGENTS.md section 9).

REFERENCE FILES:
- src/Encina.Marten.GDPR/Serialization/CryptoShredderSerializer.cs
- tests/Encina.GuardTests/Marten/GDPR/CryptoShredderSerializerGuardTests.cs
- tests/Encina.ContractTests/Marten/GDPR/CryptoShredderSerializerContractTests.cs
```

</details>

### Phase 5: The consumers of the installed serializer (pull request 5)

<details>
<summary><strong>Tasks</strong></summary>

PR 5 holds exactly these 10 files (about 900 lines) and implements the recommended option of Design Choice 11; with option B the interface, the walker and the locator test are not written and the PR shrinks to the startup validation, the health check and their test.

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Marten.GDPR/Serialization/ICryptoShreddingInstallation.cs` | new (internal: verify the installation, the registry, the walk of one event body) |
| 2 | `src/Encina.Marten.GDPR/Serialization/CryptoShredderSerializer.cs` | modify (implements the interface; `Registry`, `WalkOptions` and `VerifyContractModifierInstalled` stay) |
| 3 | `src/Encina.Marten.GDPR/Serialization/Newtonsoft/CryptoShredderNewtonsoftSerializer.cs` | modify (implements the interface) |
| 4 | `src/Encina.Marten.GDPR/CryptoShreddingStartupValidationHostedService.cs` | modify (`RequireWrappedSerializer` and the type scan use the interface; existing `unit: 94` target is kept) |
| 5 | `src/Encina.Marten.GDPR/Health/CryptoShreddingHealthCheck.cs` | modify (`Check` uses the interface) |
| 6 | `src/Encina.Marten.GDPR/Locator/MartenEventPersonalDataLocator.cs` | modify (`LocateAllDataAsync` uses the interface instead of `is not CryptoShredderSerializer`) |
| 7 | `src/Encina.Marten.GDPR/Locator/CryptoShreddedNewtonsoftGraphWalker.cs` | new (internal; the walk over Newtonsoft contracts with the same path grammar) |
| 8 | `tests/Encina.UnitTests/Marten/GDPR/CryptoShreddingNewtonsoftConsumersTests.cs` | new (startup validation and health check over a Newtonsoft serializer set directly on `StoreOptions`, and over both serializers) |
| 9 | `tests/Encina.UnitTests/Marten/GDPR/CryptoShreddingNewtonsoftLocatorTests.cs` | new (paths, one location per field, `CurrentValue`, subject filter) |
| 10 | `.github/coverage-manifest/Encina.Marten.GDPR.json` | modify (`mechanical-fixer`) |

Tasks:

1. **Interface** (internal, so no PublicAPI line): the members the three consumers use today (`VerifyContractModifierInstalled`, `Registry`, `ResolveContract`, the walk). The engine's `FieldPathOf` falls back to the bare field name when no path options exist; the Newtonsoft path supplies its own walk.
2. **Consumers**: the startup validation keeps failing the host with `SerializerNotWrapped` for any serializer that does not implement the interface; the health check keeps reporting unhealthy for it; the locator keeps returning `CryptoShreddingErrors.SerializerUnsupported` for it.
3. **Tests** assert the three behaviors for the System.Text.Json serializer (unchanged), the Newtonsoft serializer and an unknown serializer (still rejected).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT:
PR 4 is merged: CryptoShredderNewtonsoftSerializer exists but the factory does not dispatch to it yet.

TASK:
Introduce the internal ICryptoShreddingInstallation, implement it in both serializers and switch the startup validation, the health
check and the Marten locator to it; add the Newtonsoft graph walker and the unit tests.

KEY RULES:
- Fail closed: an unknown serializer is still rejected by all three consumers.
- Keep the existing unit target of CryptoShreddingStartupValidationHostedService (94); never lower a target.
- The data subject's own identifier is never logged.

REFERENCE FILES:
- src/Encina.Marten.GDPR/CryptoShreddingStartupValidationHostedService.cs
- src/Encina.Marten.GDPR/Health/CryptoShreddingHealthCheck.cs
- src/Encina.Marten.GDPR/Locator/MartenEventPersonalDataLocator.cs
- src/Encina.Marten.GDPR/Locator/CryptoShreddedGraphWalker.cs
```

</details>

### Phase 6: Factory dispatch, registration and the end-to-end Newtonsoft test (pull request 6)

<details>
<summary><strong>Tasks</strong></summary>

PR 6 holds exactly these 9 files (about 600 lines):

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Marten.GDPR/Serialization/CryptoShredderSerializerFactory.cs` | modify (`Apply` dispatches by serializer type; the early return also covers `CryptoShredderNewtonsoftSerializer`; other types still throw `SerializerNotSupported`) |
| 2 | `src/Encina.Marten.GDPR/Diagnostics/CryptoShreddingLog.cs` | modify (8495 `NewtonsoftSerializerWrapped`, logged by the factory) |
| 3 | `src/Encina.Marten.GDPR/ServiceCollectionExtensions.cs` | modify (XML documentation: either serializer) |
| 4 | `src/Encina.Marten.GDPR/ConfigureMartenCryptoShredding.cs` | modify (XML documentation: either serializer) |
| 5 | `tests/Encina.UnitTests/Marten/GDPR/Nested/CryptoShredderSerializerInstallationTests.cs` | modify (dispatch to both serializers; `Factory_WrapsOnceAndLogs8460` at line 110 gets a Newtonsoft twin proving a second `Apply` over the Newtonsoft wrapper returns without throwing; an unknown serializer still fails closed at line 125) |
| 6 | `tests/Encina.UnitTests/Marten/GDPR/CryptoShreddingNewtonsoftRegistrationTests.cs` | new (`AddEncinaMartenGdpr` builds with `ValidateOnBuild` and `ValidateScopes` for both serializers) |
| 7 | `tests/Encina.IntegrationTests/Infrastructure/Marten/GDPR/CryptoShreddingNewtonsoftIntegrationTests.cs` | new (shared Marten collection fixture: scalar and collection round trip, forgotten subject, locator export) |
| 8 | `.github/coverage-manifest/Encina.Marten.GDPR.json` | modify (`mechanical-fixer`) |
| 9 | `changelog.d/1771-crypto-shredding-newtonsoft.added.md` | new (`mechanical-fixer`) |

Tasks:

1. **Factory** idempotency: today `Apply` returns early only for `current is CryptoShredderSerializer` (`CryptoShredderSerializerFactory.cs`); without the extended check a second `Apply` over the Newtonsoft wrapper would throw `SerializerNotSupported`. The signature of `Apply` does not change, so no caller changes (callers: `ConfigureMartenCryptoShredding.cs:51`, two integration tests, three unit tests and the guard tests).
2. **Registration**: `AddEncinaMartenGdpr` already calls the factory through an `IConfigureOptions<StoreOptions>`; the DI test builds the provider with `ValidateOnBuild` and `ValidateScopes` for both serializers and proves the startup validation, the health check and the locator resolve over each.
3. **Integration** against Marten on PostgreSQL with the Newtonsoft serializer: round trip, forgotten subject, export through the locator.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
CONTEXT:
PRs 1 to 5 are merged. The serializer and every consumer of it exist; the factory does not dispatch to Newtonsoft yet.

TASK:
Dispatch by serializer type in the factory (extend the already-wrapped early return), log 8495, update the two XML documentation
remarks, and add the DI test (ValidateOnBuild, ValidateScopes), the installation test twins and the Marten integration test.

KEY RULES:
- Unknown serializers still throw SerializerNotSupported; no plaintext fallback.
- Integration tests use the shared Marten collection fixture.
- Changelog fragment through mechanical-fixer, section added; never edit CHANGELOG.md.

REFERENCE FILES:
- src/Encina.Marten.GDPR/Serialization/CryptoShredderSerializerFactory.cs
- src/Encina.Marten.GDPR/ServiceCollectionExtensions.cs
- tests/Encina.UnitTests/Marten/GDPR/Nested/CryptoShredderSerializerInstallationTests.cs
- tests/Encina.IntegrationTests/Infrastructure/Marten/GDPR/CryptoShredderSerializerIntegrationTests.cs
```

</details>

### Phase 7: Documentation (pull request 7)

<details>
<summary><strong>Tasks</strong></summary>

PR 7 holds exactly these 4 files, all written by `docs-writer`:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Marten.GDPR/README.md` | modify (shapes, Newtonsoft support, plaintext keys, forgotten-subject element semantics) |
| 2 | `docs/features/crypto-shredding.md` | modify (the Newtonsoft serializer, the choice between the two, the answers to Design Choices 3, 5, 9 and 11) |
| 3 | `docs/INVENTORY.md` | modify |
| 4 | `docs/architecture/adr/034-crypto-shredding-through-the-stj-contract.md` | modify (addendum, only if the maintainer chooses option A of Design Choice 9 and the Newtonsoft path changes the documented scope of the serializer; no new ADR number) |

Tasks: the documentation states the supported shapes, that dictionary keys are plaintext and survive an erasure, the forgotten-subject element semantics and the serializer choice. No coverage figure is typed by hand.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
CONTEXT:
PRs 1 to 6 are merged. Everything the documentation describes exists.

TASK:
Ask docs-writer for the package README, the feature page, INVENTORY.md and, when the maintainer chose option A of Design Choice 9,
the ADR-034 addendum.

KEY RULES:
- Documentation never types coverage figures; state the plaintext dictionary keys and the element-count behavior for forgotten subjects.
- Public text never names a private client application.

REFERENCE FILES:
- .claude/skills/encina-docs/SKILL.md
- docs/features/crypto-shredding.md
- docs/architecture/adr/034-crypto-shredding-through-the-stj-contract.md
```

</details>

### Phase 8: Observability, logging and coverage targets

<details>
<summary><strong>Tasks</strong></summary>

1. **EventIds** (existing range `MartenGDPRCryptoShredding` 8450-8499, `EventIdRanges.cs:338`; 8450-8484 are used, 8485-8491 are claimed by #2455 and #2457 in `eventid-blocks.md`, 8491 only if #2457 keeps its UserId warning): all four go in `Diagnostics/CryptoShreddingLog.cs` as `[LoggerMessage]` methods in the PR that logs them: PR 1 takes 8492 `CollectionContractBuilt` (Debug: owner type name, field name and kind) and 8493 `CollectionRebuiltOnRead` (Debug: counts only); PR 3 takes 8494 `NewtonsoftContractBuilt` (Debug); PR 6 takes 8495 `NewtonsoftSerializerWrapped` (Information). If #2457 drops its 8491 warning the block starts at 8491 instead (the only change is the four literal ids). 8496-8499 stay free. No new range, so `EncinaEventIdAllocationTests` is unchanged. Logs carry type and property names and counts, never an element, a key or an exception message.
2. **Telemetry**: the existing crypto-shredding counters and activities (`CryptoShreddingDiagnostics`) are reused; the encryption and decryption counters count elements, and the activity tags are unchanged. The key scope is the subject, not the tenant, so no tenant tag is added (ADR-054 D4 applies where multi-tenancy is enabled; the tenancy of subject keys is the existing behavior and is not changed here). No tenant-qualified id is produced, so the reserved `default` tenant segment and injective-id rules do not apply.
3. **Error categories (ADR-062, merged)**: no `EncinaErrors.Create` or `FromException` call is added (Design Choice 12); the existing codes keep their categories.
4. **Fail-closed gates**: unsupported shapes are rejected at startup; an element that cannot be decrypted fails the owner; an unknown serializer is rejected by the factory, the startup validation, the health check and the locator.
5. **Coverage targets (per flag, every new or touched file under `src/`)**. Flags that cannot reach a file carry a 0 with the reason; `Encina.GuardTests` has no `InternalsVisibleTo` (csproj lines 26-30 list `Encina.UnitTests`, `Encina.IntegrationTests`, `Encina.ContractTests`, `Encina.PropertyTests` and the benchmarks), so the guard flag reaches public API only. Each target is entered by `mechanical-fixer` in the same PR as the tests that meet it; each PR records the measured baseline with the command of AGENTS.md section 9 and sets a target at that value or higher; existing targets are never lowered.

| PR | File | Unit | Guard | Contract | Property | Integration | Justification |
|----|------|:----:|:-----:|:--------:|:--------:|:-----------:|---------------|
| 1 | `Metadata/CryptoShreddedPropertyClassifier.cs` | 90 | 0 | 0 | 0 | 0 | Unit: every shape rule branch (four shapes accepted, `HashSet`, `ImmutableArray`, `IEnumerable` rejected) is driven by classifier tests; guard 0: internal static type with no `InternalsVisibleTo` for the guard project; contract, property and integration 0: not a public contract and reached only indirectly. |
| 1 | `Metadata/CryptoShreddedField.cs` | 90 | 0 | 0 | 0 | 0 | Unit: `Kind` derivation for the five kinds is tested through the classifier and the collection tests; guard 0: internal class. |
| 1 | `CryptoShreddedPropertyIssue.cs` | 95 | 0 | 0 | 0 | 0 | Unit: a test iterates every problem flag through `DescribeFlag`; guard 0: the public record has no validated parameter. |
| 1 | `CryptoShreddedPropertyProblems.cs` | 0 | 0 | 0 | 0 | 0 | A `[Flags]` enum with no executable code. |
| 1 | `Serialization/CryptoShreddingContractModifier.cs` | 90 | 0 | 0 | 0 | 0 | Unit: `WrapGetter` per kind with a fake key provider; guard 0: internal class. |
| 1 | `Serialization/CryptoShreddingEngine.cs` | 90 | 0 | 0 | 0 | 0 | Unit: `EncryptCollectionForWrite` null, empty, element and failure paths; guard 0: internal class. |
| 1 | `Serialization/CryptoShreddingEngine.Read.cs` | 95 | 0 | 41 | 56 | 67 | Existing targets are kept (target-ratchet) and the collection branch is covered by the new unit tests so the unit target holds. |
| 1 | `Attributes/CryptoShreddedAttribute.cs` | 0 | 0 | 0 | 0 | 0 | Documentation-only change: no executable code is added; the file keeps its existing coverage. |
| 1 | `Diagnostics/CryptoShreddingLog.cs` | 90 | 0 | 0 | 0 | 0 | Unit: tests assert 8492 and 8493 through `FakeLogger`; guard 0: internal extension methods. |
| 2 | `Locator/CryptoShreddedGraphWalker.cs` | 90 | 0 | 0 | 0 | 0 | Unit: path suffix cases for arrays, lists and dictionaries; guard 0: internal static type. |
| 2 | `Serialization/CryptoShreddingEngine.cs` | 90 | 0 | 40 | 50 | 50 | Contract tests reach the write path through the serializer, the collection property test exercises generated shapes, the Marten integration test reaches the real key store. |
| 2 | `Serialization/CryptoShreddingEngine.Read.cs` | 95 | 0 | 45 | 60 | 70 | Raised from 41, 56 and 67 because the collection read branch is now exercised by the new contract, property and integration tests. |
| 2 | `Serialization/CryptoShreddingContractModifier.cs` | 90 | 0 | 40 | 40 | 50 | Contract, property and integration tests reach the wrapped getter with real contracts. |
| 3 | `Serialization/Newtonsoft/CryptoShreddingContractResolver.cs` | 90 | 0 | 0 | 0 | 0 | Unit: property wrapping and the `OnDeserializedCallbacks` entry driven over `JsonSerializerSettings`; guard 0: internal type; integration is raised in PR 6. |
| 3 | `Serialization/Newtonsoft/CryptoShreddingValueProvider.cs` | 95 | 0 | 0 | 0 | 0 | Unit: `GetValue` per kind and `SetValue` delegation; guard 0: internal type. |
| 3 | `Metadata/Newtonsoft/NewtonsoftCryptoContractRules.cs` | 95 | 0 | 0 | 0 | 0 | Unit: one case per rule; guard 0: internal static rules. |
| 3 | `Diagnostics/CryptoShreddingLog.cs` | 90 | 0 | 0 | 0 | 0 | Unit: 8494 asserted; the row keeps the PR 1 target. |
| 4 | `Serialization/Newtonsoft/CryptoShredderNewtonsoftSerializer.cs` | 90 | 40 | 80 | 0 | 0 | Unit: round trips and failure staging; guard: the constructor and write-member null and blank checks are reachable through the public API; contract: every `ISerializer` member; property 0: thin delegation to the shared engine whose invariants PR 2 covers; integration is raised in PR 6. |
| 5 | `Serialization/ICryptoShreddingInstallation.cs` | 0 | 0 | 0 | 0 | 0 | An internal interface with no executable code. |
| 5 | `Serialization/CryptoShredderSerializer.cs` | 85 | 30 | 60 | 60 | 60 | Existing #1698 tests reach it; the PR records the measured baseline and sets the targets at or above it. |
| 5 | `Locator/CryptoShreddedNewtonsoftGraphWalker.cs` | 90 | 0 | 0 | 0 | 0 | Unit: path cases over Newtonsoft contracts; guard 0: internal static type. |
| 5 | `CryptoShreddingStartupValidationHostedService.cs` | 94 | 0 | 0 | 0 | 0 | The existing unit target is kept (target-ratchet); guard 0: internal class. |
| 5 | `Health/CryptoShreddingHealthCheck.cs` | 90 | 40 | 0 | 0 | 0 | Unit: healthy, wrong serializer and installation-failure paths for both serializers; guard: the public constructor checks. |
| 5 | `Locator/MartenEventPersonalDataLocator.cs` | 85 | 40 | 0 | 0 | 0 | Unit: the interface branch and the unsupported-serializer `Left`; guard: public constructor and `subjectId` checks. |
| 6 | `Serialization/CryptoShredderSerializerFactory.cs` | 90 | 60 | 0 | 0 | 50 | Unit: three branches plus idempotency; guard: the four `ThrowIf*` lines and the success path are public; contract and property 0: a static class with no contract; integration: reached by every store the integration tests install. |
| 6 | `ServiceCollectionExtensions.cs` | 80 | 50 | 0 | 0 | 0 | XML documentation only; the DI test covers registration (unit); guard: existing public-argument checks. |
| 6 | `ConfigureMartenCryptoShredding.cs` | 90 | 0 | 0 | 0 | 0 | XML documentation only; the DI test resolves and runs it; guard 0: internal class. |
| 6 | `Serialization/Newtonsoft/CryptoShreddingContractResolver.cs` | 90 | 0 | 0 | 0 | 60 | Integration raised from 0: the Marten test builds the real Newtonsoft contracts; the unit target is kept. |
| 6 | `Serialization/Newtonsoft/CryptoShreddingValueProvider.cs` | 95 | 0 | 0 | 0 | 60 | Integration raised from 0: every encrypted write of the Marten test goes through the value provider; the unit target is kept. |
| 6 | `Serialization/Newtonsoft/CryptoShredderNewtonsoftSerializer.cs` | 90 | 40 | 80 | 0 | 60 | Integration raised from 0: the Marten test appends and reads events through the serializer; the other targets are kept. |
| 6 | `Diagnostics/CryptoShreddingLog.cs` | 90 | 0 | 0 | 0 | 0 | Unit: 8495 asserted; the row keeps the earlier target. |

6. **Load and benchmark**: not written; the cost is one AES-GCM operation per element, already measured by `Encina.Marten.GDPR.Benchmarks`; the manifest `reason` points there. The contract tests are written (PR 2 for System.Text.Json, PR 4 for Newtonsoft); no contract `.md` justification is used.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```
CONTEXT:
Code and tests of the pull request being worked on exist. Logs, targets and coverage remain per pull request.

TASK:
Add the log messages to the PR that uses each (8492-8493 in PR 1, 8494 in PR 3, 8495 in PR 6), measure per-flag coverage of every
touched file, and record targets with one-sentence justifications through mechanical-fixer; run --check-justifications.

KEY RULES:
- EventIds only 8492-8495, packed; no range registration is needed.
- Never lower an existing target (target-ratchet); never put an element, key or exception message in a log.

REFERENCE FILES:
- src/Encina.Marten.GDPR/Diagnostics/CryptoShreddingLog.cs
- .github/coverage-manifest/Encina.Marten.GDPR.json
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

### Phase 9: Finalization

<details>
<summary><strong>Tasks</strong></summary>

1. XML documentation with examples on every new public type (`CryptoShredderNewtonsoftSerializer`) and on `CryptoShreddedAttribute` (the supported shapes and the plaintext dictionary keys).
2. The two changelog fragments named in the phase tables (PR 1 and PR 6; the other pull requests add none because their change is internal or documentation of those two).
3. Verification per pull request: `dotnet build --configuration Release` with zero warnings; `dotnet test` of the unit, guard, contract and property projects; the integration tests with Docker; per-flag coverage with `coverage-report.cs` and `--check-justifications`; the CRAP table of changed methods; `git diff --stat origin/main...HEAD` against the file list of the phase.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 9</strong></summary>

```
CONTEXT:
All code of phases 1 to 8 is merged or ready. Finalization remains.

TASK:
Confirm XML documentation, the changelog fragments and a clean Release build; ask docs-writer for any page not covered by Phase 7.

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
| ADR-062 | Error category contract (merged, PR #3599); no new error code here |
| AGENTS.md section 3 | Event-sourced compliance modules: integration tests through Marten on PostgreSQL with Testcontainers; fail closed |
| AGENTS.md section 9 | Per-flag targets, FsCheck through `Encina.Testing.FsCheck`, contract tests for public API |
| #1698 plan | Token, associated data, subject resolution, fail-closed read semantics |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in This Feature |
|-----------|----------|----------------------|
| `CryptoShreddedPropertyClassifier` and `CryptoShreddedContractRules` | `src/Encina.Marten.GDPR/Metadata` | Shape rules to extend |
| `CryptoShreddedPropertyIssue` | `src/Encina.Marten.GDPR` | Message per problem flag |
| `CryptoShreddingContractModifier` | `src/Encina.Marten.GDPR/Serialization` | Getter wrapper and `OnDeserialized` chain |
| `CryptoShreddingEngine` (write and read) | `src/Encina.Marten.GDPR/Serialization` | Encrypt, decrypt, forgotten-subject handling |
| `CryptoShreddingFieldCipher`, `CryptoShreddingToken` | `src/Encina.Marten.GDPR/Serialization` | Unchanged cryptography and wire format |
| `CryptoShreddedGraphWalker`, `MartenEventPersonalDataLocator` | `src/Encina.Marten.GDPR/Locator` | Erasure and export paths |
| `CryptoShredderSerializerFactory` | `src/Encina.Marten.GDPR/Serialization` | Dispatch by serializer |
| `CryptoShreddingStartupValidationHostedService`, `CryptoShreddingHealthCheck` | `src/Encina.Marten.GDPR` | Consumers of the installed serializer |
| `InMemorySubjectKeyProvider` | `src/Encina.Marten.GDPR/KeyStore` | Test key provider |
| `CryptoShredderSerializerContractTests` | `tests/Encina.ContractTests/Marten/GDPR` | Template of the Newtonsoft contract tests |
| `MartenCollection`, `MartenFixture` | `tests/Encina.IntegrationTests/Infrastructure/Marten` | Shared integration fixture |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Marten.GDPR` | 8450-8499 (existing) | 8450-8484 used; 8485-8491 claimed by #2455 and #2457; this plan takes 8492-8495 in `Diagnostics/CryptoShreddingLog.cs`; 8496-8499 free |

No new range is registered.

### Estimated File Count

File touches per category over the seven pull requests (a file edited by several pull requests counts once per pull request):

| Category | New Files | Modified Files | Notes |
|----------|-----------|----------------|-------|
| Production code | 6 | 24 | New: resolver, value provider, Newtonsoft rules, Newtonsoft serializer, `ICryptoShreddingInstallation`, Newtonsoft graph walker. Modified touches: PR 1 (10: problems, issue, classifier, field, modifier, engine, engine read, attribute, log, PublicAPI), PR 2 (1: walker), PR 3 (3: props, csproj, log), PR 4 (1: PublicAPI), PR 5 (5: both serializers, startup validation, health check, locator), PR 6 (4: factory, log, `ServiceCollectionExtensions`, `ConfigureMartenCryptoShredding`) |
| Tests | 13 | 3 | New: collection unit, locator unit, property, integration (Part A); Newtonsoft resolver, rules, serializer, guard, contract, consumers, locator, registration, integration (Part B). Modified: classifier tests, STJ contract tests, installation tests |
| Documentation | 0 | 5 | `docs/features/crypto-shredding.md` (PR 1 and PR 7), README, `INVENTORY.md`, ADR-034 addendum (conditional) |
| Configuration | 2 | 6 | Changelog fragments in PR 1 and PR 6; the manifest in PR 1 to 6 |
| **Total** | **21** | **38** | 7 pull requests: 15, 6, 9, 6, 10, 9 and 4 files = 59 touches; each at most 15 files and 1500 lines |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Complete Implementation Prompt - All Phases</strong></summary>

```
PROJECT CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0, no backward compatibility. Issue #1771 adds [CryptoShredded] string collections and a
Marten Newtonsoft adapter to Encina.Marten.GDPR. Both are fail closed today (NotString, SerializerNotSupported).

IMPLEMENTATION OVERVIEW:
1. Part A (PRs 1 and 2): classify string[], List<string>, IReadOnlyList<string> and Dictionary<string,string> values; wrap the getter
   to return a same-typed collection of v2 tokens (one per element, the owner's subject); decrypt and rebuild through the compiled
   setter; locator paths Tags[] and Notes{}.
2. Part B (PRs 3 to 7): spike first; then the Newtonsoft contract resolver, value provider and rules (PR 3), the
   CryptoShredderNewtonsoftSerializer (PR 4), the serializer-neutral consumers (PR 5), the factory dispatch with registration and the
   integration test (PR 6) and the documentation (PR 7).
3. EventIds 8492-8495 in the existing range, in Diagnostics/CryptoShreddingLog.cs.
Seven pull requests in phase order.

KEY PATTERNS:
- Fail closed everywhere: an unsupported shape is rejected at startup; an element that cannot be decrypted fails the owner.
- The token, cipher and associated data never change; a store can switch serializers without migrating data.
- Never log or put in a message a value, a key or an element; dictionary keys are plaintext by contract.
- Apply Design Choices 3, 5, 9 and 11 as the Maintainer Decisions section says.

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
| 4 | Health Checks | ✅ Include | The startup validation rejects unsupported shapes and an unwrapped serializer; the health check and the locator learn the Newtonsoft serializer in PR 5. |
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
| #1699 key provider races | ✅ Closed | Collections inherit its behavior unchanged |
| ADR-062 error category contract | ✅ Merged (PR #3599) | No new error code is added |
| #2455, #2457 | ⏭️ Implementation pending | Claim EventIds 8485-8491; this plan starts at 8492 |
| Marten 9.38.0 Newtonsoft serializer package | ❓ Unverified | The spike of Phase 3 task 1 verifies it before any code |
| Maintainer answers to Design Choices 3, 5, 9 and 11 | Pending | Asked when work starts |

No blocking prerequisites for Part A. Part B is blocked until the spike confirms the package.

---

## Next Steps

1. The orchestrator asks the maintainer the Dilemmas (Design Choices 3, 5, 9 and 11) when work starts, records the answers under `## Maintainer Decisions`, then briefs one worker per pull request in phase order (Part B only after the spike; pull request 5 follows the answer to Design Choice 11).
2. After merge, comment on #1771 with the result and on #1698's follow-up list that items 1 and 2 are done.
