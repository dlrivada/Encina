# Implementation Plan: `Encina.Security.BreakTheGlass` — Emergency Access with Justification, Time Box and Review

> **Issue**: [#1244](https://github.com/dlrivada/Encina/issues/1244) (SPEC-002 tracking id **P-42**, priority P0)
> **Type**: Feature
> **Complexity**: High (9 phases, 10 database providers, ~80 files)
> **Estimated Scope**: ~2,300-2,900 lines of production code + ~2,600-3,300 lines of tests
> **Milestone**: v0.17.0 — Compliance Lifecycle (SPEC-002 §13.2)
> **Parent EPIC**: [#1186](https://github.com/dlrivada/Encina/issues/1186) — EU regulatory readiness (SPEC-002)
> **Specification**: [SPEC-002](../specifications/SPEC-002-eu-regulatory-readiness.md) REQ-051 (line 373), AC-039 (line 490: "break-the-glass access without a justification is refused and one with a justification is audited and expires"); cross-cutting REQ-061 / AC-043 (tenancy) and REQ-062 / AC-044 (instrumentation); research note F, gap G19 (`docs/specifications/research/SPEC-002/F-health-practice.md:197,239`)
> **Depends on**: [#1705](https://github.com/dlrivada/Encina/issues/1705) (one request identity; open, phase 3 of 7 merged), [#1193](https://github.com/dlrivada/Encina/issues/1193) (P-05 evidential read audit; open)
> **Related**: [#751](https://github.com/dlrivada/Encina/issues/751) (ABAC decision audit; open), [#1189](https://github.com/dlrivada/Encina/issues/1189) (P-03 blocked data, the analogous "scoped audited disclosure"), [#798](https://github.com/dlrivada/Encina/issues/798) (tenant filtering for Security.Audit), [#1678](https://github.com/dlrivada/Encina/issues/1678) (named pipeline stages)

---

## Summary

A clinician who is not the treating professional sometimes needs a patient's record now: the patient arrives unconscious, the treating psychologist is away, continuity of care cannot wait. Ley 41/2002 art. 16 and the Código Deontológico del Psicólogo art. 46 restrict access to the treating professional (SPEC-002 research R16, R22), and EHDS Art. 8 describes a controlled override. This plan adds that override to Encina: a user without the normal permission opens access to **one data subject**, for **one named scope**, with a **mandatory justification**, for a **capped duration**. Every access under the elevation is audited with the elevation id, the elevation stops working when it expires, and every elevation waits in a review queue until a second person rules it justified or unjustified.

**State of the code (verified on `5b485b12`).** Nothing of this exists. A search of `src/` for `break.?glass`, `BreakGlass`, `emergency access` and `Elevation` finds only the unrelated `PrivilegeEscalationRule` of BreachNotification (`src/Encina.Compliance.BreachNotification/Detection/Rules/PrivilegeEscalationRule.cs:17`) and the `RequireElevation` policy name used as an example in `src/Encina.AspNetCore/AuthorizationPipelineBehavior.cs:26,58`; SPEC-002 research row C25 records the same ("There is no break-glass concept (grep, verified)"). The building blocks it will use do exist:

| Building block | Where | State |
|---|---|---|
| Request identity carried by the context | `src/Encina/Abstractions/IRequestContext.cs:87` (`RequestIdentity Identity`), `src/Encina/Identity/RequestIdentity.cs:47` (`Kind`, `UserId`, `Roles`, `Permissions`, `IsAuthenticated`) | Merged with #1705 phases 1-3 |
| ASP.NET Core gate reading the identity | `src/Encina.AspNetCore/AuthorizationPipelineBehavior.cs:130-163` (`Handle` → `AuthorizeAsync` → `EvaluateAsync`, denial built at `:256-320`) | Reads `IRequestContext.Identity` today |
| Attribute gate | `src/Encina.Security/SecurityPipelineBehavior.cs:80-178` (attribute loop, denial returned at `:169-173`) | Still reads `ISecurityContextAccessor` (`:51,118`); moves to `IRequestContext.Identity` in #1705 phase 4 |
| ABAC gate | `src/Encina.Security.ABAC/ABACPipelineBehavior.cs:113-197` (`Handle`), `HandleDenyAsync` `:285-330` (definite deny → `ApplyEnforcementAsync` at `:329`), `HandleIndeterminate` `:338-358` | Still reads `ISecurityContextAccessor` (`:72,90,365-374`); moves in #1705 phase 4 |
| Operation audit (general write sink, ADR-036 decision 2) | `src/Encina.Security.Audit/Abstractions/IOperationAuditStore.cs:32-171`, `OperationAuditEntry.cs:50-252` (`Action`, `EntityType`, `EntityId`, `Outcome`, `TenantId`, `Metadata`) | On the 10 providers (`*/Auditing/OperationAuditStore{ADO,Dapper,EF,MongoDB}.cs`), Marten and InMemory |
| Read audit with purpose | `src/Encina.Security.Audit/Abstractions/IReadAuditContext.cs:38-65` (`Purpose`, `WithPurpose`), `ReadAuditEntry.cs:52-177` (`Purpose`, `Metadata`) | Fire-and-forget today (`AuditedReadOnlyRepository.cs:92,253`); made evidential by #1193 |
| Subject id extraction pattern | `src/Encina.Compliance.DataSubjectRights/Abstractions/IDataSubjectIdExtractor.cs:45-65`, internal `SubjectIdConversion` in DSR and Consent | Reused as a pattern; #1193 OD-3 extracts a shared converter into core |
| HTTP mapping of authorization codes | `src/Encina.AspNetCore/ProblemDetailsExtensions.cs:155`, `src/Encina.AwsLambda/ApiGatewayResponseExtensions.cs:250`, `src/Encina.AzureFunctions/HttpResponseDataExtensions.cs:218` (`encina.authorization.*` → 401/403, #1919) | Merged |

**What this plan delivers**

1. A core contract `IAccessElevationGate` that the three gates consult only on a **definite** denial; a no-op default keeps the cost at zero when the feature is off.
2. A new package `Encina.Security.BreakTheGlass` with `IBreakTheGlassService` (open, revoke, review, review queue), the `[BreakTheGlassEligible]` request attribute, the gate implementation, options with validation, the expiry sweeper and observability.
3. An `IBreakTheGlassStore` on the 10 database providers plus an InMemory store for development and tests.
4. Every open, access, expiry, revocation and review written to `IOperationAuditStore` write-ahead and fail-closed; every read under an elevation read-audited with the purpose `break-the-glass` and the elevation id in the entry metadata.

**Standards**: GDPR Art. 5(2), 9(2)(c) and (h), 32; EHDS Reg. (EU) 2025/327 Art. 8; Ley 41/2002 art. 16; Código Deontológico del Psicólogo art. 46; ISO/IEC 27002:2022 controls 5.18, 8.2 and 8.15; HL7 v3 purpose-of-use `BTG`.

**Affected packages**: `Encina` (core contract), new `Encina.Security.BreakTheGlass`, `Encina.Security`, `Encina.Security.ABAC`, `Encina.AspNetCore` (gate integration), `Encina.Security.Audit` (read-audit context metadata), `Encina.Messaging` and `Encina.MongoDB` (store flags), `Encina.ADO.{SqlServer,PostgreSQL,MySQL}`, `Encina.Dapper.{SqlServer,PostgreSQL,MySQL}`, `Encina.EntityFrameworkCore`, `Encina.MongoDB` (stores).

**Provider category**: Database (10) for `IBreakTheGlassStore` under the recommended Design Choice 2; none for the gate itself. The issue body says "no provider-specific code"; that holds only if the maintainer picks the audit-derived option of Design Choice 2, which this plan does not recommend (see its Rationale).

---

## Design Choices

<details>
<summary><strong>1. Package Placement — new <code>Encina.Security.BreakTheGlass</code> package, gate contract in core</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) New `Encina.Security.BreakTheGlass` package; a small `IAccessElevationGate` contract plus a no-op default in core `Encina`** | The three gates (in `Encina.Security`, `Encina.Security.ABAC`, `Encina.AspNetCore`) consult one contract without new project references; pay-for-what-you-use (no package, no cost); own EventId range, options, README | One more package; a core type that only one package implements |
| **B) Everything in `Encina.Security.Audit`** (the issue's "Affected packages") | No new package; the audit stores are next door | `Encina.Security` references only core (`src/Encina.Security/Encina.Security.csproj`), so `SecurityPipelineBehavior` cannot see the contract; mixes access control into an audit package whose stores ADR-036 scoped to three audit purposes |
| **C) Everything in `Encina.Security`** | Lives with the attribute gate | `Encina.AspNetCore` does not reference `Encina.Security` and would gain the dependency; bloats the base security package with a store, a sweeper and provider flags |

### Chosen Option: **A — New `Encina.Security.BreakTheGlass` package, gate contract in core** (recommended, pending the maintainer)

### Rationale

- We recommend A because it is the only option in which all three gates can honour an elevation without reshaping the package graph: `Encina.Security.ABAC` references `Encina.Security` and `Encina.Security.Audit`, `Encina.AspNetCore` references `Encina.Security.Audit` but not `Encina.Security`, and `Encina.Security` references only core (csproj files read on `5b485b12`).
- The core contract is three types in `src/Encina/Authorization/`: `IAccessElevationGate`, `AccessElevation` (the grant a gate receives) and an internal `NoAccessElevationGate` registered with `TryAddSingleton` by `AddEncina`, so an application without the package never pays more than one interface call on a path that already denies.
- The package references `Encina.Security.Audit` (operation and read audit) and core; provider packages reference it for the store interface, as they reference `Encina.Security.Audit` for the audit stores today.

</details>

<details>
<summary><strong>2. Elevation Persistence — <code>IBreakTheGlassStore</code> on the 10 database providers</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A dedicated `IBreakTheGlassStore` on the 10 database providers plus InMemory (`BreakTheGlassStore{ADO,Dapper,EF,MongoDB}`, table `BreakTheGlassElevations`)** | Indexed lookups for the two hot questions (active elevation for user + subject + scope; unreviewed queue); conditional status updates make expiry safe on several hosts without a lock; works on every provider an application uses | 10 store implementations, scripts and integration suites; a new table |
| **B) Event-sourced `BreakTheGlassAggregate` on Marten (ADR-019 pattern, like `ConsentAggregate`)** | Full history by construction; matches the compliance modules; one implementation | Marten (PostgreSQL) becomes mandatory for break-glass; ADR-019 scopes Marten to compliance modules, and break-glass is an access-control mechanism; no InMemory store (AGENTS.md §3), so every unit test mocks `IAggregateRepository` |
| **C) No store: elevations are operation-audit entries, the active check and the queue query `IOperationAuditStore`** (what the issue's "no provider-specific code" implies) | No new table or store; the evidence is the state | `OperationAuditQuery` (`src/Encina.Security.Audit/OperationAuditQuery.cs:33-161`) has no subject or scope filter and no "not yet reviewed" anti-join, so the queue becomes an in-memory join over a date window; operation-audit retention can purge an open elevation; status changes become appends that every reader must fold |

### Chosen Option: **A — `IBreakTheGlassStore` on the 10 database providers** (recommended, pending the maintainer)

### Rationale

- We recommend A because the gate needs a single indexed answer ("is there an active elevation for this tenant, user, subject and scope at this instant?") and the sweeper needs a conditional update ("close it if it is still active"); neither maps onto the audit query model (option C) without new query members on the 10 audit stores anyway.
- History is not lost: the store holds the current state, and the operation audit holds every transition (Design Choice 5), so option A keeps the accountability that option B offers without making Marten a prerequisite for a P0 capability.
- AGENTS.md §5 then requires the store on all 10 providers with integration tests on each; the Marten audit store keeps receiving the audit entries, so an application whose audit runs on Marten still has its evidence there.
- If the maintainer picks C, the provider phase disappears, a health check becomes not applicable, and `OperationAuditQuery` gains `EntityId`-prefix and metadata filters instead (a change on the 10 audit stores and Marten).

</details>

<details>
<summary><strong>3. How the Gates Honour an Elevation — consult on a definite denial, inside each gate</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Each gate (`AuthorizationPipelineBehavior`, `SecurityPipelineBehavior`, `ABACPipelineBehavior`) calls `IAccessElevationGate.TryElevateAsync` only when it is about to return a definite denial; a grant lets the request continue** | Works whatever the behavior order is (no dependency on #1678); never touches the allow path; unauthenticated, indeterminate and error outcomes are never overridable because the gates never offer them | Three gates change; each needs a test that only definite denials are offered |
| **B) A `BreakTheGlassPipelineBehavior` placed before the gates resolves the elevation and stamps the context; the gates read the stamp** | One lookup per request; gates do not call a service | Depends on behavior order (#1678 open); a lookup on every eligible request, also when the gate would have allowed; context metadata is application-writable, so the stamp must be an unforgeable internal type |
| **C) ABAC only: an attribute provider contributes `break_glass.active`; policies decide** | XACML-native (break-glass as a policy with obligations) | Applications on `[Authorize]` or the Security attributes get nothing; whether an elevation permits depends on every policy author remembering the rule |

### Chosen Option: **A — Consult on a definite denial, inside each gate** (recommended, pending the maintainer)

### Rationale

- We recommend A because an override must be exactly as narrow as the denial it overrides: each gate already knows whether its result is a definite deny (`AuthorizationPipelineBehavior` policy or role failure at `:241-320`; `SecurityPipelineBehavior` attribute failure at `:169-173`; ABAC `Effect.Deny` in `HandleDenyAsync` `:285-330`) or something else (no identity, `Indeterminate`, an exception, a missing context), and only the first may be offered to the gate.
- Overridable denials are listed in the contract: `encina.authorization.forbidden`, `encina.authorization.policy_failed`, `encina.authorization.resource_denied`, the Security codes `InsufficientRoles`, `PermissionDenied`, `ClaimMissing`, `NotOwner`, and the ABAC codes `abac.access_denied` and `abac.condition_not_met` (`src/Encina.Security.ABAC/ABACErrors.cs`). `encina.authorization.unauthenticated`, `abac.missing_context` (renamed `abac.unauthenticated_caller` by #1705), `abac.indeterminate`, `abac.evaluation_failed`, `abac.obligation_failed` and `abac.policy_not_found` (a missing required policy is a configuration error, not a verdict about this caller) are never overridable; the gate implementation rejects them again (defence in depth).
- When several gates deny the same request, the first grant is kept in a scoped holder keyed by the correlation id, so the access is audited once (Design Choice 5) and the later gates reuse the grant.
- The ABAC OnDeny obligations still run before the gate is consulted (XACML §7.18 semantics unchanged); when #751 lands, its decision recorder records the Deny with the elevation id in its metadata.

</details>

<details>
<summary><strong>4. Elevation Binding — one subject, one named scope, declared by the request type</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) An elevation binds tenant + user + data subject + named scope; a request type opts in with `[BreakTheGlassEligible("clinical-record", SubjectIdProperty = nameof(PatientId))]`** | Narrowest grant that serves the use case; administrative or bulk request types can never be opened because they are not marked; the subject comes from the request, not from the caller | Each eligible request type must carry the attribute and expose the subject id |
| **B) User + scope, no subject** | No subject extraction | Opens every patient of the scope: contradicts art. 46 and "access to a subject's data" in REQ-051 |
| **C) User + subject, every request type eligible** | No attribute | Opens deletes, exports and administrative commands for that subject |

