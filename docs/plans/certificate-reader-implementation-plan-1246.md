# Implementation Plan: X.509 Certificate Retrieval in `Encina.Security.Secrets` — `ICertificateReader`

> **Issue**: [#1246](https://github.com/dlrivada/Encina/issues/1246) (SPEC-002 REQ-053, AC-039, scenarios S9 and S10, tracking id P-44, priority P0; part of #1186)
> **Type**: Feature
> **Complexity**: Medium (one reader over the existing secret providers, a file source, a cache with rotation, a health check; 3 pull requests)
> **Estimated Scope**: ~900-1,300 lines of production code + ~1,800-2,400 lines of tests

---

## Summary

Add `ICertificateReader` to `Encina.Security.Secrets`: `GetCertificateAsync(name, ct)` returns an `X509Certificate2` with its private key, resolved per tenant, from any of the four existing secret providers (Azure Key Vault, AWS Secrets Manager, HashiCorp Vault, Google Secret Manager) or from a file/PFX source. Certificates are cached in the process and refreshed so that a rotated certificate is picked up. A health check turns degraded before expiry. The consumer named in the issue is `Encina.Http` (P-31, mTLS to the tax authority, scenario S9); it only needs the reader and a rotation notification.

### What the code does today (2026-11-11, `main` at `7a0049c07`)

1. **There is no certificate API.** `src/Encina.Security.Secrets/Abstractions/ISecretReader.cs` has `GetSecretAsync(string)` returning `Either<EncinaError, string>` and a typed overload that deserializes into a class. No type in `src/` mentions `X509Certificate2` for retrieval.
2. **All four providers return text.** `AwsSecretsManagerProvider` reads `response.SecretString` only (`src/Encina.Security.Secrets.AwsSecretsManager/AwsSecretsManagerProvider.cs:99`, never `SecretBinary`); `GoogleCloudSecretManagerProvider` decodes the payload as UTF-8 (`GoogleCloudSecretManagerProvider.cs:110`); Azure Key Vault returns the secret value, which for a certificate-backed secret is the base64 PFX or the PEM text; Vault returns the stored value. A certificate must therefore be stored as PEM text or as base64-encoded PFX text in every provider.
3. **The default chain would cache the private key in a shared cache.** `AddEncinaSecrets` wraps the reader in resilience (innermost), `CachingSecretReaderDecorator` and auditing (outermost) (`src/Encina.Security.Secrets/ServiceCollectionExtensions.cs`, `WrapWithDecorators`). `SecretsOptions.EnableCaching` defaults to `true`, and the caching decorator stores values through `ICacheProvider.GetOrSetAsync` (`Caching/CachingSecretReaderDecorator.cs`), which can be Redis or another distributed provider. A certificate read through the default reader would put private key text into that cache (Design Choice 2).
4. **Rotation and expiry exist for secrets, not certificates.** `SecretRotationCoordinator` and `ISecretRotationHandler` rotate secrets written through Encina; `SecretsHealthCheck` (`DefaultName = "encina-secrets"`, tags `["encina","secrets","ready"]`) probes one secret and goes `Degraded` on failure or an open circuit breaker. Nothing reads a certificate's `NotAfter`.
5. **Auditing exists.** `AuditedSecretReaderDecorator` records secret reads through `IOperationAuditStore` (resolved per write from a scope factory, `SecretAuditRecorder`); #1163 (an unregistered `IRequestContext`) is closed, so the recorder works.
6. **Tenancy.** `ITenantProvider.GetCurrentTenantId()` returns null without context; `MultiTenancyMarker` signals that tenancy is on (`src/Encina/Abstractions/MultiTenancyMarker.cs`).
7. **EventIds.** `SecuritySecrets` (5100-5199) uses 5100-5150; certificates take 5151-5170 (51 IDs counted in the files of the package today).

### Scope

- **Affected packages**: `Encina.Security.Secrets` only. The four provider satellites are not modified: the reader works over `ISecretReader` (Design Choice 1).
- **Provider category**: Cloud/secrets (AWS, Azure, plus the Vault and Google satellites that already exist). The feature adds no provider-specific code, as the issue states.
- **Out of scope**: certificate issuance; writing certificates; the mTLS client (`Encina.Http`, P-31); native certificate APIs of the providers (Design Choice 1, option B).
- **Estimated files**: ~15 production files, ~22 test files, ~6 documentation and manifest files. Three pull requests (Phase 9).

---

## Design Choices

<details>
<summary><strong>1. Where certificates come from — the existing secret providers as text, or each provider's native certificate API</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One `SecretCertificateReader` over a keyed `ISecretReader`; the stored secret is PEM text or base64 PFX text** | No change to the four satellites; the same code serves every provider present and future; Key Vault certificate auto-renewal still works because the certificate-backed secret always serves the latest version; a contract test parameterized over providers proves coherence | The provider does not expose certificate metadata, so the reader parses the certificate to read `NotAfter` and the thumbprint |
| **B) Native APIs per provider (Key Vault Certificates, AWS Certificate Manager export, Vault PKI)** | Provider-side metadata (expiry, policy) without parsing; PKI engines can issue on demand | A new method on every satellite and a new SDK package (`Azure.Security.KeyVault.Certificates`); provider-specific behavior behind one interface (against provider coherence, AGENTS.md §3); AWS and Google have no equivalent for private-key export |

