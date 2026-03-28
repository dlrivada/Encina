# BenchmarkTests - Security Audit

## Status: Not Implemented

## Justification

### 1. Fire-and-Forget Pattern
`AuditPipelineBehavior` records audit entries asynchronously without blocking the request pipeline. The audit write is intentionally non-blocking — benchmarking it measures async scheduling overhead, not meaningful application performance.

### 2. Store Performance is Provider-Dependent
Audit write performance depends entirely on the backing store (SQL Server, PostgreSQL, etc.), not on the Encina audit layer. Benchmarking the InMemory store is meaningless; benchmarking real DB stores belongs in integration tests.

### 3. Adequate Coverage from Other Test Types
- **Unit Tests**: Cover audit entry creation, outcome mapping, filtering logic
- **Load Tests**: Cover concurrent audit writes under high request volume (to be created)
- **Integration Tests**: Cover real DB performance across 10 providers (to be created)

### 4. Recommended Alternative
If audit throughput becomes a concern, benchmark the specific `IAuditStore` implementation (e.g., SQL Server batch insert) using BenchmarkDotNet with a real database connection.

## Related Files
- `src/Encina.Security.Audit/` — Source
- `tests/Encina.UnitTests/Security/Audit/` — Unit tests

## Date: 2026-03-15