### Chosen Option: **A — One subject, one named scope, declared by the request type** (recommended, pending the maintainer)

### Rationale

- We recommend A because it matches the issue's API (`OpenAsync(subjectId, scope, justification, duration)`) and the reference scenario: one patient, the clinical record, for a short time.
- The subject id is read from the property named by `SubjectIdProperty` through a compiled accessor cached per request type, converted with the same rules as the DSR extractor (`IDataSubjectIdExtractor.cs:34-65`: string, `Guid`, strongly typed ids; a missing property or an unconvertible type throws at first use, never silently matches). When #1193 OD-3 has extracted the shared `IDataSubjectIdConverter` into core, the gate uses it; otherwise the package carries an internal copy and the follow-up replaces it.
- Scopes are free-form, case-sensitive names checked against `BreakTheGlassOptions.Scopes` (a request type marked with an undeclared scope fails at start-up).
- The store keeps the subject id in clear, as the consent read model does (`src/Encina.Compliance.Consent/ReadModels/ConsentReadModel.cs:39`), because the reviewer must see whose data was opened; it never reaches logs, activity tags or metric tags (REQ-062).

</details>

<details>
<summary><strong>5. Audit of Each Access — write-ahead operation audit plus read-audit purpose and metadata</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Write-ahead `OperationAuditEntry` per elevated request (`Action = "break_glass.access"`, `EntityType = "BreakTheGlassElevation"`, `EntityId = elevation id`), fail-closed; plus `IReadAuditContext` set to purpose `break-the-glass` with the elevation id in the read-audit entry metadata** | One query (`GetByEntityAsync("BreakTheGlassElevation", id)`) returns the whole history of an elevation; reads carry the link without a schema change (`Metadata` is already persisted by the 10 read-audit stores, e.g. `ReadAuditStoreADO.cs:55`); evidence even for requests that read nothing | `IReadAuditContext` gains a metadata member; depends on #1193 for the read side to be evidential |
| **B) A new `ElevationId` column on `ReadAuditEntry` on the 10 providers and Marten; no operation audit per access** | Typed, indexable link | Schema change on 11 stores; requests that write rather than read leave no trace |
| **C) Only a log line and a metric per access** | Cheapest | Not evidence (AC-039 asks for an audited access) |

### Chosen Option: **A — Write-ahead operation audit plus read-audit purpose and metadata** (recommended, pending the maintainer)

### Rationale

- We recommend A because REQ-051 asks that "each access under it is read-audited with the elevation id" and the issue's matrix asks that "open, each access, expiry and review are audited": the operation entry covers every elevated request (including commands), the read entries cover what was read.
- The access entry is written **before** the gate lets the request continue; a `Left` from the store denies the request with `break_glass.audit_failed` (AGENTS.md §3, compliance gates fail closed). The open entry is written before the store row (write-ahead): an audit entry for an elevation that then failed to persist over-reports, never under-reports.
- `IReadAuditContext` gains `WithMetadata(string key, string value)` and `Metadata`; the read-audit decorator of #1193 copies it into `ReadAuditEntry.Metadata`. The gate sets the purpose to `break-the-glass` and keeps any purpose the application declared under the metadata key `declared_purpose`.
- The justification is free text that may contain health data: it is stored on the elevation record (the reviewer needs it) and never copied into audit metadata, logs or telemetry.

</details>

<details>
<summary><strong>6. Expiry and Review — enforced at check time, closed by an idempotent sweeper, reviewed by a second person</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The gate compares `ExpiresAtUtc` with `TimeProvider` on every check; a hosted `BreakTheGlassExpiryService` closes expired rows with a conditional update and audits the expiry; the review queue lists every unreviewed elevation, active or closed; a reviewer (not the opener) records Justified or Unjustified, and Unjustified revokes an active elevation** | Expiry never depends on the sweeper running; conditional update means one host wins without a distributed lock; expiry becomes an audited event as the issue requires | A hosted service and an extra store method |
| **B) Lazy expiry only; no expiry event** | No background work | The issue's "expiry … audited" is not met; the queue cannot tell expired from active without recomputing |
| **C) Schedule an expiry message per elevation through Encina scheduling** | Exact expiry time | Requires the scheduling store and processor; the message can be lost or late, so the check-time comparison is needed anyway |

