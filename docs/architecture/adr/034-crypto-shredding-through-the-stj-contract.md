# ADR-034: Crypto-Shredding Runs Through the System.Text.Json Contract

## Status

**Accepted** - decided in issue #1698 (2026-10-05), implemented in `Encina.Marten.GDPR`. Supersedes the mechanism part of [ADR-030](030-encryption-at-the-serializer-level.md) for Marten: ADR-030's decision that encryption is applied by decorating the serializer still holds as a principle, but `Encina.Marten.GDPR` no longer encrypts by overwriting properties around an inner `ISerializer` call, and its field format is no longer `ENC:v1` / a JSON envelope with a `kid`. The key-management and algorithm decisions of ADR-030 (AES-256-GCM, key management behind `ISubjectKeyProvider`) are unchanged. ADR-020 (temporal crypto-shredding audit store) is unaffected.

## Context

`CryptoShredderSerializer` decorated Marten's `ISerializer`. It scanned only the top-level event type, replaced each `[CryptoShredded]` string with a JSON envelope before serialization and restored the original afterwards. Consequences found in #1698:

- A `[CryptoShredded]` property on a nested object, a collection element, a dictionary value or a non-public member was stored in plaintext, and nothing rejected it. The event store is append-only, so plaintext written once can never be shredded.
- The envelope kept the subject id inside the immutable event (`kid`), so erasure left the identity behind.
- The read path turned every failure (key-store outage, tampering, missing key) into the anonymized placeholder, so an outage could bake the placeholder into projections and, once snapshots are encrypted, overwrite real data on the next save. This contradicts the rule that errors are never swallowed in background infrastructure and that compliance gates fail closed (AGENTS.md section 3; SPEC-002 DEC-006).
- Misconfiguration reported property names only, never the reason.

The maintainer chose to implement full nested encryption rather than reject nested types. The detailed design and the options weighed are in [the implementation plan](../../plans/crypto-shredding-nested-implementation-plan-1698.md); this record keeps the decisions.

## Decision

- **Mechanism: a System.Text.Json contract modifier, not a graph-walking decorator.** A modifier is installed on every `JsonSerializerOptions` of Marten's `SystemTextJsonSerializer`. Encryption happens in a wrapped `JsonPropertyInfo.Get`, which receives the declaring object and returns a ciphertext token, so the caller's object is never mutated and no process-wide lock is needed. Decryption starts in `JsonTypeInfo.OnDeserialized`, which fires for every constructed owner at any depth and decrypts through compiled setters. STJ recurses on its own, so nested objects, collections, dictionary values, polymorphic members, records and closed generics are covered by the same contract.
- **Subject: the sibling `SubjectIdProperty` on the declaring object, at every depth.** There is no ancestor lookup. A nested value object that holds `[CryptoShredded]` data carries its own subject-id property. This is the documented contract of `CryptoShreddedAttribute`, and STJ hooks expose only the declaring object.
- **One classifier with per-property problem flags.** A single classifier (reflection rules plus STJ-contract rules) returns `CryptoShreddedPropertyProblems` flags. The runtime modifier, the startup validator, the health check and `CryptoShreddingConfigurationException` all use it. Every shape that would store plaintext or fail to round-trip is rejected, and STJ caches the modifier's failure for the type, so a rejected type stays rejected for the life of the process.
- **Wire format: the v2 token with associated data and no subject id.** A value is `cs2:{version}:{base64url(nonce || ciphertext || tag)}`; the tombstone is `cs2:erased`. The AES-GCM associated data binds the subject and the key version, not the property name, so a token copied onto another subject's object fails the integrity check and renaming a property does not break decryption. Parsing is strict. No v1 reader exists (pre-1.0).
- **Reads fail closed.** Only a genuinely forgotten subject reads as the placeholder. A tombstone is accepted only after `ISubjectKeyProvider.IsSubjectForgottenAsync` confirms the subject is forgotten. Every other failure throws `CryptoShreddingDecryptionException`. The configurator sets Marten's `Projections.Errors.SkipSerializationErrors` to `false`, so a decryption failure pauses the async daemon shard instead of dead-lettering the event, and the startup validator fails if a later configuration turns it back on.
- **Keys through `IServiceScopeFactory`, per call.** The serializer resolves `ISubjectKeyProvider` and `IForgottenSubjectHandler` in a DI scope created lazily on the first key need of a call. Keys are cached per call only, never across calls, and the frame never retains arrays a provider returned.
- **System.Text.Json only.** A non-STJ inner serializer throws `CryptoShreddingConfigurationException(SerializerNotSupported)` at configuration; it is never passed through unencrypted. Marten's raw `JsonDocument` upcasters are not supported (they cast the serializer to the concrete type); a stream that uses one fails to load.
- **Installation check by identity, chain and output.** At startup, in the health check and lazily on the first write, the serializer verifies that each captured options object still holds the exact resolver instance it installed with an unchanged `TypeInfoResolverChain`, and that a nested canary serializes to a `cs2` token. A canary alone is not enough, because a user `JsonSerializerContext` placed ahead of the modifier never resolves the canary type. This is why `UseTypeInfoResolver(context)` must run before `AddEncinaMartenGdpr`.
- **Erasure routing.** `AddEncinaMartenGdpr` registers an internal routing `IDataErasureStrategy`: locations produced by `MartenEventPersonalDataLocator` always reach `CryptoShredErasureStrategy`, every other location goes to the strategy that was registered before (its inner strategy). The DSR executor resolves exactly one strategy for all locations, so without the router a custom strategy would report success while Marten data stays decryptable. A strategy registered after `AddEncinaMartenGdpr` bypasses the router and the startup validator stops the host. Crypto-shred erasure is idempotent on `crypto.subject_forgotten`.

