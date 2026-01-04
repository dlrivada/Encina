# Testing Dogfooding Communication Plan

This document outlines communication practices for the testing dogfooding initiative.

---

## Overview

**Epic**: [Issue #498](https://github.com/dlrivada/Encina/issues/498)

**Goal**: Ensure all contributors understand the migration process and can provide/receive feedback effectively.

---

## Communication Channels

### Primary: GitHub Issues

**Use for**:
- Bug reports
- Feature requests
- Migration blockers
- Technical questions

**Template**: Use `.github/ISSUE_TEMPLATE/testing-dogfooding-feedback.md`

**Label**: `testing-dogfooding`

### Secondary: Pull Request Comments

**Use for**:
- Code review feedback
- Pattern discussions
- Implementation alternatives

**Best Practices**:
- Reference the migration phase
- Link to relevant documentation
- Include before/after code snippets

### Progress Updates: Progress Tracking Doc

**Location**: `docs/plans/checklists/progress-tracking.md`

**Update frequency**: After each project phase completion

---

## Milestone Communication

### Phase Completion Announcements

When completing a migration phase for a project:

1. Update progress tracking document
2. Create a brief summary comment on Issue #498
3. Format:

```markdown
## Phase [X] Complete: [Project Name]

**Completed**: [Date]
**Files migrated**: [Count]
**Patterns used**: [List key patterns]
**Issues encountered**: [None / Link to issues]
**PR**: #[number]
```

### Blocker Notifications

When encountering a blocker:

1. Create issue with `testing-dogfooding` and `blocked` labels
2. Add to "Blockers & Issues" in progress tracking
3. Comment on Issue #498 with brief description

---

## Decision Documentation

### When to Document Decisions

- API usage differs from documentation
- New pattern discovered
- Trade-off made between approaches

### Where to Document

1. Update `docs/plans/checklists/common-patterns-and-edge-cases.md`
2. Add to Section 13 of `docs/plans/testing-dogfooding-plan.md`
3. Update reference examples if applicable

### Decision Record Format

```markdown
### [Decision Title]

**Date**: YYYY-MM-DD
**Context**: [Why this decision was needed]
**Decision**: [What was decided]
**Consequences**: [Impact on migration]
**Example**:
```csharp
// Code example
```
```

---

## Escalation Path

### Level 1: Self-Service

1. Check reference examples: `tests/Encina.Testing.Examples/`
2. Check common patterns: `docs/plans/checklists/common-patterns-and-edge-cases.md`
3. Check migration guide: `docs/plans/migration-priority-guide.md`

### Level 2: GitHub Issue

1. Search existing issues for similar problems
2. Create new issue using feedback template
3. Wait 24-48 hours for response

### Level 3: Epic Discussion

1. Comment on Issue #498 with summary
2. Tag relevant contributors
3. Request synchronous discussion if needed

---

## Weekly Status Template

For complex migrations, use this weekly update format:

```markdown
## Testing Dogfooding Weekly Update - [Date]

### Completed This Week
- [x] [Task 1]
- [x] [Task 2]

### In Progress
- [ ] [Task 3] - [% complete]

### Blockers
- [Issue #X]: [Brief description]

### Metrics
- Files migrated: X
- Tests passing: X/Y
- Estimated remaining: X hours

### Next Week Focus
- [Priority 1]
- [Priority 2]
```

---

## PR Guidelines

### Title Format

```
refactor(tests): migrate [Project] to [Package] - Phase [X]
```

### Description Template

```markdown
## Summary

Migrates [Project] to use [Encina.Testing.* packages].

## Changes

- Replaced [X] with [Y]
- Added [package] reference
- Updated [N] test files

## Patterns Used

- `ShouldBeSuccess()` for Either assertions
- `EncinaFaker<T>` for test data
- `FakeOutboxStore` for messaging

## Testing

- [ ] All tests pass
- [ ] No behavior changes
- [ ] Reference examples still build

## Related

- Epic: #498
- Phase: [X]
```

---

## Success Criteria Communication

### Phase Complete When

- [ ] All tests pass
- [ ] No legacy patterns remain in scope
- [ ] Progress tracking updated
- [ ] Any new patterns documented

### Migration Complete When

- [ ] All test projects migrated
- [ ] CI validation passing
- [ ] Documentation finalized
- [ ] Issue #498 closed
