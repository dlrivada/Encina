# ADR-006: Pure Railway Oriented Programming - Fail-Fast Exception Handling

## Status

Accepted

## Context

Durante el trabajo de mutation testing, identificamos que el 43% de las mutaciones sobrevivientes (52 de 120) estaban en código defensivo de `PipelineBuilder.cs` y `RequestDispatcher.cs` que capturaba excepciones de behaviors, handlers, y processors para convertirlas en `Either<MediatorError, T>`.

Este código defensivo representaba un trade-off entre:

- **Robustez**: Proteger contra bugs de usuarios que olvidan usar ROP correctamente
- **Pureza ROP**: Excepciones = bugs de programación que deberían crashear (fail-fast)
- **Mutation score**: Código defensivo difícil de testear sin simular bugs del usuario

### Filosofía Original (Defensive)

```csharp
// PipelineBuilder.ExecuteBehaviorAsync
catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
{
    // OK: Cancellation es comportamiento esperado
    return Left(...);
}
catch (Exception ex)  // ← Código defensivo
{
    // Capturar cualquier bug del usuario y convertir a Left
    var metadata = new Dictionary<string, object?>
    {
        ["behavior"] = behavior.GetType().FullName,
        ["request"] = typeof(TRequest).FullName,
        ["stage"] = "behavior"
    };
    return Left(MediatorErrors.FromException(...));
}
```

Este patrón se repetía en 4 métodos de `PipelineBuilder` y en `RequestDispatcher`, resultando en:

- ~100 líneas de código defensivo
- 52 mutaciones en strings de metadata que nunca se ejercitaban en tests
- Complejidad adicional en el flujo de error handling

## Decision

**Adoptar Pure Railway Oriented Programming con fail-fast para todas las excepciones excepto `OperationCanceledException`.**

Esto significa:

1. **Eliminar** todos los `catch (Exception ex)` de `PipelineBuilder` y `RequestDispatcher`
2. **Mantener** solo `catch (OperationCanceledException)` porque la cancelación es comportamiento cooperativo esperado, no un bug
3. **Responsabilizar** a los usuarios de la biblioteca de convertir todos los errores esperados a `Either<Left>`
4. **Permitir** que cualquier otra excepción (NullReferenceException, InvalidOperationException, etc.) propague y crashee el proceso

### Código Resultante (Pure ROP)

```csharp
// PipelineBuilder.ExecuteBehaviorAsync
private static async ValueTask<Either<MediatorError, TResponse>> ExecuteBehaviorAsync(...)
{
    try
    {
        return await behavior.Handle(request, nextStep, cancellationToken).ConfigureAwait(false);
    }
    catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
    {
        // Cancellation es esperado, convertir a Left
        var message = $"Behavior {behavior.GetType().Name} cancelled...";
        var metadata = new Dictionary<string, object?>
        {
            ["behavior"] = behavior.GetType().FullName,
            ["request"] = typeof(TRequest).FullName,
            ["stage"] = "behavior"
        };
        return Left(MediatorErrors.Create(MediatorErrorCodes.BehaviorCancelled, message, ex, metadata));
    }
    // Pure ROP: Cualquier otra excepción indica un bug y propagará (fail-fast)
}
```

## Consequences

### Positive

✅ **Mutation Score mejorado**: De 73.87% a **78.49%** (+4.62 pp)

- Eliminados 28 mutantes de código defensivo en PipelineBuilder
- Eliminados ~10 mutantes de código defensivo en RequestDispatcher
- Total: ~38 mutantes eliminados

✅ **Código más simple**: ~100 líneas menos de código defensivo

✅ **Fail-fast principle**: Bugs se detectan inmediatamente en desarrollo/testing

✅ **Filosofía ROP más pura**: Excepciones = bugs, no errores funcionales

✅ **Menos ambigüedad**: Es claro que las excepciones son siempre bugs

### Negative

❌ **Menos robusto ante bugs de usuarios**: Un `throw` accidental en un handler crashea el proceso

❌ **Curva de aprendizaje más pronunciada**: Usuarios deben entender ROP completamente desde el inicio

❌ **Debugging en producción**: Crashes en lugar de `Left<MediatorError>` con metadata

### Mitigations

Para mitigar los aspectos negativos:

1. **Documentación clara** en README y guías de usuario sobre ROP obligatorio
2. **Ejemplos exhaustivos** de handlers bien escritos que nunca lanzan
3. **Analyzers/Linters** (futuro) que detecten `throw` en handlers
4. **Tests contractuales** que validen que handlers retornan Either correctamente

## Migration Impact

### Tests Afectados

- **9 tests skipped** en `EncinaTests.cs` que verificaban captura de excepciones
- **1 test skipped** en `ConfigurationProperties.cs` (property test que verificaba excepciones)
- Total: **10 tests skipped** con razón: `"Pure ROP: exceptions now propagate (fail-fast)"`

Estos tests verificaban comportamiento defensivo que ya no existe. En ROP puro, las excepciones de handlers/behaviors/processors son bugs del código del usuario, no de la biblioteca.

### Breaking Change

**Sí, esto es un breaking change** para usuarios que dependían del comportamiento defensivo:

**Antes (v1.x - Defensive)**:

```csharp
public class MyHandler : IRequestHandler<MyRequest, int>
{
    public Task<Either<MediatorError, int>> Handle(MyRequest request, CancellationToken ct)
    {
        if (request.Value < 0)
            throw new InvalidOperationException("Negative value");  // ← Capturado, convertido a Left
        return Task.FromResult(Right<MediatorError, int>(request.Value));
    }
}
```

- **Resultado**: Exception capturada → `Left<MediatorError>`
- **Usuario ve**: Error limpio con código `mediator.handler.exception`

**Ahora (v2.0+ - Pure ROP)**:

```csharp
public class MyHandler : IRequestHandler<MyRequest, int>
{
    public Task<Either<MediatorError, int>> Handle(MyRequest request, CancellationToken ct)
    {
        if (request.Value < 0)
            throw new InvalidOperationException("Negative value");  // ← CRASH!
        return Task.FromResult(Right<MediatorError, int>(request.Value));
    }
}
```

- **Resultado**: **Process crash** con stack trace
- **Corrección**: `return Task.FromResult(Left<MediatorError, int>(Errors.NegativeValue));`

### Migration Guide

Para usuarios migrando a v2.0+:

```csharp
// ❌ ANTES (Defensivo - Ya no funciona)
public async Task<Either<MediatorError, User>> Handle(GetUserQuery request, CancellationToken ct)
{
    var user = await _repo.FindByIdAsync(request.Id);
    if (user == null)
        throw new NotFoundException("User not found");  // ← Ahora crashea!
    return Right<MediatorError, User>(user);
}

// ✅ AHORA (Pure ROP - Correcto)
public async Task<Either<MediatorError, User>> Handle(GetUserQuery request, CancellationToken ct)
{
    var userOption = await _repo.FindByIdAsync(request.Id);
    return userOption.Match(
        Some: user => Right<MediatorError, User>(user),
        None: () => Left<MediatorError, User>(Errors.UserNotFound)  // ← Funcional
    );
}
```

## Alternatives Considered

### Opción A: Status Quo (Captura en RequestDispatcher)

- **Pro**: Más robusto, 75% mutation score
- **Con**: Código defensivo complejo, metadata genérica
- **Decisión**: Rechazada - No alineada con filosofía ROP

### Opción B: Captura en PipelineBuilder con metadata rica

- **Pro**: Metadata detallada, ~80% mutation score
- **Con**: Tres niveles de try-catch, mucha complejidad
- **Decisión**: Rechazada - Complejidad no justificada para código defensivo

### Opción C: Pure ROP con fail-fast (ELEGIDA)

- **Pro**: Simple, 78.49% mutation score, puro ROP
- **Con**: Menos robusto para usuarios novatos
- **Decisión**: **Aceptada** - Alineada con filosofía de la biblioteca

## References

- [Railway Oriented Programming (Scott Wlaschin)](https://fsharpforfunandprofit.com/rop/)
- [Fail-Fast Principle (Martin Fowler)](https://martinfowler.com/ieeeSoftware/failFast.pdf)
- [Exception Handling Analysis](../exception-handling-analysis.md)
- Mutation Testing Report: 78.49% (2025-12-13)

## Version

Decision made: 2025-12-13
Implemented in: v2.0.0
Mutation Score: 73.87% → 78.49% (+4.62%)
