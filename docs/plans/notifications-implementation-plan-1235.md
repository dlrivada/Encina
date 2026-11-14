# Implementation Plan: Consent-Aware Multi-Channel Notifications — `Encina.Notifications` and the Channel Satellites of DEC-016

> **Issue**: [#1235](https://github.com/dlrivada/Encina/issues/1235) (SPEC-002 P-33, REQ-041, AC-039, AC-043, scenario S12; parent [#1186](https://github.com/dlrivada/Encina/issues/1186))
> **Type**: Feature
> **Complexity**: Very High (a core package, four channel satellites, a delivery-status store on the 10 database providers, consent and outbox integration)
> **Estimated Scope**: ~3,500-4,500 lines of production code + ~5,000-6,500 lines of tests; ~70 new and ~25 modified production files (split into 11 pull requests, see Next Steps)

---

## Summary

A practice sends reminders and messages to patients by e-mail, SMS and WhatsApp, some transactional and some commercial. Every send has to respect channel consent and the LSSI rules for commercial messages (SPEC-002 REQ-014, REQ-041, note H, H-R28 to H-R31), has to survive a restart, and has to leave an auditable delivery status without keeping the message payload forever (REQ-018). This plan adds `Encina.Notifications` (sender, templates, quiet hours, deduplication, delivery status, consent gate, outbox delivery) and the four channel satellites fixed by SPEC-002 DEC-016: SMTP, one HTTP mail API, one SMS provider and the WhatsApp Cloud API.

### What exists today (verified on `main` at `7a0049c0`)

| Area | Current state | Evidence |
|------|---------------|----------|
| E-mail, SMS, WhatsApp code | None; no `Smtp`, `MailKit`, `Twilio`, `SendGrid` or `WhatsApp` reference in `src/` or `Directory.Packages.props` | grep over `src/`, `Directory.Packages.props`, `docker-compose.yml` |
| "Notification" in core | In-process publish/subscribe (`INotification`, `IHasNotifications`), unrelated to user-facing delivery | `src/Encina.Messaging/Outbox/IHasNotifications.cs`, `OutboxOrchestrator.cs:88` |
| Consent API | `IConsentService.HasValidConsentAsync(subject, purpose)`; purpose is a string; no channel scoping | `src/Encina.Compliance.Consent/Abstractions/IConsentService.cs:189` |
| Channel-scoped consent (P-11) and the transactional/commercial marker | Do not exist ("channel" appears only as the collection `source`) | `IConsentService.cs:52`, `ConsentAggregate.cs:70` |
| Outbox | `OutboxOrchestrator.AddAsync<T>`, `IOutboxStore`, retry with backoff, exhausted state | `src/Encina.Messaging/Outbox/OutboxOrchestrator.cs:88`, `IOutboxStore.cs:23`, `OutboxRetryBackoff.cs` |
| Dead letter and error classification | `IDeadLetterStore`, `IErrorClassifier`, `OutboundErrorCodes`, `RetryAfterParser` | `src/Encina.Messaging/DeadLetter/`, `src/Encina/Recoverability/` |
| Webhook validation (#731, #734) | No package and no plan; only test helpers in `Encina.Testing.WireMock` | `src/Encina.Testing.WireMock/WebhookTestingExtensions.cs` |
| Tenant | `IRequestContext.TenantId` is `string?`; no tenant time zone anywhere | `src/Encina/Abstractions/IRequestContext.cs:125` |
| Store layout and scripts | Highest ADO script `030` (SqlServer, PostgreSQL); next number 031 (used by plan #1234, see Prerequisites) | `src/Encina.ADO.*/Scripts/` |
| SMTP test server | None in `docker-compose.yml` or `tests/` | grep for smtp4dev, mailpit, papercut |

### Standards covered

LSSI-CE (Ley 34/2002) art. 21 (commercial communications, art. 21.2 existing-client exception and opt-out), GDPR Art. 6(1)(a), 7 and 21(2) to (3), ePrivacy Directive 2002/58/EC art. 13, WhatsApp Business Cloud API, RFC 5321 and 6531 (SMTP), SPEC-002 REQ-014, REQ-018, REQ-041, REQ-061, REQ-062, AC-039, AC-043, AC-044, DEC-016.

### Affected packages

- New: `Encina.Notifications`, `Encina.Notifications.Smtp`, `Encina.Notifications.MailApi`, `Encina.Notifications.Sms`, `Encina.Notifications.WhatsApp`.
- Modified: `Encina.Messaging` (`MessagingConfiguration` flag `UseNotificationDeliveryStore`), `Encina.ADO.{SqlServer,PostgreSQL,MySQL}`, `Encina.Dapper.{SqlServer,PostgreSQL,MySQL}`, `Encina.EntityFrameworkCore`, `Encina.MongoDB`, `tests/Encina.TestInfrastructure`, `src/Encina/Diagnostics/EventIdRanges.cs`.
- Used, not modified: `Encina.Compliance.Consent`, `Encina.Security.Audit`, the outbox and scheduling in `Encina.Messaging`.

### Provider category

**Database (10)** for the delivery-status store. The channel satellites are integrations (HTTP, SMTP), not a provider category of AGENTS.md section 5; each satellite gets the full test matrix, tenancy and telemetry, as DEC-016 requires.

---

## Design Choices

<details>
<summary><strong>1. Package split — a core package plus one satellite per channel</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Encina.Notifications` core plus `.Smtp`, `.MailApi`, `.Sms`, `.WhatsApp` satellites** | Pay for what you use; each channel has its own dependencies (MailKit, HTTP clients) and manifest; matches the issue text | Five packages to version and document |
| **B) One package with all channels** | One dependency | Every application carries MailKit and four vendor clients; coverage and credentials are mixed |
| **C) Abstractions only (DEC-016 option c)** | Smallest | The reference scenario would build every channel itself; rejected by DEC-016 |

### Chosen Option: **A — core plus four satellites**

**Class:** Settled
**Settled by:** SPEC-002 DEC-016 (a) and issue #1235 (Proposed Solution), AGENTS.md section 1 (pay-for-what-you-use)

### Rationale

- The satellite family follows `Encina.Security.Secrets.*`: options, options validator, channel class, `Log.cs`, registration, README, PublicAPI files.
- Each satellite registers an `INotificationChannel` named after its channel (`email`, `sms`, `whatsapp`); the core resolves channels by name, so a second e-mail vendor can be added after 1.0 without changing the abstractions.

</details>

<details>
<summary><strong>2. Consent — a gate abstraction with a purpose-and-channel convention until P-11 exists</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `INotificationConsentGate` in the core; the default implementation calls `IConsentService.HasValidConsentAsync` with the purpose string `"{purpose}:{channel}"`; replaced when P-11 adds channel-scoped consent** | Works today; one seam; no change to Consent | A string convention until P-11 |
| **B) Wait for P-11 (channel-scoped consent)** | Cleaner model | Blocks the feature on an unplanned issue |
| **C) Add channel scoping to `Encina.Compliance.Consent` in this plan** | Cleaner model now | Changes a Marten-based event-sourced package outside this plan's scope; belongs to P-11 |

### Chosen Option: **A — gate abstraction with the purpose-and-channel convention** (recommended, pending the maintainer)

### Rationale

- We recommend A because `HasValidConsentAsync(subject, purpose)` exists (`IConsentService.cs:189`) and the gate is the only place that knows the convention; P-11 replaces one class.
- The gate decides by message class (`NotificationClass.Transactional` needs no marketing consent; `Commercial` needs consent for the purpose and channel, or the existing-client basis of LSSI art. 21.2 for e-mail and equivalent electronic messages). The existing-client basis is an explicit `CommercialBasis.ExistingClient` supplied by the caller and is audited.
- B wins if the maintainer prefers to avoid a temporary string convention.
- Cost of A: a purpose containing `:` would collide with the separator (`"a:b"` plus channel versus `"a"` plus `"b:channel"`). The gate (and the purpose validator) rejects a purpose that contains `:`, which is one more constraint on callers until P-11.

</details>

<details>
<summary><strong>3. Delivery pipeline — deduplicate, gate, defer for quiet hours, then deliver through the outbox</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `SendAsync` inserts a `Queued` delivery row (unique on tenant and dedup key), runs the consent gate, applies quiet hours by scheduling, and writes a `SendNotificationCommand` to the outbox in the same transaction; a handler sends and updates the status** | Survives restarts; retries and dead letter come from the outbox; duplicates are rejected by the database | More moving parts than a direct send |
| **B) Send inline in `SendAsync`** | Simplest | A crash after the provider call loses the status; no retry |
| **C) Poll the delivery table with a hosted service** | No outbox dependency | A second delivery loop beside the outbox; duplicates the retry logic |

### Chosen Option: **A — delivery row plus outbox in one transaction**

**Class:** Settled
**Settled by:** issue #1235 (Motivation: "Delivery through the outbox with status tracking"), AGENTS.md section 3 (Outbox, Transactions), section 6 functions 8 and 9

### Rationale

- The delivery row stores no content and no recipient address, only an opaque recipient reference, the channel, the class, the status (`Queued`, `Sent`, `Delivered`, `Failed`), timestamps and the dedup key (REQ-018). The outbox row holds the payload and is purged by the outbox purge of P-12.
- Quiet hours are evaluated per tenant in the tenant's time zone (`TimeZoneInfo`, `TimeProvider`); a message inside quiet hours is deferred to the end of the window, never dropped. Which path carries the deferred message and how it stays atomic with its `Queued` delivery row is Design Choice 7 (`IScheduledMessageStore` does not use `ITransactionLeaseSource`, so joining the lease is not established).
- Errors follow AGENTS.md section 3: a `Left` from the store, the gate or the channel fails the operation; transient provider failures are retried by the outbox, non-transient ones (`invalid recipient`, rejected template) go to the dead letter store and mark the delivery `Failed`.

</details>

<details>
<summary><strong>4. Vendors of the HTTP mail API and SMS satellites</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One EU-hosted vendor offering both transactional e-mail and SMS (Brevo)** | One Art. 28 processor contract and one transfer analysis; EU data location | Less common than the US leaders; one vendor for two channels is a concentration risk |
| **B) Postmark for e-mail and Twilio for SMS** | Widely used, strong deliverability and documentation | Two processor contracts; US transfers need a documented basis (SPEC-002 H-R33) |
| **C) Amazon SES and Amazon SNS** | One cloud account, regional endpoints in the EU | Cloud-tied; AWS credentials model and LocalStack-only tests; AGENTS.md notes cloud services need emulators |

### Chosen Option: **A — Brevo for the HTTP mail API and SMS satellites** (recommended, pending the maintainer)

### Rationale

- We recommend A because SPEC-002 H-R28 and H-R33 require an Art. 28 contract and a documented transfer basis for each processor; one EU-hosted vendor minimises both. The choice is behind `INotificationChannel`, so another vendor is one class.
- B wins when deliverability tooling or an existing contract matters more than the number of processors. C wins when the application already runs on AWS.
- The issue comment of 2026-10-17 says the HTTP vendors are a plan detail needing no maintainer decision; this plan still lists the choice as a Dilemma because asking is safe and the vendor carries processor-contract consequences. The maintainer may answer "plan detail" and the recommendation stands.
- The claim that one EU-hosted vendor offers transactional e-mail and SMS under one DPA is unverified in this plan; verifying it (DPA, hosting region, API terms) is a gate before coding the satellites.
- The plan fixes constraints, not a market study: the implementer verifies the vendor's current DPA, hosting region and API terms before coding, and the README states them.

</details>

<details>
<summary><strong>5. Delivery-status webhooks — a local signature check per satellite until #731 and #734 exist</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Each satellite exposes a webhook mapper that verifies the provider signature (HMAC) and updates the delivery status through `INotificationDeliveryStore`; switch to the shared inbound validation when #731 and #734 land** | Works now; small per-provider code | Signature code in several places until #731 |
| **B) Wait for #731 and #734** | One mechanism | No plan or package exists; blocks S12 delivery status |
| **C) No webhooks; status is only `Sent`** | Least code | Cannot reach `Delivered`; the issue asks for webhook-driven updates |

### Chosen Option: **A — local signature verification per satellite**

**Class:** Trivial
**Settled by:** issue #1235 (Proposed Solution: "webhook-driven updates (#731, #734 presets)") and the absence of any #731 or #734 package in `src/`; endpoint shape follows `src/Encina.AspNetCore/DPIAEndpointExtensions.cs:69`

### Rationale

- The WhatsApp Cloud API signs its callbacks with `X-Hub-Signature-256`; the mail and SMS vendor callbacks use their documented signature header. The comparison is constant-time and fails closed.
- Webhook handling is idempotent: a status that does not advance (`Delivered` after `Delivered`) is ignored, and an unknown delivery is logged by code only.

</details>

<details>
<summary><strong>6. Credentials and settings per tenant — a provider interface with an options-backed default, failing closed</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `INotificationTenantSettings` (channel credentials, sender identity, time zone, quiet hours) resolved per tenant; options-backed default; fail closed when the tenant is required and unknown** | Meets AC-043; secrets can come from `Encina.Security.Secrets` | One more abstraction |
| **B) Global options only** | Simplest | Violates REQ-061; one practice's credentials serve all |

### Chosen Option: **A — per-tenant settings provider**

**Class:** Settled
**Settled by:** SPEC-002 REQ-061 and AC-043, issue #1235 ("Senders and channel credentials per tenant"), AGENTS.md section 3 (fail closed, secret-bearing options use `[JsonIgnore]` and `ToString()`)

### Rationale

- The default reads `NotificationsOptions.Tenants`; a production application registers a provider backed by `ISecretReader` of `Encina.Security.Secrets`.
- Tenant resolution copies the decision table of `DefaultConsentService` (`RequireTenantContext`, ambient tenant, fixed default tenant when tenancy is off).

</details>

<details>
<summary><strong>7. Quiet hours — which store carries the deferred message and how it stays atomic with its delivery row</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The delivery row is inserted as `Queued` with a `NotBeforeUtc`; the `SendNotificationCommand` is written to `IScheduledMessageStore` for `NotBeforeUtc`; the insert and the scheduling are idempotent by the dedup key, and the handler treats a missing row as a retryable error** | Uses the scheduling store built for delayed domain messages; no outbox change | Two stores are not one transaction (`IScheduledMessageStore` does not use `ITransactionLeaseSource`); atomicity is by idempotent retry |
| **B) Write the command to the outbox now and let the handler re-defer itself until `NotBeforeUtc`** | One store, one lease, atomic with the row | The handler loops until the window opens; extra outbox cycles |
| **C) Add lease support to `IScheduledMessageStore`** | True atomicity | Changes `Encina.Messaging` for 10 providers; out of this plan |

### Chosen Option: **A — scheduling store with idempotent retry** (recommended, pending the maintainer)

### Rationale

- We recommend A: AGENTS.md section 3 names Scheduling for delayed messages; the failure window (row written, scheduling failed) returns a `Left` to the caller, who retries with the same dedup key.
- B wins if the maintainer wants single-transaction atomicity without touching the scheduling store.

</details>

<details>
<summary><strong>8. Writing to the outbox — which public seam <code>Encina.Notifications</code> calls</strong></summary>

### Options Considered

`OutboxWriter` is `internal sealed` (`src/Encina.Messaging/Outbox/OutboxWriter.cs:15`) and cannot be used from `Encina.Notifications`.

| Option | Pros | Cons |
|--------|------|------|
| **A) Public `OutboxOrchestrator.AddAsync<T>` (`src/Encina.Messaging/Outbox/OutboxOrchestrator.cs:87`), returning `Either<EncinaError, Unit>`** | Public today; no change to `Encina.Messaging` | Couples to the orchestrator type |
| **B) `IOutboxStore.AddAsync` with `IOutboxMessageFactory` and `IMessageSerializer`** | Fully public lowest level | Rebuilds the writer |
| **C) Make a writer public in `Encina.Messaging` with PublicAPI lines** | One seam shared with the #1234 plan | Public API growth |

### Chosen Option: **A — `OutboxOrchestrator.AddAsync<T>`** (recommended, pending the maintainer)

### Rationale

- We recommend A: it is public and returns an `Either`, so a `Left` fails the send (AGENTS.md section 3). The #1234 plan has the same choice; both follow the maintainer's answer.

</details>

---

## Implementation Phases

### Phase 1: Core Model and Abstractions (`Encina.Notifications`)

> **Goal**: The vocabulary, contracts and errors of a notification.

<details>
<summary><strong>Tasks</strong></summary>

#### New files in `src/Encina.Notifications/` (namespace `Encina.Notifications`)

1. `Encina.Notifications.csproj`, `PublicAPI.*.txt`, `README.md` (layout of `src/Encina.Security.Secrets/`).
2. `Model/NotificationRequest.cs` — `sealed record NotificationRequest(string Channel, string RecipientRef, string TemplateId, NotificationClass Class, string Purpose, string DedupKey, IReadOnlyDictionary<string,string> Parameters, CommercialBasis? Basis)`.
3. `Model/NotificationClass.cs` (`Transactional = 1`, `Commercial = 2`), `Model/CommercialBasis.cs` (`Consent = 1`, `ExistingClient = 2`), `Model/DeliveryStatus.cs` (`Queued = 1`, `Sent = 2`, `Delivered = 3`, `Failed = 4`).
4. `Model/NotificationDelivery.cs` — id, tenant, channel, class, status, dedup key, opaque recipient reference, `CreatedAtUtc`, `LastUpdatedAtUtc`, `FailureCode` (code only).
5. `Abstractions/INotificationSender.cs` — `SendAsync(NotificationRequest, ct)` returning `Either<EncinaError, NotificationDelivery>`.
6. `Abstractions/INotificationChannel.cs` — `Name`, `SendAsync(ChannelMessage, tenantSettings, ct)` returning `Either<EncinaError, ChannelReceipt>`.
7. `Abstractions/INotificationDeliveryStore.cs` — `TryCreateAsync` (unique tenant and dedup key; returns the existing row on a duplicate), `UpdateStatusAsync`, `GetAsync`.
8. `Abstractions/INotificationConsentGate.cs`, `INotificationTemplateRenderer.cs`, `INotificationTenantSettings.cs`, `IOptOutLinkProvider.cs`.
9. `NotificationErrors.cs` — codes `notifications.consent_missing`, `notifications.duplicate`, `notifications.channel_unknown`, `notifications.template_unknown`, `notifications.tenant_required`, `notifications.delivery_failed`, `notifications.quiet_hours_unresolved`.
10. `Commands/SendNotificationCommand.cs` — the outbox payload (an `INotification`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of issue #1235 (Encina.Notifications).

CONTEXT:
- Encina is a .NET 10 / C# 14 library, pre-1.0, Railway Oriented Programming (Either<EncinaError, T>).
- New package src/Encina.Notifications; copy the layout of src/Encina.Security.Secrets.
- Plan: docs/plans/notifications-implementation-plan-1235.md, Design Choices 1-3 and 6.

TASK:
Create the project, the model, the abstractions, NotificationErrors and SendNotificationCommand exactly as listed in
the Phase 1 Tasks. Add every public symbol to PublicAPI.Unshipped.txt.

KEY RULES:
- No recipient address, message body or template parameter value is stored in NotificationDelivery or in error metadata.
- Every method returns Either and takes a CancellationToken; no exceptions for business outcomes.
- Options with secrets use [JsonIgnore] and override ToString().
- XML docs on all public members; no [Obsolete].

REFERENCE FILES:
- src/Encina.Security.Secrets/ (layout)
- src/Encina.Messaging/Outbox/IHasNotifications.cs
- src/Encina.Compliance.Consent/Abstractions/IConsentService.cs
```

</details>

---

### Phase 2: Sender, Gate, Templates, Quiet Hours and In-Memory Store

> **Goal**: The delivery pipeline of Design Choice 3 with in-memory defaults.

<details>
<summary><strong>Tasks</strong></summary>

1. `Services/DefaultNotificationSender.cs` — implements `INotificationSender` (constructor: `INotificationDeliveryStore`, `INotificationConsentGate`, `INotificationTenantSettings`, `QuietHoursEvaluator`, `OutboxOrchestrator` (Design Choice 8), `IScheduledMessageStore` (Design Choice 7), `ITransactionLeaseSource`, `TimeProvider`, `IOptions<NotificationsOptions>`): dedup, gate, quiet hours, outbox, all in one lease.
2. `Services/DefaultNotificationConsentGate.cs` — Design Choice 2; transactional bypasses marketing consent; commercial requires consent for `"{purpose}:{channel}"` or `CommercialBasis.ExistingClient`.
3. `Services/QuietHoursEvaluator.cs` — pure: `(DateTimeOffset nowUtc, TimeZoneInfo zone, QuietHours window)` returns `DateTimeOffset? deferUntilUtc`; handles windows crossing midnight and DST.
4. `Services/SimpleTemplateRenderer.cs` — `{{name}}` substitution from a registered template set; appends the opt-out text and link to commercial messages (LSSI art. 21.2); unknown template returns `notifications.template_unknown`.
5. `Handlers/SendNotificationHandler.cs` — the outbox consumer: resolves the channel by name, sends, updates the status; classifies failures with `IErrorClassifier`.
6. `Webhooks/NotificationStatusUpdater.cs` — forward-only status transitions used by the satellite webhooks.
7. `Stores/InMemoryNotificationDeliveryStore.cs`, `Tenancy/NotificationTenantResolver.cs`, `NotificationsOptions.cs` and `NotificationsOptionsValidator.cs`.
8. `ServiceCollectionExtensions.cs` — `AddEncinaNotifications(Action<NotificationsOptions>)` with `TryAdd*`; `src/Encina.Messaging/MessagingConfiguration.cs` flag `UseNotificationDeliveryStore`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of issue #1235.

CONTEXT:
- Phase 1 created the abstractions. Plan: docs/plans/notifications-implementation-plan-1235.md, Design Choices 2, 3, 6.

TASK:
Implement the sender, consent gate, quiet-hours evaluator, renderer, outbox handler, status updater, in-memory store,
tenant resolver, options and AddEncinaNotifications as listed in the Phase 2 Tasks.

KEY RULES:
- Time comes from TimeProvider; the tenant zone comes from INotificationTenantSettings.
- The delivery row and the outbox row are written in one lease; a Left rolls both back.
- A commercial message without consent or the ExistingClient basis is refused and audited; fail closed.
- Registration completeness proven by a DI test with ValidateOnBuild and ValidateScopes.
- One private method per step so every changed method has CRAP <= 10.

REFERENCE FILES:
- src/Encina.Messaging/Outbox/OutboxOrchestrator.cs
- src/Encina.Compliance.Consent/Services/DefaultConsentService.cs
- src/Encina/Recoverability/ (IErrorClassifier)
```

</details>

---

### Phase 3: Persistence — Entity, Mapper and Scripts

> **Goal**: The shared shape of the delivery row.

<details>
<summary><strong>Tasks</strong></summary>

1. `Persistence/NotificationDeliveryEntity.cs` and `NotificationDeliveryMapper.cs`; unique index `(TenantId, DedupKey)`; index `(TenantId, Status, LastUpdatedAtUtc)`.
2. Scripts `NNN_CreateNotificationDeliveriesTable.sql` for the three ADO providers and the Dapper script folders (the number is the next free one at implementation time; plan #1234 uses 031) and the aggregate `000_CreateAllTables.sql`.
3. `tests/Encina.TestInfrastructure/Schemas/{SqlServerSchema,PostgreSqlSchema,MySqlSchema}.cs` and `EFCoreSchema.cs`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of issue #1235.

CONTEXT:
- Plan: docs/plans/notifications-implementation-plan-1235.md.

TASK:
Create the entity, mapper, SQL scripts and test schemas for the delivery table.

KEY RULES:
- No content and no recipient address columns; only an opaque recipient reference.
- Unique (TenantId, DedupKey) is the deduplication arbiter on every provider.
- UTC timestamps with the AtUtc suffix; SQL differences per AGENTS.md section 5.

REFERENCE FILES:
- src/Encina.ADO.SqlServer/Scripts/030_CreateBreakTheGlassElevationsTable.sql
- src/Encina.EntityFrameworkCore/BreakTheGlass/BreakTheGlassElevationEntity.cs
- tests/Encina.TestInfrastructure/Schemas/SqlServerSchema.cs
```

</details>

---

### Phase 4: Multi-Provider Delivery Stores — All 10 Database Providers

> **Goal**: `INotificationDeliveryStore` on every provider, joining the ambient transaction.

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.ADO.{SqlServer,PostgreSQL,MySQL}/Notifications/NotificationDeliveryStoreADO.cs` (3).
2. `src/Encina.Dapper.{SqlServer,PostgreSQL,MySQL}/Notifications/NotificationDeliveryStoreDapper.cs` (3).
3. `src/Encina.EntityFrameworkCore/Notifications/` — entity configuration, `ApplyNotificationsConfiguration`, `NotificationDeliveryStoreEF.cs`.
4. `src/Encina.MongoDB/Notifications/` — document, store, unique index in `MongoDbIndexCreator.cs`, option and collection name.
5. Registration in each provider's `ServiceCollectionExtensions.cs` with `RemoveInMemoryDefault` then `TryAddScoped`, gated by `UseNotificationDeliveryStore`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of issue #1235.

CONTEXT:
- Plan: docs/plans/notifications-implementation-plan-1235.md. Phases 1-3 exist. All 10 database providers apply.

TASK:
Implement the delivery store and its registration on ADO x3, Dapper x3, EF Core and MongoDB.

KEY RULES:
- TryCreateAsync maps the unique-key violation to the existing row (no exception to the caller).
- Async database calls with CancellationToken; join the ambient transaction (IDbTransactionAccessor, DbContext, IMongoSessionAccessor).
- A database store wins over the in-memory default in any registration order.
- Every query filters by TenantId.

REFERENCE FILES:
- src/Encina.Messaging/Outbox/ (store files per provider)
- src/Encina.ADO.SqlServer/ServiceCollectionExtensions.cs:113-118
- src/Encina.MongoDB/MongoDbIndexCreator.cs
```

</details>

---

### Phase 5: Channel Satellites (SMTP, HTTP Mail API, SMS, WhatsApp Cloud API)

> **Goal**: Four `INotificationChannel` implementations with webhooks for delivery status.

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.Notifications.Smtp/` — `SmtpNotificationChannel` (MailKit `SmtpClient`, async, TLS required by default), `SmtpChannelOptions` (password `[JsonIgnore]`, `ToString()`), validator, `Log.cs`, `AddEncinaNotificationsSmtp`.
2. `src/Encina.Notifications.MailApi/` — vendor channel (Design Choice 4) over `IHttpClientFactory`, webhook mapper with signature check (Design Choice 5).
3. `src/Encina.Notifications.Sms/` — same shape for SMS.
4. `src/Encina.Notifications.WhatsApp/` — WhatsApp Cloud API (template messages, `X-Hub-Signature-256` webhook, verification handshake).
5. Webhook route mapping lives in each satellite as a `MapXxxWebhook(this IEndpointRouteBuilder, string policyOrSignatureScheme)` extension (shape of `src/Encina.AspNetCore/DPIAEndpointExtensions.cs`); the satellite references ASP.NET Core through `<FrameworkReference Include="Microsoft.AspNetCore.App" />` and not `Encina.AspNetCore`, so the core stays free of it. SMTP has no webhook.
6. Each satellite: `PublicAPI.*.txt`, `README.md` (processor contract, hosting region, transfer basis), `Directory.Packages.props` entry for any new package (MailKit).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are implementing Phase 5 of issue #1235, one satellite per pull request.

CONTEXT:
- Plan: docs/plans/notifications-implementation-plan-1235.md, Design Choices 1, 4, 5, 6.

TASK:
Implement the satellite you were assigned (Smtp, MailApi, Sms or WhatsApp) exactly as in the Phase 5 Tasks.

KEY RULES:
- Credentials per tenant come from INotificationTenantSettings; secrets are [JsonIgnore] and never in ToString().
- Never log a recipient address, body, template parameter or provider response body; log the error code only.
- HTTP satellites retry only transient errors (honour Retry-After); 4xx rejections are non-transient.
- Webhook signature comparison is constant-time and fails closed; statuses only move forward.
- Zero new package without a Directory.Packages.props entry and a license check.

REFERENCE FILES:
- src/Encina.Security.Secrets.AzureKeyVault/ (satellite layout)
- src/Encina.Testing.WireMock/
- src/Encina/Recoverability/RetryAfterParser.cs
```

</details>

---

### Phase 6: Cross-Cutting Integration

> **Goal**: Health, audit, validation and resilience.

<details>
<summary><strong>Tasks</strong></summary>

1. `Health/NotificationChannelHealthCheck.cs` per satellite (SMTP connect and authenticate with a short timeout; HTTP credentials check without sending); `DefaultName`, static `Tags`.
2. `Services/NotificationAuditWriter.cs` — consent refusals, commercial sends and existing-client bases audited by code and tenant, never by address.
3. Options validators of the core and each satellite fail start-up on missing endpoints.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
You are implementing Phase 6 of issue #1235.

CONTEXT:
- Plan: docs/plans/notifications-implementation-plan-1235.md, cross-cutting matrix.

TASK:
Add the channel health checks and the audit writer; complete the options validators.

KEY RULES:
- Health data carries codes and counts only; scoped resolution through CreateScope().
- Audit events carry no recipient address or content.

REFERENCE FILES:
- src/Encina.Security.BreakTheGlass/Health/BreakTheGlassHealthCheck.cs
- src/Encina.Security.Audit/
```

</details>

---

### Phase 7: Observability

> **Goal**: Activity, metrics and logging with the tenant as an attribute (REQ-062).

<details>
<summary><strong>Tasks</strong></summary>

1. Register in `src/Encina/Diagnostics/EventIdRanges.cs`: `Notifications = (5550, 5599)`, `NotificationsSmtp = (5600, 5624)`, `NotificationsMailApi = (5625, 5649)`, `NotificationsSms = (5650, 5674)`, `NotificationsWhatsApp = (5675, 5699)`; add the five assemblies to `EncinaEventIdAllocationTests.cs`.
2. `Diagnostics/NotificationsDiagnostics.cs` — `ActivitySource` and `Meter` `Encina.Notifications`; counters `encina.notifications.sent`, `encina.notifications.consent_refused`, `encina.notifications.delivery_failures`; span `encina.notifications.send` with `encina.tenant_id`, `encina.notifications.channel`, `encina.message.class`.
3. `Diagnostics/NotificationsLogMessages.cs` and one `Log.cs` per satellite, `[LoggerMessage]`, packed sequentially.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
You are implementing Phase 7 of issue #1235.

CONTEXT:
- Plan: docs/plans/notifications-implementation-plan-1235.md; EventId ranges 5550-5699.

TASK:
Register the five ranges, map the five assemblies in EncinaEventIdAllocationTests and add the diagnostics.

KEY RULES:
- Register each range in EventIdRanges.cs before using an EventId; pack sequentially; XML doc names the range.
- No recipient address, body, template parameter or provider response in tags, metrics or messages.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
- src/Encina.Compliance.Consent/Diagnostics/
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

---

### Phase 8: Testing

> **Goal**: Every flag reaches its manifest target for the core and each satellite.

<details>
<summary><strong>Tasks</strong></summary>

1. Unit: sender flow (duplicate, refused, deferred, queued), quiet hours (midnight window, DST), renderer opt-out, status transitions, webhook signature, tenant table, DI test with `ValidateOnBuild` and `ValidateScopes`.
2. Guard and Contract (delivery store contract on the 10 providers; channel contract on the four satellites).
3. Property: no commercial message is queued without consent or the existing-client basis; a status never moves backwards.
4. Integration: delivery store on the 10 providers in shared `[Collection]` fixtures; WireMock per HTTP channel; SMTP through a Testcontainers mail catcher (new fixture in `Encina.Testing.Testcontainers`); two-tenant test; in-memory exporter test; no address or content in telemetry.
5. Load and benchmark `.md` justifications.
6. Manifest files for the five new packages and the provider manifest entries.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```
You are implementing Phase 8 of issue #1235.

CONTEXT:
- Plan: docs/plans/notifications-implementation-plan-1235.md; manifests in .github/coverage-manifest/.

TASK:
Write the tests listed in the Phase 8 Tasks and the manifest entries.

KEY RULES:
- Tests execute real package code; Shouldly through Encina.Testing.Shouldly; FakeTimeProvider, no Thread.Sleep.
- Shared [Collection] fixtures; never IClassFixture for databases.
- Each flag reaches its own target.

REFERENCE FILES:
- tests/Encina.IntegrationTests/Security/BreakTheGlass/
- tests/Encina.ContractTests/Messaging/DeadLetter/DeadLetterStoreContract.cs
- src/Encina.Testing.WireMock/
```

</details>

---

### Phase 9: Documentation and Finalization

> **Goal**: Documentation, changelog, public API and build verification.

<details>
<summary><strong>Tasks</strong></summary>

1. XML docs on every public member.
2. `changelog.d/1235-notifications.added.md`.
3. Five package READMEs (each satellite states processor contract, hosting and transfer basis); `docs/features/notifications.md`; `docs/INVENTORY.md`.
4. ADR numbered at implementation time for Design Choices 2 and 3.
5. `PublicAPI.Unshipped.txt` for the five packages; build with zero warnings; per-flag coverage and CRAP table.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 9</strong></summary>

```
You are implementing Phase 9 of issue #1235.

CONTEXT:
- Plan: docs/plans/notifications-implementation-plan-1235.md.

TASK:
Write the documentation, changelog fragment, ADR and PublicAPI lines listed in the Phase 9 Tasks.

KEY RULES:
- Never type coverage figures by hand; cite with covref markers.
- The PR records the ADR-018 evaluation of the 12 functions.
- English only; write "the reference application", never a client name.

REFERENCE FILES:
- .claude/skills/encina-docs/SKILL.md
- changelog.d/README.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Relevance to this plan |
|--------|-----------|------------------------|
| LSSI-CE (Ley 34/2002) | Art. 21.1, 21.2 | Commercial communications need consent; the existing-client exception and the opt-out mechanism |
| ePrivacy Directive 2002/58/EC | Art. 13 | Unsolicited communications by electronic means |
| GDPR | Art. 6(1)(a), 7, 21(2)-(3) | Consent and the right to object to direct marketing |
| WhatsApp Business Cloud API | Messages, templates, webhooks | The WhatsApp satellite and its signature check |
| RFC 5321, 6531 | SMTP, internationalised mail | The SMTP satellite |
| SPEC-002 | REQ-014, REQ-018, REQ-041, REQ-061, REQ-062, DEC-016, H-R28 to H-R33, S12 | Requirement, retention, tenancy, telemetry, satellite set, processor contracts |
| ADR-018, ADR-021 | Cross-cutting, EventIds | Matrix and ranges 5550-5699 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Use in this feature |
|-----------|----------|---------------------|
| `OutboxOrchestrator`, `IOutboxStore` | `src/Encina.Messaging/Outbox/` | Reliable delivery and retries |
| `IDeadLetterStore`, `IErrorClassifier`, `RetryAfterParser` | `src/Encina.Messaging/DeadLetter/`, `src/Encina/Recoverability/` | Non-transient failures, HTTP retry classification |
| `IConsentService` | `src/Encina.Compliance.Consent/Abstractions/IConsentService.cs:189` | Default consent gate |
| `DefaultConsentService` tenant decision table | `src/Encina.Compliance.Consent/Services/DefaultConsentService.cs:121-128` | `NotificationTenantResolver` |
| `ITransactionLeaseSource` family | `src/Encina.Messaging/Transactions/` | Delivery row and outbox row atomicity |
| `Encina.Security.Secrets` | `src/Encina.Security.Secrets/` | Source of channel credentials per tenant |
| `Encina.Testing.WireMock` | `src/Encina.Testing.WireMock/` | HTTP channel tests |
| Satellite layout | `src/Encina.Security.Secrets.AzureKeyVault/` | Options, validator, `Log.cs`, registration |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Notifications` | 5550-5599 (new `Notifications`) | Owned by this plan; free block per the registry |
| `Encina.Notifications.Smtp` | 5600-5624 (new) | |
| `Encina.Notifications.MailApi` | 5625-5649 (new) | |
| `Encina.Notifications.Sms` | 5650-5674 (new) | |
| `Encina.Notifications.WhatsApp` | 5675-5699 (new) | |
| Neighbours in the family | 5500-5549 (#1234), 5700-5749 (#1236), 5750-5799 (#1238) | Reserved by the sibling plans |

### Estimated File Count

| Category | New | Modified | Notes |
|----------|----:|---------:|-------|
| Core package (model, abstractions, services, store, DI) | 30 | 1 | Phases 1-2 |
| Persistence and scripts | 10 | 4 | Phase 3 |
| Provider stores and registrations | 17 | 9 | Phase 4 |
| Four satellites | 36 | 1 | Phase 5 (about 9 files each) |
| Health, audit | 6 | 0 | Phase 6 |
| Diagnostics | 8 | 2 | Phase 7 |
| Tests | ~80 | ~4 | Phase 8, two `.md` justifications |
| Documentation | 8 | 3 | Phase 9 |

### Findings in the Current Code

- Channel-scoped consent (P-11) does not exist: the gate uses a purpose convention (Design Choice 2).
- `#731` and `#734` have neither a package nor a plan, so webhook signature checks are local (Design Choice 5).
- There is no tenant time zone concept; the settings provider introduces it (Design Choice 6).
- There is no SMTP test server in the repository; Phase 8 adds a Testcontainers fixture.

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT:
Encina is a pre-1.0 .NET 10 / C# 14 library (nullable on, Railway Oriented Programming with Either<EncinaError, T>).
Issue #1235 (SPEC-002 P-33, REQ-041, AC-039, AC-043, S12, DEC-016) adds Encina.Notifications: consent-aware
multi-channel delivery with templates, quiet hours, deduplication and delivery status through the outbox, a delivery
store on the 10 database providers, and four channel satellites (SMTP, HTTP mail API, SMS, WhatsApp Cloud API).
Plan: docs/plans/notifications-implementation-plan-1235.md. Rules: AGENTS.md.

IMPLEMENTATION OVERVIEW:
1. Core model, abstractions, errors, SendNotificationCommand.
2. Sender (dedup, consent gate, quiet hours, outbox in one lease), renderer with opt-out, handler, in-memory store,
   tenant resolver, options, DI.
3. Entity, mapper, scripts, test schemas; unique (TenantId, DedupKey).
4. Delivery stores on ADO x3, Dapper x3, EF Core, MongoDB.
5. Four satellites with webhooks for delivery status.
6. Channel health checks, audit writer, validators.
7. EventIds 5550-5699, diagnostics, [LoggerMessage].
8. Tests on every flag, SMTP and WireMock fixtures, two-tenant and no-PII-in-telemetry tests.
9. Docs, ADR, changelog, PublicAPI, build, coverage, CRAP.

KEY PATTERNS:
- Everything returns Either; a Left from a store, gate or channel fails the operation (retry or dead letter).
- No recipient address, content or provider response in logs, tags, metrics, audit or the delivery row.
- Secrets use [JsonIgnore] and ToString(); credentials per tenant; fail closed on a missing tenant.
- TimeProvider for time; async database calls with CancellationToken; database store wins over in-memory.
- Integration tests on shared [Collection] fixtures; every flag reaches its target; CRAP <= 10.

REFERENCE FILES:
- src/Encina.Messaging/Outbox/OutboxOrchestrator.cs
- src/Encina.Compliance.Consent/Abstractions/IConsentService.cs
- src/Encina.Security.Secrets.AzureKeyVault/ (satellite layout)
- src/Encina/Recoverability/ (error classification, Retry-After)
- src/Encina/Diagnostics/EventIdRanges.cs
- docs/specifications/SPEC-002-eu-regulatory-readiness.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | No read path that benefits from caching; templates are held in memory by the renderer |
| 2 | OpenTelemetry | ✅ | `encina.notifications.send` span and `sent` / `consent_refused` / `delivery_failures` counters with `encina.tenant_id` (Phase 7) |
| 3 | Structured Logging | ✅ | `[LoggerMessage]` in ranges 5550-5699, one range per package (Phase 7) |
| 4 | Health Checks | ✅ | One check per channel satellite: SMTP reachability, API credentials (Phase 6) |
| 5 | Validation | ✅ | Options validators at start-up; template and recipient-reference validation in the sender (Phases 2 and 6) |
| 6 | Resilience | ✅ | Outbox retries transient failures; HTTP satellites honour `Retry-After`; non-transient failures go to the dead letter store (Phases 2 and 5) |
| 7 | Distributed Locks | ❌ | No shared state updated by concurrent hosts; the unique dedup key is the arbiter and the outbox claims rows |
| 8 | Transactions | ✅ | Delivery row and outbox row in one lease (Phase 2, Design Choice 3) |
| 9 | Idempotency | ✅ | Deduplication key unique per tenant (Design Choice 3); webhook status updates are forward-only |
| 10 | Multi-Tenancy | ✅ | `TenantId` on every row, per-tenant credentials, sender identity, time zone and quiet hours, fail closed (Design Choice 6, AC-043) |
| 11 | Module Isolation | ❌ | SPEC-002 requires no module scoping; `ModuleId` in messaging stays with #747 |
| 12 | Audit Trail | ✅ | Consent refusals, commercial sends and existing-client bases audited by code and tenant (Phase 6) |

---

## Prerequisites & Dependencies

| Item | State | Relation |
|------|-------|----------|
| #1186 EU regulatory readiness (SPEC-002) | open (EPIC) | Parent |
| P-11 Channel-scoped consent | open | Not blocking; replaces the default gate when it lands (Design Choice 2) |
| P-12 Outbox purge | open | Advisable: purges the payload held in the outbox row; without it the retention of REQ-018 is met only by the outbox's own cleanup |
| #731, #734 Inbound webhook validation and idempotency | open | Not blocking; satellites verify signatures locally (Design Choice 5) |
| P-21 Recoverability / dead letter | present in `src/Encina.Messaging` | Used as is |
| Plan #1234 (OAuth) | this family | Shares SQL script numbering and the `MessagingConfiguration` flag pattern; whichever merges second takes the next free script number |
| Vendor contracts | maintainer | An Art. 28 processor agreement and a documented transfer basis before the vendor satellites ship (H-R28, H-R33) |
| Issue file `plan-1235-consent-channel-convention.md` | to open | Replace the `"{purpose}:{channel}"` convention when P-11 lands |

---

## Next Steps

1. The maintainer decides Design Choices 2, 4, 7 and 8 (the Dilemmas); the orchestrator records the answers in a `## Maintainer Decisions` section and updates each Chosen Option.
2. The feature exceeds one worker pull request (15 files, 1500 lines). The core alone is about 30 files plus tests, so the split is eleven pull requests, each at most 15 files and 1500 changed lines (sizes are estimates, re-measured when each sub-issue is written): (a1) Phase 1 model and abstractions, about 12 files with tests; (a2) Phase 2 services, templates, quiet hours and in-memory store, about 14 files; (a3) diagnostics, health check, audit and EventId registration, about 8 files; (b) Phase 3 and the ADO providers; (c) the Dapper providers; (d1) EF Core and the schemas; (d2) MongoDB; (e) the SMTP satellite, about 9 production files; (f) the HTTP mail API satellite, about 9 production files; (g) the SMS satellite; (h) the WhatsApp satellite. A satellite whose production files, tests, README and `Directory.Packages.props` entry exceed 15 splits tests into a second PR. Each PR carries its own manifest entries.
3. The orchestrator opens the issue file `artifacts/issues/plan-1235-consent-channel-convention.md` and links it from #1235.
4. Link this plan from #1235 and start sub-issue (a) with an `issue-worker`.
