# ADR-004: Decision to NOT Implement EncinaResult<T>

**Status:** Rejected
**Date:** 2025-12-12
**Deciders:** Architecture Team
**Related:** ADR-001 (Railway Oriented Programming)

## Context

During the design phase, we considered creating a `EncinaResult<T>` wrapper type over `Either<EncinaError, T>` to provide a more discoverable and C#-friendly API.

### Proposed Design

```csharp
public sealed class EncinaResult<T>
{
    private readonly Either<EncinaError, T> _inner;

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    public T GetValue() => _inner.Match(
        Right: v => v,
        Left: _ => throw new InvalidOperationException("Result is not successful"));

    public EncinaError GetError() => _inner.Match(
        Right: _ => throw new InvalidOperationException("Result is successful"),
        Left: e => e);

    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<EncinaError, TResult> onFailure)
        => _inner.Match(Right: onSuccess, Left: onFailure);

    public EncinaResult<TNew> Map<TNew>(Func<T, TNew> mapper)
        => new EncinaResult<TNew>(_inner.Map(mapper));

    public EncinaResult<TNew> Bind<TNew>(Func<T, EncinaResult<TNew>> binder)
        => new EncinaResult<TNew>(_inner.Bind(v => binder(v)._inner));

    // Implicit conversions
    public static implicit operator EncinaResult<T>(T value) => ...
    public static implicit operator EncinaResult<T>(EncinaError error) => ...
    public static implicit operator Either<EncinaError, T>(EncinaResult<T> result) => result._inner;
}
```

### API Impact

```csharp
// Before (current)
ValueTask<Either<EncinaError, TResponse>> Send<TResponse>(...);

// After (proposed)
ValueTask<EncinaResult<TResponse>> Send<TResponse>(...);
```

## Decision

**We REJECT the implementation of `EncinaResult<T>`.**

We will continue using `Either<EncinaError, T>` from LanguageExt directly.

## Rationale

### 1. Unnecessary Abstraction

`Either<EncinaError, T>` from LanguageExt already provides everything we need:

- **Pattern matching:** `Match(Right: ..., Left: ...)`
- **Transformations:** `Map`, `Bind`, `MapLeft`, `BiMap`
- **Predicates:** `IsRight`, `IsLeft`
- **Extraction:** `IfLeft`, `IfRight`, `LeftOrDefault`, `RightOrDefault`
- **Conversions:** `ToOption`, `ToSeq`, `ToArray`

Adding `EncinaResult<T>` would duplicate all this functionality without adding value.

### 2. Added Complexity

- **Wrapper Overhead:** Extra layer of indirection with no functional benefit
- **Conversion Tax:** Constant conversions between `Either` and `EncinaResult`
- **API Surface:** Increases public API surface area for maintenance
- **Testing:** More code to test with no additional coverage of real scenarios

### 3. Loss of Composability

LanguageExt's `Either` integrates seamlessly with the rest of the LanguageExt ecosystem:

```csharp
// Works with other LanguageExt types
Either<EncinaError, User> result = ...;
Option<User> maybeUser = result.ToOption();
Seq<User> users = result.ToSeq();

// Composes with other Either operations
var final = from user in result
            from profile in GetProfile(user.Id)
            from settings in GetSettings(user.Id)
            select new UserView(user, profile, settings);
```

A custom `EncinaResult<T>` would break this integration.

### 4. Learning Curve is Acceptable

While `Either` has a learning curve for developers unfamiliar with functional programming, the investment pays off:

- **Industry Standard:** `Either` is a well-known pattern in functional programming
- **Transferable Knowledge:** Learning `Either` benefits developers across many FP libraries
- **Rich Ecosystem:** Access to extensive LanguageExt documentation and community
- **Real FP:** Encourages functional thinking rather than hiding behind imperative wrappers

### 5. Performance Considerations

- **Zero-cost abstraction:** LanguageExt's `Either` is highly optimized
- **Struct-based:** `Either` is a struct, avoiding heap allocations
- **JIT-friendly:** Well-tested and JIT-optimized over years
- **Wrapper cost:** `EncinaResult` would add allocation overhead

## Alternatives Considered

### Alternative 1: Extension Methods on Either

Instead of wrapping, provide extension methods:

```csharp
public static class EitherEncinaExtensions
{
    public static T GetValueOrThrow<T>(this Either<EncinaError, T> either)
        => either.Match(Right: v => v, Left: e => throw new EncinaException(e));
}
```

**Status:** Not needed. LanguageExt already provides sufficient extension methods.

### Alternative 2: Documentation and Examples

Provide comprehensive documentation showing how to use `Either` effectively.

**Status:** ✅ Adopted. Better documentation is always valuable.

### Alternative 3: Analyzer/CodeFix

Create a Roslyn analyzer to suggest `Either` usage patterns.

**Status:** Deferred. Documentation first, tooling if needed later.

## Consequences

### Positive

- ✅ **Simplicity:** Less code to maintain
- ✅ **Integration:** Full LanguageExt ecosystem available
- ✅ **Performance:** No wrapper overhead
- ✅ **Standards:** Using industry-standard functional patterns
- ✅ **Composability:** Seamless integration with other FP operations

### Negative

- ⚠️ **Learning Curve:** Developers must learn `Either` directly
- ⚠️ **Discoverability:** `Either` API might be less discoverable than custom wrapper
- ⚠️ **C# Style:** Feels less "C#-native" than custom types

### Mitigation

1. **Comprehensive Documentation:**
   - Provide clear examples of common `Either` patterns
   - Document common pitfalls and how to avoid them
   - Create a "quick start" guide for developers new to `Either`

2. **Code Examples:**
   - Include examples in XML documentation
   - Provide sample projects demonstrating patterns
   - Document common transformation patterns

3. **Team Education:**
   - Lunch & learn sessions on functional patterns
   - Pair programming for knowledge transfer
   - Code review focus on proper `Either` usage

## Examples

### Instead of EncinaResult, use Either directly

```csharp
// ✅ Correct usage with Either
var result = await Encina.Send(new GetUserQuery(userId));

result.Match(
    Right: user => Console.WriteLine($"Found: {user.Name}"),
    Left: error => Console.WriteLine($"Error: {error.Code} - {error.Message}")
);

// ✅ Transform and chain
var userName = result
    .Map(user => user.Name)
    .IfLeft("Unknown");

// ✅ Combine results
var combinedResult =
    from user in userResult
    from profile in profileResult
    select new { user, profile };
```

## Related Decisions

- ADR-001: Railway Oriented Programming (establishes `Either` as the core pattern)
- ADR-005: Decision to NOT Use Source Generators (related simplicity concerns)

## References

- [LanguageExt Either Documentation](https://github.com/louthy/language-ext/wiki/Either)
- [Railway Oriented Programming](https://fsharpforfunandprofit.com/rop/) - Scott Wlaschin
- [Functional Programming in C#](https://www.manning.com/books/functional-programming-in-c-sharp) - Enrico Buonanno

## Review

This decision will be reviewed if:

- Significant developer friction with `Either` API is observed
- LanguageExt makes breaking changes to `Either`
- Community feedback strongly favors a wrapper type

Last reviewed: 2025-12-12
