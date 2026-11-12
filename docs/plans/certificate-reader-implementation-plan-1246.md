# Implementation Plan: X.509 Certificate Retrieval in `Encina.Security.Secrets` — `ICertificateReader`

> **Issue**: [#1246](https://github.com/dlrivada/Encina/issues/1246) (SPEC-002 REQ-053, AC-039, scenarios S9 and S10, tracking id P-44, priority P0; part of #1186)
> **Type**: Feature
> **Complexity**: Medium (one reader over the existing secret providers, a file source, a cache with rotation, a health check; 5 pull requests)
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
6. **Tenancy.** `Encina.Security.Secrets.csproj` references `Encina`, `Encina.Caching` and `Encina.Security.Audit` only, so `ITenantProvider` and `ITenantStore` (`Encina.Tenancy`) are not reachable from it. The core package has what the fail-closed pattern needs: `IRequestContextAccessor` (`src/Encina/Abstractions/IRequestContextAccessor.cs`; `IRequestContext.TenantId`) and `MultiTenancyMarker` (`src/Encina/Abstractions/MultiTenancyMarker.cs`), the pair `DeadLetterManager` uses (choice 7).
7. **EventIds.** `SecuritySecrets` (5100-5199) uses 5100-5150; certificates take 5151-5170 (51 IDs counted in the files of the package today).
8. **Secret name rules differ per provider.** Azure Key Vault secret names allow letters, digits and hyphens and are case-insensitive; AWS Secrets Manager, Vault and Google names are case-sensitive. A tenant-qualified name must therefore be unambiguous after lower-casing (choice 4).

### Scope

- **Affected packages**: `Encina.Security.Secrets` only. The four provider satellites are not modified: the reader works over `ISecretReader` (Design Choice 1).
- **Provider category**: Cloud/secrets (AWS, Azure, plus the Vault and Google satellites that already exist). The feature adds no provider-specific code, as the issue states.
- **Out of scope**: certificate issuance; writing certificates; the mTLS client (`Encina.Http`, P-31); native certificate APIs of the providers (Design Choice 1, option B).
- **Estimated files**: ~15 production files, ~22 test files, ~6 documentation and manifest files. Five pull requests (see "Pull request cut" under Implementation Phases).

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
- The in-process cache keeps `(tenant, name)` to certificate with `RefreshInterval` and replaces an entry only when the thumbprint changes. A replaced certificate is disposed after `DisposeGracePeriod` so that a handshake in flight does not see a disposed key. When a refresh parses a certificate whose thumbprint is unchanged, the newly parsed instance (and its intermediates) is disposed at once and the cached one is kept: on Windows each PKCS#12 load with `DefaultKeySet` writes a private-key file to the user or machine key store that is removed only when the instance is disposed, so an undisposed instance per refresh would leave one key file on disk every `RefreshInterval` (choice 3). Callers must not dispose the instance they receive (XML docs). Defaults are choice 8.
- What a failed refresh does is choice 6; an expired certificate is never served: `Left(secrets.certificate_expired)`.

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

- Key storage flags (`X509KeyStorageFlags`) default to `DefaultKeySet` (choice 8); whether `EphemeralKeySet` works for client authentication with `SslStream` on Windows (SChannel) is an open question to verify with the loopback test (Research). The option is exposed so that deployments choose.
- On Windows, `DefaultKeySet` persists the private key in a key file of the user or machine key store for the lifetime of the `X509Certificate2` instance; the file is deleted when the instance is disposed. Every parsed instance is therefore disposed by its owner: the cache disposes a replaced instance after `DisposeGracePeriod` and a re-parsed instance with an unchanged thumbprint at once (choice 2), and the Windows PEM re-export disposes its intermediate instance after the reload. The feature guide documents the key file and the disposal rule.
- The chain is kept: `X509Certificate2.CreateFromPem` loads the leaf only, so the parser reads every further `CERTIFICATE` block of the PEM as an intermediate, and loads a PFX with `X509CertificateLoader.LoadPkcs12Collection` to keep its intermediates. `ICertificateReader.GetCertificateChainAsync(name, ct)` returns them (leaf excluded) so a consumer can build `SslStreamCertificateContext.Create(leaf, intermediates)`.
- PEM keys are ephemeral on Windows, which SChannel can reject for client authentication; on Windows the parser re-exports a PEM-loaded certificate to PKCS#12 and reloads it with `X509CertificateLoader.LoadPkcs12` and the configured flags. The loopback mTLS test covers the PEM path and the chain on Windows and Linux.
- The PFX password never lives in options: it is read as a secret named by a convention or by `CertificateOptions.PasswordSecretNameTemplate`; options hold no secret (AGENTS.md §3).

</details>

<details>
<summary><strong>4. Per-tenant names — the tenant as part of the secret name, without collisions</strong></summary>

The rule itself is settled: under tenancy the certificate resolves per tenant and a missing tenant fails closed (SPEC-002 REQ-061 and AC-043 as restated in #1246, "client certificates resolve per tenant; a missing tenant configuration fails closed"; AGENTS.md §3, gates fail closed). How the tenant becomes part of the stored name is not settled, because more than one encoding satisfies it.

The first draft used `{tenant}--{name}` and only forbade `--` inside each part. That is not collision-free: tenant `a-` with name `b` and tenant `a` with name `-b` both give `a---b`, so tenant `a` could read tenant `a-`'s certificate. Key Vault names are also case-insensitive, so tenants `Clinic` and `clinic` would share a name there.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `{tenant}--{name}` with strict segments**: both the tenant id and the name must match `^[a-z0-9]+(-[a-z0-9]+)*$` (lower-case letters and digits, single hyphens, no leading, trailing or doubled hyphen), and the full name, with `SecretsOptions.KeyPrefix` when set, fits the shortest provider limit (127 characters, Key Vault); the separator `--` is a constant. A tenant id or name that does not match is `Left(secrets.certificate_invalid_name)`, never rewritten | Readable names an operator can create by hand; the concatenation is injective: neither segment contains `--` or ends or starts with `-`, so the first `--` is always the separator; lower-case only, so no case collision on Key Vault | Tenant ids that use upper case, `_` or other characters cannot use the reader until they are mapped |
| **B) Encoded tenant segment**: `t{base32(lower-case UTF-8 of the tenant id), lower-case, no padding}--{name}`, with the name under the rule of option A | Any tenant id works; injective by construction | Operators must compute the encoded segment to name a secret; names are opaque in the provider console |
| **C) A tenant-to-secret-name dictionary in configuration** | Arbitrary names | Configuration per tenant, which REQ-061 says must not be needed with tenancy off, and which drifts from the stored secrets |

