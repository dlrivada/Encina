<!--
title: [DEBT] Split docs/messaging/sagas.md into Diátaxis quadrants
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

The file `docs/messaging/sagas.md` violates the Diátaxis documentation structure by mixing Explanation and How-to quadrants under a single title. Specifically, sections "Decision Flowchart", "Comparison Table", and "FAQ" contain Explanation content (why to choose one pattern over another, trade-offs), while "Configuration" and "Usage" sections under both "Orchestration Sagas" and "Choreography Sagas" contain How-to material (step-by-step code to wire up a feature). According to `.claude/skills/encina-docs/SKILL.md` §1, Explanation belongs under `docs/architecture/` or `docs/specifications/`, and How-to belongs under `docs/guides/` with a title starting "How to". The file fails the "One quadrant" checklist requirement and is not on the "Mixed today" exception list (only `docs/features/*.md` is grandfathered).

## Location

- **File(s)**: `docs/messaging/sagas.md`
- **Package(s)**: Encina (Documentation)

## Current Behavior

The file `docs/messaging/sagas.md` contains a mix of Diátaxis quadrants under a single title:
- **Explanation content**: "Decision Flowchart" (`:38-73`), "Comparison Table" (`:419-433`), and "FAQ" (`:505-550`) discuss why to choose one pattern over another and trade-offs.
- **How-to content**: "Configuration" and "Usage" sections under both "Orchestration Sagas" and "Choreography Sagas" (`:118-237`, `:289-415`) provide step-by-step code to wire up a feature.

This structure fails the "One quadrant" row of the documentation checklist, as `docs/messaging/*.md` is not on the "Mixed today" exception list (only `docs/features/*.md` is grandfathered).

## Expected Behavior

The content in `docs/messaging/sagas.md` should be split according to the Diátaxis quadrants defined in `.claude/skills/encina-docs/SKILL.md` §1:
- **Explanation content** (Decision Flowchart, Comparison Table, FAQ) should be moved to `docs/architecture/` or `docs/specifications/`.
- **How-to content** (Configuration, Usage) should be moved to `docs/guides/` with a title starting "How to".
- The original `docs/messaging/sagas.md` file should contain only a single quadrant or be removed if its content is fully distributed, adhering to the "One quadrant" checklist rule.

## Root Cause

The documentation was created without strictly adhering to the Diátaxis framework mapping defined in `.claude/skills/encina-docs/SKILL.md` §1. The file `docs/messaging/sagas.md` was not reviewed against the "One quadrant" checklist requirement, and since it is not part of the `docs/features/*.md` grandfathered exception list, it is subject to the strict separation of quadrants.

## Proposed Fix

1. Identify and extract all Explanation content (Decision Flowchart, Comparison Table, FAQ) from `docs/messaging/sagas.md`.
2. Move the extracted Explanation content to an appropriate file under `docs/architecture/` or `docs/specifications/`.
3. Identify and extract all How-to content (Configuration, Usage sections) from `docs/messaging/sagas.md`.
4. Move the extracted How-to content to an appropriate file under `docs/guides/` with a title starting "How to".
5. Update or remove `docs/messaging/sagas.md` to ensure it contains only a single Diátaxis quadrant or is eliminated if fully decomposed.
6. Update any internal links pointing to the original sections to point to the new locations.

## Priority

- [ ] **High** - Blocks functionality or causes failures in production
- [x] **Medium** - Should be fixed before 1.0 release
- [ ] **Low** - Nice to have, can be deferred

## Effort Estimate

- [ ] Small (< 1 hour)
- [x] **Medium** (1-4 hours)
- [ ] Large (> 4 hours)

## Related Issues

#16