### Chosen Option: **A — Check-time expiry, idempotent sweeper, four-eyes review** (recommended, pending the maintainer)

### Rationale

- We recommend A because the property AC-039 cares about ("no access after expiry for any duration", the issue's property test) must hold even if no host runs the sweeper; the sweeper only turns an already-ineffective elevation into a recorded, audited `Expired` state.
- Sweeper order: `TryCloseAsync(id, expected: Active, new: Expired)` (only the winning host continues), then the `break_glass.expired` audit entry, then `MarkExpiryRecordedAsync`; rows closed but not yet recorded are retried on the next cycle, so a failed audit write is never lost (AGENTS.md §3, errors are never swallowed in background infrastructure). A duplicate expiry entry after a crash is possible and over-reports.
- Review requires a role in `BreakTheGlassOptions.ReviewerRoles` and a reviewer whose user id differs from the opener's; the outcome publishes `BreakTheGlassReviewedNotification` so an application (or BreachNotification, deferred) can react to an unjustified access.
- Opening publishes `BreakTheGlassOpenedNotification` (elevation id, tenant, scope, times; no subject id, no justification) so the application can alert the treating professional or the DPO.

</details>

<details>
<summary><strong>7. Who May Open, and the Limits — a pluggable authorizer with a fail-closed default</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IBreakTheGlassOpenAuthorizer` with a default that requires one of `BreakTheGlassOptions.OpenerRoles` (or `OpenerPermissions`) per scope on `RequestIdentity`; the options validator rejects a configuration with no opener for a declared scope** | Fail-closed by default; replaceable by an ABAC-backed authorizer; service identities are refused (an emergency is a human decision) | Roles must be configured per scope |
| **B) Any authenticated user may open; the review is the only control** | No configuration | A receptionist can open a clinical record; review after the fact does not prevent the disclosure |
| **C) Open through an application command that the application protects with `[Authorize]` or ABAC** | Reuses the application's own gates | The service could still be called directly; the library cannot guarantee that the open is protected |

### Chosen Option: **A — Pluggable authorizer with a fail-closed default** (recommended, pending the maintainer)

### Rationale

- We recommend A because "a user without the normal permission" is still a user with **some** standing (a clinician of the practice), and the library must make the unsafe configuration impossible rather than documented.
- Limits enforced by `BreakTheGlassRequestValidator` (Either, no exceptions): justification required, trimmed, at least `MinimumJustificationLength` characters (default 20) and at most 2,000; duration greater than zero and at most `MaxDuration` (default 4 hours; the validator rejects a `MaxDuration` above 24 hours); scope declared; subject id non-blank; tenant required when tenancy is on.
- A second open for the same tenant, user, subject and scope while one is active is refused with `break_glass.already_active` (no silent extension); the caller revokes and reopens with a new justification.

</details>

---

## Implementation Phases

### Phase 1: Core Contract, Model and Errors

> **Goal**: The gate contract in core and the domain types of the new package.

<details>
<summary><strong>Tasks</strong></summary>

#### Core (`src/Encina/Authorization/`)

1. `IAccessElevationGate` (public interface, namespace `Encina`):
   - `ValueTask<Either<EncinaError, Option<AccessElevation>>> TryElevateAsync<TRequest>(TRequest request, IRequestContext context, EncinaError denial, CancellationToken cancellationToken) where TRequest : notnull`
   - Contract (XML docs): called only with a definite denial; `None` keeps the denial; `Left` replaces the denial (audit failure); `Some` lets the request continue.
2. `AccessElevation` (public sealed record): `Guid ElevationId`, `string Scope`, `DateTimeOffset ExpiresAtUtc`. No subject id, no justification.
3. `AccessElevationDenials` (public static class): `IsOverridable(EncinaError denial)` over the list of codes in Design Choice 3; used by the gates and re-checked by the implementation.
4. `NoAccessElevationGate` (internal sealed): returns `Right(None)`; registered with `TryAddSingleton<IAccessElevationGate, NoAccessElevationGate>()` in `AddEncina`.
5. `src/Encina/PublicAPI.Unshipped.txt`: the public symbols.

#### New project `src/Encina.Security.BreakTheGlass/`

6. `Encina.Security.BreakTheGlass.csproj` (`net10.0`, nullable, XML docs, PublicAPI analyzers); references `Encina`, `Encina.Security.Audit`; packages `Microsoft.Extensions.Hosting.Abstractions`, `Microsoft.Extensions.Diagnostics.HealthChecks`, `Microsoft.Extensions.Options`, `Microsoft.Extensions.Logging.Abstractions`. Add to `Encina.slnx`.
7. `Model/BreakTheGlassStatus.cs`: `Active`, `Expired`, `Revoked`.
8. `Model/BreakTheGlassReviewOutcome.cs`: `Justified`, `Unjustified`.
9. `Model/BreakTheGlassElevation.cs` (sealed record): `Guid ElevationId`, `string? TenantId`, `string UserId`, `string SubjectId`, `string Scope`, `string Justification`, `DateTimeOffset OpenedAtUtc`, `DateTimeOffset ExpiresAtUtc`, `BreakTheGlassStatus Status`, `DateTimeOffset? ClosedAtUtc`, `string? CloseReason`, `DateTimeOffset? ExpiryRecordedAtUtc`, `BreakTheGlassReviewOutcome? ReviewOutcome`, `string? ReviewedByUserId`, `DateTimeOffset? ReviewedAtUtc`, `string? ReviewNote`; `IsEffectiveAt(DateTimeOffset nowUtc)` (`Status == Active && nowUtc < ExpiresAtUtc`); `ToString()` overridden to omit subject id and justification.
10. `Model/BreakTheGlassRequest.cs` (sealed record): `SubjectId`, `Scope`, `Justification`, `TimeSpan Duration`; `ToString()` without subject and justification.
11. `Model/BreakTheGlassReviewQuery.cs`: `string? Scope`, `BreakTheGlassStatus? Status`, `DateTimeOffset? OpenedFromUtc`, `DateTimeOffset? OpenedToUtc`, `int PageNumber = 1`, `int PageSize = 50` (max 500).
12. `BreakTheGlassErrors.cs` (`EncinaErrors.Create` factory, code constants): `break_glass.validation_failed`, `break_glass.scope_unknown`, `break_glass.already_active`, `break_glass.not_found`, `break_glass.not_active`, `break_glass.already_reviewed`, `break_glass.self_review`, `break_glass.audit_failed`, `break_glass.store_failed`, `break_glass.tenant_required`; authorization refusals use `encina.authorization.forbidden` with `details["reason"] = "break_glass_not_eligible"` / `"break_glass_reviewer_not_eligible"` so `ProblemDetailsExtensions.cs:155` and the Lambda/Functions mappers return 403 unchanged. Messages are fixed strings without ids.
13. `Attributes/BreakTheGlassEligibleAttribute.cs`: `[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]`, ctor `(string scope)`, property `SubjectIdProperty` (required at validation time).
14. `PublicAPI.Shipped.txt` (empty) and `PublicAPI.Unshipped.txt`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
You are implementing Phase 1 of issue #1244 (break-the-glass access, SPEC-002 REQ-051).

CONTEXT:
- Encina is a .NET 10 / C# 14 library; Railway Oriented Programming with Either<EncinaError, T>; pre-1.0, no compatibility shims.
- Core request identity: src/Encina/Abstractions/IRequestContext.cs (Identity at line 87), src/Encina/Identity/RequestIdentity.cs.
- Error factory: EncinaErrors.Create; authorization codes in src/Encina/Errors/EncinaErrorCodes.cs (AuthorizationPrefix at line 95).
- The plan file docs/plans/break-the-glass-implementation-plan-1244.md, Design Choices 1, 3, 4 and 7, fixes the names.

TASK:
Create src/Encina/Authorization/{IAccessElevationGate,AccessElevation,AccessElevationDenials,NoAccessElevationGate}.cs,
register NoAccessElevationGate with TryAddSingleton in AddEncina, and create the project
src/Encina.Security.BreakTheGlass with the model, errors and attribute listed in the Phase 1 tasks. Add it to Encina.slnx.

KEY RULES:
- Sealed records, XML docs on every public member with <summary>, <remarks> and examples where useful.
- No subject id, user id or justification in any ToString, error message or exception message.
- AccessElevationDenials.IsOverridable returns false for encina.authorization.unauthenticated, abac.missing_context /
  abac.unauthenticated_caller, abac.indeterminate, abac.evaluation_failed, abac.obligation_failed and
  abac.policy_not_found; true only for the definite denials listed in Design Choice 3.
- Timestamps are DateTimeOffset with the AtUtc suffix.
- Every public symbol goes to PublicAPI.Unshipped.txt (RS0016 must not fire).

REFERENCE FILES:
- src/Encina/Identity/RequestIdentity.cs
- src/Encina/Errors/EncinaErrorCodes.cs
- src/Encina.Security.ABAC/ABACErrors.cs
- src/Encina.Security/SecurityErrors.cs
- src/Encina.Compliance.DataSubjectRights/Abstractions/IDataSubjectIdExtractor.cs
```

</details>

---

### Phase 2: Service, Validation, Gate Implementation and InMemory Store

> **Goal**: The behaviour of the feature, provider-neutral.

<details>
<summary><strong>Tasks</strong></summary>

1. `Abstractions/IBreakTheGlassStore.cs` (all `ValueTask<Either<EncinaError, T>>`, every method takes `CancellationToken`):
   - `AddAsync(BreakTheGlassElevation elevation, ct)`
   - `GetByIdAsync(string? tenantId, Guid elevationId, ct)` → `Option<BreakTheGlassElevation>`
   - `FindEffectiveAsync(string? tenantId, string userId, string subjectId, string scope, DateTimeOffset nowUtc, ct)` → `Option<BreakTheGlassElevation>`
   - `TryCloseAsync(string? tenantId, Guid elevationId, BreakTheGlassStatus newStatus, string reason, DateTimeOffset closedAtUtc, ct)` → `bool` (only from `Active`)
   - `GetExpiredUnrecordedAsync(DateTimeOffset nowUtc, int batchSize, ct)` → list (across tenants; sweeper only)
   - `MarkExpiryRecordedAsync(string? tenantId, Guid elevationId, DateTimeOffset recordedAtUtc, ct)`
   - `TryRecordReviewAsync(string? tenantId, Guid elevationId, BreakTheGlassReviewOutcome outcome, string reviewerUserId, string? note, DateTimeOffset reviewedAtUtc, ct)` → `bool` (only when unreviewed)
   - `QueryReviewQueueAsync(string? tenantId, BreakTheGlassReviewQuery query, ct)` → `PagedResult<BreakTheGlassElevation>` (unreviewed, oldest first)
   - `PurgeAsync(DateTimeOffset reviewedBeforeUtc, ct)` → `int` (closed and reviewed only)
   - `ProbeAsync(ct)` (health check)
2. `Abstractions/IBreakTheGlassService.cs`: `OpenAsync(BreakTheGlassRequest request, ct)`, convenience `OpenAsync(string subjectId, string scope, string justification, TimeSpan duration, ct)`, `RevokeAsync(Guid elevationId, string reason, ct)`, `ReviewAsync(Guid elevationId, BreakTheGlassReviewOutcome outcome, string? note, ct)`, `GetReviewQueueAsync(BreakTheGlassReviewQuery query, ct)`, `GetAsync(Guid elevationId, ct)`.
3. `Abstractions/IBreakTheGlassOpenAuthorizer.cs`: `ValueTask<Either<EncinaError, Unit>> AuthorizeOpenAsync(RequestIdentity identity, string scope, ct)` and `AuthorizeReviewAsync(RequestIdentity identity, BreakTheGlassElevation elevation, ct)`; default `RoleBreakTheGlassAuthorizer` (opener roles or permissions per scope; reviewer roles; `Kind == Service` refused; reviewer user id must differ from `elevation.UserId`).
4. `Validation/BreakTheGlassRequestValidator.cs`: Design Choice 7 limits; returns `Either<EncinaError, BreakTheGlassRequest>` with the normalised (trimmed) request.
5. `Services/DefaultBreakTheGlassService.cs` (scoped). Ctor: `IBreakTheGlassStore`, `IOperationAuditStore`, `IRequestContextAccessor`, `IBreakTheGlassOpenAuthorizer`, `BreakTheGlassRequestValidator`, `IEncina` (notifications), `IOptions<BreakTheGlassOptions>`, `TimeProvider`, `ILogger<DefaultBreakTheGlassService>`.
   - `OpenAsync`: identity authenticated → authorizer → validator → tenant rule → `FindEffectiveAsync` (refuse `already_active`) → new `ElevationId` (`Guid.CreateVersion7(timeProvider.GetUtcNow())`) → write-ahead audit `break_glass.opened` → `AddAsync` (on `Left`: audit `break_glass.open_failed`, return `Left`) → publish `BreakTheGlassOpenedNotification` → `Right(elevation)`.
   - `RevokeAsync`: opener or reviewer role; `TryCloseAsync(Revoked)`; audit `break_glass.revoked`.
   - `ReviewAsync`: authorizer → `TryRecordReviewAsync` (refuse `already_reviewed`) → audit `break_glass.reviewed` (outcome in metadata) → `Unjustified` and still active: `TryCloseAsync(Revoked, "review_unjustified")` + audit → publish `BreakTheGlassReviewedNotification`.
   - `GetReviewQueueAsync`: reviewer role required; tenant from the context.
6. `Gate/BreakTheGlassAccessGate.cs : IAccessElevationGate` (scoped). Ctor: `IBreakTheGlassStore`, `IOperationAuditStore`, `IReadAuditContext`, `BreakTheGlassRequestGrants` (scoped holder), `IOptions<BreakTheGlassOptions>`, `TimeProvider`, `ILogger<BreakTheGlassAccessGate>`.
   - Order: options enabled → `AccessElevationDenials.IsOverridable(denial)` → identity is `User` → `BreakTheGlassEligibility<TRequest>` (static per-type cache: attribute, compiled subject accessor) → holder already has a grant for this correlation id → return it → subject id non-blank → `FindEffectiveAsync(tenant, user, subject, scope, now)` → write-ahead audit `break_glass.access` (request type name, gate name, denial code in metadata; `Left` → `break_glass.audit_failed`) → set read-audit purpose and metadata → store grant in holder → `Some(AccessElevation)`.
7. `Gate/BreakTheGlassEligibility.cs` (internal static generic cache) and `Gate/BreakTheGlassRequestGrants.cs` (internal scoped holder).
8. `Notifications/BreakTheGlassOpenedNotification.cs`, `BreakTheGlassReviewedNotification.cs` (`INotification`; elevation id, tenant, scope, times, outcome; no subject id, no justification).
9. `Stores/InMemoryBreakTheGlassStore.cs`: `ConcurrentDictionary`, conditional updates under a per-row lock, `TimeProvider`-free (receives timestamps).
10. `src/Encina.Security.Audit/Abstractions/IReadAuditContext.cs` and `ReadAuditContext.cs`: add `IReadOnlyDictionary<string, string> Metadata` and `IReadAuditContext WithMetadata(string key, string value)`; `AuditedReadOnlyRepository.cs:247-250` (and the #1193 decorator) copy it into `ReadAuditEntry.Metadata`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
You are implementing Phase 2 of issue #1244: the break-the-glass service, validator, gate and InMemory store.

CONTEXT:
- Phase 1 types exist in src/Encina/Authorization/ and src/Encina.Security.BreakTheGlass/.
- Audit sink: IOperationAuditStore (src/Encina.Security.Audit/Abstractions/IOperationAuditStore.cs), entry shape in
  src/Encina.Security.Audit/OperationAuditEntry.cs; read-audit purpose in IReadAuditContext.cs.
- Identity: IRequestContext.Identity (RequestIdentity: Kind, UserId, Roles, Permissions, IsAuthenticated).
- Plan Design Choices 3 to 7 fix the order of operations, the audit actions and the error codes.

TASK:
Implement IBreakTheGlassStore, IBreakTheGlassService/DefaultBreakTheGlassService, IBreakTheGlassOpenAuthorizer/
RoleBreakTheGlassAuthorizer, BreakTheGlassRequestValidator, BreakTheGlassAccessGate with its per-type eligibility
cache and per-request grant holder, the two notifications, InMemoryBreakTheGlassStore, and the IReadAuditContext
metadata extension, as listed in the Phase 2 tasks.

KEY RULES:
- Time only from TimeProvider (never DateTime.UtcNow / DateTimeOffset.UtcNow).
- Audit writes are awaited and write-ahead; a Left from IOperationAuditStore fails the operation (fail closed).
- Never override an unauthenticated, indeterminate or error denial; a Service identity never gets an elevation.
- A second gate in the same request reuses the grant from the holder and writes no second access entry.
- No subject id, user id, justification or EncinaError.Message in logs, tags or audit metadata; exceptions go through ForLogging().
- Either everywhere; no exceptions for business outcomes; CancellationToken on every async call.

REFERENCE FILES:
- src/Encina.Security.Audit/AuditedReadOnlyRepository.cs
- src/Encina.Security.Audit/InMemoryOperationAuditStore.cs
- src/Encina.Compliance.Consent/Services/DefaultConsentService.cs
- src/Encina.Compliance.DataSubjectRights/DefaultDataSubjectIdExtractor.cs
- src/Encina/Identity/RequestIdentity.cs
```

</details>

---

### Phase 3: Gate Integration in the Three Pipeline Behaviors

> **Goal**: `AuthorizationPipelineBehavior`, `SecurityPipelineBehavior` and `ABACPipelineBehavior` honour an elevation for definite denials only.

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.AspNetCore/AuthorizationPipelineBehavior.cs`: ctor gains `IAccessElevationGate`; in `Handle` (`:130-150`), when `AuthorizeAsync` returns a denial and `AccessElevationDenials.IsOverridable(denial)`, call `TryElevateAsync`; `Some` → log `AuthorizationElevated` (identity kind and elevation id only) and continue; `Left` → return it; `None` → return the denial. `Unauthenticated` (`:270-283`) is never offered.
2. `src/Encina.Security/SecurityPipelineBehavior.cs`: same hook at the denial return (`:169-173`); `MissingContext` (`:120-129`) and `Unauthenticated` (`:138-145`) are never offered. Written against the post-#1705-phase-4 shape (identity from `IRequestContext`), not against `ISecurityContextAccessor`.
3. `src/Encina.Security.ABAC/ABACPipelineBehavior.cs`: in `HandleDenyAsync` after the OnDeny obligations and advice (`:299-319`) and before `ApplyEnforcementAsync` (`:329`), offer `verdict.DenyError ?? AccessDenied`; `HandleIndeterminate` (`:338-358`), the exception path (`:179-196`) and the missing-context path (`:365-379`) never call the gate. Record the elevation on the activity (`abac.break_glass = true`, no ids).
4. DI tests for each package keep `ValidateOnBuild` + `ValidateScopes` green without the BreakTheGlass package (the core default resolves).
5. Coordination note in the XML docs: when #751 merges, the ABAC decision recorder adds `elevation_id` to the recorded Deny.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
You are implementing Phase 3 of issue #1244: honour break-the-glass elevations in the three authorization gates.

CONTEXT:
- Contract: src/Encina/Authorization/IAccessElevationGate.cs and AccessElevationDenials.cs (Phase 1).
- Gates: src/Encina.AspNetCore/AuthorizationPipelineBehavior.cs (Handle at line 130, AuthorizeAsync at 152),
  src/Encina.Security/SecurityPipelineBehavior.cs (denial returned at 169-173),
  src/Encina.Security.ABAC/ABACPipelineBehavior.cs (HandleDenyAsync 285-330, HandleIndeterminate 338-358).
- Prerequisite: #1705 phase 4 has moved SecurityPipelineBehavior and ABACPipelineBehavior to IRequestContext.Identity.

TASK:
Inject IAccessElevationGate into the three behaviors and consult it only for definite denials as described in
the Phase 3 tasks. Add unit tests per gate: definite deny + grant continues; definite deny + None keeps the
denial; gate Left replaces the denial; unauthenticated, indeterminate, exception and missing-context paths never
call the gate (NSubstitute DidNotReceive).

KEY RULES:
- The allow path is unchanged and never calls the gate.
- ABAC OnDeny obligations and advice still run before the gate is consulted.
- Warn enforcement mode keeps its current meaning; an elevation is evaluated before ApplyEnforcementAsync.
- Logs and tags record the identity kind and a boolean, never the user id, subject id or elevation id in tags.
- Behaviors stay registered with TryAddEnumerable (#1635).

REFERENCE FILES:
- tests/Encina.UnitTests/AspNetCore/AuthorizationPipelineBehaviorTests.cs
- tests/Encina.UnitTests/Security/ABAC/ABACRegistrationTests.cs
- docs/plans/security-context-population-implementation-plan-1705.md (phase 4 shape)
```

</details>

---

### Phase 4: Configuration, DI, Expiry Sweeper and Health Check

> **Goal**: `AddEncinaBreakTheGlass` registers everything it resolves; expired elevations are closed and audited.

<details>
<summary><strong>Tasks</strong></summary>

1. `BreakTheGlassOptions.cs`: `bool Enabled = true`, `IDictionary<string, BreakTheGlassScopeOptions> Scopes` (per scope: `OpenerRoles`, `OpenerPermissions`, `MaxDuration?`), `ISet<string> ReviewerRoles`, `TimeSpan MaxDuration = 4h`, `int MinimumJustificationLength = 20`, `TimeSpan ExpirySweepInterval = 1 min`, `int ExpirySweepBatchSize = 100`, `int RetentionDays = 2555` (aligned with the operation-audit default, ADR-036 decision 1), `bool PublishNotifications = true`; `DeclareScope(string name, Action<BreakTheGlassScopeOptions>)` fluent helper.
2. `BreakTheGlassOptionsValidator : IValidateOptions<BreakTheGlassOptions>` with `ValidateOnStart`: at least one scope; every scope has an opener role or permission; `ReviewerRoles` non-empty; `MaxDuration` in (0, 24h]; interval ≥ 10 s.
3. `BreakTheGlassStartupValidator` (hosted, runs once): every loaded type with `[BreakTheGlassEligible]` names a declared scope and an existing, convertible `SubjectIdProperty` (scan of the assemblies passed to `AddEncinaBreakTheGlass(options, params Assembly[])`); the InMemory store outside `Development` fails start-up unless `AllowInMemoryStore` is set (logged).
4. `ServiceCollectionExtensions.AddEncinaBreakTheGlass(this IServiceCollection, Action<BreakTheGlassOptions>, params Assembly[])`: options + validators; `TryAddSingleton(TimeProvider.System)`; `TryAddSingleton<IBreakTheGlassStore, InMemoryBreakTheGlassStore>()`; scoped service, gate, grant holder, authorizer, validator; **replaces** the core `NoAccessElevationGate` with `BreakTheGlassAccessGate` (`services.Replace`, not `TryAdd`, because core registers the default first); requires `IOperationAuditStore` and `IReadAuditContext` (registers `AddEncinaReadAuditing` defaults with `TryAdd` if missing); hosted `BreakTheGlassExpiryService`; health check.
5. `Services/BreakTheGlassExpiryService.cs : BackgroundService`: `PeriodicTimer` from `TimeProvider`; per cycle `GetExpiredUnrecordedAsync` → `TryCloseAsync(Expired)` (winner only) → audit `break_glass.expired` → `MarkExpiryRecordedAsync`; then `PurgeAsync(now - RetentionDays)`; a `Left` fails the cycle (log, metric, retried next cycle), never reports success.
6. `Health/BreakTheGlassHealthCheck.cs` (`DefaultName = "encina-break-the-glass"`, `Tags = ["encina", "security", "break-the-glass", "ready"]`, scoped resolution through `IServiceProvider.CreateScope()`): `ProbeAsync` on the store; Degraded when the sweeper has not completed a cycle within 3 × interval; Degraded with the InMemory store.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
You are implementing Phase 4 of issue #1244: options, DI registration, the expiry sweeper and the health check.

CONTEXT:
- Phases 1-3 are merged: core contract, BreakTheGlass service/gate/store, gate integration.
- Registration pattern: src/Encina.Compliance.Consent/ServiceCollectionExtensions.cs (TryAdd, TryAddEnumerable).
- Database store override pattern: RemoveInMemoryDefault in src/Encina.ADO.SqlServer/ServiceCollectionExtensions.cs (#1269).

TASK:
Write BreakTheGlassOptions (+ scope options), the options validator, the start-up validator, AddEncinaBreakTheGlass,
BreakTheGlassExpiryService and BreakTheGlassHealthCheck as listed in the Phase 4 tasks.

KEY RULES:
- Registration completeness: a DI test builds the provider with ValidateOnBuild and ValidateScopes, resolves
  IAccessElevationGate and gets BreakTheGlassAccessGate in both orders of AddEncina / AddEncinaBreakTheGlass.
- The sweeper never uses a distributed lock: the conditional TryCloseAsync decides the winner.
- A Left inside the sweeper fails the cycle; nothing is reported as done that was not done.
- Health check: DefaultName const, static Tags, scoped resolution via CreateScope().
- Time only from TimeProvider; the sweeper is testable with FakeTimeProvider.

REFERENCE FILES:
- src/Encina.Security.Audit/ServiceCollectionExtensions.cs
- src/Encina.Security.Audit/ReadAuditRetentionService.cs
- src/Encina.Security.Audit/Health/ReadAuditStoreHealthCheck.cs
- src/Encina.Compliance.Consent/ConsentOptionsValidator.cs
```

</details>

---

### Phase 5: Provider Implementations — 10 Database Providers

> **Goal**: `IBreakTheGlassStore` on ADO.NET ×3, Dapper ×3, EF Core ×3 and MongoDB.

<details>
<summary><strong>Tasks</strong></summary>

#### 5a. ADO.NET ×3 and 5b. Dapper ×3 (`BreakTheGlass/BreakTheGlassStore{ADO|Dapper}.cs`, `Scripts/0NN_CreateBreakTheGlassElevationsTable.sql`, `000_CreateAllTables.sql`)

1. Table `BreakTheGlassElevations`: `ElevationId` (PK; `UNIQUEIDENTIFIER` / `UUID` / `CHAR(36)`), `TenantId` (nullable), `UserId`, `SubjectId`, `Scope`, `Justification` (`NVARCHAR(2000)` / `TEXT` / `VARCHAR(2000)`), `OpenedAtUtc`, `ExpiresAtUtc`, `Status` (int), `ClosedAtUtc`, `CloseReason`, `ExpiryRecordedAtUtc`, `ReviewOutcome` (int, nullable), `ReviewedByUserId`, `ReviewedAtUtc`, `ReviewNote`.
2. Indexes: `IX_BreakTheGlassElevations_Effective (TenantId, UserId, SubjectId, Scope, Status, ExpiresAtUtc)`, `IX_BreakTheGlassElevations_ReviewQueue (TenantId, ReviewOutcome, OpenedAtUtc)`, `IX_BreakTheGlassElevations_Expiry (Status, ExpiresAtUtc)`.
3. Script number: the next free number in each `Scripts/` folder at implementation time (028 is the last on `5b485b12`; #1187 and #1189 also add scripts, so re-check before writing). Dapper scripts byte-identical to the ADO ones.
4. Conditional updates: `UPDATE … SET Status = @new, ClosedAtUtc = @at, CloseReason = @r WHERE ElevationId = @id AND (TenantId = @t OR (@t IS NULL AND TenantId IS NULL)) AND Status = 0`, result = rows affected = 1; review likewise with `ReviewOutcome IS NULL`.
5. SQL dialects per AGENTS.md §5 (`TOP (@n)` vs `LIMIT @n`, `bit` vs `true/false` vs `0/1`, provider quoting); parameters only.
6. Every method returns `Left(break_glass.store_failed)` on provider exceptions (the #1135 pattern).

#### 5c. EF Core (`BreakTheGlass/BreakTheGlassElevationEntity.cs`, `BreakTheGlassElevationEntityConfiguration.cs`, `BreakTheGlassStoreEF.cs`)

7. Entity + configuration with the indexes above; conditional updates with `ExecuteUpdateAsync`; `ModelBuilder.ApplyEncinaBreakTheGlass()` extension; writes through `IDbContextFactory<TContext>` (as #1193 OD-9 for the read-audit store), never `SaveChangesAsync` on the application's context.

#### 5d. MongoDB (`BreakTheGlass/BreakTheGlassElevationDocument.cs`, `BreakTheGlassStoreMongoDB.cs`)

8. Collection `break_glass_elevations` (`EncinaMongoDbOptions` collection name property); indexes created at start-up; conditional updates with `UpdateOneAsync` filters on `status` / `review_outcome`.

#### 5e. Registration on every provider

9. `MessagingConfiguration.UseBreakTheGlassStore` (`src/Encina.Messaging/MessagingConfiguration.cs`, next to `UseReadAuditStore` at `:526`) and `EncinaMongoDbOptions.UseBreakTheGlassStore` (`src/Encina.MongoDB/EncinaMongoDbOptions.cs:108`); each provider's `ServiceCollectionExtensions` removes the InMemory default and `TryAddScoped`s its store (as `ReadAuditStoreADO` at `src/Encina.ADO.SqlServer/ServiceCollectionExtensions.cs:100-106`), so the database store wins in any order without overriding an application registration.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
You are implementing Phase 5 of issue #1244: IBreakTheGlassStore on the 10 database providers.

CONTEXT:
- Contract: src/Encina.Security.BreakTheGlass/Abstractions/IBreakTheGlassStore.cs (Phase 2).
- Pattern to copy: the read-audit stores src/Encina.{ADO|Dapper}.{SqlServer|PostgreSQL|MySQL}/Auditing/ReadAuditStore*.cs,
  src/Encina.EntityFrameworkCore/Auditing/ReadAuditStoreEF.cs, src/Encina.MongoDB/Auditing/ReadAuditStoreMongoDB.cs,
  their scripts (Scripts/020_CreateReadAuditEntriesTable.sql, 000_CreateAllTables.sql) and the UseReadAuditStore flag.

TASK:
Implement BreakTheGlassStoreADO (x3), BreakTheGlassStoreDapper (x3), BreakTheGlassStoreEF, BreakTheGlassStoreMongoDB,
the table scripts, the EF entity/configuration, the MongoDB document and the UseBreakTheGlassStore flags with their
registrations, as listed in 5a-5e.

KEY RULES:
- Store naming {Pattern}Store{Provider}; feature folder BreakTheGlass/ in each provider package (AGENTS.md section 4).
- Async calls with CancellationToken only; never the synchronous overloads.
- Conditional updates decide TryCloseAsync and TryRecordReviewAsync; return false when no row matched.
- Every method returns Left on provider exceptions; never let them escape.
- TenantId null matches only rows with TenantId null (fixed default tenant when tenancy is off).
- The database store wins over the InMemory default in any order and never removes an application registration.

REFERENCE FILES:
- src/Encina.ADO.SqlServer/Auditing/ReadAuditStoreADO.cs
- src/Encina.ADO.SqlServer/ServiceCollectionExtensions.cs
- src/Encina.EntityFrameworkCore/Auditing/ReadAuditEntryEntityConfiguration.cs
- src/Encina.MongoDB/Auditing/ReadAuditEntryDocument.cs
- src/Encina.Messaging/MessagingConfiguration.cs
```

</details>

---

### Phase 6: Cross-Cutting Integration

> **Goal**: Tenancy, read-audit linkage and notifications wired end to end.

<details>
<summary><strong>Tasks</strong></summary>

1. **Multi-tenancy** (REQ-061, AC-043): `OpenAsync`, the gate, the review queue and `GetAsync` take the tenant from `IRequestContext.TenantId`; when tenancy is on (`ITenantProvider` registered, `src/Encina.Tenancy/Abstractions/ITenantProvider.cs:57`) a missing tenant fails with `break_glass.tenant_required`; with tenancy off the fixed default tenant (`null`) applies, as in the #1189 plan. The sweeper works across tenants and audits each entry with the row's tenant.
2. **Read audit**: the gate sets `IReadAuditContext.WithPurpose("break-the-glass")` and `WithMetadata("break_glass.elevation_id", id)`; the #1193 functional-repository decorator copies it; verify in an integration test once #1193 has merged (until then the test targets `AuditedReadOnlyRepository`).
3. **Audit trail**: actions `break_glass.opened`, `break_glass.open_failed`, `break_glass.access`, `break_glass.revoked`, `break_glass.expired`, `break_glass.reviewed`; `EntityType = "BreakTheGlassElevation"`, `EntityId = elevation id`, `UserId` = actor, `TenantId`, `Outcome`, `Metadata` (scope, request type, gate, denial code, review outcome); timestamps from `TimeProvider` (ADR-036 decision 8). No justification, no subject id in metadata.
4. **Notifications**: published through `IEncina.Publish` when `PublishNotifications` is on; a `Left` from publishing is logged and metered and returned by `OpenAsync` / `ReviewAsync` after the state change is audited (the caller learns that the alert did not go out).
5. **ABAC decision audit (#751)**: if #751 has merged, pass the elevation id to its recorder; otherwise leave the documented hook.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```text
You are implementing Phase 6 of issue #1244: cross-cutting integration of break-the-glass.

CONTEXT:
- Phases 1-5 are merged. Tenant comes from IRequestContext.TenantId; tenancy is on when ITenantProvider is registered.
- Read audit: IReadAuditContext (Purpose, and the Metadata added in Phase 2); #1193 may or may not have merged.
- Operation audit: IOperationAuditStore, OperationAuditEntry (Action, EntityType, EntityId, Outcome, Metadata).

TASK:
Wire tenancy, the read-audit linkage, the audit actions and the notifications as listed in the Phase 6 tasks, and add
the two-tenant tests: tenant A never sees, uses, reviews or revokes tenant B's elevations.

KEY RULES:
- Fail closed when tenancy is on and the tenant is missing.
- One access entry per elevated request even when several gates deny it.
- No justification, subject id or EncinaError.Message in audit metadata, logs or tags.
- Publishing failures are reported to the caller, never swallowed.

REFERENCE FILES:
- src/Encina.Security.Audit/OperationAuditEntry.cs
- src/Encina.Security.Audit/Abstractions/IReadAuditContext.cs
- src/Encina.Tenancy/Abstractions/ITenantProvider.cs
- docs/plans/blocked-data-state-implementation-plan-1189.md (Design 6, disclosure scope)
```

</details>

---

### Phase 7: Observability

> **Goal**: Tracing, metrics and structured logs that carry the tenant and never a subject.

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina/Diagnostics/EventIdRanges.cs`: register `SecurityBreakTheGlass = (9750, 9799)` (reserved; see `docs/architecture/adr/index.md`); add the field to `src/Encina/PublicAPI.Unshipped.txt`.
2. `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`: add `Encina.Security.BreakTheGlass` to `AssemblyRanges`.
3. `Diagnostics/BreakTheGlassLogMessages.cs` (`[LoggerMessage]`, EventIds 9750-9764 packed): Opened, OpenRefused (reason code), OpenAuditFailed, AccessGranted (request type, gate), AccessAuditFailed, AccessNotEligible, Revoked, Reviewed (outcome), SelfReviewRefused, ExpirySweepCompleted (count), ExpirySweepFailed (error code), ExpiryRecorded, NotificationFailed (error code), InMemoryStoreInUse, StartupValidationFailed. XML doc `Event IDs: 9750-9764 (see EventIdRanges.SecurityBreakTheGlass)`.
4. `Diagnostics/BreakTheGlassDiagnostics.cs`: `ActivitySource("Encina.Security.BreakTheGlass")`, spans `encina.break_glass.open`, `encina.break_glass.access`, `encina.break_glass.review`, `encina.break_glass.expiry_sweep`; tags `encina.tenant_id`, `encina.break_glass.scope`, `encina.break_glass.outcome`, `encina.break_glass.gate`.
5. `Meter("Encina.Security.BreakTheGlass")`: `Counter<long>` `encina.break_glass.opened` (tenant, scope, outcome), `encina.break_glass.accesses` (tenant, scope, gate), `encina.break_glass.reviewed` (tenant, outcome), `encina.break_glass.expired` (tenant), `encina.break_glass.audit_failures` (tenant, action); `ObservableGauge<long>` `encina.break_glass.review_queue_depth` is **not** added (it would query the store per scrape).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```text
You are implementing Phase 7 of issue #1244: observability for break-the-glass.

CONTEXT:
- EventId registry: src/Encina/Diagnostics/EventIdRanges.cs (ADR-021); 9750-9799 reserved.
- Allocation test: tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs (AssemblyRanges map).
- Reference diagnostics: src/Encina.Security.Audit/Diagnostics/ReadAudit{ActivitySource,Meter,Log}.cs.

TASK:
Register SecurityBreakTheGlass = (9750, 9799), write BreakTheGlassLogMessages (9750-9764, packed), the
ActivitySource and Meter listed in the Phase 7 tasks, and call them from the service, gate and sweeper.

KEY RULES:
- Register the range before using any EventId; never outside it; sequential, no gaps.
- [LoggerMessage] source generator only.
- Tags and log arguments: tenant id, scope, outcome, gate, request type name, error code. Never subject id,
  user id, justification, elevation payload or EncinaError.Message.
- A test with an in-memory exporter asserts the spans and counters carry the tenant and none of the forbidden values.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
- src/Encina.Security.Audit/Diagnostics/ReadAuditMeter.cs
- src/Encina.Security.ABAC/Diagnostics/ABACDiagnostics.cs
```

</details>

---

### Phase 8: Testing

> **Goal**: Every flag reaches its target in `.github/coverage-manifest/Encina.Security.BreakTheGlass.json`; AC-039 proven.

<details>
<summary><strong>Tasks</strong></summary>

1. **Coverage manifest**: create `.github/coverage-manifest/Encina.Security.BreakTheGlass.json` with per-file per-flag targets and one-sentence justifications; extend the manifests of `Encina`, `Encina.Security`, `Encina.Security.ABAC`, `Encina.AspNetCore`, `Encina.Security.Audit` and the 10 provider packages for the files touched.
2. **UnitTests** (`tests/Encina.UnitTests/Security/BreakTheGlass/`): validator (every limit), authorizer (roles, permissions, service identity, self-review), service (each step and each `Left`, write-ahead order with NSubstitute `Received.InOrder`), gate (each guard, grant reuse across gates, audit failure denies), sweeper with `FakeTimeProvider` (winner/loser, failed audit retried), options validator, start-up validator, InMemory store, the three gate integrations (Phase 3).
3. **GuardTests** (`tests/Encina.GuardTests/Security/BreakTheGlass/`): every public constructor and method parameter.
4. **ContractTests** (`tests/Encina.ContractTests/Security/BreakTheGlass/`): an abstract `BreakTheGlassStoreContract` run against InMemory (and reused by the integration suites): effective lookup excludes expired, revoked and other tenants; `TryCloseAsync` true once then false; review once; queue order and paging; purge only closed and reviewed. `IAccessElevationGate` contract: `NoAccessElevationGate` and `BreakTheGlassAccessGate` return `None` for non-overridable denials.
5. **PropertyTests** (`tests/Encina.PropertyTests/Security/BreakTheGlass/`, FsCheck through `Encina.Testing.FsCheck`): for any duration in (0, MaxDuration] and any clock advance, an access at `t ≥ ExpiresAtUtc` is never granted and an access at `t < ExpiresAtUtc` of an active elevation always is; any justification shorter than the minimum after trimming is refused; any non-overridable denial code is never overridden.
6. **IntegrationTests** (`tests/Encina.IntegrationTests/Security/BreakTheGlass/{ADO,Dapper,EFCore}/{SqlServer,PostgreSQL,MySQL}/`, `MongoDB/`): the store contract on all 10 providers with the shared `[Collection("<Family>-<Database>")]` fixtures, `ClearAllDataAsync` in `InitializeAsync`; one end-to-end AC-039 test per family (open without justification refused; open with justification → elevated read audited in operation and read audit → clock past expiry → refused → sweeper records expiry), and a two-tenant test.
7. **LoadTests**: `tests/Encina.LoadTests/Security/BreakTheGlass/BreakTheGlass.md` justification (the gate runs only on denied requests of eligible types; no concurrent hot path; the conditional update is covered by contract and integration tests).
8. **BenchmarkTests**: `tests/Encina.BenchmarkTests/Security/BreakTheGlass/BreakTheGlass.md` justification (not a hot path: the allow path never calls the gate; the deny path adds one indexed lookup).
9. **CRAP ≤ 10** on every changed method (measure locally with the crap-gate table before review).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```text
You are implementing Phase 8 of issue #1244: the tests of break-the-glass across all test types.

CONTEXT:
- Phases 1-7 are merged. Test projects: tests/Encina.{Unit,Guard,Contract,Property,Integration}Tests, plus
  .md justifications for Load and Benchmark tests (AGENTS.md section 9).
- Integration fixtures: shared collections ADO-/Dapper-/EFCore- x SqlServer/PostgreSQL/MySQL and MongoDB.

TASK:
Write the coverage manifest and the tests listed in the Phase 8 tasks, then measure each flag:
dotnet test tests\Encina.<Flag>Tests --collect "XPlat Code Coverage" --results-directory artifacts\coverage\<Flag>Tests,
dotnet run --file .github/scripts/coverage-report.cs -- --input artifacts/coverage --output artifacts/coverage-report,
dotnet run --file .github/scripts/coverage-report.cs -- --check-justifications.

KEY RULES:
- Tests execute real package code; no reflection-only tests.
- Shouldly through Encina.Testing.Shouldly; FsCheck through Encina.Testing.FsCheck; FakeTimeProvider for time.
- Never IClassFixture or new fixtures for databases; never dispose the fixture from a test.
- No Thread.Sleep; deterministic; AAA; descriptive names.
- Every applicable flag reaches its manifest target; every changed method has CRAP <= 10.

REFERENCE FILES:
- tests/Encina.IntegrationTests (Collections.cs files and an existing read-audit store suite)
- tests/Encina.UnitTests/Security/ABAC/ABACRegistrationTests.cs
- docs/testing/coverage-measurement-methodology.md
- docs/testing/integration-tests.md
```

</details>

---

### Phase 9: Documentation and Finalization

> **Goal**: Every document the prompt requires, build and tests green.

<details>
<summary><strong>Tasks</strong></summary>

1. XML documentation on every new public API (`<summary>`, `<remarks>`, `<param>`, `<returns>`, `<example>`), citing SPEC-002 REQ-051 and the regulations in Research.
2. `src/Encina.Security.BreakTheGlass/README.md`: what it is, registration, declaring scopes and eligible requests, opening, review, the gates it integrates with, provider stores, what is audited.
3. `docs/features/break-the-glass.md` (how-to and configuration, one Diátaxis quadrant per page per the `encina-docs` skill) and a reference entry for the error codes in the existing error reference.
4. ADR "Break-the-glass overrides only definite denials, at the enforcement point, write-ahead audited": the next ADR number neither used nor reserved in `docs/architecture/adr/index.md` (037 on `5b485b12`), reserved in the index by the plan PR.
5. `changelog.d/1244-break-the-glass.added.md`.
6. `docs/INVENTORY.md` (new package), `ROADMAP.md` (P-42 in v0.17.0), `docs/releases/` notes if the milestone has a release page.
7. `PublicAPI.Unshipped.txt` complete in every touched package.
8. `dotnet build Encina.slnx --configuration Release` → 0 errors, 0 warnings; `dotnet test` → all pass; every coverage flag at its manifest target.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 9</strong></summary>

```text
You are implementing Phase 9 of issue #1244: documentation and finalization of break-the-glass.

CONTEXT:
- Phases 1-8 are merged and green. Documentation rules: .claude/skills/encina-docs/SKILL.md (Diátaxis, cited
  figures, no hand-typed coverage numbers).

TASK:
Write the XML docs review, the package README, docs/features/break-the-glass.md, the ADR, the changelog fragment and
the INVENTORY/ROADMAP updates listed in the Phase 9 tasks; then run the Release build and the tests.

KEY RULES:
- English only; no emojis; no AI attribution.
- Never edit the [Unreleased] section of CHANGELOG.md; add changelog.d/1244-break-the-glass.added.md.
- Coverage figures are cited with covref markers, never typed.
- The ADR takes the next number that is neither used nor reserved in docs/architecture/adr/index.md.
- Build: zero warnings; tests: all pass; flags at target.

REFERENCE FILES:
- src/Encina.Security.ABAC/README.md
- docs/features/read-auditing.md
- docs/architecture/adr/036-three-audit-stores.md
- changelog.d/README.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | What it asks of this feature |
|--------|-----------|------------------------------|
| SPEC-002 REQ-051, AC-039 | Break-the-glass with mandatory justification, time box and review | Refuse without justification; audit with justification; expire |
| SPEC-002 REQ-061 / AC-043, REQ-062 / AC-044 | Tenant awareness; instrumentation without identifiers | Tenant on every record and query; spans, metrics and logs carry the tenant, never a subject |
| SPEC-002 DEC-006, INV-005 | Compliance gates fail closed | Missing context, failed audit write or missing tenant deny |
| GDPR Art. 5(2), Art. 32 | Accountability; security of processing | Evidence of who opened, why, what was accessed, who reviewed |
| GDPR Art. 9(2)(c), (h) | Vital interests; health care | The legal grounds an emergency access usually relies on (the application's decision) |
| EHDS Reg. (EU) 2025/327 Art. 8 | Override of access restrictions in vital-interest cases, logged | The override exists, is logged and is exceptional |
| Ley 41/2002 art. 16 | Access to the clinical history by treating professionals | Normal access stays restricted; the override is the exception |
| Código Deontológico del Psicólogo art. 46 | Records restricted to the professional | Same; the reason the reference application needs the feature |
| ISO/IEC 27002:2022 5.18, 8.2, 8.15 | Access rights; privileged access rights; logging | Time-limited privileged access, approved and logged, reviewed |
| HL7 v3 ActReason `BTG` (purpose of use) | "Break the glass" purpose code | The read-audit purpose value `break-the-glass` |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|----------------------|
| `IRequestContext.Identity`, `RequestIdentity` | `src/Encina/Abstractions/IRequestContext.cs:87`, `src/Encina/Identity/RequestIdentity.cs:47-212` | Who opens, who accesses, who reviews; `Kind` refuses service identities |
| `AuthorizationPipelineBehavior` | `src/Encina.AspNetCore/AuthorizationPipelineBehavior.cs:130-320` | Consults the gate on policy/role denials |
| `SecurityPipelineBehavior` | `src/Encina.Security/SecurityPipelineBehavior.cs:80-178` | Consults the gate on attribute denials |
| `ABACPipelineBehavior` | `src/Encina.Security.ABAC/ABACPipelineBehavior.cs:285-330` | Consults the gate on a definite ABAC Deny |
| `IOperationAuditStore`, `OperationAuditEntry` | `src/Encina.Security.Audit/Abstractions/IOperationAuditStore.cs:32`, `OperationAuditEntry.cs:50` | Write-ahead audit of open, access, expiry, revocation, review |
| `IReadAuditContext`, `ReadAuditEntry.Metadata` | `src/Encina.Security.Audit/Abstractions/IReadAuditContext.cs:38`, `ReadAuditEntry.cs:176` | Purpose `break-the-glass` and elevation id on read entries |
| Read-audit store pattern and flag | `src/Encina.ADO.SqlServer/Auditing/ReadAuditStoreADO.cs`, `src/Encina.Messaging/MessagingConfiguration.cs:526` | Template for the 10 store implementations and `UseBreakTheGlassStore` |
| `IDataSubjectIdExtractor` conversion rules | `src/Encina.Compliance.DataSubjectRights/Abstractions/IDataSubjectIdExtractor.cs:45-65` | Same subject-id conversion in the eligibility accessor |
| `encina.authorization.*` HTTP mapping | `src/Encina.AspNetCore/ProblemDetailsExtensions.cs:155` and the Lambda/Functions mappers | Refusals of the opener/reviewer map to 403 with no new code |
| Shared integration fixtures | `tests/Encina.IntegrationTests` `Collections.cs` files | Store suites on the 10 providers |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Security.Audit` (read audit) | 1700-1799 | Existing (`SecurityAuditRead`) |
| `Encina.Security.Audit` | 5000-5099 | Existing (`SecurityAudit`) |
| `Encina.Security.ABAC` | 9000-9099 | Existing; gate integration reuses this range for its one new message |
| `Encina.AspNetCore` | 200-249 | Existing; `AuthorizationElevated` message added inside it |
| **`Encina.Security.BreakTheGlass`** | **9750-9799** | **New (`SecurityBreakTheGlass`); 9750-9764 used, packed** |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Core contract (Phase 1) | 4 | `src/Encina/Authorization/` + PublicAPI |
| New package (Phases 1, 2, 4, 7) | ~27 | Model, abstractions, service, gate, validators, options, DI, sweeper, health, diagnostics, notifications, InMemory store, csproj, PublicAPI |
| Gate integration (Phase 3) | 3 modified | Three pipeline behaviors |
| Read-audit context (Phase 2) | 3 modified | `IReadAuditContext`, `ReadAuditContext`, decorator |
| Providers (Phase 5) | ~24 | ADO ×3 and Dapper ×3 (store + script + all-tables script), EF Core (3), MongoDB (2), flags (2), DI (8 modified) |
| Tests (Phase 8) | ~30 | Unit, guard, contract, property, integration ×10, two `.md` justifications, manifests |
| Documentation (Phase 9) | ~7 | README, feature page, ADR, changelog fragment, INVENTORY, ROADMAP, error reference |
| **Total** | **~80** | |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #1244: break-the-glass access with justification, time box and review
(SPEC-002 REQ-051, AC-039, tracking id P-42).

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library; ROP with Either<EncinaError, T>; pre-1.0 (no compatibility layers).
- One request identity: IRequestContext.Identity (RequestIdentity) after #1705; the three gates read it.
- Audit: IOperationAuditStore (general write sink, ADR-036) and IReadAuditStore with IReadAuditContext (purpose).
- 10 database providers: ADO.NET, Dapper, EF Core x SqlServer/PostgreSQL/MySQL, plus MongoDB.

IMPLEMENTATION OVERVIEW:
Phase 1: core IAccessElevationGate / AccessElevation / AccessElevationDenials / NoAccessElevationGate; new package
         Encina.Security.BreakTheGlass with model, errors, [BreakTheGlassEligible].
Phase 2: IBreakTheGlassStore, IBreakTheGlassService, authorizer, validator, BreakTheGlassAccessGate, notifications,
         InMemory store, IReadAuditContext metadata.
Phase 3: the three gates consult the elevation gate on definite denials only.
Phase 4: options, validators, AddEncinaBreakTheGlass, expiry sweeper, health check.
Phase 5: BreakTheGlassStore{ADO,Dapper,EF,MongoDB} on the 10 providers + UseBreakTheGlassStore flags.
Phase 6: tenancy, read-audit linkage, audit actions, notifications, #751 hook.
Phase 7: EventIds 9750-9799, ActivitySource and Meter "Encina.Security.BreakTheGlass".
Phase 8: unit, guard, contract, property (no access after expiry), integration on 10 providers; load and
         benchmark .md justifications; coverage manifest.
Phase 9: XML docs, README, feature page, ADR, changelog fragment, INVENTORY/ROADMAP; Release build clean.

KEY PATTERNS:
- An elevation binds tenant + user + subject + scope; only request types marked [BreakTheGlassEligible] qualify.
- Only definite authorization denials are overridable; unauthenticated, indeterminate and errors never are.
- Audit writes are awaited, write-ahead and fail closed; one access entry per request even across gates.
- Expiry holds at check time through TimeProvider; the sweeper only records it, with a conditional update.
- Review by a different user holding a reviewer role; Unjustified revokes an active elevation.
- No subject id, user id, justification or EncinaError.Message in logs, tags or audit metadata.
- Store naming {Pattern}Store{Provider}; DB store wins over InMemory in any order; TryAdd/TryAddEnumerable.

REFERENCE FILES:
- docs/plans/break-the-glass-implementation-plan-1244.md (this plan)
- src/Encina/Identity/RequestIdentity.cs, src/Encina/Abstractions/IRequestContext.cs
- src/Encina.AspNetCore/AuthorizationPipelineBehavior.cs, src/Encina.Security/SecurityPipelineBehavior.cs,
  src/Encina.Security.ABAC/ABACPipelineBehavior.cs
- src/Encina.Security.Audit/ (operation and read audit), src/Encina.ADO.SqlServer/Auditing/ReadAuditStoreADO.cs
- docs/plans/evidential-read-audit-implementation-plan-1193.md, docs/plans/security-context-population-implementation-plan-1705.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | The effective-elevation lookup must see a revocation or an expiry at once; a cache would extend access past the time box. The lookup runs only on a definite denial of an eligible request type, so it is not a hot read |
| 2 | OpenTelemetry | ✅ Phase 7 | `ActivitySource` and `Meter` "Encina.Security.BreakTheGlass"; spans for open, access, review and sweep; counters with tenant, scope, outcome and gate tags; no subject or user id (REQ-062) |
| 3 | Structured Logging | ✅ Phase 7 | `[LoggerMessage]` EventIds 9750-9764 in the new `SecurityBreakTheGlass` range 9750-9799 registered in `EventIdRanges.cs` (ADR-021) |
| 4 | Health Checks | ✅ Phase 4 | `BreakTheGlassHealthCheck` probes the new store and the sweeper's last cycle. Differs from the issue's N/A because Design Choice 2 adds a store; becomes N/A if the maintainer chooses the audit-derived option |
| 5 | Validation | ✅ Phase 2 | `BreakTheGlassRequestValidator` (justification mandatory and bounded, duration capped, scope declared), `BreakTheGlassOptionsValidator` with `ValidateOnStart`, start-up check of eligible request types |
| 6 | Resilience | ❌ N/A | No external system; store calls return `Left` and the gate fails closed; retrying an authorization decision would delay the denial without changing it, and the provider's database circuit breaker already applies |
| 7 | Distributed Locks | ❌ N/A | The only multi-host work (expiry sweep) is decided by a conditional `UPDATE … WHERE Status = Active`; exactly one host wins, no lock needed |
| 8 | Transactions | ❌ N/A | Write-ahead ordering instead: the audit entry precedes the state change, and the audit and elevation stores may live in different databases, so a shared transaction cannot be assumed |
| 9 | Idempotency | ✅ Phase 2 | A duplicate open (double submit) for the same tenant, user, subject and scope is refused with `break_glass.already_active`; review and close are conditional and apply once; not a message entry point, so no inbox |
| 10 | Multi-Tenancy | ✅ Phase 6 | Tenant on every elevation and audit entry, every lookup and queue query filtered by it, fail closed when tenancy is on and the tenant is missing, fixed default tenant (`null`) when off; two-tenant tests (AC-043) |
| 11 | Module Isolation | ❌ N/A | SPEC-002 requires no module scoping for break-the-glass; `ModuleId` in messaging stays with #747 |
| 12 | Audit Trail | ✅ Phase 6 | `IOperationAuditStore` entries for opened, open_failed, access, revoked, expired, reviewed (one elevation's history by `EntityId`); read-audit purpose `break-the-glass` with the elevation id in metadata |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| [#1705](https://github.com/dlrivada/Encina/issues/1705) One request identity, phase 4 (`SecurityPipelineBehavior` and `ABACPipelineBehavior` read `IRequestContext.Identity`) | Open (phases 1-3 merged, e.g. `584ece93`) | Phase 3 of this plan targets the post-#1705 gate shape; `AuthorizationPipelineBehavior` already reads the identity and can be integrated first |
| [#1193](https://github.com/dlrivada/Encina/issues/1193) Evidential read audit (P-05) | Open | "Each access read-audited" is evidence only once read audit is awaited and covers the 10 provider repositories; the read-audit metadata of Phase 2 is copied by its decorator |
| `IOperationAuditStore` on the 10 providers | Done ([#1633](https://github.com/dlrivada/Encina/issues/1633) closed; `028_CreateOperationAuditEntriesTable.sql`) | Sink for the write-ahead entries |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| ABAC decision audit | [#751](https://github.com/dlrivada/Encina/issues/751) (open) | Its recorder should carry the elevation id on an overridden Deny; hook documented in Phase 3 |
| Shared subject-id converter in core | #1193 OD-3 | Replaces the package's internal copy of the DSR conversion rules |
| Tenant filtering for Security.Audit queries | [#798](https://github.com/dlrivada/Encina/issues/798) (open) | Lets a tenant read its own break-glass audit history without cross-tenant leakage |
| Breach detection on unjustified reviews | `artifacts/issues/plan-1244-breach-event-on-unjustified-review.md` (draft) | Deferred integration: an unjustified break-glass access may be a personal-data breach (GDPR Art. 33) |

---

## Next Steps

1. The maintainer decides Design Choices 1-7; the orchestrator records the answers in a "Maintainer Decisions" section.
2. Reserve the ADR number (037 on `5b485b12`) in `docs/architecture/adr/index.md` in the plan PR.
3. Open the deferred issue drafted in `artifacts/issues/plan-1244-breach-event-on-unjustified-review.md`.
4. Link this plan from #1244 and start Phase 1 once #1705 phase 4 is scheduled (Phases 1-2 do not wait for it).
5. Each phase is a self-contained commit; the final PR references `Fixes #1244`.