### Chosen Option: **A — one reader over `ISecretReader`** (recommended, pending the maintainer)

### Rationale

**Class:** Dilemma. Option A wins when certificates are stored as secrets, the case of the reference application's tax authority client certificate. Option B wins if the organization issues certificates from a PKI engine or wants the provider to rotate and report them natively.

- We recommend A because it is one implementation for the whole category, matches the issue's own wording ("from Azure Key Vault, AWS Secrets Manager, HashiCorp Vault, Google Secret Manager (existing satellites)") and avoids new SDK dependencies.
- The issue's file/PFX provider is a separate source (Phase 4), not a secret provider.

</details>

<details>
<summary><strong>2. Where certificates are cached — a keyed chain without the shared cache, plus an in-process certificate cache</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The certificate reader resolves `ISecretReader` from a keyed registration (`"certificates"`) whose chain keeps resilience and auditing but omits `CachingSecretReaderDecorator`; the reader caches the parsed `X509Certificate2` in process** | The private key never reaches `ICacheProvider` or Redis; the cache is by thumbprint with its own refresh; audit and retry still apply | A second decorator chain to register and test; one more registration to keep in step with `WrapWithDecorators` |
| **B) Keep the default chain and exclude certificate names from the secret cache (a name pattern list)** | One chain | A forgotten pattern silently caches a private key in a shared cache; the exclusion list is configuration that can drift |
| **C) Use the default chain as is** | No work | Private key text in a distributed cache by default (`EnableCaching = true`) |

### Chosen Option: **A — keyed uncached chain and an in-process certificate cache** (recommended, pending the maintainer)

### Rationale

**Class:** Dilemma. Option A wins in any deployment with a distributed `ICacheProvider`, the common case. Option B wins only if the team insists on one chain and accepts configuring exclusions. Option C is rejected: the private key would reach a shared cache.

- We recommend A because key material must not be handed to a store the application shares with unrelated data; the in-process cache holds `X509Certificate2` instances only, never the text.
- The in-process cache keeps `(tenant, name)` to certificate with `RefreshInterval` (default 15 minutes) and replaces an entry only when the thumbprint changes. A replaced certificate is disposed after `DisposeGracePeriod` (default 5 minutes) so that a handshake in flight does not see a disposed key. Callers must not dispose the instance they receive (XML docs).
- A failed refresh keeps serving the cached certificate until it expires and records the failure (logged by code, health degraded); an expired certificate is never served silently: `Left(secrets.certificate_expired)`.

</details>

<details>
<summary><strong>3. Stored format and key storage flags</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) PEM (certificate chain and private key in one text, detected by `-----BEGIN`) or base64 PFX with an optional password in a second secret `{name}.password`; `X509CertificateLoader` and `X509Certificate2.CreateFromPem`; key storage flags in options** | Matches what Key Vault certificate-backed secrets, Secrets Manager and Vault can hold as text; no binary through text-only providers; the loader API is the current .NET 10 one | Two formats to parse and test |
| **B) Base64 PFX only** | One format | PEM is the natural format for Vault and Linux deployments; forces a conversion step |

### Chosen Option: **A — PEM or base64 PFX, loader-based parsing**

### Rationale

**Class:** Trivial. **Settled by:** the provider implementations (`AwsSecretsManagerProvider.cs:99` and `GoogleCloudSecretManagerProvider.cs:110` return text), the issue body ("returning `X509Certificate2`", file/PFX provider), and .NET 10 guidance (the `X509Certificate2` constructors that load files or bytes are obsolete in favor of `X509CertificateLoader`; AGENTS.md §1, latest C# and .NET 10 APIs).

- Key storage flags (`X509KeyStorageFlags`) default to `DefaultKeySet`; whether `EphemeralKeySet` works for client authentication with `SslStream` on Windows (SChannel) is an open question to verify with the loopback test (Research). The option is exposed so that deployments choose.
- The PFX password never lives in options: it is read as a secret named by a convention or by `CertificateOptions.PasswordSecretNameTemplate`; options hold no secret (AGENTS.md §3).

</details>

<details>
<summary><strong>4. Per-tenant names and fail closed</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Secret name from `NameTemplate`: `{tenant}--{name}` when tenancy is on (`MultiTenancyMarker` registered), `{name}` when it is off; no resolvable tenant under tenancy returns `Left(secrets.certificate_tenant_required)`** | No tenant configuration needed with tenancy off (a fixed default); the `--` separator is accepted by Key Vault (letters, digits, hyphen), AWS, Vault and Google; collision-free because tenant ids and names are validated not to contain `--` | The separator is a convention applications must follow when naming secrets |
| **B) A tenant-to-secret-name dictionary in configuration** | Arbitrary names | Configuration per tenant, which REQ-061 says must not be needed with tenancy off, and which drifts from the stored secrets |

### Chosen Option: **A — template with a validated separator, fail closed under tenancy**

### Rationale

