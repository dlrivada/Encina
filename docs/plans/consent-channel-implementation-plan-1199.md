# Implementation Plan: `Encina.Compliance.Consent` — Channel-Scoped Consent, LSSI Art. 21.2 Existing-Client Basis and Transactional/Commercial Marker

> **Issue**: [#1199](https://github.com/dlrivada/Encina/issues/1199)
> **Type**: Feature (SPEC-002 REQ-014, AC-014, scenario S12, tracking id P-11, P0)
> **Milestone**: EU regulatory readiness (parent EPIC #1186)
> **Complexity**: High (8 phases, one package, no database provider matrix)
> **Estimated Scope**: ~900-1,100 lines of production code + ~1,800-2,200 lines of tests; exceeds one worker pull request, split into three (see Scope)

---

## Summary

Make consent in `Encina.Compliance.Consent` specific to a **purpose and a channel** (e-mail, SMS, WhatsApp, phone, terminal storage), let a record state that the sender relies on the **LSSI art. 21.2 existing-client basis** (with an opt-out that every message must offer), and let an application **mark each outbound message `Transactional` or `Commercial`**, so that an appointment reminder passes without marketing consent and a promotion does not (SPEC-002 REQ-014; an appointment reminder is not a commercial communication under LSSI Annex (f), marked [I] in the issue: the application decides how each message is marked).

### What the issue asks and what the code does today

- The issue asks for: a consent key (purpose, channel) with an extensible channel set and five built-in channels; a legal basis `ExistingClient` with an opt-out that the notification layer (P-33) must offer in every message; a message classification the application sets, which the consent behaviour uses to require channel consent or the art. 21.2 basis only for commercial messages; GDPR, ePrivacy Art. 5(3) and LSSI arts. 20-22 tables in the Consent specification (with P-17, #1209).
- The issue body is **settled by elimination** (comment of 2026-10-17): encoding the channel in the purpose string and treating every message as commercial are rejected; compliance modules enforce by default, so a commercial message without channel consent or the art. 21.2 basis is refused.
- Today consent has no channel dimension. `ConsentGranted` (`src/Encina.Compliance.Consent/Events/ConsentEvents.cs:36-49`) carries `Purpose` and a `Source` string that describes **how consent was collected** ("web-form", "api"), not the **communication channel**; the plan keeps both and names the new field `Channel` to avoid the clash.
- The natural key is `(DataSubjectId, Purpose)` plus tenant. `DefaultConsentService.GetConsentBySubjectAndPurposeAsync` (`Services/DefaultConsentService.cs:507-509`) and `HasValidConsentAsync` (`:574-580`) filter on those three fields only and take `readModels[0]`, so a second consent for another channel would be picked arbitrarily; the cache key (`:494`) has the same shape.
- `ConsentRequiredPipelineBehavior.Handle` (`ConsentRequiredPipelineBehavior.cs:85-192`) takes only `RequireConsentAttribute.Purposes` (`Attributes/RequireConsentAttribute.cs`) and the subject (property or `IRequestContext.UserId`). It cannot tell a reminder from a promotion. `ConsentPurposes.Marketing` and `Newsletter` (`Model/ConsentPurposes.cs`) are plain strings.
- `Encina.Notifications` (P-33) does not exist yet (`src/` has no such project), so this plan defines the **contract the notification layer will call**, not the notification layer.
- Grant is not idempotent: `GrantConsentAsync` (`DefaultConsentService.cs:135-185`) creates a new aggregate with a random id on every call, so two active records can exist for one key. With a channel dimension a withdrawal on one record would not stop the other. This is recorded as a follow-up issue (see Next Steps) and covered by a test in Phase 7.

### Scope

- **Affected package**: `Encina.Compliance.Consent` only (plus tests, README, `docs/features/consent-management.md`, coverage manifest, changelog fragment, `PublicAPI.Unshipped.txt`).
- **Provider category**: none of the AGENTS.md §5 matrices. Consent is event-sourced on Marten (PostgreSQL); SPEC-002 DEC-008 (a) and ADR-019 keep the nine event-sourced compliance modules Marten-only in 1.0; AGENTS.md §3 requires unit tests with a mocked `IAggregateRepository` and integration tests on Marten through Testcontainers.
- **Estimated files**: ~12 new source files, ~14 modified source files, ~14 test files, ~5 documentation files.
- **Pull request split** (the feature exceeds 15 files and 1,500 lines): **PR 1** = Phases 1-2 (shape: channel, basis, classification types, events, aggregate, projection, service key; ~13 files); **PR 2** = Phases 3-5 (options, evaluator, attribute, behaviour, cross-cutting; ~14 files); **PR 3** = Phases 6-8 (observability, remaining tests, documentation). Each PR carries the tests of its own phases.
- **Event IDs**: 8279-8287 in `ComplianceConsent = (8200, 8299)`, after #811 (8269-8274) and #810 (8275-8278); #1255 takes 8288-8293 (its plan). See Research.

### Related open issues that touch the same files

- #811 and #810 change `ConsentOptions`, `IConsentService`, `DefaultConsentService` and `ConsentRequiredPipelineBehavior`; #1197 (representation) adds `Representation` to the same four events; #1255 (minors) adds an age flag to `ConsentGranted`. Order and coordination are in Prerequisites & Dependencies.
- #1209 (Consent article-coverage specification) creates the specification the issue asks to extend.
- P-33 (`Encina.Notifications`) consumes the evaluator defined here; S12 is verified by #1227.

---

## Design Choices

<details>
<summary><strong>1. Channel model — string identifiers in a static class, plus a validated set of definitions in the options</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ConsentChannels` static class of `const string` identifiers (`email`, `sms`, `whatsapp`, `phone`, `terminal-storage`), with `ConsentOptions.DefineChannel(id, configure)` for application channels and startup validation that a used channel is defined** | Same convention as `ConsentPurposes` and as the `string Purpose` already stored on the aggregate and event; extensible; no enum migration | A typo is a runtime error, mitigated by the startup validation |
| **B) `enum ConsentChannel`** | Compile-time safety | Not extensible, which the issue requires; every new channel is a persisted-enum change |
| **C) `readonly record struct ConsentChannel(string Value)` with static members** | Typed and extensible | A new serialized type for Marten and the read model for little gain over A; diverges from how purposes are modelled |

### Chosen Option: **A — string identifiers, validated by options**

Class: Trivial
Settled by: `src/Encina.Compliance.Consent/Model/ConsentPurposes.cs` (purposes are lowercase hyphen-separated `const string` values stored as `string`), issue #1199 Proposed Solution ("channels are an extensible set with the five above built in")

### Rationale

- A channel is stored as `string? Channel` on `ConsentGranted`, `ConsentAggregate` and `ConsentReadModel`, exactly like `Purpose`.
- `ConsentChannelDefinition` (per channel: `Description`, `SupportsExistingClientBasis`) lives in `ConsentOptions.ChannelDefinitions`; the five built-ins are pre-registered. An unknown channel is a startup failure for attributes and an `Either` error at grant time (fail closed).

</details>

<details>
<summary><strong>2. Consent that names no channel — channel-agnostic record that never satisfies a channel requirement</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Channel` is `null` for purposes where a channel does not apply (analytics, profiling); a null-channel record satisfies only requirements without a channel; a requirement for a channel needs a record for that channel** | Fail closed; "no WhatsApp marketing without channel consent while e-mail marketing is allowed" (S12) holds by construction; existing purposes keep working unchanged | An application that wants "marketing on any channel" must grant one record per channel |
| **B) A wildcard channel `ConsentChannels.All` that satisfies every channel requirement** | One record for "any channel" | A single tick can authorise a channel the subject was never shown; the wildcard is the exact over-broad consent S12 forbids; harder to withdraw per channel |

### Chosen Option: **A — null channel is channel-agnostic and never satisfies a channel requirement** (recommended, pending the maintainer)

Class: Dilemma

### Rationale

- We recommend A because it is the only option that cannot authorise a channel the subject did not choose (AGENTS.md §3, fail closed).
- B wins in an application whose consent form legitimately offers a single "any electronic channel" checkbox and where counsel confirms that wording as specific enough; then `All` is added as a sixth built-in and the evaluator treats it as matching. The data model is the same for both, so the choice can change without a persisted-shape change.

</details>

<details>
<summary><strong>3. Where the LSSI art. 21.2 existing-client basis is recorded — a basis field on the consent record, opt-out is a withdrawal</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ConsentBasis` (`Consent = 1`, `ExistingClientLssi212 = 2`) and `BasisReference` on `ConsentGranted` and the read model; the opt-out is the existing `ConsentWithdrawn` event on the same stream** | Reuses the lifecycle, projection, tenant scoping and audit trail; "opt-out" and "withdrawal" are the same state change; one record per (subject, purpose, channel) answers both questions | A record that is not consent sits in the consent store; `HasValidConsentAsync` would return true for it unless the basis is filtered |
| **B) A separate `ExistingClientAggregate` with its own projection, as #811 does for exemptions** | Consent queries, Art. 15 exports and Art. 7 semantics stay purely about consent | New aggregate, projection, read model, registration and opt-out events (~6 files) for a state that behaves like a consent lifecycle; two stores to query for one "may we send" answer |

### Chosen Option: **A — basis field on the consent record** (recommended, pending the maintainer)

Class: Dilemma

### Rationale

- We recommend A because the art. 21.2 situation has the same lifecycle as consent (it starts, it can be ended by the recipient, the end must be provable) and the evaluator needs one lookup per (subject, purpose, channel).
- To keep Art. 7 semantics clean under A, `IConsentService.HasValidConsentAsync` and `DefaultConsentValidator` count only `Basis == Consent` records; only the evaluator (Phase 4) accepts `ExistingClientLssi212`, and only for commercial messages on channels whose definition has `SupportsExistingClientBasis`.
- B wins when the maintainer wants the Consent dashboard, the Art. 15 export and `GetAllConsentsAsync` to list only genuine consents without a filter; the cost is the second aggregate.
- The legal premise (art. 21.2 covers electronic communications "equivalent" to e-mail, so SMS and WhatsApp qualify, while phone calls do not) is marked [I] in the issue and must be confirmed against the LSSI text before the XML documentation names it; the default flags are `SupportsExistingClientBasis = true` for e-mail, SMS and WhatsApp and `false` for phone and terminal storage, and an application can change them.

</details>

<details>
<summary><strong>4. How a request declares its channel and classification — attribute values plus property names, following <code>SubjectIdProperty</code></strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `[RequireConsent]` gains `Channel`, `ChannelProperty`, `Classification` and `ClassificationProperty`; a static value covers a command type that is always one channel, a property name covers a generic "send message" command; resolved with the same cached reflection and fail-closed rules as `SubjectIdProperty`** | Exactly the existing pattern (`PropertyCache`, `SubjectIdConversion`); both static and per-instance cases work; no new interface for requests to implement | Reflection by property name (already accepted for the subject) |
| **B) A marker interface `IOutboundMessage { string Channel; MessageClassification Classification }` on the request** | Compile-time checked; no reflection | Forces every request type to implement it, including requests that only need a static channel; a second way to find the subject and a different way to find the channel |
| **C) Both A and B** | Maximum flexibility | Two ways to do one thing; precedence rules to define and test |

### Chosen Option: **A — attribute properties, same pattern as `SubjectIdProperty`**

Class: Trivial
Settled by: `src/Encina.Compliance.Consent/Attributes/RequireConsentAttribute.cs` (`SubjectIdProperty` and its cached reflection in `ConsentRequiredPipelineBehavior.ExtractSubjectId`, lines 194-225)

### Rationale

- A property that resolves to `null` or an unknown value fails closed: an unreadable classification on a request that declares a channel is treated as `Commercial`, an unreadable or unknown channel is a coded error, never a pass.
- A static `Classification = Transactional` together with a purpose in the marketing set (`Marketing`, `Newsletter`) is a contradiction that the startup attribute scan reports (Phase 3), so a promotion cannot be marked transactional by declaration.

</details>

<details>
<summary><strong>5. Where the decision lives and how P-33 learns about the opt-out — a public evaluator that the behaviour and the notification layer both call</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IChannelConsentEvaluator.EvaluateAsync(subjectId, purposes, channel, classification, ct)` returning `Either<EncinaError, ChannelConsentDecision>` (`Allowed`, `Basis`, `OptOutRequired`, `MissingPurposes`); the behaviour calls it and `Encina.Notifications` (P-33) calls the same service before sending** | One rule, one test suite; the notification layer cannot disagree with the pipeline; `OptOutRequired` is the machine-readable form of "offer an opt-out in every message" | A new public service |
| **B) Only the pipeline behaviour decides; P-33 relies on the request having passed** | No new public API | Messages sent outside a `[RequireConsent]` request path (a scheduled reminder job) are unchecked; the opt-out requirement has no carrier |
| **C) Put the rule in `IConsentValidator.ValidateAsync` by adding channel and classification parameters** | No new service | Changes a shipped signature used by `DefaultConsentValidator` and every custom validator; mixes the generic purpose check with the LSSI rule |

