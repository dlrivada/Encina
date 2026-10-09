# Delta scope of issue #13 (set rules-2026-10)

Reused from the original audit; do not re-derive it. Rules in this delta: see tools/ai/audit/pipeline-delta.json.

**Scope source:** the published record and the published audit result (docs/knowledge/audits/issue-13.md) only: the original audit has no published stage files.

## Knowledge record (docs/knowledge/issues/13.md)

```yaml
schema: 1
nav_exclude: true
issue: 13
title: "[REFACTOR] Apply Orchestrator pattern to Encina.Caching (8 packages)"
closed: 2025-12-23
state_reason: completed
outcome: rejected-reasoned
type: refactor
area: caching
review: verified
packages:
  - Encina.Caching
  - Encina.Caching.Memory
  - Encina.Caching.Hybrid
  - Encina.Caching.Redis
  - Encina.Caching.Garnet
  - Encina.Caching.Valkey
  - Encina.Caching.Dragonfly
  - Encina.Caching.KeyDB
prs:
linked_prs:
knowledge:
  - kind: decision
    statement: "The Orchestrator pattern used for Encina.Validation does not apply to Encina.Caching: the three caching pipeline behaviors (QueryCachingPipelineBehavior<,>, CacheInvalidationPipelineBehavior<,>, DistributedIdempotencyPipelineBehavior<,>) already live centrally in the core Encina.Caching package, and every provider package implements only ICacheProvider (the four Redis-protocol-compatible providers only a ServiceCollectionExtensions.cs), so there is no duplication to remove."
    current: yes
    sources:
      - "quote: \"Behaviors are already centralized\" in Encina.Caching and \"Providers only implement ICacheProvider\" (closing comment by dlrivada, https://github.com/dlrivada/Encina/issues/13#issuecomment-3687895697, 2025-12-23)"
      - "paraphrase: the three behaviors still sit in src/Encina.Caching/Behaviors and the Garnet, Valkey, Dragonfly and KeyDB packages hold only registration code (https://github.com/dlrivada/Encina/tree/main/src/Encina.Caching/Behaviors, re-verified 2026-10-07)"
    destinations:
      - kind: executable-rule
        status: done
        target: "src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs"
  - kind: rejected-alternative
    statement: "A CacheOrchestrator wrapping all 8 caching providers, proposed by the issue on the assumption of about 300-400 duplicated lines across the provider packages, was rejected as over-engineering after the maintainer verified the actual architecture had no such duplication."
    current: yes
    sources:
      - "quote: \"Adding CacheOrchestrator would be over-engineering - No benefit, just added complexity\" (https://github.com/dlrivada/Encina/issues/13#issuecomment-3687895697, 2025-12-23)"
    destinations:
      - kind: executable-rule
        status: done
        target: "src/Encina.Caching/Behaviors/CacheInvalidationPipelineBehavior.cs"
  - kind: rule
    statement: "Not every cross-provider package needs the Orchestrator pattern; it earns its cost only when providers genuinely duplicate behavior (Validation, where each provider had its own pipeline behavior), not when they already share behavior through the core package (Caching)."
    current: unknown
    sources:
      - "quote: \"Validation needed orchestrator because each provider had its own behavior. Caching already has centralized behaviors.\" (https://github.com/dlrivada/Encina/issues/13#issuecomment-3687895697, 2025-12-23)"
    destinations:
      - kind: none
        status: done
  - kind: gotcha
    statement: "The issue's opening premise ('~300-400 duplicated lines' across the caching providers) was factually wrong; the maintainer refuted it by reading the provider code instead of assuming the Validation analogy held. A refactor proposal built on an unverified duplication estimate should be checked against the real source tree before triage, not only before implementation."
    current: unknown
    sources:
      - "paraphrase: the closing comment states 'The issue mentioned ~300-400 duplicated lines but this is not the case' (https://github.com/dlrivada/Encina/issues/13#issuecomment-3687895697, 2025-12-23)"
    destinations:
      - kind: none
        status: done
remediation:
audit:
  checklist: 1
  date: 2026-09-25
  verdict: conforms-with-na
  record: "docs/knowledge/audits/issue-13.md"
```

## Where the knowledge lives (record)

- The decision is encoded in the code: the three behaviors in `src/Encina.Caching/Behaviors/` and provider packages that only implement `ICacheProvider`.
- Before the SPEC-003 migration the reasoning lived only in the issue's closing comment; this record is now its durable home. A one-line note in `AGENTS.md` would be a nice-to-have, not a gap worth an issue.

## Audit result (docs/knowledge/audits/issue-13.md)

# Audit — Issue #13

Checklist version: SPEC-003 §5.2 (AUD-01..AUD-18, §15.3 amendments).
Date: 2026-09-25.
Record: [issues/13.md](../../../issues/13.md).

## Scope determination

Issue #13 closed with outcome `rejected-reasoned` and 0 linked PRs (confirmed via
`gh api repos/dlrivada/Encina/issues/13/timeline --paginate`: only `labeled`, `milestoned`,
`commented`, `closed`, `added_to_project_v2`, `project_v2_item_status_changed` events, plus
5 `cross-referenced` events from unrelated issues #15, #16, #18, #280, #650 — none of them a
PR that implements #13). No code was ever written for this issue.

Per SPEC-003 REQ-010, the full checklist (AUD-01..AUD-18) applies only to records whose
outcome is `delivered`/`partial`, or `no-evidence` with a subject still present in `src/`.
`rejected-reasoned` is none of these, so this issue is **out of scope for the full audit**.
What remains to verify is narrower: is the reasoning the maintainer gave in 2025-12-23 still
true of the code today? If the caching architecture had since grown the duplication the
issue originally (wrongly) assumed, the rejection would need revisiting.

