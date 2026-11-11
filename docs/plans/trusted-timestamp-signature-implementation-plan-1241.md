# Implementation Plan: `Encina.Compliance.Evidence` — Trusted Time-Stamping (RFC 3161) and E-Signature Integration Points

> **Issue**: [#1241](https://github.com/dlrivada/Encina/issues/1241) (SPEC-002 tracking id **P-39**, priority **P0**)
> **Type**: Feature
> **Complexity**: Medium-High (8 phases, new package, no provider-specific code, ~42 files, 3 pull requests)
> **Estimated Scope**: ~1,500-2,000 lines of production code + ~1,900-2,500 lines of tests
> **Milestone**: v0.17.0 — Compliance Lifecycle
> **Parent EPIC**: [#1186](https://github.com/dlrivada/Encina/issues/1186) — EU regulatory readiness (SPEC-002)
> **Specification**: [SPEC-002](../specifications/SPEC-002-eu-regulatory-readiness.md) REQ-048, AC-039, section 5.11 (generic extension points), DEC-015 (eIDAS 2 is post-1.0, #805); cross-cutting REQ-061 (AC-043) and REQ-062 (AC-044)
> **Depends on**: [#1233](https://github.com/dlrivada/Encina/issues/1233) (P-31, `Encina.Http`; plan [http-implementation-plan-1233.md](http-implementation-plan-1233.md)) for the RFC 3161 client; [#1240](https://github.com/dlrivada/Encina/issues/1240) (P-38, `Encina.ChainedLog`; plan [chained-log-implementation-plan-1240.md](chained-log-implementation-plan-1240.md)) for the chained-log hook
> **Related**: [#1246](https://github.com/dlrivada/Encina/issues/1246) (`ICertificateReader`, X.509 retrieval per tenant), [#805](https://github.com/dlrivada/Encina/issues/805) (eIDAS 2, post-1.0)

---

## Summary

Clinical consents, documents and chained records benefit from evidential time (a trusted third party states that a hash existed at a given instant) and from signatures (SPEC-002 note F, G17). SPEC-002 section 5.11 puts **generic extension points** for both in 1.0 and leaves a full eIDAS 2 module to after 1.0 (#805, DEC-015). Today Encina has no such point: `Encina.Compliance.Attestation` has a hash-chain provider and an HTTP attestation provider for its own service, but nothing that talks RFC 3161 or signs and verifies a document.

This plan delivers:

1. A new package **`Encina.Compliance.Evidence`** with `ITimeStampAuthority` (request a token for a digest, verify a token) and `ISignatureProvider` (sign, verify), their models and coded errors.
2. **`Rfc3161TimeStampAuthority`**: an RFC 3161 client built on `System.Security.Cryptography.Pkcs` (`Rfc3161TimestampRequest`, `Rfc3161TimestampToken`; no third-party library) that sends the request over a named `HttpClient` from `Encina.Http` (#1233), so credentials and client certificates resolve per tenant and calls are rate limited and redacted by that package.
3. **`CertificateSignatureProvider`**: a CMS (PKCS #7, detached) signature provider over an `X509Certificate2` resolved per tenant (`ICertificateReader`, #1246). It is a plain electronic signature and is documented as **not** a qualified electronic signature; applications that need a qualified signature back `ISignatureProvider` with their provider.
4. **Optional hooks** from the chained log (P-38) and Attestation: extension methods that time-stamp a `ChainedLogEntry` or an `AttestationReceipt` on request and return the token to the caller, plus notifications that audit handlers can record. Automatic, durable stamping is a follow-up (Design Choice 4).

Qualification is not decided by this code. A token is a qualified time-stamp only if the authority is a qualified trust service provider on the EU trusted list; the package verifies the token against trust roots the operator configures and says so in its documentation. It does not validate the EU list of trusted lists (out of scope, post-1.0 with #805).

**Standards**: RFC 3161 (Time-Stamp Protocol), RFC 5652 (CMS), eIDAS Reg. (EU) 910/2014 Art. 41-42 (electronic time stamps) and as amended by Reg. (EU) 2024/1183 (eIDAS 2) [S]; GDPR Art. 5(1)(f), Art. 32; SPEC-002 REQ-048.

**Affected packages**: new `Encina.Compliance.Evidence`; `Encina` (core: one EventId range); `Encina.Http` and `Encina.ChainedLog` and `Encina.Compliance.Attestation` and `Encina.Security.Secrets` are consumed and not modified.

**Provider category**: none. The issue states "no provider-specific code": the package keeps no store, so the 10-provider matrix does not apply. Persisting tokens inside the chained log is a deferred issue (Design Choice 4).

**Size**: about 42 files, so three worker pull requests: PR 1 (abstractions, models, errors, options, RFC 3161 client and verification, diagnostics, unit and guard tests); PR 2 (signature provider, hooks, DI, health check, notifications, contract and integration tests with a fake authority); PR 3 (documentation, manifests follow-ups, load/benchmark justifications). Each stays within 15 files and 1,500 lines.

---

## Design Choices

<details>
<summary><strong>1. Package Placement — a new package <code>Encina.Compliance.Evidence</code></strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) New package `Encina.Compliance.Evidence` referencing `Encina.Http`, `Encina.ChainedLog` and `Encina.Compliance.Attestation`** | Applications that only use attestation do not pull `Encina.Http` and `System.Security.Cryptography.Pkcs`; the extension points are opt-in | One more package; it references the two evidence-producing packages for the hooks |
| **B) Inside `Encina.Compliance.Attestation`** | The issue lists Attestation as affected; no new package | Every Attestation user takes `Encina.Http` and the Pkcs library; Attestation grows beyond attestation |
| **C) Two packages, `Encina.TimeStamping` and `Encina.Signatures`** | Smallest units | The two share models, options, tenant handling and hooks; two packages for two small abstractions |

### Chosen Option: **A — one new package**

### Rationale

- The issue says "home package fixed in the implementation plan"; pay-for-what-you-use (AGENTS.md section 1) puts an optional capability that needs an HTTP client and a cryptography package into a package of its own.
- The name is regulation-neutral (evidence), consistent with the neutral naming of `Encina.ChainedLog`; eIDAS appears in the documentation, not in type or option names.

**Class:** Settled
**Settled by:** AGENTS.md section 1 ("pay-for-what-you-use (every feature opt-in)") and section 3 (provider coherence: abstractions and implementations placed by dependency); the issue body ("home package fixed in the implementation plan")

</details>

<details>
<summary><strong>2. RFC 3161 Implementation — the platform library, a named <code>HttpClient</code> from <code>Encina.Http</code></strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `System.Security.Cryptography.Pkcs` (`Rfc3161TimestampRequest`, `Rfc3161TimestampToken`) for the request, parsing and signature check; transport through a named client registered with `AddEncinaHttpClient` of `Encina.Http`** | No third-party dependency (the package is already version-managed by the repository); per-tenant credentials, mTLS, rate limits and redaction come from #1233 | Depends on #1233 being implemented; the platform API cannot build a token (tests build one by hand) |
| **B) A third-party RFC 3161 library (for example BouncyCastle)** | Can also create tokens | A large dependency for a client that only requests and verifies |
| **C) Hand-written ASN.1 request and response parsing** | No dependency | Security-sensitive parsing we should not own |

### Chosen Option: **A — platform Pkcs library over `Encina.Http`**

### Rationale

- The client sends only the digest (`Rfc3161TimestampRequest.CreateFromHash`), a random nonce and `RequestSignerCertificates = true`; the document never leaves the application.
- Verification checks, in order: the response status (granted, grantedWithMods; anything else is `Left(evidence.timestamp_rejected)` with the status code), the nonce, the message imprint against the expected digest and algorithm, the CMS signature over the TSTInfo, and the signer certificate chain against `TimeStampOptions.TrustedRoots` **at the token's generation time** (`ChainPolicy.VerificationTime`), with the `id-kp-timeStamping` extended key usage. Revocation checking is on by default (`X509RevocationMode.Online`); an unreachable revocation source makes verification `IsValid = false` (fail closed), and `RevocationMode = Offline` is an explicit, logged opt-out for air-gapped systems.
- A Right with `IsValid = false` and a code describes a bad token; a `Left` means the verification could not run (for example no trust roots configured).
- The fake authority of the tests builds a TSTInfo with `AsnWriter`, signs it with `SignedCms` and a test certificate, and serves it through `Encina.Testing.WireMock`.

**Class:** Settled
**Settled by:** RFC 3161 and RFC 5652; the issue body ("an RFC 3161 HTTP client implementation over `Encina.Http` (P-31)"); AGENTS.md section 3 ("Compliance and security gates fail closed"); `Directory.Packages.props` (`System.Security.Cryptography.Pkcs` is already managed)

</details>

<details>
<summary><strong>3. Signature Provider — a CMS reference implementation, with the qualification caveat</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Ship `ISignatureProvider` plus `CertificateSignatureProvider` (detached CMS with a per-tenant certificate from `ICertificateReader`); document that it is not a qualified signature and that applications back the interface with their qualified provider** | The abstraction is used and tested by something real (AGENTS.md: every line serves a current purpose); a test double is not the only implementation | The name may be mistaken for a qualified signature; mitigated by documentation and by `SignatureLevel` in the result |
| **B) Ship only the interface (the issue's "applications back with their provider")** | Smallest | An abstraction with no implementation is untestable in the package and is dead code until an application writes one |
| **C) Ship PAdES/XAdES/CAdES profiles** | Closer to eIDAS formats | A signature-format module: the eIDAS 2 module that is post-1.0 (#805) |

### Chosen Option: **A — interface plus a plain CMS reference implementation**

### Rationale

- `SignatureResult` carries `SignatureLevel` (`Plain` for the shipped provider; an application's provider may report `Advanced` or `Qualified`, which Encina does not check), the signature bytes, the algorithm, the signer certificate thumbprint and the signing time from `TimeProvider`.
- The certificate (with private key) is resolved per tenant by `ICertificateReader` (#1246); the private key is never logged or serialized.
- Verification checks the CMS signature, the chain against `SignatureOptions.TrustedRoots` and the certificate validity at the signing time.

**Class:** Settled
**Settled by:** AGENTS.md section 3 ("NEVER add ... legacy code" / every line serves a current purpose, section 1); SPEC-002 section 5.11 and DEC-015 (generic points now, eIDAS 2 module post-1.0)

</details>

<details>
<summary><strong>4. The Hooks — explicit helpers now; automatic durable stamping later</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Explicit helpers: `TimeStampAsync(this ITimeStampAuthority, ChainedLogEntry)` and `TimeStampAsync(..., AttestationReceipt)` return a `TimeStampEvidence` (token plus the entry reference); a notification `EvidenceTimeStampedNotification` lets the application persist or audit it; no store and no background work in this issue** | Matches the issue's matrix (no provider-specific code); the application decides when to call and where to keep the token; never holds a database lock while calling a network service | The application must call the helper and store the token itself |
| **B) Automatic stamping: a decorator on `IChainedLog` that stamps every appended entry after commit, with a durable pending queue and token columns in the chained log on the 10 providers** | Complete evidence with no application code; a catch-up job fixes crashes | Provider-specific code on 10 providers plus a worker and queue: a separate feature larger than this issue, and it modifies the schema of #1240 |
| **C) Automatic stamping in memory only (a channel and a worker)** | No schema | Lost on crash, so entries silently stay unstamped; not evidence |

### Chosen Option: **A — explicit helpers (recommended, pending the maintainer)**

### Rationale

- Scenario in which A wins: the scope the issue declares ("Provider Implementation Matrix: Not applicable"), and applications that stamp selected records (a signed consent, a closing invoice batch) rather than every entry.
- Scenario in which B wins: the reference application wants every chained-log entry stamped without writing code and wants the tokens retrievable with the chain; then token persistence belongs in the chained log on all providers.
- Recommendation A for this issue because B needs the chained log (#1240) to exist first and is a 10-provider feature of its own; a follow-up issue proposing B ("Automatic time-stamping of chained-log entries with persisted RFC 3161 tokens on the 10 database providers") is opened with this plan, so nothing is forgotten.
- The helpers never run inside a database transaction on the caller's behalf: stamping happens after the append has committed, with the entry's hash as the digest.

**Class:** Dilemma
**Settled by:** none; awaits the maintainer

</details>

<details>
<summary><strong>5. Per-Tenant Endpoints and Credentials</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A default TSA endpoint plus an optional per-tenant endpoint map in `TimeStampOptions`; credentials (API key, bearer, client certificate) resolved per tenant by the `Encina.Http` auth providers and `ISecretReader` under a name composed from client, tenant and purpose; with tenancy required and no tenant, `Left(evidence.tenant_required)`** | Meets AC-043 ("TSA and signature credentials resolve per tenant"); secrets stay in the secrets providers | A tenant map in options for endpoints |
| **B) One global endpoint and credential** | Simplest | A tenant's credential or certificate could be used for another; fails AC-043 |
| **C) A store of tenant TSA settings** | Dynamic | A new store on 10 providers, outside this issue's matrix |

### Chosen Option: **A — default endpoint, tenant map, secrets-based credentials**

### Rationale

- Plain `http://` endpoints are refused unless `AllowInsecureHttp` is set (an explicit, logged opt-out): the token is signed, but the request leaks the digest and metadata, and defaults must fail closed.
- Tenant resolution follows `DefaultConsentService` (`RequireTenantContext`, ambient tenant, fixed default tenant when tenancy is off).
- The signature certificate name is composed the same way (`evidence-signing`, tenant).

**Class:** Settled
**Settled by:** SPEC-002 REQ-061 and AC-043 (as written in the issue's acceptance criteria: "TSA and signature credentials resolve per tenant"); plan #1233 (per-client, per-tenant handlers and secret names); AGENTS.md section 3 (fail closed)

</details>

---

## Implementation Phases

### Phase 1: Core Models, Abstractions and Errors (`Encina.Compliance.Evidence`)

<details>
<summary><strong>Tasks</strong></summary>

#### `src/Encina.Compliance.Evidence/` (new package)

1. **`Encina.Compliance.Evidence.csproj`** (references `Encina`, `Encina.Http`, `Encina.ChainedLog`, `Encina.Compliance.Attestation`, `Encina.Security.Secrets`, `System.Security.Cryptography.Pkcs`, PublicApiAnalyzers), `PublicAPI.Shipped.txt`/`Unshipped.txt`, `README.md`, `Encina.slnx`, coverage manifest (written by `mechanical-fixer`)
2. **`Model/TimeStampRequest.cs`** — `HashAlgorithmName`, `Digest` (`ReadOnlyMemory<byte>`), `Policy?` (OID), `RequestCertificate` (default true)
3. **`Model/TimeStampToken.cs`** — `Encoded` (bytes), `GenerationTimeUtc`, `SerialNumber`, `HashAlgorithm`, `Imprint`, `TsaName?`, `PolicyOid?`
4. **`Model/TimeStampVerification.cs`** — `IsValid`, `Code?` (`evidence.imprint_mismatch`, `evidence.chain_invalid`, `evidence.signature_invalid`, `evidence.revocation_unavailable`, ...), `GenerationTimeUtc?`, `VerifiedAtUtc`
5. **`Model/SignatureRequest.cs`, `Model/SignatureResult.cs`, `Model/SignatureVerification.cs`, `Model/SignatureLevel.cs`** (`Plain`, `Advanced`, `Qualified`)
6. **`Abstractions/ITimeStampAuthority.cs`** — `RequestAsync(TimeStampRequest, ct)` and `VerifyAsync(TimeStampToken, ReadOnlyMemory<byte> expectedDigest, HashAlgorithmName, ct)`, both `Either<EncinaError, T>`
7. **`Abstractions/ISignatureProvider.cs`** — `SignAsync(SignatureRequest, ct)` and `VerifyAsync(SignatureResult, ReadOnlyMemory<byte> digest, ct)`
8. **`EvidenceErrors.cs`** — `evidence.timestamp_rejected`, `evidence.timestamp_failed`, `evidence.tenant_required`, `evidence.trust_roots_missing`, `evidence.endpoint_insecure`, `evidence.certificate_unavailable`, `evidence.signature_failed` (codes and non-identifying details; never digests, tokens or certificate text)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
- Issue #1241 (SPEC-002 P-39). New package src/Encina.Compliance.Evidence (does not exist yet). Style reference for a small
  compliance package: src/Encina.Compliance.Attestation. System.Security.Cryptography.Pkcs is version-managed in
  Directory.Packages.props.
- Pre-1.0: no [Obsolete], no compatibility layers.

TASK:
Create the package skeleton, models, abstractions and errors listed in the Phase 1 tasks.

KEY RULES:
- ROP: every operation returns Either<EncinaError, T>; a bad token is a Right with IsValid = false and a code; Left means the
  check could not run.
- Neutral naming (evidence, time stamp, signature); no regulation-named type or option.
- No digest, token bytes or certificate content in error details.
- XML docs on every public member, with an example; PublicAPI files complete; README states that qualification is not
  decided by this code.

REFERENCE FILES:
- src/Encina.Compliance.Attestation/ (layout, AttestationErrors.cs, Abstractions/IAuditAttestationProvider.cs)
- Directory.Packages.props
```

</details>

---

### Phase 2: RFC 3161 Client and Token Verification

<details>
<summary><strong>Tasks</strong></summary>

1. **`TimeStamp/Rfc3161TimeStampAuthority.cs`** (public sealed, `ITimeStampAuthority`) — builds `Rfc3161TimestampRequest.CreateFromHash` with a cryptographic-random nonce; posts `application/timestamp-query` through the named `HttpClient` (`IHttpClientFactory`); reads `application/timestamp-reply`; maps HTTP and protocol failures to coded `Left`; response size capped (`MaxResponseBytes`, default 256 KB)
2. **`TimeStamp/TimeStampTokenVerifier.cs`** (internal) — implements Design Choice 2's verification order (status, nonce, imprint, CMS signature, chain at generation time with the time-stamping EKU, revocation per option)
3. **`TimeStampOptions.cs`** — `Endpoint`, `TenantEndpoints`, `ClientName` (default `encina-timestamp-authority`), `TrustedRoots` (`X509Certificate2Collection`, not serialized), `RevocationMode` (default Online), `AllowInsecureHttp` (default false), `PolicyOid?`, `MaxResponseBytes`, `Timeout`; `TimeStampOptionsValidator` (https unless opted out, roots present, endpoint absolute)
4. Token creation by Encina is not implemented (not needed; the platform cannot create tokens, the test fake does)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
- Rfc3161TimestampRequest.CreateFromHash(ReadOnlyMemory<byte> hash, HashAlgorithmName, Oid? policy, ReadOnlyMemory<byte>? nonce,
  bool requestSignerCertificates, ...), Rfc3161TimestampToken.TryDecode(...), token.VerifySignatureForHash(...),
  token.TokenInfo (GetMessageHash, GetNonce, Timestamp, SerialNumber, PolicyId) are in System.Security.Cryptography.Pkcs.
- Transport: a named client registered by Encina.Http (plan #1233: AddEncinaHttpClient(name, ...)); it applies per-tenant
  auth, mTLS, rate limits and redaction, and raises typed rejections that become coded Lefts.

TASK:
Implement Rfc3161TimeStampAuthority, the verifier and the options with validation as listed in Phase 2.

KEY RULES:
- Fail closed: an unreachable revocation source or missing trust roots is never "valid".
- Nonce from RandomNumberGenerator; compare in constant time; the document never leaves the application, only its digest.
- Async HTTP with a CancellationToken; bounded response size; no response body in logs or errors.
- Time comes from TimeProvider for VerifiedAtUtc; the token's own generation time is data, not "now".
- CRAP <= 10 per method: one method per verification step.

REFERENCE FILES:
- src/Encina.Compliance.Attestation/Providers/HttpAttestationProvider.cs
- docs/plans/http-implementation-plan-1233.md
```

</details>

---

### Phase 3: Signature Provider

<details>
<summary><strong>Tasks</strong></summary>

1. **`Signing/CertificateSignatureProvider.cs`** (public sealed, `ISignatureProvider`) — resolves the tenant's certificate through `ICertificateReader` (name `evidence-signing`), signs the digest with a detached `SignedCms` (`CmsSigner`, SHA-256 by default), returns `SignatureResult` with `SignatureLevel.Plain`; `VerifyAsync` checks the CMS signature, the chain against `SignatureOptions.TrustedRoots` and the certificate validity at the signing time
2. **`SignatureOptions.cs`** — `CertificateName`, `DigestAlgorithm`, `TrustedRoots`, `RevocationMode`, validator
3. A missing certificate or tenant is `Left` (`evidence.certificate_unavailable`, `evidence.tenant_required`), never a fallback to another tenant's certificate

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
- ICertificateReader (plan #1246) returns an X509Certificate2 with private key per tenant from the secret providers.
- SignedCms with ContentInfo(digest) and detached = true; CmsSigner(certificate) with the digest algorithm.

TASK:
Implement CertificateSignatureProvider and its options as in Phase 3.

KEY RULES:
- Plain signature only; never claim Advanced or Qualified; documentation says so.
- Private key never logged, serialized or exposed; options with key material are [JsonIgnore] with ToString() override.
- Tenant never defaults to another tenant's certificate; fail closed.
- TimeProvider for the signing time.

REFERENCE FILES:
- docs/plans/certificate-reader-implementation-plan-1246.md
- src/Encina.Compliance.Attestation/Providers/HashChainAttestationProvider.cs (key handling style)
```

</details>

---

### Phase 4: Hooks, Configuration and DI

<details>
<summary><strong>Tasks</strong></summary>

1. **`Hooks/ChainedLogTimeStampExtensions.cs`** — `TimeStampAsync(this ITimeStampAuthority, ChainedLogEntry entry, ct)`: digest = the entry's hash bytes with the hasher's algorithm; returns `Either<EncinaError, TimeStampEvidence>` (`TimeStampEvidence`: token, `Reference` = chain tenant, partition and sequence); `VerifyTimeStampAsync(...)`
2. **`Hooks/AttestationTimeStampExtensions.cs`** — same for an `AttestationReceipt` (digest = the receipt's content hash)
3. **`Notifications/EvidenceTimeStampedNotification.cs`, `EvidenceSignedNotification.cs`** — published after a successful stamp or signature through the Encina publisher (tenant, outcome, generation time; no token bytes, no digest) so applications can persist or audit the evidence
4. **`ServiceCollectionExtensions.AddEncinaEvidence(Action<EvidenceOptions>)`** — `UseRfc3161TimeStamping(Action<TimeStampOptions>)` (registers the named `Encina.Http` client, `Rfc3161TimeStampAuthority`, validator with `ValidateOnStart`) and `UseCertificateSignatures(Action<SignatureOptions>)`; each is opt-in; DI test with `ValidateOnBuild` and `ValidateScopes`
5. **Health check** — `EvidenceHealthCheck` (`DefaultName` const, static `Tags`, scoped resolution): configuration valid, trust roots present, and the rolling success rate of recent time-stamp requests; it never makes a paid probe request

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
- ChainedLogEntry and AttestationReceipt come from Encina.ChainedLog (plan #1240) and Encina.Compliance.Attestation.
  Notifications are INotification records published through the Encina publisher.

TASK:
Implement the two hook extension classes, the notifications, the registration and the health check as in Phase 4.

KEY RULES:
- Registration completeness: every option type and dependency is registered, proven by a provider built with ValidateOnBuild
  and ValidateScopes; both capabilities are opt-in and off by default.
- Hooks never run inside a database transaction on the caller's behalf.
- Health check: DefaultName const, Tags static array, scoped resolution via IServiceProvider.CreateScope().
- No digest, token bytes or certificate content in notifications, logs or tags.

REFERENCE FILES:
- src/Encina.Compliance.Attestation/ServiceCollectionExtensions.cs
- src/Encina.Compliance.Attestation/Health/AttestationHealthCheck.cs
- docs/plans/chained-log-implementation-plan-1240.md
```

</details>

---

### Phase 5: Cross-Cutting Integration

<details>
<summary><strong>Tasks</strong></summary>

1. **Multi-tenancy** — tenant endpoint, credential and certificate resolution (Design Choice 5); two-tenant test: tenant A's request never uses tenant B's endpoint, credential or certificate (REQ-061, AC-043)
2. **Resilience** — transient TSA failures are retried by the resilience handler composed on the named `Encina.Http` client (ADR-055: one retry owner); a retried request is safe because a second token for the same digest is harmless
3. **Validation** — options validators; request validation (digest length matches the algorithm)
4. **Audit trail** — the two notifications; the application records them with its audit store
5. **Health** — Phase 4
6. **Evaluated and not integrated** — caching, locks, transactions, idempotency, module isolation (see the matrix)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT:
- Tenant resolution decision table: DefaultConsentService (RequireTenantContext, ambient tenant, fixed default tenant when
  tenancy is off). Encina.Http composes retry through the standard resilience handler (ADR-055).

TASK:
Wire tenant resolution into the authority and signature provider and add the two-tenant tests.

KEY RULES:
- Tenant comes from IRequestContext, never from request data.
- With tenancy off, a fixed default tenant applies and no tenant configuration is needed.
- No second retry layer inside the authority.

REFERENCE FILES:
- src/Encina.Compliance.Consent/ (DefaultConsentService)
- docs/plans/http-implementation-plan-1233.md
```

</details>

---

### Phase 6: Observability

<details>
<summary><strong>Tasks</strong></summary>

The names in the issue (`encina.timestamp.requests`, `encina.timestamp.request`, `server.address`) were proposals; the plan names them after the package: source and meter `Encina.Compliance.Evidence`.

1. **`Diagnostics/EvidenceDiagnostics.cs`** — `ActivitySource` and `Meter` `Encina.Compliance.Evidence`; counters `evidence.timestamps.requested.total` (tag `evidence.outcome`), `evidence.timestamps.verified.total` (tag `evidence.outcome`), `evidence.signatures.total` (tags `evidence.operation`, `evidence.outcome`); histogram `evidence.timestamp.duration.ms`; activities `Evidence.TimeStamp`, `Evidence.VerifyTimeStamp`, `Evidence.Sign` with `evidence.tenant_id` and `evidence.hash_algorithm`; no digest, token, endpoint query or certificate content
2. **`EventIdRanges.cs`** — register `ComplianceEvidence = (6590, 6599)` (from the batch block 6550-6599) and map `Encina.Compliance.Evidence` in `EncinaEventIdAllocationTests`
3. **`Diagnostics/EvidenceLogMessages.cs`** — `[LoggerMessage]` 6590 onward, packed: `TimeStampRequested` (6590, Debug), `TimeStampGranted` (6591, Information; generation time and serial only), `TimeStampFailed` (6592, Error; code), `TimeStampRejected` (6593, Warning; status code), `TokenVerified` (6594, Debug), `TokenInvalid` (6595, Warning; code), `SignatureProduced` (6596, Debug), `SignatureFailed` (6597, Error; code), `TrustRootsMissing` (6598, Critical, start-up), `InsecureEndpointAllowed` (6599, Warning, start-up)
4. The host name of the authority is not a tag; `server.address` from the issue's proposal is dropped (an endpoint can identify the tenant's provider contract)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
CONTEXT:
- Register the new range in src/Encina/Diagnostics/EventIdRanges.cs first (ADR-021). 6590-6599 is the part of the plan
  batch's block 6550-6599 assigned to this plan.

TASK:
Add the diagnostics, the range registration, the allocation-test mapping and the [LoggerMessage] entries.

KEY RULES:
- Pack EventIds sequentially from 6590; XML docs name the range ("Event IDs: 6590-6599 (see EventIdRanges.ComplianceEvidence)").
- No digests, tokens, certificate content, endpoint URLs or Error.Message in tags, metrics or logs; exceptions via ForLogging().
- Tenant as "evidence.tenant_id".

REFERENCE FILES:
- src/Encina.Compliance.Attestation/Diagnostics/AttestationDiagnostics.cs and AttestationLogMessages.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

---

### Phase 7: Testing

<details>
<summary><strong>Tasks</strong></summary>

Manifest first: per-file targets with one-sentence justifications for every new file in `.github/coverage-manifest/Encina.Compliance.Evidence.json` (written by `mechanical-fixer`).

1. **UnitTests** — request building (nonce, imprint, algorithm), every verification step failing alone (status, nonce, imprint, signature, chain, EKU, revocation unavailable, expired signer at generation time), options validators (http refused, roots required), tenant resolution, signature sign and verify round trip with a test certificate, hooks and notifications, telemetry with an in-memory exporter (tenant tag present; no digest, token or URL anywhere), health check, DI tests
2. **GuardTests** — all public constructors, methods and extension methods
3. **ContractTests** — `ITimeStampAuthority` and `ISignatureProvider` contracts against the real implementations and the fake authority
4. **PropertyTests** — justified: protocol client; covered by integration tests against a fake authority (as the issue's matrix says)
5. **IntegrationTests** — fake RFC 3161 authority hosted with `Encina.Testing.WireMock` (a TSTInfo built with `AsnWriter`, signed with `SignedCms` and a test certificate): granted token verifies; wrong imprint, wrong nonce, rejected status, untrusted chain, oversize response and a 503 each produce the right code; two-tenant credentials; the chained-log and Attestation hooks on real entries and receipts (AC-039)
6. **Load/Benchmark** — `.md` justifications: stamping and signing are not concurrent hot paths
7. **PR split** — PR 1 Phases 1-2, 6 (diagnostics) with unit and guard tests; PR 2 Phases 3-5 with contract and integration tests; PR 3 documentation

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
CONTEXT:
- Test projects are consolidated under tests/ (UnitTests, GuardTests, ContractTests, IntegrationTests). Assertions use
  Shouldly via Encina.Testing.Shouldly; fake HTTP services use Encina.Testing.WireMock.

TASK:
Implement the tests listed in Phase 7 and the manifest targets, then measure per-flag coverage.

KEY RULES:
- Tests execute real package code; no reflection-only tests; contract tests instantiate real implementations.
- The fake authority builds real RFC 3161 tokens (AsnWriter + SignedCms) so verification is exercised for real.
- No Thread.Sleep; FakeTimeProvider; deterministic; builders for test data; test certificates generated per run, never committed.
- A test never swallows an exception thrown by the code it drives.

REFERENCE FILES:
- .github/coverage-manifest/Encina.Compliance.Attestation.json
- tests/Encina.UnitTests/Compliance/Attestation/
- src/Encina.Testing.WireMock/
```

</details>

---

### Phase 8: Documentation & Finalization

<details>
<summary><strong>Tasks</strong></summary>

1. XML doc comments on all new public APIs (`<summary>`, `<remarks>`, `<param>`, `<returns>`, `<example>`)
2. `changelog.d/1241-evidence-timestamp-signature.added.md`
3. `src/Encina.Compliance.Evidence/README.md`; a short section in the READMEs of `Encina.ChainedLog` and `Encina.Compliance.Attestation` pointing to the hooks
4. `docs/features/` page: configure a time-stamp authority and trust roots, stamp a chained-log entry, sign a document, what is and is not qualified, why tokens are not stored by Encina yet
5. `docs/INVENTORY.md`; `PublicAPI.Unshipped.txt`; `Encina.slnx`
6. ADR: not needed (SPEC-002 section 5.11 and DEC-015 decide the scope; the choices follow AGENTS.md). ADR number assigned when the ADR pull request opens, if the maintainer asks for one
7. Follow-up issue file for persisted tokens (Design Choice 4)
8. Build verification: `dotnet build --configuration Release` with 0 warnings; `dotnet test` green; every coverage flag reaches its manifest target; no hand-typed coverage figures (cite `cov:` markers)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```
CONTEXT:
- Documentation rules: .claude/skills/encina-docs/SKILL.md. Changelog fragments: changelog.d/README.md.

TASK:
Document the time-stamping and signature extension points, including their limits.

KEY RULES:
- English only; no private client application named (write "the reference application").
- State plainly that qualification (a qualified time stamp or signature) is decided by the trust service provider and the
  trust list, not by this code, and that eIDAS 2 as a module is post-1.0.
- Never type coverage figures; cite cov: markers. Never edit CHANGELOG.md.

REFERENCE FILES:
- src/Encina.Compliance.Attestation/README.md
- docs/specifications/SPEC-002-eu-regulatory-readiness.md (section 5.11)
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Relevance |
|--------|-----------|-----------|
| RFC 3161 | Time-Stamp Protocol (request, response, TSTInfo) | The client and the verification steps |
| RFC 5652 | Cryptographic Message Syntax | Token signature and the detached signature |
| eIDAS | Reg. (EU) 910/2014 Art. 41-42, as amended by Reg. (EU) 2024/1183 [S] | Electronic time stamps and their qualified level (decided by the provider, not by this code) |
| GDPR | Art. 5(1)(f), Art. 32 | Integrity; the digest is sent, never the document |
| SPEC-002 | REQ-048, AC-039, section 5.11, DEC-015, REQ-061, REQ-062 | The requirement and the scope limit (eIDAS 2 module post-1.0, #805) |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|----------------------|
| `Encina.Http` (named clients, per-tenant auth and mTLS, rate limits, redaction) | Plan #1233 | Transport of the RFC 3161 request |
| `ICertificateReader` | Plan #1246 (`Encina.Security.Secrets`) | Per-tenant signing certificate |
| `Encina.ChainedLog` (`ChainedLogEntry`) | Plan #1240 | Hook: stamp an entry hash |
| `AttestationReceipt`, `IAuditAttestationProvider` | `src/Encina.Compliance.Attestation/` | Hook: stamp a receipt's content hash; package layout and health check style |
| `System.Security.Cryptography.Pkcs` | `Directory.Packages.props` | RFC 3161 request and token, CMS signatures |
| `Encina.Testing.WireMock` | `src/Encina.Testing.WireMock/` | Fake authority |
| `DefaultConsentService` tenant resolution | `src/Encina.Compliance.Consent/` | Tenant decision table |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Compliance.Evidence` | 6590-6599 (`ComplianceEvidence`, new, from the batch block 6550-6599) | Uses all ten ids; to be registered in `EventIdRanges.cs` by PR 1 |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Production | ~24 | csproj, models, abstractions, errors, RFC 3161 client and verifier, signature provider, options and validators, hooks, notifications, DI, health check, diagnostics, log messages, README, PublicAPI |
| Tests | ~14 | unit, guard, contract, integration with a fake authority, DI |
| Justifications, manifest, changelog, documentation | ~6 | |
| **Total** | **~44** | three pull requests |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
You are implementing issue #1241 (SPEC-002 P-39): the package Encina.Compliance.Evidence with trusted time-stamping
(RFC 3161) and e-signature extension points.

PROJECT CONTEXT:
- .NET 10 / C# 14, ROP (Either<EncinaError, T>), pre-1.0 (breaking changes preferred, no [Obsolete]).
- No provider-specific code and no store (the issue's matrix). Depends on Encina.Http (#1233), Encina.ChainedLog (#1240),
  Encina.Security.Secrets ICertificateReader (#1246) and Encina.Compliance.Attestation.
- eIDAS 2 as a module is post-1.0 (#805, DEC-015): generic extension points only.

IMPLEMENTATION OVERVIEW:
Phase 1: package skeleton: models, ITimeStampAuthority, ISignatureProvider, errors
Phase 2: Rfc3161TimeStampAuthority over a named Encina.Http client; verifier (status, nonce, imprint, CMS signature, chain at
         generation time with the time-stamping EKU, revocation fail closed); options and validator
Phase 3: CertificateSignatureProvider (detached CMS, per-tenant certificate, SignatureLevel.Plain)
Phase 4: explicit hook helpers for ChainedLogEntry and AttestationReceipt, notifications, AddEncinaEvidence, health check
Phase 5: per-tenant endpoint/credential/certificate; resilience owned by the Encina.Http pipeline
Phase 6: Encina.Compliance.Evidence meter/activities; EventIds 6590-6599 in the new range
Phase 7: unit, guard, contract, integration with a fake RFC 3161 authority (real tokens), manifests
Phase 8: docs, README, changelog fragment, PublicAPI, follow-up issue for persisted tokens

KEY PATTERNS:
- Only digests leave the application; qualification is decided by the trust provider and trust list, not by this code.
- Fail closed: no trust roots, no revocation answer, plain http (without opt-out) => not valid / not started.
- Never hold a database lock while calling a network service; hooks run after commit, on the caller's request.
- No digests, tokens, certificate content or endpoint URLs in logs, tags, metrics or errors; exceptions via ForLogging().
- TimeProvider for time; CRAP <= 10 per new method; tests execute real code.

REFERENCE FILES:
- src/Encina.Compliance.Attestation/ (layout, providers, health check)
- docs/plans/http-implementation-plan-1233.md, docs/plans/chained-log-implementation-plan-1240.md,
  docs/plans/certificate-reader-implementation-plan-1246.md
- docs/specifications/SPEC-002-eu-regulatory-readiness.md (REQ-048, AC-039, section 5.11)
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | A time-stamp or signature is evidence of a specific digest and moment; caching tokens would hide failures and has no reuse across digests |
| 2 | OpenTelemetry | ✅ | `Evidence.TimeStamp`, `Evidence.VerifyTimeStamp` and `Evidence.Sign` activities, `evidence.*` counters and a histogram, `evidence.tenant_id` tag, no digests, tokens or URLs (Phase 6) |
| 3 | Structured Logging | ✅ | `[LoggerMessage]` 6590-6599 in the new range `ComplianceEvidence` (ADR-021) |
| 4 | Health Checks | ✅ | `EvidenceHealthCheck`: valid configuration, trust roots present, rolling success rate of recent requests; no paid probe request |
| 5 | Validation | ✅ | Options validators with `ValidateOnStart` (https, trust roots, endpoint) and request validation (digest length) |
| 6 | Resilience | ✅ | Transient TSA failures are retried by the resilience handler composed on the `Encina.Http` named client (ADR-055: one retry owner); a coded `Left` when exhausted |
| 7 | Distributed Locks | ❌ | No shared state updated by concurrent hosts; the package keeps no store |
| 8 | Transactions | ❌ | No store; the hooks deliberately run after the caller's commit so no lock is held during a network call |
| 9 | Idempotency | ❌ | A second token for the same digest is harmless and expected on retry; nothing to deduplicate |
| 10 | Multi-Tenancy | ✅ | REQ-061/AC-043: tenant endpoint, credentials and signing certificate resolved per tenant; two-tenant test; fixed default tenant when tenancy is off |
| 11 | Module Isolation | ❌ | SPEC-002 requires no module scoping; `ModuleId` in messaging stays with #747 |
| 12 | Audit Trail | ✅ | `EvidenceTimeStampedNotification` and `EvidenceSignedNotification` let the application record each result with its evidence; the package keeps no audit store of its own |

---

## Prerequisites & Dependencies

| Item | Why | State |
|------|-----|-------|
| #1233 `Encina.Http` (P-31) | Transport, per-tenant credentials, mTLS, rate limits | Open; plan in review. PR 1 can use a plain named `HttpClient` until it lands |
| #1240 `Encina.ChainedLog` (P-38) | Chained-log hook | Open; plan in review. The Attestation hook does not wait for it |
| #1246 `ICertificateReader` | Per-tenant signing certificate | Open; plan in review. PR 2 waits for it |
| EventId range 6590-6599 | New logging | Registered by PR 1 |
| Follow-up issue: persisted tokens in the chained log on the 10 providers | Design Choice 4, option B | Issue file written with this plan |

---

## Next Steps

1. Link this plan from issue #1241; ask the maintainer the Dilemma of Design Choice 4 when work on the issue starts
2. PR 1 (abstractions, RFC 3161 client and verification, diagnostics), PR 2 (signature provider, hooks, DI, health, integration tests), PR 3 (documentation)
3. Open the follow-up issue for persisted time-stamp tokens (Design Choice 4, option B)
4. The final commit references `Fixes #1241`
