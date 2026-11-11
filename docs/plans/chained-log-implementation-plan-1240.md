# Implementation Plan: `Encina.ChainedLog` — Persistent Hash-Chained Log on the 10 Database Providers, with Attestation Built on It

> **Issue**: [#1240](https://github.com/dlrivada/Encina/issues/1240) (SPEC-002 tracking id **P-38**, priority **P0**)
> **Type**: Feature
> **Complexity**: High (10 phases, 10 database providers, new package, ~60 files, 5 pull requests)
> **Estimated Scope**: ~2,200-2,900 lines of production code + ~3,000-3,800 lines of tests
> **Milestone**: v0.17.0 — Compliance Lifecycle
> **Parent EPIC**: [#1186](https://github.com/dlrivada/Encina/issues/1186) — EU regulatory readiness (SPEC-002)
> **Specification**: [SPEC-002](../specifications/SPEC-002-eu-regulatory-readiness.md) REQ-047, REQ-021, AC-039, scenario S8, DEC-002 (no Verifactu code), INV-004; [ADR-057](../architecture/adr/057-verifactu-boundary.md) (the boundary: D11, D12, D23); cross-cutting REQ-061 (AC-043) and REQ-062 (AC-044)
> **Related**: [#860](https://github.com/dlrivada/Encina/issues/860) (the in-memory `HashChainAttestationProvider`), [#2925](https://github.com/dlrivada/Encina/issues/2925) (architecture test of neutrality), [#2926](https://github.com/dlrivada/Encina/issues/2926) (neutrality review checklist), P-36 (gapless sequence primitive, a sibling), P-24 (the ADR-057 work, merged)

---

## Summary

Ley 41/2002 art. 14 (authenticity of the clinical record and of its changes) and LOPDGDD art. 32.4 (digital evidence) need tamper-evident history that survives a restart. Encina's only chaining code, `HashChainAttestationProvider` (`src/Encina.Compliance.Attestation/Providers/`), keeps its chain in memory (a `List<AttestationReceipt>` behind a `Lock`, with a random HMAC key when none is configured, and a `HashChainOptions.StoragePath` property marked "reserved for future persistence"). After a restart the chain is gone, a verification of an older receipt cannot succeed, and scenario S8 (invoice records whose chain survives restarts) cannot pass.

This plan delivers a **regulation-neutral persistent hash-chained log**:

1. A new package **`Encina.ChainedLog`** with `IChainedLog` (append, verify, head, read) and `IChainHasher` (pluggable), `ChainedLogEntry`, verification result and errors, an in-memory implementation for tests, and two generic hashers (plain SHA-256 and keyed HMAC over a documented canonical form). One chain per `(tenant, partition key)`; entries numbered gaplessly from 1.
2. **Ten provider stores** (`ChainedLogStoreADO`, `ChainedLogStoreDapper` on SqlServer, PostgreSQL and MySQL, `ChainedLogStoreEF` for the three EF Core databases, `ChainedLogStoreMongoDB`) that serialise appends per chain by locking the chain's head row inside the caller's transaction.
3. **Attestation on the persistent log**: `HashChainAttestationProvider` stops keeping its own list and appends to `IChainedLog` with the HMAC hasher; the in-memory attestation provider stays for tests.
4. **ADR-057 conformance** (the boundary of SPEC-002 DEC-002): no field name, format, endpoint, preset or hasher of the Spanish invoicing-record regime; no option named after a regulation; one README sentence stating the boundary (D23).

**Standards**: Ley 41/2002 art. 14; LOPDGDD art. 32.4; GDPR Art. 5(1)(f), Art. 32 (integrity); SPEC-002 REQ-047, REQ-021, AC-039, S8; ADR-057.

**Affected packages**: new `Encina.ChainedLog`; `Encina.Compliance.Attestation`; `Encina.ADO.{SqlServer,PostgreSQL,MySQL}`, `Encina.Dapper.{SqlServer,PostgreSQL,MySQL}`, `Encina.EntityFrameworkCore`, `Encina.MongoDB`; `Encina` (core: one EventId range).

**Provider category**: Database (10): ADO.NET x3, Dapper x3, EF Core x3 (one package, three databases), MongoDB (AGENTS.md section 5).

**Size**: about 60 files, so five worker pull requests: PR 1 (`Encina.ChainedLog` core: abstractions, in-memory log, hashers, verifier, options, DI, diagnostics, unit and guard tests); PR 2 (ADO.NET x3); PR 3 (Dapper x3); PR 4 (EF Core and MongoDB); PR 5 (Attestation on the log, cross-provider contract, property, integration and load tests, documentation). Each stays within 15 files and 1,500 lines.

---

## Design Choices

<details>
<summary><strong>1. Package Placement — a new package <code>Encina.ChainedLog</code></strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) New package `Encina.ChainedLog` holding the abstractions, hashers, verifier and the in-memory log; provider stores in the provider packages; `Encina.Compliance.Attestation` references it** | A neutral primitive with its own README and boundary sentence; provider packages do not depend on a compliance package; reusable beyond compliance | One more package |
| **B) Put the abstractions inside `Encina.Compliance.Attestation`** | No new package | The ten provider packages would reference the attestation package; a general primitive hidden in a compliance package |
| **C) Put them in core `Encina`** | No new package | Grows core for an optional feature (pay-for-what-you-use) |

### Chosen Option: **A — new package**

### Rationale

- ADR-057 (D23) requires "one sentence in the README of each new primitive package" and speaks of "the sequence or chain package": the chained log is planned as a package of its own, like the gapless sequence (P-36).
- Providers differ only in the store implementation; the abstractions and options stay in the shared package (AGENTS.md section 3, provider coherence).

**Class:** Settled
**Settled by:** ADR-057 section "Where the boundary is published (D23)" (a README sentence "in each new primitive package"; "the sequence or chain package"); AGENTS.md section 1 (pay-for-what-you-use) and section 3 (provider coherence)

</details>

<details>
<summary><strong>2. What an Entry Stores — digests and an opaque reference, not content</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `TenantId`, `PartitionKey`, `Sequence`, `PayloadHash` (digest the caller computed), optional `Reference` (opaque, unique per chain when set), small `Attributes`, `PreviousHash`, `EntryHash`, `AppendedAtUtc`** | No personal data or document content in the log; matches `AttestationReceipt`, which stores a content hash only; verification of a document is done by recomputing its digest | A reader needs the document elsewhere to check content |
| **B) Also store the payload** | Self-contained evidence | Personal data (clinical or invoice content) duplicated into a log that is append-only and cannot be erased (GDPR Art. 17, Art. 5(1)(c)) |