### Chosen Option: **A — strict segments, fail closed on any tenant id or name outside the rule** (recommended, pending the maintainer)

### Rationale

**Class:** Dilemma. Option A wins when tenant ids are slugs the application controls, which keeps secrets nameable by hand. Option B wins when tenant ids are free-form (upper case, underscores, display names). Option C is eliminated by REQ-061 (no tenant configuration needed with tenancy off).

Rules common to every option (settled by REQ-061, AC-043 and AGENTS.md §3):

- Tenancy on (`MultiTenancyMarker` registered): the stored name always carries the tenant segment. A missing tenant is `Left(secrets.certificate_tenant_required)`; there is no fallback to the unqualified `{name}`, which would be a certificate shared by every tenant.
- Tenancy off: the name is `{name}` (no tenant configuration, the fixed default of REQ-061).
- The file source keys its entries by the same tenant-qualified logical name, so a file entry without the tenant segment is never served under tenancy.
- The separator `--` is a constant of `CertificateNameResolver`, not an option. A configurable separator would void the proof below: with `-`, tenant `a` + name `b-c` and tenant `a-b` + name `c` both give `a-b-c`, so tenant `a` would read tenant `a-b`'s certificate.
- Injectivity of option A (with the constant `--`): both segments match `^[a-z0-9]+(-[a-z0-9]+)*$`, so neither contains `--` and the tenant ends with a letter or digit. In `{tenant}--{name}` the first `--` therefore starts exactly at index `len(tenant)`: no earlier pair of characters can be `--`, because inside the tenant there is none and the pair that starts at the tenant's last character begins with a letter or digit. Splitting the stored name at its first `--` gives back the tenant and the name, so two different pairs never give the same stored name. All characters are lower case, so the result also holds case-insensitively (Key Vault). Option B keeps the same constant separator; the Base32 alphabet (`a-z`, `2-7`) has no `-`, so the same split argument applies.
- The cache key is `(tenant, name)` and the audit record carries the tenant.
- Length: the 127-character Key Vault limit applies to the name the provider receives. `SecretsOptions.KeyPrefix` (`src/Encina.Security.Secrets/SecretsOptions.cs:80`) is declared and documented as prepended by `EnvironmentSecretProvider`, but no reader on `main` applies it today; so that the limit holds whether or not a provider applies it, the resolver checks `len(KeyPrefix ?? "") + len(stored name) <= 127` and returns `Left(secrets.certificate_invalid_name)` above it. The prefix is never part of the injectivity proof: it is the same for every tenant.
- Tests: a property test generates pairs of (tenant, name) that pass validation and asserts that two different pairs never produce the same stored name, compared case-insensitively, and that splitting at the first `--` returns the pair; a unit test covers `a-`/`b` against `a`/`-b` (both rejected under option A) and `a`/`b-c` against `a-b`/`c` (distinct stored names `a--b-c` and `a-b--c`); a unit test covers the length limit with and without `KeyPrefix`; a two-tenant integration test proves isolation (AC-043).

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

