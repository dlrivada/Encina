# Time Estimation Matrix

This matrix provides effort estimates for migrating test projects to Encina.Testing.* packages.

---

## Estimation Factors

### Complexity Levels

| Level | Description | Multiplier |
|-------|-------------|------------|
| Low | Simple assertions, few dependencies | 1x |
| Medium | Multiple patterns, some refactoring | 1.5x |
| High | Complex mocks, extensive refactoring | 2x |

### Project Size Categories

| Size | Test Count | Base Time |
|------|------------|-----------|
| Small | 1-20 tests | 1 hour |
| Medium | 21-50 tests | 2 hours |
| Large | 51-100 tests | 4 hours |
| Extra Large | 100+ tests | 8 hours |

---

## Phase Time Estimates

### Phase 1: Foundation (Add Package References)

| Task | Time | Notes |
|------|------|-------|
| Add project references | 5 min/project | Mechanical change |
| Add global usings | 5 min/project | Optional |
| Verify build | 5 min/project | Quick validation |
| **Total per project** | **15 min** | |

### Phase 2: Either Assertions

| Project Size | Low Complexity | Medium | High |
|--------------|----------------|--------|------|
| Small | 30 min | 45 min | 1 hour |
| Medium | 1 hour | 1.5 hours | 2 hours |
| Large | 2 hours | 3 hours | 4 hours |
| Extra Large | 4 hours | 6 hours | 8 hours |

### Phase 3: Test Data Generation

| Project Size | Low Complexity | Medium | High |
|--------------|----------------|--------|------|
| Small | 20 min | 30 min | 45 min |
| Medium | 45 min | 1 hour | 1.5 hours |
| Large | 1.5 hours | 2 hours | 3 hours |
| Extra Large | 3 hours | 4 hours | 6 hours |

### Phase 4: Messaging Fakes

| Project Size | Low Complexity | Medium | High |
|--------------|----------------|--------|------|
| Small | 30 min | 45 min | 1 hour |
| Medium | 1 hour | 1.5 hours | 2 hours |
| Large | 2 hours | 3 hours | 4 hours |
| Extra Large | 4 hours | 6 hours | 8 hours |

---

## Project-Specific Estimates

### Tier 1: Core Tests

| Project | Size | Complexity | Phase 1 | Phase 2 | Phase 3 | Phase 4 | Total |
|---------|------|------------|---------|---------|---------|---------|-------|
| Encina.Tests | Large | Medium | 15 min | 3 hours | 2 hours | 3 hours | 8.25 hours |
| Encina.Messaging.Tests | Medium | High | 15 min | 2 hours | 1.5 hours | 2 hours | 5.75 hours |

### Tier 2: Provider Tests

| Project | Size | Complexity | Phase 1 | Phase 2 | Phase 3 | Phase 4 | Total |
|---------|------|------------|---------|---------|---------|---------|-------|
| Encina.EntityFrameworkCore.Tests | Large | Medium | 15 min | 3 hours | 2 hours | 3 hours | 8.25 hours |
| Encina.Dapper.Tests | Medium | Medium | 15 min | 1.5 hours | 1 hour | 1.5 hours | 4.25 hours |
| Encina.Data.Tests | Medium | Medium | 15 min | 1.5 hours | 1 hour | 1.5 hours | 4.25 hours |

### Tier 3: Feature Tests

| Project | Size | Complexity | Phase 1 | Phase 2 | Phase 3 | Phase 4 | Total |
|---------|------|------------|---------|---------|---------|---------|-------|
| Encina.Validation.*.Tests | Small | Low | 15 min | 30 min | 20 min | 30 min | 1.6 hours |
| Encina.Caching.*.Tests | Small | Low | 15 min | 30 min | 20 min | 30 min | 1.6 hours |
| Encina.Resilience.Tests | Small | Medium | 15 min | 45 min | 30 min | 45 min | 2.25 hours |

### Tier 4: Integration/Property Tests

| Project | Size | Complexity | Phase 1 | Phase 2 | Phase 3 | Phase 4 | Total |
|---------|------|------------|---------|---------|---------|---------|-------|
| *.IntegrationTests | Medium | High | 15 min | 2 hours | 1.5 hours | 2 hours | 5.75 hours |
| *.PropertyTests | Small | Medium | 15 min | 45 min | 30 min | N/A | 1.5 hours |

---

## Total Migration Estimate

| Tier | Projects | Estimated Time |
|------|----------|----------------|
| Tier 1 (Core) | 2 | 14 hours |
| Tier 2 (Providers) | 3 | 17 hours |
| Tier 3 (Features) | ~8 | 14 hours |
| Tier 4 (Integration) | ~5 | 15 hours |
| **Total** | **~18** | **60 hours** |

**Note**: Estimates assume familiarity with patterns after first project. Actual times may vary.

---

## Risk Factors

| Factor | Impact | Mitigation |
|--------|--------|------------|
| Unfamiliar APIs | +25% time | Review reference examples first |
| Complex mocking | +50% time | Consult edge cases doc |
| Legacy test patterns | +30% time | Refactor incrementally |
| CI failures | +20% time | Run tests frequently |

---

## Tracking Template

```markdown
## Project: [Name]
- [ ] Phase 1: ___ min (estimated: 15 min)
- [ ] Phase 2: ___ hours (estimated: X hours)
- [ ] Phase 3: ___ hours (estimated: X hours)
- [ ] Phase 4: ___ hours (estimated: X hours)
- Total actual: ___ hours
- Notes:
```