## Rationale

- The contract modifier proves coverage by construction (what STJ writes is what is encrypted) and turns every other shape into a named, permanent rejection. The decorator's coverage could only be documented, not verified, and it needed a lock around Marten's serialization and a visible mutation of the caller's object.
- Fail-closed reads follow from existing rules, not from a new product choice. Its cost (reads stop during a key-store outage) is the intended behaviour.
- Keeping the subject out of the token removes the identity from the immutable log, which is what makes erasure meaningful.

## Alternatives rejected

- **Keep the decorator and walk a per-type plan graph, mutating and restoring.** Needs a process-wide gate, exposes envelopes to other threads, and leaves collections that yield copies as undetectable plaintext holes.
- **A bare contract modifier that returns early for non-object kinds.** A type-level `[JsonConverter]` writes the property in clear, and nothing detects a modifier dropped by a later `UseTypeInfoResolver`.
- **Ancestor lookup of the subject id.** `Get` and `OnDeserialized` do not see ancestors, and a value object shared by two subjects would be ambiguous.
- **A Newtonsoft adapter now.** No project references `Marten.Newtonsoft`; it would add a dependency and a second test matrix. It is a follow-up.
- **Passing a non-STJ serializer through unencrypted.** Fail-open; forbidden.

## Consequences

- Rejected shapes (structs, getter-only properties, computed members beside an owner, converters over the crypto graph, personal data declared on interfaces, source-generated contexts in serialization mode and others) need a small change in user code; the [feature page](../../features/crypto-shredding.md) lists the flags.
- Limits that no serializer hook can cover are documented, not enforced: Marten `Patch`, duplicated fields, raw `JsonDocument` upcasters.
- Erasure is subject-wide whatever `FieldName` or `ErasureScope.SpecificFields` say, until the key model changes (#1144). The key model, a durable default key store and tenant-scoped keys belong to #1144 and #1191 and will change the token and its associated data; v2 reserves no key scope.
- With the in-memory key store (the default) every key is lost on restart and older encrypted data then fails to read, instead of reading as the placeholder. The startup validator logs warning 8484 when it is in use.
- Readers of the whole event store that deserialize through the crypto serializer (`MartenEventMetadataQuery`, `MartenProjectionManager`, the DSR locator) fail when any event they return cannot be decrypted. The locator limits decryption to the requested subject.
- Snapshots and read models are encrypted for the first time. `Encina.DomainModeling` base types gain the serialization attributes that make them valid owners.

## References

- Issue #1698; [implementation plan](../../plans/crypto-shredding-nested-implementation-plan-1698.md).
- [ADR-030](030-encryption-at-the-serializer-level.md) (superseded in part), ADR-020, ADR-019, [SPEC-002](../../specifications/SPEC-002-eu-regulatory-readiness.md) DEC-006.
- Open issues that own the key model: #1144, #1191.
- [Crypto-shredding feature page](../../features/crypto-shredding.md).
