# LoadTests - Security PII Masking

## Status: Not Implemented

## Justification

### 1. Stateless Algorithmic Transformation
PII masking strategies (`IMaskingStrategy`) are pure functions: input string → masked string. No shared state, no I/O, no resource contention. Each strategy is a singleton with zero mutable state.

### 2. No Concurrency Concerns
All 9 masking strategies (Email, Phone, CreditCard, SSN, Name, Address, DateOfBirth, IPAddress, FullMask) operate independently with no shared resources. The log path (`IPIIMasker.MaskForLog`, used by `LogMasked`) adds no shared mutable state: it reuses the per-type property scanner cache, which is a concurrency-safe collection, and works on a copy of each logged object.

### 3. Adequate Coverage from Other Test Types
- **Unit Tests**: Cover all masking patterns, edge cases, null handling and the log path
- **Guard Tests**: Verify parameter validation for all strategies
- **Property Tests**: Verify "masked output never contains original PII" invariant
- **Benchmark Tests**: `PIIMaskerBenchmarks` measures masking throughput, including the log path (`MaskForLog_SingleField`, `MaskForLog_MultiField`), which runs once per masked log line

### 4. Recommended Alternative
If pipeline behavior performance under load becomes a concern, benchmark the `PIIMaskingPipelineBehavior` end-to-end with BenchmarkDotNet rather than load testing individual strategies.

## Related Files
- `src/Encina.Security.PII/` — Source
- `tests/Encina.UnitTests/Security/PII/` — Unit tests
- `tests/Encina.BenchmarkTests/Encina.Security.PII.Benchmarks/PIIMaskerBenchmarks.cs` — Benchmarks

## Date: 2026-11-04

## Issue: #1839 (#3030)
