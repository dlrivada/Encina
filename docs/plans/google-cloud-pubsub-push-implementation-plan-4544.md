# Implementation Plan: Google Cloud Pub/Sub push subscriptions with signed-token validation that fails closed

> **Issue**: [#4544](https://github.com/dlrivada/Encina/issues/4544)
> **Type**: Feature (security: token validation, fail closed, no secrets in logs)
> **Verified against**: origin/main `f9063538e` (2026-11-27)
> **Complexity**: Medium (6 phases, 5 pull requests of about 14, 9, 12, 8 and 4 files)
> **Estimated Scope**: about 600 lines of production code and about 1,500 lines of tests
> **Family**: transport-new-1 follow-ups (#4543, #4544, #4545). Builds on #237 (the pull transport).

---

## Summary

#237 ships Google Cloud Pub/Sub with streaming pull only; its Design Choice 3 recommends pull and leaves push for this issue. This plan adds a minimal-API endpoint that receives push requests, validates the Google-signed OIDC token in the `Authorization` header, decodes the push envelope and invokes the handler, so Cloud Run and Cloud Functions hosts can receive messages by HTTP. State of `origin/main` (`f9063538e`):

- `Encina.GoogleCloudPubSub` is not on `main` (only its plan is); the pull handler is a delegate passed to `SubscribeAsync` (#237 Design Choice 2), so there is no handler registry to dispatch to: the push endpoint takes the delegate as a parameter (Design Choice 7). The issue's snippet `MapEncinaGoogleCloudPubSubPush("/pubsub/orders", options => ...)` has no handler argument; this plan adds one.
- The web layer: `Encina.AspNetCore` references the compliance, DPIA, audit and tenancy packages (`src/Encina.AspNetCore/Encina.AspNetCore.csproj`), too heavy for a transport; `Encina.Tenancy.AspNetCore` and `Encina.AspNetCore.Blazor` are the precedent for a small `X.AspNetCore` package with a `FrameworkReference` to `Microsoft.AspNetCore.App` and PublicAPI analyzers. `DPIAEndpointExtensions` is the precedent for an `Encina` endpoint-mapping extension.
- Token library, verified on 2026-11-27 by downloading **Google.Apis.Auth 1.78.0** (Apache-2.0; arrives transitively through `Google.Api.Gax.Grpc` 4.16.0, a dependency of `Google.Cloud.PubSub.V1`): its XML documentation has `GoogleJsonWebSignature.ValidateAsync(string, ValidationSettings)` throwing `InvalidJwtException` for any failed check, `ValidationSettings.Audience` (**null suppresses audience validation**), `ValidationSettings.Clock` (an `IClock`), `IssuedAtClockTolerance`, `ExpirationTimeClockTolerance`, `ForceGoogleCertRefresh`, and `Payload.Email` and `Payload.EmailVerified`. The null-suppresses-audience behavior is why the options validator below makes the audience mandatory.
- Time comes from `TimeProvider` (AGENTS.md section 3): an internal `IClock` adapter over `TimeProvider` is passed through `ValidationSettings.Clock`.

Delivered: package `Encina.GoogleCloudPubSub.AspNetCore` with `EncinaGoogleCloudPubSubPushOptions` (validated), `AddEncinaGoogleCloudPubSubPush`, `MapEncinaGoogleCloudPubSubPush<TMessage>`, an internal token verifier, a push envelope reader, a category-to-status mapper, log messages 40688-40689 (spare ids of the #237 block), a consume span and a counter, unit, guard, property, contract and integration tests, and documentation. Delivery stays at-least-once: a non-2xx answer makes Pub/Sub redeliver and, with a dead-letter policy, dead letter.

**Provider category**: Messaging transports (10 existing, 6 planned; AGENTS.md section 5); one cloud transport, tested with the emulator of #237 (SPEC-000 DEC-009). The AWS and Azure side of the cloud triangle has no push-signed-token counterpart in scope: EventBridge delivers to targets (#238).

**Shared pieces**: #237 owns the transport, the client holder, `PubSubEmulatorFixture` and the spare ids 40688-40689; #240 owns the conformance base; #4543 owns the tenant header contract (the push path reads the attribute, it does not stamp). This plan owns the package `Encina.GoogleCloudPubSub.AspNetCore` (when D1 is answered A).

---

## Design Choices

<details>
<summary><strong>1. Package placement (Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A separate package `Encina.GoogleCloudPubSub.AspNetCore`** | The transport keeps no web dependency (pay-for-what-you-use); the `X.AspNetCore` precedent exists | One more package, manifest and README |
| **B) A `Push/` folder inside `Encina.GoogleCloudPubSub`** | Fewer packages | Every user of the transport takes `Microsoft.AspNetCore.App` |
| **C) Inside `Encina.AspNetCore`** | One web package | Adds Google dependencies to a package that references compliance and audit packages |

### Chosen Option: **A, a separate package** (recommended, pending the maintainer)

### Rationale

A wins for a worker that only pulls; B wins if the maintainer wants one package per transport whatever the cost; C is listed by the issue's affected packages but forces the Google client on every web user. Under B the package-specific tasks (csproj, slnx, manifest) move into #237's package and the EventIds stay in its assembly.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Token validation library (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Google.Apis.Auth` `GoogleJsonWebSignature.ValidateAsync`** | Already in the dependency graph through the Pub/Sub client; knows Google's issuers and rotating certificates; verified API | Static call: an internal seam is needed for unit tests; no ASP.NET authentication integration |
| **B) ASP.NET Core JWT bearer authentication with the Google OpenID configuration** | `[Authorize]`, standard handlers and events | A new package family (`Microsoft.AspNetCore.Authentication.JwtBearer` is not in `Directory.Packages.props`); the host owns the authentication configuration, so a misconfiguration fails open |

### Chosen Option: **A, `Google.Apis.Auth`** (recommended, pending the maintainer)

### Rationale

A wins because the endpoint itself owns the gate (no host configuration can leave it open) and adds no package; B wins for hosts that already standardize on ASP.NET authentication. A needs a direct `PackageReference` and a `Directory.Packages.props` entry pinned to the version `Google.Cloud.PubSub.V1` resolves (1.78.0 today; the restore of the first pull request shows no downgrade warning).

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Valid token of a principal that is not allowed (Dilemma D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Forbidden` (403, ADR-062 R6: denials and identity conflicts) when the signature, issuer, audience and expiry pass but the email is not in the allow-list or is unverified; `Unauthenticated` (401 with `WWW-Authenticate: Bearer`) when the token is missing or fails validation** | Follows the choice rules; operators can tell a wrong sender from a forged token in the counter | The issue says "a missing or invalid token returns 401"; a wrong principal returns 403 |
| **B) 401 for every token failure** | Literal reading of the issue; leaks nothing to the caller | Ignores R6; the counter conflates two causes |

### Chosen Option: **A, 401 for no identity and 403 for a wrong principal** (recommended, pending the maintainer)

### Rationale

Pub/Sub treats every non-2xx answer as a nack, so the status matters to operators, not to the sender: A wins for monitoring and for ADR-062; B wins if the maintainer reads the issue's sentence literally. Under both options the verification is fail closed and nothing is dispatched.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Binding the request to one subscription (Dilemma D4)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Token only: any request with a valid token for the audience and an allowed account is accepted** | The service's own model; no extra option | A token captured for one subscription of the same project and audience is valid for the others for its lifetime (about an hour) |
| **B) An optional `ExpectedSubscription`: when set, the `subscription` field of the envelope must equal it, else `Forbidden` (`GOOGLEPUBSUB_PUSH_SUBSCRIPTION_MISMATCH`)** | Narrows replay across subscriptions that share an audience; off unless asked for | The body is not signed, so a captured token still lets an attacker send any body to a matching subscription |

### Chosen Option: **B, optional subscription binding** (recommended, pending the maintainer)

### Rationale

B costs nothing when unset and removes one replay path when set; A wins for the simplest configuration. The documentation states the residual risk: the token authorizes the sender, it does not sign the body, so the audience must be unique per endpoint and the endpoint HTTPS only.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Local and emulator testing without a bypass (Dilemma D5)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No switch disables validation; tests replace the internal `IPushTokenVerifier` through DI (`TryAddSingleton` in production, replaced in the test host)** | No code path that can ship open; AGENTS.md section 3 | The real Google certificate fetch is exercised only against a stub |
| **B) An explicit `AllowUnauthenticatedPush` flag valid only with `AllowLocalEndpoints` of #237, logged at startup** | The emulator can push without a token in end-to-end tests | A fail-open switch exists in the shipped code |

### Chosen Option: **A, no bypass** (recommended, pending the maintainer)

### Rationale

Both options keep the opt-out explicit and logged as the rule requires, so the rule does not decide; A wins for security review, B wins if the maintainer wants the emulator to exercise the endpoint without a stub. The emulator probe (Phase 5) records whether it sends an `Authorization` header at all.

**Class:** Dilemma

</details>

<details>
<summary><strong>6. Mapping an error category to the HTTP status (Dilemma D6)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A small internal mapper in the new package for the five categories it produces (`Unauthenticated` 401, `Forbidden` 403, `Validation` 400, `Unavailable` 503, `Internal` 500) with an empty body, checked against the ADR-062 table by a unit test** | No dependency on `Encina.AspNetCore`; the body never carries an error message | A second mapping table beside `Encina.AspNetCore` |
| **B) Use `ProblemDetailsExtensions.ToProblemDetails` of `Encina.AspNetCore`** | One mapping | A project reference to the heavy web package, and a ProblemDetails body Pub/Sub ignores |