**Class:** Settled. **Settled by:** SPEC-002 REQ-061 and AC-043 as restated in #1246 ("client certificates resolve per tenant; a missing tenant configuration fails closed"; with tenancy off a fixed default tenant applies and no tenant configuration is needed); AGENTS.md §3 (compliance and security gates fail closed; `MultiTenancyMarker` is the signal used by the dead-letter manager and ABAC audit).

- The key prefix `SecretsOptions.KeyPrefix` is applied by the underlying reader as today; the template result is the logical name.
- A tenant's certificate is never served to another tenant: the cache key includes the tenant, and a two-tenant test proves it (AC-043).

</details>

<details>
<summary><strong>5. Rotation and expiry — polling refresh, a change notification and a health check window</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Polling refresh (`RefreshInterval`), an `ICertificateRotationListener` callback on thumbprint change, and `CertificateExpiryHealthCheck` degraded inside `ExpiryWarningWindow` (default 30 days) and unhealthy after expiry** | Works for rotation done outside Encina (Key Vault auto-renew, a cron job); the listener lets `Encina.Http` rebuild its handler; the health check needs no schedule | A rotated certificate can be served for up to one refresh interval; the listener is a new abstraction |
| **B) Only the rotation callbacks of `SecretRotationCoordinator`** | No polling | Misses every rotation not made through Encina, which is the normal case for certificates |

### Chosen Option: **A — polling, a listener and an expiry health check**

### Rationale

**Class:** Trivial. **Settled by:** the issue body ("caching with rotation on change", "a health check that turns degraded before expiry (configurable window)") and the existing pattern of `SecretsHealthCheck` (`DefaultName`, static `Tags`, `Degraded` results).

