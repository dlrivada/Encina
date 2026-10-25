---
nav_exclude: true
---

# Crypto-shredding bypass paths: which guards (spikes #1770 and #2465)

This page is for the maintainer, who has to decide which guards stop personal data from reaching storage in plaintext around crypto-shredding, and for a contributor who needs the evidence. It consolidates the #1770 investigation (posted as an issue comment on 2026-10-17 at `ad6986bb3`), re-verifies it against `origin/main` at `af3742f54` on 2026-10-25, separates what a rule already settles from what is a real choice, and lists the dilemmas with a recommendation each. The recommendations are input, not decisions: [#2465](https://github.com/dlrivada/Encina/issues/2465) stays open until the maintainer decides, and the decisions then become an ADR that amends [ADR-034](../architecture/adr/034-crypto-shredding-through-the-stj-contract.md).

**Status:** evidence re-verified on 2026-10-25. **Issues:** [#1770](https://github.com/dlrivada/Encina/issues/1770) (investigation), [#2465](https://github.com/dlrivada/Encina/issues/2465) (decision record). **Decision register:** `artifacts/decisions/1770.md` (36 entries, a local working file of the orchestration, not in the repository; the D-keys in section 6 refer to it).

Pre-1.0, backward compatibility is not a criterion ([AGENTS.md](../../AGENTS.md) section 1). Nothing in the investigation was run end to end: every Marten finding comes from reading the decompiled Marten 9.38.0 assemblies (the version pinned in `Directory.Packages.props:53`). The proof-of-concept tests of [#2463](https://github.com/dlrivada/Encina/issues/2463) would turn each finding into a failing test.

## 1. The problem from the basics

**Crypto-shredding** erases personal data by deleting a key instead of rewriting every copy. Encina encrypts each `[CryptoShredded]` property with a key that belongs to one data subject; when the subject asks for erasure (GDPR Art. 17), Encina deletes that key, and every stored copy of the property becomes unreadable at once, including copies in an append-only event store that cannot be edited. Since [#1698](https://github.com/dlrivada/Encina/issues/1698) the encryption is a System.Text.Json contract modifier that runs whenever Marten serializes an **owner object** (an object that has `[CryptoShredded]` members).

The guarantee holds only if **every** copy of the value depends on the deleted key. A copy that Marten writes without serializing its owner object, or a copy the application makes into a member without `[CryptoShredded]`, is plaintext, survives the key deletion, and is still readable after Encina reports the erasure as done.

A concrete story. A psychology practice runs the reference application ([SPEC-002](../specifications/SPEC-002-eu-regulatory-readiness.md); health data, Art. 9). A `PatientRegistered` event carries `[CryptoShredded] string Email`.

- A developer adds `[DuplicateField]` to `Email` on the patient read model so a lookup by e-mail is fast. Marten fills that column from the in-memory object, which is decrypted: the e-mail is in a plain column.
- An `IEventMetadataEnricher` copies the e-mail into an event header "for support". Headers are a dictionary of strings; a string has no owner, so the value is stored in plaintext on every event of the session.
- The outbox publishes `PatientRegistered` to a broker. `IMessageSerializer` knows nothing about crypto-shredding, so the outbox row and the broker payload hold the plaintext.
- The patient later asks for erasure. Encina deletes the key and `MartenEventPersonalDataLocator` reports success, because it reads only events. The three plaintext copies remain.

Each step looks harmless. Together they turn an Art. 17 erasure into a false statement, which is the worst outcome for a compliance feature.

## 2. What the code does today

### 2.1 The bypass paths

Severity scale of the #1770 report: **High** personal data stored in plaintext or erased data made readable again; **Medium** plaintext in logs or a likely misuse with limited reach; **Low** integrity or availability only. Encina citations re-verified at `af3742f54`; Marten citations are decompiler output, not re-checked.

| # | Path | Through Encina API? | Guarded today? | Severity |
| --- | --- | --- | --- | --- |
| 1 | Marten Patch `Set`/`Insert`/`Append` with a string value on a crypto path: the value is serialized alone, without its owner | No (Encina only calls `Store`: `MartenReadModelRepository.cs:148`, `MartenSnapshotStore.cs:130`, `InlineProjectionDispatcher.cs:330`); yes through Marten's `IDocumentSession` | No; documented (`docs/features/crypto-shredding.md:808`, ADR-034 line 47) | High |
| 2 | Duplicated fields over a crypto member: the column is filled from the decrypted .NET member | Through Marten configuration and `[DuplicateField]` | No; `CryptoShreddingStartupValidationHostedService.cs:59-75` has no such check | High |
| 3 | Document identity on a crypto member: the `id` column holds plaintext | Through any `Store` of such a type | No; the classifier has no identity rule | High (unlikely in practice) |
| 4 | Computed, GIN and full-text indexes over a crypto member: they index the token | Through Marten configuration | Not needed for privacy | Low (a lookup by plaintext never matches; a unique computed index never enforces uniqueness) |
| 5 | LINQ `Where(x => x.Email == "...")` sends the plaintext literal as a parameter; `Select` returns the token | Yes: `IReadModelRepository.QueryAsync`/`CountAsync` accept any `IQueryable` transform (`MartenReadModelRepository.cs:107-119`, `:304-314`) | No; documentation covers `Select` only (`crypto-shredding.md:809`) | Medium |
| 6 | Raw `JsonDocument` upcasters | Through Marten configuration; Encina's upcasters are typed (`EventUpcasterBase.cs:37-38`, `LambdaEventUpcaster.cs:22-23`) | Yes, by Marten: it throws when the serializer is not the concrete `SystemTextJsonSerializer`, so the stream fails to load (fail closed) | Low (availability) |
| 7 | `FlatTableProjection` columns read the decrypted event | Through Marten projection registration | No; documented (`crypto-shredding.md:808`) | High |
| 8 | Read models and aggregate state that copy a decrypted value into an unattributed member | Yes: `InlineProjectionRelay.cs:102-118`, `MartenProjectionManager.cs:207-236`, `MartenReadModelRepository.cs:148` | Partly: attributed read models are encrypted; unattributed copies are a documented limit (`crypto-shredding.md:615`, `:816`) | High (the most likely real-world leak) |
| 9 | Snapshots | Yes | Yes, through the serializer (`SnapshotEnvelope.cs:70`) | None |
| 10 | Event metadata headers: built-in `UserId`/`TenantId`, `CustomHeaders` and every enricher value, written with `session.SetHeader` (`EventMetadataEnrichmentService.cs:95-114`, `:137-141`); an enricher receives the plaintext event | Yes | No | High when an enricher copies personal data; Medium baseline (`UserId` is a direct identifier kept forever) |
| 11 | Metadata readers after [#1773](https://github.com/dlrivada/Encina/issues/1773) return tokens (`MartenEventMetadataQuery.cs:59`, `:93`, `:136`, `:168`) | Yes | Yes | Low; tracked by [#2423](https://github.com/dlrivada/Encina/issues/2423), [#2433](https://github.com/dlrivada/Encina/issues/2433) |
| 12 | Async daemon | Yes | Yes: `SkipSerializationErrors=false` forced (`ConfigureMartenCryptoShredding.cs:52`) and verified at startup | Low |
| 13 | Marten's `DefaultMartenLogger` logs SQL parameter values at Debug, and on failure unconditionally | Indirectly: Encina sets no Marten logger | No | Medium (it amplifies rows 1, 2, 3, 5, 7, 10) |
| 14 | Archived events and Marten event masking | Through Marten | Yes, through the serializer | Low |
| 15 | Domain events leaving Marten: the outbox serializes notifications with `IMessageSerializer` (now `OutboxWriter.cs:63`, shared by `OutboxPostProcessor` and `TransactionalOutboxPipelineBehavior`), which has no crypto-shredding awareness | Yes | No | High: plaintext at rest in the outbox table and in broker payloads |
| 15b | **New since the report:** the dead letter queue serializes failed requests with the same serializer (`DeadLetterOrchestrator.cs:144`, `:222`) | Yes | No | High: a failed request with personal data is dead-lettered in plaintext |
| 16 | Key store co-located and unwrapped (`SubjectKeyDocument.cs:46`); a backup restored from before an erasure brings the key back | Yes | No | High; tracked by [#1191](https://github.com/dlrivada/Encina/issues/1191) |
| 17 | DSR locator coverage: `MartenEventPersonalDataLocator` reads events only (`MartenEventPersonalDataLocator.cs:95`), so the copies of rows 1-3, 7, 8, 10, 15 are neither reported (Art. 15) nor erased (Art. 17) | Yes | No | High (false assurance) |

### 2.2 What changed since the report

- No file under `src/Encina.Marten*`, `docs/features/crypto-shredding.md` or ADR-034 changed between `ad6986bb3` and `af3742f54`. ADR-034 line 47 still reads "Limits that no serializer hook can cover are documented, not enforced".
- The outbox write moved: [#718](https://github.com/dlrivada/Encina/issues/718) and [#719](https://github.com/dlrivada/Encina/issues/719) introduced `OutboxWriter`, now the single place where both outbox paths serialize (`OutboxWriter.cs:63`), and [ADR-037](../architecture/adr/037-transactional-outbox.md) commits the row with the business transaction. The kind of leak is unchanged; there is now one fix point.
- [ADR-050](../architecture/adr/050-modules-may-require-modules.md) amends ADR-019 and ADR-028: event-sourced modules will publish domain events through an outbox written in the Marten session ([#2383](https://github.com/dlrivada/Encina/issues/2383)). That future writer must reuse whatever messaging guard is chosen, or it reopens row 15.
- `Encina.Messaging.Encryption` already ships an `EncryptingMessageSerializer` decorator (`src/Encina.Messaging.Encryption/Serialization/EncryptingMessageSerializer.cs:49`), and `IMessageSerializer` documents decorators (`IMessageSerializer.cs:6-16`): a guard can be a decorator without changing the abstraction.
- The follow-up issues opened on 2026-10-17 are all still open: [#2455](https://github.com/dlrivada/Encina/issues/2455) (startup validation), [#2456](https://github.com/dlrivada/Encina/issues/2456) (outbox plaintext), [#2457](https://github.com/dlrivada/Encina/issues/2457) (header guard), [#2461](https://github.com/dlrivada/Encina/issues/2461) (analyzer), [#2462](https://github.com/dlrivada/Encina/issues/2462) (Marten logger), [#2463](https://github.com/dlrivada/Encina/issues/2463) (proof-of-concept tests).

### 2.3 Claims to correct

| Claim | What the evidence shows |
| --- | --- |
| #1770 context: "`StoreOptions.Storage` exposes no public enumeration of all document mappings" | Marten 9.38 exposes `IReadOnlyStoreOptions.AllKnownDocumentTypes()` and `IDocumentType.DuplicatedFields`, `IdMember` and `Indexes` (report, row 2; the names are present in the local `Marten.dll`, visibility not re-checked here). If they are public, as the decompiled code indicates, a startup guard for rows 2 and 3 is feasible; #2463 and #2455 confirm it |
| #2465 comment of 2026-10-17: "Settled by elimination ... A + B + C together; D, E, F rejected" | Written by an agent, not by the maintainer; the same comment sits on #2455, #2456, #2457, #2461 and #2462. Only E is excluded by a rule (as the sole answer, below). The reasons given against D and F are reliability and trade-off arguments from code reading, so they return to the maintainer as dilemmas. The same pattern was overturned for #2414 |

## 3. What others do

- **Marten** itself: verified only in decompiled code, its documentation names none of these paths as bypasses of `ISerializer`. It has a built-in event masking feature (`AddMaskingRuleForProtectedInformation`, `ApplyEventDataMasking`) that rewrites stored events through the serializer, which could complement a header guard after the fact (row 14).
- **EF Core** hides SQL parameter values in logs unless `EnableSensitiveDataLogging()` is opted in; Marten's default logger does the opposite (row 13). EF Core's documentation was not re-opened for this page.
- **Roslyn analyzers in Encina**: `Encina.Security.ABAC.Analyzers` already ships diagnostics with mixed severity (`DiagnosticDescriptors.cs:20` Error, `:32` Warning), a precedent for option C.
- No other .NET event-sourcing library with crypto-shredding was surveyed in this pass; the #1770 investigation was Marten-specific.

## 4. EU and Spanish law (not legal advice)

- **GDPR Art. 17 and Art. 5(1)(e).** Crypto-shredding is erasure only when every copy depends on the deleted key. Rows 1-3, 7, 8, 10, 15, 15b and 16 leave copies that do not; row 17 reports the erasure as complete when it is not.
- **Art. 32(1)(a)** names encryption as a security measure; plaintext copies and keys stored next to the ciphertext (row 16) weaken it.
- **Art. 34(3)(a)** waives telling data subjects about a breach only when the data was unintelligible to the attacker; plaintext copies lose that waiver for the affected fields.
- **Art. 25 (data protection by design)** favours guards that stop the misconfiguration over documentation that describes it.
- **Art. 9**: the reference application processes health data, a special category.
- Not verified: the AEPD's position on whether crypto-shredding counts as erasure. NIST SP 800-88 Rev. 1 recognises cryptographic erase as a sanitization technique (engineering basis, not law).

## 5. Settled points

These are not dilemmas; each has a citation and the ADR records them without reopening them. The full register is `artifacts/decisions/1770.md`. The rows that rest on INV-005 apply it to behaviour Encina's own code performs (its startup gate, its header writer, its outbox serializer): writing `[CryptoShredded]` plaintext there is a compliance-relevant behaviour that does not fail closed, so documentation alone would not satisfy the invariant. That reading is the spike's; the maintainer can veto it, which would turn those rows into dilemmas.

| Point | Settled by |
| --- | --- |
| A confirmed plaintext path that Encina can detect (a duplicated field or an identity member over `[CryptoShredded]` data, rows 2 and 3) fails the startup instead of being documented; the only exit is an explicit, logged opt-out | SPEC-002 DEC-006 and INV-005 (`SPEC-002-eu-regulatory-readiness.md:514`), [AGENTS.md](../../AGENTS.md) section 3 (compliance and security gates fail closed); precedent: ADR-034's single classifier rejects every shape that would store plaintext. This reverses ADR-034 line 47 for these paths, which pre-dates the finding that enumeration is possible |
| The enumeration uses Marten's public API (`AllKnownDocumentTypes()`, `IDocumentType`, public according to the decompiled code, to be confirmed by #2455), never reflection into internals | The #1770 option text ("a supported way") |
| Document types not known at startup are caught by a flag in the shared classifier, so the runtime modifier rejects them at first use | ADR-034 (one classifier used by the modifier, the startup validator and the health check); INV-005 |
| A header guard exists: Encina's own `EventMetadataEnrichmentService` must not write `[CryptoShredded]` plaintext into headers | INV-005, DEC-006; the code that writes headers is Encina's (`EventMetadataEnrichmentService.cs:95-114`) |
| The header guard hook lives in `Encina.Marten` and its implementation in `Encina.Marten.GDPR` | `Encina.Marten.GDPR.csproj:12` references `Encina.Marten`, so the reverse is a project cycle; AGENTS.md section 1 (opt-in) |
| The header guard runs outside the enricher `try/catch` (today `EventMetadataEnrichmentService.cs:144-148` catches enricher exceptions, logs them and continues), so a rejection fails the append and is never swallowed; it reports the header key and an error code only | AGENTS.md section 3 (errors never swallowed, `EncinaError.Message` never logged, ROP) |
| A messaging guard exists: the outbox must not store a domain event that reaches a `[CryptoShredded]` owner in plaintext | INV-005, DEC-006; `OutboxWriter.cs:63` |
| `Encina.Messaging` never references the GDPR attribute | Project cycle (`Encina.Marten.GDPR.csproj:12`, `Encina.Marten.csproj:31`); AGENTS.md section 3 (no provider-specific code in abstractions) |
| The messaging guard needs no per-database implementation | It sits on `IMessageSerializer`, which is provider independent; AGENTS.md section 5 |
| Documentation is never the sole answer for the Encina-owned guardable paths (rows 2, 3, 10, 15), and it is always required for whatever stays residual, stating partial coverage as partial | INV-005; INV-002 (`SPEC-002:511`) |
| Scope is Marten on PostgreSQL only | SPEC-002 DEC-008 (event-sourced compliance modules are Marten-only in 1.0); AGENTS.md section 5 |
| Rows 11, 12, 14 and 16 stay on their existing issues (#2423, #2433, #1191); cached query results that hold decrypted data and survive erasure until their TTL (adjacent to #1770, not investigated) need their own issue | AGENTS.md section 11 |
| Proof-of-concept tests (#2463) come before the guards, so each guard has a failing test first | AGENTS.md section 9 (tests first) |
| New log messages need an EventId range check first: `Encina.Marten.GDPR` owns 8450-8499 and uses up to 8484 | AGENTS.md section 7 |

## 6. Open decisions (dilemmas)

Effort is relative size. Each option states when it wins. The recommendation is the spike's reading; the maintainer decides. The register keys (D3, D4, ...) are given for traceability.

### C1. A build-time analyzer for paths in application code (rows 1, 5, 7 `Map`, 8)

INV-005 binds what Encina does, not what the application writes, so no rule decides how far Encina goes into application code. Register D23, D28-D30.

| | C1a: build `Encina.Marten.GDPR.Analyzers` | C1b: documentation only |
| --- | --- | --- |
| What it is | Diagnostics for Patch on a crypto path, LINQ `Where`/`Select` on crypto members, flat-table `Map` of crypto members, and assignments from a `[CryptoShredded]` member into an unattributed member of a stored type (data flow) | The residual paths are documented, with examples |
| Better when | Application teams write projections and patches, and row 8 (the most likely leak) must be caught before it ships | The reference application is small and code-reviewed, and 1.0 effort is better spent elsewhere |
| Pros | The only option that sees row 8; precedent in `Encina.Security.ABAC.Analyzers` | No work |
| Cons | Advisory; the data-flow rule has false negatives and false positives; three sub-choices (severity, id prefix and packaging, which types count as "stored") | Row 8 stays the most likely leak |
| Effort | Medium to large | Small |

Sub-choices if C1a wins: severity (mixed Error for precise rules and Warning for data flow, all Warning, or all Error); id prefix and packaging; data-flow scope (types passed to `Store` or projection registration in the same compilation, any type with an `Id`, or a configured list).

**Spike's recommendation: C1a after the runtime guards**, with mixed severity and the data-flow scope limited to types passed to `Store` or projection registration, following the ABAC analyzer package pattern.

### C2. Runtime interception of application code (rows 1 and 5)

Register D25, D26. Not exclusive with C1.

| | C2a: none | C2b: decorated `IDocumentSession` in DI | C2c: session listener over Patch | C2d: expression-tree guard in `MartenReadModelRepository.QueryAsync`/`CountAsync` |
| --- | --- | --- | --- | --- |
| Better when | The analyzer is judged enough | Applications get sessions from DI and dynamic patch paths matter | A prototype proves `PatchOperation` is detectable | Queries are built dynamically through Encina's repository |
| Pros | No fragile code | Catches non-literal patch paths the analyzer cannot | Sees every Patch | Covers the one LINQ path Encina owns |
| Cons | Rows 1 and 5 rely on C1 or documentation | Cannot stop sessions opened directly from `IDocumentStore` | Depends on the internal `PatchOperation` and a `Role()` hidden with `new` (code reading); every Marten upgrade re-tests it | Cost per query; does not cover direct Marten LINQ |
| Effort | None | Medium | Medium, plus upgrade risk | Small to medium |

**Spike's recommendation: C2a now, C2d as a cheap addition** because it is the only Encina-owned path among rows 1 and 5. C2b and C2c can be revisited if #2463's tests show the analyzer misses real cases.

### C3. Raw `JsonDocument` upcasters: keep the wrapper, or register the real serializer (issue option F)

Register D8, D24. Today raw upcasters fail closed (Marten throws); Encina's typed upcasters work.

| | C3a: keep the wrapper; no startup check | C3b: keep the wrapper; add a startup check | C3c: option F, register the real `SystemTextJsonSerializer` and move framing and staging to a session listener |
| --- | --- | --- | --- |
| Better when | Raw upcasters are not used | A production stream that fails to load later is worse than an early message | An application has many raw upcasters on streams holding `cs2` tokens |
| Pros | No work; already fail closed | Earlier, clearer failure | Removes the availability limit |
| Cons | The failure surfaces at first load of the stream | `UpcastingRegistry.AllTransformations` may not reliably tell raw from typed transformations (report, summary) | Fixes availability, not a leak; a raw upcaster would copy tokens into new events without decrypting them; loses the wrapper's lazy installation check and staged writes |
| Effort | None | Small, if detection proves reliable | Medium |

**Spike's recommendation: C3a.** It is fail closed today, and C3c weakens guarantees for an availability gain. C3b only if a prototype shows reliable detection.

### C4. Startup validation details (rows 2, 3, 4, 7)

The rejection of rows 2 and 3 is settled (section 5); its shape is not. Register D3, D4, D6, D7.

| Sub-choice | Options | Spike's recommendation |
| --- | --- | --- |
| **Opt-out shape** | (a) reuse `CryptoShreddingOptions.ValidateOnStartup = false`, which turns off all validation including the check that the contract modifier is installed; (b) a per-rule switch; (c) a per-type or per-member allow-list; (d) an attribute on the member (not usable on third-party types) | (c), because (a) also disables the installation check and (c) keeps every other mapping guarded |
| **Which members count as crypto** | (a) a member of the duplicated or identity chain carries `[CryptoShredded]` directly; (b) also a member whose type reaches an owner; (c) also owner types in assemblies not scanned | (b): fail closed for nested value objects; (c) only if multi-assembly solutions show false negatives |
| **Flat-table projections (row 7)** | (a) fail start on a coarse over-approximation (handled event types reach an owner) with a logged opt-out per projection; (b) warn until a prototype proves exact detection; (c) leave to the analyzer and documentation | (a) for a health-data deployment, since one plaintext column outweighs a blocked startup; it needs the prototype that #2455 already lists |
| **Indexes over crypto members (row 4)** | (a) startup warning; (b) fail start; (c) documentation only | (a): nothing leaks, but a unique computed index that never enforces uniqueness deserves a visible warning |

### C5. The header guard (row 10)

The guard is settled; its rule is not. Register D14, D16.

| Sub-choice | Options | Spike's recommendation |
| --- | --- | --- |
| **Matching rule** | (a) exact equality with any `[CryptoShredded]` plaintext of the events being appended; (b) normalized equality (trim, case); (c) substring; (d) also detect unsalted hashes | (b): catches trivial transformations without the short-value collisions of (c); the documentation states what it misses (INV-002) |
| **Scope** | (e) enricher values only (static `CustomHeaders` cannot hold event plaintext); (f) every header writer | (e) |
| **Built-in `UserId` and `TenantId` headers** | (a) keep and document them as a retained identifier; (b) make `UserId` capture an explicit opt-in, or warn at startup; (c) pseudonymize before storing (needs a shreddable mapping) | (b): the default should not retain a direct identifier forever in an append-only store; (a) is legitimate where staff act for the controller and the audit purpose is documented |

### C6. The messaging guard (rows 15 and 15b)

The guard is settled; where it lives and how far it reaches are not. Register D18-D21.

| Sub-choice | Options | Spike's recommendation |
| --- | --- | --- |
| **Where it lives** | (a) a generic extension point in `Encina.Messaging` that `Encina.Marten.GDPR` implements; (b) an `IMessageSerializer` decorator registered by `AddEncinaMartenGdpr`, ordered against `EncryptingMessageSerializer`; (c) an extension of `Encina.Messaging.Encryption` | (b): no change to the abstraction, which already invites decorators |
| **Coverage** | (a) the outbox only; (b) every `IMessageSerializer` user (outbox, dead letter, inbox, saga state, scheduler, delayed retry) | (b) with one exception to decide: failing at dead-letter capture would lose the failing message, so the dead letter path could store a redacted record instead of throwing |
| **Escape** | (a) only messages marked `[EncryptedMessage]`; (b) also a shared crypto contract, so consumers receive tokens and need the key store; (c) no escape, integration events must be mapped to data-minimal types | (a) |
| **Failure form** | (a) throw from `Serialize<T>` (a configuration error, like `CryptoShreddingConfigurationException`); (b) change `IMessageSerializer` to return `Either`, breaking every implementer; (c) detect at startup from registered notification types (cannot see runtime types) | (a) plus (c) as an early warning |

### C7. Marten's logger (row 13)

Register D31.

| | C7a: startup warning | C7b: install a redacting `IMartenLogger` when the application set none | C7c: wrap and redact the application's own logger | C7d: C7a and C7b | C7e: documentation |
| --- | --- | --- | --- | --- | --- |
| Better when | No behaviour change is wanted | Health data must not reach logs by default | The application's destination must be kept but redacted | Defence in depth | Least work |
| Cons | Cannot stop the unconditional parameter logging on failure | Hides parameters needed for debugging | Overrides application behaviour; needs an explicit opt-out | Most work | Leaves the amplifier of rows 1-3, 5, 7, 10 |

**Spike's recommendation: C7d.** It is the EF Core default (values hidden unless opted in) applied to Marten, and the warning tells the team why parameters are missing.

### C8. The DSR locator's blind spot (row 17)

INV-002 requires partial coverage to be stated as partial, but not where. Register D32.

| | C8a: documentation | C8b: the locator also reports the known plaintext copies where a guard was opted out | C8c: the erasure result states partial coverage at runtime |
| --- | --- | --- | --- |
| Better when | The guards shrink the surface enough | Art. 15 and 17 evidence must cover opted-out configurations | No false assurance may reach the controller's records |
| Effort | Small | Medium | Small to medium |

**Spike's recommendation: C8c with C8a.** An erasure that cannot prove completeness should say so in the result the controller keeps, not only in a page.

### C9. Form of the ADR-034 amendment

Register D33. AGENTS.md section 8 requires an ADR, not its form.

| | C9a: a new ADR that amends ADR-034 | C9b: an inline "Amendment" paragraph in ADR-034, as done for #1773 |
| --- | --- | --- |
| Better when | Three new mechanisms and a reversed stance on line 47 are a decision of their own | Traceability in one place matters most |

**Spike's recommendation: C9a**, with a one-line pointer added to ADR-034.

## 7. Proposed sequencing

Ordering is not a decision for the maintainer; this is the spike's proposal once C1-C9 are decided.

1. Proof-of-concept tests that show plaintext in PostgreSQL for rows 2, 7, 10 and 15 ([#2463](https://github.com/dlrivada/Encina/issues/2463)).
2. EventId range check for `Encina.Marten.GDPR`; startup validation for rows 2 and 3 plus the C4 choices ([#2455](https://github.com/dlrivada/Encina/issues/2455)).
3. Messaging guard ([#2456](https://github.com/dlrivada/Encina/issues/2456), extended to row 15b) and header guard ([#2457](https://github.com/dlrivada/Encina/issues/2457)); #2383's Marten-session outbox reuses the messaging guard.
4. Marten logger ([#2462](https://github.com/dlrivada/Encina/issues/2462)); DSR locator result (C8).
5. Analyzer ([#2461](https://github.com/dlrivada/Encina/issues/2461)) if C1a wins.
6. Documentation of the residual limits and the new guards ([#2464](https://github.com/dlrivada/Encina/issues/2464)).

The agent-written "settled by elimination" comments on #2455, #2456, #2457, #2461 and #2462 should be superseded by a pointer to the maintainer's decisions before any worker brief is written.

## 8. Unknowns

- Visibility and exact semantics of `AllKnownDocumentTypes()` and `IDocumentType.DuplicatedFields`/`IdMember`/`Indexes` in Marten 9.38 (names present; not exercised).
- Whether a `FlatTableProjection`'s handled event types can be read through public API (needs the #2455 prototype).
- Whether `UpcastingRegistry.AllTransformations` can tell raw `JsonDocument` transformations from typed ones.
- Whether a duplicated nested value object is serialized through the serializer (and so encrypted), which decides how strict C4 "which members count" must be.
- The content of JasperFx dead letters when an application turns on `SkipApplyErrors` (row 12).
- The AEPD's position on crypto-shredding as erasure.

## Related issues

[#1770](https://github.com/dlrivada/Encina/issues/1770), [#2465](https://github.com/dlrivada/Encina/issues/2465), [#1698](https://github.com/dlrivada/Encina/issues/1698), [#1773](https://github.com/dlrivada/Encina/issues/1773), [#2455](https://github.com/dlrivada/Encina/issues/2455), [#2456](https://github.com/dlrivada/Encina/issues/2456), [#2457](https://github.com/dlrivada/Encina/issues/2457), [#2461](https://github.com/dlrivada/Encina/issues/2461), [#2462](https://github.com/dlrivada/Encina/issues/2462), [#2463](https://github.com/dlrivada/Encina/issues/2463), [#2464](https://github.com/dlrivada/Encina/issues/2464), [#2423](https://github.com/dlrivada/Encina/issues/2423), [#2433](https://github.com/dlrivada/Encina/issues/2433), [#1191](https://github.com/dlrivada/Encina/issues/1191), [#2383](https://github.com/dlrivada/Encina/issues/2383), [#718](https://github.com/dlrivada/Encina/issues/718), [#719](https://github.com/dlrivada/Encina/issues/719), [#2414](https://github.com/dlrivada/Encina/issues/2414).
