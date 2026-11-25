# Implementation Plan: `Encina.Compliance.eIDAS2` — European Digital Identity Framework (Regulation (EU) 2024/1183)

> **Issue**: [#805](https://github.com/dlrivada/Encina/issues/805)
> **Type**: Feature (new package)
> **Complexity**: Very High (wallet credential verification, trusted-list consumption, qualified signing and sealing, evidence stream; 8 pull requests)
> **Estimated Scope**: 96 file touches and about 7,600 lines including tests and per-PR metadata; split into 8 pull requests, see "Pull request split"
> **Milestone**: v0.17.0 — Compliance Lifecycle. In the 1.0 scope since SPEC-000 DEC-009 (2026-10-23, REQ-030, which supersedes SPEC-002 DEC-015); the issue's `post-1.0` label and the issue comment "stays post-1.0" are stale
> **Provider Category**: None for databases (verification evidence in Marten on PostgreSQL, ADR-027; the external calls are HTTP to trust service providers)
> **Family**: `compliance-nis2-2`. Builds on the Evidence ports of #1241 and the reporting port of #812.

---

## Summary

eIDAS 2 makes the European Digital Identity Wallet (EUDIW) a credential that relying parties may have to accept (Art. 5f; wallets due by 24 December 2026, acceptance by private relying parties by 24 December 2027, micro and small enterprises exempt, per the issue comment of 2026-09-23). Encina has no eIDAS code today. The 1.0 extension points SPEC-002 REQ-048 asks for (RFC 3161 time-stamping and a signature port) are planned by #1241 in `Encina.Compliance.Evidence` (`ITimeStampAuthority`, `ISignatureProvider`); that plan explicitly does not validate the EU trusted list and does not produce qualified signatures. This plan adds exactly that.

**What it implements** (Regulation (EU) 2024/1183 amending 910/2014; Implementing Regulation (EU) 2015/1502 for levels of assurance; ETSI TS 119 612 trusted lists):

- Relying-party **wallet credential verification**: `IWalletCredentialVerifier` over a presentation (SD-JWT VC through OpenID4VP first, Design Choice 3), with issuer trust from the trusted list, status (revocation) check, selective-disclosure handling and a minimum level of assurance.
- **Trusted-list consumption**: `ITrustedListProvider` reads the EU List of Trusted Lists (LOTL) and the national lists, caches a snapshot, and answers "is this issuer or TSP a qualified provider of this service, now".
- **Level of assurance** (`AssuranceLevel`: Low, Substantial, High) with `MinimumAssuranceLevel`.
- **Qualified signature and seal**: `QualifiedSignatureProvider` implements the `ISignatureProvider` of #1241 over a remote signing service (Cloud Signature Consortium API); qualified time-stamps use `ITimeStampAuthority` from #1241 with the trusted-list check that #1241 leaves out.
- An event-sourced **verification evidence** stream (`CredentialVerificationAggregate`: who verified what, when, level reached, issuer id; no attribute values) for audit.
- Incident reporting for trust-service security breaches through the #812 port (last PR).

**Not in scope**: issuing credentials or acting as a wallet provider (Design Choice 7), the RFC 3161 client and the plain CMS signature provider (#1241), persisting RFC 3161 tokens for chained logs (#3622), and national eID scheme notification.

**Affected packages**: `Encina.Compliance.eIDAS2` (new), `Encina` (EventId range registration), `Encina.Compliance.Evidence` (referenced; created by #1241), `Encina.Compliance.BreachNotification` (one enum member, last PR).

**Provider category**: none. The 12-function check is in the matrix at the end.

### Corrections to the issue text (checked against `origin/main` 4b0a600c7)

| Issue text | Today's code and sibling plans | Plan decision |
|---|---|---|
| `AddEncinaCompliance(config => config.UseEIDAS2(...))` | No such entry point; each package has `AddEncina<Name>(Action<Options>)` | `AddEncinaEIDAS2(Action<EIDAS2Options>? configure = null)` |
| "Qualified timestamps", "QES integration" as new abstractions | #1241 plans `ITimeStampAuthority` and `ISignatureProvider` in `Encina.Compliance.Evidence` | Reuse; this plan adds qualification (trusted list) and a qualified provider |
| "Atomic credential issuance" (matrix row 8) | Issuing is wallet-provider or issuer work | Out of scope (Design Choice 7) |
| "Credential stores will use Marten" | A verifier should not retain credential attributes (data minimisation) | Evidence stream holds ids, levels and times only (Design Choice 6) |
| `IWalletVerifier`, `ISignatureService` names | #1241 owns `ISignatureProvider` | `IWalletCredentialVerifier`; no second signature port |
| Integration "ENISA Single Entry Point (#812)" | `IncidentRegulation` in the #812 plan is `{ GDPR, NIS2, DORA, CRA }` | One member `EIDAS` added in the last PR |
| Package name `Encina.Compliance.eIDAS2` | Other packages use upper-case acronyms (`NIS2`, `GDPR`) | Design Choice 8 |
| Related `#396` (closed), `#809` (closed epic) | Closed | Context only |

---

## Design Choices

<details>
<summary><strong>1. Package placement — new package</strong></summary>

- **Class**: Settled
- **Settled by**: issue #805 (Affected Packages "new package"); pay-for-what-you-use (AGENTS.md section 1).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) New package `Encina.Compliance.eIDAS2`** | Only relying parties and trust-service users take it | New csproj, manifest, README |
| B) Extend `Encina.Compliance.Evidence` | Fewer packages | Mixes a generic evidence port with an EU-specific regime |

### Chosen Option: **A**

### Rationale

- References: `Encina`, `Encina.DomainModeling`, `Encina.Marten`, `Encina.Compliance.Evidence`; `InternalsVisibleTo` for the six test assemblies.

</details>

<details>
<summary><strong>2. Reuse the Evidence ports of #1241</strong></summary>

- **Class**: Settled
- **Settled by**: the #1241 plan (it owns `ITimeStampAuthority` and `ISignatureProvider`, states that qualification is decided by the trusted list and names #805 as the follow-up).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Reference `Encina.Compliance.Evidence`; add a trusted-list-checking decorator for `ITimeStampAuthority` and a `QualifiedSignatureProvider`** | One RFC 3161 client and one signature port | This plan cannot start before #1241 PR 1 and the client PR merge |
| B) Own time-stamp and signature ports | Independent | Two ports for one concept |

### Chosen Option: **A**

### Rationale

- Only A avoids a second `ISignatureProvider`; the dependency is recorded in Prerequisites.

</details>

<details>
<summary><strong>3. Credential formats for the first release</strong></summary>

- **Class**: Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) SD-JWT VC through OpenID4VP only, behind `ICredentialFormatVerifier`; ISO/IEC 18013-5 mdoc later through the same port** | Smaller first release; JSON based; what most web relying parties meet first | A relying party that must accept mdoc waits for the follow-up |
| B) SD-JWT VC and mdoc (CBOR, COSE) in the first release | Matches the Architecture and Reference Framework, which names both | About 40 percent more code and a CBOR dependency in the first release |