### Chosen Option: **A — `IChannelConsentEvaluator`**

Class: Settled
Settled by: issue #1199 Proposed Solution ("an opt-out that the notification layer (P-33) must offer in every message"), SPEC-002 scenario S12 ("the reminder is delivered through `Encina.Notifications`")

### Rationale

- The notification layer must receive "an opt-out is required" from somewhere; a decision object is the smallest carrier and keeps the Consent package free of any sending code.
- The default implementation composes `IConsentService` queries; it is registered with `TryAdd`, so an application can replace it.

</details>

<details>
<summary><strong>6. Enforcement of the new rule — enforced by default, missing classification is Commercial</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The channel rule follows `ConsentOptions.EnforcementMode` (default `Block`); a request that declares a channel but yields no classification is `Commercial`** | Fails closed; consistent with every other check in the behaviour; warn-only stays an explicit opt-out | An application that forgets to mark reminders sees them refused until it marks them |
| **B) A new `ChannelEnforcementMode` defaulting to `Warn` for the first release** | Gentle rollout | Pre-1.0 there is no installed base to protect; a warn default is the over-permissive default that AGENTS.md §3 forbids |

### Chosen Option: **A — enforce by default, unclassified means Commercial**

Class: Settled
Settled by: `AGENTS.md` §3 (compliance and security gates fail closed; the only opt-out is explicit and logged), issue #1199 comment of 2026-10-17 ("compliance modules enforce by default")

