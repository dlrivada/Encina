# Implementation Plan: Policy Administration Point Authorizes the Actor with a Configurable Permission, Fail Closed

> **Issue**: [#1986](https://github.com/dlrivada/Encina/issues/1986)
> **Type**: Feature (security gate)
> **Complexity**: Medium (7 phases, 4 PRs, one package plus a comment in the core registry)
> **Estimated Scope**: ~1,600 changed lines across 4 PRs (about 450 of production code, the rest tests, records and docs), about 32 files (8 new, 24 modified)

---

## Summary

Since [#1677](https://github.com/dlrivada/Encina/issues/1677) and [#1705](https://github.com/dlrivada/Encina/issues/1705) the persistent Policy Administration Point (`PersistentPolicyAdministrationPoint`) attributes every policy change to the authenticated caller of the ambient `IRequestContext` and refuses an anonymous caller with `abac.policy_change_principal_required`. It does **not** authorize that caller: any authenticated user or service whose code reaches `IPolicyAdministrationPoint` can add, update or remove policy sets and policies. The documentation says so in plain words ("The PAP records the actor but does not authorize it: the application gates its own policy-administration path", `docs/features/abac/reference/persistent-pap.md`). For a security gate that is fail-open (SPEC-002 DEC-006).

This plan adds the missing authorization inside the PAP: a configurable permission that the actor must hold before any of the six mutations is applied.

**What it implements**

- `ABACOptions.PolicyAdministrationPermission` (default `abac:policies:manage`), validated at startup by `ABACOptionsValidator` (not blank, no surrounding whitespace, no `*`).
- An internal `PolicyChangeAuthorizer` that reads `IRequestContext.Identity` **once** and answers one of three outcomes: anonymous (existing `abac.policy_change_principal_required`), authenticated without the permission (new `encina.authorization.abac_policy_change_forbidden`, answered with HTTP 403 by every host adapter through the shared `encina.authorization.` prefix), or granted (an actor record for the audit entry).
- Both `PersistentPolicyAdministrationPoint` and `InMemoryPolicyAdministrationPoint` call it before `AddPolicySetAsync`, `UpdatePolicySetAsync`, `RemovePolicySetAsync`, `AddPolicyAsync`, `UpdatePolicyAsync` and `RemovePolicyAsync`, and **before** any store read, so a refused caller learns nothing about which policies exist.
- The built-in seeding identity `service:encina.abac.policy-seeding` is declared with the configured permission, taken from the final options, so startup seeding keeps working.
- A refused attempt is recorded: one `Denied` `OperationAuditEntry` in the persistent PAP (tenant, correlation id and caller id from the same identity read, the required permission name, never the caller's own permissions), a Warning log (EventId 9041), a counter and an activity in both PAPs.

**Standards and specifications**: SPEC-002 DEC-006 (compliance and security gates fail closed); AGENTS.md section 3 (gates fail closed, `EncinaError.Message` never reaches logs, background and audit rules); ADR-006 (ROP), ADR-018 (cross-cutting), ADR-021 (EventIds); NIST SP 800-162 (the PAP is the point that creates and manages policy, which is why its callers need their own authorization); OASIS XACML 3.0 (PAP role).

**Affected packages**: `Encina.Security.ABAC` (code, `PublicAPI.Unshipped.txt`, README); `Encina` core only for a comment line in `src/Encina/Diagnostics/EventIdRanges.cs` (the `SecurityABAC` range 9000-9099 already exists, no new range). Tests in `tests/Encina.UnitTests`, `GuardTests`, `ContractTests`, `IntegrationTests`. Docs: four pages under `docs/features/abac/reference/`.

**Provider category**: none of the database matrix. The PAP operates over `IPolicyStore` (ADO.NET, Dapper and EF Core on SQL Server, PostgreSQL and MySQL; MongoDB) without a change to any provider code, and the new check runs before the store is touched, so it is identical for all 10 providers. Integration tests run the existing SQL Server scenario for ADO.NET, Dapper and EF Core (the families the package already tests through `PersistentPapScopeScenario`).

### Goal and non-goals

**Goal.** A caller without the configured permission cannot change a policy through either PAP; the refusal is a 403-class `Left`, is audited (persistent PAP), logged and counted; the anonymous rule and the audit-before-apply rule stay as they are; startup seeding is unaffected.

**Non-goals**

- Authorizing **reads** (`GetPolicySetsAsync`, `GetPolicySetAsync`, `GetPoliciesAsync`, `GetPolicyAsync`): the decision point and the health check read the PAP in contexts that have no administrator (choice 10).
- Per-action permissions (`abac:policies:add`, `abac:policies:remove`): one permission now (choice 2); finer granularity is a later feature if the maintainer wants it.
- Tenant-scoped or module-scoped policy administration: `PolicySet` and `Policy` have no `TenantId` or `ModuleId` today (`src/Encina.Security.ABAC/Model`), so the permission is global to the application.
- Changing the HTTP status of the **anonymous** refusal. Today `abac.policy_change_principal_required` has no `encina.authorization.` prefix, so host adapters answer it with 500 instead of 401. The acceptance criteria of #1986 keep that code unchanged; a follow-up [DEBT] issue is drafted by this planning run (see Next Steps).
- A Policy Enforcement Point meta-policy ("ABAC authorizes its own administration"): rejected in choice 1.

### Prerequisite and delivery split (size rule)

[#1985](https://github.com/dlrivada/Encina/issues/1985) (open, `[DEBT]`) gives `InMemoryPolicyAdministrationPoint` a required `IRequestContextAccessor` and the anonymous refusal. Today the in-memory PAP reads no identity at all (`InMemoryPolicyAdministrationPoint.cs`, constructor takes only `ILogger`), so it cannot check a permission. #1986 for the in-memory PAP **depends on #1985**: PR 3 below starts after #1985 is merged. If the maintainer prefers one PR for both, PR 3 also carries #1985's constructor change; the plan does not decide that, it records the dependency (see Prerequisites & Dependencies).

The size rule of AGENTS.md section 11 (one package, at most 15 files and 1500 changed lines per worker PR) is met by splitting the feature into four PRs. Re-measure each at brief time.

| PR | Phases | Closes | Size (estimate) |
|----|--------|--------|-----------------|
| 1 | 1 Core models and contracts, 2 Observability: options, validator, error, authorizer, actor record, instruments, log message, `PublicAPI.Unshipped.txt`, registry comment, and their unit tests | `Refs #1986` | 12 files, ~600 lines |
| 2 | 3 Persistent PAP enforcement, denial audit, DI and seeding identity; 5 Cross-cutting integration tests (tenant on the entry, registration test with `ValidateOnBuild` and `ValidateScopes`) | `Refs #1986` | 10 files, ~500 lines |
| 3 | 4 In-memory PAP enforcement and its DI wiring (after #1985) | `Refs #1986` | 10 files, ~330 lines |
| 4 | 6 Testing (contract and integration) and 7 Documentation and finalization | `Fixes #1986` | 9 files, ~400 lines |

---

## Design Choices

Classes follow the decision register of #2470. Choices 1, 3, 5, 8 and 10 are Settled, choices 6, 7, 11 and 12 are Trivial, and choices 2, 4 and 9 are Dilemmas: each is written with the recommendation marked "(recommended, pending the maintainer)", and the maintainer is asked when work on the issue starts. No `Verified` line is set here: the orchestrator runs the adversarial verification later.

<details>
<summary><strong>1. Source of the permission — the caller's own RequestIdentity, read once</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `RequestIdentity.Permissions` of the same `IRequestContext.Identity` read that yields the actor** | One snapshot: the user id, tenant, kind and permission always come from the same identity (the identity turns anonymous when its scope ends, #1892); no new dependency; works for users and declared service identities; exact, case-insensitive set membership | Needs the identity to carry permissions (the claims map and `AddEncinaServiceIdentity(...).WithPermissions` already do) |
| B) Ask `ISecurityContext` or the security package's `IPermissionEvaluator` | Reuses `DefaultPermissionEvaluator` | `ISecurityContext` is the model #1705 replaced; adds a dependency of `Encina.Security.ABAC` on a second authorization stack; a second read of the caller |
| C) Make ABAC authorize its own administration (a meta-policy evaluated by the PDP) | Policy-driven, no permission string | Bootstrap problem: the first policy cannot be added before a policy exists; a policy-administration error would lock the application out |

### Chosen Option: **A — the identity's own permissions, one read**

### Rationale

- The persistent PAP already reads the identity once per change for the actor (`TryResolveActor`); the permission is one more field of the same snapshot.
- The issue's proposed solution is exactly this ("The PAP checks RequestIdentity.Permissions (read once) before any mutation").
- Option C is the meta-policy question left for a future feature; it cannot be the default because an empty policy store would make every change impossible.

**Class:** Settled
**Settled by:** Issue #1986 ("Settled by elimination (2026-10-17)": a permission checked by both PAPs before any mutation); SPEC-002 DEC-006 (fail closed); `RequestIdentity` contract (`src/Encina/Identity/RequestIdentity.cs`, "Every gate ... reads this same instance")

</details>

<details>
<summary><strong>2. Granularity — one permission for all six mutations</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One option, `PolicyAdministrationPermission`, for every add, update and remove of a policy set or policy** | Matches the issue; one value to configure and to declare on the seeding identity; smallest surface | A reviewer who may update a policy can also remove it |
| B) One permission per action (`...:add`, `...:update`, `...:remove`), or one per entity type | Separation of duties; least privilege per action | Six or three options to validate and to declare on the seeding identity; larger documentation and test matrix; no consumer asked for it |

### Chosen Option: **A — one permission** (recommended, pending the maintainer)

### Rationale

- **Option B wins when** an organization separates the people who may create policies from those who may delete them (for example a two-person rule on removals).
- Option A is what the issue proposes and is the smaller change; B can be added later without breaking A if the options gain optional per-action overrides that default to the single permission.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Default value and opt-out — a non-empty default, no way to turn the check off</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Default `abac:policies:manage`; blank, whitespace-padded or wildcard values fail startup; no property turns the check off** | Secure by default: the gate is on even when the application configures nothing; nothing to forget | An application whose administrators lack the permission must grant it (pre-1.0, acceptable) |
| B) Default `null` meaning "no check", the application opts in by setting a name | No change for existing code | Fail-open by default: exactly the gap #1986 closes |
| C) Default permission plus an explicit, logged opt-out property | Lets a development setup skip the check | A switch that can ship to production; the same goal is reached by granting the permission to the test identity |