### Chosen Option: **A (recommended, pending the maintainer)**

### Rationale

- A wins while the web-relying-party case drives adoption; B wins for in-person or mobile verifiers that receive mdoc. If B is chosen, the mdoc verifier becomes PR 9 of the split.

</details>

<details>
<summary><strong>4. JWS and JWT handling — BCL only, or a validated library</strong></summary>

- **Class**: Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A) BCL only (`System.Text.Json`, `ECDsa`, `RSA`, `X509Certificate2`) | No new package; no `Directory.Packages.props` edit | Encina owns signature-verification code paths for credentials (algorithm confusion, key selection) |
| **B) `Microsoft.IdentityModel.JsonWebTokens` for token validation (one `Directory.Packages.props` edit, counted in PR 1)** | Maintained validation of algorithms, lifetimes and keys | A new dependency (restore audits transitive packages); the repository has no use of it today (`git grep` of `Directory.Packages.props`) |

### Chosen Option: **B (recommended, pending the maintainer)**

### Rationale

- B wins for security review (a widely used validator); A wins if the dependency policy forbids a new identity package. #1241 chose BCL only for RFC 3161 because `System.Security.Cryptography.Pkcs` covers it; no BCL type covers JWT validation.

</details>

<details>
<summary><strong>5. Trusted-list handling — fail closed, opt-in refresh</strong></summary>

