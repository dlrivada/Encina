# ADR-001: Railway Oriented Programming for Error Handling

**Status:** Accepted
**Date:** 2025-12-12
**Deciders:** Architecture Team
**Technical Story:** Establish consistent error handling strategy for Encina pipeline

## Context

The Encina needs a robust, type-safe error handling strategy that:

- Avoids throwing exceptions for expected failures (validation errors, business rule violations, not found, etc.)
- Maintains type safety throughout the pipeline
- Enables composable error handling in behaviors and processors
- Provides clear distinction between technical errors (bugs) and functional failures (expected scenarios)
- Supports functional programming patterns

Traditional exception-based error handling has several drawbacks:

- Exceptions are invisible in method signatures (no compile-time verification)
- Performance overhead for control flow
- Makes composition and chaining difficult
- Unclear whether a failure is expected or unexpected
- Difficult to unit test error paths

## Decision

We adopt **Railway Oriented Programming (ROP)** using `Either<EncinaError, TResponse>` from LanguageExt as the return type for all Encina operations.

### Key Principles

1. **Two Tracks:** Success (Right) and Failure (Left)
2. **Explicit Errors:** All potential failures are visible in the type signature
3. **Short-Circuiting:** First Left value stops pipeline execution
4. **Exception Safety Net:** Unexpected exceptions are caught at dispatcher level and converted to Left

### Implementation (Pure ROP - Updated 2025-12-12)

**Status:** Fully migrated to Pure Railway Oriented Programming

```csharp
// Public API
ValueTask<Either<EncinaError, TResponse>> Send<TResponse>(IRequest<TResponse> request, ...);
ValueTask<Either<EncinaError, Unit>> Publish<TNotification>(TNotification notification, ...);

// Pipeline behaviors
ValueTask<Either<EncinaError, TResponse>> Handle(TRequest request, RequestHandlerCallback<TResponse> next, ...);

// Handlers now return Either (Pure ROP - no exceptions)
Task<Either<EncinaError, TResponse>> Handle(TRequest request, CancellationToken cancellationToken);
Task<Either<EncinaError, Unit>> Handle(TNotification notification, CancellationToken cancellationToken);
```

**Key Change:** Handlers now return `Either` types directly instead of plain `TResponse`. This completes the Pure ROP migration - handlers are responsible for returning success/failure explicitly rather than throwing exceptions.

### Error Flow

```
Request → [Validation Guards] → [Pre-Processors] → [Behaviors] → [Handler] → [Post-Processors] → Response
            ↓ Left                ↓ Left            ↓ Left        ↓ Left        ↓ Left              ↓ Right
         EncinaError        EncinaError     EncinaError  EncinaError  EncinaError      TResponse
```

Any Left value short-circuits the pipeline and returns immediately.

### EncinaError Structure

```csharp
public sealed class EncinaError
{
    public string Code { get; }           // e.g., "Encina.handler.missing"
    public string Message { get; }         // Human-readable description
    public Option<Exception> Exception { get; } // Original exception if any
    public IReadOnlyDictionary<string, object?> Details { get; } // Context metadata
}
```

## Consequences

### Positive

- **Type Safety:** Errors are part of the type system, enforced at compile time
- **Explicit Flow:** Clear distinction between success and failure paths
- **Composability:** Easy to chain and compose operations
- **Performance:** No exception throwing for expected failures
- **Testability:** Easy to test both success and failure scenarios
- **Observability:** Error codes and metadata enable better monitoring
- **Functional Style:** Aligns with functional programming principles

### Negative

- **Learning Curve:** Developers unfamiliar with functional patterns need to learn Either/Left/Right
- **Verbosity:** More verbose than try-catch blocks, requires explicit error handling
- **Library Dependency:** Depends on LanguageExt for Either type
- **Handler Complexity:** Handlers must return `Either` instead of throwing, requiring more ceremony

### Neutral

- **Exception Policy:** Exceptions are allowed in startup/configuration code but discouraged in runtime handlers
- **Safety Net:** Unexpected exceptions during handler execution are caught and converted to `EncinaError` with code `Encina.handler.exception`
- **Interoperability:** External dependencies that throw are wrapped in try-catch and converted to Left

