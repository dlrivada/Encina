# Implementation Plan: `Encina.ChainedLog` — Persistent Hash-Chained Log on the 10 Database Providers, with Attestation Built on It

> **Issue**: [#1240](https://github.com/dlrivada/Encina/issues/1240) (SPEC-002 tracking id **P-38**, priority **P0**)
> **Type**: Feature
> **Complexity**: High (10 phases, 10 database providers, new package, ~160 files, 16 pull requests)
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

**Size**: about 160 files including tests, scripts, manifests, PublicAPI files, `ProjectReference` edits and fragments, so sixteen worker pull requests. Every PR builds and passes alone with its own tests, manifest rows and PublicAPI lines, and stays within 15 files and 1,500 lines (`pr-size`):

| PR | Content | Files |
|----|---------|-------|
| 1 | Package skeleton: csproj, `Encina.slnx`, `PublicAPI.Shipped/Unshipped.txt`, README, manifest, models `ChainedLogEntry`, `ChainedLogAppend`, `ChainHead`, `ChainVerification`, `ChainedLogErrors`; unit and guard tests | 13 |
| 2 | Hashing: `IChainHasher`, `ChainLink`, `CanonicalForm`, `Sha256ChainHasher`, `HmacChainHasher`, `ChainedLogOptions` and validator, PublicAPI, manifest; unit (fixed vectors) and guard tests | 12 |
| 3 | Log core: `IChainedLog`, `IChainedLogStore`, `ChainVerifier`, `InMemoryChainedLogStore`, `DefaultChainedLog`, PublicAPI, manifest; unit tests, guard tests and the store contract suite run against the in-memory store | 13 |
| 4 | `AddEncinaChainedLog`, `ChainedLogDiagnostics`, `ChainedLogLogMessages`, `EventIdRanges.cs`, `EncinaEventIdAllocationTests`, PublicAPI (`Encina` and `Encina.ChainedLog`), manifest; DI, telemetry and guard tests | 11 |
| 5 | `ChainedLogHealthCheck`, periodic verification service, `ChainVerificationFailedNotification`, PublicAPI, manifest; health, service and notification unit tests | 9 |
| 6, 7, 8 | ADO.NET on SQL Server, PostgreSQL, MySQL (one PR each): `ChainedLogStoreADO`, registration (modify), script and `000_CreateAllTables.sql` (modify), PublicAPI, manifest, integration test, store contract class, DI test | 9 each |
| 9, 10, 11 | Dapper on SQL Server, PostgreSQL, MySQL (one PR each): same set as the ADO.NET PRs | 9 each |
| 12 | EF Core: `ChainedLogStoreEF`, two entities, configuration, `ApplyEncinaChainedLog`, registration, PublicAPI, manifest, three integration test classes (SQL Server, PostgreSQL, MySQL), store contract class, DI test | 13 |
| 13 | MongoDB: `ChainedLogStoreMongoDB`, two documents, index initializer, registration, PublicAPI, manifest, integration test, store contract class, DI test | 10 |
| 14 | Attestation on the log: `HashChainAttestationProvider`, `ServiceCollectionExtensions`, validator, options (modify), PublicAPI, manifest, unit tests, the S8 integration test, `changed` fragment | 10 |
| 15 | Property tests, load tests (NBomber), benchmark justification `.md` | 5 |
| 16 | Documentation: `Encina.ChainedLog` and `Encina.Compliance.Attestation` READMEs, seven provider READMEs, the feature page, `docs/INVENTORY.md`, `added` fragment | 12 |

PRs 6 to 13 need PR 4; PR 5 needs PR 4; PR 14 needs PR 4 and PR 7 (its S8 integration test runs on PostgreSQL); the rest follow in order. No PR references a package that is not on main.

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
- Key lengths: `TenantId`, `PartitionKey` and `Reference` are at most 128 characters each (validated on append, `Validation` errors), so the composite unique index stays under the MySQL InnoDB index byte limit with `utf8mb4` (3 x 128 x 4 bytes).
- Timestamp precision: `AppendedAtUtc` is truncated to whole milliseconds by `DefaultChainedLog` before it is hashed and stored, and the canonical form carries it as Unix milliseconds (`long`), not ticks. PostgreSQL `TIMESTAMPTZ` and MySQL `DATETIME(6)` keep microseconds and MongoDB milliseconds, so a 100 ns tick value read back would differ from the hashed one and break verification on three of the four families (SQL Server `DATETIME2` would hide it). The columns are declared with millisecond precision (`DATETIME2(3)`, `TIMESTAMPTZ(3)`, `DATETIME(3)`), a fixed-vector unit test covers the truncation, and the store contract test "append, read back, verify" runs on every provider.
- Chain identity: `TenantId` and `PartitionKey` are compared case-sensitively and byte-wise on every provider, so partition `A` and `a` are two chains everywhere. The key columns use a binary collation (SQL Server `Latin1_General_100_BIN2`, MySQL `utf8mb4_bin`; PostgreSQL and MongoDB are case-sensitive by default), and the contract suite appends to both spellings and asserts two independent chains.
- Tenancy off: the tenant key is the empty string `""`. A real tenant id must be non-empty (an empty ambient tenant is `Left(chainedlog.tenant_required)`), so no real tenant, including one literally called `default`, can share the tenancy-off chain, and turning tenancy on later never merges chains: the `""` chain stays reachable only while tenancy is off. Main has no default-tenant constant (`DefaultConsentService` uses a null tenant); the sentinel is documented in the XML docs and README.

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
- Option C (no shipped hasher) also satisfies D11; the exclusion is the issue's own line "SHA-256 default" (issue body and comment of 2026-10-17), which requires a hasher out of the box. SHA-256 is the algorithm of the default hasher. The comment on the issue repeats D11 ("no default, preset or hasher reproducing the RRSIF concatenation"), which is satisfied: the default is generic, and an application that needs a regulation-specific concatenation writes its own `IChainHasher` and carries that responsibility (ADR-057, D20).
- Neutrality is checked mechanically by the architecture test of #2925 and by the review checklist of #2926; this plan adds both packages (`Encina.ChainedLog`, the provider `ChainedLog/` folders) to the allow-list scope those issues define and its own tests assert the canonical form with fixed vectors.

**Class:** Settled
**Settled by:** ADR-057 D11, D12, D20 and D23; SPEC-002 DEC-002 and INV-004; the issue comment of 2026-10-17 ("regulation-neutral primitive; Encina ships no Verifactu code ... no regulation-named option")

</details>

<details>
<summary><strong>4. Serialising Appends — lock the chain's head row inside the caller's transaction</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A head row per chain (`ChainedLogHeads`: tenant, partition, last sequence, last hash), locked for update inside the caller's transaction (`UPDLOCK, HOLDLOCK` on SQL Server; `SELECT ... FOR UPDATE` on PostgreSQL and MySQL; a transactional `findOneAndUpdate` on MongoDB), then the entry insert and head update; the first append creates the head with an insert that ignores only the duplicate key** | Gapless by construction; a rollback of the caller's transaction rolls back the entry and the head; the lock lives exactly as long as the transaction | Appenders on the same chain wait for each other; a long caller transaction holds the chain |
| **B) `IDistributedLockProvider` around the append** | Provider-independent | The lock is released before the caller's transaction commits, so a rollback leaves a gap or a later append sees a stale head |
| **C) Optimistic: unique `(chain, sequence)` and retry on conflict** | No waiting | The caller's work is already done in its transaction; a retry means re-running the caller; a PostgreSQL unique violation aborts the whole transaction |