- **Class**: Trivial
- **Settled by**: AGENTS.md section 3 (compliance gates fail closed; resilience on external calls) and Reg. (EU) 910/2014 Art. 22 (trusted lists are the authority for qualified status).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `TrustedListProvider` fetches the LOTL and national lists through a named `HttpClient` from `IHttpClientFactory`, verifies the list signature against configured anchors, keeps an in-memory snapshot with a refresh interval and a maximum age; no snapshot or a snapshot older than the maximum age denies** | Fail closed; the endpoint is configurable (a local emulator or test list in tests) | The signature verification of XML lists is the most complex part (XMLDSig) |
| B) Trust the issuer certificate chain only | Simple | Not qualified status; contradicts the issue |

### Chosen Option: **A**

### Rationale

- A refresh failure keeps the old snapshot until the maximum age and logs by error code; the hosted refresh service is a `PeriodicTimer` loop that catches per iteration (AGENTS.md section 3).

</details>

<details>
<summary><strong>6. Persistence — evidence stream without attribute values</strong></summary>

- **Class**: Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Marten stream `CredentialVerificationAggregate` per verification: issuer id, format, level reached, outcome code, timestamps, tenant and module ids; no attribute values and no subject identifier beyond a salted hash** | Auditable; nothing personal in an immutable stream | A relying party that needs the attributes keeps them in its own store |
| B) No persistence at all | Smallest | No audit record of an acceptance decision |
| C) Persist attributes with the crypto-shredding of `Encina.Marten.GDPR` | Full record | Adds key management; retains identity data |

### Chosen Option: **A (recommended, pending the maintainer)**

### Rationale

- B wins for a relying party that already audits through `IOperationAuditStore`; C wins where a legal duty requires keeping the presented attributes. A follows the outcome of the personal-data Dilemma of the #822 plan.

</details>

<details>
<summary><strong>7. Role — relying party and verifier, not issuer</strong></summary>

- **Class**: Settled
- **Settled by**: Reg. (EU) 2024/1183 Art. 5f (obligations of relying parties); the issue's own scope list (verification, signature services, TSP status consumption) names no issuance function.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Relying party and verifier (and a client of qualified signing services)** | Matches every bullet of the issue's key areas | Credential issuance for applications that are issuers is not covered |
| B) Also issuer | Matches "credential issuance" in matrix row 8 | Wallet-provider and issuer certification is a different regulatory role |

### Chosen Option: **A**

### Rationale

- A conflicts with nothing in the issue's key areas; row 8 of the issue's matrix is the only mention and is replaced (Corrections table).

</details>

<details>
<summary><strong>8. Package and namespace casing</strong></summary>

