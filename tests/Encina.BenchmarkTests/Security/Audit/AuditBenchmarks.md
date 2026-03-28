# BenchmarkTests - Security Audit

## Status: Not Implemented

## Justification

### 1. Fire-and-Forget Pattern
The `AuditPipelineBehavior` records audit entries asynchronously (fire-and-forget). The performance bottleneck is the underlying store (database I/O), not the audit logic itself. Micro-benchmarking the in-memory orchestration adds negligible value.

### 2. Store Performance Is Provider-Dependent
Audit store performance is dominated by database write latency, which varies by provider (SQL Server ~5ms, PostgreSQL ~3ms, MySQL ~4ms). BenchmarkDotNet cannot meaningfully benchmark I/O-bound operations — use Load Tests instead.

### 3. Existing Benchmark Coverage
The `Encina.Audit.Marten.Benchmarks` project already benchmarks the most computationally expensive audit path: temporal crypto-shredding encryption, which adds ~900ns per field.

### 4. Adequate Coverage from Other Test Types
- **Unit Tests**: Cover AuditPipelineBehavior, InMemoryAuditStore, AuditEntryFactory
- **Load Tests**: Validate concurrent write throughput (the actual performance concern)
- **Integration Tests**: Cover all 10 database providers with real I/O

### 5. Recommended Alternative
If audit entry creation overhead becomes a concern, profile the `DefaultAuditEntryFactory` and `RequestMetadataExtractor` using `dotnet-trace` rather than BenchmarkDotNet.

## Related Files
- `src/Encina.Security.Audit/` — Source
- `tests/Encina.UnitTests/Security/Audit/` — Unit tests
- `tests/Encina.LoadTests/Security/Audit/` — Load tests

## Date: 2026-03-19
## Issue: #797
