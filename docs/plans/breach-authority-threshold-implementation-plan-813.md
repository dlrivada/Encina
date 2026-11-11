# Implementation Plan: Authority notification severity threshold for `Encina.Compliance.BreachNotification` (Digital Omnibus, COM(2025) 837)

> **Issue**: [#813](https://github.com/dlrivada/Encina/issues/813)
> **Type**: Feature
> **Milestone**: v0.15.0 — EU Compliance: NIS2 & Digital Omnibus
> **Complexity**: Low-Medium (5 phases, Marten module, no new store, ~8 production files, 1 new range-free log block)
> **Estimated Scope**: ~350-500 lines of production code + ~900-1,200 lines of tests

---

## Summary

Add an **independent authority notification severity threshold** to `Encina.Compliance.BreachNotification`: `BreachNotificationOptions.AuthorityNotificationSeverityThreshold`, evaluated by a replaceable policy, `IBreachAuthorityNotificationPolicy`, read at call time so it can change at run time. The Digital Omnibus proposal COM(2025) 837 would limit notification of the supervisory authority to breaches with a "high risk" to the rights and freedoms of natural persons (SPEC-002 §3.5: GDPR Art. 33(1) as amended). **This is draft law.** Under SPEC-002 REQ-024 and DEC-012 the stricter threshold is off by default: the default stays the current-law reading of Art. 33(1) ("unless unlikely to result in a risk", which Encina maps to `BreachSeverity.Medium` and above), the draft behaviour sits behind an option named after its draft article, and a test asserts that the default follows current law (AC-024).

### Standards covered

- GDPR Art. 33(1): notify the supervisory authority within 72 hours unless the breach "is unlikely to result in a risk to the rights and freedoms of natural persons". Current-law default.
- GDPR Art. 33(5): document every breach, including those not notified. Relevant to the deferred audit item (Design Choice 5 and the issue file listed in Prerequisites).
- GDPR Art. 34(1): communication to data subjects at "high risk". The subject threshold already exists (`SubjectNotificationSeverityThreshold`).
- Digital Omnibus COM(2025) 837 (draft): "high risk" threshold for authority notification, per SPEC-002 §3.5 (GDPR Art. 33(1) as amended). **Unverified** against the published text of the proposal: the draft article number, the final wording and any change to the 72-hour deadline (SPEC-002 §3.5 mentions 96 hours) are taken from SPEC-002, not from the Official Journal. The procedure 2025/0360(COD) is still in committee according to SPEC-002 §3.5, and nothing in this plan treats the proposal as adopted.

### What already exists

- `src/Encina.Compliance.BreachNotification/BreachNotificationOptions.cs:181` declares `SubjectNotificationSeverityThreshold` (default `High`). `:150` declares `AutoNotifyOnHighSeverity` (default `false`). A search of `src/` shows that **neither is read anywhere except the options validator** (`BreachNotificationOptionsValidator.cs:83` checks that the subject threshold is a defined enum value). No code selects breaches for authority or subject notification by severity today.
- Authority notification is manual: `Services/DefaultBreachNotificationService.cs:165` (`ReportToDPAAsync`) only records on the aggregate that a report was made. It applies no severity test.
- The "Medium and above" reading in the issue comes from the XML documentation of `Model/BreachSeverity.cs` (the `Medium` remark: "Supervisory authority notification is required under Art. 33"; `Low`: "may not be required"). It is documentation, not behaviour.
- `BreachDeadlineMonitorService.cs` (singleton hosted service, `IOptions<BreachNotificationOptions>` captured at construction, lines 65 and 75) logs and publishes deadline warnings for every breach returned by `Services/DefaultBreachNotificationService.cs:468` (`GetApproachingDeadlineBreachesAsync`: not yet reported to the DPA, not closed, deadline within 24 hours). The query has **no severity filter**, so a `Low` breach that was never reported is logged as overdue.
- `Health/BreachNotificationHealthCheck.cs:199-215` counts overdue breaches from the same query, again for any severity.
- Plan #812 (`docs/plans/enisa-single-entry-point-implementation-plan-812.md`) adds the reporting layer. It states that its reporting service "applies no severity filter" and that the threshold is this issue's option. It also removes `SupervisoryAuthority` and `AutoNotifyOnHighSeverity` from the options. This plan therefore does not depend on `AutoNotifyOnHighSeverity`.
- No code in `src/` uses `IOptionsMonitor` yet (#817 is open). #812 plans the first use, in its route resolver.

### Corrections to the issue body

- "Authority notification happens for Medium+ severity": no behaviour implements that today (see above). The plan creates the one place where the rule lives and makes the deadline monitor and the health check consult it.
- "Update `BreachDeadlineMonitorService` to use the new threshold": the monitor does not decide whether to notify. It is a consumer of the policy (Phase 2), together with the health check.
- "`options.AuthorityNotificationSeverityThreshold = BreachSeverity.High; // New: Omnibus default`" contradicts the same issue's "default remains Medium". SPEC-002 AC-024 settles it: the default is `Medium`; `High` is reachable only through the Omnibus option (Design Choice 3).
- "Default matches current GDPR (backward compatible)": the project is pre-1.0 (AGENTS.md §1) and keeps no compatibility layer. The criterion is read as "the default follows current law", which is what AC-024 tests.
- "GuardTests: new property validation": guard tests cover null arguments of public methods and constructors (AGENTS.md §9). Property validation is covered by unit and property tests.
- "Hot-swappable via `IOptionsMonitor`": the policy reads `IOptionsMonitor<BreachNotificationOptions>.CurrentValue` per call. Migrating the existing `IOptions` consumers is #817, not this issue.

### Affected packages

- `Encina.Compliance.BreachNotification`: options, validator, new policy, monitor service, health check, DI, diagnostics.
- Tests: `Encina.UnitTests`, `Encina.GuardTests`, `Encina.ContractTests`, `Encina.PropertyTests`; `.md` justifications for integration, load and benchmark.

### Provider category

None. The feature adds an options property and a pure decision over `BreachSeverity`. It touches no store and no provider-specific code (AGENTS.md §5).

---

## Design Choices

<details>
<summary><strong>1. Where the rule lives — a replaceable <code>IBreachAuthorityNotificationPolicy</code></strong></summary>

**Class:** Trivial
**Settled by:** AGENTS.md section 3 (registration completeness: default services registered with `TryAdd` so the application can replace them); precedent in plan #812 (`IIncidentReportingDeadlinePolicy`, a replaceable policy with an options-driven default)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Interface `IBreachAuthorityNotificationPolicy` with `DefaultBreachAuthorityNotificationPolicy`, registered with `TryAdd`** | One place for the rule; monitor, health check and (later) the #812 service share it; an application can replace it; easy to test | One more public type |
| **B) Static helper on `BreachNotificationOptions` (`options.RequiresAuthorityNotification(severity)`)** | Smallest change | Every consumer needs the options instance; no replacement point; a static method on an options class mixes data and behaviour |
| **C) Inline comparison in each consumer** | No new type | Three copies of the rule that must agree; the exact drift this issue is about |

