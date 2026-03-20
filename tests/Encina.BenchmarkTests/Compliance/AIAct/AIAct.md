# BenchmarkTests - Compliance AIAct

## Status: Not Implemented

## Justification

The AI Act compliance engine is a stateless pipeline behavior that runs attribute reflection (cached), options checks, and validator orchestration. None of these operations are hot-path or micro-optimization candidates.

### 1. No Hot-Path Operations

- Attribute reflection is cached in a static `ConcurrentDictionary` — after first access, lookup is O(1) dictionary hit
- Compliance validation delegates to injectable services (`IAIActClassifier`, `IHumanOversightEnforcer`) whose real-world implementations will be I/O-bound (database, external API)
- The pipeline behavior adds negligible overhead compared to the actual request handler
- Enforcement mode check is a single enum comparison

### 2. Stateless In-Memory Engine

- `DefaultAIActClassifier` reads from `InMemoryAISystemRegistry` (ConcurrentDictionary lookup)
- `DefaultHumanOversightEnforcer` checks a static attribute cache + ConcurrentDictionary
- `DefaultAIActComplianceValidator` orchestrates these lookups with no computation-heavy logic
- Production deployments will use database-backed registries (child issue #839) where benchmarks should target the persistence layer, not the orchestration

### 3. Adequate Coverage from Other Test Types

- **Unit Tests**: Cover all pipeline paths (disabled, skip, prohibited, block, warn, pass), validator orchestration, classifier logic
- **Guard Tests**: Verify all public methods validate parameters correctly
- **Property Tests**: Verify registry invariants, classifier invariants, record equality
- **Contract Tests**: Verify interface contracts for all implementations

### 4. Recommended Alternative

If performance profiling becomes necessary in the future:

1. Benchmark the `ConcurrentDictionary` attribute cache under high concurrency
2. Measure `EvaluateComplianceAsync` latency with varying registry sizes
3. Compare `InMemoryAISystemRegistry` vs database-backed registries

```csharp
[MemoryDiagnoser]
public class AIActComplianceBenchmarks
{
    [Params(10, 100, 1000)]
    public int RegisteredSystems { get; set; }

    [Benchmark(Baseline = true)]
    public AIActComplianceResult EvaluateCompliance()
    {
        return _classifier.EvaluateComplianceAsync("sys-1").AsTask().Result
            .Match(Right: r => r, Left: _ => throw new InvalidOperationException());
    }
}
```

## Related Files

- `src/Encina.Compliance.AIAct/` - Core AI Act compliance module source
- `tests/Encina.UnitTests/Compliance/AIAct/` - Unit tests
- `tests/Encina.GuardTests/Compliance/AIAct/` - Guard tests
- `tests/Encina.PropertyTests/Compliance/AIAct/` - Property tests

## Date: 2026-03-20
## Issue: #415
