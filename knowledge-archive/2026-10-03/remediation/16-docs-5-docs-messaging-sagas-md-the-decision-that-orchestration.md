<!--
title: [DEBT] Missing ADR link for separate Orchestration and Choreography saga strategies
labels: technical-debt
milestone: 
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

The documentation for saga messaging states that Orchestration and Choreography are two mutually exclusive strategies, but it does not link to any Architecture Decision Record (ADR) or Specification (SPEC) that formalizes this architectural choice. While the decision is real, the lack of a linked reference violates the house rule that decisions must be linked, not just restated in the text.

## Location

- **File(s)**: `docs/messaging/sagas.md`
- **Package(s)**: Encina.Core (Documentation)

## Current Behavior

The page `docs/messaging/sagas.md` describes Orchestration and Choreography as separate systems and includes a FAQ section on migration between them. However, it provides no links to an ADR or SPEC. A search for saga-related ADRs in `docs/architecture/adr/` returns no results, confirming that no such document currently exists in the repository.

## Expected Behavior

The documentation should either link to an existing ADR/SPEC that defines the separation of Orchestration and Choreography, or, if no such document exists, the report/documentation should explicitly state that no ADR exists for this decision, rather than implying a rationale without a reference.

## Root Cause

The architectural decision to keep Orchestration and Choreography as separate, non-unified systems was implemented but not formally recorded in an ADR. The documentation was written to describe the behavior without creating the necessary architectural reference material, leading to a gap between the documented decision and the required traceability.

## Proposed Fix

Create a new Architecture Decision Record (ADR) that documents the rationale for keeping Orchestration and Choreography as mutually exclusive strategies. Update `docs/messaging/sagas.md` to link to this new ADR. If an ADR cannot be created immediately, update the documentation to explicitly note that no ADR currently exists for this decision, in compliance with SKILL.md §3 house rule 4.

## Priority

- [ ] **High** - Blocks functionality or causes failures in production
- [x] **Medium** - Should be fixed before 1.0 release
- [ ] **Low** - Nice to have, can be deferred

## Effort Estimate

- [x] Small (< 1 hour)
- [ ] Medium (1-4 hours)
- [ ] Large (> 4 hours)

## Related Issues

#16