# BenchmarkTests - Data Residency

## Status: Not Implemented

## Justification

The Data Residency module (Encina.Compliance.DataResidency) does not require dedicated BenchmarkDotNet benchmarks for the following reasons:

### 1. Not a Hot Path

Data residency checks execute once per request via the pipeline behavior, not in tight inner loops. The per-request overhead (policy lookup + region comparison) is negligible compared to actual I/O operations (database queries, HTTP calls) that dominate request latency.

### 2. Static Attribute Caching Eliminates Reflection Cost

The `DataResidencyPipelineBehavior<TRequest, TResponse>` uses `static readonly` fields for attribute resolution. Each closed generic type resolves its attribute info exactly once via the CLR's static field guarantee. There is no per-request reflection overhead to benchmark.

### 3. Simple Comparison Operations

Core operations are string/enum comparisons:
- `Region.Equals()` — case-insensitive string comparison (O(n) on code length)
- `RegionRegistry.GetByCode()` — dictionary lookup (O(1))
- `IsAllowedAsync()` — policy lookup + region contains check (O(m) where m = allowed regions)
- `HasAdequacy()` — HashSet contains check (O(1))

These are already well-optimized by the runtime and do not benefit from micro-benchmarking.

### 4. Mapper Operations Are Trivial

The static mappers (`DataLocationMapper`, `ResidencyPolicyMapper`, `ResidencyAuditEntryMapper`) perform simple property copying with minimal transformations (int casts, string splits, JSON serialization for metadata). These are not performance-critical paths.

### 5. Adequate Coverage from Other Test Types

- **Unit Tests**: 23 files covering all components
- **Property Tests**: 3 files with FsCheck verifying round-trip correctness
- **Contract Tests**: 6 files verifying store behavioral contracts
- **Integration Tests**: Full lifecycle with DI and concurrent access verification

### 6. Recommended Alternative

If benchmarks become necessary (e.g., after observing performance regression), consider:
- Benchmark `RegionRegistry.GetByCode()` lookup with varying registry sizes
- Benchmark `DataLocationMapper.ToDomain()` with large metadata dictionaries
- Benchmark `DefaultCrossBorderTransferValidator.ValidateTransferAsync()` decision tree paths
- Use `[MemoryDiagnoser]` to verify zero-allocation claims of `[LoggerMessage]` source generator

## Related Files

- `src/Encina.Compliance.DataResidency/` — Source implementation
- `tests/Encina.UnitTests/Compliance/DataResidency/` — Unit tests
- `tests/Encina.PropertyTests/Compliance/DataResidency/` — Property tests

## Date: 2026-03-02
## Issue: #405