## Re-verification of the closing rationale (measured against today's code)

| Claim (2025-12-23 closing comment) | Verified today (2026-09-25) | Evidence |
|---|---|---|
| `QueryCachingPipelineBehavior<,>`, `CacheInvalidationPipelineBehavior<,>`, `DistributedIdempotencyPipelineBehavior<,>` live centrally in `Encina.Caching` | Confirmed — all three classes still declared only in `src/Encina.Caching/Behaviors/` | `src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs`, `CacheInvalidationPipelineBehavior.cs`, `DistributedIdempotencyPipelineBehavior.cs` |
| Providers (Memory, Hybrid, Redis) implement only `ICacheProvider`, no duplicated behavior | Confirmed — `MemoryCacheProvider`, `HybridCacheProvider`, `RedisCacheProvider` are the only provider classes found in those packages; no `*PipelineBehavior`/`*Orchestrator` class exists in any `Encina.Caching.*` package | `Grep "class.*PipelineBehavior\|class.*Orchestrator"` over `src/Encina.Caching.*/**/*.cs` → 0 matches; `Grep "class \w+CacheProvider"` → `RedisCacheProvider.cs`, `HybridCacheProvider.cs`, `MemoryCacheProvider.cs` |
| Garnet, Valkey, Dragonfly, KeyDB add no provider-specific duplication | Confirmed and stronger than the 2025-12-23 description: these 4 packages contain **no provider class at all**, only `ServiceCollectionExtensions.cs` (+ generated files) — they are Redis-protocol-compatible and reuse `RedisCacheProvider` | `Get-ChildItem -Recurse -Filter *.cs` on `src/Encina.Caching.{Garnet,Valkey,Dragonfly,KeyDB}` → only `GlobalSuppressions.cs`, `ServiceCollectionExtensions.cs` and generated files per package |

Conclusion: the rejection still holds. No architectural drift occurred that would revive the
case for a `CacheOrchestrator`.

## Checklist items

| Item | Outcome | Evidence / reason |
|---|---|---|
| AUD-01 (decision still honored) | n/a | Rejected-reasoned, no delivered decision to drift from; re-verified above that the stated reasoning still matches today's code |
| AUD-02 (12 cross-cutting functions) | n/a | No entities/stores/behaviors/services created by this issue |
| AUD-03 (coverage manifest) | n/a | No code delivered |
| AUD-04 (no reflection-only tests) | n/a | No tests delivered |
| AUD-05 (test types per feature category) | n/a | No code delivered |
| AUD-06 (regression test for a bug) | n/a | Type is `refactor`, not `bug` |
| AUD-07 (provider matrix) | n/a | No code delivered |
| AUD-08 (EventIds) | n/a | No logging added |
| AUD-09 (PublicAPI) | n/a | No public API added |
| AUD-10 (XML docs) | n/a | No code delivered |
| AUD-11 (docs/README accuracy, per-package) | n/a | Out of scope for this per-issue pass (runs once per package per SPEC-003 §15.3); no doc claim was made or changed by this issue |
| AUD-12 (fail-closed defaults) | n/a | No code delivered |
| AUD-13 (`EncinaError.Message` leaks) | n/a | No code delivered |
| AUD-14 (`TimeProvider`) | n/a | No code delivered |
| AUD-15 (secrets in options) | n/a | No code delivered |
| AUD-16 (async DB calls) | n/a | No code delivered |
| AUD-17 (registration completeness / `ValidateOnBuild`) | n/a | No registration code delivered |
| AUD-18 | n/a | No applicable item beyond AUD-01..AUD-17 for this checklist version |

## Coverage per flag

Not applicable — no files in `src/` or `tests/` belong to this issue's scope (0 linked PRs).

## Specialist passes

- `adversarial-reviewer`: skipped. There is no diff, PR or delivered code to review; the issue
  was rejected before any pull request existed.
- `docs-reviewer`: skipped. No documentation or README was created or changed by this issue.

## Findings

| # | Severity | Finding | Evidence | Disposition |
|---|---|---|---|---|
| 1 | minor | Before this SPEC-003 pass, the rejection reasoning existed only in the issue's own closing comment, with no ADR or CLAUDE.md entry a future contributor could discover without re-reading the closed issue. | `gh issue view 13 --comments`; absence of any "CacheOrchestrator" / "Caching...Orchestrator" match in `CLAUDE.md` (`Grep` found none) | Resolved by this audit: `docs/knowledge/issues/13.md` (published; first written under `artifacts/knowledge/issues/`) is now the durable, searchable record (SPEC-003 §5.5 destination). No remediation issue drafted — the fix is the record itself, and a one-line CLAUDE.md addition would be marginal for a rejected, code-free issue. |

No remediation issue drafted. Deduplication search (`gh issue list --state open --search "Orchestrator Caching"`) returned no related open issue, confirming there is nothing outstanding to link.

## Local-model usage

- `remediation-check-13` (via `tools/ai/local-ai-ask.cs`, `Set-Location` in `.claude/worktrees/wia-13`): drafted the remediation-need summary from verified facts (prompt=948, completion=169, 3.4s, 49.7 tok/s). Output: `artifacts/local-ai/out/remediation-check-13.md`. Verified against the source-tree checks above before being trusted.
- The KNOWLEDGE section of the record started from the pre-draft at `artifacts/knowledge/predraft/13.md` (local-model output), verified against the issue, its comments and the timeline API, and corrected/expanded (added `linked_prs: []` confirmation, schema-2 outcome split, provenance links, and the re-verification table above).


