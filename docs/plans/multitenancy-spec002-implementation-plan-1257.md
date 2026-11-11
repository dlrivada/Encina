# Implementation Plan: Multi-tenancy of the SPEC-002 capabilities and the two-tenant isolation suite

> **Issue**: [#1257](https://github.com/dlrivada/Encina/issues/1257)
> **Type**: Feature (SPEC-002 tracking id P-54, requirement REQ-061, acceptance AC-043, scenario S11; part of the EPIC #1186)
> **Complexity**: Very High (10 phases, 10 database providers plus Marten, about 109 files, seven worker pull requests plus the two-tenant classes that each child feature adds)
> **Estimated Scope**: ~34 production files, ~65 test files, ~10 documentation files
> **Related plans** (children that consume the pieces defined here): `audit-abac-multitenancy-implementation-plan-798.md`, `aiact-multitenancy-implementation-plan-845.md`, `config-change-audit-implementation-plan-752.md`, `audit-module-id-implementation-plan-1636.md` (module, not tenant)

---

## Summary

SPEC-002 DEC-009 (b) requires every capability SPEC-002 adds or changes to be tenant-aware on every store and provider it ships on, because a tenant key added after 1.0 breaks the API and the schema. This issue owns what no child covers: the **shared tenant-scope primitive**, the **fail-closed tenant configuration lookup**, the **tenant scoping of keys**, the **two-tenant isolation suite of AC-043** and the **tracking of the children** that carry the rest. It is a programme plan: it defines the shared pieces precisely, names the owner of every other requirement, and splits the work into worker pull requests.

**State of the code on `7a0049c07`.**

- `Encina.Tenancy` resolves tenants (`ITenantProvider.GetCurrentTenantId()` returns `string?`, `ITenantStore` is read-only, `TenancyOptions.Tenants` is a static list, `AddEncinaTenancy` registers `MultiTenancyMarker`). `ITenantContext`, a `TenantId` type and `IComplianceApplicabilityResolver` do not exist. #876 (the tenancy core epic) and #456 are open; #357 was closed as not planned.
- The **fail-closed tenant rule is implemented three times, each slightly different**: `DefaultConsentService.TryResolveTenantScope`/`TryResolveWriteTenantScope`, `ABACDecisionAuditReader.ResolveTenant`, `DeadLetterManager` (with a tri-state `RequireTenantContext` in Consent and an `AllowCrossTenantQueries` boolean in the ABAC reader that #3271 removes). DSR and `RepositoryErrors.TenantRequired` have their own copies. Open bugs of the same class: #1949, #2439, #3135, #3147.
- Persisted state with no tenant: the outbox, inbox, saga and scheduled-message interfaces (`IOutboxMessage`, `IInboxMessage`, `ISagaState`, `IScheduledMessage` contain no `TenantId`; #737, #738, #739, #760 are open); subject keys (`ISubjectKeyProvider` methods take a `subjectId` only, `SubjectKeyDocument` has `SubjectId` and no tenant); column-encryption keys (`IKeyProvider.GetKeyAsync(keyId)`); the audit and ABAC stores until #798; the AI Act module until #845/#847. `Encina.Security.PII` has no persisted state.
- The capabilities that need per-tenant credentials, sequences, senders and storage (OAuth tokens P-32, outbound credentials and client certificates P-31/P-44, webhook secrets #731/#734, sequences and chains P-36/P-38, senders P-33, blob storage P-37, pacing #469) are mostly **not implemented yet**; this plan defines the contracts they must use, not their stores.
- The reference tests exist for the pattern: `ConsentTenantScopingIntegrationTests`, `DSRTenantIsolationContractTests`, `DSRTenantIsolationPropertyTests`. There is no cross-store, cross-provider two-tenant suite.

**Shared pieces and their owner** (the sibling plans refer to these names):

| Shared piece | Owner | Used by |
|--------------|-------|---------|
| `ITenantScopeResolver`, `TenantScope`, `TenantScopeErrors`, `TenantCacheKey` (core, namespace `Encina`) | this plan, PR 1257-A | #798, #845, #752, every SPEC-002 store |
| Default-tenant rule (Design Choice 2) | this plan, PR 1257-A | every new tenant-keyed store |
| `ITenantConfigurationResolver<TConfig>`, `TenantSecretRef`, metric `encina.tenancy.missing_tenant_config`, span `encina.tenancy.resolve` | this plan, PR 1257-B | credential, sender, storage features |
| Tenant-scoped subject and column keys | this plan, PR 1257-C | crypto-shredding, column encryption |
| Two-tenant isolation suite and its architecture test | this plan, PR 1257-D | all children (each adds its store descriptor) |
| Tenant telemetry helper (`SetTenantTag`) | #2676 | all packages (ADR-054 D4) |
| Tenant restored from persisted context in background work | #1164 | outbox, inbox, saga, scheduler, retention sweep, purge |
| Tenant columns in messaging entities | #737, #738, #739, #760 | AC-043 messaging rows |
| EventId range for the primitives (`TenantIsolation = (7420, 7469)`) | this plan, PR 1257-A | the primitives |

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
- Proposed shape (names are proposals): `ITenantScopeResolver { Either<EncinaError, TenantScope> ResolveRead(string? requestedTenantId); Either<EncinaError, TenantScope> ResolveWrite(string? explicitTenantId); }`, `readonly record struct TenantScope(string? TenantId, bool IsMultiTenant)`, error code `encina.tenancy.tenant_required` and `encina.tenancy.tenant_mismatch` with fixed messages and no tenant value. A blank or whitespace tenant is no tenant (the #3147 bug class); a padded tenant id is rejected, never trimmed.
- Existing implementations (Consent, DSR, ABAC reader, dead letters) migrate in their own issues; they are not part of this plan.

</details>

<details>
<summary><strong>2. When tenancy is off: a fixed default tenant, or a null tenant</strong></summary>

**Class:** Dilemma (recommended, pending the maintainer)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Null tenant everywhere (shipped behavior of Consent, DSR, audit): `TenantId` is `NULL`, filters are skipped, keyed stores use `COALESCE(TenantId, '')` in unique indexes and keys** | No change to shipped modules; zero configuration | NULL is special in unique indexes and composite keys on every provider (SQL treats NULLs as distinct); sequences and key stores need a sentinel anyway; contradicts the approved SPEC text |
| **B) A reserved default tenant id (`"default"`) that the primitive returns when tenancy is off, stamped on every row of the stores SPEC-002 adds or changes; `Encina.Tenancy` rejects it as a real tenant id** | Matches SPEC-002 REQ-061 ("a fixed default tenant applies") and AC-043 ("with tenancy off, the same tests pass with no tenant configuration"); no NULL in keys, sequences or indexes; turning tenancy on later renames one tenant | Shipped modules that use NULL need a one-time alignment (pre-1.0, no installed base); a real tenant called "default" is forbidden |
| **C) B for new keyed stores only, A for existing modules** | Smallest change | Two representations of "no tenant" in one codebase |

### Chosen Option: **B — A reserved default tenant** (recommended, pending the maintainer)

### Rationale

- A wins if the maintainer prefers to keep shipped shapes and accepts sentinel handling per store; B wins when keys and sequences must be tenant-keyed without special cases, which REQ-061 demands for sequences, chains, key stores and credential stores.
- The sibling plans are agnostic: they call `scope.TenantId`, and their `COALESCE` index expressions are harmless under B. Under B, `TenantScope.TenantId` is never null.

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
- `ITenantConfigurationResolver<TConfig>.ResolveAsync(ct)` returns `Left(encina.tenancy.missing_tenant_config)` when the ambient tenant has no configuration, never another tenant's and never a default (comment of 2026-10-17 on this issue).

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
<summary><strong>5. Keys: the tenant is part of the key identity and an explicit parameter of the key ports</strong></summary>

**Class:** Trivial
**Settled by:** `OperationAuditQuery.TenantId` and `DeadLetterFilter` (tenant passed as a value to the store), SPEC-002 REQ-061 ("one tenant's key material never decrypts another tenant's data, and shredding in one tenant never affects another"), and `ISubjectKeyProvider` (`src/Encina.Marten.GDPR/Abstractions/ISubjectKeyProvider.cs`) which is called from event serialization where no request exists.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Add `string tenantId` to every method of `ISubjectKeyProvider` and `IKeyProvider`; key documents get a tenant field and the key id becomes `{tenant}:{subject}`; callers pass the tenant from the scope or from the event metadata** | Works in serializers and background replay, where the ambient context may be absent; provider classes stay free of request context | Breaking change of two ports (pre-1.0) |
| **B) Providers read the ambient tenant** | No signature change | Event replay and projections have no ambient tenant; a silent wrong tenant is possible |

### Chosen Option: **A — Explicit tenant parameter**

### Rationale

- Crypto-shredding a subject in tenant A deletes the keys of `(A, subject)` only; the same subject id in tenant B stays readable (AC-043).

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

**Class:** Settled
**Settled by:** ADR-054 D4 (the tenant is an attribute and a metric tag only when multi-tenancy is on), settled point 4 (lowercase dot-separated instrument names, no `_total`), SPEC-002 REQ-062 and AC-044, and the names proposed in this issue (`encina.tenancy.missing_tenant_config`, span `encina.tenancy.resolve` with attribute `encina.tenant_id`).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Use the issue's names; the attribute key lives in the constants class of #2676 once it exists** | Consistent with the family | #2676 may name the key differently; one rename |
| **B) Invent a key per package** | Independent | The inconsistency #2676 exists to remove |

### Chosen Option: **A — The issue's names**

### Rationale

- The metric counts refused operations; it carries no tenant value as a tag (cardinality, ADR-054 consequences).

</details>

---

## Implementation Phases

### Phase 1: Core Abstractions & Models (PR 1257-A)

> **Goal**: the shared tenant-scope primitive and the default-tenant rule.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create `src/Encina/Abstractions/ITenantScopeResolver.cs`**, **`TenantScope.cs`**, **`TenantScopeErrors.cs`** (codes `encina.tenancy.tenant_required`, `encina.tenancy.tenant_mismatch`; fixed messages).
2. **Create `TenantCacheKey.cs`**: `static string Segment(string? tenantId)` (percent-encodes the separator so a tenant id cannot alias another; `_` for none) used by every cache-key builder of the family.
3. **Create `DefaultTenant.cs`**: `public const string Id = "default"` (only if Design Choice 2 is B).
4. **Modify `src/Encina.Tenancy/Configuration/TenancyOptions.cs` validation** (or the tenancy options validator): reject the reserved default id, blank ids and ids with leading or trailing whitespace.
5. PublicAPI lines (mechanical-fixer).

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

### Phase 2: Default Implementation & Registration (PR 1257-A)

> **Goal**: the resolver implementation, registered once.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create `src/Encina/Core/TenantScopeResolver.cs`** (`internal sealed`): constructor `(IRequestContextAccessor accessor, IServiceProviderIsService isService)`; `IsMultiTenant` computed once from `IsService(typeof(MultiTenancyMarker))`; `ResolveRead(requested)`: ambient wins; a requested tenant different from the ambient one is `tenant_mismatch`; no ambient in a multi-tenant application is `tenant_required`; single-tenant returns `TenantScope(default or null, false)` per Design Choice 2. `ResolveWrite(explicit)`: ambient default; explicit different from ambient is `tenant_mismatch`; multi-tenant with neither is `tenant_required`.
2. **Modify the core registration** (`AddEncina`): `TryAddSingleton<ITenantScopeResolver, TenantScopeResolver>()`.
3. DI test with `ValidateOnBuild` and `ValidateScopes`; the marker registered before and after.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
- Phase 1 merged. The accessor is a singleton backed by AsyncLocal; the marker is a singleton registered by AddEncinaTenancy.

TASK:
Implement and register TenantScopeResolver; unit tests for the six cases (single-tenant, multi-tenant with ambient, with matching request, with mismatching request, without any, whitespace tenant).

KEY RULES:
- Registration completeness (AGENTS.md section 3): the resolver sees the marker in any registration order, because it checks the container lazily at first use.
- CRAP <= 10 per method.

REFERENCE FILES:
- src/Encina.Messaging/DeadLetter/DeadLetterManager.cs (AmbientTenantId)
- src/Encina.Tenancy/ServiceCollectionExtensions.cs (marker registration at line 109)
```

</details>

### Phase 3: Fail-Closed Tenant Configuration (PR 1257-B)

> **Goal**: one lookup contract for per-tenant configuration and secrets.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create `src/Encina/Abstractions/ITenantConfigurationResolver.cs`**: `ITenantConfigurationResolver<TConfig>` with `ValueTask<Either<EncinaError, TConfig>> ResolveAsync(CancellationToken ct)`; **`TenantSecretRef.cs`** (`sealed record (string Provider, string Name)`, `ToString()` prints the provider and not the name).
2. **Create `src/Encina.Tenancy/Configuration/TenantConfigurationResolver.cs`**: the default implementation backed by a `ITenantConfigurationSource<TConfig>` (static per-tenant options map for development and tests, `Tenants[tenantId]`); resolves the tenant with `ITenantScopeResolver`; a missing entry returns `encina.tenancy.missing_tenant_config`.
3. **Errors**: `TenantConfigurationErrors.Missing()` with a fixed message.
4. **Diagnostics**: counter `encina.tenancy.missing_tenant_config` (`Counter<long>`, tag `capability`), activity `encina.tenancy.resolve` (attribute `encina.tenant_id` when multi-tenancy is on) in `TenancyActivitySource`/`TenancyMetrics`.
5. **DI**: `AddEncinaTenantConfiguration<TConfig>(Action<...>)` in `Encina.Tenancy`.
6. The secret-provider-backed source (Design Choice 3A) is added by the first capability that needs a secret (OAuth, P-32); its contract test is already part of this PR (a fake secret provider).

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

### Phase 4: Tenant-Scoped Keys (PR 1257-C)

> **Goal**: subject keys and column-encryption keys never cross tenants.

<details>
<summary><strong>Tasks</strong></summary>

1. **`src/Encina.Marten.GDPR/Abstractions/ISubjectKeyProvider.cs`**: add `string tenantId` to `GetOrCreateSubjectKeyAsync`, `GetSubjectKeyAsync`, `DeleteSubjectKeysAsync`, `IsSubjectForgottenAsync`, `RotateSubjectKeyAsync`, `GetSubjectInfoAsync`.
2. **`KeyStore/SubjectKeyDocument.cs`**: `TenantId` property; `Id` = `{tenant}:{subject}:{version}` style key (keep the current shape, prefixed by the tenant); **`PostgreSqlSubjectKeyProvider.cs`**, **`InMemorySubjectKeyProvider.cs`**, **`SubjectForgottenMarker.cs`** (the forgotten marker is per tenant), `SubjectKeyInfo`, `SubjectEncryptionInfo` and the event types `SubjectForgottenEvent`/`SubjectKeyRotatedEvent` carry the tenant.
3. **Callers** (`DefaultForgottenSubjectHandler`, the encryption serializer): pass the tenant from `TenantScope` or from the event metadata.
4. **`src/Encina.Security.Encryption/Abstractions/IKeyProvider.cs`**: tenant parameter on `GetKeyAsync`, `GetCurrentKeyIdAsync`, `RotateKeyAsync`; `InMemoryKeyProvider.cs` keyed by `(tenant, keyId)`.
5. Tests: key of tenant A cannot decrypt tenant B's data; shredding `(A, s)` leaves `(B, s)` readable; the in-memory and PostgreSQL (Testcontainers) providers.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT:
- Design Choice 5 (explicit tenant parameter, key identity includes the tenant). Marten.GDPR keys are stored by PostgreSqlSubjectKeyProvider; event serialization has no ambient request.

TASK:
Change the two key ports and their implementations and callers; update the existing tests; add the two-tenant key tests.

KEY RULES:
- Key material never appears in logs or exceptions.
- Event-sourced compliance modules use Marten on PostgreSQL via Testcontainers for integration tests; unit tests mock IAggregateRepository (AGENTS.md section 3).
- Breaking changes are made completely: no overloads kept for the old signatures.

REFERENCE FILES:
- src/Encina.Marten.GDPR/Abstractions/ISubjectKeyProvider.cs
- src/Encina.Marten.GDPR/KeyStore/PostgreSqlSubjectKeyProvider.cs
- src/Encina.Security.Encryption/Abstractions/IKeyProvider.cs
```

</details>

### Phase 5: Configuration & DI

> **Goal**: opt-in registration and start-up checks.

<details>
<summary><strong>Tasks</strong></summary>

1. With tenancy off nothing needs configuring (Design Choice 2); with `AddEncinaTenancy` the primitive reports multi-tenant automatically.
2. A start-up check in `Encina.Tenancy` fails the host when a tenant-keyed capability is registered with `ITenantConfigurationSource<T>` empty in a multi-tenant application.
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

### Phase 6: The Two-Tenant Isolation Suite on 10 Providers and Marten (PR 1257-D and the children)

> **Goal**: AC-043 as an executable, registry-driven suite.

<details>
<summary><strong>Tasks</strong></summary>

1. **`tests/Encina.TestInfrastructure/TenantIsolation/`**: `ITenantIsolationStoreDescriptor` (`Name`, `WriteAsync(tenant, key)`, `QueryAsync(tenant)`, `ExportAsync(tenant)`, `RunBackgroundCycleAsync(tenant)`, `CountAsync(tenant)`), `TenantIsolationStoreRegistry`, and the abstract bases `TenantIsolationQueryTestsBase`, `TenantIsolationBackgroundCycleTestsBase`, `TenantIsolationDefaultTenantTestsBase` (tenancy off, no tenant configuration).
2. **Concrete classes per provider collection**: `ADO-SqlServer`, `ADO-PostgreSQL`, `ADO-MySQL`, `Dapper-*` (3), `EFCore-*` (3), MongoDB, and Marten on PostgreSQL; each in the existing `[Collection("<Family>-<Database>")]` and `ClearAllDataAsync` in `InitializeAsync`.
3. **Architecture test** `tests/Encina.UnitTests/Testing/Architecture/TenantIsolationCoverageTests.cs`: every type in the closed list of SPEC-002 store interfaces (audit, read audit, policy, consent, DSR, retention, lawful basis, outbox, inbox, saga, scheduled message, dead letter, subject keys, ...) has a descriptor; the list is extended by the child that adds a store.
4. **Start with the stores that exist and are already tenant-keyed** (consent, DSR, retention, dead letters, audit after #798); the rest join as their issues merge (#737, #738, #739, #760, #798, #596).
5. **S11**: the scenario test of SPEC-002 section 4.3 runs the descriptors for two tenants in one deployment.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```text
CONTEXT:
- Shared integration fixtures: SqlServerFixture, PostgreSqlFixture, MySqlFixture and the EFCore variants; collections defined in Collections.cs files; never IClassFixture for database fixtures.

TASK:
Create the harness, the registry, the three abstract bases, the concrete classes for the stores that already exist, and the architecture test. Do not duplicate assertions per provider.

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

1. Register the new `TenantIsolation` range `TenantIsolation = (7420, 7469)` in `src/Encina/Diagnostics/EventIdRanges.cs` first, and add the `Encina` assembly entry to `AssemblyRanges` in `EncinaEventIdAllocationTests`.
2. `[LoggerMessage]` ids, the first 10 of that range: tenant required, tenant mismatch, missing tenant configuration, default tenant used (debug), invalid tenant id; codes only.
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

**Per-file targets** (proposed; measured values replace them, never lower):

| File | Flags | Justification |
|------|-------|---------------|
| `Encina/Core/TenantScopeResolver.cs` (new) | unit 100, guard 90, contract 95 | six paths, all unit-testable |
| `Encina/Abstractions/TenantScope.cs`, `TenantScopeErrors.cs`, `TenantCacheKey.cs` (new) | unit 100 | pure static and record code |
| `Encina.Tenancy/Configuration/TenantConfigurationResolver.cs` (new) | unit 98, guard 90, contract 90 | missing and present paths |
| `Encina.Marten.GDPR/KeyStore/PostgreSqlSubjectKeyProvider.cs` | unit 80, integration 80 | integration reaches the SQL on PostgreSQL |
| `Encina.Marten.GDPR/KeyStore/InMemorySubjectKeyProvider.cs` | unit 95 | dictionary logic |
| `Encina.Security.Encryption/InMemoryKeyProvider.cs` | unit 95 | dictionary logic |
| `Encina.Tenancy/ServiceCollectionExtensions.cs` (existing unit 95, guard 70) | unit 95, guard 70 | one registration |

CRAP gate: methods stay at or below 10; the six resolver cases are separate small methods.

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

1. ADR in `docs/architecture/adr/`: "Tenant scope primitive, default tenant and fail-closed tenant configuration" once Design Choices 1 to 3 are answered.
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
| ISubjectKeyProvider, PostgreSqlSubjectKeyProvider, SubjectKeyDocument | src/Encina.Marten.GDPR/Abstractions/ISubjectKeyProvider.cs, src/Encina.Marten.GDPR/KeyStore/ | subject keys keyed by SubjectId only today |
| IKeyProvider, InMemoryKeyProvider | src/Encina.Security.Encryption/Abstractions/IKeyProvider.cs, src/Encina.Security.Encryption/InMemoryKeyProvider.cs | column-encryption keys keyed by key id only today |
| Secret providers (Azure Key Vault, AWS Secrets Manager, HashiCorp Vault, Google Cloud Secret Manager) | src/Encina.Security.Secrets* | hold tenant credential values; tenants keep references |
| Outbox, inbox, saga and scheduled message interfaces (no TenantId yet) | src/Encina.Messaging/Outbox/IOutboxMessage.cs, Inbox/IInboxMessage.cs, Sagas/ISagaState.cs, Scheduling/IScheduledMessage.cs | tenant columns are #737, #738, #739, #760 |
| Shared integration fixtures and collections | tests/Encina.TestInfrastructure | the two-tenant suite base classes live here |
| Existing tenant tests as templates | tests/Encina.IntegrationTests/Compliance/Consent/ConsentTenantScopingIntegrationTests.cs, tests/Encina.ContractTests/Compliance/DataSubjectRights/DSRTenantIsolationContractTests.cs, tests/Encina.PropertyTests/Compliance/DataSubjectRights/DSRTenantIsolationPropertyTests.cs | structure of the new suite |
| EventIdRanges and EncinaEventIdAllocationTests | src/Encina/Diagnostics/EventIdRanges.cs, tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs | the new range is registered here |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina` (tenant isolation primitives) | 7420-7469 (new `TenantIsolation`) | register in `EventIdRanges.cs` first; this plan uses the first 10 |
| `Encina.Tenancy` | none new | 1805-1809 are reserved by the #752 plan inside `Tenancy = (1800, 1899)`; the tenancy diagnostics of this plan are a counter and a span, its log messages use the new `TenantIsolation` range |

### Estimated File Count

| Category | Files | Notes |
| :--- | :--- | :--- |
| Production, tenant scope primitive (Encina core and Encina.Tenancy validator) | 9 | PR 1257-A |
| Production, fail-closed tenant configuration (Encina core and Encina.Tenancy) | 8 | PR 1257-B |
| Production, subject and column-encryption keys (Marten.GDPR, Security.Encryption) | 11 | PR 1257-C |
| Production, diagnostics (health, metrics, spans, log messages, EventIdRanges) | 6 | spread over A and B |
| Tests, isolation suite harness and architecture test (TestInfrastructure) | 8 | PR 1257-D |
| Tests, per-store two-tenant classes (10 providers plus Marten, per store family) | 33 | added by each child feature, counted here for the suite |
| Tests, unit, guard, contract, property, load for the primitives | 24 | with A, B, C |
| Documentation (ADR, guide, READMEs, changelog fragments, PublicAPI, manifests) | 10 | docs-writer and mechanical-fixer |
| Total | 109 | seven worker pull requests plus the per-store classes of the children |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Combined prompt — all phases</strong></summary>

```text
PROJECT CONTEXT:
- Encina, .NET 10 / C# 14, pre-1.0, ROP (Either<EncinaError, T>). Issue #1257 (SPEC-002 P-54, REQ-061, AC-043): the shared tenant-scope primitive, fail-closed tenant configuration, tenant-scoped keys and the two-tenant isolation suite on 10 database providers and Marten.
- ITenantContext and a TenantId type do not exist. The tenant is IRequestContext.TenantId via IRequestContextAccessor; multi-tenancy is on when MultiTenancyMarker is registered; the default tenant rule is Design Choice 2.

IMPLEMENTATION OVERVIEW:
1. Core: ITenantScopeResolver, TenantScope, TenantScopeErrors, TenantCacheKey, DefaultTenant; Tenancy validator for the reserved id.
2. TenantScopeResolver registered with TryAddSingleton; six unit cases.
3. ITenantConfigurationResolver<TConfig>, TenantSecretRef, default source, counter encina.tenancy.missing_tenant_config, span encina.tenancy.resolve.
4. Explicit tenant parameter on ISubjectKeyProvider and IKeyProvider; tenant in key identity; per-tenant shredding.
5. DI tests in both registration orders; start-up check.
6. Isolation suite: descriptors, registry, abstract bases, per-provider classes, architecture test; S11.
7. Cache-key architecture test; verification of #1164, #596, #752.
8. EventId range 7420-7469 registered first; log messages; AC-044 exporter test.
9. Tests of 7 types; manifests via mechanical-fixer. 10. ADR, guide, READMEs, fragments via specialists.

KEY PATTERNS:
- Ambient tenant wins; multi-tenant with no tenant is denied; blank tenant is no tenant; padded ids rejected.
- Missing tenant configuration fails closed, never a fallback; secrets are references, never values.
- Tenant passed as a value to stores and key providers; no ambient reads inside provider classes.
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
| 12 | Audit Trail | ⏭️ | Tenant configuration changes are audited by #752 (plan `config-change-audit-implementation-plan-752.md`). |

---

## Prerequisites & Dependencies

**AC-043 tracker** (what must be closed for the acceptance, who owns it):

| Requirement of AC-043 | Owner | State |
|-----------------------|-------|-------|
| Tenant columns and filters of outbox, inbox, saga, scheduled messages | #737, #738, #739, #760 | open |
| Audit and ABAC filtering | #798 (plan `audit-abac-multitenancy-implementation-plan-798.md`) | open, plan |
| Persistent tenant store and its health check | #596 | open |
| Background cycles restore the tenant | #1164 | open |
| Tenant tag helper | #2676 | open |
| Tenant-scoped subject and column keys, fail-closed configuration, default tenant, the suite | this plan | planned |
| Sequences, chains, credentials, senders, storage, pacing | P-32, P-31, P-44, P-33, P-36, P-37, P-38, #731, #734, #469 | not implemented; each uses the contracts of this plan |
| AI Act per-tenant behavior | #845 and #847 | open, plans |

**Sequencing**: PR 1257-A first (the sibling plans' hard prerequisite); B and C are independent of each other; D follows A and grows as the children land. The shared files of #798, #845, #752 and #1636 are listed in their plans.

---

## Next Steps

1. Maintainer answers Design Choices 1, 2 and 3 when work on the issue starts (recorded in `## Maintainer Decisions`); the answers also fix the sibling plans (`ITenantScopeResolver`, the default tenant).
2. Worker pull requests, each at most 15 files and 1500 lines:
   - **PR 1257-A**: Phases 1, 2 and 8 (primitive, resolver, range, log messages, DI) with unit, guard and contract tests: about 15 files.
   - **PR 1257-B**: Phase 3 (configuration resolver, secret reference, errors, counter, span) and its tests: about 15 files.
   - **PR 1257-C1** (Marten.GDPR subject keys) and **PR 1257-C2** (Security.Encryption keys): Phase 4, 14 and 8 files.
   - **PR 1257-D**: Phase 6 harness, registry, bases, architecture test and the concrete classes for existing stores: about 15 files; further classes land with each child.
   - **PR 1257-E**: Phase 9 load and property tests, Phase 7 cache-key test, Phase 10 documentation and ADR.
3. Link this plan from #1257 and from the EPIC #1186.
