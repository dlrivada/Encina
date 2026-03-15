# ContractTests - Security Sanitization

## Status: Not Implemented

## Justification

### 1. Single Implementation
`ISanitizer` and `IOutputEncoder` each have a single implementation. Contract tests verify behavioral consistency across multiple implementations — with only one implementation, unit tests are sufficient.

### 2. Simple Interface
`ISanitizer.SanitizeAsync(input, profile)` and `IOutputEncoder.Encode(input, context)` are straightforward transformation methods. Their contracts are fully verified by unit tests.

### 3. Adequate Coverage from Other Test Types
- **Unit Tests**: Comprehensive coverage of all sanitization profiles and encoding contexts
- **Property Tests**: Verify XSS-safety invariants
- **Guard Tests**: Verify parameter validation

### 4. Recommended Alternative
If multiple sanitization backends are added (e.g., OWASP AntiSamy, DOMPurify via JS interop), create a shared contract test base class.

## Related Files
- `src/Encina.Security.Sanitization/` — Source
- `tests/Encina.UnitTests/Security/Sanitization/` — Unit tests

## Date: 2026-03-15