### Rationale

- `ConsentOptions.EnforcementMode` already defaults to `Block` (`ConsentOptions.cs`); `Warn` and `Disabled` keep their meaning and the Warn path logs through EventId 8279-8287 messages instead of passing silently.

</details>

---

## Implementation Phases

### Phase 1: Core Models, Events, Aggregate and Projection

> **Goal**: The persisted shape: channel, basis and classification exist on events, aggregate and read model.

<details>
<summary><strong>Tasks</strong></summary>

1. **`src/Encina.Compliance.Consent/Model/ConsentChannels.cs`** (new, `public static class`): `Email = "email"`, `Sms = "sms"`, `WhatsApp = "whatsapp"`, `Phone = "phone"`, `TerminalStorage = "terminal-storage"`, XML docs citing ePrivacy Art. 13 and Art. 5(3) and LSSI art. 21 and 22.1.
2. **`Model/ConsentBasis.cs`** (new enum, explicit numbering, no `0` member): `Consent = 1`, `ExistingClientLssi212 = 2`.
3. **`Model/MessageClassification.cs`** (new enum): `Transactional = 1`, `Commercial = 2`; XML docs state that the application decides and that an unclassified message is treated as `Commercial`.
4. **`Events/ConsentEvents.cs`** (modify `ConsentGranted`, line 36): add `string? Channel`, `ConsentBasis Basis`, `string? BasisReference` (the reference to the prior contractual relationship; an opaque identifier, never a name or contact data) before `TenantId`. Coordinate the parameter order with #1197 and #1255 (Prerequisites & Dependencies). `ConsentReconsentProvided` keeps the channel of its stream (no change).
5. **`Aggregates/ConsentAggregate.cs`** (modify): properties `Channel`, `Basis`, `BasisReference`; `Grant(...)` gains `string? channel = null, ConsentBasis basis = ConsentBasis.Consent, string? basisReference = null` and throws `ArgumentException` for `ExistingClientLssi212` without a `basisReference`; `Apply(ConsentGranted)` sets them.
6. **`ReadModels/ConsentReadModel.cs`** and **`ReadModels/ConsentProjection.cs`** (modify): `Channel`, `Basis`, `BasisReference` set in `Create(ConsentGranted, ...)`.
7. **`PublicAPI.Unshipped.txt`**: every new public symbol (delegated to `mechanical-fixer`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of channel-scoped consent in Encina.Compliance.Consent (issue #1199).

CONTEXT:
- Consent is event-sourced on Marten: ConsentAggregate (Aggregates/ConsentAggregate.cs), events in Events/ConsentEvents.cs, ConsentProjection and ConsentReadModel in ReadModels/.
- ConsentGranted carries Purpose and Source ("how consent was collected"); it has no communication channel.
- Purposes are lowercase hyphen-separated const strings in Model/ConsentPurposes.cs.

TASK:
Add ConsentChannels (five const strings), ConsentBasis and MessageClassification enums (explicit values, no zero member), and carry Channel, Basis and BasisReference through ConsentGranted, ConsentAggregate.Grant/Apply, ConsentReadModel and ConsentProjection.Create as listed in the plan's Phase 1 Tasks.

KEY RULES:
- Name the new field Channel, never Source (Source already means how consent was collected).
- ExistingClientLssi212 requires a BasisReference; the aggregate throws ArgumentException without one.
- BasisReference is an opaque identifier; it never reaches logs, activity tags or metric tags.
- XML docs on every public member; no [Obsolete], no compatibility shims; zero warnings.

REFERENCE FILES:
- src/Encina.Compliance.Consent/Events/ConsentEvents.cs
- src/Encina.Compliance.Consent/Aggregates/ConsentAggregate.cs
- src/Encina.Compliance.Consent/ReadModels/ConsentProjection.cs
- tests/Encina.UnitTests/Compliance/Consent/Aggregates/ConsentAggregateTests.cs
```

</details>

---

### Phase 2: Service Key (purpose, channel) and Opt-Out

> **Goal**: Every lookup and every cache key distinguishes the channel; the opt-out has a service operation.

<details>
<summary><strong>Tasks</strong></summary>

1. **`Abstractions/IConsentService.cs`** (modify): `GrantConsentAsync` gains `string? channel = null, ConsentBasis basis = ConsentBasis.Consent, string? basisReference = null` before `CancellationToken`; `GetConsentBySubjectAndPurposeAsync` and `HasValidConsentAsync` gain `string? channel = null`; new `ValueTask<Either<EncinaError, Unit>> OptOutAsync(string dataSubjectId, string purpose, string channel, string optedOutBy, CancellationToken cancellationToken = default)` that finds the active record for the key and withdraws it (idempotent: no active record returns `Right(unit)`).
2. **`Services/DefaultConsentService.cs`** (modify): queries filter `c.Channel == channel` (null matches null, so existing channel-less purposes behave as before); `HasValidConsentAsync` also requires `c.Basis == ConsentBasis.Consent` (Design Choice 3); the cache key (line 494) gains `:channel:{channel ?? "-"}`; `GrantConsentAsync` validates the channel against `ConsentOptions.ChannelDefinitions` and the basis against `SupportsExistingClientBasis`, returning `ConsentErrors.UnknownChannel` / `ConsentErrors.BasisNotAllowedOnChannel`.
3. **`ConsentErrors.cs`** (modify): `UnknownChannelCode = "consent.unknown_channel"`, `BasisNotAllowedOnChannelCode = "consent.basis_not_allowed_on_channel"`, `ChannelConsentMissingCode = "consent.channel_missing"` and factories; messages carry codes and channel names only, never the subject id.
4. **`DefaultConsentValidator.cs`**: no signature change; it keeps the channel-less path (calls the service with `channel: null`).
5. **`PublicAPI.Unshipped.txt`** (via `mechanical-fixer`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of channel-scoped consent in Encina.Compliance.Consent (issue #1199).

CONTEXT:
- Phase 1 added Channel, Basis, BasisReference to the events, aggregate and read model.
- DefaultConsentService.GetConsentBySubjectAndPurposeAsync (lines 480-530) and HasValidConsentAsync (558-607) filter on subject, purpose and tenant only and take readModels[0]; the cache key at line 494 has the same shape.

TASK:
Add the channel parameter to the three service members, filter and key by it, add OptOutAsync, validate channel and basis on grant, and add the three error codes, as listed in the plan's Phase 2 Tasks.

KEY RULES:
- A null channel matches only null-channel records, never a channel record (fail closed, Design Choice 2).
- HasValidConsentAsync counts only Basis == Consent.
- Tenant scoping (TryResolveTenantScope) and the tenant-scoped cache key stay exactly as they are.
- Logs carry purpose and channel only: never the subject id, the actor or the basis reference (#1314).
- Time comes from TimeProvider; ROP everywhere; no swallowed errors.

REFERENCE FILES:
- src/Encina.Compliance.Consent/Services/DefaultConsentService.cs
- src/Encina.Compliance.Consent/Abstractions/IConsentService.cs
- src/Encina.Compliance.Consent/ConsentErrors.cs
- tests/Encina.UnitTests/Compliance/Consent/
```

</details>

---

### Phase 3: Options, Channel Definitions and Startup Validation

> **Goal**: Channels are configured and validated before the first request.

<details>
<summary><strong>Tasks</strong></summary>

1. **`ConsentOptions.cs`** (modify): `Dictionary<string, ConsentChannelDefinition> ChannelDefinitions` pre-filled with the five built-ins (`SupportsExistingClientBasis`: true for `email`, `sms`, `whatsapp`; false for `phone`, `terminal-storage`); `ConsentOptions DefineChannel(string channel, Action<ConsentChannelDefinition>? configure = null)` (fluent, `ArgumentException.ThrowIfNullOrWhiteSpace`); nested `ConsentChannelDefinition` with `Description` and `SupportsExistingClientBasis`.
2. **`ConsentOptionsValidator.cs`** (modify): failures for an empty channel key and for a channel key that is not lowercase hyphen-separated.
3. **`ConsentAutoRegistrationHostedService.cs`** (modify `ValidatePurposes`, lines 114-140): the attribute scan also checks each `[RequireConsent]` channel (static value) against `ChannelDefinitions` and reports a static `Classification = Transactional` combined with a purpose in `Marketing` or `Newsletter` as a configuration error (Design Choice 4).
4. **`Attributes/RequireConsentAttribute.cs`** (modify): `Channel`, `ChannelProperty`, `Classification` (`MessageClassification?`), `ClassificationProperty`; XML docs with the reminder and promotion examples.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of channel-scoped consent in Encina.Compliance.Consent (issue #1199).

CONTEXT:
- ConsentOptions (ConsentOptions.cs) has PurposeDefinitions and DefinePurpose; ConsentOptionsValidator and ConsentAutoRegistrationHostedService validate them at startup.
- RequireConsentAttribute has Purposes, SubjectIdProperty and ErrorMessage.

TASK:
Add ChannelDefinitions with the five built-ins, DefineChannel, the nested ConsentChannelDefinition, the validator rules, the attribute properties and the startup attribute checks listed in the plan's Phase 3 Tasks.

KEY RULES:
- SupportsExistingClientBasis defaults: true for email, sms, whatsapp; false for phone and terminal-storage.
- A static Transactional classification with a Marketing or Newsletter purpose is a startup configuration error, not a warning.
- Follow the DefinePurpose style exactly (fluent, returns this, guards).
- XML docs on every public member; zero warnings.

REFERENCE FILES:
- src/Encina.Compliance.Consent/ConsentOptions.cs
- src/Encina.Compliance.Consent/ConsentOptionsValidator.cs
- src/Encina.Compliance.Consent/ConsentAutoRegistrationHostedService.cs
- src/Encina.Compliance.Consent/Attributes/RequireConsentAttribute.cs
```

</details>

---

### Phase 4: Evaluator and Pipeline Behaviour

> **Goal**: One rule decides whether a message to a channel may be sent, used by the behaviour and by P-33.

<details>
<summary><strong>Tasks</strong></summary>

1. **`Abstractions/IChannelConsentEvaluator.cs`** (new): `ValueTask<Either<EncinaError, ChannelConsentDecision>> EvaluateAsync(string subjectId, IReadOnlyList<string> purposes, string channel, MessageClassification classification, CancellationToken cancellationToken = default)`.
2. **`Model/ChannelConsentDecision.cs`** (new `sealed record`): `bool Allowed`, `ConsentBasis? Basis`, `bool OptOutRequired`, `IReadOnlyList<string> MissingPurposes`; factories `Allow(basis, optOutRequired)`, `Refuse(missingPurposes)`.
3. **`DefaultChannelConsentEvaluator.cs`** (new, `sealed`, scoped): rule: `Transactional` is allowed without consent records (no marketing consent is read) and returns `OptOutRequired = false`; `Commercial` needs, per purpose, an active unexpired `Basis == Consent` record for the channel, else an active unexpired `ExistingClientLssi212` record for the channel when the channel definition allows it (then `OptOutRequired = true`); otherwise `Refuse`. Uses `IConsentService` and `TimeProvider`; a `Left` from the service is returned as `Left` (never a pass).
4. **`ConsentRequiredPipelineBehavior.cs`** (modify `Handle`, after subject extraction at line 121): resolve channel and classification (static value, else cached property lookup; missing classification means `Commercial`); when a channel is present call the evaluator instead of `IConsentValidator`, map a refusal to `ConsentErrors.ChannelConsentMissing` in `Block` mode and log in `Warn` mode; requests without a channel keep the existing path. `OptOutRequired` is written to `IRequestContext` metadata item `encina.consent.opt_out_required` when the context offers items (verify the context type in the worker's first step), otherwise exposed only through the evaluator.
5. **`ServiceCollectionExtensions.cs`** (modify): `TryAddScoped<IChannelConsentEvaluator, DefaultChannelConsentEvaluator>()` in `AddEncinaConsent`.
6. **`PublicAPI.Unshipped.txt`** (via `mechanical-fixer`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of channel-scoped consent in Encina.Compliance.Consent (issue #1199).

CONTEXT:
- Phases 1-3 added Channel/Basis on the record, the service key, ChannelDefinitions and the attribute properties.
- ConsentRequiredPipelineBehavior.Handle (ConsentRequiredPipelineBehavior.cs:85-192) validates purposes through IConsentValidator after extracting the subject.
- Encina.Notifications (P-33) does not exist yet; it will call IChannelConsentEvaluator before sending.

TASK:
Create IChannelConsentEvaluator, ChannelConsentDecision and DefaultChannelConsentEvaluator, call the evaluator from the behaviour when the request declares a channel, and register it with TryAdd, as listed in the plan's Phase 4 Tasks.

KEY RULES:
- Transactional passes without reading marketing consent; Commercial needs channel consent or an active art. 21.2 record (then OptOutRequired is true).
- A missing or unreadable classification is Commercial; an unknown channel is a coded error (fail closed).
- A Left from the service fails the check; never report success (AGENTS.md section 3).
- Expiry uses TimeProvider; the subject id never reaches logs, tags or metrics.
- Cache the attribute and property lookups per generic type like the existing AttributeCache/PropertyCache.

REFERENCE FILES:
- src/Encina.Compliance.Consent/ConsentRequiredPipelineBehavior.cs
- src/Encina.Compliance.Consent/DefaultConsentValidator.cs
- src/Encina.Compliance.Consent/ServiceCollectionExtensions.cs
- tests/Encina.UnitTests/Compliance/Consent/ConsentRequiredPipelineBehaviorTests.cs
```

</details>

---

### Phase 5: Cross-Cutting Integration

> **Goal**: Tenant, audit and validation behave like the rest of the module.

<details>
<summary><strong>Tasks</strong></summary>

1. **Multi-tenancy**: the evaluator and `OptOutAsync` go through the service's `TryResolveTenantScope` (`DefaultConsentService.cs`, #1315); no new tenant logic. A two-tenant test shows tenant A's e-mail marketing consent never satisfies tenant B (AC-043).
2. **Audit trail**: the events are the audit trail; `ConsentGranted` records channel, basis and reference, `ConsentWithdrawn` records the opt-out; no new audit store.
3. **Validation**: channel, basis and classification validation lives in Phases 2-4 (service, options validator, behaviour); nothing separate.
4. **Not applicable here (see matrix)**: caching of decisions, health check, resilience, locks, transactions, idempotency, module isolation.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are verifying the cross-cutting integration of channel-scoped consent in Encina.Compliance.Consent (issue #1199).

CONTEXT:
- DefaultConsentService resolves the ambient tenant for every read and write (TryResolveTenantScope, #1315).
- Events are the audit trail (ConsentGranted, ConsentWithdrawn).

TASK:
Make sure the evaluator and OptOutAsync use the service's tenant-scoped path only, add the two-tenant tests of the plan's Phase 7, and confirm that no new audit store or lock is introduced.

KEY RULES:
- No query without the tenant predicate; with tenancy off the fixed default tenant applies.
- Do not add caching of decisions: consent is a mutable permission.
- Do not duplicate tenant logic in the evaluator.

REFERENCE FILES:
- src/Encina.Compliance.Consent/Services/DefaultConsentService.cs
- tests/Encina.IntegrationTests/Compliance/Consent/ConsentTenantScopingIntegrationTests.cs
```

</details>

---

### Phase 6: Observability

> **Goal**: Instruments and logs for the channel decision, without payloads or subject identifiers (SPEC-002 REQ-062, AC-044).

<details>
<summary><strong>Tasks</strong></summary>

1. **`Diagnostics/ConsentDiagnostics.cs`** (modify): counters `consent.channel.checks.total` (tags `consent.channel`, `consent.message_class`, `consent.outcome`), `consent.channel.refusals.total` (tags `consent.channel`, `consent.failure_reason`), `consent.existing_client.optouts.total` (tag `consent.channel`); tag constants `TagChannel`, `TagMessageClass`, `TagTenantId = "encina.tenant_id"`; the existing `Consent.Check` activity gains `consent.channel`, `consent.message_class` and `encina.tenant_id` (the activity has no tenant tag today). The issue's proposed names (`encina.consent.channel_refusals`, `encina.consent.check`) are replaced by the existing naming of the package (`consent.*`, activity `Consent.Check`).
2. **`Diagnostics/ConsentChannelLogMessages.cs`** (new, `internal static partial class`, `[LoggerMessage]`, XML doc "Event IDs: 8279-8287 (see EventIdRanges.ComplianceConsent)"): 8279 `ChannelConsentPassed`, 8280 `ChannelConsentRefused`, 8281 `ExistingClientBasisUsed`, 8282 `ExistingClientOptedOut`, 8283 `TransactionalMessagePassed`, 8284 `ClassificationMissingTreatedAsCommercial`, 8285 `UnknownChannelRejected`, 8286 `ChannelEvaluationFailed` (error code only), 8287 `ChannelWarnModeProceeded`; each carries purpose, channel and class, never the subject, actor or basis reference.
3. No range change: the assembly is already mapped in `EncinaEventIdAllocationTests.cs:63`. `PublicAPI.Unshipped.txt` unaffected by internal log classes.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
You are implementing Phase 6 of channel-scoped consent in Encina.Compliance.Consent (issue #1199).

CONTEXT:
- ConsentDiagnostics (Diagnostics/ConsentDiagnostics.cs) owns the ActivitySource, the Meter and the tag constants; Consent.Check is the activity.
- Existing logs use LoggerMessage.Define in Diagnostics/ConsentLogMessages.cs (EventIds up to 8268); #811 uses 8269-8274 and #810 uses 8275-8278.

TASK:
Add the three counters, the tag constants and the activity tags, and create ConsentChannelLogMessages with EventIds 8279-8287 as listed in the plan's Phase 6 Tasks. Call them from the evaluator, the behaviour and OptOutAsync.

KEY RULES:
- [LoggerMessage] source generator only, packed sequentially 8279-8287, all inside 8200-8299.
- No subject id, actor, basis reference, payload or EncinaError.Message in any log, tag or metric (AGENTS.md section 3, #1314).
- Run EncinaEventIdAllocationTests and the architecture tests.

REFERENCE FILES:
- src/Encina.Compliance.Consent/Diagnostics/ConsentDiagnostics.cs
- src/Encina.Compliance.Consent/Diagnostics/ConsentLogMessages.cs
- src/Encina/Diagnostics/EventIdRanges.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

---

### Phase 7: Testing

> **Goal**: Every flag reaches its target from the manifest; the S12 behaviours are proven.

<details>
<summary><strong>Tasks</strong></summary>

1. **Unit** (`tests/Encina.UnitTests/Compliance/Consent/Channels/`): `DefaultChannelConsentEvaluatorTests` (transactional passes with no record; commercial refused without channel consent; e-mail consent does not satisfy WhatsApp; art. 21.2 record accepted and `OptOutRequired`; expired and withdrawn records; basis not allowed on phone; service `Left` fails closed), `ConsentChannelOptionsTests`, `ConsentChannelAggregateTests`, `ConsentChannelProjectionTests`, behaviour tests for static and property channel and classification, unknown channel, missing classification as commercial, Warn and Disabled modes.
2. **Guard** (`tests/Encina.GuardTests/Compliance/Consent/`): constructors and public methods of the evaluator, `OptOutAsync`, `DefineChannel`.
3. **Contract** (`tests/Encina.ContractTests/Compliance/Consent/`): `IChannelConsentEvaluator` contract against the real default; `IConsentService` channel members.
4. **Property** (`tests/Encina.PropertyTests/Compliance/Consent/`): for every channel and any consent state, a `Transactional` message never requires marketing consent and a `Commercial` one always does.
5. **Integration** (`tests/Encina.IntegrationTests/Compliance/Consent/`, `[Collection(MartenCollection.Name)]`, PostgreSQL through Testcontainers): grant e-mail, refuse WhatsApp, opt out of an art. 21.2 record, two-tenant isolation, and the duplicate-active-record case that the follow-up issue covers.
6. **Telemetry/privacy**: an in-memory exporter test (AC-044) and a leak test (no subject id, actor or basis reference in tags, metrics or log messages), extending `ConsentPiiLeakTests`.
7. **Load and benchmark**: `.md` justifications in `tests/Encina.LoadTests/Compliance/Consent/` and `tests/Encina.BenchmarkTests/Encina.Benchmarks/Compliance/Consent/` (not concurrent or hot paths); never for unit, guard, contract or integration.
8. **Coverage manifest**: per-file `targets` and one-sentence `justifications` in `.github/coverage-manifest/Encina.Compliance.Consent.json` for every new or changed file (delegated to `mechanical-fixer`, targets demanding and realistic); measure each flag as AGENTS.md §9 describes and run the CRAP gate on the changed methods (`ConsentRequiredPipelineBehavior.Handle` is already long: extract the channel branch into a private method to keep CRAP at or below 10).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
You are implementing Phase 7 of channel-scoped consent in Encina.Compliance.Consent (issue #1199).

CONTEXT:
- Phases 1-6 are merged. Consent tests live in tests/Encina.{Unit,Guard,Contract,Property,Integration}Tests/Compliance/Consent/.
- Unit tests mock IAggregateRepository (NSubstitute); integration tests use [Collection(MartenCollection.Name)] with MartenFixture on PostgreSQL (see ConsentTenantScopingIntegrationTests.cs).
- Assertions use Shouldly through Encina.Testing.Shouldly; never FluentAssertions.

TASK:
Write the tests listed in the plan's Phase 7 Tasks. Tests must execute real package code.

KEY RULES:
- Use FakeTimeProvider; never Thread.Sleep; no swallowed exceptions.
- Property test: Transactional never needs marketing consent, Commercial always does, for every built-in channel.
- Include the two-tenant isolation test and the telemetry no-identifier test.
- Measure per-flag coverage against the manifest targets before reporting.

REFERENCE FILES:
- tests/Encina.UnitTests/Compliance/Consent/ConsentRequiredPipelineBehaviorTests.cs
- tests/Encina.IntegrationTests/Compliance/Consent/ConsentTenantScopingIntegrationTests.cs
- tests/Encina.PropertyTests/Compliance/Consent/ConsentValidationPropertyTests.cs
- .github/coverage-manifest/Encina.Compliance.Consent.json
```

</details>

---

### Phase 8: Documentation and Finalization

> **Goal**: Users and the specification describe the channel model; the package builds clean.

<details>
<summary><strong>Tasks</strong></summary>

1. XML docs on every new public API (examples for the reminder and promotion commands).
2. `changelog.d/1199-consent-channel.added.md` (fragment text decided by the worker, written by `mechanical-fixer`).
3. `src/Encina.Compliance.Consent/README.md` and `docs/features/consent-management.md` (written by `docs-writer`): channels, the art. 21.2 basis, the classification, the opt-out contract for P-33.
4. The Consent specification tables (GDPR, ePrivacy Art. 5(3), LSSI arts. 20-22): added to the specification that #1209 creates; if #1209 has not landed, a "Channels and LSSI" section goes into `docs/features/consent-management.md` and moves with #1209. Every LSSI statement marked [I] in the issue is verified against the law text before it is written.
5. `PublicAPI.Unshipped.txt`, `docs/INVENTORY.md` if it lists the package files.
6. Verification: `dotnet build Encina.slnx --configuration Release` (0 warnings); `dotnet test` for the five Consent test projects; EventId architecture tests; `dotnet run --file .github/scripts/changelog-fragments.cs -- --check`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```
You are finalising channel-scoped consent in Encina.Compliance.Consent (issue #1199).

CONTEXT:
- Phases 1-7 are merged. Documentation pages and READMEs go to the docs-writer; changelog fragments and PublicAPI lines to the mechanical-fixer.

TASK:
Complete the plan's Phase 8 Tasks and run the verification commands.

KEY RULES:
- Documentation never types coverage figures by hand (covref markers only).
- No legal statement marked [I] in the issue is published before it is checked against the law.
- Zero build warnings; PublicAPI.Unshipped.txt complete.

REFERENCE FILES:
- src/Encina.Compliance.Consent/README.md
- docs/features/consent-management.md
- changelog.d/README.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Provision | Relevance |
|----------|-----------|-----------|
| LSSI (Ley 34/2002) | art. 21.1 and 21.2 | Commercial electronic communications need prior consent, except to existing clients for similar products with a free, easy opt-out in every message |
| LSSI | art. 22.1 and Annex (f) | Opt-out right; definition of commercial communication (the reminder-is-not-commercial reading is [I]) |
| ePrivacy Directive 2002/58/EC | Art. 13 and Art. 5(3) | Unsolicited communications; consent for terminal storage (the `terminal-storage` channel) |
| GDPR | Art. 6(1)(a), 7(1), 7(3) | Consent must be demonstrable and withdrawable as easily as given (per channel) |
| WhatsApp Business Messaging Policy | opt-in requirement | WhatsApp needs channel-specific opt-in (cited in the issue as [V]) |
| SPEC-002 | REQ-014, AC-014, S12, DEC-006, DEC-008, REQ-061, REQ-062 | Requirement, acceptance, fail closed, Marten-only, tenancy, instrumentation |
| ADR-018, ADR-019, ADR-021, ADR-001 | cross-cutting check, Marten, EventIds, ROP | Design rules followed |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `ConsentAggregate`, events, projection, read model | `src/Encina.Compliance.Consent/Aggregates/`, `Events/`, `ReadModels/` | Carry channel, basis and reference |
| `DefaultConsentService` tenant scope and cache | `Services/DefaultConsentService.cs` (`TryResolveTenantScope`, tenant cache key) | Channel in the key; tenant isolation reused |
| `ConsentRequiredPipelineBehavior` caches | `ConsentRequiredPipelineBehavior.cs` (`AttributeCache`, `PropertyCache`) | Channel and classification lookup |
| `ConsentOptions.DefinePurpose` and validator | `ConsentOptions.cs`, `ConsentOptionsValidator.cs` | Style for `DefineChannel` |
| `ConsentDiagnostics`, `ConsentLogMessages` | `Diagnostics/` | Instruments and EventId conventions |
| `EncinaEventIdAllocationTests` | `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs:63` | Assembly already mapped |
| `MartenFixture` and `MartenCollection` | `tests/Encina.IntegrationTests/Infrastructure/Marten/Fixtures/` | Integration tests |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Compliance.Consent` (existing) | 8200-8299 (`EventIdRanges.ComplianceConsent`) | Used: 8200-8207, 8230-8232, 8240-8243, 8250, 8260-8268 |
| #811 exemptions | 8269-8274 | Reserved by its plan |
| #810 cooldown | 8275-8278 | Reserved by its plan (the #1197 plan also names 8275-8276: that overlap is to be reconciled by the maintainer's ordering) |
| **This plan (#1199)** | **8279-8287** | Packed after 8278; new `ConsentChannelLogMessages` |
| #1255 minors | 8288-8293 | Reserved by its plan |
| Left in the range | 8294-8299 | A further Consent feature needs a second range |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| New source | 12 | Model (3), abstractions (1), evaluator, decision, log messages, definition, errors additions |
| Modified source | 14 | Events, aggregate, read model, projection, service, interface, options, validator, hosted service, attribute, behaviour, diagnostics, registration, PublicAPI |
| Tests | 14 | Unit 6, guard 1, contract 1, property 1, integration 1, leak 1, justifications 2, manifest |
| Documentation | 5 | README, feature guide, specification tables, changelog fragment, PublicAPI |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Combined prompt — all phases</strong></summary>

```
PROJECT CONTEXT:
Encina is a pre-1.0 .NET 10 library (no backward compatibility, ROP with Either<EncinaError, T>, TimeProvider for time, tenant-scoped reads). Encina.Compliance.Consent is event-sourced on Marten (PostgreSQL). Issue #1199 makes consent specific to a purpose and a channel, records the LSSI art. 21.2 existing-client basis, and lets requests be marked Transactional or Commercial.

IMPLEMENTATION OVERVIEW:
1. Add ConsentChannels, ConsentBasis, MessageClassification; carry Channel, Basis, BasisReference through ConsentGranted, the aggregate, read model and projection.
2. Add the channel to every IConsentService lookup and cache key; HasValidConsentAsync counts only Basis == Consent; add OptOutAsync.
3. Add ChannelDefinitions, DefineChannel, validator rules, attribute properties and startup attribute checks.
4. Add IChannelConsentEvaluator and DefaultChannelConsentEvaluator; call it from ConsentRequiredPipelineBehavior when a channel is declared.
5. Verify tenant scoping and audit; add counters, activity tags and EventIds 8279-8287.
6. Write unit, guard, contract, property and Marten integration tests; coverage manifest targets.
7. Document, changelog fragment, PublicAPI.

KEY PATTERNS:
- Static per-generic-type attribute caching in the behaviour; reflection by property name like SubjectIdProperty.
- A null channel never satisfies a channel requirement; unclassified means Commercial; unknown channel is a coded error.
- No subject id, actor, basis reference or EncinaError.Message in logs, tags or metrics; [LoggerMessage] with registered EventIds.
- TryAdd registrations; DI test with ValidateOnBuild and ValidateScopes.

REFERENCE FILES:
- src/Encina.Compliance.Consent/ (Events, Aggregates, ReadModels, Services, ConsentRequiredPipelineBehavior.cs, ConsentOptions.cs)
- tests/Encina.IntegrationTests/Compliance/Consent/ConsentTenantScopingIntegrationTests.cs
- docs/plans/consent-exempt-purposes-implementation-plan-811.md (style and shared files)
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | No read path that benefits from caching; consent is a mutable permission, the existing 5-minute service cache only gains the channel in its key (Phase 2) |
| 2 | OpenTelemetry | ✅ | Counters and activity tags with the tenant as attribute, no identifiers (Phase 6) |
| 3 | Structured Logging | ✅ | `[LoggerMessage]` EventIds 8279-8287 in `ComplianceConsent` (ADR-021, Phase 6) |
| 4 | Health Checks | ❌ N/A | No new checkable dependency; `ConsentHealthCheck` is unchanged |
| 5 | Validation | ✅ | Channel, basis and classification validated by the options validator, the service and the behaviour (Phases 2-4) |
| 6 | Resilience | ❌ N/A | No call to an external system |
| 7 | Distributed Locks | ❌ N/A | No shared state updated by concurrent hosts; duplicate active records are a separate issue |
| 8 | Transactions | ❌ N/A | Single-aggregate writes |
| 9 | Idempotency | ❌ N/A | Not a message or request entry point; grant idempotency is the follow-up issue |
| 10 | Multi-Tenancy | ✅ | Tenant-aware per SPEC-002 REQ-061 through the service's tenant scope (Phase 5); fixed default tenant when tenancy is off |
| 11 | Module Isolation | ❌ N/A | SPEC-002 requires no module scoping; `ModuleId` in messaging stays with #747 |
| 12 | Audit Trail | ✅ | Grant (channel, basis) and withdrawal (opt-out) are audited events (Phase 5) |

---

## Prerequisites & Dependencies

### Blocking

- None. The feature adds fields and a service; it does not need another feature first. The Consent specification tables need #1209 (see Phase 8).

### Coordination

- **Same-file changes**: #811, #810, #1197 and #1255 edit `ConsentGranted`, `ConsentAggregate.Grant`, `ConsentReadModel`, `IConsentService`, `DefaultConsentService` and the behaviour. Land them one after another and rebase; parameter order in `ConsentGranted` is fixed by the first to land, the others append. Recommended order for the persisted shape: #1197 (representation), #1199, #1255, because #1255 reuses #1197's type and none of them reads the others' fields.
- **EventIds**: this plan 8279-8287, #1255 8288-8293; the #810 plan (8275-8278) and the #1197 plan (8275-8276) overlap and need one owner for 8275-8276.
- **P-33 (`Encina.Notifications`)** consumes `IChannelConsentEvaluator` and must render the opt-out; S12 (#1227) verifies the whole path.

### Deferred

- Grant idempotency per (tenant, subject, purpose, channel): follow-up issue file `artifacts/issues/consent-grant-duplicate-active-records.md` (bug: a second grant for the same key creates a second active aggregate).
- Deriving the applicable channel rules from the jurisdiction (#686 resolver): not needed for 1.0 of this feature; the default is Spain/EU.

---

## Next Steps

1. Maintainer answers the two Dilemmas (Design Choices 2 and 3) when work on #1199 starts; their answers go in a `## Maintainer Decisions` section appended to this plan.
2. Open the follow-up issue for duplicate active consent records.
3. Brief the worker for PR 1 (Phases 1-2) after #1197's shape has landed or its parameter order is agreed.
4. Implement PR 2 and PR 3; update the Consent specification with #1209.