### Chosen Option: **A — secure default, no opt-out**

### Rationale

- AGENTS.md section 3 requires gates to fail closed; the only allowed opt-out is "explicit and logged", and none is needed here because the permission name itself is configurable and a test identity can simply hold it.
- The Marten audit resolver plan took the same position for its operator permission (`audit:resolve`, "no opt-out", `docs/plans/audit-projection-resolver-implementation-plan-3049.md`).

**Class:** Settled
**Settled by:** AGENTS.md section 3 (compliance and security gates fail closed); SPEC-002 DEC-006; issue #1986 (secure default posture, example name `abac:policies:manage`); precedent in the plan of #3049 (`audit:resolve`)

</details>

<details>
<summary><strong>4. Where the check lives — inside each PAP, through one shared internal authorizer</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) An internal `PolicyChangeAuthorizer` called by both PAPs, with `PolicyActor` extracted to its own internal file** | One place for the rule and its tests; the persistent PAP keeps one identity read for actor, tenant and permission and can write the `Denied` entry with its own audit scope; the check runs before any store read | A third-party `IPolicyAdministrationPoint` registered by an application does not get the check |
| B) A decorator `AuthorizingPolicyAdministrationPoint` wrapping whatever `IPolicyAdministrationPoint` is registered | Protects custom PAPs too; the two PAPs stay untouched | The decorator cannot reach the persistent PAP's audit scope, so the `Denied` entry needs its own audit store resolution; a second identity read (the identity can turn anonymous between the two reads); the registration order of decorators becomes a new way to bypass the gate |
| C) Duplicate the check in each PAP | No shared type | Two copies of a security rule that drift apart |

### Chosen Option: **A — inline shared authorizer** (recommended, pending the maintainer)

### Rationale

- **Option B wins when** applications commonly register their own `IPolicyAdministrationPoint` and expect Encina to protect it.
- Option A keeps one identity snapshot per change (the invariant the persistent PAP documents) and matches the issue ("Both PAPs ... apply the check").

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Order of checks — authorization before any store read</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Anonymous check, then permission check, then everything else (existence checks, store reads, audit write-ahead, apply)** | A refused caller cannot tell a duplicate id from a missing one (no oracle); no store round trip for a refused call; the denial is the first and only thing that happens | None |
| B) Run the existing duplicate and not-found checks first, then authorize | Slightly simpler diff | Leaks policy existence to a caller who may not administer policies; spends a store read on a refused call |

### Chosen Option: **A — authorize first**

### Rationale

- A gate that answers after it has read the protected data is not fail closed. In the persistent PAP the natural place is `RunAsActorAsync`, which already refuses the anonymous caller before the DI scope for the store is opened.

**Class:** Settled
**Settled by:** AGENTS.md section 3 (compliance and security gates fail closed); SPEC-002 DEC-006; existing order in `PersistentPolicyAdministrationPoint.RunAsActorAsync` (actor check before the scope)

</details>

