# Implementation Plan: ProcessorAgreements Handles the DSR Erasure Notification and Notifies Each Processor (GDPR Art. 19)

> **Issue**: [#3975](https://github.com/dlrivada/Encina/issues/3975)
> **Type**: Feature
> **Related**: #2407 (DSR erasure completeness: owns `ErasureNotification` and writes it to the outbox; its Design Choices 9 and 10 are pending Dilemmas this plan follows), #1198 (processor roles), #2396 (processor liveness), #1251 (outbox row claiming), #782 (ProcessorAgreements on event sourcing), #733 (notification idempotency), #2676 (tenant tag helper), #2381 and #2382 (module requirements), ADR-050 (ProcessorAgreements subscribes to DSR erasures), ADR-054, ADR-062 (merged: error category contract)
> **Complexity**: Medium-High (one handler, one new port, one small event-sourced ledger, one new project reference)
> **Estimated Scope**: ~2,600 lines in total (~520 production, ~1,700 tests and manifests, ~380 documentation), in six pull requests of at most 13 files and 900 lines each (counts in "Estimated File Count"; every pull request is at most 15 files)

---

## Summary

### What exists today (read on 2026-11-24, main `278c164f1`)

- `ErasureNotification` does not exist in `src`: the plan of #2407 creates it in `Encina.Compliance.DataSubjectRights/Notifications/` and writes it to the outbox from `DefaultDSRService` after an erasure (its Design Choices 6 to 10). Today the DSR service publishes the best-effort `DataErasedNotification(SubjectId, AffectedFields, DSRRequestId, OccurredAtUtc)`; #2407 replaces it. Nothing consumes either one.
- #2407's Design Choice 9 (pending, recommended option A) puts **no subject id** in `ErasureNotification` (fields: `DSRRequestId`, `TenantId`, `FieldsErasedCount`, `FieldsRetainedCount`, `OccurredAtUtc`) because the outbox has no retention. The text of this issue lists a subject id in the notification; the two disagree, which is Design Choice 2 below.
- `Encina.Compliance.ProcessorAgreements` is event sourced: `ProcessorAggregate` (stream per processor) with the read model `ProcessorReadModel(Id: Guid, Name, Country, ContactEmail, ParentProcessorId, Depth, IsRemoved, TenantId, ModuleId, ...)`; `IProcessorService` is scoped and has no tenant-explicit read: `GetAllProcessorsAsync(ct)` reads the **ambient** tenant through `ProcessorAgreementTenantScope.TryResolve` (`DefaultProcessorService.cs`) and returns every non-removed processor of that tenant, sub-processors included. `TryResolveWrite(operation, explicitTenantId, ...)` already accepts an explicit tenant when it does not contradict the ambient one.
- The package has no reference to `Encina.Compliance.DataSubjectRights` or `Encina.Security.Audit` (`Encina.Compliance.ProcessorAgreements.csproj`); both are referenced by every test project. It references `Encina.Messaging`. `AddEncinaProcessorAgreements` registers the services with `TryAdd*`; the application registers the aggregates separately with `AddProcessorAgreementAggregates` (`ProcessorAgreementsMartenExtensions.cs`).
- Notification handlers are `INotificationHandler<T>` (`Task<Either<EncinaError, Unit>> Handle(T, CancellationToken)`); `NotificationDispatcher` returns `Left` when any handler fails, and `OutboxProcessorBase` publishes the stored notification with `encina.Publish` without restoring a request context, so an outbox-driven handler has no ambient tenant (`OutboxProcessorBase.cs`, `PublishAsync`).
- ADR-050 lists for ProcessorAgreements "subscribes to DSR erasures (Art. 19, 28)" and for DSR "reliable processor notification of the erasure (Art. 19, planned (#2407))".
- Marten saves aggregates with optimistic concurrency: `IAggregateRepository<T>.SaveAsync` returns a `Left` on a concurrent write (`MartenAggregateRepository.cs`, the stream-modified-by-another-session case).
- `ProcessorAgreementLogMessages` uses ids up to 9487 of `ComplianceProcessorAgreements = (9400, 9499)`, and the plan of #1198 claims 9486-9493 against a stale count; the ids left in that range are too few for this feature, so it takes a range of its own.
- Not in `src`: any outbound channel to a processor, any record of which processor was told about which erasure.

### What this plan delivers

When the DSR erasure notification arrives, each active processor of the tenant is told, once per DSR request, and the outcome is recorded:

1. **`ProcessorErasureNotificationHandler : INotificationHandler<ErasureNotification>`** in ProcessorAgreements: reads the tenant's processors (a new tenant-explicit read, `IProcessorService.GetProcessorsForTenantAsync`), skips those the ledger already marks notified, claims each remaining one in the ledger, calls the port, records the result and writes one `IOperationAuditStore` entry per processor; any failure returns a `Left`, so the outbox retries the notification and, after its attempts, dead-letters it, while the ledger keeps the failed state.
2. **`IProcessorErasureNotifier`**, the outbound port (`NotifyAsync(ProcessorErasureNotice, ct)`), with `FailClosedProcessorErasureNotifier` as the default: it returns `processor_agreements.erasure_notifier_not_configured` until the application registers a real notifier (email, webhook, ...).
3. **`ErasureNotificationLedger`**, a small event-sourced aggregate with one stream per (tenant, DSR request) recording each processor's attempt, success and failure; it is what makes "once per DSR request id" true and what shows "recorded as failed".
4. **Diagnostics**: a new EventId range, two counters and one activity per delivery, the tenant tag only when multi-tenancy is on.

Nothing changes for an application that never receives an `ErasureNotification`.

- Affected packages: `Encina.Compliance.ProcessorAgreements` (production), `Encina` (one EventId range registration). No database-provider code: the ledger rides on Marten like the rest of the package.
- Provider category: none of the 10 database providers (event-sourced compliance modules use Marten on PostgreSQL, `AGENTS.md` section 3); the outbound channel is the application's notifier. Event-sourced modules have no InMemory store: unit tests mock `IAggregateRepository` and integration tests run Marten through Testcontainers.
- Shared pieces: consumes `ErasureNotification` (owned by #2407); owns `IProcessorErasureNotifier`, `ProcessorErasureNotice`, the ledger and the codes `processor_agreements.erasure_*`.

---

## Design Choices

<details>
<summary><strong>1. Who is notified</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Every active (not removed) processor of the tenant, at every depth, whether or not its DPA is current** | Matches the issue's acceptance criterion ("each active processor of the tenant"); safe against under-notification; sub-processors received data too | Over-notifies a processor that never held this subject's data (it learns that an erasure happened, and the counts) |
| **B) Top-level processors only; each passes the erasure on to its sub-processors under its DPA** | Fewer messages; follows the Art. 28(4) chain of obligations | The controller cannot show the sub-processors were told; a broken chain is invisible to the controller |
| **C) Record per-subject disclosures first and notify only the processors that held the subject's data** | Precise, no over-notification | Needs a disclosure data model, and every write path that discloses data must record it, which is far outside this issue |