### Chosen Option: **A — `IBreachAuthorityNotificationPolicy` with a default implementation**

### Rationale

- The rule has three consumers already (monitor, health check, and #812's reporting service when it lands). A single policy keeps them consistent.
- The default implementation takes `IOptionsMonitor<BreachNotificationOptions>`, which is what makes hot-swap possible without changing the consumers.

</details>

<details>
<summary><strong>2. The decision is evaluated at read time, never stored</strong></summary>

**Class:** Settled
**Settled by:** the issue's requirement "Hot-swappable: must work with IOptionsMonitor (#817)"; GDPR Art. 33(1) ("after having become aware of it": the 72-hour clock starts at awareness, not at classification); SPEC-002 DEC-012 (the persisted shape of #813 is "one options property")

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Compute `AuthorityNotificationRequired` from the breach's current `Severity` and the threshold in force, on every call; persist nothing** | Hot-swap works; a severity upgrade or a threshold change takes effect at once; no new event or read-model field | The historical reason for "not notified" is not on the stream (deferred, see Design Choice 5) |
| **B) Persist the decision as an event when a breach is assessed** | Self-documenting stream (Art. 33(5)) | A hot-swapped threshold leaves old decisions stale; changes the persisted shape for an additive feature; DEC-012 classes #813 as additive and cheap |

### Chosen Option: **A — Evaluate at read time from `BreachReadModel.Severity`**

### Rationale

- The deadline stays anchored to awareness: `BreachReadModel.DeadlineUtc` is computed at detection and is not touched. A breach that is `Medium` at hour 1 and upgraded to `High` at hour 70 still has its original deadline. Only the question "does this breach need an authority report" changes.
- Option B's benefit is real and is kept as a separate, deferred issue (Design Choice 5), so this issue stays additive.

</details>

<details>
<summary><strong>3. Off by default — separate Omnibus option, named after the draft article, guarding the threshold</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `AuthorityNotificationSeverityThreshold` (default `Medium`) plus `EnableOmnibusArticle33HighRiskThreshold` (default `false`); the validator fails closed when the threshold is above `Medium` and the flag is off** | Follows the precedent `DataSubjectRightsOptions.EnableOmnibusArticle12RefusalGrounds` (`src/Encina.Compliance.DataSubjectRights/DataSubjectRightsOptions.cs:96`); a stricter threshold cannot be set by mistake under current law; XML docs name the draft article (AC-024) | Two settings to reach the draft behaviour |
| **B) Only the property, default `Medium`; documentation says not to raise it before adoption** | One setting | Nothing stops an application from silently under-reporting under current law; AGENTS.md §3 asks gates to fail closed |
| **C) One profile enum (`BreachRegulatoryProfile.CurrentLaw` / `DigitalOmnibusDraft`) that sets threshold, deadline and #812's single entry point together** | One switch for the whole Omnibus | Couples three issues' options; the 96-hour deadline and the SEP are other issues' behaviours; the final text is unknown; a profile that mixes them is a guess |

### Chosen Option: **A — Threshold plus `EnableOmnibusArticle33HighRiskThreshold`, validator fails closed** (recommended, pending the maintainer)

### Rationale

- I recommend A because under current law, ignoring a `Medium` breach is a violation, so the library should not let that happen by a single property assignment. The flag is the explicit, logged opt-in (AGENTS.md §3, "the only opt-out is explicit").
- C wins only if the maintainer wants one Omnibus switch for all of #812, #813 and a future deadline option once the text is adopted. It can be added later without changing A's properties, because a profile would only set them.
- The flag name embeds "Article33"; the numbering comes from SPEC-002 §3.5 and is **unverified** against the Official Journal. If the adopted numbering differs, the rename is a pre-1.0 change.

