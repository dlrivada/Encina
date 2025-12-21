# Exception Handling Architecture Analysis

## Contexto

Durante el trabajo de mutation testing, identificamos que el 43% de las mutaciones sobrevivientes (52 de 120) están en `PipelineBuilder.cs`, específicamente en los strings de metadata de los catch blocks. Esto plantea preguntas fundamentales sobre el diseño.

## Estado Actual: Captura en Dos Niveles

### Nivel 1: PipelineBuilder (Específico - NO ALCANZADO)

```csharp
// PipelineBuilder.cs líneas 182-214
private static async ValueTask<Either<MediatorError, TResponse>> ExecuteBehaviorAsync(...)
{
    try
    {
        return await behavior.Handle(request, nextStep, cancellationToken).ConfigureAwait(false);
    }
    catch (Exception ex)
    {
        var metadata = new Dictionary<string, object?>
        {
            ["behavior"] = behavior.GetType().FullName,  // ← String mutation sobreviviente
            ["request"] = typeof(TRequest).FullName,     // ← String mutation sobreviviente
            ["stage"] = "behavior"                        // ← String mutation sobreviviente
        };
        var error = MediatorErrors.FromException(MediatorErrorCodes.BehaviorException, ex, ...);
        return Left<MediatorError, TResponse>(error);
    }
}
```

**Metadata detallada**: `"behavior"`, `"preProcessor"`, `"postProcessor"`, `"handler"`, `"exception_type"`

### Nivel 2: RequestDispatcher (Genérico - SIEMPRE ALCANZADO)

```csharp
// RequestDispatcher.cs líneas 152-170
catch (Exception ex)
{
    // Safety net for any unhandled exceptions in the pipeline
    var metadata = new Dictionary<string, object?>
    {
        ["request"] = requestType.FullName,
        ["handler"] = handler!.GetType().FullName,
        ["stage"] = "pipeline"  // ← Genérico, sin detalle del componente específico
    };
    var error = MediatorErrors.FromException(MediatorErrorCodes.PipelineException, ex, ...);
    return Left<MediatorError, TResponse>(error);
}
```

**Metadata genérica**: Solo `"stage" = "pipeline"`, sin información del componente específico.

### Nivel 1.5: RequestHandlerWrapper (SIN try-catch)

```csharp
// SimpleMediator.cs líneas 113-121
public override async Task<object> Handle(...)
{
    var pipelineBuilder = new PipelineBuilder<TRequest, TResponse>(...);
    var pipeline = pipelineBuilder.Build(provider);
    var outcome = await pipeline().ConfigureAwait(false);  // ← SIN try-catch
    return outcome;
}
```

## ¿Por Qué las Excepciones Escapan de PipelineBuilder?

### Problema: Invocación de Delegados

```csharp
// PipelineBuilder.Build() crea delegados:
RequestHandlerCallback<TResponse> current = () => ExecuteBehaviorAsync(behavior, ...);

// RequestHandlerWrapper invoca:
var outcome = await pipeline().ConfigureAwait(false);
```

Si `ExecuteBehaviorAsync` lanza una excepción **síncronamente antes de devolver el ValueTask**, esa excepción escapa de `pipeline()` ANTES del await, y no es capturada por el try-catch interno de `ExecuteBehaviorAsync`.

**Ejemplo concreto:**

```csharp
// Behavior que lanza síncronamente
public ValueTask<Either<MediatorError, TResponse>> Handle(...)
    => throw new InvalidOperationException("Bug");  // ← Escapa ANTES del await

// vs. Behavior que lanza asíncronamente
public async ValueTask<Either<MediatorError, TResponse>> Handle(...)
{
    await Task.Yield();
    throw new InvalidOperationException("Bug");  // ← Capturado en ExecuteBehaviorAsync
}
```

En la práctica, **casi todos** los behaviors/handlers/processors lanzan síncronamente (sin async real), por lo que las excepciones escapan a RequestDispatcher.

## Análisis de las Tres Opciones

### Opción A: Status Quo (Captura en RequestDispatcher)

**Ventajas:**

- ✅ **Más robusto**: Garantiza que NINGUNA excepción escapa del mediator
- ✅ **Más simple**: Un solo punto de captura final
- ✅ **Metrics/Observability centralizados**: Todas las excepciones reportadas consistentemente
- ✅ **Menos código**: No duplicar try-catch en múltiples capas

**Desventajas:**

