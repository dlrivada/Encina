# Implementation Plan: Resilience Documentation — `Encina.Polly` and `Encina.Extensions.Resilience`, Rate Limiting and Bulkhead

> **Issue**: [#904](https://github.com/dlrivada/Encina/issues/904)
> **Type**: Feature (documentation only, no production code)
> **Complexity**: Medium (4 phases, one pull request, 5 files)
> **Estimated Scope**: one page of about 450 lines of Markdown, three small edits, one changelog fragment

---

## Summary

Write `docs/features/resilience.md`, the first page about Encina's two resilience packages. The root `README.md` ("Resilience", from line 345) shows `AddEncinaStandardResilience` with no link to a detailed page, and the only resilience page under `docs/features/` is `docs/features/database-resilience.md` (provider resilience). The page, verified against `src/` on origin/main (942c0f124):

- **Package comparison**. `Encina.Polly` is attribute driven (`[Retry]`, `[CircuitBreaker]`, `[Bulkhead]`, `[RateLimit]`, registered by `AddEncinaPolly()` at `src/Encina.Polly/ServiceCollectionExtensions.cs:46`; `AddDatabaseCircuitBreaker` at lines 97 and 134). `Encina.Extensions.Resilience` is options driven (`AddEncinaStandardResilience`, `AddEncinaStandardResilienceFor<TRequest, TResponse>`, `StandardResilienceOptions` with `RateLimiter`, `TotalRequestTimeout`, `Retry`, `CircuitBreaker`, `AttemptTimeout`). Registering both stacks two retries and two circuit breakers on one request ([ADR-055](../architecture/adr/055-resilience-family.md) Context); the page says so and states the ADR direction (one `Encina.Resilience` package, which does not exist in `src/` yet).
- **Per strategy**, defaults read from the attribute sources: retry (`MaxAttempts` 3, `BackoffType` Exponential, `BaseDelayMs` 1000, `MaxDelayMs` 30000, `RetryOnAllExceptions`, `RetryNonIdempotent`), circuit breaker, timeout (only `Encina.Extensions.Resilience` has one; `Encina.Polly` has no timeout attribute, so the original "per-handler timeout" is not documented as a feature), rate limiting (property table of `[RateLimit]`, state diagram of `AdaptiveRateLimiter` over `RateLimitState` `Normal`, `Throttled`, `Recovering`, the denial result of `RateLimitingPipelineBehavior`, which returns `EncinaError.New(errorMessage)` without an error code today, and the relation to the `RateLimiter` of `StandardResilienceOptions`) and bulkhead (`MaxConcurrency` 10, `MaxQueuedActions` 20, `QueueTimeoutMs` 30000).
- **Known limits** stated and linked: the bulkhead queue bound is not atomic (#2628), the queue timeout ignores the injected `TimeProvider` (#2629), `BulkheadAttribute` has no validation so `QueueTimeoutMs = 0` rejects every request (#2630), and the bulkhead key is `typeof(TRequest).Name` (#2275); the rate limiter behaviors #2272 and #2276. All are open today.
- **Hedging** is not implemented (no `Hedging` symbol in `src/`); it is dropped from the page and named as part of the planned `Encina.Resilience` package.
- Pipeline integration (`PipelineStage` ordering), a link to `docs/features/database-resilience.md`, and observability (`encina.retry.attempts_total`, `encina.retry.skipped_total`, `Encina.Retry` from `src/Encina.Polly/Diagnostics/RetryTelemetry.cs`, with the note that instrument names change under [ADR-054](../architecture/adr/054-observability-conventions-and-trace-propagation.md)).

The Polly README classification table and decision flowchart are linked, not copied. The audit comments on the issue (audits of #40 and #53) are acceptance criteria.

**Which provider category applies**: Resilience (Polly, Extensions.Resilience, Extensions.Http.Resilience; `AGENTS.md` section 5). `Encina.Extensions.Http.Resilience` is not part of the issue.
**Affected packages**: `Encina.Polly` and `Encina.Extensions.Resilience`, documented only.

---

## Design Choices

<details>
<summary><strong>1. Location and navigation — <code>docs/features/resilience.md</code>, entry in the features index</strong></summary>

**Class:** Trivial
**Settled by:** `.claude/skills/encina-docs/SKILL.md` section 1 and the sibling `docs/features/database-resilience.md`.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One page `docs/features/resilience.md`, `parent: "Features"`, entry in `docs/features/index.md`** | Follows the sibling feature pages; there is no `docs/toc.yml` | A long page (about 450 lines) |
| **B) One page per strategy (retry, circuit breaker, rate limiting, bulkhead)** | Short pages | The issue and its acceptance criteria ask for one page; the strategies share the comparison and the pipeline section |

### Chosen Option: **A — one page, features index entry**

### Rationale

- The issue specifies one page, and a long page with a clear table of contents serves the comparison better than four pages that repeat it. A later split is cheap if the page outgrows readers.

</details>

<details>
<summary><strong>2. Tutorials index entry — add one or leave the features index only</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Features index only** | `docs/tutorials/index.md` stays what it says ("Guided, hands-on lessons"); one quadrant per index; matches the acceptance criteria, which name only the README and `docs/features/index.md` | The audit comment of #40 asked for a tutorials entry, so that request is answered by a later tutorial instead |
| **B) Add a "Related" line in `docs/tutorials/index.md` pointing to the feature page** | A reader who finishes the quickstart finds the next topic from the tutorials list; answers the audit comment now | Puts a feature page (reference and explanation) in the index of lessons, against the Diataxis rule that an index holds its own quadrant |

### Chosen Option: **A — features index only** (recommended, pending the maintainer)

### Rationale

- A wins when the tutorials index must stay a list of tested lessons; B wins when discoverability from the quickstart matters more than the purity of the index, for example when the tutorials folder stays a single page for a long time. The recommendation keeps the index clean and records a tutorial on resilience as the proper answer to the audit comment.

</details>

<details>
<summary><strong>3. Document the packages as they are, with the ADR-055 direction stated</strong></summary>

**Class:** Settled
**Settled by:** ADR-055 (accepted 2026-10-24, spike #2691): one `Encina.Resilience` package replaces both; nothing of either package has shipped (both `PublicAPI.Shipped.txt` are empty).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Document `Encina.Polly` and `Encina.Extensions.Resilience` as they exist and state the target shape** | The reader can use the code that exists today and does not build on the split that is going away | The page is refreshed when `Encina.Resilience` lands |
| **B) Wait for `Encina.Resilience`** | One rewrite | The issue is in the documentation milestone now; users have no page meanwhile |

### Chosen Option: **A — document what exists, state the direction**

### Rationale

- ADR-055 is accepted but its implementation is open work, so the page must be useful now and honest about the interim split (two retries and two circuit breakers when both packages are registered).

</details>

<details>
<summary><strong>4. Known defects are stated and linked, not hidden</strong></summary>

**Class:** Settled
**Settled by:** the audit comments on #904 (audits of #40 and #53) and `AGENTS.md` section 11 (an identified problem is recorded, never left unrecorded).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A "Known limits" list per strategy linked to #2628, #2629, #2630, #2275, #2272, #2276** | The reader is not surprised by a rejected request or a non-atomic queue bound | The lines are removed when the bugs are fixed |
| **B) Omit them until fixed** | A cleaner page | A reader hits `QueueTimeoutMs = 0` rejecting every request with no warning |

