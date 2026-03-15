# IntegrationTests - Security Sanitization

## Status: Not Implemented

## Justification

### 1. Stateless Module
Sanitization (`ISanitizer`, `IOutputEncoder`) is a stateless transformation with no database, no external services, and no I/O. HTML parsing and encoding are CPU-only operations.

### 2. No Provider Implementations
Unlike database-backed modules, sanitization has no provider-specific implementations. All sanitization logic is self-contained.

### 3. Adequate Coverage from Other Test Types
- **Unit Tests**: Cover all sanitization profiles, XSS attack vectors, encoding edge cases
- **Property Tests**: Verify "sanitized output is XSS-safe" invariant
- **Guard Tests**: Verify parameter validation

### 4. Recommended Alternative
If integration with ASP.NET middleware pipeline needs testing, create an end-to-end test in the application layer.

## Related Files
- `src/Encina.Security.Sanitization/` — Source
- `tests/Encina.UnitTests/Security/Sanitization/` — Unit tests

## Date: 2026-03-15