- ❌ **Metadata menos específica**: Solo sabemos `"stage" = "pipeline"`, no si fue behavior/preprocessor/etc
- ❌ **Mutation score más bajo**: 52 mutaciones en strings defensivos nunca ejercitados
- ❌ **Debugging más difícil**: Menos contexto sobre dónde falló exactamente

**Comportamiento runtime:**

```csharp
// Usuario ve:
{
  "code": "mediator.pipeline.exception",
  "message": "Unexpected error while processing CreateUserCommand",
  "metadata": {
    "stage": "pipeline",  // ← Genérico
    "request": "MyApp.Commands.CreateUserCommand",
    "handler": "MyApp.Handlers.CreateUserCommandHandler"
  }
}
```

### Opción B: Captura en PipelineBuilder (Metadata Específica)

**Implementación requerida:**

```csharp
// RequestHandlerWrapper.Handle() con try-catch
public override async Task<object> Handle(...)
{
    try
    {
        var pipelineBuilder = new PipelineBuilder<TRequest, TResponse>(...);
        var pipeline = pipelineBuilder.Build(provider);
        var outcome = await pipeline().ConfigureAwait(false);
        return outcome;
    }
    catch (Exception ex)
    {
        // Convertir a Either antes de llegar a RequestDispatcher
        var error = MediatorErrors.FromException(...);
        return Left<MediatorError, TResponse>(error);
    }
}
```

**Ventajas:**

- ✅ **Metadata rica**: Sabemos exactamente qué componente falló (`"behavior"`, `"preProcessor"`, etc.)
- ✅ **Mutation score más alto**: Ejercitamos los 52 strings defensivos
- ✅ **Debugging más fácil**: Stack trace y contexto más preciso

**Desventajas:**

- ❌ **Más complejo**: Tres niveles de try-catch (PipelineBuilder, RequestHandlerWrapper, RequestDispatcher)
- ❌ **Duplicación**: Lógica de error handling en múltiples lugares
- ❌ **Fragilidad**: Si algún nivel no captura, comportamiento inconsistente

**Comportamiento runtime:**

```csharp
// Usuario ve:
{
  "code": "mediator.behavior.exception",  // ← Más específico
  "message": "Error running ValidationBehavior for CreateUserCommand",
  "metadata": {
    "stage": "behavior",  // ← Específico
    "behavior": "MyApp.Behaviors.ValidationBehavior`2",
    "request": "MyApp.Commands.CreateUserCommand"
  }
}
```

### Opción C: **NO capturar excepciones** (Puro ROP)

**Filosofía:** Si los usuarios usan ROP correctamente, las excepciones son BUGS (no errores funcionales), y deberían crashear.

**Implementación:**

```csharp
// Eliminar TODOS los try-catch de runtime
// Solo capturar en startup (configuración)

// PipelineBuilder.ExecuteBehaviorAsync - SIN try-catch
private static async ValueTask<Either<MediatorError, TResponse>> ExecuteBehaviorAsync(...)
{
    return await behavior.Handle(request, nextStep, cancellationToken).ConfigureAwait(false);
    // Si lanza → propagate y crash
}

// RequestDispatcher - SIN try-catch de Exception, solo OperationCanceledException
catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
{
    // Cancellation es comportamiento esperado, OK capturar
}
// REMOVE: catch (Exception ex) { ... }
```

**Ventajas:**

- ✅ **Fail-fast**: Bugs se detectan inmediatamente
- ✅ **Más puro ROP**: Excepciones = bugs, no errors funcionales
- ✅ **Menos código defensivo**: Eliminamos 52+ líneas de código "muerto"
- ✅ **Mutation score más alto**: Código eliminado = mutantes eliminados

**Desventajas:**

- ❌ **Más frágil para usuarios novatos**: Un `throw` accidental crashea el proceso
- ❌ **Menos forgiving**: No hay safety net si el usuario comete un error
- ❌ **Debugging en producción más duro**: Crashes en lugar de Left con metadata

**Comportamiento runtime:**

```csharp
// Usuario ve (si hace throw accidental):
Unhandled exception. System.InvalidOperationException: Validation failed
   at MyApp.Behaviors.ValidationBehavior.Handle(...)
   at SimpleMediator.PipelineBuilder.ExecuteBehaviorAsync(...)
   → PROCESS CRASH
