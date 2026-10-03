<!-- issue
title: [DEBT] Name "fail closed by default" as a CLAUDE.md rule for security and compliance pipeline behaviors
labels: technical-debt, area-security, area-compliance
milestone:
-->

## Type

- [x] Documentation gap
- [x] Incorrect implementation

## Description

Three closely-dated issues in the knowledge-migration pilot's recent-issue sample are the same defect shape in different packages: a pipeline behavior or service silently succeeds (fails open) instead of rejecting the operation when it cannot complete a security- or compliance-relevant check.

- #1155: `HMACValidationPipelineBehavior` skipped signature validation entirely (fail-open) whenever `HttpContext` was unavailable, instead of rejecting the request unless an explicit opt-out was configured.
- #1160: Retention enforcement erased every data category of an entity instead of only the expired one — an over-erasure "fail open" on scope, not on error, but the same family of "the safe path was not the default."
- #1161: `LiftHoldAsync` reported success when releasing records failed, and the other-holds check itself failed open on error.

Each was fixed individually and well (see pilot records for #1155 and #1160), including a new ADR for the retention pair (ADR-031). But CLAUDE.md has no general rule stating that security/compliance pipeline behaviors must fail closed by default, so a fourth instance of the same defect shape is not structurally prevented — it would need the same kind of bug report and after-the-fact fix as these three.

## Location

- **File(s)**: `CLAUDE.md` (new rule under "Code Quality Standards" or "Design Principles"); candidate future architecture-test location: `src/Encina.Testing.Architecture/*.cs`
- **Package(s)**: Encina.Security.AntiTampering, Encina.Compliance.Retention, and any future security/compliance pipeline behavior

## Current Behavior

CLAUDE.md documents specific, narrow rules (`TimeProvider` injection, `[JsonIgnore]` for secrets, async DB calls) each citing the issue numbers that motivated them, but has no equivalent rule for fail-closed-by-default in security/compliance code paths, despite three qualifying incidents landing in the same week.

## Expected Behavior

CLAUDE.md's "Code Quality Standards" section gets a new bullet, in the same style as the existing ones (TimeProvider, secrets, async DB calls), stating that pipeline behaviors and services enforcing a security or compliance rule must fail closed by default: any code path that cannot complete its check must reject/deny the operation, and any intentional skip must be an explicit, named, opt-in configuration flag — never a silent fallthrough. Cite #1155, #1160, #1161 as project history, matching the citation style already used for the other rules in that section.

## Root Cause

No single named principle existed to guide the original implementations of `HMACValidationPipelineBehavior`, the retention eraser, and `LiftHoldAsync` toward fail-closed behavior; each was written independently and each defaulted to the easier-to-write fail-open path when an edge case (missing `HttpContext`, a partial erasure, a release failure) was hit.

## Proposed Fix

1. Add the CLAUDE.md bullet as described above.
2. Optionally (larger follow-up, not required for this issue): a lightweight architecture-test heuristic in `Encina.Testing.Architecture` that flags pipeline behaviors under `Security.*` or `Compliance.*` namespaces whose `catch` blocks or early-return branches do not reference an explicit opt-out option type, as a hint for reviewers rather than a hard gate.

## Priority

- [x] **Medium** - Should be fixed before 1.0 release

## Effort Estimate

- [x] Small (< 1 hour)

## Related Issues

- #1155 — HMACValidationPipelineBehavior fails open without HttpContext
- #1160 — Retention enforcement erases every data category instead of only the expired one
- #1161 — LiftHoldAsync reports success when releasing records fails
- Pilot source records: `artifacts/pilot/records/1155.md`, `artifacts/pilot/records/1160.md` (knowledge-migration pilot, 2026-09-24)
