# SimpleMediator.GuardClauses

[![NuGet](https://img.shields.io/nuget/v/SimpleMediator.GuardClauses.svg)](https://www.nuget.org/packages/SimpleMediator.GuardClauses)
[![License](https://img.shields.io/github/license/dlrivada/SimpleMediator)](https://github.com/dlrivada/SimpleMediator/blob/main/LICENSE)

Defensive programming and domain invariant validation for SimpleMediator with Railway Oriented Programming (ROP) support.

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
- 🛤️ **ROP Integration** - Consistent with SimpleMediator's `Try-pattern` using `out` parameters
- 🎯 **Domain-Focused** - Designed for domain models and state validation
- ⚡ **Performance** - No exceptions in the happy path
- 🔄 **Consistent API** - Follows the same patterns as SimpleMediator's internal guards
- 🧪 **Fully Tested** - Comprehensive test coverage (68 tests)

## Installation

```bash
dotnet add package SimpleMediator.GuardClauses
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

    public async Task<Either<MediatorError, Unit>> Handle(
        CancelOrder request,
        CancellationToken ct)
    {
        var order = await _orders.FindById(request.OrderId, ct);

        // Validate state retrieved from database
        if (!Guards.TryValidateNotNull(order, nameof(order), out var error,
            message: $"Order {request.OrderId} not found"))
            return Left<MediatorError, Unit>(error);

        // Validate business rule
        if (!Guards.TryValidate(order.Status == OrderStatus.Pending, nameof(order), out error,
            message: "Only pending orders can be cancelled"))
            return Left<MediatorError, Unit>(error);

        order.Cancel();
        await _orders.Save(order, ct);
        return Right<MediatorError, Unit>(Unit.Default);
    }
}
```

### 3. Domain Service Preconditions

```csharp
public class OrderDomainService
{
    public Either<MediatorError, Order> CreateOrder(
        Customer customer,
        IEnumerable<OrderItem> items)
    {
        // Validate preconditions
        if (!Guards.TryValidateNotNull(customer, nameof(customer), out var error))
            return Left<MediatorError, Order>(error);

        if (!Guards.TryValidateNotEmpty(items, nameof(items), out error))
            return Left<MediatorError, Order>(error);

        if (!Guards.TryValidate(customer.IsActive, nameof(customer), out error,
            message: "Customer must be active to create orders"))
            return Left<MediatorError, Order>(error);

        var order = new Order(customer.Id, items.ToList());
        return Right<MediatorError, Order>(order);
    }
}
```

## Available Guards

### Null Checks

```csharp
// Validates that a value is not null
Guards.TryValidateNotNull<T>(T? value, string paramName, out MediatorError error, string? message = null)
```

### String Validation

```csharp
// Validates that a string is not null or empty
Guards.TryValidateNotEmpty(string? value, string paramName, out MediatorError error, string? message = null)

// Validates that a string is not null, empty, or whitespace
Guards.TryValidateNotWhiteSpace(string? value, string paramName, out MediatorError error, string? message = null)

// Validates email format
Guards.TryValidateEmail(string? value, string paramName, out MediatorError error, string? message = null)

// Validates URL format (HTTP/HTTPS)
Guards.TryValidateUrl(string? value, string paramName, out MediatorError error, string? message = null)

// Validates against regex pattern
Guards.TryValidatePattern(string? value, string paramName, string pattern, out MediatorError error, string? message = null)
```

### Collection Validation

```csharp
// Validates that a collection is not null or empty
Guards.TryValidateNotEmpty<T>(IEnumerable<T>? value, string paramName, out MediatorError error, string? message = null)
```

### Numeric Validation

```csharp
// Validates that a number is positive (> 0)
Guards.TryValidatePositive<T>(T value, string paramName, out MediatorError error, string? message = null) where T : IComparable<T>

// Validates that a number is negative (< 0)
Guards.TryValidateNegative<T>(T value, string paramName, out MediatorError error, string? message = null) where T : IComparable<T>

// Validates that a value is within an inclusive range
Guards.TryValidateInRange<T>(T value, string paramName, T min, T max, out MediatorError error, string? message = null) where T : IComparable<T>
```

### GUID Validation

```csharp
// Validates that a GUID is not empty
Guards.TryValidateNotEmpty(Guid value, string paramName, out MediatorError error, string? message = null)
```

### Custom Conditions

```csharp
// Validates a custom boolean condition
Guards.TryValidate(bool condition, string paramName, out MediatorError error, string? message = null)
```

## Design Philosophy

### Consistent with SimpleMediator's Internal Guards

SimpleMediator uses the **Try-pattern** internally for all guard validations:

```csharp
// SimpleMediator's internal pattern (MediatorBehaviorGuards)
if (!MediatorBehaviorGuards.TryValidateRequest(behaviorType, request, out var failure))
{
    return Left<MediatorError, TResponse>(failure);
}

// SimpleMediator.GuardClauses follows the same pattern
if (!Guards.TryValidateNotNull(order, nameof(order), out var error))
{
    return Left<MediatorError, Unit>(error);
}
```

**Key design principles**:

1. **Try-pattern**: Methods return `bool` with `out` parameter for errors
2. **Explicit**: Caller decides what to do with validation failures
3. **ROP-friendly**: Errors are `MediatorError` instances ready for `Either`
4. **Consistent**: Same pattern used throughout SimpleMediator

### Why Not Extension Methods?

We considered functional-style extension methods (from the analysis document):

```csharp
// Considered but not implemented
return order
    .GuardNotNull(nameof(order))
    .Bind(o => o.GuardCanBeCancelled())
    .BindAsync(async o => { ... });
```

**We chose static helpers instead** to match SimpleMediator's internal conventions:

- Consistent with `MediatorBehaviorGuards`, `MediatorRequestGuards`
- More explicit and predictable
- Familiar to developers already using SimpleMediator
- No extension method pollution

## Comparison with Other Libraries

| Library | Purpose | When to Use |
|---------|---------|-------------|
| **FluentValidation** | Complex input validation | Validating user requests with complex rules |
| **DataAnnotations** | Simple input validation | Validating user requests with attributes |
| **MiniValidator** | Lightweight input validation | Validating user requests in Minimal APIs |
| **GuardClauses** | Defensive programming | Protecting domain invariants & state validation |

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
    public async Task<Either<MediatorError, OrderId>> Handle(
        CreateOrder request, // Already validated!
        CancellationToken ct)
    {
        // Input validation already done by FluentValidation
        // DON'T re-validate with Guards here!

        // DO use Guards for state validation
        var customer = await _customers.FindById(request.CustomerId, ct);

        if (!Guards.TryValidateNotNull(customer, nameof(customer), out var error,
            message: $"Customer {request.CustomerId} not found"))
            return Left<MediatorError, OrderId>(error);

        // DO use Guards in domain models
        var order = new Order(customer.Id, request.Items); // Guards in constructor
        await _orders.Save(order, ct);

        return Right<MediatorError, OrderId>(order.Id);
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

    public async Task<Either<MediatorError, PaymentId>> Handle(
        ProcessPayment request,
        CancellationToken ct)
    {
        // Validate order exists
        var order = await _orders.FindById(request.OrderId, ct);
        if (!Guards.TryValidateNotNull(order, nameof(order), out var error,
            message: $"Order {request.OrderId} not found"))
            return Left<MediatorError, PaymentId>(error);

        // Validate order is ready for payment
        if (!Guards.TryValidate(order.Status == OrderStatus.Pending, nameof(order), out error,
            message: "Order must be in Pending status to process payment"))
            return Left<MediatorError, PaymentId>(error);

        // Validate amount matches
        if (!Guards.TryValidate(order.TotalAmount == request.Amount, nameof(request.Amount), out error,
            message: $"Payment amount {request.Amount} doesn't match order total {order.TotalAmount}"))
            return Left<MediatorError, PaymentId>(error);

        // Process payment
        var payment = await _gateway.ProcessPayment(order.TotalAmount, request.PaymentMethod, ct);
        order.MarkAsPaid(payment.Id);
        await _orders.Save(order, ct);

        return Right<MediatorError, PaymentId>(payment.Id);
    }
}
```

### User Management with Email Verification

```csharp
public sealed class VerifyEmailHandler : ICommandHandler<VerifyEmail, Unit>
{
    private readonly IUserRepository _users;

    public async Task<Either<MediatorError, Unit>> Handle(
        VerifyEmail request,
        CancellationToken ct)
    {
        // Validate token format
        if (!Guards.TryValidateNotEmpty(request.Token, nameof(request.Token), out var error))
            return Left<MediatorError, Unit>(error);

        if (!Guards.TryValidatePattern(request.Token, nameof(request.Token),
            @"^[0-9A-Fa-f]{64}$", out error,
            message: "Invalid verification token format"))
            return Left<MediatorError, Unit>(error);

        // Find user by token
        var user = await _users.FindByVerificationToken(request.Token, ct);
        if (!Guards.TryValidateNotNull(user, nameof(user), out error,
            message: "Invalid or expired verification token"))
            return Left<MediatorError, Unit>(error);

        // Validate not already verified
        if (!Guards.TryValidate(!user.EmailVerified, nameof(user), out error,
            message: "Email already verified"))
            return Left<MediatorError, Unit>(error);

        user.VerifyEmail();
        await _users.Save(user, ct);

        return Right<MediatorError, Unit>(Unit.Default);
    }
}
```

## Error Handling

All guards return `MediatorError` instances with:

- **Message**: Clear description of what failed
- **Error Code**: `Guards.GuardValidationFailed` constant
- **Metadata**: Diagnostic information (parameter name, type, guard type, etc.)

```csharp
if (!Guards.TryValidatePositive(quantity, nameof(quantity), out var error))
{
    // error.Message: "quantity must be positive (greater than zero)."
    // Error integrates seamlessly with Either<MediatorError, T>
    return Left<MediatorError, OrderId>(error);
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

Contributions are welcome! Please read the [contributing guidelines](https://github.com/dlrivada/SimpleMediator/blob/main/CONTRIBUTING.md) first.

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/dlrivada/SimpleMediator/blob/main/LICENSE) file for details.

## Related Packages

- **SimpleMediator** - Core mediator library
- **SimpleMediator.FluentValidation** - Complex input validation
- **SimpleMediator.DataAnnotations** - Simple input validation
- **SimpleMediator.MiniValidator** - Lightweight input validation for Minimal APIs

## Support

- 📖 [Documentation](https://github.com/dlrivada/SimpleMediator)
- 🐛 [Issue Tracker](https://github.com/dlrivada/SimpleMediator/issues)
- 💬 [Discussions](https://github.com/dlrivada/SimpleMediator/discussions)

## Summary

**GuardClauses** completes SimpleMediator's validation ecosystem:

- **Input Validation** (before handler): FluentValidation, DataAnnotations, MiniValidator
- **Defensive Programming** (inside handler/domain): **GuardClauses**

Use the right tool for the right job:

- Validate user input → Use input validation libraries
- Protect domain invariants → Use **GuardClauses**
- Validate state & preconditions → Use **GuardClauses**