## Alternatives Considered

### 1. Traditional Exceptions

```csharp
Task<TResponse> Send<TResponse>(IRequest<TResponse> request);
```

**Rejected:** Exceptions are invisible, expensive, and make composition difficult.

### 2. Result<T, E> Pattern (Custom Type)

```csharp
ValueTask<Result<TResponse, EncinaError>> Send<TResponse>(...)
```

**Rejected:** Reinventing the wheel. LanguageExt's Either is well-tested and feature-rich.

### 3. Discriminated Unions (C# 10+)

```csharp
ValueTask<OneOf<TResponse, EncinaError>> Send<TResponse>(...)
```

**Rejected:** OneOf lacks the functional combinators that Either provides (Map, Bind, Match, etc.).

### 4. Nullable Reference Types

```csharp
ValueTask<TResponse?> Send<TResponse>(...)
```

**Rejected:** Cannot distinguish between different error types. No error context.

## Examples

### Success Path

```csharp
var result = await Encina.Send(new GetUserQuery(userId));
result.Match(
    Right: user => Console.WriteLine($"Found: {user.Name}"),
    Left: error => Console.WriteLine($"Error: {error.Message}")
);
```

### Failure Path

```csharp
var result = await Encina.Send(new DeleteUserCommand(invalidId));
// result is Left<EncinaError>("Encina.handler.missing")
```

### Behavior Composition

```csharp
public async ValueTask<Either<EncinaError, TResponse>> Handle(...)
{
    // Can short-circuit with validation errors
    if (!IsValid(request))
        return Left(EncinaError.New("validation.failed"));

    // Continue pipeline
    var response = await next();

    // Can inspect and transform the result
    return response.Map(r => Transform(r));
}
```

## Related Decisions

- ADR-002: Dependency Injection Strategy (affects how errors from DI are handled)
- ADR-003: Caching Strategy (cached delegates still return Either)

## References

- [Railway Oriented Programming](https://fsharpforfunandprofit.com/rop/) by Scott Wlaschin
- [LanguageExt Either Documentation](https://github.com/louthy/language-ext/wiki/Either)
- [Functional C# with Language-Ext](https://github.com/louthy/language-ext)

## Exception Policy

### When Exceptions ARE Allowed

**Only during startup and configuration (fail-fast scenarios):**

1. **Service Registration:**
   - During `AddEncina()` - DI setup errors
   - Assembly scanning failures
   - Invalid configuration (missing handlers, invalid behavior registrations)
   - Null parameters in constructors (`ArgumentNullException`)
   - Invalid behavior/processor types (`ArgumentException`)

2. **These are fail-fast scenarios that prevent the application from starting incorrectly.**

### When Exceptions are NOT Allowed

**All runtime operations use Railway Oriented Programming:**

1. **API Entry Points:**
   - Null request/notification → `Left<EncinaError>` with code `request.null` or `notification.null`
   - Missing handler → `Left<EncinaError>` with code `request.handler.missing`
   - Type mismatches → `Left<EncinaError>` with code `request.handler.type_mismatch`

2. **Handler Execution:**
   - Handlers must return `Left<EncinaError>` for all failures
   - Validation errors → `Left`
   - Business rule violations → `Left`
   - Not found scenarios → `Left`
   - Unauthorized access → `Left`

3. **Operational Failures:**
   - Database connection issues → `Left`
   - External API failures → `Left`
   - Timeout scenarios → `Left`
   - Network errors → `Left`

### Exception Safety Net

If a handler accidentally throws an exception during execution:

- The exception is caught at the dispatcher level
- Converted to `EncinaError` with code `Encina.handler.exception`
- Logged with full context (handler type, request type, stage)
- Returned as `Left<EncinaError>` to the caller

This provides graceful degradation while encouraging proper Either-based error handling.

## Notes

This decision was made early in the project and fully adopted on 2025-12-12 with the Pure ROP migration. All handlers now return `Either` types, making error handling completely explicit and type-safe.

The functional approach with Either provides excellent composability and makes error handling explicit and testable. The learning curve is acceptable given the benefits, and the exception safety net ensures the library remains resilient even when handlers don't follow best practices.
