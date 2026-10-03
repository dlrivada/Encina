<!--
title: [DEBT] docs/messaging/transports.md mixes all four Diataxis quadrants in one page
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

`docs/messaging/transports.md` mixes all four Diataxis quadrants under one title, with no split into separate pages. The docs house style is one quadrant per page (`.claude/skills/encina-docs/SKILL.md`), and this page is not one of the `docs/features/*.md` pages that the skill exempts under its "Mixed today" row.

## Location

- **File(s)**: `docs/messaging/transports.md:35` (Design Philosophy callout), `:39-96` (Decision Flowchart), `:211-479` (Detailed Transport Guides), `:518-565` (FAQ)
- **Package(s)**: none (documentation only)

## Current Behavior

The page combines:

- Explanation ("why", discussion, trade-offs): the "Design Philosophy" callout (`:35`) and the whole FAQ section (`:518-565`, with headings such as "Should I use a unified messaging interface?" and "When should I use gRPC vs a message broker?").
- How-to ("how do I choose a transport"): the "Decision Flowchart" section (`:39-96`).
- Reference mixed with how-to: the "Detailed Transport Guides" section (`:211-479`) combines reference material (option names, the comparison table) with task-oriented code samples.

## Expected Behavior

Each page covers exactly one quadrant: the explanation, the how-to for choosing a transport, the per-transport reference and the per-transport how-to guides live on separate pages that link to their neighbours.

## Root Cause

The page was written as a single document instead of being split by quadrant from the start.

## Proposed Fix

Split `docs/messaging/transports.md` by quadrant: move the design philosophy and the FAQ to an explanation page, keep the decision flowchart and quick selection guide as a how-to, move option tables and the comparison table to a reference page, and keep task-oriented samples as how-to pages per transport, each linking to an adjacent quadrant. Update `docs/messaging/index.md` and any other inbound links to the new pages.

## Priority

- [ ] **High** - Blocks functionality or causes failures in production
- [ ] **Medium** - Should be fixed before 1.0 release
- [x] **Low** - Nice to have, can be deferred

## Effort Estimate

- [ ] Small (< 1 hour)
- [ ] Medium (1-4 hours)
- [x] Large (> 4 hours)

## Related Issues

- #18 (This issue)
