# IntegrationTests - Security PII Masking

## Status: Not Implemented

## Justification

### 1. Stateless Algorithmic Module
PII masking is a pure algorithmic transformation with no database, no external services, and no I/O. There is nothing to "integrate" with: the module transforms strings using regex and string manipulation.

### 2. No Provider Implementations
Unlike database-backed modules (Audit, ABAC), PII masking has no provider-specific implementations. All 9 masking strategies are self-contained singletons.

### 3. The Log Path Needs No Real Logging Provider
`IPIIMasker.MaskForLog` and the `LogMasked` helpers (`LogInformationMasked`, `LogWarningMasked`, `LogErrorMasked`) mask the object before it reaches `ILogger`. A real `ILoggerFactory` or logging provider only receives the already masked copy (or the `[MASKING FAILED]` placeholder), so a test with a real provider would exercise the provider, not Encina code.

### 4. Adequate Coverage from Other Test Types
- **Unit Tests**: Cover all 9 strategies with comprehensive edge cases, and the log path (`[MaskInLogs]` on properties and classes, precedence, non-string fail-closed, `MaskInLogs = false` opt-out) against the real `PIIMasker` with a `FakeLogger`
- **Property Tests**: Verify masking invariants (output never contains original PII)
- **Guard Tests**: Verify null/empty input handling
- **Contract Tests**: Verify IMaskingStrategy contract consistency

### 5. Recommended Alternative
If integration with `PIIMaskingPipelineBehavior` in a real ASP.NET pipeline needs testing, create an end-to-end test in the application layer, not in the PII module tests.

## Related Files
- `src/Encina.Security.PII/` — Source
- `tests/Encina.UnitTests/Security/PII/` — Unit tests
- `tests/Encina.BenchmarkTests/Encina.Security.PII.Benchmarks/PIIMaskerBenchmarks.cs` — Log path cost

## Date: 2026-11-04

## Issue: #1839 (#3030)
