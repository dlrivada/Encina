# SimpleMediator.GuardClauses - Análisis de Diseño e Implementación

## 📋 Tabla de Contenidos

1. [Resumen Ejecutivo](#resumen-ejecutivo)
2. [Diferencias Fundamentales con Otras Librerías de Validación](#diferencias-fundamentales)
3. [Propuestas de Diseño](#propuestas-de-diseño)
4. [Impacto en el API Actual](#impacto-en-el-api-actual)
5. [Ventajas y Desventajas](#ventajas-y-desventajas)
6. [Recomendación Final](#recomendación-final)

---

## Resumen Ejecutivo

**Guard Clauses** es fundamentalmente **DIFERENTE** a FluentValidation, DataAnnotations y MiniValidator:

| Aspecto | FluentValidation/DataAnnotations/MiniValidator | Guard Clauses |
|---------|-----------------------------------------------|---------------|
| **Cuándo se ejecuta** | **ANTES** del handler (pipeline behavior) | **DENTRO** del handler |
| **Qué valida** | Input del request (validación externa) | Preconditions/invariants (defensive programming) |
| **Propósito** | Validación de input del usuario | Defensive programming contra bugs |
| **Retorna** | `Left<MediatorError>` automáticamente | Throw exception o custom error |
| **Patrón** | Pipeline interception | Guard pattern |

**Conclusión clave**: Guard Clauses NO es un pipeline behavior. Requiere un diseño completamente diferente.

---

## Diferencias Fundamentales

### 1. Validación de Input vs Defensive Programming

#### Validación de Input (FluentValidation, etc.)

```csharp
// ANTES del handler - valida input del usuario
public record CreateUser(string Email, string Password) : ICommand<User>;

public class CreateUserValidator : AbstractValidator<CreateUser>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).MinimumLength(8);
    }
}

// Handler recibe request VÁLIDO
public class CreateUserHandler : ICommandHandler<CreateUser, User>
{
    public async Task<Either<MediatorError, User>> Handle(CreateUser request, CancellationToken ct)
    {
        // request.Email ya está validado como email válido
        var user = new User(request.Email, request.Password);
        return user;
    }
}
```

#### Defensive Programming (Guard Clauses)

```csharp
// DENTRO del handler - valida preconditions de métodos/constructores
public class CreateUserHandler : ICommandHandler<CreateUser, User>
{
    private readonly IUserRepository _users;

    public async Task<Either<MediatorError, User>> Handle(CreateUser request, CancellationToken ct)
    {
        // Guard contra bugs de programación
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrEmpty(request.Email, nameof(request.Email));

        var existingUser = await _users.FindByEmail(request.Email, ct);

        // Guard contra estado inválido
        Guard.Against.NotNull(existingUser, nameof(existingUser), "User already exists");

        var user = new User(request.Email, request.Password);
        return user;
    }
}

// O en el constructor del domain model
public class User
{
    public User(string email, string password)
    {
        Email = Guard.Against.NullOrEmpty(email, nameof(email));
        Password = Guard.Against.NullOrEmpty(password, nameof(password));
        Guard.Against.InvalidFormat(email, nameof(email), @"^[^@]+@[^@]+$", "Invalid email");
    }

    public string Email { get; }
    public string Password { get; }
}
```

**Diferencia clave**:

- **Validación de input**: Protege contra usuarios malintencionados/errores de UX
- **Guard Clauses**: Protege contra bugs de programación y violaciones de invariantes

---

## Propuestas de Diseño

### ❌ Opción 1: Pipeline Behavior (NO RECOMENDADA)

```csharp
// ❌ NO tiene sentido - los guards son para DENTRO del handler
public class GuardClausesBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    public async ValueTask<Either<MediatorError, TResponse>> Handle(...)
    {
        // ¿Qué validar aquí? Los guards son específicos de cada handler
        // No hay forma genérica de saber qué preconditions validar
        return await nextStep();
    }
}
```

**Por qué NO funciona**:

- Guards son específicos del contexto (cada handler tiene diferentes preconditions)
- No hay forma genérica de saber qué validar sin conocer la lógica del handler
- Los guards se usan para validar estado interno, no input externo

---

### ✅ Opción 2: Extension Methods para ROP (RECOMENDADA)

Crear extension methods que integren Ardalis.GuardClauses con el sistema ROP de SimpleMediator.

```csharp
namespace SimpleMediator.GuardClauses;

public static class GuardExtensions
{
    /// <summary>
    /// Wraps Guard.Against.Null to return Either instead of throwing
    /// </summary>
    public static Either<MediatorError, T> GuardNotNull<T>(
        this T value,
        string parameterName,
        string? message = null) where T : class
    {
        try
        {
            Guard.Against.Null(value, parameterName, message);
            return Right<MediatorError, T>(value);
        }
        catch (ArgumentNullException ex)
        {
            return Left<MediatorError, T>(MediatorError.New(ex, message ?? ex.Message));
        }
    }

    /// <summary>
    /// Wraps Guard.Against.NullOrEmpty to return Either
    /// </summary>
    public static Either<MediatorError, string> GuardNotNullOrEmpty(
        this string value,
        string parameterName,
        string? message = null)
    {
        try
        {
            Guard.Against.NullOrEmpty(value, parameterName, message);
            return Right<MediatorError, string>(value);
        }
        catch (ArgumentException ex)
        {
            return Left<MediatorError, string>(MediatorError.New(ex, message ?? ex.Message));
        }
    }

    /// <summary>
    /// Wraps Guard.Against.NegativeOrZero to return Either
    /// </summary>
    public static Either<MediatorError, T> GuardPositive<T>(
        this T value,
        string parameterName,
        string? message = null) where T : struct, IComparable
    {
        try
        {
            Guard.Against.NegativeOrZero(value, parameterName, message);
            return Right<MediatorError, T>(value);
        }
        catch (ArgumentException ex)
        {
            return Left<MediatorError, T>(MediatorError.New(ex, message ?? ex.Message));
        }
    }

    // ... más guards según sea necesario
}
```

**Uso en handlers**:

```csharp
public class CreateOrderHandler : ICommandHandler<CreateOrder, OrderId>
{
    private readonly IOrderRepository _orders;

    public async Task<Either<MediatorError, OrderId>> Handle(
        CreateOrder request,
        CancellationToken ct)
    {
        // Estilo funcional con ROP
        return await request.CustomerId
            .GuardNotNullOrEmpty(nameof(request.CustomerId), "Customer ID is required")
            .Bind(customerId => request.Quantity
                .GuardPositive(nameof(request.Quantity), "Quantity must be positive")
                .Map(_ => customerId))
            .BindAsync(async customerId =>
            {
                var customer = await _customers.FindById(customerId, ct);
                return customer.GuardNotNull(nameof(customer), $"Customer {customerId} not found");
            })
            .BindAsync(async customer =>
            {
                var order = new Order(customer.Id, request.Items);
                await _orders.Save(order, ct);
                return Right<MediatorError, OrderId>(order.Id);
            });
    }
}
```

**Ventajas**:

- ✅ Integración natural con ROP
- ✅ No modifica el API actual de SimpleMediator
- ✅ Los developers eligen cuándo usar guards
- ✅ Composable con Bind/Map de LanguageExt

**Desventajas**:

- ⚠️ Requiere estilo funcional (puede ser unfamiliar para algunos)
- ⚠️ Más verboso que guards tradicionales

---

### 🔄 Opción 3: Helper Methods Imperativos (ALTERNATIVA)

Para developers que prefieren estilo imperativo:

```csharp
namespace SimpleMediator.GuardClauses;

public static class GuardHelpers
{
    /// <summary>
    /// Validates and returns Left if guard fails, otherwise continues
    /// </summary>
    public static Either<MediatorError, Unit> EnsureNotNull<T>(
        T value,
        string parameterName,
        string? message = null) where T : class
    {
        try
        {
            Guard.Against.Null(value, parameterName, message);
            return Right<MediatorError, Unit>(Unit.Default);
        }
        catch (ArgumentNullException ex)
        {
            return Left<MediatorError, Unit>(MediatorError.New(ex, message ?? ex.Message));
        }
    }

    // ... más helpers
}
```

**Uso imperativo**:

```csharp
public async Task<Either<MediatorError, OrderId>> Handle(
    CreateOrder request,
    CancellationToken ct)
{
    // Estilo imperativo - early return
    var guardResult = GuardHelpers.EnsureNotNull(request, nameof(request));
    if (guardResult.IsLeft) return guardResult.Map(_ => default(OrderId));

    guardResult = GuardHelpers.EnsurePositive(request.Quantity, nameof(request.Quantity));
    if (guardResult.IsLeft) return guardResult.Map(_ => default(OrderId));

    // Continuar con lógica del handler
    var order = new Order(request.CustomerId, request.Items);
    await _orders.Save(order, ct);
    return Right<MediatorError, OrderId>(order.Id);
}
```

---

### 🎯 Opción 4: Hybrid Approach (MEJOR DE AMBOS MUNDOS)

Ofrecer AMBOS estilos - funcional e imperativo:

```csharp
namespace SimpleMediator.GuardClauses;

/// <summary>
/// Functional-style guards that return Either
/// </summary>
public static class GuardExtensions
{
    public static Either<MediatorError, T> GuardNotNull<T>(this T value, string name, string? msg = null) { ... }
    public static Either<MediatorError, string> GuardNotEmpty(this string value, string name, string? msg = null) { ... }
    // ... más guards funcionales
}

/// <summary>
/// Imperative-style guards for early-return pattern
/// </summary>
public static class GuardHelpers
{
    public static Either<MediatorError, Unit> EnsureNotNull<T>(T value, string name, string? msg = null) { ... }
    public static Either<MediatorError, Unit> EnsureNotEmpty(string value, string name, string? msg = null) { ... }
    // ... más helpers imperativos
}
```

**Developers eligen su estilo preferido**:

```csharp
// Estilo funcional
return request.Email
    .GuardNotEmpty(nameof(request.Email))
    .Bind(email => email.GuardValidEmail())
    .BindAsync(async email => await CreateUser(email));

// Estilo imperativo
var emailGuard = GuardHelpers.EnsureNotEmpty(request.Email, nameof(request.Email));
if (emailGuard.IsLeft) return emailGuard.Map(_ => default(UserId));

var formatGuard = GuardHelpers.EnsureValidEmail(request.Email);
if (formatGuard.IsLeft) return formatGuard.Map(_ => default(UserId));

return await CreateUser(request.Email);
```

---

## Impacto en el API Actual

### ✅ Impacto MÍNIMO (Opción 2, 3, o 4)

**Lo que NO cambia**:

- ❌ NO requiere modificar `IPipelineBehavior`
- ❌ NO requiere modificar `IRequestContext`
- ❌ NO requiere modificar handlers existentes
- ❌ NO requiere modificar el pipeline de SimpleMediator

**Lo que se AGREGA**:

- ✅ Nuevo package: `SimpleMediator.GuardClauses`
- ✅ Extension methods para `Either<MediatorError, T>`
- ✅ Helpers opcionales para estilo imperativo
- ✅ Documentación y ejemplos

**Compatibilidad**:

- ✅ 100% compatible con código existente
- ✅ Opt-in (solo se usa si el developer lo importa)
- ✅ No afecta performance si no se usa

---

## Ventajas y Desventajas

### ✅ Ventajas de Implementar GuardClauses

1. **Defensive Programming con ROP**
   - Integra guards con Either<MediatorError, T>
   - Mantiene la filosofía funcional de SimpleMediator

2. **Complementa Validación de Input**
   - FluentValidation/DataAnnotations: validan input externo
   - GuardClauses: validan preconditions internas

3. **Mejora la Robustez**
   - Protege contra null reference exceptions
   - Valida invariantes de dominio
   - Fail-fast ante estados inválidos

4. **Zero Breaking Changes**
   - No modifica API existente
   - Opt-in (solo se usa si se quiere)
   - Compatible con todos los handlers existentes

5. **Completa el Ecosistema de Validación**
   - FluentValidation: validación compleja de input
   - DataAnnotations: validación simple de input
   - MiniValidator: validación ligera de input
   - **GuardClauses**: defensive programming interno

### ❌ Desventajas de Implementar GuardClauses

1. **Puede Ser Confuso para Developers**
   - ¿Cuándo usar FluentValidation vs GuardClauses?
   - Curva de aprendizaje adicional
   - Riesgo de over-engineering

2. **Duplicación de Validación**

   ```csharp
   // Ya validado por FluentValidation
   public record CreateUser(string Email) : ICommand<User>;

   // ¿Por qué validar de nuevo con guards?
   public class CreateUserHandler : ICommandHandler<CreateUser, User>
   {
       public Task<Either<MediatorError, User>> Handle(CreateUser request, CancellationToken ct)
       {
           // Redundante si FluentValidation ya lo validó
           return request.Email.GuardNotEmpty(nameof(request.Email))
               .BindAsync(email => CreateUser(email));
       }
   }
   ```

3. **Los Guards Son Más Útiles en Domain Models**

   ```csharp
   // Aquí SÍ tiene sentido
   public class User
   {
       public User(string email, string password)
       {
           // Guard en constructor de domain model
           Email = Guard.Against.NullOrEmpty(email, nameof(email));
           Password = Guard.Against.NullOrEmpty(password, nameof(password));
       }
   }

   // En handlers es menos necesario si ya hay validación de input
   ```

4. **Potencial Confusión con Exceptions**
   - Ardalis.GuardClauses usa exceptions
   - SimpleMediator usa ROP (Either)
   - Hay que wrappear las exceptions → overhead

5. **Overhead de Wrapping Exceptions**

   ```csharp
   // Cada guard tiene try-catch
   public static Either<MediatorError, T> GuardNotNull<T>(this T value, string name)
   {
       try
       {
           Guard.Against.Null(value, name); // Puede lanzar exception
           return Right<MediatorError, T>(value);
       }
       catch (ArgumentNullException ex) // Wrapping exception
       {
           return Left<MediatorError, T>(MediatorError.New(ex));
       }
   }
   ```

---

## Casos de Uso Reales

### ✅ Dónde SÍ Tiene Sentido Usar GuardClauses

#### 1. Domain Models (Constructor Guards)

```csharp
public class Order
{
    public Order(CustomerId customerId, List<OrderItem> items)
    {
        // Guards protegen invariantes del dominio
        CustomerId = Guard.Against.Null(customerId, nameof(customerId));
        Items = Guard.Against.NullOrEmpty(items, nameof(items));
        Guard.Against.Negative(items.Sum(i => i.Quantity), nameof(items), "Order must have at least one item");

        TotalAmount = items.Sum(i => i.Price * i.Quantity);
    }

    public CustomerId CustomerId { get; }
    public List<OrderItem> Items { get; }
    public decimal TotalAmount { get; }
}
```

#### 2. Validación de Estado Recuperado de DB

```csharp
public class CancelOrderHandler : ICommandHandler<CancelOrder, Unit>
{
    public async Task<Either<MediatorError, Unit>> Handle(CancelOrder request, CancellationToken ct)
    {
        var order = await _orders.FindById(request.OrderId, ct);

        // Guard contra estado inválido recuperado de DB
        return order
            .GuardNotNull(nameof(order), $"Order {request.OrderId} not found")
            .Bind(o => o.Status == OrderStatus.Cancelled
                ? Left<MediatorError, Order>(MediatorError.New("Order already cancelled"))
                : Right<MediatorError, Order>(o))
            .BindAsync(async o =>
            {
                o.Cancel();
                await _orders.Save(o, ct);
                return Right<MediatorError, Unit>(Unit.Default);
            });
    }
}
```

#### 3. Preconditions en Domain Services

```csharp
public class OrderDomainService
{
    public Either<MediatorError, Order> CreateOrder(Customer customer, List<OrderItem> items)
    {
        // Guards validan preconditions del método
        return customer
            .GuardNotNull(nameof(customer))
            .Bind(c => c.IsActive
                ? Right<MediatorError, Customer>(c)
                : Left<MediatorError, Customer>(MediatorError.New("Customer is inactive")))
            .Bind(c => items
                .GuardNotNullOrEmpty(nameof(items))
                .Map(_ => c))
            .Map(c => new Order(c.Id, items));
    }
}
```

### ❌ Dónde NO Tiene Tanto Sentido

#### 1. Handlers con Validación de Input Ya Hecha

```csharp
// ❌ Redundante - FluentValidation ya validó esto
public class CreateUserHandler : ICommandHandler<CreateUser, UserId>
{
    public Task<Either<MediatorError, UserId>> Handle(CreateUser request, CancellationToken ct)
    {
        // Si FluentValidation ya validó que Email no es null/empty,
        // ¿para qué validar de nuevo con guards?
        return request.Email
            .GuardNotEmpty(nameof(request.Email)) // Redundante
            .GuardValidEmail() // Redundante
            .BindAsync(email => CreateUser(email));
    }
}

// ✅ Mejor: Confiar en la validación de input
public Task<Either<MediatorError, UserId>> Handle(CreateUser request, CancellationToken ct)
{
    // request.Email ya está validado por FluentValidation
    return CreateUser(request.Email);
}
```

---

## Recomendación Final

### 🎯 Decisión: IMPLEMENTAR con Enfoque Híbrido (Opción 4)

**Razones**:

1. **Completa el Ecosistema** ✅
   - FluentValidation: input validation compleja
   - DataAnnotations: input validation simple
   - MiniValidator: input validation lightweight
   - **GuardClauses**: defensive programming + domain invariants

2. **Uso Estratégico** ✅
   - NO para validación de input (ya cubierto)
   - SÍ para domain models y state validation
   - SÍ para preconditions de domain services

3. **Zero Breaking Changes** ✅
   - Opt-in package
   - No modifica API actual
   - Compatible con todo el código existente

4. **Flexible** ✅
   - Estilo funcional (GuardExtensions)
   - Estilo imperativo (GuardHelpers)
   - Developer elige su preferencia

### 📦 Alcance de Implementación

**Incluir en SimpleMediator.GuardClauses**:

1. **Extension Methods Funcionales** (GuardExtensions)
   - `GuardNotNull<T>`
   - `GuardNotNullOrEmpty`
   - `GuardNotEmpty<T>` (collections)
   - `GuardPositive<T>`
   - `GuardInRange<T>`
   - `GuardValidEmail`
   - `GuardValidUrl`

2. **Helper Methods Imperativos** (GuardHelpers)
   - `EnsureNotNull<T>`
   - `EnsureNotNullOrEmpty`
   - `EnsureNotEmpty<T>`
   - `EnsurePositive<T>`
   - `EnsureInRange<T>`
   - `EnsureValidEmail`
   - `EnsureValidUrl`

3. **Documentación Clara**
   - README con ejemplos de cuándo usar guards vs validación de input
   - Best practices
   - Comparativa con las otras librerías

### ⚠️ Advertencias en la Documentación

El README debe dejar MUY claro:

```markdown
## ⚠️ When NOT to Use GuardClauses

**DON'T use GuardClauses for input validation** - that's what FluentValidation/DataAnnotations/MiniValidator are for.

❌ **Bad** - Redundant validation in handler:
```csharp
public class CreateUserHandler : ICommandHandler<CreateUser, UserId>
{
    public Task<Either<MediatorError, UserId>> Handle(CreateUser request, CancellationToken ct)
    {
        // ❌ BAD: Input already validated by FluentValidation
        return request.Email
            .GuardNotEmpty(nameof(request.Email))
            .BindAsync(email => CreateUser(email));
    }
}
```

✅ **Good** - Guards in domain models:

```csharp
public class User
{
    public User(string email, string password)
    {
        // ✅ GOOD: Protecting domain invariants
        Email = Guard.Against.NullOrEmpty(email, nameof(email));
        Password = Guard.Against.NullOrEmpty(password, nameof(password));
    }
}
```

✅ **Good** - Guards for state validation:

```csharp
public class CancelOrderHandler : ICommandHandler<CancelOrder, Unit>
{
    public async Task<Either<MediatorError, Unit>> Handle(CancelOrder request, CancellationToken ct)
    {
        var order = await _orders.FindById(request.OrderId, ct);

        // ✅ GOOD: Validating state retrieved from database
        return order
            .GuardNotNull(nameof(order), $"Order {request.OrderId} not found")
            .Bind(o => o.CanBeCancelled()
                ? Right<MediatorError, Order>(o)
                : Left<MediatorError, Order>(MediatorError.New("Order cannot be cancelled")))
            .BindAsync(async o =>
            {
                o.Cancel();
                await _orders.Save(o, ct);
                return Unit.Default;
            });
    }
}
```

```

---

## Implementación Sugerida

### Fase 1: Core Guards (Mínimo Viable)
- `GuardNotNull`
- `GuardNotNullOrEmpty`
- `GuardNotEmpty` (collections)
- Versiones funcionales e imperativas

### Fase 2: Guards Avanzados (Opcional)
- `GuardPositive`, `GuardNegative`
- `GuardInRange`
- `GuardValidEmail`, `GuardValidUrl`
- Custom guard builders

### Fase 3: Domain-Specific Guards (Futuro)
- Guards específicos para patrones DDD
- Integration con aggregate roots
- Invariant validation helpers

---

## Conclusión

**SimpleMediator.GuardClauses vale la pena SOLO SI**:
1. Se documenta claramente cuándo usarlo vs validación de input
2. Se enfoca en domain models y state validation
3. Se ofrece estilo funcional e imperativo
4. Se marca como "advanced" feature en la documentación

**Si se implementa correctamente**, completa el ecosistema de validación de SimpleMediator dando a los developers herramientas para TODOS los escenarios:
- **Input validation**: FluentValidation/DataAnnotations/MiniValidator
- **Defensive programming**: GuardClauses
- **Domain invariants**: GuardClauses en constructores/métodos de dominio

---

## Pregunta para el Usuario

**¿Quieres que implemente SimpleMediator.GuardClauses con el enfoque híbrido (funcional + imperativo)?**

**Alternativas**:
1. ✅ **Implementar** con Opción 4 (Hybrid - funcional + imperativo)
2. ⏸️ **Posponer** hasta que haya más feedback de usuarios
3. ❌ **No implementar** - los 3 packages de validación de input son suficientes
4. 🔧 **Implementar** pero solo estilo funcional (GuardExtensions)
5. 🔧 **Implementar** pero solo estilo imperativo (GuardHelpers)