<details>
<summary><strong>6. Error code — a new ABAC-specific code under the authorization prefix</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ABACErrors.PolicyChangeForbiddenCode = "encina.authorization.abac_policy_change_forbidden"` with a fixed message and the ABAC stage detail** | Follows the package convention `encina.authorization.abac_{reason}` (`ABACErrors.cs` header); the shared prefix makes ASP.NET Core, Azure Functions and AWS Lambda answer 403; tests and logs can tell this denial from others | One more constant and factory |
| B) Reuse `EncinaErrorCodes.AuthorizationPermissionDenied` | No new constant | Not distinguishable from any other permission denial in logs and tests |

### Chosen Option: **A — `encina.authorization.abac_policy_change_forbidden`**

### Rationale

- The message is fixed and carries no caller data; the required permission name is logged (EventId 9041) but not placed in the error, so a 403 body does not advertise the administration permission.

**Class:** Trivial
**Settled by:** Convention in `src/Encina.Security.ABAC/ABACErrors.cs` (codes `encina.authorization.abac_{reason}`, "so every host adapter answers them with HTTP 403 through the shared `encina.authorization.*` prefix rule"); AGENTS.md section 3 (`EncinaError.Message` never reaches logs)

</details>

<details>
<summary><strong>7. Seeding identity — declared with the permission from the final options</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Declare `service:encina.abac.policy-seeding` with `WithPermissions(<final options value>)`, through a catalog configuration that reads `IOptions<ABACOptions>`** | The seeding identity holds exactly the permission the PAP requires, even when the application changes it after `AddEncinaABAC` (for example binding the options from configuration); the existing rule "the gate is the resolved options, never the temporary instance" holds | `ABACPolicySeedingHostedService`'s registration condition (seed lists non-empty) still reads the temporary instance, as today |
| B) Declare it at registration with the temporary options instance | One line | A later `Configure<ABACOptions>` changes the required permission but not the identity's: seeding is refused and startup fails (fail closed, but a confusing failure) |
| C) Exempt the seeding identity from the check | No declaration | A bypass keyed on an identity name; the issue asks for the identity to be "accepted", not exempt |

### Chosen Option: **A — final options**

### Rationale

- `ServiceIdentityCatalogOptions.Declare` and `ServiceIdentityBuilder.Build` are internal to the core package and visible to `Encina.Security.ABAC` (`InternalsVisibleTo`, `Encina.csproj`), so no core change is needed.
- The catalog rejects a wildcard permission (`ServiceIdentityCatalogOptionsValidator`), which is why the options validator rejects `*` as well: the failure appears once, with the ABAC option name.

**Class:** Trivial
**Settled by:** `ServiceCollectionExtensions.cs` comment "the gate is the resolved IOptions<ABACOptions>, never the temporary options instance above"; AGENTS.md section 3 (registration completeness: every dependency an `AddEncina*` method needs is registered, proven by a `ValidateOnBuild` and `ValidateScopes` test)

</details>

<details>
<summary><strong>8. Evidence of a refusal in the persistent PAP — one Denied audit entry, result never changed by its write</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Write one `OperationAuditEntry` with `AuditOutcome.Denied` (action, entity type and id, caller id, tenant, correlation id, metadata `source`, `actor` kind and `requiredPermission`) in its own DI scope; a failed or throwing audit write is logged with the existing EventIds 9094 and 9095 and the caller still gets the `Forbidden` error** | Satisfies "Audit entry recorded for refused policy change"; the denial is already the safe outcome, so a failing audit write cannot make it safer or less safe; the caller sees the real reason | An attacker who spams refused calls can fill the audit store (volume, noted in docs) |
| B) Fail the refusal with `abac.policy_change_audit_failed` when the `Denied` write fails | Symmetric with the success path | Hides the 403 behind a 500 and tells the caller nothing true; the change was refused either way |
| C) Log and count only, no audit entry | Cheap | Fails the acceptance criterion and loses the security evidence |

### Chosen Option: **A — best-effort `Denied` entry, `Forbidden` result**

### Rationale

- The success path is fail closed because an unaudited change would be applied; a refusal applies nothing, so there is nothing to protect by failing the audit.
- When no `IOperationAuditStore` is registered the existing 9097 warning (changes without an audit record) already says so; the refusal is still logged and counted.

**Class:** Settled
**Settled by:** Issue #1986 acceptance criteria ("Audit entry recorded for refused policy change"); AGENTS.md section 6 row 12 (audit trail) and section 3 (errors never swallowed: the write failure is logged with its code); precedent in the plan of #3049 ("security-relevant denials are audited as one Denied entry")

</details>

<details>
<summary><strong>9. Evidence of a refusal in the in-memory PAP — log, counter and activity, or also an audit entry</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Log (EventId 9041), counter and activity only** | The in-memory PAP audits nothing today, not even successful changes; no new constructor dependency; pay for what you use; documented as "not suitable for production" | The acceptance criterion "Audit entry recorded for refused policy change" is met by the persistent PAP only |
| B) Give the in-memory PAP an optional `IOperationAuditStore` and write the `Denied` entry | Same evidence in both PAPs | An audit store for a PAP that otherwise keeps no audit trail; a new scoped-service resolution problem in a singleton; half-audited (refusals recorded, successes not) |

### Chosen Option: **A — telemetry only in the in-memory PAP** (recommended, pending the maintainer)

### Rationale

- **Option B wins when** a team runs a compliance test suite on the in-memory PAP and asserts the audit entry, or ships the in-memory PAP to production with an audit store.
- The contract test of Phase 6 asserts the same `Left` code and the same log and counter from both PAPs; only the `Denied` audit entry differs, and it is documented.

**Class:** Dilemma

</details>

<details>
<summary><strong>10. Reads stay open — only the six mutations are authorized</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Authorize the six mutations only** | Matches the issue ("before any mutation"); the policy decision point (`XACMLPolicyDecisionPoint`) and `ABACHealthCheck` read `IPolicyAdministrationPoint` for every request or probe, in contexts without an administrator | Anyone able to call `GetPolicySetsAsync` can read policy content |
| B) Also require the permission for reads | Policy content is protected | Breaks the decision point and the health check unless they run under a privileged identity, which would be a much larger change and would put an administrator permission on every request path |

### Chosen Option: **A — mutations only**

### Rationale

- Reads are an information-disclosure topic, not the privilege-escalation gap #1986 describes; a read-protection design would need a separate read interface for the decision point and is not part of this issue.

**Class:** Settled
**Settled by:** Issue #1986 (title and motivation: "authorizes the authenticated actor before allowing any policy mutations"); consumers verified in `src/Encina.Security.ABAC/Evaluation/XACMLPolicyDecisionPoint.cs` and `Health/ABACHealthCheck.cs`

</details>

<details>
<summary><strong>11. Telemetry names — follow the existing abac.pap.* and ABAC.PAP.* conventions</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Counter `abac.pap.change.denied` (tags `abac.operation`, `abac.entity_type`), activity `ABAC.PAP.Authorize` (tags `abac.permission_required`, `abac.outcome`), on the existing `Encina.Security.ABAC` source and meter** | Same names as the neighbours (`abac.pap.save.total`, `ABAC.PAP.Save`); one meter to subscribe to | Differs from the names the issue proposed (`encina.abac.policy_change_authorization_denied_total`, `encina.abac.policy_change_authorization`), which the issue marks as a proposed shape |
| B) The issue's names | Literal reading of the issue | A second naming scheme inside one meter; the package has no `encina.abac.` instrument today |

### Chosen Option: **A — package conventions**

### Rationale

- Tags carry enumeration names, the operation and the entity type only; never the caller id, tenant or the caller's permissions (the rule stated above the decision-audit instruments in `ABACDiagnostics.cs`, SPEC-002 REQ-062).

**Class:** Trivial
**Settled by:** `src/Encina.Security.ABAC/Diagnostics/ABACDiagnostics.cs` (instrument and activity naming, tag rule); AGENTS.md section 6 rows 2 and 3

</details>

<details>
<summary><strong>12. Matching — exact, case-insensitive set membership, no wildcard</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `identity.Permissions.Contains(required)` (the set is case-insensitive); no wildcard expansion** | Same semantics as `DefaultPermissionEvaluator` and the identity model; the service-identity catalog already rejects `*`, so a wildcard can never be declared | An application cannot grant `abac:*` |
| B) Support hierarchical wildcards (`abac:*`) | Fewer grants | A new matching language to specify and test; wildcards on a security gate widen the grant by accident |

### Chosen Option: **A — exact match**

### Rationale

- The behavior is the one the identity model already defines; adding a matcher is a separate decision for the whole authorization stack.

**Class:** Trivial
**Settled by:** `src/Encina/Identity/RequestIdentity.cs` (`Permissions` is a case-insensitive set) and `ServiceIdentityCatalogOptionsValidator` (no wildcard in a role or permission); AGENTS.md section 3 (gates fail closed)

</details>

---

## Implementation Phases

### Phase 1: Core Models & Contracts

<details>
<summary>Tasks</summary>

1. `src/Encina.Security.ABAC/ABACOptions.cs` (modify): add `public string PolicyAdministrationPermission { get; set; } = "abac:policies:manage";` with XML docs naming the six guarded operations, the seeding identity and the fail-closed behavior; `<example>` with `options.PolicyAdministrationPermission = "ops:policies";`.
2. `src/Encina.Security.ABAC/DecisionAudit/ABACOptionsValidator.cs` (modify): one more rule in the existing `rules` array: the value is not null, empty or whitespace, equals its trimmed form, and contains no `*` (message names `ABACOptions.PolicyAdministrationPermission`). The class keeps its name and namespace.
3. `src/Encina.Security.ABAC/ABACErrors.cs` (modify): `public const string PolicyChangeForbiddenCode = "encina.authorization.abac_policy_change_forbidden";` and `public static EncinaError PolicyChangeForbidden()` built like `PolicyChangePrincipalRequired()` (fixed message, `MetadataKeyStage = MetadataStageAbac`, no caller data).
4. `src/Encina.Security.ABAC/Administration/PolicyActor.cs` (new): `internal readonly record struct PolicyActor(string UserId, string? TenantId, string CorrelationId, IdentityKind Kind)` moved out of `PersistentPolicyAdministrationPoint` with its `ToString()` that prints the kind only.
5. `src/Encina.Security.ABAC/Administration/PolicyChangeAuthorizer.cs` (new): `internal sealed class PolicyChangeAuthorizer(string requiredPermission)` with `Either<EncinaError, PolicyActor> Authorize(IRequestContext? context, out PolicyRefusal refusal)` or an equivalent that returns the actor on success and, on failure, which refusal it is (`Anonymous` or `Forbidden`) plus the identity kind and tenant needed for the audit entry. Reads `context?.Identity` once; anonymous or no user id returns `ABACErrors.PolicyChangePrincipalRequired()`; missing permission returns `ABACErrors.PolicyChangeForbidden()`. Complexity kept at or below 4 so CRAP stays at or below 10 without tests of every branch order.
6. `src/Encina.Security.ABAC/PublicAPI.Unshipped.txt` (modify, via `mechanical-fixer`): `ABACOptions.PolicyAdministrationPermission.get/set`, `ABACErrors.PolicyChangeForbiddenCode`, `ABACErrors.PolicyChangeForbidden()`. The authorizer and actor are internal.
7. Unit tests: `PolicyChangeAuthorizerTests` (anonymous, user without permission, user with permission, service with permission, case-insensitive permission, null context, tenant and correlation carried), validator tests for the new rule, error factory test (code, fixed message, no caller data).

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT
Issue #1986 adds an actor-permission check to the ABAC Policy Administration Point. This phase adds the
option, its validation, the error and the shared internal decision type. No PAP code changes yet.
Package: src/Encina.Security.ABAC. .NET 10 / C# 14, nullable enabled. Today PersistentPolicyAdministrationPoint
resolves the actor in TryResolveActor (IRequestContextAccessor.RequestContext.Identity, read once).

TASK
1. Add ABACOptions.PolicyAdministrationPermission (default "abac:policies:manage") with XML docs and an example.
2. Add one rule to ABACOptionsValidator: not blank, equals its trimmed form, no '*'.
3. Add ABACErrors.PolicyChangeForbiddenCode = "encina.authorization.abac_policy_change_forbidden" and
   PolicyChangeForbidden() (fixed message, stage detail, no caller data), modelled on PolicyChangePrincipalRequired().
4. Move the nested PolicyActor record struct of PersistentPolicyAdministrationPoint to Administration/PolicyActor.cs
   (internal, ToString prints the kind only) and update the persistent PAP to use it (no behavior change).
5. Create Administration/PolicyChangeAuthorizer.cs: given the ambient IRequestContext? and the required
   permission, read Identity ONCE and return the PolicyActor, or ABACErrors.PolicyChangePrincipalRequired() for an
   anonymous caller, or ABACErrors.PolicyChangeForbidden() when identity.Permissions does not contain the permission.
6. Unit tests (UnitTests/Security/ABAC) with Encina.Testing TestIdentity builders and Shouldly through
   Encina.Testing.Shouldly: every outcome, case-insensitive match, null context, validator rule and error factory.

KEY RULES
- AGENTS.md section 3: gates fail closed; EncinaError.Message never reaches logs; ROP (Either<EncinaError, T>).
- No [Obsolete], no compatibility shims. PublicAPI lines go to PublicAPI.Unshipped.txt (mechanical-fixer owns them).
- Keep cyclomatic complexity of every new method low (CRAP <= 10 on changed methods).
- Tests execute the real classes; no reflection-only tests; AAA; no Thread.Sleep.
- Do not touch InMemoryPolicyAdministrationPoint here.

REFERENCE FILES
- src/Encina.Security.ABAC/Administration/PersistentPolicyAdministrationPoint.cs (TryResolveActor, PolicyActor)
- src/Encina.Security.ABAC/ABACErrors.cs (PolicyChangePrincipalRequired, code constants)
- src/Encina.Security.ABAC/DecisionAudit/ABACOptionsValidator.cs
- src/Encina/Identity/RequestIdentity.cs (Permissions, IsAuthenticated, Kind)
- tests/Encina.UnitTests/Security/ABAC (existing PAP and validator tests)
- docs/plans/abac-pap-actor-authorization-implementation-plan-1986.md (choices 1-6, 12)
```

