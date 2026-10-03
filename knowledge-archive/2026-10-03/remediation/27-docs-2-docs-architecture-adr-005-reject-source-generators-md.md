<!--
title: [DEBT] ADR-005 rejects source generators while open issues #50 and #51 plan one, with no superseding ADR
labels: technical-debt
milestone: 
kind: docs
-->

## Type

- [ ] Failing tests
- [ ] Missing tests
- [ ] Code quality (warnings, analyzers)
- [ ] Performance optimization
- [ ] Refactoring needed
- [x] Documentation gap
- [ ] Incorrect implementation
- [ ] Other

## Description

`docs/architecture/adr/005-reject-source-generators.md` has "**Status:** Rejected" (line 10) and states "We REJECT the use of Source Generators for handler dispatch" (line 75). It is indexed as "Reject Source Generators" in `docs/architecture/adr/index.md:17`. Open issues #50 and #51, the `ROADMAP.md` Performance bullet (line 324) and `docs/INVENTORY.md:4421-4442` plan exactly that generator: compile-time handler discovery, switch-based dispatch and NativeAOT support. No ADR records a decision to build it. Neither `ROADMAP.md` nor `docs/INVENTORY.md` mentions ADR-005 (searched for `ADR-005` and `005-reject`), and no ADR in `docs/architecture/adr/` supersedes ADR-005.

ADR-005 also disagrees with itself: its status is Rejected (line 10), while its "Native AOT Consideration" section ends with "**Status:** Deferred until Native AOT is a concrete requirement." (line 293). The other ADRs that touch the topic do not resolve it: `docs/architecture/adr/003-caching-strategy.md:374` mentions an optional future `Encina.SourceGenerators` package, and `docs/architecture/adr/007-extensibility-strategy.md:412` only points to ADR-005.

## Location

- **File(s)**: `docs/architecture/adr/005-reject-source-generators.md:10`, `:75`, `:293`; `docs/architecture/adr/index.md:17`; `docs/architecture/adr/003-caching-strategy.md:374`; `ROADMAP.md:324`; `docs/INVENTORY.md:4421-4442`
- **Package(s)**: None (documentation only)

## Current Behavior

An accepted ADR rejects source generators for handler dispatch, and the project plans and tracks the opposite work (#50 and #51) with no ADR that supersedes or amends the rejection. ADR-005 carries two different statuses (Rejected at line 10, Deferred at line 293).

## Expected Behavior

The decision record and the plan agree. When #50 is taken up, a new ADR supersedes ADR-005, records why the earlier rejection no longer holds (for example a concrete Native AOT requirement, which is the condition ADR-005 itself names at line 293), and is linked from `docs/architecture/adr/index.md`, `ROADMAP.md` and `docs/INVENTORY.md`. The accepted ADR-005 is not edited to hide the earlier decision.

## Root Cause

ADR-005 is dated 2025-12-12 (`docs/architecture/adr/005-reject-source-generators.md:11`), and #50 was created on 2025-12-24, after the rejection. The generator was therefore planned after ADR-005 rejected it, and the ADR was never revisited or superseded when the plan was made. ADR-005 also mixes a final Rejected status with a conditional "Deferred" note, which leaves its intent ambiguous.

## Proposed Fix

Write a new ADR, when #50 is taken up, that supersedes ADR-005 and records the reversal and its trigger. Link it from the ADR index, `ROADMAP.md` and `docs/INVENTORY.md`. Until then, cross-reference ADR-005 from the pages that describe #50 and #51 so a reader sees the standing decision. Do not rewrite the accepted ADR-005.

## Priority

- [ ] **High** - Blocks functionality or causes failures in production
- [x] **Medium** - Should be fixed before 1.0 release
- [ ] **Low** - Nice to have, can be deferred

## Effort Estimate

- [ ] Small (< 1 hour)
- [x] Medium (1-4 hours)
- [ ] Large (> 4 hours)

## Related Issues

- #27 (This issue)
- #50
- #51