### Chosen Option: **A — state the limits**

### Rationale

- The audit comments name the limits as required content; each line links its bug, and the fixing pull request removes the line in its documentation phase.

</details>

<details>
<summary><strong>5. Defaults and figures — read from source, never typed</strong></summary>

**Class:** Settled
**Settled by:** `AGENTS.md` section 9 and SPEC-001; `.claude/skills/encina-docs/SKILL.md` section 3 rules 2 and 3.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Copy defaults from the attribute classes and the options class, cite benchmarks and coverage by marker** | Correct at writing time and checkable by Grep | The table is regenerated when a default changes |
| **B) Quote the defaults from the original issue text** | Quick | Can drift from the code |

### Chosen Option: **A — read from source**

### Rationale

- The issue's values were re-checked against main today and match the sources (for example `[Bulkhead]` 10, 20, 30000 and `[RateLimit]` 100, 60, 50.0, 60, 2.0, true, 10); the writer re-reads them anyway.

</details>

---

## Implementation Phases

### Phase 1: Page Content — Comparison, Retry, Circuit Breaker and Timeout

<details>
<summary>Tasks</summary>

1. Create `docs/features/resilience.md` with front matter `title: "Resilience"`, `layout: default`, `parent: "Features"`; the opening sentence names the reader (a developer who must protect handlers against transient failures) and what they get.
2. Sections: overview (ADR-055 settled points: a `Left` is classified, not converted to an exception); package comparison table and the double-registration warning; retry (`RetryAttribute`, link the classification table and flowchart of `src/Encina.Polly/README.md`); circuit breaker (`CircuitBreakerAttribute` properties, `AddDatabaseCircuitBreaker`); timeout (`TotalRequestTimeout`, `AttemptTimeout`); strategy selection.
3. A Mermaid decision diagram for strategy selection.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT:
Encina is a pre-1.0 .NET 10 library. Issue #904 asks for a resilience page covering Encina.Polly and Encina.Extensions.Resilience. ADR-055 decides that both are replaced by Encina.Resilience; nothing of the new package exists yet. Documentation only; write no production code.