</details>

### Phase 2: Observability

<details>
<summary>Tasks</summary>

1. `src/Encina.Security.ABAC/Diagnostics/ABACDiagnostics.cs` (modify): `internal static readonly Counter<long> PapChangeDenied = Meter.CreateCounter<long>("abac.pap.change.denied", description: ...)`; `StartPolicyChangeAuthorization()` returning `ActivitySource.StartActivity("ABAC.PAP.Authorize", ActivityKind.Internal)`; a `RecordPolicyChangeDenied(string operation, string entityType)` helper that increments the counter with the tags `abac.operation` and `abac.entity_type`; tag constant `TagPermissionRequired = "abac.permission_required"`. The activity sets `abac.outcome` to `granted`, `forbidden` or `anonymous`; it never sets the caller id, tenant or permissions.
2. `src/Encina.Security.ABAC/Diagnostics/ABACLogMessages.cs` (modify): `PolicyChangeDenied(ILogger, string operation, string entityType, string entityId, string requiredPermission)` with `EventId = 9041`, `LogLevel.Warning`, in a new group comment "PAP authorization (9041)". It carries no caller id and no error message.
3. `src/Encina/Diagnostics/EventIdRanges.cs` (modify, comment only): remove 9041 from the "Unused ids to reuse first" list and add "Used: 9041 by the policy administration authorization of #1986".
4. Run `EncinaEventIdAllocationTests` (the `Encina.Security.ABAC` assembly is already in its `AssemblyRanges` map).
5. Unit tests with a `MeterListener` and an `ActivityListener` (as the existing PAP diagnostics tests do): counter value and tags, activity name and tags, and that no tag value equals the caller id.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT
Phase 1 of issue #1986 added the authorizer. This phase adds the telemetry the PAP will emit when it refuses
a policy change. EventIds of Encina.Security.ABAC live in 9000-9099 (EventIdRanges.SecurityABAC); 9041 is free.

TASK
1. In Diagnostics/ABACDiagnostics.cs add counter "abac.pap.change.denied" (Counter<long>), the activity
   "ABAC.PAP.Authorize", the tag name "abac.permission_required" and a helper that records a denial with tags
   abac.operation and abac.entity_type.
2. In Diagnostics/ABACLogMessages.cs add one [LoggerMessage] PolicyChangeDenied with EventId 9041, Warning,
   parameters operation, entityType, entityId, requiredPermission (no caller id, no message of an error).
3. Update the comment of EventIdRanges.SecurityABAC in src/Encina/Diagnostics/EventIdRanges.cs ("Used: 9041 ...",
   drop 9041 from the free list).
4. Unit tests with MeterListener / ActivityListener that assert the counter increments with the expected tags
   and that no tag or log argument carries the caller id, tenant or the caller's own permissions.

KEY RULES
- AGENTS.md section 7: never use an EventId outside the registered range; pack sequentially; run the
  architecture tests (EncinaEventIdAllocationTests).
- AGENTS.md section 3: EncinaError.Message never reaches logs, tags or activities; exceptions only via ForLogging().
- Use [LoggerMessage] source generation, not LoggerMessage.Define.
- Names follow the package (abac.pap.*, ABAC.PAP.*); do not introduce an "encina.abac." scheme.