- **Class**: Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Encina.Compliance.eIDAS2`, as in the issue, SPEC-000 and SPEC-002** | Names the regulation as written | Lower-case first letter in a namespace; the analyzers (CA1707, CA1709) must accept it or carry a justified suppression |
| B) `Encina.Compliance.EIDAS2` | Matches `NIS2` and `GDPR` | Differs from every document that names the package |

### Chosen Option: **A (recommended, pending the maintainer)**

### Rationale

- A wins for traceability to the issue and the specifications; B wins if the build flags A and the maintainer prefers a rename to a suppression. PR 1 builds with the analyzers on and reports which one applies.

</details>

---

## Implementation Phases

### Pull request split (read first)

Prerequisites for PR 1: #1241 merged up to its signature and time-stamp ports. Test project edits are `ProjectReference` additions (UnitTests and GuardTests in PR 1; PropertyTests in PR 3; ContractTests in PR 2; IntegrationTests in PR 5).

| PR | Phases | Files (production, metadata) | Test files | Files / lines (approx.) |
|---|---|---|---|---|
| 1 | 1 | `EventIdRanges.cs`, registry row, `Encina` `PublicAPI.Unshipped.txt`, `Encina.slnx`, csproj, `Directory.Packages.props` (if Dilemma 4 is B), two PublicAPI files, manifest, `EIDAS2Errors` (10) | `AssemblyRanges` entry, UnitTests and GuardTests csproj references, unit errors test (4) | 14 files / 600 |
| 2 | 2 | `ITrustedListProvider`, `TrustedListSnapshot`, `TrustedServiceStatus`, `TrustedListProvider`, list parser and signature check, `PublicAPI.Unshipped.txt`, manifest, `AssuranceLevel`, `EIDAS2Options` (10) | unit parser, unit provider (`FakeTimeProvider`, WireMock list), guard, contract csproj reference (4) | 14 files / 1,500 |
| 3 | 3 | `IWalletCredentialVerifier`, `ICredentialFormatVerifier`, `SdJwtVcVerifier`, `PresentationRequest`, `VerificationResult`, `PublicAPI.Unshipped.txt`, manifest (7) | unit verifier, guard, property (selective disclosure round trip), property csproj reference (4) | 11 files / 1,400 |
| 4 | 3 (status) | `CredentialStatusChecker` (token status list), `DefaultWalletCredentialVerifier` composition, `PublicAPI.Unshipped.txt`, manifest (4) | unit checker, unit composition, guard (3) | 7 files / 900 |
| 5 | 4 | `CredentialVerificationEvents`, `CredentialVerificationAggregate`, read model, projection, `EIDAS2MartenExtensions`, `PublicAPI.Unshipped.txt`, manifest (7) | unit aggregate, unit projection, integration Marten, integration csproj reference (4) | 11 files / 1,200 |
| 6 | 5 | `QualifiedSignatureProvider`, `CscRemoteSigningClient`, `QualifiedTimeStampAuthority` decorator, `PublicAPI.Unshipped.txt`, manifest (5) | unit provider, unit client (WireMock), unit decorator, guard (4) | 9 files / 1,400 |
| 7 | 6, 7, 8 | `ServiceCollectionExtensions` (`AddEncinaEIDAS2`), options validator, `TrustedListRefreshService`, health check, `EIDAS2Diagnostics`, `EIDAS2LogMessages`, `PublicAPI.Unshipped.txt`, manifest, changelog fragment, README, feature page (11) | DI test, unit refresh service, unit health check (3) | 14 files / 1,300 |
| 8 | 9 | `BreachNotification` `IncidentRegulation` edit, `EIDAS2IncidentReportMapper`, `PublicAPI.Unshipped.txt` (BreachNotification and eIDAS2), manifest (5) | unit mapper, contract of the `IncidentRegulation` member (2) | 7 files / 400 |

Every PR is recounted with `git ls-tree` before it is briefed. `AssuranceLevel` and `EIDAS2Options` land in PR 2 so that PR 1 stays within 15 files.

### Phase 1: Package Skeleton, EventIds & Errors (PR 1)

<details>
<summary>Tasks</summary>

1. `EventIdRanges.cs`: `ComplianceEIDAS2 = (52900, 52949)` registered first (ADR-021), registry row, `Encina` PublicAPI line, `AssemblyRanges` entry for `Encina.Compliance.eIDAS2`.
2. csproj with the references of Design Choice 1, `Encina.slnx` entry, PublicAPI files, `Directory.Packages.props` line when Dilemma 4 is B.
3. `EIDAS2Errors` with ADR-062 categories: `TrustedListUnavailable` Unavailable, `IssuerNotTrusted` Forbidden, `CredentialInvalid` Validation, `AssuranceLevelTooLow` Forbidden, `CredentialRevoked` Forbidden, `SigningFailed` UpstreamFailure, `TenantMissing` Forbidden, `EventStoreFailed` UpstreamFailure.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
You are implementing Phase 1 of Encina.Compliance.eIDAS2 (Issue #805).

CONTEXT:
- New package; mirror src/Encina.Compliance.NIS2/Encina.Compliance.NIS2.csproj. EventId range 52900-52949.

TASK:
1. Register the range first, then create the csproj, slnx entry, PublicAPI files and EIDAS2Errors.

KEY RULES:
- ErrorCategory per ADR-062; Either<EncinaError, T>; XML docs; zero warnings.

REFERENCE FILES: src/Encina.Compliance.NIS2/NIS2Errors.cs, docs/plans/trusted-timestamp-signature-implementation-plan-1241.md
```