### Chosen Option: **A - every active processor of the tenant** (recommended, pending the maintainer)

### Rationale

- Art. 19 asks the controller to tell each recipient the data were disclosed to; Encina has no per-subject disclosure record (`Processor` carries `ContactEmail` and `TenantId`, nothing per subject), so the only safe set is the tenant's. B wins when the maintainer treats the sub-processor chain as the processor's own duty; C wins when over-notification is unacceptable and the disclosure model exists (a follow-on listed in Next Steps).
- A processor without a `ContactEmail` is not skipped: the port decides the channel, and the notice carries a null address; an application whose notifier needs an address returns a `Left` for it, which is recorded as failed and never dropped.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. What the processor learns about the subject</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The notice carries the DSR request id, tenant, counts and time, and no subject id; the application's notifier resolves the subject through an authorized lookup of the DSR request when its channel needs one** | No personal data in the outbox table or in the ledger (the outbox has no retention today); consistent with #2407's recommended option | The notifier cannot act on the message alone; an external processor with no access to the DSR aggregate gets only the request id |
| **B) The notice also carries the subject id, taken from `ErasureNotification` when #2407 puts it there** | Self-contained for the processor | Every erasure leaves the subject id in the outbox table until a retention job exists, and it travels to every processor |

### Chosen Option: **A - request id, no subject id** (recommended, pending the maintainer)

### Rationale

- This choice mirrors #2407's Design Choice 9 and must stay equal to its answer: if the maintainer picks B there, `ProcessorErasureNotice.SubjectId` is filled from the notification; under A it is always null and the field is documented as such. B wins when the processors cannot call back into the controller.
- Whatever the option, the subject id never enters a log, a counter tag, a ledger event or an audit entry.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. How "once per DSR request" is guaranteed</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A per-DSR-request ledger aggregate (Marten stream) with claim-then-send: append an attempt event with the stream's expected version, call the notifier, append the result; a concurrent handler loses the claim and the outbox retries it** | Works today with the package's own storage; records attempts and failures, which the acceptance criteria need; survives restarts; no lock | A new aggregate and its events; a crash after the claim and before the call leads to a second call on retry (at-least-once, documented, the notice carries an idempotency key) |
| **B) The notification inbox of #733: an idempotency key per (request, processor) on a fan-out of one outbox message per processor** | Reuses the messaging patterns | #733 is a plan, not code; the inbox records processing, not a failed state per processor; the fan-out needs deterministic message ids |

### Chosen Option: **A - the ledger aggregate** (recommended, pending the maintainer)

### Rationale

