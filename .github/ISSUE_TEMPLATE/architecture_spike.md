---
name: Architecture Decision / Spike
about: Investigate a technical approach, evaluate options, or document an architecture decision
title: "[SPIKE] "
labels: investigation
assignees: ''
---

## Context

What is the problem or question that needs investigation? Why is this decision needed now?

## Options to Evaluate

Evaluate only real options. A choice with a single option that complies is Settled: write it with its verifiable citation (an `AGENTS.md` section or rule, a SPEC id, an ADR number, a law article, a link to an earlier maintainer decision, or named comparable products) instead of inventing alternatives. A Dilemma has two or more valid options, each better in a concrete named scenario; name that scenario for each option. Only an issue with a Dilemma carries the `needs-decision` label. Zero Dilemmas is valid: write "No open design choices" and the settled table.

### Option A: [Name]

- **Description**: How this option works
- **Pros**: Benefits
- **Cons**: Drawbacks
- **Effort**: Estimated complexity

### Option B: [Name]

- **Description**: How this option works
- **Pros**: Benefits
- **Cons**: Drawbacks
- **Effort**: Estimated complexity

### Option C: [Name] (if applicable)

- **Description**: How this option works
- **Pros**: Benefits
- **Cons**: Drawbacks
- **Effort**: Estimated complexity

## Decision Criteria

What factors will determine the best option?

- [ ] Performance impact
- [ ] Complexity / maintainability
- [ ] Ecosystem compatibility (.NET 10)
- [ ] Provider coverage (10 database providers)
- [ ] Breaking change impact
- [ ] Community/industry adoption
- [ ] Other: ___

## Scope

### In Scope

- What this investigation covers

### Out of Scope

- What this investigation does NOT cover

## Deliverables

- [ ] ADR document in `docs/architecture/adr/`
- [ ] Proof of concept / prototype
- [ ] Technical document in `docs/`
- [ ] Migration guide (if changing existing approach)
- [ ] Updated ROADMAP.md (if applicable)

## Time Box

Estimated investigation time: [e.g., 2 hours, 1 day]

## Related Issues

- #___ - Description