</details>

<details>
<summary><strong>4. What the threshold governs — obligations and alerts, never a refusal to report</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The policy governs *obligation*: deadline warnings, overdue counts and health. `ReportToDPAAsync` and the #812 reporting service still accept any breach (voluntary reporting)** | A mis-assessed severity can never block a report; over-reporting is not forbidden by Art. 33; matches #812, which "applies no severity filter" | A below-threshold breach can still reach the authority |
| **B) Also refuse `ReportToDPAAsync` for a below-threshold breach with a new error code** | The configured regime is enforced end to end | A wrong severity assessment blocks a report the controller wants to make; a refusal in a compliance path needs its own error code and tests; contradicts #812's plan |

### Chosen Option: **A — Obligations and alerts only** (recommended, pending the maintainer)

### Rationale

- I recommend A because the harm of a refused report (a breach the controller wanted to notify but could not) is larger than the harm of a voluntary one. B wins for an application that must demonstrate to an auditor that nothing below its threshold ever reaches the authority.
- With A, the consumers change as follows: `BreachDeadlineMonitorService` skips below-threshold breaches for deadline warnings and overdue errors (it logs one `Debug`-level "not required" line per cycle count, no per-breach noise); `BreachNotificationHealthCheck` counts only breaches that require notification as overdue.

</details>

<details>
<summary><strong>5. Recording the decision on the breach stream — deferred</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Defer to a separate `[FEATURE]` issue; this plan persists nothing** | Keeps #813 additive and cheap (DEC-012); one PR | Until the follow-up, the reason "not notified because below the threshold in force" is not on the stream (GDPR Art. 33(5) asks for documentation of every breach) |
| **B) Add a `BreachAuthorityNotificationAssessed` event now, written when severity is assessed** | Documentation complete from the start | Changes the persisted shape of the breach stream in a PR whose scope is an options property; overlaps the events #812 adds (`BreachReportSubmitted`); the event must carry the threshold in force, which makes tests on hot-swap heavier |

### Chosen Option: **A — Defer, with an issue file** (recommended, pending the maintainer)

### Rationale

- I recommend A because #812 is adding events to the same aggregate; one coordinated design of "decision and submission events" is cheaper than two. B wins if the maintainer wants Art. 33(5) completeness to ship in the same release as the threshold.
- The issue file is `artifacts/issues/plan-813-breach-authority-decision-event.md` (see Prerequisites).

</details>

<details>
<summary><strong>6. Validation: defined values and the ordering invariant, fail closed</strong></summary>

**Class:** Settled
**Settled by:** AGENTS.md section 3 (compliance and security gates fail closed; options validated at start); GDPR Art. 33(1) and Art. 34(1) (a breach that is high risk is also a breach likely to result in a risk, so the authority threshold cannot be stricter than the subject threshold)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `BreachNotificationOptionsValidator` rejects: an undefined enum value; `AuthorityNotificationSeverityThreshold` above `SubjectNotificationSeverityThreshold`; a threshold above `Medium` while `EnableOmnibusArticle33HighRiskThreshold` is `false`** | Misconfiguration fails at start; matches the issue's property test "authority <= subject" | None significant |
| **B) Clamp silently at read time** | Never throws | Hides a configuration error in a compliance setting |

### Chosen Option: **A — Validate and fail**

### Rationale

- The rules are three, each testable alone. The same validator runs when `IOptionsMonitor` builds a new snapshot (Design Choice 7).

</details>

<details>
<summary><strong>7. Hot-swap and failure behaviour of the policy</strong></summary>

**Class:** Settled
**Settled by:** AGENTS.md section 3 (compliance gates fail closed: on a failed lookup the gate takes the protective side, which here is notifying more); the issue ("Hot-swappable via IOptionsMonitor"); #817 (migration of the other consumers)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The default policy reads `IOptionsMonitor.CurrentValue` on each call; if reading throws `OptionsValidationException` it logs the error code and returns the current-law threshold (`Medium`)** | Takes effect without a restart; an invalid reload cannot silence notifications | Each call touches the monitor (cheap: a cached snapshot) |
| **B) Cache the value at construction (as the monitor service does today)** | No monitor use | Not hot-swappable; fails the acceptance criterion |
| **C) Let the exception propagate** | No special code | The monitor cycle catches and logs it but then skips the whole cycle, so deadline warnings stop for all breaches |

### Chosen Option: **A — Per-call read with a protective fallback**

### Rationale

- A configuration reload that fails validation must not turn into "no breach needs notification". The fallback is the lowest threshold current law allows. Logged by error code only, never with the option values or exception message (AGENTS.md §3).

</details>

<details>
<summary><strong>8. Per-tenant thresholds — not now, but the contract carries the tenant</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One global threshold; `IBreachAuthorityNotificationPolicy.IsAuthorityNotificationRequired(BreachSeverity severity, string? tenantId = null)` already takes the tenant and ignores it** | Smallest change; adding per-tenant overrides later is not a signature change; the legal regime is a property of the deployment | The tenant argument is unused today |
| **B) `TenantAuthorityNotificationSeverityThresholds` dictionary override now** | Satisfies SPEC-002 REQ-061 literally ("tenant-aware") | More options, validation and tests for a case with no known user; two tenants of one deployment are under the same law |
| **C) Global threshold and no tenant parameter** | Simplest contract | A per-tenant override later would be an interface break after 1.0 |

