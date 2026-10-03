<!--
title: [DEBT] ROADMAP.md lists open issues #50 and #51 under "Legacy Phase 2 Details (Completed Items)"
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

`ROADMAP.md` places "Source generators for NativeAOT" (#50) and "Switch-based dispatch" (#51) inside the section headed "Legacy Phase 2 Details (Completed Items)", whose lead-in says "Key areas already completed:". Only "Delegate cache optimization" (#49) carries the ✅ mark on that line, but #50 and #51 carry no pending marker, so a reader takes source generators and switch dispatch as delivered. Both issues are still open and the repository contains no source generator: `src/` has no `IIncrementalGenerator` or `ISourceGenerator` implementation and no generator project. The same work is described as planned ("Issues Pendientes (Performance)", "Features planificadas") in `docs/INVENTORY.md`, so the two pages contradict each other.

## Location

- **File(s)**: `ROADMAP.md:318-324` (heading at line 318, lead-in at line 320, the Performance bullet at line 324); `docs/INVENTORY.md:4419-4442` (the planned wording for #50 and #51)
- **Package(s)**: None (documentation only)

## Current Behavior

`ROADMAP.md:324` reads: "**Performance** — ✅ Delegate cache optimization [#49], Source generators for NativeAOT [#50], Switch-based dispatch [#51]", under the heading "Legacy Phase 2 Details (Completed Items)" (line 318) and the lead-in "Key areas already completed:" (line 320). `ROADMAP.md` mentions #50 and #51 only on line 324. `docs/INVENTORY.md:4419-4442` lists both as pending issues with planned features (compile-time handler discovery, switch-based dispatch, NativeAOT compatibility).

## Expected Behavior

`ROADMAP.md` does not present undelivered work as completed. Either #50 and #51 move out of the "Completed Items" section into a section of planned work, or they carry an explicit pending marker, and `ROADMAP.md` and `docs/INVENTORY.md` agree on their status.

## Root Cause

The Performance bullet groups one delivered item (#49) with two items that were planned in the same phase, and only the delivered one received a ✅. When the section was relabelled as completed items, the two open issues stayed in it unmarked.

## Proposed Fix

Move the #50 and #51 entries from `ROADMAP.md:324` to the section that lists planned work (or mark them explicitly as not yet delivered), keeping the issue links. Keep the wording consistent with `docs/INVENTORY.md:4419-4442`. See also the open question of which ADR governs a source generator, tracked separately in the related ADR-005 issue.

## Priority

- [ ] **High** - Blocks functionality or causes failures in production
- [x] **Medium** - Should be fixed before 1.0 release
- [ ] **Low** - Nice to have, can be deferred

## Effort Estimate

- [x] Small (< 1 hour)
- [ ] Medium (1-4 hours)
- [ ] Large (> 4 hours)

## Related Issues

- #27 (This issue)
- #50
- #51