### Chosen Option: **A, an internal mapper** (recommended, pending the maintainer)

### Rationale

A wins for dependency weight and for a body that cannot leak anything; B wins if the application already references `Encina.AspNetCore` and wants one table. A 401 carries `WWW-Authenticate: Bearer` (ADR-062 header rules).

**Class:** Dilemma

</details>

<details>
<summary><strong>7. Handler shape (Dilemma D7)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `MapEncinaGoogleCloudPubSubPush<TMessage>(pattern, Func<TMessage, ValueTask> handler, Action<EncinaGoogleCloudPubSubPushOptions>? configure = null)`: the same delegate type as `SubscribeAsync` (the answer of #237 D2 decides the exact delegate)** | The same handler serves pull and push; no registry | The delegate cannot resolve scoped services by itself |
| **B) The handler is a service `IGoogleCloudPubSubPushHandler<TMessage>` resolved per request** | Scoped dependencies work | A second handler concept that pull does not have |

### Chosen Option: **A, the same delegate** (recommended, pending the maintainer)

### Rationale

A wins for "the same handler as pull" (the issue); B wins when the handler needs a request scope (for example an `IEncina` call with a `DbContext`). A handler failure (exception, or a `Left` when #237 D2 answers B) is a 500 nack with the exception type logged through `ForLogging()`.

**Class:** Dilemma

</details>

<details>
<summary><strong>8. Mandatory audience and allowed accounts</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Audience` and at least one `AllowedServiceAccounts` entry are required; startup validation fails (`ValidateOnStart`) when either is empty, and the verifier also requires `email_verified`** | No configuration validates every Google token | Two options to set |
| **B) Optional audience and account list** | Fewer options | A missing audience validates any Google-signed token for any audience |

### Chosen Option: **A, mandatory**

### Rationale

The token library suppresses audience validation when the audience is null, and a gate with missing context denies (AGENTS.md section 3, SPEC-002 DEC-006); the issue names issuer, audience, signature and service account as the checks. Only A satisfies all three under every option of D2.

**Class:** Settled
**Settled by:** AGENTS.md section 3 (compliance and security gates fail closed) and #4544 (the endpoint "validates the token (issuer, audience, signature, service account)")

</details>

<details>
<summary><strong>9. Acknowledgement and poison messages</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) 2xx only after the handler returned; a failed handler, an invalid envelope or an unavailable verifier answers non-2xx so the service nacks, redelivers and applies the dead-letter policy** | Never reports success on a failure | A permanently invalid message cycles until the dead-letter policy or the retention ends it |
| **B) Answer 2xx for an invalid envelope to stop redelivery** | No cycling | Silently drops data |

### Chosen Option: **A, non-2xx on any failure**

### Rationale

Errors are never swallowed (AGENTS.md section 3) and the service defines acknowledgement by a 2xx status and negative acknowledgement by any other status (the issue's Alternatives entry 3). Categories (ADR-062): an invalid envelope or body over `MaxBodyBytes` is `Validation` (400, R3); a failure of the verifier that is not an invalid token (certificate fetch, network) is `Unavailable` (503, R7) and fails closed; a handler failure is `Internal` (500, R4). Cancellation of the request propagates and is never a `Left` or a breaker failure.

**Class:** Settled
**Settled by:** AGENTS.md section 3 (errors are never swallowed) and #4544 Alternatives entry 3 (2xx acknowledges, any other status nacks)

</details>

---

## Implementation Phases

### Phase 1: Package, options and DI (pull request 1)

<details>
<summary>Tasks</summary>

1. `src/Encina.GoogleCloudPubSub.AspNetCore/Encina.GoogleCloudPubSub.AspNetCore.csproj` (`FrameworkReference Microsoft.AspNetCore.App`, `PackageReference Google.Apis.Auth`, project references to `Encina.GoogleCloudPubSub` and `Encina`, `Microsoft.CodeAnalysis.PublicApiAnalyzers`, `InternalsVisibleTo Encina.UnitTests`), `Encina.slnx` entry, `Directory.Packages.props` entry for `Google.Apis.Auth` 1.78.0, `PublicAPI.Shipped.txt` and `PublicAPI.Unshipped.txt` (through `mechanical-fixer`).
2. `EncinaGoogleCloudPubSubPushOptions`: `Audience`, `AllowedServiceAccounts`, `ExpectedSubscription`, `MaxBodyBytes` (default 16 MiB, validated above zero: a 10 MB message base64-encoded and wrapped is about 13.4 MB, to confirm against vendor documentation), `ClockSkew` (default one minute, validated 0 to 5 minutes). The class holds no secret (audience and account emails are public identifiers), so the `[JsonIgnore]` rule does not apply. Internal `EncinaGoogleCloudPubSubPushOptionsValidator : IValidateOptions<>` (Design Choice 8).
3. `AddEncinaGoogleCloudPubSubPush(this IServiceCollection, Action<EncinaGoogleCloudPubSubPushOptions>? configure = null)`: options with `ValidateOnStart`, `TryAddEnumerable` of the validator, `TryAddSingleton(TimeProvider.System)`; the verifier is registered in pull request 2, so this pull request builds alone.
4. Tests: validator and options unit tests, DI unit test under `ValidateOnBuild` and `ValidateScopes`, `GoogleCloudPubSubPushGuardTests` (null `services`; null `configure` accepted), `UnitTests.csproj` and `GuardTests.csproj` references, the manifest `Encina.GoogleCloudPubSub.AspNetCore.json` with package-level targets and the per-file targets of Phase 6 (`coverage-report.cs --check-justifications` fails a manifest without them). 14 files in all.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: Encina.Tenancy.AspNetCore is the small X.AspNetCore precedent. Google.Apis.Auth 1.78.0 comes transitively with Google.Cloud.PubSub.V1 and must be pinned for a direct reference.
TASK: Create the package skeleton, the validated options and AddEncinaGoogleCloudPubSubPush with unit and guard tests.
KEY RULES: audience and at least one allowed account are mandatory (startup fails otherwise); TryAdd registrations only; DI test with ValidateOnBuild and ValidateScopes; no Obsolete; PublicAPI files for public types.
REFERENCE FILES: src/Encina.Tenancy.AspNetCore/, docs/plans for #237 (options and DI), .github/coverage-manifest/Encina.Tenancy.AspNetCore.json.
```

</details>

### Phase 2: Token verification (pull request 2)

<details>
<summary>Tasks</summary>

1. Internal `IPushTokenVerifier.VerifyAsync(string? authorizationHeader, CancellationToken)` returning `Either<EncinaError, PushPrincipal>` and `GoogleJwtPushTokenVerifier` (singleton; `IOptions<>`, `TimeProvider`): parses `Bearer <jwt>` (missing or not Bearer: `GOOGLEPUBSUB_PUSH_TOKEN_MISSING`), calls `GoogleJsonWebSignature.ValidateAsync` with `Audience` from the options, `Clock` from an internal `TimeProviderClock : IClock`, and the tolerances from `ClockSkew`; `InvalidJwtException` becomes `GOOGLEPUBSUB_PUSH_TOKEN_INVALID`; any other exception (network, certificate fetch) becomes `GOOGLEPUBSUB_PUSH_VERIFIER_UNAVAILABLE`; then requires `Payload.EmailVerified` and the email in `AllowedServiceAccounts` (ordinal, ignoring case), else `GOOGLEPUBSUB_PUSH_PRINCIPAL_DENIED`. The static library call sits behind an internal delegate so unit tests substitute it. The token and the header are never logged.
2. Codes in `GoogleCloudPubSubPushErrorCodes.cs` (ADR-062): `..._TOKEN_MISSING` and `..._TOKEN_INVALID` `Unauthenticated` (R6, no identity); `..._PRINCIPAL_DENIED` `Forbidden` (R6; `Unauthenticated` if D3 is answered B); `..._VERIFIER_UNAVAILABLE` `Unavailable` (R7); constant messages; `OperationCanceledException` propagates.
3. `Log.cs` 40688 (request rejected: error code and category only, level Information so unauthenticated traffic cannot flood Warning) and 40689 (handler failed, added in pull request 3); both are the spare ids inside `EventIdRanges.GoogleCloudPubSub` (40680-40689) registered by #237, so no new range is created; the `AssemblyRanges` entry of `EncinaEventIdAllocationTests.cs` maps `Encina.GoogleCloudPubSub.AspNetCore` to the `GoogleCloudPubSub` field (if the #237 implementation has used 40688 or 40689 first, the ids are to be assigned by the orchestrator). DI edit (`TryAddSingleton<IPushTokenVerifier, GoogleJwtPushTokenVerifier>`) with the resolution test.
4. Tests: `GoogleJwtPushTokenVerifierTests` (missing, malformed, invalid, unavailable, unverified email, other email, allowed email, tolerance and clock from a `FakeTimeProvider`), DI test edit, manifest edit. 9 files (verifier, codes, `Log`, allocation test, DI edit, DI test edit, verifier test, guard edit if any public member, manifest).

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: Google.Apis.Auth GoogleJsonWebSignature.ValidateAsync throws InvalidJwtException on any failed check; a null Audience suppresses audience validation.
TASK: Implement the internal token verifier behind a seam, its codes and log message, with unit tests.
KEY RULES: fail closed on every path (nothing dispatched unless Right); never log the token, the header, an email or a message of an exception; named ADR-062 category on every EncinaErrors call; TimeProvider through ValidationSettings.Clock; cancellation propagates.
REFERENCE FILES: src/Encina/Abstractions/IRequestContext.cs for TimeProvider use, docs/architecture/adr/062-error-category-contract.md, tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs.
```

</details>

### Phase 3: Envelope, endpoint and telemetry (pull request 3)

<details>
<summary>Tasks</summary>

1. Internal `PushEnvelope` records and `PushEnvelopeReader`: reads the body with a hard limit of `MaxBodyBytes` (the token is verified before the body is read, so an unauthenticated caller cannot make the endpoint buffer a large body), then the push format of the service (`message.data` base64, `message.attributes`, `message.messageId`, `message.publishTime`, `message.orderingKey`, `subscription`, `deliveryAttempt`), decodes the data and deserializes `TMessage` with the serializer of the pull path (#237 Design Choice 8). A malformed envelope or data is `GOOGLEPUBSUB_PUSH_ENVELOPE_INVALID` `Validation` (R3).
2. `GoogleCloudPubSubPushEndpointExtensions.MapEncinaGoogleCloudPubSubPush<TMessage>(this IEndpointRouteBuilder endpoints, string pattern, Func<TMessage, ValueTask> handler, Action<EncinaGoogleCloudPubSubPushOptions>? configure = null)` (public, returns `IEndpointConventionBuilder`; the route is a POST): verifier, then the optional subscription check (D4), envelope, handler, then 204. A handler exception is `GOOGLEPUBSUB_PUSH_HANDLER_FAILED` `Internal` (R4), logged as 40689 with `ForLogging()`. The internal status mapper (D6) writes the status and, for 401, `WWW-Authenticate: Bearer`; the body is empty.
3. Telemetry: the span `googlecloudpubsub push` (kind Consumer) on the transport's `ActivitySource` named `Encina.GoogleCloudPubSub` (registered in `Encina.OpenTelemetry` by #237), linked to the producer through the `traceparent` attribute with no baggage (ADR-054 D2); counter `googlecloudpubsub.push.received` with tags `outcome` and `reason` (the error code); the tenant tag `encina.tenant_id` only when `MultiTenancyMarker` is registered and the `encinatenantid` attribute of #4543 is present (ADR-054 D4). `Encina.GoogleCloudPubSub.csproj` gains `InternalsVisibleTo Encina.GoogleCloudPubSub.AspNetCore` to reuse the source and metrics.
4. Tests: `PushEnvelopeReaderTests` (unit), `PushEnvelopeReaderPropertyTests` (FsCheck: base64 round trip of arbitrary payloads and attributes), `GoogleCloudPubSubPushEndpointTests` (unit over `TestServer`, which `Encina.UnitTests` references: 401 and nothing dispatched without a token, 403, 400, 503, 500, 204), `PushStatusMapperTests` (parity with the ADR-062 table), guard edit (null `endpoints`, `pattern`, `handler`), manifest edit. 12 files.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: The verifier and options exist. The service pushes POST bodies with an OIDC token; any non-2xx answer is a nack.
TASK: Implement the envelope reader, the endpoint mapping, the status mapper and the telemetry, with unit, property and guard tests.
KEY RULES: verify the token before reading the body; a handler exception never escapes the endpoint; nothing is dispatched on any Left; no payload, attribute value, token or message id in logs or tags; the body of every response is empty; 401 carries WWW-Authenticate.
REFERENCE FILES: src/Encina.AspNetCore/DPIAEndpointExtensions.cs, docs/plans for #237 (attributes, serializer), src/Encina.Messaging/Diagnostics/OutboxActivitySource.cs.
```

</details>

### Phase 4: Cross-cutting integration

<details>
<summary>Tasks</summary>

1. Verify each row of the matrix below against the code; the deferred rows point to #4543 and #4392.
2. Confirm every code has its ADR-062 category, that no log call or tag carries the token, an email, a payload or `EncinaError.Message`, and that no switch disables validation.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT: The push endpoint verifies, decodes, dispatches and reports.
TASK: Verify the cross-cutting matrix against the code and fix any gap.
KEY RULES: AGENTS.md sections 3, 6, 7; ADR-054 D4; ADR-062 categories.
REFERENCE FILES: AGENTS.md section 6, docs/architecture/adr/062-error-category-contract.md.
```

</details>

### Phase 5: Emulator integration and contract tests (pull request 4)

<details>
<summary>Tasks</summary>

1. First task, a probe: `GoogleCloudPubSubPushProbeTests` records whether the emulator of #237 delivers push requests to a reachable URL (the fixture exposes the test host through `Testcontainers` host port exposure) and whether it sends an `Authorization` header; the integration targets below hold when it does, and a feature the emulator does not support is asserted at the unit level and named in the test, never skipped silently.
2. `GoogleCloudPubSubPushIntegrationTests` in the `PubSubEmulator` collection of #237 (`[Trait("Category","Integration")]`, `[Trait("Transport","GoogleCloudPubSub")]`): a real publish reaches the handler through the endpoint with a stub `IPushTokenVerifier` registered in the test host (D5 A), and a request with no token dispatches nothing.
3. `GoogleCloudPubSubPushConformanceTests` in `tests/Encina.ContractTests/MessageBrokers/GoogleCloudPubSub/`: the receive scenarios of the conformance base of #240 (message-id round trip, failure then non-2xx) over `TestServer`; `ContractTests.csproj` gains the `Microsoft.AspNetCore.App` framework reference and the `TestHost` package reference (it has neither today), `IntegrationTests.csproj` the project reference. 8 files with the fixture edit and the manifest edit.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT: SPEC-000 requires the Pub/Sub transport to be tested against the free emulator through a shared collection; its push support is limited.
TASK: Add the probe, the integration tests with a stub verifier, and the conformance subclass.
KEY RULES: one shared emulator container, never per class; no Thread.Sleep (bounded waits on TaskCompletionSource); Shouldly through Encina.Testing.Shouldly; never a code switch that disables validation.
REFERENCE FILES: docs/plans for #237 and #240, tests/Encina.IntegrationTests/MessageBrokers/AmazonSQS/.
```

</details>

### Phase 6: Documentation and finalization (pull request 5)

<details>
<summary>Tasks</summary>

Per-file targets (entered in `Encina.GoogleCloudPubSub.AspNetCore.json` by the pull request that adds the tests; the manifest does not exist yet, so no row is "unchanged"; a target needs its test file in the same pull request):

| Pull request | File | unit | guard | contract | property | integration | Justification |
|---|---|:---:|:---:|:---:|:---:|:---:|---|
| 1 | `EncinaGoogleCloudPubSubPushOptions.cs`, validator | 100 | 0 | 0 | 0 | 0 | Pure data and validation; guard tests have no argument to check. |
| 1 | `ServiceCollectionExtensions.cs` | 85 | 10 | 0 | 0 | 0 | Unit tests build the provider per option set; guard reaches the null check of `services`. |
| 2 | `GoogleJwtPushTokenVerifier.cs` | 90 | 0 | 0 | 0 | 0 | An internal type: the seam reaches every branch; guard tests cannot reach it. |
| 2 | `GoogleCloudPubSubPushErrorCodes.cs`, `Log.cs` | 0 | 0 | 0 | 0 | 0 | Constants and generated delegates have no executable lines. |
| 3 | `PushEnvelopeReader.cs` | 95 | 0 | 0 | 50 | 0 | Unit tests cover each malformed shape; the property test round-trips payloads. |
| 3 | `GoogleCloudPubSubPushEndpointExtensions.cs` | 85 | 20 | 50 | 0 | 60 | `TestServer` tests reach each status; guard reaches the three argument checks; conformance and the emulator run the happy path. |
| 3 | `PushStatusMapper.cs` | 100 | 0 | 0 | 0 | 0 | A pure table, tested against ADR-062. |

1. `src/Encina.GoogleCloudPubSub.AspNetCore/README.md` and the Push section of the Pub/Sub feature page (the page of #237's documentation pull request; when it is not on `main` the README carries the section) through `docs-writer`; `changelog.d/4544-google-cloud-pubsub-push.added.md` through `mechanical-fixer`; load and benchmark justification files `tests/Encina.LoadTests/Messaging/GoogleCloudPubSubPush/GoogleCloudPubSubPush.md` and the benchmark twin (throughput is the service's and the token library's). 5 files.
2. `dotnet build --configuration Release` with zero warnings; every flag at its target; CRAP of every new method at most 10.

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT: The push endpoint is implemented and tested.
TASK: Write the two justification files and hand the README, feature page section and changelog fragment to the owning specialists; re-measure the per-flag targets.
KEY RULES: targets never go down; no hand-typed coverage figures in docs; the docs state the residual risk of an unsigned body and that the audience must be unique per endpoint.
REFERENCE FILES: tests/Encina.LoadTests/Messaging/RedisPubSub/RedisPubSub.md, changelog.d/README.md.
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard / Spec | Relevance | Notes |
|-----------------|-----------|-------|
| `AGENTS.md` sections 3, 5, 6, 7, 9 | Fail-closed gates, transport category, 12 functions, EventIds, coverage | All phases |
| ADR-062 R3, R4, R6, R7 | Categories of every code; 401 carries `WWW-Authenticate` | Design Choices 3, 9 |
| ADR-054 D2, D4 | Links without baggage; tenant tag only when tenancy is on | Phase 3 |
| SPEC-000 DEC-009 | Pub/Sub is in 1.0 scope, tested with the emulator | Scope |
| Cloud Pub/Sub push subscription contract (named product documentation) | POST body, OIDC token, 2xx acknowledges | Design Choice 9 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `Encina.Tenancy.AspNetCore` | `src/Encina.Tenancy.AspNetCore/` | Small `X.AspNetCore` package precedent |
| `DPIAEndpointExtensions` | `src/Encina.AspNetCore/DPIAEndpointExtensions.cs` | Endpoint mapping precedent |
| `MultiTenancyMarker` | `src/Encina/Abstractions/MultiTenancyMarker.cs` | Tenant tag gate |
| Plan of #237 | `docs/plans/` | Transport, serializer, attributes, emulator fixture, spare ids |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.GoogleCloudPubSub.AspNetCore` (uses the registered `GoogleCloudPubSub` range) | 40688-40689 | 40688 request rejected, 40689 handler failed; the two spare ids of #237's 40680-40689 (its ids 40680-40687 are claimed); no new range |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production code | 11 | csproj, options, validator, DI, verifier, codes, `Log`, envelope, reader, endpoint, mapper |
| Tests | 14 | options, validator, DI, verifier, reader, property, endpoint, mapper, guard, probe, integration, conformance, 2 csproj edits |
| Shared edits | 7 | `Encina.slnx`, `Directory.Packages.props`, 2 test csproj, `ContractTests.csproj`, `IntegrationTests.csproj`, allocation test |
| Documentation and manifests | 8 | README, feature page, changelog, 2 justification files, 2 PublicAPI files, manifest |
| **Pull request 1** | **14** | about 600 lines |
| **Pull request 2** | **9** | about 500 lines |
| **Pull request 3** | **12** | about 900 lines |
| **Pull request 4** | **8** | about 450 lines |
| **Pull request 5** | **5** | about 200 lines |

Implementers and callers: all types are new, so there are no implementers or `new X(` sites to update; the edit of the transport csproj is additive. Each pull request builds alone in the order 1 to 5.

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina is a pre-1.0 .NET 10 / C# 14 library; no backward compatibility, zero warnings, ROP (Either<EncinaError, T>). #237 provides the Pub/Sub pull transport.

IMPLEMENTATION OVERVIEW: PR1 package, options (audience and allowed accounts mandatory), DI; PR2 token verifier over GoogleJsonWebSignature behind a seam, codes, log 40688; PR3 envelope reader, endpoint mapping, status mapper, telemetry, log 40689; PR4 emulator probe, integration and conformance tests; PR5 documentation. Dilemmas D1 to D7 are implemented as the maintainer answers, the recommended options until then.

KEY PATTERNS: fail closed (any Left dispatches nothing); verify before reading the body; no token, email, payload or exception message in logs; named ADR-062 category on every error; TimeProvider through ValidationSettings.Clock; cancellation never a Left; targets never go down; CRAP <= 10.

REFERENCE FILES: docs/plans for #237, src/Encina.Tenancy.AspNetCore, src/Encina.AspNetCore/DPIAEndpointExtensions.cs.
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Signing certificates are cached by the token library; Encina caches nothing. |
| 2 | OpenTelemetry | ✅ | Consume span, counter, tenant tag only when tenancy is on (Phase 3). |
| 3 | Structured Logging | ✅ | 40688-40689, error code and exception type only. |
| 4 | Health Checks | ❌ | No dependency of its own; the transport's health check of #237 covers the topic. |
| 5 | Validation | ✅ | Options, token, envelope and body size are validated. |
| 6 | Resilience | ⏭️ | No consumer loop here; consumer-side resilience is #4392 (open); the certificate fetch retries are the library's. |
| 7 | Distributed Locks | ❌ | None. |
| 8 | Transactions | ❌ | HTTP acknowledgement. |
| 9 | Idempotency | ✅ | The `message-id` attribute and `message.messageId` reach the handler's envelope as on pull; deduplication stays with the inbox. |
| 10 | Multi-Tenancy | ⏭️ | The tenant header is stamped by #4543 (open); this plan only tags telemetry; rebuilding a request context from it is #1164 and #4392. |
| 11 | Module Isolation | ⏭️ | Same, with #4543 Phase 5. |
| 12 | Audit Trail | ❌ | Rejected requests are counted and logged by code (40688); the audit stores record operator and business actions. |

---

## Prerequisites & Dependencies

- **Blocking**: #237 pull requests 1 to 4 for pull requests 1 to 3 here (options, the client holder, serializer, the `ActivitySource`), and its pull request 5 (the emulator fixture) for pull request 4.
- **Related, not blocking**: #4543 (tenant attribute), #4392 (consumer resilience), #742, #1164, #260.
- **Sibling plans**: #4545 is independent; #240 owns the conformance base this plan subclasses.

---

## Next Steps

1. Ask the maintainer D1 to D7 when work on #4544 starts; record the answers in `## Maintainer Decisions`.
2. Pull request 1: package skeleton (14 files).
3. Pull requests 2 and 3: token verification (9), envelope, endpoint and telemetry (12).
4. Pull requests 4 and 5: emulator and conformance tests (8), documentation (5).
