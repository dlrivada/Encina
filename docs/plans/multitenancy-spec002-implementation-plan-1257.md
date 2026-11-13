# Implementation Plan: Multi-tenancy of the SPEC-002 capabilities and the two-tenant isolation suite

> **Issue**: [#1257](https://github.com/dlrivada/Encina/issues/1257)
> **Type**: Feature (SPEC-002 tracking id P-54, requirement REQ-061, acceptance AC-043, scenario S11; part of the EPIC #1186)
> **Complexity**: Very High (10 phases, 10 database providers plus Marten, about 119 files, eleven worker pull requests plus the two-tenant classes that each child feature adds)
> **Estimated Scope**: ~36 production files, ~68 test files, ~11 documentation files
> **Related plans** (children that consume the pieces defined here, cited by issue number): #798, #845, #752, #1636 (module, not tenant), #751, #711 (owns `Tenancy` EventIds 1805-1810)

---

## Summary

SPEC-002 DEC-009 (b) requires every capability SPEC-002 adds or changes to be tenant-aware on every store and provider it ships on, because a tenant key added after 1.0 breaks the API and the schema. This issue owns what no child covers: the **shared tenant-scope primitive**, the **fail-closed tenant configuration lookup**, the **tenant scoping of keys**, the **two-tenant isolation suite of AC-043** and the **tracking of the children** that carry the rest. It is a programme plan: it defines the shared pieces precisely, names the owner of every other requirement, and splits the work into worker pull requests.

**State of the code on `d1793713d` (current `origin/main`; the cited files were re-read on it).**

- `Encina.Tenancy` resolves tenants (`ITenantProvider.GetCurrentTenantId()` returns `string?`, `ITenantStore` is read-only, `TenancyOptions.Tenants` is a static list, `AddEncinaTenancy` registers `MultiTenancyMarker`). `ITenantContext`, a `TenantId` type and `IComplianceApplicabilityResolver` do not exist. #876 (the tenancy core epic) and #456 are open; #357 was closed as not planned.
- The **fail-closed tenant rule is implemented three times, each slightly different**: `DefaultConsentService.TryResolveTenantScope`/`TryResolveWriteTenantScope`, `ABACDecisionAuditReader.ResolveTenant`, `DeadLetterManager` (with a tri-state `RequireTenantContext` in Consent and an `AllowCrossTenantQueries` boolean in the ABAC reader that #3271 removes). DSR and `RepositoryErrors.TenantRequired` have their own copies. Open bugs of the same class: #1949, #2439, #3135, #3147.
- Persisted state with no tenant: the outbox, inbox, saga and scheduled-message interfaces (`IOutboxMessage`, `IInboxMessage`, `ISagaState`, `IScheduledMessage` contain no `TenantId`; #737, #738, #739, #760 are open); column-encryption keys (`Encina.Security.Encryption.IKeyProvider.GetKeyAsync(keyId)`); the audit and ABAC stores until #798; the AI Act module until #845/#847. `Encina.Security.PII` has no persisted state.
- Subject keys are **already tenant-scoped on PostgreSQL**: `PostgreSqlSubjectKeyProvider` takes `session.TenantId` at construction (`_tenantId`, line 95), opens `_store.QuerySession(_tenantId)` per operation (lines 138, 195, 233), `LightweightSession(_tenantId, ...)` for writes (line 487) and locks with `ComputeSubjectLockKey(tenantId, subjectId)` (line 272), i.e. it is scoped through Marten's tenancy. The port methods take only a `subjectId`; `SubjectKeyDocument` has `SubjectId` and no tenant field of its own; `InMemorySubjectKeyProvider` has no tenant at all. The real gaps are the in-memory provider, the key identity and the ciphertext `kid`, and where the tenant comes from.
- Three different interfaces are named `IKeyProvider` (`Encina.Security.Encryption`, `Encina.Security.AntiTampering`, `Encina.Compliance.Anonymization`). The column-encryption port is `Encina.Security.Encryption.Abstractions.IKeyProvider`; it is implemented by `InMemoryKeyProvider` (Security.Encryption), `AwsKmsKeyProvider` (`Encina.Messaging.Encryption.AwsKms`), `AzureKeyVaultKeyProvider` (`Encina.Messaging.Encryption.AzureKeyVault`) and a guard-test fake, and called from `AesGcmFieldEncryptor` and `EncryptionHealthCheck`; `DefaultMessageEncryptionProvider` and the NIS2 `DefaultEncryptionValidator` also call methods of this name (the worker confirms which interface each uses); by grep 22 source files and 19 test files mention it, many only in documentation comments. `Encina.Messaging.Encryption.ITenantKeyResolver` exists but the message encryption pipeline does not call it yet.
- The capabilities that need per-tenant credentials, sequences, senders and storage (OAuth tokens P-32, outbound credentials and client certificates P-31/P-44, webhook secrets #731/#734, sequences and chains P-36/P-38, senders P-33, blob storage P-37, pacing #469) are mostly **not implemented yet**; this plan defines the contracts they must use, not their stores.
- The reference tests exist for the pattern: `ConsentTenantScopingIntegrationTests`, `DSRTenantIsolationContractTests`, `DSRTenantIsolationPropertyTests`. There is no cross-store, cross-provider two-tenant suite.

**Shared pieces and their owner** (the sibling plans refer to these names):

| Shared piece | Owner | Used by |
|--------------|-------|---------|
| `ITenantScopeResolver` with the one signature of Design Choice 1 (`ResolveRead(string? requestedTenantId)`, `ResolveWrite(string? explicitTenantId, bool allowTenantless = false)`), `TenantScope`, `TenantScopeErrors`, `TenantCacheKey` (core, namespace `Encina`) | this plan, PR 1257-A1 | #798, #845, #752, every SPEC-002 store |
| Default-tenant rule (Design Choice 2) | this plan, PR 1257-A1 | every new tenant-keyed store |
| `ITenantConfigurationResolver<TConfig>`, `TenantSecretRef`, metric `encina.tenancy.missing_tenant_config`, span `encina.tenancy.resolve` | this plan, PRs 1257-B1 and B2 | credential, sender, storage features |
| Tenant-scoped subject and column keys | this plan, PRs 1257-C1, C2a, C2b | crypto-shredding, column encryption |
| Two-tenant isolation suite and its architecture test | this plan, PRs 1257-D1 and D2 | all children (each adds its store descriptor) |
| Tenant telemetry helper (`SetTenantTag`) | #2676 | all packages (ADR-054 D4) |
| Tenant restored from persisted context in background work | #1164 | outbox, inbox, saga, scheduler, retention sweep, purge |
| Tenant columns in messaging entities | #737, #738, #739, #760 | AC-043 messaging rows |
| EventId range for the primitives (`TenantIsolation = (7420, 7469)`, ids 7420-7424 used) | this plan, PR 1257-A2 | the primitives |

**What the siblings need from this plan, by name** (one signature, repeated identically in #798, #752 and #845): `ITenantScopeResolver.ResolveRead(string? requestedTenantId)` and `ResolveWrite(string? explicitTenantId, bool allowTenantless = false)`, both returning `Either<EncinaError, TenantScope>`; `TenantScope(string? TenantId, bool IsMultiTenant)`; error codes `encina.tenancy.tenant_required` (ErrorCategory InvalidRequest) and `encina.tenancy.tenant_mismatch` (Forbidden); `TenantCacheKey.Segment(string? tenantId)`. Every sibling is **agnostic to the default-tenant Dilemma (Design Choice 2)** because each states its behavior under both outcomes (the table of Design Choice 2); none hard-codes `null` or `"default"`.

**Scope.** (1) the shared primitive and the default-tenant rule; (2) the fail-closed tenant configuration contract with secret references; (3) tenant-scoped subject and column-encryption keys; (4) the registry-driven two-tenant isolation suite on 10 providers and Marten with an architecture test that fails when a SPEC-002 store has no isolation descriptor; (5) the AC-043 closure tracker. **Out of scope**: the stores of the children (each child plan lists its own provider work), the platform-operator cross-tenant permission (#3271, #2972), pacing implementation (#469, #740).

**Affected packages**: `Encina` (primitive), `Encina.Tenancy`, `Encina.Marten.GDPR`, `Encina.Security.Encryption`, `Encina.TestInfrastructure` (test harness), and, for the suite, every package with a store. **Provider category**: Database (10 providers) plus Marten for the suite; Caching for the key helper.

---

## Design Choices

<details>
<summary><strong>1. Where the tenant-resolution rule lives: one shared primitive in the core, or per-package code</strong></summary>

**Class:** Dilemma (recommended, pending the maintainer)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ITenantScopeResolver` and `TenantScope` in `Encina` (`ResolveRead`, `ResolveWrite`), over `IRequestContextAccessor` and `MultiTenancyMarker`; the cross-tenant operator path is not part of it** | One rule for at least five new consumers in this family (#798, #845, #752, the key stores, the configuration lookup); the next shape change (#456, #3271) is made once | A public API in the core, frozen at 1.0; the cross-tenant path (#3271, #2972) is undecided, so the primitive must stay minimal |
| **B) Keep the rule inside each package (copy `TryResolveWriteTenantScope`)** | No new public API; each package tunes its rule | Already drifted three times (tri-state option, `AllowCrossTenantQueries`, different error codes); every new store copies it again |
| **C) A shared helper in `Encina.Tenancy`** | Close to tenant concepts | Compliance and audit packages would reference `Encina.Tenancy`, which they do not do today (the marker exists to avoid exactly that) |

### Chosen Option: **A — A minimal shared primitive in the core** (recommended, pending the maintainer)

### Rationale

- A wins when the number of consumers is large and the rule is stable, which holds for the read/write rule (ambient wins; multi-tenant with no tenant is denied; single-tenant passes); B wins if the cross-tenant rule is still moving, which is why the primitive excludes it.
- Proposed shape (names are proposals; this is the single signature, repeated in Phase 2 and its prompt and in the siblings): `ITenantScopeResolver { Either<EncinaError, TenantScope> ResolveRead(string? requestedTenantId); Either<EncinaError, TenantScope> ResolveWrite(string? explicitTenantId, bool allowTenantless = false); }` (`allowTenantless` returns, instead of `tenant_required`, the single-tenant scope when tenancy is off (no tenant, or the default tenant under Design Choice 2 B) and `TenantScope(null, true)` when a multi-tenant application has neither an ambient nor an explicit tenant, for system writers such as audit and startup seeding; a mismatch is still denied), `readonly record struct TenantScope(string? TenantId, bool IsMultiTenant)`, error codes `encina.tenancy.tenant_required` (`ErrorCategory.InvalidRequest`, ADR-062 R3: the request lacks a tenant) and `encina.tenancy.tenant_mismatch` (`ErrorCategory.Forbidden`), created with `EncinaErrors.Create(code, category, message)`, fixed messages and no tenant value. A blank or whitespace tenant is no tenant (the #3147 bug class); a padded tenant id is rejected, never trimmed.
- **Activation and the missing marker (fail closed).** The scope is multi-tenant when `MultiTenancyMarker` is registered **or** the request context carries a non-blank ambient tenant. An application that resolves tenants itself and never registers the marker therefore still gets the gate (the control is never disabled by configuration, the 2026-10-30 principle); the resolver logs once, by code, that an ambient tenant arrived without the marker (EventId 7423). A test covers the case.
- **The resolver rejects the reserved id (Design Choice 2 B only).** `default` is rejected as a requested, explicit or ambient tenant by `TenantScopeResolver` itself when multi-tenant (a request or write that names it gets `tenant_mismatch`; an ambient `default` gets `tenant_required`), so an application that resolves tenants by hand and registers the marker manually cannot alias the no-tenant data. The `Encina.Tenancy` options validator rejects it as well (Phase 1 task 5); the resolver is the place every tenant enters, the validator is the early failure.
- Existing implementations (Consent, DSR, ABAC reader, dead letters) migrate in their own issues; they are not part of this plan.

</details>

<details>
<summary><strong>2. When tenancy is off: a fixed default tenant, or a null tenant</strong></summary>

**Class:** Dilemma (recommended, pending the maintainer)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Null tenant everywhere (shipped behavior of Consent, DSR, audit): `TenantId` is `NULL`, filters are skipped, keyed stores use `COALESCE(TenantId, '')` in unique indexes and keys** | No change to shipped modules; zero configuration | NULL is special in unique indexes and composite keys on every provider (SQL treats NULLs as distinct); sequences and key stores need a sentinel anyway; contradicts the approved SPEC text |
| **B) A reserved default tenant id (`"default"`) that the primitive returns when tenancy is off, stamped on every row of the stores SPEC-002 adds or changes; the resolver and the `Encina.Tenancy` options validator reject it as a real tenant id** | Matches SPEC-002 REQ-061 ("a fixed default tenant applies") and AC-043 ("with tenancy off, the same tests pass with no tenant configuration"); no NULL in keys, sequences or indexes | (a) **Turning tenancy on later cannot rename the single tenant everywhere**: Marten events carry the tenant and are immutable, and ciphertext whose `kid` embeds the tenant cannot be re-keyed without re-encrypting, so under B `default` stays permanent in event-sourced stores and in existing key ids; (b) **shipped NULL-tenant rows** (audit, consent, DSR, dead letters) are not read by a single-tenant query for `default`, so they need a named alignment issue per store family (a data update, pre-1.0, no installed base), opened when B is chosen; no phase of this plan does it; (c) a real tenant called "default" is forbidden, enforced in the resolver and the validator (Design Choice 1); (d) the doc example in `IQueryCacheKeyGenerator` (`requestContext.TenantId ?? "default"`, `src/Encina.Caching/Abstractions/IQueryCacheKeyGenerator.cs:44`) teaches the opposite idiom and is changed in PR 1257-A2 |
| **C) B for new keyed stores only, A for existing modules** | Smallest change | Two representations of "no tenant" in one codebase |

### Chosen Option: **B — A reserved default tenant** (recommended, pending the maintainer)

### Rationale

- A wins if the maintainer prefers to keep shipped shapes and accepts sentinel handling per store, and when permanence of `default` in events and key ids is not acceptable; B wins when keys and sequences must be tenant-keyed without special cases, which REQ-061 demands for sequences, chains, key stores and credential stores.
- **The siblings are agnostic only through this table**: every consumer reads `scope.TenantId` and states its behavior under both outcomes.

| Consumer behavior | Under A (null tenant) | Under B (`default`) |
|-------------------|-----------------------|---------------------|
| `TenantScope.TenantId` of a single-tenant request | `null` | `"default"`, never null |
| Single-tenant read of a store | tenant filter skipped | filter on `default` (rows written under the previous shipped shape with NULL need the alignment of con (b)) |
| System write with `allowTenantless: true` (audit, startup seeding) in a single-tenant application | `null` tenant row | `default` tenant row |
| System write with `allowTenantless: true` in a multi-tenant application with no ambient and no explicit tenant (operator or background row) | `TenantScope(null, true)`: a no-tenant row | `TenantScope(null, true)`: the same no-tenant row (the default tenant is never stamped on an operator row); consumers that need a global row (the #798 policy seeding) decide it from `scope.IsMultiTenant` and the caller's declared identity, never from `scope.TenantId` being null |
| `TenantCacheKey.Segment(scope.TenantId)` | `g` | `t.default` |
| Unique index over `(tenant, key)` | `COALESCE(TenantId, '')` or two filtered indexes | plain `(TenantId, key)` |
| Reserved id check | none (`default` is an ordinary tenant) | resolver and options validator reject `default` |

</details>

<details>
<summary><strong>3. Where per-tenant credentials live: references to a secret provider, or a store per capability</strong></summary>

**Class:** Dilemma (recommended, pending the maintainer)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A tenant holds `TenantSecretRef` values (a provider name and a secret name) and the value stays in a secret provider (`Encina.Security.Secrets` and its 4 providers); `ITenantConfigurationResolver<TConfig>` loads the non-secret settings and resolves the secrets for the ambient tenant** | Secrets never sit in an Encina table or in a log (`AGENTS.md` section 3); one lookup, one metric and one failure mode; works with the existing providers | Needs a secret provider in production; local development uses the in-memory provider |
| **B) Each capability (OAuth tokens, gateway credentials, certificates, webhook secrets, senders, storage) brings its own tenant-keyed store** | Fully independent | Seven credential stores with seven encryption and rotation stories; seven fail-closed implementations |
| **C) Credentials as encrypted columns of one new tenant configuration table on 10 providers** | One store | A new store on 10 providers and Marten for something secret providers already do; Encina becomes a secret vault |

### Chosen Option: **A — Secret references and one fail-closed resolver** (recommended, pending the maintainer)

### Rationale

- A wins when a secret manager exists in the deployment (the usual case for a processor of health data); C wins for small deployments that want zero external services, and the in-memory provider covers development.
- `ITenantConfigurationResolver<TConfig>.ResolveAsync(ct)` returns `Left(encina.tenancy.missing_tenant_config)` when the ambient tenant has no configuration, never another tenant's and never a default (the maintainer comment of 2026-10-17 on [#1257](https://github.com/dlrivada/Encina/issues/1257); the error code `encina.tenancy.missing_tenant_config` has `ErrorCategory.Internal`, ADR-062 R1: a server-side configuration lookup is not a request error).

</details>

<details>
<summary><strong>4. A missing tenant configuration fails closed, and background work restores its tenant</strong></summary>

**Class:** Settled
**Settled by:** the maintainer comment on [#1257](https://github.com/dlrivada/Encina/issues/1257) of 2026-10-17 ("a missing tenant configuration fails closed and never falls back to another tenant"), SPEC-002 DEC-006, INV-008 and REQ-061, `AGENTS.md` section 3 (gates fail closed; errors are never swallowed in background infrastructure), and #1164 (persisted request context).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Fail closed on a missing tenant or configuration; background cycles read the tenant from the persisted context of the message and never mix tenants in one unit of work** | INV-008 holds | A job without a restored context fails and is retried or dead-lettered |
| **B) Fall back to the default or the first tenant** | Never blocks | Cross-tenant use of credentials and keys |

### Chosen Option: **A — Fail closed**

### Rationale

- This plan owns the verification (the suite runs every background cycle for tenant A and asserts it never touches B); the restore itself is #1164 and the tenant columns are #737, #738, #739, #760.

</details>

<details>
<summary><strong>5. Subject keys (Marten.GDPR): keep the session-bound tenant, or add an explicit tenant parameter to the port</strong></summary>

**Class:** Dilemma (recommended, pending the maintainer)

Verified fact: `PostgreSqlSubjectKeyProvider` is already tenant-scoped through the Marten session it is constructed with (State of the code). The requirement (SPEC-002 REQ-061: "one tenant's key material never decrypts another tenant's data, and shredding in one tenant never affects another") is met on PostgreSQL for the key rows; the gaps are `InMemorySubjectKeyProvider` (no tenant), the key identity (below) and the ciphertext `kid`.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Add `string tenantId` to the six methods of `ISubjectKeyProvider`; callers pass the tenant from the scope or the event metadata** | Explicit; works where no session exists | Breaking change of a port that 29 files name (the Marten.GDPR serializer, erasure strategy, health check, startup validation, options, models and 13 test files), so it cannot fit one pull request; replay and serialization would have to read a tenant from event metadata that the session already carries |
| **B) Keep the tenant bound to the session (the shipped PostgreSQL behavior); `InMemorySubjectKeyProvider` takes its tenant the same way (constructed per tenant by a factory, or keyed by the `IQuerySession.TenantId` it is given); the port stays unchanged; the key identity and `SubjectForgottenMarker` become tenant-qualified and injective** | No port change, smallest pull request; consistent with the shipped implementation and with Marten's per-tenant sessions; a session with no tenant is `*DEFAULT*` in Marten and is mapped by the Design Choice 2 rule | The provider is tied to the session's tenant (replay without a tenant-scoped session uses the Marten default tenant, which must be refused when multi-tenancy is on) |

### Chosen Option: **B — Session-bound tenant, injective tenant-qualified identity** (recommended, pending the maintainer)

### Rationale

- A wins when key operations must run for a tenant other than the session's (an operator tool shredding for a named tenant); B wins when the keys are only used inside tenant-scoped Marten sessions, which is how the module works today.
- **Injective identity (both options).** The key identity `{tenant}:{subject}:{version}` joined by `:` is not injective (tenant `a:b` with subject `c` equals tenant `a` with subject `b:c`), and it is stamped into ciphertext as the `kid` (`subject:{subjectId}:v{version}` today): one tenant's shredding or decryption could reach another tenant's keys. The document id and the `kid` use the percent-encoded segment of `TenantCacheKey` for the tenant and for the subject (`%` and `:` encoded), so two distinct `(tenant, subject)` pairs never produce one id; a test uses `:`-bearing tenant and subject ids on both providers.
- When multi-tenancy is on and the session's tenant is the Marten default (no tenant), every key operation returns `Left(encina.tenancy.tenant_required)` (fail closed).

</details>

<details>
<summary><strong>5b. Column-encryption keys (`Encina.Security.Encryption.IKeyProvider`): explicit tenant parameter, ambient tenant, or the existing resolver</strong></summary>

**Class:** Dilemma (recommended, pending the maintainer)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Add `string? tenantId` to `GetKeyAsync`, `GetCurrentKeyIdAsync`, `RotateKeyAsync`; the key id embeds the tenant by the injective encoding of Design Choice 5; callers pass `scope.TenantId`. Staged so every pull request builds: PR 1257-C2a adds the parameter with a `null` default and teaches `InMemoryKeyProvider` to scope by it, while `AwsKmsKeyProvider` and `AzureKeyVaultKeyProvider` reject a non-null tenant with `Left` until they can scope it (fail closed, never ignored); PR 1257-C2b implements the tenant in the two cloud providers, passes the tenant in every caller and makes the parameter required (no default)** | Explicit; the key port has no request context; each pull request builds and fails closed | A temporary optional parameter between C2a and C2b; the port is referenced by about 22 source and 19 test files, so two pull requests |
| **B) Providers read the ambient tenant through `ITenantScopeResolver`** | No signature change | Background jobs (rotation, health check, NIS2 validation) have no ambient tenant; a silent wrong tenant or a fail-closed denial of every maintenance job |
| **C) Use the existing `ITenantKeyResolver` of `Encina.Messaging.Encryption`** | Already present | It is not called by the pipeline yet and lives in the messaging package, not in the port that column encryption uses |

### Chosen Option: **A — Explicit tenant parameter, staged** (recommended, pending the maintainer)

### Rationale

- A wins when jobs outside a request need keys; B wins if every use is inside a request; C wins if the message-encryption package becomes the one key abstraction (a separate decision). The staging is the "optional default first, required last" rule of the plan brief: no overload of the old signature is kept at the end.
- Shredding or rotating the keys of `(A, keyId)` never affects `(B, keyId)` (AC-043).

</details>

<details>
<summary><strong>6. The isolation suite: registry-driven descriptors, one base per assertion, one concrete class per provider</strong></summary>

**Class:** Trivial
**Settled by:** `AGENTS.md` section 9 (shared `[Collection]` fixtures, tests execute real code, tests of a provider-dependent feature cover all 10 providers) and the existing templates `ConsentTenantScopingIntegrationTests`, `DSRTenantIsolationContractTests`, `DSRTenantIsolationPropertyTests`.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ITenantIsolationStoreDescriptor` per store (write for a tenant, list/query/export/background-cycle for a tenant, count); abstract test bases in `Encina.TestInfrastructure`; a concrete class per provider collection; an architecture test that fails when a SPEC-002 store has no descriptor** | New stores cannot skip AC-043; one assertion set reused | A descriptor per store to maintain |
| **B) Hand-written two-tenant test per store and provider** | Simple | About 11 times the number of stores in classes; drift between copies |

### Chosen Option: **A — Descriptors, bases and an architecture test**

### Rationale

- The descriptor list is the AC-043 checklist: a store enters the list in the pull request that adds it.

</details>

<details>
<summary><strong>7. Telemetry names and the tenant tag</strong></summary>

**Class:** Trivial
**Settled by:** #2676 owns the tenant attribute key and the tenant tag helper; ADR-054 D4 decides only that the tenant is an attribute and a metric tag when multi-tenancy is on, and settled point 4 the instrument naming (lowercase dot-separated, no `_total`); the instrument names `encina.tenancy.missing_tenant_config` and `encina.tenancy.resolve` come from the issue text.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Use the issue's instrument names; the tenant attribute is set only when multi-tenancy is on, through the #2676 helper once it exists (until then the span carries no tenant attribute, the key `encina.tenant_id` of the issue is not written in this plan's code and not public API); the counter's only tag is `capability`** | One attribute key for the family; no cardinality blow-up | The span has no tenant attribute until #2676 lands |
| **B) Write the attribute key in this package now** | Immediate attribute | A second owner of the key; a rename when #2676 lands |

### Chosen Option: **A — Issue names, tenant attribute through the #2676 helper**

### Rationale

- The tenant is never in log messages; the counter counts refused operations and carries no tenant value (cardinality, ADR-054 consequences).

</details>

<details>
<summary><strong>8. Tenant id rules, in-memory configuration source, `TenantSecretRef.ToString()` and the start-up check (registered choices with a user-visible consequence)</strong></summary>

**Class:** Dilemma (recommended, pending the maintainer) for the tenant id format. Registered next to it, each Trivial citing its rule (`AGENTS.md` section 3, no option satisfies it better): `TenantSecretRef.ToString()` prints the provider only, never the name (secret-bearing types override `ToString()`, #851); the in-memory `ITenantConfigurationSource<TConfig>` holds secret references only, never values, and a reference resolves through the in-memory secret provider (no secret in options or logs); the start-up check (Phase 5) fails host start, logged once by code, when a tenant-keyed capability is registered with an empty configuration source in a multi-tenant application (gates fail closed).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A tenant id is at most 128 characters, compared ordinally, blank is no tenant, padded ids rejected, enforced by the resolver and the options validator** | 128 equals the `TenantId` column length of the audit stores, so no store truncates or fails; one rule for every store | A hosting application with longer tenant ids must map them |
| **B) No limit in the resolver; each store decides its column size** | No cap in the primitive | Each store truncates, fails or hashes differently; a tenant id valid in one store is refused in another |

### Chosen Option: **A — 128 characters, ordinal, no trimming** (recommended, pending the maintainer)

### Rationale

- A wins when one tenant id must be valid in every store, which the two-tenant suite assumes; B wins if tenant ids are long external identifiers that the host cannot map. A tenant id is persisted in every store and becomes API at 1.0.

</details>

---

## Implementation Phases

### Phase 1: Core Abstractions & Models (PR 1257-A1; the Tenancy validator is in PR 1257-A2)

> **Goal**: the shared tenant-scope primitive and the default-tenant rule.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create `src/Encina/Abstractions/ITenantScopeResolver.cs`**, **`TenantScope.cs`**, **`TenantScopeErrors.cs`** (codes `encina.tenancy.tenant_required` with `ErrorCategory.InvalidRequest` and `encina.tenancy.tenant_mismatch` with `ErrorCategory.Forbidden`, created with `EncinaErrors.Create(code, category, message)`; fixed messages; a `GetCategory` unit test per code, ADR-062).
2. **Create `TenantCacheKey.cs`**: `static string Segment(string? tenantId)` (returns `t.{percent-encoded tenant}` for a tenant and `g` for none, two disjoint namespaces, so no tenant id such as `_` can alias the global scope; `%`, `:` and `.` are percent-encoded, so the segment is injective: two distinct tenant ids never give one segment) used by every cache-key builder and by the key identity of Design Choice 5.
3. **Create `DefaultTenant.cs`**: `public const string Id = "default"` (only if Design Choice 2 is B).
4. **Reserved id (B only):** `TenantScopeResolver` rejects `default` as a requested, explicit or ambient tenant (Design Choice 1); its unit tests cover each.
5. **Modify `src/Encina.Tenancy/Configuration/TenancyOptions.cs` validation** in PR 1257-A2: reject the reserved default id, blank ids, ids with leading or trailing whitespace and ids over the length limit of Design Choice 8.
6. PublicAPI lines (mechanical-fixer).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
- Issue #1257, plan multitenancy-spec002-implementation-plan-1257.md. Design Choices 1 (shared primitive) and 2 (default tenant) are answered by the maintainer before this phase starts.
- The rule today lives in DefaultConsentService, ABACDecisionAuditReader and DeadLetterManager.

TASK:
Create the types of the tasks above in the Encina core. No existing consumer is migrated here.

KEY RULES:
- ROP: Either<EncinaError, TenantScope>; no exceptions for business logic.
- Errors carry fixed messages and no tenant value; whitespace tenant is no tenant; padded ids are rejected, never trimmed.
- XML docs on every public member; PublicAPI.Unshipped.txt via mechanical-fixer; zero warnings.

REFERENCE FILES:
- src/Encina.Compliance.Consent/Services/DefaultConsentService.cs (TryResolveWriteTenantScope)
- src/Encina.Security.ABAC/DecisionAudit/ABACDecisionAuditReader.cs (ResolveTenant)
- src/Encina/Abstractions/MultiTenancyMarker.cs
```

</details>

### Phase 2: Default Implementation & Registration (PR 1257-A1; logging in PR 1257-A2)

> **Goal**: the resolver implementation, registered once.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create `src/Encina/Core/TenantScopeResolver.cs`** (`internal sealed`): constructor `(IRequestContextAccessor accessor, IServiceProviderIsService isService)`; the scope is multi-tenant when `IsService(typeof(MultiTenancyMarker))` (checked lazily at first use) **or** a non-blank ambient tenant is present (Design Choice 1: an ambient tenant without the marker is treated as multi-tenant, never as single-tenant). `ResolveRead(string? requestedTenantId)`: ambient wins; a requested tenant different from the ambient one is `tenant_mismatch`; no ambient in a multi-tenant application is `tenant_required`; single-tenant returns `TenantScope(default or null, false)` per Design Choice 2. `ResolveWrite(string? explicitTenantId, bool allowTenantless = false)`: ambient default; explicit different from ambient is `tenant_mismatch`; multi-tenant with neither is `tenant_required` unless `allowTenantless`, which returns `TenantScope(null, true)` (a single-tenant application with `allowTenantless` gets the single-tenant scope of Design Choice 2: no tenant under A, the default tenant under B). The reserved id is rejected as in Design Choice 1.
2. **Modify the core registration** (`AddEncina`, `Core/ServiceCollectionExtensions.cs`): `TryAddSingleton<ITenantScopeResolver, TenantScopeResolver>()`.
3. DI test with `ValidateOnBuild` and `ValidateScopes`; the marker registered before and after. Resolver unit tests: single-tenant allows (no marker, no ambient), multi-tenant with ambient, with matching request, with mismatching request, without any (denied), whitespace tenant, ambient tenant without the marker (treated as multi-tenant), the reserved `default` as requested, explicit and ambient tenant (B), `allowTenantless` on a system write.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
- Phase 1 merged. The accessor is a singleton backed by AsyncLocal; the marker is a singleton registered by AddEncinaTenancy.

TASK:
Implement and register TenantScopeResolver with the single signature ResolveRead(string? requestedTenantId) and ResolveWrite(string? explicitTenantId, bool allowTenantless = false); unit tests for the cases listed in the Phase 2 tasks, including ambient tenant without the marker and the reserved default id.

KEY RULES:
- Ambient tenant without the marker is multi-tenant (the control is never off by configuration).
- Registration completeness (AGENTS.md section 3): the resolver sees the marker in any registration order, because it checks the container lazily at first use.
- CRAP <= 10 per method.

REFERENCE FILES:
- src/Encina.Messaging/DeadLetter/DeadLetterManager.cs (AmbientTenantId)
- src/Encina.Tenancy/ServiceCollectionExtensions.cs (marker registration at line 109)
```

</details>

### Phase 3: Fail-Closed Tenant Configuration (PRs 1257-B1 core contract and B2 Tenancy implementation)

> **Goal**: one lookup contract for per-tenant configuration and secrets.

<details>
<summary><strong>Tasks</strong></summary>

1. **PR B1.** **Create `src/Encina/Abstractions/ITenantConfigurationResolver.cs`**: `ITenantConfigurationResolver<TConfig>` with `ValueTask<Either<EncinaError, TConfig>> ResolveAsync(CancellationToken ct)`; **`TenantSecretRef.cs`** (`sealed record (string Provider, string Name)`, `ToString()` prints the provider and not the name); **`TenantConfigurationErrors.cs`**: `Missing()` with code `encina.tenancy.missing_tenant_config`, `ErrorCategory.Internal` (ADR-062 R1), a fixed message and a `GetCategory` unit test.
2. **PR B2.** **Create `src/Encina.Tenancy/Configuration/TenantConfigurationResolver.cs`**: the default implementation backed by a `ITenantConfigurationSource<TConfig>` (static per-tenant options map for development and tests, `Tenants[tenantId]`); resolves the tenant with `ITenantScopeResolver`; a missing entry returns `TenantConfigurationErrors.Missing()` and logs EventId 7424 by code.
3. **PR B2 diagnostics**: counter `encina.tenancy.missing_tenant_config` (`Counter<long>`, tag `capability`), activity `encina.tenancy.resolve` in `TenancyActivitySource`/`TenancyMetrics`; the tenant attribute only when multi-tenancy is on, through the #2676 helper once it exists (Design Choice 7).
4. **PR B2 DI**: `AddEncinaTenantConfiguration<TConfig>(Action<...>)` in `Encina.Tenancy`.
5. The secret-provider-backed source (Design Choice 3A) is added by the first capability that needs a secret (OAuth, P-32); its contract test is already part of PR B2 (a fake secret provider).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
- Phase 2 merged. Design Choices 3 (secret references) and 4 (fail closed) are answered.

TASK:
Create the resolver contract, the default source, the errors, the counter and the span. Unit, guard and contract tests; a two-tenant test proving tenant A never gets tenant B's configuration and that a tenant without configuration is refused.

KEY RULES:
- Secrets are never values in options, logs or exceptions: options with a reference override ToString() (AGENTS.md section 3).
- Log by error code only; never EncinaError.Message.
- The metric has no tenant tag value.

REFERENCE FILES:
- src/Encina.Tenancy/Diagnostics/TenancyMetrics.cs
- src/Encina.Tenancy/Diagnostics/TenancyActivitySource.cs
- src/Encina.Security.Secrets/ServiceCollectionExtensions.cs
```

</details>

### Phase 4: Tenant-Scoped Keys (PRs 1257-C1, C2a, C2b)

> **Goal**: subject keys and column-encryption keys never cross tenants.

<details>
<summary><strong>Tasks</strong></summary>

1. **PR C1 (subject keys, Design Choice 5 B recommended).** `KeyStore/SubjectKeyDocument.cs`: a `TenantId` field and the injective id (`TenantCacheKey`-style percent-encoded tenant and subject segments, then the version); `KeyStore/InMemorySubjectKeyProvider.cs` scoped to a tenant the way `PostgreSqlSubjectKeyProvider` is (through the tenant of the session or factory it is built from); `KeyStore/SubjectForgottenMarker.cs` per tenant; the ciphertext `kid` built from the same encoded segments; both providers return `Left(encina.tenancy.tenant_required)` when multi-tenancy is on and no tenant is bound. `ISubjectKeyProvider` is unchanged under B (under A it gains `string tenantId` on its six methods and the 29 files naming it move in a staged series like Design Choice 5b).
2. **PR C2a (column keys, Design Choice 5b A).** `src/Encina.Security.Encryption/Abstractions/IKeyProvider.cs`: `string? tenantId = null` on `GetKeyAsync`, `GetCurrentKeyIdAsync`, `RotateKeyAsync`; `InMemoryKeyProvider.cs` keyed by `(tenant, keyId)` with the injective encoding; `AwsKmsKeyProvider` and `AzureKeyVaultKeyProvider` return `Left(encina.encryption.tenant_unsupported)` (`ErrorCategory.Internal`, a server-side limit, ADR-062 R1; created with `EncinaErrors.Create`, `GetCategory` tested) for a non-null tenant they cannot yet scope (fail closed, never ignored); the guard-test fake `GuardKeyProvider` and the contract test follow.
3. **PR C2b.** Implement the tenant in the two cloud providers (key alias or vault key name from the encoded segments), pass `scope.TenantId` in `AesGcmFieldEncryptor`, `EncryptionHealthCheck` and the other callers confirmed by grep, and remove the default so the parameter is required.
4. Tests: key of tenant A cannot decrypt tenant B's data; shredding `(A, s)` leaves `(B, s)` readable; a tenant `a:b` with subject `c` and a tenant `a` with subject `b:c` get different key ids and different `kid` values (both key ports, in-memory and PostgreSQL through Testcontainers); a provider with no tenant when multi-tenancy is on is refused.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT:
- Design Choices 5 and 5b. PostgreSqlSubjectKeyProvider is already tenant-scoped through its Marten session (_tenantId, QuerySession(_tenantId), ComputeSubjectLockKey(tenantId, subjectId)); InMemorySubjectKeyProvider is not. The column-encryption port is Encina.Security.Encryption.Abstractions.IKeyProvider, implemented by InMemoryKeyProvider, AwsKmsKeyProvider, AzureKeyVaultKeyProvider and a guard-test fake.

TASK:
PR C1: make the subject-key identity and the in-memory provider tenant-qualified and injective. PR C2a/C2b: the staged tenant parameter on the column-encryption port. One pull request at a time; each builds alone with its tests.

KEY RULES:
- Key material never appears in logs or exceptions.
- Identity is injective: percent-encode tenant and subject segments; test with ':' in both.
- Event-sourced compliance modules use Marten on PostgreSQL via Testcontainers for integration tests; unit tests mock IAggregateRepository (AGENTS.md section 3).
- The staged optional parameter ends required in C2b; no overload of the old signature remains.

REFERENCE FILES:
- src/Encina.Marten.GDPR/KeyStore/PostgreSqlSubjectKeyProvider.cs
- src/Encina.Marten.GDPR/Abstractions/ISubjectKeyProvider.cs
- src/Encina.Security.Encryption/Abstractions/IKeyProvider.cs
```

</details>

### Phase 5: Configuration & DI

> **Goal**: opt-in registration and start-up checks.

<details>
<summary><strong>Tasks</strong></summary>

1. With tenancy off nothing needs configuring (Design Choice 2); with `AddEncinaTenancy` the primitive reports multi-tenant automatically.
2. A start-up check in `Encina.Tenancy` fails the host (logged once by code) when a tenant-keyed capability is registered with `ITenantConfigurationSource<T>` empty in a multi-tenant application (Design Choice 8).
3. DI tests for every new registration (`ValidateOnBuild`, `ValidateScopes`), registration order both ways.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT:
- Every AddEncina* method that adds a service must register the options and dependencies it resolves (AGENTS.md section 3).

TASK:
Add the start-up check and the DI tests.

KEY RULES:
- A database store wins over an in-memory default in any registration order.
- Startup checks log once, by code.

REFERENCE FILES:
- src/Encina.Security.ABAC/DecisionAudit/ABACDecisionAuditStartupCheck.cs
```

</details>

### Phase 6: The Two-Tenant Isolation Suite on 10 Providers and Marten (PRs 1257-D1, D2 and the children)

> **Goal**: AC-043 as an executable, registry-driven suite.

<details>
<summary><strong>Tasks</strong></summary>

1. **`tests/Encina.TestInfrastructure/TenantIsolation/`**: `ITenantIsolationStoreDescriptor` (`Name`, `WriteAsync(tenant, key)`, `QueryAsync(tenant)`, `ExportAsync(tenant)`, `RunBackgroundCycleAsync(tenant)`, `CountAsync(tenant)`), `TenantIsolationStoreRegistry`, and the abstract bases `TenantIsolationQueryTestsBase`, `TenantIsolationBackgroundCycleTestsBase`, `TenantIsolationDefaultTenantTestsBase` (tenancy off, no tenant configuration).
2. **Concrete classes per provider collection**: `ADO-SqlServer`, `ADO-PostgreSQL`, `ADO-MySQL`, `Dapper-*` (3), `EFCore-*` (3), MongoDB, and Marten on PostgreSQL; each in the existing `[Collection("<Family>-<Database>")]` and `ClearAllDataAsync` in `InitializeAsync`.
3. **Architecture test** `tests/Encina.UnitTests/Testing/Architecture/TenantIsolationCoverageTests.cs`: every type in the closed list of SPEC-002 store interfaces (audit, read audit, policy, consent, DSR, retention, lawful basis, outbox, inbox, saga, scheduled message, dead letter, subject keys, ...) has a descriptor, **or** is on an explicit allowlist of pending stores, each entry naming its open issue (`IOutboxMessage` store #737, inbox #738, saga #739, scheduled messages #760, policy and audit stores #798, the AI Act stores #845), so the test is green the day it lands and an allowlist entry cannot be added without an issue number; the list is extended, and an allowlist entry removed, by the child that adds the store.
4. **Start with the stores that exist and are already tenant-keyed** (consent, DSR, retention, dead letters, audit after #798); the rest join as their issues merge (#737, #738, #739, #760, #798, #596).
5. **S11**: the scenario test of SPEC-002 section 4.3 runs the descriptors for two tenants in one deployment.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```text
CONTEXT:
- Shared integration fixtures: SqlServerFixture, PostgreSqlFixture, MySqlFixture and the EFCore variants; collections defined in Collections.cs files; never IClassFixture for database fixtures.

TASK:
Create the harness, the registry, the three abstract bases, the concrete classes for the stores that already exist, and the architecture test with its allowlist of pending stores (each entry carries its issue number). Do not duplicate assertions per provider.

KEY RULES:
- Tests execute real package code; Shouldly through Encina.Testing.Shouldly; no Thread.Sleep.
- Every query, export, DSR request and background cycle run as tenant A asserts it returns, acts on and exports nothing of tenant B.
- With tenancy off the same tests pass with no tenant configuration.

REFERENCE FILES:
- tests/Encina.IntegrationTests/Compliance/Consent/ConsentTenantScopingIntegrationTests.cs
- tests/Encina.ContractTests/Compliance/DataSubjectRights/DSRTenantIsolationContractTests.cs
- tests/Encina.PropertyTests/Compliance/DataSubjectRights/DSRTenantIsolationPropertyTests.cs
```

</details>

### Phase 7: Cross-Cutting Integration

> **Goal**: caching, health, background cycles, audit.

<details>
<summary><strong>Tasks</strong></summary>

1. **Caching**: `TenantCacheKey.Segment` is the only way to build a tenant segment; an architecture test scans cache-key builders of the family for the helper.
2. **Health**: the tenant store check of #596 reports a tenant-store failure as unhealthy; owned by #596, referenced here in the tracker.
3. **Background cycles**: verification only (Phase 6); the restore is #1164.
4. **Audit**: tenant configuration changes are audited by #752.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```text
CONTEXT:
- Cross-cutting items owned elsewhere are verified, not re-implemented.

TASK:
Add the cache-key architecture test and the tracker entries; implement nothing owned by #1164, #596 or #752.

KEY RULES:
- No duplicated tenant segment logic in any package.

REFERENCE FILES:
- src/Encina.Security.ABAC/Persistence/CachingPolicyStoreDecorator.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

### Phase 8: Observability

> **Goal**: metrics, span, log messages, range.

<details>
<summary><strong>Tasks</strong></summary>

1. Register the new `TenantIsolation` range `TenantIsolation = (7420, 7469)` in `src/Encina/Diagnostics/EventIdRanges.cs` first (PR 1257-A2), and extend the existing `Encina` entry of `AssemblyRanges` in `EncinaEventIdAllocationTests` (it is `[Core, ModuleIsolation]` on `origin/main`, line 41) with `nameof(EventIdRanges.TenantIsolation)`; a new entry is not added.
2. `[LoggerMessage]` ids, five sequential ids from 7420 and no gaps: 7420 tenant required, 7421 tenant mismatch, 7422 invalid or reserved tenant id, 7423 ambient tenant without the marker (warning, once), 7424 missing tenant configuration (used by PR 1257-B2); codes only, never a tenant value. 7425-7469 stay unallocated until used.
3. Counter and span of Phase 3; a test with an in-memory exporter (AC-044) asserts that the main operations emit them with the tenant attribute only when multi-tenancy is on, and that no tag, metric or log carries a payload or a direct identifier.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```text
CONTEXT:
- ADR-021 and ADR-054 D4. The new range is `TenantIsolation = (7420, 7469)`.

TASK:
Register the range, add the log messages, the exporter test, and the architecture-test map entry.

KEY RULES:
- Ranges registered before use; ids sequential; never log EncinaError.Message or the tenant secret reference name.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

### Phase 9: Testing — 7 Test Types

> **Goal**: per-flag targets for the primitives and the suite.

<details>
<summary><strong>Tasks</strong></summary>

| Test type | Scope | Location |
|-----------|-------|----------|
| Unit | resolver (6 cases), configuration resolver, key providers (in-memory), validators | `tests/Encina.UnitTests/Tenancy/`, `.../Marten/GDPR/`, `.../Security/Encryption/` |
| Guard | every new public constructor and method | `tests/Encina.GuardTests/` |
| Contract | `ITenantScopeResolver`, `ITenantConfigurationResolver<T>`, `ISubjectKeyProvider`, `IKeyProvider` tenant contracts | `tests/Encina.ContractTests/Tenancy/` |
| Property | FsCheck: no generated query or export returns another tenant's record, for any two distinct tenants and any keys | `tests/Encina.PropertyTests/Tenancy/TenantIsolationPropertyTests.cs` |
| Integration | the two-tenant suite of Phase 6 on 10 providers and Marten | `tests/Encina.IntegrationTests/TenantIsolation/` |
| Load | required (issue): tenant isolation under concurrent access (two tenants, interleaved writes and reads, no leak); `DOTNET_JitObjectStackAllocationConditionalEscape=0` locally | `tests/Encina.LoadTests/Tenancy/TenantIsolationLoadTests.cs` |
| Benchmark | justification `.md`: the resolver is a marker check and a string comparison; the overhead benchmark is #935 | `tests/Encina.BenchmarkTests/Tenancy/TenantScope.md` |

**Per-file targets** (proposed; measured values replace them, never lower). "Baseline" is the per-file target in `.github/coverage-manifest/` on `d1793713d`; "none" means the file has no per-file entry and the package default applies. A modified file keeps its baseline at least.

| File | Baseline | Flags | Justification |
|------|----------|-------|---------------|
| `Encina/Core/TenantScopeResolver.cs` (new, A1) | new | unit 100, guard 90, contract 95 | every resolver case of Phase 2 is unit-testable; guard covers the constructor arguments. |
| `Encina/Abstractions/TenantScope.cs` (new, A1) | new | unit 100 | a record struct built in the resolver tests. |
| `Encina/Abstractions/TenantScopeErrors.cs` (new, A1) | new | unit 100 | two factories asserted with their category. |
| `Encina/Abstractions/TenantCacheKey.cs` (new, A1) | new | unit 100, property 90 | pure static; FsCheck proves injectivity (two distinct ids never give one segment). |
| `Encina/Abstractions/DefaultTenant.cs` (new, A1, B only) | new | unit 0 | one constant, no executable line. |
| `Encina/Core/ServiceCollectionExtensions.cs` (A1) | unit 96, guard 86, contract 92 | unchanged | one registration in a covered method; the DI test proves it. |
| `Encina/Diagnostics/EventIdRanges.cs` (A2) | none | unit 0 | one static field, covered by the architecture tests that scan it. |
| `Encina/Diagnostics/TenantIsolationLog.cs` (new, A2) | new | unit 90 | generated logging methods exercised by the resolver tests with a fake logger. |
| `Encina/Abstractions/ITenantConfigurationResolver.cs` (new, B1) | new | unit 0 | an interface, no executable line. |
| `Encina/Abstractions/TenantSecretRef.cs` (new, B1) | new | unit 100, guard 80 | a record whose `ToString()` is asserted not to print the name; guard covers null and blank arguments. |
| `Encina/Abstractions/TenantConfigurationErrors.cs` (new, B1) | new | unit 100 | one factory asserted with its category. |
| `Encina.Tenancy/Configuration/TenantConfigurationResolver.cs` (new, B2) | new | unit 98, guard 90, contract 90 | missing and present paths. |
| `Encina.Tenancy/Configuration/TenancyOptions.cs` and its validator (A2) | unit 100, guard 85 | unchanged | the reserved, blank, padded and over-long id rules are unit-tested. |
| `Encina.Tenancy/Diagnostics/TenancyMetrics.cs` (B2) | none | unit 90 | the counter is read through an in-memory meter listener. |
| `Encina.Tenancy/Diagnostics/TenancyActivitySource.cs` (B2) | unit 90 | unchanged | one more span, asserted by the exporter test. |
| `Encina.Tenancy/ServiceCollectionExtensions.cs` (B2) | unit 95, guard 70 | unit 95, guard 70 | one registration in a covered method. |
| `Encina.Marten.GDPR/KeyStore/PostgreSqlSubjectKeyProvider.cs` (C1) | none | unit 80, integration 80 | integration reaches the SQL on PostgreSQL; the id encoding is unit-tested. |
| `Encina.Marten.GDPR/KeyStore/InMemorySubjectKeyProvider.cs` (C1) | none | unit 95 | dictionary logic. |
| `Encina.Marten.GDPR/KeyStore/SubjectKeyDocument.cs` and `SubjectForgottenMarker.cs` (C1) | none | unit 90 | the id and tenant fields are asserted in the provider tests. |
| `Encina.Security.Encryption/InMemoryKeyProvider.cs` (C2a) | unit 90, guard 90, contract 90, property 55 | unit 95, guard 90, contract 90, property 55 | dictionary logic with the tenant in the key. |
| `Encina.Security.Encryption/Algorithms/AesGcmFieldEncryptor.cs` and `Health/EncryptionHealthCheck.cs` (C2b) | unit 98, guard 95, contract 90, property 80; unit 95, guard 85, contract 85 | unchanged | one argument passed in already covered methods. |
| `Messaging.Encryption.AwsKms/AwsKmsKeyProvider.cs` and `AzureKeyVault/AzureKeyVaultKeyProvider.cs` (C2a, C2b) | none | unit 80 | the refusal (C2a) and the tenant-qualified key name (C2b) are unit-tested with fakes. |

CRAP gate: methods stay at or below 10; the resolver cases are separate small methods.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 9</strong></summary>

```text
CONTEXT:
- Per-flag coverage (AGENTS.md section 9). Load tests are excluded from standard CI; run them locally.

TASK:
Write the tests of the table; update the manifests of Encina, Encina.Tenancy, Encina.Marten.GDPR, Encina.Security.Encryption through mechanical-fixer with measured targets.

KEY RULES:
- Justification .md only for the benchmark; unit, guard, contract and integration are never justified away.
- Shared Collection fixtures; no per-class database fixtures.

REFERENCE FILES:
- tests/Encina.LoadTests (existing load test layout)
- .github/coverage-manifest/Encina.Tenancy.json
```

</details>

### Phase 10: Documentation & Finalization

> **Goal**: ADR, guide, READMEs, tracker closure.

<details>
<summary><strong>Tasks</strong></summary>

1. ADR in `docs/architecture/adr/`: "Tenant scope primitive, default tenant and fail-closed tenant configuration" once Design Choices 1 to 3 are answered (ADR number assigned when the ADR pull request opens).
2. Guide `docs/features/multi-tenancy.md` (how the marker, the primitive, the default tenant, secret references and the isolation suite fit together), READMEs of `Encina.Tenancy`, `Encina.Marten.GDPR`, `Encina.Security.Encryption` (docs-writer).
3. `changelog.d/1257-tenant-scope-primitive.added.md`, `1257-tenant-scoped-keys.changed.md` (mechanical-fixer); `PublicAPI.Unshipped.txt`.
4. AC-043 tracker: a table in the guide generated from the descriptor list (covref-style marker, never typed by hand).
5. Build with 0 warnings; every flag at its target.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 10</strong></summary>

```text
CONTEXT:
- Code phases merged; documentation remains.

TASK:
Delegate pages to docs-writer, fragments and PublicAPI lines to mechanical-fixer, then verify the build.

KEY RULES:
- Never edit CHANGELOG.md; coverage figures are cited by marker, never typed.

REFERENCE FILES:
- changelog.d/README.md
- docs/engineering/PHASE0-BASELINE.md (style of cited figures)
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Requirement | Use in this feature |
|--------|-------------|---------------------|
| SPEC-002 REQ-061, INV-008, AC-043, S11 | every SPEC-002 store tenant-aware; no cross-tenant read, action or export; two-tenant suite | scope and acceptance |
| SPEC-002 DEC-006, DEC-009 (b) | fail closed; tenancy required in 1.0 | Design Choices 1, 2, 4 |
| SPEC-002 REQ-062, AC-044 | tenant as attribute; ADR-018 evaluation | Phase 8 |
| GDPR Art. 28, Art. 32 | each practice is its own controller; security of processing | per-tenant keys and credentials |
| ADR-054 D4 | tenant tag only when multi-tenancy is on | Design Choice 7 |
| ADR-018 | 12-function evaluation | matrix below |
| Maintainer principle of 2026-10-30 (#686) | compliance never optional | no opt-out switch in the primitive |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
| :--- | :--- | :--- |
| MultiTenancyMarker | src/Encina/Abstractions/MultiTenancyMarker.cs | container signal that multi-tenancy is on; registered by AddEncinaTenancy |
| IRequestContext.TenantId and IRequestContextAccessor | src/Encina/Abstractions/IRequestContext.cs, src/Encina/Abstractions/IRequestContextAccessor.cs | ambient tenant of the request |
| AddEncinaTenancy, ITenantProvider, ITenantStore, TenantInfo, TenancyOptions | src/Encina.Tenancy/ServiceCollectionExtensions.cs, src/Encina.Tenancy/Abstractions/, src/Encina.Tenancy/Configuration/TenancyOptions.cs | tenant resolution and the static tenant list |
| TenancyMetrics, TenancyActivitySource, TenancyLog (EventIds 1800-1804) | src/Encina.Tenancy/Diagnostics/ | diagnostics of the tenancy package |
| DefaultConsentService.TryResolveTenantScope / TryResolveWriteTenantScope | src/Encina.Compliance.Consent/Services/DefaultConsentService.cs | fail-closed tenant pattern to extract |
| ABACDecisionAuditReader.ResolveTenant | src/Encina.Security.ABAC/DecisionAudit/ABACDecisionAuditReader.cs | same pattern |
| DeadLetterManager (AllTenants, TenantRequired) | src/Encina.Messaging/DeadLetter/DeadLetterManager.cs | same pattern for messaging |
| ISubjectKeyProvider, PostgreSqlSubjectKeyProvider (session-bound tenant), InMemorySubjectKeyProvider, SubjectKeyDocument | src/Encina.Marten.GDPR/Abstractions/ISubjectKeyProvider.cs, src/Encina.Marten.GDPR/KeyStore/ | PostgreSQL provider already tenant-scoped through the Marten session; in-memory provider and key identity are the gaps |
| Encina.Security.Encryption IKeyProvider, InMemoryKeyProvider, AwsKmsKeyProvider, AzureKeyVaultKeyProvider | src/Encina.Security.Encryption/Abstractions/IKeyProvider.cs, src/Encina.Security.Encryption/InMemoryKeyProvider.cs, src/Encina.Messaging.Encryption.AwsKms/, src/Encina.Messaging.Encryption.AzureKeyVault/ | column-encryption keys keyed by key id only today; four implementers |
| Secret providers (Azure Key Vault, AWS Secrets Manager, HashiCorp Vault, Google Cloud Secret Manager) | src/Encina.Security.Secrets* | hold tenant credential values; tenants keep references |
| Outbox, inbox, saga and scheduled message interfaces (no TenantId yet) | src/Encina.Messaging/Outbox/IOutboxMessage.cs, Inbox/IInboxMessage.cs, Sagas/ISagaState.cs, Scheduling/IScheduledMessage.cs | tenant columns are #737, #738, #739, #760 |
| Shared integration fixtures and collections | tests/Encina.TestInfrastructure | the two-tenant suite base classes live here |
| Existing tenant tests as templates | tests/Encina.IntegrationTests/Compliance/Consent/ConsentTenantScopingIntegrationTests.cs, tests/Encina.ContractTests/Compliance/DataSubjectRights/DSRTenantIsolationContractTests.cs, tests/Encina.PropertyTests/Compliance/DataSubjectRights/DSRTenantIsolationPropertyTests.cs | structure of the new suite |
| EventIdRanges and EncinaEventIdAllocationTests | src/Encina/Diagnostics/EventIdRanges.cs, tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs | the new range is registered here |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina` (tenant isolation primitives) | 7420-7469 (new `TenantIsolation`) | assigned in `artifacts/decisions/plan-batch/eventid-blocks.md`; register in `EventIdRanges.cs` first (PR 1257-A2); ids 7420-7424 used, packed from 7420 |
| `Encina.Tenancy` | none new | `Tenancy = (1800, 1899)` ids 1805-1810 belong to #711; this plan adds none there: its tenancy diagnostics are a counter and a span, and its log messages use the new `TenantIsolation` range. |

### Estimated File Count

| Category | Files | Notes |
| :--- | :--- | :--- |
| Production, tenant scope primitive (Encina core, Encina.Tenancy validator, IQueryCacheKeyGenerator doc fix) | 10 | PRs 1257-A1 and A2 |
| Production, fail-closed tenant configuration (Encina core and Encina.Tenancy) | 9 | PRs 1257-B1 and B2 |
| Production, subject and column-encryption keys (Marten.GDPR, Security.Encryption, the two cloud key providers, callers) | 14 | PRs 1257-C1, C2a, C2b |
| Production, diagnostics (metrics, spans, log class, EventIdRanges) | 5 | spread over A2 and B2 |
| Tests, isolation suite harness and architecture test (TestInfrastructure) | 8 | PR 1257-D1 |
| Tests, per-store two-tenant classes (10 providers plus Marten, per store family) | 33 | added by each child feature and PR 1257-D2, counted here for the suite |
| Tests, unit, guard, contract, property, load for the primitives and the key changes | 28 | with A, B, C |
| Documentation (ADR, guide, READMEs, changelog fragments, PublicAPI, manifests) | 12 | docs-writer and mechanical-fixer |
| Total | about 119 | eleven worker pull requests plus the per-store classes of the children |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Combined prompt — all phases</strong></summary>

```text
PROJECT CONTEXT:
- Encina, .NET 10 / C# 14, pre-1.0, ROP (Either<EncinaError, T>). Issue #1257 (SPEC-002 P-54, REQ-061, AC-043): the shared tenant-scope primitive, fail-closed tenant configuration, tenant-scoped keys and the two-tenant isolation suite on 10 database providers and Marten.
- ITenantContext and a TenantId type do not exist. The tenant is IRequestContext.TenantId via IRequestContextAccessor; multi-tenancy is on when MultiTenancyMarker is registered; the default tenant rule is Design Choice 2.

IMPLEMENTATION OVERVIEW:
1. Core: ITenantScopeResolver (ResolveRead(string? requestedTenantId), ResolveWrite(string? explicitTenantId, bool allowTenantless = false)), TenantScope, TenantScopeErrors (InvalidRequest and Forbidden), TenantCacheKey, DefaultTenant; Tenancy validator for the reserved id.
2. TenantScopeResolver registered with TryAddSingleton; unit cases including ambient tenant without the marker and the reserved id.
3. ITenantConfigurationResolver<TConfig>, TenantSecretRef, default source, counter encina.tenancy.missing_tenant_config (Internal), span encina.tenancy.resolve.
4. Subject keys: injective tenant-qualified identity, in-memory provider scoped like the PostgreSQL one (Design Choice 5); column keys: staged tenant parameter (Design Choice 5b); per-tenant shredding.
5. DI tests in both registration orders; start-up check.
6. Isolation suite: descriptors, registry, abstract bases, per-provider classes, architecture test with an allowlist of pending stores; S11.
7. Cache-key architecture test; verification of #1164, #596, #752.
8. EventId range 7420-7469 registered first; five sequential log messages; AC-044 exporter test.
9. Tests of 7 types; manifests via mechanical-fixer. 10. ADR, guide, READMEs, fragments via specialists.

KEY PATTERNS:
- Ambient tenant wins; multi-tenant (marker or ambient tenant) with no tenant is denied; blank tenant is no tenant; padded ids rejected; the reserved default id is rejected by the resolver (B).
- Missing tenant configuration fails closed, never a fallback; secrets are references, never values.
- Composite ids built from tenant and key are injective (percent-encoded segments or separate fields).
- Tenant passed as a value to stores; key providers bind it as Design Choices 5 and 5b state.
- All 10 providers and Marten in the suite; shared [Collection] fixtures; Shouldly via Encina.Testing.Shouldly.
- No AllTenants switch; the platform-operator path is #3271/#2972.

REFERENCE FILES:
- src/Encina.Compliance.Consent/Services/DefaultConsentService.cs
- src/Encina.Security.ABAC/DecisionAudit/ABACDecisionAuditReader.cs
- src/Encina.Marten.GDPR/Abstractions/ISubjectKeyProvider.cs
- tests/Encina.IntegrationTests/Compliance/Consent/ConsentTenantScopingIntegrationTests.cs
- docs/specifications/SPEC-002-eu-regulatory-readiness.md (REQ-061, AC-043, S11)
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ | `TenantCacheKey.Segment` is the one tenant segment builder; cache keys of the family include it (Phase 7). |
| 2 | OpenTelemetry | ✅ | Span `encina.tenancy.resolve`, counter `encina.tenancy.missing_tenant_config`, tenant attribute only when multi-tenancy is on (ADR-054 D4, #2676). |
| 3 | Structured Logging | ✅ | New `TenantIsolation` range 7420-7469 registered first; codes only. |
| 4 | Health Checks | ⏭️ | The tenant store health check is #596 (open, already linked from the issue); this plan verifies it in the tracker. |
| 5 | Validation | ✅ | Tenancy options validated at start-up: reserved default id, blank, padded ids; configuration sources non-empty in multi-tenant mode. |
| 6 | Resilience | ❌ | The primitives call no external system; secret-provider resilience belongs to `Encina.Security.Secrets`. |
| 7 | Distributed Locks | ❌ | No shared state updated by concurrent hosts is introduced. |
| 8 | Transactions | ❌ | Single-operation writes; "never two tenants in one unit of work" is verified by the background-cycle tests and restored by #1164. |
| 9 | Idempotency | ❌ | Not a message or request entry point. |
| 10 | Multi-Tenancy | ✅ | This feature (SPEC-002 REQ-061, AC-043). |
| 11 | Module Isolation | ❌ | SPEC-002 requires no module scoping here; `ModuleId` is #1636 and #747. |
| 12 | Audit Trail | ⏭️ | Tenant configuration changes are audited by #752. |

---

## Prerequisites & Dependencies

**AC-043 tracker** (what must be closed for the acceptance, who owns it):

| Requirement of AC-043 | Owner | State |
|-----------------------|-------|-------|
| Tenant columns and filters of outbox, inbox, saga, scheduled messages | #737, #738, #739, #760 | open |
| Audit and ABAC filtering | #798 | open, plan |
| Persistent tenant store and its health check | #596 | open |
| Background cycles restore the tenant | #1164 | open |
| Tenant tag helper | #2676 | open |
| Tenant-scoped subject and column keys, fail-closed configuration, default tenant, the suite | this plan | planned |
| Sequences, chains, credentials, senders, storage, pacing | P-32, P-31, P-44, P-33, P-36, P-37, P-38, #731, #734, #469 | not implemented; each uses the contracts of this plan |
| AI Act per-tenant behavior | #845 and #847 | open, plans |

**Sequencing**: PR 1257-A1 first (the sibling plans' hard prerequisite), then A2; B1 then B2; C1 and C2a/C2b are independent of each other and of B; D1 follows A1 and grows as the children land. The shared files of #798, #845, #752 and #1636 are listed in their plans.

---

## Next Steps

1. Maintainer answers Design Choices 1, 2, 3, 5, 5b and 8 when work on the issue starts (recorded in `## Maintainer Decisions`); the answers also fix the sibling plans (`ITenantScopeResolver`, the default tenant, the tenant-id format).
2. Worker pull requests. Counts include tests, manifest entries, PublicAPI lines, ProjectReference edits and fragments; each builds alone with its own tests and stays within 15 files and 1500 lines (recount by grep when the worker starts, the counts below are estimates):
   - **PR 1257-A1** (primitive, no logging): `ITenantScopeResolver`, `TenantScope`, `TenantScopeErrors`, `TenantCacheKey`, `DefaultTenant` (B only), `TenantScopeResolver`, `Core/ServiceCollectionExtensions.cs` (7); resolver unit test, errors and cache-key unit test, cache-key property test, guard test, contract test, DI test (6); `Encina.json` manifest, `PublicAPI.Unshipped.txt` (2): 15 files.
   - **PR 1257-A2** (range, log class, Tenancy validator): `EventIdRanges.cs`, `EncinaEventIdAllocationTests.cs` (the `Encina` map entry), `TenantIsolationLog.cs`, `TenancyOptions` validator, the resolver edit that logs, the `IQueryCacheKeyGenerator` doc fix (B only) (6); log and validator unit tests, guard test (3); `Encina.json` and `Encina.Tenancy.json` manifests, `PublicAPI.Unshipped.txt`, changelog fragment `1257-tenant-scope-primitive.added.md` (4): 13 files.
   - **PR 1257-B1** (core contract): `ITenantConfigurationResolver`, `TenantSecretRef`, `TenantConfigurationErrors` (3); unit and guard tests (2); manifest, PublicAPI (2): 7 files.
   - **PR 1257-B2** (Tenancy implementation): `TenantConfigurationResolver`, `ITenantConfigurationSource`, the in-memory source, `TenancyMetrics`, `TenancyActivitySource`, `ServiceCollectionExtensions`, `TenantIsolationLog` (id 7424) (7); unit, guard, contract, DI tests (4); `Encina.Tenancy.json` manifest, PublicAPI (2): 13 files.
   - **PR 1257-C1** (subject keys, Design Choice 5 B): `SubjectKeyDocument`, `InMemorySubjectKeyProvider`, `PostgreSqlSubjectKeyProvider`, `SubjectForgottenMarker`, an id-encoding helper (5); unit and contract tests (`ISubjectKeyProviderContractTests`), two-tenant integration test on PostgreSQL, key-id property test (4); `Encina.Marten.GDPR.json` manifest (the files have no targets yet), PublicAPI (2): 11 files. Under A the port change is staged like C2.
   - **PR 1257-C2a** (column keys, staged parameter): `IKeyProvider`, `InMemoryKeyProvider`, `AwsKmsKeyProvider`, `AzureKeyVaultKeyProvider` (4); `GuardKeyProvider` fake (the guard test file), `KeyProviderContractTests`, `InMemoryKeyProviderTests`, the two cloud provider unit tests (5); manifests and PublicAPI of three packages (4): 13 files.
   - **PR 1257-C2b** (tenant in the cloud providers, callers, parameter required): the two cloud providers, `AesGcmFieldEncryptor`, `EncryptionHealthCheck`, `IKeyProvider` (5 plus the callers confirmed by grep); their tests; PublicAPI: at most 15 files, else split by package.
   - **PR 1257-D1**: Phase 6 harness in `Encina.TestInfrastructure`, registry, three bases and the architecture test with its allowlist: 8 files plus the first concrete classes (consent, DSR, retention, dead letters on the collections they already use): about 15 files; further classes land with each child (**PR 1257-D2** groups the rest, at most 15 files each).
   - **PR 1257-E**: Phase 9 load test and benchmark justification, Phase 7 cache-key architecture test, Phase 10 documentation and ADR.
3. Link this plan from #1257 and from the EPIC #1186.
