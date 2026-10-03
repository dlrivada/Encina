<!--
title: [DEBT] docs/messaging/transports.md states the "no unified messaging interface" decision as settled fact with no ADR or SPEC link
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

The FAQ answer "Should I use a unified messaging interface? No." in `docs/messaging/transports.md` restates a design decision (native per-transport APIs instead of a unified messaging interface) as settled fact, with no ADR or SPEC link anywhere on the page. No ADR records the decision, so the page can neither link it nor point the reader to its rationale.

## Location

- **File(s)**: `docs/messaging/transports.md:520-528`; `docs/architecture/adr/index.md`
- **Package(s)**: none (documentation only)

## Current Behavior

Lines 520-528 answer "No" and give three reasons, with no link. `docs/architecture/adr/index.md` lists no ADR on the subject. A case-insensitive search of `docs/architecture/adr` for "unified messaging", "unified transport", "transport strategy", "transport abstraction" and "IMessageTransport" returns a single hit, ADR-023 (a table row about coverage targets for message transports, not a decision on the transport API). Several other ADRs (014, 018, 021, 023, 025, 028, 029, 030) mention the word "transport" in other contexts; none decides this question.

The docs house rule is that decisions are linked, not restated, and that when no ADR exists for a decision the page needs, the gap is stated instead of inventing a rationale (`.claude/skills/encina-docs/SKILL.md`, rule 4).

## Expected Behavior

The FAQ answer links the ADR that records the decision in one sentence, or, until one exists, says explicitly that no ADR records it.

## Root Cause

The decision was copied into the page without an ADR being written for it.

## Proposed Fix

Write an ADR for the decision (native per-transport APIs rather than a unified messaging interface, and the use of `Encina.InMemory` for tests), add it to `docs/architecture/adr/index.md`, and link it from the FAQ answer, keeping the restatement to one sentence. If the decision is not to be recorded now, change the answer to state that no ADR covers it yet.

## Priority

- [ ] **High** - Blocks functionality or causes failures in production
- [x] **Medium** - Should be fixed before 1.0 release
- [ ] **Low** - Nice to have, can be deferred

## Effort Estimate

- [ ] Small (< 1 hour)
- [x] Medium (1-4 hours)
- [ ] Large (> 4 hours)

## Related Issues

- #18 (This issue)