### Chosen Option: **A — Global value, tenant-aware signature** (recommended, pending the maintainer)

### Rationale

- I recommend A because it keeps the contract stable at no cost. B wins for a hosting provider that runs controllers under different regimes in one deployment (for example a controller outside the EU that applies only its own rules); then the dictionary is an additive change behind the same interface.

</details>

---

## Implementation Phases

### Phase 1: Options and Policy

> **Goal**: The setting, the Omnibus flag and the policy, with the current-law default.

<details>
<summary><strong>Tasks</strong></summary>

All paths are in `src/Encina.Compliance.BreachNotification/`.

1. `BreachNotificationOptions.cs` (modify): add `public BreachSeverity AuthorityNotificationSeverityThreshold { get; set; } = BreachSeverity.Medium;` placed next to `SubjectNotificationSeverityThreshold` (line 181) and `public bool EnableOmnibusArticle33HighRiskThreshold { get; set; }` (default `false`). XML docs name the draft: "Digital Omnibus COM(2025) 837 (draft), GDPR Art. 33(1) as amended per SPEC-002 §3.5; off by default under SPEC-002 REQ-024". Update the `<example>` block.
2. `Abstractions/IBreachAuthorityNotificationPolicy.cs` (new, public): `bool IsAuthorityNotificationRequired(BreachSeverity severity, string? tenantId = null);` and `BreachSeverity GetEffectiveThreshold(string? tenantId = null);`.
3. `Services/DefaultBreachAuthorityNotificationPolicy.cs` (new, public sealed): constructor `(IOptionsMonitor<BreachNotificationOptions> options, ILogger<DefaultBreachAuthorityNotificationPolicy> logger)`. `GetEffectiveThreshold` returns `options.CurrentValue.AuthorityNotificationSeverityThreshold`; on `OptionsValidationException` logs by error code and returns `BreachSeverity.Medium` (Design Choice 7). `IsAuthorityNotificationRequired` returns `severity >= GetEffectiveThreshold(tenantId)`. Both methods are straight comparisons: complexity 2 and 1.
4. `Model/BreachSeverity.cs` (modify): XML docs for `Low` and `Medium` say the authority-notification statement is the current-law default and refer to the policy.
5. `PublicAPI.Unshipped.txt`: the two properties, the interface (two members), the default policy (type, constructor, two members). Written by `mechanical-fixer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
Issue #813, plan docs/plans/breach-authority-threshold-implementation-plan-813.md. Package
src/Encina.Compliance.BreachNotification (event-sourced on Marten, ADR-019). Today
SubjectNotificationSeverityThreshold and AutoNotifyOnHighSeverity are never read outside the validator.
The Digital Omnibus COM(2025) 837 is draft law: the behaviour is off by default (SPEC-002 REQ-024, AC-024).

TASK:
1. Add AuthorityNotificationSeverityThreshold (default BreachSeverity.Medium) and
   EnableOmnibusArticle33HighRiskThreshold (default false) to BreachNotificationOptions, with XML docs that
   name the draft article and say "off by default".
2. Create IBreachAuthorityNotificationPolicy and DefaultBreachAuthorityNotificationPolicy as in the Tasks list.
   The default reads IOptionsMonitor<BreachNotificationOptions>.CurrentValue per call; on
   OptionsValidationException it logs the error code only and returns BreachSeverity.Medium.
3. Update the BreachSeverity XML docs.

KEY RULES:
- No exception message or option value in logs (AGENTS.md section 3).
- Time is not involved; do not add DateTime.UtcNow.
- No [Obsolete], no aliases. XML docs on every public member.
- Add PublicAPI lines (RS0016) for every new public symbol.

REFERENCE FILES:
- src/Encina.Compliance.BreachNotification/BreachNotificationOptions.cs
- src/Encina.Compliance.DataSubjectRights/DataSubjectRightsOptions.cs (line 96, the Omnibus precedent)
- src/Encina.Compliance.BreachNotification/Services/DefaultBreachNotificationService.cs (style)
```

</details>

### Phase 2: Validation, Consumers and DI

> **Goal**: The validator enforces Design Choice 6; the monitor and the health check consult the policy.

<details>
<summary><strong>Tasks</strong></summary>