REFERENCE FILES
- src/Encina.Security.ABAC/Diagnostics/ABACDiagnostics.cs (PapSaveTotal, StartPapSave helper pattern)
- src/Encina.Security.ABAC/Diagnostics/ABACLogMessages.cs
- src/Encina/Diagnostics/EventIdRanges.cs (SecurityABAC)
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
- docs/plans/abac-pap-actor-authorization-implementation-plan-1986.md (choice 11)
```

</details>

### Phase 3: Persistent PAP Enforcement, Denial Audit, DI and Seeding Identity

<details>
<summary>Tasks</summary>

1. `Administration/PersistentPolicyAdministrationPoint.cs` (modify): the public constructor gains the required permission (a `string policyAdministrationPermission` parameter; the class builds its internal `PolicyChangeAuthorizer` from it, because the authorizer is internal and cannot appear in a public signature). The exact signature is fixed in the brief and recorded in `PublicAPI.Unshipped.txt`. `TryResolveActor` is replaced by the authorizer call inside `RunAsActorAsync`, **before** `_scopeFactory.CreateAsyncScope()`.
2. On a refusal: `ABACDiagnostics` counter and activity, `ABACLogMessages.PolicyChangeDenied` (9041), and `RecordDeniedAsync` that builds an `OperationAuditEntry` with `Outcome = AuditOutcome.Denied`, `ErrorMessage = ABACErrors.PolicyChangeForbiddenCode`, `UserId`, `TenantId`, `CorrelationId` from the refusal, the same `Action`/`EntityType`/`EntityId` as the change that was refused (the change description is built before the check so the entry names it), and metadata `source`, `actor` kind and `requiredPermission`. The entry is written in a separate DI scope through the existing `RecordAuditAsync` (timeout and `TimeProvider` already handled), ignoring caller cancellation like `RecordFailedChangeAsync`. A failed write is logged by the existing 9094/9095 and does not change the returned `Forbidden`. An anonymous refusal writes no entry (there is no caller to attribute it to), as today.
3. `ServiceCollectionExtensions.cs` (modify): `CreatePersistentPolicyAdministrationPoint` reads `IOptions<ABACOptions>` and passes the permission; the seeding identity is declared with the permission from the final options through `services.AddOptions<ServiceIdentityCatalogOptions>().Configure<IOptions<ABACOptions>>(...)` using the internal `ServiceIdentityBuilder` and `Declare` (choice 7), still only when a seed list is non-empty; the XML remarks list the new behavior.
4. `ABACPolicySeedingHostedService.cs` is not changed (the identity name stays). Its remark on "does not authorize" is updated in the same PR if it appears in XML docs.
5. Update the existing persistent PAP unit tests (`PersistentPolicyAdministrationPoint*Tests.cs`, four files) so their test identities hold the permission.
6. New `PersistentPolicyAdministrationPointAuthorizationTests`: user without the permission refused with the new code for each of the six mutations; nothing reaches the store (strict NSubstitute store); a `Denied` entry is written with tenant, correlation id, caller id and `requiredPermission`; the entry never contains the caller's permissions; a failing audit store does not change the `Forbidden` result and is logged with 9094; a user with the permission is accepted; the seeding identity is accepted; anonymous keeps `abac.policy_change_principal_required` and writes no entry.
7. Registration test (`ValidateOnBuild` and `ValidateScopes`): `AddEncinaABAC` with `UsePersistentPAP` and seeds builds; a custom permission reaches both the PAP and the seeding identity; a blank permission fails the build with the validator message.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT
Issue #1986. Phases 1-2 added ABACOptions.PolicyAdministrationPermission, ABACErrors.PolicyChangeForbidden,
PolicyChangeAuthorizer, PolicyActor and the telemetry (counter, activity, EventId 9041). The persistent PAP is a
singleton; it opens a DI scope per operation and a separate scope for the audit store.

TASK
1. PersistentPolicyAdministrationPoint: take the required permission (public constructor shape decided in the
   brief), build a PolicyChangeAuthorizer, and call it at the start of RunAsActorAsync, before any scope is opened
   and before any store read. Replace TryResolveActor.
2. On a forbidden caller: increment the counter, log EventId 9041, write ONE OperationAuditEntry with
   AuditOutcome.Denied (ErrorMessage = the error code; metadata source, actor kind, requiredPermission; UserId,
   TenantId, CorrelationId from the same identity read) in its own DI scope with the existing RecordAuditAsync, and
   return ABACErrors.PolicyChangeForbidden(). A failed audit write is logged (9094/9095) and never changes the result.
   An anonymous caller keeps abac.policy_change_principal_required and writes no entry.
3. ServiceCollectionExtensions: pass the permission from IOptions<ABACOptions> to the persistent PAP; declare the
   seeding identity service:encina.abac.policy-seeding with WithPermissions(<final option value>) through an
   options configuration that depends on IOptions<ABACOptions> (internal APIs are visible to this package).
4. Update the existing persistent PAP tests so their identities hold the permission; add the authorization tests
   and the DI registration test (ValidateOnBuild + ValidateScopes) described in the plan.

KEY RULES
- AGENTS.md section 3: gates fail closed; errors are never swallowed (log the audit failure with its code);
  EncinaError.Message never reaches logs; DB calls async with CancellationToken; time from TimeProvider.
- The identity is read ONCE per change; do not re-read it for the audit entry.
- The permission name may be logged; the caller's permissions, id and tenant never go to logs, tags or activities.
- Registration completeness: the seeding identity must work in any registration order.
- No [Obsolete], no compatibility overloads. PublicAPI.Unshipped.txt lines go through mechanical-fixer.
- Tests: Shouldly via Encina.Testing.Shouldly, TestIdentity builders, NSubstitute; no Thread.Sleep.

REFERENCE FILES
- src/Encina.Security.ABAC/Administration/PersistentPolicyAdministrationPoint.cs
- src/Encina.Security.ABAC/ServiceCollectionExtensions.cs (AddStartupServices, CreatePersistentPolicyAdministrationPoint)
- src/Encina.Security.ABAC/ABACPolicySeedingHostedService.cs
- src/Encina/Identity/ServiceIdentityCatalogOptions.cs, ServiceIdentityBuilder.cs, RequestIdentityServiceCollectionExtensions.cs
- tests/Encina.UnitTests/Security/ABAC/Persistence/PersistentPolicyAdministrationPointAuditTests.cs
- tests/Encina.UnitTests/Security/ABAC/ABACPolicySeedingHostedServiceTests.cs
- docs/plans/abac-pap-actor-authorization-implementation-plan-1986.md (choices 4, 5, 7, 8)
```

</details>

### Phase 4: In-Memory PAP Enforcement

<details>
<summary>Tasks</summary>

