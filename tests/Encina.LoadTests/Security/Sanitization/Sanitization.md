# LoadTests - Security Sanitization

## Status: Not Implemented

## Justification

### 1. Stateless Transformation
Input sanitization (`ISanitizer`) and output encoding (`IOutputEncoder`) are stateless transformations. No shared state, no I/O, no resource contention.

### 2. No Concurrency Concerns
All sanitization operations are independent. HTML parsing and encoding are CPU-bound with no shared resources.

### 3. Adequate Coverage from Other Test Types
- **Unit Tests**: Cover all sanitization profiles, XSS vectors, encoding edge cases
- **Guard Tests**: Verify parameter validation
- **Property Tests**: Verify "sanitized output never contains script tags" invariant
- **Benchmark Tests**: Measure HTML sanitization throughput (to be created)

### 4. Recommended Alternative
If sanitization becomes a bottleneck (e.g., large HTML documents), benchmark `ISanitizer.SanitizeAsync` with BenchmarkDotNet using varying input sizes.

## Related Files
- `src/Encina.Security.Sanitization/` — Source
- `tests/Encina.UnitTests/Security/Sanitization/` — Unit tests

## Date: 2026-03-15