**Class:** Settled. **Settled by:** the issue body ("caching with rotation on change", "a health check that turns degraded before expiry (configurable window)"); option B misses the rotations made outside Encina, so it does not meet "rotation on change". The interval and window values are choice 8.

- Which certificates the health check examines: the names in `CertificateOptions.MonitoredCertificates`, for each tenant returned by `ICertificateTenantSource` when tenancy is on (choice 7). The check reads through the same cache, so it does not add provider calls beyond the refresh.
- The gauge `encina.secrets.certificate_days_to_expiry` is tagged by a hash of the certificate name (the issue's `name hash`), never the name or the tenant id alone, to bound cardinality and avoid leaking identifiers.
- Source fall-through: with several sources (`Secrets`, `File`), only `Left(secrets.not_found)` moves to the next source; any other `Left` is returned. Settled by AGENTS.md §3 (errors are never swallowed): falling through on a provider error would hide it and could serve a different certificate.

</details>

<details>
<summary><strong>6. A refresh that fails — serve the cached certificate until it expires, or only up to a maximum stale age</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Serve the cached certificate until `NotAfter`**, log the failure by code, count it, health degraded | Highest availability: a provider outage does not stop mTLS calls | A certificate rotated out because its key was compromised keeps being used for as long as the provider is unreachable, possibly months |
| **B) Serve it at most `MaxStaleAge` after the last successful read** (proposed 24 hours), then `Left(secrets.certificate_stale)`; failures logged, counted, health degraded, then unhealthy | Bounds how long a revoked certificate can be used during an outage; still rides out ordinary outages | After the bound, calls that need the certificate fail until the provider answers |

### Chosen Option: **B — maximum stale age, 24 hours** (recommended, pending the maintainer)

### Rationale

**Class:** Dilemma. Option B wins when certificates protect personal data and a compromise must stop being usable in bounded time (fail closed, AGENTS.md §3). Option A wins when availability of the external call matters more than a bound on a revoked certificate.

</details>

<details>
<summary><strong>7. How the package gets the tenant and the tenant list</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Core abstractions only**: the current tenant from `IRequestContextAccessor` and fail closed under `MultiTenancyMarker`, as `DeadLetterManager` does; the tenant list of the health check from a small `ICertificateTenantSource` in `Encina.Security.Secrets`, with a documented adapter over `ITenantStore` | No new package edge; the same tenant source as messaging and audit | The application registers the adapter to monitor every tenant |
| **B) `Encina.Security.Secrets` references `Encina.Tenancy`** and uses `ITenantProvider` and `ITenantStore` | Direct | Every secrets user takes the tenancy package |