1. `BreachNotificationOptionsValidator.cs` (modify): after the subject-threshold check (line 83) add three failures: `AuthorityNotificationSeverityThreshold` not a defined enum value; authority threshold greater than `SubjectNotificationSeverityThreshold`; authority threshold greater than `Medium` while `EnableOmnibusArticle33HighRiskThreshold` is `false`. Update the class XML docs list. Keep the method's complexity down by extracting `ValidateAuthorityThreshold(options, failures)`.
2. `BreachDeadlineMonitorService.cs` (modify): inject `IBreachAuthorityNotificationPolicy` through the scope (the service is a singleton, the policy is a singleton, so constructor injection is fine). In `ExecuteMonitoringCycleAsync` (lines 99-156) filter `approaching` through a private method `SplitByAuthorityRequirement(IReadOnlyList<BreachReadModel>)` returning the breaches that require notification; below-threshold breaches are counted, not warned about. Keep `PublishDeadlineWarningsAsync` for the filtered list only.
3. `Health/BreachNotificationHealthCheck.cs` (modify, lines 199-215): count overdue and approaching breaches only for breaches that require authority notification; add `data["belowThresholdBreachCount"]`. The health result values contain counts only, never breach content.
4. `ServiceCollectionExtensions.cs` (modify, near line 90): `services.TryAddSingleton<IBreachAuthorityNotificationPolicy, DefaultBreachAuthorityNotificationPolicy>();`. Update the class XML docs list of registrations. `IOptionsMonitor<>` is registered by `AddOptions`, which the existing `ConfigureOptions` already calls through `services.Configure`; the DI test proves it.
5. `Services/DefaultBreachNotificationService.cs`: **no change** to `GetApproachingDeadlineBreachesAsync` or `ReportToDPAAsync` (Design Choice 4).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
Phase 1 of plan #813 added the options and IBreachAuthorityNotificationPolicy. The deadline monitor
(BreachDeadlineMonitorService, singleton hosted service, PeriodicTimer loop) and the health check treat every
unreported breach as owing a notification.

TASK:
1. Extend BreachNotificationOptionsValidator with the three failures of Design Choice 6.
2. Make BreachDeadlineMonitorService and BreachNotificationHealthCheck consult the policy so that only breaches
   with IsAuthorityNotificationRequired(breach.Severity, breach.TenantId) == true produce deadline warnings,
   overdue errors and overdue counts.
3. Register the policy with TryAddSingleton.