- The acceptance criteria need a durable "failed" state per processor, which only a purpose-built record gives. B wins when #733 has shipped and the maintainer prefers no new aggregate.
- Exactly-once delivery to an external channel is impossible; the plan promises one *recorded* notification per processor and request, and passes `IdempotencyKey` (an opaque SHA-256 of the canonical key and the processor id) so the channel can deduplicate without parsing a composite string. The notice itself carries `TenantId` and `DsrRequestId` in the clear to the notifier, because the channel needs them; the opaque key does not hide them from the notifier, it only keeps them out of logs, counters and ledger events (register row C10).

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Where the consumed contract comes from</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Encina.Compliance.ProcessorAgreements` references `Encina.Compliance.DataSubjectRights` and handles `ErasureNotification` where #2407 defines it** | Follows ADR-050 ("ProcessorAgreements subscribes to DSR erasures") and #2407's placement; no change to #2407 | Every ProcessorAgreements user carries the DSR assembly, which it may not use; no cycle (DSR references neither ProcessorAgreements nor Security.Audit) |
| **B) The contract record lives in `Encina.Compliance.GDPR`, which both packages already reference** | No new coupling | #2407's file placement changes; `GDPR` has no notifications folder today and becomes a home for cross-module events |

### Chosen Option: **A - reference DataSubjectRights** (recommended, pending the maintainer)

### Rationale

- A is the direction the ADR names and needs no change in the sibling plan; the cost is one `ProjectReference`. B wins when pay-for-what-you-use outweighs the coordination: a deployment with ProcessorAgreements and no DSR avoids the assembly.

**Class:** Dilemma

</details>

### Register of defaults and semantics

| Id | Choice | Class | Settled by |
|----|--------|-------|------------|
| C5 | The handler fails the operation on any delivery failure: it returns a `Left`, never reports success for a processor that was not notified; the outbox owns retry and dead-lettering; the ledger keeps the failed state | Settled | AGENTS.md section 3 (errors are never swallowed in background infrastructure); the acceptance criteria of #3975 |
| C6 | One failing processor does not stop the others: every processor is attempted in a deterministic order (by id), the failures are collected, and one `Left` (`processor_agreements.erasure_notification_incomplete`, `ErrorCategory.UpstreamFailure`, ADR-062 R7) is returned at the end | Trivial | The aggregate-failure behavior of `NotificationDispatcher`; ADR-062 R7 |
| C7 | Error codes and categories (ADR-062): `processor_agreements.erasure_notifier_not_configured` `Internal` (R1: a configuration entry); the existing `ProcessorAgreementErrors.TenantRequiredCode` (`processor.tenant_required`, `InvalidRequest`, R3: a missing tenant; the handler returns the error `TryResolveWrite` already produces and adds no code of its own for it); `processor_agreements.erasure_notification_failed` `UpstreamFailure` and `processor_agreements.erasure_notification_timeout` `Timeout` (R7: outbound failures); `processor_agreements.erasure_notification_incomplete` `UpstreamFailure`; `processor_agreements.erasure_ledger_key_mismatch` `Internal` (R4) | Settled | ADR-062 R1, R3, R4, R7 |
| C8 | Tenant: the handler takes the tenant from the notification, never from the ambient context (an outbox publish has none); `TryResolveWrite` denies when multi-tenancy requires a tenant and the notification has none (the existing `processor.tenant_required`) and when it contradicts a present ambient tenant; a single-tenant deployment (`RequireTenantContext` false and no tenancy) uses the null-tenant scope | Settled | AGENTS.md section 3 (gates fail closed when context is missing); `ProcessorAgreementTenantScope.cs`; SPEC-002 DEC-006 |
| C9 | The ledger key is canonical and injective: `t<len>:<tenant>` or `-` for no tenant, then `\|r<len>:<requestId>`. The stream id is the first 16 bytes of the SHA-256 of that key; the aggregate stores the canonical key, and a loaded stream whose key differs returns `processor_agreements.erasure_ledger_key_mismatch` (fail closed), so a hash collision can never merge two requests and wrongly skip a processor | Settled | AGENTS.md section 3 (gates fail closed; a non-injective key could mark a processor of one request as already notified in another); the property is proven by the unit tests of S3 |
| C9b | The tenant `default` is reserved and never produced for the no-tenant case. The reservation follows the Dilemma DC2 of #1257 (a reserved default tenant, recommended and pending the maintainer there); #3981, which the earlier draft cited, is itself open and recommended-only on this point, so neither is a source that settles it. If #1257 settles differently, only `ErasureNotificationLedger.Open` changes | Dilemma (recommended, pending the maintainer) | #1257 DC2 |
| C10 | The handler never reads a subject id into a log, tag, ledger event or audit entry; the notice carries the processor's `ContactEmail`, the tenant id and the DSR request id for the channel only, and they never leave the notifier call (never a log, tag, ledger event or audit field; the `ToString()` override of the notice prints none of them) | Settled | AGENTS.md section 3 (`EncinaError.Message` and personal data never reach logs, tags or plaintext storage); GDPR Art. 5(1)(c) |
| C11 | Tenant-explicit read: a new `IProcessorService.GetProcessorsForTenantAsync(string? tenantId, CancellationToken)` (uses `TryResolveWrite`, so it refuses a contradiction with a present ambient tenant) instead of running the handler under a service identity scope (`IRequestContextScopeFactory.RunAsServiceAsync` with `IdentityScopeOptions(TenantId)`), which needs a declared service identity and no package uses yet. The interface has two implementers on `origin/main` (grep `class .*IProcessorService` over `src` and `tests`): `src/Encina.Compliance.ProcessorAgreements/Services/DefaultProcessorService.cs:41` and the hand-written `private sealed class InMemoryProcessorService : IProcessorService` at `tests/Encina.LoadTests/Compliance/ProcessorAgreements/ProcessorAgreementLoadTests.cs:493`, which S0 must update or the solution no longer builds (CS0535); every other test double is an NSubstitute substitute (`Substitute.For<IProcessorService>()` in the unit, guard, integration and benchmark projects) and is unaffected | Dilemma (recommended, pending the maintainer) | Wins differently: the overload is the smaller change; the scope wins if the handler later needs other ambient-tenant services (the audit store, caching) |
| C12 | The audit entry goes to `IOperationAuditStore` (a new `ProjectReference` to `Encina.Security.Audit`): `Action = "ErasureNotificationSent"` or `"ErasureNotificationFailed"`, `EntityType = "Processor"`, `EntityId` the processor id, `CorrelationId` the DSR request id, `TenantId`, the error code in `ErrorMessage`, never a contact or a subject. The alternative keeps the ledger events as the only record and adds no reference | Dilemma (recommended, pending the maintainer) | The issue says "one audit entry for each processor notified" (ADR-036, ADR-050 sub-decision 5); wins differently for a deployment that wants no audit-store dependency |
| C13 | Numeric defaults: `ErasureNotificationTimeout` 30 seconds (a hung notifier must not block the outbox loop), validated above zero; the handler uses `TimeProvider` for the timeout, the ledger time and the audit time | Dilemma (recommended, pending the maintainer) | A numeric default; AGENTS.md section 3 (time comes from `TimeProvider`) |
| C14 | Opt-out: `ProcessorAgreementOptions.NotifyProcessorsOnErasure` defaults to true (ADR-050 makes the subscription a requirement of the module); when false the handler is not registered and the opt-out is logged at start | Settled | ADR-050 (ProcessorAgreements subscribes to DSR erasures); AGENTS.md section 3 (the only opt-out is explicit and logged) |
| C15 | The default notifier fails closed at delivery, and a start-up warning (EventId 52605) says only the default notifier is registered, so a deployment learns before the first erasure | Settled | The requirement of #3975 (fail closed until an application registers a notifier); AGENTS.md section 3 |

---

## Implementation Phases

### Phase 1: Tenant-Explicit Processor Read (pull request S0)

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `src/Encina.Compliance.ProcessorAgreements/Abstractions/IProcessorService.cs`: `ValueTask<Either<EncinaError, IReadOnlyList<ProcessorReadModel>>> GetProcessorsForTenantAsync(string? tenantId, CancellationToken cancellationToken = default);` with XML docs.
2. **Modify** `src/Encina.Compliance.ProcessorAgreements/Services/DefaultProcessorService.cs`: implement it with `_tenantScope.TryResolveWrite("GetProcessorsForTenant", tenantId, ...)` and the same query as `GetAllProcessorsAsync` (`!p.IsRemoved && p.TenantId == tenant`).
3. **Modify** `tests/Encina.LoadTests/Compliance/ProcessorAgreements/ProcessorAgreementLoadTests.cs`: add `GetProcessorsForTenantAsync` to the hand-written `InMemoryProcessorService` (line 493), filtering its in-memory processors by the tenant argument (the only other implementer on `origin/main`).
4. **Modify** `src/Encina.Compliance.ProcessorAgreements/PublicAPI.Unshipped.txt` and `.github/coverage-manifest/Encina.Compliance.ProcessorAgreements.json` through `mechanical-fixer`.
5. **Tests**: edit `tests/Encina.UnitTests/Compliance/ProcessorAgreements/DefaultProcessorServiceTests.cs` (explicit tenant returns that tenant's processors only, another tenant's are not returned, a contradiction with the ambient tenant is refused, a missing tenant is refused when enforcement is on, the null-tenant scope in a single-tenant deployment, a store failure is a `Left`) and `tests/Encina.GuardTests/Compliance/ProcessorAgreements/DefaultProcessorServiceMethodGuardTests.cs` (a null tenant accepted as a valid result).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT: Encina.Compliance.ProcessorAgreements is event sourced; IProcessorService.GetAllProcessorsAsync reads the ambient tenant through ProcessorAgreementTenantScope.TryResolve. A background handler needs the processors of an explicit tenant.
TASK: Add IProcessorService.GetProcessorsForTenantAsync(string? tenantId, CancellationToken) and implement it in DefaultProcessorService with TryResolveWrite and in the hand-written InMemoryProcessorService of tests/Encina.LoadTests/Compliance/ProcessorAgreements/ProcessorAgreementLoadTests.cs (grep every ": IProcessorService" implementer in src and tests first); add unit and guard tests; update PublicAPI.Unshipped.txt and the manifest through mechanical-fixer.
KEY RULES: Fail closed: a contradiction with the ambient tenant and a missing required tenant are refused, never widened. Never include another tenant's processors. Errors are Either, never exceptions for business input. Never put exception messages in errors that reach logs.
REFERENCE FILES: src/Encina.Compliance.ProcessorAgreements/Services/DefaultProcessorService.cs, src/Encina.Compliance.ProcessorAgreements/Services/ProcessorAgreementTenantScope.cs, tests/Encina.UnitTests/Compliance/ProcessorAgreements/DefaultProcessorServiceTests.cs.
```