</details>

### Phase 2: Levels of Assurance & Trusted-List Provider (PR 2)

<details>
<summary>Tasks</summary>

1. `Model/AssuranceLevel.cs`, `EIDAS2Options` (`MinimumAssuranceLevel`, `TrustedListEndpoint`, `TrustedListMaxAge`, `TrustedListRefreshInterval`, anchors; no secret fields).
2. `ITrustedListProvider` (`IsQualifiedAsync(issuerId, serviceType, atUtc)`), `TrustedListSnapshot`, `TrustedListProvider` (named `HttpClient`, `TimeProvider`), the TS 119 612 parser and list-signature verification.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
You are implementing Phase 2 of Encina.Compliance.eIDAS2 (Issue #805).

CONTEXT:
- Phase 1 merged. Trusted lists follow ETSI TS 119 612; the client is a named HttpClient from IHttpClientFactory.

TASK:
1. Create AssuranceLevel and EIDAS2Options.
2. Create the trusted-list provider, parser and signature check; no snapshot or an aged snapshot denies.

KEY RULES:
- Fail closed; time from TimeProvider; resilience on the external call; no EncinaError.Message in logs.

REFERENCE FILES: docs/plans/trusted-timestamp-signature-implementation-plan-1241.md
```

</details>

### Phase 3: Wallet Credential Verification (PRs 3 and 4)

<details>
<summary>Tasks</summary>

1. `IWalletCredentialVerifier`, `ICredentialFormatVerifier`, `PresentationRequest`, `VerificationResult` (level reached, issuer id, disclosed claim names only).
2. `SdJwtVcVerifier`: signature, `iss`, `exp`, key binding, disclosure digests; issuer checked with `ITrustedListProvider`.
3. `CredentialStatusChecker` (token status list) and `DefaultWalletCredentialVerifier` composing format verifier, trusted list, status and the minimum level; every failure is a `Left` and a denial.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
You are implementing Phase 3 of Encina.Compliance.eIDAS2 (Issue #805).

CONTEXT:
- The trusted-list provider exists (Phase 2). Presentation protocol: OpenID4VP; format: SD-JWT VC.

TASK:
1. Create the verifier ports, SdJwtVcVerifier, CredentialStatusChecker and DefaultWalletCredentialVerifier.

KEY RULES:
- Missing context or an unreachable status list denies (fail closed); claim values are never logged.

REFERENCE FILES: src/Encina.Compliance.NIS2/DefaultMFAEnforcer.cs
```

</details>

### Phase 4: Verification Evidence Stream (PR 5)

<details>
<summary>Tasks</summary>

1. `CredentialVerificationEvents` (verified, rejected, with outcome code, tenant and module ids) and `CredentialVerificationAggregate` (`Record(...)` returning `Either`).
2. `CredentialVerificationReadModel`, `CredentialVerificationProjection` (idempotent, constructor-injected) and `EIDAS2MartenExtensions`.
3. The verifier appends the evidence when a repository is registered; a store `Left` fails the verification (never reports success).

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
You are implementing Phase 4 of Encina.Compliance.eIDAS2 (Issue #805).

CONTEXT:
- Mirror src/Encina.Compliance.BreachNotification/ReadModels/BreachProjection.cs; no InMemory store.

TASK:
1. Create the evidence events, aggregate, read model, projection and Marten registration.

KEY RULES:
- No attribute values or raw subject identifiers in events; Marten on PostgreSQL via Testcontainers in integration tests.

REFERENCE FILES: src/Encina.Marten/Projections/IReadModelRepository.cs
```

</details>

### Phase 5: Qualified Signature, Seal & Time-Stamp (PR 6)

<details>
<summary>Tasks</summary>

1. `CscRemoteSigningClient` (Cloud Signature Consortium API over a named `HttpClient`) and `QualifiedSignatureProvider : ISignatureProvider` (sign and seal; verify checks the signer certificate against the trusted list at the signing time).
2. `QualifiedTimeStampAuthority` decorating `ITimeStampAuthority`: a token counts as qualified only when the TSA is a qualified service in the snapshot; otherwise the result carries `Qualified = false` and the caller decides.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
You are implementing Phase 5 of Encina.Compliance.eIDAS2 (Issue #805).

CONTEXT:
- ISignatureProvider and ITimeStampAuthority come from Encina.Compliance.Evidence (#1241).

TASK:
1. Create the CSC client, QualifiedSignatureProvider and QualifiedTimeStampAuthority.

KEY RULES:
- Credentials and tenant resolution come from the named client; WireMock for the TSP in tests; a failed signing is a Left, never a partial success.

REFERENCE FILES: docs/plans/trusted-timestamp-signature-implementation-plan-1241.md
```

</details>

### Phase 6: Configuration, DI & Refresh Service (PR 7)

<details>
<summary>Tasks</summary>

1. `AddEncinaEIDAS2(...)`: options, validator, trusted-list provider (singleton, its HTTP client by name), verifier (scoped), evidence store registration, `TryAdd*`; DI test with `ValidateOnBuild` and `ValidateScopes`.
2. `TrustedListRefreshService` (opt-in `BackgroundService`, `PeriodicTimer`, per-iteration catch).

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
You are implementing Phase 6 of Encina.Compliance.eIDAS2 (Issue #805).

CONTEXT:
- Components from Phases 2 to 5 exist.

TASK:
1. Add AddEncinaEIDAS2 with TryAdd* and the DI test; add the refresh service.

KEY RULES:
- Never inject a scoped service into a singleton (the provider is a singleton; the verifier is scoped).

REFERENCE FILES: src/Encina.Compliance.NIS2/ServiceCollectionExtensions.cs, src/Encina.Compliance.BreachNotification/BreachDeadlineMonitorService.cs
```

</details>

### Phase 7: Cross-Cutting Integration & Observability (PR 7)

<details>
<summary>Tasks</summary>

1. `EIDAS2Diagnostics` (meter and `ActivitySource` `Encina.Compliance.eIDAS2`; instruments `eidas2.verification.*`, `eidas2.trusted_list.*`, `eidas2.signature.*`; no `_total`; tenant tag with multi-tenancy).
2. `EIDAS2LogMessages`: ids 52900-52929 packed from 52900 (52930-52949 free).
3. Health check: no snapshot or a snapshot past the maximum age gives `Unhealthy`; a snapshot past the refresh interval gives `Degraded`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```text
You are implementing Phase 7 of Encina.Compliance.eIDAS2 (Issue #805).

CONTEXT:
- Range 52900-52949 is registered (Phase 1).

TASK:
1. Add diagnostics, log messages and the health check.

KEY RULES:
- Log error codes or exception types via ForLogging(); never claim values, EncinaError.Message or raw ids.

REFERENCE FILES: src/Encina.Compliance.NIS2/Diagnostics/NIS2Diagnostics.cs
```

</details>

### Phase 8: Testing (PRs 1 to 8)

<details>
<summary>Tasks</summary>

1. Unit, guard (public constructors and methods), contract (`ICredentialFormatVerifier`, `ITrustedListProvider`), property (selective disclosure round trip, LoA ordering), integration (Marten on PostgreSQL; WireMock for TSP and list endpoints).
2. Per-file targets: `SdJwtVcVerifier` unit 90, property 80; `TrustedListProvider` unit 90, guard 85; list parser unit 95; `DefaultWalletCredentialVerifier` unit 90, guard 85; `QualifiedSignatureProvider` unit 90, guard 85; aggregate and projection unit 90, integration 80; options validator unit 95, guard 90; diagnostics unit 85. Load and benchmark: `.md` justification (verification is I/O-bound and low-throughput; the trusted-list lookup is an in-memory read).
3. CRAP of every changed method at most 10; the XML signature check is split into small methods.

</details>

<details>
<summary>Prompt for AI Agents — Phase 8</summary>

```text
You are implementing the tests of Encina.Compliance.eIDAS2 (Issue #805).

CONTEXT:
- Manifest .github/coverage-manifest/Encina.Compliance.eIDAS2.json; production code per PR exists.

TASK:
1. Add the tests of each PR and report per-flag coverage against the manifest.

KEY RULES:
- Tests execute real code; Shouldly via Encina.Testing.Shouldly; WireMock through Encina.Testing.WireMock; no Thread.Sleep.

REFERENCE FILES: tests/Encina.UnitTests/Compliance, tests/Encina.GuardTests
```

</details>

### Phase 9: Incident Reporting & Documentation (PR 8 and PR 7)

<details>
<summary>Tasks</summary>

1. After #812 PR 1 merges: add `EIDAS` to `IncidentRegulation` and `EIDAS2IncidentReportMapper` mapping a trust-service security breach to `IncidentReport`; a `Left` from the adapter fails the report.
2. Docs: XML docs, `changelog.d/805-eidas2-compliance.added.md`, package README, `docs/features/eidas2.md` (by `docs-writer`), `docs/INVENTORY.md`; the ADR number is assigned when the ADR pull request opens.

</details>

<details>
<summary>Prompt for AI Agents — Phase 9</summary>

```text
You are implementing Phase 9 of Encina.Compliance.eIDAS2 (Issue #805).

CONTEXT:
- #812 PR 1 is merged; IncidentRegulation lives in Encina.Compliance.BreachNotification (Reporting/).

TASK:
1. Add the enum member and the mapper; documentation goes to the docs-writer.

KEY RULES:
- Update every consumer of IncidentRegulation found by git grep (switches, serializers, tests) in the same PR.

REFERENCE FILES: docs/plans/enisa-single-entry-point-implementation-plan-812.md
```

</details>

---

## Research

### Regulatory References

| Source | Requirement | Applies in this plan |
| :--- | :--- | :--- |
| Reg. (EU) 2024/1183 Art. 5f | Relying parties accept the wallet | Wallet credential verification |
| Reg. (EU) 910/2014 Art. 22 and ETSI TS 119 612 | Trusted lists | `ITrustedListProvider` |
| Implementing Reg. (EU) 2015/1502 | Levels of assurance | `AssuranceLevel` |
| Reg. (EU) 910/2014 Arts. 25-35, 41-42 | Qualified signatures, seals, time-stamps | `QualifiedSignatureProvider`, `QualifiedTimeStampAuthority` |
| RFC 3161 | Time-stamp protocol | Through #1241 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
| :--- | :--- | :--- |
| `ITimeStampAuthority`, `ISignatureProvider` | #1241 plan, `Encina.Compliance.Evidence` | Reused ports |
| `IIncidentReportingAdapter`, `IncidentRegulation` | #812 plan, `Encina.Compliance.BreachNotification` | Incident reporting |
| `ICertificateReader` | #1246 | Signer certificates per tenant |
| `BreachProjection`, `BreachDeadlineMonitorService` | `src/Encina.Compliance.BreachNotification` | Projection and loop patterns |
| `NIS2Diagnostics`, `NIS2Errors` | `src/Encina.Compliance.NIS2` | Style |
| `ErrorCategory` | `src/Encina/Errors/ErrorCategory.cs` (ADR-062) | Error categories |

### Event ID Allocation

| Package | Range | Notes |
|---|---|---|
| `Encina.Compliance.eIDAS2` | `52900-52949` (`ComplianceEIDAS2`, new, Security and compliance zone) | Uses 52900-52929; 52930-52949 free. After the claims known on 2026-11-25 (up to 52749, 52750-52799 for #827, 52800-52899 for #804); the orchestrator confirms against the registry when the PR opens |

### Estimated File Count

| Category | Files | Notes |
|---|---|---|
| Production | 36 | Models, ports, verifiers, provider, aggregate, services |
| Metadata (csproj, slnx, packages props, PublicAPI, manifest, registry, README, fragment) | 24 | Counted per PR |
| Tests | 35 | Unit, guard, contract, property, integration, `.md` justifications, project references |
| Documentation page | 1 | |

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina is a .NET 10 / C# 14 pre-1.0 library with ROP. Issue #805 adds Encina.Compliance.eIDAS2: wallet credential verification (relying party), trusted-list consumption, levels of assurance, qualified signing and time-stamping on top of #1241's ports, a verification evidence stream, and incident reporting through #812.

IMPLEMENTATION OVERVIEW: eight pull requests (skeleton, trusted list, verifier, status and composition, evidence, qualified services, DI and observability, reporting).

KEY PATTERNS: fail closed (unknown issuer, stale list, unreachable status list, missing tenant deny); no attribute values in events or logs; injective tenant-qualified keys with the reserved 'default' segment; ErrorCategory per ADR-062; no _total counters (ADR-054); no InMemory store for the evidence stream; scoped services never injected into singletons; optional constructor parameters last.

REFERENCE FILES: docs/plans/trusted-timestamp-signature-implementation-plan-1241.md; docs/plans/enisa-single-entry-point-implementation-plan-812.md; src/Encina.Compliance.NIS2
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ | The trusted-list snapshot is cached in memory by the provider with a maximum age; credential results are never cached |
| 2 | OpenTelemetry | ✅ | `eidas2.*` instruments and activities; tenant tag with multi-tenancy (ADR-054 D4) |
| 3 | Structured Logging | ✅ | Ids 52900-52929 |
| 4 | Health Checks | ✅ | Trusted-list freshness |
| 5 | Validation | ✅ | Presentation and options validation return `Left` |
| 6 | Resilience | ✅ | Retry and circuit breaker on trusted-list, status-list and signing HTTP calls (`Encina.Http.Resilience` handlers on the named clients) |
| 7 | Distributed Locks | ❌ | The only shared mutable state is a per-process snapshot; each instance refreshes its own |
| 8 | Transactions | ❌ | One evidence stream append per verification; the repository commit is the owner |
| 9 | Idempotency | ❌ | Verification is idempotent by nature; the evidence append is keyed by a random stream id |
| 10 | Multi-Tenancy | ✅ | Tenant and module ids on every event; per-tenant named clients and certificates; missing tenant denies |
| 11 | Module Isolation | ✅ | `ModuleId` on every event |
| 12 | Audit Trail | ✅ | The evidence stream is the audit trail of acceptance decisions |

---

## Prerequisites & Dependencies

- #1241 (open, plan on main): `ITimeStampAuthority` and `ISignatureProvider`. #812 (open, plan on main): the reporting port, needed for PR 8 only. #1246 (`ICertificateReader`) for per-tenant signer certificates in PR 6. #3622 (open) is independent (it consumes `ITimeStampAuthority` for chained logs).
- #2668 (open) builds the shared rule-code catalogue; this plan adds its codes to `EIDAS2Errors`.

## Next Steps

1. Maintainer answers Dilemmas 3, 4, 6 and 8 when work on #805 starts; each is marked "recommended, pending the maintainer".
2. Brief PR 1 after the #1241 ports have merged; PR 2 to PR 8 follow in order, PR 8 after #812 PR 1.
3. Run `check-plan.ps1` without `-Draft` after the adversarial review sets the `Verified` lines.