### Chosen Option: **A — core abstractions with an adapter for the tenant list** (recommended, pending the maintainer)

### Rationale

**Class:** Dilemma. Option A wins for a lean dependency graph and single-tenant users. Option B wins when every deployment is multi-tenant.

</details>

<details>
<summary><strong>8. Defaults</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The values below** | Each has a stated reason; all are options | They are proposals, not derived from a rule |
| **B) Other values chosen by the maintainer** | Fits a deployment the maintainer knows | Needs a reason recorded per value |

### Chosen Option: **A — the values below** (recommended, pending the maintainer)

### Rationale

**Class:** Dilemma (values that no rule fixes).

| Default | Value | Reason |
|---|---|---|
| `RefreshInterval` | 15 minutes | A rotated certificate is picked up within a quarter of an hour; one provider read per certificate and host per interval |
| `DisposeGracePeriod` | 5 minutes, and validated to be at least the consumer's pooled connection lifetime when the consumer declares it (`Encina.Http`, #1233) | A connection opened with the old certificate must not outlive its key handle |
| `ExpiryWarningWindow` | 30 days | Enough time to renew by hand; Key Vault auto-renewal policies commonly act earlier |
| `MaxStaleAge` (choice 6) | 24 hours | Rides out ordinary provider outages; bounds the use of a revoked certificate |
| `KeyStorageFlags` | `DefaultKeySet` | Works for client authentication on Windows and Linux; `EphemeralKeySet` is an open question (Research) |

</details>

---

## Implementation Phases

**Pull request cut.** The `WrapWithDecorators` refactor (Phase 7) comes first, because the keyed uncached chain of Phase 3 needs it. Counts include tests, PublicAPI and the manifest; every pull request stays within 15 files and 1,500 changed lines.

| PR | Content | Files | Est. lines |
|----|---------|-------|-----------|
| 1 | Phase 7 refactor and Phase 1: `ServiceCollectionExtensions` (`WrapWithDecorators` can omit caching; keyed `"certificates"` chain registration helper), `ICertificateReader`, `ICertificateRotationListener`, `CertificateOptions`, `CertificateNameResolver`, `ICertificateTenantSource`, `SecretsErrors`, PublicAPI; unit tests (chain never calls a recording `ICacheProvider`; resolver), guard test, property test (name injectivity); manifest | 13 | ~1,100 |
| 2 | Phases 2-3: `CertificateParser` (PEM chain, PFX collection, Windows re-export), `SecretCertificateReader`, internal `ITenantCertificateReader` (explicit-tenant path for refresh and health check), `CertificateCache` (stale age of choice 6, disposal of an unchanged re-parse), `Log.cs` (5151-5160), PublicAPI; unit tests (parser, reader, cache with `FakeTimeProvider`), guard test; manifest | 11 | ~1,300 |
| 3 | Phases 4-5: `FileCertificateReader`, `FileCertificateOptions`, `CompositeCertificateReader`, `AuditedCertificateReaderDecorator`, `AddEncinaCertificates` in `ServiceCollectionExtensions`, PublicAPI; unit tests (file reader, composite fall-through, audit), DI test (`ValidateOnBuild`, `ValidateScopes`), guard test; manifest | 11 | ~1,200 |
| 4 | Phases 6 and 8: `CertificateExpiryHealthCheck`, `SecretsMetrics`, `SecretsActivitySource`, `Log.cs` (5161-5170), PublicAPI; unit test (health window), AC-044 exporter test, contract test over the provider-shaped fakes and the file reader; manifest | 9 | ~900 |
| 5 | Phase 9: `LocalStackFixture` (Secrets Manager client), integration tests (Vault dev server, LocalStack, file source, loopback mTLS with the PEM path and the chain, two tenants), load and benchmark justification files, README, `docs/features/` page, changelog fragment, `ROADMAP.md`, `docs/INVENTORY.md` | 14 | ~1,300 |