TASK:
1. Read .claude/skills/encina-docs/SKILL.md and write the compass verdict before writing.
2. Read src/Encina.Polly (attributes, behaviors, ServiceCollectionExtensions.cs, Diagnostics/RetryTelemetry.cs), src/Encina.Extensions.Resilience (ServiceCollectionExtensions.cs, Configuration/StandardResilienceOptions.cs, Behaviors/) and ADR-055.
3. Write the first part of docs/features/resilience.md: overview, package comparison with the double-registration warning, retry, circuit breaker, timeout, strategy selection.
4. Copy every default from the attribute sources; do not type figures.

KEY RULES:
- English only; junior-readable; one Diataxis quadrant per section, other quadrants linked.
- Link ADR-055 and ADR-054; restate decisions in one sentence only.
- Do not document hedging or a per-handler timeout for Encina.Polly: they are not implemented.
- Log or show the error code, never EncinaError.Message.
- Pre-1.0: no migration notes, no Obsolete members.

REFERENCE FILES:
- src/Encina.Polly/ServiceCollectionExtensions.cs, Attributes/RetryAttribute.cs, Attributes/CircuitBreakerAttribute.cs, README.md
- src/Encina.Extensions.Resilience/ServiceCollectionExtensions.cs, Configuration/StandardResilienceOptions.cs
- docs/architecture/adr/055-resilience-family.md, docs/features/database-resilience.md
```

</details>

### Phase 2: Page Content — Rate Limiting, Bulkhead, Pipeline Integration and Observability

<details>
<summary>Tasks</summary>

1. Rate limiting section: `[RateLimit]` property table (`MaxRequestsPerWindow`, `WindowSizeSeconds`, `ErrorThresholdPercent`, `CooldownSeconds`, `RampUpFactor`, `EnableAdaptiveThrottling`, `MinimumThroughputForThrottling`), a Mermaid state diagram of `AdaptiveRateLimiter` (`Normal`, `Throttled`, `Recovering`; the XML summary and the audit comment say "Throttling", the enum member is `Throttled`, so the page uses the code name), the denial result, and the relation to the `RateLimiter` of `StandardResilienceOptions`.
2. Bulkhead section: `[Bulkhead]` property table (`MaxConcurrency`, `MaxQueuedActions`, `QueueTimeoutMs`) and the known limits #2628, #2629, #2630, #2275.
3. Pipeline integration: how the behaviors compose with the others (`PipelineStage` ordering); link `docs/features/database-resilience.md`.
4. Observability: the counters and activity of `RetryTelemetry`, with the ADR-054 rename note.
5. A "Hedging and the planned package" paragraph: not implemented, part of ADR-055.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT:
Phase 1 wrote the first half of docs/features/resilience.md. The audit comments on issue #904 require a rate limiting section (from the audit of #40) and a bulkhead section (from the audit of #53) with the known limits.

TASK:
1. Read src/Encina.Polly/Attributes/RateLimitAttribute.cs, BulkheadAttribute.cs, RateLimiting/ (AdaptiveRateLimiter.cs, RateLimitState.cs, RateLimitResult.cs), Behaviors/RateLimitingPipelineBehavior.cs, BulkheadPipelineBehavior.cs and Bulkhead/BulkheadManager.cs.
2. Write the rate limiting section with the property table, the Mermaid state diagram using the enum names Normal, Throttled and Recovering, the denial result as it is today (EncinaError.New with a plain message, no error code) and the relation to StandardResilienceOptions.RateLimiter.
3. Write the bulkhead section with the property table and the known limits linked to #2628, #2629, #2630 and #2275.
4. Write pipeline integration, observability and the hedging paragraph.
5. Re-check that each linked issue is still open; remove the line of any issue that has been fixed.

KEY RULES:
- Same rules as Phase 1.
- State defects as limits with their issue links; never hide them and never promise a fix date.
- The denial text is not a stable contract: do not quote the message.

REFERENCE FILES:
- src/Encina.Polly/Attributes/, RateLimiting/, Bulkhead/, Behaviors/, Diagnostics/RetryTelemetry.cs
- docs/architecture/adr/054-observability-conventions-and-trace-propagation.md, 055-resilience-family.md
- docs/features/resilience.md (Phase 1)
```

</details>

