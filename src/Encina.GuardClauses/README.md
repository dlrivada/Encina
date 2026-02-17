# Encina.GuardClauses

[![NuGet](https://img.shields.io/nuget/v/Encina.GuardClauses.svg)](https://www.nuget.org/packages/Encina.GuardClauses)
[![License](https://img.shields.io/github/license/dlrivada/Encina)](https://github.com/dlrivada/Encina/blob/main/LICENSE)

Defensive programming and domain invariant validation for Encina with Railway Oriented Programming (ROP) support.

## ⚠️ Important: GuardClauses vs Input Validation

**GuardClauses is NOT for input validation** - that's what FluentValidation/DataAnnotations/MiniValidator are for!

| Aspect | Input Validation Libraries | GuardClauses |
|--------|---------------------------|--------------|
| **When** | **BEFORE** handler execution (pipeline behavior) | **INSIDE** handlers/domain models |
| **What** | Validates external input from users | Validates preconditions & invariants |
| **Purpose** | Protect against invalid user input | Defensive programming against bugs |
| **Where** | Pipeline interception | Domain logic / State validation |

## Features

- 🛡️ **Defensive Programming** - Validate preconditions and domain invariants
- 🛤️ **ROP Integration** - Consistent with Encina's `Try-pattern` using `out` parameters
- 🎯 **Domain-Focused** - Designed for domain models and state validation
- ⚡ **Performance** - No exceptions in the happy path
- 🔄 **Consistent API** - Follows the same patterns as Encina's internal guards
- 🧪 **Fully Tested** - Comprehensive test coverage (68 tests)

## Installation

```bash
dotnet add package Encina.GuardClauses
```

**Dependencies**: Ardalis.GuardClauses

## Quick Start

### When to Use GuardClauses

✅ **DO Use For**:

- **Domain Model Invariants**: Protect class invariants in constructors
- **State Validation**: Validate objects retrieved from database
- **Preconditions**: Validate method preconditions in domain services

❌ **DON'T Use For**:

- **Input Validation**: Use FluentValidation/DataAnnotations/MiniValidator instead
- **Request Validation**: Input is already validated by pipeline behaviors

### 1. Domain Model Invariants

```csharp
public class User
{
    public User(string email, string password)
    {
        if (!Guards.TryValidateNotEmpty(email, nameof(email), out var emailError))
            throw new InvalidOperationException(emailError.Message);

        if (!Guards.TryValidateNotEmpty(password, nameof(password), out var pwdError))
            throw new InvalidOperationException(pwdError.Message);

        if (!Guards.TryValidateEmail(email, nameof(email), out var formatError))
            throw new InvalidOperationException(formatError.Message);

        Email = email;
        Password = password;
    }

    public string Email { get; }
    public string Password { get; }
}
```

### 2. State Validation in Handlers

```csharp
public sealed class CancelOrderHandler : ICommandHandler<CancelOrder, Unit>
{
    private readonly IOrderRepository _orders;

    public async Task<Either<EncinaError, Unit>> Handle(
        CancelOrder request,
        CancellationToken ct)
    {
        var order = await _orders.FindById(request.OrderId, ct);

        // Validate state retrieved from database
        if (!Guards.TryValidateNotNull(order, nameof(order), out var error,
            message: $"Order {request.OrderId} not found"))
            return Left<EncinaError, Unit>(error);

        // Validate business rule
        if (!Guards.TryValidate(order.Status == OrderStatus.Pending, nameof(order), out error,
            message: "Only pending orders can be cancelled"))
            return Left<EncinaError, Unit>(error);

        order.Cancel();
        await _orders.Save(order, ct);
        return Right<EncinaError, Unit>(Unit.Default);
    }
}
```

### 3. Domain Service Preconditions

```csharp
public class OrderDomainService
{
    public Either<EncinaError, Order> CreateOrder(
        Customer customer,
        IEnumerable<OrderItem> items)
    {
        // Validate preconditions
        if (!Guards.TryValidateNotNull(customer, nameof(customer), out var error))
            return Left<EncinaError, Order>(error);

        if (!Guards.TryValidateNotEmpty(items, nameof(items), out error))
            return Left<EncinaError, Order>(error);

        if (!Guards.TryValidate(customer.IsActive, nameof(customer), out error,
            message: "Customer must be active to create orders"))
            return Left<EncinaError, Order>(error);

        var order = new Order(customer.Id, items.ToList());
        return Right<EncinaError, Order>(order);
    }
}
```

## Available Guards

### Null Checks

```csharp
// Validates that a value is not null
Guards.TryValidateNotNull<T>(T? value, string paramName, out EncinaError error, string? message = null)
```

### String Validation

```csharp
// Validates that a string is not null or empty
Guards.TryValidateNotEmpty(string? value, string paramName, out EncinaError error, string? message = null)

// Validates that a string is not null, empty, or whitespace
Guards.TryValidateNotWhiteSpace(string? value, string paramName, out EncinaError error, string? message = null)

// Validates email format
Guards.TryValidateEmail(string? value, string paramName, out EncinaError error, string? message = null)

// Validates URL format (HTTP/HTTPS)
Guards.TryValidateUrl(string? value, string paramName, out EncinaError error, string? message = null)

// Validates against regex pattern
Guards.TryValidatePattern(string? value, string paramName, string pattern, out EncinaError error, string? message = null)
```

### Collection Validation

```csharp
// Validates that a collection is not null or empty
Guards.TryValidateNotEmpty<T>(IEnumerable<T>? value, string paramName, out EncinaError error, string? message = null)
```

### Numeric Validation

```csharp
// Validates that a number is positive (> 0)
Guards.TryValidatePositive<T>(T value, string paramName, out EncinaError error, string? message = null) where T : IComparable<T>

// Validates that a number is negative (< 0)
Guards.TryValidateNegative<T>(T value, string paramName, out EncinaError error, string? message = null) where T : IComparable<T>

// Validates that a value is within an inclusive range
Guards.TryValidateInRange<T>(T value, string paramName, T min, T max, out EncinaError error, string? message = null) where T : IComparable<T>
```

### GUID Validation

```csharp
// Validates that a GUID is not empty
Guards.TryValidateNotEmpty(Guid value, string paramName, out EncinaError error, string? message = null)
```

### Custom Conditions

```csharp
// Validates a custom boolean condition
Guards.TryValidate(bool condition, string paramName, out EncinaError error, string? message = null)
```

## Design Philosophy

### Two Approaches: Try-Pattern and Ensure (Choose Your Style)

Encina provides **two complementary approaches** for ROP guard validation. Both are fully supported — use whichever fits your team's style and the specific scenario.

#### Approach 1: Try-Pattern (Imperative Style)

The `Guards` static class uses the **Try-pattern**: methods return `bool` for success/failure and provide error details via `out EncinaError`. This is the same pattern used internally by Encina (`EncinaBehaviorGuards`, `EncinaRequestGuards`).

```csharp
public async Task<Either<EncinaError, OrderId>> Handle(CancelOrder cmd, CancellationToken ct)
{
    var order = await _orders.FindById(cmd.OrderId, ct);

    // Guard: order exists
    if (!Guards.TryValidateNotNull(order, nameof(order), out var error,
        message: $"Order {cmd.OrderId} not found"))
        return Left<EncinaError, OrderId>(error);

    // Guard: order is in correct state
    if (!Guards.TryValidate(order.Status == OrderStatus.Pending, nameof(order), out error,
        message: "Only pending orders can be cancelled"))
        return Left<EncinaError, OrderId>(error);

    // Guard: amount is positive
    if (!Guards.TryValidatePositive(order.TotalAmount, nameof(order.TotalAmount), out error))
        return Left<EncinaError, OrderId>(error);

    order.Cancel();
    await _orders.Save(order, ct);
    return Right<EncinaError, OrderId>(order.Id);
}
```

**When to prefer Try-Pattern:**

- ✅ Familiar imperative code flow (if/return)
- ✅ Each guard is explicit and self-contained
- ✅ Easy to debug (breakpoints on each guard)
- ✅ Rich structured metadata per guard (parameter name, type, guard kind, actual value)
- ✅ Best for developers coming from traditional C#/.NET

#### Approach 2: Ensure Extension (Functional Style)

The `EitherExtensions.Ensure()` method (from `Encina.DomainModeling`) enables fluent, chainable validation:

```csharp
public async Task<Either<EncinaError, OrderId>> Handle(CancelOrder cmd, CancellationToken ct)
{
    var order = await _orders.FindById(cmd.OrderId, ct);

    return Right<EncinaError, Order>(order!)
        .Ensure(
            o => o is not null,
            _ => EncinaErrors.Create("order.not_found", $"Order {cmd.OrderId} not found"))
        .Ensure(
            o => o.Status == OrderStatus.Pending,
            o => EncinaErrors.Create("order.invalid_status", $"Order is {o.Status}, expected Pending"))
        .Ensure(
            o => o.TotalAmount > 0,
            o => EncinaErrors.Create("order.invalid_amount", $"Amount {o.TotalAmount} is not positive"))
        .Map(o =>
        {
            o.Cancel();
            return o.Id;
        });
}
```

**When to prefer Ensure:**

- ✅ Chainable — compose multiple validations fluently
- ✅ No temporary variables (`out var error`)
- ✅ Natural for developers familiar with functional programming
- ✅ Reads as a pipeline of constraints
- ✅ Pairs well with `Bind`, `Map`, `BindAsync`, `Tap`

#### Side-by-Side Comparison

| Aspect | Try-Pattern (`Guards`) | Ensure (`EitherExtensions`) |
|--------|----------------------|---------------------------|
| **Style** | Imperative (if/return) | Functional (chain) |
| **Package** | `Encina.GuardClauses` | `Encina.DomainModeling` |
| **Structured metadata** | Automatic (guard type, param, value) | Manual (you build `EncinaError`) |
| **Debugging** | Breakpoint per guard | Breakpoint on lambda |
| **Composability** | Sequential guards | Fluent chains |
| **Learning curve** | Low (familiar C# patterns) | Medium (functional programming) |
| **Best for** | State validation, preconditions | Domain invariants, pipelines |

#### Mixing Both Styles

You can freely mix both approaches in the same codebase. A common pattern is to use Try-Pattern for individual preconditions and Ensure for chained domain rules:

```csharp
public Either<EncinaError, Order> CreateOrder(Customer customer, IEnumerable<OrderItem> items)
{
    // Preconditions with Try-Pattern
    if (!Guards.TryValidateNotNull(customer, nameof(customer), out var error))
        return Left<EncinaError, Order>(error);

    if (!Guards.TryValidateNotEmpty(items, nameof(items), out error))
        return Left<EncinaError, Order>(error);

    // Domain rules with Ensure
    return Right<EncinaError, Customer>(customer)
        .Ensure(
            c => c.IsActive,
            c => EncinaErrors.Create("customer.inactive", $"Customer {c.Id} is not active"))
        .Ensure(
            c => c.CreditLimit > 0,
            c => EncinaErrors.Create("customer.no_credit", $"Customer {c.Id} has no credit limit"))
        .Map(c => new Order(c.Id, items.ToList()));
}
```

### Why Not Only Extension Methods?

We considered offering *only* functional-style extension methods, but chose to also provide the Try-Pattern static helpers:

- Consistent with `EncinaBehaviorGuards`, `EncinaRequestGuards` (Encina's internal conventions)
- More explicit and predictable for imperative code
- Familiar to the majority of .NET developers
- No extension method pollution on all types
- Automatic structured metadata without manual `EncinaError` construction

### Design Decision: ObjectDisposedException and ROP

.NET 7+ provides `ObjectDisposedException.ThrowIf(bool, object)` for detecting disposed objects. Encina **intentionally does not provide** an ROP equivalent (`TryValidateNotDisposed`) because:

1. **Programming error, not domain error**: A disposed object is a bug in resource lifecycle management, not a recoverable domain condition. It should never happen in production code that is correctly written.

2. **Exceptions are appropriate here**: Unlike domain validation (where we want `Either<EncinaError, T>` to flow through the railway), a disposed object indicates a **broken invariant at the infrastructure level**. The correct response is to throw, not to return `Left`.

3. **Not a domain concept**: Guard clauses in Encina are designed for **domain invariants** and **state validation**. Disposal state is an infrastructure concern that belongs in the infrastructure layer, not the domain.

**Recommended approach**: Use standard .NET `ObjectDisposedException.ThrowIf()` for disposal checks:

```csharp
public class OrderRepository : IOrderRepository, IDisposable
{
    private bool _disposed;

    public async Task<Order?> FindById(OrderId id, CancellationToken ct)
    {
        // ✅ Use .NET's built-in throw helper — this IS a programming error
        ObjectDisposedException.ThrowIf(_disposed, this);

        // Domain logic continues with ROP...
        var order = await _context.Orders.FindAsync(id, ct);
        if (!Guards.TryValidateNotNull(order, nameof(order), out var error))
            return null; // or handle via ROP
        return order;
    }

    public void Dispose()
    {
        _disposed = true;
        _context.Dispose();
    }
}
```

**Rule of thumb**: If the error represents a **domain condition** (order not found, invalid state, business rule violation) → use ROP (`Either<EncinaError, T>`). If the error represents a **programming bug** (null reference, disposed object, thread safety violation) → throw an exception.

## Comparison with Other Libraries

| Library | Purpose | Style | When to Use |
|---------|---------|-------|-------------|
| **FluentValidation** | Complex input validation | Pipeline behavior | Validating user requests with complex rules |
| **DataAnnotations** | Simple input validation | Pipeline behavior | Validating user requests with attributes |
| **MiniValidator** | Lightweight input validation | Pipeline behavior | Validating user requests in Minimal APIs |
| **GuardClauses (Try-Pattern)** | Defensive programming | Imperative | Protecting domain invariants & state validation |
| **EitherExtensions.Ensure** | Functional guards | Functional | Chaining domain constraints in pipelines |

### Example: Complete Validation Strategy

```csharp
// 1. Input Validation (BEFORE handler) - use FluentValidation
public class CreateOrderValidator : AbstractValidator<CreateOrder>
{
    public CreateOrderValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Items).NotEmpty();
        RuleFor(x => x.Items).Must(items => items.All(i => i.Quantity > 0));
    }
}

// 2. Handler receives VALIDATED input - no need for guards here!
public class CreateOrderHandler : ICommandHandler<CreateOrder, OrderId>
{
    public async Task<Either<EncinaError, OrderId>> Handle(
        CreateOrder request, // Already validated!
        CancellationToken ct)
    {
        // Input validation already done by FluentValidation
        // DON'T re-validate with Guards here!

        // DO use Guards for state validation
        var customer = await _customers.FindById(request.CustomerId, ct);

        if (!Guards.TryValidateNotNull(customer, nameof(customer), out var error,
            message: $"Customer {request.CustomerId} not found"))
            return Left<EncinaError, OrderId>(error);

        // DO use Guards in domain models
        var order = new Order(customer.Id, request.Items); // Guards in constructor
        await _orders.Save(order, ct);

        return Right<EncinaError, OrderId>(order.Id);
    }
}

// 3. Domain Model - use Guards for invariants
public class Order
{
    public Order(CustomerId customerId, List<OrderItem> items)
    {
        if (!Guards.TryValidateNotNull(customerId, nameof(customerId), out var error))
            throw new InvalidOperationException(error.Message);

        if (!Guards.TryValidateNotEmpty(items, nameof(items), out error))
            throw new InvalidOperationException(error.Message);

        CustomerId = customerId;
        Items = items;
        TotalAmount = items.Sum(i => i.Price * i.Quantity);
    }

    public CustomerId CustomerId { get; }
    public List<OrderItem> Items { get; }
    public decimal TotalAmount { get; }
}
```

## Best Practices

### ✅ DO

- **Use Guards in Domain Models** - Protect class invariants
- **Use Guards for State Validation** - Validate objects from database
- **Use Guards in Domain Services** - Validate method preconditions
- **Provide Clear Error Messages** - Use the optional `message` parameter
- **Use Guards for Business Rules** - Validate domain-specific conditions

### ❌ DON'T

- **Don't Use for Input Validation** - Use FluentValidation/DataAnnotations/MiniValidator
- **Don't Re-validate Already Validated Input** - Trust your pipeline behaviors
- **Don't Use Guards in Pipeline Behaviors** - That's for input validation libraries
- **Don't Overuse Guards** - Only where they add value (domain logic)

## Real-World Examples

### E-Commerce Order Processing

```csharp
public sealed class ProcessPaymentHandler : ICommandHandler<ProcessPayment, PaymentId>
{
    private readonly IOrderRepository _orders;
    private readonly IPaymentGateway _gateway;

    public async Task<Either<EncinaError, PaymentId>> Handle(
        ProcessPayment request,
        CancellationToken ct)
    {
        // Validate order exists
        var order = await _orders.FindById(request.OrderId, ct);
        if (!Guards.TryValidateNotNull(order, nameof(order), out var error,
            message: $"Order {request.OrderId} not found"))
            return Left<EncinaError, PaymentId>(error);

        // Validate order is ready for payment
        if (!Guards.TryValidate(order.Status == OrderStatus.Pending, nameof(order), out error,
            message: "Order must be in Pending status to process payment"))
            return Left<EncinaError, PaymentId>(error);

        // Validate amount matches
        if (!Guards.TryValidate(order.TotalAmount == request.Amount, nameof(request.Amount), out error,
            message: $"Payment amount {request.Amount} doesn't match order total {order.TotalAmount}"))
            return Left<EncinaError, PaymentId>(error);

        // Process payment
        var payment = await _gateway.ProcessPayment(order.TotalAmount, request.PaymentMethod, ct);
        order.MarkAsPaid(payment.Id);
        await _orders.Save(order, ct);

        return Right<EncinaError, PaymentId>(payment.Id);
    }
}
```

### User Management with Email Verification

```csharp
public sealed class VerifyEmailHandler : ICommandHandler<VerifyEmail, Unit>
{
    private readonly IUserRepository _users;

    public async Task<Either<EncinaError, Unit>> Handle(
        VerifyEmail request,
        CancellationToken ct)
    {
        // Validate token format
        if (!Guards.TryValidateNotEmpty(request.Token, nameof(request.Token), out var error))
            return Left<EncinaError, Unit>(error);

        if (!Guards.TryValidatePattern(request.Token, nameof(request.Token),
            @"^[0-9A-Fa-f]{64}$", out error,
            message: "Invalid verification token format"))
            return Left<EncinaError, Unit>(error);

        // Find user by token
        var user = await _users.FindByVerificationToken(request.Token, ct);
        if (!Guards.TryValidateNotNull(user, nameof(user), out error,
            message: "Invalid or expired verification token"))
            return Left<EncinaError, Unit>(error);

        // Validate not already verified
        if (!Guards.TryValidate(!user.EmailVerified, nameof(user), out error,
            message: "Email already verified"))
            return Left<EncinaError, Unit>(error);

        user.VerifyEmail();
        await _users.Save(user, ct);

        return Right<EncinaError, Unit>(Unit.Default);
    }
}
```

## Error Handling

All guards return `EncinaError` instances with:

- **Message**: Clear description of what failed
- **Error Code**: `Guards.GuardValidationFailed` constant
- **Metadata**: Diagnostic information (parameter name, type, guard type, etc.)

```csharp
if (!Guards.TryValidatePositive(quantity, nameof(quantity), out var error))
{
    // error.Message: "quantity must be positive (greater than zero)."
    // Error integrates seamlessly with Either<EncinaError, T>
    return Left<EncinaError, OrderId>(error);
}
```

## Testing

Guards make testing straightforward since validation happens explicitly in your code:

```csharp
[Fact]
public async Task Handle_WithNullOrder_ShouldReturnError()
{
    // Arrange
    var handler = new CancelOrderHandler(_mockOrders.Object);
    _mockOrders.Setup(x => x.FindById(It.IsAny<OrderId>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync((Order?)null);

    // Act
    var result = await handler.Handle(new CancelOrder(OrderId.New()), CancellationToken.None);

    // Assert
    result.IsLeft.ShouldBeTrue();
    result.Match(
        Right: _ => throw new InvalidOperationException("Expected error"),
        Left: error => error.Message.ShouldContain("not found")
    );
}
```

## Performance

- **Zero allocation** on success path
- **No exceptions** in happy path
- **Fast validation** using efficient .NET primitives
- **Minimal overhead** compared to throwing exceptions

## Contributing

Contributions are welcome! Please read the [contributing guidelines](https://github.com/dlrivada/Encina/blob/main/CONTRIBUTING.md) first.

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/dlrivada/Encina/blob/main/LICENSE) file for details.

## Related Packages

- **Encina** - Core Encina library
- **Encina.FluentValidation** - Complex input validation
- **Encina.DataAnnotations** - Simple input validation
- **Encina.MiniValidator** - Lightweight input validation for Minimal APIs

## Support

- 📖 [Documentation](https://github.com/dlrivada/Encina)
- 🐛 [Issue Tracker](https://github.com/dlrivada/Encina/issues)
- 💬 [Discussions](https://github.com/dlrivada/Encina/discussions)

## ROP vs Exceptions: When to Use What

Encina follows a clear principle: **domain errors flow through the railway; programming bugs throw exceptions**.

| Scenario | Pattern | Why |
|----------|---------|-----|
| Order not found after DB query | `Guards.TryValidateNotNull` → `Either` | Recoverable domain condition |
| Invalid business state | `Guards.TryValidate` → `Either` | Domain rule violation |
| Negative quantity in domain model | `Guards.TryValidatePositive` → `Either` | Domain invariant |
| Value out of allowed range | `Guards.TryValidateInRange` → `Either` | Business constraint |
| Null constructor argument in domain model | `throw` via Guards or `ArgumentNullException.ThrowIfNull` | Programming error (broken contract) |
| Disposed object accessed | `ObjectDisposedException.ThrowIf` | Infrastructure bug |
| Thread safety violation | `throw InvalidOperationException` | Programming error |
| Unexpected enum value | `throw ArgumentOutOfRangeException` | Programming error |

### .NET 10 Throw Helpers vs Encina Guards

.NET 8–10 introduced static throw helpers (`ThrowIfNull`, `ThrowIfNegative`, `ThrowIfLessThan`, etc.) across `ArgumentNullException`, `ArgumentException`, `ArgumentOutOfRangeException`, and `ObjectDisposedException`. These are **exception-based** and designed for traditional guard clauses.

Encina's `Guards.TryValidateXxx` methods provide **ROP-compatible equivalents** that return errors through the railway instead of throwing:

```csharp
// .NET 10 (exception-based) — use in constructors, infrastructure code
ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);

// Encina (ROP-based) — use in handlers, domain services, anywhere you use Either
if (!Guards.TryValidatePositive(quantity, nameof(quantity), out var error))
    return Left<EncinaError, OrderId>(error);
```

**Both coexist in the same codebase**. Use .NET throw helpers where exceptions are appropriate, and Encina guards where ROP flow is needed.

## Summary

**GuardClauses** completes Encina's validation ecosystem:

- **Input Validation** (before handler): FluentValidation, DataAnnotations, MiniValidator
- **Defensive Programming** (inside handler/domain): **GuardClauses** (Try-Pattern) or **EitherExtensions.Ensure** (Functional)
- **Programming Error Detection** (infrastructure): .NET built-in throw helpers

Use the right tool for the right job:

- Validate user input → Use input validation libraries (pipeline behaviors)
- Protect domain invariants (imperative) → Use **Guards.TryValidateXxx**
- Chain domain constraints (functional) → Use **EitherExtensions.Ensure**
- Detect programming bugs → Use .NET **ThrowIfXxx** helpers