### Chosen Option: **A — digest, reference and attributes**

### Rationale

- `Attributes` (at most a few ordinal-sorted key/value strings, for example a correlation id) are part of the canonical form, so an edit to them breaks the chain.
- `Reference` makes append idempotent: appending the same `Reference` to the same chain returns the existing entry (Attestation's "same `RecordId` returns the existing receipt"), enforced by a unique index on `(TenantId, PartitionKey, Reference)` where `Reference` is not null.
- `VerifyPayloadAsync(entry, payloadHash)` lets a caller prove that a document still matches its entry.

**Class:** Settled
**Settled by:** Existing convention: `src/Encina.Compliance.Attestation/Model/AttestationReceipt.cs` (content hash only, ADR-022); GDPR Art. 5(1)(c) (data minimisation) and Art. 17; AGENTS.md section 3 (no personal data where it is not needed)

</details>

<details>
<summary><strong>3. Hashing — pluggable <code>IChainHasher</code>, two generic hashers, nothing regulation-shaped</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IChainHasher` with `ComputeEntryHash(ChainLink link)` over a documented canonical form (length-prefixed fields, UTF-8, fixed order: tenant, partition, sequence, previous hash, payload hash, reference, sorted attributes, appended time); shipped `Sha256ChainHasher` and `HmacChainHasher` (key from options, `[JsonIgnore]`, `ToString()` redacted)** | Generic and verifiable; the keyed variant defeats an administrator who edits rows and recomputes plain hashes; the application can supply its own hasher | Two shipped hashers to test |
| **B) Only an unkeyed hash** | Simplest | A database administrator can rewrite the whole chain consistently |
| **C) A hasher abstraction with no shipped hasher** | Zero chance of shipping something regulation-shaped | Unusable out of the box; every application writes a hasher |

### Chosen Option: **A — canonical form with a plain and a keyed hasher**

### Rationale

- ADR-057 D11: a primitive is neutral when it has no regulation field names, formats or endpoints, "ships no default, preset or hasher that reproduces the RRSIF concatenation", and has no regulation-named option. The canonical form above is generic (length-prefixed, tenant and partition based) and is not the concatenation of any regulation's field list; no type, option, constant, sample or test fixture names Verifactu, RRSIF, AEAT, invoice or tax.
- The issue says "SHA-256 default": SHA-256 is the algorithm of the default hasher. The comment on the issue repeats D11 ("no default, preset or hasher reproducing the RRSIF concatenation"), which is satisfied: the default is generic, and an application that needs a regulation-specific concatenation writes its own `IChainHasher` and carries that responsibility (ADR-057, D20).
- Neutrality is checked mechanically by the architecture test of #2925 and by the review checklist of #2926; this plan adds both packages (`Encina.ChainedLog`, the provider `ChainedLog/` folders) to the allow-list scope those issues define and its own tests assert the canonical form with fixed vectors.

**Class:** Settled
**Settled by:** ADR-057 D11, D12, D20 and D23; SPEC-002 DEC-002 and INV-004; the issue comment of 2026-10-17 ("regulation-neutral primitive; Encina ships no Verifactu code ... no regulation-named option")

</details>

<details>
<summary><strong>4. Serialising Appends — lock the chain's head row inside the caller's transaction</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A head row per chain (`ChainedLogHeads`: tenant, partition, last sequence, last hash), locked for update inside the caller's transaction (`UPDLOCK, HOLDLOCK` on SQL Server; `SELECT ... FOR UPDATE` on PostgreSQL and MySQL; a transactional `findOneAndUpdate` on MongoDB), then the entry insert and head update; the first append creates the head with an upsert that ignores conflicts** | Gapless by construction; a rollback of the caller's transaction rolls back the entry and the head; the lock lives exactly as long as the transaction | Appenders on the same chain wait for each other; a long caller transaction holds the chain |
| **B) `IDistributedLockProvider` around the append** | Provider-independent | The lock is released before the caller's transaction commits, so a rollback leaves a gap or a later append sees a stale head |
| **C) Optimistic: unique `(chain, sequence)` and retry on conflict** | No waiting | The caller's work is already done in its transaction; a retry means re-running the caller; a PostgreSQL unique violation aborts the whole transaction |

### Chosen Option: **A — head-row lock in the caller's transaction**

### Rationale

- The issue says it: "Append is serialised per chain inside the caller's transaction; verification detects any gap or edit."
- Connection and transaction handling follow the existing stores: ADO.NET and Dapper enlist through `IDbTransactionAccessor` (the outbox pattern), EF Core uses the scoped `DbContext` and its transaction, MongoDB uses `IMongoSessionAccessor`.
- MongoDB needs a replica set for transactions; on a standalone server the store returns `Left(chainedlog.transaction_required)` instead of appending without a guarantee.
- Retention: the log is append-only; there is no delete or update method in the abstraction.

**Class:** Settled
**Settled by:** The issue comment of 2026-10-17 and issue body ("serialised per chain inside the caller's transaction"); SPEC-002 REQ-047; AGENTS.md section 3 ("Database calls MUST be asynchronous with a CancellationToken")

</details>

<details>
<summary><strong>5. Append Without an Ambient Transaction — the default</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) When no ambient transaction exists, the store opens its own short transaction for the lock, entry and head (`ChainedLogOptions.RequireAmbientTransaction = false` by default); an application may set it to `true` to make an append without the caller's transaction a `Left(chainedlog.transaction_required)`** | Works for attestation of decisions that have no database transaction of their own (the attestation pipeline behavior); an invoice flow that already uses a unit of work gets its append in that transaction | An application that forgets its transaction gets an entry that commits even if its business write fails |
| **B) Always require an ambient transaction** | The entry and the business write always commit or roll back together | `AttestationPipelineBehavior` and other callers without a unit of work cannot append at all |

### Chosen Option: **A — own short transaction by default (recommended, pending the maintainer)**

### Rationale

- Scenario in which A wins: attestation of access-control decisions and other events that are not part of a business transaction; they need the chain to work with no unit of work.
- Scenario in which B wins: the reference application's invoice records (S8), where an entry that survives a rolled-back invoice would be a false record; with `RequireAmbientTransaction = true` the application chooses B explicitly, and the documentation says to do so for such flows.
- Recommendation A as the default because fail-closed behavior is preserved either way (the append returns `Left` when it cannot lock or write), and B is one option away.

**Class:** Dilemma
**Settled by:** none; awaits the maintainer

</details>

<details>
<summary><strong>6. Attestation on the Log — re-base the provider; fail closed without a persistent key</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `HashChainAttestationProvider` appends each attestation to `IChainedLog` with `HmacChainHasher` (partition key `attestation`, receipt fields derived from the entry: content hash = payload hash, attested time = appended time, chain index = sequence, signature = entry hash, correlation id = an attribute); `HashChainOptions.StoragePath` and its in-memory list are removed; start-up fails when a persistent store is registered and no HMAC key is configured** | One chain implementation; receipts survive restarts; verification works across restarts | A random per-start key (today's default) is no longer allowed with persistence |
| **B) Keep the provider in memory and add a second persistent provider** | No behavior change | Two chaining implementations; the in-memory one stays non-evidential |
| **C) Persist only the receipts and keep the chain logic in the provider** | Smaller change in Attestation | Duplicates what the log does, per provider; the head-row serialisation would not be reused |

### Chosen Option: **A — Attestation appends to the chained log**

### Rationale

- The issue: "Attestation uses it as its persistent backend; the in-memory provider stays for tests." `InMemoryAttestationProvider` is unchanged; the in-memory `IChainedLog` of `Encina.ChainedLog` backs unit tests of the hash-chain provider.
- An ephemeral key makes every receipt of a previous run unverifiable. With a persistent log, a missing key is a start-up error (fail closed, AGENTS.md section 3), not a warning.
- ADR-057 records `HashChainAttestationProvider` as already shipped and "stays as it is" as a position; this plan changes its storage, not its neutrality (it already holds no regulation concepts).

**Class:** Settled
**Settled by:** The issue comment of 2026-10-17 ("Attestation uses it as its persistent backend, the in-memory provider stays for tests"); SPEC-002 REQ-021 and REQ-047; AGENTS.md section 3 ("Compliance and security gates fail closed"); ADR-057 (position of `HashChainAttestationProvider`)

</details>

<details>
<summary><strong>7. Verification — what it proves, and what it cannot</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) On-demand `VerifyAsync(tenant, partition, fromSequence?, toSequence?)` that recomputes every hash, checks the sequence is gapless, checks the last entry against the head row, and returns the first broken sequence with a code; plus an optional hosted service that verifies configured chains periodically (off by default)** | Detects an edit, a deleted middle entry, a reordering and (with a keyed hasher) a rewrite; the periodic check is opt-in | Tail truncation together with an edit of the head row is undetectable without an external anchor |
| **B) Verify on every append** | Always checked | Appends become O(n) |
| **C) On-demand only, no hosted service** | Smallest | Nobody checks unless the application schedules it |

### Chosen Option: **A — on-demand verification and an opt-in periodic service**

### Rationale

- `GetHeadAsync` returns `(sequence, entryHash)` so an application can anchor the head outside the database (a signed file, another store); the documentation states that without an anchor, the strongest guarantee is against edits and middle deletions, and, with the keyed hasher, against an administrator who does not hold the key.
- A broken chain returns `Right(ChainVerification { IsValid = false, FirstBrokenSequence, Code })`, not `Left`: the verification ran; the evidence is bad. The failure is metered, logged at critical level, published as a notification, and reported by the health check as `Unhealthy`.
- The periodic service (`ChainedLogOptions.VerifyEvery`, null by default) runs inside a `PeriodicTimer` loop that catches per iteration and lets cancellation end the loop (AGENTS.md section 3, background loops).

**Class:** Trivial
**Settled by:** Existing convention: `IAuditAttestationProvider.VerifyAsync` returns `AttestationVerification` (valid or not) rather than `Left` for a bad receipt; AGENTS.md section 3 (background loops and timers)

</details>

---

## Implementation Phases

### Phase 1: Core Models, Abstractions and Errors (`Encina.ChainedLog`)

<details>
<summary><strong>Tasks</strong></summary>

#### `src/Encina.ChainedLog/` (new package)

1. **`Encina.ChainedLog.csproj`**, `PublicAPI.Shipped.txt`/`Unshipped.txt`, `README.md` (including the ADR-057 D23 sentence: "a regulation-neutral primitive; Encina ships no code for any specific regulation"), added to `Encina.slnx`; coverage manifest `.github/coverage-manifest/Encina.ChainedLog.json` (written by `mechanical-fixer`)
2. **`Model/ChainedLogEntry.cs`** — `TenantId`, `PartitionKey`, `Sequence` (long), `PayloadHash` (string, hex), `Reference?`, `Attributes` (read-only dictionary), `PreviousHash`, `EntryHash`, `AppendedAtUtc`
3. **`Model/ChainedLogAppend.cs`** — the caller's input: `PartitionKey`, `PayloadHash`, `Reference?`, `Attributes?` (tenant comes from the context)
4. **`Model/ChainHead.cs`, `Model/ChainVerification.cs`** — head `(Sequence, EntryHash)`; verification result `(IsValid, EntriesChecked, FirstBrokenSequence?, Code?)`
5. **`Abstractions/IChainedLog.cs`** — `AppendAsync(ChainedLogAppend, ct)`, `GetHeadAsync(partition, ct)`, `GetEntriesAsync(partition, fromSequence, take, ct)`, `VerifyAsync(partition, range?, ct)`, `VerifyPayloadAsync(entry, payloadHash, ct)`; all `Either<EncinaError, T>`
6. **`Abstractions/IChainedLogStore.cs`** — the provider-facing storage contract (lock head, insert entry, update head, read range); `IChainedLog`'s default implementation `DefaultChainedLog` composes store + hasher + verifier
7. **`Abstractions/IChainHasher.cs`, `Model/ChainLink.cs`** — hashing contract
8. **`ChainedLogErrors.cs`** — `chainedlog.append_failed`, `chainedlog.transaction_required`, `chainedlog.chain_broken`, `chainedlog.key_missing`, `chainedlog.invalid_partition`, `chainedlog.attributes_too_large`, `chainedlog.not_found` (codes and non-identifying details only)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
- Issue #1240 (SPEC-002 P-38). New package src/Encina.ChainedLog (does not exist yet). ADR-057
  (docs/architecture/adr/057-verifactu-boundary.md) makes the primitive regulation-neutral: no regulation field names,
  formats, endpoints, presets or hashers reproducing a regulation's concatenation, no regulation-named option.
- Pre-1.0: no [Obsolete], no compatibility layers. Style reference for a small compliance package: src/Encina.Compliance.Attestation.

TASK:
Create the package skeleton, the models, IChainedLog, IChainedLogStore, IChainHasher and ChainedLogErrors as listed.

KEY RULES:
- Neutral naming everywhere (no invoice, tax, fiscal or authority words in types, options, tests or samples).
- ROP: every operation returns Either<EncinaError, T>; errors carry a code and non-identifying details.
- No update or delete method exists: the log is append-only.
- XML docs on every public member; PublicAPI files complete; README carries the ADR-057 D23 sentence.

REFERENCE FILES:
- docs/architecture/adr/057-verifactu-boundary.md
- src/Encina.Compliance.Attestation/ (layout, AttestationErrors.cs, Model/AttestationReceipt.cs)
```

</details>

---

### Phase 2: Hashers, Canonical Form and Verifier

<details>
<summary><strong>Tasks</strong></summary>

1. **`Hashing/CanonicalForm.cs`** (internal) — length-prefixed UTF-8 fields in the fixed order of Design Choice 3; attributes sorted ordinally; appended time as UTC ticks; documented in XML and in the README
2. **`Hashing/Sha256ChainHasher.cs`** (public sealed) — SHA-256 over the canonical form
3. **`Hashing/HmacChainHasher.cs`** (public sealed) — HMAC-SHA-256 (algorithm configurable to SHA-384/512) with a key from `ChainedLogOptions.HmacKey` (`[JsonIgnore]`, `ToString()` redacts; null key is a start-up error when this hasher is selected)
4. **`Verification/ChainVerifier.cs`** (internal) — walks entries in sequence order: first sequence is 1, no gaps, `PreviousHash` equals the previous `EntryHash` (genesis for the first), recomputed `EntryHash` equals the stored one, last entry equals the head; returns the first broken sequence
5. **`InMemory/InMemoryChainedLogStore.cs`** — per-chain lock; the unit-test and Attestation-test backend (documented as non-persistent)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
- IChainHasher.ComputeEntryHash(ChainLink) is the only hashing seam; the canonical form must be stable forever
  (fixed vectors in tests).

TASK:
Implement the canonical form, the two hashers, the verifier and the in-memory store as in the Phase 2 tasks.

KEY RULES:
- Use System.Security.Cryptography only; constant-time comparison (CryptographicOperations.FixedTimeEquals) for hashes.
- The HMAC key never appears in logs, errors, ToString() or serialization.
- Time comes from TimeProvider (the store/log, never DateTime.UtcNow).
- CRAP <= 10 per method: split the verifier into small checks (link, hash, sequence, head).

REFERENCE FILES:
- src/Encina.Compliance.Attestation/Providers/HashChainAttestationProvider.cs (ComputeHmac, key handling)
- src/Encina.Compliance.Attestation/ContentHasher.cs
```

</details>

---

### Phase 3: Default Log, Configuration and DI

<details>
<summary><strong>Tasks</strong></summary>

1. **`DefaultChainedLog.cs`** — `AppendAsync`: validate partition and attributes, resolve the tenant (`IRequestContext`, fixed default tenant when tenancy is off, `Left(chainedlog.tenant_required)` when a tenant is required and none exists), delegate to the store inside its transaction handling, compute the entry hash with `IChainHasher`, idempotent by `Reference`
2. **`ChainedLogOptions.cs`** — `Hasher` (Sha256 | Hmac), `HmacKey`, `HmacAlgorithm`, `RequireAmbientTransaction` (default false), `MaxAttributeBytes`, `VerifyEvery` (null), `VerifiedPartitions`, `AddHealthCheck`; `ChainedLogOptionsValidator`
3. **`ServiceCollectionExtensions.AddEncinaChainedLog(Action<ChainedLogOptions>?)`** — options, validator, hasher selection, `IChainedLog`, in-memory store as a `TryAdd` default that **fails at start-up outside `Development` when no database store is registered** and a hasher requires evidence (so the chain is never silently kept in memory), optional verification service and health check; a database store wins over the in-memory default in any registration order without overriding the application's own registration (the `Replace`-style rule of AGENTS.md section 3)
4. **DI test** — built with `ValidateOnBuild` and `ValidateScopes` for the in-memory default and for each provider registration

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
- Registration completeness rule: an AddEncina* method that adds a service must register every option type and
  dependency it resolves, proven by a DI test with ValidateOnBuild and ValidateScopes; a database store must win over an
  in-memory default in any registration order (see the read-audit store fix #1269 and Encina.Audit.Marten's Replace).
- Tenant resolution decision table: DefaultConsentService (RequireTenantContext, ambient tenant, fixed default tenant).

TASK:
Implement DefaultChainedLog, the options with validator, the registration and the DI tests.

KEY RULES:
- Options with a key: [JsonIgnore] on the property and a ToString() override that redacts.
- A background verification loop catches per iteration; cancellation ends the loop and is not an error.
- No exception for business outcomes; Left for every failure.

REFERENCE FILES:
- src/Encina.Compliance.Attestation/ServiceCollectionExtensions.cs and Validation/HashChainOptionsValidator.cs
- src/Encina.Security.Audit/ServiceCollectionExtensions.cs
```

</details>

---

### Phase 4: Persistence — Schema and Mapping

<details>
<summary><strong>Tasks</strong></summary>

1. **Tables** (per relational provider; script numbers are the next free ones at implementation time, never reused from another plan): `ChainedLogEntries` (`TenantId`, `PartitionKey`, `Sequence`, `PayloadHash`, `Reference` null, `AttributesJson`, `PreviousHash`, `EntryHash`, `AppendedAtUtc`; primary key `(TenantId, PartitionKey, Sequence)`; unique index `(TenantId, PartitionKey, Reference)` filtered on `Reference IS NOT NULL` where the provider supports filters, otherwise a nullable-unique column) and `ChainedLogHeads` (`TenantId`, `PartitionKey`, `LastSequence`, `LastHash`; primary key `(TenantId, PartitionKey)`)
2. **Types** — SQL Server `NVARCHAR(128)` keys, `BIGINT` sequence, `NVARCHAR(128)` hashes (hex), `DATETIME2`; PostgreSQL `TEXT`, `BIGINT`, `TIMESTAMPTZ`; MySQL `VARCHAR(128)` (utf8mb4 index length checked), `BIGINT`, `DATETIME(6)`
3. **EF Core** — `ChainedLogEntryEntity`, `ChainedLogHeadEntity`, `ChainedLogEntityConfiguration`, `ModelBuilder.ApplyEncinaChainedLog()`
4. **MongoDB** — documents `ChainedLogEntryDocument` and `ChainedLogHeadDocument`, indexes created at start-up (unique `{tenant_id, partition_key, sequence}`, unique partial `{tenant_id, partition_key, reference}`)
5. Append-only is enforced by the stores (no update or delete SQL for entries) and documented; database-level `REVOKE UPDATE, DELETE` guidance goes in the documentation, not in code

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
- Existing scripts: src/Encina.ADO.*/Scripts/NNN_Create*Table.sql plus 000_CreateAllTables.sql; the Dapper scripts are
  byte-identical to the ADO ones. Existing EF entity configuration style: ReadAuditEntryEntityConfiguration (Security.Audit).

TASK:
Write the schema for the 6 ADO.NET/Dapper databases, the EF Core entities and the MongoDB documents.

KEY RULES:
- Parameters only; provider quoting; case-sensitive PostgreSQL identifiers; MySQL index length limits respected.
- Take the next free script number when implementing; do not copy numbers from other plans.
- Neutral names only (ChainedLog*), no regulation words.

REFERENCE FILES:
- src/Encina.ADO.PostgreSQL/Scripts/020_CreateReadAuditEntriesTable.sql
- src/Encina.Security.Audit/ReadAuditEntryEntity.cs
- src/Encina.MongoDB/Auditing/
```

</details>

---

### Phase 5: Provider Implementations — 10 Database Providers

<details>
<summary><strong>Tasks</strong></summary>

#### 5a. ADO.NET x3 and 5b. Dapper x3 (`ChainedLog/ChainedLogStoreADO.cs`, `ChainedLog/ChainedLogStoreDapper.cs`)

1. Enlist in the scope's transaction through `IDbTransactionAccessor`; with none, begin an own transaction (or return `Left(chainedlog.transaction_required)` when `RequireAmbientTransaction`)
2. Lock sequence: ensure the head row exists (SQL Server `IF NOT EXISTS ... INSERT` under `UPDLOCK, HOLDLOCK`; PostgreSQL `INSERT ... ON CONFLICT DO NOTHING`; MySQL `INSERT IGNORE`), then select it for update (`WITH (UPDLOCK, HOLDLOCK)` / `FOR UPDATE`), read `LastSequence` and `LastHash`, insert the entry, update the head
3. Reads: `GetEntriesAsync` with provider pagination (`OFFSET/FETCH`, `LIMIT`), `GetHeadAsync`
4. `OpenAsync(ct)`, `ExecuteReaderAsync(ct)`, `BeginTransactionAsync(ct)` only; provider exceptions become `Left(chainedlog.append_failed)` with the exception type; unique violation on `Reference` returns the existing entry (idempotent)

#### 5c. EF Core (`ChainedLogStoreEF.cs`)

1. Uses the scoped `DbContext` and its current transaction (or a short own one); the head lock uses provider-specific raw SQL selected from `Database.ProviderName` (SqlServer `UPDLOCK`, PostgreSQL and MySQL `FOR UPDATE`); entry and head changes saved with one `SaveChangesAsync(ct)`
2. Integration tests run on SqlServer, PostgreSQL and MySQL

#### 5d. MongoDB (`ChainedLogStoreMongoDB.cs`)

1. Requires a session from `IMongoSessionAccessor`; the head is taken with `FindOneAndUpdateAsync` (`$inc`, upsert) inside the transaction; entry insert and head hash update in the same session; without a transaction-capable deployment returns `Left(chainedlog.transaction_required)`

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT:
- Neighbouring stores to copy structure and transaction handling from: OutboxStoreADO/Dapper (IDbTransactionAccessor),
  OutboxStoreEF (scoped DbContext), OutboxStoreMongoDB (IMongoSessionAccessor).
- SQL differences: SQL Server TOP/OFFSET FETCH, UPDLOCK/HOLDLOCK; PostgreSQL FOR UPDATE, ON CONFLICT; MySQL FOR UPDATE,
  INSERT IGNORE, backtick quoting.

TASK:
Implement IChainedLogStore on the 10 providers as in 5a-5d.

KEY RULES:
- Database calls are asynchronous with a CancellationToken; NEVER the synchronous overloads (Open(), ExecuteNonQuery()).
- The lock lives in the caller's transaction; never release it early; never swallow a provider exception.
- Provider exceptions become Left with the exception type only; OperationCanceledException propagates.
- Entry rows are never updated or deleted.
- Names follow {Pattern}Store{Provider}: ChainedLogStoreADO, ChainedLogStoreDapper, ChainedLogStoreEF, ChainedLogStoreMongoDB,
  in a ChainedLog/ folder named after the feature.

REFERENCE FILES:
- src/Encina.ADO.PostgreSQL/Outbox/OutboxStoreADO.cs
- src/Encina.EntityFrameworkCore/Outbox/OutboxStoreEF.cs
- src/Encina.MongoDB/Outbox/OutboxStoreMongoDB.cs
```

</details>

---

### Phase 6: Attestation on the Chained Log

<details>
<summary><strong>Tasks</strong></summary>

1. **`HashChainAttestationProvider.cs`** (modify) — constructor takes `IChainedLog`; `AttestAsync` appends (`Reference = RecordId`, `PayloadHash = ContentHash`, attributes: correlation id) and maps the entry to `AttestationReceipt`; `VerifyAsync` recomputes through `IChainedLog.VerifyAsync` for the receipt's chain range; the in-memory list, the lock, `_previousSignature` and `HashChainOptions.StoragePath` are removed
2. **`AttestationReceipt` mapping** — `Signature` = `EntryHash`, `ProofMetadata["chain_index"]` = sequence, `ProofMetadata["hash_algorithm"]` = the hasher's name
3. **`ServiceCollectionExtensions.AddHashChainProvider`** (modify) — registers `HmacChainHasher` with the configured key and requires `AddEncinaChainedLog`; start-up fails without a key (`chainedlog.key_missing`); the `EphemeralHmacKeyWarning` path is removed
4. **`HashChainOptionsValidator`** (modify) — key required when the log is persistent; the algorithm check stays
5. `IAttestationReceiptStore` for the hash-chain provider reads receipts back from the log by `Reference`
6. The tenant of an `AuditRecord` (`TenantId`) selects the chain; the partition key is the constant `attestation` unless an option sets another

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
CONTEXT:
- HashChainAttestationProvider (src/Encina.Compliance.Attestation/Providers/) keeps a List<AttestationReceipt> chain under a
  Lock with an HMAC key (random when none is configured). IAuditAttestationProvider has AttestAsync, VerifyAsync,
  GetReceiptAsync, GetReceiptsAsync; IAttestationReceiptStore has Store/Get receipts.

TASK:
Re-base the provider on IChainedLog as in the Phase 6 tasks and update the registration and validators.

KEY RULES:
- Idempotency: the same RecordId returns the existing receipt (Reference is unique per chain).
- Fail closed: no key, no start (chainedlog.key_missing); a Left from the log fails the attestation.
- The InMemoryAttestationProvider is unchanged.
- No DateTime.UtcNow; TimeProvider only. No content in logs or tags.

REFERENCE FILES:
- src/Encina.Compliance.Attestation/Providers/HashChainAttestationProvider.cs
- src/Encina.Compliance.Attestation/ServiceCollectionExtensions.cs
- src/Encina.Compliance.Attestation/Validation/HashChainOptionsValidator.cs
```

</details>

---

### Phase 7: Cross-Cutting Integration

<details>
<summary><strong>Tasks</strong></summary>

1. **Multi-tenancy** — one chain per `(tenant, partition)`; `IRequestContext.TenantId` or the fixed default tenant; `Left(chainedlog.tenant_required)` when required; two-tenant test: tenant A never reads, verifies or extends tenant B's chain (REQ-061, ADR-057 "keyed per tenant")
2. **Transactions** — Design Choices 4 and 5
3. **Locks** — the head-row lock is the serialisation; `IDistributedLockProvider` is not used (Design Choice 4)
4. **Audit trail** — a failed verification publishes `ChainVerificationFailedNotification` (an `INotification`) with the chain's tenant and the first broken sequence, never a partition key value
5. **Validation** — `ChainedLogOptionsValidator`, partition and attribute validation
6. **Health check** — `ChainedLogHealthCheck` (`DefaultName` const, static `Tags`, scoped resolution via `CreateScope()`): store reachable, optional last-verification result
7. **Evaluated and not integrated** — caching, resilience, idempotency beyond `Reference`, module isolation (see the matrix)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
CONTEXT:
- Tenant decision table: DefaultConsentService. Health check pattern: src/Encina.Compliance.Attestation/Health/AttestationHealthCheck.cs.
  Notifications are INotification records published through the Encina publisher.

TASK:
Wire tenant resolution, the verification-failed notification, the health check and the two-tenant tests.

KEY RULES:
- Tenant is never taken from the payload; it comes from IRequestContext.
- Health check: DefaultName const, Tags static array, scoped resolution via IServiceProvider.CreateScope().
- No partition key value, payload hash or entry hash in tags or log messages.

REFERENCE FILES:
- src/Encina.Compliance.Attestation/Health/AttestationHealthCheck.cs
- src/Encina.Compliance.Consent/ (DefaultConsentService tenant resolution)
```

</details>

---

### Phase 8: Observability

<details>
<summary><strong>Tasks</strong></summary>

The names in the issue (`encina.chain.appends`, `encina.chain.verify_failures`, `encina.chain.append`) were proposals; the plan names them after the package: source and meter `Encina.ChainedLog`.

1. **`Diagnostics/ChainedLogDiagnostics.cs`** — `ActivitySource` and `Meter` `Encina.ChainedLog`; counters `chainedlog.appends.total` (tag `chainedlog.outcome`), `chainedlog.verifications.total` (tag `chainedlog.outcome`), `chainedlog.verify_failures.total`; histogram `chainedlog.append.duration.ms`; activities `ChainedLog.Append` and `ChainedLog.Verify` with `chainedlog.tenant_id`; no partition key value, hashes or attributes
2. **`EventIdRanges.cs`** — register `ChainedLog = (6580, 6589)` (from the batch block 6550-6599) and map `Encina.ChainedLog` in `EncinaEventIdAllocationTests`
3. **`Diagnostics/ChainedLogLogMessages.cs`** — `[LoggerMessage]` 6580 onward, packed: `AppendCompleted` (6580, Debug), `AppendFailed` (6581, Error), `IdempotentReturned` (6582, Debug), `ChainVerified` (6583, Information), `ChainBroken` (6584, Critical; sequence and code only), `VerificationFailed` (6585, Error), `TransactionRequired` (6586, Warning), `KeyMissing` (6587, Critical, start-up), `InMemoryStoreInUse` (6588, Warning)
4. Provider stores do not log (the log composes and logs); Attestation keeps its own range `ComplianceAttestation` (9600-9699) for its messages

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```
CONTEXT:
- Register the new range in src/Encina/Diagnostics/EventIdRanges.cs first (ADR-021). 6580-6589 is the part of the plan
  batch's block 6550-6599 assigned to this plan.

TASK:
Add the diagnostics, the range registration, the allocation-test mapping and the [LoggerMessage] entries.

KEY RULES:
- Pack EventIds sequentially from 6580; XML docs name the range ("Event IDs: 6580-6588 (see EventIdRanges.ChainedLog)").
- No hashes, partition values, attributes or Error.Message in tags, metrics or logs; exceptions via ForLogging().
- Tenant as "chainedlog.tenant_id".

REFERENCE FILES:
- src/Encina.Compliance.Attestation/Diagnostics/AttestationDiagnostics.cs and AttestationLogMessages.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

---

### Phase 9: Testing

<details>
<summary><strong>Tasks</strong></summary>

Manifest first: per-file targets with one-sentence justifications for every new file in `.github/coverage-manifest/Encina.ChainedLog.json`, the Attestation manifest and the seven provider manifests (written by `mechanical-fixer`).

1. **UnitTests** — canonical form (fixed vectors), both hashers, verifier (edit, middle deletion, reorder, gap, head mismatch each reported at the right sequence), `DefaultChainedLog` (idempotent by `Reference`, tenant required, attributes too large, invalid partition), options validator, DI tests, telemetry with an in-memory exporter (tenant tag present; no partition, hash or attribute value anywhere), health check, Attestation provider on the in-memory log (restart simulated by a new provider over the same store: old receipts still verify)
2. **GuardTests** — all public constructors and methods
3. **ContractTests** — one store contract run against the in-memory store and every provider store with real instances: same ordering, same `Left` codes, idempotent `Reference`, no gaps
4. **PropertyTests** — FsCheck: any single edit, deletion or swap in a valid chain is detected by `VerifyAsync`; appends in any order of interleaved chains keep each chain gapless
5. **IntegrationTests** — shared `[Collection]` fixtures on the 10 providers: restart survives (new service provider over the same database verifies the old chain), rollback of the caller's transaction leaves no entry and no head change, two concurrent appenders on one chain produce a gapless chain, two tenants, `RequireAmbientTransaction`, MongoDB on a replica set; S8-style test through Attestation
6. **LoadTests** — required: concurrent appends on one chain and on many chains (NBomber), asserting gapless and valid chains afterwards; run locally with `DOTNET_JitObjectStackAllocationConditionalEscape=0` as the repository documents
7. **BenchmarkTests** — `.md` justification: append latency is dominated by the database round trips and the lock, not by hashing; hashing is measured inside the load test report
8. **PR split** — PR 1 Phases 1-3 with unit and guard tests; PR 2 ADO.NET; PR 3 Dapper; PR 4 EF Core and MongoDB; PR 5 Phase 6 and the cross-provider tests, load test, documentation

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 9</strong></summary>

```
CONTEXT:
- Test projects are consolidated under tests/ (UnitTests, GuardTests, ContractTests, PropertyTests, IntegrationTests,
  LoadTests). Integration tests use shared [Collection("<Family>-<Database>")] fixtures, never IClassFixture;
  ClearAllDataAsync in InitializeAsync. Assertions use Shouldly via Encina.Testing.Shouldly.

TASK:
Implement the tests listed in Phase 9 and the manifest targets, then measure per-flag coverage.

KEY RULES:
- Tests execute real package code; contract and property tests instantiate real implementations; no reflection-only tests.
- Provider-dependent feature: tests cover all 10 database providers.
- No Thread.Sleep; FakeTimeProvider; deterministic; builders for test data; neutral naming in fixtures.
- A test never swallows an exception thrown by the code it drives.

REFERENCE FILES:
- .github/coverage-manifest/Encina.Compliance.Attestation.json
- tests/Encina.IntegrationTests/Security/Audit/ReadAudit/ (collection fixture usage)
- tests/Encina.LoadTests/
```

</details>

---

### Phase 10: Documentation & Finalization

<details>
<summary><strong>Tasks</strong></summary>

1. XML doc comments on all new public APIs (`<summary>`, `<remarks>`, `<param>`, `<returns>`, `<example>`); the canonical form documented exactly
2. `changelog.d/1240-chained-log.added.md` and `changelog.d/1240-attestation-persistent-hash-chain.changed.md` (the provider's storage and key requirement change)
3. READMEs: `Encina.ChainedLog` (with the ADR-057 D23 sentence), `Encina.Compliance.Attestation`, and one short section in each of the seven provider packages
4. `docs/features/` page for the chained log: append in a transaction, verify, anchor the head, what the log does and does not prove, database-level `REVOKE UPDATE, DELETE` guidance
5. `docs/INVENTORY.md`; `PublicAPI.Unshipped.txt` in every touched package; `Encina.slnx`
6. ADR: not needed (ADR-057 already records the boundary; the choices follow it). ADR number assigned when the ADR pull request opens, if the maintainer asks for one
7. Build verification: `dotnet build --configuration Release` with 0 warnings; `dotnet test` green; every coverage flag reaches its manifest target; no hand-typed coverage figures (cite `cov:` markers)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 10</strong></summary>

```
CONTEXT:
- Documentation rules: .claude/skills/encina-docs/SKILL.md. Changelog fragments: changelog.d/README.md.

TASK:
Document the chained log, its guarantees and limits, and the Attestation change.

KEY RULES:
- English only; no private client application named (write "the reference application").
- Say plainly that the primitive is regulation-neutral and that Encina ships no code for any specific regulation (ADR-057).
- Never type coverage figures; cite cov: markers. Never edit CHANGELOG.md.

REFERENCE FILES:
- docs/architecture/adr/057-verifactu-boundary.md
- src/Encina.Compliance.Attestation/README.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Relevance |
|--------|-----------|-----------|
| Ley 41/2002 | Art. 14 | Authenticity of the clinical record and of its changes |
| LOPDGDD | Art. 32.4 | Digital evidence |
| GDPR | Art. 5(1)(f), Art. 32 | Integrity and security of processing; Art. 5(1)(c) minimisation (no content in the log) |
| SPEC-002 | REQ-047, REQ-021, AC-039, S8, DEC-002, INV-004, REQ-061, REQ-062 | The requirement this plan implements and the Verifactu boundary |
| ADR-057 | D11, D12, D20, D23 | Regulation-neutral primitive; mechanical and review checks; README sentence |
| ADR-022 | Provider-agnostic attestation | Attestation model the log serves |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|----------------------|
| `HashChainAttestationProvider`, `IAuditAttestationProvider`, `IAttestationReceiptStore` | `src/Encina.Compliance.Attestation/` | Re-based on the log |
| `HashChainOptions`, `HashChainOptionsValidator` | `src/Encina.Compliance.Attestation/` | HMAC key and algorithm handling |
| `IDbTransactionAccessor`, `IMongoSessionAccessor`, scoped `DbContext` | `Encina.Messaging.Transactions`, `Encina.MongoDB`, EF Core | Joining the caller's transaction |
| `OutboxStoreADO/Dapper/EF/MongoDB` | provider `Outbox/` folders | Structure and transaction handling to copy |
| Provider `Scripts/` and `ServiceCollectionExtensions.cs` | provider packages | Schema scripts and registration style |
| `DefaultConsentService` tenant resolution | `src/Encina.Compliance.Consent/` | Tenant decision table |
| `AttestationHealthCheck`, `AttestationDiagnostics` | `src/Encina.Compliance.Attestation/` | Health check and telemetry style |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.ChainedLog` | 6580-6589 (`ChainedLog`, new, from the batch block 6550-6599) | Uses 6580-6588; to be registered in `EventIdRanges.cs` by PR 1 |
| `Encina.Compliance.Attestation` | 9600-9699 (`ComplianceAttestation`, registered) | Existing messages unchanged; no new range |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| `Encina.ChainedLog` core | ~22 | csproj, models, abstractions, hashers, verifier, in-memory store, default log, options, validator, DI, health check, service, diagnostics, log messages, errors, README, PublicAPI |
| ADO.NET x3 + Dapper x3 | ~18 | store, registration, script per package |
| EF Core + MongoDB | ~8 | store, entities, configuration, model extension, documents, registrations |
| Attestation | ~5 | provider, DI, validator, options, README |
| Tests | ~30 | across 6 test types and provider families |
| Manifests, changelog, documentation | ~12 | |
| **Total** | **~95** | five pull requests |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
You are implementing issue #1240 (SPEC-002 P-38): the persistent hash-chained log Encina.ChainedLog on the 10 database
providers, with Encina.Compliance.Attestation built on it.

PROJECT CONTEXT:
- .NET 10 / C# 14, ROP (Either<EncinaError, T>), pre-1.0 (breaking changes preferred, no [Obsolete]).
- ADR-057: the primitive is regulation-neutral. No regulation field names, formats, endpoints, presets or hashers that
  reproduce a regulation's concatenation; no regulation-named option; the README of the package states the boundary.
- Providers: ADO.NET x3, Dapper x3, EF Core x3 (one package), MongoDB.

IMPLEMENTATION OVERVIEW:
Phase 1: new package Encina.ChainedLog: models, IChainedLog, IChainedLogStore, IChainHasher, errors
Phase 2: canonical form, Sha256ChainHasher, HmacChainHasher, verifier, in-memory store
Phase 3: DefaultChainedLog, options and validator, AddEncinaChainedLog, DI tests
Phase 4: schema (entries + heads) for 6 relational databases, EF entities, MongoDB documents
Phase 5: provider stores: head-row lock in the caller's transaction, gapless sequence, idempotent Reference
Phase 6: HashChainAttestationProvider re-based on IChainedLog; key required; in-memory provider unchanged
Phase 7: tenant per chain, verification-failed notification, health check
Phase 8: Encina.ChainedLog meter/activities, EventIds 6580-6588 in the new range 6580-6589
Phase 9: unit, guard, contract, property, integration (10 providers), load; manifests
Phase 10: docs, READMEs, changelog fragments, PublicAPI

KEY PATTERNS:
- Append-only; no update or delete; digest and reference only, no content.
- Lock the chain's head row inside the caller's transaction; async database calls with CancellationToken only.
- Fail closed: no key, no start; a Left from the log fails the attestation; tail-truncation limit documented.
- No hashes, partition values or attributes in logs, tags or metrics; exceptions via ForLogging().
- TimeProvider for time; CRAP <= 10 per new method; tests execute real code; shared [Collection] fixtures.

REFERENCE FILES:
- docs/architecture/adr/057-verifactu-boundary.md
- src/Encina.Compliance.Attestation/ (Providers, ServiceCollectionExtensions.cs, Validation)
- src/Encina.ADO.PostgreSQL/Outbox/OutboxStoreADO.cs, src/Encina.EntityFrameworkCore/Outbox/OutboxStoreEF.cs,
  src/Encina.MongoDB/Outbox/OutboxStoreMongoDB.cs
- docs/specifications/SPEC-002-eu-regulatory-readiness.md (REQ-047, AC-039, S8)
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | A chain read must reflect the stored entries; caching verification input would defeat its purpose |
| 2 | OpenTelemetry | ✅ | `Encina.ChainedLog` source and meter: append and verify activities, counters and a histogram, `chainedlog.tenant_id`, no partition, hash or attribute values (Phase 8) |
| 3 | Structured Logging | ✅ | `[LoggerMessage]` 6580-6588 in the new range `ChainedLog` (ADR-021) |
| 4 | Health Checks | ✅ | `ChainedLogHealthCheck`: store reachable, last periodic verification result; `Unhealthy` when a chain is broken |
| 5 | Validation | ✅ | `ChainedLogOptionsValidator` at start-up (key required for the keyed hasher, positive limits); partition and attribute validation on append |
| 6 | Resilience | ❌ | A local database write inside the caller's transaction; a failure must fail the append visibly, and retrying inside a held lock would extend it |
| 7 | Distributed Locks | ❌ | The head-row lock inside the caller's transaction is the serialisation (Design Choice 4); an `IDistributedLockProvider` lock would be released before the commit |
| 8 | Transactions | ✅ | Entry and head are written in the caller's transaction (`IDbTransactionAccessor`, `DbContext`, `IMongoSessionAccessor`); an own short transaction otherwise (Design Choice 5) |
| 9 | Idempotency | ✅ | `Reference` is unique per chain: appending it again returns the existing entry |
| 10 | Multi-Tenancy | ✅ | REQ-061/AC-043 and ADR-057 ("chains are keyed per tenant"): one chain per tenant and partition; two-tenant test; a fixed default tenant when tenancy is off |
| 11 | Module Isolation | ❌ | SPEC-002 requires no module scoping; `ModuleId` in messaging stays with #747 |
| 12 | Audit Trail | ✅ | The log is the tamper-evident trail; a failed verification is published as `ChainVerificationFailedNotification` and counted |

---

## Prerequisites & Dependencies

| Item | Why | State |
|------|-----|-------|
| ADR-057 (P-24) | The neutrality boundary this plan implements | Accepted and merged |
| Architecture test #2925 and checklist #2926 | Mechanical and review checks of neutrality | Open; this plan's package must pass them when they land |
| EventId range 6580-6589 | New logging | Registered by PR 1 |
| `IDbTransactionAccessor` and `IMongoSessionAccessor` | Joining the caller's transaction | Exist |
| P-36 gapless sequence (sibling) | Not required; the chain's own sequence is per chain | Independent |

---

## Next Steps

1. Link this plan from issue #1240; ask the maintainer the Dilemma of Design Choice 5 when work on the issue starts
2. PR 1 (core package), PR 2 (ADO.NET), PR 3 (Dapper), PR 4 (EF Core and MongoDB), PR 5 (Attestation, cross-provider tests, load test, documentation)
3. Run the neutrality checks of #2925 and #2926 against the new package before the first merge
4. The final commit references `Fixes #1240`