### Phase 3: Samples and Verification

<details>
<summary>Tasks</summary>

1. One configuration sample for each strategy that exists (retry, circuit breaker, timeout, rate limiting, bulkhead), compiled in a scratch project outside the repository that references `Encina.Polly` and `Encina.Extensions.Resilience`.
2. Run `lychee --offline --config .github/lychee.toml 'docs/**/*.md' 'README.md'`, `npx --yes markdownlint-cli2 "docs/**/*.md" "#docs/INVENTORY.md"` and the `cov-docs-render.cs --check-dangling` command of the encina-docs skill; report any missing tool and check links with `Test-Path`.
3. `docs-reviewer` reviews the page; fix blockers and majors.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT:
docs/features/resilience.md is written (Phases 1 and 2). Verify it; add only the samples the acceptance criteria require.

TASK:
1. Add one configuration sample per existing strategy and compile every C# block in a scratch project outside the repository.
2. Run the link, lint and citation checks of .claude/skills/encina-docs/SKILL.md section 5.
3. Run docs-reviewer on the page and fix blockers and majors.

KEY RULES:
- Never skip a verification step silently; name any missing tool.
- Samples name real API only (Grep each identifier in src/).
- No hand-typed performance or coverage figures.

REFERENCE FILES:
- docs/features/resilience.md
- src/Encina.Polly/ServiceCollectionExtensions.cs, src/Encina.Extensions.Resilience/ServiceCollectionExtensions.cs
- .claude/skills/encina-docs/SKILL.md (sections 5 and 6)
```

</details>

### Phase 4: Navigation, README Cross-Reference and Finalization

<details>
<summary>Tasks</summary>

1. `docs/features/index.md`: add `- [Resilience](resilience.md)` next to `Database Resilience` or in the Core section.
2. `README.md`: link the page from the Resilience section.
3. `docs/tutorials/index.md`: only if the maintainer chooses Design Choice 2 option B.
4. `changelog.d/904-resilience-docs.added.md` (written by `mechanical-fixer`).
5. The README gaps of `src/Encina.Polly/README.md` stay with #2279 and #2635.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT:
The resilience page is written and verified. Wire it into the navigation and the README.

TASK:
1. Add the Resilience entry to docs/features/index.md and link the page from the Resilience section of README.md.
2. Apply the maintainer's answer on the tutorials index (Design Choice 2) before editing docs/tutorials/index.md; without an answer, leave that file alone.
3. Ask mechanical-fixer, naming the worktree path, for changelog.d/904-resilience-docs.added.md.
4. Run changelog-fragments.cs --check and the link and lint checks again.

KEY RULES:
- Never edit CHANGELOG.md.
- Do not edit src/Encina.Polly/README.md here (#2279, #2635 own it).
- No AI attribution in commits.

REFERENCE FILES:
- docs/features/index.md, docs/tutorials/index.md, README.md (Resilience section)
- changelog.d/README.md, .github/scripts/changelog-fragments.cs
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard / Specification | Relevance |
|--------------------------|-----------|
| ADR-055 (resilience family) | Target shape (one `Encina.Resilience`), settled points (a `Left` is classified, not converted to an exception), the double-registration warning |
| ADR-054 (observability conventions) | Instrument names change; the page flags the pending rename |
| ADR-029 (recoverability error classification) | Retry classification linked from the retry section |
| `AGENTS.md` sections 3 and 9, SPEC-001 | Error message rule; no hand-typed figures |
| `.claude/skills/encina-docs/SKILL.md` | Page shape, front matter, verification |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `AddEncinaPolly`, `AddDatabaseCircuitBreaker` | `src/Encina.Polly/ServiceCollectionExtensions.cs` | Registration and the database breaker |
| `RetryAttribute`, `CircuitBreakerAttribute`, `RateLimitAttribute`, `BulkheadAttribute` | `src/Encina.Polly/Attributes/` | Property tables and defaults |
| `RateLimitingPipelineBehavior`, `AdaptiveRateLimiter`, `RateLimitState`, `RateLimitResult` | `src/Encina.Polly/Behaviors/`, `src/Encina.Polly/RateLimiting/` | Rate limiting section and state diagram |
| `BulkheadPipelineBehavior`, `BulkheadManager` | `src/Encina.Polly/Behaviors/`, `src/Encina.Polly/Bulkhead/` | Bulkhead section and known limits |
| `RetryTelemetry` | `src/Encina.Polly/Diagnostics/RetryTelemetry.cs` | Observability section |
| `AddEncinaStandardResilience`, `AddEncinaStandardResilienceFor<,>`, `StandardResilienceOptions`, `StandardResiliencePipelineBehavior` | `src/Encina.Extensions.Resilience/` | Options-driven package and its timeout and rate limiter |
| Polly README (classification table, flowchart) | `src/Encina.Polly/README.md` | Linked from the retry section |
| Database resilience page | `docs/features/database-resilience.md` | Cross-link |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| Encina.Polly | 3900-3949 (`EventIdRanges.Polly`) | Existing; documented only, no new EventId |
| Encina.Extensions.Resilience | 3950-3999 (`EventIdRanges.ExtensionsResilience`) | Existing; documented only |

No new range is needed: this plan adds no logging.

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Feature page | 1 | `docs/features/resilience.md` |
| Navigation entry (`docs/features/index.md`) | 1 | Plus `docs/tutorials/index.md` only if Design Choice 2 becomes option B |
| README cross-reference | 1 | Root `README.md` |
| Changelog fragment | 1 | `changelog.d/904-resilience-docs.added.md` |

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT:
Encina is a pre-1.0 .NET 10 library (C# 14, nullable enabled) with Railway Oriented Programming (Either<EncinaError, T>). Issue #904 asks for a resilience page covering Encina.Polly (attribute driven) and Encina.Extensions.Resilience (options driven). ADR-055 replaces both with Encina.Resilience, which does not exist in src/ yet. Documentation only; write no production code. Follow AGENTS.md and .claude/skills/encina-docs/SKILL.md.

IMPLEMENTATION OVERVIEW:
One pull request of 4 or 5 files: docs/features/resilience.md (package comparison, retry, circuit breaker, timeout, rate limiting with the Normal/Throttled/Recovering state diagram, bulkhead with its known limits, pipeline integration, observability, hedging as planned), a line in docs/features/index.md, a README link, and changelog.d/904-resilience-docs.added.md (mechanical-fixer). Verify with the commands of the encina-docs skill, section 5, and review with docs-reviewer.

KEY PATTERNS:
- just-the-docs front matter (layout: default, parent: "Features").
- Defaults are copied from the attribute sources, never typed from memory.
- Known limits link #2628, #2629, #2630, #2275, #2272 and #2276.
- No hedging, no per-handler timeout for Encina.Polly: not implemented.
- Samples show the error code or exception type, never EncinaError.Message.

REFERENCE FILES:
- src/Encina.Polly/ (Attributes, Behaviors, RateLimiting, Bulkhead, Diagnostics, ServiceCollectionExtensions.cs, README.md)
- src/Encina.Extensions.Resilience/ (ServiceCollectionExtensions.cs, Configuration/StandardResilienceOptions.cs, Behaviors/)
- docs/architecture/adr/054-observability-conventions-and-trace-propagation.md, 055-resilience-family.md
- docs/features/database-resilience.md, docs/features/index.md, README.md
```