</details>

### Phase 2: EventIds, Log Class and Instruments (pull request S1)

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `src/Encina/Diagnostics/EventIdRanges.cs`: `ComplianceProcessorErasureNotification = (52600, 52649)` (area Security and compliance; the first free 50-id slot after the audit-stores reservations 52350-52599 of the plans of #578 to #581); **modify** `src/Encina/PublicAPI.Unshipped.txt`, `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` (assembly `Encina.Compliance.ProcessorAgreements` maps to both `ComplianceProcessorAgreements` and the new range) and `docs/engineering/eventid-block-registry.md` (a `reserved` and a `zone` row), mechanical.
2. **Create** `src/Encina.Compliance.ProcessorAgreements/Diagnostics/ProcessorErasureNotificationLog.cs`: `internal static partial class`, `[LoggerMessage]` 52600 (Debug: delivered, processor id), 52601 (Warning: delivery failed, processor id and error code, attempt), 52602 (Warning: delivery timed out), 52603 (Debug: already notified, skipped), 52604 (Debug: claim lost to a concurrent handler), 52605 (Warning: only the fail-closed default notifier is registered), 52606 (Warning: tenant required but missing); packed, no subject, tenant or contact in any message.
3. **Modify** `src/Encina.Compliance.ProcessorAgreements/Diagnostics/ProcessorAgreementDiagnostics.cs`: counters `processor_agreement.erasure_notifications.sent` and `processor_agreement.erasure_notifications.failed` (no `_total` suffix; tags `error_code` and the tenant tag through the helper of #2676 only when multi-tenancy is on) and an activity `processor_agreements.erasure_notify` per delivery.
4. **Manifest** `.github/coverage-manifest/Encina.Compliance.ProcessorAgreements.json` through `mechanical-fixer`.
5. **Tests**: new `tests/Encina.UnitTests/Compliance/ProcessorAgreements/ProcessorErasureNotificationLogTests.cs` (ids, levels, rendered text carries no contact, tenant or subject) and `ProcessorErasureNotificationDiagnosticsTests.cs` (the counters and the activity with and without the tenant tag).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT: Encina issue #3975, phase 2. ProcessorAgreements logs with [LoggerMessage] in Diagnostics/ProcessorAgreementLogMessages.cs (ids up to 9487 of ComplianceProcessorAgreements) and has ProcessorAgreementDiagnostics with an ActivitySource and a Meter.
TASK: Register the range ComplianceProcessorErasureNotification (52600, 52649) in EventIdRanges.cs first, map the assembly in EncinaEventIdAllocationTests, add the registry rows and PublicAPI line, write the log class (ids 52600-52606) and the two counters and the activity, with tests.
KEY RULES: ADR-021: register the range before using ids; pack ids with no gap; use the source generator. Never put a tenant id, subject id, contact or EncinaError.Message in a log message or tag; log codes and processor ids (Guid) only. Metric names have no _total suffix; the tenant tag only when multi-tenancy is on (ADR-054 D4).
REFERENCE FILES: src/Encina/Diagnostics/EventIdRanges.cs, docs/engineering/eventid-block-registry.md, src/Encina.Compliance.ProcessorAgreements/Diagnostics/ProcessorAgreementLogMessages.cs, src/Encina.Compliance.ProcessorAgreements/Diagnostics/ProcessorAgreementDiagnostics.cs.
```

</details>

### Phase 3: Port, Default Notifier, Errors and Options (pull request S2, after #2407 PR 1)

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `src/Encina.Compliance.ProcessorAgreements/Encina.Compliance.ProcessorAgreements.csproj`: `ProjectReference` to `..\Encina.Compliance.DataSubjectRights\Encina.Compliance.DataSubjectRights.csproj` (Design Choice 4, one reference; the `Encina.Security.Audit` reference lands in S4).
2. **Create** `Abstractions/IProcessorErasureNotifier.cs`: `ValueTask<Either<EncinaError, Unit>> NotifyAsync(ProcessorErasureNotice notice, CancellationToken cancellationToken);` with XML docs naming the contract (idempotent by `IdempotencyKey`, never throw for expected failures, return a `Left`).
3. **Create** `Model/ProcessorErasureNotice.cs`: `public sealed record ProcessorErasureNotice(Guid ProcessorId, string ProcessorName, string? ContactEmail, string? ProcessorModuleId, string? TenantId, string DsrRequestId, string IdempotencyKey, int FieldsErasedCount, int FieldsRetainedCount, DateTimeOffset ErasedAtUtc, string? SubjectId)` (`SubjectId` is null under Design Choice 2 option A); `ToString()` overridden so a contact and a tenant never print.
4. **Create** `Services/FailClosedProcessorErasureNotifier.cs`: `internal sealed`, returns `ProcessorAgreementErrors.ErasureNotifierNotConfigured()`.
5. **Modify** `ProcessorAgreementErrors.cs` (the five new codes of register row C7, `erasure_notifier_not_configured`, `erasure_notification_failed`, `_timeout`, `_incomplete` and `erasure_ledger_key_mismatch`, with explicit categories and constant messages; the missing-tenant case reuses the existing `TenantRequiredCode`), `ProcessorAgreementOptions.cs` (`NotifyProcessorsOnErasure` true, `ErasureNotificationTimeout` 30 seconds) and `ProcessorAgreementOptionsValidator.cs` (timeout above zero).
6. **Modify** `PublicAPI.Unshipped.txt` and the manifest through `mechanical-fixer`.
7. **Tests**: new `tests/Encina.UnitTests/Compliance/ProcessorAgreements/ProcessorErasureNotifierTests.cs` (the default returns the code and category; the five codes, categories and constant messages; the notice prints no contact or tenant), edit `ProcessorAgreementOptionsTests.cs` and `ProcessorAgreementOptionsValidatorTests.cs` (the two options and the timeout rule), new `tests/Encina.GuardTests/Compliance/ProcessorAgreements/ProcessorErasureNoticeGuardTests.cs` (the notice's validated members, the default notifier's `NotifyAsync`). The contract test of the port lands with the handler in S4.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT: Encina issue #3975, phase 3. ErasureNotification exists after #2407 PR 1. This phase adds the outbound port and its fail-closed default; the handler comes later.
TASK: Add the DataSubjectRights ProjectReference, IProcessorErasureNotifier, ProcessorErasureNotice, FailClosedProcessorErasureNotifier, the five error codes and the two options with validation; write unit and guard tests; update PublicAPI and the manifest through mechanical-fixer.
KEY RULES: Every new error code has an explicit ErrorCategory (register row C7) and a constant message with no tenant, contact or request id. ProcessorErasureNotice overrides ToString so nothing personal prints. The default notifier never reports success. Options validated at startup. No Obsolete, no compatibility code.
REFERENCE FILES: src/Encina.Compliance.ProcessorAgreements/ProcessorAgreementErrors.cs, src/Encina.Compliance.ProcessorAgreements/ProcessorAgreementOptions.cs, src/Encina.Compliance.ProcessorAgreements/ProcessorAgreementOptionsValidator.cs, docs/architecture/adr/062-error-category-contract.md.
```

</details>

### Phase 4: The Ledger Aggregate (pull request S3)

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `Events/ErasureNotificationEvents.cs`: `ErasureLedgerOpened(Guid LedgerId, string LedgerKey, string DsrRequestId, string? TenantId, DateTimeOffset OccurredAtUtc)`, `ProcessorNotificationAttempted(Guid ProcessorId, int Attempt, DateTimeOffset OccurredAtUtc)`, `ProcessorNotified(Guid ProcessorId, DateTimeOffset OccurredAtUtc)`, `ProcessorNotificationFailed(Guid ProcessorId, string ErrorCode, DateTimeOffset OccurredAtUtc)`.
2. **Create** `Aggregates/ErasureNotificationLedger.cs` (`AggregateBase`): `static ErasureNotificationLedger Open(string? tenantId, string dsrRequestId, DateTimeOffset now)` (the canonical key and stream id of register row C9, a static `LedgerIdFor`), `bool IsNotified(Guid processorId)`, `int AttemptsOf(Guid processorId)`, `void BeginAttempt(Guid processorId, DateTimeOffset now)` (refused when already notified), `void MarkNotified(...)`, `void MarkFailed(Guid processorId, string errorCode, ...)`, `Apply` for the four events.
3. **Modify** `ProcessorAgreementsMartenExtensions.cs`: `services.AddAggregateRepository<ErasureNotificationLedger>();` (no projection: the ledger is read as an aggregate).
4. **Modify** `PublicAPI.Unshipped.txt` and the manifest through `mechanical-fixer`.
5. **Tests**: new `tests/Encina.UnitTests/Compliance/ProcessorAgreements/ErasureNotificationLedgerTests.cs` (state transitions, a notified processor cannot begin again, attempts count, the key format for no tenant, tenant `default` and tenants containing `:` and `\|`), a guard test, and new `tests/Encina.IntegrationTests/Compliance/ProcessorAgreements/ErasureNotificationLedgerIntegrationTests.cs` (`[Collection(MartenCollection.Name)]`, Marten on PostgreSQL: the round trip, and two sessions claiming the same processor where the second `SaveAsync` returns the concurrency `Left`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT: Encina issue #3975, phase 4. ProcessorAgreements aggregates (ProcessorAggregate, DPAAggregate) derive from AggregateBase and are saved through IAggregateRepository<T> with optimistic concurrency.
TASK: Add the ledger events and the ErasureNotificationLedger aggregate, register its repository in ProcessorAgreementsMartenExtensions, write unit, guard and Marten integration tests.
KEY RULES: The ledger key is canonical and injective (length-prefixed segments, a marker for no tenant, the tenant "default" never produced); the aggregate stores the key and the handler fails closed on a mismatch. Event-sourced modules have no InMemory store: unit tests mock IAggregateRepository with NSubstitute and the integration test uses the shared Marten collection fixture, never a per-class container. TimeProvider supplied by the caller, never DateTime.UtcNow. No personal data in any event.
REFERENCE FILES: src/Encina.Compliance.ProcessorAgreements/Aggregates/ProcessorAggregate.cs, src/Encina.Compliance.ProcessorAgreements/Events/ProcessorEvents.cs, src/Encina.Compliance.ProcessorAgreements/ProcessorAgreementsMartenExtensions.cs, tests/Encina.IntegrationTests/Compliance/ProcessorAgreements/ProcessorAgreementAggregateIntegrationTests.cs.
```

</details>

### Phase 5: The Handler, Registration and Audit (pull request S4)

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `Encina.Compliance.ProcessorAgreements.csproj`: `ProjectReference` to `..\Encina.Security.Audit\Encina.Security.Audit.csproj` (register row C12).
2. **Create** `Services/ProcessorErasureNotificationHandler.cs`: `internal sealed class ProcessorErasureNotificationHandler : INotificationHandler<ErasureNotification>`, constructor `(IProcessorService processors, IAggregateRepository<ErasureNotificationLedger> ledgers, IProcessorErasureNotifier notifier, IOperationAuditStore auditStore, ProcessorAgreementTenantScope tenantScope, IOptions<ProcessorAgreementOptions> options, TimeProvider? timeProvider, ILogger<ProcessorErasureNotificationHandler> logger)`; `Handle` implements the flow: resolve the tenant (register row C8); load or create the ledger; read the tenant's processors with `GetProcessorsForTenantAsync`; for each processor in id order skip a notified one, claim with `BeginAttempt` and `SaveAsync` (a concurrency `Left` logs 52604 and fails the handler), call the notifier under a linked token that cancels after `ErasureNotificationTimeout` (a timeout is `erasure_notification_timeout`; a caller cancellation propagates), record `MarkNotified` or `MarkFailed` and save, write the audit entry (an audit failure is logged by code and counted, and fails the handler: the erasure notification must not report success without its audit), collect failures, and return `Right(unit)` only when no processor failed.
3. **Modify** `ServiceCollectionExtensions.cs`: when `NotifyProcessorsOnErasure` is true, `TryAddEnumerable` of the handler as `INotificationHandler<ErasureNotification>`, `TryAddSingleton<IProcessorErasureNotifier, FailClosedProcessorErasureNotifier>()`, `TryAddScoped` of the tenant scope if absent, and a start-up check (hosted service) that logs 52605 when only the default notifier is registered and the opt-out when the option is false; the registration needs the `IAggregateRepository<ErasureNotificationLedger>`, which `AddProcessorAgreementAggregates` registers: a registration test builds the provider with `ValidateOnBuild` and `ValidateScopes` and fails when it is missing (AGENTS.md section 3, registration completeness).
4. **Modify** `PublicAPI.Unshipped.txt` and the manifest through `mechanical-fixer`.
5. **Tests**: new `ProcessorErasureNotificationHandlerTests.cs` (real handler, mocked `IAggregateRepository` and a fake notifier: every active processor notified once; a notified processor skipped on retry; a failing processor does not stop the others and the result is a `Left` with the incomplete code; a timeout; a lost claim; a missing required tenant denies; a processor of another tenant never notified; no subject, tenant or contact in any log, audit entry or counter tag; zero processors is a `Right`), `ProcessorErasureRegistrationTests.cs` (the provider builds with `ValidateOnBuild` and `ValidateScopes`; an application notifier wins over the default in any registration order), a guard test for the constructor and `Handle`, and a contract test `tests/Encina.ContractTests/Compliance/ProcessorAgreements/ProcessorErasureNotifierContractTests.cs` (any notifier, the fail-closed default and a fake, returns a `Left` instead of throwing for an expected failure and is idempotent by `IdempotencyKey`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT: Encina issue #3975, phase 5. Phases 1 to 4 are merged: GetProcessorsForTenantAsync, the log and instruments, IProcessorErasureNotifier with the fail-closed default, and the ledger aggregate. The notification arrives through the outbox processor, which publishes it with no ambient request context.
TASK: Implement ProcessorErasureNotificationHandler and its registration, the start-up check and the audit entry; write unit, registration and guard tests.
KEY RULES: Claim in the ledger with the expected stream version before calling the notifier; a concurrency Left fails the handler (the outbox retries). Never report success for a processor that was not notified; return a Left when any failed. Take the tenant from the notification, never the ambient context. Time and timeouts through TimeProvider. Never log or audit a subject id, tenant id, contact or EncinaError.Message; codes and processor ids only. Opt-out explicit and logged. An application notifier registered in any order wins over the default (TryAdd).
REFERENCE FILES: src/Encina/Dispatchers/Encina.NotificationDispatcher.cs, src/Encina.Messaging/Outbox/OutboxProcessorBase.cs, src/Encina.Security.Audit/AuditPipelineBehavior.cs, src/Encina.Compliance.ProcessorAgreements/Services/DefaultProcessorService.cs, docs/plans for #2407 (by issue number).
```

</details>

### Phase 6: End-to-End Test, Justifications and Documentation (pull request S5)

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `tests/Encina.IntegrationTests/Compliance/ProcessorAgreements/ProcessorErasureNotificationIntegrationTests.cs` (`[Collection(MartenCollection.Name)]`): the real `DefaultDSRService` erasure writes `ErasureNotification` to the outbox, the real outbox processor publishes it, the handler notifies two registered processors through a fake notifier and writes two audit entries; a failing notifier leaves the ledger failed and the outbox row retried; a second publish of the same request notifies nobody twice.
2. **Create** justification files `tests/Encina.PropertyTests/Compliance/ProcessorAgreements/ProcessorErasureNotification.md`, `tests/Encina.LoadTests/Compliance/ProcessorAgreements/ProcessorErasureNotification.md` and `tests/Encina.BenchmarkTests/Encina.Benchmarks/Compliance/ProcessorAgreements/ProcessorErasureNotification.md` (a few deliveries per erasure, not a hot path; the key mapping is covered by the unit and guard tests).
3. **Modify** `docs/features/processor-agreements.md` and `src/Encina.Compliance.ProcessorAgreements/README.md` through `docs-writer` (how to register a notifier, the notice, the ledger, at-least-once and the idempotency key, what failed looks like, the option); **modify** `docs/INVENTORY.md` (`docs-writer`).
4. **Create** `changelog.d/3975-processor-erasure-notification.added.md` through `mechanical-fixer`; **modify** `.github/coverage-manifest/Encina.Compliance.ProcessorAgreements.json` (the integration target of the handler, 85, with its one-sentence justification) through `mechanical-fixer`.
5. Build verification: `dotnet build --configuration Release` with 0 warnings; every flag reaches the targets of "Estimated File Count".

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```text
CONTEXT: Encina issue #3975, phase 6. The handler, ledger, port and registration exist; #2407 delivers ErasureNotification and the outbox write.
TASK: Write the end-to-end integration test, the three justification files, hand the documentation, README, INVENTORY row and changelog fragment to docs-writer and mechanical-fixer, and run build, tests and the coverage commands.
KEY RULES: Integration tests use the shared Marten collection fixture, never a per-class container; ClearAllDataAsync in InitializeAsync. Justification files follow the template of AGENTS.md section 9 and are never used for unit, guard or contract tests. Documentation never types coverage figures by hand (covref markers). Never edit CHANGELOG.md. Public text never names a private application.
REFERENCE FILES: tests/Encina.IntegrationTests/Compliance/ProcessorAgreements/ProcessorAgreementAggregateIntegrationTests.cs, docs/features/processor-agreements.md, changelog.d/README.md.
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Requirement | How the plan meets it |
|----------|-------------|-----------------------|
| GDPR Art. 19 | The controller communicates an erasure to each recipient to whom the data were disclosed, unless impossible or disproportionate | Every active processor of the tenant is notified; Design Choice 1 |
| GDPR Art. 28(3)(g) and 28(4) | Processor duties at the end of processing and flow-down to sub-processors | The notice reaches every depth by default |
| GDPR Art. 5(1)(c) and 5(2) | Minimisation and accountability | No subject id by default; a ledger and an audit entry per processor |
| ADR-050 | ProcessorAgreements subscribes to DSR erasures | The handler and the project reference |
| ADR-036 | Operation audit | One `IOperationAuditStore` entry per processor |
| ADR-054 D4 | Tenant tag only when multi-tenancy is on | The counters |
| ADR-062 R1, R3, R4, R7 | Error categories | Register row C7 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `IProcessorService`, `DefaultProcessorService`, `ProcessorAgreementTenantScope` | `src/Encina.Compliance.ProcessorAgreements/` | The processors of the tenant and the tenant rule |
| `ProcessorAggregate`, `IAggregateRepository<T>`, `AddAggregateRepository<T>` | `src/Encina.Compliance.ProcessorAgreements/Aggregates/`, `src/Encina.Marten/` | The ledger follows the same pattern |
| `INotificationHandler<T>`, `NotificationDispatcher` | `src/Encina/Dispatchers/` | The handler contract and the failure semantics |
| `OutboxProcessorBase` | `src/Encina.Messaging/Outbox/` | Retry and dead-lettering of the notification |
| `IOperationAuditStore`, `OperationAuditEntry` | `src/Encina.Security.Audit/` | The audit entry |
| `ProcessorAgreementDiagnostics`, `ProcessorAgreementLogMessages` | `src/Encina.Compliance.ProcessorAgreements/Diagnostics/` | Instruments and the neighboring log style |
| `MartenCollection`, `MartenFixture` | `tests/Encina.IntegrationTests/Infrastructure/Marten/Fixtures/MartenFixture.cs` | Integration fixture |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Compliance.ProcessorAgreements` | `52600-52649` (`ComplianceProcessorErasureNotification`), ids 52600-52606 used | New range, area Security and compliance, the first free 50-id slot on `origin/main` after the audit-stores reservations 52350-52599 (plans of #578 to #581); the assembly maps to this range and to `ComplianceProcessorAgreements` (the map value of each assembly is an array of range names; the precedent of two ranges for one assembly is `["Encina"] = [nameof(EventIdRanges.Core), nameof(EventIdRanges.ModuleIsolation)]` in `EncinaEventIdAllocationTests.cs`); registered in S1 with its registry rows |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| S0 (tenant read) | 7 (`IProcessorService.cs`, `DefaultProcessorService.cs`, `PublicAPI.Unshipped.txt`, manifest, unit test edit, guard test edit, `ProcessorAgreementLoadTests.cs` for its `InMemoryProcessorService`) | Targets: `DefaultProcessorService.cs` keeps its targets and the new method reaches unit 95 and guard 90 (every branch of the tenant rule, a null tenant a valid result); `IProcessorService.cs` unit 0 guard 0 (an interface has no executable lines) |
| S1 (EventIds, log, instruments) | 9 (`EventIdRanges.cs`, `PublicAPI.Unshipped.txt` of `Encina`, `EncinaEventIdAllocationTests.cs`, registry page, log class, `ProcessorAgreementDiagnostics.cs`, manifest, 2 unit tests) | Targets: `ProcessorErasureNotificationLog.cs` unit 90 guard 0 (generated bodies; no validated parameters); `ProcessorAgreementDiagnostics.cs` keeps its targets, new members unit 95 |
| S2 (port, default, errors, options) | 13 (csproj, 3 new production files, errors, options, validator, `PublicAPI.Unshipped.txt`, manifest, 4 tests: notifier and errors unit, options unit edit, validator unit edit, notice guard) | Targets: `FailClosedProcessorErasureNotifier.cs` unit 100 guard 0 (internal, no parameters); `ProcessorErasureNotice.cs` unit 90 guard 80; `IProcessorErasureNotifier.cs` unit 0 guard 0 (an interface; its contract is reached by the contract test of S4); `ProcessorAgreementErrors.cs` unit 95 (today's); `ProcessorAgreementOptions.cs` unit 95 (today's); `ProcessorAgreementOptionsValidator.cs` unit 95 guard 85 |
| S3 (ledger) | 8 (events, aggregate, Marten extension, `PublicAPI.Unshipped.txt`, manifest, unit test, guard test, integration test) | Targets: `ErasureNotificationLedger.cs` unit 95 guard 90 integration 80; `ErasureNotificationEvents.cs` unit 0 guard 0 (positional records, no logic); `ProcessorAgreementsMartenExtensions.cs` unit 90 guard 90 |
| S4 (handler, registration, audit) | 10 (csproj, handler, `ServiceCollectionExtensions.cs`, start-up check, `PublicAPI.Unshipped.txt`, manifest, 4 tests: handler unit, registration unit, guard, contract) | Targets: `ProcessorErasureNotificationHandler.cs` unit 95 guard 90 (its integration target 85 is entered in S5, where its integration test lives; S4 has no `Encina.IntegrationTests` file); `ServiceCollectionExtensions.cs` unit 95 guard 90 (today's, never lowered); the start-up check unit 95 guard 0 (internal, no validated parameters) |
| S5 (integration, justifications, docs) | 9 (integration test, 3 justification files, `processor-agreements.md`, README, `INVENTORY.md`, changelog fragment, manifest) | Targets: `ProcessorErasureNotificationHandler.cs` integration 85 (the end-to-end test drives the real outbox processor, the handler, the ledger on Marten and two registered processors, so every branch except the lost-claim race is reached); documentation phase |
| Total | 56 files over six pull requests (7, 9, 13, 8, 10, 9) | At most 13 files and 900 lines each |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Combined prompt for all phases</strong></summary>

```text
PROJECT CONTEXT: Encina (.NET 10, C# 14, pre-1.0). Issue #3975: Encina.Compliance.ProcessorAgreements consumes the ErasureNotification that the DSR erasure (#2407) writes to the outbox, and notifies each active processor of the tenant (GDPR Art. 19). ProcessorAgreements is event sourced on Marten (aggregates saved with optimistic concurrency); the outbox processor publishes notifications with no ambient request context.
IMPLEMENTATION OVERVIEW: (1) IProcessorService.GetProcessorsForTenantAsync(tenantId, ct). (2) EventIdRanges.ComplianceProcessorErasureNotification (52600, 52649), ids 52600-52606, two counters, one activity. (3) IProcessorErasureNotifier, ProcessorErasureNotice, FailClosedProcessorErasureNotifier, five error codes, two options, a ProjectReference to DataSubjectRights. (4) ErasureNotificationLedger aggregate (one stream per tenant and DSR request). (5) ProcessorErasureNotificationHandler with claim-then-send, per-delivery timeout, one IOperationAuditStore entry per processor, registration, start-up warning. (6) End-to-end test, justifications, documentation.
KEY PATTERNS: Never report success for a processor not notified: any failure is a Left and the outbox retries; the ledger skips the notified ones. Tenant from the notification, never from the ambient context; a missing required tenant denies. Ledger key length-prefixed and injective, stream id from its SHA-256, key verified on load. No subject id, tenant id, contact or EncinaError.Message in any log, tag, ledger event or audit entry. TimeProvider for every time. TryAdd registrations; an application notifier wins over the fail-closed default in any order; ValidateOnBuild and ValidateScopes test. Design Choices 1 to 4 are Dilemmas recommended and pending the maintainer: every active processor, no subject id, the ledger aggregate, a reference to DataSubjectRights.
REFERENCE FILES: src/Encina.Compliance.ProcessorAgreements/Services/DefaultProcessorService.cs, src/Encina.Compliance.ProcessorAgreements/Aggregates/ProcessorAggregate.cs, src/Encina.Compliance.ProcessorAgreements/ServiceCollectionExtensions.cs, src/Encina.Messaging/Outbox/OutboxProcessorBase.cs, src/Encina.Security.Audit/AuditPipelineBehavior.cs, src/Encina/Diagnostics/EventIdRanges.cs, docs/plans for #2407 (by issue number).
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Delivery is a write path, not a read path. |
| 2 | OpenTelemetry | ✅ | Counters `processor_agreement.erasure_notifications.sent` and `.failed` in the existing `Encina.Compliance.ProcessorAgreements` meter, tenant tag only when multi-tenancy is on (ADR-054 D4, helper of #2676), plus one activity per processor delivery. |
| 3 | Structured Logging | ✅ | Seven `[LoggerMessage]` ids in the new range `ComplianceProcessorErasureNotification` (52600-52649); error codes only, never a subject value or a processor contact. |
| 4 | Health Checks | ⏭️ | Processor liveness belongs to #2396; this plan adds a startup warning when only the fail-closed default notifier is registered. |
| 5 | Validation | ❌ | The notification is produced by the framework, not received from a caller. |
| 6 | Resilience | ✅ | The outbox retries the whole notification; a per-delivery timeout bounds a hung notifier; a failed delivery stays failed in the ledger and is never dropped. |
| 7 | Distributed Locks | ❌ | The ledger append uses the stream version of the aggregate (optimistic concurrency); a second handler loses the claim and the outbox retries it. |
| 8 | Transactions | ❌ | Each delivery is its own step: a ledger claim, the notifier call, a ledger result; none is atomic with the others. |
| 9 | Idempotency | ✅ | A per-DSR-request ledger deduplicates per processor, so each active processor is notified once per DSR request id. |
| 10 | Multi-Tenancy | ✅ | Processors are tenant-scoped; the handler reads them under the tenant of the notification, and a missing tenant denies. |
| 11 | Module Isolation | ❌ | The notification is tenant-wide; the processor's `ModuleId` rides in the notice but does not filter recipients. |
| 12 | Audit Trail | ✅ | One `IOperationAuditStore` entry per processor notified or failed, with codes and ids only. |

---

## Prerequisites & Dependencies

| Item | Kind | Why |
|------|------|-----|
| None for S0 and S1 | - | The tenant read and the diagnostics stand alone |
| #2407 PR 1 (`ErasureNotification`) | Hard for S2 onward | The handler consumes the type; S2 adds the project reference |
| #2407 outbox write (its `NotifyErasureStep`) | Hard for S5's end-to-end test | Without it nothing publishes the notification |
| #2407 Design Choice 9 | Follows | Design Choice 2 mirrors its answer; if it picks the subject id, the notice field is filled |
| #1198 | Coordination | Its stale ids 9486-9493 overlap ids already used on `origin/main` (9486-9487 by #3679); this plan uses its own range and is unaffected |
| #2676 | Soft | The tenant tag helper; until then the counters carry no tenant tag |
| #733 | Alternative | The notification inbox of Design Choice 3, option B |

---

## Next Steps

1. Open S0 and S1 first (no dependencies), then S2 after #2407's first pull request, S3, S4, S5 in order.
2. A per-subject disclosure record (Design Choice 1, option C) is a follow-on that narrows the recipients; it starts when the disclosure model of the write paths is designed and reuses the ledger and the port unchanged.
3. Retention of ledger streams after the legal period: the ledger holds no personal data (request id, processor ids, error codes), so it is covered by the audit-retention decision of the compliance modules and needs no job of its own now.
