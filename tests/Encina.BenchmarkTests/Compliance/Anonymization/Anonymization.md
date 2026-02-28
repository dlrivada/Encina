# BenchmarkTests - Compliance Anonymization (Core Module)

## Status: Not Implemented

## Justification

The core Anonymization module delegates all performance-critical work to .NET BCL cryptographic primitives and uses well-known caching patterns for reflection metadata. Micro-benchmarking this module would measure .NET BCL performance rather than Encina code, providing no actionable optimization insights.

### 1. Cryptographic Operations Use .NET BCL Implementations

The anonymization strategies rely entirely on .NET BCL cryptographic classes:

- **AES-256-GCM** (`System.Security.Cryptography.AesGcm`) for reversible encryption
- **HMAC-SHA256** (`System.Security.Cryptography.HMACSHA256`) for consistent tokenization
- **SHA-256** (`System.Security.Cryptography.SHA256`) for irreversible hashing

These implementations are already extensively benchmarked by Microsoft as part of the .NET runtime performance suite. Benchmarking them through Encina's thin wrapper layer would only measure the BCL, not Encina code.

### 2. Reflection Metadata Is Cached

The `[Anonymize]` attribute discovery uses reflection, but results are cached in a static `ConcurrentDictionary`. This means:

- **First access**: Reflection cost (one-time, amortized across application lifetime)
- **Subsequent accesses**: Dictionary lookup, O(1) amortized

Benchmarking would show the cached path is fast (dictionary lookup) and the uncached path is slow (reflection). Both are well-understood performance characteristics that do not benefit from micro-benchmarking.

### 3. No Hot-Path Operations

Anonymization is not a hot-path operation in typical applications:

- It runs once per data access or API response, not in tight loops
- Typical call frequency: tens to hundreds per second, not millions
- The operation is inherently I/O-adjacent (applied before/after database reads)
- At these frequencies, the overhead of the C# wrapper is immeasurable compared to the cryptographic operation itself

### 4. Adequate Coverage from Other Test Types

- **Unit Tests**: Verify correctness of all anonymization strategies, attribute discovery, and service orchestration. Include tests for edge cases (empty strings, large payloads, special characters)
- **Guard Tests**: Verify all public methods validate parameters correctly, ensuring no unnecessary exceptions in the normal path
- **Property Tests**: Verify anonymization invariants across randomized inputs (tokenization reversibility, hash consistency, encryption round-trip fidelity)
- **Contract Tests**: Verify all `IAnonymizationStrategy` implementations satisfy the interface contract uniformly

### 5. Recommended Alternative

If performance profiling becomes necessary in the future:

1. Use `BenchmarkSwitcher` with `[MemoryDiagnoser]` to measure allocations per anonymization operation
2. Focus benchmarks on the `AnonymizationService.AnonymizeAsync<T>()` method with varying entity sizes (1 field, 10 fields, 50 fields)
3. Compare strategies: measure relative cost of encryption vs hashing vs tokenization
4. Use `--job short` for quick validation: `dotnet run -c Release -- --filter "*Anonymization*" --job short`

```csharp
// Example future benchmark structure
[MemoryDiagnoser]
public class AnonymizationBenchmarks
{
    [Params(1, 10, 50)]
    public int FieldCount { get; set; }

    [Benchmark(Baseline = true)]
    public object Encryption() => _service.AnonymizeAsync(_encryptionEntity);

    [Benchmark]
    public object Hashing() => _service.AnonymizeAsync(_hashingEntity);

    [Benchmark]
    public object Tokenization() => _service.AnonymizeAsync(_tokenizationEntity);
}
```

## Related Files

- `src/Encina.Compliance.Anonymization/` - Core anonymization module source
- `tests/Encina.UnitTests/Compliance/Anonymization/` - Unit tests

## Date: 2026-02-28
## Issue: #407