### Phase 1: Abstractions, options and errors

> **Goal**: The public surface and the name resolution.

<details>
<summary><strong>Tasks</strong></summary>

1. **`src/Encina.Security.Secrets/Abstractions/ICertificateReader.cs`**: `ValueTask<Either<EncinaError, X509Certificate2>> GetCertificateAsync(string name, CancellationToken cancellationToken = default)` and `ValueTask<Either<EncinaError, X509Certificate2Collection>> GetCertificateChainAsync(string name, CancellationToken cancellationToken = default)` (intermediates, leaf excluded); XML docs: shared instances, do not dispose, tenant resolved from context.
2. **`Abstractions/ICertificateRotationListener.cs`**: `ValueTask OnCertificateRotatedAsync(string name, string thumbprint, CancellationToken)`.
3. **`Certificates/CertificateOptions.cs`**: `NameTemplate`, `RefreshInterval`, `DisposeGracePeriod`, `ExpiryWarningWindow`, `MaxStaleAge`, `KeyStorageFlags`, `PasswordSecretNameTemplate`, `MonitoredCertificates`, `Sources` ordering (defaults of choice 8); `Validate`.
4. **`Certificates/CertificateNameResolver.cs`**: builds the logical name from the template, the tenant of `IRequestContextAccessor` and `MultiTenancyMarker` (choice 7); joins them with the constant separator `--` (not configurable, choice 4); validates the tenant id and the name against the segment rule of choice 4 (`^[a-z0-9]+(-[a-z0-9]+)*$`, total length plus the length of `SecretsOptions.KeyPrefix` at most 127) and never rewrites them; under tenancy a missing tenant is `Left(secrets.certificate_tenant_required)` and there is no fallback to the unqualified name.
5. **`Abstractions/ICertificateTenantSource.cs`**: the tenant list for the health check (choice 7); no default implementation beyond "no tenants" with tenancy off.
6. **`SecretsErrors.cs`**: codes `secrets.certificate_invalid`, `secrets.certificate_invalid_name`, `secrets.certificate_expired`, `secrets.certificate_stale`, `secrets.certificate_tenant_required`, `secrets.certificate_password_missing` and factories.
7. `PublicAPI.Unshipped.txt`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of issue #1246 in Encina: the certificate reader contract, options, name resolution and error codes.

CONTEXT:
- Package src/Encina.Security.Secrets/. ISecretReader returns ValueTask<Either<EncinaError, string>>; error codes live in SecretsErrors.cs.
- Encina.Security.Secrets does not reference Encina.Tenancy. The tenant comes from IRequestContextAccessor (IRequestContext.TenantId,
  src/Encina/Abstractions/IRequestContextAccessor.cs) and MultiTenancyMarker (src/Encina/Abstractions/MultiTenancyMarker.cs), as in
  src/Encina.Messaging/DeadLetter/DeadLetterManager.cs; tenancy on with no tenant must fail closed. Do not add a Tenancy reference.
- Stored name under tenancy: {tenant}--{name}; tenant and name must each match ^[a-z0-9]+(-[a-z0-9]+)*$ (no leading, trailing or
  doubled hyphen, lower case only), len(SecretsOptions.KeyPrefix ?? "") + total length <= 127. Anything else is
  Left(secrets.certificate_invalid_name), never rewritten. The separator "--" is a constant, never an option: a configurable
  separator breaks injectivity. With the constant, the first "--" of a stored name always ends the tenant, so the concatenation
  is injective, also case-insensitively (Key Vault).

TASK:
Refactor WrapWithDecorators so a keyed chain can omit caching (the default chain is unchanged), then create ICertificateReader,
ICertificateRotationListener, ICertificateTenantSource, CertificateOptions (with Validate), CertificateNameResolver and the new
SecretsErrors codes, with unit, guard and property tests (name resolution with tenancy on/off, missing tenant, no fallback to the
unqualified name, tenant "a-" + name "b" versus tenant "a" + name "-b" both rejected, and a property that two different valid
(tenant, name) pairs never map to the same stored name compared case-insensitively).

