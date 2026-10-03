<!--
title: [DEBT] Agent and skill instructions routed from AGENTS.md section 12 give per-flag and release coverage targets of 85% that contradict section 9
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

Reported by: code 3, docs 5.

AGENTS.md section 12 routes agents to `.opencode/skills/` and `.opencode/agents/` files that state the opposite of AGENTS.md section 9. Section 9 says targets come from `.github/coverage-manifest/{Package}.json`, there is no project-wide percentage, and branch and method coverage are informational. `docs/engineering/AI-DEVELOPMENT-MODEL.md:103` and `:128` already call the generic "line coverage must be >= 85%" rule documentation drift.

The contradicting text:

- `.opencode/skills/test-workflow/SKILL.md:28-32` lists per-flag targets of "≥85% line" for Unit, Guard, Contract and Integration, and "≥80% branch" for Property.
- `.opencode/agents/encina-test.md:19-23` gives the same table plus "≥90% method" for Unit.
- `.opencode/skills/release-checklist/SKILL.md:32-34` lists "Line ≥85%, Branch ≥80%, Method ≥90%" with "Overall codebase" as the scope, as release gates.

An agent following the skill chases 85% per flag against manifests whose targets are set per package and flag (for example `.github/coverage-manifest/Encina.json` has unit 70, guard 20 and contract 15).

## Location

- **File(s)**: `.opencode/skills/test-workflow/SKILL.md:28-32`, `.opencode/agents/encina-test.md:19-23`, `.opencode/skills/release-checklist/SKILL.md:32-34`
- **Package(s)**: none (agent and skill instructions, not an Encina package)

## Current Behavior

The three files give agents fixed per-flag and overall coverage targets. The release checklist treats an overall-codebase percentage as a release gate, although no such percentage exists as a target.

## Expected Behavior

Each table is replaced by an instruction to read the flag targets from `.github/coverage-manifest/{Package}.json`. The overall-codebase release gate is deleted, and the release checklist instead points at the per-package dashboard state (every applicable flag at its own target). The mutation score row, which is already per file, stays.

## Root Cause

The category-based coverage model was replaced by the per-flag manifest model, but these tool-specific instruction files were not updated with the rest of the documentation.

## Proposed Fix

Edit the three files as described in Expected Behavior. The repository-wide text check proposed for the issue templates (no hand-typed 85% coverage target under `.opencode`) can cover these files too.

## Priority

- [ ] **High** - Blocks functionality or causes failures in production
- [x] **Medium** - Should be fixed before 1.0 release
- [ ] **Low** - Nice to have, can be deferred

## Effort Estimate

- [x] Small (< 1 hour)
- [ ] Medium (1-4 hours)
- [ ] Large (> 4 hours)

## Related Issues

- #19 (This issue)