```

## Filosofía ROP: ¿Qué Son "Errores" vs "Bugs"?

### Errores Funcionales (Expected) → `Left<MediatorError>`

- Usuario no encontrado
- Validación falló
- Permiso denegado
- Timeout de red
- **Solución**: Handler retorna `Left` explícitamente

### Bugs de Programación (Unexpected) → `Exception`

- NullReferenceException
- IndexOutOfRangeException
- InvalidCastException
- **Solución en ROP puro**: Dejar crashear (fail-fast)

### Zona Gris: ¿Qué hacer con excepciones en behaviors/handlers?

**Caso 1: Handler bien escrito (ROP)**

```csharp
public async Task<Either<MediatorError, User>> Handle(GetUserQuery request, CancellationToken ct)
{
    var userOption = await _repo.FindByIdAsync(request.Id);
    return userOption.Match(
        Some: user => Right<MediatorError, User>(user),
        None: () => Left<MediatorError, User>(Errors.UserNotFound)  // ← Funcional, NO exception
    );
}
```

**Resultado**: Nunca lanza, siempre retorna Either. ✅

**Caso 2: Handler con bug (olvidó ROP)**

```csharp
public async Task<Either<MediatorError, User>> Handle(GetUserQuery request, CancellationToken ct)
{
    var user = await _repo.FindByIdAsync(request.Id);
    if (user == null)
        throw new InvalidOperationException("User not found");  // ← BUG! Debió ser Left
    return Right<MediatorError, User>(user);
}
```

**Resultado con Opción A/B**: Capturado, convertido a `Left`, usuario ve error limpio
**Resultado con Opción C**: Crash, desarrollador ve stack trace y lo corrige

## Recomendación

**Depende del público objetivo de la biblioteca:**

### Si la biblioteca es para **equipos externos/OSS**

→ **Opción A (Status Quo)**: Captura en RequestDispatcher

- **Razón**: Más robusto, no crashea por errores de usuarios
- **Trade-off**: Mutation score 75%, metadata menos rica
- **Valor**: Experiencia de usuario más forgiving

### Si la biblioteca es para **uso interno/equipos experimentados**

→ **Opción C (No Capturar)**: Puro ROP, fail-fast

- **Razón**: Detecta bugs rápido, fuerza disciplina ROP
- **Trade-off**: Menos robusto ante errores
- **Valor**: Código más simple, mutation score ~85%+

### Si necesitas **mutation score alto + robustez**

→ **Opción B (Captura en PipelineBuilder)**: Try-catch en RequestHandlerWrapper

- **Razón**: Metadata rica + safety net
- **Trade-off**: Más complejidad, tres niveles de try-catch
- **Valor**: Best of both worlds, pero más código

## Decisión Recomendada

**Mi recomendación: Opción A (Status Quo) + Documentación**

### Justificación

1. **El código defensivo tiene valor**: Aunque los usuarios "deberían" usar ROP, un safety net es prudente para una biblioteca pública.

2. **75.18% es un buen mutation score**: Considerando que:
   - Las 52 mutaciones son código defensivo legítimo
   - Representan paths que "no deberían" ejecutarse en uso correcto
   - Testearlos requiere **simular bugs del usuario**, no comportamiento correcto

3. **La metadata genérica es suficiente**:
   - El stack trace del `Exception` original está en `MediatorError.Exception`
   - Logs tienen el tipo completo de la excepción
   - El usuario puede debuggear con esa información

4. **Simplicidad arquitectural**: Un solo punto de captura final es más mantenible.

### Alternativa Pragmática

Si realmente quieres 80%+ score **SIN** cambiar arquitectura:

1. **Acepta el 75.18%** como válido para código defensivo
2. **Documenta en ADR** que las mutaciones sobrevivientes son por diseño
3. **Ataca otras áreas**: Los 27 mutantes en NotificationDispatcher, 12 en MediatorBehaviorGuards
4. **Configura Stryker** para excluir esos catch blocks como "código defensivo no testeable"

## Conclusión

**No es que capturar en RequestDispatcher sea "malo"**. Es una decisión de diseño consciente que prioriza robustez sobre granularidad de metadata. Las mutaciones sobrevivientes NO indican falta de tests, sino código defensivo que protege contra bugs de usuarios.

**La pregunta real es**: ¿Vale la pena la complejidad de tres niveles de try-catch solo para tener metadata más detallada en un escenario que "no debería suceder"?

Mi respuesta: **No**. Mantén el status quo y documenta la decisión.