KEY RULES:
- No secret in options (the password is a secret name template); options remain [JsonIgnore]-free because they hold none.
- Error codes use the secrets.* prefix; never EncinaError.Message in logs.
- XML docs with an example; PublicAPI.Unshipped.txt; zero warnings.

REFERENCE FILES:
- src/Encina.Security.Secrets/SecretsErrors.cs, SecretsOptions.cs
- src/Encina.Security.Secrets/Abstractions/ISecretReader.cs
- src/Encina.Security.Secrets/ServiceCollectionExtensions.cs (WrapWithDecorators)
- the plan of #1246 (Design Choices 2, 3, 4 and 7)
```

</details>

---

### Phase 2: Certificate parser

> **Goal**: PEM and PFX text become an `X509Certificate2` with its private key, or a typed error.

<details>
<summary><strong>Tasks</strong></summary>

1. **`Certificates/CertificateParser.cs`**: `Either<EncinaError, ParsedCertificate> Parse(string text, string? password, X509KeyStorageFlags flags)` where `ParsedCertificate` holds the leaf and its intermediates: PEM detected by `-----BEGIN`, the leaf loaded with `X509Certificate2.CreateFromPem(certPem, keyPem)` (the first `CERTIFICATE` block and the key block, split by label) and every further `CERTIFICATE` block loaded as an intermediate; on Windows the PEM-loaded leaf is re-exported to PKCS#12 and reloaded with `X509CertificateLoader.LoadPkcs12` and the configured flags (ephemeral PEM keys, choice 3); otherwise base64-decoded PFX loaded with `X509CertificateLoader.LoadPkcs12Collection` (the entry with the private key is the leaf); a certificate without a private key is `Left(secrets.certificate_invalid)`.
2. Expiry check helper: `NotAfter` against `TimeProvider`.
3. Tests: valid PFX with and without password, PEM with RSA and ECDSA keys, PEM and PFX with an intermediate (the chain is kept), truncated input, wrong password, certificate without key, expired and not-yet-valid certificates (generated in the test with `CertificateRequest`).

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
2. **`Certificates/CertificateCache.cs`**: `ConcurrentDictionary<(string Tenant, string Name), Entry>` (the entry holds the leaf, the intermediates and the time of the last successful read); `GetOrAddAsync` with single-flight per key (a `Lazy<Task>` or per-key semaphore); a refresh task per entry on `RefreshInterval` via `TimeProvider.CreateTimer` whose callback catches everything and never throws (AGENTS.md §3, timer callbacks); thumbprint comparison; the replaced instance is disposed after `DisposeGracePeriod`, and a re-parsed instance whose thumbprint is unchanged (leaf and intermediates) is disposed at once (choice 2, Windows key files); the refresh reads through the explicit-tenant path of task 5 with the entry's own `(tenant, name)`; listeners notified on change; after a failed refresh the entry is served until `MaxStaleAge` past its last successful read, then `Left(secrets.certificate_stale)` (choice 6).
3. **The keyed reader**: the keyed `ISecretReader` `"certificates"` is the base provider wrapped in resilience (if enabled) and auditing, without caching (Design Choice 2), built with the helper added in PR 1; `AddEncinaCertificates` (Phase 5, PR 3) registers it.
4. Tests: first read, cached read (no provider call), rotation (provider returns a new certificate, listener fires, old instance disposed after the grace period with `FakeTimeProvider`), a refresh with an unchanged thumbprint disposes the newly parsed instance and keeps the cached one, refresh failure serves until `MaxStaleAge` and then refuses, expired certificate refused, two tenants isolated, tenancy on without tenant fails closed, a refresh with no request context reads the entry's tenant and records it in the audit.
5. **Explicit-tenant read path** (internal): an internal interface `ITenantCertificateReader` with `GetCertificateForTenantAsync(string? tenant, string name, CancellationToken)` (and its chain counterpart), implemented by `SecretCertificateReader` here and by `FileCertificateReader`, `CompositeCertificateReader` and `AuditedCertificateReaderDecorator` in Phase 4, so the explicit path crosses the same sources, fall-through and audit as the public one; each public `GetCertificateAsync` resolves the tenant from `IRequestContextAccessor` and `MultiTenancyMarker` (choice 7) and then calls it. Only the cache refresh (task 2) and the health check (Phase 6), which run without a request context, call it directly; it applies the same segment validation (choice 4) and refuses a null tenant under tenancy. These reads record the tenant explicitly in their audit entries, together with the caller (`refresh` or `health`), rather than the empty request context. It is not public: application code always goes through the ambient tenant.

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
Implement the reader, the cache (single flight, refresh, grace-period disposal, listeners, MaxStaleAge), using the keyed uncached
chain helper from PR 1, and the unit tests listed in the plan.

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

1. **`Certificates/FileCertificateReader.cs : ICertificateReader`**: `FileCertificateOptions` maps a tenant-qualified logical name (choice 4) to a path (PFX or PEM) and a password source (an environment variable name or a secret name; never a literal password); files are read asynchronously, re-read when the file's last write time changes (checked at refresh), parsed by `CertificateParser`.
2. **Source ordering**: `CertificateOptions.Sources` (`Secrets`, `File`); a composite reader tries them in order and returns the first `Right`; `Left(secrets.not_found)` falls through, any other `Left` is returned (no silent failover on errors, choice 5).
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

1. **`AddEncinaCertificates(this IServiceCollection, Action<CertificateOptions>?)`** in `ServiceCollectionExtensions`: options with validation, name resolver, parser, cache, the keyed reader chain, `ICertificateReader` (composite); the health check and metrics are added to it in PR 4; `TryAdd` throughout so an application's own `ICertificateReader` wins.
2. Needs a source: `AddEncinaSecrets` for the provider or a file source. Without either, `AddEncinaCertificates` fails at start-up naming the missing registration. This is a registration-completeness check inside one feature (AGENTS.md §3), not a requirement between modules, so it needs no ADR-050 amendment.
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

1. **`Health/CertificateExpiryHealthCheck.cs : IHealthCheck`** (model `SecretsHealthCheck`): `DefaultName = "encina-certificates"`, static `Tags` `["encina","secrets","certificates","ready"]`; for each monitored name (and each tenant of `ICertificateTenantSource` when tenancy is on) reads through the internal explicit-tenant path of Phase 3 (task 5), `GetCertificateForTenantAsync(tenant, name, ct)`, because a health check runs without a request context; the audit entries of these reads carry the tenant explicitly; `Healthy` outside the window, `Degraded` inside `ExpiryWarningWindow` (description carries the number of certificates and the smallest remaining days, never names), `Unhealthy` when any is expired or unreadable.
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

0. The `WrapWithDecorators` refactor of this phase ships in PR 1, before the reader that needs it (see the pull request cut).
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
You are implementing Phase 7 of issue #1246 in Encina (delivered in PR 1, before Phases 1-3 use it): wiring resilience and audit
into the keyed certificate chain.

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
| Property | name resolution: two different valid (tenant, name) pairs never produce the same stored name, compared case-insensitively (choice 4); parser round trip of generated certificates | 80: the resolver is pure logic |
| Integration | Vault dev server (a generic `hashicorp/vault` dev-mode container through `Testcontainers`), AWS Secrets Manager on LocalStack (fixture extended), file source on a temp directory; Azure Key Vault and Google through fakes of their clients; a loopback mTLS handshake with the returned certificate, once loaded from PEM and once from PFX, each with an intermediate, using `SslStreamCertificateContext.Create(leaf, intermediates)`; two tenants with the same logical name | real services where an emulator exists |
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
| IRequestContextAccessor and MultiTenancyMarker | src/Encina/Abstractions/ | per-tenant names and fail closed (choice 7) |
| DeadLetterManager | src/Encina.Messaging/DeadLetter/DeadLetterManager.cs | the fail-closed tenant pattern to follow |
| ITenantStore | src/Encina.Tenancy/Abstractions/ITenantStore.cs | source of the documented `ICertificateTenantSource` adapter; not referenced by the package |
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
| Is the Windows re-export of a PEM-loaded certificate (choice 3) needed for client authentication, or only for server authentication? | The loopback test on Windows with and without the re-export; keep the re-export only if the handshake fails without it |
| Does the Azure Key Vault secret of a certificate return the PFX base64 or PEM depending on the certificate content type? | Read the provider test doubles and the Key Vault documentation; both formats are parsed anyway |
| Can `hashicorp/vault` dev mode run through the generic `Testcontainers` package already in `Directory.Packages.props`? | Spike in the PR 5 integration test; fall back to a fake `IVaultClient` if not |
| Does the existing `HashiCorpVault` provider return a single value suitable for PEM text? | Read `HashiCorpVaultProvider` and its options (path and key) |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| New production files in Encina.Security.Secrets (ICertificateReader, listener, tenant source, options, parser, name resolver, reader, cache, composite, audit decorator) | 10 | |
| File provider (FileCertificateReader, options) | 2 | |
| Health check | 1 | |
| Diagnostics additions (log messages, metrics, activity) | 3 | edits to existing files |
| Modified existing files (ServiceCollectionExtensions, SecretsErrors, SecretsMetrics, PublicAPI.Unshipped.txt) | 4 | |
| Unit tests | 8 | |
| Guard tests | 2 | |
| Contract tests | 2 | |
| Integration tests | 4 | Vault dev server, LocalStack, file source, loopback mTLS handshake |
| Property tests | 1 | name injectivity and parser round trip |
| Load and benchmark justification files | 2 | |
| Documentation | 5 | README, feature page, changelog fragment, ROADMAP line, INVENTORY |
| Coverage manifest edits | 1 | |
| Total | 45 | Distinct files; the per-PR table under Implementation Phases counts 57 file changes over 5 pull requests, because `ServiceCollectionExtensions`, `Log.cs`, PublicAPI and the manifest change in several |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT:
You are implementing issue #1246 in Encina (.NET 10, C# 14, nullable enabled, pre-1.0, no backward compatibility): X.509 client
certificate retrieval in Encina.Security.Secrets (SPEC-002 REQ-053). Rules: AGENTS.md. The consumer is Encina.Http (P-31, not part of this plan).

IMPLEMENTATION OVERVIEW:
0. WrapWithDecorators refactor so a keyed chain can omit caching (first PR).
1. ICertificateReader (certificate and chain), ICertificateRotationListener, ICertificateTenantSource, CertificateOptions,
   CertificateNameResolver ({tenant}--{name}, both segments ^[a-z0-9]+(-[a-z0-9]+)*$, injective; tenant from IRequestContextAccessor;
   no fallback to the unqualified name under tenancy), new SecretsErrors codes.
2. CertificateParser: PEM or base64 PFX text to the leaf and its intermediates through X509CertificateLoader / CreateFromPem.
3. SecretCertificateReader over a keyed ISecretReader chain without caching, plus CertificateCache (single flight, TimeProvider timer refresh,
   thumbprint change, grace-period disposal, listeners, MaxStaleAge after a failed refresh).
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
- src/Encina/Abstractions/MultiTenancyMarker.cs, src/Encina/Abstractions/IRequestContextAccessor.cs
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

1. The maintainer answers the Dilemmas of this plan (choices 1, 2, 4, 6, 7 and 8); the orchestrator adds the Maintainer Decisions section.
2. Link this plan from #1246.
3. Implement PR 1 to PR 5 in the order of the cut; PR 5 needs PR 3 and PR 4.
4. Verify the open questions (key storage flags, the Windows PEM re-export, Vault dev container) during PR 2 and PR 5.