- Which certificates the health check examines: the names in `CertificateOptions.MonitoredCertificates` (for each tenant returned by `ITenantStore` when tenancy is on). The check reads through the same cache, so it does not add provider calls beyond the refresh.
- The gauge `encina.secrets.certificate_days_to_expiry` is tagged by a hash of the certificate name (the issue's `name hash`), never the name or the tenant id alone, to bound cardinality and avoid leaking identifiers.

</details>

---

## Implementation Phases

Pull request cut: PR 1 = Phases 1-3 (contract, parser, reader, cache), PR 2 = Phases 4-6 (file source, DI, health), PR 3 = Phases 7-9 (cross-cutting, observability, tests, docs).

### Phase 1: Abstractions, options and errors

> **Goal**: The public surface and the name resolution.

<details>
<summary><strong>Tasks</strong></summary>

1. **`src/Encina.Security.Secrets/Abstractions/ICertificateReader.cs`**: `ValueTask<Either<EncinaError, X509Certificate2>> GetCertificateAsync(string name, CancellationToken cancellationToken = default)`; XML docs: shared instance, do not dispose, tenant resolved from context.
2. **`Abstractions/ICertificateRotationListener.cs`**: `ValueTask OnCertificateRotatedAsync(string name, string thumbprint, CancellationToken)`.
3. **`Certificates/CertificateOptions.cs`**: `NameTemplate`, `TenantSeparator` (default `--`), `RefreshInterval`, `DisposeGracePeriod`, `ExpiryWarningWindow`, `KeyStorageFlags`, `PasswordSecretNameTemplate`, `MonitoredCertificates`, `Sources` ordering; `Validate`.
4. **`Certificates/CertificateNameResolver.cs`**: builds the logical name from the template and `ITenantProvider`; validates names and tenant ids (no separator inside, length, characters).
5. **`SecretsErrors.cs`**: codes `secrets.certificate_invalid`, `secrets.certificate_expired`, `secrets.certificate_tenant_required`, `secrets.certificate_password_missing` and factories.
6. `PublicAPI.Unshipped.txt`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of issue #1246 in Encina: the certificate reader contract, options, name resolution and error codes.

CONTEXT:
- Package src/Encina.Security.Secrets/. ISecretReader returns ValueTask<Either<EncinaError, string>>; error codes live in SecretsErrors.cs.
- ITenantProvider (src/Encina.Tenancy/Abstractions/ITenantProvider.cs) and MultiTenancyMarker (src/Encina/Abstractions/MultiTenancyMarker.cs)
  decide the tenant; tenancy on with no tenant must fail closed.

TASK:
Create ICertificateReader, ICertificateRotationListener, CertificateOptions (with Validate), CertificateNameResolver and the new
SecretsErrors codes, with unit and guard tests (name resolution with tenancy on/off, invalid separators, missing tenant).

KEY RULES:
- No secret in options (the password is a secret name template); options remain [JsonIgnore]-free because they hold none.
- Error codes use the secrets.* prefix; never EncinaError.Message in logs.
- XML docs with an example; PublicAPI.Unshipped.txt; zero warnings.

REFERENCE FILES:
- src/Encina.Security.Secrets/SecretsErrors.cs, SecretsOptions.cs
- src/Encina.Security.Secrets/Abstractions/ISecretReader.cs
- docs/plans/certificate-reader-implementation-plan-1246.md (Design Choices 3 and 4)
```

</details>

---

### Phase 2: Certificate parser

> **Goal**: PEM and PFX text become an `X509Certificate2` with its private key, or a typed error.

<details>
<summary><strong>Tasks</strong></summary>

1. **`Certificates/CertificateParser.cs`**: `Either<EncinaError, X509Certificate2> Parse(string text, string? password, X509KeyStorageFlags flags)`: PEM detected by `-----BEGIN`, loaded with `X509Certificate2.CreateFromPem(certPem, keyPem)` (certificate chain and key blocks split by label); otherwise base64-decoded PFX loaded with `X509CertificateLoader.LoadPkcs12`; a certificate without a private key is `Left(secrets.certificate_invalid)`.
2. Expiry check helper: `NotAfter` against `TimeProvider`.
3. Tests: valid PFX with and without password, PEM with RSA and ECDSA keys, truncated input, wrong password, certificate without key, expired and not-yet-valid certificates (generated in the test with `CertificateRequest`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of issue #1246 in Encina: the certificate parser.

CONTEXT:
- Providers return text (see the plan): PEM text or base64 PFX text. .NET 10: use X509CertificateLoader, not the obsolete constructors.
- Tests generate certificates with System.Security.Cryptography.X509Certificates.CertificateRequest; no checked-in key files.

TASK:
Implement CertificateParser (PEM and PFX, password handling, flags) and the validity helper, with unit, guard and property tests
(round trip of generated certificates; malformed input never throws).

KEY RULES:
- Never log certificate text, the password or key bytes; errors carry the secrets.certificate_* code and, at most, the exception type.
- Parse failures return Left; no exception escapes.
- Time through TimeProvider.

REFERENCE FILES:
- src/Encina.Security.Secrets/Injection/SecretPropertyCache.cs (parsing/caching style in this package)
- src/Encina.Security.Secrets/SecretsErrors.cs
```

</details>

---

### Phase 3: `SecretCertificateReader` and the in-process cache

> **Goal**: Certificates from any secret provider, per tenant, cached and refreshed.

<details>
<summary><strong>Tasks</strong></summary>

1. **`Certificates/SecretCertificateReader.cs : ICertificateReader`**: resolves the logical name (Phase 1), reads the certificate secret and, when needed, the password secret through the keyed `ISecretReader` `"certificates"`, parses (Phase 2), checks validity, and stores the instance in the cache.
2. **`Certificates/CertificateCache.cs`**: `ConcurrentDictionary<(string Tenant, string Name), Entry>`; `GetOrAddAsync` with single-flight per key (a `Lazy<Task>` or per-key semaphore); a refresh task per entry on `RefreshInterval` via `TimeProvider.CreateTimer` whose callback catches everything and never throws (AGENTS.md §3, timer callbacks); thumbprint comparison; the replaced instance is disposed after `DisposeGracePeriod`; listeners notified on change.
3. **Registration of the keyed reader**: `AddEncinaCertificates` registers the keyed `ISecretReader` `"certificates"` as the base provider wrapped in resilience (if enabled) and auditing, without caching (Design Choice 2).
4. Tests: first read, cached read (no provider call), rotation (provider returns a new certificate, listener fires, old instance disposed after the grace period with `FakeTimeProvider`), refresh failure keeps serving, expired certificate refused, two tenants isolated, tenancy on without tenant fails closed.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of issue #1246 in Encina: SecretCertificateReader and CertificateCache.

CONTEXT:
- The decorator chain is built in src/Encina.Security.Secrets/ServiceCollectionExtensions.cs (WrapWithDecorators); the default chain
  includes CachingSecretReaderDecorator, which stores values through ICacheProvider - certificates must NOT go through it.
- Timer rule (AGENTS.md §3): a TimeProvider.CreateTimer callback catches its failures and never throws; tests use FakeTimeProvider
  and never swallow an exception thrown by the code under test.

TASK:
Implement the reader, the cache (single flight, refresh, grace-period disposal, listeners), the keyed reader chain without caching,
and the unit tests listed in the plan.

KEY RULES:
- Private key material never reaches ICacheProvider, logs, activity tags or exceptions.
- A failed refresh is logged by error code and counted, never swallowed silently; an expired certificate is never served.
- Dispose replaced certificates only after DisposeGracePeriod; callers must not dispose the shared instance.
- TimeProvider only; CancellationToken on every async call.

REFERENCE FILES:
- src/Encina.Security.Secrets/Caching/CachingSecretReaderDecorator.cs (what to bypass)
- src/Encina.Security.Secrets/Resilience/ResilientSecretReaderDecorator.cs
- src/Encina.Security.Secrets/Rotation/SecretRotationCoordinator.cs
```

</details>

---

### Phase 4: File / PFX source

> **Goal**: Certificates from disk for development, on-premise and the file provider the issue lists.

<details>
<summary><strong>Tasks</strong></summary>

1. **`Certificates/FileCertificateReader.cs : ICertificateReader`**: `FileCertificateOptions` maps a logical name to a path (PFX or PEM) and a password source (an environment variable name or a secret name; never a literal password); files are read asynchronously, re-read when the file's last write time changes (checked at refresh), parsed by `CertificateParser`.
2. **Source ordering**: `CertificateOptions.Sources` (`Secrets`, `File`); a composite reader tries them in order and returns the first `Right`; `Left(secrets.not_found)` falls through, any other `Left` is returned (no silent failover on errors).
3. **Audit**: `AuditedCertificateReaderDecorator` records file reads through `SecretAuditRecorder` (secret reads are already audited by the secret chain).
4. Tests with temp files and `FakeTimeProvider`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of issue #1246 in Encina: the file/PFX certificate source and the composite reader.

CONTEXT:
- CertificateParser, CertificateCache and SecretCertificateReader exist (Phases 2-3). The audit recorder is
  src/Encina.Security.Secrets/Auditing/SecretAuditRecorder.cs.

TASK:
Implement FileCertificateReader, its options (validated: absolute existing paths, no literal passwords), the ordered composite
reader and the audit decorator, with unit tests on temp files.

KEY RULES:
- Passwords come from environment variables or secrets, never from options values; options with a path or variable name are fine.
- Async file reads with CancellationToken; no sync File.ReadAllBytes.
- Only not_found falls through to the next source; other errors are returned.

REFERENCE FILES:
- src/Encina.Security.Secrets/Providers/EnvironmentSecretProvider.cs
- src/Encina.Security.Secrets/Auditing/SecretAuditRecorder.cs
```

</details>

---

### Phase 5: Configuration and DI

> **Goal**: `AddEncinaCertificates` registers everything it resolves.

<details>
<summary><strong>Tasks</strong></summary>

1. **`AddEncinaCertificates(this IServiceCollection, Action<CertificateOptions>?)`** in `ServiceCollectionExtensions`: options with validation, name resolver, parser, cache, the keyed reader chain, `ICertificateReader` (composite), the health check and metrics; `TryAdd` throughout so an application's own `ICertificateReader` wins.
2. Requires `AddEncinaSecrets` for the provider; without a registered `ISecretReader` and without a file source, start-up fails naming the missing source (ADR-050 rule 3).
3. DI test with `ValidateOnBuild` and `ValidateScopes` for: secrets only, file only, both, with and without tenancy.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are implementing Phase 5 of issue #1246 in Encina: registration of the certificate reader.

CONTEXT:
- AGENTS.md §3 registration completeness: register every option type and dependency, proven by a ValidateOnBuild/ValidateScopes test;
  the application's own registrations win. Existing registration: src/Encina.Security.Secrets/ServiceCollectionExtensions.cs.

TASK:
Add AddEncinaCertificates and the DI tests described.

KEY RULES:
- TryAdd; no silent default source; the failure message names the missing source and the registration to add.
- Opt-in: nothing registered unless AddEncinaCertificates is called.

REFERENCE FILES:
- src/Encina.Security.Secrets/ServiceCollectionExtensions.cs
- docs/architecture/adr/050-modules-may-require-modules.md
```

</details>

---

### Phase 6: Expiry health check

> **Goal**: Degraded before expiry, unhealthy after.

<details>
<summary><strong>Tasks</strong></summary>

1. **`Health/CertificateExpiryHealthCheck.cs : IHealthCheck`** (model `SecretsHealthCheck`): `DefaultName = "encina-certificates"`, static `Tags` `["encina","secrets","certificates","ready"]`; for each monitored name (and each tenant when tenancy is on) reads through the reader; `Healthy` outside the window, `Degraded` inside `ExpiryWarningWindow` (description carries the number of certificates and the smallest remaining days, never names), `Unhealthy` when any is expired or unreadable.
2. Registered by `AddEncinaCertificates` when `MonitoredCertificates` is not empty.
3. Tests with `FakeTimeProvider` and generated certificates around the window boundary.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
You are implementing Phase 6 of issue #1246 in Encina: the certificate expiry health check.

CONTEXT:
- Model: src/Encina.Security.Secrets/Health/SecretsHealthCheck.cs (DefaultName const, static tags, Degraded results).

TASK:
Implement CertificateExpiryHealthCheck, register it, and test the boundary of the warning window and the unhealthy cases.

KEY RULES:
- Health results never contain certificate names, thumbprints, tenant ids or EncinaError.Message: counts and error codes only.
- The check reads through the cache; it adds no provider calls beyond the refresh.
- Time through TimeProvider.

REFERENCE FILES:
- src/Encina.Security.Secrets/Health/SecretsHealthCheck.cs
```

</details>

---

### Phase 7: Cross-cutting integration

> **Goal**: The matrix items marked included.

<details>
<summary><strong>Tasks</strong></summary>

1. **Resilience**: the keyed chain keeps `ResilientSecretReaderDecorator` when `SecretsOptions.EnableResilience` is on (transient provider errors retried).
2. **Audit**: secret-backed reads audited by `AuditedSecretReaderDecorator` in the keyed chain; file reads by Phase 4.
3. **Validation**: `CertificateOptions.Validate` and `FileCertificateOptions` validation at start-up.
4. **Multi-tenancy**: Phases 1 and 3 (fail closed; isolation test).
5. **Caching**: the in-process certificate cache (Phase 3); the shared cache is deliberately bypassed.
6. **Deferred**: distributed locks, transactions, idempotency, module isolation (matrix).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
You are implementing Phase 7 of issue #1246 in Encina: wiring resilience and audit into the keyed certificate chain.

CONTEXT:
- WrapWithDecorators in src/Encina.Security.Secrets/ServiceCollectionExtensions.cs builds the default chain; the keyed chain reuses
  its resilience and audit layers and omits caching.

TASK:
Refactor WrapWithDecorators minimally so the keyed chain can ask for "no caching", and add tests that a read through the keyed chain
is audited and retried but never calls ICacheProvider.

KEY RULES:
- No behavior change for the default chain; the existing tests must pass unchanged.
- Test with a recording ICacheProvider that no certificate text is ever written to it.

REFERENCE FILES:
- src/Encina.Security.Secrets/ServiceCollectionExtensions.cs
- src/Encina.Security.Secrets/Caching/CachingSecretReaderDecorator.cs
```

</details>

---

### Phase 8: Observability

> **Goal**: Spans, metrics and logs with no material.

<details>
<summary><strong>Tasks</strong></summary>

1. **EventIds** 5151-5170 inside the registered `SecuritySecrets` range (no new range; `Encina.Security.Secrets` is already in `EncinaEventIdAllocationTests`): `Log.cs` additions packed from 5151 (certificate loaded, refreshed, rotated, refresh failed, expired, parse failed, tenant required).
2. **Metrics** in `SecretsMetrics`: gauge `encina.secrets.certificate_days_to_expiry` (tag: hash of the name), counters `encina.secrets.certificate_refresh_failures`.
3. **Span** `encina.secrets.get_certificate` in `SecretsActivitySource` with `encina.tenant_id` and `encina.secrets.provider`; no material.
4. **Test** (AC-044): in-memory exporter asserts the attributes, and that no tag, metric or log carries certificate text, a thumbprint, a password or a subject distinguished name.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```
You are implementing Phase 8 of issue #1246 in Encina: observability for the certificate reader.

CONTEXT:
- SecuritySecrets 5100-5199 is registered; 5100-5150 are used; certificates use 5151-5170, packed.
- Existing: src/Encina.Security.Secrets/Log.cs, Diagnostics/SecretsMetrics.cs, Diagnostics/SecretsActivitySource.cs.

TASK:
Add the log messages, the gauge and counter, the span and the exporter test.

KEY RULES:
- Never outside 5151-5170 for this feature; no gaps; XML doc "Event IDs: 5151-5170 (see EventIdRanges.SecuritySecrets)".
- Bounded tag values; name hash only; no subject names, thumbprints or material.

REFERENCE FILES:
- src/Encina.Security.Secrets/Log.cs
- src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

---

### Phase 9: Testing, documentation and finalization

> **Goal**: Every applicable flag at target; documentation and fragments.

<details>
<summary><strong>Tasks</strong></summary>

| Flag | Scope | Per-file target and justification |
|------|-------|-----------------------------------|
| Unit | parser, name resolver, reader, cache, file reader, health check | 90 for the parser, resolver and cache: pure logic with generated certificates and `FakeTimeProvider` |
| Guard | every public method with parameters | 90 |
| Contract | the same `ICertificateReader` contract over a fake `ISecretReader` per provider shape (Azure Key Vault, AWS, Vault, Google) and the file reader | 85: real reader against provider-shaped fakes |
| Property | `.md` justification: parsing and retrieval are covered by unit and integration tests | |
| Integration | Vault dev server (a generic `hashicorp/vault` dev-mode container through `Testcontainers`), AWS Secrets Manager on LocalStack (fixture extended), file source on a temp directory; Azure Key Vault and Google through fakes of their clients; a loopback mTLS handshake with the returned certificate | real services where an emulator exists |
| Load, Benchmark | `.md` justifications: not a concurrent hot path (cached reads) | |

- Documentation (docs-writer): XML docs; `changelog.d/1246-certificate-reader.added.md`; package README of `Encina.Security.Secrets` and a section in `docs/features/` (storage format, naming convention, rotation, health check, the mTLS key storage flags); `docs/INVENTORY.md`; `ROADMAP.md`. No ADR is needed: the keyed uncached chain and the format are recorded in the feature page.
- Update the coverage manifest `Encina.Security.Secrets.json`; measure each flag and the CRAP of changed methods; `dotnet build --configuration Release` with 0 warnings.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 9</strong></summary>

```
You are implementing Phase 9 of issue #1246 in Encina: tests, documentation and finalization of the certificate reader.

CONTEXT:
- Per-flag coverage targets live in .github/coverage-manifest/Encina.Security.Secrets.json; tests must execute real package code.
- Fixtures: LocalStackFixture (Cloud-LocalStack; extend with a Secrets Manager client), generic Testcontainers for Vault dev mode.

TASK:
Write the tests of the table (including the loopback mTLS handshake and the two-tenant isolation test), the justification files,
update the manifest, and ask docs-writer for the documentation list; report measured per-flag coverage and CRAP.

KEY RULES:
- Generated certificates only; no key files checked in; no Thread.Sleep; Shouldly via Encina.Testing.Shouldly.
- Shared [Collection] fixtures; fixture clients are owned by the fixture.
- Never edit CHANGELOG.md; coverage figures only through covref markers; English only; no emojis; no private client names.

REFERENCE FILES:
- tests/Encina.TestInfrastructure/Fixtures/LocalStackFixture.cs
- tests/Encina.GuardTests/Security/Secrets/HashiCorpVaultGuardTests.cs
- docs/testing/coverage-measurement-methodology.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Reference | Relevance |
|--------|-----------|-----------|
| SPEC-002 | REQ-053, AC-039, scenarios S9 and S10, note H, H-R12 | mTLS to the tax authority needs a client certificate per tenant |
| SPEC-002 | REQ-061 / AC-043, REQ-062 / AC-044 | Per-tenant resolution, fail closed, instrumentation without material |
| RFC 5280 | X.509 certificate profile (validity, `NotAfter`) | Expiry check |
| RFC 7468 | PEM textual encodings | PEM detection and parsing |
| PKCS #12 (RFC 7292) | PFX container | Base64 PFX format |
| Comparable products | Azure Key Vault certificates (certificate-backed secret), AWS Secrets Manager, HashiCorp Vault KV, ASP.NET Core certificate loading | The text formats providers can hold |
| AGENTS.md §3 | Fail closed, timer callbacks never throw, secrets in options, registration completeness | Rules applied |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| ISecretReader | src/Encina.Security.Secrets/Abstractions/ISecretReader.cs | string secrets; the certificate reader decodes PFX or PEM text read through it |
| SecretsErrors | src/Encina.Security.Secrets/SecretsErrors.cs | error code convention (secrets.*) for the new certificate codes |
| AddEncinaSecrets and the decorator chain | src/Encina.Security.Secrets/ServiceCollectionExtensions.cs | resilience, caching and auditing decorators already wrap ISecretReader; the keyed chain reuses resilience and audit |
| CachingSecretReaderDecorator | src/Encina.Security.Secrets/Caching/CachingSecretReaderDecorator.cs | what the keyed chain must bypass (shared cache) |
| ResilientSecretReaderDecorator | src/Encina.Security.Secrets/Resilience/ | transient retry of provider calls |
| AuditedSecretReaderDecorator and SecretAuditRecorder | src/Encina.Security.Secrets/Auditing/ | secret read audit; the file source needs its own audited decorator |
| SecretsHealthCheck | src/Encina.Security.Secrets/Health/SecretsHealthCheck.cs | model for DefaultName, Tags and Degraded results |
| SecretRotationCoordinator | src/Encina.Security.Secrets/Rotation/SecretRotationCoordinator.cs | rotation done through Encina |
| SecretsMetrics and SecretsActivitySource | src/Encina.Security.Secrets/Diagnostics/ | meter and activity source to extend |
| Provider satellites | src/Encina.Security.Secrets.AzureKeyVault, .AwsSecretsManager, .HashiCorpVault, .GoogleCloudSecretManager | serve the certificate text; unchanged |
| ITenantProvider and MultiTenancyMarker | src/Encina.Tenancy/Abstractions/ITenantProvider.cs, src/Encina/Abstractions/MultiTenancyMarker.cs | per-tenant names and fail closed |
| EventIdRanges SecuritySecrets 5100-5199 | src/Encina/Diagnostics/EventIdRanges.cs | 5100-5150 used; certificates take 5151-5170 |
| Secrets coverage manifest | .github/coverage-manifest/Encina.Security.Secrets.json | per-file targets |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Security.Secrets` (certificates) | `SecuritySecrets` 5100-5199 (already registered) | **5151-5170** new; 5171-5199 stay free; no registry change |
| Provider satellites | 5200-5399 (existing) | Not touched |
| Family of plans | 6000-6049 (#599, #122), 6050-6149 (#1239), 6150-6199 (#122), 6200-6249 (#1250) | Disjoint |

### Open Questions to Verify During Implementation

| Question | How to verify |
|----------|---------------|
| Does `X509KeyStorageFlags.EphemeralKeySet` work for client authentication with `SslStream`/`HttpClient` on Windows? | The loopback mTLS integration test on Windows and Linux CI; pick the default flag set from the result |
| Does the Azure Key Vault secret of a certificate return the PFX base64 or PEM depending on the certificate content type? | Read the provider test doubles and the Key Vault documentation; both formats are parsed anyway |
| Can `hashicorp/vault` dev mode run through the generic `Testcontainers` package already in `Directory.Packages.props`? | Spike in the PR 2 integration test; fall back to a fake `IVaultClient` if not |
| Does the existing `HashiCorpVault` provider return a single value suitable for PEM text? | Read `HashiCorpVaultProvider` and its options (path and key) |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| New production files in Encina.Security.Secrets (ICertificateReader, listener, options, parser, name resolver, reader, cache, composite) | 8 | |
| File provider (FileCertificateReader, options) | 2 | |
| Health check | 1 | |
| Diagnostics additions (log messages, metrics, activity) | 3 | edits to existing files |
| Modified existing files (ServiceCollectionExtensions, SecretsErrors, SecretsMetrics, PublicAPI.Unshipped.txt) | 4 | |
| Unit tests | 8 | |
| Guard tests | 2 | |
| Contract tests | 2 | |
| Integration tests | 4 | Vault dev server, LocalStack, file source, loopback mTLS handshake |
| Load, benchmark and property justification files | 3 | |
| Documentation | 5 | README, feature page, changelog fragment, ROADMAP line, INVENTORY |
| Coverage manifest edits | 1 | |
| Total | 43 | Three pull requests |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT:
You are implementing issue #1246 in Encina (.NET 10, C# 14, nullable enabled, pre-1.0, no backward compatibility): X.509 client
certificate retrieval in Encina.Security.Secrets (SPEC-002 REQ-053). Rules: AGENTS.md. The consumer is Encina.Http (P-31, not part of this plan).

IMPLEMENTATION OVERVIEW:
1. ICertificateReader, ICertificateRotationListener, CertificateOptions, CertificateNameResolver, new SecretsErrors codes.
2. CertificateParser: PEM or base64 PFX text to X509Certificate2 through X509CertificateLoader / CreateFromPem.
3. SecretCertificateReader over a keyed ISecretReader chain without caching, plus CertificateCache (single flight, TimeProvider timer refresh,
   thumbprint change, grace-period disposal, listeners).
4. FileCertificateReader and the ordered composite; audit decorator for file reads.
5. AddEncinaCertificates; CertificateExpiryHealthCheck (degraded inside the warning window, unhealthy after expiry).
6. EventIds 5151-5170 in the registered SecuritySecrets range; gauge certificate_days_to_expiry (tag: name hash), span get_certificate.
7. Tests per flag including the loopback mTLS handshake and two-tenant isolation; docs.

KEY PATTERNS:
- Private key material never reaches ICacheProvider, logs, tags, health results or exceptions.
- Fail closed: tenancy on with no tenant returns Left(secrets.certificate_tenant_required); expired certificates are never served.
- Timer callbacks catch everything and never throw; TimeProvider everywhere; CancellationToken on every async call.
- Either<EncinaError, T>; TryAdd registration; keyed chain reuses resilience and audit layers.
- Generated certificates in tests; shared fixtures; per-flag coverage; CRAP <= 10 on changed methods.

REFERENCE FILES:
- src/Encina.Security.Secrets/ServiceCollectionExtensions.cs, Caching/CachingSecretReaderDecorator.cs, Health/SecretsHealthCheck.cs
- src/Encina.Security.Secrets.AwsSecretsManager/AwsSecretsManagerProvider.cs
- src/Encina/Abstractions/MultiTenancyMarker.cs
- docs/plans/certificate-reader-implementation-plan-1246.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ | Phase 3: in-process certificate cache with refresh; the shared `ICacheProvider` is deliberately bypassed for private keys (Design Choice 2) |
| 2 | OpenTelemetry | ✅ | Phase 8: span `encina.secrets.get_certificate` with the tenant attribute, expiry gauge, refresh failure counter |
| 3 | Structured Logging | ✅ | Phase 8: EventIds 5151-5170 in the registered `SecuritySecrets` range |
| 4 | Health Checks | ✅ | Phase 6: `CertificateExpiryHealthCheck` |
| 5 | Validation | ✅ | Phase 1 and 5: options validated at start-up; names and tenant ids validated |
| 6 | Resilience | ✅ | Phase 7: provider calls retried through the existing resilient reader layer |
| 7 | Distributed Locks | ❌ | No shared state updated by concurrent hosts; each host caches its own copy |
| 8 | Transactions | ❌ | Read-only feature; no writes |
| 9 | Idempotency | ❌ | Not a message or request entry point |
| 10 | Multi-Tenancy | ✅ | Phases 1 and 3: per-tenant names, fail closed, isolation test (AC-043) |
| 11 | Module Isolation | ❌ | SPEC-002 requires no module scoping; `ModuleId` in messaging stays with #747 |
| 12 | Audit Trail | ✅ | Phases 4 and 7: secret reads audited by the existing decorator in the keyed chain, file reads by `AuditedCertificateReaderDecorator` |

---

## Prerequisites & Dependencies

### Advisable before or with this work

- None blocks the plan. #1163 (the unregistered `IRequestContext`) is closed, so the audit layer works.
- #1257 (multi-tenancy of the SPEC-002 capabilities, includes "per-tenant credentials"): this plan implements the certificate part of REQ-061; #1257 tracks the cross-feature verification.

### Parallel and follow-up work

- P-31 `Encina.Http` (mTLS consumer): uses `ICertificateReader` and `ICertificateRotationListener`; its plan should reference this one.
- Native certificate APIs per provider (Design Choice 1, option B) stay a possible follow-up if a PKI engine is adopted.

---

## Next Steps

1. The maintainer answers the Dilemmas of this plan (choices 1 and 2); the orchestrator adds the Maintainer Decisions section.
2. Link this plan from #1246.
3. Implement PR 1 (Phases 1-3), PR 2 (Phases 4-6), PR 3 (Phases 7-9).
4. Verify the open questions (key storage flags, Vault dev container) during PR 2.