1. Precondition: [#1985](https://github.com/dlrivada/Encina/issues/1985) is merged (`InMemoryPolicyAdministrationPoint` takes a required `IRequestContextAccessor`). If it is not, this PR carries that constructor change and its anonymous test first; the brief states which.
2. `Administration/InMemoryPolicyAdministrationPoint.cs` (modify): the constructor takes the required permission (same public shape as the persistent PAP) and builds the authorizer; each of the six mutation methods starts with the authorizer call, before any dictionary access; on refusal it emits the counter, the activity and the 9041 log (choice 9) and returns the same error as the persistent PAP. No audit entry (choice 9, pending the maintainer).
3. `ServiceCollectionExtensions.cs` (modify): the in-memory registration passes the permission from `IOptions<ABACOptions>` (`TryAddSingleton` with a factory, keeping the existing `TryAdd` semantics so an application's own `IPolicyAdministrationPoint` still wins).
4. Update the existing tests that construct the in-memory PAP: `ABACBuiltInSubjectAttributeTargetTests`, `AttributeDesignatorSelectionTests`, `XACMLPolicyDecisionPointNamedPolicyTests`, `XACMLPolicyDecisionPointContractTests`, `InMemoryPolicyAdminGuardTests`.
5. New `InMemoryPolicyAdministrationPointAuthorizationTests`: each of the six mutations refused without the permission, accepted with it, anonymous refused with `abac.policy_change_principal_required`, the store untouched on refusal, counter and log emitted.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT
Issue #1986, after #1985 (the in-memory PAP reads the request identity). Phases 1-3 are merged: the options,
the authorizer, the error, the telemetry and the persistent PAP enforcement exist. The in-memory PAP keeps no
audit trail and is documented as "not suitable for production".

TASK
1. InMemoryPolicyAdministrationPoint: take the required permission, build a PolicyChangeAuthorizer and call it
   first in AddPolicySetAsync, UpdatePolicySetAsync, RemovePolicySetAsync, AddPolicyAsync, UpdatePolicyAsync and
   RemovePolicyAsync. On a refusal emit the counter, activity and the EventId 9041 log, and return the same Left as
   the persistent PAP. Do not add an audit store (unless the maintainer chose option B of choice 9).
2. ServiceCollectionExtensions: register the in-memory PAP with a factory that passes the permission from
   IOptions<ABACOptions> (TryAdd semantics preserved).
3. Update every existing test that constructs the in-memory PAP (list in the plan) so its identity holds the
   permission; add InMemoryPolicyAdministrationPointAuthorizationTests.

KEY RULES
- AGENTS.md section 3: gates fail closed; EncinaError.Message never reaches logs; ROP.
- Authorize BEFORE any dictionary access so a refused caller learns nothing.
- Same error code, same log and counter as the persistent PAP; ContractTests (Phase 6) will assert it.
- No [Obsolete], no compatibility overloads; PublicAPI.Unshipped.txt via mechanical-fixer.
- Time and clocks are not involved; do not add DateTime.UtcNow.

REFERENCE FILES
- src/Encina.Security.ABAC/Administration/InMemoryPolicyAdministrationPoint.cs
- src/Encina.Security.ABAC/Administration/PolicyChangeAuthorizer.cs (Phase 1)
- src/Encina.Security.ABAC/ServiceCollectionExtensions.cs (AddPolicyAdministrationPoint)
- tests/Encina.GuardTests/Security/ABAC/Administration/InMemoryPolicyAdminGuardTests.cs
- tests/Encina.UnitTests/Security/ABAC/Administration/InMemoryPolicyAdministrationPointNotFoundTests.cs
- docs/plans/abac-pap-actor-authorization-implementation-plan-1986.md (choices 4, 9)
```

</details>

### Phase 5: Cross-Cutting Integration

<details>
<summary>Tasks</summary>

1. Multi-tenancy (function 10): tests prove the `Denied` entry of a caller in tenant `t1` carries `TenantId = "t1"`, and that a caller with no tenant yields `null`, like the success entries; the permission itself is global (non-goal).
2. Audit trail (function 12): the `Denied` entry uses the existing `OperationAuditEntry` fields and the `Metadata` keys of the success entries (`source`, `actor`); it is a separate entry from the write-ahead/failure pair of a change, and an audit store that throws does not escape.
3. Validation (function 5): the options validator rule is exercised through `ValidateOnStart` in the registration test of Phase 3 (a blank, padded or wildcard permission fails the host build with the option name in the message).
4. Registration completeness (AGENTS.md section 3): one test builds the provider with `ValidateOnBuild` and `ValidateScopes` for both PAPs with and without seeds, and with an application-registered `IPolicyAdministrationPoint` registered first (still wins).
5. No new hosted service, store, pipeline behavior or external integration is added, so functions 4, 6, 7, 8, 9 and 11 stay not applicable (see the matrix).

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT
Issue #1986. The PAP enforcement of Phases 3-4 exists. This phase proves the cross-cutting claims of the plan's
matrix with tests; it adds no production feature.

TASK
1. Add unit tests that the Denied OperationAuditEntry carries TenantId (value and null), CorrelationId, UserId,
   metadata source/actor/requiredPermission and never the caller's own permissions.
2. Add the ValidateOnBuild + ValidateScopes registration tests: persistent PAP with seeds, in-memory PAP with seeds,
   an application-registered IPolicyAdministrationPoint registered before AddEncinaABAC (it wins), and an invalid
   permission that fails the build.
3. If a gap appears (a field missing on the entry, a service that does not resolve), fix the production code in
   the file that owns it and say so in the commit message.

KEY RULES
- AGENTS.md section 3: registration completeness and fail-closed gates; section 6: the 12-function matrix.
- Tests execute real classes (real PAP, real catalog, real authorizer); use TestIdentity builders.
- Shouldly via Encina.Testing.Shouldly; no Thread.Sleep; deterministic (FakeTimeProvider where time matters).

REFERENCE FILES
- tests/Encina.UnitTests/Security/ABAC/ (registration and seeding tests)
- src/Encina.Security.ABAC/ServiceCollectionExtensions.cs
- src/Encina/Identity/ServiceIdentityCatalog.cs
- docs/plans/abac-pap-actor-authorization-implementation-plan-1986.md (matrix)
```

</details>

### Phase 6: Testing

<details>
<summary>Tasks</summary>

1. **Contract tests** (`tests/Encina.ContractTests/Security/ABAC/PolicyAdministrationAuthorizationContractTests.cs`, new): one theory over both PAP implementations (`InMemoryPolicyAdministrationPoint`, `PersistentPolicyAdministrationPoint` over an in-memory store double) asserting for each of the six mutations the same `Left` code for a caller without the permission, the same success for a caller with it, the same anonymous code, and that the store (or dictionaries) is unchanged after a refusal.
2. **Guard tests** (modify `InMemoryPolicyAdminGuardTests`, `PersistentPAPGuardTests`): the new constructor parameter rejects null/blank.
3. **Integration tests** (modify `tests/Encina.IntegrationTests/Security/ABAC/PersistentPapScopeScenario.cs`, plus one denied-caller scenario): on the existing SQL Server collections for ADO.NET, Dapper and EF Core, a user without the permission is refused for add/update/remove and the database rows are unchanged; a user with the permission changes them; seeding under the built-in identity still succeeds; a `Denied` row appears in the operation audit store. They use the shared `[Collection]` fixtures and `ClearAllDataAsync` in `InitializeAsync`; MySQL, PostgreSQL and MongoDB add no value because the check runs before `IPolicyStore` and the store contract tests already cover those providers.
4. **Property tests**: not planned. The decision is one set membership; unit and contract tests cover every branch. The manifest records `property` as not applicable with this reason.
5. **Load and benchmark tests**: not planned (a set lookup per mutation, mutations are rare administrative calls); no justification file is needed because neither project has an ABAC PAP folder.
6. **Per-file coverage obligations** (`.github/coverage-manifest/Encina.Security.ABAC.json`, entries written by `mechanical-fixer`; never lower an existing target):

| File | Flag targets | One-sentence justification |
|------|--------------|----------------------------|
| `Administration/PolicyChangeAuthorizer.cs` (new) | unit 95, guard 30 | Every outcome and the null context are unit tests; the guard flag only covers the constructor argument guard |
| `Administration/PolicyActor.cs` (new) | unit 90, guard 0 | A data record with a kind-only `ToString`; no parameters to guard |
| `Administration/PersistentPolicyAdministrationPoint.cs` | unit 90 (kept), guard 5 (kept), integration 50 (from 45) | The denied paths add unit coverage and the SQL Server scenario now exercises a refusal and its `Denied` row |
| `Administration/InMemoryPolicyAdministrationPoint.cs` | unit 90, guard 30, contract 60 | Six guarded mutations are unit tested; guards cover the constructor and arguments; the contract theory covers the success and refusal paths |
| `ABACOptions.cs` | unit 80 (kept), guard 95 (kept) | The new property is a plain accessor covered by the existing option tests |
| `DecisionAudit/ABACOptionsValidator.cs` | unit 95 (kept), guard 95 (kept) | One more rule with a passing and three failing values |
| `ABACErrors.cs` | unit 85 (kept), guard 90 (kept) | The new factory is built and checked like its neighbours |
| `Diagnostics/ABACDiagnostics.cs` | unit 75 (kept), guard 70 (kept) | The new counter and activity are driven with listeners |
| `ServiceCollectionExtensions.cs` | unit 75 (kept), guard 60 (kept) | The registration tests build both PAPs and the seeding identity |

7. CRAP: `PolicyChangeAuthorizer.Authorize` and the two `Authorize`-calling mutation wrappers stay at complexity 4 or below; measure the local crap-gate table before pushing.
8. Measure from the repository root: `dotnet test tests\Encina.UnitTests --collect "XPlat Code Coverage" --results-directory artifacts\coverage\UnitTests` (and the guard, contract and integration projects), then `dotnet run --file .github/scripts/coverage-report.cs -- --input artifacts/coverage --output artifacts/coverage-report` and `-- --check-justifications`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT
Issue #1986. Production code of Phases 1-5 is merged. This phase adds contract and integration tests, adjusts
guard tests and records the per-file coverage obligations.

TASK
1. Create ContractTests/Security/ABAC/PolicyAdministrationAuthorizationContractTests.cs: one theory over both
   PAP implementations for the six mutations: same Left code without the permission, success with it, anonymous
   code unchanged, storage untouched after a refusal. Instantiate the real PAPs.
2. Update the two guard test files for the new constructor parameter (null, empty, whitespace).
3. Extend IntegrationTests/Security/ABAC/PersistentPapScopeScenario.cs and add a denied-caller scenario that runs
   on the existing SQL Server collections for ADO.NET, Dapper and EF Core ([Collection("ADO-SqlServer")] and
   siblings, ClearAllDataAsync in InitializeAsync; never a per-class fixture).
4. Hand the manifest entries of the plan's coverage table to mechanical-fixer; measure each flag and report it.

KEY RULES
- AGENTS.md section 9: tests execute real package code; shared collection fixtures; Shouldly via
  Encina.Testing.Shouldly; targets never go down; CRAP <= 10 on changed methods.
- Tests of a provider-dependent feature cover all 10 providers: here the check runs before IPolicyStore, so the
  existing store contract tests cover provider differences; state this in the PR.
- No Thread.Sleep, deterministic, resources cleaned up.

REFERENCE FILES
- tests/Encina.ContractTests/Security/ABAC/PersistentPAPContractTests.cs
- tests/Encina.IntegrationTests/Security/ABAC/PersistentPapScopeScenario.cs
- tests/Encina.GuardTests/Security/ABAC/PersistentPAPGuardTests.cs
- .github/coverage-manifest/Encina.Security.ABAC.json
- docs/testing/coverage-measurement-methodology.md
```

</details>

### Phase 7: Documentation & Finalization

<details>
<summary>Tasks</summary>

1. XML docs on `ABACOptions.PolicyAdministrationPermission`, `ABACErrors.PolicyChangeForbidden*`, both PAP constructors and the remarks of `IPolicyAdministrationPoint` (mutations are authorized by the PAP implementation); update the persistent PAP remark "This PAP records the actor but does not authorize it".
2. Docs (via `docs-writer`, which runs `docs-reviewer`): `docs/features/abac/reference/persistent-pap.md` (replace the line "records the actor but does not authorize it" and add the permission, the seeding identity, the audit entry and its volume note), `configuration.md` (new option row and the registration note), `errors.md` (new code row, HTTP 403, and the existing anonymous code's current 500 mapping noted until the follow-up lands), `observability.md` (counter, activity, EventId 9041). `docs/features/abac/reference/decision-audit.md` line 148 is read and changed only if it repeats the old sentence. A configuration guide section "Who may administer policies" with a Mermaid flow (caller, authorizer, PAP, audit) is added to `persistent-pap.md`.
3. `src/Encina.Security.ABAC/README.md` (via `docs-writer`): one paragraph and a snippet.
4. Changelog fragment `changelog.d/1986-pap-actor-authorization.added.md` (via `mechanical-fixer`), one bullet: "ABAC policy administration: `ABACOptions.PolicyAdministrationPermission` (default `abac:policies:manage`) is required to add, update or remove a policy set or policy in the in-memory and persistent PAP; refusals are audited (persistent PAP), logged (EventId 9041) and counted."
5. `PublicAPI.Unshipped.txt` complete (via `mechanical-fixer`); no `docs/INVENTORY.md`, ROADMAP or release-notes change (no new package or module); no ADR (the decisions are in this plan and SPEC-002 DEC-006 already states the rule).
6. Build and test verification: `dotnet build Encina.slnx --configuration Release` has 0 errors and 0 warnings; `dotnet test` passes; every coverage flag reaches its own target.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```text
CONTEXT
Issue #1986 is implemented and tested. This phase finishes documentation and records.

TASK
1. Review the XML docs of every new or changed public symbol (summary, remarks, param, returns, example) and
   remove the claim that the PAP "does not authorize" the actor.
2. Delegate pages under docs/ and the package README to docs-writer (persistent-pap.md, configuration.md,
   errors.md, observability.md, src/Encina.Security.ABAC/README.md): the new option, the seeding identity, the
   forbidden code (403), the audit entry, telemetry and EventId 9041, with a Mermaid flow and no hand-typed
   coverage figures.
3. Delegate the changelog fragment (changelog.d/1986-pap-actor-authorization.added.md), PublicAPI lines and
   coverage-manifest entries to mechanical-fixer with the exact lines decided here.
4. Run the build, the tests, the coverage report and the CRAP table; fix every gap.

KEY RULES
- AGENTS.md section 8: zero warnings, PublicAPI tracked, XML docs on every public API.
- AGENTS.md section 11: never edit CHANGELOG.md; fragments go to changelog.d/ via mechanical-fixer.
- Documentation cites coverage with cov: markers, never typed figures; English only; no private application names.
- Self-review with adversarial-reviewer on the diff before reporting.

REFERENCE FILES
- docs/features/abac/reference/persistent-pap.md (section "Policy Change Principal and Audit Trail")
- docs/features/abac/reference/{configuration,errors,observability}.md
- changelog.d/README.md
- .claude/skills/encina-docs/SKILL.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard / specification | Relevance |
|--------------------------|-----------|
| SPEC-002 DEC-006 | Compliance and security gates fail closed: a missing permission denies, there is no silent opt-out |
| ADR-006, ADR-001 | The check returns `Either<EncinaError, Unit>`; no exception for a business refusal |
| ADR-018 | The 12-function cross-cutting evaluation recorded in the matrix below |
| ADR-021 | EventId 9041 inside the registered `SecurityABAC` range |
| NIST SP 800-162 (ABAC guide) | The PAP creates and manages policy, so its callers need their own authorization |
| OASIS XACML 3.0 | Defines the PAP role; the specification leaves PAP access control to the implementation |
| NIS2 Art. 10 and SOX section 404 (as cited in the persistent PAP remarks) | Policy changes must be attributable and auditable; this feature adds the "who may" half |
| GDPR Art. 32 | Access control over the rules that govern access to personal data is part of appropriate security measures |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `RequestIdentity` (`Permissions`, `IsAuthenticated`, `Kind`) | `src/Encina/Identity/RequestIdentity.cs` | The authorizer reads the caller's kind, id and permission set from one snapshot |
| `IRequestContext.Identity` and `IRequestContextAccessor` | `src/Encina/Identity` | Supplies the ambient context to both PAPs |
| `AddBuiltInServiceIdentity`, `ServiceIdentityBuilder.WithPermissions`, `ServiceIdentityCatalogOptions.Declare`, `ServiceIdentityCatalogOptionsValidator` (rejects `*`) | `src/Encina/Identity` (internals visible to `Encina.Security.ABAC`) | Declares the seeding identity with the required permission |
| `IOperationAuditStore`, `OperationAuditEntry`, `AuditOutcome.Denied` | `Encina.Security.Audit` | The `Denied` entry of a refused change |
| `ABACErrors` (codes `encina.authorization.abac_{reason}`) | `src/Encina.Security.ABAC/ABACErrors.cs` | Hosts the new forbidden code and factory |
| `ABACDiagnostics` (ActivitySource and Meter `Encina.Security.ABAC`, `abac.pap.*`) | `src/Encina.Security.ABAC/Diagnostics/ABACDiagnostics.cs` | Hosts the new counter and activity |
| `ABACLogMessages` (EventIds 9000-9099) | `src/Encina.Security.ABAC/Diagnostics/ABACLogMessages.cs` | Hosts the new `[LoggerMessage]` 9041 |
| `EventIdRanges.SecurityABAC` (9000-9099, free 9041-9049) | `src/Encina/Diagnostics/EventIdRanges.cs` | Registry comment updated; no new range |
| `ABACPolicySeedingHostedService` and `service:encina.abac.policy-seeding` | `src/Encina.Security.ABAC/ABACPolicySeedingHostedService.cs` | The caller that must keep working |
| `ABACOptionsValidator` (`IValidateOptions<ABACOptions>`) | `src/Encina.Security.ABAC/DecisionAudit/ABACOptionsValidator.cs` | Validates the new option |
| `EncinaErrorCodes.AuthorizationPrefix` | `src/Encina/Errors/EncinaErrorCodes.cs` | Makes ASP.NET Core, Azure Functions and AWS Lambda answer 403 |
| `TestIdentity` builders | `Encina.Testing` | Build users and services with the permission in tests |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| Encina.Security.ABAC | 9041 (inside the registered 9000-9099) | One new Warning for a refused policy change; no new range; the existing 9094 and 9095 of the persistent PAP are reused for a failed `Denied` audit write; 9041 is currently listed free in `EventIdRanges.cs` |

### Estimated File Count

| Category | New | Modified | Approx. lines |
|----------|-----|----------|---------------|
| Production code (`Encina.Security.ABAC`) | 2 | 8 | 450 |
| Core registry comment (`EventIdRanges.cs`) | 0 | 1 | 2 |
| Unit tests | 4 | 5 | 820 |
| Guard tests | 0 | 2 | 40 |
| Contract tests | 1 | 0 | 160 |
| Integration tests | 0 | 1 | 120 |
| Docs (4 reference pages, package README) | 0 | 5 | 15 |
| Records (changelog fragment, coverage manifest, `PublicAPI.Unshipped.txt`) | 1 | 2 | 8 |
| **Total** | **8** | **24** | **~1,600** |

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT
Encina is a pre-1.0 .NET 10 / C# 14 library (nullable enabled, no backward compatibility, no [Obsolete]).
Issue #1986: the ABAC Policy Administration Point (src/Encina.Security.ABAC) must authorize the actor of every
policy mutation with a configurable permission, fail closed. Today PersistentPolicyAdministrationPoint attributes
the change to the authenticated caller of IRequestContext.Identity and refuses an anonymous caller
(abac.policy_change_principal_required) but never checks what the caller may do; InMemoryPolicyAdministrationPoint
reads no identity (issue #1985 adds that and is a prerequisite of the in-memory part).

IMPLEMENTATION OVERVIEW
1. ABACOptions.PolicyAdministrationPermission (default "abac:policies:manage"), validated (not blank, trimmed,
   no '*'); ABACErrors.PolicyChangeForbidden ("encina.authorization.abac_policy_change_forbidden", HTTP 403).
2. Internal PolicyChangeAuthorizer + PolicyActor: read the identity ONCE, return actor / anonymous refusal /
   forbidden refusal. Check BEFORE any store read in both PAPs, for the six mutations only (reads stay open).
3. Persistent PAP: a refused caller gets ONE Denied OperationAuditEntry in a separate DI scope; a failed audit
   write is logged (9094/9095) and never changes the Forbidden result. In-memory PAP: log, counter, activity only
   (choice 9 is pending the maintainer).
4. Seeding identity service:encina.abac.policy-seeding is declared WithPermissions(<final options value>).
5. Telemetry: counter abac.pap.change.denied, activity ABAC.PAP.Authorize, [LoggerMessage] EventId 9041 (Warning).
6. Tests: unit, guard, contract (both PAPs), integration (SQL Server collections for ADO/Dapper/EF Core),
   registration test with ValidateOnBuild + ValidateScopes. Docs, changelog fragment, PublicAPI, coverage manifest.
Delivery: four PRs (1: phases 1-2; 2: phases 3 and 5; 3: phase 4 after #1985; 4: phases 6-7).

KEY PATTERNS
- Either<EncinaError, T> everywhere; fixed error messages with no caller data; EncinaError.Message never logged.
- The identity is read once per change; user id, tenant, correlation id and kind come from that snapshot.
- Singleton PAP: one async DI scope per operation, a separate scope for the audit store.
- Telemetry follows abac.pap.* / ABAC.PAP.* naming; tags are enumeration names, operation and entity type only.
- EventIds only inside the registered range 9000-9099 (9041), [LoggerMessage] source generation.
- Tests: Shouldly via Encina.Testing.Shouldly, TestIdentity builders, NSubstitute, shared [Collection] fixtures,
  tests execute real classes; targets never go down; CRAP <= 10 on changed methods.
- PublicAPI, changelog fragments and coverage manifests go through mechanical-fixer; docs through docs-writer.

REFERENCE FILES
- src/Encina.Security.ABAC/Administration/{PersistentPolicyAdministrationPoint,InMemoryPolicyAdministrationPoint}.cs
- src/Encina.Security.ABAC/{ABACOptions,ABACErrors,ServiceCollectionExtensions,ABACPolicySeedingHostedService}.cs
- src/Encina.Security.ABAC/DecisionAudit/ABACOptionsValidator.cs
- src/Encina.Security.ABAC/Diagnostics/{ABACDiagnostics,ABACLogMessages}.cs
- src/Encina/Identity/{RequestIdentity,ServiceIdentityCatalogOptions,ServiceIdentityBuilder}.cs
- src/Encina/Diagnostics/EventIdRanges.cs
- tests/Encina.IntegrationTests/Security/ABAC/PersistentPapScopeScenario.cs
- docs/features/abac/reference/persistent-pap.md
- docs/plans/abac-pap-actor-authorization-implementation-plan-1986.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | The check is a set lookup on the identity already in hand; nothing is read that a cache could serve (the issue's "deferred" is not needed). |
| 2 | OpenTelemetry | ✅ | Counter `abac.pap.change.denied` and activity `ABAC.PAP.Authorize` in Phase 2; no caller data in tags. |
| 3 | Structured Logging | ✅ | One `[LoggerMessage]` EventId 9041 (Warning) inside the registered `SecurityABAC` range. |
| 4 | Health Checks | ❌ | The check has no external dependency to probe; `ABACHealthCheck` is unchanged. |
| 5 | Validation | ✅ | `ABACOptionsValidator` rule for `PolicyAdministrationPermission` (Phase 1), proven at host build. |
| 6 | Resilience | ❌ | In-process membership test; no external call. |
| 7 | Distributed Locks | ❌ | No shared mutable state is introduced; the check reads one immutable identity. |
| 8 | Transactions | ❌ | The check precedes the existing audited apply and writes nothing to the policy store; the `Denied` entry is a standalone audit write. |
| 9 | Idempotency | ❌ | A refusal changes no state and nothing receives duplicates. |
| 10 | Multi-Tenancy | ✅ | The `Denied` entry carries the caller's tenant; policies themselves have no tenant dimension, so the permission is global (non-goal). |
| 11 | Module Isolation | ❌ | `PolicySet` and `Policy` have no module dimension and `RequestIdentity` carries no module; the issue left this to the plan and it is not applicable until policy sets are module-scoped. |
| 12 | Audit Trail | ✅ | One `Denied` `OperationAuditEntry` per refused attempt in the persistent PAP; the in-memory PAP keeps no audit trail (choice 9). |

No integration is deferred, so no deferral issue is needed; the issue text listed caching and module isolation as deferred, and this plan finds both not applicable for the reasons above.

---

## Prerequisites & Dependencies

| Item | State | Effect on this plan |
|------|-------|---------------------|
| [#1985](https://github.com/dlrivada/Encina/issues/1985) `InMemoryPolicyAdministrationPoint` accepts anonymous, unattributed policy changes | Open | Hard dependency of Phase 4 (PR 3): the in-memory PAP needs the `IRequestContextAccessor` and the anonymous refusal first. PRs 1 and 2 (persistent PAP) do not depend on it. |
| [#1705](https://github.com/dlrivada/Encina/issues/1705) / [#1677](https://github.com/dlrivada/Encina/issues/1677) request identity and actor recording | Done | Provides `RequestIdentity.Permissions`, the service identity catalog and the built-in seeding identity |
| `Encina.Security.Audit` `IOperationAuditStore` and `AuditOutcome.Denied` | Done | Used as is |
| Maintainer answers to choices 2, 4 and 9 | Pending | Asked when work starts; the recommendations are written in the choices |

---

## Next Steps

1. When work on #1986 starts, present choices 2, 4 and 9 to the maintainer, record the answers in a `## Maintainer Decisions` section (after this one), run `adversarial-reviewer` on the Settled and Trivial choices to set their `Verified` lines, and run `check-plan` without `-Draft`.
2. Brief PR 1 (phases 1-2) with the `worker-brief` skill, using the phase prompts; PR 2 follows; PR 3 waits for #1985.
3. A follow-up `[DEBT]` issue is drafted by this planning run: `abac.policy_change_principal_required` (and the other ABAC codes without the `encina.authorization.` prefix that signal a refused caller) is answered with HTTP 500 by the host adapters instead of 401; the orchestrator opens it and links it from #1986.
4. Link this plan from #1986 with a short issue comment.
