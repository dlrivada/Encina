# Implementation Plan: `[RequirePasskey]` — passkey (WebAuthn/FIDO2) authentication requirement for Encina requests

> **Issue**: [#204](https://github.com/dlrivada/Encina/issues/204)
> **Type**: Feature
> **Complexity**: Medium (5 phases, no database provider, about 22 files)
> **Estimated Scope**: ~450-650 lines of production code + ~900-1,200 lines of tests

---

## Summary

Let a request declare that its caller must have authenticated with a passkey (WebAuthn/FIDO2): `[RequirePasskey]` on a command or query, enforced by `SecurityPipelineBehavior` in `Encina.Security`, with `AllowFallback = true` for "passkey preferred, password allowed". The recommended shape (Dilemma D1, option A) is **enforcement only**: Encina reads how the principal authenticated (the `amr` claim of RFC 8176) and leaves registration and assertion, the cryptographic ceremony, to ASP.NET Core Identity (.NET 10 passkey support) or to any identity provider. Encina therefore owns no credential store, no challenge state and no WebAuthn parsing.

Facts checked against `origin/main` (`18ec8dd19`):

- There is no passkey code in `src/`: no `RequirePasskey`, no `IPasskeyChallenger`, and `Directory.Packages.props` has no FIDO2 or WebAuthn package.
- Security attributes derive from `SecurityAttribute` (`src/Encina.Security/Attributes/`) and are dispatched in `SecurityPipelineBehavior<TRequest,TResponse>.EvaluateAttributeAsync`, with `GetDenialCode` and `GetAttributeTypePriority` as the two other places that list each attribute.
- ADR-062 (accepted 2026-11-11) replaces the prefix rule: hosts read the `ErrorCategory` set where the error is created and never parse the code, so the new code is built with the categorized `EncinaErrors.Create` and needs no host status-table entry (Design Choice 4). The completeness scan fails on an uncategorized call outside `ErrorCategoryPending.txt`.
- `RequestIdentityOptions` keeps `amr` and `acr` out of the per-token claims on purpose: they are step-up signals, so a change in either is a different identity (`src/Encina/Identity/RequestIdentityOptions.cs`, `RequestIdentityOptionsValidator.cs`).
- .NET 10 documents passkeys for ASP.NET Core Identity at <https://learn.microsoft.com/en-us/aspnet/core/security/authentication/passkeys/?view=aspnetcore-10.0> (this replaces the Identity API authorization link in the issue). It names `IdentityPasskeyOptions` (`ServerDomain`, `UserVerificationRequirement`, `ResidentKeyRequirement`, `AuthenticatorTimeout`), `IPasskeyHandler<TUser>` and `SignInManager<TUser>.PasskeySignInAsync`. That page does not state which claims a passkey sign-in adds; Phase 1 task 1 settles it with a test against a real Identity host instead of an assumption.

**Standards**: W3C WebAuthn, FIDO2/CTAP2, RFC 8176 (`amr` values), OpenID Connect Core (`amr`, `acr`).
**Affected packages**: `Encina.Security` (behavior, attribute, options), `Encina` (one error code constant). `Encina.AspNetCore` only if D2 selects it.
**Provider category**: none. Option A persists nothing; if D1 selects option B or C and a store is needed, that store is provider-dependent and covers all 10 database providers (AGENTS.md section 5), which this plan then re-opens.

---

## Design Choices

<details>
<summary><strong>1. What does <code>[RequirePasskey]</code> check, and who runs the WebAuthn ceremony? (Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — Encina only enforces**: read how the principal authenticated (`amr`, RFC 8176) from authenticated identities; registration and assertion stay with ASP.NET Core Identity or the identity provider | No cryptographic ceremony, credential store or challenge state in Encina; works with Identity's own passkey endpoints and with external providers (Entra, Keycloak); smallest and safest surface | The claim must be trusted and mapped per provider; Encina cannot offer a registration flow of its own |
| **B — Encina implements `IPasskeyChallenger`** on a FIDO2 library | Works for applications with neither Identity nor a provider; one API from challenge to enforcement | Owns credential storage (all 10 database providers), cross-node challenge consumption, attestation parsing and a security-critical protocol; adds a dependency not in `Directory.Packages.props` |
| **C — Adapter over the .NET 10 Identity passkey APIs** (`IPasskeyHandler<TUser>`, `SignInManager<TUser>`) | Avoids reimplementing the ceremony for Identity applications; can stamp the method claim itself | An Identity dependency inside the package; useless for applications on an external provider; couples to Identity's API surface |

### Chosen Option: **A — Encina only enforces** (recommended, pending the maintainer)

### Rationale

Option A wins when the application already has Identity or an external provider, which is the case for every deployment that can use passkeys in .NET 10 without writing a ceremony. It keeps Encina free of a cryptographic protocol (a wrong parser is a vulnerability, not a bug) and of the storage and locking questions that B opens (the issue's own cross-cutting rows 7 and 9: a challenge must be consumed once across nodes). B wins only for an application with no identity system at all; C wins when the maintainer wants a first-class Identity experience, at the cost of the dependency, and can be added later as a thin `Encina.AspNetCore` helper that stamps the method claim without changing the attribute. The issue's acceptance items "PasskeyChallenger implementation (or the D1 equivalent)", "Integration with ASP.NET Core Identity" and "Registration flow support" become, under A: the evidence reader (the D1 equivalent), a documented and tested mapping of Identity's sign-in result to the claim, and a guide that points registration to Identity.

**State and replay (part of D1).** Under A Encina keeps no state: the challenge, its expiry and its single use belong to the ceremony owner (Identity or the provider), so the issue's rows "Distributed Locks" and "Idempotency" do not apply and no `TimeProvider` surface exists; replay protection is only as good as the ceremony owner's. Under B the answer flips: challenge expiry comes from `TimeProvider` (AGENTS.md section 3) and a challenge is consumed once with an atomic store operation or a lock for all 10 database providers (the issue's original "challenges are unique" is not "consumed once"). Under C the ceremony owner is Identity, so the state answer is A's.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Where does the behavior live: <code>Encina.Security</code> or <code>Encina.AspNetCore</code>? (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — `Encina.Security`**, claim based | Framework-neutral: works in workers, serverless and Blazor circuits; all security attributes stay in one dispatcher; no new package edges | Reads `ISecurityContext.User`, which the application must populate (`ISecurityContextAccessor`, see the package README); no HTTP-aware helpers |
| **B — `Encina.AspNetCore`**, HTTP and Identity aware | Can read `IRequestContext.Identity` and `HttpContext`; natural home if D1 selects option C | A second attribute family next to `[Authorize]`; not usable outside HTTP; the Security dispatcher would not know the attribute |

### Chosen Option: **A — `Encina.Security`** (recommended, pending the maintainer)

### Rationale

Under D1 option A the check is a pure function of a `ClaimsPrincipal`, which is exactly what `SecurityContext` already wraps. Placing it in `Encina.Security` keeps one dispatcher (`EvaluateAttributeAsync`) and lets non-HTTP hosts require a passkey for a request that arrived from an HTTP front end. B wins if D1 selects option C or if the maintainer wants the attribute to read `RequestIdentity` directly; the `AuthorizationPipelineBehavior` in `Encina.AspNetCore` already evaluates `IRequestContext.Identity`, so B would put two authorization gates on one request type. The HTTP-only parts that exist (claim stamping for Identity sign-in, if Phase 1 shows Identity does not emit the claim) belong to `Encina.AspNetCore` either way.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Fail-closed evaluation and the meaning of <code>AllowFallback</code> (Dilemma D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — Four-way evidence**: `Unauthenticated`, `Unknown` (authenticated, no usable `amr`), `NotPasskey` (`amr` present, no passkey value), `Passkey`; only `Passkey` passes unless `AllowFallback`, which also accepts `Unknown` and `NotPasskey` but never `Unauthenticated`; every fallback is counted and logged | Fail closed when the method cannot be determined; `AllowFallback` is usable with a plain Identity cookie sign-in that emits no `amr`; operations can see how many fallbacks remain | Three denial reasons to document |
| **B — Binary** (`amr` contains a passkey value or deny); `AllowFallback` accepts any authenticated principal | Simplest | No visibility into why a request was denied or how many fall back; cannot tell "unknown" from "password" |
| **C — Fallback accepts only listed methods** (default `pwd`) | Stricter fallback | A password sign-in without an `amr` claim (the common Identity cookie case) would be denied, so "password allowed" would not hold |

### Chosen Option: **A — Four-way evidence** (recommended, pending the maintainer)

### Rationale

AGENTS.md section 3 requires compliance and security gates to fail closed when context is missing, and issue #204 repeats it (no principal, no claim: deny). All three options satisfy that rule for a missing principal (every one denies `Unauthenticated`); they differ only in what `AllowFallback` accepts, so the choice is the maintainer's. A keeps the issue's "passkey preferred, password allowed" meaningful on a plain Identity cookie sign-in that emits no `amr`, and counts and logs every fallback. B wins for the smallest surface; C wins when the maintainer wants the fallback limited to listed methods. Only authenticated identities of the principal count (`ClaimsPrincipal.Identities` filtered by `IsAuthenticated`), so a forged unauthenticated identity cannot carry the claim.

Operational consequence of A: `AllowFallback` accepts `Unknown`, so on a host that emits no `amr` the attribute with fallback is equivalent to `[Authorize]`; the fallback counter (its tag carries the evidence kind) and EventId 8006 are the only signal, and the guide says so and tells operators to alert on the counter.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Denial error code and HTTP status</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — New code `encina.authorization.passkey_required` in `EncinaErrorCodes`, built by `SecurityErrors.PasskeyRequired` through the categorized `EncinaErrors.Create(code, ErrorCategory.Forbidden, ...)`** | Hosts answer 403 from the category (ADR-062), not from the code text; same factory shape as `ClaimMissing` | One more constant to keep in the public error-code catalog |
| **B — Reuse `AuthorizationClaimMissing`** | No new constant | Hides the reason; the metadata would carry the claim name, not the requirement |
| **C — 401 with an RFC 9470 step-up challenge** | Standard way to ask the client to re-authenticate | Needs the `WWW-Authenticate` challenge work of the status-mapping epic; beyond this issue |

### Chosen Option: **A — New code, category `Forbidden`**

### Rationale

ADR-062 rule R6: denials are `Forbidden` and "no identity" is `Unauthenticated`. An authenticated identity without a passkey is a denial: `passkey_required` is `Forbidden` (403); a request with no authenticated identity keeps the existing `Unauthenticated` path (401 with `WWW-Authenticate`). The error is built only through the categorized overload, so the completeness scan passes and the code never enters `ErrorCategoryPending.txt`. #3420 (open, `needs-decision`) owns compliance and security denials, whose codes stay uncategorized and annotated in the pending file: `passkey_required` is an attribute-level authorization denial, not a compliance one, so it is categorized now; if the maintainer rules it into #3420, only the category argument and the pending entry move. Option C is a follow-up of the status-mapping epic (#3419, #3420, #3426), not of this feature. Error details carry the request type and the requirement (`passkey`, `passkey_or_fallback`), never the claim values or the user id in a message (AGENTS.md section 3, `EncinaError.Message` rule).

**Class:** Trivial
**Settled by:** ADR-062 (contract and rule R6), `src/Encina.Security/SecurityErrors.cs` (family rule), AGENTS.md section 3

</details>

<details>
<summary><strong>5. Attribute shape, ordering and configuration</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — `RequirePasskeyAttribute : SecurityAttribute` (sealed) with `AllowFallback`; claim name and passkey values in a `PasskeyOptions` class validated at startup** | Same shape as `RequireClaimAttribute`; the per-provider mapping (RFC 8176 `hwk`/`swk`, a provider-specific value) is configuration, not code | One more options class |
| **B — Put the settings on `SecurityOptions`** | No new class | `SecurityOptions` holds claim types for identity, not method semantics; grows a flat bag |
| **C — Settings on the attribute** | Local to the request | Repeated per request type; not changeable per environment |

### Chosen Option: **A — Attribute plus validated `PasskeyOptions`**

### Rationale

`RequireClaimAttribute` is the precedent for a claim-driven attribute, and `AddEncinaSecurity` already configures options with `TryAdd`. `PasskeyOptions` carries `AuthenticationMethodClaimType` (default `amr`) and `PasskeyMethodValues` (no default, see D7); an `IValidateOptions<PasskeyOptions>` rejects empty or blank entries at startup. The evaluation order follows `GetAttributeTypePriority`: `RequirePasskey` runs after `DenyAnonymous` and the role and permission checks (priority 6), so cheap denials come first. The shape of the attribute and the options does not depend on D2: only the package that holds the classes would move.

**Class:** Trivial
**Settled by:** `src/Encina.Security/Attributes/RequireClaimAttribute.cs`, `src/Encina.Security/ServiceCollectionExtensions.cs`, `src/Encina/Identity/RequestIdentityOptionsValidator.cs`

</details>

<details>
<summary><strong>6. Default passkey method values (Dilemma D7)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — No default list: `PasskeyMethodValues` must be set by the application; with an empty list no evidence is ever `Passkey`, so `[RequirePasskey]` denies (and logs 8007) until it is set; the validator rejects blank entries and a blank claim type at startup but allows an empty list, so an application that never uses the attribute pays nothing** | Fails closed: the gate never ships an unverified claim about which `amr` values mean "passkey" | One more line of configuration |
| **B — Default `hwk` and `swk` (RFC 8176 key values), after the Phase 1 spike verifies what Identity emits** | Works out of the box for Identity | RFC 8176 defines them as proof of possession of a hardware- or software-secured key, which smart-card and client-certificate sign-ins also assert; a non-empty default can admit non-passkey authenticators, so it must be verified, not guessed |

### Chosen Option: **A — no default list** (recommended, pending the maintainer)

### Rationale

- A security gate must not ship a non-empty default it has not verified; the plan itself says Identity's claims are unknown until the Phase 1 spike. B is only acceptable if the spike proves that Identity emits exactly those values for passkey sign-ins and that no other common sign-in emits them. The guide states that `amr` is only as trustworthy as the issuer of the token or cookie.

**Class:** Dilemma

</details>

---

## Implementation Phases

### Phase 1: Core models, attribute, options and evidence reader

<details>
<summary><strong>Tasks</strong></summary>

1. **Spike, first test of the phase** (`tests/Encina.IntegrationTests/Security/PasskeyIdentityClaimsTests.cs`): build a host with ASP.NET Core Identity (.NET 10), perform a passkey sign-in with a scripted authenticator response or, if that is impractical, read the claims produced by `SignInManager<TUser>` for a passkey sign-in, and record in the guide which `amr` values it adds. The result is written into this plan's `## Maintainer Decisions` before PR 2 starts, so the split and the D7 default are decided by evidence. If Identity adds none, Phase 4 adds the claim-stamping helper to `Encina.AspNetCore`.
2. `src/Encina.Security/Attributes/RequirePasskeyAttribute.cs`: `public sealed class RequirePasskeyAttribute : SecurityAttribute` with `public bool AllowFallback { get; set; }`.
3. `src/Encina.Security/PasskeyOptions.cs`: `AuthenticationMethodClaimType` (default `"amr"`), `PasskeyMethodValues` (`IList<string>`, empty by default under D7 option A, so the application must choose; under D7 option B the default is the values the spike verified).
4. `src/Encina.Security/PasskeyOptionsValidator.cs`: `internal sealed class PasskeyOptionsValidator : IValidateOptions<PasskeyOptions>` (a blank claim type or a blank value fails at startup; an empty list is allowed and makes the attribute deny, D7 option A).
5. `src/Encina.Security/Abstractions/IPasskeyEvidenceReader.cs`: `PasskeyEvidence Read(ISecurityContext context)`, and `enum PasskeyEvidence { Unauthenticated, Unknown, NotPasskey, Passkey }` in `PasskeyEvidence.cs`.
6. `src/Encina.Security/ClaimsPasskeyEvidenceReader.cs`: `internal sealed class ClaimsPasskeyEvidenceReader(IOptions<PasskeyOptions>) : IPasskeyEvidenceReader`; reads only authenticated identities of `context.User`, splits nothing (one value per claim, as RFC 8176 arrays are emitted as repeated claims), compares ordinal ignore-case.
7. `src/Encina/Errors/EncinaErrorCodes.cs`: `AuthorizationPasskeyRequired = "encina.authorization.passkey_required"`; `src/Encina.Security/SecurityErrors.cs`: `PasskeyRequired(Type requestType, string requirement)` built with `EncinaErrors.Create(code, ErrorCategory.Forbidden, ...)` (Design Choice 4).
8. `PublicAPI.Unshipped.txt` lines for every public symbol (written by `mechanical-fixer`). The changed public constructor of `SecurityPipelineBehavior` (Phase 2) is not in `Shipped` (its line is in `src/Encina.Security/PublicAPI.Unshipped.txt`), so that existing line is rewritten in place, with no `*REMOVED*` entry.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT: Encina.Security evaluates security attributes in SecurityPipelineBehavior. Issue #204 adds [RequirePasskey] (enforcement only: read the amr claim, never run a WebAuthn ceremony). Decisions D1, D2, D3 and D7 are recommended, pending the maintainer: option A and Encina.Security, four-way evidence, no default method values. The denial is ErrorCategory.Forbidden (ADR-062).
TASK: Create RequirePasskeyAttribute, PasskeyOptions (+ validator), PasskeyEvidence, IPasskeyEvidenceReader and ClaimsPasskeyEvidenceReader; add EncinaErrorCodes.AuthorizationPasskeyRequired and SecurityErrors.PasskeyRequired. Start with the Identity claims spike test and record the result.
KEY RULES: sealed classes, XML docs with examples, nullable enabled, no [Obsolete]; only authenticated ClaimsIdentity instances count; deny (Unauthenticated/Unknown/NotPasskey) never throws; error details carry the request type and requirement, never claim values or a user id in the message; Either<EncinaError,T> is not needed here (pure functions).
REFERENCE FILES: src/Encina.Security/Attributes/RequireClaimAttribute.cs, src/Encina.Security/SecurityErrors.cs, src/Encina.Security/SecurityContext.cs, src/Encina/Errors/EncinaErrorCodes.cs, src/Encina/Identity/RequestIdentityOptionsValidator.cs
```

</details>

### Phase 2: Pipeline behavior, DI and health check

<details>
<summary><strong>Tasks</strong></summary>

1. `SecurityPipelineBehavior.cs`: add the constructor dependency `IPasskeyEvidenceReader` (the reader owns `PasskeyOptions`, so the behavior takes no options); add `RequirePasskeyAttribute` to `EvaluateAttributeAsync` (`EvaluateRequirePasskey(securityContext, attribute, requestType)` returning `EncinaError?`), to `GetDenialCode` (`AuthorizationPasskeyRequired`) and to `GetAttributeTypePriority` (6). Keep the existing `// crap-exempt: single-question switch` marker honest: the new branch adds one arm, the evaluator method holds the logic (comp 3).
2. `ServiceCollectionExtensions.AddEncinaSecurity`: `services.AddOptions<PasskeyOptions>()` with `IValidateOptions<PasskeyOptions>` (`TryAddEnumerable`) and `services.TryAddSingleton<IPasskeyEvidenceReader, ClaimsPasskeyEvidenceReader>()`.
3. No `SecurityHealthCheck` change: `AddEncinaSecurity` always registers the reader with `TryAdd`, so a "reader missing" check would be dead code, and there is no external dependency to probe under D1 option A.
4. DI test: build the provider with `ValidateOnBuild` and `ValidateScopes` and resolve `SecurityPipelineBehavior<,>` for a request marked `[RequirePasskey]` (registration completeness, AGENTS.md section 3).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT: Phase 1 delivered the attribute, options, evidence reader and error factory in Encina.Security. SecurityPipelineBehavior dispatches attributes in EvaluateAttributeAsync.
TASK: Wire RequirePasskey into SecurityPipelineBehavior (evaluation, denial code, priority), register options, validator and reader in AddEncinaSecurity with TryAdd, and add the ValidateOnBuild/ValidateScopes DI test.
KEY RULES: fail closed (Unauthenticated, Unknown and NotPasskey deny unless AllowFallback, and Unauthenticated always denies); a fallback allows the request and is recorded (Phase 3); keep the behavior short-circuiting on the first denial; every registration is TryAdd; CRAP <= 10 on every changed method.
REFERENCE FILES: src/Encina.Security/SecurityPipelineBehavior.cs, src/Encina.Security/ServiceCollectionExtensions.cs
```

</details>

### Phase 3: Observability and audit hook

<details>
<summary><strong>Tasks</strong></summary>

1. EventIds inside the registered `EventIdRanges.Security` range (8000-8009, 8000-8004 used): `8005 PasskeyRequirementDenied` (warning: request type, evidence), `8006 PasskeyFallbackAccepted` (information: request type), `8007 PasskeyMethodClaimInvalid` (warning: options or principal claim unusable, no value logged). New entries go in `SecurityLogMessages.cs` as `[LoggerMessage]`-style or `LoggerMessage.Define` with one literal `new EventId(<n>, ...)` each, matching that file; the architecture test `EncinaEventIdAllocationTests` already maps `Encina.Security` to the range.
2. `SecurityDiagnostics`: counter `security.authorization.passkey_fallback` (`Counter<long>`; no `_total` suffix, ADR-054 decision 4; unit in the metadata) and tag `security.passkey_evidence` (the evidence kind, so operators can alert on `Unknown` fallbacks); the existing allowed and denied counters carry the denial reason `encina.authorization.passkey_required` through `GetDenialCode`. Per ADR-054 D4 the tenant id is a span attribute and a metric tag on these instruments only when multi-tenancy is on, and no tenant tag is emitted when it is off (the cardinality risk is documented by #906); logs never carry the tenant id.
3. Audit: the behavior does not write audit records for any attribute today; the denial audit (ADR-036 operation audit store) is deferred as one issue for all security attributes (Next Steps).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT: Encina.Security already has SecurityDiagnostics (ActivitySource and Meter "Encina.Security") and SecurityLogMessages with EventIds 8000-8004 in the registered range 8000-8009.
TASK: Add EventIds 8005-8007, the passkey fallback counter and the evidence tag; log with request type and evidence only.
KEY RULES: never log claim values, user ids in messages or EncinaError.Message; sequential EventIds with no gaps; XML docs name the range; no new range is needed.
REFERENCE FILES: src/Encina.Security/Diagnostics/SecurityLogMessages.cs, src/Encina.Security/Diagnostics/SecurityDiagnostics.cs, src/Encina/Diagnostics/EventIdRanges.cs, tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

### Phase 4: ASP.NET Core Identity alignment and documentation guide

<details>
<summary><strong>Tasks</strong></summary>

1. If the Phase 1 spike shows Identity emits no method claim: `src/Encina.AspNetCore/Passkeys/PasskeyAuthenticationMethodClaims.cs` (a helper that adds the configured `amr` value to the principal built by a passkey sign-in), with its registration; otherwise no code, only the mapping documented.
2. Guide `docs/en/guides/passkey-authentication.md` (written by `docs-writer`): how to enable Identity passkeys (link above), how to map the claim, `ServerDomain` and host-header validation warnings from the Microsoft page, how `AllowFallback` behaves, why Encina has no registration flow.
3. `src/Encina.Security/README.md` section; `ROADMAP.md:668` entry updated; ADR only if D1 is answered with B or C.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT: [RequirePasskey] reads the amr claim; ASP.NET Core Identity (.NET 10) runs the passkey ceremony (IPasskeyHandler<TUser>, SignInManager<TUser>.PasskeySignInAsync).
TASK: Act on the Phase 1 spike result (helper or documentation only) and write the guide and README section.
KEY RULES: documentation goes through docs-writer; never type coverage figures; link the Microsoft passkeys page, not the Identity API authorization page; state that registration and recovery stay with Identity.
REFERENCE FILES: src/Encina.Security/README.md, docs/engineering/prompts/implementation-plan-prompt.md, ROADMAP.md
```

</details>

### Phase 5: Testing — 7 test types

<details>
<summary><strong>Tasks</strong></summary>

1. **Unit** (`tests/Encina.UnitTests/Security/`): attribute defaults; `ClaimsPasskeyEvidenceReader` for the four outcomes (no principal, unauthenticated identity carrying `amr=hwk`, authenticated without `amr`, `amr=pwd`, `amr=hwk`, case differences); `SecurityPipelineBehavior` with `[RequirePasskey]` and `[RequirePasskey(AllowFallback = true)]` (allow, deny, fallback counted, short-circuit order); validator; `SecurityErrors.PasskeyRequired` metadata.
2. **Guard** (`tests/Encina.GuardTests/Security/`): null checks of every new public method.
3. **Contract** (`tests/Encina.ContractTests/Security/`): `IPasskeyEvidenceReader` contract on the real reader; attribute usage (`AttributeUsage` inherited, multiple allowed as in `SecurityAttribute`).
4. **Property** (`tests/Encina.PropertyTests/Security/`): a set of `amr` values with no configured passkey value never yields `Passkey`; adding one always does; case-insensitivity.
5. **Integration** (`tests/Encina.IntegrationTests/Security/`): `AddEncina` + `AddEncinaSecurity` host (not a database feature, no container); the Phase 1 Identity spike; DI `ValidateOnBuild`.
6. **Load / Benchmark**: `.md` justification files (stateless claim read; the per-request cost is a few dictionary lookups).
7. Per-flag targets for the manifests `.github/coverage-manifest/Encina.Security.json` and `Encina.json` (written by `mechanical-fixer`, never lower than the existing targets; `-` = flag not applicable). Each PR reports the measured values of its own files against them:

| File | Unit | Guard | Contract | Property | Integration | Justification |
|------|:----:|:-----:|:--------:|:--------:|:-----------:|---------------|
| `Attributes/RequirePasskeyAttribute.cs` | 100 | 0 | 90 | - | - | A property and a constructor with no parameters: nothing for guard tests to reach; the contract test pins `AttributeUsage`. |
| `PasskeyOptions.cs` | 95 | 0 | - | - | 80 | Properties only, no parameters, so guard tests cannot reach it; the integration host binds it. |
| `PasskeyOptionsValidator.cs` | 95 | 90 | - | - | 80 | Few branches (blank claim type, blank value); the guard test covers the null options argument. |
| `Abstractions/IPasskeyEvidenceReader.cs` and `PasskeyEvidence.cs` | 0 | 0 | 90 | - | - | An interface and an enum with no executable code; the contract test runs the real reader against it. |
| `ClaimsPasskeyEvidenceReader.cs` | 95 | 90 | 90 | 85 | 80 | The core classification (four outcomes, case, unauthenticated identities); property tests cover the evidence algebra, the integration host proves the real claim shape. |
| `SecurityErrors.cs` | 95 | 90 | - | - | - | The new factory is a pure function; its metadata and null guards are unit and guard testable. |
| `Diagnostics/SecurityLogMessages.cs` | 0 | 0 | - | - | - | Source-generated or `Define` delegates with no hand-written branches; reached through the behavior tests. |
| `Diagnostics/SecurityDiagnostics.cs` | 90 | 85 | - | - | - | Counter and tag recording with a tenant tag only when tenancy is on; both branches are unit testable with a meter listener. |
| `ServiceCollectionExtensions.cs` (the `AddEncinaSecurity` change) | 90 | 90 | - | - | 80 | Registration only; the `ValidateOnBuild`/`ValidateScopes` DI test and null guards cover it. |
| `SecurityPipelineBehavior.cs` | 90 | 70 | 60 | - | - | Current manifest targets (unit 90, guard 70, contract 60), which never go down; the new arm is covered by unit tests, and PR 2 reports the measured baseline of the file before and after. |
| `Errors/EncinaErrorCodes.cs` (`Encina.json`) | its current manifest target (read from `Encina.json` by the first PR; never lowered) | 0 | - | - | - | One new string constant; no executable code. |

The CRAP table of the changed methods (comp, cov, CRAP <= 10) is reported with each PR.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT: Phases 1-4 added [RequirePasskey] to Encina.Security. Tests must execute real package code (AGENTS.md section 9).
TASK: Write the unit, guard, contract, property and integration tests and the load/benchmark justification files; measure per-flag coverage of the touched files.
KEY RULES: Shouldly through Encina.Testing.Shouldly; FakeTimeProvider is not needed (no time); builders for principals; no reflection-only tests; assert the four evidence outcomes and the fail-closed paths; run the CRAP gate table locally.
REFERENCE FILES: tests/Encina.UnitTests/Security/, .github/coverage-manifest/Encina.Security.json, docs/en/guides/TESTING.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Reference | Relevance to this feature |
|---|---|---|
| W3C Web Authentication (WebAuthn) | <https://www.w3.org/TR/webauthn-2/> | The browser API for passkeys; the ceremony is performed by ASP.NET Core Identity, not by Encina |
| FIDO2 / CTAP2 | <https://fidoalliance.org/specifications/> | The authenticator protocol used for passkey interactions |
| RFC 8176 | <https://datatracker.ietf.org/doc/html/rfc8176> | The `amr` values `hwk` (hardware-secured key), `swk` (software-secured key), `user`, `pwd`, `mfa` |
| OpenID Connect Core, ID Token | <https://openid.net/specs/openid-connect-core-1_0.html#IDToken> | The `amr` and `acr` claims used as step-up signals |
| ASP.NET Core Identity passkeys (.NET 10) | <https://learn.microsoft.com/en-us/aspnet/core/security/authentication/passkeys/?view=aspnetcore-10.0> | `IdentityPasskeyOptions`, `IPasskeyHandler<TUser>`, `SignInManager<TUser>.PasskeySignInAsync` |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|---|---|---|
| `SecurityPipelineBehavior<TRequest,TResponse>` | `src/Encina.Security/SecurityPipelineBehavior.cs` | Gains one attribute branch (`EvaluateAttributeAsync`, `GetDenialCode`, `GetAttributeTypePriority`) |
| `SecurityAttribute` | `src/Encina.Security/Attributes/SecurityAttribute.cs` | Base class of `RequirePasskeyAttribute` |
| `RequireClaimAttribute` | `src/Encina.Security/Attributes/RequireClaimAttribute.cs` | Shape reference for the attribute |
| `SecurityErrors` | `src/Encina.Security/SecurityErrors.cs` | New factory `PasskeyRequired` in the `encina.authorization.*` family |
| `EncinaErrorCodes` | `src/Encina/Errors/EncinaErrorCodes.cs` | New constant `AuthorizationPasskeyRequired`; its status comes from the `Forbidden` category (ADR-062), not from the code text |
| `SecurityOptions`, `SecurityContext`, `ISecurityContext` | `src/Encina.Security/` | Claim types and the `ClaimsPrincipal` the reader inspects |
| `SecurityDiagnostics`, `SecurityLogMessages` | `src/Encina.Security/Diagnostics/` | `Encina.Security` activity source and meter; EventIds 8005-8007 |
| `AddEncinaSecurity` | `src/Encina.Security/ServiceCollectionExtensions.cs` | `TryAdd` registration of options, validator and reader |
| `RequestIdentityOptions` and its validator | `src/Encina/Identity/` | `amr` and `acr` are deliberately kept as step-up signals |
| `AuthorizationPipelineBehavior` | `src/Encina.AspNetCore/AuthorizationPipelineBehavior.cs` | HTTP-side gate on `IRequestContext.Identity`; unchanged |

### Event ID Allocation

| Package | Range | Notes |
|---|---|---|
| `Encina.Security` | 8005-8007 | Inside the registered `EventIdRanges.Security` (8000-8009); 8000-8004 used; 8008-8009 stay free; no new range |

### Estimated File Count

| Category | Files | Notes |
|---|---|---|
| Production, `Encina.Security` (new) | 5 | `RequirePasskeyAttribute`, `PasskeyOptions`, `PasskeyOptionsValidator`, `IPasskeyEvidenceReader` with `PasskeyEvidence`, `ClaimsPasskeyEvidenceReader` |
| Production (modified) | 7 | `SecurityPipelineBehavior`, `SecurityErrors`, `ServiceCollectionExtensions`, `SecurityDiagnostics`, `SecurityLogMessages`, `EncinaErrorCodes`, `PublicAPI.Unshipped.txt` (`SecurityHealthCheck` is not touched) |
| Tests | 8 new + 4 edited | Unit, guard, contract, property, integration (including the Identity spike); the new constructor parameter edits four existing test files (`SecurityCoreContractTests`, `SecurityCoreGuardTests`, `ObservabilityTests`, `SecurityPipelineBehaviorTests`, 8 construction sites) |
| Load and benchmark | 2 | Justification `.md` files |
| Manifests | 2 | `Encina.Security.json` and `Encina.json` (for `EncinaErrorCodes.cs`) |
| Documentation | 3 | Guide, README section, changelog fragment |

About 31 files in total, so the work is split in pull requests that each count code, tests, manifest and `PublicAPI` edits, build and pass alone, and stay within 15 files and 1500 lines (the targets of a file ship with the file, never in a later documentation PR):

| PR | Content | Files |
|----|---------|-------|
| 1 | Attribute, `PasskeyOptions` and validator, `IPasskeyEvidenceReader` with `PasskeyEvidence`, `ClaimsPasskeyEvidenceReader`, `EncinaErrorCodes.cs`, `SecurityErrors.cs`, `PublicAPI.Unshipped.txt`, both manifests; unit, guard, contract and property tests | about 14 |
| 2 | `SecurityPipelineBehavior` branch and constructor, `AddEncinaSecurity` registration, the four existing test files edited for the new constructor argument, the behavior unit tests, the DI test and the Identity spike integration test, the `PublicAPI.Unshipped.txt` constructor line | about 11 |
| 3 | `SecurityLogMessages.cs`, `SecurityDiagnostics.cs`, their tests, the manifest edit | about 6 |
| 4 | Guide, README section, changelog fragment, load and benchmark justification files, `ROADMAP.md` | about 6 |
| 5 (only if the spike shows Identity emits no method claim) | The `Encina.AspNetCore` claim-stamping helper, its tests, registration, manifest | about 6 |

PR 1 builds because the reader and the code are not yet consumed; PR 2 changes the constructor and its eight call sites together, so no PR leaves the build broken.

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
PROJECT CONTEXT: Encina is a pre-1.0 .NET 10 / C# 14 library with Railway Oriented Programming (Either<EncinaError,T>). Issue #204 adds [RequirePasskey] to Encina.Security. Decisions D1 (enforcement only, option A), D2 (Encina.Security, option A), D3 (four-way evidence) and D7 (no default method values) are recommended and pending the maintainer: do not start before they are answered. The denial code is categorized ErrorCategory.Forbidden (ADR-062).
IMPLEMENTATION OVERVIEW: Phase 1 attribute, options, evidence reader, error code (start with the Identity claims spike). Phase 2 behavior branch, DI, health check. Phase 3 EventIds 8005-8007, fallback counter. Phase 4 Identity alignment and guide. Phase 5 tests in 7 types.
KEY PATTERNS: fail closed; only authenticated identities count; no claim value or EncinaError.Message in logs, traces or health results; TryAdd registrations proven by a ValidateOnBuild/ValidateScopes test; CRAP <= 10 on changed methods; Shouldly via Encina.Testing.Shouldly; changelog fragment, PublicAPI lines and coverage manifest through mechanical-fixer; documentation through docs-writer.
REFERENCE FILES: src/Encina.Security/SecurityPipelineBehavior.cs, src/Encina.Security/Attributes/RequireClaimAttribute.cs, src/Encina.Security/SecurityErrors.cs, src/Encina/Errors/EncinaErrorCodes.cs, src/Encina/Identity/RequestIdentityOptions.cs, src/Encina/Diagnostics/EventIdRanges.cs, .github/coverage-manifest/Encina.Security.json
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | An authentication method is read per request from the principal; nothing to cache. |
| 2 | OpenTelemetry | ✅ | Existing `Encina.Security` activity source and meter: passkey fallback counter (no `_total`), evidence tag, denial reason through `GetDenialCode`, tenant tag only when tenancy is on (ADR-054 D4) (Phase 3). |
| 3 | Structured Logging | ✅ | EventIds 8005-8007 inside the registered `Security` range; no credential material, claim values or error messages (Phase 3). |
| 4 | Health Checks | ❌ | No external dependency to probe under D1 option A, and `AddEncinaSecurity` always registers the reader, so a "reader missing" check would be dead code. |
| 5 | Validation | ✅ | `IValidateOptions<PasskeyOptions>` validates the claim type and passkey values at startup; request validation does not apply (no input). |
| 6 | Resilience | ❌ | No external call under D1 option A; an external identity provider is called by Identity, not by Encina. |
| 7 | Distributed Locks | ❌ | Under D1 option A there is no challenge state in Encina; the issue's "consume once across nodes" concern belongs to the ceremony owner and returns only if D1 selects option B. |
| 8 | Transactions | ❌ | No state is persisted. |
| 9 | Idempotency | ❌ | Replay protection is the ceremony owner's (a challenge is consumed once by Identity); the request-level `InboxPipelineBehavior` is unrelated. Returns with D1 option B. |
| 10 | Multi-Tenancy | ⏭️ | Tenant-specific relying-party settings are `IdentityPasskeyOptions.ServerDomain` of the application; Encina has none under option A. Deferred to D1 option B, where an issue would be opened. |
| 11 | Module Isolation | ❌ | A cross-module authentication requirement; no module scoping. |
| 12 | Audit Trail | ⏭️ | `SecurityPipelineBehavior` writes no audit record for any attribute today; an authorization-denial audit through the operation audit store (ADR-036) is one issue for all security attributes, drafted as the issue file `artifacts/issues/security-attribute-denial-audit.md` (git-ignored; the orchestrator opens it, and it must exist as an issue before the plan is briefed). |

---

## Prerequisites & Dependencies

| Prerequisite | State | Effect on this plan |
|---|---|---|
| Answers to D1, D2, D3 and D7 | Pending the maintainer | Phases 1-4 assume the recommended option for each; any other answer re-opens the plan (a store for all 10 providers under D1 B, an `Encina.AspNetCore` placement under D2 B, other fallback semantics under D3, a verified default list under D7 B) |
| `ISecurityContextAccessor` population | Application code (`src/Encina.Security/README.md` shows the middleware) | The attribute reads whatever `ISecurityContext.User` holds; the One Request Identity work (#1705) is the model for how the `AuthorizationPipelineBehavior` side already gets it |
| ADR-062 (error category contract, accepted) and #3545 | ADR accepted; implementation S1 in flight | The 403 comes from `ErrorCategory.Forbidden` set at the call site; this plan needs the categorized `EncinaErrors.Create` overload on main |
| #3420, #3426 (compliance and security denials; exposure of error codes) | Open | #3420 owns the status of compliance and security denials (see Design Choice 4); a 401 step-up challenge (RFC 9470) is a follow-up |

---

## Next Steps

1. The maintainer answers D1, D2, D3 and D7 when work on #204 starts; the answers go into `## Maintainer Decisions`.
2. Run the Phase 1 Identity spike first and write its result into `## Maintainer Decisions` before PR 2 starts; it decides D7 option B's values and whether PR 5 exists.
3. Open the deferred audit issue from the issue file `artifacts/issues/security-attribute-denial-audit.md` (authorization denials of `SecurityPipelineBehavior` into the operation audit store) before the plan is briefed.
4. Brief a worker per pull request of the split in the Estimated File Count table, in order.
