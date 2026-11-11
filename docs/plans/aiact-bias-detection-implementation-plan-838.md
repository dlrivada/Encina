# Implementation Plan: `Encina.Compliance.AIAct.BiasDetection` — Bias Detection & Fairness Metrics (Art. 10(2)(f))

> **Issue**: [#838](https://github.com/dlrivada/Encina/issues/838) (child of [#881](https://github.com/dlrivada/Encina/issues/881), milestone v0.16.0 — AI Act)
> **Type**: Feature
> **Complexity**: High (9 phases, 7 pull requests, 1 new package, 1 core extension, no persistence, ~76 files)
> **Estimated Scope**: ~1,900-2,400 lines of production code + ~2,000-2,500 lines of tests
> **Prerequisites**: none blocking; [#847](https://github.com/dlrivada/Encina/issues/847) (Marten) is independent, [#837](https://github.com/dlrivada/Encina/issues/837) (data governance) is a consumer

---

## Summary

Implement the statistical core of EU AI Act Article 10(2)(f) and (g) for high-risk AI systems built with Encina: a pluggable bias detector that examines a data set for biases likely to affect health and safety, fundamental rights or lead to discrimination prohibited under Union law, with six fairness metrics, confidence intervals on every indicator, threshold-based verdicts, an explicit signal when two metrics disagree (escalated to human review, never auto-enforced), and intersectional analysis under minimum subgroup sizes. The package supplies the real analysis behind the seam the core already ships and leaves the stub in place only when the package is not installed.

### What is already in the code (verified on `7a0049c07`)

| Item | Location | State |
|---|---|---|
| Seam `IDataQualityValidator.DetectBiasAsync(datasetId, protectedAttributes, ct)` | `src/Encina.Compliance.AIAct/Abstractions/IDataQualityValidator.cs` | Done; no consumer in the core pipeline today |
| Stub `DefaultDataQualityValidator` | `src/Encina.Compliance.AIAct/DefaultDataQualityValidator.cs:52`, `:77` | `DetectBiasAsync` returns a report with no indicators and `OverallFairness = true`; documented as "not empirically measured" (XML remarks, lines 14-31) |
| `BiasReport` (`DatasetId`, `ProtectedAttributes`, `Indicators`, `OverallFairness`, `EvaluatedAtUtc`) | `src/Encina.Compliance.AIAct/Model/BiasReport.cs` | Done; no conflict or subgroup surface |
| `BiasIndicator` (`ProtectedAttribute`, `DisparateImpactRatio`, `ConfidenceInterval` as one `double`, `ExceedsThreshold`, `SampleSize`, `Methodology`) | `src/Encina.Compliance.AIAct/Model/BiasIndicator.cs` | Done; disparate impact only, one-sided interval, boolean verdict |
| Registration | `src/Encina.Compliance.AIAct/ServiceCollectionExtensions.cs:31` (`AddEncinaAIAct`), `:86` (`TryAddSingleton<IDataQualityValidator, DefaultDataQualityValidator>`) | Done; a satellite registration that runs before or after it must still win over the stub |
| Notifications published through `IEncina.Publish` | `InMemoryAISystemRegistry.cs:109`, `Notifications/*` | Done; pattern reused |
| Package diagnostics | `Diagnostics/AIActDiagnostics.cs` (`Encina.Compliance.AIAct` source and meter), `Diagnostics/AIActLogMessages.cs` (9500-9512) | Done; the new package gets its own source and meter |
| Bias analysis, fairness metrics, intersections, protected attribute catalog | none | **This plan** |

### Deviations from the issue text (each is put to the maintainer below or follows a binding rule)

- **The detector needs data and the issue's request carries none.** `BiasDetectionRequest` holds a `DatasetId` and attribute names only. Design Choice 3 adds the abstraction through which rows reach the calculators.
- **`FairnessScore` is `BiasIndicator`.** The issue defines `FairnessScore` next to the core `BiasIndicator`, which would be two records for one concept; the acceptance criterion "Integration with `BiasIndicator` and `BiasReport` from #415" and `AGENTS.md` §1 (no compatibility layers, pre-1.0) decide it: `BiasIndicator` is extended and `FairnessScore` is not created (Design Choice 1).
- **`double ConfidenceInterval` becomes two bounds and a level**; **`bool PassesThreshold` becomes a three-valued verdict** (Pass, Fail, Inconclusive) because a verdict that ignores the interval makes the interval decorative (Design Choices 4 and 5).
- **`double? ThresholdOverride` becomes a per-metric map.** One number cannot mean a ratio of 0.8 for disparate impact and a gap of 0.1 for equalized odds (Design Choice 10).
- **Below-minimum subgroups are not a whole-run `Left`.** The issue's Alternatives Considered item 7 makes them a `Left`; Design Choice 6 recommends a per-indicator `Inconclusive` result so one rare cell does not discard eleven valid results, while never reporting the dataset as fair.
- **No mutable in-memory registry by default.** The issue's item 9 sets an in-memory registry as the default; `AGENTS.md` §3 says event-sourced compliance modules have no InMemory stores. Design Choice 7 puts this to the maintainer.
- **Article references.** Art. 10(5) (special categories of personal data for bias detection) no longer exists after Reg. (EU) 2026/1744: the consolidated text carries it as new Art. 4a. The issue does not mention it; Design Choice 8 does.
- **Dates.** Annex III high-risk obligations, Art. 10 among them, apply from 2 Dec 2027 and Annex I high-risk from 2 Aug 2028 (Reg. (EU) 2026/1744, SPEC-002 §3.2, issue comment). The feature is opt-in and carries no date logic; XML docs and the feature page state the dates.

### Scope per pull request (one worker issue = one PR, `AGENTS.md` §11)

The whole feature is ~76 files and ~4,300 lines, five times a worker PR (15 files, 1,500 lines). It ships as seven sequential pull requests; each builds, passes its own tests and reaches its per-flag targets alone.

| PR | Phases | Content | Files (est.) | Lines (est.) |
|---|---|---|---|---|
| 1 | 1 | Shared core types and report extension (`Encina.Compliance.AIAct`) | 11 | ~600 |
| 2 | 2 | Package skeleton, abstractions, requests, interval statistics | 15 | ~1,100 |
| 3 | 3 | Rate-based metrics (disparate impact, demographic parity, equalized odds, predictive parity) and the calculator | 10 | ~1,000 |
| 4 | 4 | Calibration, counterfactual, options, threshold evaluation | 10 | ~900 |
| 5 | 5 | Registry, intersections, conflict detection, detector, seam wiring, DI | 14 | ~1,400 |
| 6 | 6-8 | Cross-cutting (notification, audit), observability, benchmark, justifications | 10 | ~900 |
| 7 | 9 | Documentation (`docs-writer`), changelog, ADR | 6 | ~600 |

Counts are estimates re-measured when each worker brief is written (`Size:` line). **Affected packages**: `Encina.Compliance.AIAct` (modified), `Encina.Compliance.AIAct.BiasDetection` (new), `Encina` (EventIdRanges is not touched: the ids sit inside `ComplianceAIAct`; only the architecture test map changes).

**Provider category**: none. The 10-provider database rule does not apply (`Encina.Compliance.AIAct` is a Marten event-sourced module, ADR-019; persistence is #847). No caching, transport, lock or validation-provider category applies.

**Family coordination (AI Act modules #838, #840, #841, #843).** This plan owns the shared analysis types `FairnessMetricType`, `FairnessVerdict`, `ConfidenceInterval`, `SubgroupSize`, `MetricConflict` and the extended `BiasIndicator` and `BiasReport`, all in core `Encina.Compliance.AIAct.Model`, so that #840 (Annex IV technical documentation, point 2(g): metrics and "potentially discriminatory impacts") and #843 can cite bias results without referencing this package. EventIds 9548-9561 are this plan's; the remaining 9562-9599 of `ComplianceAIAct` must be split by the other plans of the family (collision to check when each plan is merged).

---

## Design Choices

<details>
<summary><strong>1. Package placement and where the shared types live</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — New package `Encina.Compliance.AIAct.BiasDetection`; report and indicator types stay in the core and are extended there** | Matches the issue title, the `new-package` label and the EPIC #881 layout of one package per module; applications that do not need bias analysis take no statistics code; #840 and #843 read `BiasReport` without referencing the package | The core `Model/` changes (a breaking change to `BiasIndicator`, allowed pre-1.0); two assemblies to version |
| **B — Everything in a `BiasDetection/` folder of the core package** | One assembly; no cross-package plumbing | The core grows by ~40 files for an opt-in feature; the issue and the `new-package` label say otherwise (contrast #842, which needs the pipeline behavior and so recommends a folder) |
| **C — New package with its own report types, core types left untouched** | No core change | Two parallel bias report types and a mapping layer: a compatibility layer `AGENTS.md` §3 forbids; breaks the acceptance criterion "Integration with `BiasIndicator` and `BiasReport` from #415" |

### Chosen Option: **A — New package, shared types in the core**

### Rationale

The issue and EPIC #881 fix the package. The detector has no pipeline hook, so unlike #842 nothing forces it into the core. The shared records are what other modules read, so they stay where the existing `BiasReport` is.

**Class:** Settled
**Settled by:** issue #838 (title, `new-package` label, acceptance criterion "Integration with `BiasIndicator` and `BiasReport` from #415"); EPIC #881; `AGENTS.md` §1 (pay-for-what-you-use, no legacy) and §3 (no compatibility layers)

</details>

<details>
<summary><strong>2. How the detector reaches the existing `IDataQualityValidator.DetectBiasAsync` seam</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — The package registers `BiasAwareDataQualityValidator`, which delegates `DetectBiasAsync` to `IBiasDetector` and `ValidateTrainingDataAsync` to the stub; the registration replaces the descriptor only while it is still `DefaultDataQualityValidator`** | The core gains no unused abstraction; wins in either registration order; an application's own `IDataQualityValidator` is never overridden; `IBiasDetector` stays in the package | A descriptor inspection at registration time; delegation of `ValidateTrainingDataAsync` to the stub until #837 replaces it |
| **B — `IBiasDetector` lives in the core and `DefaultDataQualityValidator` resolves it optionally** | No descriptor swapping; the stub becomes real as soon as a detector is registered | The core owns an abstraction nobody uses until a satellite is installed; the stub gains a service-locator dependency, against pay-for-what-you-use |
| **C — Leave the seam alone; callers use `IBiasDetector` directly** | Smallest change | `IDataQualityValidator.DetectBiasAsync` stays a fail-open stub in applications that install the package, and #837 cannot rely on it |

### Chosen Option: **A — Package-side composite that replaces only the default** (recommended, pending the maintainer)

### Rationale

We recommend A. It follows the rule written for stores in `AGENTS.md` §3 (the specialized implementation wins over the default in any order, without overriding the application's own registration) and keeps the core free of a seam without an implementation. B wins if the maintainer prefers the core to define every abstraction of the module (Validation's orchestrator pattern in `AGENTS.md` §3 puts abstractions in the core); then `IBiasDetector` moves to the core in Phase 1 and nothing else changes. C wins only if the seam is to be retired.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. How the data set reaches the calculators</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — `IBiasObservationSource` streams `BiasObservation` rows (group values, prediction, label, score, optional counterfactual predictions) for a dataset id; the calculators reduce them to per-group counters in one pass** | Constant memory (O(groups)); the package never holds individual records; works with any store (ML.NET, files, a database) through one adapter; same input for every metric | The application writes one adapter; a source per dataset id convention to document |
| **B — The request carries the rows or the group counts** | No new abstraction | Rows in memory (unbounded); the issue's request stays at 5 fields only if the caller pre-aggregates, which pushes the statistics into every caller |
| **C — Delegate everything to ML tooling (Fairlearn, AIF360) through `IBiasDetector` only; no default calculators** | Smallest code | Every user needs an external tool; the acceptance criteria require "default implementations with statistical framework" |

### Chosen Option: **A — Streaming observation source** (recommended, pending the maintainer)

### Rationale

We recommend A. Art. 10(3) asks that data sets have appropriate statistical properties "as regards the persons or groups of persons", which is a per-group computation; one pass over a stream gives it without materializing personal data. B wins for a tiny dataset analyzed in a test; it stays possible because an adapter over an in-memory list is a few lines. C wins for an organization that already runs Fairlearn: it replaces `IBiasDetector` (still pluggable, Design Choice 2) and loses nothing. Inputs per metric: disparate impact and demographic parity need the prediction; equalized odds and predictive parity need prediction and label; calibration needs score and label; counterfactual fairness needs the model's prediction under a changed attribute, which the source supplies per row (the package never calls a model). A metric whose input is missing is a `Left` (`aiact.bias_metric_input_missing`), never skipped silently.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Confidence intervals: method and representation</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — Closed-form intervals (Wilson score for a rate, Newcombe hybrid score for a difference of rates, Katz log method for a ratio), returned as `ConfidenceInterval(Lower, Upper, Level)`** | Deterministic; one pass; valid for ratios, whose distribution is asymmetric; no random seed to manage | Each metric needs an approximation; calibration gets a normal approximation only |
| **B — Bootstrap resampling for every metric** | One method for all six metrics, including those with no closed form | Needs the rows twice or a stored sample; randomness (seed, non-reproducible reports); slow on large sets |
| **C — Keep one `double` half-width (the shape of today's `BiasIndicator.ConfidenceInterval`)** | No model change | A symmetric half-width misstates the bounds of a ratio near 0 or 1; hides the confidence level |

### Chosen Option: **A — Closed-form intervals with explicit bounds and level** (recommended, pending the maintainer)

### Rationale

We recommend A: the default level is 0.95 (`BiasDetectionOptions.ConfidenceLevel`), the bounds are clamped to the metric's range, a zero cell uses the standard +0.5 adjustment and the method is named in `BiasIndicator.Methodology` so a reviewer can reproduce the figure. B wins for a metric with no usable approximation (a full calibration curve); a backend plugged behind `IBiasDetector` can do that. C is rejected as the representation in every scenario except API brevity, and pre-1.0 brevity does not justify a wrong interval. The reproducibility of reports matters because Annex IV point 2(g) asks for test reports "dated and signed", which a seeded-bootstrap result can still satisfy but only with the seed stored.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Verdicts, metric conflicts and escalation to human review</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — Each indicator has a verdict Pass, Fail or Inconclusive (the interval straddles the threshold); `BiasReport.Conflicts` lists attributes where one metric passes and another fails; any conflict or Inconclusive sets `RequiresHumanReview` and `OverallFairness = false`; a notification is published; nothing is enforced automatically** | Implements the maintainer's decision; uses the interval; cannot produce a false sense of compliance (`AGENTS.md` §3 fail closed) | Reports are never "fair" for borderline data until a human reviews them |
| **B — Boolean `PassesThreshold` on the point estimate; conflicts listed but not escalated** | Matches the issue's record shape | Ignores the interval; a conflict can still yield `OverallFairness = true` |
| **C — Auto-enforce the strictest metric when metrics conflict** | A decision is always produced | The maintainer rejected it on #838 ("silently auto-enforcing a threshold in that scenario would be worse than doing nothing") |

### Chosen Option: **A — Three-valued verdicts, conflicts escalate to a human**

### Rationale

The maintainer's comment on #838 fixes the behavior: when two fairness metrics disagree on the same dataset, the report flags it explicitly for human review. The subgroup size is exposed next to each metric (`BiasIndicator.SubgroupSizes`) and `MetricConflict.SmallestSubgroupSize` shows when a conflict coincides with a small subgroup (the "n=12" example in the comments). The Inconclusive verdict follows from the same fail-closed reading. Escalation is a flag on the report plus `BiasConflictDetectedNotification` (the `Notifications/*` pattern); coupling to `IHumanOversightEnforcer` is not part of this issue.

**Class:** Settled
**Settled by:** the maintainer's decision recorded in the comments of #838 (`MetricCorrelation` or conflict surface, human review instead of auto-enforcement, subgroup size exposed); `AGENTS.md` §3 (compliance gates fail closed); Art. 10(2)(f) AI Act

</details>

<details>
<summary><strong>6. Intersectional attributes and subgroups below the minimum size</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — Tiers fixed by the maintainer (single axis by default, pairwise opt-in, three or more dimensions only with explicit configuration); a subgroup below `MinimumSubgroupSize` makes the indicator Inconclusive with its size shown; the run still succeeds** | One rare cell does not discard the valid results of the other cells; the report is never "fair" while a cell is unevaluable; the caller sees which cell | `Indicators` can contain unevaluable entries the caller must read |
| **B — A subgroup below the minimum fails the whole run with a `Left` (issue item 7)** | Simple contract; nothing partial | Eleven valid intersections are lost because one cell has n=12; callers cannot get any result at pairwise depth on real data |
| **C — Silently drop small subgroups** | Clean reports | Hides the very population Art. 10(2)(f) cares about; a silent pass |

### Chosen Option: **A — Inconclusive indicators, run succeeds** (recommended, pending the maintainer)

### Rationale

The tier structure itself is the maintainer's decision on #838; only the below-minimum behavior is open. We recommend A because it keeps fail-closed semantics (`OverallFairness = false`, `RequiresHumanReview = true`) without throwing away valid results. B wins for a caller who wants a hard stop in a CI gate; that caller checks `RequiresHumanReview` and fails the build. C is not a valid option under `AGENTS.md` §3. Defaults: `MinimumSubgroupSize = 30` (an engineering default, documented as such, not a legal value), `IntersectionDepth = 1`, depth 2 by option, depth 3 or more only with `AllowHigherOrderIntersections = true`; the number of enumerated intersections is capped (`MaxIntersections`) so the combinatorial growth the maintainer mentioned cannot exhaust memory.

**Class:** Dilemma

</details>

<details>
<summary><strong>7. State of the protected attribute registry</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — The catalog is declared in options at startup (`BiasDetectionOptions.ProtectedAttributes`); `IProtectedAttributeRegistry` is a read-only view (`GetAttributeAsync`, `GetAllAttributesAsync`); `RegisterAttributeAsync` is dropped** | No store, so nothing to persist or to lose on restart; follows `AGENTS.md` §3 (no InMemory stores in event-sourced modules); the catalog is configuration under source control, reviewable | No runtime registration; changing the catalog needs a restart |
| **B — Mutable in-memory registry as the default, Marten later (issue item 9)** | Matches the issue text; runtime registration works in tests and tools | An InMemory store in an event-sourced module (contradicts `AGENTS.md` §3); catalog lost on restart; the audit question "which attributes did you examine, and when did that list change" has no answer |
| **C — Marten-backed registry in this package** | Durable and audited | Pulls Marten into a stateless statistics package; duplicates #847's persistence work |

### Chosen Option: **A — Options-declared, read-only catalog** (recommended, pending the maintainer)

### Rationale

We recommend A: the list of protected attributes is a governance decision that belongs in reviewed configuration, and a read-only view needs no store. B wins for an interactive tool that edits the catalog at runtime; if that need appears, #847 adds a Marten-backed `IProtectedAttributeRegistry` behind the same interface. The deviation from the issue (item 9 and the `RegisterAttributeAsync` method) is stated in the Summary. An unregistered attribute in a request is a `Left` (`aiact.bias_attribute_not_registered`), never an implicit registration.

**Class:** Dilemma

</details>

<details>
<summary><strong>8. Special categories of personal data used as protected attributes (Art. 4a)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — `ProtectedAttribute.IsSpecialCategory` marks GDPR Art. 9(1) attributes; the detector refuses to analyse one unless `BiasDetectionOptions` carries an explicit, logged acknowledgement for that attribute that the Art. 4a conditions are met by the application; the refusal is a `Left`** | Fail closed; the opt-in is explicit and visible in configuration and logs (`AGENTS.md` §3 gates); the audit notification records attribute names, never values | An acknowledgement is an attestation the library cannot verify |
| **B — Flag only; conditions documented, never enforced** | No friction | Silently analyses special-category data with no trace of the decision |
| **C — Never allow special categories** | Strictest | Defeats the purpose: bias on ethnic origin, health or religion cannot be measured without those attributes (the reason Art. 4a exists) |

### Chosen Option: **A — Explicit, logged acknowledgement per attribute** (recommended, pending the maintainer)

### Rationale

We recommend A. Per the consolidated text, Reg. (EU) 2026/1744 replaces Art. 10(5) with Art. 4a, which lets providers exceptionally process special categories of personal data "to the extent strictly necessary" for bias detection and correction under conditions: other data (synthetic or anonymised) cannot do the job, technical limits on re-use with state-of-the-art security including pseudonymisation, and controlled, documented access (conditions further down, and the scope of Art. 4a, are unverified here). The library cannot check those conditions; it can make the decision explicit and traceable. B wins where the application already enforces the conditions elsewhere and wants no duplication. The package keeps no rows and logs no values in any case. The record of processing activities entry required by the Article stays the application's duty; the XML docs and the feature page say so.

**Class:** Dilemma

</details>

<details>
<summary><strong>9. EventId allocation and telemetry placement</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — Sub-allocate 9548-9561 inside the existing `ComplianceAIAct` range (9500-9599) and map the new assembly to it; own `ActivitySource` and `Meter` named after the package** | The pattern #847 (9513-9529) and #842 (9530-9547) use; no new range to register; 9562-9599 stay free for the family | The family shares 52 ids, so the other plans must stay inside it |
| **B — New range `ComplianceAIActBiasDetection` from the free 9800-9999 block** | Independent range | The block is already claimed by the #686-#688 plans; one more registration for 14 ids |

### Chosen Option: **A — Sub-allocation in `ComplianceAIAct`**

### Rationale

`EncinaEventIdAllocationTests.AssemblyRanges` maps an assembly to a list of range names, so a second assembly can use `nameof(EventIdRanges.ComplianceAIAct)` without a new registry entry. Messages are `[LoggerMessage]`, packed from 9548, with the XML line `Event IDs: 9548-9561 (see EventIdRanges.ComplianceAIAct)`.

**Class:** Trivial
**Settled by:** ADR-021; `AGENTS.md` §7; the EventId sections of the #842 and #847 plans; `src/Encina/Diagnostics/EventIdRanges.cs:389`

</details>

<details>
<summary><strong>10. Threshold defaults and the override shape</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — `BiasDetectionOptions` holds a threshold per `FairnessMetricType`; `BiasDetectionRequest.ThresholdOverrides` is a per-metric map; defaults: disparate impact at least 0.8, every difference-type metric at most 0.1** | A ratio and a gap each get a threshold of the right kind; the only legal figure (0.8) is the documented default | The 0.1 gap is an engineering default with no legal source, flagged as such |
| **B — One `double? ThresholdOverride` (issue text)** | Matches the issue | One number cannot be both a ratio and a gap |
| **C — No defaults for the non-ratio metrics; the caller must configure them** | No invented figures | The default detector would only run disparate impact |

### Chosen Option: **A — Per-metric thresholds with documented defaults**

### Rationale

The four-fifths rule for disparate impact comes from the EEOC Uniform Guidelines, which the issue cites (29 CFR 1607.4(D); that citation is unverified in this plan). The 0.1 figure for gaps is not from any law or guideline; the XML docs and the feature page say it is an engineering default to review per use case. Direction per metric (higher is better for the ratio, lower is better for gaps and the flip rate) is part of the metric definition.

**Class:** Trivial
**Settled by:** issue #838 references (EEOC Questions and Answers on the Uniform Guidelines, four-fifths rule); `AGENTS.md` §3 (options classes, no hidden constants)

</details>

---

## Implementation Phases

### Phase 1: Shared Core Types & Report Extension (PR 1, `Encina.Compliance.AIAct`)

> **Goal**: The analysis vocabulary lives in the core, so any module can read a bias report.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create `Model/FairnessMetricType.cs`**: `enum FairnessMetricType { DisparateImpact, DemographicParity, EqualizedOdds, PredictiveParity, CalibrationByGroup, CounterfactualFairness }`.
2. **Create `Model/FairnessVerdict.cs`**: `enum FairnessVerdict { Pass, Fail, Inconclusive }`.
3. **Create `Model/ConfidenceInterval.cs`**: `sealed record ConfidenceInterval(double Lower, double Upper, double Level)`; the constructor-free positional record is validated by the producers (Lower <= Upper, Level in (0,1)).
4. **Create `Model/SubgroupSize.cs`**: `sealed record SubgroupSize(string Group, int Count)`.
5. **Create `Model/MetricConflict.cs`**: `sealed record MetricConflict` with `string ProtectedAttribute`, `IReadOnlyList<FairnessMetricType> PassingMetrics`, `IReadOnlyList<FairnessMetricType> FailingMetrics`, `IReadOnlyList<SubgroupSize> SubgroupSizes`, `int SmallestSubgroupSize`, `bool InvolvesSubgroupBelowMinimum`.
6. **Modify `Model/BiasIndicator.cs`**: replace `DisparateImpactRatio` and the single-`double` interval by `FairnessMetricType Metric`, `double Score`, `ConfidenceInterval Interval`, `double Threshold`, `FairnessVerdict Verdict`, `IReadOnlyList<SubgroupSize> SubgroupSizes`; keep `ProtectedAttribute` (an intersection is the names joined with `×`), `int? SampleSize`, `string? Methodology` (names the interval method). Remove `ExceedsThreshold`.
7. **Modify `Model/BiasReport.cs`**: add `IReadOnlyList<MetricConflict> Conflicts { get; init; } = []` and `bool RequiresHumanReview { get; init; }`; `OverallFairness` stays and means "every verdict Pass and no conflict".
8. **Modify `PublicAPI.Unshipped.txt`** for every changed or new symbol; update XML docs; update `Encina.Compliance.AIAct` tests that construct `BiasIndicator` (`DefaultDataQualityValidatorTests.cs`, `ServiceCollectionExtensionsTests.cs` if affected).
9. **Tests** (`tests/Encina.UnitTests/Compliance/AIAct/Model/`): construction and equality of the new records, `BiasReport` defaults; guard-free records need no guard tests.
10. **Coverage manifest**: per-file targets with one-sentence justifications for the new core files in `.github/coverage-manifest/Encina.Compliance.AIAct.json`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of issue #838 in Encina.Compliance.AIAct.

CONTEXT:
- BiasIndicator and BiasReport exist in src/Encina.Compliance.AIAct/Model/ and are produced only by the stub
  DefaultDataQualityValidator (empty Indicators). Pre-1.0: change them completely, no compatibility members.
- This plan owns the shared analysis types; other AI Act modules (#840, #843) will read them.

TASK:
Create FairnessMetricType, FairnessVerdict, ConfidenceInterval, SubgroupSize and MetricConflict; rewrite BiasIndicator as
described in the plan (Metric, Score, Interval, Threshold, Verdict, SubgroupSizes, SampleSize, Methodology); add Conflicts and
RequiresHumanReview to BiasReport; update PublicAPI.Unshipped.txt, existing tests, and the coverage manifest entries.

KEY RULES:
- No [Obsolete], no aliases. Records are sealed, XML-documented, with required members where a value is mandatory.
- Time stays out of the records (EvaluatedAtUtc comes from TimeProvider in the producers).
- Zero warnings; RS0016/RS0017 resolved in PublicAPI.Unshipped.txt.
- Tests execute real code; Shouldly through Encina.Testing.Shouldly.

REFERENCE FILES:
- src/Encina.Compliance.AIAct/Model/BiasIndicator.cs
- src/Encina.Compliance.AIAct/Model/BiasReport.cs
- src/Encina.Compliance.AIAct/DefaultDataQualityValidator.cs
- src/Encina.Compliance.AIAct/PublicAPI.Unshipped.txt
- tests/Encina.UnitTests/Compliance/AIAct/DefaultDataQualityValidatorTests.cs
- .github/coverage-manifest/Encina.Compliance.AIAct.json
```

</details>

---

### Phase 2: Package Skeleton, Abstractions & Interval Statistics (PR 2)

> **Goal**: A buildable package with the contracts, the request models and the statistics every metric uses.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create `src/Encina.Compliance.AIAct.BiasDetection/Encina.Compliance.AIAct.BiasDetection.csproj`** (references `Encina.Compliance.AIAct`; `PublicApiAnalyzers`; `InternalsVisibleTo` the test projects like the core csproj), `PublicAPI.Shipped.txt`, `PublicAPI.Unshipped.txt`; add the project to `Encina.slnx`.
2. **Create `BiasDetectionErrors.cs`** (static, error codes `aiact.bias_invalid_request`, `aiact.bias_attribute_not_registered`, `aiact.bias_special_category_refused`, `aiact.bias_intersection_refused`, `aiact.bias_source_not_registered`, `aiact.bias_source_failed`, `aiact.bias_metric_input_missing`; factory methods returning `EncinaError`; messages never carry exception messages).
3. **Create `Abstractions/IBiasObservationSource.cs`**: `ValueTask<Either<EncinaError, IAsyncEnumerable<BiasObservation>>> GetObservationsAsync(string datasetId, IReadOnlyList<string> protectedAttributes, CancellationToken cancellationToken = default)`.
4. **Create `Abstractions/IFairnessMetricCalculator.cs`**: `ValueTask<Either<EncinaError, FairnessMetrics>> CalculateAsync(FairnessMetricRequest request, CancellationToken cancellationToken = default)`.
5. **Create `Model/BiasObservation.cs`**: `sealed record BiasObservation` with `IReadOnlyDictionary<string, string> AttributeValues`, `bool? Prediction`, `bool? Label`, `double? Score`, `IReadOnlyDictionary<string, bool>? CounterfactualPredictions` (per attribute).
6. **Create `Model/BiasDetectionRequest.cs`**: `DatasetId`, `ProtectedAttributes`, `MetricsToCalculate` (empty means every metric the observations support), `ThresholdOverrides` (`IReadOnlyDictionary<FairnessMetricType, double>`), `IntersectionDepth` (default 1).
7. **Create `Model/FairnessMetricRequest.cs`** (the calculator input: dataset id, attributes, metrics, thresholds, confidence level, minimum subgroup size, observations as `IAsyncEnumerable<BiasObservation>`) and **`Model/FairnessMetrics.cs`** (`DatasetId`, `IReadOnlyList<BiasIndicator> Indicators`, `CalculatedAtUtc`).
8. **Create `Statistics/IntervalStatistics.cs`** (internal static): `Wilson(successes, n, level)`, `NewcombeDifference(s1, n1, s2, n2, level)`, `KatzRatio(s1, n1, s2, n2, level)`, `NormalMeanDifference(...)`; zero-cell +0.5 adjustment; bounds clamped; all pure.
9. **Tests**: unit tests of each interval against hand-computed values (published worked examples cited in comments); guard tests for the public constructors/factories; coverage manifest file `.github/coverage-manifest/Encina.Compliance.AIAct.BiasDetection.json` generated with `.github/scripts/generate-coverage-manifest.cs`, then targets and justifications written.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of issue #838: the new package Encina.Compliance.AIAct.BiasDetection.

CONTEXT:
- Phase 1 added FairnessMetricType, FairnessVerdict, ConfidenceInterval, SubgroupSize, MetricConflict and the extended
  BiasIndicator/BiasReport to Encina.Compliance.AIAct.
- The package reads rows through IBiasObservationSource (streaming) and never stores them (plan Design Choice 3).

TASK:
Create the project, slnx entry, PublicAPI files, errors, the two abstractions, the request/observation/result records and the
internal IntervalStatistics class; write unit and guard tests and the coverage manifest.

KEY RULES:
- Operations return Either<EncinaError, T>; errors carry codes, never exception messages.
- Interval math is pure and deterministic; level in (0,1); bounds clamped to the metric range.
- XML docs on every public member (Art. 10(2)(f) in remarks); English only.
- CRAP <= 10 on every changed method; measure the local table.

REFERENCE FILES:
- src/Encina.Compliance.AIAct/Encina.Compliance.AIAct.csproj
- src/Encina.Compliance.AIAct/AIActErrors.cs
- src/Encina.Compliance.AIAct/Abstractions/IDataQualityValidator.cs
- .github/scripts/generate-coverage-manifest.cs
- Encina.slnx
```

</details>

---

### Phase 3: Rate-Based Fairness Metrics & Calculator (PR 3)

> **Goal**: Four metrics computed in one pass from per-group counters.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create `Metrics/IFairnessMetric.cs`** (internal): `FairnessMetricType Type { get; }`, `bool Supports(ObservationInputs available)`, `BiasIndicator Evaluate(GroupCounters counters, MetricContext context)`.
2. **Create `Metrics/GroupCounters.cs`** (internal): per group `Total`, `PredictedPositive`, `TruePositive`, `FalsePositive`, `TrueNegative`, `FalseNegative`, `ScoreSum`, `LabelPositive`, `CounterfactualFlips`, updated by `Add(BiasObservation)`.
3. **Create `Metrics/DisparateImpactMetric.cs`**: each group's selection rate over the highest group's rate; the indicator reports the minimum ratio, interval by the Katz log method; Pass when the lower bound is at least the threshold, Fail when the upper bound is below it, Inconclusive otherwise.
4. **Create `Metrics/DemographicParityMetric.cs`**: largest gap in selection rate between two groups; Newcombe interval; direction "at most".
5. **Create `Metrics/EqualizedOddsMetric.cs`**: the larger of the TPR gap and the FPR gap; needs prediction and label.
6. **Create `Metrics/PredictiveParityMetric.cs`**: precision gap; needs prediction and label.
7. **Create `DefaultFairnessMetricCalculator.cs`** (public sealed, `IFairnessMetricCalculator`): consumes the stream once, feeds counters per attribute and intersection key, evaluates the requested metrics, returns `FairnessMetrics`; `TimeProvider` for `CalculatedAtUtc`; a metric whose inputs are missing yields `Left(aiact.bias_metric_input_missing)`; cancellation propagates.
8. **Tests**: unit tests with datasets whose results are computed by hand (including the 0.8 boundary and a zero cell); property tests (score ranges, `Lower <= Score <= Upper`, permutation invariance of the stream); contract test of `IFairnessMetricCalculator` against the real calculator; guard tests; manifest targets.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of issue #838 in Encina.Compliance.AIAct.BiasDetection.

CONTEXT:
- Phase 2 delivered IBiasObservationSource, FairnessMetricRequest/FairnessMetrics, BiasObservation and IntervalStatistics.
- Metric definitions and inputs are in the plan (Design Choices 3-5): disparate impact and demographic parity need Prediction;
  equalized odds and predictive parity need Prediction and Label.

TASK:
Implement GroupCounters, the four rate-based IFairnessMetric classes and DefaultFairnessMetricCalculator, which makes one pass
over the observation stream (O(groups) memory) and returns Either<EncinaError, FairnessMetrics>.

KEY RULES:
- Verdict: Pass when the whole interval is on the passing side of the threshold, Fail when it is wholly on the failing side,
  otherwise Inconclusive.
- TimeProvider for timestamps; never DateTime.UtcNow. Cancellation propagates; no exception escapes as success.
- Methods small enough for CRAP <= 10; one metric per class.
- Tests build datasets with builders and compare with hand-computed values; no reflection-only tests.

REFERENCE FILES:
- src/Encina.Compliance.AIAct.BiasDetection/Statistics/IntervalStatistics.cs
- src/Encina.Compliance.AIAct/Model/BiasIndicator.cs
- docs/en/guides/TESTING.md
- tests/Encina.PropertyTests/Compliance/AIAct/
```

</details>

---

### Phase 4: Calibration, Counterfactual, Options & Threshold Evaluation (PR 4)

> **Goal**: The remaining two metrics, the configuration and the shared verdict logic.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create `Metrics/CalibrationByGroupMetric.cs`**: per group, the gap between mean predicted score and observed positive rate (calibration-in-the-large); score is the largest gap between two groups; normal-approximation interval; needs Score and Label.
2. **Create `Metrics/CounterfactualFairnessMetric.cs`**: flip rate (share of rows whose prediction changes when only the attribute changes) from `CounterfactualPredictions`; Wilson interval; direction "at most"; missing counterfactual data is `Left(aiact.bias_metric_input_missing)`.
3. **Create `BiasDetectionOptions.cs`**: `ConfidenceLevel` (0.95), `DisparateImpactThreshold` (0.8), `DifferenceThreshold` (0.1) with per-metric overrides, `MinimumSubgroupSize` (30), `IntersectionDepth` (1), `AllowHigherOrderIntersections` (false), `MaxIntersections` (e.g. 200), `ProtectedAttributes` (list of `ProtectedAttribute`), `SpecialCategoryAcknowledgements` (set of attribute names). Options with free text hold no secrets; `ToString()` not required.
4. **Create `BiasDetectionOptionsValidator.cs`** (`IValidateOptions<BiasDetectionOptions>`): level in (0,1), thresholds in range, depth >= 1, depth > 2 requires the flag, minimum size >= 2, unique attribute names, acknowledgements refer to registered special-category attributes.
5. **Create `Metrics/ThresholdEvaluator.cs`** (internal static): `FairnessVerdict Evaluate(MetricDirection, ConfidenceInterval, double threshold)` used by every metric (refactor Phase 3 metrics to call it); a subgroup below the minimum size yields Inconclusive.
6. **Tests**: unit, property (verdict monotonic in threshold), guard, validator tests for every failure message; manifest targets.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of issue #838 in Encina.Compliance.AIAct.BiasDetection.

CONTEXT:
- Phases 1-3 are merged. Design Choices 4, 6 and 10 of the plan define intervals, minimum subgroup size and thresholds.

TASK:
Add CalibrationByGroupMetric, CounterfactualFairnessMetric, BiasDetectionOptions with its validator, and ThresholdEvaluator;
route every metric's verdict through ThresholdEvaluator.

KEY RULES:
- Defaults: confidence level 0.95, disparate impact >= 0.8, gaps <= 0.1, minimum subgroup size 30, depth 1; the 0.1 and 30 figures
  are documented engineering defaults, not legal values.
- Options validated by IValidateOptions; every failure is a clear message naming the option.
- A subgroup below the minimum size is Inconclusive, never Pass.
- CRAP <= 10; tests execute real classes.

REFERENCE FILES:
- src/Encina.Compliance.AIAct/AIActOptions.cs
- src/Encina.Compliance.AIAct/AIActOptionsValidator.cs
- src/Encina.Compliance.AIAct.BiasDetection/Metrics/
- tests/Encina.UnitTests/Compliance/AIAct/AIActOptionsValidatorTests.cs
```

</details>

---

### Phase 5: Registry, Intersections, Conflict Detection, Detector & Seam (PR 5)

> **Goal**: `IBiasDetector` end to end, wired into `IDataQualityValidator` without overriding an application's own registration.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create `Model/ProtectedAttribute.cs`**: `Name`, `LegalBasis` (for example "EU Charter Art. 21"), `KnownValues`, `bool IsSpecialCategory`.
2. **Create `Abstractions/IProtectedAttributeRegistry.cs`**: `ValueTask<Either<EncinaError, ProtectedAttribute>> GetAttributeAsync(string name, ...)`, `ValueTask<Either<EncinaError, IReadOnlyList<ProtectedAttribute>>> GetAllAttributesAsync(...)`; **`OptionsProtectedAttributeRegistry`** reads `IOptions<BiasDetectionOptions>` (Design Choice 7).
3. **Create `Abstractions/IBiasDetector.cs`**: `ValueTask<Either<EncinaError, BiasReport>> DetectBiasAsync(BiasDetectionRequest request, CancellationToken cancellationToken = default)`.
4. **Create `Intersections/SubgroupEnumerator.cs`** (internal): builds the attribute-combination keys for depth 1, 2 or more, honoring `AllowHigherOrderIntersections` and `MaxIntersections` (`Left(aiact.bias_intersection_refused)` beyond them).
5. **Create `Conflicts/MetricConflictDetector.cs`** (internal): per attribute key, a conflict when at least one metric is Pass and another Fail; fills `MetricConflict.SmallestSubgroupSize` and `InvolvesSubgroupBelowMinimum`.
6. **Create `DefaultBiasDetector.cs`**: validate request, resolve attributes through the registry (unregistered is `Left`; special category without acknowledgement is `Left(aiact.bias_special_category_refused)`), open the source (`Left(aiact.bias_source_not_registered)` when absent), call the calculator, run the conflict detector, build `BiasReport` (`OverallFairness`, `RequiresHumanReview`, `EvaluatedAtUtc` from `TimeProvider`), map a mid-stream failure to `Left(aiact.bias_source_failed)`.
7. **Create `BiasAwareDataQualityValidator.cs`**: `DetectBiasAsync(datasetId, attributes, ct)` builds a `BiasDetectionRequest` with the default metric set and calls `IBiasDetector`; `ValidateTrainingDataAsync` delegates to the stub.
8. **Create `ServiceCollectionExtensions.cs`**: `AddEncinaAIActBiasDetection(this IServiceCollection, Action<BiasDetectionOptions>? configure = null)` registers options, `IValidateOptions`, `TimeProvider`, the metrics (`TryAddEnumerable`), `IFairnessMetricCalculator`, `IBiasDetector`, `IProtectedAttributeRegistry` with `TryAdd`, and replaces `IDataQualityValidator` only while the descriptor is `DefaultDataQualityValidator` (or absent) in either registration order.
9. **Tests**: unit tests for every `Left` path and the conflict example from the comments (demographic parity fails, disparate impact passes, subgroup n=12); intersection tiers; DI test building the provider with `ValidateOnBuild` and `ValidateScopes`, in both registration orders, with and without an application `IDataQualityValidator`; contract tests of the three interfaces on real implementations; guard tests; manifest targets.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are implementing Phase 5 of issue #838 in Encina.Compliance.AIAct.BiasDetection.

CONTEXT:
- Phases 1-4 are merged: models, abstractions, metrics, calculator, options and validator exist.
- The core registration AddEncinaAIAct does TryAddSingleton<IDataQualityValidator, DefaultDataQualityValidator>.

TASK:
Implement ProtectedAttribute, IProtectedAttributeRegistry (options-backed), IBiasDetector with DefaultBiasDetector,
SubgroupEnumerator, MetricConflictDetector, BiasAwareDataQualityValidator and AddEncinaAIActBiasDetection.

KEY RULES:
- Fail closed: unregistered attribute, special category without acknowledgement, missing source, over-deep intersections and
  stream failures are Left values, never a silent pass.
- Registration completeness (AGENTS.md section 3): every option type and dependency registered; DI test with ValidateOnBuild and
  ValidateScopes in both registration orders; the application's own IDataQualityValidator is never replaced.
- A conflict never auto-enforces: it sets RequiresHumanReview and OverallFairness=false.
- Time from TimeProvider; EncinaError.Message never logged.

REFERENCE FILES:
- src/Encina.Compliance.AIAct/ServiceCollectionExtensions.cs
- src/Encina.Compliance.AIAct/InMemoryAISystemRegistry.cs
- src/Encina.Compliance.AIAct/DefaultDataQualityValidator.cs
- tests/Encina.UnitTests/Compliance/AIAct/ServiceCollectionExtensionsTests.cs
```

</details>

---

### Phase 6: Cross-Cutting Integration (PR 6)

> **Goal**: Audit notification and the remaining ADR-018 functions marked for this feature.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create `Notifications/BiasConflictDetectedNotification.cs`** (`sealed record ... : INotification`): `DatasetId`, `ProtectedAttribute`, failing and passing metric names, `SmallestSubgroupSize`, `OccurredAtUtc`; never attribute values or rows.
2. **Modify `DefaultBiasDetector`**: publish one notification per conflict through `IEncina.Publish`; a publish `Left` fails the run visibly (logged by code, returned as `Left`), never reports success (`AGENTS.md` §3).
3. **Modify `DefaultBiasDetector`**: validate the request before any work (dataset id not blank, attributes non-empty and distinct, depth within options) with `Left(aiact.bias_invalid_request)`.
4. **Defer**: tenant scoping (#845) and `ModuleId` scoping (#846) of the AI Act enforcement; no issue file is needed, both exist.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
You are implementing Phase 6 of issue #838 in Encina.Compliance.AIAct.BiasDetection.

CONTEXT:
- DefaultBiasDetector exists (Phase 5). Notifications of the core live in src/Encina.Compliance.AIAct/Notifications/ and are
  published through IEncina.Publish (see InMemoryAISystemRegistry.cs:109).

TASK:
Add BiasConflictDetectedNotification, publish it for every conflict, add request validation, and cover both with tests.

KEY RULES:
- A Left from IEncina.Publish fails the operation; it is never swallowed (AGENTS.md section 3).
- Notifications carry names and counts, never rows, attribute values or EncinaError.Message.
- Tenant and module scoping are deferred to #845 and #846.

REFERENCE FILES:
- src/Encina.Compliance.AIAct/Notifications/ProhibitedUseBlockedNotification.cs
- src/Encina.Compliance.AIAct/InMemoryAISystemRegistry.cs
- src/Encina.Compliance.AIAct.BiasDetection/DefaultBiasDetector.cs
```

</details>

---

### Phase 7: Observability (PR 6)

> **Goal**: A span, three instruments and fourteen structured log events.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create `Diagnostics/BiasDetectionDiagnostics.cs`** (internal): `ActivitySource` and `Meter` named `Encina.Compliance.AIAct.BiasDetection`; span `aiact.bias_detection.detect` (attributes: dataset id, metric count, outcome); counters `aiact.bias_detection.total` (tag `aiact.outcome`) and `aiact.bias_detection.conflicts`; histogram `aiact.bias_detection.duration` (ms). The dataset id is a span attribute only, never a metric tag.
2. **Create `Diagnostics/BiasDetectionLogMessages.cs`** with `[LoggerMessage]`, EventIds 9548-9561 packed: 9548 detection started, 9549 completed, 9550 request invalid, 9551 attribute not registered, 9552 special category refused, 9553 intersection refused, 9554 source not registered, 9555 source failed, 9556 metric input missing, 9557 subgroup below minimum, 9558 indicator failed, 9559 indicator inconclusive, 9560 metric conflict detected, 9561 notification publish failed. Errors are logged by code; exceptions through `ForLogging()`.
3. **Modify `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`**: add `["Encina.Compliance.AIAct.BiasDetection"] = [nameof(EventIdRanges.ComplianceAIAct)]`; add the sub-allocation note to the `ComplianceAIAct` XML summary in `EventIdRanges.cs`.
4. **Modify `DefaultBiasDetector`** to start the span, record instruments and call the log extension methods.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
You are implementing Phase 7 of issue #838 in Encina.Compliance.AIAct.BiasDetection.

CONTEXT:
- The package range is inside EventIdRanges.ComplianceAIAct (9500-9599): 9500-9512 core, 9513-9529 #847, 9530-9547 #842,
  9548-9561 this issue.

TASK:
Create BiasDetectionDiagnostics and BiasDetectionLogMessages (14 [LoggerMessage] methods, ids 9548-9561, no gaps), map the
assembly in EncinaEventIdAllocationTests, and instrument DefaultBiasDetector.

KEY RULES:
- ADR-021: no id outside the registered range, no sparse allocation; run the architecture tests.
- Never log or tag EncinaError.Message or exception messages; error codes and ForLogging() only.
- Activities start only when ActivitySource.HasListeners(); no high-cardinality metric tags.
- XML doc line: Event IDs: 9548-9561 (see EventIdRanges.ComplianceAIAct).

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
- src/Encina.Compliance.AIAct/Diagnostics/AIActDiagnostics.cs
- src/Encina.Compliance.AIAct/Diagnostics/AIActLogMessages.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

---

### Phase 8: Testing (spread over the PRs, completed in PR 6)

> **Goal**: Every flag reaches its target; the hot path is benchmarked.

<details>
<summary><strong>Tasks</strong></summary>

1. **Unit** (`tests/Encina.UnitTests/Compliance/AIAct/BiasDetection/`): intervals against published worked examples, each metric on hand-computed datasets, thresholds and verdict boundaries, registry, intersections, conflicts (including the n=12 example), detector `Left` paths, validator, composite validator, notification publishing failure, telemetry via listeners.
2. **Guard** (`tests/Encina.GuardTests/Compliance/AIAct/BiasDetection/`): every public constructor and method parameter.
3. **Contract** (`tests/Encina.ContractTests/Compliance/AIAct/BiasDetection/`): `IBiasDetector`, `IFairnessMetricCalculator`, `IProtectedAttributeRegistry`, `IBiasObservationSource` contracts exercised on real implementations and a test source.
4. **Property** (`tests/Encina.PropertyTests/Compliance/AIAct/BiasDetection/`): scores in range, `Lower <= Score <= Upper`, permutation invariance, verdict monotonic in threshold, conflict symmetry (metric order does not change the conflict set).
5. **DI test**: provider built with `ValidateOnBuild` and `ValidateScopes` (Phase 5).
6. **Integration**: `.md` justification (stateless, no store, no external system) at `tests/Encina.IntegrationTests/Compliance/AIAct/BiasDetection/BiasDetection.md`, until #847 or a later issue adds a Marten-backed registry. **Load**: `.md` justification (pure calculations, no shared mutable state). **Benchmark**: `tests/Encina.BenchmarkTests/Compliance/AIAct/BiasDetection/` one BenchmarkDotNet class through `BenchmarkSwitcher` for the single-pass aggregation over 1M observations; results under `artifacts/performance/`.
7. **Coverage manifest**: `.github/coverage-manifest/Encina.Compliance.AIAct.BiasDetection.json` with per-file targets and one-sentence justifications for every new file; measure each flag with `dotnet test tests\Encina.<Flag>Tests --collect "XPlat Code Coverage"`, then `coverage-report.cs` and `--check-justifications`; local CRAP table for changed methods (<= 10).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```
You are completing the tests of issue #838 in Encina.Compliance.AIAct.BiasDetection.

CONTEXT:
- Phases 1-7 are implemented; earlier PRs brought their own tests. Missing pieces are the benchmark, the justification files and
  any flag below its manifest target.

TASK:
Write the integration and load .md justifications, the benchmark, and the tests needed to bring unit, guard, contract and
property flags to their targets in .github/coverage-manifest/Encina.Compliance.AIAct.BiasDetection.json.

KEY RULES:
- Tests execute real package code; Shouldly via Encina.Testing.Shouldly; no FluentAssertions; no Thread.Sleep.
- BenchmarkSwitcher.FromAssembly(...).Run(args, config), never BenchmarkRunner.Run<T>(); outputs under artifacts/.
- Justification files follow the template of AGENTS.md section 9 (Status, Justification, Adequate Coverage, Alternative, Date, Issue).
- Do not lower any target; CRAP <= 10 on changed methods.

REFERENCE FILES:
- tests/Encina.UnitTests/Compliance/AIAct/
- tests/Encina.BenchmarkTests/Compliance/AIAct/AIAct.md
- tests/Encina.LoadTests/Compliance/AIAct/AIAct.md
- docs/testing/coverage-measurement-methodology.md
```

</details>

---

### Phase 9: Documentation & Finalization (PR 7)

> **Goal**: Documentation, changelog, public API and verification.

<details>
<summary><strong>Tasks</strong></summary>

1. XML doc comments on all new public APIs (`<summary>`, `<remarks>` with the article, `<param>`, `<returns>`, `<example>`).
2. `changelog.d/838-aiact-bias-detection.added.md` (one fragment, written by `mechanical-fixer`).
3. `src/Encina.Compliance.AIAct.BiasDetection/README.md`: registration, observation source adapter example, metric table, thresholds, human review, Art. 4a note.
4. `docs/features/aiact-compliance.md`: Art. 10(2)(f) and (g) section (what is measured, how to read verdicts and conflicts, intersection tiers, dates of Reg. (EU) 2026/1744), through `docs-writer`.
5. `docs/INVENTORY.md`: new package and files; `ROADMAP.md`: mark the v0.16.0 bias detection item.
6. ADR (number: the next one neither used nor reserved in `docs/architecture/adr/index.md` when PR 1 starts; reserve it in that PR): statistics approach, verdicts and conflict escalation, observation source.
7. `PublicAPI.Unshipped.txt` complete in both packages; build `dotnet build Encina.slnx --configuration Release` (0 errors, 0 warnings); `dotnet test`; every flag at its target.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 9</strong></summary>

```
You are implementing Phase 9 of issue #838 in Encina.Compliance.AIAct.BiasDetection.

CONTEXT:
- Phases 1-8 are merged and measured.

TASK:
Write the changelog fragment, package README, feature-page section, INVENTORY and ROADMAP entries and the ADR; finish the public
API files; verify build, tests and per-flag coverage.

KEY RULES:
- Never edit CHANGELOG.md [Unreleased]; add changelog.d/838-aiact-bias-detection.added.md.
- Documentation never types coverage figures by hand (covref markers, SPEC-001); pages go through the docs-writer agent.
- State that 0.8 is the only legal figure and that other defaults are engineering defaults.
- English only; no AI attribution; zero warnings.

REFERENCE FILES:
- changelog.d/README.md
- docs/features/aiact-compliance.md
- .claude/skills/encina-docs/SKILL.md
- docs/architecture/adr/index.md
```

</details>

---

## Research

### Relevant Standards & Specifications

Status of each legal reference, checked on 2026-11-11: "text read" means read on the consolidated text at artificialintelligenceact.eu (the EUR-Lex Official Journal page returned an empty challenge page to this tool, so the Official Journal text itself was not read); everything else is unverified.

| Provision | Requirement | Where in this plan | Status |
|-----------|-------------|--------------------|--------|
| AI Act Art. 10(2)(f) | Examination of data in view of possible biases likely to affect health and safety, negatively impact fundamental rights or lead to discrimination prohibited under Union law, "especially where data outputs influence inputs for future operations" | Phases 3-5 | Text read; the feedback-loop clause is not modeled by this package |
| AI Act Art. 10(2)(g) | Appropriate measures to detect, prevent and mitigate the biases identified under (f) | Phases 3-5 (detect); prevent and mitigate are the application's | Text read |
| AI Act Art. 10(2)(h) | Identification of relevant data gaps or shortcomings | Out of scope (#837) | Text read |
| AI Act Art. 10(3) | Data sets have appropriate statistical properties, including as regards the persons or groups of persons the system is intended for | Phases 3-4 (per-group statistics) | Text read |
| AI Act Art. 4a (replaces Art. 10(5)) | Exceptional processing of special categories of personal data for bias detection and correction, "to the extent strictly necessary", under conditions | Design Choice 8, Phase 5 | Text read for the paragraph 1 lead-in and conditions (a)-(c); conditions beyond (c), later paragraphs and the exact scope after the amendment are unverified |
| AI Act Annex IV point 2(g) | Technical documentation lists validation and testing metrics "as well as potentially discriminatory impacts" | Consumer is #840; shared types in Phase 1 | Text read |
| Reg. (EU) 2026/1744 | Annex III high-risk obligations from 2 Dec 2027, Annex I from 2 Aug 2028; Art. 4a in force from 27 Jul 2026 | Feature page, XML docs | Dates from SPEC-002 §3.2 (maintainer-verified 2026-09-23) and the issue comment; the consolidation shows 27 July 2026 for Art. 4a; not re-read in the Official Journal |
| GDPR Art. 9(1) | Special categories of personal data | Design Choice 8 | Unverified here (well-known text) |
| EU Charter of Fundamental Rights Art. 21 | Non-discrimination (legal basis string of the example attribute) | `ProtectedAttribute.LegalBasis` | Unverified here |
| EEOC Uniform Guidelines, four-fifths rule | Selection rate of a group below 80% of the highest group's rate is evidence of adverse impact (29 CFR 1607.4(D)) | Design Choice 10 | Unverified here; the issue cites the EEOC Q&A page |
| Wilson (1927), Newcombe (1998), Katz et al. (1978) | Interval methods for a rate, a difference of rates and a ratio of rates | Design Choice 4, Phase 2 | Named from memory; the worker confirms the formulas against the original papers |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `IDataQualityValidator`, `DefaultDataQualityValidator` | `src/Encina.Compliance.AIAct/Abstractions/IDataQualityValidator.cs`, `DefaultDataQualityValidator.cs` | Seam wired by `BiasAwareDataQualityValidator`; stub kept for `ValidateTrainingDataAsync` |
| `BiasReport`, `BiasIndicator` | `src/Encina.Compliance.AIAct/Model/` | Extended in Phase 1 and produced by the detector |
| `AddEncinaAIAct` registration | `src/Encina.Compliance.AIAct/ServiceCollectionExtensions.cs:31` | Pattern (`TryAdd`, `TimeProvider`, options validator); order-independence check |
| `AIActErrors`, `AIActOptionsValidator` | `src/Encina.Compliance.AIAct/` | Error-code naming and validator style |
| Notifications and `IEncina.Publish` | `src/Encina.Compliance.AIAct/Notifications/`, `InMemoryAISystemRegistry.cs:109` | Conflict notification |
| `AIActDiagnostics`, `AIActLogMessages` | `src/Encina.Compliance.AIAct/Diagnostics/` | Style for the package's own source, meter and logging |
| `EventIdRanges.ComplianceAIAct` | `src/Encina/Diagnostics/EventIdRanges.cs:389` | Range 9548-9561 |
| `EncinaEventIdAllocationTests.AssemblyRanges` | `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` | Assembly map entry |
| Coverage manifest generator | `.github/scripts/generate-coverage-manifest.cs`, `.github/coverage-manifest/Encina.Compliance.AIAct.json` | New package manifest |
| Benchmark and load justification folders | `tests/Encina.BenchmarkTests/Compliance/AIAct/AIAct.md`, `tests/Encina.LoadTests/Compliance/AIAct/AIAct.md` | Style of the `.md` files |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Compliance.AIAct` (#415) | 9500-9512 | `Diagnostics/AIActLogMessages.cs`, in use |
| `Encina.Compliance.AIAct` (#847) | 9513-9529 | Reserved by the #847 plan |
| `Encina.Compliance.AIAct` (#842) | 9530-9547 | Reserved by the #842 plan |
| **`Encina.Compliance.AIAct.BiasDetection` (#838)** | **9548-9561** | **14 ids inside `ComplianceAIAct`, no new range; 9562-9599 stay for #840, #841 and #843** |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Core shared types and report (Phase 1) | 8 | 5 new records/enums, 2 modified records, core `PublicAPI.Unshipped.txt` |
| Package project files (Phase 2) | 5 | csproj, two PublicAPI files, `Encina.slnx`, coverage manifest |
| Package models | 5 | `BiasObservation`, `BiasDetectionRequest`, `FairnessMetricRequest`, `FairnessMetrics`, `ProtectedAttribute` |
| Abstractions | 4 | `IBiasObservationSource`, `IFairnessMetricCalculator`, `IBiasDetector`, `IProtectedAttributeRegistry` |
| Statistics and metrics (Phases 2-4) | 11 | `IntervalStatistics`, `IFairnessMetric`, `GroupCounters`, six metrics, `ThresholdEvaluator`, calculator |
| Detector, registry, intersections, conflicts (Phase 5) | 5 | `DefaultBiasDetector`, `OptionsProtectedAttributeRegistry`, `SubgroupEnumerator`, `MetricConflictDetector`, `BiasAwareDataQualityValidator` |
| Options, validator, errors, DI | 4 | `BiasDetectionOptions`, validator, `BiasDetectionErrors`, `ServiceCollectionExtensions` |
| Notification and diagnostics (Phases 6-7) | 3 | `BiasConflictDetectedNotification`, `BiasDetectionDiagnostics`, `BiasDetectionLogMessages` |
| Architecture test map and registry note | 2 | `EncinaEventIdAllocationTests.cs`, `EventIdRanges.cs` |
| Tests (Phase 8) | ~23 | unit, guard, contract, property, DI, benchmark, two `.md` justifications |
| Documentation (Phase 9) | 6 | changelog, README, feature page, ADR, INVENTORY, ROADMAP |
| **Total** | **~76** | **Seven pull requests of 6 to 15 files** |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
You are implementing issue #838 — EU AI Act Art. 10(2)(f) bias detection and fairness metrics — as the new package
Encina.Compliance.AIAct.BiasDetection plus an extension of Encina.Compliance.AIAct. Work one phase per pull request.

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library, pre-1.0: no backward compatibility, best solution always, no [Obsolete].
- Railway Oriented Programming: every operation returns Either<EncinaError, T>; a failure is never reported as success.
- Compliance modules fail closed; the analysis never reports a dataset as fair while a metric conflicts, is inconclusive or a
  subgroup is too small. Event-sourced modules have no InMemory stores (the protected attribute catalog is options-declared).
- The package is stateless and holds no rows: observations stream through IBiasObservationSource.

IMPLEMENTATION OVERVIEW:
Phase 1: core Model types FairnessMetricType, FairnessVerdict, ConfidenceInterval, SubgroupSize, MetricConflict; extended
         BiasIndicator (Metric, Score, Interval, Threshold, Verdict, SubgroupSizes) and BiasReport (Conflicts, RequiresHumanReview).
Phase 2: package skeleton, errors, IBiasObservationSource, IFairnessMetricCalculator, request/result records, IntervalStatistics
         (Wilson, Newcombe, Katz, normal difference).
Phase 3: GroupCounters, disparate impact, demographic parity, equalized odds, predictive parity, DefaultFairnessMetricCalculator.
Phase 4: calibration by group, counterfactual fairness, BiasDetectionOptions + validator, ThresholdEvaluator.
Phase 5: ProtectedAttribute, options-backed registry, IBiasDetector/DefaultBiasDetector, SubgroupEnumerator,
         MetricConflictDetector, BiasAwareDataQualityValidator, AddEncinaAIActBiasDetection with order-independent wiring.
Phase 6: BiasConflictDetectedNotification, request validation.
Phase 7: ActivitySource/Meter, 14 [LoggerMessage] events 9548-9561 inside ComplianceAIAct, architecture-test map.
Phase 8: unit, guard, contract, property, DI test, benchmark, integration and load .md justifications, coverage manifest.
Phase 9: docs-writer pages, changelog fragment, ADR, INVENTORY, ROADMAP, PublicAPI files.

KEY PATTERNS:
- TimeProvider only; cancellation propagates; no exception escapes a background path.
- EncinaError.Message and exception messages never reach logs or tags; error codes and ForLogging() only.
- TryAdd registrations; DI completeness test with ValidateOnBuild and ValidateScopes.
- Verdict: Pass when the interval is wholly on the passing side, Fail when wholly on the failing side, else Inconclusive.
- Defaults: level 0.95, disparate impact >= 0.8 (EEOC), gaps <= 0.1 and minimum subgroup 30 (engineering defaults), depth 1.
- CRAP <= 10 on every changed method; per-flag targets from .github/coverage-manifest/Encina.Compliance.AIAct.BiasDetection.json.

REFERENCE FILES:
- src/Encina.Compliance.AIAct/DefaultDataQualityValidator.cs
- src/Encina.Compliance.AIAct/Model/BiasReport.cs
- src/Encina.Compliance.AIAct/ServiceCollectionExtensions.cs
- src/Encina.Compliance.AIAct/Diagnostics/AIActLogMessages.cs
- src/Encina/Diagnostics/EventIdRanges.cs
- docs/plans/aiact-record-keeping-implementation-plan-842.md
- docs/plans/aiact-marten-event-sourcing-implementation-plan-847.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | **Caching** | ❌ N/A | Results are per dataset and request, computed from a stream; a cached report would hide a changed dataset |
| 2 | **OpenTelemetry** | ✅ Phase 7 | Span `aiact.bias_detection.detect`; counters `aiact.bias_detection.total`, `aiact.bias_detection.conflicts`; histogram `aiact.bias_detection.duration` |
| 3 | **Structured Logging** | ✅ Phase 7 | 14 `[LoggerMessage]` events, ids 9548-9561 inside `ComplianceAIAct` (ADR-021) |
| 4 | **Health Checks** | ❌ N/A | The calculators are stateless and the package owns no checkable dependency; the observation source is the application's |
| 5 | **Validation** | ✅ Phases 4-6 | Options validator; request validation before any work (dataset id, attributes, depth) |
| 6 | **Resilience** | ❌ N/A | No external call in the default implementation; an `IBiasObservationSource` or a backend behind `IBiasDetector` owns its own resilience |
| 7 | **Distributed Locks** | ❌ N/A | No shared mutable state or background processor |
| 8 | **Transactions** | ❌ N/A | Calculations are pure and persist nothing |
| 9 | **Idempotency** | ❌ N/A | A detection run is a pure function of its input; repeating it is safe |
| 10 | **Multi-Tenancy** | ⏭️ Defer ([#845](https://github.com/dlrivada/Encina/issues/845)) | Tenant-scoped AI Act enforcement is delivered package-wide by #845; the report carries no tenant data |
| 11 | **Module Isolation** | ⏭️ Defer ([#846](https://github.com/dlrivada/Encina/issues/846)) | `ModuleId`-scoped enforcement is delivered package-wide by #846 |
| 12 | **Audit Trail** | ✅ Phase 6 | `BiasConflictDetectedNotification` per conflict through `IEncina.Publish`, like the core `Notifications/*`; a publish failure fails the run |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | State | Why |
|---|---|---|
| `Encina.Compliance.AIAct` core ([#415](https://github.com/dlrivada/Encina/issues/415)) | Closed, in `main` | `BiasReport`, `BiasIndicator`, `IDataQualityValidator`, registration pattern |

No other issue blocks this plan; `needs-decision` issues #3419 and #3400 are not among its dependencies.

### Recommended (Not Blocking)

- [#837](https://github.com/dlrivada/Encina/issues/837) data governance: consumes `IBiasDetector` and takes over `ValidateTrainingDataAsync`; its plan should agree on Design Choice 2 (the composite delegates `ValidateTrainingDataAsync` to whatever `DefaultDataQualityValidator` becomes).
- [#840](https://github.com/dlrivada/Encina/issues/840) technical documentation: reads `BiasReport` and the shared types of Phase 1 for Annex IV point 2(g).
- [#847](https://github.com/dlrivada/Encina/issues/847): a Marten-backed `IProtectedAttributeRegistry` if the maintainer wants runtime-editable catalogs (Design Choice 7, option C).
- [#845](https://github.com/dlrivada/Encina/issues/845), [#846](https://github.com/dlrivada/Encina/issues/846): tenant and module scoping of the rest of the AI Act package.
- The EventId split of 9562-9599 among the family plans (#840, #841, #843) must be reconciled when they merge.

---

## Next Steps

1. The maintainer answers Design Choices 2, 3, 4, 6, 7 and 8 when work on #838 starts; the orchestrator records the answers in a `## Maintainer Decisions` section after this one and runs `check-plan` without `-Draft`.
2. `adversarial-reviewer` tries to break the Settled and Trivial choices (1, 5, 9, 10) before the first worker brief, and the `decision-extractor` lists any choice this plan left implicit.
3. Brief the seven pull requests in order; each brief carries its `Size:` line re-measured, its per-file coverage targets and the local CRAP command.
4. Update #838's acceptance criteria to the decided scope (per-metric thresholds, three-valued verdicts, observation source, read-only registry if option A stands).
