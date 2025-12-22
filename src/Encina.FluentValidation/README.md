# Encina.FluentValidation

[![NuGet](https://img.shields.io/nuget/v/Encina.FluentValidation.svg)](https://www.nuget.org/packages/Encina.FluentValidation)
[![License](https://img.shields.io/github/license/dlrivada/Encina)](https://github.com/dlrivada/Encina/blob/main/LICENSE)

FluentValidation integration for Encina with Railway Oriented Programming (ROP) support.

## Features

- 🚦 **Automatic Request Validation** - Validates requests before handler execution
- 🛤️ **ROP Integration** - Returns validation failures as `Left<EncinaError>` for functional error handling
- 🎯 **Zero Boilerplate** - No need to manually call validators in handlers
- 🔄 **Context Enrichment** - Passes correlation ID, user ID, and tenant ID to validators
- ⚡ **Parallel Validation** - Runs multiple validators concurrently for better performance
- 🧪 **Fully Tested** - Comprehensive test coverage with property-based tests

## Installation

```bash
dotnet add package Encina.FluentValidation
```

## Quick Start

### 1. Define Your Validators

```csharp
using FluentValidation;

public sealed record CreateUser(string Email, string Name, int Age) : ICommand<UserId>;

public sealed class CreateUserValidator : AbstractValidator<CreateUser>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Name too long");

        RuleFor(x => x.Age)
            .GreaterThanOrEqualTo(18).WithMessage("Must be 18 or older");
    }
}
```

### 2. Register FluentValidation

```csharp
using Encina.FluentValidation;

var services = new ServiceCollection();

// Register Encina with FluentValidation
services.AddEncina(cfg =>
{
    // FluentValidation behavior is automatically registered
}, typeof(CreateUser).Assembly);

// Register all validators from assemblies
services.AddEncinaFluentValidation(
    typeof(CreateUserValidator).Assembly);
```

### 3. Write Your Handler (Validation is Automatic!)

```csharp
public sealed class CreateUserHandler : ICommandHandler<CreateUser, UserId>
{
    private readonly IUserRepository _users;

    public CreateUserHandler(IUserRepository users) => _users = users;

    public async Task<Either<EncinaError, UserId>> Handle(
        CreateUser request,
        CancellationToken ct)
    {
        // request is GUARANTEED to be valid here!
        // No need to manually validate - the behavior did it for you

        var user = new User(request.Email, request.Name, request.Age);
        await _users.Save(user, ct);
        return Right<EncinaError, UserId>(user.Id);
    }
}
```

### 4. Handle Validation Errors Functionally

```csharp
var result = await Encina.Send(new CreateUser("invalid-email", "", 15));

result.Match(
    Right: userId => Console.WriteLine($"User created: {userId}"),
    Left: error =>
    {
        // Validation failed - error contains ValidationException
        Console.WriteLine($"Validation failed: {error.Message}");

        error.Exception.IfSome(ex =>
        {
            if (ex is ValidationException validationEx)
            {
                foreach (var failure in validationEx.Errors)
                {
                    Console.WriteLine($"  - {failure.PropertyName}: {failure.ErrorMessage}");
                }
            }
        });
    }
);
```

## Advanced Usage

### Context-Aware Validators

Access request context metadata (correlation ID, user ID, tenant ID) inside validators:

```csharp
public sealed class CreateOrderValidator : AbstractValidator<CreateOrder>
{
    private readonly IOrderRepository _orders;

    public CreateOrderValidator(IOrderRepository orders)
    {
        _orders = orders;

        RuleFor(x => x.ProductId).NotEmpty();

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .MustAsync(CheckInventory)
            .WithMessage("Insufficient inventory for tenant");
    }

    private async Task<bool> CheckInventory(
        CreateOrder order,
        int quantity,
        ValidationContext<CreateOrder> context,
        CancellationToken ct)
    {
        // Access tenant ID from request context
        var tenantId = context.RootContextData.TryGetValue("TenantId", out var tid)
            ? (string?)tid
            : null;

        if (tenantId is null) return false;

        var available = await _orders.GetAvailableInventory(
            order.ProductId,
            tenantId,
            ct);

        return available >= quantity;
    }
}
```

### Custom Validator Lifetime

By default, validators are registered as **Singletons** (recommended for stateless validators). If you need Scoped or Transient validators:

```csharp
// Register validators as Scoped (e.g., for EF Core DbContext dependency)
services.AddEncinaFluentValidation(
    ServiceLifetime.Scoped,
    typeof(CreateUserValidator).Assembly);
```

### Multiple Validators per Request

You can register multiple validators for the same request type - all will run in parallel:

```csharp
public sealed class CreateUserBusinessRulesValidator : AbstractValidator<CreateUser>
{
    public CreateUserBusinessRulesValidator(IUserRepository users)
    {
        RuleFor(x => x.Email)
            .MustAsync(async (email, ct) =>
                !await users.EmailExists(email, ct))
            .WithMessage("Email already registered");
    }
}

// Both CreateUserValidator and CreateUserBusinessRulesValidator will run
services.AddEncinaFluentValidation(typeof(CreateUser).Assembly);
```

### Validation Failure Structure

When validation fails, the error structure looks like this:

```csharp
EncinaError
{
    Message = "Validation failed for CreateUser with 3 error(s).",
    Exception = Some(ValidationException
    {
        Errors =
        [
            { PropertyName = "Email", ErrorMessage = "Email is required" },
            { PropertyName = "Name", ErrorMessage = "Name is required" },
            { PropertyName = "Age", ErrorMessage = "Must be 18 or older" }
        ]
    })
}
```

## How It Works

The `ValidationPipelineBehavior<TRequest, TResponse>` intercepts all requests:

1. **Resolve Validators**: Gets all `IValidator<TRequest>` from DI container
2. **Enrich Context**: Passes correlation ID, user ID, tenant ID to validation context
3. **Parallel Validation**: Runs all validators concurrently using `Task.WhenAll`
4. **Aggregate Failures**: Collects all validation errors from all validators
5. **Short-Circuit**: If validation fails, returns `Left<EncinaError>` with `ValidationException`
6. **Continue**: If validation passes, calls the next pipeline step (handler)

## Performance

- **Zero Allocation** when no validators are registered (fast path)
- **Parallel Validation** runs multiple validators concurrently
- **Singleton Lifetime** by default (validators created once, reused)
- **No Reflection** at runtime (all resolved via DI)

## Integration with ASP.NET Core

Example of extracting validation errors in minimal APIs:

```csharp
app.MapPost("/users", async (CreateUser request, IEncina Encina) =>
{
    var result = await Encina.Send(request);

    return result.Match(
        Right: userId => Results.Created($"/users/{userId}", userId),
        Left: error => error.Exception.Match(
            Some: ex => ex is ValidationException validationEx
                ? Results.ValidationProblem(validationEx.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray()))
                : Results.Problem(error.Message),
            None: () => Results.Problem(error.Message)
        )
    );
});
```

## Best Practices

### ✅ DO

- **Use validators for input validation** - format, required fields, ranges
- **Keep validators stateless when possible** - use Singleton lifetime
- **Compose small validators** - multiple validators per request type
- **Use async validators for DB checks** - uniqueness, existence, business rules
- **Access context metadata** - correlation ID, user ID, tenant ID

### ❌ DON'T

- **Don't use validators for business logic** - that belongs in handlers or domain services
- **Don't throw exceptions in validators** - return validation failures instead
- **Don't validate inside handlers** - let the behavior do it
- **Don't register validators manually** - use `AddEncinaFluentValidation`

## Testing

Testing handlers is simpler because validation is already done:

```csharp
[Fact]
public async Task CreateUserHandler_CreatesUser_WhenRequestIsValid()
{
    // Arrange
    var repository = new InMemoryUserRepository();
    var handler = new CreateUserHandler(repository);
    var request = new CreateUser("john@example.com", "John Doe", 25);

    // Act - no need to worry about validation in handler tests!
    var result = await handler.Handle(request, CancellationToken.None);

    // Assert
    result.IsRight.ShouldBeTrue();
    repository.Users.ShouldHaveSingleItem();
}
```

Test validation separately using the validator directly:

```csharp
[Fact]
public void CreateUserValidator_Fails_WhenEmailIsInvalid()
{
    // Arrange
    var validator = new CreateUserValidator();
    var request = new CreateUser("invalid-email", "John", 25);

    // Act
    var result = validator.Validate(request);

    // Assert
    result.IsValid.ShouldBeFalse();
    result.Errors.ShouldContain(e => e.PropertyName == "Email");
}
```

## Migration from Manual Validation

**Before** (manual validation in handler):

```csharp
public sealed class CreateUserHandler : ICommandHandler<CreateUser, UserId>
{
    private readonly IValidator<CreateUser> _validator;
    private readonly IUserRepository _users;

    public async Task<Either<EncinaError, UserId>> Handle(
        CreateUser request,
        CancellationToken ct)
    {
        // ❌ Manual validation boilerplate
        var validation = await _validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return EncinaError.New(new ValidationException(validation.Errors));
        }

        var user = new User(request.Email, request.Name, request.Age);
        await _users.Save(user, ct);
        return user.Id;
    }
}
```

**After** (automatic validation):

```csharp
public sealed class CreateUserHandler : ICommandHandler<CreateUser, UserId>
{
    private readonly IUserRepository _users;

    public async Task<Either<EncinaError, UserId>> Handle(
        CreateUser request,
        CancellationToken ct)
    {
        // ✅ No validation code needed - automatically validated!
        var user = new User(request.Email, request.Name, request.Age);
        await _users.Save(user, ct);
        return user.Id;
    }
}
```

## Contributing

Contributions are welcome! Please read the [contributing guidelines](https://github.com/dlrivada/Encina/blob/main/CONTRIBUTING.md) first.

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/dlrivada/Encina/blob/main/LICENSE) file for details.

## Related Packages

- **Encina** - Core Encina library
- **Encina.AspNetCore** - ASP.NET Core integration (coming soon)
- **Encina.OpenTelemetry** - Distributed tracing (coming soon)

## Support

- 📖 [Documentation](https://github.com/dlrivada/Encina)
- 🐛 [Issue Tracker](https://github.com/dlrivada/Encina/issues)
- 💬 [Discussions](https://github.com/dlrivada/Encina/discussions)