KEY RULES:
- Background loop: the existing catch-per-iteration stays; nothing new may escape (AGENTS.md section 3, #2774).
- Do not change GetApproachingDeadlineBreachesAsync or ReportToDPAAsync (voluntary reports stay possible).
- CRAP <= 10 on every method you add or change: extract helpers instead of nesting.
- Logs carry error codes and counts only.

REFERENCE FILES:
- src/Encina.Compliance.BreachNotification/BreachDeadlineMonitorService.cs
- src/Encina.Compliance.BreachNotification/Health/BreachNotificationHealthCheck.cs
- src/Encina.Compliance.BreachNotification/BreachNotificationOptionsValidator.cs
- src/Encina.Compliance.BreachNotification/ServiceCollectionExtensions.cs
```

</details>

### Phase 3: Observability

> **Goal**: Log messages in the existing range and one counter.

<details>
<summary><strong>Tasks</strong></summary>

1. `Diagnostics/BreachNotificationLogMessages.cs` (modify): three `[LoggerMessage]` entries with EventIds **8792-8794** inside the existing range `ComplianceBreachNotification` (8700-8799, highest used 8791): 8792 policy options invalid (fallback to current law, error code only); 8793 below-threshold breaches skipped in a monitor cycle (count); 8794 Omnibus threshold active (logged once at start-up when `EnableOmnibusArticle33HighRiskThreshold` is `true`). No new range is registered, so `EventIdRanges.cs` and `EncinaEventIdAllocationTests` do not change.
2. `Diagnostics/BreachNotificationDiagnostics.cs` (modify): counter `breach.authority_notification.below_threshold.total` on the package `Meter`, tag `breach.severity` only.
3. XML docs: `/// Event IDs: 8792-8794 (see EventIdRanges.ComplianceBreachNotification)`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
Plan #813, Phase 3. EventIds are registered per ADR-021 in src/Encina/Diagnostics/EventIdRanges.cs. The
BreachNotification range 8700-8799 has ids up to 8791 in use. Plan #812 uses a new range 9700-9749 and does not
touch 8792-8799.

TASK:
Add EventIds 8792-8794 (packed, no gaps) to BreachNotificationLogMessages using the [LoggerMessage] source
generator, and the below-threshold counter to BreachNotificationDiagnostics.

KEY RULES:
- No EventId outside 8700-8799. Packed sequentially.
- Never log EncinaError.Message or an exception message; use the error code or ex.ForLogging().
- The counter tag is the severity name only; no breach id, no tenant data.
- Run the architecture test EncinaEventIdAllocationTests after the change.

REFERENCE FILES:
- src/Encina.Compliance.BreachNotification/Diagnostics/BreachNotificationLogMessages.cs
- src/Encina.Compliance.BreachNotification/Diagnostics/BreachNotificationDiagnostics.cs
- src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

### Phase 4: Testing

> **Goal**: Every flag reaches its target; AC-024 is proven.

<details>
<summary><strong>Tasks</strong></summary>

1. **Unit** (`tests/Encina.UnitTests/Compliance/BreachNotification/`): `DefaultBreachAuthorityNotificationPolicyTests` (each severity against each threshold; fallback to `Medium` on an invalid reload; the **AC-024 test**: default options mean `Medium` and a `Low` breach is not required while `Medium`, `High` and `Critical` are); `BreachNotificationOptionsTests` (defaults: threshold `Medium`, flag `false`); `BreachNotificationOptionsValidatorTests` (the three failures and the valid combinations); `BreachDeadlineMonitorServiceTests` (with `FakeTimeProvider`: a `Low` breach produces no warning, a `Medium` one does under the default, and not under `High`; a threshold change through a test `IOptionsMonitor` takes effect on the next cycle); `BreachNotificationHealthCheckTests` (below-threshold breach not counted as overdue); a DI test building the provider with `ValidateOnBuild` and `ValidateScopes` that resolves `IBreachAuthorityNotificationPolicy`.
2. **Guard** (`Encina.GuardTests`): constructor of the default policy (null monitor, null logger); the monitor's new constructor parameter.
3. **Contract** (`Encina.ContractTests`): `IBreachAuthorityNotificationPolicy` contract base run against the default implementation (monotonic in severity; tenant argument accepted).
4. **Property** (`Encina.PropertyTests`, FsCheck through `Encina.Testing.FsCheck`): monotonicity (if `s1 <= s2` and `s1` is required then `s2` is required, for every threshold); the validator accepts exactly the combinations with `authority <= subject` and, without the flag, `authority <= Medium`.
5. **Integration**: justification file `tests/Encina.IntegrationTests/Compliance/BreachNotification/AuthorityThreshold.md` (no persistence or provider code is touched; the policy is pure). **Load** and **Benchmark**: `.md` justifications (pure comparison, no concurrency).
6. **Per-flag targets** for the touched files (AGENTS.md §9). Baselines are not measured by this plan; the worker measures them with the commands in AGENTS.md §9 and sets each target at or above the baseline (targets never go down). Planned targets: new `DefaultBreachAuthorityNotificationPolicy.cs`: unit 95, guard 100, contract 100, property 100 (two tiny methods, every branch reachable by a pure test); `IBreachAuthorityNotificationPolicy.cs`: contract 100; `BreachNotificationOptions.cs`: unit 100 (property defaults); `BreachNotificationOptionsValidator.cs`: unit 95, guard 100, property 90 (three added rules, all pure); `BreachDeadlineMonitorService.cs` and `Health/BreachNotificationHealthCheck.cs`: unit at least the baseline plus the new branches covered.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT:
Plan #813, Phase 4. Tests must execute real package code (AGENTS.md section 9). Assertions use Shouldly through
Encina.Testing.Shouldly; property tests use Encina.Testing.FsCheck. Event-sourced compliance modules have no
InMemory stores: mock IBreachNotificationService with NSubstitute.

TASK:
Write the unit, guard, contract and property tests listed in the Tasks block, the DI test with ValidateOnBuild
and ValidateScopes, and the .md justifications for integration, load and benchmark.

KEY RULES:
- Use FakeTimeProvider for the monitor tests; never Thread.Sleep.
- A test never swallows an exception thrown by the code it drives.
- Test names describe behaviour; Arrange-Act-Assert; one behaviour per test.
- Include the AC-024 test: default options => threshold Medium, flag false, Medium breach requires notification.
- Measure per-flag coverage with the AGENTS.md section 9 commands and report it per touched file.

REFERENCE FILES:
- tests/Encina.UnitTests/ (existing BreachNotification tests, for the fixtures in use)
- .github/coverage-manifest/Encina.Compliance.BreachNotification.json
- docs/en/guides/TESTING.md
```

</details>

### Phase 5: Documentation and Finalization

> **Goal**: Docs name the draft article; changelog, PublicAPI and verification.

<details>
<summary><strong>Tasks</strong></summary>

1. XML docs on every new public member, with `<remarks>` naming the draft article and "off by default".
2. `changelog.d/813-breach-authority-threshold.added.md`: the new option and policy (written by `mechanical-fixer`).
3. `src/Encina.Compliance.BreachNotification/README.md`: the setting, the flag, the current-law default and the hot-swap note (by `docs-writer`).
4. Package specification page for BreachNotification (#1208, when it exists): the Art. 33(1) row says "Partial: Omnibus threshold available behind `EnableOmnibusArticle33HighRiskThreshold`, off by default (draft)". If #1208 has not landed, record the row in the PR description and leave a checklist item on #1208.
5. `docs/features/` configuration section for BreachNotification (by `docs-writer`): a table of the two settings and the three validator rules.
6. `ROADMAP.md`: no change unless the milestone moves. `docs/INVENTORY.md`: one new type row if the inventory lists types.
7. No ADR: Design Choices 3 and 4 are recorded in this plan; an ADR is written only if the maintainer's answers change the architecture (for example option C of Design Choice 3).
8. Build and tests: `dotnet build --configuration Release` with 0 warnings; `dotnet test` on the four test projects; the per-flag measurement of AGENTS.md §9 and `dotnet run --file .github/scripts/coverage-report.cs -- --check-justifications`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT:
Plan #813, Phase 5. Documentation edits under docs/ and READMEs go to the docs-writer; changelog fragments and
PublicAPI lines go to mechanical-fixer (path-ownership hooks enforce it).

TASK:
Finish XML docs, PublicAPI.Unshipped.txt lines, the changelog fragment
changelog.d/813-breach-authority-threshold.added.md, the package README and the feature documentation. Run the
build with zero warnings and the test and coverage commands of AGENTS.md section 9.

KEY RULES:
- The Digital Omnibus is draft law: say "proposed" and "draft", never "will" or "requires". Name the draft
  article as given in SPEC-002 section 3.5 and mark it unverified until the Official Journal text is published.
- Never type coverage figures by hand in documentation; use covref markers (SPEC-001).
- Public text names no private client application; use "the reference application" if needed.

REFERENCE FILES:
- docs/specifications/SPEC-002-eu-regulatory-readiness.md (section 3.5, REQ-024, AC-024)
- changelog.d/README.md
- src/Encina.Compliance.BreachNotification/README.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Article / Section | Relevance |
|----------|-------------------|-----------|
| GDPR (Reg. (EU) 2016/679) | Art. 33(1) | Current-law default: notify unless "unlikely to result in a risk"; 72 hours from awareness |
| GDPR | Art. 33(5) | Documentation of every breach; the deferred decision-event issue |
| GDPR | Art. 34(1) | "High risk" threshold for data subjects; basis for the `authority <= subject` invariant |
| Digital Omnibus COM(2025) 837 (draft) | GDPR Art. 33(1) as amended (per SPEC-002 §3.5) | "High risk" threshold for authority notification; unverified against the Official Journal; off by default |
| SPEC-002 | REQ-024, DEC-012, AC-024 | Draft behaviour off by default, named by its draft article; test that the default follows current law |
| SPEC-002 | REQ-061, REQ-062 | Tenant-aware and instrumented capabilities (Design Choice 8, Phase 3) |
| EDPB Guidelines 9/2022 | Breach notification | Severity assessment that feeds `BreachSeverity` (already cited in `Model/BreachSeverity.cs`) |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|----------------------|
| `BreachNotificationOptions` | `src/Encina.Compliance.BreachNotification/BreachNotificationOptions.cs` | New property and flag next to `SubjectNotificationSeverityThreshold` (line 181) |
| `BreachNotificationOptionsValidator` | `src/Encina.Compliance.BreachNotification/BreachNotificationOptionsValidator.cs` | Three new rules (line 83 is the neighbouring rule) |
| `BreachDeadlineMonitorService` | `src/Encina.Compliance.BreachNotification/BreachDeadlineMonitorService.cs` | Consumer of the policy; keeps its catch-per-iteration loop |
| `BreachNotificationHealthCheck` | `src/Encina.Compliance.BreachNotification/Health/BreachNotificationHealthCheck.cs` | Overdue counting limited to required breaches (lines 199-215) |
| `DefaultBreachNotificationService` | `src/Encina.Compliance.BreachNotification/Services/DefaultBreachNotificationService.cs` | Unchanged; `GetApproachingDeadlineBreachesAsync` (line 468) and `ReportToDPAAsync` (line 165) |
| `DataSubjectRightsOptions.EnableOmnibusArticle12RefusalGrounds` | `src/Encina.Compliance.DataSubjectRights/DataSubjectRightsOptions.cs:96` | Naming and default-off precedent (#814) |
| `BreachNotificationLogMessages`, `BreachNotificationDiagnostics` | `src/Encina.Compliance.BreachNotification/Diagnostics/` | EventIds 8792-8794 and the counter |
| `ServiceCollectionExtensions` | `src/Encina.Compliance.BreachNotification/ServiceCollectionExtensions.cs` | `TryAddSingleton` of the policy; options validation registration (line 90) |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Compliance.BreachNotification` | 8792-8794 (inside the registered `ComplianceBreachNotification` 8700-8799) | Highest id in use is 8791; no new range and no change to `EventIdRanges.cs`. Plan #812 uses the separate range 9700-9749 and does not touch 8792-8799 |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| New production files | 2 | `IBreachAuthorityNotificationPolicy`, `DefaultBreachAuthorityNotificationPolicy` |
| Modified production files | 6 | options, validator, monitor, health check, DI, log messages and diagnostics (counted as 2) plus `BreachSeverity` docs |
| Tests | ~9 | unit 5, guard 1, contract 1, property 1, justifications 3 (`.md`) |
| Documentation and records | 4 | README, feature doc, changelog fragment, PublicAPI lines |

Total about 24 files, above the 15-file limit of one worker PR (AGENTS.md §11). Proposed split: **PR A** (Phases 1 to 4: 2 new and 7 modified production files, PublicAPI, and the tests, grouped by flag) and **PR B** (Phase 5: README, feature page, changelog fragment, specification row). If PR A alone passes 15 files, move the guard, contract and property tests into a follow-up PR on the same issue.

---

## Combined AI Agent Prompts

<details>
<summary><strong>Complete implementation prompt (all phases)</strong></summary>

```text
PROJECT CONTEXT:
Encina is a pre-1.0 .NET 10 / C# 14 library (nullable on, ROP with Either<EncinaError, T>, TimeProvider
injection, [LoggerMessage] logging with EventIds in registered ranges, TryAdd DI). AGENTS.md binds every change.
Encina.Compliance.BreachNotification is event-sourced on Marten (ADR-019). Issue #813 adds a separate authority
notification severity threshold for the Digital Omnibus proposal COM(2025) 837, which is draft law and therefore
off by default (SPEC-002 REQ-024, DEC-012, AC-024).
Plan: docs/plans/breach-authority-threshold-implementation-plan-813.md (follow the Design Choices as decided in
its Maintainer Decisions section, once it exists).

IMPLEMENTATION OVERVIEW:
1. BreachNotificationOptions: AuthorityNotificationSeverityThreshold (default Medium) and
   EnableOmnibusArticle33HighRiskThreshold (default false).
2. IBreachAuthorityNotificationPolicy and DefaultBreachAuthorityNotificationPolicy (IOptionsMonitor, per-call,
   fallback to Medium on an invalid reload).
3. Validator rules: defined enum value; authority <= subject; above Medium only with the Omnibus flag.
4. BreachDeadlineMonitorService and BreachNotificationHealthCheck count only breaches that require notification.
   ReportToDPAAsync and GetApproachingDeadlineBreachesAsync are unchanged.
5. Observability: EventIds 8792-8794 inside 8700-8799; one counter.
6. Tests on every flag including the AC-024 test; justifications for integration, load and benchmark.
7. Docs, changelog fragment, PublicAPI, zero warnings.

KEY PATTERNS:
- Draft law off by default and named by its draft article in XML docs; say "proposed", never "will".
- Fail closed: an invalid reload yields the current-law threshold, never silence.
- No exception message, option value or breach content in logs, tags or health data.
- Pre-1.0: no [Obsolete], no aliases. CRAP <= 10 on changed methods. Per-flag coverage targets.

REFERENCE FILES:
- src/Encina.Compliance.BreachNotification/BreachNotificationOptions.cs
- src/Encina.Compliance.BreachNotification/BreachDeadlineMonitorService.cs
- src/Encina.Compliance.BreachNotification/Health/BreachNotificationHealthCheck.cs
- src/Encina.Compliance.DataSubjectRights/DataSubjectRightsOptions.cs
- docs/specifications/SPEC-002-eu-regulatory-readiness.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | The decision is a comparison over an already-cached options snapshot and the breach read model; nothing to cache |
| 2 | OpenTelemetry | ✅ | Counter `breach.authority_notification.below_threshold.total` with the severity tag; no breach content (Phase 3) |
| 3 | Structured Logging | ✅ | EventIds 8792-8794 in the registered range 8700-8799 (Phase 3) |
| 4 | Health Checks | ✅ | `BreachNotificationHealthCheck` counts only required breaches and reports the below-threshold count (Phase 2) |
| 5 | Validation | ✅ | Three validator rules, fail closed (Design Choice 6, Phase 2) |
| 6 | Resilience | ❌ | No external call |
| 7 | Distributed Locks | ❌ | No shared mutable state; the policy is stateless |
| 8 | Transactions | ❌ | No write; the decision is not persisted (Design Choice 2) |
| 9 | Idempotency | ❌ | Pure function of severity and options |
| 10 | Multi-Tenancy | ⏭️ | The policy signature carries `tenantId` (Design Choice 8); per-tenant overrides are deferred and need no interface change; issue file `artifacts/issues/plan-813-breach-authority-decision-event.md` covers the stream record, and a per-tenant override issue is opened only if the maintainer picks B of Design Choice 8 |
| 11 | Module Isolation | ❌ | Global configuration; no module scoping needed |
| 12 | Audit Trail | ⏭️ | Recording the threshold in force and the decision on the breach stream (GDPR Art. 33(5)) is deferred to the issue file `artifacts/issues/plan-813-breach-authority-decision-event.md` (Design Choice 5) |

---

## Prerequisites & Dependencies

### Must coordinate

| Item | State (2026-11-11) | Relationship |
|------|--------------------|--------------|
| #812 Incident reporting adapters (plan `enisa-single-entry-point-implementation-plan-812.md`) | Open, plan merged | Independent. #812 applies no severity filter and removes `AutoNotifyOnHighSeverity` and `SupervisoryAuthority`; this plan does not use them. If #812 lands first, its `DefaultBreachReportingService` may inject `IBreachAuthorityNotificationPolicy` only to label a submission voluntary; that is not required by either plan. Both edit `BreachNotificationOptions.cs`, `BreachNotificationOptionsValidator.cs` and `ServiceCollectionExtensions.cs`; whichever lands second rebases. Event ids do not collide (8792-8794 here, 9700-9749 there) |
| #817 Compliance options to `IOptionsMonitor` | Open | Migrates the other `IOptions` consumers. This plan needs only the monitor in the new policy, so it does not wait for #817 |
| #1208 Article-coverage specification for BreachNotification | Open | Must list Art. 33(1) as Partial (draft, off) with REQ-024 wording |

### Advisable, not blocking

| Item | State | Relationship |
|------|-------|--------------|
| #809 Digital Omnibus impact assessment EPIC | Open | Parent; option C of Design Choice 3 (one profile) would be decided there |

### Missing prerequisites and findings, written as issue files

| File | Template | Finding |
|------|----------|---------|
| `artifacts/issues/plan-813-breach-authority-decision-event.md` | `[FEATURE]` | The breach stream does not record which authority-notification threshold was in force or why a breach was not notified (GDPR Art. 33(5)) |

---

## Next Steps

1. The maintainer decides Design Choices 3, 4, 5 and 8 when work on #813 starts; the orchestrator records them in a "Maintainer Decisions" section, updates each Chosen Option, then runs `adversarial-reviewer` on Choices 1, 2, 6 and 7.
2. Open the issue file above (`open-issue` skill) and link it here and from #813.
3. Link this plan from #813, then start Phase 1 in a worktree (`worker-brief` skill) with the local CRAP table required in the brief.
4. Re-verify the draft article numbering against the Official Journal text within 30 days of any publication of COM(2025) 837 (SPEC-002 AC-025).