### Chosen Option: **A — head-row lock in the caller's transaction**

### Rationale

- The issue says it: "Append is serialised per chain inside the caller's transaction; verification detects any gap or edit."
- Connection and transaction handling follow the existing stores: ADO.NET and Dapper enlist through `IDbTransactionAccessor` (the outbox pattern), EF Core uses the scoped `DbContext` and its transaction, MongoDB uses `IMongoSessionAccessor`.
- MongoDB needs a replica set for transactions; on a standalone server the store returns `Left(chainedlog.transactions_unsupported)` (`Internal`, a deployment problem) instead of appending without a guarantee.
- Idempotency never relies on catching a unique violation: on PostgreSQL a unique violation aborts the whole transaction, which is the caller's (option C is rejected for the same reason). Because the head row is locked first, the store first selects by `(tenant, partition, reference)` under the lock and returns the existing entry when found; a unique violation afterwards is only a fault (`Left(chainedlog.append_failed)`).
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
- Lifetime: today `AddHashChainProvider` registers `HashChainAttestationProvider`, `IAuditAttestationProvider` and `IAttestationReceiptStore` as singletons (`ServiceCollectionExtensions.cs`). The provider now needs `IChainedLog`, whose stores depend on the scoped `DbContext`, `IDbTransactionAccessor` and `IMongoSessionAccessor`, so a singleton would be a captive dependency that `ValidateScopes` rejects. The three registrations become scoped (the in-memory and HTTP providers stay as they are), which also lets an attestation join the request's transaction: `AttestationPipelineBehavior` is a transient behavior resolved in the request scope, so it appends in the scope's ambient transaction when one exists and in an own short transaction otherwise (Design Choice 5). `AttestationHealthCheck` and other consumers of `IAuditAttestationProvider` resolve it from a scope (health checks run in a scope per execution); the DI test with `ValidateOnBuild` and `ValidateScopes` proves no singleton holds it. The lifetime change goes in the `changed` changelog fragment.
- Keys: after the change `ChainedLogOptions.HmacKey` and `HashChainOptions.HmacKey` both exist. `AddHashChainProvider` copies the Attestation key into the log's options (post-configure; one source of truth for the application), both properties keep `[JsonIgnore]` and a redacting `ToString()` (AGENTS.md section 3), and the `chainedlog.key_missing` check runs at start-up (`ValidateOnStart`) for the Attestation path as well.

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
- The periodic service (`ChainedLogOptions.VerifyEvery`, null by default) runs inside a `PeriodicTimer` loop that catches per iteration and lets cancellation end the loop (AGENTS.md section 3, background loops). A `Left` from `Publish` of `ChainVerificationFailedNotification`, from the periodic service or from `VerifyAsync`, is never swallowed: it is logged by error code (`VerificationFailed`, 6585), counted in `chainedlog.verify_failures.total`, and the loop keeps running; a unit test covers it.

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
8. **`ChainedLogErrors.cs`** (PR 1) — every code is created with `EncinaErrors.Create(code, category, ...)` (ADR-062; the completeness scan fails an uncategorized call), codes and non-identifying details only, each category asserted by a `GetCategory` test:

   | Code | `ErrorCategory` | Rule |
   |------|-----------------|------|
   | `chainedlog.append_failed` | `Internal` | R4: store failure, including a unique violation treated as a fault |
   | `chainedlog.transaction_required` | `InvalidRequest` | R3: `RequireAmbientTransaction` is set and the caller has no transaction (`FailedPrecondition` is not used before #3420) |
   | `chainedlog.transactions_unsupported` | `Internal` | R1/R4: the MongoDB deployment cannot run transactions (wiring) |
   | `chainedlog.key_missing` | `Internal` | R1: the keyed hasher has no key (wiring, found at start-up) |
   | `chainedlog.invalid_partition`, `chainedlog.reference_invalid`, `chainedlog.attributes_too_large` | `Validation` | R3: field rules |
   | `chainedlog.tenant_required` | `InvalidRequest` | R3: a required (or empty) tenant is missing; used by `DefaultChainedLog` (Phase 3) |
   | `chainedlog.not_found` | `NotFound` | R1, only when the request names an entry or chain that does not exist |

   A broken chain is not an error: `chainedlog.chain_broken` is the `Code` of a `Right(ChainVerification { IsValid = false })` (Design Choice 7)

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

1. **`Hashing/CanonicalForm.cs`** (internal) — length-prefixed UTF-8 fields in the fixed order of Design Choice 3; attributes sorted ordinally; appended time as Unix milliseconds (`long`, already truncated to the millisecond by `DefaultChainedLog`, Design Choice 2); documented in XML and in the README
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

1. **`DefaultChainedLog.cs`** — `AppendAsync`: validate partition, reference length and attributes, resolve the tenant (`IRequestContextAccessor`; the empty-string tenant key when tenancy is off (Design Choice 2); `Left(chainedlog.tenant_required)` when a tenant is required and none exists or the ambient tenant is empty), truncate `AppendedAtUtc` (from `TimeProvider`) to milliseconds, delegate to the store inside its transaction handling, compute the entry hash with `IChainHasher`, idempotent by `Reference`
2. **`ChainedLogOptions.cs`** — `Hasher` (Sha256 | Hmac), `HmacKey`, `HmacAlgorithm`, `RequireAmbientTransaction` (default false), `MaxAttributeBytes`, `VerifyEvery` (null), `VerifiedPartitions`, `AddHealthCheck`; `ChainedLogOptionsValidator`
3. **`ServiceCollectionExtensions.AddEncinaChainedLog(Action<ChainedLogOptions>?)`** — options, validator, hasher selection, `IChainedLog`, in-memory store as a `TryAdd` default that **fails at start-up when `IHostEnvironment.IsDevelopment()` is false and no database store is registered** (a start-up validator resolves `IHostEnvironment`; the in-memory chain is never silently kept outside development; both environments are unit-tested), optional verification service and health check; a database store wins over the in-memory default in any registration order without overriding the application's own registration (the `Replace`-style rule of AGENTS.md section 3)
4. **DI test** — built with `ValidateOnBuild` and `ValidateScopes` for the in-memory default and for each provider registration

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
- Registration completeness rule: an AddEncina* method that adds a service must register every option type and
  dependency it resolves, proven by a DI test with ValidateOnBuild and ValidateScopes; a database store must win over an
  in-memory default in any registration order (see the read-audit store fix #1269 and Encina.Audit.Marten's Replace).
- Tenant resolution decision table: DefaultConsentService (RequireTenantContext, ambient tenant through IRequestContextAccessor; tenancy off is a null tenant there and the empty-string key here).

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
2. **Types** — SQL Server `NVARCHAR(128)` keys with `COLLATE Latin1_General_100_BIN2`, `BIGINT` sequence, `NVARCHAR(128)` hashes (hex), `DATETIME2(3)`; PostgreSQL `VARCHAR(128)` keys (case-sensitive by default), `BIGINT`, `TIMESTAMPTZ(3)`; MySQL `VARCHAR(128)` keys with `utf8mb4_bin` (index length checked: three 128-character columns), `BIGINT`, `DATETIME(3)`; MongoDB ordinal string comparison and millisecond dates (Design Choice 2)
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
2. Lock sequence: ensure the head row exists with an insert that ignores only the duplicate key (SQL Server `IF NOT EXISTS ... INSERT` under `UPDLOCK, HOLDLOCK`; PostgreSQL `INSERT ... ON CONFLICT DO NOTHING`, which does not abort the transaction; MySQL `INSERT ... ON DUPLICATE KEY UPDATE LastSequence = LastSequence`, a no-op, never `INSERT IGNORE`, which would also swallow truncation and constraint errors), then select it for update (`WITH (UPDLOCK, HOLDLOCK)` / `FOR UPDATE`), read `LastSequence` and `LastHash`
3. Idempotency under the lock: when `Reference` is set, select the entry by `(tenant, partition, reference)` and return it if found; otherwise insert the entry and update the head. A unique violation on the insert is a fault (`Left(chainedlog.append_failed)`), never a "return the existing entry" signal
4. Reads: `GetEntriesAsync` with provider pagination (`OFFSET/FETCH`, `LIMIT`), `GetHeadAsync`
5. `OpenAsync(ct)`, `ExecuteReaderAsync(ct)`, `BeginTransactionAsync(ct)` only; provider exceptions become `Left(chainedlog.append_failed)` with the exception type

#### 5c. EF Core (`ChainedLogStoreEF.cs`)

1. Uses the scoped `DbContext` and its current transaction; the head lock and the entry and head writes are raw SQL executed on that context's connection and transaction (`Database.ExecuteSqlAsync` / `SqlQueryRaw`, provider-specific text selected from `Database.ProviderName`: SqlServer `UPDLOCK`, PostgreSQL and MySQL `FOR UPDATE`), never `SaveChangesAsync`: the lock-read-modify-write must run immediately, and a `SaveChangesAsync` would flush every other pending change of the caller's tracked context at a moment the caller did not choose. The EF entities exist for the model and schema, not for tracking the chain rows
2. With no ambient transaction, the store begins its own short transaction through `Database.BeginTransactionAsync(ct)` only when `Database.CurrentTransaction` is null and commits it before returning, so it never interferes with a transaction the caller opens afterwards on the same scoped context; a unit test and an integration test cover both cases
3. Integration tests run on SqlServer, PostgreSQL and MySQL

#### 5d. MongoDB (`ChainedLogStoreMongoDB.cs`)

1. Requires a session from `IMongoSessionAccessor`; the head is taken with `FindOneAndUpdateAsync` (`$inc`, upsert) inside the transaction, and a duplicate-key error from two upserts racing on a first append is retried once; entry insert and head hash update in the same session; without a transaction-capable deployment returns `Left(chainedlog.transactions_unsupported)`

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
- Idempotency by Reference is a select under the head lock, never a caught unique violation (PostgreSQL aborts the caller's transaction); MySQL head creation uses ON DUPLICATE KEY UPDATE no-op, never INSERT IGNORE; EF never calls SaveChangesAsync.
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
3. **`ServiceCollectionExtensions.AddHashChainProvider`** (modify) — registers `HmacChainHasher` with the configured key (copied into `ChainedLogOptions`, Design Choice 6) and requires `AddEncinaChainedLog`; start-up fails without a key (`chainedlog.key_missing`, `ValidateOnStart`); the `EphemeralHmacKeyWarning` path is removed; `HashChainAttestationProvider`, `IAuditAttestationProvider` and `IAttestationReceiptStore` change from singleton to scoped (Design Choice 6), and the DI test with `ValidateOnBuild` and `ValidateScopes` proves it
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

1. **Multi-tenancy** — one chain per `(tenant, partition)`; the ambient tenant through `IRequestContextAccessor`, or the empty-string tenant key when tenancy is off (Design Choice 2); `Left(chainedlog.tenant_required)` when required or empty; two-tenant test: tenant A never reads, verifies or extends tenant B's chain (REQ-061, ADR-057 "keyed per tenant")
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
- Tenant is never taken from the payload; it comes from IRequestContextAccessor; tenancy off is the empty-string key.
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

1. **`Diagnostics/ChainedLogDiagnostics.cs`** — `ActivitySource` and `Meter` `Encina.ChainedLog`; counters `chainedlog.appends.total` (tag `chainedlog.outcome`), `chainedlog.verifications.total` (tag `chainedlog.outcome`), `chainedlog.verify_failures.total`; histogram `chainedlog.append.duration.ms`; activities `ChainedLog.Append` and `ChainedLog.Verify` with `chainedlog.tenant_id` (emitted only when multi-tenancy is on and absent when it is off, ADR-054 D4; a test covers both); no partition key value, hashes or attributes
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
- Tenant as "chainedlog.tenant_id", only when multi-tenancy is on (ADR-054 D4), absent when off.

REFERENCE FILES:
- src/Encina.Compliance.Attestation/Diagnostics/AttestationDiagnostics.cs and AttestationLogMessages.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

---

### Phase 9: Testing

<details>
<summary><strong>Tasks</strong></summary>

Manifest first: each pull request adds the manifest rows of its own files in the same PR (`.github/coverage-manifest/Encina.ChainedLog.json`, the Attestation manifest and the provider manifests, written by `mechanical-fixer`). Numeric per-file targets (percent of coverable lines per flag) with one-sentence justifications:

| File | Unit | Guard | Contract | Property | Integration | Justification |
|------|-----:|------:|---------:|---------:|------------:|---------------|
| `Model/*.cs` (entry, append, head, verification, link) | 90 | 0 | 0 | 0 | 0 | Records and constructors exercised by every log test; no validated parameters |
| `ChainedLogErrors.cs` | 100 | 0 | 0 | 0 | 0 | Every factory asserted for code and `ErrorCategory` |
| `Hashing/CanonicalForm.cs` | 100 | 40 | 0 | 80 | 0 | Fixed vectors pin the form; the property test varies every field |
| `Hashing/Sha256ChainHasher.cs`, `HmacChainHasher.cs` | 100 | 80 | 0 | 80 | 0 | Pure functions; fixed vectors, key redaction and wrong-key cases; the property test checks that any change alters the hash |
| `Verification/ChainVerifier.cs` | 100 | 60 | 80 | 100 | 40 | The file where mutation matters most: four small checks (link, hash, sequence, head), each with a unit case per failure kind; the property test edits, deletes and swaps entries of valid chains; the contract suite runs it on real stores |
| `InMemory/InMemoryChainedLogStore.cs` | 95 | 60 | 90 | 60 | 0 | Test backend; the store contract suite runs against it |
| `DefaultChainedLog.cs` | 95 | 80 | 70 | 60 | 60 | Idempotency, tenant, validation and millisecond truncation are unit cases; contract and integration run it over real stores |
| `ChainedLogOptions.cs`, `ChainedLogOptionsValidator.cs` | 100 | 100 | 0 | 0 | 0 | Option validation and key redaction are pure |
| `ServiceCollectionExtensions.cs` (new package) | 100 | 100 | 0 | 0 | 0 | DI tests with `ValidateOnBuild`, `ValidateScopes` and both environments |
| `Diagnostics/ChainedLogDiagnostics.cs` | 100 | 0 | 0 | 0 | 0 | Listener-based telemetry tests, tenancy on and off |
| `Diagnostics/ChainedLogLogMessages.cs` | 0 | 0 | 0 | 0 | 0 | Source-generated delegates without executable lines |
| Health check, periodic verification service, notification | 95 | 60 | 0 | 0 | 40 | Unit cases for healthy, broken and `Left` from `Publish`; integration builds a real host |
| `ChainedLogStoreADO.cs` and `ChainedLogStoreDapper.cs` (6 packages) | 20 | 0 | 80 | 0 | 85 | Needs a real server: the store contract suite and the integration tests reach the SQL paths |
| `ChainedLogStoreEF.cs`, entities, configuration | 20 | 0 | 80 | 0 | 85 | Same reasoning on SQL Server, PostgreSQL and MySQL |
| `ChainedLogStoreMongoDB.cs`, documents | 20 | 0 | 80 | 0 | 85 | Needs a MongoDB replica set |
| `HashChainAttestationProvider.cs` (modify) | 92 | 60 | 0 | 0 | 50 | Baseline unit 92, guard 60 kept (targets never go down); S8 integration added |
| Attestation `ServiceCollectionExtensions.cs` (modify) | 91 | 68 | 0 | 0 | 0 | Baseline unit 91, guard 68 kept; scoped-lifetime DI test |
| `Validation/HashChainOptionsValidator.cs`, `HashChainOptions.cs` (modify) | 98 | 98 | 0 | 98 | 0 | Baselines kept (validator unit, guard and property 98; options unit 98) |
| Provider `ServiceCollectionExtensions.cs` (7 packages, modify) | baseline | baseline | baseline | baseline | baseline | Baselines kept (for example ADO.NET SQL Server unit 55, guard 55, integration 40; EF Core unit 64, guard 60, integration 40; MongoDB unit 55, guard 2, integration 20); the provider DI test covers the new registration |

The load-test flag is outside the manifest: the load tests of PR 15 assert gapless, valid chains after concurrent appends on the store files.

1. **UnitTests** — canonical form (fixed vectors, millisecond truncation), both hashers, verifier (edit, middle deletion, reorder, gap, head mismatch each reported at the right sequence), `DefaultChainedLog` (idempotent by `Reference`, tenant required, empty tenant, attributes too large, invalid partition, over-long reference), options validator, DI tests (in-memory default in and outside development), a `GetCategory` test per error code (ADR-062), telemetry with an in-memory exporter (`chainedlog.tenant_id` present when tenancy is on and absent when off; no partition, hash or attribute value anywhere), health check, a `Left` from `Publish` logged and counted with the loop kept running, Attestation provider on the in-memory log (restart simulated by a new provider over the same store: old receipts still verify)
2. **GuardTests** — all public constructors and methods
3. **ContractTests** — one store contract suite introduced in PR 3 against the in-memory store and reused by each provider PR (6 to 13) as a derived class over that provider's real instance: same ordering, same `Left` codes, idempotent `Reference` (no aborted transaction on PostgreSQL), no gaps, "append, read back, verify" after the storage round trip (millisecond precision), and partitions `A` and `a` as two independent chains
4. **PropertyTests** — FsCheck: any single edit, deletion or swap in a valid chain is detected by `VerifyAsync`; appends in any order of interleaved chains keep each chain gapless
5. **IntegrationTests** — shared `[Collection]` fixtures on the 10 providers: restart survives (new service provider over the same database verifies the old chain), rollback of the caller's transaction leaves no entry and no head change, two concurrent appenders on one chain produce a gapless chain, two tenants, `RequireAmbientTransaction`, MongoDB on a replica set; S8-style test through Attestation
6. **LoadTests** — required: concurrent appends on one chain and on many chains (NBomber), asserting gapless and valid chains afterwards; run locally with `DOTNET_JitObjectStackAllocationConditionalEscape=0` as the repository documents
7. **BenchmarkTests** — `.md` justification: append latency is dominated by the database round trips and the lock, not by hashing; hashing is measured inside the load test report
8. **PR split** — the sixteen pull requests of the split table in the Summary; each provider PR carries its own integration tests and store contract class, so no PR relies on tests of a later one

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
| `Encina.ChainedLog` | 6580-6589 (`ChainedLog`, new, from the batch block 6550-6599) | Uses 6580-6588; to be registered in `EventIdRanges.cs` by PR 4, the first PR that logs |
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
| **Total** | **~160** | sixteen pull requests (see the split table in the Summary; the per-category counts above are approximate and exclude scripts, `000_CreateAllTables.sql` edits, contract classes and DI tests) |

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
| 10 | Multi-Tenancy | ✅ | REQ-061/AC-043 and ADR-057 ("chains are keyed per tenant"): one chain per tenant and partition; two-tenant test; the empty-string tenant key when tenancy is off, which no real tenant can equal |
| 11 | Module Isolation | ❌ | SPEC-002 requires no module scoping; `ModuleId` in messaging stays with #747 |
| 12 | Audit Trail | ✅ | The log is the tamper-evident trail; a failed verification is published as `ChainVerificationFailedNotification` and counted |

---

## Prerequisites & Dependencies

| Item | Why | State |
|------|-----|-------|
| ADR-057 (P-24) | The neutrality boundary this plan implements | Accepted and merged |
| Architecture test #2925 and checklist #2926 | Mechanical and review checks of neutrality | Open; this plan's package must pass them when they land |
| EventId range 6580-6589 | New logging | Registered by PR 4, the first PR that logs |
| `IDbTransactionAccessor` and `IMongoSessionAccessor` | Joining the caller's transaction | Exist |
| P-36 gapless sequence (sibling) | Not required; the chain's own sequence is per chain | Independent |

---

## Next Steps

1. Link this plan from issue #1240; ask the maintainer the Dilemma of Design Choice 5 when work on the issue starts
2. The sixteen pull requests of the split table, in order (1 to 5 core, 6 to 11 ADO.NET and Dapper, 12 EF Core, 13 MongoDB, 14 Attestation, 15 property and load tests, 16 documentation)
3. Run the neutrality checks of #2925 and #2926 against the new package before the first merge
4. The final commit references `Fixes #1240`