</details>

---

## Cross-Cutting Integration Matrix

> Per `AGENTS.md` section 6 (ADR-018): documentation only, so no runtime function is added. A function the page describes is marked ✅ as page content.

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | No runtime code. |
| 2 | OpenTelemetry | ✅ | The retry counters and activity of `RetryTelemetry` are documented as page content, with the ADR-054 rename note. |
| 3 | Structured Logging | ❌ | No runtime code. |
| 4 | Health Checks | ❌ | No runtime code. |
| 5 | Validation | ❌ | No runtime code; the missing attribute validation (#2276, #2630) is stated as a known limit. |
| 6 | Resilience | ✅ | This is the subject of the page. |
| 7 | Distributed Locks | ❌ | No runtime code. |
| 8 | Transactions | ❌ | No runtime code. |
| 9 | Idempotency | ✅ | `RetryNonIdempotent` and the rule that a command is retried only when idempotent (ADR-029, ADR-055) are documented as page content. |
| 10 | Multi-Tenancy | ❌ | No runtime code; limiter partitioning by tenant belongs to the ADR-055 work. |
| 11 | Module Isolation | ❌ | No runtime code. |
| 12 | Audit Trail | ❌ | No runtime code. |

---

## Prerequisites & Dependencies

- No code prerequisite. The bugs #2628, #2629, #2630, #2275, #2272 and #2276 are open; each fixing pull request removes its line from the page.
- The README of `Encina.Polly` is outside this plan (#2279, #2635).
- The page is refreshed when `Encina.Resilience` (ADR-055) ships.

---

## Next Steps

1. Maintainer answers Design Choice 2 (tutorials index) when work on the issue starts.
2. Brief a `docs-writer` with Phase 1 to Phase 4 (one pull request).
3. Link this plan from #904.
