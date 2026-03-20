# LoadTests - Compliance AIAct

## Status: Not Implemented

## Justification

The AI Act compliance engine is a stateless, in-memory pipeline behavior. Load testing in-memory ConcurrentDictionary operations provides no actionable insights — the bottleneck in production will be the database-backed registry and external classifier implementations.

### 1. In-Memory Implementation Is Not Representative of Production Load

- `InMemoryAISystemRegistry` uses `ConcurrentDictionary` — already designed for high concurrency
- `DefaultHumanOversightEnforcer` uses `ConcurrentDictionary` for decisions and a static cache for attributes
- Production deployments will replace these with database-backed stores (child issue #839)
- Load testing the in-memory implementations would benchmark `ConcurrentDictionary` performance, not AI Act compliance logic

### 2. Pipeline Behavior Is Stateless

- `AIActCompliancePipelineBehavior` reads options, checks attributes (cached), and delegates to validators
- No shared mutable state beyond the attribute cache (populated once, read-only afterward)
- The enforcement mode check is a single enum comparison with no contention
- No file I/O, network calls, or database access in the core pipeline

### 3. Adequate Coverage from Other Test Types

- **Unit Tests**: Cover all enforcement modes, prohibited-always-blocked invariant, validator error handling
- **Guard Tests**: Verify parameter validation on all public methods
- **Property Tests**: Verify registry round-trip invariants under random inputs, classifier prohibited-practice invariant
- **Contract Tests**: Verify interface implementations behave consistently

### 4. Recommended Alternative

When database-backed implementations are available (child issue #839):

1. Load test `IAISystemRegistry` with concurrent registrations and lookups against PostgreSQL/SQL Server
2. Load test `IHumanOversightEnforcer.RecordHumanDecisionAsync` under concurrent write pressure
3. Use `[Collection("Provider-Database")]` fixtures with Docker containers

```csharp
[Trait("Category", "Load")]
public class AIActRegistryLoadTests
{
    [Fact]
    public async Task ConcurrentRegistrations_1000Systems_AllSucceed()
    {
        // Requires database-backed IAISystemRegistry (child issue #839)
        var tasks = Enumerable.Range(0, 1000)
            .Select(i => _registry.RegisterSystemAsync(CreateRegistration($"sys-{i}")));
        var results = await Task.WhenAll(tasks.Select(t => t.AsTask()));
        results.All(r => r.IsRight).ShouldBeTrue();
    }
}
```

## Related Files

- `src/Encina.Compliance.AIAct/` - Core AI Act compliance module source
- `tests/Encina.UnitTests/Compliance/AIAct/` - Unit tests
- `tests/Encina.PropertyTests/Compliance/AIAct/` - Property tests

## Date: 2026-03-20
## Issue: #415
